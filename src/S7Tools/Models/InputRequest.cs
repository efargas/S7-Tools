namespace S7Tools.Models;

/// <summary>
/// Represents a request for user text input.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The dialog message or prompt.</param>
/// <param name="DefaultValue">The default value for the input field.</param>
/// <param name="Placeholder">The placeholder text for the input field.</param>
public record InputRequest(
    string Title,
    string Message,
    string? DefaultValue = null,
    string? Placeholder = null);

/// <summary>
/// Represents the result of a user text input dialog.
/// </summary>
/// <param name="Value">The user-entered text value, or null if cancelled.</param>
/// <param name="IsCancelled">True if the user cancelled the dialog.</param>
public record InputResult(string? Value, bool IsCancelled)
{
    /// <summary>
    /// Creates a successful input result.
    /// </summary>
    /// <param name="value">The user-entered value.</param>
    /// <returns>An InputResult representing success.</returns>
    public static InputResult Success(string value) => new(value, false);

    /// <summary>
    /// Creates a cancelled input result.
    /// </summary>
    /// <returns>An InputResult representing cancellation.</returns>
    public static InputResult Cancelled() => new(null, true);
}
