---
title: "Dependency Injection Patterns"
version: "1.0.0"
created: "2025-11-12"
last-updated: "2025-11-12"
status: "current"
tags: ["architecture", "dependency-injection", "service-registration", "di"]
related:
  - docs/architecture/overview.md
  - docs/architecture/clean-architecture.md
  - docs/patterns/system-patterns.md
---

S7Tools uses **Microsoft.Extensions.DependencyInjection** as the dependency injection container, following standard .NET patterns with centralized service registration.

## Core Principles

### 1. Centralized Registration

**All service registration happens in `ServiceCollectionExtensions.cs`** - NEVER register services in `Program.cs`.

```csharp
// src/S7Tools/Extensions/ServiceCollectionExtensions.cs
namespace S7Tools.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS7ToolsServices(this IServiceCollection services)
    {
        services.AddS7ToolsFoundationServices();
        services.AddS7ToolsAdvancedServices();
        services.AddS7ToolsLogging();
        services.AddS7ToolsViewModels();
        return services;
    }

    public static IServiceCollection AddS7ToolsFoundationServices(this IServiceCollection services)
    {
        // Core application services
        services.TryAddSingleton<IClipboardService, ClipboardService>();
        services.TryAddSingleton<IDialogService, DialogService>();
        services.TryAddSingleton<IUIThreadService, UIThreadService>();
        return services;
    }
}
```

### 2. Service Lifetime Guidelines

| Lifetime | When to Use | Example |
|----------|-------------|---------|
| **Singleton** | Stateless services, shared resources, managers | `ILoggingService`, `IActivityBarService` |
| **Transient** | Stateful components, ViewModels with local state | `JobWizardViewModel`, `SerialPortScannerViewModel` |
| **Scoped** | Not used in desktop apps (HTTP request scope) | N/A |

### 3. Interface-Based Design

All services depend on interfaces from the Core layer:

```csharp
// Core/Services/Interfaces/IProfileManager.cs
public interface IProfileManager<T> where T : class, IProfileBase
{
    Task<T> CreateAsync(T profile, CancellationToken ct = default);
    Task<T> UpdateAsync(T profile, CancellationToken ct = default);
    // ... other methods
}

// S7Tools/Services/ProfileService.cs
public class StandardProfileManager<T> : IProfileManager<T>
    where T : class, IProfileBase
{
    // Implementation
}
```

## Registration Patterns

### Foundation Services (Singleton)

Core application services registered as singletons:

```csharp
public static IServiceCollection AddS7ToolsFoundationServices(this IServiceCollection services)
{
    // UI Services
    services.TryAddSingleton<IClipboardService, ClipboardService>();
    services.TryAddSingleton<IDialogService, DialogService>();
    services.TryAddSingleton<IUIThreadService, UIThreadService>();

    // Navigation and Layout
    services.TryAddSingleton<IActivityBarService, ActivityBarService>();
    services.TryAddSingleton<ILayoutService, LayoutService>();

    // Theme and Settings
    services.TryAddSingleton<IThemeService, ThemeService>();
    services.TryAddSingleton<ISettingsService, SettingsService>();

    return services;
}
```

### Advanced Services (Profile Managers)

Profile management services with generic types:

```csharp
public static IServiceCollection AddS7ToolsAdvancedServices(this IServiceCollection services)
{
    // Profile Managers (all singletons for consistency)
    services.TryAddSingleton<ISerialPortProfileService, SerialPortProfileService>();
    services.TryAddSingleton<ISocatProfileService, SocatProfileService>();
    services.TryAddSingleton<IPowerSupplyProfileService, PowerSupplyProfileService>();
    services.TryAddSingleton<IJobProfileService, JobProfileService>();

    // Resource Coordination
    services.TryAddSingleton<IResourceCoordinator, ResourceCoordinator>();

    // Task Management
    services.TryAddSingleton<ITaskScheduler, EnhancedTaskScheduler>();

    return services;
}
```

### ViewModels (Mixed Lifetimes)

ViewModels registered based on state management needs:

```csharp
public static IServiceCollection AddS7ToolsViewModels(this IServiceCollection services)
{
    // Singleton ViewModels (shared state)
    services.TryAddSingleton<MainWindowViewModel>();
    services.TryAddSingleton<ActivityBarViewModel>();

    // Page ViewModels (singleton for navigation consistency)
    services.TryAddSingleton<HomeViewModel>();
    services.TryAddSingleton<SerialPortsSettingsViewModel>();
    services.TryAddSingleton<SocatSettingsViewModel>();

    // Dialog ViewModels (transient for state isolation)
    services.TryAddTransient<JobWizardViewModel>();

    // Control ViewModels (transient for state isolation)
    services.TryAddTransient<SerialPortScannerViewModel>();

    return services;
}
```

## Dependency Injection in Practice

### Constructor Injection (Standard Pattern)

```csharp
public class SerialPortsSettingsViewModel : ReactiveObject
{
    private readonly ISerialPortProfileService _profileService;
    private readonly IUIThreadService _uiThreadService;
    private readonly ILogger<SerialPortsSettingsViewModel> _logger;
    private readonly SerialPortScannerViewModel _portScanner;

    public SerialPortsSettingsViewModel(
        ISerialPortProfileService profileService,
        IUIThreadService uiThreadService,
        ILogger<SerialPortsSettingsViewModel> logger,
        SerialPortScannerViewModel portScanner)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _portScanner = portScanner ?? throw new ArgumentNullException(nameof(portScanner));

        InitializeCommands();
    }
}
```

### Child ViewModel Injection

When ViewModels contain child ViewModels (e.g., reusable controls):

```csharp
// Parent ViewModel (Singleton)
public class MySettingsViewModel : ReactiveObject
{
    private readonly SerialPortScannerViewModel _portScanner;

    public MySettingsViewModel(SerialPortScannerViewModel portScanner)
    {
        _portScanner = portScanner;
    }

    public SerialPortScannerViewModel PortScanner => _portScanner;
}

// DI Registration
services.TryAddSingleton<MySettingsViewModel>();
services.TryAddTransient<SerialPortScannerViewModel>();  // Transient for state isolation
```

### Factory Pattern for Dynamic Creation

When services need runtime configuration:

```csharp
// Factory interface
public interface IServiceFactory<TService>
{
    TService Create(params object[] args);
}

// Factory implementation
public class LogExportServiceFactory : IServiceFactory<ILogExportService>
{
    private readonly IServiceProvider _serviceProvider;

    public LogExportServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILogExportService Create(params object[] args)
    {
        // Create with runtime configuration
        return new LogExportService(
            _serviceProvider.GetRequiredService<ILogger<LogExportService>>(),
            args);
    }
}

// Registration
services.TryAddSingleton<IServiceFactory<ILogExportService>, LogExportServiceFactory>();
```

## Parallel Service Initialization

Services are initialized in parallel during startup for performance:

```csharp
public async Task InitializeS7ToolsServicesAsync(CancellationToken ct = default)
{
    var coordinator = _serviceProvider.GetRequiredService<IResourceCoordinator>();

    // Parallel initialization tasks
    var tasks = new List<Task>
    {
        _settingsService.LoadAsync(ct),
        InitializeProfileServicesAsync(ct),
        InitializeLoggingServicesAsync(ct)
    };

    await Task.WhenAll(tasks);
}
```

## Testing with Dependency Injection

### Unit Testing with Mocks

```csharp
[Fact]
public async Task CreateAsync_ValidProfile_ReturnsProfile()
{
    // Arrange
    var mockLogger = new Mock<ILogger<StandardProfileManager<SerialPortProfile>>>();
    var mockUIThread = new Mock<IUIThreadService>();
    var manager = new StandardProfileManager<SerialPortProfile>(
        mockLogger.Object,
        mockUIThread.Object);

    var profile = new SerialPortProfile { Name = "Test", Device = "/dev/ttyUSB0" };

    // Act
    var result = await manager.CreateAsync(profile);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Name);
}
```

### Integration Testing with Real Container

```csharp
[Fact]
public void ServiceProvider_CanResolveAllServices()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddS7ToolsServices();
    var provider = services.BuildServiceProvider();

    // Act & Assert - Verify all services can be resolved
    var dialogService = provider.GetRequiredService<IDialogService>();
    var profileService = provider.GetRequiredService<ISerialPortProfileService>();

    Assert.NotNull(dialogService);
    Assert.NotNull(profileService);
}
```

## Anti-Patterns to Avoid

### ❌ Service Locator Pattern

```csharp
// BAD - Service locator anti-pattern
public class MyViewModel
{
    public MyViewModel()
    {
        var service = App.ServiceProvider.GetRequiredService<IMyService>();
    }
}

// GOOD - Constructor injection
public class MyViewModel
{
    private readonly IMyService _service;

    public MyViewModel(IMyService service)
    {
        _service = service;
    }
}
```

### ❌ Circular Dependencies

```csharp
// BAD - Circular dependency
public class ServiceA
{
    public ServiceA(ServiceB serviceB) { }
}

public class ServiceB
{
    public ServiceB(ServiceA serviceA) { }
}

// GOOD - Introduce interface and break cycle
public class ServiceA
{
    public ServiceA(IServiceB serviceB) { }
}

public class ServiceB : IServiceB
{
    // ServiceA not injected directly
}
```

### ❌ Registering in Multiple Places

```csharp
// BAD - Services registered in Program.cs
var builder = App.Services;
builder.Services.AddSingleton<IMyService, MyService>();

// GOOD - All registration in ServiceCollectionExtensions
public static IServiceCollection AddS7ToolsServices(...)
{
    services.TryAddSingleton<IMyService, MyService>();
    return services;
}
```

## Best Practices

1. **Use `TryAdd*` methods** - Allows test overrides without conflicts
2. **Register interfaces, not concrete types** - Better testability and flexibility
3. **Keep registration grouped logically** - Foundation, Advanced, ViewModels, etc.
4. **Document service lifetime decisions** - Especially when choosing Transient
5. **Initialize services in parallel** - Use ResourceCoordinator for startup performance
6. **Validate dependencies at startup** - Diagnostic mode can verify all services resolve

## Related Documentation

- [Index](../INDEX.md)
- [_Index](_index.md)
- [Clean Architecture](clean-architecture.md)
- [Overview](overview.md)
- [Development Workflow](../guides/development-workflow.md)
- [System Patterns](../patterns/system-patterns.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
