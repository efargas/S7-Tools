# Instructions: S7Tools Development Guidelines

**Last Updated**: January 2025 - New Functionality Phase
**Context Type**: Development patterns, architecture rules, and implementation guidelines

## Critical Development Rules

### **Memory Bank Usage - MANDATORY**

**Key Patterns Established from Code Review Implementation**:
```csharp
// TreatWarningsAsErrors Pattern - Strategic Warning Management
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<WarningsNotAsErrors>CS8618;CS8625;CS8603;CS8604;CS8602;CS1591;CS1998;CS0067</WarningsNotAsErrors>

// Compiled Bindings Pattern - UI Performance Optimization
<DataTemplate DataType="sys:String">
  <!-- Content with compile-time binding resolution -->
</DataTemplate>

// Design-Time Mock Services Pattern
public PlcInputViewModel() // Design-time constructor
{
    ValidatorFactory = new DesignTimeValidatorFactory();
    PrimaryAddress = "DB1.DBW10"; // Sample data
}

// External Code Review Response Protocol Pattern
1. Systematic Validation - Verify each finding against actual codebase
2. Priority Classification - Critical bugs vs quality improvements
3. Risk Assessment - Impact analysis for each proposed change
4. Strategic Implementation - Safe fixes immediately, defer risky changes
5. Task Creation - Document deferred improvements with detailed plans
```

### Appendix: Async Test Best Practices (xUnit)

- Prefer `async Task` test methods when working with asynchronous code.
- Avoid blocking calls in tests (`.Wait()`, `.Result`, `Task.WaitAll(...)`).
- Use `await` and `Task.WhenAll(...)` instead to prevent deadlocks and satisfy analyzers (e.g., xUnit1031).

**Technical Excellence Achieved**:
- Build quality enhanced with strict compilation standards
- UI performance optimized through compiled bindings
- Developer experience improved with design-time mock services
- Repository hygiene maintained with automated backup file prevention
- Complete localization readiness for internationalization
- Elimination of all async void anti-patterns with proper ReactiveUI Observable patterns

### **Recent Session Accomplishments**

### **🎉 PHASE 2 COMPLETE: Code Review Findings Implementation (2025-10-16)**

**Phase 1 & 2 Successfully Completed**
- ✅ **All critical issues resolved** - Deadlocks, fire-and-forget patterns, exception swallowing
- ✅ **Architectural improvements complete** - Localization, Clean Architecture compliance
- ✅ **ReactiveUI patterns implemented** - Async void eliminated, proper disposal patterns
- ✅ **Code duplication removed** - Unified command pattern with parameter-based approach

**Key Patterns Established**:
```csharp
// ReactiveUI Pattern - Replace async void with Observable.Timer
this.WhenAnyValue(x => x.PropertyToWatch)
    .Where(value => !string.IsNullOrEmpty(value))
    .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(3)))
    .ObserveOn(RxApp.MainThreadScheduler)
    .Subscribe(_ => { /* Action */ })
    .DisposeWith(_disposables);

// Unified Command Pattern - Single command with parameter
public ReactiveCommand<TParameter, Unit> UnifiedCommand { get; }
// Individual commands delegate to unified command
SpecificCommand = ReactiveCommand.Create(() => UnifiedCommand.Execute(parameter).Subscribe());
```

**IDisposable Pattern** (Required for ViewModels with subscriptions):
```csharp
public class MyViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables?.Dispose();
        }
    }
}
```

**Architecture Decision Documented** (User Clarified):
- Serial configuration done **separately** via Serial Ports Settings
- Socat uses **already-configured** devices (NO stty calls in socat start)
- Separation of concerns: Serial service handles stty, Socat handles TCP bridging

**Pattern Established: Bug Fix Task Creation**
```markdown
# Task Structure for Bug Fixes
1. **Original Request**: Capture exact user words and context
2. **Thought Process**: Root cause analysis and solution strategy
3. **Implementation Plan**: 3-phase approach (Critical → UI → Verification)
4. **Progress Tracking**: Subtasks table with status + detailed progress log
5. **Success Criteria**: Phase-specific validation requirements
```

### **🎉 MAJOR BREAKTHROUGH: External Code Review Validation & Bug Fixes (2025-10-09)**

**Code Review Validation Complete**
- ✅ **Validated all external code review findings** against S7Tools codebase
- ✅ **Identified and fixed 4 critical bugs** affecting code quality and maintainability
- ✅ **Applied safe, high-impact improvements** while deferring risky architectural changes

**Critical Bug Fixes Applied**:
1. ✅ **Backup File Cleanup** - Removed `.bak` files cluttering the codebase
2. ✅ **Spanish Comments Translation** - Translated all Spanish comments to English for team consistency
3. ✅ **Exception Swallowing Fix** - Enhanced exception handling in ServiceCollectionExtensions with proper ILogger usage
4. ✅ **UI Notification Optimization** - Fixed inefficient Reset notifications in LogDataStore, optimized to Add notifications only

**High-Impact Improvements Implemented**:
- ✅ **ILogger Integration in Program.cs** - Enhanced diagnostic mode with dual logging (console + ILogger)
- ✅ **ConfigureAwait Standardization** - Applied `.ConfigureAwait(false)` pattern in service layer methods
- ✅ **Record Type Conversion** - Converted LogModel to record type for immutability and value equality
- ✅ **Constants for Magic Strings** - Added ExportFormats constants class to eliminate magic strings

**Pattern Established: Code Review Response Protocol**
```csharp
// Code Review Validation Process (Required for all external reviews)
1. **Systematic Validation**: Verify each finding against actual codebase
2. **Priority Classification**: Critical bugs vs. quality improvements
3. **Risk Assessment**: Impact analysis for each proposed change
4. **Strategic Implementation**: Apply safe fixes immediately, defer risky changes
5. **Task Creation**: Document deferred improvements with detailed implementation plans
6. **Blocking Strategy**: Prevent interference with high-priority feature development
```

**Quality Assurance Results**:
- ✅ **Build Verification**: Clean compilation with only warnings (no errors)
- ✅ **Application Startup**: Verified successful application launch
- ✅ **Architecture Compliance**: All changes maintain Clean Architecture principles
- ✅ **Memory Bank Updated**: All patterns documented for future reference

**Technical Excellence Achieved**:
- ✅ **Domain-Driven Design** - Rich domain models with business logic encapsulation
- ✅ **SOLID Principles** - Single responsibility, dependency inversion, template method pattern
- ✅ **Clean Architecture** - Interfaces in Core, implementations properly layered
- ✅ **ReactiveUI Compliance** - Individual property subscriptions, proper disposal patterns
- ✅ **Thread Safety** - UI thread marshaling using established IUIThreadService patterns

**Pattern Established: Unified Profile Management Architecture**
```csharp
// Core Interface Pattern (Required for all profile types)
public interface IProfileBase
{
    int Id { get; set; }
    string Name { get; set; }
    string Description { get; set; }
    string Options { get; set; }        // Command options/flags
    string Flags { get; set; }          // Additional flags
    DateTime CreatedAt { get; set; }    // Creation timestamp
    DateTime ModifiedAt { get; set; }   // Last modification
    bool IsDefault { get; set; }
    bool IsReadOnly { get; set; }

    // Business logic methods
    bool CanModify();
    bool CanDelete();
    string GetSummary();
    IProfileBase Clone();
}

// Generic Profile Manager Pattern (Required for all profile services)
public interface IProfileManager<T> where T : class, IProfileBase
{
    Task<T> CreateAsync(T profile, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T profile, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int profileId, CancellationToken cancellationToken = default);
    Task<T> DuplicateAsync(int sourceProfileId, string newName, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(int profileId, CancellationToken cancellationToken = default);
    Task<bool> SetDefaultAsync(int profileId, CancellationToken cancellationToken = default);
}

// Base ViewModel Pattern (Required for all profile management ViewModels)
public abstract class ProfileManagementViewModelBase<TProfile> : ViewModelBase, IDisposable
    where TProfile : class, IProfileBase
{
    // Template method pattern for customization points:
    protected abstract Task<IEnumerable<TProfile>> LoadProfilesAsync();
    protected abstract string GetDefaultProfileName();
    protected abstract TProfile CreateDefaultProfile();
    protected abstract Task<bool> ShowProfileEditDialogAsync(TProfile profile);
    protected abstract Task<string?> ShowProfileNameInputDialogAsync(string currentName);
}
```

**Implementation Guidelines for Profile Management**:
1. **All profiles must implement IProfileBase** - Ensures consistent metadata and behavior
2. **All profile services must implement IProfileManager<T>** - Provides unified CRUD operations
3. **All profile ViewModels should inherit ProfileManagementViewModelBase<T>** - Ensures consistent UI behavior
4. **Dialog-Only Operations** - Create, Edit, Duplicate all use dialogs, no inline input fields
5. **Consistent Button Order** - Create - Edit - Duplicate - Default - Delete - Refresh
6. **Enhanced DataGrid** - ID column first, complete metadata columns (Options, Flags, Created, Modified)
7. **Thread-Safe Operations** - Always use IUIThreadService for UI thread marshaling
- **progress.md** - Current implementation status and task progress tracking
- **activeContext.md** - Current session context and immediate next steps for next agent

**Update Pattern for Every Task/Phase**:
1. **Before Implementation**: Update activeContext.md with current task and next steps
2. **During Implementation**: Update progress.md with actual progress and issues
3. **After Implementation**: Update instructions.md with new patterns learned
4. **NEVER mark complete without user validation** - This is the fundamental rule

### **User Validation Rule - CRITICAL**

- **Implementation ≠ Working functionality**
- **User feedback is the ONLY source of truth for completion**
- Always use "In Progress", "Blocked", or "Not Started" until user confirms
- Document user feedback verbatim in progress.md
- Investigate discrepancies between implementation and user experience

### **UI Standards - MANDATORY**

**CRUD Button Order** (All modules MUST follow):
Create - Edit - Duplicate - Default - Delete - Refresh

**DataGrid Layout** (Enhanced metadata columns):
- ID column FIRST
- Complete metadata: Name, Description, Options, Flags, Created, Modified, Default, ReadOnly

**Dialog-Only Operations**:
- Create: Opens dialog with default values
- Edit: Opens dialog with existing data
- Duplicate: Name input → direct list addition

### **Validation Patterns - REQUIRED**

**Name Uniqueness**:
```csharp
// Real-time validation in all dialogs
public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null)
{
    return !_profiles.Any(p => p.Id != excludeId &&
                          string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
}
```

**ID Assignment**:
```csharp
// Find first available ID starting from 1
public async Task<int> GetNextAvailableIdAsync()
{
    var existingIds = _profiles.Select(p => p.Id).OrderBy(id => id).ToList();
    for (int i = 1; i <= existingIds.Count + 1; i++)
    {
        if (!existingIds.Contains(i)) return i;
    }
    return existingIds.Count + 1;
}
```

### **Thread Safety - CRITICAL**

**UI Thread Marshaling** (Always use IUIThreadService):
```csharp
// REQUIRED pattern for collection updates
private readonly IUIThreadService _uiThreadService;

protected async Task RefreshProfilesAsync()
{
    var profiles = await LoadProfilesAsync();
    await _uiThreadService.InvokeOnUIThreadAsync(() =>
    {
        _profiles.Clear();
        foreach (var profile in profiles)
        {
            _profiles.Add(profile);
        }
    });
}
```

## Application Architecture - IMMUTABLE CORE

### **Project Structure - DO NOT CHANGE**
```
S7Tools/
├── S7Tools.Core/                    # Domain models and interfaces (dependency-free)
│   ├── Services/Interfaces/         # Service contracts
│   ├── Models/                      # Domain models and value objects
│   └── Commands/                    # Command pattern implementations
├── S7Tools.Infrastructure.Logging/  # Logging infrastructure
└── S7Tools/                         # Main application
    ├── Services/                    # Service implementations
    ├── ViewModels/                  # MVVM ViewModels
    ├── Views/                       # XAML Views
    ├── Models/                      # Application models
    └── Extensions/                  # Service registration
```

### **Clean Architecture Rules - MANDATORY**
1. **Dependencies flow inward** - Core has no external dependencies
2. **Infrastructure depends only on Core**
3. **Application depends on Core and Infrastructure**
4. **No circular dependencies** between layers

### **Service Registration Pattern - REQUIRED**
```csharp
// In ServiceCollectionExtensions.cs - ALWAYS use this pattern
services.AddSingleton<IServiceInterface, ServiceImplementation>();

// Constructor injection - ALWAYS use this pattern
public class MyService
{
    private readonly IDependency _dependency;

    public MyService(IDependency dependency)
    {
        _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
    }
}
```

## MVVM Implementation - ESTABLISHED PATTERNS

### **ReactiveUI Standards - MANDATORY**
```csharp
public class ExampleViewModel : ReactiveObject, IDisposable
{
    private readonly IService _service;
    private readonly CompositeDisposable _disposables = new();

    public ExampleViewModel(IService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        InitializeCommands();
    }

    public ReactiveCommand<Unit, Unit> ExampleCommand { get; private set; }

    private void InitializeCommands()
    {
        ExampleCommand = ReactiveCommand.CreateFromTask(ExecuteExampleAsync);
        ExampleCommand.ThrownExceptions
            .Subscribe(ex => /* handle exceptions */)
            .DisposeWith(_disposables);
    }

    public void Dispose() => _disposables?.Dispose();
}
```

### **View-ViewModel Binding - REQUIRED PATTERN**
- **Views contain ONLY UI logic and data binding**
- **ViewModels handle ALL presentation logic**
- **NO direct View instantiation in ViewModels**
- **Use ReactiveUI Interactions for dialogs**
- **All ViewModels created through DI container**

## UI Framework Rules - ESTABLISHED

### **VSCode-Style Interface - DO NOT CHANGE**
- **Activity Bar** - Left sidebar with icon navigation
- **Collapsible Sidebar** - Dynamic content switching
- **Main Content Area** - Primary workspace
- **Bottom Panel** - Resizable with tabs
- **Menu System** - Complete with keyboard shortcuts

### **Avalonia UI Patterns - ESTABLISHED**
- **2-space indentation** for XAML files
- **PascalCase** for elements and properties
- **Data binding** over code-behind manipulation
- **Styles.axaml** for consistent styling
- **ViewLocator** for View-ViewModel resolution

## Logging System - IMMUTABLE

### **Structured Logging - REQUIRED**
```csharp
// Inject logger - ALWAYS use this pattern
private readonly ILogger<MyClass> _logger;

// Structured logging - ALWAYS use this pattern
_logger.LogInformation("User performed action: {Action} at {Timestamp}", action, DateTime.Now);
_logger.LogError(exception, "Error occurred in {Method}", nameof(MyMethod));
```

### **Log Infrastructure - DO NOT MODIFY**
- **S7Tools.Infrastructure.Logging** - Complete circular buffer system
- **DataStore provider** - Custom logging provider
- **Real-time notifications** - INotifyPropertyChanged integration
- **Thread-safe operations** - Concurrent access patterns

## Design Patterns - ESTABLISHED

### **Command Pattern - IMPLEMENTED**
- **BaseCommandHandler** - Generic command handling
- **ICommandDispatcher** - Command dispatching
- **Async support** - All commands support async operations
- **Error handling** - Comprehensive exception handling

### **Factory Pattern - IMPLEMENTED**
- **IViewModelFactory** - ViewModel creation
- **EnhancedViewModelFactory** - Advanced factory with caching
- **Keyed factories** - Multiple factory types
- **DI integration** - Factory uses dependency injection

### **Resource Pattern - IMPLEMENTED**
- **IResourceManager** - Localization support
- **Multi-culture** - Support for multiple languages
- **Strongly-typed access** - Type-safe resource access
- **Fallback mechanisms** - Default resource handling

### **Validation Pattern - IMPLEMENTED**
- **IValidator<T>** - Generic validation interface
- **ValidationService** - Centralized validation
- **Rule-based validation** - Flexible validation rules
- **Async validation** - Support for async validation

## Code Modernization Standards - ESTABLISHED (January 2025)

### **File-Scoped Namespaces - REQUIRED** (C# 10+)

**Modern Namespace Pattern** (99.5% of codebase):
```csharp
using System;
using System.Collections.Generic;

namespace S7Tools.Services;

/// <summary>
/// Service implementation
/// </summary>
public class MyService
{
    // Implementation
}
```

**Legacy Pattern** (Only in auto-generated files):
```csharp
namespace S7Tools.Services
{
    public class MyService
    {
        // Implementation
    }
}
```

**Benefits**:
- Reduced indentation level
- Modern C# 10+ conventions
- Cleaner, more readable code
- Consistent with .NET ecosystem standards

### **Performance Profiling Infrastructure - ESTABLISHED**

**BenchmarkDotNet Setup**:
```bash
# Run all benchmarks
cd benchmarks/S7Tools.Benchmarks
dotnet run -c Release

# Run specific benchmark
dotnet run -c Release --filter "*ProfileCrud*"
dotnet run -c Release --filter "*Logging*"
```

**Benchmark Pattern**:
```csharp
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MyBenchmarks
{
    [GlobalSetup]
    public void Setup() { }

    [GlobalCleanup]
    public void Cleanup() { }

    [Benchmark]
    public void MyOperation() { }
}
```

**Available Benchmarks**:
1. **ProfileCrudBenchmarks** - Profile CRUD operations performance
2. **LoggingPerformanceBenchmarks** - Logging system performance

**Usage Guidelines**:
- Run benchmarks in Release mode only
- Close other applications to reduce noise
- Run multiple iterations for statistical significance
- Document baseline metrics for comparison
- Track performance trends over time

## Code Quality Standards - MANDATORY

### **Naming Conventions - REQUIRED**
- **Services**: `{FeatureName}Service.cs` with `I{FeatureName}Service.cs`
- **ViewModels**: `{ViewName}ViewModel.cs`
- **Views**: `{ViewName}.axaml` with `{ViewName}.axaml.cs`
- **Models**: `{EntityName}.cs` or `{EntityName}Model.cs`
- **Interfaces**: Prefixed with 'I' (e.g., `IActivityBarService`)
- **Private Fields**: Camel case with underscore prefix (e.g., `_fieldName`)

### **Documentation Standards - REQUIRED**
- **All public APIs** must have XML documentation
- **Service interfaces** must document all methods
- **Complex logic** must have inline comments
- **Architecture decisions** must be documented

### **Error Handling - MANDATORY**
- **All service methods** must have try-catch blocks
- **User-friendly error messages** for UI operations
- **Structured logging** for all exceptions
- **Graceful degradation** where possible
- **Use domain-specific exceptions** from S7Tools.Core.Exceptions

### **Custom Exception Pattern - REQUIRED** (Implemented 2025-01-16)

**Domain Exception Types** (Defined in S7Tools.Core/Exceptions/):
```csharp
// ValidationException - Input validation, command validation, configuration errors
throw new ValidationException("PropertyName", "Validation error message");
throw new ValidationException(validationErrors); // List of errors

// ConnectionException - Port accessibility, TCP conflicts, connection failures
throw new ConnectionException(
    "connectionTarget",
    "connectionType",
    "Connection error message");

// ConfigurationException - Settings save, max instances, configuration limits
throw new ConfigurationException(
    "ConfigurationKey",
    "Configuration error message");
```

**Exception Usage Guidelines**:
1. **ValidationException** - Use for input validation failures, command validation, configuration parsing errors
2. **ConnectionException** - Use for network/port/device connection failures
3. **ConfigurationException** - Use for settings/configuration save failures, limit violations
4. **ArgumentException** - Keep for parameter validation (standard .NET practice)
5. **Always log before throwing** - Use structured logging with context

**Services Updated with Custom Exceptions** (100% Complete):
- ✅ StandardProfileManager.cs - Profile CRUD validation and business rules
- ✅ PowerSupplyService.cs - Connection and configuration validation
- ✅ SocatService.cs - Process management and port validation
- ✅ SerialPortService.cs - Port accessibility and configuration
- ✅ SettingsService.cs - Settings persistence

**Example Implementation**:
```csharp
// Before (generic exception)
throw new InvalidOperationException($"Port {port} is not accessible");

// After (domain-specific exception)
throw new ConnectionException(
    portPath,
    "SerialPort",
    $"Port {portPath} is not accessible");
```

## Testing Framework - ESTABLISHED

### **Test Structure - DO NOT CHANGE**
```
tests/
├── S7Tools.Tests/                   # Main application tests
├── S7Tools.Core.Tests/              # Domain model tests
└── S7Tools.Infrastructure.Logging.Tests/  # Infrastructure tests
```

### **Testing Patterns - REQUIRED**
- **xUnit** - Primary testing framework
- **FluentAssertions** - Expressive assertions
- **Moq/NSubstitute** - Mocking frameworks
- **AAA Pattern** - Arrange, Act, Assert
- **Dependency injection** in tests

## New Feature Development Workflow

### **Step 1: Define Interface in Core**
```csharp
// S7Tools.Core/Services/Interfaces/INewFeatureService.cs
public interface INewFeatureService
{
    Task<Result<T>> DoSomethingAsync(Parameters parameters);
}
```

### **Step 2: Implement Service**
```csharp
// S7Tools/Services/NewFeatureService.cs
public class NewFeatureService : INewFeatureService
{
    private readonly ILogger<NewFeatureService> _logger;

    public NewFeatureService(ILogger<NewFeatureService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<T>> DoSomethingAsync(Parameters parameters)
    {
        try
        {
            // Implementation
            _logger.LogInformation("Operation completed successfully");
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed");
            return Result<T>.Failure(ex.Message);
        }
    }
}
```

### **Step 3: Register Service**
```csharp
// ServiceCollectionExtensions.cs
services.AddSingleton<INewFeatureService, NewFeatureService>();
```

### **Step 4: Create ViewModel**
```csharp
// S7Tools/ViewModels/NewFeatureViewModel.cs
public class NewFeatureViewModel : ReactiveObject, IDisposable
{
    private readonly INewFeatureService _service;
    private readonly CompositeDisposable _disposables = new();

    // Follow established ReactiveUI patterns
}
```

### **Step 5: Create View**
```xml
<!-- S7Tools/Views/NewFeatureView.axaml -->
<UserControl x:Class="S7Tools.Views.NewFeatureView">
    <!-- Follow established XAML patterns -->
</UserControl>
```

## Common Pitfalls - AVOID

### **Architecture Violations**
- **DO NOT** create circular dependencies
- **DO NOT** put business logic in ViewModels
- **DO NOT** access Views directly from ViewModels
- **DO NOT** bypass dependency injection

### **UI Implementation Issues**
- **DO NOT** assume XAML changes work without testing
- **DO NOT** ignore cross-platform considerations
- **DO NOT** hardcode UI values
- **DO NOT** create tight coupling between Views

### **Service Implementation Issues**
- **DO NOT** use incorrect service lifetimes
- **DO NOT** ignore error handling
- **DO NOT** skip logging integration
- **DO NOT** create services without interfaces

## Memory Bank Update Rules

### **When to Update Each File**

#### **instructions.md** (this file)
- **When**: New patterns are established or rules change
- **What**: Add new patterns, update architecture rules, document lessons learned
- **Who**: Agent that discovers new patterns or establishes new rules

#### **progress.md**
- **When**: Every implementation step, phase completion, or status change
- **What**: Current task status, progress percentages, issues encountered, user feedback
- **Who**: Agent currently working on implementation

#### **activeContext.md**
- **When**: Beginning of session, task changes, or context switches
- **What**: Current task, immediate next steps, blockers, session goals
- **Who**: Agent starting new work or changing context

### **Update Frequency**
- **activeContext.md**: Every session start and major context change
- **progress.md**: Every significant progress milestone or issue
- **instructions.md**: When new patterns are established or rules change

---

**Document Status**: Living guidelines for S7Tools development
**Next Update**: When new patterns are established or architecture changes
**Owner**: Development Team with AI Assistance

**Key Reminder**: These patterns and rules are established and working. Follow them consistently to maintain application quality and architecture integrity.
