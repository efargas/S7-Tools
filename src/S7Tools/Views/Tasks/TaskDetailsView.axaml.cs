using Avalonia.Controls;
using Avalonia.ReactiveUI;
using S7Tools.ViewModels.Tasks;

namespace S7Tools.Views.Tasks;

/// <summary>
/// View for displaying detailed information about a selected task.
/// </summary>
public partial class TaskDetailsView : ReactiveUserControl<TaskDetailsViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDetailsView"/> class.
    /// </summary>
    public TaskDetailsView()
    {
        InitializeComponent();
    }
}
