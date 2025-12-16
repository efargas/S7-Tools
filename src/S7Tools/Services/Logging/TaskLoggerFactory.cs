using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Jobs;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Services.Logging;

/// <summary>
/// Factory for creating task-specific loggers with dedicated DataStores and file outputs.
/// </summary>
public class TaskLoggerFactory : ITaskLoggerFactory, IDisposable
{
    private readonly IPathService _pathService;
    private readonly ILogger<TaskLoggerFactory> _logger;
    private readonly ConcurrentDictionary<Guid, TaskLoggerContext> _activeLoggers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the TaskLoggerFactory class.
    /// </summary>
    /// <param name="pathService">The path service for log file locations.</param>
    /// <param name="logger">The logger for this factory.</param>
    public TaskLoggerFactory(IPathService pathService, ILogger<TaskLoggerFactory> logger)
    {
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TaskLogger> CreateTaskLoggerAsync(
        Guid taskId,
        string taskName,
        bool captureProtocol = true,
        bool captureProcessOutput = true,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeLoggers.ContainsKey(taskId))
            {
                _logger.LogWarning("Task logger already exists for task {TaskId}. Returning existing logger.", taskId);
                return _activeLoggers[taskId].TaskLogger;
            }

            // Create log directory for this task
            string sanitizedTaskName = SanitizeFileName(taskName);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string taskLogDir = Path.Combine(
                _pathService.LogsDirectory,
                "Tasks",
                $"{sanitizedTaskName}_{timestamp}_{taskId.ToString()[..8]}");

            Directory.CreateDirectory(taskLogDir);

            // Create DataStores for in-memory logging
            var mainDataStore = new LogDataStore(new LogDataStoreOptions { MaxEntries = 10000 });
            LogDataStore? protocolDataStore = captureProtocol
                ? new LogDataStore(new LogDataStoreOptions { MaxEntries = 50000 })
                : null;
            LogDataStore? processDataStore = captureProcessOutput
                ? new LogDataStore(new LogDataStoreOptions { MaxEntries = 20000 })
                : null;

            // Create logger providers with DataStores
            var mainConfig = new DataStoreLoggerConfiguration
            {
                LogLevel = LogLevel.Debug,
                IncludeScopes = true,
                CaptureProperties = true
            };

            var protocolConfig = new DataStoreLoggerConfiguration
            {
                LogLevel = LogLevel.Trace,
                IncludeScopes = true,
                CaptureProperties = true
            };

            var mainProvider = new DataStoreLoggerProvider(mainDataStore, mainConfig);
            DataStoreLoggerProvider? protocolProvider = protocolDataStore != null
                ? new DataStoreLoggerProvider(protocolDataStore, protocolConfig)
                : null;
            DataStoreLoggerProvider? processProvider = processDataStore != null
                ? new DataStoreLoggerProvider(processDataStore, mainConfig)
                : null;

            // ARCHITECTURAL CHANGE: No file loggers during task execution.
            // Logs are stored ONLY in DataStore (in-memory) during task execution.
            // Files are written ONLY in FinalizeTaskLoggerAsync() when task completes.
            // This prevents real-time disk I/O that causes UI freezes and massive file growth.
            
            // Create loggers (DataStore only - no file I/O during execution)
            var mainLogger = mainProvider.CreateLogger($"Task.{taskName}");

            ILogger? protocolLogger = captureProtocol
                ? protocolProvider?.CreateLogger($"Task.{taskName}.Protocol")
                : null;

            ILogger? processLogger = captureProcessOutput
                ? processProvider?.CreateLogger($"Task.{taskName}.Process")
                : null;

            // Create TaskLogger metadata
            var taskLogger = new TaskLogger
            {
                TaskId = taskId,
                MainLogger = mainLogger,
                ProtocolLogger = protocolLogger,
                ProcessLogger = processLogger,
                MainLogDataStoreId = mainDataStore.GetHashCode().ToString(),
                ProtocolLogDataStoreId = protocolDataStore?.GetHashCode().ToString(),
                ProcessLogDataStoreId = processDataStore?.GetHashCode().ToString(),
                MainLogFilePath = Path.Combine(taskLogDir, "main.log"),
                ProtocolLogFilePath = captureProtocol ? Path.Combine(taskLogDir, "protocol.log") : null,
                ProcessLogFilePath = captureProcessOutput ? Path.Combine(taskLogDir, "process.log") : null,
                CaptureProtocol = captureProtocol,
                CaptureProcessOutput = captureProcessOutput,
                CreatedAt = DateTime.UtcNow
            };

            // Store context for cleanup
            var context = new TaskLoggerContext
            {
                TaskLogger = taskLogger,
                MainDataStore = mainDataStore,
                ProtocolDataStore = protocolDataStore,
                ProcessDataStore = processDataStore,
                MainProvider = mainProvider,
                ProtocolProvider = protocolProvider,
                ProcessProvider = processProvider,
                LogDirectory = taskLogDir
                // Note: No FileLoggers - logs written only on finalization
            };

            _activeLoggers[taskId] = context;

            _logger.LogInformation(
                "Created task logger for {TaskName} ({TaskId}) at {LogDir}",
                taskName, taskId, taskLogDir);

            return taskLogger;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task FinalizeTaskLoggerAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_activeLoggers.TryRemove(taskId, out TaskLoggerContext? context))
            {
                _logger.LogWarning("No active logger found for task {TaskId}", taskId);
                return;
            }

            // DEFERRED FILE WRITE: Export all logs from DataStores to files now (task completion).
            // This is the ONLY time logs are written to disk, preventing real-time I/O overhead.
            
            // Export main logs
            if (context.MainDataStore != null && !string.IsNullOrEmpty(context.TaskLogger.MainLogFilePath))
            {
                string exportText = await context.MainDataStore.ExportAsync(LogDataStore.ExportFormats.Text)
                    .ConfigureAwait(false);
                await File.WriteAllTextAsync(
                    context.TaskLogger.MainLogFilePath,
                    exportText,
                    cancellationToken).ConfigureAwait(false);
            }

            // Export protocol logs
            if (context.ProtocolDataStore != null && !string.IsNullOrEmpty(context.TaskLogger.ProtocolLogFilePath))
            {
                string exportText = await context.ProtocolDataStore.ExportAsync(LogDataStore.ExportFormats.Text)
                    .ConfigureAwait(false);
                await File.WriteAllTextAsync(
                    context.TaskLogger.ProtocolLogFilePath,
                    exportText,
                    cancellationToken).ConfigureAwait(false);
            }

            // Export process logs
            if (context.ProcessDataStore != null && !string.IsNullOrEmpty(context.TaskLogger.ProcessLogFilePath))
            {
                string exportText = await context.ProcessDataStore.ExportAsync(LogDataStore.ExportFormats.Text)
                    .ConfigureAwait(false);
                await File.WriteAllTextAsync(
                    context.TaskLogger.ProcessLogFilePath,
                    exportText,
                    cancellationToken).ConfigureAwait(false);
            }

            // Calculate total log size
            long totalSize = 0;
            if (Directory.Exists(context.LogDirectory))
            {
                string[] files = Directory.GetFiles(context.LogDirectory);
                totalSize = files.Sum(f => new FileInfo(f).Length);
            }

            context.TaskLogger.TotalLogFilesSize = totalSize;
            context.TaskLogger.TotalLogEntries = context.MainDataStore?.Count ?? 0;
            context.TaskLogger.FinalizedAt = DateTime.UtcNow;

            // Cleanup DataStores and providers
            context.MainProvider?.Dispose();
            context.ProtocolProvider?.Dispose();
            context.ProcessProvider?.Dispose();
            context.MainDataStore?.Dispose();
            context.ProtocolDataStore?.Dispose();
            context.ProcessDataStore?.Dispose();
            
            // No FileLoggers to dispose - logs written only on finalization

            _logger.LogInformation(
                "Finalized task logger for {TaskId}. Total size: {Size} bytes, Entries: {Entries}",
                taskId, totalSize, context.TaskLogger.TotalLogEntries);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public TaskLogger? GetTaskLogger(Guid taskId)
    {
        return _activeLoggers.TryGetValue(taskId, out TaskLoggerContext? context)
            ? context.TaskLogger
            : null;
    }

    /// <inheritdoc />
    public object? GetTaskDataStore(Guid taskId, TaskLogType logType)
    {
        if (!_activeLoggers.TryGetValue(taskId, out TaskLoggerContext? context))
        {
            return null;
        }

        return logType switch
        {
            TaskLogType.Main => context.MainDataStore,
            TaskLogType.Protocol => context.ProtocolDataStore,
            TaskLogType.Process => context.ProcessDataStore,
            _ => null
        };
    }

    // CreateFileLoggerAsync removed - no longer needed.
    // Logs are stored in-memory (DataStore) and written to files only on finalization.

    private static string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (TaskLoggerContext context in _activeLoggers.Values)
            {
                context.MainProvider?.Dispose();
                context.ProtocolProvider?.Dispose();
                context.ProcessProvider?.Dispose();
                context.MainDataStore?.Dispose();
                context.ProtocolDataStore?.Dispose();
                context.ProcessDataStore?.Dispose();
                // No FileLoggers to dispose - logs written only on finalization
            }

            _activeLoggers.Clear();
            _semaphore.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Context for tracking active task loggers and their associated DataStores.
    /// </summary>
    private class TaskLoggerContext
    {
        public TaskLogger TaskLogger { get; set; } = null!;
        public LogDataStore? MainDataStore { get; set; }
        public LogDataStore? ProtocolDataStore { get; set; }
        public LogDataStore? ProcessDataStore { get; set; }
        public DataStoreLoggerProvider? MainProvider { get; set; }
        public DataStoreLoggerProvider? ProtocolProvider { get; set; }
        public DataStoreLoggerProvider? ProcessProvider { get; set; }
        public string LogDirectory { get; set; } = string.Empty;
        // Note: No FileLoggers - logs written to files only on finalization
    }
}

// Note: CompositeLogger and FileLogger classes removed.
// Logs are now stored ONLY in DataStore (in-memory) during task execution.
// Files are written ONLY in FinalizeTaskLoggerAsync() when task completes.
// This eliminates real-time disk I/O that caused UI freezes and massive file growth.
