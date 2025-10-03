using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines a contract for reading and writing tags from a data source.
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Asynchronously reads a tag from the specified address.
    /// </summary>
    /// <param name="address">The address of the tag to read.</param>
    /// <returns>A <see cref="Tag"/> object representing the read data.</returns>
    Task<Tag> ReadTagAsync(string address);
}
