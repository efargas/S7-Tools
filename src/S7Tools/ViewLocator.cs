using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using S7Tools.ViewModels;

namespace S7Tools;

/// <summary>
/// A data template that locates and creates views for view models.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        // Map typical ViewModel namespace to Views namespace and replace suffix
        // e.g. S7Tools.ViewModels.SerialPortsSettingsViewModel -> S7Tools.Views.SerialPortsSettingsView
        var vmType = param.GetType();
        var vmFullName = vmType.FullName ?? string.Empty;

        var name = vmFullName
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        // First try to get the type by full name
        var type = Type.GetType(name);

        // If not found, try to get it from the same assembly as the ViewModel
        if (type == null)
        {
            var viewModelAssembly = vmType.Assembly;
            type = viewModelAssembly.GetType(name);
        }

        // Fallback: search for a type in the ViewModel assembly with the same class name under any namespace
        if (type == null)
        {
            var shortName = vmType.Name.Replace("ViewModel", "View", StringComparison.Ordinal);
            type = vmType.Assembly.GetTypes().FirstOrDefault(t => t.Name == shortName);
        }

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <inheritdoc />
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}