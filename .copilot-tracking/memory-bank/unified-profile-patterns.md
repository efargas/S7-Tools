# Unified Profile Management Patterns

**Created**: October 14, 2025 - TASK008 Complete, TASK009 Phase 1 Complete
**Context Type**: Architectural patterns and migration implementation guidelines
**Status**: Phase 1 Complete - Foundation Established, Replication Ready

## 🎉 Phase 1 Achievement Summary

### **Complete Architectural Foundation (SUCCESSFUL)**

✅ **IUnifiedProfileDialogService Interface**: 272-line complete contract for all profile dialog operations
✅ **UnifiedProfileDialogService Implementation**: 350+ lines implementing adapter pattern with type-safe delegation
✅ **SerialPortsSettingsViewModel Migration**: Successfully inherits from ProfileManagementViewModelBase<SerialPortProfile>
✅ **Template Method Pattern Integration**: All 7 abstract methods implemented with proven success
✅ **Build and Runtime Verification**: Clean compilation (0 errors) and successful application startup
✅ **Zero Regression**: All CRUD operations, UI bindings, and command enablement preserved

### **Migration Success Metrics Achieved**

- **Code Unification**: Template method pattern provides 440+ lines of standardized infrastructure
- **Adapter Pattern Success**: Maintained 9 existing dependencies through composition while gaining template benefits
- **Type Safety**: Generic constraints ensure compile-time verification across all profile operations
- **Architectural Consistency**: Clean Architecture and SOLID principles maintained throughout migration

## 🚀 Proven Migration Pattern (Ready for Replication)

### **Step-by-Step Migration Blueprint**

The SerialPortsSettingsViewModel migration provides the proven blueprint for SocatSettingsViewModel and PowerSupplySettingsViewModel. Follow this exact pattern:

#### **Step 1: Update Class Declaration**
```csharp
// BEFORE: ViewModelBase inheritance
public class SocatSettingsViewModel : ViewModelBase, IDisposable

// AFTER: ProfileManagementViewModelBase inheritance
public class SocatSettingsViewModel : ProfileManagementViewModelBase<SocatProfile>, IDisposable
// Note: Remove IDisposable from declaration (base class provides it)
```

#### **Step 2: Add Required Dependencies**
```csharp
// Add to field declarations
private readonly IUnifiedProfileDialogService _unifiedDialogService;

// Add to constructor parameters (before logger parameter)
IUnifiedProfileDialogService unifiedProfileDialogService,

// Initialize in constructor
_unifiedDialogService = unifiedProfileDialogService ?? throw new ArgumentNullException(nameof(unifiedProfileDialogService));
```

#### **Step 3: Remove Conflicting Properties**
Remove these properties (provided by base class):
```csharp
// REMOVE - Base class provides these
public ObservableCollection<SocatProfile> Profiles { get; }
public SocatProfile? SelectedProfile { get; set; }
public bool IsLoading { get; set; }
public string StatusMessage { get; set; }
public string ProfilesPath { get; set; }
```

#### **Step 4: Implement Abstract Methods**
```csharp
#region Abstract Method Implementations

protected override IProfileManager<SocatProfile> GetProfileManager()
{
    return _socatProfileService; // Use existing service field
}

protected override string GetDefaultProfileName()
{
    return "SocatDefault"; // Standardized naming
}

protected override string GetProfileTypeName()
{
    return "Socat"; // Human-readable type name
}

protected override SocatProfile CreateDefaultProfile()
{
    return SocatProfile.CreateDefaultProfile(); // Use existing method
}

protected override async Task<ProfileDialogResult<SocatProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
{
    return await _unifiedDialogService.ShowSocatCreateDialogAsync(request).ConfigureAwait(false);
}

protected override async Task<ProfileDialogResult<SocatProfile>> ShowEditDialogAsync(ProfileEditRequest request)
{
    return await _unifiedDialogService.ShowSocatEditDialogAsync(request).ConfigureAwait(false);
}

protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
{
    return await _unifiedDialogService.ShowSocatDuplicateDialogAsync(request).ConfigureAwait(false);
}

#endregion
```

#### **Step 5: Update Logger Access**
```csharp
// Replace all instances of _logger with _specificLogger (if you have a specific logger field)
// OR use constructor parameter injection for the logger and store in private field

private readonly ILogger<SocatSettingsViewModel> _specificLogger;

// In constructor
_specificLogger = logger ?? throw new ArgumentNullException(nameof(logger));

// Replace usage throughout class
_specificLogger.LogInformation("...");  // Instead of _logger.LogInformation
```

#### **Step 6: Override Dispose Method**
```csharp
#region Disposal Overrides

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        try
        {
            _disposables?.Dispose(); // Dispose local disposables
            if (_settingsChangedHandler != null)
            {
                _settingsService.SettingsChanged -= _settingsChangedHandler;
            }
        }
        catch { }
    }

    base.Dispose(disposing); // Call base class disposal
}

#endregion
```

#### **Step 7: Update SettingsViewModel Constructor Call**
```csharp
// In SettingsViewModel.cs CreateSocatSettingsViewModel method
private SocatSettingsViewModel CreateSocatSettingsViewModel()
{
    // ... existing service retrievals ...
    var unifiedProfileDialogService = _serviceProvider.GetRequiredService<IUnifiedProfileDialogService>();
    var logger = _serviceProvider.GetRequiredService<ILogger<SocatSettingsViewModel>>();

    return new SocatSettingsViewModel(
        // ... existing parameters ...,
        unifiedProfileDialogService,  // Add before logger
        logger
    );
}
```

### **Complete Migration Examples**

#### **SocatSettingsViewModel Migration Example**
```csharp
// Constructor signature after migration
public SocatSettingsViewModel(
    ISocatProfileService socatProfileService,
    ISocatService socatService,
    ISerialPortService serialPortService,
    IDialogService dialogService,
    IProfileEditDialogService profileEditDialogService,
    IClipboardService clipboardService,
    IFileDialogService? fileDialogService,
    S7Tools.Services.Interfaces.ISettingsService settingsService,
    S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
    IUnifiedProfileDialogService unifiedProfileDialogService,  // NEW
    ILogger<SocatSettingsViewModel> logger)
    : base(logger, unifiedProfileDialogService, uiThreadService)  // NEW base call
{
    // Initialize all existing dependencies exactly as before
    _socatProfileService = socatProfileService ?? throw new ArgumentNullException(nameof(socatProfileService));
    _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
    // ... all other initializations remain identical ...
    _unifiedDialogService = unifiedProfileDialogService ?? throw new ArgumentNullException(nameof(unifiedProfileDialogService));

    // All existing initialization code remains unchanged
    // Initialize collections, commands, etc. exactly as before
}
```

#### **PowerSupplySettingsViewModel Migration Example**
```csharp
// Class declaration
public class PowerSupplySettingsViewModel : ProfileManagementViewModelBase<PowerSupplyProfile>

// Abstract method implementations
protected override IProfileManager<PowerSupplyProfile> GetProfileManager()
{
    return _powerSupplyProfileService;
}

protected override string GetDefaultProfileName()
{
    return "PowerSupplyDefault";
}

protected override string GetProfileTypeName()
{
    return "Power Supply";
}

protected override PowerSupplyProfile CreateDefaultProfile()
{
    return PowerSupplyProfile.CreateDefaultProfile();
}

// Dialog method implementations follow same pattern as above
protected override async Task<ProfileDialogResult<PowerSupplyProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
{
    return await _unifiedDialogService.ShowPowerSupplyCreateDialogAsync(request).ConfigureAwait(false);
}

// ... etc.
```

## Architecture Overview

### **Core Interface Foundation (759 lines implemented)**

The unified profile management architecture establishes consistent patterns across all profile types (Serial, Socat, PowerSupply) through a comprehensive interface system:

```csharp
// Base interface implemented by all profiles
public interface IProfileBase
{
    // Core identity and metadata
    int Id { get; set; }
    string Name { get; set; }
    string Description { get; set; }

    // Command configuration
    string Options { get; set; }        // Command options/flags
    string Flags { get; set; }          // Additional flags

    // Audit trail
    DateTime CreatedAt { get; set; }    // Creation timestamp
    DateTime ModifiedAt { get; set; }   // Last modification

    // State management
    bool IsDefault { get; set; }
    bool IsReadOnly { get; set; }

    // Business logic methods
    bool CanModify();
    bool CanDelete();
    string GetSummary();
    IProfileBase Clone();
}
```

### **Service Layer Standards**

All profile services implement unified CRUD operations:

```csharp
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
```

### **ViewModel Inheritance Pattern**

Base ViewModel implementing template method pattern:

```csharp
public abstract class ProfileManagementViewModelBase<TProfile> : ViewModelBase, IDisposable
    where TProfile : class, IProfileBase
{
    // Template methods for customization
    protected abstract Task<IEnumerable<TProfile>> LoadProfilesAsync();
    protected abstract string GetDefaultProfileName();
    protected abstract TProfile CreateDefaultProfile();
    protected abstract Task<bool> ShowProfileEditDialogAsync(TProfile profile);
    protected abstract Task<string?> ShowProfileNameInputDialogAsync(string currentName);

    // Common CRUD operations
    protected async Task CreateProfileAsync();
    protected async Task EditProfileAsync();
    protected async Task DuplicateProfileAsync();
    protected async Task DeleteProfileAsync();
    protected async Task SetDefaultProfileAsync();
}
```

## Implementation Standards

### **UI Standardization Requirements**

#### **CRUD Button Order (Mandatory)**
Create - Edit - Duplicate - Default - Delete - Refresh

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
  <Button Command="{Binding CreateProfileCommand}" Background="#28A745">Create</Button>
  <Button Command="{Binding EditProfileCommand}" Background="#0E639C">Edit</Button>
  <Button Command="{Binding DuplicateProfileCommand}" Background="#0E639C">Duplicate</Button>
  <Button Command="{Binding SetDefaultProfileCommand}" Background="#0E639C">Default</Button>
  <Button Command="{Binding DeleteProfileCommand}" Background="#D13438">Delete</Button>
  <Button Command="{Binding RefreshProfilesCommand}" Background="Transparent">Refresh</Button>
</StackPanel>
```

#### **Enhanced DataGrid Layout**
ID column first, complete metadata columns:

```xml
<DataGrid.Columns>
  <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="60" IsReadOnly="True"/>
  <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="200" IsReadOnly="True"/>
  <DataGridTextColumn Header="Description" Binding="{Binding Description}" Width="250" IsReadOnly="True"/>
  <DataGridTextColumn Header="Options" Binding="{Binding Options}" Width="150" IsReadOnly="True"/>
  <DataGridTextColumn Header="Flags" Binding="{Binding Flags}" Width="120" IsReadOnly="True"/>
  <DataGridTextColumn Header="Created" Binding="{Binding CreatedAt, StringFormat=yyyy-MM-dd}" Width="100" IsReadOnly="True"/>
  <DataGridTextColumn Header="Modified" Binding="{Binding ModifiedAt, StringFormat=yyyy-MM-dd}" Width="100" IsReadOnly="True"/>
  <DataGridCheckBoxColumn Header="Default" Binding="{Binding IsDefault}" Width="80" IsReadOnly="True"/>
  <DataGridCheckBoxColumn Header="Read-Only" Binding="{Binding IsReadOnly}" Width="90" IsReadOnly="True"/>
</DataGrid.Columns>
```

#### **Dialog-Only Operations**
- **Create**: Opens dialog with default values pre-populated
- **Edit**: Opens dialog with existing data, preserves ID
- **Duplicate**: Name input dialog → direct list addition (no edit dialog)

### **Validation Patterns**

#### **Name Uniqueness Validation**
```csharp
public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null)
{
    return !_profiles.Any(p => p.Id != excludeId &&
                          string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
}
```

#### **ID Assignment Algorithm**
```csharp
public async Task<int> GetNextAvailableIdAsync()
{
    var existingIds = _profiles.Select(p => p.Id).OrderBy(id => id).ToList();

    // Find first gap or next sequential ID
    for (int i = 1; i <= existingIds.Count + 1; i++)
    {
        if (!existingIds.Contains(i))
        {
            return i;
        }
    }

    return existingIds.Count + 1; // Fallback
}
```

#### **Metadata Management**
```csharp
// In CreateAsync
profile.CreatedAt = DateTime.UtcNow;
profile.ModifiedAt = DateTime.UtcNow;
profile.Version = "1.0";

// In UpdateAsync
profile.ModifiedAt = DateTime.UtcNow;
```

### **Thread Safety Patterns**

#### **UI Thread Marshaling (Critical)**
Always use IUIThreadService for collection updates:

```csharp
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

### **Service Implementation Pattern**

#### **Metadata Initialization**
```csharp
public async Task<T> CreateAsync(T profile, CancellationToken cancellationToken = default)
{
    try
    {
        // Validate input
        ValidateProfile(profile);

        // Ensure unique name
        profile.Name = await EnsureUniqueNameAsync(profile.Name, excludeId: null);

        // Assign new ID
        profile.Id = await GetNextAvailableIdAsync();

        // Set metadata
        profile.CreatedAt = DateTime.UtcNow;
        profile.ModifiedAt = DateTime.UtcNow;
        profile.Version = "1.0";

        // Save and return
        await SaveProfileAsync(profile);
        return profile;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating profile {ProfileName}", profile.Name);
        throw;
    }
}
```

#### **Thread-Safe Service Operations**
```csharp
// Single semaphore acquisition per call chain
private readonly SemaphoreSlim _semaphore = new(1, 1);

public async Task<T> CreateAsync(T profile, CancellationToken cancellationToken = default)
{
    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
        // Direct collection access inside protected block
        if (_profiles.Any(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Profile name '{profile.Name}' already exists");
        }

        // Business logic...
        return profile;
    }
    finally
    {
        _semaphore.Release();
    }
}
```

## Dialog Service Enhancement

### **Request/Response Pattern**
```csharp
public class ProfileCreateRequest<T> where T : class, IProfileBase
{
    public string Title { get; set; } = string.Empty;
    public T DefaultProfile { get; set; } = default!;
    public bool PrePopulateDefaults { get; set; } = true;
}

public class ProfileEditResult<T> where T : class, IProfileBase
{
    public bool IsSuccess { get; set; }
    public T? Profile { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCancelled => !IsSuccess && string.IsNullOrEmpty(ErrorMessage);
}
```

### **Dialog Service Interface**
```csharp
public interface IUnifiedProfileDialogService
{
    Task<ProfileEditResult<SerialPortProfileViewModel>> CreateSerialProfileAsync();
    Task<ProfileEditResult<SerialPortProfileViewModel>> EditSerialProfileAsync(SerialPortProfile profile);
    Task<ProfileEditResult<string>> DuplicateSerialProfileAsync(SerialPortProfile sourceProfile);

    // Similar methods for Socat and PowerSupply profiles
}
```

## Phase Implementation Status

### **Phase 1 Complete (Architecture Design) ✅**
- ✅ All core interfaces implemented (759 lines)
- ✅ Base ViewModel with template method pattern
- ✅ Profile model updates (IProfileBase implementation)
- ✅ Thread-safe UI operations
- ✅ Build verification successful

### **Phase 2 Ready (Profile Model Enhancements)**
**Objective**: Add missing metadata to SerialPortProfile and SocatProfile

**Required Changes**:
```csharp
// Add to SerialPortProfile and SocatProfile
public string Options { get; set; } = string.Empty;
public string Flags { get; set; } = string.Empty;
public DateTime CreatedAt { get; set; }
public DateTime ModifiedAt { get; set; }
public string Version { get; set; } = "1.0";
```

**Service Updates**:
- Initialize metadata in CreateAsync/UpdateAsync
- Handle new properties in JSON serialization
- Maintain backward compatibility

### **Phases 3-10 Planned**
- Phase 3: Enhanced DataGrid Layout (ID column first, metadata columns)
- Phase 4: Remove Inline Input Fields (dialog-only operations)
- Phase 5: Standardize Button Layout (consistent CRUD button order)
- Phase 6-8: Dialog enhancement, validation, ViewModel updates
- Phase 9-10: Testing, quality review, documentation

## Quality Standards

### **Architecture Compliance**
- ✅ Clean Architecture (dependencies flow inward)
- ✅ SOLID Principles (SRP, DIP, OCP maintained)
- ✅ Domain-Driven Design (rich domain models)
- ✅ Template Method Pattern (customization points)

### **Code Quality Standards**
- ✅ XML documentation for all public APIs
- ✅ Comprehensive error handling with structured logging
- ✅ Thread-safe operations with proper synchronization
- ✅ ReactiveUI compliance (individual property subscriptions)

### **Testing Requirements**
- Unit tests for all CRUD operations
- Validation logic testing
- Thread safety verification
- UI behavior validation

## Common Pitfalls and Solutions

### **Thread Safety Issues**
❌ **Avoid**: RxApp.MainThreadScheduler.Schedule() usage
✅ **Use**: IUIThreadService.InvokeOnUIThreadAsync()

### **Semaphore Deadlocks**
❌ **Avoid**: Nested semaphore acquisitions
✅ **Use**: Single acquisition per call chain

### **ReactiveUI Constraints**
❌ **Avoid**: Large WhenAnyValue calls (12+ properties)
✅ **Use**: Individual property subscriptions

### **Service Implementation**
❌ **Avoid**: Exception swallowing
✅ **Use**: Comprehensive logging with proper error propagation

## Future Extension Guidelines

### **Adding New Profile Types**
1. Implement IProfileBase interface
2. Create IProfileManager<T> service
3. Inherit from ProfileManagementViewModelBase<T>
4. Follow established UI patterns
5. Add to IUnifiedProfileDialogService

### **Extending Validation**
1. Implement IProfileValidator<T>
2. Add business rule validation
3. Integrate with dialog real-time validation
4. Maintain consistent error messaging

---

**Document Status**: Complete architectural foundation documentation
**Next Update**: After Phase 2 completion (Profile Model Enhancements)
**Usage**: Reference guide for implementing unified profile management patterns
