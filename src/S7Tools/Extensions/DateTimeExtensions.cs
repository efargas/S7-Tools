namespace S7Tools.Extensions;

/// <summary>
/// Extension methods for DateTime handling, ensuring consistent UTC storage and local display.
/// </summary>
/// <remarks>
/// ARCHITECTURAL DECISION: UTC Storage, Local Display
/// =====================================================
/// 
/// All internal storage, business logic, and persistence uses UTC time (DateTime.UtcNow).
/// Conversion to local time happens ONLY at the UI presentation layer.
/// 
/// This prevents timezone-related bugs, ensures data integrity, and makes the system
/// timezone-agnostic for distributed scenarios.
/// 
/// Usage:
/// - Storage/Logic: Always use DateTime.UtcNow
/// - UI Display: Call .ToDisplayTime() to convert UTC to local
/// - User Input: Call .ToStorageTime() to convert local to UTC
/// </remarks>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a UTC DateTime to local time for UI display.
    /// </summary>
    /// <param name="utcTime">The UTC DateTime to convert.</param>
    /// <returns>The DateTime in local timezone.</returns>
    /// <remarks>
    /// This should be called at the UI boundary when displaying timestamps to users.
    /// If the DateTime is already local or unspecified, returns as-is.
    /// </remarks>
    public static DateTime ToDisplayTime(this DateTime utcTime)
    {
        return utcTime.Kind switch
        {
            DateTimeKind.Utc => utcTime.ToLocalTime(),
            DateTimeKind.Local => utcTime,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(utcTime, DateTimeKind.Local),
            _ => utcTime
        };
    }

    /// <summary>
    /// Converts a local DateTime to UTC for storage/business logic.
    /// </summary>
    /// <param name="localTime">The local DateTime to convert.</param>
    /// <returns>The DateTime in UTC.</returns>
    /// <remarks>
    /// This should be called when receiving user input that needs to be stored.
    /// If the DateTime is already UTC or unspecified, handles appropriately.
    /// </remarks>
    public static DateTime ToStorageTime(this DateTime localTime)
    {
        return localTime.Kind switch
        {
            DateTimeKind.Local => localTime.ToUniversalTime(),
            DateTimeKind.Utc => localTime,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(localTime, DateTimeKind.Utc),
            _ => localTime
        };
    }

    /// <summary>
    /// Ensures a DateTime is in UTC, converting if necessary.
    /// </summary>
    /// <param name="dateTime">The DateTime to ensure is UTC.</param>
    /// <returns>A DateTime guaranteed to be in UTC.</returns>
    public static DateTime EnsureUtc(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => dateTime
        };
    }

    /// <summary>
    /// Gets a formatted string representation suitable for logging (always UTC).
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>ISO 8601 formatted string with UTC marker.</returns>
    public static string ToLogString(this DateTime dateTime)
    {
        DateTime utc = dateTime.EnsureUtc();
        return utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// Gets a formatted string representation suitable for UI display (local time).
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <param name="format">Optional format string (default: "yyyy-MM-dd HH:mm:ss").</param>
    /// <returns>Formatted string in local time.</returns>
    public static string ToDisplayString(this DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        DateTime local = dateTime.ToDisplayTime();
        return local.ToString(format);
    }
}
