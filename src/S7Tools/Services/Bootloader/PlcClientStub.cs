using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// A lightweight stub implementation of IPlcClient that logs all calls and simulates responses.
/// Replace with a real implementation when the reference PLC client is available.
/// </summary>
public sealed class PlcClientStub : IPlcClient
{
    private readonly ILogger<PlcClientStub> _logger;
    private bool _disposed;

    public PlcClientStub(ILogger<PlcClientStub> logger)
    {
        _logger = logger;
        _logger.LogInformation("PlcClientStub created");
    }

    public Task HandshakeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[PLC] HandshakeAsync invoked");
        return Task.CompletedTask;
    }

    public Task<string> GetBootloaderVersionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[PLC] GetBootloaderVersionAsync invoked");
        return Task.FromResult("BOOTLOADER-STUB 0.0.1");
    }

    public Task InstallStagerAsync(byte[] stager, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[PLC] InstallStagerAsync invoked (size: {Size} bytes)", stager?.Length ?? 0);
        return Task.CompletedTask;
    }

    public Task<byte[]> DumpMemoryAsync(uint address, uint length, byte[] dumpPayload, IProgress<long> progress, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[PLC] DumpMemoryAsync invoked (addr: 0x{Address:X}, len: {Length})", address, length);

        // Simulate progressive reporting
        long total = length;
        long step = Math.Max(1024, total / 10);
        long sent = 0;
        while (sent < total)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sent = Math.Min(total, sent + step);
            progress?.Report(sent);
        }

        // Return a zero filled array of requested size
        return Task.FromResult(new byte[length]);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }
        _disposed = true;
        _logger.LogInformation("PlcClientStub disposed");
        return ValueTask.CompletedTask;
    }
}
