using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

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
        if (_categoryViewModels.TryGetValue(category, out var existingViewModel))
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
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<LoggingSettingsViewModel>>();

        return new LoggingSettingsViewModel(settingsService, fileDialogService, logger);
    }

    private SerialPortsSettingsViewModel CreateSerialPortsSettingsViewModel()
    {
        var profileService = _serviceProvider.GetRequiredService<ISerialPortProfileService>();
        var portService = _serviceProvider.GetRequiredService<ISerialPortService>();
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        var profileEditDialogService = _serviceProvider.GetRequiredService<IProfileEditDialogService>();
        var clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        var fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        var unifiedProfileDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<SerialPortsSettingsViewModel>>();

        return new SerialPortsSettingsViewModel(profileService, portService, dialogService, profileEditDialogService, clipboardService, fileDialogService, settingsService, uiThreadService, unifiedProfileDialogService, logger);
    }

    private SocatSettingsViewModel CreateSocatSettingsViewModel()
    {
        var socatProfileService = _serviceProvider.GetRequiredService<ISocatProfileService>();
        var socatService = _serviceProvider.GetRequiredService<ISocatService>();
        var serialPortService = _serviceProvider.GetRequiredService<ISerialPortService>();
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        var profileEditDialogService = _serviceProvider.GetRequiredService<IProfileEditDialogService>();
        var clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        var fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<SocatSettingsViewModel>>();

        return new SocatSettingsViewModel(socatProfileService, socatService, serialPortService, dialogService, profileEditDialogService, clipboardService, fileDialogService, settingsService, uiThreadService, logger);
    }

    private PowerSupplySettingsViewModel CreatePowerSupplySettingsViewModel()
    {
        var profileService = _serviceProvider.GetRequiredService<IPowerSupplyProfileService>();
        var powerSupplyService = _serviceProvider.GetRequiredService<IPowerSupplyService>();
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        var profileEditDialogService = _serviceProvider.GetRequiredService<IProfileEditDialogService>();
        var clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        var fileDialogService = _serviceProvider.GetService<IFileDialogService>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var uiThreadService = _serviceProvider.GetRequiredService<S7Tools.Services.Interfaces.IUIThreadService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<PowerSupplySettingsViewModel>>();

        return new PowerSupplySettingsViewModel(profileService, powerSupplyService, dialogService, profileEditDialogService, clipboardService, fileDialogService, settingsService, uiThreadService, logger);
    }
}
