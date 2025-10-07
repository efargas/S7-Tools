using S7Tools.Core.Commands;

namespace S7Tools.Core.Commands;

/// <summary>
/// Options for importing logs.
/// </summary>
public class ImportLogsOptions
{
    /// <summary>
    /// Gets or sets the file path to import from.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Command to import logs from a file.
/// </summary>
public class ImportLogsCommand : ICommand<CommandResult>
{
    /// <summary>
    /// Gets the import options.
    /// </summary>
    public ImportLogsOptions Options { get; }

    /// <summary>
    /// Gets the unique identifier for this command type.
    /// </summary>
    public string CommandType => nameof(ImportLogsCommand);

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportLogsCommand"/> class.
    /// </summary>
    /// <param name="options">The import options.</param>
    public ImportLogsCommand(ImportLogsOptions options) => Options = options;
}
