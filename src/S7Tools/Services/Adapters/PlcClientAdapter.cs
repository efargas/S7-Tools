// PlcClientAdapter.cs
namespace S7Tools.Services;

using S7Tools.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using S7.Net;

public sealed class PlcClientAdapter : IPlcClient
{
    private readonly Plc _inner;

    public PlcClientAdapter(Plc inner) => _inner = inner;

    public async Task HandshakeAsync(CancellationToken ct = default)
    {
        await _inner.OpenAsync();
    }

    public Task<string> GetBootloaderVersionAsync(CancellationToken ct = default)
    {
        // S7.Net.Plc does not have a direct equivalent, returning a placeholder.
        return Task.FromResult("S7.Net.Plc");
    }

    public async Task InstallStagerAsync(byte[] stager, CancellationToken ct = default)
    {
        if (stager == null || stager.Length == 0)
        {
            throw new ArgumentNullException(nameof(stager));
        }
        // Assuming the stager is written to DB1 at offset 0.
        // This would need to be replaced with the actual implementation details.
        await _inner.WriteBytesAsync(DataType.DataBlock, 1, 0, stager, ct);
    }

    public async Task<byte[]> DumpMemoryAsync(uint startAddress, uint length, byte[] dumperPayload, IProgress<long> progress, CancellationToken ct = default)
    {
        // A real implementation would first write the dumperPayload to the PLC.
        // For now, we simulate this step.
        if (dumperPayload != null && dumperPayload.Length > 0)
        {
            // Placeholder for writing the dumper payload.
            // await _inner.WriteBytesAsync(DataType.DataBlock, 2, 0, dumperPayload, ct);
        }

        var buffer = new byte[length];
        uint bytesRead = 0;
        const int chunkSize = 1024; // Read in 1KB chunks

        while (bytesRead < length)
        {
            ct.ThrowIfCancellationRequested();

            var bytesToRead = (int)Math.Min(chunkSize, length - bytesRead);
            var chunk = await _inner.ReadBytesAsync(DataType.DataBlock, 1, (int)(startAddress + bytesRead), bytesToRead, ct);

            if (chunk == null || chunk.Length == 0)
            {
                throw new InvalidOperationException("Failed to read memory from PLC.");
            }

            Array.Copy(chunk, 0, buffer, bytesRead, chunk.Length);
            bytesRead += (uint)chunk.Length;

            progress?.Report(bytesRead);
        }

        return buffer;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Close();
        return ValueTask.CompletedTask;
    }
}