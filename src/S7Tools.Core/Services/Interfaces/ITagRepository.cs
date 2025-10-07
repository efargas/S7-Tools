using S7Tools.Core.Models;
using S7Tools.Core.Models.ValueObjects;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines a contract for reading and writing tags from a data source with modern async patterns.
/// Implements the Repository pattern with Result-based error handling.
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Asynchronously reads a tag from the specified address.
    /// </summary>
    /// <param name="address">The PLC address to read from.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the Tag or an error message.</returns>
    Task<Result<Tag>> ReadTagAsync(PlcAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reads a tag by name from the configured tag list.
    /// </summary>
    /// <param name="tagName">The name of the tag to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the Tag or an error message.</returns>
    Task<Result<Tag>> ReadTagByNameAsync(string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reads multiple tags in a single operation for better performance.
    /// </summary>
    /// <param name="addresses">The collection of addresses to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the collection of Tags or an error message.</returns>
    Task<Result<IReadOnlyCollection<Tag>>> ReadTagsAsync(IEnumerable<PlcAddress> addresses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously writes a value to the specified tag address.
    /// </summary>
    /// <param name="address">The PLC address to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> WriteTagAsync(PlcAddress address, object? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously writes a value to a tag by name.
    /// </summary>
    /// <param name="tagName">The name of the tag to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> WriteTagByNameAsync(string tagName, object? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously writes multiple tags in a single operation for better performance.
    /// </summary>
    /// <param name="tagWrites">The collection of tag addresses and values to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> WriteTagsAsync(IEnumerable<(PlcAddress Address, object? Value)> tagWrites, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously adds a tag to the repository's managed tag list.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> AddTagAsync(Tag tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes a tag from the repository's managed tag list.
    /// </summary>
    /// <param name="tagName">The name of the tag to remove.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> RemoveTagAsync(string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets all managed tags from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the collection of all managed Tags.</returns>
    Task<Result<IReadOnlyCollection<Tag>>> GetAllTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets tags filtered by group.
    /// </summary>
    /// <param name="group">The group name to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the filtered collection of Tags.</returns>
    Task<Result<IReadOnlyCollection<Tag>>> GetTagsByGroupAsync(string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously validates if a tag address is accessible.
    /// </summary>
    /// <param name="address">The address to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating if the address is valid and accessible.</returns>
    Task<Result> ValidateAddressAsync(PlcAddress address, CancellationToken cancellationToken = default);
}
