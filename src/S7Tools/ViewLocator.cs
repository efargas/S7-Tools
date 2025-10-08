using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using S7Tools.ViewModels;

namespace S7Tools;

public class ViewLocator : IDataTemplate
{

    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        
        // First try to get the type from the current assembly
        var type = Type.GetType(name);
        
        // If not found, try to get it from the same assembly as the ViewModel
        if (type == null)
        {
            var viewModelAssembly = param.GetType().Assembly;
            type = viewModelAssembly.GetType(name);
        }

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
