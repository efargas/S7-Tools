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
    /// Initializes a new instance of the <see cref="ResourceDemoViewModel"/> class.
    /// </summary>
    /// <param name="resourceFactory">The factory to create resource managers.</param>
    public ResourceDemoViewModel(IKeyedFactory<string, IResourceManager> resourceFactory)
    {
        _resourceFactory = resourceFactory;
        Greeting = _resourceFactory.Create("InMemory").GetString("Hello");
    }
}
