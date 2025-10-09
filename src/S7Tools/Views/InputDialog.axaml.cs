using Avalonia.Controls;
using Avalonia.Input;
using System.Reactive;

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
}
