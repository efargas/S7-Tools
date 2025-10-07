using ReactiveUI;
using S7Tools.Models;
using System.Reactive;
using System.Threading.Tasks;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for displaying dialogs using the interaction pattern.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Gets the interaction for showing confirmation dialogs.
    /// </summary>
    Interaction<ConfirmationRequest, bool> ShowConfirmation { get; }
    
    /// <summary>
    /// Gets the interaction for showing error dialogs.
    /// </summary>
    Interaction<ConfirmationRequest, Unit> ShowError { get; }
    
    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the user confirmed, false otherwise.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowErrorAsync(string title, string message);
}