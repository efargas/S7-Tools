using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;
using S7Tools.Extensions;

namespace S7Tools.Services.Logging;

/// <summary>
/// Factory for creating task-specific loggers with dedicated DataStores and file outputs.
/// </summary>
public class TaskLoggerFactory(
    IPathService pathService,
    ILogger<TaskLoggerFactory> logger,
    S7Tools.Core.Services.Interfaces.ICentralizedTaskLogService centralizedTaskLogService,
    IApplicationSettingsService applicationSettingsService,
    ITimeProvider timeProvider) : ITaskLoggerFactory, IDisposable
{
    private readonly IPathService _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
    private readonly ILogger<TaskLoggerFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly S7Tools.Core.Services.Interfaces.ICentralizedTaskLogService _centralizedTaskLogService = centralizedTaskLogService ?? throw new ArgumentNullException(nameof(centralizedTaskLogService));
    private readonly IApplicationSettingsService _applicationSettingsService = applicationSettingsService ?? throw new ArgumentNullException(nameof(applicationSettingsService));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ConcurrentDictionary<Guid, TaskLoggerContext> _activeLoggers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <inheritdoc />
    public async Task<TaskLogger> CreateTaskLoggerAsync(
        Guid taskId,
        string taskName,
        bool captureProcessOutput = true,
        CancellationToken cancellationToken = default)
    {
        return await _semaphore.ExecuteAsync(async () =>
        {
            if (_activeLoggers.TryGetValue(taskId, out TaskLoggerContext? existingLogger))
            {
                _logger.LogWarning("Task logger already exists for task {TaskId}. Returning existing logger.", taskId);
                return existingLogger.TaskLogger;
            }

            DataStoreLoggerProvider? mainProvider = null;
            DataStoreLoggerProvider? processProvider = null;
            var fileLoggers = new List<ILogger>();

            try
            {
                // Create log directory for this task
                string sanitizedTaskName = SanitizeFileName(taskName);
                string timestamp = _timeProvider.GetLocalNow().ToString("yyyyMMdd_HHmmss");
                string taskLogDir = Path.Combine(
                    _pathService.LogsDirectory,
                    "Tasks",
                    $"{sanitizedTaskName}_{timestamp}_{taskId.ToString()[..8]}");

                Directory.CreateDirectory(taskLogDir);

                // Get shared DataStores from CentralizedTaskLogService
                (ITaskLogDataStore mainDataStore, ITaskLogDataStore processDataStore, ITaskLogDataStore _) = _centralizedTaskLogService.GetOrCreateStoresForTask(taskId);

                // Cast to LogDataStore for use with providers
                var mainLogDataStore = (LogDataStore)mainDataStore;
                LogDataStore? processLogDataStore = captureProcessOutput ? (LogDataStore?)processDataStore : null;

                // Create logger providers with DataStores
                string logLevelString = _applicationSettingsService.GetSetting<string>("logging.level", "Information");

                if (!Enum.TryParse(logLevelString, true, out LogLevel configuredLogLevel))
                {
                    configuredLogLevel = LogLevel.Information;
                }

                var mainConfig = new DataStoreLoggerConfiguration
                {
                    LogLevel = configuredLogLevel,
                    IncludeScopes = true,
                    CaptureProperties = true
                };

                mainProvider = new DataStoreLoggerProvider(mainLogDataStore, _timeProvider, mainConfig);
                processProvider = processLogDataStore != null
                    ? new DataStoreLoggerProvider(processLogDataStore, _timeProvider, mainConfig)
                    : null;

                // Create file logger providers
                ILogger mainFileLogger = await CreateFileLoggerAsync(
                    Path.Combine(taskLogDir, "main.log"),
                    configuredLogLevel).ConfigureAwait(false);
                fileLoggers.Add(mainFileLogger);



                ILogger? processFileLogger = null;
                if (captureProcessOutput)
                {
                    processFileLogger = await CreateFileLoggerAsync(
                        Path.Combine(taskLogDir, "process.log"),
                        configuredLogLevel).ConfigureAwait(false);
                    fileLoggers.Add(processFileLogger);
                }

                // Create composite loggers
                var mainLogger = new CompositeLogger(
                    [.. new[] { mainProvider.CreateLogger($"Task.{taskName}"), mainFileLogger }.Where(l => l != null)]);

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
                    ProcessLogger = processLogger,
                    MainLogDataStoreId = mainDataStore.GetHashCode().ToString(),
                    ProcessLogDataStoreId = processDataStore?.GetHashCode().ToString(),
                    MainLogFilePath = Path.Combine(taskLogDir, "main.log"),
                    ProcessLogFilePath = captureProcessOutput ? Path.Combine(taskLogDir, "process.log") : null,
                    CaptureProcessOutput = captureProcessOutput,
                    CreatedAt = DateTime.UtcNow
                };

                // Store context for cleanup
                var context = new TaskLoggerContext
                {
                    TaskLogger = taskLogger,
                    MainDataStore = mainLogDataStore,
                    ProcessDataStore = processLogDataStore,
                    MainProvider = mainProvider,
                    ProcessProvider = processProvider,
                    FileLoggers = [.. fileLoggers],
                    LogDirectory = taskLogDir
                };

                _activeLoggers[taskId] = context;

                _logger.LogInformation(
                    "Created task logger for {TaskName} ({TaskId}) at {LogDir}",
                    taskName, taskId, taskLogDir);

                return taskLogger;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create task logger for {TaskName} ({TaskId})", taskName, taskId);

                // Cleanup partially created resources
                mainProvider?.Dispose();
                processProvider?.Dispose();

                foreach (ILogger fileLogger in fileLoggers)
                {
                    if (fileLogger is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (fileLogger is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                throw;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task FinalizeTaskLoggerAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await _semaphore.ExecuteAsync(async () =>
        {
            if (!_activeLoggers.TryRemove(taskId, out TaskLoggerContext? context))
            {
                _logger.LogWarning("No active logger found for task {TaskId}", taskId);
                return;
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
            context.ProcessProvider?.Dispose();
            context.MainDataStore?.Dispose();
            context.ProcessDataStore?.Dispose();

            foreach (ILogger? fileLogger in context.FileLoggers)
            {
                if (fileLogger is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (fileLogger is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _logger.LogInformation(
                "Finalized task logger for {TaskId}. Total size: {Size} bytes, Entries: {Entries}",
                taskId, totalSize, context.TaskLogger.TotalLogEntries);
        }, cancellationToken);
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

        // Create async-safe file logger using Channels
        var fileLogger = new AsyncFileLogger(filePath, minLevel);
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
                context.ProcessProvider?.Dispose();
                context.MainDataStore?.Dispose();
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
        public LogDataStore? ProcessDataStore { get; set; }
        public DataStoreLoggerProvider? MainProvider { get; set; }
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
/// Async-safe file logger implementation using System.Threading.Channels.
/// Eliminates deadlock risks by using non-blocking writes with a background processing task.
/// </summary>
internal class AsyncFileLogger : ILogger, IAsyncDisposable
{
    private readonly Channel<LogEntry> _logChannel;
    private readonly Task _writerTask;
    private int _writesSinceFlush;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly LogLevel _minLevel;
    private bool _disposed;

    public AsyncFileLogger(string filePath, LogLevel minLevel)
    {
        _minLevel = minLevel;

        // Create unbounded channel for log entries (unbounded to never block producers)
        _logChannel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleWriter = false,  // Multiple threads can log
            SingleReader = true    // Single background writer
        });

        // Start background writer task
        _writerTask = Task.Run(async () => await ProcessLogQueueAsync(filePath));
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

        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel,
            Message = message,
            Exception = exception
        };

        // Non-blocking write - fire and forget
        // If channel is full (shouldn't happen with unbounded), log is dropped
        _logChannel.Writer.TryWrite(entry);
    }

    /// <summary>
    /// Background task that processes the log queue and writes to file.
    /// </summary>
    private async Task ProcessLogQueueAsync(string filePath)
    {
        try
        {
            await using var writer = new StreamWriter(filePath, append: true, System.Text.Encoding.UTF8)
            {
                AutoFlush = false // Batch writes for better performance
            };

            // Process entries until channel is completed
            await foreach (var entry in _logChannel.Reader.ReadAllAsync(_shutdownCts.Token))
            {
                string timestamp = entry.Timestamp.ToLocalTime().ToString(S7Tools.Constants.AppConstants.StandardDateFormat);
                string logLine = $"[{timestamp}] [{entry.Level}] {entry.Message}";

                if (entry.Exception != null)
                {
                    logLine += Environment.NewLine + entry.Exception.ToString();
                }

                await writer.WriteLineAsync(logLine);

                // Flush on every Error/Critical to ensure critical logs are persisted immediately.
                // For warnings and below, we rely on the StreamWriter's internal buffer
                // to avoid I/O bottlenecks during high-frequency logging.
                if (entry.Level >= LogLevel.Error)
                {
                    await writer.FlushAsync();
                    _writesSinceFlush = 0;
                }
                else if (++_writesSinceFlush >= 50)  // e.g. every 50 writes
                {
                    await writer.FlushAsync();
                    _writesSinceFlush = 0;
                }
            }

            // Final flush on shutdown
            await writer.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            // Log to console as last resort (can't use logger from logger)
            Console.Error.WriteLine($"FileLogger background task failed: {ex}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Signal completion and wait for background writer to finish
        _logChannel.Writer.Complete();

        try
        {
            // Wait for writer task to complete with longer timeout for large log files
            // (e.g., verbose socat output can be 30+ MB)
            await _writerTask.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            // Force cancellation if task doesn't complete in time
            _shutdownCts.Cancel();
            try
            {
                await _writerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
        finally
        {
            _shutdownCts.Dispose();
        }
    }

    /// <summary>
    /// Log entry record for channel processing.
    /// </summary>
    private record LogEntry
    {
        public DateTime Timestamp { get; init; }
        public LogLevel Level { get; init; }
        public string Message { get; init; } = string.Empty;
        public Exception? Exception { get; init; }
    }
}
