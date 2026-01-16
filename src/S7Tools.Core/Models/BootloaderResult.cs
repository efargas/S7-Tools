using System.Collections.Generic;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents the result of a bootloader dump operation.
/// </summary>
/// <param name="Data">The raw dumped data segments.</param>
/// <param name="SavedFiles">The absolute paths of the saved dump files.</param>
public record BootloaderResult(IList<byte[]> Data, IList<string> SavedFiles);
