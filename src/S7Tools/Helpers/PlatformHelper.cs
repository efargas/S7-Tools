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
                ProcessStartInfo psi;
                if (OperatingSystem.IsWindows())
                {
                    psi = new ProcessStartInfo(path) { UseShellExecute = true };
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Try xdg-open first
                    var candidates = new List<(string fileName, string args)>
                    {
                        ("xdg-open", path),
                        ("nautilus", path),
                        ("dolphin", path),
                        ("thunar", path),
                        ("pcmanfm", path)
                    };

                    bool opened = false;
                    foreach (var (fileName, args) in candidates)
                    {
                        try
                        {
                            var testPsi = new ProcessStartInfo(fileName, args)
                            {
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using var proc = Process.Start(testPsi);
                            if (proc != null)
                            {
                                // Successfully opened the directory
                                opened = true;
                                return; // Exit immediately after successful open
                            }
                        }
                        catch
                        {
                            // Try next candidate
                            continue;
                        }
                    }

                    if (!opened)
                    {
                        throw new InvalidOperationException("No suitable file manager found to open directory on Linux.");
                    }

                    return; // Already opened, no need to execute code below
                }
                else if (OperatingSystem.IsMacOS())
                {
                    psi = new ProcessStartInfo("open", path) { UseShellExecute = false };
                }
                else
                {
                    throw new PlatformNotSupportedException(
                        "Opening directories in explorer is not supported on this platform");
                }
                Process.Start(psi);
            }
            catch (Exception ex) when (ex is not PlatformNotSupportedException)
            {
                throw new InvalidOperationException(
                    $"Failed to open directory in explorer: {path}", ex);
            }
        }).ConfigureAwait(false);
    }
}
