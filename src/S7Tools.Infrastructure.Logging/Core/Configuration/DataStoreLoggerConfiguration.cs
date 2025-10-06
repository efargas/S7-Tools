using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

/// <summary>
/// Configuration for the DataStore logger provider.
/// </summary>
public sealed class DataStoreLoggerConfiguration
{
    /// <summary>
    /// Gets or sets the minimum log level to capture.
    /// Default is LogLevel.Information.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the event ID to use for filtering logs.
    /// If null, all event IDs are captured.
    /// </summary>
    public int? EventId { get; set; }

    /// <summary>
    /// Gets or sets whether to include scopes in log entries.
    /// Default is true.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture additional properties from structured logging.
    /// Default is true.
    /// </summary>
    public bool CaptureProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets the category name filter pattern.
    /// If null, all categories are captured.
    /// Supports wildcards (* and ?).
    /// </summary>
    public string? CategoryFilter { get; set; }

    /// <summary>
    /// Gets or sets whether to format messages using the provided formatter.
    /// Default is true.
    /// </summary>
    public bool FormatMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum message length before truncation.
    /// If null, messages are not truncated.
    /// Default is 10,000 characters.
    /// </summary>
    public int? MaxMessageLength { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets whether to capture stack traces for exceptions.
    /// Default is true.
    /// </summary>
    public bool CaptureStackTrace { get; set; } = true;

    /// <summary>
    /// Determines if the specified log level is enabled for this configuration.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>True if the log level is enabled; otherwise, false.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= LogLevel;
    }

    /// <summary>
    /// Determines if the specified category matches the category filter.
    /// </summary>
    /// <param name="categoryName">The category name to check.</param>
    /// <returns>True if the category matches the filter; otherwise, false.</returns>
    public bool MatchesCategory(string categoryName)
    {
        if (string.IsNullOrEmpty(CategoryFilter))
            return true;

        if (string.IsNullOrEmpty(categoryName))
            return false;

        return MatchesPattern(categoryName, CategoryFilter);
    }

    private static bool MatchesPattern(string input, string pattern)
    {
        // Simple wildcard matching implementation
        if (pattern == "*")
            return true;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);

        // Convert wildcard pattern to regex-like matching
        var regexPattern = "^" + pattern
            .Replace(".", "\\.")
            .Replace("*", ".*")
            .Replace("?", ".")
            + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}