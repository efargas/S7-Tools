using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// Wrapper ViewModel for the "Task Runner" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class TaskRunnerViewModel : ViewModelBase
{
    public TaskRunnerViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public TaskManagerViewModel Manager { get; }
}
