using Avalonia.Controls;

namespace S7Tools.Views.Settings;

/// <summary>
/// View for Memory Region settings configuration, providing UI for managing
/// memory region profiles for PLC firmware memory mapping.
/// </summary>
/// <remarks>
/// This view follows the established settings page pattern with DataGrid-based
/// profile management, path configuration, and import/export functionality.
/// Uses the unified profile management ViewModel pattern for consistency.
/// </remarks>
public partial class MemoryRegionSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the MemoryRegionSettingsView class.
    /// </summary>
    public MemoryRegionSettingsView()
    {
        InitializeComponent();
    }
}
