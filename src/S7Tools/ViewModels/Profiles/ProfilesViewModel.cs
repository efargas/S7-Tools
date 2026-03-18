using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Settings;

namespace S7Tools.ViewModels.Profiles;

public class ProfilesViewModel : ViewModelBase, IDockableViewModel
{
    // IDockableViewModel implementation
    public string DockId => "Profiles";
    public string DockTitle => "Profiles";
    public bool CanClose => true;
    public bool CanFloat => true;

    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, ViewModelBase> _categoryViewModels;

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

    public ObservableCollection<string> Categories { get; }

    private string _selectedCategory = "Serial Ports";
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
    public ViewModelBase? SelectedCategoryViewModel
    {
        get => _selectedCategoryViewModel;
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryViewModel, value);
    }

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
                "Serial Ports" => _serviceProvider.GetRequiredService<SerialPortsSettingsViewModel>(),
                "Servers" => _serviceProvider.GetRequiredService<SocatSettingsViewModel>(),
                "Power Supply" => _serviceProvider.GetRequiredService<PowerSupplySettingsViewModel>(),
                "Memory Regions" => _serviceProvider.GetRequiredService<MemoryRegionSettingsViewModel>(),
                _ => _serviceProvider.GetRequiredService<SerialPortsSettingsViewModel>()
            };

            _categoryViewModels[category] = viewModel;
            return viewModel;
        }
        catch (Exception ex)
        {
            var logger = _serviceProvider.GetService<ILogger<ProfilesViewModel>>();
            logger?.LogError(ex, "Error creating ViewModel for profile category: {Category}", category);
            
            // Fallback
            return _serviceProvider.GetRequiredService<SerialPortsSettingsViewModel>();
        }
    }
}
