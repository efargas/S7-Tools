using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// Wrapper ViewModel for the "Scheduled" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class ScheduledTasksViewModel : ViewModelBase
{
    public ScheduledTasksViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public TaskManagerViewModel Manager { get; }
}
