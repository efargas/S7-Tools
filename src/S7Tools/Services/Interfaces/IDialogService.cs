using System.Threading.Tasks;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Defines the contract for a service that displays dialogs.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display in the dialog.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the user confirmed the action.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The error message to display in the dialog.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowErrorAsync(string title, string message);
}