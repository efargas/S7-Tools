# [TASK003] - Servers Settings Category Implementation (socat Configuration)

**Status:** In Progress
**Added:** 2025-10-09
**Updated:** 2025-10-09
**Priority:** High
**Progress:** 85% Complete - Phase 4 UI Implementation finished

## Original Request

Implement a new settings category "Servers" for configuring socat (Serial-to-TCP Proxy). The application will connect to PLC's serial port via TCP socket using socat to forward the serial device to a TCP port.

### Command Pattern to Support
```bash
socat -v -b 4 -x TCP-LISTEN:1238,fork,reuseaddr ${SERIAL_DEV}
```

### Configuration Requirements
- **Profile-based configuration** (similar to Serial Ports Settings)
- **TCP Listen Port** configuration
- **TCP Host** configuration
- **Serial Device** selection (not saved in profile, dynamically selected)
- **socat flags**: verbose (-v), hex dump (-x), block size (-b)
- **Additional parameters**: fork, reuseaddr, and other socat options

## Thought Process

### Analysis of Reference Project
From the SiemensS7-Bootloader project, I identified the following socat configuration parameters:

**From Settings.json:**
- `SocatTcpPort`: 1238 (TCP listen port)
- `SocatVerbose`: true (maps to -v flag)
- `SocatHexDump`: true (maps to -x flag)
- `SocatBlockSize`: 4 (maps to -b flag)

**From SocatService.cs analysis:**
- Additional flags: `-d -d` (debug level when verbose)
- Serial device parameters: `raw,echo=0`
- TCP parameters: `fork,reuseaddr`
- Automatic stty configuration before socat startup

### socat Command Structure Analysis
```bash
socat [flags] TCP-LISTEN:port,fork,reuseaddr /dev/device,raw,echo=0
```

**Flags:**
- `-v`: Verbose logging
- `-d -d`: Debug level (double debug when verbose enabled)
- `-b size`: Block size for transfers
- `-x`: Hex dump of transferred data

**TCP-LISTEN Parameters:**
- `port`: TCP port to listen on
- `fork`: Allow multiple concurrent connections
- `reuseaddr`: Reuse socket addresses

**Serial Device Parameters:**
- `raw`: Raw mode (no character processing)
- `echo=0`: Disable echo

### Architecture Decision
Following the established Serial Ports Settings pattern:
1. **Core Models** - SocatProfile, SocatConfiguration, SocatSettings
2. **Service Layer** - ISocatProfileService, ISocatService
3. **ViewModels** - SocatSettingsViewModel, SocatProfileViewModel
4. **UI Implementation** - 4-row layout with command preview
5. **Integration** - Add to Settings categories with DI registration

### Key Differences from Serial Settings
- **Dynamic Serial Device** - Device path selected at runtime, not stored in profile
- **Network Parameters** - TCP host/port configuration
- **Process Management** - Start/stop socat process, handle multiple instances
- **Real-time Logging** - Capture socat output for debugging

## Implementation Plan

### Phase 1: Core Models and Data Structures (2-3 hours)
- **SocatProfile.cs** - Profile model with socat-specific properties
- **SocatConfiguration.cs** - Complete socat flags and parameters
- **SocatSettings.cs** - Settings integration model
- **ApplicationSettings.cs** - Add Socat property

### Phase 2: Service Layer Implementation (3-4 hours)
- **ISocatProfileService.cs** - Profile management interface
- **ISocatService.cs** - socat process and command interface
- **SocatProfileService.cs** - JSON-based profile persistence
- **SocatService.cs** - socat command generation and process management
- **Service Registration** - Register in ServiceCollectionExtensions.cs

### Phase 3: ViewModel Implementation (4-5 hours)
- **SocatProfileViewModel.cs** - Individual profile ViewModel with command preview
- **SocatSettingsViewModel.cs** - Main settings category ViewModel
- **SocatConnectionViewModel.cs** - Connection management and status monitoring
- **Reactive patterns** - Follow established ReactiveUI optimization patterns

### Phase 4: UI Implementation (2-3 hours)
- **SocatSettingsView.axaml** - Main settings category view with 4-row layout
- **SocatSettingsView.axaml.cs** - Code-behind implementation
- **Command preview** - Real-time socat command generation display
- **Status indicators** - Connection status and process monitoring

### Phase 5: Integration & Registration (1-2 hours)
- **Settings Integration** - Update SettingsViewModel for Servers category
- **Service Registration** - Complete DI container registration
- **Navigation** - Add Servers to settings navigation

### Phase 6: Testing & Validation (2-3 hours)
- **Profile Management** - Create, edit, delete, import/export functionality
- **Command Generation** - Verify socat command accuracy
- **Process Management** - Test start/stop operations
- **User Validation** - Manual testing and feedback incorporation

## Progress Tracking

**Overall Status:** 67% Complete - Phase 3 ViewModel Implementation finished

### Subtasks
| ID | Description | Status | Updated | Notes |
|----|-------------|--------|---------|-------|
| 1.1 | Research socat parameters and create complete configuration model | ✅ Complete | 2025-10-09 | socat command structure and parameters fully analyzed |
| 1.2 | Create SocatProfile model with validation attributes | ✅ Complete | 2025-10-09 | Complete with factory methods and business logic |
| 1.3 | Create SocatConfiguration model with all flags and parameters | ✅ Complete | 2025-10-09 | Includes command generation and factory methods |
| 1.4 | Create SocatSettings model for settings integration | ✅ Complete | 2025-10-09 | Complete settings integration model |
| 1.5 | Update ApplicationSettings.cs to include Socat property | ✅ Complete | 2025-10-09 | Integration complete with Clone() method updated |
| 2.1 | Create ISocatProfileService interface in Core project | ✅ Complete | 2025-10-09 | Profile management contract already implemented |
| 2.2 | Create ISocatService interface in Core project | ✅ Complete | 2025-10-09 | socat operations contract already implemented |
| 2.3 | Implement SocatProfileService with JSON persistence | ✅ Complete | 2025-10-09 | Following SerialPortProfileService pattern, complete |
| 2.4 | Implement SocatService with command generation and process management | ✅ Complete | 2025-10-09 | Based on reference project, process management implemented |
| 2.5 | Register services in ServiceCollectionExtensions.cs | ✅ Complete | 2025-10-09 | DI integration complete (lines 89-90) |
| 3.1 | Create SocatProfileViewModel with reactive patterns | ✅ Complete | 2025-10-09 | 892 lines, comprehensive profile editing with validation |
| 3.2 | Create SocatSettingsViewModel for main category | ✅ Complete | 2025-10-09 | 1243 lines, full profile management and process control |
| 3.3 | Register ViewModels in DI container | ✅ Complete | 2025-10-09 | ServiceCollectionExtensions.cs updated |
| 3.4 | Add "Servers" category to settings navigation | ✅ Complete | 2025-10-09 | SettingsViewModel updated with factory method |
| 3.5 | Apply user manual edits and verify build | ✅ Complete | 2025-10-09 | User modifications applied, clean compilation verified |
| 4.1 | Create SocatSettingsView with 4-row layout | ✅ Complete | 2025-10-09 | 673 lines of comprehensive XAML with VSCode styling |
| 4.2 | Implement profile management UI components | ✅ Complete | 2025-10-09 | DataGrid, CRUD buttons, status displays complete |
| 4.3 | Add command preview and connection status UI | ✅ Complete | 2025-10-09 | Real-time command display and status monitoring |
| 4.4 | Implement VSCode-style theming and layout | ✅ Complete | 2025-10-09 | Consistent with existing settings categories |
| 5.1 | Final integration testing and verification | Not Started | | Complete end-to-end testing |
| 5.2 | Complete service registration verification | Not Started | | Ensure all services registered |
| 5.3 | Test integration with existing settings system | Not Started | | Verify navigation and functionality |
| 6.1 | Test profile management functionality | Not Started | | Create, edit, delete, import/export |
| 6.2 | Test socat command generation accuracy | Not Started | | Verify command matches expected pattern |
| 6.3 | Test process start/stop operations | Not Started | | Verify socat process management |
| 6.4 | User validation and feedback incorporation | Not Started | | Manual testing and adjustments |

## Progress Log

### 2025-10-09 - Phase 4 COMPLETE ✅ (UI Implementation)
- **Major Milestone**: Phase 4 UI Implementation successfully completed
- **UI Files Created**:
  - `SocatSettingsView.axaml` (673 lines) - Comprehensive 4-row layout with complete data binding
  - `SocatSettingsView.axaml.cs` - Code-behind file with proper initialization
- **Layout Features**:
  - **Row 1**: Profile Management (DataGrid, Add/Edit/Delete/Duplicate buttons, Status display)
  - **Row 2**: Device Discovery (Device list, refresh controls, selection feedback)
  - **Row 3**: Process Management (Start/Stop controls, Status monitoring, Command preview)
  - **Row 4**: Import/Export (File operations, Settings management)
- **Technical Challenges Resolved**:
  - Fixed XAML compilation issues (line breaks in StringFormat attributes)
  - Corrected FallbackValue syntax split across lines
  - Applied consistent bullet point format ('• {0}') matching SerialPortsSettingsView
- **Build Verification**: Clean compilation (warnings only, 0 errors)
- **Pattern Consistency**: Follows established Serial Ports Settings UI structure exactly
- **VSCode Styling**: Consistent theming and layout with existing categories
- **Next Steps**: Begin Phase 5 - Final Integration & Registration verification

### 2025-10-09 - Phase 3 COMPLETE ✅ (ViewModels Implementation)
- **Major Milestone**: Phase 3 ViewModel Implementation successfully completed
- **ViewModels Created**:
  - `SocatProfileViewModel.cs` (892 lines) - Individual profile editing with comprehensive validation, real-time command generation, ReactiveUI optimization, clipboard integration, preset system
  - `SocatSettingsViewModel.cs` (1243 lines) - Main settings category with CRUD operations, process management, serial port integration, import/export, status monitoring
- **Integration Complete**:
  - DI Registration: Both ViewModels registered in ServiceCollectionExtensions.cs
  - Settings Navigation: "Servers" category added to SettingsViewModel
  - Factory Method: CreateSocatSettingsViewModel() implemented with full dependency injection
- **User Manual Edits**: Post-AI implementation, user made manual edits to both ViewModels
- **Build Verification**: Clean compilation (41 warnings, 0 errors)
- **Technical Excellence**: ReactiveUI optimization, Clean Architecture compliance, comprehensive error handling
- **Next Steps**: Begin Phase 4 - UI Implementation (4-row layout)

### 2025-10-09 - Phase 1 COMPLETE ✅
- **Core Models Created**: All 4 models successfully implemented
  - `SocatProfile.cs` - Complete profile model with factory methods (Default, User, HighSpeed, Debug)
  - `SocatConfiguration.cs` - Complete configuration with command generation and validation
  - `SocatSettings.cs` - Complete settings integration following SerialPortSettings pattern
  - `ApplicationSettings.cs` - Updated with Socat property integration
- **Technical Excellence**: Clean Architecture compliance, comprehensive validation, XML documentation
- **Build Verification**: Clean compilation (246 warnings, 0 errors)
- **Architecture Patterns**: Following established SerialPortProfile/Configuration/Settings patterns exactly
- **Next Steps**: Begin Phase 2 - Service Layer Implementation

### 2025-10-09
- Created TASK003 for Servers settings implementation
- Analyzed reference project (SiemensS7-Bootloader) socat configuration
- Identified key parameters: SocatTcpPort, SocatVerbose, SocatHexDump, SocatBlockSize
- Examined SocatService.cs for command structure and process management patterns
- Found additional parameters: debug levels (-d -d), fork, reuseaddr, raw mode, echo=0
- Established implementation plan following Serial Ports Settings successful pattern
- Status: Starting Phase 1 - socat parameter research and model design

### Next Steps
1. Complete socat parameter research and documentation
2. Create core models (SocatProfile, SocatConfiguration, SocatSettings)
3. Begin service layer implementation

## socat Configuration Model

### Core Parameters (From Reference Project)
```json
{
  "SocatTcpPort": 1238,
  "SocatVerbose": true,
  "SocatHexDump": true,
  "SocatBlockSize": 4
}
```

### socat Command Structure
```bash
# Full command with all flags
socat -d -d -v -b 4 -x TCP-LISTEN:1238,fork,reuseaddr /dev/ttyUSB0,raw,echo=0

# Flag breakdown:
# -d -d    : Double debug level (when verbose enabled)
# -v       : Verbose logging
# -b 4     : Block size 4 bytes
# -x       : Hex dump of data
# TCP-LISTEN:1238,fork,reuseaddr : Listen on port 1238, allow multiple connections, reuse addresses
# /dev/device,raw,echo=0 : Serial device in raw mode with echo disabled
```

### Additional Configuration Needed
- **TCP Host** - Interface to bind to (default: all interfaces)
- **Connection Timeout** - TCP connection timeout
- **Keep-Alive Settings** - TCP keep-alive configuration
- **Log Level** - socat verbosity levels
- **Buffer Settings** - Additional buffer configuration

## Success Criteria

### Technical Requirements
- [ ] Clean Architecture compliance maintained
- [ ] All services properly registered in DI container
- [ ] ReactiveUI patterns follow established optimization
- [ ] socat command generation matches reference implementation
- [ ] Profile management follows Serial Ports Settings pattern

### Functional Requirements
- [ ] Profile CRUD operations (Create, Read, Update, Delete)
- [ ] Import/export functionality for profiles
- [ ] Real-time socat command preview
- [ ] Process start/stop with status monitoring
- [ ] Error handling and user feedback

### Quality Requirements
- [ ] Comprehensive XML documentation for all public APIs
- [ ] Structured logging throughout implementation
- [ ] User-friendly error messages and status updates
- [ ] Clean build (0 errors, minimal warnings)

### User Validation Requirements
- [ ] Navigate to Settings > Servers successfully
- [ ] Create and manage socat profiles
- [ ] Generate valid socat commands
- [ ] Start/stop socat processes
- [ ] Export/import profile configurations

---

**Task Owner**: Development Team with AI Assistance
**Next Session Focus**: Complete Phase 1 - Core Models and Data Structures
**Dependencies**: Serial Ports Settings implementation (completed), socat utility installed on target system

**Key Insight**: This implementation will reuse all established patterns from Serial Ports Settings while adding network/process management capabilities specific to socat operations.
