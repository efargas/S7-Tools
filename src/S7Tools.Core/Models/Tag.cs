using S7Tools.Core.Models.ValueObjects;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents an immutable tag (data point) from a PLC with type safety and validation.
/// Uses modern C# record syntax for immutability and value semantics.
/// </summary>
/// <param name="Name">The human-readable name of the tag.</param>
/// <param name="Address">The PLC address of the tag.</param>
/// <param name="Value">The current value of the tag.</param>
/// <param name="Description">Optional description of the tag's purpose.</param>
/// <param name="Group">Optional group/category for organizing tags.</param>
/// <param name="IsEnabled">Whether the tag is enabled for reading/writing.</param>
/// <param name="ScanRate">The scan rate in milliseconds (0 = use default).</param>
public sealed record Tag(
    string Name,
    PlcAddress Address,
    TagValue Value,
    string Description = "",
    string Group = "",
    bool IsEnabled = true,
    int ScanRate = 0)
{
    /// <summary>
    /// Gets the unique identifier for this tag (combination of name and address).
    /// </summary>
    public string Id => $"{Name}@{Address}";

    /// <summary>
    /// Gets a value indicating whether this tag has a valid value.
    /// </summary>
    public bool HasValidValue => Value.IsValid;

    /// <summary>
    /// Gets the data type of the tag's value.
    /// </summary>
    public PlcDataType DataType => Value.DataType;

    /// <summary>
    /// Gets the quality of the tag's value.
    /// </summary>
    public TagQuality Quality => Value.Quality;

    /// <summary>
    /// Gets the timestamp of the last value update.
    /// </summary>
    public DateTimeOffset LastUpdated => Value.Timestamp;

    /// <summary>
    /// Creates a new Tag with validation.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <param name="address">The PLC address string.</param>
    /// <param name="value">The initial value.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="group">Optional group.</param>
    /// <param name="isEnabled">Whether the tag is enabled.</param>
    /// <param name="scanRate">The scan rate in milliseconds.</param>
    /// <returns>A Result containing the Tag or validation errors.</returns>
    public static Result<Tag> Create(
        string name,
        string address,
        object? value = null,
        PlcDataType dataType = PlcDataType.Unknown,
        string description = "",
        string group = "",
        bool isEnabled = true,
        int scanRate = 0)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Tag>.Failure("Tag name cannot be null or empty");
        }

        if (scanRate < 0)
        {
            return Result<Tag>.Failure("Scan rate cannot be negative");
        }

        // Create PLC address
        var addressResult = PlcAddress.Create(address);
        if (addressResult.IsFailure)
        {
            return Result<Tag>.Failure($"Invalid address: {addressResult.Error}");
        }

        // Create tag value
        var tagValueResult = dataType == PlcDataType.Unknown
            ? TagValue.Create(value)
            : TagValue.Create(value, dataType);

        if (tagValueResult.IsFailure)
        {
            return Result<Tag>.Failure($"Invalid value: {tagValueResult.Error}");
        }

        var tag = new Tag(
            name.Trim(),
            addressResult.Value,
            tagValueResult.Value,
            description.Trim(),
            group.Trim(),
            isEnabled,
            scanRate);

        return Result<Tag>.Success(tag);
    }

    /// <summary>
    /// Creates a new Tag with an updated value.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    /// <param name="quality">The quality of the new value.</param>
    /// <param name="timestamp">The timestamp (defaults to current time).</param>
    /// <returns>A new Tag instance with the updated value.</returns>
    public Tag WithValue(object? newValue, TagQuality quality = TagQuality.Good, DateTimeOffset? timestamp = null)
    {
        var tagValueResult = TagValue.Create(newValue, DataType, quality);
        var tagValue = tagValueResult.IsSuccess
            ? tagValueResult.Value
            : new TagValue(newValue, PlcDataType.Unknown, TagQuality.Bad, timestamp);

        if (timestamp.HasValue)
        {
            tagValue = tagValue.WithTimestamp(timestamp.Value);
        }

        return this with { Value = tagValue };
    }

    /// <summary>
    /// Creates a new Tag with updated quality.
    /// </summary>
    /// <param name="quality">The new quality.</param>
    /// <returns>A new Tag instance with the updated quality.</returns>
    public Tag WithQuality(TagQuality quality) =>
        this with { Value = Value.WithQuality(quality) };

    /// <summary>
    /// Creates a new Tag with updated enabled state.
    /// </summary>
    /// <param name="enabled">Whether the tag should be enabled.</param>
    /// <returns>A new Tag instance with the updated enabled state.</returns>
    public Tag WithEnabled(bool enabled) => this with { IsEnabled = enabled };

    /// <summary>
    /// Creates a new Tag with updated scan rate.
    /// </summary>
    /// <param name="scanRate">The new scan rate in milliseconds.</param>
    /// <returns>A new Tag instance with the updated scan rate.</returns>
    public Tag WithScanRate(int scanRate) =>
        scanRate >= 0 ? this with { ScanRate = scanRate } : this;

    /// <summary>
    /// Gets the tag value as the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>A Result containing the converted value or an error.</returns>
    public Result<T> GetValue<T>() => Value.GetValue<T>();

    /// <summary>
    /// Gets the tag value as a string representation.
    /// </summary>
    /// <returns>The string representation of the value.</returns>
    public string GetDisplayValue() => Value.AsString().GetValueOrDefault("N/A");

    /// <summary>
    /// Determines if this tag can be written to based on its address type.
    /// </summary>
    public bool IsWritable => Address.AddressType switch
    {
        PlcAddressType.Output => true,
        PlcAddressType.Memory => true,
        PlcAddressType.DataBlockBit or PlcAddressType.DataBlockByte or
        PlcAddressType.DataBlockWord or PlcAddressType.DataBlockDWord => true,
        PlcAddressType.Variable => true,
        _ => false
    };

    /// <summary>
    /// Determines if this tag represents a bit-level value.
    /// </summary>
    public bool IsBitTag => Address.IsBitAddress;

    /// <summary>
    /// Returns a detailed string representation of the tag.
    /// </summary>
    public override string ToString() =>
        $"{Name} [{Address}] = {GetDisplayValue()} ({Quality}) - {(IsEnabled ? "Enabled" : "Disabled")}";
}
