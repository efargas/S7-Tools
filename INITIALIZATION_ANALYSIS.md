# S7Tools Initialization Order Analysis - CORRECTED# S7Tools Initialization Order Analysis

**Date**: 2025-10-23 (Updated)

**Date**: 2025-10-23 (Updated)  **Branch**: 007-resources-paths-management

**Branch**: 007-resources-paths-management  **Analysis By**: GitHub Copilot

**Analysis By**: GitHub Copilot

## Summary

## Summary

✅ **Status Update**: Initial analysis incorrectly reported missing `App.axaml.cs` file. The file EXISTS and implements proper initialization. This document has been updated with correct findings.

✅ **CORRECTION**: The initial analysis incorrectly reported that `App.axaml.cs` was missing. **The file EXISTS and is properly implemented at `/home/kali/WS/S7-Tools/src/S7Tools/App.axaml.cs`**.

Review of initialization order, duplicate code, and synchronous/async patterns in the S7Tools application startup sequence.

This document reviews initialization order, identifies duplicate initialization patterns in --diag mode, and validates synchronous/async patterns.

## Current Architecture

---

### Service Dependencies (Bottom-Up)

## Current Architecture```

1. PathService (no dependencies, resolves paths from executable location)

### Service Dependencies (Bottom-Up)   ↓

2. ResourceManagerService (depends on: PathService)

```text   ↓

1. PathService (no dependencies, resolves paths from executable location)3. ApplicationSettingsService (depends on: PathService)

   ↓   ↓

2. ResourceManagerService (depends on: PathService)4. Profile Services (depend on: PathService, ApplicationSettingsService)

   ↓   - SerialPortProfileService

3. ApplicationSettingsService (depends on: PathService)   - SocatProfileService

   ↓   - PowerSupplyProfileService

4. Profile Services (depend on: PathService, ApplicationSettingsService)   - JobManager

   - SerialPortProfileService   ↓

   - SocatProfileService5. UI Services (depend on all above)

   - PowerSupplyProfileService   - ViewModels

   - JobManager   - Views

   ↓```

5. UI Services (depend on all above)

   - ViewModels## Issues Identified

   - Views

```### 1. ✅ App Class Implementation - EXISTS AND WORKING



---**Location**: `/home/kali/WS/S7-Tools/src/S7Tools/App.axaml.cs`



## Findings**Status**: File exists and implements proper initialization.



### 1. ✅ App.axaml.cs EXISTS and Works Correctly**Implementation Details**:

- Constructor: `App(IServiceProvider serviceProvider)` ✅

**Location**: `/home/kali/WS/S7-Tools/src/S7Tools/App.axaml.cs`- `Initialize()` method loads XAML and ResourceManager ✅

- `OnFrameworkInitializationCompleted()` implements synchronous path/settings initialization ✅

**Status**: ✅ File exists, properly implemented- Dialog interaction handlers registered properly ✅

- Global exception handler configured ✅

**Implementation**:

**Initialization Flow in App.axaml.cs**:

- Constructor: `public App(IServiceProvider serviceProvider)` ✅```csharp

- `Initialize()` method: Loads XAML and initializes UIStrings.ResourceManager ✅1. Constructor receives IServiceProvider

- `OnFrameworkInitializationCompleted()`: Calls `InitializePathAndSettingsSync()` ✅2. Initialize() → AvaloniaXamlLoader.Load() + UIStrings.ResourceManager setup

- `InitializePathAndSettingsSync(ILogger)`: Forces synchronous initialization of:3. OnFrameworkInitializationCompleted():

  1. PathService.InitializeAsync()   a. InitializePathAndSettingsSync():

  2. ResourceManagerService.InitializeResourcesAsync()      - PathService.InitializeAsync() (forced synchronous via GetAwaiter().GetResult())

  3. ApplicationSettingsService.LoadSettingsAsync()      - ResourceManagerService.InitializeResourcesAsync() (forced synchronous)

  4. FileLogWriter initialization      - ApplicationSettingsService.LoadSettingsAsync() (forced synchronous)

- Dialog interaction handlers registered ✅      - FileLogWriter initialization

- Global exception handler configured ✅   b. RegisterInteractionHandlers() for dialogs

   c. Create and set MainWindow

**Initialization Flow**:```



```csharp**Note**: Initial analysis incorrectly reported this file as missing. The file exists and works correctly.

Program.Main()

├─> ConfigureServices() - Register all services**Resolution Required**:

├─> BuildAvaloniaApp(serviceProvider) - Create App instanceCreate `/home/kali/WS/S7-Tools/src/S7Tools/App.axaml.cs` with proper initialization:

│   └─> new App(serviceProvider) - Constructor

│```csharp

├─> App.Initialize() [Avalonia lifecycle]using System;

│   └─> AvaloniaXamlLoader.Load(this)using System.Threading.Tasks;

│   └─> UIStrings.ResourceManager = resourceManagerusing Avalonia;

│using Avalonia.Controls.ApplicationLifetimes;

├─> App.OnFrameworkInitializationCompleted() [Avalonia lifecycle]using Avalonia.Markup.Xaml;

│   └─> InitializePathAndSettingsSync(logger) [SYNCHRONOUS - uses GetAwaiter().GetResult()]using Microsoft.Extensions.DependencyInjection;

│       ├─> PathService.InitializeAsync() → FORCED SYNCusing Microsoft.Extensions.Logging;

│       ├─> ResourceManagerService.InitializeResourcesAsync() → FORCED SYNCusing S7Tools.Core.Interfaces.Services;

│       ├─> ApplicationSettingsService.LoadSettingsAsync() → FORCED SYNCusing S7Tools.Services.Interfaces;

│       └─> FileLogWriter initializationusing S7Tools.Views;

│   └─> RegisterInteractionHandlers(dialogService, logger)

│   └─> desktop.MainWindow = GetRequiredService<MainWindow>()namespace S7Tools

│{

└─> Application starts with fully initialized services    /// <summary>

```    /// The main application class.

    /// </summary>

**✅ This implementation is CORRECT** - forces foundational services to initialize synchronously before UI starts.    public partial class App : Application

    {

---        private readonly IServiceProvider _serviceProvider;

        private ILogger<App>? _logger;

### 2. ⚠️ Confusing Initialization in --diag Mode (Low Priority)

        /// <summary>

**Location**: `Program.cs` lines 59-147        /// Initializes a new instance of the <see cref="App"/> class.

        /// </summary>

**Issue**: When running with `--diag` flag, `InitializeS7ToolsServicesAsync()` is called explicitly in Program.cs:        /// <param name="serviceProvider">The service provider.</param>

        public App(IServiceProvider serviceProvider)

```csharp        {

if (args != null && args.Length > 0 && args.Contains("--diag"))            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

{        }

    // Run initialization asynchronously for diagnostics

    await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);        /// <summary>

            /// Initializes the application.

    // Then manually test profile services...        /// </summary>

```        public override void Initialize()

        {

But `InitializeS7ToolsServicesAsync()` only initializes **profile services**, not foundational services:            AvaloniaXamlLoader.Load(this);

        }

```csharp

// ServiceCollectionExtensions.cs line 429        /// <summary>

public static async Task InitializeS7ToolsServicesAsync(this IServiceProvider serviceProvider)        /// Called when the Avalonia framework initialization is completed.

{        /// </summary>

    // Only initializes profile services in parallel        public override void OnFrameworkInitializationCompleted()

    await Task.WhenAll(        {

        layoutTask, themeTask, serialTask, socatTask, powerSupplyTask, jobTask            _logger = _serviceProvider.GetService<ILogger<App>>();

    ).ConfigureAwait(false);

}            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)

```            {

                // Initialize foundational services synchronously in correct order

**Impact**:                Task.Run(async () =>

                {

- Code is confusing - looks like services are initialized twice                    try

- **Actual behavior**: Foundational services (PathService, ResourceManager, ApplicationSettings) are NOT initialized in --diag mode before profile services are tested                    {

- This could cause --diag mode failures if profile services depend on foundational services                        await InitializeFoundationalServicesAsync();

                        await _serviceProvider.InitializeS7ToolsServicesAsync();

**Recommendation**:                    }

                    catch (Exception ex)

Option A: Explicitly initialize foundational services in --diag mode before calling `InitializeS7ToolsServicesAsync()`:                    {

                        _logger?.LogError(ex, "Failed to initialize S7Tools services");

```csharp                    }

if (args.Contains("--diag"))                }).Wait();

{

    // Initialize foundational services first                // Register dialog interaction handlers

    var pathService = serviceProvider.GetRequiredService<IPathService>();                var dialogService = _serviceProvider.GetService<IDialogService>();

    await pathService.InitializeAsync();                if (dialogService != null)

                    {

    var resourceService = serviceProvider.GetRequiredService<IResourceManagerService>();                    _logger?.LogDebug("Registering dialog interaction handlers");

    await resourceService.InitializeResourcesAsync();                    RegisterInteractionHandlers(dialogService, _logger);

                        _logger?.LogInformation("Dialog interaction handlers registered successfully");

    var settingsService = serviceProvider.GetRequiredService<IApplicationSettingsService>();                }

    await settingsService.LoadSettingsAsync();

                    desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();

    // Then initialize profile services

    await serviceProvider.InitializeS7ToolsServicesAsync();                _logger?.LogInformation("Application initialization completed successfully");

                }

    // Test profile services...

}            base.OnFrameworkInitializationCompleted();

```        }



Option B: Refactor `InitializeS7ToolsServicesAsync()` to include foundational services initialization.        private async Task InitializeFoundationalServicesAsync()

        {

---            _logger?.LogInformation("🔄 Starting synchronous path and settings initialization...");

            _logger?.LogInformation("🔄 Initializing path services and application settings synchronously");

### 3. ℹ️ Outdated Comment in Program.cs (Low Priority)

            // Step 1: Initialize PathService

**Location**: `Program.cs` lines 156-159            _logger?.LogDebug("Step 1: Initializing path service");

            var pathService = _serviceProvider.GetRequiredService<IPathService>();

**Current Comment**:            await pathService.InitializeAsync().ConfigureAwait(false);

            _logger?.LogInformation("✅ Path service initialized - Base directory: {BaseDirectory}", pathService.BaseDirectory);

```csharp

// Normal startup: initialize background services asynchronously without blocking the UI thread.            // Step 2: Initialize ResourceManager

// This avoids blocking startup hangs while still allowing services to initialize in the background.            _logger?.LogDebug("Step 2: Initializing resource manager");

_ = Task.Run(async () =>            var resourceManager = _serviceProvider.GetRequiredService<IResourceManagerService>();

{            await resourceManager.InitializeResourcesAsync().ConfigureAwait(false);

    await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);            _logger?.LogInformation("✅ Resource manager initialized - Created {ResourceCount} resources", 0);

```

            // Step 3: Load ApplicationSettings

**Issue**:             _logger?.LogDebug("Step 3: Loading application settings");

            var settingsService = _serviceProvider.GetRequiredService<IApplicationSettingsService>();

- This comment suggests background initialization to avoid blocking UI startup            await settingsService.LoadSettingsAsync().ConfigureAwait(false);

- **Reality**: Foundational services ARE initialized synchronously in `App.OnFrameworkInitializationCompleted()` via `InitializePathAndSettingsSync()`            _logger?.LogInformation("✅ Application settings loaded successfully");

- The background Task.Run only initializes **profile services**, not foundational services

            // Step 4: Initialize FileLogWriter

**Recommendation**: Update comment to clarify:            _logger?.LogDebug("Step 4: Initializing file logging service");

            var fileLogWriter = _serviceProvider.GetService<IFileLogWriter>();

```csharp            if (fileLogWriter != null)

// Normal startup: Initialize profile services asynchronously in the background            {

// without blocking the UI thread. Foundational services (PathService, ResourceManager,                await fileLogWriter.InitializeAsync().ConfigureAwait(false);

// ApplicationSettings) are initialized synchronously in App.OnFrameworkInitializationCompleted()                _logger?.LogInformation("✅ File logging service initialized and monitoring DataStore");

// to ensure proper startup order.            }

_ = Task.Run(async () =>

{            _logger?.LogInformation("🎉 Synchronous initialization completed successfully");

    await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);            _logger?.LogInformation("✅ Path and settings initialization completed successfully");

```        }



---        /// <summary>

        /// Registers interaction handlers for dialogs.

### 4. ✅ Correct: ServiceCollectionExtensions Implementation        /// </summary>

        private void RegisterInteractionHandlers(IDialogService dialogService, ILogger? logger)

**Location**: `ServiceCollectionExtensions.cs` line 429        {

            // Register any interaction handlers here if needed

**Status**: ✅ Correctly implements parallel profile service initialization            // This method is called during app initialization

        }

```csharp    }

public static async Task InitializeS7ToolsServicesAsync(this IServiceProvider serviceProvider)}

{```

    // Initialize layout and theme services

    Task layoutTask = layoutService.InitializeAsync();### 2. ⚠️ Duplicate Initialization in --diag Mode

    Task themeTask = themeService.InitializeAsync();

**Location**: `Program.cs` lines 41-64 vs line 64

    // Initialize profile services in parallel

    Task serialTask = serialService.GetAllAsync();**Problem**: When running with `--diag` flag, foundational services are initialized twice:

    Task socatTask = socatService.GetAllAsync();

    Task powerSupplyTask = powerSupplyService.GetAllAsync();```csharp

    Task jobTask = jobService.GetAllAsync();// First: Manual initialization (lines 41-60)

await pathService.InitializeAsync();

    await Task.WhenAll(layoutTask, themeTask, serialTask, socatTask, powerSupplyTask, jobTask)await resourceService.InitializeResourcesAsync();

        .ConfigureAwait(false);await settingsService.LoadSettingsAsync();

}

```// Then: Via extension method (line 64)

await serviceProvider.InitializeS7ToolsServicesAsync();

**✅ This is correct** - profile services have no interdependencies and can be initialized in parallel.// ^ This only initializes profile services, NOT foundational services

```

---

**Impact**:

## Correct Initialization Order- Misleading code - suggests double initialization but actually doesn't happen

- `InitializeS7ToolsServicesAsync()` only initializes profile services (Layout, Theme, SerialPort, Socat, PowerSupply, JobManager)

### Phase 1: Foundational Services (Sequential, SYNCHRONOUS)- Not a runtime issue, but confusing code maintenance



**Where**: `App.InitializePathAndSettingsSync()`  **Resolution**:

**When**: During `App.OnFrameworkInitializationCompleted()`  Either:

**Order**:1. Remove the manual initialization in --diag mode and rely on App class to do it

2. Or document clearly that `InitializeS7ToolsServicesAsync()` only handles profile services

1. **PathService.InitializeAsync()** - Creates directory structure, resolves BaseDirectory

2. **ResourceManagerService.InitializeResourcesAsync()** - Creates missing resource files### 3. ℹ️ Misleading Comment About Initialization Location

3. **ApplicationSettingsService.LoadSettingsAsync()** - Loads default + user settings

4. **FileLogWriter** - Starts monitoring DataStore for log file writing**Location**: `Program.cs` lines 156-159



### Phase 2: Profile Services (Parallel, ASYNCHRONOUS)**Current Comment**:

```csharp

**Where**: `ServiceCollectionExtensions.InitializeS7ToolsServicesAsync()`  // Note: Background service initialization was removed - services are now initialized

**When**: Background Task.Run after UI starts (or explicitly in --diag mode)  // synchronously in the correct order during App startup (App.axaml.cs) after

**Services** (order independent):// path service, resource manager, and application settings are ready.

```

- LayoutService

- ThemeService**Problem**:

- SerialPortProfileService- References `App.axaml.cs` which doesn't exist

- SocatProfileService- Claims services are initialized in App class, but actually happens in --diag mode only

- PowerSupplyProfileService

- JobManager**Resolution**: Update comment to reflect actual architecture once App class is created



### Phase 3: UI Initialization (Sequential)### 4. ✅ Correct: Extension Method Implementation



**Where**: `App.OnFrameworkInitializationCompleted()`  **Location**: `ServiceCollectionExtensions.cs` line 429

**After**: Phase 1 completes

**Order**:The `InitializeS7ToolsServicesAsync()` method correctly:

- Initializes services in parallel where safe (profile services)

1. RegisterInteractionHandlers() - Setup dialog handlers- Uses proper async/await patterns

2. Create MainWindow- Has comprehensive logging

3. Set desktop.MainWindow- Returns Task for proper async composition



---```csharp

public static async Task InitializeS7ToolsServicesAsync(this IServiceProvider serviceProvider)

## Synchronous vs Asynchronous Patterns{

    // Layout & Theme services (independent)

### ✅ Correct: Forced Synchronous Initialization    // Profile services (can run in parallel)

    var profileInitTasks = new List<Task>();

**Location**: `App.InitializePathAndSettingsSync()`

    // Serial, Socat, PowerSupply, JobManager

```csharp    await Task.WhenAll(profileInitTasks);  // ✅ Proper parallel async

Task<PathConfiguration> pathTask = pathService.InitializeAsync();}

PathConfiguration pathConfig = pathTask.GetAwaiter().GetResult(); // CORRECT - forces sync```

```

## Correct Initialization Order

**Why This Works**:

### Phase 1: Foundational Services (Sequential, MUST be in this order)

- Ensures foundational services complete BEFORE UI starts```

- Prevents race conditions where UI tries to access uninitialized services1. PathService.InitializeAsync()              - Creates directory structure

- Acceptable for startup (short duration)2. ResourceManagerService.InitializeResourcesAsync()  - Validates resources

3. ApplicationSettingsService.LoadSettingsAsync()     - Loads settings

### ✅ Correct: Background Async for Profile Services4. FileLogWriter.InitializeAsync()            - Starts file logging

```

**Location**: `Program.cs` lines 156-169

**Why Sequential**: Each service depends on the previous one's completion.

```csharp

_ = Task.Run(async () =>### Phase 2: Profile Services (Parallel, Order Independent)

{```csharp

    await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);Task.WhenAll(

```    LayoutService.LoadLayoutAsync(),

    ThemeService.LoadThemeConfigurationAsync(),

**Why This Works**:    SerialPortProfileService.GetAllAsync(),

    SocatProfileService.GetAllAsync(),

- Profile services don't need to be ready immediately    PowerSupplyProfileService.GetAllAsync(),

- UI can start while profiles load in background    JobManager.GetAllAsync()

- No race conditions since foundational services are already initialized)

```

---

**Why Parallel**: These services are independent and can initialize concurrently.

## Recommendations

### Phase 3: UI Initialization (Sequential)

### Priority 1: Fix --diag Mode Initialization```

1. DialogService - Register interaction handlers

**Issue**: --diag mode doesn't initialize foundational services before testing profile services2. MainWindow - Create and show main window

```

**Fix**: In `Program.cs`, add foundational service initialization before calling `InitializeS7ToolsServicesAsync()`:

## Async/Await Pattern Analysis

```csharp

if (args.Contains("--diag"))### ✅ Correct Patterns Found

{

    ILogger<Program>? logger = serviceProvider.GetService<ILogger<Program>>();1. **Service Extension Method** (ServiceCollectionExtensions.cs:429)

    try   ```csharp

    {   public static async Task InitializeS7ToolsServicesAsync(this IServiceProvider serviceProvider)

        // Initialize foundational services first (critical for --diag tests)   {

        logger?.LogInformation("[S7Tools] Initializing foundational services for diagnostics");       await Task.WhenAll(profileInitTasks).ConfigureAwait(false);  // ✅

           }

        var pathService = serviceProvider.GetRequiredService<IPathService>();   ```

        await pathService.InitializeAsync().ConfigureAwait(false);

        2. **PathService InitializeAsync** (PathService.cs:144)

        var resourceService = serviceProvider.GetRequiredService<IResourceManagerService>();   ```csharp

        await resourceService.InitializeResourcesAsync().ConfigureAwait(false);   public async Task<PathConfiguration> InitializeAsync()

           {

        var settingsService = serviceProvider.GetRequiredService<IApplicationSettingsService>();       foreach (string? directory in directoriesToCreate)

        await settingsService.LoadSettingsAsync().ConfigureAwait(false);       {

                   if (await EnsureDirectoryExistsAsync(directory).ConfigureAwait(false))  // ✅

        logger?.LogInformation("[S7Tools] Foundational services initialized, testing profile services");       }

           }

        // Now test profile services   ```

        await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);

        3. **ViewModels RefreshFromSettings** (Fixed in this session)

        // ... rest of --diag code   ```csharp

```   private void RefreshFromSettings()  // ✅ Synchronous - no await needed

   {

### Priority 2: Update Misleading Comments       string serialProfilePath = _settingsService.GetSetting<string>(...);

       ProfilesPath = _pathService.ResolvePath(...);  // ✅ Sync path resolution

**Location**: `Program.cs` lines 156-159   }

   ```

**Current**:

### ⚠️ Potential Issues

```csharp

// Normal startup: initialize background services asynchronously without blocking the UI thread.1. **Program.cs --diag mode** (Line 64)

```   ```csharp

   await serviceProvider.InitializeS7ToolsServicesAsync().ConfigureAwait(false);

**Recommended**:   ```

   Should also call foundational services initialization, or document that it's done above.

```csharp

// Normal startup: Initialize profile services asynchronously in the background.## Recommendations

// Foundational services (PathService, ResourceManager, ApplicationSettings) are already

// initialized synchronously in App.OnFrameworkInitializationCompleted() before UI starts.### Priority 1: CRITICAL - Create App.axaml.cs

```

**Action**: Create the missing App code-behind file with proper initialization sequence.

### Priority 3: Document Initialization Order

**File**: `/home/kali/WS/S7-Tools/src/S7Tools/App.axaml.cs`

Add this summary to `AGENTS.md` or `systemPatterns.md`:

See complete implementation above in Issue #1 resolution.

```markdown

## Initialization Order### Priority 2: Refactor --diag Mode Initialization



### Synchronous (Blocks UI Startup)**Action**: Consolidate initialization logic to avoid confusion.

1. PathService - Creates directory structure

2. ResourceManagerService - Creates missing resource files  **File**: `/home/kali/WS/S7-Tools/src/S7Tools/Program.cs`

3. ApplicationSettingsService - Loads settings

4. FileLogWriter - Starts log monitoring**Option A - Extract to Helper Method** (Recommended):

```csharp

### Asynchronous (Background)if (args != null && args.Length > 0 && args.Contains("--diag"))

- LayoutService, ThemeService{

- SerialPortProfileService, SocatProfileService    ILogger<Program>? logger = serviceProvider.GetService<ILogger<Program>>();

- PowerSupplyProfileService, JobManager    try

    {

**Implementation**: See `App.InitializePathAndSettingsSync()` in `App.axaml.cs`        logger?.LogInformation("[S7Tools] Initializing services for diagnostics");

```

        // Use the same initialization as App class

---        await InitializeAllServicesAsync(serviceProvider, logger);



## Conclusion        // Then run diagnostics

        await RunDiagnosticsAsync(serviceProvider, logger);

**✅ App.axaml.cs EXISTS** - Initial analysis was incorrect

**✅ Initialization order is correct** - Foundational services initialize synchronously before UI          logger?.LogInformation("[S7Tools] Diagnostics complete. Exiting due to --diag flag");

**✅ Async patterns are correct** - Profile services load in background          return;

**⚠️ --diag mode needs fix** - Missing foundational service initialization      }

**ℹ️ Comments need update** - Clarify what initializes where    catch (Exception ex)

    {

**Next Steps**:        logger?.LogError(ex, "[S7Tools] Startup diagnostics failed");

    }

1. Fix --diag mode to initialize foundational services first}

2. Update comments in Program.cs to clarify initialization strategy

3. Add initialization order documentation to Memory Bankprivate static async Task InitializeAllServicesAsync(IServiceProvider serviceProvider, ILogger? logger)

{

    // Foundational services (sequential)
    var pathService = serviceProvider.GetRequiredService<IPathService>();
    await pathService.InitializeAsync();

    var resourceService = serviceProvider.GetRequiredService<IResourceManagerService>();
    await resourceService.InitializeResourcesAsync();

    var settingsService = serviceProvider.GetRequiredService<IApplicationSettingsService>();
    await settingsService.LoadSettingsAsync();

    // Profile services (parallel)
    await serviceProvider.InitializeS7ToolsServicesAsync();
}

private static async Task RunDiagnosticsAsync(IServiceProvider serviceProvider, ILogger? logger)
{
    // All the diagnostic logging code currently in --diag block
    ...
}
```

### Priority 3: Update Documentation Comments

**Action**: Fix misleading comments in Program.cs

**Change**:
```csharp
// OLD:
// Note: Background service initialization was removed - services are now initialized
// synchronously in the correct order during App startup (App.axaml.cs) after
// path service, resource manager, and application settings are ready.

// NEW:
// Note: Service initialization follows a strict order:
// 1. Foundational services (PathService, ResourceManager, ApplicationSettings) - Sequential
// 2. Profile services (Serial, Socat, PowerSupply, Jobs) - Parallel via InitializeS7ToolsServicesAsync()
// 3. UI services (Dialogs, Windows) - After all services ready
// See App.axaml.cs for the complete initialization sequence.
```

### Priority 4: Add Initialization Guard

**Action**: Prevent accidental re-initialization

**File**: `ServiceCollectionExtensions.cs`

```csharp
private static bool _isInitialized = false;
private static readonly object _initLock = new object();

public static async Task InitializeS7ToolsServicesAsync(this IServiceProvider serviceProvider)
{
    lock (_initLock)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("S7Tools services have already been initialized");
        }
        _isInitialized = true;
    }

    // Rest of initialization...
}
```

## Testing Checklist

After implementing fixes:

- [ ] Application starts without errors
- [ ] PathService initializes before other services
- [ ] Profile services load correctly
- [ ] --diag mode works and shows all services initialized
- [ ] No duplicate initialization logs
- [ ] Settings paths resolve correctly (from earlier fix)
- [ ] MainWindow displays correctly
- [ ] No async/await warnings or deadlocks

## Conclusion

**Main Issues**:
1. ❌ **Critical**: Missing `App.axaml.cs` file
2. ⚠️ **Medium**: Confusing/duplicate code in --diag mode
3. ℹ️ **Low**: Misleading comments

**Correct Patterns**:
✅ Service dependency order is architecturally sound
✅ Async/await usage is correct throughout
✅ Profile services properly initialize in parallel
✅ Path resolution fix (from earlier) is correct

**Next Steps**:
1. Create `App.axaml.cs` with proper initialization
2. Refactor --diag mode for clarity
3. Update documentation comments
4. Test thoroughly

---

**Note**: This analysis complements the path resolution fix completed earlier in this session. Both fixes should be applied together for a complete solution.
