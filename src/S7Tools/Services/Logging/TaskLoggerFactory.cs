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
public class TaskLoggerFactory(IPathService pathService, ILogger<TaskLoggerFactory> logger) : ITaskLoggerFactory, IDisposable
{
    private readonly IPathService _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
    private readonly ILogger<TaskLoggerFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<Guid, TaskLoggerContext> _activeLoggers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

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
            if (_activeLoggers.TryGetValue(taskId, out TaskLoggerContext? existingLogger))
            {
                _logger.LogWarning("Task logger already exists for task {TaskId}. Returning existing logger.", taskId);
                return existingLogger.TaskLogger;
            }

            // Create log directory for this task
            string sanitizedTaskName = SanitizeFileName(taskName);
            string timestamp = DateTime.UtcNow.ToLocalTime().ToString("yyyyMMdd_HHmmss");
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

            // Create file logger providers
            ILogger mainFileLogger = await CreateFileLoggerAsync(
                Path.Combine(taskLogDir, "main.log"),
                LogLevel.Debug).ConfigureAwait(false);

            ILogger? protocolFileLogger = captureProtocol
                ? await CreateFileLoggerAsync(
                    Path.Combine(taskLogDir, "protocol.log"),
                    LogLevel.Trace).ConfigureAwait(false)
                : null;

            ILogger? processFileLogger = captureProcessOutput
                ? await CreateFileLoggerAsync(
                    Path.Combine(taskLogDir, "process.log"),
                    LogLevel.Debug).ConfigureAwait(false)
                : null;

            // Create composite loggers
            // Create composite loggers
            var mainLogger = new CompositeLogger(
                [.. new[] { mainProvider.CreateLogger($"Task.{taskName}"), mainFileLogger }.Where(l => l != null)]);

            CompositeLogger? protocolLogger = captureProtocol
                ? new CompositeLogger(
                    new[] { protocolProvider?.CreateLogger($"Task.{taskName}.Protocol"), protocolFileLogger }
                        .Where(l => l != null).ToArray()!)
                : null;

            CompositeLogger? processLogger = captureProcessOutput
                ? new CompositeLogger(
                    new[] { processProvider?.CreateLogger($"Task.{taskName}.Process"), processFileLogger }
                        .Where(l => l != null).ToArray()!)
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
                FileLoggers = new[] { mainFileLogger, protocolFileLogger, processFileLogger }
                    .Where(l => l != null).ToList()!,
                LogDirectory = taskLogDir
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

            // Export final logs to files
            if (context.MainDataStore != null)
            {
                string exportText = await context.MainDataStore.ExportAsync(LogDataStore.ExportFormats.Text)
                    .ConfigureAwait(false);
                await File.AppendAllTextAsync(
                    context.TaskLogger.MainLogFilePath!,
                    "\n=== Final Export ===\n" + exportText,
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

            // Cleanup
            context.MainProvider?.Dispose();
            context.ProtocolProvider?.Dispose();
            context.ProcessProvider?.Dispose();
            context.MainDataStore?.Dispose();
            context.ProtocolDataStore?.Dispose();
            context.ProcessDataStore?.Dispose();

            foreach (ILogger? fileLogger in context.FileLoggers)
            {
                if (fileLogger is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

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

    private static async Task<ILogger> CreateFileLoggerAsync(
        string filePath,
        LogLevel minLevel)
    {
        // Create directory if needed
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create a simple file logger using a StreamWriter
        var fileLogger = new FileLogger(filePath, minLevel);
        await Task.CompletedTask; // For async pattern consistency

        return fileLogger;
    }

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

                foreach (ILogger? fileLogger in context.FileLoggers)
                {
                    if (fileLogger is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            _activeLoggers.Clear();
            _semaphore.Dispose();
        }

        _disposed = true;
    }

    private class TaskLoggerContext
    {
        public TaskLogger TaskLogger { get; set; } = null!;
        public LogDataStore? MainDataStore { get; set; }
        public LogDataStore? ProtocolDataStore { get; set; }
        public LogDataStore? ProcessDataStore { get; set; }
        public DataStoreLoggerProvider? MainProvider { get; set; }
        public DataStoreLoggerProvider? ProtocolProvider { get; set; }
        public DataStoreLoggerProvider? ProcessProvider { get; set; }
        public List<ILogger> FileLoggers { get; set; } = [];
        public string LogDirectory { get; set; } = string.Empty;
    }
}

/// <summary>
/// Composite logger that writes to multiple logger instances.
/// </summary>
internal class CompositeLogger(ILogger[] loggers) : ILogger
{
    private readonly ILogger[] _loggers = loggers ?? [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return new CompositeScope([.. _loggers.Select(l => l.BeginScope(state))]);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _loggers.Any(l => l.IsEnabled(logLevel));
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        foreach (ILogger logger in _loggers)
        {
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }

    private class CompositeScope(IDisposable?[] scopes) : IDisposable
    {
        private readonly IDisposable?[] _scopes = scopes;

        public void Dispose()
        {
            foreach (IDisposable? scope in _scopes)
            {
                scope?.Dispose();
            }
        }
    }
}

/// <summary>
/// Simple file logger implementation.
/// </summary>
internal class FileLogger : ILogger, IDisposable
{
    private readonly string _filePath;
    private readonly LogLevel _minLevel;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public FileLogger(string filePath, LogLevel minLevel)
    {
        _filePath = filePath;
        _minLevel = minLevel;
        _writer = new StreamWriter(filePath, append: true, System.Text.Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null; // Simple implementation without scope support
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || _disposed)
        {
            return;
        }

        string message = formatter(state, exception);
        string timestamp = DateTime.UtcNow.ToLocalTime().ToString(S7Tools.Constants.AppConstants.StandardDateFormat);
        string logLine = $"[{timestamp}] [{logLevel}] {message}";

        if (exception != null)
        {
            logLine += Environment.NewLine + exception.ToString();
        }

        _semaphore.Wait();
        try
        {
            _writer.WriteLine(logLine);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaphore.Wait();
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
        finally
        {
            _semaphore.Release();
            _semaphore.Dispose();
        }

        _disposed = true;
    }
}
