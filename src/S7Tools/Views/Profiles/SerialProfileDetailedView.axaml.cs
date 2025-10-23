using Avalonia.Controls;

namespace S7Tools.Views.Profiles;

/// <summary>
/// Detailed view for serial port profile configuration that shows ALL properties,
/// including advanced STTY flags marked with [Browsable(false)].
/// This is used in contexts like the Job Wizard where users need to see
/// comprehensive configuration details.
/// </summary>
public partial class SerialProfileDetailedView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerialProfileDetailedView"/> class.
    /// </summary>
    public SerialProfileDetailedView()
    {
        InitializeComponent();
    }
}
