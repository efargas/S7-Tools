# GitHub Copilot Instructions for S7Tools

This project is a cross-platform desktop application for Siemens S7-1200 PLC communication, built with .NET 8, Avalonia UI, and ReactiveUI following Clean Architecture principles.

## Essential Architecture Patterns

### Clean Architecture Structure
- **Domain** (`S7Tools.Core`): Business entities, interfaces, no external dependencies
- **Application** (`S7Tools`): UI, ViewModels, application services
- **Infrastructure** (`S7Tools.Infrastructure.*`): External concerns (logging, data access)
- **Dependency Flow**: UI → Application → Domain; Infrastructure → Domain

### Unified Profile Management
All profile types (Serial, Socat, PowerSupply, Job) implement `IProfileBase` and use `StandardProfileManager<T>`:

```csharp
// Extending the unified pattern
public class MyProfileService : StandardProfileManager<MyProfile>, IMyProfileService
{
    // Template method pattern - implement abstract members for type-specific behavior
    protected override MyProfile CreateDefaultProfile() => new() { Name = "MyDefault" };
}

// Register in ServiceCollectionExtensions.cs - NEVER in Program.cs
services.TryAddSingleton<IMyProfileService, MyProfileService>();
```

### MVVM with ReactiveUI
All ViewModels inherit from `ReactiveObject` and use reactive patterns:

```csharp
private string _searchText = string.Empty;
public string SearchText
{
    get => _searchText;
    set => this.RaiseAndSetIfChanged(ref _searchText, value);
}

// Commands with validation
var canExecute = this.WhenAnyValue(x => x.IsValid);
SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canExecute);
```

## Critical Threading Patterns

### Semaphore Deadlock Prevention
**NEVER** call semaphore-acquiring methods from within a semaphore lock. Use the Internal Method Pattern:

```csharp
// Public API (acquires semaphore)
public async Task<bool> IsPortInUseAsync(int port)
{
    await _semaphore.WaitAsync().ConfigureAwait(false);
    try { return await IsPortInUseInternalAsync(port); }
    finally { _semaphore.Release(); }
}

// Internal (assumes semaphore held)
private Task<bool> IsPortInUseInternalAsync(int port) { /* no semaphore */ }
```

### UI Thread Management
Use `IUIThreadService` for all UI updates from background threads:

```csharp
await _uiThreadService.InvokeAsync(() => {
    // UI updates here
});
```

## Service Registration Pattern

All services are registered in `ServiceCollectionExtensions.cs` using extension methods:

```csharp
public static IServiceCollection AddS7ToolsFoundationServices(this IServiceCollection services)
{
    services.TryAddSingleton<IMyService, MyService>();
    return services;
}
```

Services are initialized in parallel during startup via `InitializeS7ToolsServicesAsync()`.

## Custom Exception Hierarchy

Use domain-specific exceptions in `S7Tools.Core/Exceptions/`:

```csharp
// Specific exceptions with context
throw new ProfileNotFoundException(profileId);
throw new DuplicateProfileNameException(existingName, attemptedName);

// Catch specific types for targeted handling
catch (ProfileNotFoundException ex) {
    StatusMessage = $"Profile not found: {ex.ProfileId}";
}
```

## Memory Management & Performance

### Circular Buffer Pattern
For collections that grow indefinitely (logs, events):

```csharp
private readonly ConcurrentQueue<T> _items = new();
public void Add(T item) {
    _items.Enqueue(item);
    while (_items.Count > _maxItems) _items.TryDequeue(out _);
}
```

### Reactive Property Optimization
Limit `WhenAnyValue` to max 12 properties, use individual subscriptions for better performance:

```csharp
this.WhenAnyValue(x => x.Property1).Skip(1).Subscribe(_ => OnChanged()).DisposeWith(_disposables);
```

## Dialog and UI Patterns

### Profile Dialog Success Pattern
When Create/Edit/Duplicate dialogs complete:
1. Close dialog only after `SaveAsync` succeeds
2. Refresh collection by replacing entire `ObservableCollection`
3. Reselect relevant item (by Id for edit, by name for create)

```csharp
// Replace collection to trigger DataGrid refresh
var profiles = await _manager.GetAllAsync();
Profiles.Clear();
Profiles.AddRange(profiles);
SelectedProfile = profiles.FirstOrDefault(p => p.Id == targetId);
```

## Build and Development Commands

```bash
# Essential commands
dotnet build src/S7Tools.sln --configuration Debug
dotnet test  # Must maintain 100% passing tests
dotnet run --project src/S7Tools -- --diag  # Diagnostic mode

# Code quality
dotnet format  # Required before commit
```

## Key Files and Locations

- **DI Registration**: `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`
- **Architecture Patterns**: `.copilot-tracking/memory-bank/systemPatterns.md`
- **Project Documentation**: `.copilot-tracking/memory-bank/`
- **Profile Templates**: All profiles in `src/S7Tools.Core/Models/`
- **Custom Exceptions**: `src/S7Tools.Core/Exceptions/`

## Testing Requirements

- Structure: AAA pattern (Arrange-Act-Assert)
- Async tests: Use `async Task`, avoid `.Wait()` or `.Result`
- Maintain 100% test pass rate
- Test exception scenarios with specific exception types

## Memory Bank Integration

Always review Memory Bank files before starting work:
1. `activeContext.md` - Current work focus
2. `systemPatterns.md` - Architecture patterns and rules
3. `progress.md` - Implementation status
4. `tasks/_index.md` - Priority tasks

Update Memory Bank after significant architectural changes or when requested with "update memory bank".

## Anti-Patterns to Avoid

- ❌ Registering services in `Program.cs` (use `ServiceCollectionExtensions.cs`)
- ❌ Nested semaphore acquisitions (use Internal Method Pattern)
- ❌ Large `WhenAnyValue` tuples (max 12 properties)
- ❌ Blocking UI thread with I/O operations
- ❌ Generic exceptions (use domain-specific exceptions)
- ❌ String interpolation in log messages (use structured logging)

This architecture enables rapid development while maintaining clean separation of concerns and excellent testability.
