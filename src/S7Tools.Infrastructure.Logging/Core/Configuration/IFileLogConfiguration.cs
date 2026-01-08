using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

/// <summary>
/// Defines configuration for file-based logging.
/// </summary>
public interface IFileLogConfiguration
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets the appropriate file path based on the log category.
    /// </summary>
    /// <param name="categoryName">The category name of the logger.</param>
    /// <returns>The file path where logs for this category should be written.</returns>
    string GetFilePathForCategory(string categoryName);
}
