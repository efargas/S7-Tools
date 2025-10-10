using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace S7Tools.Helpers;

/// <summary>
/// Provides cross-platform utility methods for common operations.
/// </summary>
public static class PlatformHelper
{
    /// <summary>
    /// Opens a directory in the system's file explorer.
    /// </summary>
    /// <param name="path">The directory path to open.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails.</exception>
    public static async Task OpenDirectoryInExplorerAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path), "Path cannot be null or empty");
        }

        await Task.Run(() =>
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("explorer.exe", path);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", path);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", path);
                }
                else
                {
                    throw new PlatformNotSupportedException(
                        "Opening directories in explorer is not supported on this platform");
                }
            }
            catch (Exception ex) when (ex is not PlatformNotSupportedException)
            {
                throw new InvalidOperationException(
                    $"Failed to open directory in explorer: {path}", ex);
            }
        }).ConfigureAwait(false);
    }
}
