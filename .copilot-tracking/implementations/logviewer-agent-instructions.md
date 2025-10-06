# LogViewer Implementation Agent Instructions

**Agent Role**: LogViewer Implementation Specialist  
**Project**: S7Tools - Avalonia Application with Logging Infrastructure  
**Implementation Phase**: Complete LogViewer Integration  
**Reference Location**: `.github/agents/workspace/referent-projects/LogViewerControl/`

## Agent Mission

You are tasked with implementing a comprehensive LogViewer system for the S7Tools Avalonia application. Your implementation must follow .NET best practices, maintain existing architectural patterns, and integrate seamlessly without breaking current functionality.

## Critical Implementation Rules

### 🚫 **NEVER MODIFY THESE FILES/PATTERNS**
- `src/S7Tools/Services/` - Existing service implementations
- `src/S7Tools/ViewModels/MainWindowViewModel.cs` - Core navigation logic (only ADD logging navigation item)
- `src/S7Tools/Views/MainWindow.axaml` - Main window structure
- `src/S7Tools/Program.cs` - Only ADD logging services, don't modify existing registrations
- `src/S7Tools.Core/` - Any existing core business logic
- Existing dependency injection patterns and service lifetimes
- Current ReactiveUI and MVVM patterns
- Avalonia project configuration and existing NuGet packages

### ✅ **REQUIRED PATTERNS TO FOLLOW**

#### 1. **Dependency Injection Pattern**
```csharp
// REQUIRED: Constructor injection with null checks
public class LogViewerControlViewModel(ILogDataStore dataStore)
{
    private readonly ILogDataStore _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
}

// REQUIRED: Service registration pattern
services.AddSingleton<ILogDataStore, LogDataStore>();
services.AddSingleton<LogViewerControlViewModel>();
```

#### 2. **MVVM Pattern with ReactiveUI**
```csharp
// REQUIRED: ReactiveUI ViewModelBase inheritance
public class LogViewerControlViewModel : ViewModelBase
{
    // REQUIRED: ReactiveCommand usage
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; }
    
    // REQUIRED: RaiseAndSetIfChanged for properties
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
}
```

#### 3. **Async/Await Patterns**
```csharp
// REQUIRED: Async methods with ConfigureAwait(false) in library code
public async Task<IEnumerable<LogModel>> GetEntriesAsync(LogLevel? minLevel = null)
{
    await _semaphore.WaitAsync().ConfigureAwait(false);
    try
    {
        // Implementation
    }
    finally
    {
        _semaphore.Release();
    }
}
```

#### 4. **Resource Management**
```csharp
// REQUIRED: Proper disposal patterns
public void Dispose()
{
    _loggers.Clear();
    _onChangeToken?.Dispose();
}

// REQUIRED: Thread-safe operations
private static readonly SemaphoreSlim _semaphore = new(1);
```

#### 5. **Configuration Pattern**
```csharp
// REQUIRED: Strongly-typed configuration with IOptions
public class DataStoreLoggerConfiguration
{
    public Dictionary<LogLevel, LogEntryColor> Colors { get; } = new();
}

// REQUIRED: Configuration binding
services.Configure<DataStoreLoggerConfiguration>(
    configuration.GetSection("DataStoreLogger"));
```

## Implementation Sequence

### Phase 1: Infrastructure Setup (Days 1-3)

#### Step 1.1: Create Logging Infrastructure Project
```bash
# Create new project
dotnet new classlib -n S7Tools.Infrastructure.Logging -f net8.0
cd S7Tools.Infrastructure.Logging
dotnet add package Avalonia --version 11.3.6
dotnet add package Avalonia.ReactiveUI --version 11.3.6
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
dotnet add package Microsoft.Extensions.Options --version 8.0.0
dotnet add package ReactiveUI --version 20.1.1
dotnet add package System.Drawing.Common --version 8.0.0
```

#### Step 1.2: Implement Core Models
**REQUIRED FILES TO CREATE:**
- `Core/Models/LogModel.cs` - Log entry data model
- `Core/Models/LogEntryColor.cs` - Color configuration model
- `Core/Models/LogDataStoreOptions.cs` - Storage configuration
- `Core/Storage/ILogDataStore.cs` - Storage interface
- `Core/Storage/LogDataStore.cs` - Thread-safe storage implementation
- `Core/Configuration/DataStoreLoggerConfiguration.cs` - Logger configuration

**CRITICAL REQUIREMENTS:**
- All models must have comprehensive XML documentation
- Thread-safe operations using SemaphoreSlim
- Circular buffer implementation to prevent memory leaks
- Proper null checks and argument validation

#### Step 1.3: Implement Microsoft Logger Provider
**REQUIRED FILES TO CREATE:**
- `Providers/Microsoft/DataStoreLogger.cs` - ILogger implementation
- `Providers/Microsoft/DataStoreLoggerProvider.cs` - ILoggerProvider implementation
- `Providers/Extensions/LoggingServiceCollectionExtensions.cs` - DI extensions

**CRITICAL REQUIREMENTS:**
- Follow Microsoft.Extensions.Logging patterns exactly
- Implement proper configuration monitoring with IOptionsMonitor
- Thread-safe logger creation with ConcurrentDictionary
- Proper disposal patterns

### Phase 2: UI Components (Days 4-7)

#### Step 2.1: Create LogViewer Control
**REQUIRED FILES TO CREATE:**
- `Controls/LogViewerControl.axaml` - Avalonia UserControl XAML
- `Controls/LogViewerControl.axaml.cs` - Control code-behind
- `Controls/ViewModels/LogViewerControlViewModel.cs` - Control ViewModel
- `Converters/LogLevelToColorConverter.cs` - Color value converter
- `Converters/LogLevelToIconConverter.cs` - Icon value converter
- `Converters/EventIdConverter.cs` - EventId display converter

**CRITICAL REQUIREMENTS:**
- Use DataGrid with virtualization for performance
- Implement auto-scroll functionality
- Support real-time updates via ObservableCollection
- Color-coded log entries based on log level
- Search and filtering capabilities
- Export functionality

#### Step 2.2: Implement Value Converters
```csharp
// REQUIRED: IValueConverter implementation
public class LogLevelToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(Avalonia.Media.Color.FromArgb(
                color.A, color.R, color.G, color.B));
        }
        return parameter; // Fallback color
    }
}
```

### Phase 3: Integration (Days 8-10)

#### Step 3.1: Add Project References
```xml
<!-- Add to S7Tools.csproj -->
<ProjectReference Include="..\S7Tools.Infrastructure.Logging\S7Tools.Infrastructure.Logging.csproj" />
```

#### Step 3.2: Create Application Configuration
**REQUIRED FILE TO CREATE:**
- `src/S7Tools/appsettings.json` - Application configuration

**CRITICAL REQUIREMENTS:**
- Configure log levels appropriately
- Set up color schemes for different log levels
- Configure maximum entries limit
- Set CopyToOutputDirectory to PreserveNewest

#### Step 3.3: Create Logging Views
**REQUIRED FILES TO CREATE:**
- `src/S7Tools/Views/Logging/LoggingView.axaml` - Logging page view
- `src/S7Tools/Views/Logging/LoggingView.axaml.cs` - View code-behind
- `src/S7Tools/ViewModels/Logging/LoggingViewModel.cs` - Page ViewModel

#### Step 3.4: Update Service Registration
**MODIFY EXISTING FILE:** `src/S7Tools/Program.cs`
```csharp
// ADD ONLY - Don't modify existing registrations
private static void ConfigureServices(IServiceCollection services)
{
    // [EXISTING SERVICES - DO NOT MODIFY]
    
    // [NEW] Configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
    services.AddSingleton<IConfiguration>(configuration);

    // [NEW] Logging Infrastructure
    services.AddLoggingInfrastructure(configuration);
    services.AddSingleton<LogViewerControlViewModel>();
    services.AddSingleton<LoggingViewModel>();

    // [NEW] Configure Logging
    services.AddLogging(builder =>
    {
        builder.AddConfiguration(configuration.GetSection("Logging"));
        builder.AddConsole();
        builder.AddDebug();
        builder.AddDataStoreLogger();
    });
}
```

#### Step 3.5: Add Navigation Integration
**MODIFY EXISTING FILE:** `src/S7Tools/ViewModels/MainWindowViewModel.cs`
```csharp
// ADD ONLY to existing NavigationItems initialization
NavigationItems.Add(new NavigationItemViewModel
{
    Label = "Logs",
    Icon = "fa-file-text-o",
    TargetView = typeof(Views.Logging.LoggingView)
});
```

### Phase 4: Testing and Validation (Days 11-15)

#### Step 4.1: Create Test Logging
```csharp
// Add to any existing service for testing
public class PlcDataService : ITagRepository, IS7ConnectionProvider
{
    private readonly ILogger<PlcDataService> _logger;

    public PlcDataService(ILogger<PlcDataService> logger)
    {
        _logger = logger;
    }

    public async Task<Tag> ReadTagAsync(string address)
    {
        _logger.LogInformation("Reading tag from address: {Address}", address);
        try
        {
            // Existing implementation
            _logger.LogDebug("Successfully read tag: {Address}", address);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read tag from address: {Address}", address);
            throw;
        }
    }
}
```

#### Step 4.2: Validation Checklist
**FUNCTIONAL TESTS:**
- [ ] LogViewer displays entries in real-time
- [ ] Color coding works for all log levels
- [ ] Auto-scroll functionality works
- [ ] Search and filtering work correctly
- [ ] Clear logs functionality works
- [ ] Export functionality works
- [ ] Navigation to logging view works
- [ ] All existing functionality remains unaffected

**PERFORMANCE TESTS:**
- [ ] UI responsive with 1000+ log entries
- [ ] Memory usage stable with circular buffer
- [ ] No memory leaks in event subscriptions
- [ ] Startup time impact < 100ms

**REGRESSION TESTS:**
- [ ] All existing views work correctly
- [ ] All existing services function properly
- [ ] Navigation between views works
- [ ] Existing ViewModels are not affected

## Error Handling Requirements

### Input Validation
```csharp
// REQUIRED: Null checks for all public methods
public void AddEntry(LogModel logModel)
{
    ArgumentNullException.ThrowIfNull(logModel);
    // Implementation
}

// REQUIRED: String validation
public async Task<Tag> ReadTagAsync(string address)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(address, nameof(address));
    // Implementation
}
```

### Exception Handling
```csharp
// REQUIRED: Structured logging for exceptions
try
{
    // Operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed with parameters: {Param1}, {Param2}", param1, param2);
    throw; // Re-throw to maintain stack trace
}
```

## Performance Requirements

### Memory Management
- Implement circular buffer with configurable maximum entries (default: 10,000)
- Use weak references for event subscriptions where appropriate
- Dispose of resources properly in Dispose methods
- Monitor memory usage during testing

### UI Performance
- Use DataGrid virtualization for large datasets
- Implement background filtering for search operations
- Use async/await for all I/O operations
- Avoid blocking the UI thread

### Threading
- All storage operations must be thread-safe
- Use SemaphoreSlim for synchronization
- Implement proper async patterns with ConfigureAwait(false)

## Documentation Requirements

### XML Documentation
```csharp
/// <summary>
/// Represents a single log entry with all associated metadata.
/// </summary>
/// <remarks>
/// This class is used to store log information that will be displayed
/// in the LogViewer control. It includes timing, level, and content information.
/// </remarks>
public class LogModel
{
    /// <summary>
    /// Gets or sets the timestamp when the log entry was created.
    /// </summary>
    /// <value>The UTC timestamp of the log entry creation.</value>
    public DateTime Timestamp { get; set; }
}
```

### Configuration Documentation
- Document all configuration options in appsettings.json
- Provide examples for custom color schemes
- Document performance tuning options
- Create troubleshooting guide

## Reference Implementation

**Primary Reference**: `.github/agents/workspace/referent-projects/LogViewerControl/CSharp/Applications/AvaloniaLoggingDI/`

**Key Reference Files:**
- `App.axaml.cs` - DI configuration patterns
- `Program.cs` - Service registration patterns
- `appsettings.json` - Configuration structure
- `LogViewerControl.axaml` - UI implementation
- `LogViewerControlViewModel.cs` - ViewModel patterns

**Architecture Reference**: `.github/agents/workspace/referent-projects/LogViewerControl/CSharp/Core/`

**Key Architecture Files:**
- `LogViewer.Core/` - Core abstractions and models
- `MsLogger.Core/` - Microsoft logger provider implementation
- `LogViewer.Avalonia/` - Avalonia-specific controls

## Success Criteria

### Functional Requirements
- ✅ Real-time log display with color coding
- ✅ Search and filtering capabilities
- ✅ Auto-scroll functionality
- ✅ Export functionality
- ✅ Integration with existing navigation
- ✅ Thread-safe operations
- ✅ Configurable via appsettings.json

### Non-Functional Requirements
- ✅ No performance degradation of existing functionality
- ✅ Memory usage remains stable
- ✅ UI remains responsive with large datasets
- ✅ Startup time impact is minimal
- ✅ All existing tests continue to pass
- ✅ Comprehensive documentation

### Quality Requirements
- ✅ Follows .NET best practices
- ✅ Comprehensive XML documentation
- ✅ Proper error handling and logging
- ✅ Thread-safe implementations
- ✅ Proper disposal patterns
- ✅ Input validation and null checks

## Final Validation

Before considering the implementation complete, ensure:

1. **Integration Testing**: All existing functionality works without modification
2. **Performance Testing**: No degradation in application performance
3. **Memory Testing**: No memory leaks or excessive memory usage
4. **Configuration Testing**: All configuration options work correctly
5. **Documentation**: Complete and accurate documentation
6. **Code Review**: Code follows established patterns and best practices

## Emergency Rollback Plan

If issues arise during implementation:

1. **Immediate**: Comment out logging service registration in Program.cs
2. **Short-term**: Remove LogViewer navigation item from MainWindowViewModel
3. **Long-term**: Remove LogViewer project references and files

The implementation is designed to be additive only, so rollback should not affect existing functionality.

---

**Remember**: This is an additive implementation. The existing S7Tools application must continue to work exactly as before, with the LogViewer system being a seamless addition that enhances the application without breaking any existing functionality.