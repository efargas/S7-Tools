using System.Collections.ObjectModel;
using ReactiveUI;
using System.Reactive;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the main settings view, which hosts various setting categories.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly Dictionary<string, ViewModelBase> _categoryViewModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="viewModelFactory">The factory to create child ViewModels.</param>
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

    /// <summary>
    /// Gets the collection of available settings categories.
    /// </summary>
    public ObservableCollection<string> Categories { get; }

    private string _selectedCategory = "Logging";
    /// <summary>
    /// Gets or sets the currently selected settings category.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the ViewModel corresponding to the currently selected category.
    /// </summary>
    public ViewModelBase? SelectedCategoryViewModel
    {
        get => _selectedCategoryViewModel;
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryViewModel, value);
    }

    /// <summary>
    /// Gets the command to select a new settings category.
    /// </summary>
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
