using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using S7Tools.Models;

namespace S7Tools.Views;

/// <summary>
/// Input dialog for text entry with user interaction.
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the InputDialog class.
    /// </summary>
    public InputDialog()
    {
        InitializeComponent();

        // Focus the input field when the dialog opens
        Opened += (_, _) => InputTextBox.Focus();

        // Handle Enter key to submit
        InputTextBox.KeyDown += OnInputTextBoxKeyDown;

        // Handle close request from ViewModel
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Handles data context changes to wire up events.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ViewModels.InputDialogViewModel viewModel)
        {
            viewModel.CloseRequested += OnCloseRequested;
        }
    }

    /// <summary>
    /// Handles close requests from the ViewModel.
    /// </summary>
    /// <param name="result">The dialog result.</param>
    private void OnCloseRequested(InputResult result)
    {
        Close(result);
    }

    /// <summary>
    /// Handles key down events on the input text box.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The key event arguments.</param>
    private void OnInputTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ViewModels.InputDialogViewModel viewModel)
        {
            // Execute the OK command directly
            viewModel.OkCommand.Execute(Unit.Default).Subscribe();
        }
    }

    /// <summary>
    /// Override to handle window closing without a result.
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            // Set cancelled result if no result was provided
            if (DataContext is ViewModels.InputDialogViewModel viewModel && viewModel.Result.IsCancelled)
            {
                // Already has a cancelled result
            }
        }
        base.OnClosing(e);
    }
}
