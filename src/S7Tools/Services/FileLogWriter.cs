using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;

namespace S7Tools.Services;

/// <summary>
/// Simple background file writer that appends log entries from the in-memory data store to
/// rolling files under the configured log path when file logging is enabled in settings.
/// It's intentionally minimal: creates directories, rolls by timestamp, and keeps limited retention.
/// </summary>
public sealed class FileLogWriter : IDisposable
{
    private readonly ILogDataStore _dataStore;
    private readonly IApplicationSettingsService _settingsService;
    private readonly IPathService _pathService;
    private readonly ILogger<FileLogWriter> _logger;
    private readonly object _sync = new();
    private readonly string _sessionLogFile; // Session-specific log file path
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogWriter"/> class.
    /// </summary>
    /// <param name="dataStore">The log data store to monitor for new entries.</param>
    /// <param name="settingsService">The settings service to retrieve logging configuration.</param>
    /// <param name="pathService">The path service to resolve log file paths.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    public FileLogWriter(ILogDataStore dataStore, IApplicationSettingsService settingsService, IPathService pathService, ILogger<FileLogWriter> logger)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Generate session-specific log file path (once per application run)
        _sessionLogFile = _pathService.GetMainLogPath(0);

        // Subscribe to collection changed to flush new entries
        _dataStore.CollectionChanged += DataStore_CollectionChanged;

        // Ensure folder exists at startup if enabled
        try
        {
            bool enableFileLogging = _settingsService.GetSetting<bool>("logging.enableFileLogging", true);
            if (enableFileLogging)
            {
                Directory.CreateDirectory(_pathService.MainLogsDirectory);
                _logger.LogInformation("File logging enabled - logs will be written to: {LogFile}", _sessionLogFile);
            }
            else
            {
                _logger.LogInformation("File logging disabled in settings");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure log directory exists at startup: {LogFile}", _sessionLogFile);
        }
    }

    private void DataStore_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                bool enableFileLogging = _settingsService.GetSetting<bool>("logging.enableFileLogging", true);
                if (!enableFileLogging)
                {
                    return;
                }

                if (e.NewItems != null)
                {
                    foreach (LogModel logEntry in e.NewItems.OfType<LogModel>())
                    {
                        WriteLogEntryToFile(logEntry);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - file logging is supplementary
                _logger.LogError(ex, "Error writing log entry to file");
            }
        }
    }

    private void WriteLogEntryToFile(LogModel log)
    {
        try
        {
            string folder = _pathService.MainLogsDirectory;
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Directory.CreateDirectory(folder);

            // Use the session-specific log file path (same file for entire session)
            string filePath = _sessionLogFile;

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
            _logger.LogDebug(ex, "Failed to write log entry to file: {LogPath}", _sessionLogFile);
        }
    }

    /// <summary>
    /// Disposes the file log writer and releases associated resources.
    /// </summary>
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
