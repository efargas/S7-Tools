using System.Collections.Generic;
using System.Threading.Tasks;
using S7Tools.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Models;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for exporting log data to various formats.
/// </summary>
public interface ILogExportService
{
    /// <summary>
    /// Exports logs to the specified format and file path.
    /// </summary>
    /// <param name="logs">The log entries to export.</param>
    /// <param name="format">The export format.</param>
    /// <param name="filePath">The file path to export to. If null, uses default path.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExportLogsAsync(IEnumerable<LogModel> logs, ExportFormat format, string? filePath = null);

    /// <summary>
    /// Gets the default export folder path.
    /// </summary>
    /// <returns>The default export folder path.</returns>
    Task<string> GetDefaultExportFolderAsync();

    /// <summary>
    /// Ensures the export folder exists, creating it if necessary.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureExportFolderExistsAsync();

    /// <summary>
    /// Generates a default file name for the specified format.
    /// </summary>
    /// <param name="format">The export format.</param>
    /// <returns>A default file name with timestamp.</returns>
    string GenerateDefaultFileName(ExportFormat format);
}

/// <summary>
/// Supported export formats for log data.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Plain text format.
    /// </summary>
    Text,

    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// CSV format.
    /// </summary>
    Csv
}
