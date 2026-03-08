using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Models;
using S7Tools.Services;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Components;

public class TaskLogsPanelViewModel : ViewModelBase, IDisposable
{
    private const int MaxLogEntries = 1000;

    private TaskExecution _task;
    private readonly IClipboardService _clipboardService;
    private readonly ICentralizedTaskLogService _centralizedTaskLogService;
    private readonly IUIThreadService? _uiThreadService;

    private ITaskLogDataStore? _mainLogDataStore;
    private ITaskLogDataStore? _processLogDataStore;
    private readonly System.Collections.Specialized.NotifyCollectionChangedEventHandler _mainHandler;
    private readonly System.Collections.Specialized.NotifyCollectionChangedEventHandler _processHandler;
    private readonly Services.BufferedCollectionUpdater<(string LogType, LogEntry Entry)> _logUpdater;

    public bool AutoScroll
    {
        get => _autoScroll;
        set => this.RaiseAndSetIfChanged(ref _autoScroll, value);
    }
    private bool _autoScroll = true;

    public TaskLogsPanelViewModel(
        TaskExecution task,
        IClipboardService clipboardService,
        ICentralizedTaskLogService centralizedTaskLogService,
        IUIThreadService? uiThreadService)
    {
        _task = task;
        _clipboardService = clipboardService;
        _centralizedTaskLogService = centralizedTaskLogService;
        _uiThreadService = uiThreadService;

        // Default to autoscroll enabled
        _autoScroll = true;

        MainLogEntries = new ObservableCollection<LogEntry>();
        ProcessLogEntries = new ObservableCollection<LogEntry>();

        CopyCommand = ReactiveCommand.CreateFromTask<IList>(async items =>
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                if (item is LogEntry entry)
                {
                    sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.FormattedMessage}");
                }
            }
            await _clipboardService.SetTextAsync(sb.ToString());
        });

        // Initialize Log Updater
        _logUpdater = new Services.BufferedCollectionUpdater<(string LogType, LogEntry Entry)>(items =>
        {
            var mainBatch = new List<LogEntry>();
            var processBatch = new List<LogEntry>();

            foreach ((string logType, LogEntry entry) in items)
            {
                if (logType == "Main")
                {
                    mainBatch.Add(entry);
                }
                else if (logType == "Process")
                {
                    processBatch.Add(entry);
                }
            }

            if (mainBatch.Count > 0)
            {
                foreach (var item in mainBatch)
                {
                    MainLogEntries.Add(item);
                }

                // Trim to prevent indefinite growth during long running tasks
                while (MainLogEntries.Count > MaxLogEntries)
                {
                    MainLogEntries.RemoveAt(0);
                }
            }
            if (processBatch.Count > 0)
            {
                foreach (var item in processBatch)
                {
                    ProcessLogEntries.Add(item);
                }

                // Trim to prevent indefinite growth during long running tasks
                while (ProcessLogEntries.Count > MaxLogEntries)
                {
                    ProcessLogEntries.RemoveAt(0);
                }
            }
        }, TimeSpan.FromMilliseconds(500), _uiThreadService!);

        _mainHandler = (s, e) => HandleLogCollectionChanged(s, e, "Main");
        _processHandler = (s, e) => HandleLogCollectionChanged(s, e, "Process");

        InitializeLogs();
    }

    private void InitializeLogs()
    {
        if (_task == null || _task.TaskId == Guid.Empty)
        {
            return;
        }

        // Get persistent stores for the task
        (_mainLogDataStore, _processLogDataStore, _) = _centralizedTaskLogService.GetOrCreateStoresForTask(_task.TaskId);

        // Populate initial logs safely on the UI thread
        PopulateInitialLogEntries(_mainLogDataStore, MainLogEntries);
        PopulateInitialLogEntries(_processLogDataStore, ProcessLogEntries);

        if (_mainLogDataStore != null)
        {
            _mainLogDataStore.CollectionChanged += _mainHandler;
        }

        if (_processLogDataStore != null)
        {
            _processLogDataStore.CollectionChanged += _processHandler;
        }
    }

    private void PopulateInitialLogEntries(ITaskLogDataStore? store, ObservableCollection<LogEntry> targetCollection)
    {
        if (store == null)
        {
            return;
        }

        int count = store.Count();
        if (count == 0) return;

        var initialEntries = new List<LogEntry>();
        int skipCount = Math.Max(0, count - MaxLogEntries);

        // Access via Linq skip
        var logsToMap = store.Skip(skipCount);
        foreach (var log in logsToMap)
        {
            initialEntries.Add(MapToLogEntry(log));
        }

        if (initialEntries.Count > 0)
        {
            _uiThreadService?.InvokeOnUIThreadAsync(() =>
            {
                // Clear before adding if not empty to ensure clean state
                targetCollection.Clear();
                foreach (var entry in initialEntries)
                {
                    targetCollection.Add(entry);
                }
            });
        }
    }

    private LogEntry MapToLogEntry(S7Tools.Core.Models.LogModel logModel)
    {
        return new LogEntry
        {
            Timestamp = logModel.Timestamp,
            Level = logModel.Level.ToString(),
            Category = logModel.Category,
            Message = logModel.Message
        };
    }

    private void HandleLogCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e, string logType)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (S7Tools.Core.Models.LogModel newItem in e.NewItems)
            {
                _logUpdater.Enqueue((logType, MapToLogEntry(newItem)));
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_mainLogDataStore != null)
            {
                _mainLogDataStore.CollectionChanged -= _mainHandler;
            }

            if (_processLogDataStore != null)
            {
                _processLogDataStore.CollectionChanged -= _processHandler;
            }

            _logUpdater.Dispose();
        }
    }

    public TaskExecution Task
    {
        get => _task;
        set => this.RaiseAndSetIfChanged(ref _task, value);
    }

    public ObservableCollection<LogEntry> MainLogEntries { get; }
    public ObservableCollection<LogEntry> ProcessLogEntries { get; }

    public ReactiveCommand<IList, Unit> CopyCommand { get; }
}
