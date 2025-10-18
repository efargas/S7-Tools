using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// Wrapper ViewModel for the "History" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class HistoryTasksViewModel : ViewModelBase
{
    public HistoryTasksViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public TaskManagerViewModel Manager { get; }
}
