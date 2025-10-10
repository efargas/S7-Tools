using System.Globalization;

namespace S7Tools.Core.Models.ValueObjects;

/// <summary>
/// Represents a type-safe tag value with automatic type conversion and validation.
/// </summary>
public readonly record struct TagValue
{
    /// <summary>
    /// Gets the raw value.
    /// </summary>
    public object? RawValue { get; }

    /// <summary>
    /// Gets the data type of the value.
    /// </summary>
    public PlcDataType DataType { get; }

    /// <summary>
    /// Gets the timestamp when this value was read.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the quality of the value (Good, Bad, Uncertain).
    /// </summary>
    public TagQuality Quality { get; }

    /// <summary>
    /// Initializes a new instance of the TagValue.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="quality">The quality of the value.</param>
    /// <param name="timestamp">The timestamp (defaults to current time).</param>
    public TagValue(object? value, PlcDataType dataType, TagQuality quality = TagQuality.Good, DateTimeOffset? timestamp = null)
    {
        RawValue = value;
        DataType = dataType;
        Quality = quality;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a TagValue with automatic type detection.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="quality">The quality of the value.</param>
    /// <returns>A Result containing the TagValue or an error.</returns>
    public static Result<TagValue> Create(object? value, TagQuality quality = TagQuality.Good)
    {
        var dataType = DetectDataType(value);
        if (dataType == PlcDataType.Unknown)
        {
            return Result<TagValue>.Failure($"Unsupported data type for value: {value?.GetType().Name ?? "null"}");
        }

        return Result<TagValue>.Success(new TagValue(value, dataType, quality));
    }

    /// <summary>
    /// Creates a TagValue with explicit type specification.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="dataType">The explicit data type.</param>
    /// <param name="quality">The quality of the value.</param>
    /// <returns>A Result containing the TagValue or an error.</returns>
    public static Result<TagValue> Create(object? value, PlcDataType dataType, TagQuality quality = TagQuality.Good)
    {
        var validationResult = ValidateValueForType(value, dataType);
        if (validationResult.IsFailure)
        {
            return Result<TagValue>.Failure(validationResult.Error);
        }

        return Result<TagValue>.Success(new TagValue(value, dataType, quality));
    }

    /// <summary>
    /// Gets the value as the specified type with safe conversion.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>A Result containing the converted value or an error.</returns>
    public Result<T> GetValue<T>()
    {
        if (RawValue is null)
        {
            return typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null
                ? Result<T>.Failure("Cannot convert null to non-nullable value type")
                : Result<T>.Success(default(T)!);
        }

        try
        {
            if (RawValue is T directValue)
            {
                return Result<T>.Success(directValue);
            }

            // Handle common conversions
            var converted = Convert.ChangeType(RawValue, typeof(T), CultureInfo.InvariantCulture);
            return Result<T>.Success((T)converted);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"Cannot convert {RawValue} to {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    public Result<bool> AsBool() => GetValue<bool>();

    /// <summary>
    /// Gets the value as a byte.
    /// </summary>
    public Result<byte> AsByte() => GetValue<byte>();

    /// <summary>
    /// Gets the value as a short (16-bit integer).
    /// </summary>
    public Result<short> AsInt16() => GetValue<short>();

    /// <summary>
    /// Gets the value as an integer (32-bit).
    /// </summary>
    public Result<int> AsInt32() => GetValue<int>();

    /// <summary>
    /// Gets the value as a long (64-bit integer).
    /// </summary>
    public Result<long> AsInt64() => GetValue<long>();

    /// <summary>
    /// Gets the value as a float (32-bit).
    /// </summary>
    public Result<float> AsFloat() => GetValue<float>();

    /// <summary>
    /// Gets the value as a double (64-bit).
    /// </summary>
    public Result<double> AsDouble() => GetValue<double>();

    /// <summary>
    /// Gets the value as a string.
    /// </summary>
    public Result<string> AsString() =>
        Result<string>.Success(RawValue?.ToString() ?? string.Empty);

    /// <summary>
    /// Detects the PLC data type from a .NET object.
    /// </summary>
    private static PlcDataType DetectDataType(object? value) => value switch
    {
        null => PlcDataType.Unknown,
        bool => PlcDataType.Bool,
        byte => PlcDataType.Byte,
        sbyte => PlcDataType.SInt,
        short => PlcDataType.Int,
        ushort => PlcDataType.UInt,
        int => PlcDataType.DInt,
        uint => PlcDataType.UDInt,
        long => PlcDataType.LInt,
        ulong => PlcDataType.ULInt,
        float => PlcDataType.Real,
        double => PlcDataType.LReal,
        string => PlcDataType.String,
        char => PlcDataType.Char,
        DateTime => PlcDataType.DateTime,
        DateTimeOffset => PlcDataType.DateTime,
        TimeSpan => PlcDataType.Time,
        _ => PlcDataType.Unknown
    };

    /// <summary>
    /// Validates that a value is compatible with the specified data type.
    /// </summary>
    private static Result ValidateValueForType(object? value, PlcDataType dataType)
    {
        if (value is null)
        {
            return Result.Success(); // Null is valid for all types
        }

        var detectedType = DetectDataType(value);
        if (detectedType == dataType || detectedType == PlcDataType.Unknown)
        {
            return Result.Success();
        }

        // Allow compatible conversions
        var isCompatible = (dataType, detectedType) switch
        {
            (PlcDataType.String, _) => true, // Any type can be converted to string
            (PlcDataType.Real, PlcDataType.Int or PlcDataType.DInt or PlcDataType.Byte) => true,
            (PlcDataType.LReal, PlcDataType.Real or PlcDataType.Int or PlcDataType.DInt or PlcDataType.Byte) => true,
            (PlcDataType.DInt, PlcDataType.Int or PlcDataType.Byte) => true,
            (PlcDataType.Int, PlcDataType.Byte) => true,
            _ => false
        };

        return isCompatible
            ? Result.Success()
            : Result.Failure($"Value of type {detectedType} is not compatible with {dataType}");
    }

    /// <summary>
    /// Creates a new TagValue with updated quality.
    /// </summary>
    /// <param name="newQuality">The new quality.</param>
    /// <returns>A new TagValue with the updated quality.</returns>
    public TagValue WithQuality(TagQuality newQuality) =>
        new(RawValue, DataType, newQuality, Timestamp);

    /// <summary>
    /// Creates a new TagValue with updated timestamp.
    /// </summary>
    /// <param name="newTimestamp">The new timestamp.</param>
    /// <returns>A new TagValue with the updated timestamp.</returns>
    public TagValue WithTimestamp(DateTimeOffset newTimestamp) =>
        new(RawValue, DataType, Quality, newTimestamp);

    /// <summary>
    /// Determines if this value is considered valid (Good quality and not null for value types).
    /// </summary>
    public bool IsValid => Quality == TagQuality.Good &&
                          (RawValue is not null || DataType == PlcDataType.String);

    /// <summary>
    /// Returns a string representation of the tag value.
    /// </summary>
    public override string ToString() =>
        $"{RawValue ?? "null"} ({DataType}, {Quality}, {Timestamp:yyyy-MM-dd HH:mm:ss.fff})";
}

/// <summary>
/// Defines the supported PLC data types.
/// </summary>
public enum PlcDataType
{
    /// <summary>Unknown or unsupported data type.</summary>
    Unknown,
    /// <summary>Boolean (1 bit).</summary>
    Bool,
    /// <summary>Signed 8-bit integer.</summary>
    SInt,
    /// <summary>Unsigned 8-bit integer (Byte).</summary>
    Byte,
    /// <summary>Signed 16-bit integer.</summary>
    Int,
    /// <summary>Unsigned 16-bit integer.</summary>
    UInt,
    /// <summary>Signed 32-bit integer.</summary>
    DInt,
    /// <summary>Unsigned 32-bit integer.</summary>
    UDInt,
    /// <summary>Signed 64-bit integer.</summary>
    LInt,
    /// <summary>Unsigned 64-bit integer.</summary>
    ULInt,
    /// <summary>32-bit floating point.</summary>
    Real,
    /// <summary>64-bit floating point.</summary>
    LReal,
    /// <summary>String data.</summary>
    String,
    /// <summary>Character data.</summary>
    Char,
    /// <summary>Date and time.</summary>
    DateTime,
    /// <summary>Time duration.</summary>
    Time
}

/// <summary>
/// Defines the quality of a tag value.
/// </summary>
public enum TagQuality
{
    /// <summary>Value is good and reliable.</summary>
    Good,
    /// <summary>Value is bad or unreliable.</summary>
    Bad,
    /// <summary>Value quality is uncertain.</summary>
    Uncertain
}
