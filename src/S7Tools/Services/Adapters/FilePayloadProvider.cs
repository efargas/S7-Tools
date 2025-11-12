using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Adapters;

/// <summary>
/// Provides bootloader payload files from the filesystem.
/// Loads stager and memory dumper binaries from configured base path.
/// </summary>
public sealed class FilePayloadProvider : IPayloadProvider, IDisposable
{
    private readonly ILogger<FilePayloadProvider> _logger;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private byte[]? _cachedStager;
    private byte[]? _cachedDumper;
    private string? _cachedBasePath;
    private bool _disposed;

    public FilePayloadProvider(ILogger<FilePayloadProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<byte[]> GetStagerAsync(string basePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath, nameof(basePath));

        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Return cached stager if base path hasn't changed
            if (_cachedStager is not null && _cachedBasePath == basePath)
            {
                _logger.LogDebug("Returning cached stager payload from {BasePath}", basePath);
                return _cachedStager;
            }

            string stagerPath = Path.Combine(basePath, "stager.bin");

            if (!File.Exists(stagerPath))
            {
                _logger.LogError("Stager payload not found at {StagerPath}", stagerPath);
                throw new FileNotFoundException($"Stager payload not found at: {stagerPath}", stagerPath);
            }

            _logger.LogInformation("Loading stager payload from {StagerPath}", stagerPath);
            byte[] payload = await File.ReadAllBytesAsync(stagerPath, cancellationToken).ConfigureAwait(false);

            if (payload.Length == 0)
            {
                _logger.LogError("Stager payload is empty at {StagerPath}", stagerPath);
                throw new InvalidOperationException($"Stager payload is empty at: {stagerPath}");
            }

            // Update cache
            _cachedStager = payload;
            _cachedBasePath = basePath;

            _logger.LogInformation("Loaded stager payload: {Size} bytes from {StagerPath}", payload.Length, stagerPath);
            return payload;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath, nameof(basePath));

        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Return cached dumper if base path hasn't changed
            if (_cachedDumper is not null && _cachedBasePath == basePath)
            {
                _logger.LogDebug("Returning cached memory dumper payload from {BasePath}", basePath);
                return _cachedDumper;
            }

            string dumperPath = Path.Combine(basePath, "dumper.bin");

            if (!File.Exists(dumperPath))
            {
                _logger.LogError("Memory dumper payload not found at {DumperPath}", dumperPath);
                throw new FileNotFoundException($"Memory dumper payload not found at: {dumperPath}", dumperPath);
            }

            _logger.LogInformation("Loading memory dumper payload from {DumperPath}", dumperPath);
            byte[] payload = await File.ReadAllBytesAsync(dumperPath, cancellationToken).ConfigureAwait(false);

            if (payload.Length == 0)
            {
                _logger.LogError("Memory dumper payload is empty at {DumperPath}", dumperPath);
                throw new InvalidOperationException($"Memory dumper payload is empty at: {dumperPath}");
            }

            // Update cache
            _cachedDumper = payload;
            _cachedBasePath = basePath;

            _logger.LogInformation("Loaded memory dumper payload: {Size} bytes from {DumperPath}", payload.Length, dumperPath);
            return payload;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Disposes resources used by this payload provider.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cacheLock.Dispose();
        _disposed = true;
    }
}
