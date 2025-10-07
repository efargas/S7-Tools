using System;
using S7Tools.Core.Commands;

namespace S7Tools.Core.Commands;

/// <summary>
/// Options for exporting logs.
/// </summary>
public class ExportLogsOptions
{
    /// <summary>
    /// Gets or sets the file name for export.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the export format (txt, json, csv).
    /// </summary>
    public string Format { get; set; } = "txt";

    /// <summary>
    /// Gets or sets the start date for filtering logs.
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for filtering logs.
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}

/// <summary>
/// Command to export logs with given options.
/// </summary>
public class ExportLogsCommand : ICommand<CommandResult<string>>
{
    /// <summary>
    /// Gets the export options.
    /// </summary>
    public ExportLogsOptions Options { get; }

    /// <summary>
    /// Gets the unique identifier for this command type.
    /// </summary>
    public string CommandType => nameof(ExportLogsCommand);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportLogsCommand"/> class.
    /// </summary>
    /// <param name="options">The export options.</param>
    public ExportLogsCommand(ExportLogsOptions options) => Options = options;
}
