namespace S7Tools.Core.Constants;

/// <summary>
/// Standard date and time format strings used throughout the application.
/// </summary>
/// <remarks>
/// These constants ensure consistent date/time formatting across all UI components
/// and provide a single source of truth for format string maintenance.
/// </remarks>
public static class DateTimeFormats
{
    /// <summary>
    /// Short date-time format: "yyyy-MM-dd HH:mm"
    /// Example: "2025-11-10 14:30"
    /// </summary>
    public const string ShortDateTime = "yyyy-MM-dd HH:mm";

    /// <summary>
    /// Long date-time format: "yyyy-MM-dd HH:mm:ss"
    /// Example: "2025-11-10 14:30:45"
    /// </summary>
    public const string LongDateTime = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// ISO 8601 date-time format: "yyyy-MM-ddTHH:mm:ssZ"
    /// Example: "2025-11-10T14:30:45Z"
    /// </summary>
    public const string IsoDateTime = "yyyy-MM-ddTHH:mm:ssZ";

    /// <summary>
    /// Date-only format: "yyyy-MM-dd"
    /// Example: "2025-11-10"
    /// </summary>
    public const string DateOnly = "yyyy-MM-dd";

    /// <summary>
    /// Time-only format: "HH:mm:ss"
    /// Example: "14:30:45"
    /// </summary>
    public const string TimeOnly = "HH:mm:ss";

    /// <summary>
    /// Short time format: "HH:mm"
    /// Example: "14:30"
    /// </summary>
    public const string ShortTime = "HH:mm";
}
