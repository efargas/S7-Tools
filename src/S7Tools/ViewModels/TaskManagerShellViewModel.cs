using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// Shell ViewModel for Task Manager that mirrors the Settings pattern:
/// - Sidebar shows categories
/// - Main content is driven via ViewLocator from SelectedCategoryViewModel
/// </summary>
public sealed class TaskManagerShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TaskRunnerViewModel _taskRunnerViewModel;
    private readonly ActiveTasksViewModel _activeTasksViewModel;
    private readonly ScheduledTasksViewModel _scheduledTasksViewModel;
    private readonly HistoryTasksViewModel _historyTasksViewModel;
    private readonly TaskCreatorViewModel _taskCreatorViewModel;

    public TaskManagerShellViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    _taskRunnerViewModel = _serviceProvider.GetRequiredService<TaskRunnerViewModel>();
    _activeTasksViewModel = _serviceProvider.GetRequiredService<ActiveTasksViewModel>();
    _scheduledTasksViewModel = _serviceProvider.GetRequiredService<ScheduledTasksViewModel>();
    _historyTasksViewModel = _serviceProvider.GetRequiredService<HistoryTasksViewModel>();
    _taskCreatorViewModel = _serviceProvider.GetRequiredService<TaskCreatorViewModel>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Task Runner",
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
            "Task Runner" => _taskRunnerViewModel,
            "Active Tasks" => _activeTasksViewModel,
            "Scheduled" => _scheduledTasksViewModel,
            "History" => _historyTasksViewModel,
            "Task Creator" => _taskCreatorViewModel,
            _ => _taskRunnerViewModel
        };
    }
}
