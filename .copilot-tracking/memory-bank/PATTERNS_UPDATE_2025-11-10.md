# System Patterns Update — Memory Region Integration (2025-11-10)

## Overview

This document captures new patterns and architectural improvements introduced during the Memory Region Profile integration (branch: 008-memory-regions-profiling).

**Integration Date**: 2025-11-10
**Related Commits**: a4699d3 through 706f016
**Status**: Ready to merge into systemPatterns.md

---

## 1. Memory Region Profile Management Pattern

### 1.1 Core Concepts

**Purpose**: Manage PLC memory region profiles with comprehensive validation, import/export, and path management.

**Key Components**:
- `MemoryMappingProfile` — Domain model for memory region configurations
- `MemorySegment` — Individual memory segment with validation
- `IMemoryRegionProfileService` — Service contract
- `MemoryRegionProfileService` — Service implementation
- `MemoryRegionSettingsViewModel` — Settings UI integration

### 1.2 Implementation Pattern

```csharp
// Memory Region Profile Model
public class MemoryMappingProfile : IProfileBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ObservableCollection<MemorySegment> Segments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsReadOnly { get; set; }

    // Factory method for default profiles
    public static MemoryMappingProfile CreateDefaultProfile()
    {
        return new MemoryMappingProfile
        {
            Name = "S7-1200 Default Memory Map",
            Description = "Default memory segments for Siemens S7-1200 PLC",
            Segments = new ObservableCollection<MemorySegment>
            {
                new MemorySegment { Name = "Program", StartAddress = 0x08000000, EndAddress = 0x0803FFFF },
                new MemorySegment { Name = "Data", StartAddress = 0x20000000, EndAddress = 0x20007FFF }
            }
        };
    }
}
```

### 1.3 Service Registration

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddS7ToolsProfileServices(this IServiceCollection services)
{
    // Register memory region profile service
    services.TryAddSingleton<IMemoryRegionProfileService, MemoryRegionProfileService>();

    // Register with StandardProfileManager pattern
    services.TryAddSingleton<StandardProfileManager<MemoryMappingProfile>>(sp =>
        new StandardProfileManager<MemoryMappingProfile>(
            sp.GetRequiredService<IApplicationSettingsService>(),
            sp.GetRequiredService<ILogger<StandardProfileManager<MemoryMappingProfile>>>(),
            "profiles.memory-regions",
            MemoryMappingProfile.CreateDefaultProfile
        )
    );

    return services;
}
```

### 1.4 Dialog Integration Pattern

```csharp
// Create Dialog
protected override async Task<ProfileDialogResult<MemoryMappingProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
{
    // Step 1: Get profile name via input dialog
    var nameResult = await _dialogService.ShowInputDialogAsync(
        "Create Memory Region Profile",
        "Enter profile name:",
        request.SuggestedName ?? "New Profile"
    );

    if (!nameResult.IsSuccess)
        return ProfileDialogResult<MemoryMappingProfile>.Cancelled();

    // Step 2: Create default profile with entered name
    var profile = MemoryMappingProfile.CreateDefaultProfile();
    profile.Name = nameResult.Result;

    // Step 3: Save profile
    var savedProfile = await _profileService.CreateAsync(profile);

    return ProfileDialogResult<MemoryMappingProfile>.Success(savedProfile);
}

// Edit Dialog
protected override async Task<ProfileDialogResult<MemoryMappingProfile>> ShowEditDialogAsync(ProfileEditRequest<MemoryMappingProfile> request)
{
    // Create specialized edit dialog
    var dialogViewModel = new EditMemoryRegionProfileDialogViewModel(
        request.ProfileToEdit,
        _loggerFactory.CreateLogger<EditMemoryRegionProfileDialogViewModel>()
    );

    var dialog = new EditMemoryRegionProfileDialog { DataContext = dialogViewModel };

    // Show dialog and get result
    var result = await _uiThreadService.InvokeAsync(async () =>
    {
        var window = GetMainWindow();
        return await dialog.ShowDialog<bool>(window);
    });

    if (result)
    {
        var updatedProfile = dialogViewModel.CreateUpdatedProfile();
        var savedProfile = await _profileService.UpdateAsync(updatedProfile);
        return ProfileDialogResult<MemoryMappingProfile>.Success(savedProfile);
    }

    return ProfileDialogResult<MemoryMappingProfile>.Cancelled();
}
```

---

## 2. Job Wizard Pattern

### 2.1 Multi-Step Wizard Architecture

**Purpose**: Guide users through complex job profile creation with validation at each step.

**Key Components**:
- `JobWizardViewModel` — Main wizard coordinator
- `JobWizardMemoryRegionStepViewModel` — Memory region selection step
- Step-based navigation with validation
- Fallback mechanism for graceful degradation

### 2.2 Implementation Pattern

```csharp
public class JobWizardViewModel : ViewModelBase
{
    // Step ViewModels
    public JobWizardMemoryRegionStepViewModel MemoryRegionStep { get; }

    // Navigation
    public ObservableCollection<object> WizardSteps { get; } = new();
    private object? _currentStep;
    public object? CurrentStep
    {
        get => _currentStep;
        set => this.RaiseAndSetIfChanged(ref _currentStep, value);
    }

    // Validation
    public IEnumerable<string> ValidateCurrentStep()
    {
        return CurrentStep switch
        {
            JobWizardMemoryRegionStepViewModel step => step.Validate(),
            _ => Enumerable.Empty<string>()
        };
    }

    // Job Creation
    public async Task<JobProfile?> CreateJobAsync()
    {
        // Collect data from all steps
        var job = new JobProfile
        {
            Name = JobName,
            Description = JobDescription,
            SerialProfileId = SelectedSerial?.Id,
            SocatProfileId = SelectedSocat?.Id,
            PowerSupplyProfileId = SelectedPower?.Id,
            MemoryRegionProfileId = MemoryRegionStep.SelectedProfile?.Id,
            SelectedSegmentId = MemoryRegionStep.SelectedSegment?.Id
        };

        // Save via JobManager
        var created = await _jobManager.CreateAsync(job);
        return created;
    }
}
```

### 2.3 Fallback Mechanism

```csharp
// In JobsManagementViewModel.cs
private object CreateNewJobViewModel()
{
    try
    {
        // Try to create full wizard
        if (_viewModelFactory != null)
        {
            return _viewModelFactory.Create<JobWizardViewModel>();
        }

        // Fallback to placeholder
        return new JobWizardPlaceholderViewModel(
            "Create New Job",
            "Use the job creation wizard to create a new job profile with guided setup."
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create job wizard");
        return CreateMainJobsContentViewModel(); // Ultimate fallback
    }
}
```

**IMPORTANT**: `JobWizardPlaceholderViewModel` is NOT dead code—it's an essential fallback mechanism for graceful degradation when the full wizard cannot be instantiated.

---

## 3. Application Settings Service Pattern

### 3.1 Purpose

Centralized, strongly-typed settings management with:
- Type-safe access to all application settings
- Change notifications via events
- Path resolution integration
- Persistence to configuration file

### 3.2 Implementation

```csharp
public interface IApplicationSettingsService
{
    // Settings access
    T GetSetting<T>(string key, T defaultValue = default);
    void SetSetting<T>(string key, T value);

    // Path settings (with resolution)
    string GetProfilesPath(string profileType);
    void SetProfilesPath(string profileType, string path);

    // Change notifications
    event EventHandler<SettingChangedEventArgs>? SettingsChanged;

    // Persistence
    Task SaveAsync();
    Task LoadAsync();
}
```

### 3.3 Settings Schema Convention

Use dot notation for hierarchical keys:

```csharp
// Logging settings
"logging.enabled"                  // bool
"logging.minLevel"                 // string (LogLevel)
"logging.maxEntries"               // int

// UI settings
"ui.theme"                         // string
"ui.showGridLines"                 // bool
"ui.enableAnimations"              // bool

// Profile paths
"profiles.serial"                  // string (file path)
"profiles.socat"                   // string (file path)
"profiles.power-supply"            // string (file path)
"profiles.memory-regions"          // string (file path)
"profiles.jobs"                    // string (file path)
```

### 3.4 ViewModel Integration

```csharp
public class MemoryRegionSettingsViewModel : ViewModelBase
{
    private readonly IApplicationSettingsService _settingsService;

    // Subscribe to settings changes
    public MemoryRegionSettingsViewModel(IApplicationSettingsService settingsService)
    {
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.Key == "profiles.memory-regions")
        {
            RefreshFromSettings();
        }
    }

    // Refresh UI from settings
    private void RefreshFromSettings()
    {
        var path = _settingsService.GetProfilesPath("memory-regions");
        var resolved = _pathService.ResolvePath(path);

        ProfilesPath = resolved;
        // ... update UI
    }
}
```

---

## 4. Path Service and Resource Management Pattern

### 4.1 Three-Tier Path Resolution

**Tiers**:
1. **Resolved Path** — Actual filesystem path (absolute)
2. **Profile-Specific Path** — User-configured path (may be relative)
3. **Default Path** — Fallback when no user path configured

```csharp
public interface IPathService
{
    // Resolution
    string ResolvePath(string path, string? basePath = null);
    bool IsPathValid(string path);

    // Default paths
    string GetDefaultProfilesDirectory(string profileType);

    // Path operations
    void EnsureDirectoryExists(string path);
    string GetRelativePath(string fromPath, string toPath);
}
```

### 4.2 Usage Pattern

```csharp
// In ViewModel
private void RefreshFromSettings()
{
    // Tier 2: Get user-configured path
    var configuredPath = _settingsService.GetProfilesPath("memory-regions");

    // Tier 1: Resolve to absolute path
    var resolvedPath = string.IsNullOrWhiteSpace(configuredPath)
        ? null
        : _pathService.ResolvePath(configuredPath);

    // Tier 3: Fall back to default if resolution fails
    if (resolvedPath == null || !_pathService.IsPathValid(resolvedPath))
    {
        resolvedPath = _pathService.GetDefaultProfilesDirectory("memory-regions");
    }

    ProfilesPath = resolvedPath;
}
```

---

## 5. Resource Coordinator Pattern (NEW)

### 5.1 Purpose

Coordinate parallel initialization of multiple profile services during application startup to improve startup time.

### 5.2 Implementation

```csharp
public interface IResourceCoordinator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class ResourceCoordinator : IResourceCoordinator
{
    private readonly ILogger<ResourceCoordinator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting parallel resource initialization");

        // Create initialization tasks
        var tasks = new[]
        {
            InitializeProfileService<ISerialPortProfileService>("SerialPort"),
            InitializeProfileService<ISocatProfileService>("Socat"),
            InitializeProfileService<IPowerSupplyProfileService>("PowerSupply"),
            InitializeProfileService<IMemoryRegionProfileService>("MemoryRegion"),
            InitializeProfileService<IJobManager>("Jobs")
        };

        // Execute in parallel
        await Task.WhenAll(tasks);

        _logger.LogInformation("Parallel resource initialization completed");
    }

    private async Task InitializeProfileService<T>(string serviceName) where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var service = _serviceProvider.GetRequiredService<T>();

            // If service has Initialize method, call it
            if (service is IAsyncInitializable initializable)
            {
                await initializable.InitializeAsync();
            }

            _logger.LogDebug("{ServiceName} initialized in {ElapsedMs}ms",
                serviceName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize {ServiceName}", serviceName);
            // Don't throw - allow other services to initialize
        }
    }
}
```

### 5.3 Registration and Usage

```csharp
// Registration in ServiceCollectionExtensions.cs
services.TryAddSingleton<IResourceCoordinator, ResourceCoordinator>();

// Usage in App.axaml.cs
public override async void OnFrameworkInitializationCompleted()
{
    var coordinator = _services.GetRequiredService<IResourceCoordinator>();
    await coordinator.InitializeAsync();

    base.OnFrameworkInitializationCompleted();
}
```

---

## 6. UI Refresh Service Pattern (NEW)

### 6.1 Purpose

Centralize UI refresh logic across ViewModels to ensure consistent collection updates and selection preservation.

### 6.2 Interface

```csharp
public interface IUIRefreshService
{
    // Refresh collection and preserve selection
    Task<T?> RefreshCollectionAsync<T>(
        ObservableCollection<T> collection,
        Func<Task<IEnumerable<T>>> fetchFunc,
        T? currentSelection = null,
        Func<T, T, bool>? matchPredicate = null
    ) where T : class;

    // Refresh and select by ID
    Task<T?> RefreshAndSelectByIdAsync<T>(
        ObservableCollection<T> collection,
        Func<Task<IEnumerable<T>>> fetchFunc,
        int? targetId
    ) where T : class, IProfileBase;

    // Refresh and select by name
    Task<T?> RefreshAndSelectByNameAsync<T>(
        ObservableCollection<T> collection,
        Func<Task<IEnumerable<T>>> fetchFunc,
        string? targetName
    ) where T : class, IProfileBase;
}
```

### 6.3 Usage Pattern

```csharp
// In ProfileManagementViewModel after Create/Edit/Delete
private async Task RefreshProfilesAfterCreateAsync(string createdName)
{
    SelectedProfile = await _uiRefreshService.RefreshAndSelectByNameAsync(
        Profiles,
        () => _profileService.GetAllAsync(),
        createdName
    );
}

private async Task RefreshProfilesAfterEditAsync(int editedId)
{
    SelectedProfile = await _uiRefreshService.RefreshAndSelectByIdAsync(
        Profiles,
        () => _profileService.GetAllAsync(),
        editedId
    );
}
```

---

## 7. Constants Organization Pattern (NEW)

### 7.1 Rationale

Extract all magic strings, numbers, and color values to typed constant classes for:
- **Maintainability** — Single source of truth
- **Type Safety** — Compile-time verification
- **Discoverability** — IntelliSense support
- **Consistency** — Shared values across codebase

### 7.2 Recommended Structure

```
src/S7Tools/Constants/
├── DateTimeFormats.cs       // Date/time format strings
├── NetworkConstants.cs      // Port ranges, timeouts
├── MemoryConstants.cs       // Memory addresses, sizes
├── ColorPalette.cs          // UI color values
├── StatusMessages.cs        // Common status messages
└── ValidationMessages.cs    // Validation error messages
```

### 7.3 Example Implementation

```csharp
// DateTimeFormats.cs
namespace S7Tools.Constants;

public static class DateTimeFormats
{
    public const string ShortDateTime = "yyyy-MM-dd HH:mm";
    public const string LongDateTime = "yyyy-MM-dd HH:mm:ss";
    public const string IsoDateTime = "yyyy-MM-ddTHH:mm:ssZ";
    public const string DateOnly = "yyyy-MM-dd";
}

// NetworkConstants.cs
namespace S7Tools.Constants;

public static class NetworkConstants
{
    public const int MinPort = 1;
    public const int MaxPort = 65535;
    public const int DefaultModbusPort = 502;
    public const int DefaultSocatPort = 50001;

    public static string GetPortRangeError() =>
        $"Port must be between {MinPort} and {MaxPort}";
}

// ColorPalette.cs
using Avalonia.Media;

namespace S7Tools.Constants;

public static class ColorPalette
{
    public static readonly Color WarningOrange = Color.FromRgb(255, 165, 0);
    public static readonly Color ErrorCrimson = Color.FromRgb(220, 20, 60);
    public static readonly Color InfoGreen = Color.FromRgb(60, 179, 113);
}
```

---

## 8. Custom Exception Hierarchy (ENHANCED)

### 8.1 New Exceptions Added

```csharp
// Memory Region specific
public class MemoryRegionException : S7ToolsException
{
    public MemoryRegionException(string message) : base(message) { }
    public MemoryRegionException(string message, Exception innerException)
        : base(message, innerException) { }
}

// Path resolution specific
public class PathResolutionException : S7ToolsException
{
    public string? AttemptedPath { get; }

    public PathResolutionException(string message, string? attemptedPath = null)
        : base(message)
    {
        AttemptedPath = attemptedPath;
    }
}

// Settings specific
public class SettingsLoadException : S7ToolsException
{
    public string? SettingsKey { get; }

    public SettingsLoadException(string message, string? settingsKey = null)
        : base(message)
    {
        SettingsKey = settingsKey;
    }
}
```

### 8.2 Usage Guidelines

```csharp
// GOOD: Use specific exception
throw new PathResolutionException(
    "Cannot resolve relative path without base path",
    attemptedPath: relativePath
);

// BAD: Use generic exception
throw new InvalidOperationException("Path resolution failed");
```

---

## 9. Integration Checklist for New Profile Types

When adding a new profile type to S7Tools, follow this checklist:

### 9.1 Core Domain (S7Tools.Core)

- [ ] Create domain model implementing `IProfileBase`
- [ ] Add service interface `I{Type}ProfileService`
- [ ] Add custom exceptions if needed
- [ ] Create default profile factory method
- [ ] Add validation logic

### 9.2 Service Implementation (S7Tools)

- [ ] Implement service using `StandardProfileManager<T>`
- [ ] Register service in `ServiceCollectionExtensions.cs`
- [ ] Add parallel initialization support
- [ ] Implement profile-specific validation

### 9.3 Settings Integration

- [ ] Add profile path to settings schema
- [ ] Register default path in `PathService`
- [ ] Add settings UI in appropriate category
- [ ] Implement path browsing and reset

### 9.4 UI Components

- [ ] Create ViewModel inheriting from `ProfileManagementViewModelBase<T>`
- [ ] Implement Create/Edit/Duplicate dialog ViewModels
- [ ] Create View with DataGrid and command buttons
- [ ] Add View to Settings or appropriate section

### 9.5 Resource Strings

- [ ] Add status messages to `UIStrings.resx`
- [ ] Add validation messages
- [ ] Add dialog titles and labels
- [ ] Verify fallback values in `UIStrings.cs`

### 9.6 Testing

- [ ] Unit tests for domain model
- [ ] Service tests (CRUD operations)
- [ ] ViewModel tests (command execution)
- [ ] Integration tests (end-to-end workflows)

---

## 10. Lessons Learned

### 10.1 Dialog Parent Resolution

**Issue**: Getting the main window for dialog parent can fail in certain lifecycle states.

**Solution**: Always wrap in try-catch and provide meaningful error message:

```csharp
Window GetMainWindow()
{
    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
        && desktop.MainWindow != null)
    {
        return desktop.MainWindow;
    }

    _logger.LogError("Could not get main window - ApplicationLifetime not available or MainWindow is null");
    throw new InvalidOperationException("Could not get main window for dialog parent");
}
```

### 10.2 ObservableCollection Refresh Pattern

**Issue**: DataGrid doesn't always update when items in collection change.

**Solution**: Replace the entire collection instance to force UI refresh:

```csharp
// BAD: May not trigger UI update
Profiles.Clear();
foreach (var profile in newProfiles)
{
    Profiles.Add(profile);
}

// GOOD: Always triggers UI update
var newProfiles = await _profileService.GetAllAsync();
Profiles.Clear();
Profiles.AddRange(newProfiles);  // Or recreate collection
```

### 10.3 Settings Refresh on Path Change

**Issue**: ViewModels don't automatically update when settings change.

**Solution**: Subscribe to `SettingsChanged` event and filter by key:

```csharp
_settingsService.SettingsChanged += OnSettingsChanged;

private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
{
    if (e.Key.StartsWith("profiles."))
    {
        RefreshFromSettings();
    }
}
```

### 10.4 Placeholder ViewModels as Fallbacks

**Lesson**: Placeholder ViewModels are NOT dead code—they provide graceful degradation when full features cannot be instantiated.

**Pattern**:
```csharp
try
{
    return _factory.Create<FullFeatureViewModel>();
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to create full feature, using placeholder");
    return new PlaceholderViewModel("Feature Unavailable", "The full feature could not be loaded.");
}
```

---

## Status and Next Steps

**Status**: ✅ All patterns documented and ready for integration

**Next Steps**:
1. Integrate this document into `systemPatterns.md` (Section 15)
2. Update `PATTERNS_REFERENCE.md` with concrete examples
3. Update `AGENTS.md` baseline notes
4. Create tasks for remaining hardcoded strings migration

**Related Documents**:
- `.copilot-tracking/CODE_QUALITY_REVIEW_2025-11-10.md`
- `specs/008-memory-regions-profiling/`
- `.copilot-tracking/memory-bank/systemPatterns.md`

---

**Document Version**: 1.0
**Last Updated**: 2025-11-10
**Author**: Code Quality Review Session
**Scope**: Patterns introduced in branch 008-memory-regions-profiling
