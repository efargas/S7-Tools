using System.Threading.Tasks;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service interface for file and folder dialog operations.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows an open file dialog and returns the selected file path.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="filters">File type filters (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="initialDirectory">The initial directory to open.</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(string title = "Open File", string? filters = null, string? initialDirectory = null);

    /// <summary>
    /// Shows a save file dialog and returns the selected file path.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="filters">File type filters (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="initialDirectory">The initial directory to open.</param>
    /// <param name="defaultFileName">The default file name.</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveFileDialogAsync(string title = "Save File", string? filters = null, string? initialDirectory = null, string? defaultFileName = null);

    /// <summary>
    /// Shows a folder browser dialog and returns the selected folder path.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="initialDirectory">The initial directory to open.</param>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    Task<string?> ShowFolderBrowserDialogAsync(string title = "Select Folder", string? initialDirectory = null);

    /// <summary>
    /// Shows an open file dialog that allows multiple file selection.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="filters">File type filters (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="initialDirectory">The initial directory to open.</param>
    /// <returns>An array of selected file paths, or null if cancelled.</returns>
    Task<string[]?> ShowOpenMultipleFilesDialogAsync(string title = "Open Files", string? filters = null, string? initialDirectory = null);
}