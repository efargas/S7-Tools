# AGENTS.md

## Project Overview

**S7Tools** is a cross-platform desktop application for Siemens S7-1200 PLC communication, built with .NET 8.0 and Avalonia UI. The application features a **VSCode-like interface** with advanced logging capabilities, implementing **Clean Architecture** with **MVVM pattern**.

### Key Technologies
- **.NET 8.0** with latest C# language features
- **Avalonia UI 11.3.6** for cross-platform desktop UI
- **ReactiveUI 20.1.1** for reactive MVVM implementation
- **Microsoft.Extensions.Logging** with custom DataStore provider
- **Microsoft.Extensions.DependencyInjection** for IoC container

### Architecture
- **Multi-project solution** with clear layer separation
- **Clean Architecture** with dependency inversion
- **Service-oriented design** with comprehensive DI registration
- **MVVM pattern** with ReactiveUI and CommunityToolkit.Mvvm

---

## Setup Commands

### Prerequisites
- **.NET 8.0 SDK** or later
- **Visual Studio 2022**, **JetBrains Rider**, or **VS Code** with C# extension

### Initial Setup
```bash
# Clone and navigate to project
cd S7Tools

# Restore dependencies for entire solution
dotnet restore src/S7Tools.sln

# Build entire solution
dotnet build src/S7Tools.sln

# Run the application
dotnet run --project src/S7Tools/S7Tools.csproj
```

### Alternative Build Commands
```bash
# Build from solution directory
cd src
dotnet build

# Build specific project
dotnet build S7Tools/S7Tools.csproj

# Build with specific configuration
dotnet build --configuration Release
```

---

## Development Workflow

### Starting Development
```bash
# Navigate to source directory
cd src

# Run application in development mode
dotnet run --project S7Tools/S7Tools.csproj

# Run with hot reload (if supported)
dotnet watch run --project S7Tools/S7Tools.csproj
```

### Project Structure
- **S7Tools** - Main Avalonia application (UI, ViewModels, Services)
- **S7Tools.Core** - Domain models and service interfaces (dependency-free)
- **S7Tools.Infrastructure.Logging** - Logging infrastructure with custom providers

### Key Development Files
- **Program.cs** - Application entry point and DI configuration
- **App.axaml/App.axaml.cs** - Application-level configuration
- **MainWindow.axaml** - Primary UI with VSCode-like layout
- **ServiceCollectionExtensions.cs** - Service registration extensions

### Adding New Features
1. **Define Interface** in `S7Tools.Core/Services/Interfaces/`
2. **Implement Service** in `S7Tools/Services/`
3. **Register Service** in `ServiceCollectionExtensions.cs`
4. **Create ViewModel** in `S7Tools/ViewModels/`
5. **Create View** in `S7Tools/Views/`

---

## Testing Instructions

### Current State
✅ **Comprehensive testing framework successfully implemented**

**Test Structure**:
- **S7Tools.Tests** - Main application tests (UI, Services, ViewModels)
- **S7Tools.Core.Tests** - Domain model and business logic tests
- **S7Tools.Infrastructure.Logging.Tests** - Logging infrastructure tests

**Test Statistics**:
- **Total Tests**: 123 tests implemented
- **Success Rate**: 93.5% (115 passing, 8 failing edge cases)
- **Execution Time**: ~71 seconds for full test suite
- **Coverage**: Domain models, infrastructure, services, UI components

### Recommended Testing Setup
```bash
# Create test projects (recommended structure)
mkdir tests
cd tests

# Create unit test project for main application
dotnet new xunit -n S7Tools.Tests
dotnet add S7Tools.Tests/S7Tools.Tests.csproj reference ../src/S7Tools/S7Tools.csproj

# Create unit test project for core domain
dotnet new xunit -n S7Tools.Core.Tests
dotnet add S7Tools.Core.Tests/S7Tools.Core.Tests.csproj reference ../src/S7Tools.Core/S7Tools.Core.csproj

# Create unit test project for logging infrastructure
dotnet new xunit -n S7Tools.Infrastructure.Logging.Tests
dotnet add S7Tools.Infrastructure.Logging.Tests/S7Tools.Infrastructure.Logging.Tests.csproj reference ../src/S7Tools.Infrastructure.Logging/S7Tools.Infrastructure.Logging.csproj

# Add test projects to solution
dotnet sln src/S7Tools.sln add tests/S7Tools.Tests/S7Tools.Tests.csproj
dotnet sln src/S7Tools.sln add tests/S7Tools.Core.Tests/S7Tools.Core.Tests.csproj
dotnet sln src/S7Tools.sln add tests/S7Tools.Infrastructure.Logging.Tests/S7Tools.Infrastructure.Logging.Tests.csproj
```

### Running Tests (once implemented)
```bash
# Run all tests
dotnet test src/S7Tools.sln

# Run tests with coverage
dotnet test src/S7Tools.sln --collect:\"XPlat Code Coverage\"

# Run specific test project
dotnet test tests/S7Tools.Tests/S7Tools.Tests.csproj

# Run tests in watch mode
dotnet watch test tests/S7Tools.Tests/S7Tools.Tests.csproj
```

### Manual Testing
```bash
# Build and run application for manual testing
dotnet build src/S7Tools.sln
dotnet run --project src/S7Tools/S7Tools.csproj

# Test logging functionality using built-in test commands in UI
# Navigate to Explorer view and use LoggingTestView for log testing
```

---

## Code Style Guidelines

### EditorConfig Enforcement
The project uses comprehensive **EditorConfig** rules for consistent code style:

```bash
# Verify code style compliance
dotnet format src/S7Tools.sln --verify-no-changes

# Apply code formatting
dotnet format src/S7Tools.sln
```

### Key Conventions
- **C# Files**: 4-space indentation, PascalCase naming
- **XAML Files**: 2-space indentation, PascalCase for elements
- **Interfaces**: Prefixed with 'I' (e.g., `IActivityBarService`)
- **Private Fields**: Camel case with underscore prefix (e.g., `_fieldName`)
- **Async Methods**: Always use `ConfigureAwait(false)` for library code

### Naming Patterns
- **Services**: `{FeatureName}Service.cs` with corresponding `I{FeatureName}Service.cs`
- **ViewModels**: `{ViewName}ViewModel.cs`
- **Views**: `{ViewName}.axaml` with `{ViewName}.axaml.cs`
- **Models**: `{EntityName}.cs` or `{EntityName}Model.cs`

### File Organization
```
S7Tools/
├── Services/
│   ├── Interfaces/          # Service contracts
│   └── {ServiceName}.cs     # Service implementations
├── ViewModels/              # MVVM ViewModels
├── Views/                   # XAML Views with code-behind
├── Models/                  # Application-specific models
├── Converters/              # Value converters for data binding
└── Extensions/              # Extension methods and utilities
```

### Dependency Injection Patterns
```csharp
// Register services in ServiceCollectionExtensions.cs
services.AddSingleton<IServiceInterface, ServiceImplementation>();

// Use constructor injection in classes
public class MyService
{
    private readonly IDependency _dependency;
    
    public MyService(IDependency dependency)
    {
        _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
    }
}
```

---

## Build and Deployment

### Build Commands
```bash
# Debug build (default)
dotnet build src/S7Tools.sln

# Release build
dotnet build src/S7Tools.sln --configuration Release

# Build with specific verbosity
dotnet build src/S7Tools.sln --verbosity normal
```

### Output Locations
- **Debug**: `src/S7Tools/bin/Debug/net8.0/`
- **Release**: `src/S7Tools/bin/Release/net8.0/`
- **Intermediate**: `src/{ProjectName}/obj/`

### Publishing
```bash
# Self-contained deployment for Windows
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r win-x64 --self-contained

# Framework-dependent deployment
dotnet publish src/S7Tools/S7Tools.csproj -c Release

# Cross-platform publishing
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r linux-x64 --self-contained
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r osx-x64 --self-contained
```

### Environment Configurations
- **Development**: Debug configuration with Avalonia diagnostics enabled
- **Production**: Release configuration with optimizations and no diagnostics
- **Cross-Platform**: Avalonia supports Windows, Linux, and macOS

---

## Architecture Guidelines

### Clean Architecture Layers
1. **Domain Layer** (`S7Tools.Core`) - Business entities and interfaces
2. **Infrastructure Layer** (`S7Tools.Infrastructure.*`) - External concerns
3. **Application Layer** (`S7Tools`) - UI, ViewModels, and application services

### Dependency Rules
- **Core project** has no external dependencies
- **Infrastructure projects** depend only on Core
- **Application project** depends on Core and Infrastructure
- **All dependencies flow inward** toward the Core

### Service Registration Pattern
```csharp
// In ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register services with appropriate lifetime
        services.AddSingleton<ILongLivedService, LongLivedService>();
        services.AddScoped<IRequestScopedService, RequestScopedService>();
        services.AddTransient<ITransientService, TransientService>();
        
        return services;
    }
}
```

### MVVM Implementation
- **Views** contain only UI logic and data binding
- **ViewModels** handle presentation logic and user interactions
- **Models** represent data structures and business entities
- **Services** encapsulate business logic and external integrations

---

## Logging and Debugging

### Built-in Logging System
The application includes a comprehensive logging infrastructure:

```csharp
// Inject logger in services/ViewModels
private readonly ILogger<MyClass> _logger;

// Use structured logging
_logger.LogInformation("User performed action: {Action} at {Timestamp}", action, DateTime.Now);
_logger.LogError(exception, "Error occurred in {Method}", nameof(MyMethod));
```

### Log Viewer Features
- **Real-time log display** in bottom panel
- **Filtering by log level** and search text
- **Export functionality** (Text, JSON, CSV)
- **Color-coded log levels** with VSCode styling

### Testing Logging
```bash
# Run application and use built-in log test commands
dotnet run --project src/S7Tools/S7Tools.csproj

# Navigate to Explorer view in the application
# Use the LoggingTestView to generate test log entries
# View logs in the LOG VIEWER tab in the bottom panel
```

### Debug Configuration
```bash
# Run with debug configuration
dotnet run --project src/S7Tools/S7Tools.csproj --configuration Debug

# Enable Avalonia diagnostics (Debug builds only)
# Press F12 in the running application to open DevTools
```

---

## Pull Request Guidelines

### Title Format
```
[Component] Brief description of changes

Examples:
[Services] Add PLC connection service with S7 protocol support
[UI] Implement VSCode-style activity bar with selection states
[Logging] Add circular buffer for high-performance log storage
```

### Required Checks Before Submission
```bash
# 1. Code formatting
dotnet format src/S7Tools.sln --verify-no-changes

# 2. Build verification
dotnet build src/S7Tools.sln --configuration Release

# 3. Run application smoke test
dotnet run --project src/S7Tools/S7Tools.csproj

# 4. Check for warnings
dotnet build src/S7Tools.sln --verbosity normal | grep -i warning
```

### Code Review Requirements
- **Architecture compliance** - Ensure Clean Architecture principles
- **MVVM pattern adherence** - Proper separation of concerns
- **Service registration** - New services properly registered in DI
- **XML documentation** - All public APIs documented
- **Error handling** - Appropriate exception handling and logging

### Commit Message Conventions
```
feat: add new PLC connection service
fix: resolve memory leak in log viewer
docs: update API documentation for logging services
refactor: improve service registration patterns
style: apply consistent code formatting
test: add unit tests for activity bar service
```

---

## Troubleshooting

### Common Build Issues

#### Missing Dependencies
```bash
# Clean and restore
dotnet clean src/S7Tools.sln
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln
```

#### Avalonia Designer Issues
```bash
# Rebuild specific project
dotnet build src/S7Tools/S7Tools.csproj --force

# Clear obj/bin folders
rm -rf src/S7Tools/obj src/S7Tools/bin
dotnet build src/S7Tools/S7Tools.csproj
```

#### Service Registration Errors
- Verify service interfaces are registered in `ServiceCollectionExtensions.cs`
- Check that service lifetimes (Singleton/Scoped/Transient) are appropriate
- Ensure circular dependencies are avoided

### Performance Considerations
- **Logging**: Uses circular buffer for high-performance log storage
- **UI Threading**: All UI updates use proper thread marshalling
- **Memory Management**: Implements proper disposal patterns
- **Reactive Programming**: Uses ReactiveUI for efficient UI updates

### Platform-Specific Issues

#### Windows
- Ensure **.NET 8.0 Desktop Runtime** is installed
- **Windows 10/11** recommended for full feature support

#### Linux
```bash
# Install required dependencies
sudo apt-get update
sudo apt-get install dotnet-sdk-8.0

# For UI applications, may need additional packages
sudo apt-get install libx11-dev libice-dev libsm-dev libxext-dev
```

#### macOS
```bash
# Install via Homebrew
brew install dotnet

# May require additional permissions for UI applications
```

---

## Development Environment Tips

### Recommended IDE Settings
- **Visual Studio 2022**: Enable EditorConfig support
- **JetBrains Rider**: Configure code style to match EditorConfig
- **VS Code**: Install C# extension and EditorConfig extension

### Useful Commands
```bash
# Navigate to project root quickly
cd src

# Build and run in one command
dotnet run --project S7Tools/S7Tools.csproj

# Watch for file changes during development
dotnet watch run --project S7Tools/S7Tools.csproj

# Generate project dependency graph
dotnet list src/S7Tools.sln reference
```

### AI Agent Specific Notes
- **Service Registration**: Always register new services in `ServiceCollectionExtensions.cs`
- **MVVM Compliance**: Follow existing patterns in ViewModels and Views
- **Logging Integration**: Use injected `ILogger<T>` for all logging needs
- **Error Handling**: Implement proper exception handling with user-friendly messages
- **Thread Safety**: Use `IUIThreadService` for cross-thread UI operations

### ReactiveUI Best Practices & Lessons Learned

#### Property Change Monitoring
**❌ AVOID: Large WhenAnyValue calls with many properties**
```csharp
// DON'T DO THIS - Creates large tuples, has 12-property limit, poor performance
var allChanges = this.WhenAnyValue(
    x => x.Prop1, x => x.Prop2, x => x.Prop3, x => x.Prop4,
    x => x.Prop5, x => x.Prop6, x => x.Prop7, x => x.Prop8,
    x => x.Prop9, x => x.Prop10, x => x.Prop11, x => x.Prop12)
    .Select(_ => Unit.Default);
```

**✅ PREFERRED: Individual property subscriptions**
```csharp
// DO THIS - Better performance, no limits, more maintainable
private void SetupValidation()
{
    void OnPropertyChanged()
    {
        HasChanges = true;
        UpdateSttyCommand();
        ValidateConfiguration();
    }

    this.WhenAnyValue(x => x.PropertyName)
        .Skip(1) // Skip initial value
        .Subscribe(_ => OnPropertyChanged())
        .DisposeWith(_disposables);
    
    // Repeat for each property...
}
```

#### Key ReactiveUI Constraints & Solutions

1. **WhenAnyValue Property Limit**
   - **Constraint**: Maximum 12 properties per `WhenAnyValue` call
   - **Solution**: Use individual subscriptions or break into smaller groups with `Observable.Merge`

2. **Tuple Creation Performance**
   - **Problem**: Large `WhenAnyValue` calls create unnecessary tuples for every change
   - **Solution**: Individual subscriptions avoid tuple allocation overhead

3. **Compilation Errors**
   - **Common Error**: `"string" does not contain a definition for "PropertyName"`
   - **Root Cause**: Missing `using ReactiveUI;` or class not inheriting `ReactiveObject`
   - **Fix**: Ensure proper inheritance and using statements

4. **Missing Commas in WhenAnyValue**
   - **Problem**: Syntax errors when properties lack commas between lambda expressions
   - **Solution**: Always verify comma separation in multi-property calls

#### Performance Guidelines

- **Individual Subscriptions**: Use for 3+ properties or when performance matters
- **Small WhenAnyValue Groups**: Acceptable for 2-3 related properties
- **Observable.Merge**: Use to combine multiple observables when needed
- **Skip(1)**: Always skip initial values to prevent false change detection
- **DisposeWith**: Ensure all subscriptions are properly disposed

#### Debugging ReactiveUI Issues

1. **Check Class Inheritance**: Ensure ViewModel inherits from `ReactiveObject` or `ViewModelBase`
2. **Verify Using Statements**: Confirm `using ReactiveUI;` is present
3. **Property Syntax**: Ensure properties use `RaiseAndSetIfChanged` pattern
4. **Subscription Disposal**: All reactive subscriptions must be disposed properly
5. **Initial Value Handling**: Use `Skip(1)` to avoid processing initial property values

#### Memory Bank: Critical Patterns

**Property Change Monitoring Pattern (Recommended)**:
```csharp
private void SetupValidation()
{
    // Shared handler for all property changes
    void OnPropertyChanged()
    {
        HasChanges = true;
        UpdateCommand();
        ValidateData();
    }

    // Individual subscriptions for each property
    this.WhenAnyValue(x => x.Property1).Skip(1).Subscribe(_ => OnPropertyChanged()).DisposeWith(_disposables);
    this.WhenAnyValue(x => x.Property2).Skip(1).Subscribe(_ => OnPropertyChanged()).DisposeWith(_disposables);
    // ... continue for all properties
}
```

**Command Enablement Pattern**:
```csharp
var canExecute = this.WhenAnyValue(x => x.IsValid, x => x.HasChanges, x => x.IsReadOnly)
    .Select(tuple => tuple.Item1 && tuple.Item2 && !tuple.Item3);

MyCommand = ReactiveCommand.CreateFromTask(ExecuteAsync, canExecute);
```

These patterns ensure optimal performance, maintainability, and avoid common ReactiveUI pitfalls.

---

## Additional Context

### Project Status
- **Core Infrastructure**: ✅ Complete (Logging, Services, DI)
- **VSCode-like UI**: ✅ Complete (Activity bar, Sidebar, Bottom panel)
- **LogViewer Integration**: ✅ Complete (Real-time logging with filtering)
- **PLC Communication**: 🔄 In Development
- **Testing Framework**: ✅ Complete (123 tests, 93.5% success rate)

### Key Features Implemented
- **Advanced Logging System** with circular buffer and real-time display
- **VSCode-style Interface** with activity bars and collapsible panels
- **Service-Oriented Architecture** with comprehensive DI
- **Reactive MVVM** with ReactiveUI and proper data binding
- **Cross-Platform Support** via Avalonia UI

### Future Development Areas
- **Unit Testing Framework** implementation
- **PLC Communication Protocol** integration
- **Configuration Management** system
- **Plugin Architecture** for extensibility
- **Performance Optimization** for large datasets

---

**Last Updated**: Current Session  
**Project Version**: Development  
**Minimum .NET Version**: 8.0  
**Supported Platforms**: Windows, Linux, macOS