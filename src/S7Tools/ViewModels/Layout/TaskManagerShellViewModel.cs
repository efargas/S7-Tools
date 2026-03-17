using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Tasks;

namespace S7Tools.ViewModels.Layout;

/// <summary>
/// Shell ViewModel for Task Manager that mirrors the Settings pattern:
/// - Sidebar shows categories
/// - Main content is driven via ViewLocator from SelectedCategoryViewModel
/// </summary>
public sealed class TaskManagerShellViewModel : ViewModelBase, IDockableViewModel
{
    // IDockableViewModel implementation
    public string DockId => "TaskManager";
    public string DockTitle => "Task Manager";
    public bool CanClose => true;
    public bool CanFloat => true;

    private readonly IServiceProvider _serviceProvider;
    private readonly TaskManagerViewModel _taskManagerViewModel;
    private readonly ActiveTasksViewModel _activeTasksViewModel;
    private readonly ScheduledTasksViewModel _scheduledTasksViewModel;
    private readonly HistoryTasksViewModel _historyTasksViewModel;
    private readonly TaskCreatorViewModel _taskCreatorViewModel;

    public TaskManagerShellViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _taskManagerViewModel = _serviceProvider.GetRequiredService<TaskManagerViewModel>();
        _activeTasksViewModel = _serviceProvider.GetRequiredService<ActiveTasksViewModel>();
        _scheduledTasksViewModel = _serviceProvider.GetRequiredService<ScheduledTasksViewModel>();
        _historyTasksViewModel = _serviceProvider.GetRequiredService<HistoryTasksViewModel>();
        _taskCreatorViewModel = _serviceProvider.GetRequiredService<TaskCreatorViewModel>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Task Manager",
            "Task Creator",
            "Active Tasks",
            "Scheduled",
            "History"
        });

        // Default selection
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

    private string _selectedCategory = string.Empty;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            // Keep the main content in sync whenever the category changes via binding
            SelectedCategoryViewModel = GetCategoryViewModel(value);
        }
    }

    private ViewModelBase? _selectedCategoryViewModel;
    public ViewModelBase? SelectedCategoryViewModel
    {
        get => _selectedCategoryViewModel!;
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryViewModel, value);
    }

    public ReactiveCommand<string, Unit> SelectCategoryCommand { get; }

    private ViewModelBase GetCategoryViewModel(string category)
    {
        return category switch
        {
            "Task Manager" => _taskManagerViewModel,
            "Active Tasks" => _activeTasksViewModel,
            "Scheduled" => _scheduledTasksViewModel,
            "History" => _historyTasksViewModel,
            "Task Creator" => _taskCreatorViewModel,
            _ => _taskManagerViewModel
        };
    }
}
