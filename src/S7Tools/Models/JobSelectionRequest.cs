using S7Tools.Core.Models.Jobs;

namespace S7Tools.Models;

/// <summary>
/// Request model for job selection dialog interaction.
/// </summary>
public class JobSelectionRequest
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = "Create Task from Job Profile";

    /// <summary>
    /// Gets or sets an optional message or instructions for the user.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSelectionRequest"/> class.
    /// </summary>
    public JobSelectionRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSelectionRequest"/> class with a custom title.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">Optional message or instructions.</param>
    public JobSelectionRequest(string title, string? message = null)
    {
        Title = title;
        Message = message;
    }
}
