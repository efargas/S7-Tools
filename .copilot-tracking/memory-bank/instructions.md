# Instructions: S7Tools Development Guidelines

**Last Updated**: January 2025 - New Functionality Phase
**Context Type**: Development patterns, architecture rules, and implementation guidelines

## Critical Development Rules

### **Memory Bank Usage - MANDATORY**

**File Structure and Purpose**:
- **instructions.md** (t### **Recent Session Accomplishments**

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

**Strategic Deferral System - TASK004 Created**:
- ✅ **Deferred Complex Changes** - File-scoped namespaces, extensive Result pattern, configuration centralization
- ✅ **Created Implementation Task** - TASK004 with comprehensive 22-30 hour implementation plan
- ✅ **Blocked Until Socat Complete** - Prevents interference with high-priority feature development
- ✅ **Risk Assessment Complete** - Documented risks and benefits for each deferred improvement

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

### **🎉 MAJOR BREAKTHROUGH: UI Dialog Integration Complete (2025-10-09)** file) - Development patterns, rules, and guidelines for next agent
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

## Recent Session Accomplishments

### **🎉 MAJOR BREAKTHROUGH: UI Dialog Integration Complete (2025-10-09)**

**Profile Name Conflict Resolution Enhancement**
- ✅ **Replaced exception throwing** with intelligent naming strategies using automatic suffix naming (`_1`, `_2`, `_3`, etc.)
- ✅ **Implemented fallback mechanisms** for edge cases with timestamp-based unique names
- ✅ **Integrated comprehensive UI dialog system** for manual conflict resolution when automatic fails

**Complete UI Dialog System Implementation**
- ✅ **InputRequest/InputResult Models**: Clean data transfer objects for dialog communication
- ✅ **IDialogService Extension**: Added `ShowInput` interaction for text input dialogs using ReactiveUI patterns
- ✅ **DialogService Implementation**: Full ReactiveUI interaction support for confirmation, error, and input dialogs
- ✅ **InputDialog UI Components**:
  - Professional Avalonia XAML view with VSCode-style theming
  - ReactiveUI ViewModel with proper command binding
  - Keyboard navigation support (Enter/Escape keys)
  - Focus management and user experience enhancements
- ✅ **Application Integration**: Complete integration in `App.axaml.cs` with proper error handling and dialog handler registration

**Technical Excellence Achieved**
- ✅ **Architecture Compliance**: Clean Architecture maintained with proper dependency flow
- ✅ **Thread Safety**: SemaphoreSlim-based concurrency control for profile operations
- ✅ **Quality Assurance**: 168 tests passing, successful compilation with only warnings
- ✅ **Error Handling**: Comprehensive logging and graceful fallbacks throughout
- ✅ **User Experience**: Professional dialog interactions with smart conflict resolution

**Pattern Established: ReactiveUI Dialog Integration**
```csharp
// Service Integration Pattern (Required for all dialog services)
public class ServiceWithDialogs
{
    private readonly IDialogService _dialogService;

    // Use ShowInput for text input with validation
    var result = await _dialogService.ShowInputAsync(
        new InputRequest("Profile Name Conflict",
                        $"Name '{originalName}' already exists. Please enter a new name:",
                        suggestedName,
                        "Enter unique profile name"));

    if (!result.IsCancelled && !string.IsNullOrWhiteSpace(result.Value))
    {
        // Process user input
    }
}

// App.axaml.cs Registration Pattern (Required for all dialogs)
dialogService.ShowInput.RegisterHandler(async interaction =>
{
    var dialog = new InputDialog
    {
        DataContext = new InputDialogViewModel(interaction.Input)
    };
    var result = await dialog.ShowDialog<InputResult?>(mainWindow);
    interaction.SetOutput(result ?? InputResult.Cancelled());
});
```

### **Previous Session Notes (2025-10-08)**

- **UI / Serial Ports fixes**
    - Fixed cross-thread DataGrid crash by marshaling profile collection updates to the UI thread using an injected IUIThreadService in `SerialPortsSettingsViewModel`.
    - Added ProfilesPath control to Serial Ports settings with Browse, Open in Explorer, and Load Default actions (default path: resources/SerialProfiles).
    - Resolved DataGrid header styling/truncation by using Avalonia `DataGrid.Styles` and smaller header padding + TextTrimming on headers.

- **Persistence & Logging**
    - Serial port profiles are now created automatically when missing; `profiles.json` is created under the app runtime resources folder (e.g. `src/S7Tools/bin/Debug/net8.0/resources/SerialProfiles/profiles.json`).
    - Added a minimal `FileLogWriter` service to persist in-memory logs to disk under the configured `Logging.DefaultLogPath`. Registered in DI for automatic startup.
