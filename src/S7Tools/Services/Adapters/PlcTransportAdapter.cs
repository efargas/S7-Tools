using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Adapters;

/// <summary>
/// Adapter for PLC transport layer communication.
/// Wraps the reference ICommunicationChannel with S7Tools' IPlcTransport interface.
/// </summary>
/// <remarks>
/// This is a stub implementation. Will be replaced with actual adapter
/// wrapping SiemensS7-Bootloader reference implementation.
/// </remarks>
public sealed class PlcTransportAdapter : IPlcTransport
{
    private readonly ILogger<PlcTransportAdapter> _logger;
    private bool _disposed;
    private bool _isConnected;

    public PlcTransportAdapter(ILogger<PlcTransportAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public bool DataAvailable => false; // Stub: Always false

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlcTransportAdapter.ConnectAsync (STUB)");
        _isConnected = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlcTransportAdapter.DisconnectAsync (STUB)");
        _isConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("PlcTransportAdapter.ReadAsync: offset={Offset}, count={Count} (STUB)", offset, count);
        // Stub: Return 0 bytes read
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("PlcTransportAdapter.WriteAsync: offset={Offset}, count={Count} (STUB)", offset, count);
        // Stub: No-op write
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _logger.LogInformation("PlcTransportAdapter.DisposeAsync (STUB)");
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
