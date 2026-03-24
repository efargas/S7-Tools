namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for PLC protocol operations.
/// Handles packet encoding/decoding and raw communication.
/// </summary>
public interface IPlcProtocol
{
    /// <summary>
    /// Gets a value indicating whether data is available for reading.
    /// </summary>
    bool DataAvailable { get; }

    /// <summary>
    /// Sends a packet to the PLC with optional chunking.
    /// </summary>
    /// <param name="payload">Payload data to send.</param>
    /// <param name="maxChunk">Maximum chunk size for splitting large payloads (optional).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendPacketAsync(byte[] payload, int? maxChunk = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a packet from the PLC.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The received packet data.</returns>
    Task<byte[]> ReceivePacketAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes raw data directly to the protocol layer.
    /// </summary>
    /// <param name="buffer">Buffer containing data to write.</param>
    /// <param name="offset">Offset in the buffer to start reading data.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RawWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads raw data directly from the protocol layer.
    /// </summary>
    /// <param name="buffer">Buffer to store the read data.</param>
    /// <param name="offset">Offset in the buffer to start storing data.</param>
    /// <param name="count">Maximum number of bytes to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of bytes actually read.</returns>
    Task<int> RawReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the protocol connection parameters.
    /// </summary>
    /// <param name="host">The host address.</param>
    /// <param name="port">The port number.</param>
    void Configure(string host, int port);

    /// <summary>
    /// Sets an optional session-specific logger (e.g., for task-specific protocol logging).
    /// </summary>
    /// <param name="logger">The logger to use for this session.</param>
    void SetLogger(Microsoft.Extensions.Logging.ILogger? logger);

    /// <summary>
    /// Establishes the connection to the PLC.
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
    /// Gets the underlying stream for the protocol connection.
    /// </summary>
    /// <returns>The underlying stream if connected, otherwise null.</returns>
    Stream? GetStream();
}
