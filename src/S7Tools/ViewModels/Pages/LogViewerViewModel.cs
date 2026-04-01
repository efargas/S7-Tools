using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using S7Tools.Core.Constants;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Dialogs.Models;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for the LogViewer functionality with real-time log display, filtering, and search capabilities.
/// </summary>
public sealed class LogViewerViewModel : ViewModelBase, IDockableViewModel, IDisposable
{
    private readonly ILogDataStore _logDataStore;
    private readonly IUIThreadService _uiThreadService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly ILogExportService? _logExportService;
    private bool _disposed;
    private readonly SourceList<LogModel> _logEntriesSource = new();
    private readonly ReadOnlyObservableCollection<LogModel> _filteredLogEntries;
    private readonly IDisposable _cleanup;

    // Sorting state
    private string _sortColumn = "Timestamp";
    private bool _sortAscending = true;

    /// <summary>
    /// Gets or sets the column to sort by.
    /// </summary>
    public string SortColumn
    {
        get => _sortColumn;
        set => this.RaiseAndSetIfChanged(ref _sortColumn, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to sort in ascending order.
    /// </summary>
    public bool SortAscending
    {
        get => _sortAscending;
        set => this.RaiseAndSetIfChanged(ref _sortAscending, value);
    }

    // IDockableViewModel implementation
    private string _dockId = "MainLog";
    private string _dockTitle = "System Logs";
    private bool _canClose = true;
    private bool _canFloat = true;

    public string DockId
    {
        get => _dockId;
        set => this.RaiseAndSetIfChanged(ref _dockId, value);
    }

    public string DockTitle
    {
        get => _dockTitle;
        set => this.RaiseAndSetIfChanged(ref _dockTitle, value);
    }

    public bool CanClose
    {
        get => _canClose;
        set => this.RaiseAndSetIfChanged(ref _canClose, value);
    }

    public bool CanFloat
    {
        get => _canFloat;
        set => this.RaiseAndSetIfChanged(ref _canFloat, value);
    }

    /// <summary>
    /// Initializes a new instance of the LogViewerViewModel class for design-time use.
    /// </summary>
    public LogViewerViewModel() : this(
        new DesignTimeLogDataStore(),
        new DesignTimeUIThreadService(),
        new DesignTimeClipboardService(),
        new DesignTimeDialogService())
    {
    }

    private Guid? _selectedTaskId;
    private string? _selectedScope;
    private LogModel? _selectedLogEntry;
    private string _searchText = string.Empty;
    private LogLevel _selectedLogLevel = LogLevel.Trace;
    private bool _autoScroll = true;
    private bool _isStuckToBottom = true;
    private bool _showTimestamp = true;
    private bool _showCategory = true;
    private bool _showLevel = true;
    private DateTimeOffset? _startDate;
    private DateTimeOffset? _endDate;
    private int _totalLogCount;
    private int _filteredLogCount;

    /// <summary>
    /// Initializes a new instance of the LogViewerViewModel class.
    /// </summary>
    /// <param name="logDataStore">The log data store service.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="logExportService">The log export service (optional).</param>
    /// <param name="taskId">The task ID to initially filter by (optional).</param>
    /// <param name="scope">The scope string to initially filter by (optional).</param>
    public LogViewerViewModel(
        ILogDataStore logDataStore,
        IUIThreadService uiThreadService,
        IClipboardService clipboardService,
        IDialogService dialogService,
        ILogExportService? logExportService = null,
        Guid? taskId = null,
        string? scope = null)
    {
        _logDataStore = logDataStore ?? throw new ArgumentNullException(nameof(logDataStore));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logExportService = logExportService;

        _selectedTaskId = taskId;
        _selectedScope = scope;

        InitializeCommands();

        IObservable<Func<LogModel, bool>> filterPredicate = this.WhenAnyValue(
            x => x.SelectedLogLevel,
            x => x.SearchText,
            x => x.StartDate,
            x => x.EndDate,
            x => x.SelectedTaskId,
            x => x.SelectedScope)
            .Select(_ => BuildFilter());

        IObservable<IComparer<LogModel>> sortComparer = this.WhenAnyValue(
            x => x.SortColumn,
            x => x.SortAscending)
            .Select(_ => BuildSort());

        _cleanup = _logEntriesSource.Connect()
            .Filter(filterPredicate)
            .Sort(sortComparer)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _filteredLogEntries)
            .Subscribe(_ =>
            {
                FilteredLogCount = _filteredLogEntries.Count;
                TotalLogCount = _logDataStore.Count;
            });

        InitializeLogStore();
    }

    /// <summary>
    /// Gets or sets the selected TaskId filter.
    /// </summary>
    public Guid? SelectedTaskId
    {
        get => _selectedTaskId;
        set => this.RaiseAndSetIfChanged(ref _selectedTaskId, value);
    }

    /// <summary>
    /// Gets or sets the selected Scope filter.
    /// </summary>
    public string? SelectedScope
    {
        get => _selectedScope;
        set => this.RaiseAndSetIfChanged(ref _selectedScope, value);
    }

    /// <summary>
    /// Gets the collection of filtered log entries for display.
    /// </summary>
    public ReadOnlyObservableCollection<LogModel> FilteredLogEntries => _filteredLogEntries;

    /// <summary>
    /// Gets or sets the search text for filtering log entries.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    /// <summary>
    /// Gets or sets the selected minimum log level for filtering.
    /// </summary>
    public LogLevel SelectedLogLevel
    {
        get => _selectedLogLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedLogLevel, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether auto-scroll is enabled.
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set => this.RaiseAndSetIfChanged(ref _autoScroll, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to invert auto-scroll (scroll to top).
    /// </summary>
    public bool InvertAutoScroll
    {
        get => _invertAutoScroll;
        set => this.RaiseAndSetIfChanged(ref _invertAutoScroll, value);
    }
    private bool _invertAutoScroll;

    /// <summary>
    /// Gets or sets a value indicating whether the view is currently stuck to the bottom.
    /// </summary>
    public bool IsStuckToBottom
    {
        get => _isStuckToBottom;
        set => this.RaiseAndSetIfChanged(ref _isStuckToBottom, value);
    }

    /// <summary>
    /// Gets or sets the selected log entry.
    /// </summary>
    public LogModel? SelectedLogEntry
    {
        get => _selectedLogEntry;
        set => this.RaiseAndSetIfChanged(ref _selectedLogEntry, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether timestamps are shown.
    /// </summary>
    public bool ShowTimestamp
    {
        get => _showTimestamp;
        set => this.RaiseAndSetIfChanged(ref _showTimestamp, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether categories are shown.
    /// </summary>
    public bool ShowCategory
    {
        get => _showCategory;
        set => this.RaiseAndSetIfChanged(ref _showCategory, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether log levels are shown.
    /// </summary>
    public bool ShowLevel
    {
        get => _showLevel;
        set => this.RaiseAndSetIfChanged(ref _showLevel, value);
    }

    /// <summary>
    /// Gets or sets the start date for date range filtering.
    /// </summary>
    public DateTimeOffset? StartDate
    {
        get => _startDate;
        set => this.RaiseAndSetIfChanged(ref _startDate, value);
    }

    /// <summary>
    /// Gets or sets the end date for date range filtering.
    /// </summary>
    public DateTimeOffset? EndDate
    {
        get => _endDate;
        set => this.RaiseAndSetIfChanged(ref _endDate, value);
    }

    /// <summary>
    /// Gets the total number of log entries.
    /// </summary>
    public int TotalLogCount
    {
        get => _totalLogCount;
        private set => this.RaiseAndSetIfChanged(ref _totalLogCount, value);
    }

    /// <summary>
    /// Gets the number of filtered log entries.
    /// </summary>
    public int FilteredLogCount
    {
        get => _filteredLogCount;
        private set => this.RaiseAndSetIfChanged(ref _filteredLogCount, value);
    }

    private double _fontSize = 12.0;
    /// <summary>
    /// Gets or sets the font size for log entries display.
    /// </summary>
    public double FontSize
    {
        get => _fontSize;
        set => this.RaiseAndSetIfChanged(ref _fontSize, value);
    }

    private string _columnWidths = string.Empty;
    /// <summary>
    /// Gets or sets the persisted column widths (comma-separated format).
    /// </summary>
    public string ColumnWidths
    {
        get => _columnWidths;
        set => this.RaiseAndSetIfChanged(ref _columnWidths, value);
    }


    /// <summary>
    /// Gets the available log levels for filtering.
    /// </summary>
    public IReadOnlyList<LogLevel> AvailableLogLevels { get; } = Array.AsReadOnly(Enum.GetValues<LogLevel>());

    /// <summary>
    /// Gets the command to clear all log entries.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to export log entries.
    /// </summary>
    public ReactiveCommand<string, Unit> ExportLogsCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to copy selected log entry to clipboard.
    /// </summary>
    public ReactiveCommand<object?, Unit> CopySelectedEntryCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to copy selected log message to clipboard.
    /// </summary>
    public ReactiveCommand<object?, Unit> CopySelectedMessageCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to sort the log entries.
    /// </summary>
    public ReactiveCommand<string, Unit> SortCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to refresh the log display.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to clear all filters.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearFiltersCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to toggle the timestamp column visibility.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleTimestampCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to toggle the level column visibility.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleLevelCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to toggle the category column visibility.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleCategoryCommand { get; private set; } = null!;

    /// <summary>
    /// Initializes the reactive commands.
    /// </summary>
    private void InitializeCommands()
    {
        ClearLogsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            bool result = await _dialogService.ShowConfirmationAsync(
                UIStrings.LogViewer_ClearLogsTitle,
                UIStrings.LogViewer_ClearLogsMessage);

            if (result)
            {
                _logDataStore.Clear();
            }
        });

        ExportLogsCommand = ReactiveCommand.CreateFromTask<string>(async formatString =>
        {
            try
            {
                if (_logExportService == null)
                {
                    await _dialogService.ShowErrorAsync(
                        UIStrings.LogViewer_ExportLogsTitle,
                        UIStrings.LogViewer_ExportServiceUnavailable);
                    return;
                }

                // Parse the format string to determine export format
                ExportFormat format = formatString?.ToLowerInvariant() switch
                {
                    "txt" or "text" => ExportFormat.Text,
                    "json" => ExportFormat.Json,
                    "csv" => ExportFormat.Csv,
                    _ => ExportFormat.Text
                };

                // Use filtered entries for export (respects current filters)
                var logsToExport = FilteredLogEntries.ToList();

                if (!logsToExport.Any())
                {
                    await _dialogService.ShowErrorAsync(
                        UIStrings.LogViewer_ExportLogsTitle,
                        UIStrings.LogViewer_NoLogsToExport);
                    return;
                }

                // Export the logs
                Result result = await _logExportService.ExportLogsAsync(logsToExport, format);

                if (result.IsSuccess)
                {
                    await _dialogService.ShowErrorAsync(
                        UIStrings.LogViewer_ExportLogsTitle,
                        string.Format(UIStrings.LogViewer_ExportSuccess, logsToExport.Count, format));
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        UIStrings.LogViewer_ExportFailed,
                        result.Error ?? UIStrings.LogViewer_UnknownError);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    UIStrings.LogViewer_ExportFailed,
                    string.Format(UIStrings.LogViewer_ExportFailedMessage, ex.Message));
            }
        });

        CopySelectedEntryCommand = ReactiveCommand.CreateFromTask<object?>(async parameter =>
        {
            var sb = new System.Text.StringBuilder();

            if (parameter is System.Collections.IList items && items.Count > 0)
            {
                foreach (object? item in items)
                {
                    if (item is LogModel entry)
                    {
                        sb.AppendLine($"[{entry.Timestamp.ToString(DateTimeFormats.LongDateTime)}.{entry.Timestamp.Millisecond:000}] [{entry.Level}] {entry.Category}: {entry.FormattedMessage}");
                    }
                }
            }
            else if (parameter is LogModel entry)
            {
                sb.AppendLine($"[{entry.Timestamp.ToString(DateTimeFormats.LongDateTime)}.{entry.Timestamp.Millisecond:000}] [{entry.Level}] {entry.Category}: {entry.FormattedMessage}");
            }
            else
            {
                LogModel? fallbackEntry = SelectedLogEntry;
                if (fallbackEntry != null)
                {
                    sb.AppendLine($"[{fallbackEntry.Timestamp.ToString(DateTimeFormats.LongDateTime)}.{fallbackEntry.Timestamp.Millisecond:000}] [{fallbackEntry.Level}] {fallbackEntry.Category}: {fallbackEntry.FormattedMessage}");
                }
            }

            if (sb.Length > 0)
            {
                await _clipboardService.SetTextAsync(sb.ToString().TrimEnd());
            }
        });

        CopySelectedMessageCommand = ReactiveCommand.CreateFromTask<object?>(async parameter =>
        {
            var sb = new System.Text.StringBuilder();

            if (parameter is System.Collections.IList items && items.Count > 0)
            {
                foreach (object? item in items)
                {
                    if (item is LogModel entry)
                    {
                        sb.AppendLine(entry.FormattedMessage ?? string.Empty);
                    }
                }
            }
            else if (parameter is LogModel entry)
            {
                sb.AppendLine(entry.FormattedMessage ?? string.Empty);
            }
            else
            {
                LogModel? fallbackEntry = SelectedLogEntry;
                if (fallbackEntry != null)
                {
                    sb.AppendLine(fallbackEntry.FormattedMessage ?? string.Empty);
                }
            }

            if (sb.Length > 0)
            {
                await _clipboardService.SetTextAsync(sb.ToString().TrimEnd());
            }
        });

        RefreshCommand = ReactiveCommand.Create(() =>
        {
            LoadLogEntries();
        });

        ClearFiltersCommand = ReactiveCommand.Create(() =>
        {
            SearchText = string.Empty;
            SelectedLogLevel = LogLevel.Trace;
            StartDate = null;
            EndDate = null;
            SelectedTaskId = null;
            SelectedScope = null;
        });

        ToggleTimestampCommand = ReactiveCommand.Create(() => { ShowTimestamp = !ShowTimestamp; });
        ToggleLevelCommand = ReactiveCommand.Create(() => { ShowLevel = !ShowLevel; });
        ToggleCategoryCommand = ReactiveCommand.Create(() => { ShowCategory = !ShowCategory; });

        SortCommand = ReactiveCommand.Create<string>(SortByColumn);
    }

    private void SortByColumn(string column)
    {
        if (SortColumn == column)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortColumn = column;
            SortAscending = true;
        }

        if (!(SortColumn == "Timestamp" && SortAscending) && !(SortColumn == "Timestamp" && !SortAscending))
        {
            AutoScroll = false;
        }

        if (SortColumn == "Timestamp" && !SortAscending)
        {
            InvertAutoScroll = true;
            // Optional: re-enable auto scroll when clicking descending timestamp for newest at top
            if (!AutoScroll)
            {
                AutoScroll = true;
            }
        }
        else
        {
            InvertAutoScroll = false;
        }
    }

    /// <summary>
    /// Initializes the log data store and subscribes to changes.
    /// </summary>
    private void InitializeLogStore()
    {
        LoadLogEntries();
        _logDataStore.CollectionChanged += OnLogDataStoreCollectionChanged;
    }

    /// <summary>
    /// Loads log entries from the data store.
    /// </summary>
    private void LoadLogEntries()
    {
        IReadOnlyList<LogModel> entries = _logDataStore.Entries;
        _uiThreadService.InvokeOnUIThread(() =>
        {
            _logEntriesSource.Edit(updater =>
            {
                updater.Clear();
                updater.AddRange(entries);
            });
            TotalLogCount = _logDataStore.Count;
        });
    }

    /// <summary>
    /// Handles collection changes from the log data store.
    /// </summary>
    private void OnLogDataStoreCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            var newItems = e.NewItems.Cast<LogModel>().ToList();
            _uiThreadService.InvokeOnUIThread(() =>
            {
                _logEntriesSource.AddRange(newItems);
            });
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            _uiThreadService.InvokeOnUIThread(() =>
            {
                _logEntriesSource.Clear();
            });
        }
    }

    private Func<LogModel, bool> BuildFilter()
    {
        return entry =>
        {
            if (entry.Level < SelectedLogLevel)
            {
                return false;
            }

            if (StartDate.HasValue && entry.Timestamp < StartDate.Value)
            {
                return false;
            }

            if (EndDate.HasValue)
            {
                DateTimeOffset endDateOffset = EndDate.Value.AddDays(1).AddTicks(-1);
                if (entry.Timestamp > endDateOffset)
                {
                    return false;
                }
            }

            if (SelectedTaskId.HasValue)
            {
                if (entry.Properties == null || !entry.Properties.TryGetValue("TaskId", out object? tid) || tid?.ToString() != SelectedTaskId.Value.ToString())
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(SelectedScope))
            {
                if (entry.Scope != SelectedScope)
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText;
                bool matches = (entry.Message?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                               (entry.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                               (entry.Exception?.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
                if (!matches)
                {
                    return false;
                }
            }

            return true;
        };
    }

    private System.Collections.Generic.IComparer<LogModel> BuildSort()
    {
        if (SortColumn == "Level")
        {
            return SortAscending
                ? SortExpressionComparer<LogModel>.Ascending(e => e.Level).ThenByAscending(e => e.Timestamp)
                : SortExpressionComparer<LogModel>.Descending(e => e.Level).ThenByDescending(e => e.Timestamp);
        }
        else if (SortColumn == "Category")
        {
            return SortAscending
                ? SortExpressionComparer<LogModel>.Ascending(e => e.Category).ThenByAscending(e => e.Timestamp)
                : SortExpressionComparer<LogModel>.Descending(e => e.Category).ThenByDescending(e => e.Timestamp);
        }
        else if (SortColumn == "Message")
        {
            return SortAscending
                ? SortExpressionComparer<LogModel>.Ascending(e => e.Message).ThenByAscending(e => e.Timestamp)
                : SortExpressionComparer<LogModel>.Descending(e => e.Message).ThenByDescending(e => e.Timestamp);
        }

        // Default to Timestamp
        return SortAscending
            ? SortExpressionComparer<LogModel>.Ascending(e => e.Timestamp)
            : SortExpressionComparer<LogModel>.Descending(e => e.Timestamp);
    }

    /// <summary>
    /// Disposes the view model and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cleanup?.Dispose();
        _logEntriesSource?.Dispose();
        _logDataStore.CollectionChanged -= OnLogDataStoreCollectionChanged;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

#region Design-Time Services

/// <summary>
/// Design-time implementation of ILogDataStore for XAML previews.
/// </summary>
internal class DesignTimeLogDataStore : ILogDataStore
{
#pragma warning disable CS0067 // Events may be wired by designer; suppress 'never used'
    public event PropertyChangedEventHandler? PropertyChanged;
    public event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore CS0067

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignTimeLogDataStore"/> class.
    /// </summary>
    public DesignTimeLogDataStore()
    {
        // Ensure analyzers see events as "used" without runtime impact
        SuppressUnusedEventWarnings();
    }

    private void SuppressUnusedEventWarnings()
    {
        // Use a runtime-evaluated condition so the compiler can't mark it as unreachable
        if (Environment.TickCount < 0)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Entries)));
            CollectionChanged?.Invoke(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Gets or sets the Entries.
    /// </summary>
    public IReadOnlyList<LogModel> Entries { get; } = new List<LogModel>
    {
        new() { Timestamp = DateTimeOffset.Now.AddMinutes(-5).DateTime, Level = LogLevel.Information, Category = "S7Tools.Services", Message = "Application started successfully" },
        new() { Timestamp = DateTimeOffset.Now.AddMinutes(-3).DateTime, Level = LogLevel.Warning, Category = "S7Tools.PLC", Message = "Connection timeout, retrying..." },
        new() { Timestamp = DateTimeOffset.Now.AddMinutes(-1).DateTime, Level = LogLevel.Error, Category = "S7Tools.Data", Message = "Failed to read tag value", Exception = new InvalidOperationException("Tag not found") }
    };

    /// <summary>
    /// Gets or sets the Count.
    /// </summary>
    public int Count => Entries.Count;
    /// <summary>
    /// Gets or sets the MaxEntries.
    /// </summary>
    public int MaxEntries => 10000;
    /// <summary>
    /// Gets or sets the IsFull.
    /// </summary>
    public bool IsFull => false;

    /// <summary>
    /// Executes the AddEntry operation.
    /// </summary>
    public void AddEntry(LogModel logEntry) { }
    /// <summary>
    /// Executes the AddEntries operation.
    /// </summary>
    public void AddEntries(IEnumerable<LogModel> logEntries) { }
    /// <summary>
    /// Executes the Clear operation.
    /// </summary>
    public void Clear() { }
    /// <summary>
    /// Executes the Flush operation (no-op for design-time).
    /// </summary>
    public void Flush() { }
    /// <summary>
    /// Executes the GetFilteredEntries operation.
    /// </summary>
    public IEnumerable<LogModel> GetFilteredEntries(Func<LogModel, bool> filter) => Entries.Where(filter);
    /// <summary>
    /// Executes the GetEntriesInTimeRange operation.
    /// </summary>
    public IEnumerable<LogModel> GetEntriesInTimeRange(DateTimeOffset startTime, DateTimeOffset endTime) => Entries.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
    /// <summary>
    /// Executes the ExportAsync operation.
    /// </summary>
    public Task<string> ExportAsync(string format = "txt") => Task.FromResult("Design-time export data");
    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose() { }
}

/// <summary>
/// Design-time implementation of IUIThreadService for XAML previews.
/// </summary>
internal class DesignTimeUIThreadService : IUIThreadService
{
    /// <summary>
    /// Gets or sets the IsUIThread.
    /// </summary>
    public bool IsUIThread => true;

    /// <summary>
    /// Executes the InvokeOnUIThread operation.
    /// </summary>
    public void InvokeOnUIThread(Action action) => action?.Invoke();
    /// <summary>
    /// Executes the InvokeOnUIThreadAsync operation.
    /// </summary>
    public Task InvokeOnUIThreadAsync(Action action)
    {
        action?.Invoke();
        return Task.CompletedTask;
    }
    public T InvokeOnUIThread<T>(Func<T> function) => function();
    public Task<T> InvokeOnUIThreadAsync<T>(Func<T> function) => Task.FromResult(function());
    /// <summary>
    /// Executes the InvokeOnUIThreadAsync operation.
    /// </summary>
    public Task InvokeOnUIThreadAsync(Func<Task> asyncAction) => asyncAction();
    public Task<T> InvokeOnUIThreadAsync<T>(Func<Task<T>> asyncFunction) => asyncFunction();
    /// <summary>
    /// Executes the PostToUIThread operation.
    /// </summary>
    public void PostToUIThread(Action action) => action?.Invoke();
}

/// <summary>
/// Design-time implementation of IClipboardService for XAML previews.
/// </summary>
internal class DesignTimeClipboardService : IClipboardService
{
    /// <summary>
    /// Executes the GetTextAsync operation.
    /// </summary>
    public Task<string?> GetTextAsync() => Task.FromResult<string?>("Design-time clipboard text");
    /// <summary>
    /// Executes the SetTextAsync operation.
    /// </summary>
    public Task SetTextAsync(string? text) => Task.CompletedTask;
}

/// <summary>
/// Design-time implementation of IDialogService for XAML previews.
/// </summary>
internal class DesignTimeDialogService : IDialogService
{
    /// <summary>
    /// Gets or sets the ShowConfirmation.
    /// </summary>
    public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; } = new();
    /// <summary>
    /// Gets or sets the ShowError.
    /// </summary>
    public Interaction<ConfirmationRequest, Unit> ShowError { get; } = new();
    /// <summary>
    /// Gets or sets the ShowInput.
    /// </summary>
    public Interaction<InputRequest, InputResult> ShowInput { get; } = new();
    /// <summary>
    /// Gets or sets the ShowJobSelection.
    /// </summary>
    public Interaction<JobSelectionRequest, Core.Models.Jobs.JobProfile?> ShowJobSelection { get; } = new();

    /// <summary>
    /// Executes the ShowConfirmationAsync operation.
    /// </summary>
    public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);
    /// <summary>
    /// Executes the ShowErrorAsync operation.
    /// </summary>
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    /// <summary>
    /// Executes the ShowInputAsync operation.
    /// </summary>
    public Task<InputResult> ShowInputAsync(string title, string message, string? defaultValue = null, string? placeholder = null)
    {
        return Task.FromResult(InputResult.Cancelled());
    }

    /// <summary>
    /// Executes the ShowJobSelectionAsync operation.
    /// </summary>
    public Task<Core.Models.Jobs.JobProfile?> ShowJobSelectionAsync()
    {
        return Task.FromResult<Core.Models.Jobs.JobProfile?>(null);
    }
}

#endregion
