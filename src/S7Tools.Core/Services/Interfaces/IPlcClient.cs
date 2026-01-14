namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for high-level PLC client operations.
/// Provides bootloader-specific functionality for S7-1200 PLCs.
/// </summary>
public interface IPlcClient : IAsyncDisposable
{
    /// <summary>
    /// Sets the protocol logger for detailed communication logging.
    /// </summary>
    /// <param name="protocolLogger">Logger for protocol-level communication.</param>
    void SetProtocolLogger(Microsoft.Extensions.Logging.ILogger? protocolLogger);

    /// <summary>
    /// Performs the initial handshake with the PLC bootloader.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandshakeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the bootloader version from the PLC.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The bootloader version string.</returns>
    Task<string> GetBootloaderVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the stager payload into the PLC.
    /// </summary>
    /// <param name="stager">Stager payload bytes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InstallStagerAsync(byte[] stager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the memory dumper payload into the PLC.
    /// </summary>
    /// <param name="dumperPayload">Memory dumper payload bytes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InstallDumperAsync(byte[] dumperPayload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the installed dumper to dump a memory region.
    /// </summary>
    /// <param name="address">Starting memory address to dump.</param>
    /// <param name="length">Length of memory region to dump.</param>
    /// <param name="progress">Progress reporter for dump operation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The dumped memory data.</returns>
    Task<byte[]> InvokeDumperAsync(
        uint address,
        uint length,
        IProgress<long> progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the installed dumper to stream a memory region using high-performance pipeline architecture.
    /// Data is delivered incrementally via callback for streaming to file or other consumers.
    /// </summary>
    /// <param name="address">Starting memory address to dump.</param>
    /// <param name="length">Length of memory region to dump.</param>
    /// <param name="dataCallback">Callback invoked with each chunk of received data.</param>
    /// <param name="progress">Progress reporter for dump operation (bytes received).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous streaming operation.</returns>
    Task InvokeDumperStreamAsync(
        uint address,
        uint length,
        Func<ReadOnlyMemory<byte>, ValueTask> dataCallback,
        IProgress<long> progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the client connection parameters.
    /// </summary>
    /// <param name="host">The host address.</param>
    /// <param name="port">The port number.</param>
    void Configure(string host, int port);

    /// <summary>
    /// Establishes the connection to the PLC (via Socat).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);
}
