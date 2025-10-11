namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for providing bootloader payload files.
/// Manages stager and memory dumper payload loading.
/// </summary>
public interface IPayloadProvider
{
    /// <summary>
    /// Retrieves the stager payload from the specified base path.
    /// </summary>
    /// <param name="basePath">Base directory path containing payload files.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stager payload bytes.</returns>
    Task<byte[]> GetStagerAsync(string basePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the memory dumper payload from the specified base path.
    /// </summary>
    /// <param name="basePath">Base directory path containing payload files.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The memory dumper payload bytes.</returns>
    Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken cancellationToken = default);
}
