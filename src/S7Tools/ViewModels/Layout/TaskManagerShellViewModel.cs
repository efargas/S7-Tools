using System.Collections.ObjectModel;
using System.Reactive;
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
    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "TaskManager";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Task Manager";
    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;
    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    private readonly IServiceProvider _serviceProvider;
    private readonly TaskManagerViewModel _taskManagerViewModel;
    private readonly ActiveTasksViewModel _activeTasksViewModel;
    private readonly ScheduledTasksViewModel _scheduledTasksViewModel;
    private readonly HistoryTasksViewModel _historyTasksViewModel;
    private readonly TaskCreatorViewModel _taskCreatorViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskManagerShellViewModel"/> class.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the Categories.
    /// </summary>
    public ObservableCollection<string> Categories { get; }

    private string _selectedCategory = string.Empty;
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

    /// <summary>
    /// Gets or sets the OpenDocumentAction and propagates it to child view models.
    /// </summary>
    private Action<IDockableViewModel>? _openDocumentAction;
    public Action<IDockableViewModel>? OpenDocumentAction
    {
        get => _openDocumentAction;
        set
        {
            _openDocumentAction = value;
            if (_taskManagerViewModel != null)
            {
                _taskManagerViewModel.OpenDocumentAction = value;
            }
        }
    }

    private Action<IDockableViewModel>? _openToolAction;
    public Action<IDockableViewModel>? OpenToolAction
    {
        get => _openToolAction;
        set
        {
            _openToolAction = value;
            if (_taskManagerViewModel != null)
            {
                _taskManagerViewModel.OpenToolAction = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the SelectCategoryCommand.
    /// </summary>
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
