---
description: 'Memory instructions for .NET LogViewer implementation patterns, best practices, and common pitfalls specific to S7Tools project architecture.'
applyTo:
  - 'src/**/*.cs'
  - 'src/**/*.axaml'
  - 'src/**/*.csproj'
  - '.copilot-tracking/**/*.md'
---

# .NET LogViewer Implementation Memory

Patterns and practices for implementing LogViewer systems in .NET Avalonia applications with proper architecture and performance considerations.

## 🚨 CRITICAL: Project Configuration Requirements

### Project File Configuration Pattern
**ALWAYS** ensure these properties are present in ALL .csproj files to prevent compilation issues:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

**CRITICAL**: `ImplicitUsings` must be enabled to prevent 100+ compilation errors from missing using statements.

### EditorConfig Requirements
**ALWAYS** maintain comprehensive .editorconfig with memory optimization rules:

```ini
# Memory optimization rules - REQUIRED
csharp_style_prefer_null_check_over_type_check = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:warning
dotnet_diagnostic.IDE0041.severity = warning # Use 'is null' check
dotnet_diagnostic.IDE0150.severity = warning # Prefer 'null' check over type check

# Performance rules - REQUIRED
dotnet_diagnostic.CA1805.severity = warning # Avoid unnecessary initialization
dotnet_diagnostic.CA1822.severity = suggestion # Mark members as static
dotnet_diagnostic.CA1825.severity = warning # Avoid zero-length array allocations
```

### Null Check Pattern (MEMORY OPTIMIZED)
**ALWAYS** use `is null` and `is not null` instead of `== null` and `!= null` for better memory performance:

```csharp
// CORRECT: Memory-optimized null checking
if (value is null)
    throw new ArgumentNullException(nameof(value));

if (result is not null)
    return result.Value;

// WRONG: Less efficient null checking
if (value == null) // Don't use this
if (result != null) // Don't use this
```

### Avalonia Dispatcher Compatibility Pattern
**CRITICAL**: Avalonia DispatcherOperation doesn't support ConfigureAwait - handle properly:

```csharp
// CORRECT: Avalonia-compatible async patterns
public async Task InvokeOnUIThreadAsync(Action action)
{
    if (IsUIThread)
    {
        action();
    }
    else
    {
        await Dispatcher.UIThread.InvokeAsync(action); // No ConfigureAwait
    }
}

// WRONG: Will cause compilation errors
await Dispatcher.UIThread.InvokeAsync(action).ConfigureAwait(false); // Don't do this
```

## Architecture Patterns

### Additive-Only Integration Pattern
When integrating LogViewer into existing applications, always use additive-only approach to prevent regressions:

```csharp
// CORRECT: Add new services without modifying existing ones
private static void ConfigureServices(IServiceCollection services)
{
    // [EXISTING SERVICES - DO NOT MODIFY]
    services.AddSingleton<IGreetingService, GreetingService>();

    // [NEW] Logging Infrastructure - ADD ONLY
    services.AddLoggingInfrastructure(configuration);
    services.AddSingleton<LogViewerControlViewModel>();
}
```

Never modify existing service registrations or change service lifetimes when adding logging functionality.

### Thread-Safe Storage Pattern
Always implement thread-safe log storage with proper synchronization:

```csharp
// CORRECT: Thread-safe storage with SemaphoreSlim
public class LogDataStore : ILogDataStore
{
    private static readonly SemaphoreSlim _semaphore = new(1);

    public virtual void AddEntry(LogModel logModel)
    {
        _semaphore.Wait();
        try
        {
            // Circular buffer logic to prevent memory leaks
            if (Entries.Count >= _maxEntries)
                Entries.RemoveAt(0);

            Entries.Add(logModel);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

Use SemaphoreSlim instead of lock() for async-compatible synchronization.

### Microsoft Logger Provider Pattern
Follow Microsoft.Extensions.Logging patterns exactly for custom providers:

```csharp
// CORRECT: Proper ILoggerProvider implementation
public class DataStoreLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DataStoreLogger> _loggers = new();
    private readonly IDisposable? _onChangeToken;

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name =>
            new DataStoreLogger(name, GetCurrentConfig, _dataStore));

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}
```

Always use ConcurrentDictionary for thread-safe logger caching and implement proper disposal.

## Performance Patterns

### Circular Buffer Implementation
Implement circular buffer to prevent memory leaks with large log volumes:

```csharp
// CORRECT: Circular buffer with configurable limit
public void AddEntry(LogModel logModel)
{
    if (Entries.Count >= _maxEntries)
    {
        Entries.RemoveAt(0); // Remove oldest entry
    }
    Entries.Add(logModel); // Add new entry
}
```

Default to 10,000 entries maximum, make configurable via IOptions pattern.

### UI Virtualization Pattern
Use DataGrid virtualization for large datasets:

```xml
<!-- CORRECT: DataGrid with virtualization enabled -->
<DataGrid Items="{Binding FilteredEntries}"
          AutoGenerateColumns="False"
          VirtualizationMode="Recycling"
          EnableRowVirtualization="True">
```

Always enable virtualization when displaying large collections in UI controls.

### Async Configuration Pattern
Use ConfigureAwait(false) in library code to prevent deadlocks:

```csharp
// CORRECT: ConfigureAwait(false) in library methods
public async Task<IEnumerable<LogModel>> GetEntriesAsync()
{
    await _semaphore.WaitAsync().ConfigureAwait(false);
    try
    {
        return await ProcessEntriesAsync().ConfigureAwait(false);
    }
    finally
    {
        _semaphore.Release();
    }
}
```

Only omit ConfigureAwait(false) in UI code where you need to return to UI thread.

## Configuration Patterns

### Strongly-Typed Configuration Pattern
Always use IOptions pattern for configuration:

```csharp
// CORRECT: Strongly-typed configuration with validation
public class DataStoreLoggerConfiguration
{
    public Dictionary<LogLevel, LogEntryColor> Colors { get; } = new()
    {
        [LogLevel.Information] = new() { Foreground = Color.Black },
        [LogLevel.Error] = new() { Foreground = Color.White, Background = Color.Red }
    };
}

// Registration
services.Configure<DataStoreLoggerConfiguration>(
    configuration.GetSection("DataStoreLogger"));
```

Use IOptionsMonitor for configuration that can change at runtime.

### Default Configuration Pattern
Always provide sensible defaults that work without configuration:

```json
{
  "DataStoreLogger": {
    "MaxEntries": 10000,
    "Colors": {
      "Information": { "Foreground": "Black", "Background": "Transparent" },
      "Error": { "Foreground": "White", "Background": "Red" }
    }
  }
}
```

Ensure application works even if configuration section is missing.

## MVVM Patterns

### ReactiveUI ViewModel Pattern
Use ReactiveUI patterns consistently with existing codebase:

```csharp
// CORRECT: ReactiveUI ViewModel with proper property handling
public class LogViewerControlViewModel : ViewModelBase
{
    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; }

    public LogViewerControlViewModel(ILogDataStore dataStore)
    {
        ClearLogsCommand = ReactiveCommand.Create(ClearLogs);
    }
}
```

Always inherit from ViewModelBase and use RaiseAndSetIfChanged for properties.

### ObservableCollection Binding Pattern
Use ObservableCollection for real-time UI updates:

```csharp
// CORRECT: ObservableCollection for UI binding
public ObservableCollection<LogModel> Entries { get; } = new();

// In ViewModel
private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    if (e.Action == NotifyCollectionChangedAction.Add)
    {
        // Handle new entries for filtering
        foreach (LogModel newItem in e.NewItems.Cast<LogModel>())
        {
            if (ShouldIncludeEntry(newItem))
                _filteredEntries.Add(newItem);
        }
    }
}
```

Always handle CollectionChanged events for filtered views.

## Error Handling Patterns

### Null Check Pattern
Use ArgumentNullException.ThrowIfNull for parameter validation:

```csharp
// CORRECT: Modern null checking
public DataStoreLogger(string categoryName, ILogDataStore dataStore)
{
    ArgumentNullException.ThrowIfNull(categoryName);
    ArgumentNullException.ThrowIfNull(dataStore);

    _categoryName = categoryName;
    _dataStore = dataStore;
}
```

Use ArgumentException.ThrowIfNullOrWhiteSpace for string parameters.

### Structured Logging Pattern
Use structured logging with proper parameter formatting:

```csharp
// CORRECT: Structured logging with parameters
_logger.LogError(ex, "Failed to read tag from address {Address} with timeout {Timeout}ms",
    address, timeout);

// AVOID: String concatenation in log messages
_logger.LogError($"Failed to read tag from {address}"); // Don't do this
```

Always use structured logging parameters instead of string interpolation.

## Memory Management Patterns

### Disposal Pattern
Implement proper disposal for resources:

```csharp
// CORRECT: Proper disposal pattern
public void Dispose()
{
    _loggers.Clear();
    _onChangeToken?.Dispose();
    _semaphore?.Dispose();
}

// In ViewModels
private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
{
    if (vm is null) return;
    vm.DataStore.Entries.CollectionChanged -= OnCollectionChanged;
}
```

Always unsubscribe from events to prevent memory leaks.

### Event Subscription Pattern
Use weak references or proper cleanup for event subscriptions:

```csharp
// CORRECT: Proper event cleanup
public LogViewerControl()
{
    InitializeComponent();
}

private void OnDataContextChanged(object? sender, EventArgs e)
{
    if (DataContext is ILogDataStoreImpl vm)
    {
        vm.DataStore.Entries.CollectionChanged += OnCollectionChanged;
    }
}

private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
{
    // Always cleanup event subscriptions
    if (vm?.DataStore?.Entries != null)
        vm.DataStore.Entries.CollectionChanged -= OnCollectionChanged;
}
```

Always pair event subscriptions with unsubscriptions.

## Common Pitfalls

### Avoid Blocking UI Thread
Never perform synchronous operations on UI thread:

```csharp
// WRONG: Blocking UI thread
public void LoadLogs()
{
    var logs = _dataStore.GetEntries(); // Synchronous call
    FilteredEntries.Clear();
    foreach (var log in logs)
        FilteredEntries.Add(log);
}

// CORRECT: Async operation
public async Task LoadLogsAsync()
{
    var logs = await _dataStore.GetEntriesAsync().ConfigureAwait(false);
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        FilteredEntries.Clear();
        foreach (var log in logs)
            FilteredEntries.Add(log);
    });
}
```

Use async/await and Dispatcher.UIThread.InvokeAsync for UI updates.

### Avoid Memory Leaks in Collections
Don't let collections grow unbounded:

```csharp
// WRONG: Unbounded collection growth
public void AddEntry(LogModel logModel)
{
    Entries.Add(logModel); // Will grow forever
}

// CORRECT: Circular buffer with limits
public void AddEntry(LogModel logModel)
{
    if (Entries.Count >= _maxEntries)
        Entries.RemoveAt(0);
    Entries.Add(logModel);
}
```

Always implement size limits for log collections.

### Avoid Configuration Hardcoding
Don't hardcode configuration values:

```csharp
// WRONG: Hardcoded values
public class LogDataStore
{
    private const int MaxEntries = 1000; // Hardcoded
}

// CORRECT: Configurable values
public class LogDataStore
{
    private readonly int _maxEntries;

    public LogDataStore(IOptions<LogDataStoreOptions> options)
    {
        _maxEntries = options.Value.MaxEntries;
    }
}
```

Always use IOptions pattern for configurable values.

## Integration Patterns

### Navigation Integration Pattern
Add navigation items without modifying existing structure:

```csharp
// CORRECT: Add to existing NavigationItems collection
public MainWindowViewModel(/* existing parameters */)
{
    // [EXISTING INITIALIZATION - DO NOT MODIFY]

    // [NEW] Add logging navigation - ADD ONLY
    NavigationItems.Add(new NavigationItemViewModel
    {
        Label = "Logs",
        Icon = "fa-file-text-o",
        TargetView = typeof(Views.Logging.LoggingView)
    });
}
```

Never modify existing navigation items or change navigation structure.

### Service Integration Pattern
Integrate logging into existing services without breaking changes:

```csharp
// CORRECT: Add logging to existing service
public class PlcDataService : ITagRepository, IS7ConnectionProvider
{
    private readonly ILogger<PlcDataService> _logger;

    // Modified constructor - add logger parameter
    public PlcDataService(ILogger<PlcDataService> logger)
    {
        _logger = logger;
    }

    public async Task<Tag> ReadTagAsync(string address)
    {
        _logger.LogInformation("Reading tag from address: {Address}", address);

        // [EXISTING IMPLEMENTATION - DO NOT MODIFY]

        _logger.LogDebug("Successfully read tag: {Address}", address);
        return result;
    }
}
```

Add logging calls without changing existing method signatures or behavior.

## 🔧 Build Validation & Troubleshooting

### Mandatory Build Validation Pattern
**ALWAYS** validate build after any changes to prevent compilation issues:

```bash
# REQUIRED: Validate build after changes
cd src
dotnet build --verbosity quiet

# Expected result: Build succeeded with warnings only (no errors)
```

### Common Compilation Issues & Solutions

#### Issue: 101+ "using" statement errors
**Cause**: Missing `<ImplicitUsings>enable</ImplicitUsings>` in .csproj files
**Solution**: Add ImplicitUsings to ALL project files

#### Issue: DispatcherOperation ConfigureAwait errors
**Cause**: Avalonia DispatcherOperation doesn't support ConfigureAwait
**Solution**: Remove `.ConfigureAwait(false)` from Dispatcher calls

#### Issue: CA1805 warnings about unnecessary initialization
**Cause**: Fields initialized to default values explicitly
**Solution**: Remove explicit initialization or suppress warning

```csharp
// WRONG: Explicit default initialization
private bool _isSelected = false;
private int _order = 0;

// CORRECT: Let compiler handle defaults
private bool _isSelected;
private int _order;
```

### Project Structure Validation Pattern
**ALWAYS** ensure consistent project structure:

```
src/
├── S7Tools/                          # Main UI project
│   ├── S7Tools.csproj                # ImplicitUsings=true
├── S7Tools.Core/                     # Core models
│   ├── S7Tools.Core.csproj           # ImplicitUsings=true
├── S7Tools.Infrastructure.Logging/   # Logging infrastructure
│   ├── S7Tools.Infrastructure.Logging.csproj # ImplicitUsings=true
└── S7Tools.sln                      # All projects referenced
```

### Solution File Validation Pattern
**ALWAYS** ensure all projects are properly referenced in solution:

```xml
<!-- REQUIRED: All projects must be in solution -->
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "S7Tools", "S7Tools\S7Tools.csproj"
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "S7Tools.Core", "S7Tools.Core\S7Tools.Core.csproj"
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "S7Tools.Infrastructure.Logging", "S7Tools.Infrastructure.Logging\S7Tools.Infrastructure.Logging.csproj"
```
## Lessons Learned
1. **Avalonia XAML Differences**: Some WPF/UWP properties don't exist in Avalonia (Watermark, ElementStyle)
2. **DataGrid Styling**: Avalonia DataGrid requires different styling approaches than other frameworks
3. **Thread Safety**: Critical for real-time log updates in UI applications
4. **Service Integration**: Proper service contracts enable seamless feature integration

### Package Reference Validation Pattern
**ALWAYS** ensure consistent package versions across projects:

```xml
<!-- REQUIRED: Consistent Microsoft.Extensions.Logging versions -->
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
```

## 🚨 CRITICAL CHECKLIST

Before any code commit, **ALWAYS** verify:

- [ ] ✅ All .csproj files have `<ImplicitUsings>enable</ImplicitUsings>`
- [ ] ✅ All .csproj files have `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>`
- [ ] ✅ .editorconfig contains memory optimization rules
- [ ] ✅ `dotnet build` succeeds with warnings only (no errors)
- [ ] ✅ All projects referenced in solution file
- [ ] ✅ No `ConfigureAwait(false)` on Avalonia Dispatcher calls
- [ ] ✅ Use `is null` and `is not null` for null checks
- [ ] ✅ Circular buffer limits implemented for collections
- [ ] ✅ Proper disposal patterns for all resources

**FAILURE TO FOLLOW THIS CHECKLIST WILL RESULT IN COMPILATION ERRORS AND PROJECT DELAYS**
