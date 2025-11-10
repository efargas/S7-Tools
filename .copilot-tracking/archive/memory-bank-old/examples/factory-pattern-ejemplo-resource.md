# Ejemplo: Factoría de recursos y uso en la UI

## 1. Implementación de una factoría de recursos

```csharp
using Microsoft.Extensions.Logging;
using S7Tools.Core.Factories;
using S7Tools.Core.Resources;

public class ResourceManagerFactory : BaseKeyedFactory<string, IResourceManager>
{
    public ResourceManagerFactory(ILogger logger) : base(logger) { }
    protected override void RegisterFactories()
    {
        RegisterFactory("InMemory", () => new InMemoryResourceManager());
        // Agrega aquí más resource managers según necesidad
    }
}
```

---

## 2. Registro en DI

```csharp
services.TryAddSingleton<IKeyedFactory<string, IResourceManager>, ResourceManagerFactory>();
```

---

## 3. Ejemplo de uso en un ViewModel

```csharp
public class ResourceDemoViewModel : ObservableObject
{
    private readonly IKeyedFactory<string, IResourceManager> _resourceFactory;
    public ResourceDemoViewModel(IKeyedFactory<string, IResourceManager> resourceFactory)
    {
        _resourceFactory = resourceFactory;
    }
    public string Greeting => _resourceFactory.Create("InMemory").GetString("Hello");
}
```

---

## 4. Test AAA para la factoría y el ViewModel

```csharp
using Xunit;
using S7Tools.Core.Factories;
using S7Tools.Core.Resources;
using Microsoft.Extensions.Logging.Abstractions;

public class ResourceManagerFactoryTests
{
    [Fact]
    public void Create_Should_Return_InMemoryResourceManager_Given_InMemory_Key()
    {
        // Arrange
        var factory = new ResourceManagerFactory(NullLogger.Instance);
        // Act
        var manager = factory.Create("InMemory");
        // Assert
        Assert.NotNull(manager);
        Assert.IsType<InMemoryResourceManager>(manager);
    }
}
```

---

## 5. Resumen
- Factoría de recursos lista para registrar y extender.
- ViewModel desacoplado, usa factoría para obtener resource manager.
- Test AAA para factoría y consumidor.