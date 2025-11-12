using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Adapters;

/// <summary>
/// Adapter for PLC protocol operations.
/// Wraps the reference PlcProtocol with S7Tools' IPlcProtocol interface.
/// </summary>
/// <remarks>
/// This is a stub implementation. Will be replaced with actual adapter
/// wrapping SiemensS7-Bootloader reference implementation.
/// </remarks>
public sealed class PlcProtocolAdapter : IPlcProtocol
{
    private readonly ILogger<PlcProtocolAdapter> _logger;

    public PlcProtocolAdapter(ILogger<PlcProtocolAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool DataAvailable => false; // Stub: Always false

    /// <inheritdoc />
    public Task SendPacketAsync(byte[] payload, int? maxChunk = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlcProtocolAdapter.SendPacketAsync: payload={Size} bytes, maxChunk={MaxChunk} (STUB)",
            payload?.Length ?? 0, maxChunk);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<byte[]> ReceivePacketAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlcProtocolAdapter.ReceivePacketAsync (STUB)");
        // Stub: Return empty packet
        return Task.FromResult(Array.Empty<byte>());
    }

    /// <inheritdoc />
    public Task RawWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("PlcProtocolAdapter.RawWriteAsync: offset={Offset}, count={Count} (STUB)", offset, count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> RawReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("PlcProtocolAdapter.RawReadAsync: offset={Offset}, count={Count} (STUB)", offset, count);
        // Stub: Return 0 bytes read
        return Task.FromResult(0);
    }
}
