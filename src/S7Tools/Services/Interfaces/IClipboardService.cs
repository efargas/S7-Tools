using System.Threading.Tasks;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Defines the contract for a service that interacts with the system clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Gets the text from the clipboard.
    /// </summary>
    /// <returns>The text from the clipboard.</returns>
    Task<string?> GetTextAsync();

    /// <summary>
    /// Sets the text on the clipboard.
    /// </summary>
    /// <param name="text">The text to set.</param>
    Task SetTextAsync(string? text);
}
