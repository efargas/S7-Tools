using S7Tools.ViewModels.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Models;
using S7Tools.ViewModels.Dialogs.Models;
using S7Tools.Services;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Components;

/// <summary>
/// Represents the TaskLogsPanelViewModel.
/// </summary>
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

    // Sorting state
    private string _sortColumn = "Timestamp";
    private bool _sortAscending = true;

    public bool AutoScroll
    {
        get => _autoScroll;
        set => this.RaiseAndSetIfChanged(ref _autoScroll, value);
    }
    private bool _autoScroll = true;

    public bool InvertAutoScroll
    {
        get => _invertAutoScroll;
        private set => this.RaiseAndSetIfChanged(ref _invertAutoScroll, value);
    }
    private bool _invertAutoScroll;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskLogsPanelViewModel"/> class.
    /// </summary>
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

        _autoScroll = true;

        MainLogEntries = new ObservableCollection<LogEntry>();
        ProcessLogEntries = new ObservableCollection<LogEntry>();
        FilteredMainLogEntries = new ObservableCollection<LogEntry>();
        FilteredProcessLogEntries = new ObservableCollection<LogEntry>();

        CopySelectedEntryCommand = ReactiveCommand.CreateFromTask<LogEntry?>(async entry =>
        {
            if (entry != null)
            {
                await _clipboardService.SetTextAsync($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.FormattedMessage}");
            }
        });

        CopySelectedMessageCommand = ReactiveCommand.CreateFromTask<LogEntry?>(async entry =>
        {
            if (entry != null)
            {
                await _clipboardService.SetTextAsync(entry.FormattedMessage ?? string.Empty);
            }
        });

        SortCommand = ReactiveCommand.Create<string>(SortByColumn);

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

            bool needsMainSort = false;
            bool needsProcessSort = false;
            bool isDefaultSort = _sortColumn == "Timestamp" && _sortAscending;

            if (mainBatch.Count > 0)
            {
                foreach (var item in mainBatch)
                {
                    MainLogEntries.Add(item);
                    if (isDefaultSort)
                    {
                        FilteredMainLogEntries.Add(item);
                    }
                }

                // Trim to prevent indefinite growth
                if (MainLogEntries.Count > MaxLogEntries)
                {
                    while (MainLogEntries.Count > MaxLogEntries)
                    {
                        MainLogEntries.RemoveAt(0);
                    }
                    needsMainSort = true;
                }
                else if (!isDefaultSort)
                {
                    needsMainSort = true;
                }
            }

            if (processBatch.Count > 0)
            {
                foreach (var item in processBatch)
                {
                    ProcessLogEntries.Add(item);
                    if (isDefaultSort)
                    {
                        FilteredProcessLogEntries.Add(item);
                    }
                }

                if (ProcessLogEntries.Count > MaxLogEntries)
                {
                    while (ProcessLogEntries.Count > MaxLogEntries)
                    {
                        ProcessLogEntries.RemoveAt(0);
                    }
                    needsProcessSort = true;
                }
                else if (!isDefaultSort)
                {
                    needsProcessSort = true;
                }
            }

            if (needsMainSort)
            {
                ApplySortToMain();
            }

            if (needsProcessSort)
            {
                ApplySortToProcess();
            }
        }, TimeSpan.FromMilliseconds(500), _uiThreadService!);

        _mainHandler = (s, e) => HandleLogCollectionChanged(s, e, "Main");
        _processHandler = (s, e) => HandleLogCollectionChanged(s, e, "Process");

        InitializeLogs();
    }

    private void SortByColumn(string column)
    {
        if (_sortColumn == column)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }

        // Disable user autoscroll if they specifically sorted to intercept live-viewing 
        if (!(_sortColumn == "Timestamp" && _sortAscending) && !(_sortColumn == "Timestamp" && !_sortAscending))
        {
            AutoScroll = false;
        }

        if (_sortColumn == "Timestamp" && !_sortAscending)
        {
            InvertAutoScroll = true;
            // Also enable auto scroll if moving to this default
            if (!AutoScroll)
            {
                AutoScroll = true;
            }
        }
        else
        {
            InvertAutoScroll = false;
        }

        ApplySortToMain();
        ApplySortToProcess();
    }

    private void ApplySortToMain()
    {
        var filtered = SortLogEntries(MainLogEntries).ToList();
        _uiThreadService?.InvokeOnUIThread(() =>
        {
            FilteredMainLogEntries = new ObservableCollection<LogEntry>(filtered);
        });
    }

    private void ApplySortToProcess()
    {
        var filtered = SortLogEntries(ProcessLogEntries).ToList();
        _uiThreadService?.InvokeOnUIThread(() =>
        {
            FilteredProcessLogEntries = new ObservableCollection<LogEntry>(filtered);
        });
    }

    private IEnumerable<LogEntry> SortLogEntries(IEnumerable<LogEntry> source)
    {
        if (_sortColumn == "Level")
        {
            return _sortAscending ? source.OrderBy(e => e.Level).ThenBy(e => e.Timestamp) : source.OrderByDescending(e => e.Level).ThenByDescending(e => e.Timestamp);
        }
        else if (_sortColumn == "Message")
        {
            return _sortAscending ? source.OrderBy(e => e.Message).ThenBy(e => e.Timestamp) : source.OrderByDescending(e => e.Message).ThenByDescending(e => e.Timestamp);
        }
        else // Timestamp
        {
            return _sortAscending ? source.OrderBy(e => e.Timestamp) : source.OrderByDescending(e => e.Timestamp);
        }
    }

    private void InitializeLogs()
    {
        if (_task == null || _task.TaskId == Guid.Empty)
        {
            return;
        }

        (_mainLogDataStore, _processLogDataStore, _) = _centralizedTaskLogService.GetOrCreateStoresForTask(_task.TaskId);

        PopulateInitialLogEntries(_mainLogDataStore, MainLogEntries, FilteredMainLogEntries);
        PopulateInitialLogEntries(_processLogDataStore, ProcessLogEntries, FilteredProcessLogEntries);

        if (_mainLogDataStore != null)
        {
            _mainLogDataStore.CollectionChanged += _mainHandler;
        }

        if (_processLogDataStore != null)
        {
            _processLogDataStore.CollectionChanged += _processHandler;
        }
    }

    private void PopulateInitialLogEntries(ITaskLogDataStore? store, ObservableCollection<LogEntry> sourceCollection, ObservableCollection<LogEntry> targetCollection)
    {
        if (store == null)
        {
            return;
        }

        int count = store.Count();
        if (count == 0)
        {
            return;
        }

        var initialEntries = new List<LogEntry>();
        int skipCount = Math.Max(0, count - MaxLogEntries);

        var logsToMap = store.Skip(skipCount);
        foreach (var log in logsToMap)
        {
            initialEntries.Add(MapToLogEntry(log));
        }

        if (initialEntries.Count > 0)
        {
            _uiThreadService?.InvokeOnUIThreadAsync(() =>
            {
                sourceCollection.Clear();
                foreach (var entry in initialEntries)
                {
                    sourceCollection.Add(entry);
                }

                var sorted = SortLogEntries(sourceCollection).ToList();
                if (targetCollection == FilteredMainLogEntries)
                {
                    FilteredMainLogEntries = new ObservableCollection<LogEntry>(sorted);
                }
                else
                {
                    FilteredProcessLogEntries = new ObservableCollection<LogEntry>(sorted);
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

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the MainLogEntries.
    /// </summary>
    public ObservableCollection<LogEntry> MainLogEntries { get; }
    /// <summary>
    /// Gets or sets the ProcessLogEntries.
    /// </summary>
    public ObservableCollection<LogEntry> ProcessLogEntries { get; }

    private ObservableCollection<LogEntry> _filteredMainLogEntries = new();
    public ObservableCollection<LogEntry> FilteredMainLogEntries
    {
        get => _filteredMainLogEntries;
        private set => this.RaiseAndSetIfChanged(ref _filteredMainLogEntries, value);
    }

    private ObservableCollection<LogEntry> _filteredProcessLogEntries = new();
    public ObservableCollection<LogEntry> FilteredProcessLogEntries
    {
        get => _filteredProcessLogEntries;
        private set => this.RaiseAndSetIfChanged(ref _filteredProcessLogEntries, value);
    }

    /// <summary>
    /// Gets or sets the CopySelectedEntryCommand.
    /// </summary>
    public ReactiveCommand<LogEntry?, Unit> CopySelectedEntryCommand { get; }
    /// <summary>
    /// Gets or sets the CopySelectedMessageCommand.
    /// </summary>
    public ReactiveCommand<LogEntry?, Unit> CopySelectedMessageCommand { get; }
    /// <summary>
    /// Gets or sets the SortCommand.
    /// </summary>
    public ReactiveCommand<string, Unit> SortCommand { get; }
}