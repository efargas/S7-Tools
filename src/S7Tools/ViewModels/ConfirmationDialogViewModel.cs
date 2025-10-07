using ReactiveUI;
using System.Reactive;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for confirmation dialogs.
/// </summary>
public class ConfirmationDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string Title { get; }
    
    /// <summary>
    /// Gets the dialog message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the command to confirm the dialog.
    /// </summary>
    public ReactiveCommand<Unit, bool> OkCommand { get; }
    
    /// <summary>
    /// Gets the command to cancel the dialog.
    /// </summary>
    public ReactiveCommand<Unit, bool> CancelCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationDialogViewModel"/> class.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    public ConfirmationDialogViewModel(string title, string message)
    {
        Title = title;
        Message = message;

        OkCommand = ReactiveCommand.Create(() => true);
        CancelCommand = ReactiveCommand.Create(() => false);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationDialogViewModel"/> class for design-time.
    /// </summary>
    public ConfirmationDialogViewModel() : this("Confirmation", "Are you sure?")
    {
    }
}