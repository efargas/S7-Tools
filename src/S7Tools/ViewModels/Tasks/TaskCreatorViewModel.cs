using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Tasks;

/// <summary>
/// Wrapper ViewModel for the "Task Creator" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class TaskCreatorViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCreatorViewModel"/> class.
    /// </summary>
    public TaskCreatorViewModel(TaskManagerViewModel manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Gets or sets the Manager.
    /// </summary>
    public TaskManagerViewModel Manager { get; }
}
