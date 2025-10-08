using System.Collections.ObjectModel;
using ReactiveUI;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using S7Tools.Services.Interfaces;
using S7Tools.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

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
            "Serial Ports"
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
            "Serial Ports" => new GeneralSettingsViewModel(), // TODO: Implement SerialPortSettingsViewModel in Phase 3
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

    // TODO: Implement CreateSerialPortSettingsViewModel in Phase 3
}