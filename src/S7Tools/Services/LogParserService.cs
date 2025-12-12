using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using S7Tools.Models;

namespace S7Tools.Services;

/// <summary>
/// Service for parsing log files into structured log entries.
/// </summary>
public class LogParserService
{
    // Regex pattern for structured logs: [2025-12-12 15:30:45.123] [Information] [Category] Message
    private static readonly Regex StructuredLogPattern = new(
        @"^\[(?<timestamp>[^\]]+)\]\s*\[(?<level>[^\]]+)\]\s*(?:\[(?<category>[^\]]+)\]\s*)?(?<message>.*)$",
        RegexOptions.Compiled);

    // Regex pattern for simple logs: 2025-12-12 15:30:45 [INF] Message
    private static readonly Regex SimpleLogPattern = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d{3})?)\s*\[(?<level>[^\]]+)\]\s*(?<message>.*)$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses log file content into a list of log entries.
    /// </summary>
    /// <param name="logContent">The raw log file content.</param>
    /// <returns>List of parsed log entries.</returns>
    public List<LogEntry> ParseLogContent(string logContent)
    {
        var entries = new List<LogEntry>();

        if (string.IsNullOrWhiteSpace(logContent))
        {
            return entries;
        }

        string[] lines = logContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            LogEntry? entry = ParseLogLine(line);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <summary>
    /// Parses a single log line into a LogEntry.
    /// </summary>
    /// <param name="line">The log line to parse.</param>
    /// <returns>Parsed LogEntry or null if parsing fails.</returns>
    private LogEntry? ParseLogLine(string line)
    {
        // Try structured log pattern first
        Match match = StructuredLogPattern.Match(line);
        if (match.Success)
        {
            return new LogEntry
            {
                Timestamp = ParseTimestamp(match.Groups["timestamp"].Value),
                Level = NormalizeLogLevel(match.Groups["level"].Value),
                Category = match.Groups["category"].Success ? match.Groups["category"].Value : string.Empty,
                Message = match.Groups["message"].Value.Trim()
            };
        }

        // Try simple log pattern
        match = SimpleLogPattern.Match(line);
        if (match.Success)
        {
            return new LogEntry
            {
                Timestamp = ParseTimestamp(match.Groups["timestamp"].Value),
                Level = NormalizeLogLevel(match.Groups["level"].Value),
                Category = string.Empty,
                Message = match.Groups["message"].Value.Trim()
            };
        }

        // If no pattern matches, treat as plain text message
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "Information",
            Category = string.Empty,
            Message = line.Trim()
        };
    }

    /// <summary>
    /// Parses a timestamp string into a DateTime object.
    /// </summary>
    /// <param name="timestampStr">The timestamp string to parse.</param>
    /// <returns>Parsed DateTime or current time if parsing fails.</returns>
    private DateTime ParseTimestamp(string timestampStr)
    {
        // Try various timestamp formats
        string[] formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss"
        };

        foreach (string format in formats)
        {
            if (DateTime.TryParseExact(timestampStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
        }

        // Fallback to general parsing
        if (DateTime.TryParse(timestampStr, out DateTime fallbackResult))
        {
            return fallbackResult;
        }

        return DateTime.Now;
    }

    /// <summary>
    /// Normalizes log level names to standard format.
    /// </summary>
    /// <param name="level">The log level string to normalize.</param>
    /// <returns>Normalized log level name.</returns>
    private string NormalizeLogLevel(string level)
    {
        return level.ToUpperInvariant() switch
        {
            "TRC" or "TRACE" or "TRCE" => "Trace",
            "DBG" or "DEBUG" or "DEBG" => "Debug",
            "INF" or "INFO" or "INFORMATION" => "Information",
            "WRN" or "WARN" or "WARNING" => "Warning",
            "ERR" or "ERROR" => "Error",
            "CRT" or "CRIT" or "CRITICAL" or "FATAL" => "Critical",
            _ => level
        };
    }
}
