using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using S7Tools.ViewModels.Pages;

namespace S7Tools.Views.Pages;

/// <summary>
/// Code-behind for the LogViewerView user control.
/// </summary>
public partial class LogViewerView : UserControl
{
    public LogViewerView()
    {
        InitializeComponent();
    }
}
