using S7Tools.Core.Commands;

namespace S7Tools.Core.Commands;

/// <summary>
/// Command to clear all logs from the log store.
/// </summary>
public class ClearLogsCommand : ICommand<CommandResult>
{
    /// <summary>
    /// Gets the unique identifier for this command type.
    /// </summary>
    public string CommandType => nameof(ClearLogsCommand);
}
