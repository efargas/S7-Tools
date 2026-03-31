using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Dock.Model.Controls;
using Dock.Model.Core;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Extensions;
using S7Tools.Factories;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Pages;
using S7Tools.ViewModels.Settings;

namespace S7Tools.ViewModels.Layout;

/// <summary>
/// Refactored MainWindowViewModel following Single Responsibility Principle.
/// Delegates specific responsibilities to specialized ViewModels.
/// This is the new, clean implementation that replaces the God Object pattern.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IApplicationSettingsService _settingsService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CompositeDisposable _disposables = new();

    private string _statusMessage = UIStrings.StatusReady;
    private string _lastButtonPressed = "";
    private IRootDock? _layout;
    private IFactory? _factory;

    #region Constructor

    /// <summary>
    /// Creates a design-time application settings service for the designer.
    /// </summary>
    /// <returns>An application settings service instance for design-time use.</returns>
    private static IApplicationSettingsService CreateDesignTimeApplicationSettingsService()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { });
        ILogger<Services.ApplicationSettingsService> settingsLogger = loggerFactory.CreateLogger<Services.ApplicationSettingsService>();

        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        // Create a mock options for design time
        var dummyOptions = new DummyOptions();
        return new Services.ApplicationSettingsService(settingsLogger, dummyOptions);
    }

    private class DummyOptions : S7Tools.Core.Interfaces.Services.IWritableOptions<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings>
    {
        /// <summary>
        /// Gets or sets the CurrentValue.
        /// </summary>
        public S7Tools.Core.Models.Configuration.StrongSettings.AppSettings CurrentValue { get; } = new();
        /// <summary>
        /// Gets or sets the Value.
        /// </summary>
        public S7Tools.Core.Models.Configuration.StrongSettings.AppSettings Value => CurrentValue;
        /// <summary>
        /// Executes the Get operation.
        /// </summary>
        public S7Tools.Core.Models.Configuration.StrongSettings.AppSettings Get(string? name) => CurrentValue;
        /// <summary>
        /// Executes the OnChange operation.
        /// </summary>
        public IDisposable? OnChange(Action<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings, string?> listener) => null;
        /// <summary>
        /// Executes the Update operation.
        /// </summary>
        public void Update(Action<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings> applyChanges) { }
        /// <summary>
        /// Executes the UpdateAsync operation.
        /// </summary>
        public Task UpdateAsync(Func<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings, Task> applyChanges) => Task.CompletedTask;
    }

    /// <summary>
    /// Creates a design-time logger for the designer.
    /// </summary>
    /// <returns>A logger instance for design-time use.</returns>
    private static ILogger<MainWindowViewModel> CreateDesignTimeLogger()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { });
        return loggerFactory.CreateLogger<MainWindowViewModel>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="navigation">The navigation ViewModel.</param>
    /// <param name="settings">The settings management ViewModel.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution during shutdown.</param>
    public MainWindowViewModel(
        NavigationViewModel navigation,
        SettingsManagementViewModel settings,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IApplicationSettingsService settingsService,
        IFileDialogService? fileDialogService,
        ILogger<MainWindowViewModel> logger,
        IServiceProvider serviceProvider)
    {
        Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _fileDialogService = fileDialogService; // optional in design-time
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Initialize commands
        ExitCommand = ReactiveCommand.CreateFromTask(ExitAsync);
        AboutCommand = ReactiveCommand.Create(() => OpenDocumentTab(_serviceProvider.GetRequiredService<IViewModelFactory>().Create<AboutViewModel>()));

        CutCommand = ReactiveCommand.Create(() => { StatusMessage = "Command executed: Cut"; _logger.LogInformation("Cut command executed"); });
        CopyCommand = ReactiveCommand.Create(() => { StatusMessage = "Command executed: Copy"; _logger.LogInformation("Copy command executed"); });
        PasteCommand = ReactiveCommand.Create(() => { StatusMessage = "Command executed: Paste"; _logger.LogInformation("Paste command executed"); });

        LoadConfigurationCommand = ReactiveCommand.CreateFromTask(LoadConfigurationAsync);
        SaveConfigurationCommand = ReactiveCommand.CreateFromTask(SaveConfigurationAsync);

        CloseApplicationInteraction = new Interaction<Unit, Unit>();

        // Set up reactive pattern for button pressed message clearing
        // This replaces the async void ClearButtonPressedAfterDelay method
        this.WhenAnyValue(x => x.LastButtonPressed)
            .Where(name => !string.IsNullOrEmpty(name))
            .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(3)))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                LastButtonPressed = "";
                StatusMessage = UIStrings.StatusReady;
            })
            .DisposeWith(_disposables);

        // Initialize Docking System
        InitializeDocking();

        // Wire navigation to open content in dock tabs
        Navigation.OpenDocumentAction = OpenDocumentTab;
        Navigation.OpenToolAction = OpenToolTab;
        Navigation.LogViewerAction = () =>
        {
            OpenToolTab(_serviceProvider.GetRequiredService<LogViewerViewModel>());
        };

        // Pre-wire TaskManagerViewModel so it can open task logs automatically
        // even if the user hasn't visited the Tasks sidebar view yet
        var taskManagerVm = _serviceProvider.GetService<ViewModels.Tasks.TaskManagerViewModel>();
        if (taskManagerVm != null)
        {
            taskManagerVm.OpenDocumentAction = vm => OpenDocumentTab(vm);
            taskManagerVm.OpenToolAction = vm => OpenToolTab(vm);
        }

        _logger.LogDebug("MainWindowViewModel initialized with specialized ViewModels and docking system");
    }

    #endregion

    #region Specialized ViewModels

    /// <summary>
    /// Gets the navigation ViewModel that handles sidebar and content management.
    /// </summary>
    public NavigationViewModel Navigation { get; }

    /// <summary>
    /// Gets the settings management ViewModel that handles all settings-related functionality.
    /// </summary>
    public SettingsManagementViewModel Settings { get; }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets the last button pressed message.
    /// </summary>
    public string LastButtonPressed
    {
        get => _lastButtonPressed;
        set => this.RaiseAndSetIfChanged(ref _lastButtonPressed, value);
    }

    /// <summary>
    /// Gets or sets the root dock layout used by DockControl in the view.
    /// </summary>
    public IRootDock? Layout
    {
        get => _layout;
        set => this.RaiseAndSetIfChanged(ref _layout, value);
    }

    #endregion

    #region Docking

    /// <summary>
    /// Initializes the docking system with factory and layout.
    /// </summary>
    private void InitializeDocking()
    {
        try
        {
            _factory = new MainDockFactory(this)
            {
                LogViewerContent = Navigation.CreateViewModel<LogViewerViewModel>(),
                SettingsContent = Navigation.CreateViewModel<SettingsViewModel>(),
                HomeContent = Navigation.CreateHomeViewModel(),
            };

            Layout = _factory.CreateLayout();
            if (Layout is { })
            {
                _factory.InitLayout(Layout);
            }

            _logger.LogDebug("Docking system initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize docking system");
        }
    }

    /// <summary>
    /// Opens a dockable ViewModel as a Document tab in the main editor area.
    /// </summary>
    public void OpenDocumentTab(IDockableViewModel docVm)
    {
        if (_factory is MainDockFactory mainFactory)
        {
            mainFactory.OpenDocument(docVm);
        }
    }

    /// <summary>
    /// Opens a dockable ViewModel as a Tool window in the bottom panel.
    /// </summary>
    public void OpenToolTab(IDockableViewModel toolVm)
    {
        if (_factory is MainDockFactory mainFactory)
        {
            mainFactory.OpenTool(toolVm);
        }
    }

    /// <summary>
    /// Closes the currently active document tab.
    /// </summary>
    public void CloseActiveDocument()
    {
        _logger.LogDebug("Closing current active dockable");
        if (Layout?.ActiveDockable is Dock.Model.Controls.IDocument document)
        {
            _factory?.CloseDockable(document);
        }
    }

    /// <summary>
    /// Opens the Settings view as a docked document tab.
    /// </summary>
    public void OpenSettings()
    {
        if (_factory is MainDockFactory mainDockFactory)
        {
            mainDockFactory.RestoreSettings();
        }
        _logger.LogDebug("Settings view opened via dock");
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to exit the application.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>
    /// Gets the command to open the About view.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AboutCommand { get; }

    /// <summary>
    /// Gets the command to perform a Cut operation.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CutCommand { get; }

    /// <summary>
    /// Gets the command to perform a Copy operation.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }

    /// <summary>
    /// Gets the command to perform a Paste operation.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PasteCommand { get; }

    /// <summary>
    /// Command to load configuration from a file via file picker.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadConfigurationCommand { get; }

    /// <summary>
    /// Command to save configuration to a file via file picker.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveConfigurationCommand { get; }

    /// <summary>
    /// Interaction to signal the view to close the application.
    /// </summary>
    public Interaction<Unit, Unit> CloseApplicationInteraction { get; }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Handles application exit with confirmation dialog.
    /// Used by File menu and keyboard shortcuts.
    /// </summary>
    private async Task ExitAsync()
    {
        try
        {
            _logger.LogInformation("Initiating application exit sequence via command");

            // Just request close; the View's OnClosing handler will manage confirmation and shutdown
            // This prevents the double-dialog issue where ExitAsync shows one and OnClosing shows another
            await CloseApplicationInteraction.Handle(Unit.Default).FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during application exit sequence");

            // Show error to user and force exit if needed
            try
            {
                await _dialogService.ShowErrorAsync(
                    "Exit Error",
                    $"An error occurred during application exit: {ex.Message}");
            }
            finally
            {
                // Force exit if we can't handle the error gracefully
                Environment.Exit(1);
            }
        }
    }

    /// <summary>
    /// Shows the exit confirmation dialog.
    /// </summary>
    /// <returns>True if user confirms exit, false otherwise.</returns>
    public async Task<bool> ShowExitConfirmationAsync()
    {
        try
        {
            _logger.LogDebug("Showing exit confirmation dialog");
            return await _dialogService.ShowConfirmationAsync(
                UIStrings.Dialog_ExitTitle,
                UIStrings.Confirm_Exit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing exit confirmation dialog");
            // Default to false (don't exit) on error
            return false;
        }
    }

    /// <summary>
    /// Performs graceful shutdown of all services.
    /// </summary>
    public async Task PerformShutdownAsync()
    {
        try
        {
            _logger.LogInformation("Starting application shutdown sequence");

            // Perform graceful shutdown of all services
            await _serviceProvider.ShutdownS7ToolsServicesAsync().ConfigureAwait(false);

            _logger.LogInformation("Application shutdown sequence completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application shutdown sequence");
            // Don't rethrow - let the calling code handle it
        }
    }


    /// <summary>
    /// Reloads the current application settings.
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
        try
        {
            await _settingsService.LoadSettingsAsync();
            StatusMessage = UIStrings.Status_ConfigurationReloadedSuccessfully;
            _logger.LogInformation("Configuration reloaded from settings file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            StatusMessage = UIStrings.Status_FailedToReloadConfiguration;
        }
    }

    /// <summary>
    /// Forces a save of current settings to the settings file.
    /// </summary>
    private async Task SaveConfigurationAsync()
    {
        try
        {
            // Save the current user settings
            await _settingsService.UpdateSettingsAsync(_ => { });
            StatusMessage = UIStrings.Status_ConfigurationSavedSuccessfully;
            _logger.LogInformation("Configuration saved to settings file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            StatusMessage = UIStrings.Status_FailedToSaveConfiguration;
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of the resources used by this ViewModel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables?.Dispose();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Navigates to the specified view model type.
    /// Delegates to the Navigation ViewModel.
    /// </summary>
    /// <param name="viewModelType">The view model type.</param>
    public void NavigateTo(Type viewModelType)
    {
        Navigation.NavigateTo(viewModelType);
    }

    #endregion
}
