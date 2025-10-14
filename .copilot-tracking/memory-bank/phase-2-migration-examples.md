# Phase 2 Migration Examples

**Status:** Ready for Implementation
**Created:** 2025-10-14
**Phase 1 Completed:** ✅ SerialPortsSettingsViewModel migrated successfully

## Overview

Phase 1 successfully completed the unified profile management foundation with:
- ✅ IUnifiedProfileDialogService interface (272 lines)
- ✅ UnifiedProfileDialogService implementation (350+ lines)
- ✅ SerialPortsSettingsViewModel migration to ProfileManagementViewModelBase
- ✅ Build verification: Clean compilation (0 errors)
- ✅ Runtime verification: All CRUD operations preserved

**Phase 2 Ready:** Apply identical proven pattern to remaining ViewModels

## SocatSettingsViewModel Migration Example

### Current State Analysis
```csharp
// Current: SocatSettingsViewModel.cs (1509 lines)
public class SocatSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly ISocatProfileService _profileService;
    private readonly ISocatService _socatService;
    private readonly ISerialPortService _serialPortService;
    private readonly IDialogService _dialogService;
    private readonly IProfileEditDialogService _profileEditDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<SocatSettingsViewModel> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IUIThreadService _uiThreadService;
    // + 1400+ lines of CRUD operations, property management, etc.
}
```

### Migration Steps

#### Step 1: Change Class Inheritance
```csharp
// BEFORE:
public class SocatSettingsViewModel : ViewModelBase, IDisposable

// AFTER:
public class SocatSettingsViewModel : ProfileManagementViewModelBase<SocatProfile>
```

#### Step 2: Update Constructor
```csharp
// BEFORE: 9 dependencies + manual registration
public SocatSettingsViewModel(
    ISocatProfileService profileService,
    ISocatService socatService,
    ISerialPortService serialPortService,
    IDialogService dialogService,
    IProfileEditDialogService profileEditDialogService,
    IClipboardService clipboardService,
    IFileDialogService? fileDialogService,
    ILogger<SocatSettingsViewModel> logger,
    ISettingsService settingsService,
    IUIThreadService uiThreadService)

// AFTER: 3 base dependencies + specific dependencies via composition
public SocatSettingsViewModel(
    IUnifiedProfileDialogService unifiedDialogService,
    ILogger<ProfileManagementViewModelBase<SocatProfile>> logger,
    IUIThreadService uiThreadService,
    ISocatProfileService profileService,
    ISocatService socatService,
    ISerialPortService serialPortService,
    IDialogService dialogService,
    IClipboardService clipboardService,
    IFileDialogService? fileDialogService,
    ISettingsService settingsService)
    : base(unifiedDialogService, logger, uiThreadService)
{
    _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
    _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
    _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
    _fileDialogService = fileDialogService;
    _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    _specificLogger = LoggerFactory.CreateLogger<SocatSettingsViewModel>();
}
```

#### Step 3: Implement Abstract Methods
```csharp
protected override IProfileManager<SocatProfile> GetProfileManager()
    => _profileService;

protected override string GetDefaultProfileName()
    => "New Socat Profile";

protected override string GetProfileTypeName()
    => "Socat";

protected override SocatProfile CreateDefaultProfile()
    => new()
    {
        Name = GetDefaultProfileName(),
        Description = "TCP to Serial proxy configuration",
        SourceAddress = "0.0.0.0",
        SourcePort = 502,
        SerialDevice = "/dev/ttyUSB0",
        BaudRate = 9600,
        DataBits = 8,
        StopBits = StopBits.One,
        Parity = Parity.None,
        Options = "-d -d",
        Flags = "-T 30"
    };

protected override async Task<SocatProfile?> ShowCreateDialogAsync()
{
    return await UnifiedDialogService.ShowSocatCreateDialogAsync().ConfigureAwait(false);
}

protected override async Task<SocatProfile?> ShowEditDialogAsync(SocatProfile profile)
{
    return await UnifiedDialogService.ShowSocatEditDialogAsync(profile).ConfigureAwait(false);
}

protected override async Task<SocatProfile?> ShowDuplicateDialogAsync(SocatProfile sourceProfile, string suggestedName)
{
    var duplicatedProfile = sourceProfile.Clone();
    duplicatedProfile.Name = suggestedName;
    return await UnifiedDialogService.ShowSocatCreateDialogAsync(duplicatedProfile).ConfigureAwait(false);
}
```

#### Step 4: Remove Duplicate Code
```csharp
// REMOVE: All base functionality now provided by ProfileManagementViewModelBase
// - Profiles property management (~50 lines)
// - SelectedProfile property management (~30 lines)
// - All CRUD command definitions (~100 lines)
// - Property change subscriptions (~50 lines)
// - Load/Save operations (~150 lines)
// - Error handling patterns (~75 lines)
// - Total reduction: ~455 lines of duplicate code
```

#### Step 5: Preserve Specific Functionality
```csharp
// KEEP: Socat-specific properties and operations
private readonly ISocatService _socatService;
private readonly ISerialPortService _serialPortService;

// Socat-specific methods
public async Task StartSocatAsync(SocatProfile profile) { /* existing implementation */ }
public async Task StopSocatAsync() { /* existing implementation */ }
public ObservableCollection<string> AvailableSerialDevices { get; } = new();

// Device discovery functionality preserved
private async Task RefreshSerialDevicesAsync() { /* existing implementation */ }
```

## PowerSupplySettingsViewModel Migration Example

### Current State Analysis
```csharp
// Current: PowerSupplySettingsViewModel.cs (1381 lines)
public class PowerSupplySettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IPowerSupplyProfileService _profileService;
    private readonly IPowerSupplyService _powerSupplyService;
    private readonly IDialogService _dialogService;
    private readonly IProfileEditDialogService _profileEditDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<PowerSupplySettingsViewModel> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IUIThreadService _uiThreadService;
    // + 1300+ lines of CRUD operations, property management, etc.
}
```

### Migration Steps

#### Step 1: Change Class Inheritance
```csharp
// BEFORE:
public class PowerSupplySettingsViewModel : ViewModelBase, IDisposable

// AFTER:
public class PowerSupplySettingsViewModel : ProfileManagementViewModelBase<PowerSupplyProfile>
```

#### Step 2: Update Constructor
```csharp
// BEFORE: 8 dependencies + manual registration
public PowerSupplySettingsViewModel(
    IPowerSupplyProfileService profileService,
    IPowerSupplyService powerSupplyService,
    IDialogService dialogService,
    IProfileEditDialogService profileEditDialogService,
    IClipboardService clipboardService,
    IFileDialogService? fileDialogService,
    ILogger<PowerSupplySettingsViewModel> logger,
    ISettingsService settingsService,
    IUIThreadService uiThreadService)

// AFTER: 3 base dependencies + specific dependencies via composition
public PowerSupplySettingsViewModel(
    IUnifiedProfileDialogService unifiedDialogService,
    ILogger<ProfileManagementViewModelBase<PowerSupplyProfile>> logger,
    IUIThreadService uiThreadService,
    IPowerSupplyProfileService profileService,
    IPowerSupplyService powerSupplyService,
    IDialogService dialogService,
    IClipboardService clipboardService,
    IFileDialogService? fileDialogService,
    ISettingsService settingsService)
    : base(unifiedDialogService, logger, uiThreadService)
{
    _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    _powerSupplyService = powerSupplyService ?? throw new ArgumentNullException(nameof(powerSupplyService));
    _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
    _fileDialogService = fileDialogService;
    _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    _specificLogger = LoggerFactory.CreateLogger<PowerSupplySettingsViewModel>();
}
```

#### Step 3: Implement Abstract Methods
```csharp
protected override IProfileManager<PowerSupplyProfile> GetProfileManager()
    => _profileService;

protected override string GetDefaultProfileName()
    => "New Power Supply Profile";

protected override string GetProfileTypeName()
    => "Power Supply";

protected override PowerSupplyProfile CreateDefaultProfile()
    => new()
    {
        Name = GetDefaultProfileName(),
        Description = "Modbus TCP power supply configuration",
        IpAddress = "192.168.1.100",
        Port = 502,
        SlaveId = 1,
        MaxVoltage = 30.0,
        MaxCurrent = 5.0,
        Options = "--timeout 5000",
        Flags = "--precision 2"
    };

protected override async Task<PowerSupplyProfile?> ShowCreateDialogAsync()
{
    return await UnifiedDialogService.ShowPowerSupplyCreateDialogAsync().ConfigureAwait(false);
}

protected override async Task<PowerSupplyProfile?> ShowEditDialogAsync(PowerSupplyProfile profile)
{
    return await UnifiedDialogService.ShowPowerSupplyEditDialogAsync(profile).ConfigureAwait(false);
}

protected override async Task<PowerSupplyProfile?> ShowDuplicateDialogAsync(PowerSupplyProfile sourceProfile, string suggestedName)
{
    var duplicatedProfile = sourceProfile.Clone();
    duplicatedProfile.Name = suggestedName;
    return await UnifiedDialogService.ShowPowerSupplyCreateDialogAsync(duplicatedProfile).ConfigureAwait(false);
}
```

#### Step 4: Remove Duplicate Code
```csharp
// REMOVE: All base functionality now provided by ProfileManagementViewModelBase
// - Profiles property management (~50 lines)
// - SelectedProfile property management (~30 lines)
// - All CRUD command definitions (~100 lines)
// - Property change subscriptions (~50 lines)
// - Load/Save operations (~150 lines)
// - Error handling patterns (~75 lines)
// - Total reduction: ~455 lines of duplicate code
```

#### Step 5: Preserve Specific Functionality
```csharp
// KEEP: Power Supply-specific properties and operations
private readonly IPowerSupplyService _powerSupplyService;

// Power Supply-specific methods
public async Task ConnectAsync(PowerSupplyProfile profile) { /* existing implementation */ }
public async Task DisconnectAsync() { /* existing implementation */ }
public async Task SetVoltageAsync(double voltage) { /* existing implementation */ }
public async Task SetCurrentAsync(double current) { /* existing implementation */ }

// Real-time monitoring functionality preserved
public ObservableCollection<PowerSupplyReading> Readings { get; } = new();
private async Task StartMonitoringAsync() { /* existing implementation */ }
```

## Code Reduction Summary

### Before Migration (Total: ~4,399 lines)
- SerialPortsSettingsViewModel: 1,509 lines → **COMPLETED** → ProfileManagementViewModelBase: 440 lines
- SocatSettingsViewModel: 1,509 lines → **Ready for migration**
- PowerSupplySettingsViewModel: 1,381 lines → **Ready for migration**

### After Migration (Projected: ~2,200 lines)
- ProfileManagementViewModelBase: 440 lines (shared template)
- SerialPortsSettingsViewModel: ~600 lines (specific functionality only)
- SocatSettingsViewModel: ~650 lines (specific functionality only)
- PowerSupplySettingsViewModel: ~510 lines (specific functionality only)

**Total Reduction: ~2,200 lines (50% code reduction)**

## Migration Checklist

### Pre-Migration Verification
- [ ] Verify ProfileManagementViewModelBase is stable and tested
- [ ] Confirm IUnifiedProfileDialogService has required methods
- [ ] Check that profile models implement IProfileBase interface
- [ ] Ensure profile manager services implement IProfileManager<T>

### Migration Process
- [ ] Change class inheritance to ProfileManagementViewModelBase<T>
- [ ] Update constructor parameters (3 base + specific dependencies)
- [ ] Implement 7 abstract methods (GetProfileManager, names, create, dialogs)
- [ ] Remove duplicate code (properties, commands, subscriptions)
- [ ] Preserve specific functionality (device operations, monitoring)
- [ ] Update service registration in ServiceCollectionExtensions.cs

### Post-Migration Validation
- [ ] Build verification: Clean compilation (0 errors)
- [ ] Runtime verification: Application starts successfully
- [ ] Functional verification: All CRUD operations work
- [ ] UI verification: DataGrid bindings and commands function
- [ ] Integration verification: Dialog operations complete successfully

## Critical Success Factors

1. **Adapter Pattern**: Maintain existing dependencies through composition
2. **Template Method**: Implement all 7 abstract methods completely
3. **Service Registration**: Update constructor calls in ServiceCollectionExtensions.cs
4. **Error Handling**: Use ConfigureAwait(false) for all async operations
5. **Logger Access**: Use private _specificLogger field for type-specific logging
6. **Dispose Override**: Call base.Dispose() in override method

## Expected Outcomes

✅ **Code Reduction**: ~900 lines per ViewModel (50% reduction)
✅ **Maintainability**: Unified CRUD operations across all profiles
✅ **Consistency**: Standardized UI patterns and error handling
✅ **Testability**: Template method pattern enables focused unit testing
✅ **Extensibility**: New profile types follow established pattern

**Next Action**: Apply migration steps to SocatSettingsViewModel first, then PowerSupplySettingsViewModel using identical pattern.
