using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// Provides bootloader payload files from the file system.
/// Loads stager and memory dumper binaries from a specified directory.
/// </summary>
public sealed class FilePayloadProvider : IPayloadProvider
{
    private readonly ILogger<FilePayloadProvider> _logger;
    private const string StagerFileName = "stager.bin";
    private const string MemoryDumperFileName = "dumper.bin";

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePayloadProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public FilePayloadProvider(ILogger<FilePayloadProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<byte[]> GetStagerAsync(string basePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        var stagerPath = Path.Combine(basePath, StagerFileName);

        _logger.LogDebug("Loading stager payload from {Path}", stagerPath);

        if (!File.Exists(stagerPath))
        {
            _logger.LogError("Stager payload not found at {Path}", stagerPath);
            throw new FileNotFoundException($"Stager payload not found: {stagerPath}", stagerPath);
        }

        var data = await File.ReadAllBytesAsync(stagerPath, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Loaded stager payload: {ByteCount} bytes from {Path}",
            data.Length, stagerPath);

        return data;
    }

    /// <inheritdoc />
    public async Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        var dumperPath = Path.Combine(basePath, MemoryDumperFileName);

        _logger.LogDebug("Loading memory dumper payload from {Path}", dumperPath);

        if (!File.Exists(dumperPath))
        {
            _logger.LogError("Memory dumper payload not found at {Path}", dumperPath);
            throw new FileNotFoundException($"Memory dumper payload not found: {dumperPath}", dumperPath);
        }

        var data = await File.ReadAllBytesAsync(dumperPath, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Loaded memory dumper payload: {ByteCount} bytes from {Path}",
            data.Length, dumperPath);

        return data;
    }
}
