namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for PLC transport layer communication.
/// Provides low-level read/write operations for PLC connectivity.
/// </summary>
public interface IPlcTransport : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the transport is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets a value indicating whether data is available for reading.
    /// </summary>
    bool DataAvailable { get; }

    /// <summary>
    /// Establishes a connection to the PLC.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the PLC.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads data from the transport into the specified buffer.
    /// </summary>
    /// <param name="buffer">Buffer to store the read data.</param>
    /// <param name="offset">Offset in the buffer to start storing data.</param>
    /// <param name="count">Maximum number of bytes to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of bytes actually read.</returns>
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes data from the specified buffer to the transport.
    /// </summary>
    /// <param name="buffer">Buffer containing data to write.</param>
    /// <param name="offset">Offset in the buffer to start reading data.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
}
