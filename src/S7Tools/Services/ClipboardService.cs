using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for interacting with the system clipboard.
/// </summary>
public class ClipboardService : IClipboardService
{
    private IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        return clipboard != null ? await clipboard.GetTextAsync() : null;
    }

    /// <inheritdoc/>
    public async Task SetTextAsync(string? text)
    {
        var clipboard = GetClipboard();
        if (clipboard != null && text != null)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}