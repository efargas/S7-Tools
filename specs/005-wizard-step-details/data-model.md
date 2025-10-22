# Data Model: Enhanced Wizard Step Profile Details

**Branch**: `005-wizard-step-details` | **Date**: 2025-10-21
**Purpose**: Define data structures and relationships for enhanced profile detail display

## Entity Overview

This enhancement leverages existing profile entities without modification, focusing on presentation layer enhancements for comprehensive profile detail display.

## Existing Entities (No Changes Required)

### SerialPortProfile
- **Purpose**: Serial port communication configuration
- **Key Properties**: Id, Name, Description, Configuration (SerialPortConfiguration), IsDefault, IsReadOnly, CreatedAt, ModifiedAt, Version, Options, Flags, Metadata
- **Detail Sections**:
  - Basic Settings: BaudRate, CharacterSize, Parity, StopBits
  - Control Flags: EnableReceiver, DisableHardwareFlowControl, ParityEnabled, OddParity
  - Input Flags: IgnoreBreak, DisableBreakInterrupt, DisableMapCRtoNL, DisableBellOnQueueFull, DisableXonXoffFlowControl
  - Output Flags: DisableOutputProcessing, DisableMapNLtoCRNL
  - Local Flags: DisableCanonicalMode, DisableSignalGeneration, DisableExtendedProcessing, DisableEcho, DisableEchoErase, DisableEchoKill, DisableEchoControl, DisableEchoKillErase
  - Special Modes: RawMode
  - Metadata: Version, CreatedAt, ModifiedAt, custom metadata

### SocatProfile
- **Purpose**: Socat network bridge configuration
- **Key Properties**: Id, Name, Description, Configuration (SocatConfiguration), IsDefault, IsReadOnly, CreatedAt, ModifiedAt, Version, Options, Flags, Metadata
- **Detail Sections**:
  - TCP Settings: TcpPort, TcpHost, EnableFork, EnableReuseAddr
  - Socat Flags: Verbose, HexDump, BlockSize, DebugLevel
  - Serial Device Settings: SerialRawMode, SerialDisableEcho
  - Process Management: AutoConfigureSerial, ConnectionTimeout, AutoRestart
  - Metadata: Version, CreatedAt, ModifiedAt, custom metadata

### PowerSupplyProfile
- **Purpose**: Power supply control configuration
- **Key Properties**: Id, Name, Description, Configuration (PowerSupplyConfiguration), IsDefault, IsReadOnly, CreatedAt, ModifiedAt, Version, Options, Flags, Metadata
- **Detail Sections**:
  - Connection Settings: Host, Port, DeviceId
  - Modbus Configuration: AddressingMode, ConnectionTimeoutMs, ReadTimeoutMs, WriteTimeoutMs
  - Control Settings: OnOffCoil, EnableAutoReconnect, MaxRetryAttempts
  - Metadata: Version, CreatedAt, ModifiedAt, custom metadata

## Enhanced ViewModel Properties

### JobWizardViewModel Extensions
The existing JobWizardViewModel will be enhanced with computed properties for comprehensive profile detail display:

#### Serial Profile Details
```csharp
// Basic Settings
public string SerialBaudRate => SelectedSerial?.Configuration?.BaudRate.ToString() ?? "N/A";
public string SerialCharacterSize => SelectedSerial?.Configuration?.CharacterSize.ToString() ?? "N/A";
public string SerialParity => SelectedSerial?.Configuration?.Parity.ToString() ?? "N/A";
public string SerialStopBits => SelectedSerial?.Configuration?.StopBits.ToString() ?? "N/A";

// Control Flags
public string SerialEnableReceiver => SelectedSerial?.Configuration?.EnableReceiver.ToString() ?? "N/A";
public string SerialDisableHardwareFlowControl => SelectedSerial?.Configuration?.DisableHardwareFlowControl.ToString() ?? "N/A";
// ... (additional control flags)

// Metadata
public string SerialVersion => SelectedSerial?.Version ?? "N/A";
public string SerialCreatedAt => SelectedSerial?.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
public string SerialModifiedAt => SelectedSerial?.ModifiedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
```

#### Socat Profile Details
```csharp
// TCP Settings
public string SocatTcpPort => SelectedSocat?.Configuration?.TcpPort.ToString() ?? "N/A";
public string SocatTcpHost => SelectedSocat?.Configuration?.TcpHost ?? "N/A";
public string SocatEnableFork => SelectedSocat?.Configuration?.EnableFork.ToString() ?? "N/A";
public string SocatEnableReuseAddr => SelectedSocat?.Configuration?.EnableReuseAddr.ToString() ?? "N/A";

// Socat Flags
public string SocatVerbose => SelectedSocat?.Configuration?.Verbose.ToString() ?? "N/A";
public string SocatHexDump => SelectedSocat?.Configuration?.HexDump.ToString() ?? "N/A";
// ... (additional socat settings)
```

#### Power Supply Profile Details
```csharp
// Connection Settings
public string PowerHost => SelectedPower?.Configuration?.Host ?? "N/A";
public string PowerPort => SelectedPower?.Configuration?.Port.ToString() ?? "N/A";
public string PowerDeviceId => SelectedPower?.Configuration?.DeviceId.ToString() ?? "N/A";

// Modbus Configuration
public string PowerAddressingMode => SelectedPower?.Configuration?.AddressingMode.ToString() ?? "N/A";
public string PowerConnectionTimeout => SelectedPower?.Configuration?.ConnectionTimeoutMs.ToString() ?? "N/A";
// ... (additional power settings)
```

## Data Flow

### Profile Selection Update Flow
1. User selects profile in wizard step ComboBox
2. SelectedProfile property updates via ReactiveUI binding
3. Computed properties automatically recalculate due to RaiseAndSetIfChanged
4. UI bindings update to display new profile details
5. All detail sections refresh immediately (<100ms performance requirement)

### Property Binding Strategy
- **Computed Properties**: Use null-coalescing operators for safe property access
- **Fallback Values**: Consistent "N/A" or appropriate default values for null/empty properties
- **Type Conversion**: Convert value types to string for display binding
- **Date Formatting**: Consistent ISO format (yyyy-MM-dd HH:mm) for timestamps

## Validation Rules

### Display Validation
- All computed properties must handle null profile selections gracefully
- Date properties must format consistently across all profile types
- Boolean properties must display as "True"/"False" strings
- Enum properties must display using ToString() for consistency
- Numeric properties must convert to string without formatting exceptions

### Performance Validation
- Profile detail updates must complete within 100ms requirement
- No computed property should trigger expensive operations
- All property access should use safe navigation operators
- Memory allocation should be minimal (string caching not required due to low frequency)

## State Transitions

### Profile Selection States
1. **No Profile Selected**: All detail properties return "N/A" or appropriate defaults
2. **Profile Selected**: All detail properties return actual configuration values
3. **Profile Changed**: Immediate transition from old to new profile details
4. **Profile Loading**: Existing profile loading patterns handle async operations

### UI State Management
- Profile detail sections remain visible regardless of selection state
- ScrollViewer enables navigation through extensive detail lists
- Section headers remain consistent across all profile types
- Property grouping follows established JobInfoDisplayView patterns

## Integration Points

### Existing Services
- **Profile Services**: ISerialPortProfileService, ISocatProfileService, IPowerSupplyProfileService
- **UI Services**: IUIThreadService for thread-safe updates
- **Logging**: ILogger<JobWizardViewModel> for diagnostic information

### Existing ViewModels
- **JobWizardViewModel**: Primary integration point for computed properties
- **JobInfoDisplayViewModel**: Reference implementation for detail organization
- **Profile-specific ViewModels**: Not required for this enhancement

### Existing Views
- **JobWizardView.axaml**: Enhanced profile detail sections
- **JobInfoDisplayView.axaml**: Reference styling and organization patterns
