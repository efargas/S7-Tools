using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Profiles;

/// <summary>
/// ViewModel for the Profiles management page.
/// Manages navigation between profile category sub-views (Serial Ports, Servers, Power Supply, Memory Regions).
/// </summary>
public class ProfilesViewModel : ViewModelBase, IDockableViewModel
{
    // IDockableViewModel implementation
    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "Profiles";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Profiles";
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
    /// Initializes a new instance of the <see cref="ProfilesViewModel"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve category-specific view models.</param>
    public ProfilesViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _categoryViewModels = new Dictionary<string, ViewModelBase>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Serial Ports",
            "Servers",
            "Power Supply",
            "Memory Regions"
        });

        // Initialize with first category
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
    /// Gets the collection of available profile category names.
    /// </summary>
    public ObservableCollection<string> Categories { get; }

    private string _selectedCategory = "Serial Ports";

    /// <summary>
    /// Gets or sets the currently selected profile category name.
    /// Setting this property also updates <see cref="SelectedCategoryViewModel"/>.
    /// </summary>
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            SelectedCategoryViewModel = GetCategoryViewModel(_selectedCategory);
        }
    }

    private ViewModelBase? _selectedCategoryViewModel;

    /// <summary>
    /// Gets or sets the ViewModel corresponding to the currently selected profile category.
    /// </summary>
    public ViewModelBase? SelectedCategoryViewModel
    {
        get => _selectedCategoryViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategoryViewModel, value);
            this.RaisePropertyChanged("SidebarItemTapped");
        }
    }

    /// <summary>
    /// Gets the dockable view model for the currently selected profile category, if applicable.
    /// </summary>
    /// <returns>The selected category view model as <see cref="IDockableViewModel"/>, or <see langword="null"/> if the selected view model is not dockable.</returns>
    public IDockableViewModel? GetDockableForOpen()
    {
        return SelectedCategoryViewModel as IDockableViewModel;
    }

    /// <summary>
    /// Gets the command to select a profile category by name.
    /// </summary>
    public ReactiveCommand<string, Unit> SelectCategoryCommand { get; }

    private ViewModelBase GetCategoryViewModel(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            category = "Serial Ports";
        }

        if (_categoryViewModels.TryGetValue(category, out ViewModelBase? existingViewModel))
        {
            return existingViewModel;
        }

        try
        {
            ViewModelBase viewModel = category switch
            {
                "Serial Ports" => _serviceProvider.GetRequiredService<SerialPortProfilesViewModel>(),
                "Servers" => _serviceProvider.GetRequiredService<SocatProfilesViewModel>(),
                "Power Supply" => _serviceProvider.GetRequiredService<PowerSupplyProfilesViewModel>(),
                "Memory Regions" => _serviceProvider.GetRequiredService<MemoryRegionProfilesViewModel>(),
                _ => _serviceProvider.GetRequiredService<SerialPortProfilesViewModel>()
            };

            _categoryViewModels[category] = viewModel;
            return viewModel;
        }
        catch (Exception ex)
        {
            var logger = _serviceProvider.GetService<ILogger<ProfilesViewModel>>();
            logger?.LogError(ex, "Error creating ViewModel for profile category: {Category}", category);

            // Fallback
            return _serviceProvider.GetRequiredService<SerialPortProfilesViewModel>();
        }
    }
}
