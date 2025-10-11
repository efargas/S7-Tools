namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for high-level PLC client operations.
/// Provides bootloader-specific functionality for S7-1200 PLCs.
/// </summary>
public interface IPlcClient : IAsyncDisposable
{
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
    /// Dumps memory from the PLC using the specified dumper payload.
    /// </summary>
    /// <param name="address">Starting memory address to dump.</param>
    /// <param name="length">Length of memory region to dump.</param>
    /// <param name="dumpPayload">Memory dumper payload bytes.</param>
    /// <param name="progress">Progress reporter for dump operation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The dumped memory data.</returns>
    Task<byte[]> DumpMemoryAsync(
        uint address,
        uint length,
        byte[] dumpPayload,
        IProgress<long> progress,
        CancellationToken cancellationToken = default);
}
