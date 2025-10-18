using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

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
        if (_categoryViewModels.TryGetValue(category, out ViewModelBase? existingViewModel))
        {
            return existingViewModel;
        }

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

    private LoggingSettingsViewModel CreateLoggingSettingsViewModel()
    {
        ISettingsService settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        ILogger<LoggingSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<LoggingSettingsViewModel>>();

        return new LoggingSettingsViewModel(settingsService, fileDialogService, logger);
    }

    private SerialPortsSettingsViewModel CreateSerialPortsSettingsViewModel()
    {
        ISerialPortProfileService profileService = _serviceProvider.GetRequiredService<ISerialPortProfileService>();
        ISerialPortService portService = _serviceProvider.GetRequiredService<ISerialPortService>();
        IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        IProfileEditDialogService profileEditDialogService = _serviceProvider.GetRequiredService<IProfileEditDialogService>();
        IClipboardService clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        ISettingsService settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        IUIThreadService uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        IUnifiedProfileDialogService unifiedProfileDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        ILogger<SerialPortsSettingsViewModel> logger = _serviceProvider.GetRequiredService<ILogger<SerialPortsSettingsViewModel>>();

        return new SerialPortsSettingsViewModel(profileService, portService, dialogService, profileEditDialogService, clipboardService, fileDialogService, settingsService, uiThreadService, unifiedProfileDialogService, logger);
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
        ISettingsService settingsService = _serviceProvider.GetRequiredService<ISettingsService>();

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
            settingsService);
    }

    private PowerSupplySettingsViewModel CreatePowerSupplySettingsViewModel()
    {
        IPowerSupplyProfileService profileService = _serviceProvider.GetRequiredService<IPowerSupplyProfileService>();
        IPowerSupplyService powerSupplyService = _serviceProvider.GetRequiredService<IPowerSupplyService>();
        IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        IUnifiedProfileDialogService unifiedDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        IClipboardService clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        IFileDialogService? fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        ISettingsService settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        IUIThreadService uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        ILogger<ProfileManagementViewModelBase<PowerSupplyProfile>> logger = _serviceProvider.GetRequiredService<ILogger<ProfileManagementViewModelBase<PowerSupplyProfile>>>();

        return new PowerSupplySettingsViewModel(unifiedDialogService, logger, uiThreadService, profileService, powerSupplyService, dialogService, clipboardService, fileDialogService, settingsService);
    }
}
