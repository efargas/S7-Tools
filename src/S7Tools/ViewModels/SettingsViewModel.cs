using System.Collections.ObjectModel;
using ReactiveUI;
using System.Reactive;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly Dictionary<string, ViewModelBase> _categoryViewModels;

    public SettingsViewModel(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _categoryViewModels = new Dictionary<string, ViewModelBase>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Logging",
            "General",
            "Appearance",
            "Advanced",
            "Serial Ports",
            "Servers"
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
            "Logging" => _viewModelFactory.Create<LoggingSettingsViewModel>(),
            "General" => _viewModelFactory.Create<GeneralSettingsViewModel>(),
            "Appearance" => _viewModelFactory.Create<AppearanceSettingsViewModel>(),
            "Advanced" => _viewModelFactory.Create<AdvancedSettingsViewModel>(),
            "Serial Ports" => _viewModelFactory.Create<SerialPortsSettingsViewModel>(),
            "Servers" => _viewModelFactory.Create<SocatSettingsViewModel>(),
            _ => _viewModelFactory.Create<GeneralSettingsViewModel>()
        };

        _categoryViewModels[category] = viewModel;
        return viewModel;
    }
}
