using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;

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
            LogLevel.Trace => Color.FromRgb(ColorPalette.Trace.R, ColorPalette.Trace.G, ColorPalette.Trace.B),
            LogLevel.Debug => Color.FromRgb(ColorPalette.Debug.R, ColorPalette.Debug.G, ColorPalette.Debug.B),
            LogLevel.Information => Color.FromRgb(ColorPalette.Information.R, ColorPalette.Information.G, ColorPalette.Information.B),
            LogLevel.Warning => Color.FromRgb(ColorPalette.Warning.R, ColorPalette.Warning.G, ColorPalette.Warning.B),
            LogLevel.Error => Color.FromRgb(ColorPalette.Error.R, ColorPalette.Error.G, ColorPalette.Error.B),
            LogLevel.Critical => Color.FromRgb(ColorPalette.Critical.R, ColorPalette.Critical.G, ColorPalette.Critical.B),
            LogLevel.None => Color.FromRgb(ColorPalette.None.R, ColorPalette.None.G, ColorPalette.None.B),
            _ => Color.FromRgb(ColorPalette.Default.R, ColorPalette.Default.G, ColorPalette.Default.B)
        });

    /// <summary>
    /// A value converter that converts LogLevel to appropriate brush.
    /// </summary>
    public static readonly IValueConverter LogLevelToBrush =
        new FuncValueConverter<LogLevel, IBrush>(level => new SolidColorBrush(level switch
        {
            LogLevel.Trace => Color.FromRgb(ColorPalette.Trace.R, ColorPalette.Trace.G, ColorPalette.Trace.B),
            LogLevel.Debug => Color.FromRgb(ColorPalette.Debug.R, ColorPalette.Debug.G, ColorPalette.Debug.B),
            LogLevel.Information => Color.FromRgb(ColorPalette.Information.R, ColorPalette.Information.G, ColorPalette.Information.B),
            LogLevel.Warning => Color.FromRgb(ColorPalette.Warning.R, ColorPalette.Warning.G, ColorPalette.Warning.B),
            LogLevel.Error => Color.FromRgb(ColorPalette.Error.R, ColorPalette.Error.G, ColorPalette.Error.B),
            LogLevel.Critical => Color.FromRgb(ColorPalette.Critical.R, ColorPalette.Critical.G, ColorPalette.Critical.B),
            LogLevel.None => Color.FromRgb(ColorPalette.None.R, ColorPalette.None.G, ColorPalette.None.B),
            _ => Color.FromRgb(ColorPalette.Default.R, ColorPalette.Default.G, ColorPalette.Default.B)
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

    /// <summary>
    /// A value converter that converts debug level to description.
    /// </summary>
    public static readonly IValueConverter DebugLevelToDescription =
        new FuncValueConverter<int, string>(level => level switch
        {
            0 => "(None)",
            1 => "(-d)",
            2 => "(-d -d)",
            3 => "(-d -d -d)",
            _ => $"(-d {level} times)"
        });

    /// <summary>
    /// A value converter that converts count to visibility.
    /// Returns true when count > 0, false otherwise.
    /// </summary>
    public static readonly IValueConverter CountToVisibility =
        new FuncValueConverter<int, bool>(count => count > 0);

    /// <summary>
    /// A value converter that converts LogLevel to appropriate FontAwesome icon string.
    /// </summary>
    public static readonly IValueConverter LogLevelToIcon =
        new FuncValueConverter<LogLevel, string>(level => level switch
        {
            LogLevel.Trace => "fa-solid fa-bug",
            LogLevel.Debug => "fa-solid fa-terminal",
            LogLevel.Information => "fa-solid fa-info-circle",
            LogLevel.Warning => "fa-solid fa-exclamation-triangle",
            LogLevel.Error => "fa-solid fa-exclamation-circle",
            LogLevel.Critical => "fa-solid fa-radiation",
            _ => "fa-solid fa-info-circle"
        });
}
