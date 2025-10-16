using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using S7Tools.Core.Factories;
using S7Tools.Core.Resources;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel de ejemplo para mostrar recursos localizados usando una factoría de resource managers.
/// </summary>
public partial class ResourceDemoViewModel : ObservableObject
{
    private readonly IKeyedFactory<string, IResourceManager> _resourceFactory;

    [ObservableProperty]
    private string? greeting;

    /// <summary>
    /// Initializes a new instance of the ResourceDemoViewModel class.
    /// </summary>
    /// <param name="resourceFactory">The resource factory.</param>
    public ResourceDemoViewModel(IKeyedFactory<string, IResourceManager> resourceFactory)
    {
        _resourceFactory = resourceFactory;
        Greeting = _resourceFactory.Create("InMemory").GetString("Hello");
    }

    /// <summary>
    /// Initializes a new instance of the ResourceDemoViewModel class for design-time.
    /// </summary>
    public ResourceDemoViewModel() : this(new DesignTimeResourceFactory())
    {
        // Initialize with sample data for better design-time experience
        Greeting = "Hello, Design Time!";
    }
}

/// <summary>
/// Design-time implementation of IKeyedFactory for resource managers.
/// </summary>
internal class DesignTimeResourceFactory : IKeyedFactory<string, IResourceManager>
{
    public IResourceManager Create(string key)
    {
        return new DesignTimeResourceManager();
    }

    public IEnumerable<string> GetAvailableKeys()
    {
        return new[] { "InMemory", "Default" };
    }

    public bool CanCreate(string key)
    {
        return key == "InMemory" || key == "Default";
    }
}

/// <summary>
/// Design-time implementation of IResourceManager for XAML preview.
/// </summary>
internal class DesignTimeResourceManager : IResourceManager
{
    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentCulture;

    public string GetString(string name)
    {
        return name switch
        {
            "Hello" => "Hello, Design Time!",
            "Welcome" => "Welcome to S7Tools",
            "Ready" => "Ready for development",
            _ => $"[{name}]"
        };
    }

    public string GetString(string name, params object[] args)
    {
        return string.Format(GetString(name), args);
    }

    public string GetString(string name, CultureInfo culture)
    {
        return GetString(name);
    }

    public string GetString(string name, CultureInfo culture, params object[] args)
    {
        return string.Format(GetString(name, culture), args);
    }

    public bool HasResource(string name)
    {
        return name is "Hello" or "Welcome" or "Ready";
    }

    public bool HasResource(string name, CultureInfo culture)
    {
        return HasResource(name);
    }

    public IEnumerable<string> GetAvailableKeys()
    {
        return new[] { "Hello", "Welcome", "Ready" };
    }

    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        return new[] { CultureInfo.CurrentCulture };
    }

    public void SetCurrentCulture(CultureInfo culture)
    {
        CurrentCulture = culture;
    }
}
