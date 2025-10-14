// PlcClientAdapter.cs
namespace S7Tools.Services.Adapters;

using S7Tools.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using S7.Net;
using Microsoft.Extensions.Logging;

public sealed class PlcClientAdapter : IPlcClient
{
    private readonly Plc _inner;
    private readonly ILogger<PlcClientAdapter> _logger;

    public PlcClientAdapter(Plc inner, ILogger<PlcClientAdapter> logger)
    {
        _inner = inner;
        _logger = logger;
    }

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

        _logger.LogInformation("Simulating installing stager with payload size {PayloadSize} bytes.", stager.Length);

        // Simulate a delay for writing the stager to the PLC
        await Task.Delay(250, ct); // A slightly longer delay to simulate a more complex write operation

        _logger.LogInformation("Simulated stager installation completed successfully.");
    }

    public async Task<byte[]> DumpMemoryAsync(uint startAddress, uint length, byte[] dumperPayload, IProgress<long> progress, CancellationToken ct = default)
    {
        _logger.LogInformation("Simulating memory dump from address {StartAddress} with length {Length}.", startAddress, length);

        // A real implementation would first write the dumperPayload to the PLC.
        // For now, we simulate this step.
        if (dumperPayload != null && dumperPayload.Length > 0)
        {
            _logger.LogInformation("Simulating writing dumper payload ({PayloadLength} bytes) to PLC.", dumperPayload.Length);
            // Simulate a short delay for writing the payload
            await Task.Delay(100, ct);
        }

        var buffer = new byte[length];
        var random = new Random();
        random.NextBytes(buffer); // Fill with random data to simulate a memory dump.

        long bytesRead = 0;
        const int chunkSize = 4096; // Simulate reading in 4KB chunks

        _logger.LogInformation("Simulating reading {Length} bytes in {ChunkSize} byte chunks.", length, chunkSize);

        while (bytesRead < length)
        {
            ct.ThrowIfCancellationRequested();

            var bytesToRead = (int)Math.Min(chunkSize, length - bytesRead);

            // Simulate reading a chunk
            await Task.Delay(50, ct); // Simulate network latency for each chunk

            bytesRead += bytesToRead;

            progress?.Report(bytesRead);
            _logger.LogDebug("Simulated reading progress: {BytesRead}/{Length} bytes.", bytesRead, length);
        }

        _logger.LogInformation("Simulated memory dump completed successfully.");
        return buffer;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Close();
        return ValueTask.CompletedTask;
    }
}