using Avalonia.Data.Converters;

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
}
