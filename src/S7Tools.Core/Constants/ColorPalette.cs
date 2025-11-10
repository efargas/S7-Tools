namespace S7Tools.Core.Constants;

/// <summary>
/// Standard color palette for UI elements throughout the application.
/// </summary>
/// <remarks>
/// These RGB color values ensure consistent visual feedback across all UI components.
/// Each color is defined with RGB byte values (0-255) for red, green, and blue channels.
/// </remarks>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class ColorPalette
{
    #region Log Level Colors

    /// <summary>
    /// Gray color for Trace level logs.
    /// RGB: (128, 128, 128)
    /// </summary>
    public static class Trace
    {
        public const byte R = 128;
        public const byte G = 128;
        public const byte B = 128;
    }

    /// <summary>
    /// Blue color for Debug level logs.
    /// RGB: (0, 122, 204)
    /// </summary>
    public static class Debug
    {
        public const byte R = 0;
        public const byte G = 122;
        public const byte B = 204;
    }

    /// <summary>
    /// Green color for Information level logs.
    /// RGB: (0, 150, 0)
    /// </summary>
    public static class Information
    {
        public const byte R = 0;
        public const byte G = 150;
        public const byte B = 0;
    }

    /// <summary>
    /// Orange color for Warning level logs and warning messages.
    /// RGB: (255, 165, 0)
    /// </summary>
    public static class Warning
    {
        public const byte R = 255;
        public const byte G = 165;
        public const byte B = 0;
    }

    /// <summary>
    /// Orange color variant for warning indicators (lighter shade).
    /// RGB: (255, 152, 0)
    /// </summary>
    public static class WarningLight
    {
        public const byte R = 255;
        public const byte G = 152;
        public const byte B = 0;
    }

    /// <summary>
    /// Crimson color for Error level logs.
    /// RGB: (220, 20, 60)
    /// </summary>
    public static class Error
    {
        public const byte R = 220;
        public const byte G = 20;
        public const byte B = 60;
    }

    /// <summary>
    /// Red color for error messages.
    /// RGB: (211, 47, 47)
    /// </summary>
    public static class ErrorRed
    {
        public const byte R = 211;
        public const byte G = 47;
        public const byte B = 47;
    }

    /// <summary>
    /// Dark Red color for Critical level logs.
    /// RGB: (139, 0, 0)
    /// </summary>
    public static class Critical
    {
        public const byte R = 139;
        public const byte G = 0;
        public const byte B = 0;
    }

    /// <summary>
    /// Dark Gray color for None/disabled states.
    /// RGB: (64, 64, 64)
    /// </summary>
    public static class None
    {
        public const byte R = 64;
        public const byte G = 64;
        public const byte B = 64;
    }

    /// <summary>
    /// Default Gray color for unknown/fallback states.
    /// RGB: (128, 128, 128)
    /// </summary>
    public static class Default
    {
        public const byte R = 128;
        public const byte G = 128;
        public const byte B = 128;
    }

    #endregion
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
