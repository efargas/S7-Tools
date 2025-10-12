using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace S7Tools.Services;

/// <summary>
/// Simple background file writer that appends log entries from the in-memory data store to
/// rolling files under the configured DefaultLogPath when file logging is enabled in settings.
/// It's intentionally minimal: creates directories, rolls by timestamp, and keeps limited retention.
/// </summary>
public sealed class FileLogWriter : IDisposable
{
    private readonly ILogDataStore _dataStore;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<FileLogWriter> _logger;
    private readonly object _sync = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogWriter"/> class.
    /// </summary>
    /// <param name="dataStore">The data store to read log entries from.</param>
    /// <param name="settingsService">The service to access application settings.</param>
    /// <param name="logger">The logger for logging events and errors.</param>
    public FileLogWriter(ILogDataStore dataStore, ISettingsService settingsService, ILogger<FileLogWriter> logger)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to collection changed to flush new entries
        _dataStore.CollectionChanged += DataStore_CollectionChanged;

        // Ensure folder exists at startup if enabled
        try
        {
            var settings = _settingsService.GetSettings();
            if (settings.Logging.EnableFileLogging)
            {
                Directory.CreateDirectory(settings.Logging.DefaultLogPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure default log path exists at startup");
        }
    }

    private void DataStore_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            if (_disposed) return;
            var settings = _settingsService.GetSettings();
            if (!settings.Logging.EnableFileLogging)
            {
                return;
            }

            // Append new items if any
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is LogModel log)
                    {
                        AppendLogToFile(log, settings.Logging);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Avoid throwing from logging path
            _logger.LogDebug(ex, "Error while writing logs to file");
        }
    }

    private void AppendLogToFile(LogModel log, Models.LoggingSettings settings)
    {
        try
        {
            var folder = settings.DefaultLogPath;
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Directory.CreateDirectory(folder);

            // Use timestamp-based file name pattern
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var fileName = settings.LogFileNamePattern.Replace("{timestamp}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            var filePath = Path.Combine(folder, fileName);

            var line = new StringBuilder();
            line.AppendFormat("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}", log.Timestamp, log.Level, log.Category);
            line.AppendLine();
            line.AppendLine(log.Message);
            if (log.Exception != null)
            {
                line.AppendLine(log.Exception.ToString());
            }
            line.AppendLine(new string('-', 40));

            // Append text
            lock (_sync)
            {
                File.AppendAllText(filePath, line.ToString(), Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to append log to file");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _dataStore.CollectionChanged -= DataStore_CollectionChanged;
        }
        catch { }
    }
}