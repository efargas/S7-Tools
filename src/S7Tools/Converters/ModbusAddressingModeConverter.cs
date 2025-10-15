using System;
using System.Globalization;
using Avalonia.Data.Converters;
using S7Tools.Core.Models;

namespace S7Tools.Converters;

/// <summary>
/// Converts ModbusAddressingMode enum to user-friendly display strings and back.
/// </summary>
/// <remarks>
/// This converter handles the two-way binding between ModbusAddressingMode enum values
/// and user-friendly display strings for ComboBox items.
///
/// Forward conversion (enum → string):
/// - Base0 → "Base-0 (0-based, 0-65535)"
/// - Base1 → "Base-1 (1-based, 1-65536)"
///
/// Reverse conversion (string → enum):
/// - Strings containing "Base-0" → Base0
/// - Strings containing "Base-1" → Base1
/// </remarks>
public class ModbusAddressingModeConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ModbusAddressingMode mode)
            return null;

        return mode switch
        {
            ModbusAddressingMode.Base0 => "Base-0 (0-based, 0-65535)",
            ModbusAddressingMode.Base1 => "Base-1 (1-based, 1-65536)",
            _ => null
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str)
            return ModbusAddressingMode.Base0; // Default value

        if (str.Contains("Base-0"))
            return ModbusAddressingMode.Base0;
        if (str.Contains("Base-1"))
            return ModbusAddressingMode.Base1;

        return ModbusAddressingMode.Base0; // Default value
    }
}
