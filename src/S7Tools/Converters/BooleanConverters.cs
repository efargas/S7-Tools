using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Provides boolean-related value converters.
/// </summary>
public static class BooleanConverters
{
    /// <summary>
    /// Returns the logical negation of a boolean value.
    /// </summary>
    public static readonly IValueConverter Not =
        new FuncValueConverter<bool, bool>(b => !b);
}
