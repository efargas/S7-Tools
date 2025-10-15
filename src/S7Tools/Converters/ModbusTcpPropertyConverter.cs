using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;

namespace S7Tools.Converters;

/// <summary>
/// Converts polymorphic PowerSupplyConfiguration to specific ModbusTcpConfiguration property values for DataGrid binding.
/// </summary>
/// <remarks>
/// This converter handles the polymorphic nature of PowerSupplyConfiguration by safely casting to ModbusTcpConfiguration
/// and extracting specific properties (Host, Port, DeviceId, OnOffCoil) for DataGrid column display.
/// This converter is ONE-WAY ONLY and should be used with IsReadOnly="True" columns.
/// </remarks>
public class ModbusTcpPropertyConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the property name to extract from ModbusTcpConfiguration.
    /// </summary>
    /// <remarks>
    /// Supported values: "Host", "Port", "DeviceId", "OnOffCoil"
    /// </remarks>
    public string PropertyName { get; set; } = string.Empty;

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ModbusTcpConfiguration config)
            return null;

        var propertyName = parameter as string ?? PropertyName;

        return propertyName switch
        {
            "Host" => config.Host,
            "Port" => config.Port,
            "DeviceId" => config.DeviceId,
            "OnOffCoil" => config.OnOffCoil,
            _ => null
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This converter is designed for ONE-WAY binding only (display-only columns)
        // ConvertBack should never be called if columns are properly marked as IsReadOnly="True"
        // However, Avalonia's DataGrid binding system may still evaluate ConvertBack during internal operations

        // Avalonia's binding system expects BindingNotification for graceful error handling
        // Return a sentinel value to indicate conversion is not supported rather than throwing
        return Avalonia.Data.BindingNotification.UnsetValue;
    }
}
