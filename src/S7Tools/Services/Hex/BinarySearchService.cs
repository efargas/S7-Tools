using AvaloniaHex.Document;

namespace S7Tools.Services.Hex
{
    /// <summary>
    /// Represents the IBinarySearchService.
    /// </summary>
    public interface IBinarySearchService
    {
        Task<IEnumerable<long>> FindAllAsync(IBinaryDocument doc, byte[] pattern, CancellationToken ct = default);
        Task<long> FindNextAsync(IBinaryDocument doc, byte[] pattern, long startOffset, CancellationToken ct = default);
    }

    /// <summary>
    /// Represents the BinarySearchService.
    /// </summary>
    public class BinarySearchService : IBinarySearchService
    {
        // 64KB buffer size for reading
        private const int BufferSize = 64 * 1024;

        /// <summary>
        /// Executes the FindAllAsync operation.
        /// </summary>
        public async Task<IEnumerable<long>> FindAllAsync(IBinaryDocument doc, byte[] pattern, CancellationToken ct = default)
        {
            var results = new List<long>();
            if (doc == null || pattern == null || pattern.Length == 0)
            {
                return results;
            }

            long docLength = (long)doc.Length;
            if (pattern.Length > docLength)
            {
                return results;
            }

            // We will read in chunks. 
            // To handle matches crossing chunk boundaries, we need to overlap reading.
            // Overlap size should be pattern.Length - 1.

            int patternLength = pattern.Length;
            int overlap = patternLength - 1;
            int effectiveBufferSize = BufferSize;

            // Allocate buffer
            byte[] buffer = new byte[effectiveBufferSize];
            long currentOffset = 0;

            // Simple naive search for now. Boyer-Moore or KMP would be better for very long patterns,
            // but for typical hex search (short patterns), naive is usually fine.
            // We can optimize later if needed.

            await Task.Run(() =>
            {
                while (currentOffset < docLength && !ct.IsCancellationRequested)
                {
                    // Calculate how much to read
                    long remaining = docLength - currentOffset;
                    int readSize = (int)Math.Min(remaining, effectiveBufferSize);

                    // Read into buffer
                    var span = buffer.AsSpan(0, readSize);

                    // Since IBinaryDocument.ReadBytes is synchronous in the interface (typically), we wrap in Task.Run just in case
                    // or just call it if it's fast. FileBinaryDocument uses FileStream.Read which is sync but might block.
                    // The interface provided in context shows ReadBytes(ulong offset, Span<byte> buffer).

                    doc.ReadBytes((ulong)currentOffset, span);

                    // Search in buffer
                    // We only search up to readSize - patternLength + 1 within this buffer
                    // UNLESS it's the last chunk. 
                    // However, because we overlap, we can search the whole 'safe' area.

                    // Actually, a simpler strategy for overlap:
                    // Read chunk. Search. 
                    // Next chunk starts at currentOffset + readSize - overlap.
                    // This way we ensure we don't miss cross-boundary matches.
                    // BUT we must avoid duplicate finds if we overlap.
                    // duplicate finds happen if we find a match in the overlapped region that was also found in previous iteration?
                    // No, because we advance currentOffset by (readSize - overlap).
                    // So the "new" bytes start at the overlap point.
                    // Wait, if we advance by readSize - overlap, the new buffer *starts* with the overlap bytes.
                    // So we search from index 0 of the new buffer. 
                    // Any match starting at index 0 of new buffer corresponds to offset (currentOffset + readSize - overlap).
                    // This is correct.

                    // Wait, let's trace:
                    // Chunk 1: [0...100]. Pattern len 4. Overlap 3.
                    // We search indices 0 to 97 (100-4+1).
                    // If we match at 97, keys are 97,98,99,100. (Wait, index 97 means bytes at 97,98,99,100... oh indices are 0-based).
                    // Bytes at 97,98,99, (need 100 which is outside).
                    // So we can only search up to readSize - patternLength.
                    // Matches start at index i where i + patternLength <= readSize.

                    // Loop for searching in buffer
                    for (int i = 0; i <= readSize - patternLength; i++)
                    {
                        if (IsMatch(span, i, pattern))
                        {
                            results.Add(currentOffset + i);
                        }
                    }

                    // Advance
                    if (remaining <= effectiveBufferSize)
                    {
                        // End of file
                        break;
                    }

                    // Move forward, but back up by overlap to catch boundary cases
                    currentOffset += (readSize - overlap);
                }
            }, ct).ConfigureAwait(false);

            return results;
        }

        /// <summary>
        /// Executes the FindNextAsync operation.
        /// </summary>
        public async Task<long> FindNextAsync(IBinaryDocument doc, byte[] pattern, long startOffset, CancellationToken ct = default)
        {
            if (doc == null || pattern == null || pattern.Length == 0)
            {
                return -1;
            }

            long docLength = (long)doc.Length;
            if (startOffset >= docLength)
            {
                return -1;
            }

            int patternLength = pattern.Length;
            int overlap = patternLength - 1;

            byte[] buffer = new byte[BufferSize];
            long currentOffset = startOffset;

            return await Task.Run<long>(() =>
            {
                while (currentOffset < docLength && !ct.IsCancellationRequested)
                {
                    long remaining = docLength - currentOffset;
                    int readSize = (int)Math.Min(remaining, BufferSize);
                    var span = buffer.AsSpan(0, readSize);

                    doc.ReadBytes((ulong)currentOffset, span);

                    for (int i = 0; i <= readSize - patternLength; i++)
                    {
                        if (IsMatch(span, i, pattern))
                        {
                            return currentOffset + i;
                        }
                    }

                    if (remaining <= BufferSize)
                    {
                        break;
                    }

                    currentOffset += (readSize - overlap);
                }
                return -1;
            }, ct).ConfigureAwait(false);
        }

        private bool IsMatch(ReadOnlySpan<byte> buffer, int offset, byte[] pattern)
        {
            // buffer slice from offset, length pattern.Length
            var slice = buffer.Slice(offset, pattern.Length);
            return slice.SequenceEqual(pattern);
        }
    }
}
