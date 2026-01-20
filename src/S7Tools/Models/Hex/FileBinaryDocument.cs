using System;
using System.IO;
using AvaloniaHex.Document;

namespace S7Tools.Models.Hex
{
    public class FileBinaryDocument : IBinaryDocument
    {
        private readonly FileStream _fileStream;
        private readonly object _lock = new();

        public FileBinaryDocument(string filePath)
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Length = (ulong)_fileStream.Length;

            _validRanges = new BitRangeUnion(new[] { new BitRange(0, (ulong)Length * 8) });
        }

        public event EventHandler<BinaryDocumentChange>? Changed;

        public ulong Length { get; }

        public bool IsReadOnly => true;

        public bool CanInsert => false;

        public bool CanRemove => false;

        public IReadOnlyBitRangeUnion ValidRanges => _validRanges;

        private readonly BitRangeUnion _validRanges;

        public void Dispose()
        {
            _fileStream.Dispose();
        }

        public void Flush()
        {
            // Read-only 
        }

        public void InsertBytes(ulong offset, ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

        public void ReadBytes(ulong offset, Span<byte> buffer)
        {
            lock (_lock)
            {
                if (offset >= Length)
                    return;

                _fileStream.Position = (long)offset;
                _fileStream.Read(buffer);
            }
        }

        public void RemoveBytes(ulong offset, ulong length) => throw new NotSupportedException();

        public void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
    }
}
