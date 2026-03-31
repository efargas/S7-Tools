using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Controls;

namespace S7Tools.ViewModels.Settings;

/// <summary>
/// Represents the SettingsViewModel.
/// </summary>
public class SettingsViewModel : ViewModelBase, IDockableViewModel, IDisposable
{
    private bool _disposed;
    // IDockableViewModel implementation
    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "Settings";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Settings";
    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;
    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, ViewModelBase> _categoryViewModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _categoryViewModels = new Dictionary<string, ViewModelBase>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Logging",
            "General",
            "Appearance",
            "Paths Settings",
            "Serial Ports",
            "Servers",
            "Test",
        });

        // Initialize with Logging category
        SelectedCategory = Categories[0];
        SelectedCategoryViewModel = GetCategoryViewModel(SelectedCategory);

        SelectCategoryCommand = ReactiveCommand.Create<string>(category =>
        {
            if (!string.IsNullOrWhiteSpace(category))
            {
                SelectedCategory = category;
                SelectedCategoryViewModel = GetCategoryViewModel(category);
            }
        });
    }

    /// <summary>
    /// Gets or sets the Categories.
    /// </summary>
    public ObservableCollection<string> Categories { get; }

    private string _selectedCategory = "Logging";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            // Guard against null or empty category
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            SelectedCategoryViewModel = GetCategoryViewModel(_selectedCategory);
        }
    }

    private ViewModelBase? _selectedCategoryViewModel;
    public ViewModelBase? SelectedCategoryViewModel
    {
        get => _selectedCategoryViewModel;
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryViewModel, value);
    }

    /// <summary>
    /// Gets or sets the SelectCategoryCommand.
    /// </summary>
    public ReactiveCommand<string, Unit> SelectCategoryCommand { get; }

    private ViewModelBase GetCategoryViewModel(string category)
    {
        // Guard against null or empty category
        if (string.IsNullOrWhiteSpace(category))
        {
            category = "Logging"; // Default to Logging category
        }

        if (_categoryViewModels.TryGetValue(category, out ViewModelBase? existingViewModel))
        {
            return existingViewModel;
        }

        try
        {
            ViewModelBase viewModel = category switch
            {
                "Logging" => CreateLoggingSettingsViewModel(),
                "General" => CreateGeneralSettingsViewModel(),
                "Appearance" => CreateAppearanceSettingsViewModel(),
                "Paths Settings" => CreatePathSettingsViewModel(),
                "Serial Ports" => CreateSerialPortsSettingsViewModel(),
                "Servers" => CreateSocatSettingsViewModel(),
                "Test" => CreateTestSettingsViewModel(),
                _ => new GeneralSettingsViewModel()
            };

            _categoryViewModels[category] = viewModel;
            return viewModel;
        }
        catch (Exception ex)
        {
            // Log the full exception with stack trace to identify the root cause
            ILogger<SettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<SettingsViewModel>>();
            logger.LogError(ex, "CRITICAL: Failed to create ViewModel for category '{Category}'. Exception: {Message}", category, ex.Message);

            // Return a placeholder ViewModel to prevent application crash
            var placeholder = new GeneralSettingsViewModel();
            _categoryViewModels[category] = placeholder;
            return placeholder;
        }
    }

    private LoggingSettingsViewModel CreateLoggingSettingsViewModel()
    {
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        S7Tools.Core.Interfaces.Services.IPathService pathService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        ILogger<LoggingSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<LoggingSettingsViewModel>>();

        return new LoggingSettingsViewModel(settingsService, pathService, fileDialogService, logger);
    }

    private GeneralSettingsViewModel CreateGeneralSettingsViewModel()
    {
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        S7Tools.Core.Interfaces.Services.IPathService pathService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
        ILogger<GeneralSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<GeneralSettingsViewModel>>();

        return new GeneralSettingsViewModel(settingsService, pathService, logger);
    }

    private AppearanceSettingsViewModel CreateAppearanceSettingsViewModel()
    {
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        return new AppearanceSettingsViewModel(settingsService);
    }

    private PathSettingsViewModel CreatePathSettingsViewModel()
    {
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        S7Tools.Core.Interfaces.Services.IPathService pathService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        ILogger<PathSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<PathSettingsViewModel>>();

        return new PathSettingsViewModel(settingsService, pathService, fileDialogService, logger);
    }

    private SerialPortsSettingsViewModel CreateSerialPortsSettingsViewModel()
    {
        ISerialPortProfileService profileService = _serviceProvider.GetRequiredService<ISerialPortProfileService>();
        ISerialPortService portService = _serviceProvider.GetRequiredService<ISerialPortService>();
        IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        IClipboardService clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        IUIThreadService uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        IUnifiedProfileDialogService unifiedProfileDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        S7Tools.Core.Interfaces.Services.IPathService pathService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
        SerialPortDiscoveryViewModel portScanner = _serviceProvider.GetRequiredService<SerialPortDiscoveryViewModel>();
        ILogger<SerialPortsSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<SerialPortsSettingsViewModel>>();

        return new SerialPortsSettingsViewModel(profileService, portService, dialogService, clipboardService, fileDialogService, settingsService, uiThreadService, unifiedProfileDialogService, pathService, portScanner, logger);
    }

    private SocatSettingsViewModel CreateSocatSettingsViewModel()
    {
        IUnifiedProfileDialogService unifiedDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        ILogger<ProfileManagementViewModelBase<SocatProfile>> logger = _serviceProvider.GetRequiredService<ILogger<ProfileManagementViewModelBase<SocatProfile>>>();
        ILogger<SocatSettingsViewModel> specificLogger = _serviceProvider.GetRequiredService<ILogger<SocatSettingsViewModel>>();
        IUIThreadService uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        ISocatProfileService socatProfileService = _serviceProvider.GetRequiredService<ISocatProfileService>();
        ISocatService socatService = _serviceProvider.GetRequiredService<ISocatService>();
        ISerialPortService serialPortService = _serviceProvider.GetRequiredService<ISerialPortService>();
        IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        IClipboardService clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        IFileDialogService fileDialogService = _serviceProvider.GetRequiredService<IFileDialogService>();
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        IPathService pathService = _serviceProvider.GetRequiredService<IPathService>();
        SerialPortDiscoveryViewModel portScanner = _serviceProvider.GetRequiredService<SerialPortDiscoveryViewModel>();

        return new SocatSettingsViewModel(
            unifiedDialogService,
            logger,
            specificLogger,
            uiThreadService,
            socatProfileService,
            socatService,
            serialPortService,
            dialogService,
            clipboardService,
            fileDialogService,
            settingsService,
            pathService,
            portScanner);
    }

    private TestSettingsViewModel CreateTestSettingsViewModel()
    {
        IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        IClipboardService clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        ILogger<TestSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<TestSettingsViewModel>>();

        return new TestSettingsViewModel(dialogService, clipboardService, logger);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            foreach (ViewModelBase vm in _categoryViewModels.Values)
            {
                if (vm is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _categoryViewModels.Clear();
        }
    }

}
