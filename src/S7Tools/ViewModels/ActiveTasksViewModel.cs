using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// Wrapper ViewModel for the "Active Tasks" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class ActiveTasksViewModel : ViewModelBase
{
    public ActiveTasksViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public TaskManagerViewModel Manager { get; }
}
