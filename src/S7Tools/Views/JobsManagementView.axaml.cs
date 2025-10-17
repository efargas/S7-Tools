using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace S7Tools.Views;

/// <summary>
/// Jobs Management View for creating, editing, and managing job profiles.
/// Provides comprehensive job profile management with detailed information display.
/// </summary>
/// <remarks>
/// This view provides a complete interface for job profile management including:
/// - Job profile listing with search and filtering capabilities
/// - Detailed job configuration display with expandable details panel
/// - CRUD operations (Create, Read, Update, Delete) for job profiles
/// - Profile reference management (Serial, Socat, Power Supply profiles)
/// - Memory region and timing configuration visualization
/// - Template and default profile management
///
/// The view is designed to work with JobsManagementViewModel and provides:
/// - DataGrid-based job profile listing with comprehensive columns
/// - Expandable details panel showing full job configuration
/// - Action buttons for profile management operations
/// - Status information and change tracking indicators
/// - Search functionality for quick profile location
/// </remarks>
public partial class JobsManagementView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobsManagementView"/> class.
    /// </summary>
    public JobsManagementView()
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
