# Instructions: S7Tools Project Intelligence

**Last Updated**: Current Session  
**Context Type**: Project-Specific Patterns, Preferences, and Intelligence  

## Project Intelligence Summary

This document captures critical patterns, preferences, and project intelligence discovered through analysis of the S7Tools codebase and documentation. These insights help AI agents work more effectively with the project's specific requirements and established patterns.

## Critical Implementation Paths

### **Service Registration Pattern**

**Pattern**: All new services must be registered in `ServiceCollectionExtensions.cs`  
**Location**: `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`  
**Critical Rule**: NEVER register services directly in `Program.cs` - always use the extension method

```csharp
// CORRECT: Service registration in ServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddSingleton<INewService, NewService>();
    return services;
}

// INCORRECT: Direct registration in Program.cs
services.AddSingleton<INewService, NewService>(); // DON'T DO THIS
```

### **MVVM Implementation Pattern**

**Pattern**: Strict ReactiveUI MVVM with specific conventions  
**ViewModels**: Must inherit from `ReactiveObject` and use `RaiseAndSetIfChanged`  
**Commands**: Always use `ReactiveCommand<TParam, TResult>`  
**Navigation**: Handled through service layer, not direct ViewModel references

```csharp
// CORRECT: ReactiveUI ViewModel pattern
public class MyViewModel : ReactiveObject
{
    private string _property;
    public string Property
    {
        get => _property;
        set => this.RaiseAndSetIfChanged(ref _property, value);
    }
    
    public ReactiveCommand<Unit, Unit> MyCommand { get; }
}
```

### **Logging Integration Pattern**

**Pattern**: Use injected `ILogger<T>` with structured logging  
**Critical Rule**: All services should log operations with structured parameters  
**Performance**: Use LogLevel appropriately to avoid performance impact

```csharp
// CORRECT: Structured logging with parameters
_logger.LogInformation("User performed action: {Action} at {Timestamp}", action, DateTime.Now);

// INCORRECT: String concatenation in logging
_logger.LogInformation($"User performed action: {action} at {DateTime.Now}"); // DON'T DO THIS
```

### **UI Thread Safety Pattern**

**Pattern**: Use `IUIThreadService` for all cross-thread UI operations  
**Critical Rule**: NEVER directly access UI from background threads  
**Implementation**: Always use `ConfigureAwait(false)` in service layer

```csharp
// CORRECT: UI thread marshalling
await _uiThreadService.InvokeAsync(() =>
{
    // UI updates here
});

// CORRECT: ConfigureAwait in services
var result = await SomeAsyncOperation().ConfigureAwait(false);
```

## User Preferences and Workflow

### **Architecture Preferences**

**Clean Architecture**: Strict adherence to dependency inversion  
- Core project has NO external dependencies
- Infrastructure depends only on Core
- Application layer orchestrates between layers
- UI depends on Application and Core only

**Service-Oriented Design**: Everything is a service with an interface  
- All business logic in services, not ViewModels
- ViewModels are thin presentation layer only
- Services are registered in DI container
- Interfaces defined in Core project when possible

### **Code Quality Standards**

**XML Documentation**: Required for ALL public APIs  
**EditorConfig Compliance**: Strict adherence to style rules  
**Nullable Reference Types**: Enabled and enforced  
**Error Handling**: Comprehensive with user-friendly messages

```csharp
/// <summary>
/// Reads a tag value from the PLC asynchronously.
/// </summary>
/// <param name="address">The tag address to read.</param>
/// <returns>A task representing the tag read operation.</returns>
/// <exception cref="ArgumentNullException">Thrown when address is null.</exception>
public async Task<Tag> ReadTagAsync(string address)
{
    ArgumentNullException.ThrowIfNull(address);
    // Implementation...
}
```

### **UI/UX Preferences**

**VSCode Design Language**: Strict adherence to VSCode patterns  
- Activity bar behavior: click selected item toggles visibility
- Color scheme: #007ACC for accents, #858585 for inactive, #FFFFFF for active
- Animations: Smooth transitions, no jarring changes
- Keyboard shortcuts: Standard VSCode shortcuts where applicable

**Responsive Design**: UI must remain responsive  
- Background operations for I/O
- Progress indicators for long operations
- No blocking UI operations
- Graceful error handling with user feedback

## Project-Specific Patterns

### **File Organization Pattern**

**Strict Naming Conventions**:
- Services: `{Feature}Service.cs` with `I{Feature}Service.cs`
- ViewModels: `{View}ViewModel.cs`
- Views: `{Feature}View.axaml` with `{Feature}View.axaml.cs`
- Models: `{Entity}Model.cs` or just `{Entity}.cs`

**Folder Structure Rules**:
- Interfaces always in `Interfaces/` subfolder
- One class per file, file name matches class name
- Namespace matches folder structure exactly
- No deep nesting (max 3 levels)

### **Dependency Injection Lifetime Patterns**

**Singleton**: Services that maintain state or are expensive to create  
- `IActivityBarService`, `ILayoutService`, `IThemeService`
- Logging infrastructure services
- Configuration services

**Transient**: Lightweight services or those that shouldn't maintain state  
- `IDialogService`, `IClipboardService`
- Repository services (when implemented)
- Factory services

```csharp
// Lifetime decision pattern
services.AddSingleton<IExpensiveService, ExpensiveService>();     // Stateful, expensive
services.AddTransient<ILightweightService, LightweightService>(); // Stateless, cheap
```

### **Error Handling Strategy**

**User-Facing Errors**: Always provide actionable error messages  
**Logging Strategy**: Log all errors with context, but don't expose internals to users  
**Recovery Pattern**: Graceful degradation where possible

```csharp
try
{
    await PerformOperation();
}
catch (SpecificException ex)
{
    _logger.LogError(ex, "Operation failed in {Method}", nameof(PerformOperation));
    await _dialogService.ShowErrorAsync("Unable to complete operation", 
        "Please check your connection and try again.");
}
```

## Known Challenges and Solutions

### **Challenge: Cross-Platform Compatibility**

**Issue**: Avalonia behavior can vary across platforms  
**Solution**: Use platform-specific services where needed  
**Pattern**: Abstract platform differences behind interfaces

### **Challenge: Memory Management with Large Datasets**

**Issue**: PLC data and logs can consume significant memory  
**Solution**: Circular buffers and data virtualization  
**Implementation**: Already implemented in logging system, extend to PLC data

### **Challenge: Real-Time Data Updates**

**Issue**: UI must remain responsive during continuous data updates  
**Solution**: Background processing with batched UI updates  
**Pattern**: Use ReactiveUI throttling and background schedulers

```csharp
// Throttle rapid updates
this.WhenAnyValue(x => x.SearchText)
    .Throttle(TimeSpan.FromMilliseconds(300))
    .ObserveOn(RxApp.MainThreadScheduler)
    .Subscribe(text => PerformSearch(text));
```

## Evolution of Project Decisions

### **Initial Architecture Decisions**

**Clean Architecture**: Chosen for maintainability and testability  
**Avalonia UI**: Selected for cross-platform desktop support  
**ReactiveUI**: Chosen for reactive programming and MVVM support  
**Microsoft.Extensions.Logging**: Selected for standardization and extensibility

### **Evolved Patterns**

**Custom Logging Infrastructure**: Added for real-time UI integration  
**Service-Oriented Architecture**: Evolved from simple MVVM to comprehensive service layer  
**VSCode UI Pattern**: Adopted for professional appearance and familiar UX

### **Lessons Learned**

**Documentation Importance**: Extensive documentation prevents confusion and duplicate work  
**Status Tracking**: Multiple tracking systems create confusion - single source of truth needed  
**Architecture Investment**: Strong architecture enables rapid feature development  
**User Experience Focus**: Professional UI significantly improves user acceptance

## Tool Usage Patterns

### **Development Workflow**

**Primary IDE**: Visual Studio 2022 with Avalonia extension  
**Build Process**: `dotnet build` from solution root  
**Testing**: Manual testing currently, xUnit planned  
**Debugging**: Avalonia DevTools (F12) for UI debugging

### **Code Analysis**

**EditorConfig**: Comprehensive style enforcement  
**Static Analysis**: Roslyn analyzers with custom rules  
**Documentation**: XML documentation for all public APIs  
**Performance**: Memory profiling for large datasets

### **Version Control**

**Git Workflow**: Feature branches with PR reviews  
**Commit Messages**: Conventional commit format preferred  
**Documentation**: Update Memory Bank files with significant changes

## AI Agent Specific Guidance

### **Memory Bank Maintenance**

**Update Frequency**: After major features or architectural changes  
**Focus Areas**: activeContext.md and progress.md most important for continuity  
**Task Management**: Always update task files with progress and decisions

### **Code Generation Guidelines**

**Follow Existing Patterns**: Study existing code before generating new code  
**Service Registration**: Always register new services in ServiceCollectionExtensions  
**Error Handling**: Include comprehensive error handling in all generated code  
**Documentation**: Generate XML documentation for all public members

### **Testing Approach**

**Service Layer Focus**: Test business logic in services, not UI  
**Mocking Strategy**: Mock external dependencies, test internal logic  
**Integration Tests**: Test service interactions and data flow  
**UI Testing**: Manual testing for UI, automated for business logic

### **Performance Considerations**

**Memory Usage**: Monitor memory usage with large datasets  
**UI Responsiveness**: Keep UI operations under 100ms  
**Background Processing**: Use background threads for I/O operations  
**Logging Performance**: Be mindful of logging overhead in tight loops

---

**Document Status**: Living document - grows with project experience  
**Next Review**: After major feature implementation or architectural changes  
**Owner**: Development Team with AI Assistance  

**Key Insight**: This project has excellent architecture and implementation quality. Focus on building upon the strong foundation rather than rebuilding existing functionality.