using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace S7Tools.Services;

/// <summary>
/// Service for exporting log data to various formats with proper error handling and folder management.
/// </summary>
public class LogExportService : ILogExportService
{
    private readonly ILogger<LogExportService> _logger;
    private readonly string _defaultExportPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogExportService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LogExportService(ILogger<LogExportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Set default export path to bin/resources/exports
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _defaultExportPath = Path.Combine(baseDirectory, "resources", "exports");
        
        _logger.LogDebug("LogExportService initialized with default path: {DefaultPath}", _defaultExportPath);
    }

    /// <inheritdoc/>
    public async Task<Result> ExportLogsAsync(IEnumerable<LogModel> logs, ExportFormat format, string? filePath = null)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(logs, nameof(logs));
            
            _logger.LogInformation("Starting log export in {Format} format", format);
            
            // Ensure export folder exists
            await EnsureExportFolderExistsAsync();
            
            // Generate file path if not provided
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = Path.Combine(_defaultExportPath, GenerateDefaultFileName(format));
            }
            
            // Ensure the directory for the file path exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }
            
            // Export based on format
            switch (format)
            {
                case ExportFormat.Text:
                    await ExportAsTextAsync(logs, filePath);
                    break;
                case ExportFormat.Json:
                    await ExportAsJsonAsync(logs, filePath);
                    break;
                case ExportFormat.Csv:
                    await ExportAsCsvAsync(logs, filePath);
                    break;
                default:
                    return Result.Failure($"Unsupported export format: {format}");
            }
            
            _logger.LogInformation("Logs exported successfully to {FilePath}", filePath);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument provided for log export");
            return Result.Failure($"Invalid argument: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when exporting logs to {FilePath}", filePath);
            return Result.Failure($"Access denied: {ex.Message}");
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directory not found when exporting logs to {FilePath}", filePath);
            return Result.Failure($"Directory not found: {ex.Message}");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error when exporting logs to {FilePath}", filePath);
            return Result.Failure($"File operation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when exporting logs to {FilePath}", filePath);
            return Result.Failure($"Export failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Task<string> GetDefaultExportFolderAsync()
    {
        return Task.FromResult(_defaultExportPath);
    }

    /// <inheritdoc/>
    public async Task EnsureExportFolderExistsAsync()
    {
        try
        {
            if (!Directory.Exists(_defaultExportPath))
            {
                Directory.CreateDirectory(_defaultExportPath);
                _logger.LogInformation("Created export folder: {ExportPath}", _defaultExportPath);
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create export folder: {ExportPath}", _defaultExportPath);
            throw;
        }
    }

    /// <inheritdoc/>
    public string GenerateDefaultFileName(ExportFormat format)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var extension = format switch
        {
            ExportFormat.Text => "txt",
            ExportFormat.Json => "json",
            ExportFormat.Csv => "csv",
            _ => "log"
        };
        
        return $"s7tools_logs_{timestamp}.{extension}";
    }

    /// <summary>
    /// Exports logs as plain text format.
    /// </summary>
    /// <param name="logs">The log entries to export.</param>
    /// <param name="filePath">The file path to export to.</param>
    private async Task ExportAsTextAsync(IEnumerable<LogModel> logs, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("S7Tools Log Export");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        foreach (var log in logs.OrderBy(l => l.Timestamp))
        {
            sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{log.Level}] {log.Category}");
            sb.AppendLine($"Message: {log.FormattedMessage}");
            
            if (log.Exception != null)
            {
                sb.AppendLine($"Exception: {log.Exception}");
            }
            
            sb.AppendLine(new string('-', 40));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        _logger.LogDebug("Exported {Count} logs as text to {FilePath}", logs.Count(), filePath);
    }

    /// <summary>
    /// Exports logs as JSON format.
    /// </summary>
    /// <param name="logs">The log entries to export.</param>
    /// <param name="filePath">The file path to export to.</param>
    private async Task ExportAsJsonAsync(IEnumerable<LogModel> logs, string filePath)
    {
        var exportData = new
        {
            ExportInfo = new
            {
                Application = "S7Tools",
                ExportDate = DateTime.Now,
                TotalEntries = logs.Count()
            },
            Logs = logs.OrderBy(l => l.Timestamp).Select(log => new
            {
                Timestamp = log.Timestamp,
                Level = log.Level.ToString(),
                Category = log.Category,
                Message = log.Message,
                FormattedMessage = log.FormattedMessage,
                Exception = log.Exception?.ToString(),
                Properties = log.Properties
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        _logger.LogDebug("Exported {Count} logs as JSON to {FilePath}", logs.Count(), filePath);
    }

    /// <summary>
    /// Exports logs as CSV format.
    /// </summary>
    /// <param name="logs">The log entries to export.</param>
    /// <param name="filePath">The file path to export to.</param>
    private async Task ExportAsCsvAsync(IEnumerable<LogModel> logs, string filePath)
    {
        var sb = new StringBuilder();
        
        // CSV Header
        sb.AppendLine("Timestamp,Level,Category,Message,Exception");

        // CSV Data
        foreach (var log in logs.OrderBy(l => l.Timestamp))
        {
            var timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = log.Level.ToString();
            var category = EscapeCsvField(log.Category);
            var message = EscapeCsvField(log.FormattedMessage);
            var exception = EscapeCsvField(log.Exception?.ToString() ?? "");

            sb.AppendLine($"{timestamp},{level},{category},{message},{exception}");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        _logger.LogDebug("Exported {Count} logs as CSV to {FilePath}", logs.Count(), filePath);
    }

    /// <summary>
    /// Escapes a field for CSV format by wrapping in quotes and escaping internal quotes.
    /// </summary>
    /// <param name="field">The field to escape.</param>
    /// <returns>The escaped field.</returns>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}