using System;
using S7Tools.Core.Commands;

namespace S7Tools.Core.Commands;

/// <summary>
/// Options for filtering logs.
/// </summary>
public class FilterLogsOptions
{
    /// <summary>
    /// Gets or sets the search text for filtering logs.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the log level to filter.
    /// </summary>
    public string? Level { get; set; }

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
/// Command to filter logs based on options.
/// </summary>
public class FilterLogsCommand : ICommand<CommandResult>
{
    /// <summary>
    /// Gets the filter options.
    /// </summary>
    public FilterLogsOptions Options { get; }

    /// <summary>
    /// Gets the unique identifier for this command type.
    /// </summary>
    public string CommandType => nameof(FilterLogsCommand);

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterLogsCommand"/> class.
    /// </summary>
    /// <param name="options">The filter options.</param>
    public FilterLogsCommand(FilterLogsOptions options) => Options = options;
}
