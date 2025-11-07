using Avalonia.Controls;
using Avalonia.Interactivity;
using S7Tools.ViewModels.Dialogs;

namespace S7Tools.Views.Dialogs;

/// <summary>
/// Code-behind for the Create Memory Region Profile dialog.
/// </summary>
public partial class CreateMemoryRegionProfileDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the CreateMemoryRegionProfileDialog class.
    /// </summary>
    public CreateMemoryRegionProfileDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the CreateMemoryRegionProfileDialog class with a specific ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind to this dialog.</param>
    public CreateMemoryRegionProfileDialog(CreateMemoryRegionProfileDialogViewModel viewModel)
        : this()
    {
        DataContext = viewModel;

        // Subscribe to close requests from the ViewModel
        if (viewModel != null)
        {
            viewModel.CloseRequested += OnProfileCreated;
        }
    }

    /// <summary>
    /// Handles the close request from the ViewModel.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="success">True if the operation was successful, false if cancelled.</param>
    private void OnProfileCreated(object? sender, bool success)
    {
        Close(success);
    }

    /// <summary>
    /// Handles the window closing event to unsubscribe from ViewModel events.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is CreateMemoryRegionProfileDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnProfileCreated;
        }

        base.OnClosing(e);
    }
}
