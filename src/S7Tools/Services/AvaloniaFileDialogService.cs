using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Avalonia-specific implementation of file dialog service.
/// </summary>
public class AvaloniaFileDialogService : IFileDialogService
{
    private readonly ILogger<AvaloniaFileDialogService> _logger;
    private readonly Func<Window?> _getMainWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaFileDialogService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="getMainWindow">Function to get the main window for dialog parent.</param>
    public AvaloniaFileDialogService(ILogger<AvaloniaFileDialogService> logger, Func<Window?> getMainWindow)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _getMainWindow = getMainWindow ?? throw new ArgumentNullException(nameof(getMainWindow));
    }

    /// <inheritdoc />
    public async Task<string?> ShowOpenFileDialogAsync(string title = "Open File", string? filters = null, string? initialDirectory = null)
    {
        try
        {
            var window = _getMainWindow();
            if (window?.StorageProvider == null)
            {
                _logger.LogWarning("Cannot show file dialog: Main window or storage provider is null");
                return null;
            }

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };

            // Set initial directory if provided
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                options.SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);
            }

            // Parse and set file type filters
            if (!string.IsNullOrEmpty(filters))
            {
                options.FileTypeFilter = ParseFileTypeFilters(filters);
            }

            var result = await window.StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                var selectedFile = result[0].Path.LocalPath;
                _logger.LogDebug("File selected: {FilePath}", selectedFile);
                return selectedFile;
            }

            _logger.LogDebug("File dialog cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing open file dialog");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ShowSaveFileDialogAsync(string title = "Save File", string? filters = null, string? initialDirectory = null, string? defaultFileName = null)
    {
        try
        {
            var window = _getMainWindow();
            if (window?.StorageProvider == null)
            {
                _logger.LogWarning("Cannot show file dialog: Main window or storage provider is null");
                return null;
            }

            var options = new FilePickerSaveOptions
            {
                Title = title,
                DefaultExtension = "txt"
            };

            // Set initial directory if provided
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                options.SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);
            }

            // Set default file name if provided
            if (!string.IsNullOrEmpty(defaultFileName))
            {
                options.SuggestedFileName = defaultFileName;
            }

            // Parse and set file type filters
            if (!string.IsNullOrEmpty(filters))
            {
                options.FileTypeChoices = ParseFileTypeFilters(filters);
            }

            var result = await window.StorageProvider.SaveFilePickerAsync(options);

            if (result != null)
            {
                var selectedFile = result.Path.LocalPath;
                _logger.LogDebug("Save file selected: {FilePath}", selectedFile);
                return selectedFile;
            }

            _logger.LogDebug("Save file dialog cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing save file dialog");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ShowFolderBrowserDialogAsync(string title = "Select Folder", string? initialDirectory = null)
    {
        try
        {
            var window = _getMainWindow();
            if (window?.StorageProvider == null)
            {
                _logger.LogWarning("Cannot show folder dialog: Main window or storage provider is null");
                return null;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };

            // Set initial directory if provided
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                options.SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);
            }

            var result = await window.StorageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                var selectedFolder = result[0].Path.LocalPath;
                _logger.LogDebug("Folder selected: {FolderPath}", selectedFolder);
                return selectedFolder;
            }

            _logger.LogDebug("Folder dialog cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing folder browser dialog");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string[]?> ShowOpenMultipleFilesDialogAsync(string title = "Open Files", string? filters = null, string? initialDirectory = null)
    {
        try
        {
            var window = _getMainWindow();
            if (window?.StorageProvider == null)
            {
                _logger.LogWarning("Cannot show file dialog: Main window or storage provider is null");
                return null;
            }

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true
            };

            // Set initial directory if provided
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                options.SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);
            }

            // Parse and set file type filters
            if (!string.IsNullOrEmpty(filters))
            {
                options.FileTypeFilter = ParseFileTypeFilters(filters);
            }

            var result = await window.StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                var selectedFiles = result.Select(f => f.Path.LocalPath).ToArray();
                _logger.LogDebug("Multiple files selected: {FileCount} files", selectedFiles.Length);
                return selectedFiles;
            }

            _logger.LogDebug("Multiple files dialog cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing open multiple files dialog");
            return null;
        }
    }

    /// <summary>
    /// Parses file type filter strings into Avalonia FilePickerFileType objects.
    /// </summary>
    /// <param name="filters">Filter string in format "Description (*.ext)|*.ext|Description2 (*.ext2)|*.ext2".</param>
    /// <returns>List of FilePickerFileType objects.</returns>
    private static FilePickerFileType[] ParseFileTypeFilters(string filters)
    {
        try
        {
            var filterParts = filters.Split('|');
            var fileTypes = new List<FilePickerFileType>();

            for (int i = 0; i < filterParts.Length; i += 2)
            {
                if (i + 1 < filterParts.Length)
                {
                    var description = filterParts[i];
                    var pattern = filterParts[i + 1];

                    // Extract extensions from pattern (e.g., "*.txt" -> "txt")
                    var extensions = pattern.Split(';')
                        .Select(p => p.Trim().TrimStart('*', '.'))
                        .Where(ext => !string.IsNullOrEmpty(ext) && ext != "*")
                        .ToArray();

                    if (extensions.Length > 0)
                    {
                        fileTypes.Add(new FilePickerFileType(description)
                        {
                            Patterns = extensions.Select(ext => $"*.{ext}").ToArray()
                        });
                    }
                    else
                    {
                        // Handle "All files" case
                        fileTypes.Add(new FilePickerFileType(description)
                        {
                            Patterns = new[] { "*.*" }
                        });
                    }
                }
            }

            return fileTypes.ToArray();
        }
        catch (Exception)
        {
            // Return default "All files" filter if parsing fails
            return new[]
            {
                new FilePickerFileType("All files")
                {
                    Patterns = new[] { "*.*" }
                }
            };
        }
    }
}
