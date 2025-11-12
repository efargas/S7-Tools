using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Adapters;

/// <summary>
/// Adapter for high-level PLC client operations.
/// Wraps the reference PlcClient with S7Tools' IPlcClient interface.
/// </summary>
/// <remarks>
/// This is a stub implementation. Will be replaced with actual adapter
/// wrapping SiemensS7-Bootloader reference implementation.
/// </remarks>
public sealed class PlcClientAdapter : IPlcClient
{
    private readonly ILogger<PlcClientAdapter> _logger;
    private bool _disposed;

    public PlcClientAdapter(ILogger<PlcClientAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task HandshakeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlcClientAdapter.HandshakeAsync (STUB)");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string> GetBootloaderVersionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlcClientAdapter.GetBootloaderVersionAsync (STUB)");
        return Task.FromResult("v1.0.0-stub");
    }

    /// <inheritdoc />
    public Task InstallStagerAsync(byte[] stager, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlcClientAdapter.InstallStagerAsync: stager={Size} bytes (STUB)", stager?.Length ?? 0);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<byte[]> DumpMemoryAsync(
        uint address,
        uint length,
        byte[] dumpPayload,
        IProgress<long> progress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "PlcClientAdapter.DumpMemoryAsync: address=0x{Address:X8}, length=0x{Length:X8}, payload={PayloadSize} bytes (STUB)",
            address, length, dumpPayload?.Length ?? 0);

        // Stub: Simulate progress reporting
        for (long i = 0; i < length; i += 256)
        {
            progress?.Report(i);
            cancellationToken.ThrowIfCancellationRequested();
        }
        progress?.Report(length);

        // Stub: Return dummy memory dump
        _logger.LogInformation("PlcClientAdapter: Returning {Size} bytes of dummy data", length);
        return Task.FromResult(new byte[length]);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _logger.LogInformation("PlcClientAdapter.DisposeAsync (STUB)");
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
