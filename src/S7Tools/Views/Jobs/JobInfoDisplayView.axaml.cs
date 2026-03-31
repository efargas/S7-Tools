using Avalonia.LogicalTree;
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
        SetupExpanderBehavior();
    }

    /// <summary>
    /// Gets or sets the view model for this view
    /// </summary>
    public JobInfoDisplayViewModel? ViewModel
    {
        get => DataContext as JobInfoDisplayViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Sets up the expander behavior so only one can be expanded at a time
    /// </summary>
    private void SetupExpanderBehavior()
    {
        this.AttachedToVisualTree += (sender, e) =>
        {
            // Find all expanders in the view
            var expanders = this.GetLogicalDescendants().OfType<Expander>().ToList();

            foreach (Expander? expander in expanders)
            {
                expander.Expanding += (s, args) =>
                {
                    if (s is Expander currentExpander)
                    {
                        // Collapse all other expanders when this one expands
                        foreach (Expander? otherExpander in expanders.Where(e => e != currentExpander))
                        {
                            otherExpander.IsExpanded = false;
                        }
                    }
                };
            }
        };
    }
}
