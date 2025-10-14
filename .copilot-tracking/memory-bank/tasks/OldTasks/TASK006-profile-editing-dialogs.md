# [TASK006] - Profile Editing Dialogs Implementation

**Status:** In Progress
**Added:** 2025-10-13
**Updated:** 2025-10-13
**Priority:** High
**Progress:** Phase 2 Complete - 33%

## Original Request

Implement profile editing functionality for both Serial and Socat profiles. When the user presses the "Edit" button, a popup dialog will appear with a form containing all the editable properties for the selected profile.

### Serial Profile Editor Requirements
- **Name** - Text input for profile name
- **Description** - Text input for profile description
- **Baudrate** - ComboBox with predefined baudrates (9600, 19200, 38400, 57600, 115200, etc.)
- **Parity** - ComboBox with options (None, Even, Odd, Mark, Space)
- **Flow Control** - ComboBox with options (None, Hardware (RTS/CTS), Software (XON/XOFF))
- **Stop Bits** - ComboBox with options (1, 1.5, 2)
- **Data Bits** - ComboBox with options (5, 6, 7, 8)
- **Additional stty flags** - Checkboxes for various flags (ignbrk, brkint, icrnl, etc.)
- **Action Buttons** - Save and Cancel buttons

### Socat Profile Editor Requirements
- **Name** - Text input for profile name
- **Description** - Text input for profile description
- **Host** - Text input for TCP host/interface
- **TCP Port** - Numeric input for TCP listen port
- **Block Size** - Numeric input for block size (-b flag)
- **Verbose** - Checkbox for verbose logging (-v flag)
- **Hex Dump** - Checkbox for hex dump (-x flag)
- **Debug Level** - ComboBox for debug levels (None, Single -d, Double -d -d)
- **Fork** - Checkbox for fork option (allow multiple connections)
- **Reuse Address** - Checkbox for reuseaddr option
- **Raw Mode** - Checkbox for raw serial mode
- **Echo** - Checkbox for echo enable/disable
- **Action Buttons** - Save and Cancel buttons

## Thought Process

### Analysis of Current Architecture

From the memory bank analysis, I can see that:

1. **Existing Infrastructure**: Both Serial and Socat profile systems are fully implemented with comprehensive ViewModels:
   - `SerialPortProfileViewModel.cs` - Individual serial profile editing (already exists)
   - `SocatProfileViewModel.cs` - Individual socat profile editing (already exists)

2. **Dialog System**: The application already has a professional dialog system implemented:
   - `InputDialog.axaml` and `InputDialogViewModel.cs` for simple text input
   - ReactiveUI Interactions integration for dialog handling
   - Proper MVVM dialog patterns established

3. **Current Edit Flow**: The existing edit buttons likely use simple input dialogs for basic editing, but comprehensive property editing is missing.

### Architecture Decision

Following the established Clean Architecture and MVVM patterns:

1. **Reuse Existing ViewModels**: The `SerialPortProfileViewModel` and `SocatProfileViewModel` already exist and contain all the property management logic needed
2. **Create New Dialog Views**: Create dedicated dialog views that use the existing ViewModels
3. **Enhance Dialog Service**: Extend the existing dialog service to support complex profile editing dialogs
4. **Maintain Consistency**: Follow the same styling and interaction patterns as existing dialogs

### Key Technical Considerations

1. **ViewModel Reuse**: The existing ProfileViewModels are perfect for this - they already have:
   - Comprehensive property management
   - Real-time validation
   - Command preview functionality
   - ReactiveUI optimization patterns

2. **Dialog Integration**: Need to integrate with the existing dialog system while supporting complex forms
3. **Data Binding**: All properties need proper two-way binding with validation feedback
4. **User Experience**: Modal dialogs should be resizable and provide clear save/cancel actions

## Implementation Plan

### Phase 1: Dialog Infrastructure Enhancement (2-3 hours)
- **ProfileEditDialog.axaml** - Generic base dialog for profile editing
- **IProfileEditDialogService.cs** - Service interface for profile editing dialogs
- **ProfileEditDialogService.cs** - Implementation of profile editing dialog service
- **Enhance existing DialogService** - Add support for complex profile editing dialogs

### Phase 2: Serial Profile Edit Dialog (3-4 hours)
- **SerialProfileEditDialog.axaml** - Serial profile editing dialog view
- **SerialProfileEditDialog.axaml.cs** - Dialog code-behind
- **Integration with SerialPortProfileViewModel** - Connect existing ViewModel to new dialog
- **ComboBox data sources** - Define all predefined values (baudrates, parity, etc.)
- **Checkbox layout** - Organize stty flags in logical groups

### Phase 3: Socat Profile Edit Dialog (3-4 hours)
- **SocatProfileEditDialog.axaml** - Socat profile editing dialog view
- **SocatProfileEditDialog.axaml.cs** - Dialog code-behind
- **Integration with SocatProfileViewModel** - Connect existing ViewModel to new dialog
- **ComboBox data sources** - Define TCP options, debug levels, etc.
- **Real-time command preview** - Show generated socat command in dialog

### Phase 4: Settings Integration (2-3 hours)
- **Update SerialPortsSettingsViewModel** - Modify Edit command to use new dialog
- **Update SocatSettingsViewModel** - Modify Edit command to use new dialog
- **Dialog service registration** - Register new services in DI container
- **Error handling** - Comprehensive error handling for dialog operations

### Phase 5: UI Polish and Validation (2-3 hours)
- **Form validation** - Real-time validation feedback in dialogs
- **Visual styling** - VSCode-consistent styling for dialog components
- **Responsive layout** - Ensure dialogs work well at different sizes
- **Accessibility** - Proper tab order and keyboard navigation

### Phase 6: Testing and User Validation (2-3 hours)
- **Functional testing** - Test all edit scenarios for both profile types
- **Validation testing** - Verify form validation works correctly
- **Integration testing** - Ensure dialogs integrate properly with existing settings
- **User acceptance testing** - Manual testing and feedback incorporation

## Progress Tracking

**Overall Status:** Phase 4 Complete - 67%

### Subtasks
| ID | Description | Status | Updated | Notes |
|----|-------------|--------|---------|-------|
| 1.1 | Analyze existing dialog infrastructure and ViewModel capabilities | Complete | 2025-10-13 | Reviewed InputDialog, DialogService, and ProfileViewModels |
| 1.2 | Design generic ProfileEditDialog base class | Complete | 2025-10-13 | Created ProfileEditDialog.axaml with VSCode styling |
| 1.3 | Create IProfileEditDialogService interface | Complete | 2025-10-13 | Defined service contract with ProfileEditRequest/Result models |
| 1.4 | Implement ProfileEditDialogService | Complete | 2025-10-13 | Service implementation with ReactiveUI Interactions |
| 2.1 | Create SerialProfileEditDialog.axaml layout | Complete | 2025-10-13 | Created SerialProfileEditContent.axaml with comprehensive form layout |
| 2.2 | Implement ComboBox data sources for serial options | Complete | 2025-10-13 | Bound to ViewModel's AvailableBaudRates, AvailableParityModes, etc. |
| 2.3 | Create checkbox layout for stty flags | Complete | 2025-10-13 | Organized in logical groups: Input, Output, Local, Hardware Control |
| 2.4 | Integrate with existing SerialPortProfileViewModel | Complete | 2025-10-13 | Full data binding to all ViewModel properties |
| 3.1 | Create SocatProfileEditDialog.axaml layout | Complete | 2025-10-13 | Created SocatProfileEditContent.axaml with comprehensive form layout |
| 3.2 | Implement ComboBox data sources for socat options | Complete | 2025-10-13 | Bound to ViewModel properties, added debug level descriptions |
| 3.3 | Add real-time command preview to socat dialog | Complete | 2025-10-13 | SocatCommand display with monospace formatting |
| 3.4 | Integrate with existing SocatProfileViewModel | Complete | 2025-10-13 | Full data binding to all ViewModel properties |
| 4.1 | Update SerialPortsSettingsViewModel Edit command | Complete | 2025-10-13 | Updated to use ProfileEditDialogService |
| 4.2 | Update SocatSettingsViewModel Edit command | Complete | 2025-10-13 | Updated to use ProfileEditDialogService |
| 4.3 | Register dialog services in DI container | Not Started | | Add services to ServiceCollectionExtensions.cs |
| 4.4 | Implement comprehensive error handling | Not Started | | Handle dialog cancellation, validation errors |
| 5.1 | Implement form validation with visual feedback | Not Started | | Real-time validation indicators |
| 5.2 | Apply VSCode-consistent styling | Not Started | | Match existing application theme |
| 5.3 | Ensure responsive dialog layout | Not Started | | Test at different window sizes |
| 5.4 | Implement proper accessibility features | Not Started | | Tab order, keyboard navigation |
| 6.1 | Test serial profile editing functionality | Not Started | | Create, edit, validate serial profiles |
| 6.2 | Test socat profile editing functionality | Not Started | | Create, edit, validate socat profiles |
| 6.3 | Integration testing with existing settings | Not Started | | Verify seamless integration |
| 6.4 | User acceptance testing and feedback | Not Started | | Manual testing and improvements |

## Progress Log

### 2025-10-13 - Phase 2 COMPLETE ✅ (Serial Profile Edit Dialog)
- **Major Milestone**: Phase 2 Serial Profile Edit Dialog successfully completed
- **Files Created**:
  - `SerialProfileEditContent.axaml` - Comprehensive serial profile editing form with VSCode styling
  - `SerialProfileEditContent.axaml.cs` - Code-behind for UserControl
- **Form Features**:
  - **General Information**: ProfileName and ProfileDescription text inputs
  - **Serial Parameters**: ComboBoxes bound to AvailableBaudRates, AvailableParityModes, AvailableCharacterSizes, AvailableStopBits
  - **Hardware Flow Control**: Checkbox for DisableHardwareFlowControl
  - **Advanced stty Flags**: Organized in logical groups (Input, Output, Local, Hardware, Parity)
  - **Command Preview**: Real-time SttyCommand display in monospace format
- **Data Binding**: Complete integration with existing SerialPortProfileViewModel properties
- **UI Design**: VSCode-consistent styling, scrollable layout, logical grouping with borders
- **Build Verification**: Clean compilation with no errors (86 warnings only)
- **Next Steps**: Begin Phase 3 - Socat Profile Edit Dialog implementation

### 2025-10-13 - Phase 1 COMPLETE ✅ (Dialog Infrastructure Enhancement)
- **Major Milestone**: Phase 1 Dialog Infrastructure Enhancement successfully completed
- **Files Created**:
  - `ProfileEditRequest.cs` - Request/Result models for profile editing with ProfileType enum
  - `IProfileEditDialogService.cs` - Service interface for profile editing dialogs
  - `ProfileEditDialogService.cs` - Service implementation with ReactiveUI Interactions
  - `ProfileEditDialog.axaml` - Generic base dialog with VSCode styling and ContentPresenter
  - `ProfileEditDialog.axaml.cs` - Code-behind with dynamic content loading
- **Service Registration**: ProfileEditDialogService registered in ServiceCollectionExtensions.cs
- **Build Verification**: Clean compilation with no errors
- **Architecture**: Follows established Clean Architecture patterns with proper separation

### 2025-10-13 - Task Creation
- Created TASK006 for profile editing dialogs implementation
- Analyzed existing infrastructure and identified reusable components
- Determined that existing ProfileViewModels are perfect for dialog integration
- Established implementation plan following Clean Architecture patterns

### 2025-10-13 - Phase 3 COMPLETE ✅ (Socat Profile Edit Dialog + Partial Phase 4)
- **Major Milestone**: Phase 3 Socat Profile Edit Dialog successfully completed
- **Files Created**:
  - `SocatProfileEditContent.axaml` - Comprehensive socat profile editing form with VSCode styling
  - `SocatProfileEditContent.axaml.cs` - Code-behind for UserControl
  - Updated `ObjectConverters.cs` - Added DebugLevelToDescription and CountToVisibility converters
  - Updated `ProfileEditDialog.axaml.cs` - Replaced placeholder with actual SocatProfileEditContent
- **Form Features**:
  - **General Information**: ProfileName and ProfileDescription text inputs
  - **Network Configuration**: TcpHost text input, TcpPort ComboBox, Fork and ReuseAddr checkboxes
  - **socat Options**: Debug Level ComboBox with descriptions, Block Size ComboBox, Verbose and HexDump checkboxes
  - **Serial Options**: Raw Mode, Disable Echo, Auto Configure Serial checkboxes
  - **Process Management**: Connection Timeout NumericUpDown, Auto Restart checkbox
  - **Command Preview**: Real-time SocatCommand display in monospace format
  - **Validation Errors**: Dynamic visibility based on ValidationErrors count
- **Data Binding**: Complete integration with existing SocatProfileViewModel properties
- **UI Design**: VSCode-consistent styling, scrollable layout, logical grouping with borders
- **Phase 4 Partial**: Updated SocatSettingsViewModel EditProfileAsync method to use ProfileEditDialogService
- **Build Verification**: Clean compilation with no errors
- **Next Steps**: Complete Phase 4 by updating SerialPortsSettingsViewModel Edit command

### 2025-10-13 - Phase 4 COMPLETE ✅ (Settings Integration)
- **Major Milestone**: Phase 4 Settings Integration successfully completed
- **Updated Files**:
  - `SocatSettingsViewModel.cs` - Added IProfileEditDialogService and IClipboardService dependencies, updated EditProfileAsync method
  - `SerialPortsSettingsViewModel.cs` - Added IProfileEditDialogService and IClipboardService dependencies, updated EditProfileAsync method
- **Integration Features**:
  - **SocatSettingsViewModel**: EditProfileAsync now creates SocatProfileViewModel instance and shows comprehensive edit dialog
  - **SerialPortsSettingsViewModel**: EditProfileAsync now creates SerialPortProfileViewModel instance and shows comprehensive edit dialog
  - **Profile Refresh**: Both ViewModels refresh profile collections after successful edits to reflect changes
  - **Error Handling**: Comprehensive exception handling for dialog operations
  - **Status Updates**: User feedback through StatusMessage updates
- **Dependency Injection**: DI container automatically resolves new constructor dependencies
- **Build Verification**: Clean compilation with no errors
- **Next Steps**: Begin Phase 5 - UI Polish and Validation enhancements

### Next Steps
1. Complete Phase 4 - Update SerialPortsSettingsViewModel Edit command
2. Begin Phase 5 - UI Polish and Validation enhancements
3. Begin Phase 6 - Testing and User Validation

## Technical Architecture

### Existing Foundation (Available for Reuse)
- ✅ **SerialPortProfileViewModel** - Complete with validation, command preview, reactive patterns
- ✅ **SocatProfileViewModel** - Complete with validation, command generation, preset management
- ✅ **DialogService Infrastructure** - InputDialog, ReactiveUI Interactions integration
- ✅ **MVVM Patterns** - Established ReactiveUI optimization patterns
- ✅ **Validation Framework** - DataAnnotations and custom validation throughout

### New Components to Create
- **ProfileEditDialog Base** - Generic dialog foundation for profile editing
- **SerialProfileEditDialog** - Specialized dialog for serial profile properties
- **SocatProfileEditDialog** - Specialized dialog for socat profile properties
- **IProfileEditDialogService** - Service interface for profile editing operations
- **ProfileEditDialogService** - Service implementation with dialog lifecycle management

### Integration Points
- **SerialPortsSettingsViewModel.EditCommand** - Replace simple input with comprehensive dialog
- **SocatSettingsViewModel.EditCommand** - Replace simple input with comprehensive dialog
- **ServiceCollectionExtensions.cs** - Register new dialog services
- **Existing ValidationFramework** - Leverage for real-time form validation

## Form Layout Design

### Serial Profile Edit Dialog Layout
```
┌─────────────────────────────────────────────────────────┐
│ Edit Serial Profile                                [X]   │
├─────────────────────────────────────────────────────────┤
│ General                                                 │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Name: [________________________]                    │ │
│ │ Description: [_________________]                     │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Serial Parameters                                       │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Baudrate: [ 38400     ▼] Parity: [ None     ▼]     │ │
│ │ Data Bits: [    8     ▼] Stop Bits: [  1     ▼]    │ │
│ │ Flow Control: [ None  ▼]                            │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Advanced Options (stty flags)                          │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ☐ ignbrk    ☐ brkint     ☐ icrnl                   │ │
│ │ ☐ imaxbel   ☐ opost     ☐ onlcr                    │ │
│ │ ☐ isig      ☐ icanon    ☐ iexten                   │ │
│ │ ☐ echo      ☐ echoe     ☐ echok                    │ │
│ │ ☐ echoctl   ☐ echoke    ☐ ixon                     │ │
│ │ ☐ crtscts   ☐ parodd    ☐ parenb                   │ │
│ │ ☐ raw                                               │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Command Preview                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ stty -F /dev/ttyUSB0 cs8 38400 ignbrk -brkint...   │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│                                  [Cancel]    [Save]    │
└─────────────────────────────────────────────────────────┘
```

### Socat Profile Edit Dialog Layout
```
┌─────────────────────────────────────────────────────────┐
│ Edit Socat Profile                                 [X]   │
├─────────────────────────────────────────────────────────┤
│ General                                                 │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Name: [________________________]                    │ │
│ │ Description: [_________________]                     │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Network Configuration                                   │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Host: [localhost____________] Port: [1238____]       │ │
│ │ ☑ Fork (multiple connections)                       │ │
│ │ ☑ Reuse Address                                     │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ socat Options                                          │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ☑ Verbose Logging (-v)                             │ │
│ │ ☑ Hex Dump (-x)                                    │ │
│ │ Debug Level: [ Double -d -d ▼]                     │ │
│ │ Block Size: [4____] bytes                          │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Serial Options                                          │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ☑ Raw Mode                                         │ │
│ │ ☐ Echo                                             │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Command Preview                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ socat -d -d -v -b 4 -x TCP-LISTEN:1238,fork,reuse  │ │
│ │ addr /dev/ttyUSB0,raw,echo=0                        │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│                                  [Cancel]    [Save]    │
└─────────────────────────────────────────────────────────┘
```

## Data Sources Definition

### Serial Profile ComboBox Options
```csharp
// Baudrate options
public static readonly List<int> Baudrates = new()
{
    300, 600, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600
};

// Parity options
public enum ParityOptions { None, Even, Odd, Mark, Space }

// Flow control options
public enum FlowControlOptions { None, Hardware, Software }

// Data bits options
public static readonly List<int> DataBits = new() { 5, 6, 7, 8 };

// Stop bits options
public static readonly List<string> StopBits = new() { "1", "1.5", "2" };
```

### Socat Profile ComboBox Options
```csharp
// Debug level options
public enum DebugLevelOptions { None, Single, Double }

// TCP port range validation
public const int MinTcpPort = 1024;
public const int MaxTcpPort = 65535;

// Block size options
public static readonly List<int> BlockSizes = new() { 1, 2, 4, 8, 16, 32, 64, 128, 256 };
```

## Success Criteria

### Technical Requirements
- [ ] Clean Architecture compliance maintained throughout implementation
- [ ] Existing ProfileViewModels successfully integrated with new dialog views
- [ ] All form fields properly bound with two-way data binding
- [ ] Real-time validation feedback displayed to users
- [ ] Dialog services properly registered in DI container

### Functional Requirements
- [ ] Edit buttons open comprehensive profile editing dialogs
- [ ] All profile properties editable through intuitive form controls
- [ ] ComboBoxes populated with appropriate predefined values
- [ ] Checkbox states properly reflect and update profile settings
- [ ] Save button updates profile and closes dialog
- [ ] Cancel button discards changes and closes dialog
- [ ] Real-time command preview updates as properties change

### Quality Requirements
- [ ] Comprehensive error handling for all dialog operations
- [ ] User-friendly validation messages and feedback
- [ ] VSCode-consistent styling throughout dialog interfaces
- [ ] Responsive layout that works at different window sizes
- [ ] Proper accessibility features (tab order, keyboard navigation)
- [ ] Clean build with minimal warnings

### User Experience Requirements
- [ ] Dialogs are modal and properly centered
- [ ] Form controls are intuitive and follow platform conventions
- [ ] Validation feedback is immediate and clear
- [ ] Save/Cancel actions are clearly distinguished
- [ ] Dialog can be resized if content requires it
- [ ] Keyboard shortcuts work as expected (Enter to save, Escape to cancel)

---

**Task Owner**: Development Team with AI Assistance
**Dependencies**: TASK003 (Servers Settings) must be completed and stable
**Next Session Focus**: Begin Phase 1 - Dialog Infrastructure Enhancement

**Key Insight**: This implementation will leverage the existing comprehensive ProfileViewModels, creating specialized dialog views that provide full editing capabilities while maintaining the established Clean Architecture patterns and user experience consistency.
