using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Microsoft.Extensions.Logging;

namespace S7Tools.Converters;

/// <summary>
/// Provides a set of useful object-related value converters.
/// </summary>
public static class ObjectConverters
{
    /// <summary>
    /// A value converter that returns <see langword="true"/> if the input is not <see langword="null"/>.
    /// </summary>
    public static readonly IValueConverter IsNotNull =
        new FuncValueConverter<object?, bool>(x => x is not null);

    /// <summary>
    /// A value converter that returns <see langword="true"/> if the input is <see langword="null"/>.
    /// </summary>
    public static readonly IValueConverter IsNull =
        new FuncValueConverter<object?, bool>(x => x is null);

    /// <summary>
    /// A value converter that converts LogLevel to appropriate color.
    /// </summary>
    public static readonly IValueConverter LogLevelToColor =
        new FuncValueConverter<LogLevel, Color>(level => level switch
        {
            LogLevel.Trace => Color.FromRgb(128, 128, 128),      // Gray
            LogLevel.Debug => Color.FromRgb(0, 122, 204),        // Blue
            LogLevel.Information => Color.FromRgb(0, 150, 0),    // Green
            LogLevel.Warning => Color.FromRgb(255, 165, 0),      // Orange
            LogLevel.Error => Color.FromRgb(220, 20, 60),        // Crimson
            LogLevel.Critical => Color.FromRgb(139, 0, 0),       // Dark Red
            LogLevel.None => Color.FromRgb(64, 64, 64),          // Dark Gray
            _ => Color.FromRgb(128, 128, 128)                    // Default Gray
        });

    /// <summary>
    /// A value converter that converts LogLevel to appropriate brush.
    /// </summary>
    public static readonly IValueConverter LogLevelToBrush =
        new FuncValueConverter<LogLevel, IBrush>(level => new SolidColorBrush(level switch
        {
            LogLevel.Trace => Color.FromRgb(128, 128, 128),      // Gray
            LogLevel.Debug => Color.FromRgb(0, 122, 204),        // Blue
            LogLevel.Information => Color.FromRgb(0, 150, 0),    // Green
            LogLevel.Warning => Color.FromRgb(255, 165, 0),      // Orange
            LogLevel.Error => Color.FromRgb(220, 20, 60),        // Crimson
            LogLevel.Critical => Color.FromRgb(139, 0, 0),       // Dark Red
            LogLevel.None => Color.FromRgb(64, 64, 64),          // Dark Gray
            _ => Color.FromRgb(128, 128, 128)                    // Default Gray
        }));

    /// <summary>
    /// A value converter that safely converts DateTimeOffset to DateTime for DatePicker binding.
    /// </summary>
    public static readonly IValueConverter DateTimeOffsetToDateTime =
        new DateTimeOffsetToDateTimeConverter();

    /// <summary>
    /// A value converter that safely handles nullable DateTime conversions with error handling.
    /// </summary>
    public static readonly IValueConverter NullableDateTime =
        new NullableDateTimeConverter();

    /// <summary>
    /// A value converter that converts boolean sidebar visibility to GridLength.
    /// Returns 300 pixels when visible, 0 when hidden.
    /// </summary>
    public static readonly IValueConverter SidebarWidthConverter =
        new FuncValueConverter<bool, GridLength>(isVisible =>
            isVisible ? new GridLength(300, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel));
}
