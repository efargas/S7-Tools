using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Tasks;

/// <summary>
/// Wrapper ViewModel for the "Scheduled" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class ScheduledTasksViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksViewModel"/> class.
    /// </summary>
    public ScheduledTasksViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Gets or sets the Manager.
    /// </summary>
    public TaskManagerViewModel Manager { get; }
}
