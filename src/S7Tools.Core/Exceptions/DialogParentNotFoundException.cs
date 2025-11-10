using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when a dialog parent window cannot be resolved or found.
/// This typically occurs when attempting to show a dialog but the main window or parent control is not available.
/// </summary>
public class DialogParentNotFoundException : S7ToolsException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialogParentNotFoundException"/> class with a default message.
    /// </summary>
    public DialogParentNotFoundException()
        : base("Could not get main window for dialog parent")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogParentNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public DialogParentNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogParentNotFoundException"/> class with a custom message
    /// and inner exception.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DialogParentNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
