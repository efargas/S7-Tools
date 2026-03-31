using System.Collections.Concurrent;
using System.Reflection;
using Avalonia.Controls.Templates;
using Dock.Model.Core;
using S7Tools.ViewModels.Base;

namespace S7Tools;

/// <summary>
/// Locates and creates views for view models using naming conventions.
/// Maps ViewModels to Views by replacing namespace and suffix patterns.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Cache for ViewModel to View type mappings to avoid repeated reflection.
    /// Key: ViewModel Type, Value: View Type (or null if not found)
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type?> ViewTypeCache = new();

    /// <summary>
    /// Explicit ViewModel → View mappings for cases where naming convention doesn't match.
    /// </summary>
    private static readonly Dictionary<Type, Type> ExplicitMappings = new()
    {
        [typeof(ViewModels.Jobs.JobsManagementViewModel)] = typeof(Views.Jobs.JobsMainView),
        [typeof(ViewModels.Profiles.ProfilesViewModel)] = typeof(Views.Profiles.ProfilesMainView),
    };
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

        // If the param is an IDockable, wrap its Context (the actual application ViewModel)
        // into a ContentControl so Avalonia resolves the View via this ViewLocator recursively.
        if (param is IDockable dockable && dockable.Context != null && param is not ViewModelBase)
        {
            return new ContentControl { Content = dockable.Context };
        }

        Type vmType = param.GetType();

        // Get cached view type or resolve and cache it
        Type? type = ViewTypeCache.GetOrAdd(vmType, ResolveViewType);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        // Build error message using the computed name
        string viewName = GetExpectedViewName(vmType);
        return new TextBlock { Text = "Not Found: " + viewName };
    }

    /// <summary>
    /// Gets the expected view name for a given ViewModel type based on naming conventions.
    /// </summary>
    /// <param name="vmType">The ViewModel type.</param>
    /// <returns>The expected view name.</returns>
    private static string GetExpectedViewName(Type vmType)
    {
        string vmFullName = vmType.FullName ?? vmType.Name;
        return vmFullName
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolves the View type for a given ViewModel type using naming conventions.
    /// This method is called once per ViewModel type and the result is cached.
    /// </summary>
    /// <param name="vmType">The ViewModel type to resolve a View for.</param>
    /// <returns>The View type if found, otherwise null.</returns>
    private static Type? ResolveViewType(Type vmType)
    {
        // Check explicit mappings first (for ViewModels that don't follow naming conventions)
        if (ExplicitMappings.TryGetValue(vmType, out var explicitType))
        {
            return explicitType;
        }

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

        return type;
    }

    /// <summary>
    /// Determines whether the specified data is a ViewModelBase and can be handled by this locator.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is a ViewModelBase; otherwise, false.</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase || data is IDockable;
    }
}
