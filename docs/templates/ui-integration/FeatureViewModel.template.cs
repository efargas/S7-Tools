using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for [FEATURE_NAME] feature with sidebar and main content integration.
/// Follows the S7Tools UI integration pattern: Activity Bar → Side Panel → Main Content.
/// </summary>
public class [FEATURE_NAME]ViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<[FEATURE_NAME]ViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    // Sidebar state
    private string? _selectedCategory;
    private ObservableCollection<string> _categories = new();

    // Main content state
    private object? _selectedContentViewModel;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    /// <summary>
    /// Initializes a new instance of the [FEATURE_NAME]ViewModel class for design-time.
    /// </summary>
    public [FEATURE_NAME]ViewModel() : this(CreateDesignTimeLogger())
    {
    }

    /// <summary>
    /// Creates a design-time logger.
    /// </summary>
    private static ILogger<[FEATURE_NAME]ViewModel> CreateDesignTimeLogger()
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => { });
        return factory.CreateLogger<[FEATURE_NAME]ViewModel>();
    }

    /// <summary>
    /// Initializes a new instance of the [FEATURE_NAME]ViewModel class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public [FEATURE_NAME]ViewModel(ILogger<[FEATURE_NAME]ViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize categories for sidebar
        InitializeCategories();

        // Initialize commands
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        
        _logger.LogDebug("[FEATURE_NAME]ViewModel initialized");
    }

    #region Properties

    /// <summary>
    /// Gets the categories for the sidebar navigation.
    /// </summary>
    public ObservableCollection<string> Categories
    {
        get => _categories;
        private set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    /// <summary>
    /// Gets or sets the selected category in the sidebar.
    /// </summary>
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            UpdateMainContent();
        }
    }

    /// <summary>
    /// Gets or sets the ViewModel for the main content area.
    /// This switches based on sidebar selection.
    /// </summary>
    public object? SelectedContentViewModel
    {
        get => _selectedContentViewModel;
        set => this.RaiseAndSetIfChanged(ref _selectedContentViewModel, value);
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to refresh data.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Initializes the sidebar categories.
    /// </summary>
    private void InitializeCategories()
    {
        Categories.Clear();
        Categories.Add("Overview");
        Categories.Add("Details");
        Categories.Add("Settings");

        // Select first category by default
        SelectedCategory = Categories.Count > 0 ? Categories[0] : null;
    }

    /// <summary>
    /// Updates the main content based on the selected sidebar category.
    /// </summary>
    private void UpdateMainContent()
    {
        try
        {
            _logger.LogDebug("Updating main content for category: {Category}", SelectedCategory);

            // Switch main content based on sidebar selection
            SelectedContentViewModel = SelectedCategory switch
            {
                "Overview" => Create[FEATURE_NAME]OverviewViewModel(),
                "Details" => Create[FEATURE_NAME]DetailsViewModel(),
                "Settings" => Create[FEATURE_NAME]SettingsViewModel(),
                _ => null
            };

            StatusMessage = $"Showing {SelectedCategory}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating main content for category: {Category}", SelectedCategory);
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates the overview ViewModel for the main content area.
    /// </summary>
    private object Create[FEATURE_NAME]OverviewViewModel()
    {
        // TODO: Create and return the overview ViewModel
        // Example: return new [FEATURE_NAME]OverviewViewModel();
        return new ViewModelBase(); // Placeholder
    }

    /// <summary>
    /// Creates the details ViewModel for the main content area.
    /// </summary>
    private object Create[FEATURE_NAME]DetailsViewModel()
    {
        // TODO: Create and return the details ViewModel
        return new ViewModelBase(); // Placeholder
    }

    /// <summary>
    /// Creates the settings ViewModel for the main content area.
    /// </summary>
    private object Create[FEATURE_NAME]SettingsViewModel()
    {
        // TODO: Create and return the settings ViewModel
        return new ViewModelBase(); // Placeholder
    }

    /// <summary>
    /// Refreshes the data asynchronously.
    /// </summary>
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing...";
            _logger.LogInformation("Refreshing [FEATURE_NAME] data");

            // TODO: Implement refresh logic
            await Task.Delay(500); // Placeholder

            StatusMessage = "Refresh complete";
            _logger.LogInformation("[FEATURE_NAME] data refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing [FEATURE_NAME] data");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by this ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposables.Dispose();
        _disposed = true;

        _logger.LogDebug("[FEATURE_NAME]ViewModel disposed");
    }

    #endregion
}
