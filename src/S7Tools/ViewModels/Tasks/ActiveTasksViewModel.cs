using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Tasks;

/// <summary>
/// Wrapper ViewModel for the "Active Tasks" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class ActiveTasksViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveTasksViewModel"/> class.
    /// </summary>
    public ActiveTasksViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Gets or sets the Manager.
    /// </summary>
    public TaskManagerViewModel Manager { get; }
}
