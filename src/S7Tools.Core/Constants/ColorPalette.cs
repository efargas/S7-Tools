namespace S7Tools.Core.Constants;

/// <summary>
/// Standard color palette for UI elements throughout the application.
/// </summary>
/// <remarks>
/// These RGB color values ensure consistent visual feedback across all UI components.
/// Each color is defined with RGB byte values (0-255) for red, green, and blue channels.
/// </remarks>

public static class ColorPalette
{
    #region Log Level Colors

    /// <summary>
    /// Gray color for Trace level logs.
    /// RGB: (128, 128, 128)
    /// </summary>
    public static class Trace
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 128;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 128;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 128;
    }

    /// <summary>
    /// Blue color for Debug level logs.
    /// RGB: (0, 122, 204)
    /// </summary>
    public static class Debug
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 0;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 122;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 204;
    }

    /// <summary>
    /// Green color for Information level logs.
    /// RGB: (0, 150, 0)
    /// </summary>
    public static class Information
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 0;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 150;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 0;
    }

    /// <summary>
    /// Orange color for Warning level logs and warning messages.
    /// RGB: (255, 165, 0)
    /// </summary>
    public static class Warning
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 255;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 165;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 0;
    }

    /// <summary>
    /// Orange color variant for warning indicators (lighter shade).
    /// RGB: (255, 152, 0)
    /// </summary>
    public static class WarningLight
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 255;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 152;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 0;
    }

    /// <summary>
    /// Crimson color for Error level logs.
    /// RGB: (220, 20, 60)
    /// </summary>
    public static class Error
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 220;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 20;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 60;
    }

    /// <summary>
    /// Red color for error messages.
    /// RGB: (211, 47, 47)
    /// </summary>
    public static class ErrorRed
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 211;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 47;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 47;
    }

    /// <summary>
    /// Dark Red color for Critical level logs.
    /// RGB: (139, 0, 0)
    /// </summary>
    public static class Critical
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 139;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 0;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 0;
    }

    /// <summary>
    /// Dark Gray color for None/disabled states.
    /// RGB: (64, 64, 64)
    /// </summary>
    public static class None
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 64;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 64;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 64;
    }

    /// <summary>
    /// Default Gray color for unknown/fallback states.
    /// RGB: (128, 128, 128)
    /// </summary>
    public static class Default
    {
        /// <summary>Gets the red channel value.</summary>
        public const byte R = 128;
        /// <summary>Gets the green channel value.</summary>
        public const byte G = 128;
        /// <summary>Gets the blue channel value.</summary>
        public const byte B = 128;
    }

    #endregion
}

