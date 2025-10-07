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
}
