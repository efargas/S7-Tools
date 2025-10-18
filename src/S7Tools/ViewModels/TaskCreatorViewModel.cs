using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// Wrapper ViewModel for the "Task Creator" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class TaskCreatorViewModel : ViewModelBase
{
    public TaskCreatorViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public TaskManagerViewModel Manager { get; }
}
