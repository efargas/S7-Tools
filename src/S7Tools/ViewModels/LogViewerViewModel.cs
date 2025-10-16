using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the LogViewer functionality with real-time log display, filtering, and search capabilities.
/// </summary>
public sealed class LogViewerViewModel : ViewModelBase, IDisposable
{
    private readonly ILogDataStore _logDataStore;
    private readonly IUIThreadService _uiThreadService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly ILogExportService? _logExportService;
    private bool _disposed;

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

    private ObservableCollection<LogModel> _logEntries;
    private ObservableCollection<LogModel> _filteredLogEntries;
    private string _searchText = string.Empty;
    private LogLevel _selectedLogLevel = LogLevel.Trace;
    private bool _autoScroll = true;
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
    public LogViewerViewModel(
        ILogDataStore logDataStore,
        IUIThreadService uiThreadService,
        IClipboardService clipboardService,
        IDialogService dialogService,
        ILogExportService? logExportService = null)
    {
        _logDataStore = logDataStore ?? throw new ArgumentNullException(nameof(logDataStore));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logExportService = logExportService;

        _logEntries = new ObservableCollection<LogModel>();
        _filteredLogEntries = new ObservableCollection<LogModel>();

        InitializeCommands();
        InitializeLogStore();
        ApplyFilters();
    }

    /// <summary>
    /// Gets the collection of all log entries.
    /// </summary>
    public ObservableCollection<LogModel> LogEntries
    {
        get => _logEntries;
        private set => this.RaiseAndSetIfChanged(ref _logEntries, value);
    }

    /// <summary>
    /// Gets the collection of filtered log entries for display.
    /// </summary>
    public ObservableCollection<LogModel> FilteredLogEntries
    {
        get => _filteredLogEntries;
        private set => this.RaiseAndSetIfChanged(ref _filteredLogEntries, value);
    }

    /// <summary>
    /// Gets or sets the search text for filtering log entries.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            ApplyFilters();
        }
    }

    /// <summary>
    /// Gets or sets the selected minimum log level for filtering.
    /// </summary>
    public LogLevel SelectedLogLevel
    {
        get => _selectedLogLevel;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLogLevel, value);
            ApplyFilters();
        }
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
        set
        {
            this.RaiseAndSetIfChanged(ref _startDate, value);
            ApplyFilters();
        }
    }

    /// <summary>
    /// Gets or sets the end date for date range filtering.
    /// </summary>
    public DateTimeOffset? EndDate
    {
        get => _endDate;
        set
        {
            this.RaiseAndSetIfChanged(ref _endDate, value);
            ApplyFilters();
        }
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
    public ReactiveCommand<LogModel, Unit> CopyLogEntryCommand { get; private set; } = null!;

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
            var result = await _dialogService.ShowConfirmationAsync(
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
                var format = formatString?.ToLowerInvariant() switch
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
                var result = await _logExportService.ExportLogsAsync(logsToExport, format);

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

        CopyLogEntryCommand = ReactiveCommand.CreateFromTask<LogModel>(async logEntry =>
        {
            if (logEntry != null)
            {
                var logText = $"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEntry.Level}] {logEntry.Category}: {logEntry.FormattedMessage}";
                await _clipboardService.SetTextAsync(logText);
            }
        });

        RefreshCommand = ReactiveCommand.Create(() =>
        {
            LoadLogEntries();
            ApplyFilters();
        });

        ClearFiltersCommand = ReactiveCommand.Create(() =>
        {
            SearchText = string.Empty;
            SelectedLogLevel = LogLevel.Trace;
            StartDate = null;
            EndDate = null;
        });

        ToggleTimestampCommand = ReactiveCommand.Create(() => { ShowTimestamp = !ShowTimestamp; });
        ToggleLevelCommand = ReactiveCommand.Create(() => { ShowLevel = !ShowLevel; });
        ToggleCategoryCommand = ReactiveCommand.Create(() => { ShowCategory = !ShowCategory; });
    }

    /// <summary>
    /// Initializes the log data store and subscribes to changes.
    /// </summary>
    private void InitializeLogStore()
    {
        // Load initial log entries
        LoadLogEntries();

        // Subscribe to log store changes for real-time updates
        _logDataStore.PropertyChanged += OnLogDataStorePropertyChanged;
        _logDataStore.CollectionChanged += OnLogDataStoreCollectionChanged;
    }

    /// <summary>
    /// Loads log entries from the data store.
    /// </summary>
    private void LoadLogEntries()
    {
        var entries = _logDataStore.Entries;

        _uiThreadService.InvokeOnUIThread(() =>
        {
            LogEntries.Clear();
            foreach (var entry in entries)
            {
                LogEntries.Add(entry);
            }

            TotalLogCount = LogEntries.Count;
        });
    }

    /// <summary>
    /// Handles property changes from the log data store.
    /// </summary>
    private void OnLogDataStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILogDataStore.Entries))
        {
            LoadLogEntries();
            ApplyFilters();
        }
        else if (e.PropertyName == nameof(ILogDataStore.Count))
        {
            _uiThreadService.InvokeOnUIThread(() =>
            {
                TotalLogCount = _logDataStore.Count;
            });
        }
    }

    /// <summary>
    /// Handles collection changes from the log data store.
    /// </summary>
    private void OnLogDataStoreCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _uiThreadService.InvokeOnUIThread(() =>
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (LogModel newItem in e.NewItems)
                        {
                            LogEntries.Add(newItem);
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    LogEntries.Clear();
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    if (e.NewItems != null && e.OldItems != null)
                    {
                        // For replace operations, we'll just refresh the entire collection
                        LoadLogEntries();
                        return; // Skip the ApplyFilters call at the end since LoadLogEntries will handle it
                    }
                    break;
            }

            TotalLogCount = LogEntries.Count;
            ApplyFilters();
        });
    }

    /// <summary>
    /// Applies the current filters to the log entries.
    /// </summary>
    private void ApplyFilters()
    {
        _uiThreadService.InvokeOnUIThread(() =>
        {
            var filtered = LogEntries.AsEnumerable();

            // Apply log level filter
            filtered = filtered.Where(entry => entry.Level >= SelectedLogLevel);

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(entry =>
                    entry.Message.ToLowerInvariant().Contains(searchLower) ||
                    entry.Category.ToLowerInvariant().Contains(searchLower) ||
                    (entry.Exception?.ToString().ToLowerInvariant().Contains(searchLower) ?? false));
            }

            // Apply date range filter
            if (StartDate.HasValue)
            {
                filtered = filtered.Where(entry => entry.Timestamp >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                var endDateOffset = EndDate.Value.AddDays(1).AddTicks(-1);
                filtered = filtered.Where(entry => entry.Timestamp <= endDateOffset);
            }

            // Update filtered collection
            FilteredLogEntries.Clear();
            foreach (var entry in filtered.OrderBy(e => e.Timestamp))
            {
                FilteredLogEntries.Add(entry);
            }

            FilteredLogCount = FilteredLogEntries.Count;
        });
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

        _logDataStore.PropertyChanged -= OnLogDataStorePropertyChanged;
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

    public IReadOnlyList<LogModel> Entries { get; } = new List<LogModel>
    {
        new() { Timestamp = DateTimeOffset.Now.AddMinutes(-5).DateTime, Level = LogLevel.Information, Category = "S7Tools.Services", Message = "Application started successfully" },
        new() { Timestamp = DateTimeOffset.Now.AddMinutes(-3).DateTime, Level = LogLevel.Warning, Category = "S7Tools.PLC", Message = "Connection timeout, retrying..." },
        new() { Timestamp = DateTimeOffset.Now.AddMinutes(-1).DateTime, Level = LogLevel.Error, Category = "S7Tools.Data", Message = "Failed to read tag value", Exception = new InvalidOperationException("Tag not found") }
    };

    public int Count => Entries.Count;
    public int MaxEntries => 10000;
    public bool IsFull => false;

    public void AddEntry(LogModel logEntry) { }
    public void AddEntries(IEnumerable<LogModel> logEntries) { }
    public void Clear() { }
    public IEnumerable<LogModel> GetFilteredEntries(Func<LogModel, bool> filter) => Entries.Where(filter);
    public IEnumerable<LogModel> GetEntriesInTimeRange(DateTimeOffset startTime, DateTimeOffset endTime) => Entries.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
    public Task<string> ExportAsync(string format = "txt") => Task.FromResult("Design-time export data");
    public void Dispose() { }
}

/// <summary>
/// Design-time implementation of IUIThreadService for XAML previews.
/// </summary>
internal class DesignTimeUIThreadService : IUIThreadService
{
    public bool IsUIThread => true;

    public void InvokeOnUIThread(Action action) => action?.Invoke();
    public Task InvokeOnUIThreadAsync(Action action) => Task.Run(action);
    public T InvokeOnUIThread<T>(Func<T> function) => function();
    public Task<T> InvokeOnUIThreadAsync<T>(Func<T> function) => Task.Run(function);
    public Task InvokeOnUIThreadAsync(Func<Task> asyncAction) => asyncAction();
    public Task<T> InvokeOnUIThreadAsync<T>(Func<Task<T>> asyncFunction) => asyncFunction();
    public void PostToUIThread(Action action) => action?.Invoke();
    public bool TryInvokeOnUIThread(Action action, TimeSpan timeout) { action?.Invoke(); return true; }
    public bool TryInvokeOnUIThread<T>(Func<T> function, TimeSpan timeout, out T result) { result = function(); return true; }
}

/// <summary>
/// Design-time implementation of IClipboardService for XAML previews.
/// </summary>
internal class DesignTimeClipboardService : IClipboardService
{
    public Task<string?> GetTextAsync() => Task.FromResult<string?>("Design-time clipboard text");
    public Task SetTextAsync(string? text) => Task.CompletedTask;
}

/// <summary>
/// Design-time implementation of IDialogService for XAML previews.
/// </summary>
internal class DesignTimeDialogService : IDialogService
{
    public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; } = new();
    public Interaction<ConfirmationRequest, Unit> ShowError { get; } = new();
    public Interaction<InputRequest, InputResult> ShowInput { get; } = new();

    public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    public Task<InputResult> ShowInputAsync(string title, string message, string? defaultValue = null, string? placeholder = null)
        => Task.FromResult(InputResult.Cancelled());
}


#endregion
