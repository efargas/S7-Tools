using Avalonia.Controls;
using Avalonia.Interactivity;
using S7Tools.ViewModels;

namespace S7Tools.Views;

/// <summary>
/// Code-behind for the LogViewerView user control.
/// </summary>
public partial class LogViewerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the LogViewerView class.
    /// </summary>
    public LogViewerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the loaded event to set up auto-scroll behavior.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Set up auto-scroll behavior if needed
        if (DataContext is LogViewerViewModel viewModel)
        {
            // Subscribe to property changes to handle auto-scroll
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Handles the unloaded event to clean up subscriptions.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (DataContext is LogViewerViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        base.OnUnloaded(e);
    }

    /// <summary>
    /// Handles property changes from the view model.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is LogViewerViewModel viewModel && e.PropertyName == nameof(LogViewerViewModel.FilteredLogEntries))
        {
            // Handle auto-scroll if enabled
            if (viewModel.AutoScroll && viewModel.FilteredLogEntries.Count > 0)
            {
                // Find the DataGrid and scroll to the last item
                var dataGrid = this.FindControl<DataGrid>("LogDataGrid");
                if (dataGrid != null && viewModel.FilteredLogEntries.Count > 0)
                {
                    var lastItem = viewModel.FilteredLogEntries[^1];
                    dataGrid.ScrollIntoView(lastItem, null);
                }
            }
        }
    }
}