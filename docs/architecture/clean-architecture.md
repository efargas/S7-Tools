---
title: "Clean Architecture in S7Tools"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["architecture", "clean-architecture", "layers", "dependencies"]
related:
  - "docs/architecture/overview.md"
  - "docs/architecture/mvvm-patterns.md"
  - "docs/architecture/dependency-injection.md"
  - "docs/patterns/system-patterns.md"
supersedes: []
---

# Clean Architecture in S7Tools

## Overview

S7Tools implements **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture) to maintain clear separation of concerns, testability, and long-term maintainability. This pattern ensures that business logic remains isolated from external concerns like UI frameworks, databases, and external services.

## Core Principles

### 1. Dependency Inversion

**Rule**: Dependencies flow inward toward the Domain layer

```
┌─────────────────────────────────────────────┐
│         UI (S7Tools)                        │
│  Views, ViewModels, UI Services            │
│                                             │
│  Dependencies: → Application, Infrastructure│
└─────────────────────────────────────────────┘
              ↓ (uses)
┌─────────────────────────────────────────────┐
│      Application Layer                      │
│  Application Services, Commands             │
│                                             │
│  Dependencies: → Domain only                │
└─────────────────────────────────────────────┘
              ↓ (uses)
┌─────────────────────────────────────────────┐
│       Domain (S7Tools.Core)                 │
│  Entities, Interfaces, Business Rules       │
│                                             │
│  Dependencies: NONE ✅                      │
└─────────────────────────────────────────────┘
              ↑ (implements)
┌─────────────────────────────────────────────┐
│  Infrastructure (S7Tools.Infrastructure.*)  │
│  Logging, Data Access, External Services    │
│                                             │
│  Dependencies: → Domain only                │
└─────────────────────────────────────────────┘
```

**Constitutional Rule** (Article II):

> Domain (S7Tools.Core) has NO external dependencies. Application layer depends only on Domain. Infrastructure layers depend only on Domain. All boundaries strictly enforced via project references.

### 2. Abstraction at Boundaries

**Interfaces in Domain, Implementations in Outer Layers**

The Domain layer defines contracts (interfaces) that outer layers must implement:

```csharp
// S7Tools.Core/Services/Interfaces/IProfileManager.cs
namespace S7Tools.Core.Services.Interfaces;

public interface IProfileManager<T> where T : class, IProfileBase
{
    Task<T> CreateAsync(T profile, CancellationToken ct = default);
    Task<T> UpdateAsync(T profile, CancellationToken ct = default);
    Task<bool> DeleteAsync(int profileId, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
}
```

```csharp
// S7Tools/Services/StandardProfileManager.cs (UI Layer)
namespace S7Tools.Services;

public class StandardProfileManager<T> : IProfileManager<T>
    where T : class, IProfileBase
{
    // Implementation in outer layer
}
```

### 3. Business Logic in Domain

All business rules, validation logic, and domain entities reside in the Core layer:

```csharp
// S7Tools.Core/Models/Jobs/JobProfile.cs
namespace S7Tools.Core.Models.Jobs;

public class JobProfile : IProfileBase
{
    // Business rules
    public bool CanModify() => !IsReadOnly;
    public bool CanDelete() => !IsDefault && !IsReadOnly;

    // Domain validation
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return "Name is required";

        if (SerialProfileId <= 0)
            yield return "Serial profile must be selected";

        // More business rules...
    }
}
```

## Layer Architecture

### Domain Layer (S7Tools.Core)

**Purpose**: Pure business logic and domain models

**Project**: `src/S7Tools.Core/`

**Structure**:
```
S7Tools.Core/
├── Models/
│   ├── Jobs/              # Job domain models
│   ├── Configuration/     # Configuration models
│   └── Profiles/          # Profile base models
├── Services/
│   └── Interfaces/        # Service contracts
├── Commands/              # Command pattern interfaces
├── Validation/            # Validation logic
└── Exceptions/            # Custom exceptions
```

**Responsibilities**:

- Define domain entities and value objects
- Declare service interfaces (contracts)
- Implement business validation rules
- Define custom exceptions for business errors
- NO dependencies on external libraries (except .NET BCL)

**Key Patterns**:

- **Interface Segregation** - Small, focused interfaces
- **Value Objects** - Immutable objects for domain concepts
- **Custom Exceptions** - Semantic error handling

**Example - Domain Entity**:

```csharp
namespace S7Tools.Core.Models.Jobs;

public class JobProfile : IProfileBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Domain-specific properties
    public int SerialProfileId { get; set; }
    public int SocatProfileId { get; set; }
    public int PowerSupplyProfileId { get; set; }
    public int MemoryRegionProfileId { get; set; }

    // Business rules
    public bool IsDefault { get; set; }
    public bool IsReadOnly { get; set; }

    public bool CanModify() => !IsReadOnly;
    public bool CanDelete() => !IsDefault && !IsReadOnly;

    public IProfileBase Clone() => (JobProfile)MemberwiseClone();
}
```

### Application Layer (S7Tools - UI Project)

**Purpose**: Application logic, use cases, and presentation

**Project**: `src/S7Tools/`

**Structure**:
```
S7Tools/
├── ViewModels/
│   ├── Base/             # Base ViewModels
│   ├── Jobs/             # Job ViewModels
│   ├── Profiles/         # Profile ViewModels
│   └── ...
├── Views/                # XAML Views
├── Services/             # Application services
│   ├── ApplicationSettingsService.cs
│   ├── StandardProfileManager.cs
│   └── ...
└── Extensions/
    └── ServiceCollectionExtensions.cs  # DI registration
```

**Responsibilities**:

- Implement ViewModels (MVVM pattern)
- Define XAML Views for presentation
- Implement application services
- Orchestrate domain operations
- Manage UI state and user interactions

**Dependencies**:
- ✅ S7Tools.Core (Domain)
- ✅ S7Tools.Infrastructure.* (Infrastructure)
- ❌ NO dependencies from Domain back to Application

**Example - Application Service**:

```csharp
namespace S7Tools.Services;

public class JobService : IJobService
{
    private readonly IProfileManager<JobProfile> _jobManager;
    private readonly ILogger<JobService> _logger;

    public JobService(
        IProfileManager<JobProfile> jobManager,
        ILogger<JobService> logger)
    {
        _jobManager = jobManager;
        _logger = logger;
    }

    public async Task<OperationResult> ExecuteJobAsync(int jobId)
    {
        // Application orchestration using domain services
        var job = await _jobManager.GetByIdAsync(jobId);

        if (!job.CanModify())
        {
            _logger.LogWarning("Cannot execute read-only job: {JobId}", jobId);
            return OperationResult.Failure("Job is read-only");
        }

        // Orchestrate domain operations
        // ...

        return OperationResult.Success();
    }
}
```

### Infrastructure Layer (S7Tools.Infrastructure.*)

**Purpose**: External concerns and technical implementations

**Projects**:
- `src/S7Tools.Infrastructure.Logging/` - Logging infrastructure
- Future: `S7Tools.Infrastructure.Data/`, `S7Tools.Infrastructure.PLC/`

**Structure**:
```
S7Tools.Infrastructure.Logging/
├── DataStore/
│   └── LogDataStore.cs         # In-memory log storage
└── Providers/
    └── DataStoreLoggerProvider.cs  # Custom log provider
```

**Responsibilities**:

- Implement domain interfaces for external services
- Provide data persistence (file I/O, databases)
- Integrate with external APIs and services
- Implement logging, caching, and monitoring
- Handle infrastructure-specific concerns

**Dependencies**:
- ✅ S7Tools.Core (Domain interfaces only)
- ❌ NO dependencies on Application/UI layers

**Example - Infrastructure Implementation**:

```csharp
namespace S7Tools.Infrastructure.Logging.DataStore;

public class LogDataStore : ILogDataStore
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private readonly int _maxEntries;

    public LogDataStore(IOptions<LoggingOptions> options)
    {
        _maxEntries = options.Value.MaxEntries;
    }

    public void AddLog(LogEntry entry)
    {
        _logs.Enqueue(entry);

        // Circular buffer - remove oldest when full
        while (_logs.Count > _maxEntries)
        {
            _logs.TryDequeue(out _);
        }
    }

    public IReadOnlyList<LogEntry> GetAllLogs()
    {
        return _logs.ToList();
    }
}
```

## Dependency Flow Rules

### ✅ Allowed Dependencies

```csharp
// UI can depend on Domain
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

// UI can depend on Infrastructure
using S7Tools.Infrastructure.Logging;

// Infrastructure can depend on Domain
using S7Tools.Core.Services.Interfaces;
```

### ❌ Forbidden Dependencies

```csharp
// Domain CANNOT depend on UI
// BAD: S7Tools.Core referencing S7Tools
using S7Tools.ViewModels;  // ❌ VIOLATION

// Domain CANNOT depend on Infrastructure
// BAD: S7Tools.Core referencing S7Tools.Infrastructure
using S7Tools.Infrastructure.Logging;  // ❌ VIOLATION

// Infrastructure CANNOT depend on UI
// BAD: S7Tools.Infrastructure.* referencing S7Tools
using S7Tools.Services;  // ❌ VIOLATION
```

### Enforcement Mechanisms

**1. Project References**:

```xml
<!-- S7Tools.Core.csproj - NO external project references -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <!-- NO ProjectReference elements allowed -->
</Project>

<!-- S7Tools.csproj - Can reference Core and Infrastructure -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\S7Tools.Core\S7Tools.Core.csproj" />
    <ProjectReference Include="..\S7Tools.Infrastructure.Logging\S7Tools.Infrastructure.Logging.csproj" />
  </ItemGroup>
</Project>
```

**2. Namespace Analysis**:

Run periodic checks to ensure no forbidden `using` statements:

```bash
# Check Domain for external dependencies
grep -r "using S7Tools\." src/S7Tools.Core/ --exclude-dir=obj --exclude-dir=bin

# Should return NO results (only S7Tools.Core.* allowed)
```

**3. Architecture Tests** (Recommended):

```csharp
[Fact]
public void Domain_Should_Have_No_External_Dependencies()
{
    var assembly = typeof(JobProfile).Assembly;
    var references = assembly.GetReferencedAssemblies();

    var externalRefs = references
        .Where(r => r.Name.StartsWith("S7Tools") && r.Name != "S7Tools.Core")
        .ToList();

    Assert.Empty(externalRefs);
}
```

## Common Patterns

### Pattern 1: Service Interface in Domain

```csharp
// S7Tools.Core/Services/Interfaces/IClipboardService.cs
namespace S7Tools.Core.Services.Interfaces;

public interface IClipboardService
{
    Task SetTextAsync(string text);
    Task<string?> GetTextAsync();
}
```

```csharp
// S7Tools/Services/ClipboardService.cs (UI Layer)
namespace S7Tools.Services;

public class ClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        // Platform-specific implementation
        await Application.Current?.Clipboard.SetTextAsync(text);
    }

    public async Task<string?> GetTextAsync()
    {
        return await Application.Current?.Clipboard.GetTextAsync();
    }
}
```

### Pattern 2: Dependency Injection Registration

All service registration happens in `ServiceCollectionExtensions.cs`:

```csharp
// S7Tools/Extensions/ServiceCollectionExtensions.cs
namespace S7Tools.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS7ToolsServices(this IServiceCollection services)
    {
        // Register domain services (interfaces defined in Core)
        services.TryAddSingleton<IClipboardService, ClipboardService>();
        services.TryAddSingleton<IDialogService, DialogService>();

        // Register application services
        services.TryAddSingleton<IJobService, JobService>();

        return services;
    }
}
```

**NEVER register services in Program.cs** - this violates the centralized DI pattern.

### Pattern 3: ViewModel Using Domain Interfaces

```csharp
// S7Tools/ViewModels/Jobs/JobListViewModel.cs
namespace S7Tools.ViewModels.Jobs;

public class JobListViewModel : ReactiveObject
{
    private readonly IProfileManager<JobProfile> _jobManager;
    private readonly ILogger<JobListViewModel> _logger;

    public JobListViewModel(
        IProfileManager<JobProfile> jobManager,
        ILogger<JobListViewModel> logger)
    {
        _jobManager = jobManager;  // Domain interface
        _logger = logger;
    }

    public async Task LoadJobsAsync()
    {
        try
        {
            var jobs = await _jobManager.GetAllAsync();
            // Update UI...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load jobs");
        }
    }
}
```

## Benefits of Clean Architecture

### 1. Testability

Domain logic can be tested without UI or infrastructure:

```csharp
[Fact]
public void JobProfile_CanDelete_Returns_False_When_IsDefault()
{
    // Arrange
    var job = new JobProfile { IsDefault = true, IsReadOnly = false };

    // Act
    var canDelete = job.CanDelete();

    // Assert
    Assert.False(canDelete);
}
```

### 2. Technology Independence

Change UI framework without affecting business logic:

- Switch from Avalonia to WPF → Domain unchanged
- Replace JSON storage with SQL → Domain unchanged
- Swap logging provider → Domain unchanged

### 3. Maintainability

Clear boundaries make it easy to:

- Locate business rules (always in Domain)
- Find UI logic (always in Application)
- Identify external dependencies (always in Infrastructure)

### 4. Team Scalability

Different teams can work on different layers:

- **Backend Team** → Domain and Infrastructure
- **Frontend Team** → Application (UI)
- **DevOps Team** → Infrastructure (deployment, monitoring)

### 5. Long-Term Evolution

Architecture supports growth:

- Add new features by extending Domain interfaces
- Introduce new UI technologies without rewriting logic
- Swap external services without domain changes

## Anti-Patterns to Avoid

### ❌ Anti-Pattern 1: Business Logic in ViewModels

```csharp
// BAD: Business logic in ViewModel
public class JobViewModel : ReactiveObject
{
    public bool CanDeleteJob()
    {
        // ❌ Business rule in UI layer
        return !IsDefault && !IsReadOnly;
    }
}

// GOOD: Business logic in Domain
public class JobProfile : IProfileBase
{
    public bool CanDelete() => !IsDefault && !IsReadOnly;
}
```

### ❌ Anti-Pattern 2: Domain Depending on Infrastructure

```csharp
// BAD: Domain using infrastructure
namespace S7Tools.Core.Services;

public class JobValidator
{
    private readonly ILogger<JobValidator> _logger;  // ❌ MEL dependency

    public JobValidator(ILogger<JobValidator> logger)
    {
        _logger = logger;
    }
}

// GOOD: Domain defines interface, Infrastructure implements
namespace S7Tools.Core.Services.Interfaces;

public interface IJobValidator
{
    ValidationResult Validate(JobProfile job);
}
```

### ❌ Anti-Pattern 3: Bypassing Abstractions

```csharp
// BAD: ViewModel directly accessing file system
public class SettingsViewModel : ReactiveObject
{
    public async Task SaveSettingsAsync()
    {
        // ❌ Direct file I/O in ViewModel
        await File.WriteAllTextAsync("settings.json", json);
    }
}

// GOOD: Use abstraction
public class SettingsViewModel : ReactiveObject
{
    private readonly ISettingsService _settings;

    public async Task SaveSettingsAsync()
    {
        await _settings.SaveAsync();  // ✅ Through abstraction
    }
}
```

## Validation Checklist

When adding new features, verify:

- [ ] Domain entities have no external dependencies
- [ ] Interfaces defined in Core, implementations in outer layers
- [ ] Business logic resides in Domain layer
- [ ] ViewModels use domain interfaces, not concrete implementations
- [ ] No circular dependencies between layers
- [ ] Services registered in ServiceCollectionExtensions.cs
- [ ] Project references follow dependency flow rules
- [ ] Unit tests can test domain logic in isolation

## Related Documentation

- [Index](../INDEX.md)
- [Project_Architecture_Blueprint](../Project_Architecture_Blueprint.md)
- [_Index](_index.md)
- [Onboarding](../guides/onboarding.md)
- [Testing Guide](../guides/testing-guide.md)
- [Custom Exceptions](../patterns/custom-exceptions.md)
- [Profile Management](../patterns/profile-management.md)
- [Resource Coordination](../patterns/resource-coordination.md)
- [Reusable Controls](../patterns/reusable-controls.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
