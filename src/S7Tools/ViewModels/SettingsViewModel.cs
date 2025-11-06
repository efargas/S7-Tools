using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Controls;

namespace S7Tools.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, ViewModelBase> _categoryViewModels;

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _categoryViewModels = new Dictionary<string, ViewModelBase>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Logging",
            "General",
            "Appearance",
            "Advanced",
            "Serial Ports",
            "Servers",
            "Power Supply"
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
                "General" => new GeneralSettingsViewModel(),
                "Appearance" => new AppearanceSettingsViewModel(),
                "Advanced" => new AdvancedSettingsViewModel(),
                "Serial Ports" => CreateSerialPortsSettingsViewModel(),
                "Servers" => CreateSocatSettingsViewModel(),
                "Power Supply" => CreatePowerSupplySettingsViewModel(),
                _ => new GeneralSettingsViewModel()
            };

            _categoryViewModels[category] = viewModel;
            return viewModel;
        }
        catch (Exception ex)
        {
            // Log the full exception with stack trace to identify the root cause
            var logger = _serviceProvider.GetRequiredService<ILogger<SettingsViewModel>>();
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

    private SerialPortsSettingsViewModel CreateSerialPortsSettingsViewModel()
    {
        ISerialPortProfileService profileService = _serviceProvider.GetRequiredService<ISerialPortProfileService>();
        ISerialPortService portService = _serviceProvider.GetRequiredService<ISerialPortService>();
        IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        IProfileEditDialogService profileEditDialogService = _serviceProvider.GetRequiredService<IProfileEditDialogService>();
        IClipboardService clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        IUIThreadService uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        IUnifiedProfileDialogService unifiedProfileDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        S7Tools.Core.Interfaces.Services.IPathService pathService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IPathService>();
        SerialPortDiscoveryViewModel portScanner = _serviceProvider.GetRequiredService<SerialPortDiscoveryViewModel>();
        ILogger<SerialPortsSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<SerialPortsSettingsViewModel>>();

        return new SerialPortsSettingsViewModel(profileService, portService, dialogService, profileEditDialogService, clipboardService, fileDialogService, settingsService, uiThreadService, unifiedProfileDialogService, pathService, portScanner, logger);
    }

    private SocatSettingsViewModel CreateSocatSettingsViewModel()
    {
        IUnifiedProfileDialogService unifiedDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        ILogger<ProfileManagementViewModelBase<SocatProfile>> logger = _serviceProvider.GetRequiredService<ILogger<ProfileManagementViewModelBase<SocatProfile>>>();
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

    private PowerSupplySettingsViewModel CreatePowerSupplySettingsViewModel()
    {
        IPowerSupplyProfileService profileService = _serviceProvider.GetRequiredService<IPowerSupplyProfileService>();
        IPowerSupplyService powerSupplyService = _serviceProvider.GetRequiredService<IPowerSupplyService>();
        IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        IUnifiedProfileDialogService unifiedDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        IClipboardService clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService = _serviceProvider.GetRequiredService<S7Tools.Core.Interfaces.Services.IApplicationSettingsService>();
        IUIThreadService uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        ILogger<ProfileManagementViewModelBase<PowerSupplyProfile>> logger = _serviceProvider.GetRequiredService<ILogger<ProfileManagementViewModelBase<PowerSupplyProfile>>>();
        IPathService pathService = _serviceProvider.GetRequiredService<IPathService>();

        return new PowerSupplySettingsViewModel(unifiedDialogService, logger, uiThreadService, profileService, powerSupplyService, dialogService, clipboardService, fileDialogService, settingsService, pathService);
    }
}
