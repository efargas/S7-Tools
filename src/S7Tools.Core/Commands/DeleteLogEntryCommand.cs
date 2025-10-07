using S7Tools.Core.Commands;

namespace S7Tools.Core.Commands;

/// <summary>
/// Command to delete a specific log entry by ID.
/// </summary>
public class DeleteLogEntryCommand : ICommand<CommandResult>
{
    /// <summary>
    /// Gets the ID of the log entry to delete.
    /// </summary>
    public string LogEntryId { get; }

    /// <summary>
    /// Gets the unique identifier for this command type.
    /// </summary>
    public string CommandType => nameof(DeleteLogEntryCommand);

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLogEntryCommand"/> class.
    /// </summary>
    /// <param name="logEntryId">The ID of the log entry to delete.</param>
    public DeleteLogEntryCommand(string logEntryId) => LogEntryId = logEntryId;
}
