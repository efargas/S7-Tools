using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace S7Tools.Views;

/// <summary>
/// Task Manager View for monitoring and managing task execution.
/// Displays real-time task progress, allows task control operations, and provides task history.
/// </summary>
/// <remarks>
/// This view provides a comprehensive interface for task management including:
/// - Active task monitoring with progress indicators
/// - Scheduled task management and modification
/// - Task history and completion status
/// - Task control operations (start, stop, cancel, restart)
/// - Auto-refresh capabilities for real-time updates
///
/// The view is designed to work with TaskManagerViewModel and provides:
/// - Multi-tab interface for different task states
/// - DataGrid-based task listings with action buttons
/// - Status information and update indicators
/// - Responsive layout with proper data binding
/// </remarks>
public partial class TaskManagerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskManagerView"/> class.
    /// </summary>
    public TaskManagerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the component using XAML markup.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
