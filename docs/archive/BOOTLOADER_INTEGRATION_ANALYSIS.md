# Bootloader Integration Analysis - Profile Configuration Mismatch

## Executive Summary

**Issue**: The bootloader service is not using the full profile configurations. The `JobProfileSet` uses simplified `ProfileRef` records that only extract primitive values, missing the rich `Configuration` objects from the full profiles.

**Impact**:
- Serial port configuration loses `Options` and `Flags` from profile
- Socat configuration loses advanced settings (Verbose, HexDump, BlockSize, etc.)
- Power supply configuration loses Modbus-specific settings
- All profiles miss custom metadata and validation logic

**Solution**: Update `JobProfileSet` and bootloader integration to use full profile objects or ensure `ProfileRef` records carry complete configuration data.

---

## Current Architecture Issues

### 1. Serial Port Profile Integration

#### Profile Model (Full)
```csharp
// SerialPortProfile.cs - Complete profile with Configuration object
public class SerialPortProfile : IProfileBase
{
    public SerialPortConfiguration Configuration { get; set; } // Rich object
    public string Options { get; set; }  // stty command options
    public string Flags { get; set; }    // Profile flags
    public Dictionary<string, string>? Metadata { get; set; }
}
```

#### Profile Configuration (Full)
```csharp
// SerialPortConfiguration.cs - Rich configuration object
public class SerialPortConfiguration
{
    public int BaudRate { get; set; }
    public ParityMode Parity { get; set; }
    public int CharacterSize { get; set; }
    public StopBits StopBits { get; set; }

    // Rich boolean flags for stty
    public bool RawMode { get; set; }
    public bool DisableEcho { get; set; }
    public bool IgnoreBreak { get; set; }
    public bool DisableCanonicalMode { get; set; }
    public bool DisableSignalGeneration { get; set; }
    public bool DisableHardwareFlowControl { get; set; }
    public bool DisableXonXoffFlowControl { get; set; }
    // ... 20+ more properties
}
```

#### JobProfileSet Reference (Simplified - PROBLEM)
```csharp
// SerialProfileRef.cs - Loses configuration richness
public sealed record SerialProfileRef(
    string Device,
    int Baud,           // ❌ Only 5 primitive properties
    string Parity,      // ❌ String instead of enum
    int DataBits,
    string StopBits,    // ❌ String instead of enum
    string Options = "",
    string Flags = ""
);
// ❌ MISSING: Full SerialPortConfiguration with 20+ boolean flags
```

#### Bootloader Usage (Workaround - FRAGILE)
```csharp
// BootloaderService.cs - Manually reconstructs configuration
var serialConfig = new SerialPortConfiguration
{
    BaudRate = profiles.Serial.Baud,
    Parity = ParseParityMode(profiles.Serial.Parity),  // ❌ Manual parsing
    CharacterSize = profiles.Serial.DataBits,
    StopBits = ParseStopBits(profiles.Serial.StopBits), // ❌ Manual parsing

    // ❌ Hardcoded defaults - not from profile
    RawMode = true,
    DisableEcho = true,
    IgnoreBreak = true,
    // ... hardcoded values
};
```

**Problem**: The profile's `SerialPortConfiguration` object is discarded. The bootloader hardcodes settings instead of using profile values.

---

### 2. Socat Profile Integration

#### Profile Model (Full)
```csharp
// SocatProfile.cs - Complete profile
public class SocatProfile : IProfileBase
{
    public SocatConfiguration Configuration { get; set; } // Rich object
    public string Options { get; set; }  // socat command options
    public string Flags { get; set; }    // Profile flags
}
```

#### Profile Configuration (Full)
```csharp
// SocatConfiguration.cs - Rich configuration
public class SocatConfiguration
{
    public int TcpPort { get; set; }
    public bool Verbose { get; set; }
    public bool HexDump { get; set; }
    public int BlockSize { get; set; }
    public int DebugLevel { get; set; }
    public bool EnableFork { get; set; }
    public bool EnableReuseAddr { get; set; }
    public bool SerialRawMode { get; set; }
    public bool SerialDisableEcho { get; set; }
    // ... 15+ more properties
}
```

#### JobProfileSet Reference (Better - Still Issues)
```csharp
// SocatProfileRef.cs - Has Configuration but nullable
public sealed record SocatProfileRef(
    int Port,
    bool Ephemeral = true,
    SocatConfiguration? Configuration = null  // ✅ Has full config (nullable)
);
```

#### Bootloader Usage (Partial Fix)
```csharp
// BootloaderService.cs - Uses Configuration if available
SocatConfiguration socatConfig = profiles.Socat.Configuration ?? new SocatConfiguration
{
    TcpPort = profiles.Socat.Port,
    // ❌ Hardcoded defaults if Configuration is null
    Verbose = true,
    HexDump = false,
    BlockSize = 4,
    // ...
};
```

**Problem**: Configuration is nullable and may fall back to hardcoded defaults instead of profile settings.

---

### 3. Power Supply Profile Integration

#### Profile Model (Full)
```csharp
// PowerSupplyProfile.cs - Complete profile
public class PowerSupplyProfile : IProfileBase
{
    public PowerSupplyConfiguration Configuration { get; set; } // Polymorphic
    public string Options { get; set; }
    public string Flags { get; set; }
}
```

#### Profile Configuration (Full)
```csharp
// PowerSupplyConfiguration.cs - Base class
public abstract class PowerSupplyConfiguration { }

// ModbusTcpConfiguration.cs - Concrete type
public class ModbusTcpConfiguration : PowerSupplyConfiguration
{
    public string Host { get; set; }
    public int Port { get; set; }
    public byte DeviceId { get; set; }
    public ushort OnOffCoil { get; set; }
    public ModbusAddressingMode AddressingMode { get; set; }
    // ... more Modbus-specific settings
}
```

#### JobProfileSet Reference (Fragmented - PROBLEM)
```csharp
// PowerProfileRef.cs - Flattened properties
public sealed record PowerProfileRef(
    string Host,
    int Port,
    int Coil,
    int DelaySeconds,
    byte DeviceId = 1,
    ModbusAddressingMode AddressingMode = ModbusAddressingMode.Base0
);
// ❌ MISSING: Full PowerSupplyConfiguration object
// ❌ Can't support different power supply types (RTU, custom, etc.)
```

#### Bootloader Usage (Manual Reconstruction - FRAGILE)
```csharp
// BootloaderService.cs - Manually builds ModbusTcpConfiguration
var powerConfig = new ModbusTcpConfiguration
{
    Host = profiles.Power.Host,
    Port = profiles.Power.Port,
    DeviceId = profiles.Power.DeviceId,
    OnOffCoil = (ushort)profiles.Power.Coil,
    AddressingMode = profiles.Power.AddressingMode
};
// ❌ Discards any custom settings from profile's Configuration object
```

**Problem**: The profile's `PowerSupplyConfiguration` object is completely discarded. Only primitive properties are passed.

---

## Root Cause Analysis

### Design Mismatch

1. **Profile Models** (`SerialPortProfile`, `SocatProfile`, `PowerSupplyProfile`)
   - Rich domain models with full configuration objects
   - Support metadata, options, flags, validation
   - 100-400 lines of functionality

2. **ProfileRef Records** (`SerialProfileRef`, `SocatProfileRef`, `PowerProfileRef`)
   - Lightweight DTOs for job serialization
   - Only extract key properties for display
   - 20-30 lines of primitives

3. **Bootloader Service**
   - Expects rich configuration objects
   - Currently receives primitive ProfileRef records
   - Forced to reconstruct or hardcode configuration

### Architectural Gap

```
┌─────────────────────────────────────────────────────────┐
│ Profile Management ViewModels                           │
│ ┌────────────────────┐                                  │
│ │ SerialPortProfile  │ ──► Configuration (Rich)         │
│ │ SocatProfile       │ ──► Configuration (Rich)         │
│ │ PowerSupplyProfile │ ──► Configuration (Rich)         │
│ └────────────────────┘                                  │
└──────────────┬──────────────────────────────────────────┘
               │
               │ ❌ LOSES DATA HERE
               ▼
┌─────────────────────────────────────────────────────────┐
│ Job Wizard / Job Profiles                               │
│ ┌────────────────┐                                      │
│ │ SerialProfileRef │ ──► Device, Baud, Parity... (5 props)│
│ │ SocatProfileRef  │ ──► Port, Configuration? (nullable)│
│ │ PowerProfileRef  │ ──► Host, Port, Coil... (6 props) │
│ └────────────────┘                                      │
└──────────────┬──────────────────────────────────────────┘
               │
               │ ❌ MISSING RICH CONFIG
               ▼
┌─────────────────────────────────────────────────────────┐
│ Bootloader Service                                      │
│ ┌──────────────────────────────────────────────────────┐│
│ │ Manually reconstructs configuration                  ││
│ │ Hardcodes defaults                                   ││
│ │ Parses string→enum manually                         ││
│ │ Missing profile metadata, options, flags            ││
│ └──────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

---

## Recommended Solution

### Option 1: Add Full Configuration to ProfileRef (RECOMMENDED)

Update `ProfileRef` records to include the full configuration objects:

```csharp
// SerialProfileRef.cs - Enhanced with full configuration
public sealed record SerialProfileRef(
    string Device,
    int Baud,  // Keep for display
    string Parity,  // Keep for display
    int DataBits,  // Keep for display
    string StopBits,  // Keep for display
    string Options = "",
    string Flags = "",
    SerialPortConfiguration? Configuration = null  // ✅ ADD FULL CONFIG
);

// PowerProfileRef.cs - Enhanced with full configuration
public sealed record PowerProfileRef(
    string Host,  // Keep for display
    int Port,  // Keep for display
    int Coil,  // Keep for display
    int DelaySeconds,
    byte DeviceId = 1,
    ModbusAddressingMode AddressingMode = ModbusAddressingMode.Base0,
    PowerSupplyConfiguration? Configuration = null  // ✅ ADD FULL CONFIG
);
```

**Job Wizard Changes**:
```csharp
// When creating SerialProfileRef from selected profile
var serialRef = new SerialProfileRef(
    Device: profile.Configuration.Device ?? selectedProfile.Name,
    Baud: profile.Configuration.BaudRate,
    Parity: profile.Configuration.Parity.ToString(),
    DataBits: profile.Configuration.CharacterSize,
    StopBits: profile.Configuration.StopBits.ToString(),
    Options: profile.Options,
    Flags: profile.Flags,
    Configuration: profile.Configuration  // ✅ PASS FULL CONFIG
);
```

**Bootloader Changes**:
```csharp
// Stage 0: Use full configuration from profile
SerialPortConfiguration serialConfig = profiles.Serial.Configuration
    ?? new SerialPortConfiguration  // Fallback only
    {
        BaudRate = profiles.Serial.Baud,
        Parity = ParseParityMode(profiles.Serial.Parity),
        // ... fallback values
    };

// Apply with options and flags from profile
bool configured = await _serialPort.ApplyConfigurationAsync(
    profiles.Serial.Device,
    serialConfig,
    options: profiles.Serial.Options,  // ✅ USE PROFILE OPTIONS
    flags: profiles.Serial.Flags,      // ✅ USE PROFILE FLAGS
    cancellationToken);
```

---

### Option 2: Use Full Profile Objects in JobProfileSet

Replace `ProfileRef` records with full profile objects:

```csharp
// JobProfileSet.cs - Use full profiles
public sealed record JobProfileSet(
    SerialPortProfile Serial,        // ✅ Full profile
    SocatProfile Socat,              // ✅ Full profile
    PowerSupplyProfile Power,        // ✅ Full profile
    MemoryRegionProfile Memory,
    PayloadSetProfile Payloads,
    string OutputPath
);
```

**Pros**:
- No data loss
- Complete profile functionality available
- Validation, metadata, options all preserved

**Cons**:
- Heavier JSON serialization
- Job files become larger
- Possible circular reference issues

---

### Option 3: Hybrid Approach (BEST)

Keep `ProfileRef` for display but add `FullProfile` property:

```csharp
// SerialProfileRef.cs - Hybrid approach
public sealed record SerialProfileRef(
    // Display properties (for UI binding)
    string Device,
    int Baud,
    string Parity,
    int DataBits,
    string StopBits,

    // Execution properties (for bootloader)
    SerialPortConfiguration Configuration,  // ✅ Required
    string Options = "",
    string Flags = ""
)
{
    // Factory method from full profile
    public static SerialProfileRef FromProfile(SerialPortProfile profile)
    {
        return new SerialProfileRef(
            Device: profile.Configuration.Device ?? profile.Name,
            Baud: profile.Configuration.BaudRate,
            Parity: profile.Configuration.Parity.ToString(),
            DataBits: profile.Configuration.CharacterSize,
            StopBits: profile.Configuration.StopBits.ToString(),
            Configuration: profile.Configuration,  // ✅ Full config
            Options: profile.Options,
            Flags: profile.Flags
        );
    }
}
```

---

## Implementation Checklist

### Phase 1: Update ProfileRef Models (2-3 hours)
- [ ] Add `Configuration` property to `SerialProfileRef` (make required, not nullable)
- [ ] Add `Configuration` property to `PowerProfileRef` (make required, not nullable)
- [ ] Verify `SocatProfileRef.Configuration` is populated (already has property)
- [ ] Add static factory methods `FromProfile()` to all ProfileRef types

### Phase 2: Update Job Wizard (2-3 hours)
- [ ] Update `JobWizardViewModel` to extract full `Configuration` objects
- [ ] Pass `profile.Configuration` when creating ProfileRef instances
- [ ] Update job profile serialization/deserialization tests
- [ ] Verify job profiles round-trip with full configuration data

### Phase 3: Update Bootloader Service (2-3 hours)
- [ ] **Serial Port Stage**: Use `profiles.Serial.Configuration` directly
  - Remove `ParseParityMode()` and `ParseStopBits()` helper methods
  - Use configuration's enum properties directly
  - Pass `Options` and `Flags` to `ApplyConfigurationAsync()`

- [ ] **Socat Stage**: Make `profiles.Socat.Configuration` required
  - Remove nullable fallback logic
  - Use configuration directly from profile

- [ ] **Power Stage**: Use `profiles.Power.Configuration` directly
  - Remove manual `ModbusTcpConfiguration` reconstruction
  - Use configuration from profile (already correct type)

### Phase 4: Update Service Interfaces (1-2 hours)
- [ ] Update `ISerialPortService.ApplyConfigurationAsync()` signature
  ```csharp
  Task<bool> ApplyConfigurationAsync(
      string portPath,
      SerialPortConfiguration configuration,
      string options = "",  // ✅ ADD
      string flags = "",    // ✅ ADD
      CancellationToken cancellationToken = default);
  ```

- [ ] Update `ISerialPortService.GenerateSttyCommand()` to use `options` and `flags`
  ```csharp
  string GenerateSttyCommand(
      SerialPortConfiguration configuration,
      string portPath,
      string additionalOptions = "");  // ✅ Append profile options
  ```

### Phase 5: Update Tests (2-3 hours)
- [ ] Update all `BootloaderServiceTests` to use full configurations
- [ ] Remove tests for `ParseParityMode()` and `ParseStopBits()` (no longer needed)
- [ ] Add tests for configuration null handling (should throw, not fallback)
- [ ] Verify end-to-end job execution with rich profile configurations

### Phase 6: Update UI/ViewModels (1-2 hours)
- [ ] Verify `SerialPortProfileViewModel` saves `Configuration` object
- [ ] Verify `SocatProfileViewModel` saves `Configuration` object
- [ ] Verify `PowerSupplyProfileViewModel` saves `Configuration` object
- [ ] Update profile edit dialogs to show configuration is being used

---

## Breaking Changes

### API Changes
1. `SerialProfileRef` constructor signature changes (adds `Configuration`)
2. `PowerProfileRef` constructor signature changes (adds `Configuration`)
3. `ISerialPortService.ApplyConfigurationAsync()` signature changes (adds `options`, `flags`)

### Migration Path
```csharp
// Old job profiles (missing Configuration)
var oldSerial = new SerialProfileRef(
    Device: "/dev/ttyUSB0",
    Baud: 38400,
    Parity: "None",
    DataBits: 8,
    StopBits: "One"
);

// New job profiles (with Configuration)
var newSerial = new SerialProfileRef(
    Device: "/dev/ttyUSB0",
    Baud: 38400,
    Parity: "None",
    DataBits: 8,
    StopBits: "One",
    Configuration: new SerialPortConfiguration
    {
        BaudRate = 38400,
        Parity = ParityMode.None,
        CharacterSize = 8,
        StopBits = StopBits.One,
        // ... full settings from profile
    },
    Options: "-F",
    Flags: "auto-configure=true"
);
```

### Backward Compatibility
- Add migration logic in `JobProfile` deserialization
- Detect old format (no `Configuration` property)
- Reconstruct `Configuration` from primitive properties
- Log warning about migrated job profiles

---

## Expected Benefits

### Correctness
- ✅ No hardcoded configuration values
- ✅ All profile settings used exactly as configured
- ✅ stty command matches profile configuration
- ✅ socat command matches profile configuration
- ✅ Modbus settings match profile configuration

### Maintainability
- ✅ Remove `ParseParityMode()` and `ParseStopBits()` helper methods
- ✅ Remove fallback configuration logic (100+ lines)
- ✅ Single source of truth for configuration
- ✅ Profile changes automatically reflected in jobs

### Extensibility
- ✅ Support for profile metadata in job execution
- ✅ Support for profile-specific options and flags
- ✅ Future power supply types (RTU, custom) work automatically
- ✅ Advanced socat features available to jobs

### Testing
- ✅ Test against actual profile configurations
- ✅ Verify profile→job→execution pipeline
- ✅ Integration tests with real profile objects

---

## Timeline Estimate

- **Phase 1-2**: 4-6 hours (ProfileRef + Job Wizard updates)
- **Phase 3**: 2-3 hours (Bootloader service cleanup)
- **Phase 4**: 1-2 hours (Service interface updates)
- **Phase 5**: 2-3 hours (Test updates)
- **Phase 6**: 1-2 hours (UI verification)

**Total**: 10-16 hours of development + testing

---

## Risk Assessment

### Low Risk
- Adding properties to ProfileRef records (backward compatible with defaults)
- Removing hardcoded fallback logic (improves correctness)
- Adding `options`/`flags` parameters to service methods (backward compatible with defaults)

### Medium Risk
- Job profile JSON deserialization (needs migration logic)
- Existing job profiles in user environments (need migration path)

### High Risk
- None identified (all changes are additive or improve correctness)

---

## Next Steps

1. **Review this analysis** with project stakeholders
2. **Approve solution approach** (Option 3: Hybrid recommended)
3. **Create implementation tasks** in project tracker
4. **Implement Phase 1-2** (ProfileRef + Job Wizard)
5. **Verify with test job profiles** before proceeding to bootloader
6. **Complete Phases 3-6** once ProfileRef validated
7. **Document migration guide** for users with existing job profiles

---

**Document Version**: 1.0
**Last Updated**: 2025-11-13
**Author**: GitHub Copilot
**Status**: Analysis Complete - Awaiting Review
