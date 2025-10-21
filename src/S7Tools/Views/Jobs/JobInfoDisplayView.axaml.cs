using Avalonia.Controls;
using S7Tools.ViewModels.Jobs;

namespace S7Tools.Views.Jobs;

/// <summary>
/// View for displaying detailed job information including all associated profile details
/// </summary>
public partial class JobInfoDisplayView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the JobInfoDisplayView
    /// </summary>
    public JobInfoDisplayView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the view model for this view
    /// </summary>
    public JobInfoDisplayViewModel? ViewModel
    {
        get => DataContext as JobInfoDisplayViewModel;
        set => DataContext = value;
    }
}
