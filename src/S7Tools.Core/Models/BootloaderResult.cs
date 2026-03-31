namespace S7Tools.Core.Models;

/// <summary>
/// Represents the result of a bootloader dump operation.
/// </summary>
/// <param name="SavedFiles">The absolute paths of the saved dump files.</param>
public record BootloaderResult(IList<string> SavedFiles);
