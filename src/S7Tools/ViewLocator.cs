using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using S7Tools.ViewModels;

namespace S7Tools;

/// <summary>
/// Locates and creates views for view models using naming conventions.
/// Maps ViewModels to Views by replacing namespace and suffix patterns.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Builds a control instance for the specified view model.
    /// </summary>
    /// <param name="param">The view model parameter to build a view for.</param>
    /// <returns>A control instance for the view model, or a TextBlock with error message if the view is not found.</returns>
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        // Map typical ViewModel namespace to Views namespace and replace suffix
        // e.g. S7Tools.ViewModels.SerialPortsSettingsViewModel -> S7Tools.Views.SerialPortsSettingsView
        Type vmType = param.GetType();
        string vmFullName = vmType.FullName ?? string.Empty;

        string name = vmFullName
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        // First try to get the type by full name
        var type = Type.GetType(name);

        // If not found, try to get it from the same assembly as the ViewModel
        if (type == null)
        {
            Assembly viewModelAssembly = vmType.Assembly;
            type = viewModelAssembly.GetType(name);
        }

        // Fallback: search for a type in the ViewModel assembly with the same class name under any namespace
        if (type == null)
        {
            string shortName = vmType.Name.Replace("ViewModel", "View", StringComparison.Ordinal);
            type = vmType.Assembly.GetTypes().FirstOrDefault(t => t.Name == shortName);
        }

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// Determines whether the specified data is a ViewModelBase and can be handled by this locator.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is a ViewModelBase; otherwise, false.</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
