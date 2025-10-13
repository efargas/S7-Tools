# [TASK007] - Power Supply Settings Category Implementation

**Status:** Approved - Ready to Start  
**Added:** 2025-10-13  
**Updated:** 2025-10-13  
**Priority:** High  
**Estimated Time:** 18-22 hours across 6 phases  
**Approved By:** @efargas on 2025-10-13

## Original Request

> We have to create another setting view, this time to control a power supply. There will be different types of power supply modules to control. And we need to think in future addition of different kind. For now we will start with a tcp modbus device, we need another profiler view where to configure it. It will contain modbus host, port, device id and coil to turn on and off. Then test buttons to connect/disconnect from modbus server, and to manual turn on and off. Create an task for the implementation using memory-bank instructions as always

## User Feedback and Approvals

**Date:** 2025-10-13  
**User:** @efargas

**Decisions:**
1. ✅ **NModbus Library Approved** - Use NModbus for Modbus TCP communication
2. ✅ **Test Hardware Available** - User has Modbus PLC available for testing
3. ✅ **Addressing Mode Requirement** - Add option to use base-0 or base-1 Modbus addressing
4. ✅ **Implementation Plan Approved** - Ready to proceed with implementation

**Key Requirements Added:**
- **Addressing Mode Configuration**: Users must be able to choose between base-0 (0-based indexing) and base-1 (1-based indexing) for Modbus coil addresses
  - Base-0: Coil addresses start at 0 (e.g., first coil is address 0)
  - Base-1: Coil addresses start at 1 (e.g., first coil is address 1, internally converted to 0)
  - This setting affects both display and internal addressing calculations

## Thought Process

### Requirements Analysis

**Core Requirements:**
1. **New Settings Category**: "Power Supply" settings view
2. **Profile-Based Configuration**: Similar to Serial Ports and Socat implementations
3. **Multiple Device Types**: Start with TCP Modbus, design for future extensibility
4. **TCP Modbus Configuration**:
   - Modbus Host (IP address or hostname)
   - Modbus Port (TCP port number)
   - Device ID (Modbus slave address)
   - Control Coil (coil number for on/off control)
5. **Test Functionality**:
   - Connect/Disconnect buttons (test Modbus server connection)
   - Manual On/Off buttons (control power supply state)
6. **Architecture**: Follow established patterns from Serial Ports and Socat implementations

### Architecture Design

**Clean Architecture Layers:**

1. **Domain Layer (S7Tools.Core)**:
   - `PowerSupplyProfile.cs` - Profile model with metadata
   - `PowerSupplyConfiguration.cs` - Base configuration (supports multiple types)
   - `ModbusTcpConfiguration.cs` - TCP Modbus specific configuration
   - `PowerSupplySettings.cs` - Settings integration model
   - `PowerSupplyType` enum - Extensible device type enumeration

2. **Application Layer (S7Tools)**:
   - `IPowerSupplyProfileService.cs` - Profile management interface
   - `IPowerSupplyService.cs` - Power supply operations interface
   - `PowerSupplyProfileService.cs` - JSON-based profile persistence
   - `PowerSupplyService.cs` - Modbus TCP communication implementation
   - `PowerSupplySettingsViewModel.cs` - Main settings ViewModel
   - `PowerSupplyProfileViewModel.cs` - Individual profile editing ViewModel
   - `PowerSupplySettingsView.axaml` - UI implementation

### Key Design Decisions

**1. Extensibility for Multiple Device Types**

Using polymorphism with a base configuration class:

```csharp
// Base configuration
public abstract class PowerSupplyConfiguration
{
    public PowerSupplyType Type { get; set; }
    public abstract string GenerateConnectionString();
    public abstract Task<bool> TestConnectionAsync();
}

// TCP Modbus implementation
public class ModbusTcpConfiguration : PowerSupplyConfiguration
{
    public string Host { get; set; }
    public int Port { get; set; }
    public byte DeviceId { get; set; }
    public ushort OnOffCoil { get; set; }
    public ModbusAddressingMode AddressingMode { get; set; } = ModbusAddressingMode.Base0;
}

// Addressing mode enumeration
public enum ModbusAddressingMode
{
    Base0 = 0,  // 0-based addressing (coils start at 0)
    Base1 = 1   // 1-based addressing (coils start at 1, internally converted to 0-based)
}
```

**Future device types could include:**
- Serial Modbus RTU
- SNMP-based power supplies
- HTTP/REST API power supplies
- Proprietary protocols

**2. Following Established Patterns**

Based on the successful Serial Ports and Socat implementations:
- **Profile-based configuration** - Users create named profiles
- **4-row UI layout** - Consistent settings category structure
- **ReactiveUI optimization** - Individual property subscriptions
- **Service-oriented design** - Interfaces in Core, implementations in Application
- **Thread-safe operations** - UI thread marshaling with IUIThreadService
- **Comprehensive logging** - Structured logging throughout

**3. Modbus TCP Protocol Integration**

Using **NModbus library** (approved by user) for Modbus TCP communication:

```csharp
// Using NModbus library for Modbus TCP communication
public class PowerSupplyService : IPowerSupplyService
{
    private readonly ILogger<PowerSupplyService> _logger;
    private IModbusMaster? _modbusMaster;
    private TcpClient? _tcpClient;
    private ModbusTcpConfiguration? _currentConfig;
    
    public async Task<bool> ConnectAsync(ModbusTcpConfiguration config)
    {
        // Establish TCP connection
        // Create Modbus master using NModbus factory
        // Store configuration for addressing mode reference
        _currentConfig = config;
        // Test communication
    }
    
    public async Task<bool> TurnOnAsync()
    {
        // Convert address based on addressing mode
        ushort actualAddress = ConvertAddress(_currentConfig.OnOffCoil, _currentConfig.AddressingMode);
        // Write true to coil address using NModbus
        await _modbusMaster.WriteSingleCoilAsync(_currentConfig.DeviceId, actualAddress, true);
    }
    
    public async Task<bool> TurnOffAsync()
    {
        // Convert address based on addressing mode
        ushort actualAddress = ConvertAddress(_currentConfig.OnOffCoil, _currentConfig.AddressingMode);
        // Write false to coil address using NModbus
        await _modbusMaster.WriteSingleCoilAsync(_currentConfig.DeviceId, actualAddress, false);
    }
    
    private ushort ConvertAddress(ushort displayAddress, ModbusAddressingMode mode)
    {
        // Base-1: Subtract 1 to convert to 0-based for Modbus protocol
        // Base-0: Use as-is
        return mode == ModbusAddressingMode.Base1 ? (ushort)(displayAddress - 1) : displayAddress;
    }
}
```

**4. UI Layout Structure (4-Row Pattern)**

Following the proven pattern:
- **Row 1**: Profile Management (DataGrid, CRUD buttons, Status display)
- **Row 2**: Connection Management (Connect/Disconnect buttons, Status indicator)
- **Row 3**: Power Control (On/Off buttons, Current state display)
- **Row 4**: Import/Export (File operations, Settings path management)

### Technical Considerations

**Dependencies:**
- **NModbus library** (v3.x or later) - APPROVED by user for Modbus TCP communication
- System.Net.Sockets for TCP/IP networking
- Existing service infrastructure (IUIThreadService, ILogger, etc.)

**Error Handling:**
- Network timeout handling
- Modbus protocol errors
- Invalid configuration detection
- Connection state management

**Testing Strategy:**
- Mock Modbus server for unit testing
- Connection timeout scenarios
- Invalid address/port handling
- State consistency validation

## Implementation Plan

### Phase 1: Core Models & Data Structures (3-4 hours)
**Status:** Pending  
**Location:** `S7Tools.Core/Models/`

**Deliverables:**
- [ ] `PowerSupplyType.cs` - Enum for device types (ModbusTcp, ModbusRtu, SNMP, HttpRest, Custom)
- [ ] `ModbusAddressingMode.cs` - Enum for addressing mode (Base0, Base1)
- [ ] `PowerSupplyConfiguration.cs` - Base configuration class (abstract)
- [ ] `ModbusTcpConfiguration.cs` - TCP Modbus configuration implementation with AddressingMode property
- [ ] `PowerSupplyProfile.cs` - Profile model with validation attributes
- [ ] `PowerSupplySettings.cs` - Settings integration model
- [ ] Update `ApplicationSettings.cs` - Add PowerSupply property

**Key Features:**
- Polymorphic configuration design for extensibility
- Comprehensive validation attributes
- Factory methods for default profiles
- Clone/duplicate functionality
- Metadata support for extensibility

**Validation Requirements:**
- Host: Required, valid hostname or IP address
- Port: Range 1-65535
- Device ID: Range 0-247 (Modbus slave address)
- Coil Address: Range depends on addressing mode
  - Base-0: Range 0-65535 (Modbus coil address, 0-based)
  - Base-1: Range 1-65536 (Display address, 1-based, converted to 0-based internally)
- Addressing Mode: Required, must be Base0 or Base1

### Phase 2: Service Layer Implementation (4-5 hours)
**Status:** Pending  
**Location:** `S7Tools.Core/Services/Interfaces/` and `S7Tools/Services/`

**Deliverables:**
- [ ] `IPowerSupplyProfileService.cs` - Profile management interface (Core)
- [ ] `IPowerSupplyService.cs` - Power supply operations interface (Core)
- [ ] `PowerSupplyProfileService.cs` - JSON-based profile persistence
- [ ] `PowerSupplyService.cs` - Modbus TCP communication implementation
- [ ] Service registration in `ServiceCollectionExtensions.cs`

**Key Features:**
- Profile CRUD operations with smart naming
- Modbus TCP connection management
- Read coil state (current power state)
- Write coil state (turn on/off)
- Connection testing functionality
- Import/export profile functionality

**Technical Details:**
- **NModbus integration** (approved library) for Modbus TCP protocol
- TCP connection lifecycle management
- Address conversion logic for base-0/base-1 addressing modes
- Retry logic for transient network failures
- Connection state tracking
- Thread-safe operations

### Phase 3: ViewModel Implementation (5-6 hours)
**Status:** Pending  
**Location:** `S7Tools/ViewModels/`

**Deliverables:**
- [ ] `PowerSupplyProfileViewModel.cs` - Individual profile editing
- [ ] `PowerSupplySettingsViewModel.cs` - Main settings category ViewModel

**PowerSupplyProfileViewModel Features:**
- Profile property editing (Name, Description, Type)
- Configuration editing (Host, Port, Device ID, Coil, Addressing Mode)
- Address display adjustment based on addressing mode
- Real-time validation feedback with mode-aware ranges
- Save/Cancel operations
- ReactiveUI individual property subscriptions

**PowerSupplySettingsViewModel Features:**
- Profile CRUD operations (Create, Edit, Delete, Duplicate, Set Default)
- Connection management (Connect, Disconnect, Test Connection)
- Power control (Turn On, Turn Off, Read State)
- Status monitoring (Connection state, Power state, Last operation result)
- Import/Export functionality
- Settings path management

**ReactiveUI Patterns:**
- Individual property subscriptions (avoid WhenAnyValue limits)
- Command enablement with reactive conditions
- Proper disposal patterns with CompositeDisposable
- Thread-safe UI updates with IUIThreadService

### Phase 4: UI Implementation (3-4 hours)
**Status:** Pending  
**Location:** `S7Tools/Views/`

**Deliverables:**
- [ ] `PowerSupplySettingsView.axaml` - Main settings view (4-row layout)
- [ ] `PowerSupplySettingsView.axaml.cs` - Code-behind with initialization
- [ ] `PowerSupplyProfileEditContent.axaml` - Profile editing dialog content
- [ ] `PowerSupplyProfileEditContent.axaml.cs` - Code-behind

**4-Row Layout Structure:**

```xml
<!-- Row 1: Profile Management -->
<Grid Row="0">
    <DataGrid ItemsSource="{Binding Profiles}" />
    <StackPanel> <!-- CRUD buttons -->
        <Button Command="{Binding CreateProfileCommand}">Add Profile</Button>
        <Button Command="{Binding EditProfileCommand}">Edit</Button>
        <Button Command="{Binding DeleteProfileCommand}">Delete</Button>
        <Button Command="{Binding DuplicateProfileCommand}">Duplicate</Button>
        <Button Command="{Binding SetDefaultCommand}">Set Default</Button>
    </StackPanel>
</Grid>

<!-- Row 2: Connection Management -->
<Grid Row="1">
    <TextBlock Text="Connection Status" />
    <StackPanel>
        <Button Command="{Binding ConnectCommand}">Connect</Button>
        <Button Command="{Binding DisconnectCommand}">Disconnect</Button>
        <Button Command="{Binding TestConnectionCommand}">Test Connection</Button>
    </StackPanel>
    <TextBlock Text="{Binding ConnectionStatus}" />
</Grid>

<!-- Row 3: Power Control -->
<Grid Row="2">
    <TextBlock Text="Power Control" />
    <StackPanel>
        <Button Command="{Binding TurnOnCommand}">Turn On</Button>
        <Button Command="{Binding TurnOffCommand}">Turn Off</Button>
        <Button Command="{Binding ReadStateCommand}">Read State</Button>
    </StackPanel>
    <TextBlock Text="{Binding PowerState}" />
</Grid>

<!-- Row 4: Import/Export -->
<Grid Row="3">
    <Button Command="{Binding ImportProfilesCommand}">Import</Button>
    <Button Command="{Binding ExportProfilesCommand}">Export</Button>
    <Button Command="{Binding BrowseProfilesPathCommand}">Browse</Button>
    <TextBlock Text="{Binding ProfilesPath}" />
</Grid>
```

**VSCode Styling:**
- Consistent color scheme with existing views
- Proper spacing and margins
- Status indicators with colors (green=connected, red=disconnected, yellow=connecting)
- Loading indicators for async operations

### Phase 5: Integration & Registration (2-3 hours)
**Status:** Pending  
**Location:** Various integration points

**Deliverables:**
- [ ] Update `SettingsViewModel.cs` - Add "Power Supply" category
- [ ] Create factory method `CreatePowerSupplySettingsViewModel()`
- [ ] Register services in `ServiceCollectionExtensions.cs`
- [ ] Verify DI container resolution
- [ ] Build verification (clean compilation)

**Integration Points:**
1. **Settings Navigation**: Add "Power Supply" to settings categories
2. **Service Registration**: All interfaces and implementations registered
3. **Profile Storage**: Default path `resources/PowerSupplyProfiles/`
4. **Default Profile**: Create system default for initial setup
5. **Build Verification**: Ensure clean compilation

### Phase 6: Testing & Validation (2-3 hours)
**Status:** Pending  
**Location:** `tests/S7Tools.Tests/` and `tests/S7Tools.Core.Tests/`

**Deliverables:**
- [ ] Unit tests for PowerSupplyProfile model
- [ ] Unit tests for ModbusTcpConfiguration
- [ ] Unit tests for PowerSupplyProfileService
- [ ] Integration tests for Modbus TCP communication
- [ ] UI manual testing and validation

**Test Coverage:**
- Model validation (valid/invalid configurations)
- Profile CRUD operations
- Modbus TCP connection scenarios
- Power control operations (On/Off)
- Error handling and edge cases
- Thread safety validation

**Manual Testing Checklist:**
- [ ] Navigate to Settings → Power Supply
- [ ] Create new profile with valid configuration
- [ ] Test connection to Modbus server
- [ ] Turn power supply on/off
- [ ] Read current power state
- [ ] Edit existing profile
- [ ] Delete profile (with confirmation)
- [ ] Duplicate profile
- [ ] Set default profile
- [ ] Import/Export profiles
- [ ] Verify profiles.json creation and updates

## Progress Tracking

**Overall Status:** Not Started - [0%] Complete

### Subtasks
| ID | Description | Status | Updated | Notes |
|----|-------------|--------|---------|-------|
| 1.1 | Create PowerSupplyType enum | Not Started | - | Extensible device type enumeration |
| 1.2 | Create PowerSupplyConfiguration base class | Not Started | - | Abstract base with polymorphism |
| 1.2a | Create ModbusAddressingMode enum | Not Started | - | Base0/Base1 addressing enumeration |
| 1.3 | Create ModbusTcpConfiguration class | Not Started | - | TCP Modbus with AddressingMode property |
| 1.4 | Create PowerSupplyProfile model | Not Started | - | Profile with validation attributes |
| 1.5 | Create PowerSupplySettings model | Not Started | - | Settings integration |
| 1.6 | Update ApplicationSettings | Not Started | - | Add PowerSupply property |
| 2.1 | Create IPowerSupplyProfileService interface | Not Started | - | Profile management contract |
| 2.2 | Create IPowerSupplyService interface | Not Started | - | Power supply operations contract |
| 2.3 | Implement PowerSupplyProfileService | Not Started | - | JSON persistence with CRUD |
| 2.4 | Implement PowerSupplyService | Not Started | - | NModbus TCP with address conversion |
| 2.5 | Register services in DI | Not Started | - | ServiceCollectionExtensions update |
| 3.1 | Create PowerSupplyProfileViewModel | Not Started | - | Profile editing with addressing mode |
| 3.2 | Create PowerSupplySettingsViewModel | Not Started | - | Main settings ViewModel |
| 3.3 | Implement ReactiveUI patterns | Not Started | - | Individual property subscriptions |
| 3.4 | Implement command logic | Not Started | - | Connect, Power On/Off, CRUD operations |
| 4.1 | Create PowerSupplySettingsView.axaml | Not Started | - | 4-row layout implementation |
| 4.2 | Create PowerSupplySettingsView.axaml.cs | Not Started | - | Code-behind initialization |
| 4.3 | Create PowerSupplyProfileEditContent.axaml | Not Started | - | Profile editing dialog |
| 4.4 | Apply VSCode styling | Not Started | - | Consistent theming |
| 5.1 | Update SettingsViewModel | Not Started | - | Add Power Supply category |
| 5.2 | Create factory method | Not Started | - | CreatePowerSupplySettingsViewModel() |
| 5.3 | Verify service registration | Not Started | - | DI container resolution |
| 5.4 | Build verification | Not Started | - | Clean compilation check |
| 6.1 | Write unit tests | Not Started | - | Models and services |
| 6.2 | Write integration tests | Not Started | - | Modbus TCP scenarios |
| 6.3 | Manual testing | Not Started | - | UI functionality validation |
| 6.4 | User acceptance testing | Not Started | - | Final validation |

## Progress Log

### 2025-10-13 - Initial Planning
- Task created based on user request for Power Supply settings implementation
- Analyzed requirements and designed architecture following established patterns
- Created comprehensive implementation plan with 6 phases
- Estimated 18-22 hours total development time
- Defined extensible architecture for multiple device types (starting with TCP Modbus)

### 2025-10-13 - User Approval and Requirements Update
- **User Feedback Received** from @efargas:
  - ✅ NModbus library approved for implementation
  - ✅ Physical Modbus PLC available for testing
  - ✅ Implementation plan approved
  - 🆕 **New Requirement**: Add base-0/base-1 addressing mode configuration option
- **Task Updated**:
  - Added ModbusAddressingMode enum (Base0, Base1)
  - Updated ModbusTcpConfiguration to include AddressingMode property
  - Added address conversion logic in service layer
  - Updated validation to handle mode-specific address ranges
  - Specified NModbus as the approved library throughout documentation
- **Status Changed**: Pending → Approved - Ready to Start

## Technical Notes

### NuGet Dependencies Required
- **NModbus** (v3.x or later) - APPROVED by user
  - Package: `NModbus` on NuGet
  - Mature, well-tested Modbus TCP/IP communication library
  - Full support for Modbus TCP protocol
  - Async operation support

### Modbus TCP Protocol Details

**Connection:**
- TCP/IP socket connection to Host:Port
- Modbus protocol over TCP (default port 502)

**Coil Operations:**
- **Read Coils (Function Code 01)**: Read current state
- **Write Single Coil (Function Code 05)**: Turn on/off
- Coil values: 0x0000 = OFF, 0xFF00 = ON

**Device ID:**
- Modbus slave address (0-247)
- Typical: 1 for single device, 1-247 for multi-drop

**Addressing Modes:**
- **Base-0 (0-based addressing)**: 
  - Coil addresses start at 0
  - First coil is address 0, second is 1, etc.
  - No conversion needed (matches Modbus protocol)
  - Used by: Most PLCs, Modbus specifications
  
- **Base-1 (1-based addressing)**:
  - Coil addresses start at 1 for user display
  - First coil is displayed as address 1 (internally 0)
  - Requires conversion: display_address - 1 = protocol_address
  - Used by: Some HMI systems, user-friendly interfaces
  
**Address Conversion Logic:**
```csharp
// Convert display address to protocol address based on mode
ushort protocolAddress = addressingMode == Base1 
    ? (ushort)(displayAddress - 1) 
    : displayAddress;
```

### Error Scenarios to Handle

1. **Network Errors:**
   - Connection timeout
   - Connection refused
   - Network unreachable
   - Host not found

2. **Modbus Protocol Errors:**
   - Invalid function code
   - Invalid data address
   - Slave device failure
   - Illegal data value

3. **Configuration Errors:**
   - Invalid host format
   - Port out of range
   - Device ID out of range
   - Coil address out of range

4. **State Errors:**
   - Operation while disconnected
   - Concurrent operation attempts
   - Invalid state transitions

### Thread Safety Considerations

- **Connection State**: Protected with SemaphoreSlim
- **UI Updates**: Marshal to UI thread with IUIThreadService
- **Profile Collection**: Thread-safe updates following established patterns
- **Async Operations**: Proper cancellation token support

### Performance Optimizations

- **Connection Pooling**: Reuse TCP connection for multiple operations
- **Async/Await**: Non-blocking I/O operations
- **Timeout Configuration**: Configurable timeouts for operations
- **Retry Logic**: Exponential backoff for transient failures

## Dependencies and Blockers

### Prerequisites
- [x] Build system operational
- [x] Test framework established
- [x] Service infrastructure available
- [x] Settings system functional

### Potential Blockers
- [x] NuGet package selection and approval - **RESOLVED: NModbus approved by user**
- [x] Hardware power supply for integration testing - **RESOLVED: User has Modbus PLC available**
- [ ] Network access for testing (assumed available)
- [ ] Modbus test server/simulator for unit testing (can mock if needed)

### Mitigation Strategies
- Use mock Modbus server for unit testing
- Implement comprehensive error handling
- Provide simulator mode for development
- Create detailed logging for troubleshooting

## Success Criteria

### Functional Requirements
- [x] Create, edit, delete, duplicate profiles
- [x] Connect/disconnect from Modbus server
- [x] Turn power supply on/off via Modbus coil
- [x] Read current power state
- [x] Test connection functionality
- [x] Import/export profiles
- [x] Persist settings to JSON

### Quality Requirements
- [x] Clean compilation (0 errors)
- [x] Comprehensive unit tests (>80% coverage)
- [x] Clean Architecture compliance
- [x] ReactiveUI best practices followed
- [x] Thread-safe operations
- [x] Structured logging throughout

### User Experience Requirements
- [x] Intuitive 4-row layout
- [x] VSCode consistent styling
- [x] Real-time status updates
- [x] Clear error messages
- [x] Responsive UI (no blocking operations)
- [x] Professional visual design

## Architecture Compliance Checklist

- [ ] **Clean Architecture**: Interfaces in Core, implementations in Application
- [ ] **Dependency Inversion**: All dependencies flow toward Core
- [ ] **SOLID Principles**: Single responsibility, open/closed, etc.
- [ ] **Service Registration**: All services properly registered in DI
- [ ] **MVVM Compliance**: Proper separation of View/ViewModel/Model
- [ ] **ReactiveUI Patterns**: Individual property subscriptions
- [ ] **Error Handling**: Comprehensive exception handling
- [ ] **Logging**: Structured logging with ILogger
- [ ] **XML Documentation**: All public APIs documented
- [ ] **Testing**: Comprehensive unit and integration tests

## References

### Successful Implementations to Follow
1. **Serial Ports Settings** - Complete reference implementation
   - 4-row UI layout pattern
   - Profile management system
   - ReactiveUI optimization patterns
   - Thread-safe operations

2. **Socat Settings** - Network service management
   - Process lifecycle management
   - Connection status monitoring
   - Command generation and validation

### Key Patterns to Reuse
1. **Profile-Based Configuration** - Named profiles with metadata
2. **Service-Oriented Design** - Interfaces and implementations
3. **ReactiveUI Optimization** - Individual property subscriptions
4. **Thread-Safe UI Updates** - IUIThreadService integration
5. **4-Row Layout Structure** - Consistent settings UI design

### Documentation References
- **AGENTS.md** - Development patterns and best practices
- **systemPatterns.md** - Architecture and design patterns
- **Project_Folders_Structure_Blueprint.md** - File placement patterns
- **mvvm-lessons-learned.md** - ReactiveUI best practices

---

**Next Action:** Begin Phase 1 - Core Models & Data Structures
**Estimated Start:** Upon user approval and task prioritization
**Required Resources:** NuGet package selection, Modbus protocol documentation
