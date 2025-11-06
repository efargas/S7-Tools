# Views and ViewModels Folder Reorganization Plan

**Date**: 2025-11-06
**Purpose**: Reorganize Views and ViewModels into logical, maintainable folder structure

---

## Analysis Summary

After deep analysis of all ViewModels and Views, here's the comprehensive reorganization based on:
- Actual file functionality and dependencies
- Naming conventions and patterns
- Architecture patterns (MVVM, Clean Architecture)
- Usage context (dialogs, pages, controls, etc.)

---

## Proposed Folder Structure

### ViewModels Structure

```
src/S7Tools/ViewModels/
├── Base/                          # Base classes
│   ├── ViewModelBase.cs           # EXISTING - Base for all VMs
│   └── ProfileManagementViewModelBase.cs  # EXISTING in Base/
│
├── Controls/                      # Reusable UI control ViewModels
│   ├── SerialPortDiscoveryViewModel.cs  # EXISTING
│   └── PropertyDisplayItem.cs     # MOVE from Profiles/
│
├── Dialogs/                       # Dialog ViewModels
│   ├── ConfirmationDialogViewModel.cs   # MOVE from root
│   └── InputDialogViewModel.cs          # MOVE from root
│
├── Jobs/                          # Job management (EXISTING)
│   ├── JobInfoDisplayViewModel.cs       # EXISTING
│   ├── JobsManagementViewModel.cs       # MOVE from root
│   ├── JobWizardViewModel.cs            # MOVE from root
│   └── JobWizardPlaceholderViewModel.cs # MOVE from root
│
├── Layout/                        # Main layout and shell components
│   ├── MainWindowViewModel.cs           # MOVE from root
│   ├── NavigationViewModel.cs           # MOVE from root
│   ├── NavigationItemViewModel.cs       # MOVE from root
│   ├── BottomPanelViewModel.cs          # MOVE from root
│   ├── TabViewModel.cs                  # MOVE from root
│   └── SettingsManagementViewModel.cs   # MOVE from root (layout concern)
│
├── Pages/                         # Main content page ViewModels
│   ├── HomeViewModel.cs                 # MOVE from root
│   ├── ConnectionsViewModel.cs          # MOVE from root
│   ├── LogViewerViewModel.cs            # MOVE from root
│   ├── AboutViewModel.cs                # MOVE from root
│   └── PlcInputViewModel.cs             # MOVE from root
│
├── Profiles/                      # Profile management (EXISTING)
│   ├── IProfileDetailsViewModel.cs      # EXISTING
│   ├── ProfileDetailsViewModel.cs       # EXISTING
│   ├── SerialPortProfileViewModel.cs    # MOVE from root
│   ├── SocatProfileViewModel.cs         # MOVE from root
│   └── PowerSupplyProfileViewModel.cs   # MOVE from root
│
├── Settings/                      # Settings page ViewModels
│   ├── SettingsViewModel.cs                    # MOVE from root
│   ├── GeneralSettingsViewModel.cs             # MOVE from root
│   ├── AppearanceSettingsViewModel.cs          # MOVE from root
│   ├── AdvancedSettingsViewModel.cs            # MOVE from root
│   ├── LoggingSettingsViewModel.cs             # MOVE from root
│   ├── SerialPortsSettingsViewModel.cs         # MOVE from root
│   ├── SocatSettingsViewModel.cs               # MOVE from root
│   └── PowerSupplySettingsViewModel.cs         # MOVE from root
│
└── Tasks/                         # Task management ViewModels
    ├── TaskManagerShellViewModel.cs     # MOVE from root
    ├── TaskManagerViewModel.cs          # MOVE from root
    ├── TaskCreatorViewModel.cs          # MOVE from root
    ├── TaskRunnerViewModel.cs           # MOVE from root
    ├── ActiveTasksViewModel.cs          # MOVE from root
    ├── ScheduledTasksViewModel.cs       # MOVE from root
    └── HistoryTasksViewModel.cs         # MOVE from root
```

### Views Structure

```
src/S7Tools/Views/
├── Controls/                      # Reusable UI controls
│   ├── SerialPortDiscoveryControl.axaml/.cs  # EXISTING in Views/Controls/
│   ├── SidebarSection.axaml/.cs              # MOVE from src/S7Tools/Controls/
│   ├── PropertyDisplayItem.axaml/.cs         # MOVE from root
│   └── CloseApplicationBehavior.cs           # MOVE from root
│
├── Dialogs/                       # Dialog views
│   ├── ConfirmationDialog.axaml/.cs          # MOVE from root
│   ├── InputDialog.axaml/.cs                 # MOVE from root
│   └── ProfileEditDialog.axaml/.cs           # MOVE from root
│
├── Jobs/                          # Job management views (EXISTING)
│   ├── JobInfoDisplayView.axaml/.cs          # EXISTING
│   ├── JobsMainView.axaml/.cs                # MOVE from root
│   ├── JobsMainContentView.axaml/.cs         # MOVE from root
│   ├── JobsSidebarView.axaml/.cs             # MOVE from root
│   ├── JobWizardView.axaml/.cs               # MOVE from root
│   ├── JobWizardMainView.axaml/.cs           # MOVE from root
│   └── JobWizardPlaceholderView.axaml/.cs    # MOVE from root
│
├── Layout/                        # Main layout components
│   ├── MainWindow.axaml/.cs                  # MOVE from root
│   └── TaskManagerShellView.axaml/.cs        # MOVE from root
│
├── Pages/                         # Main content pages
│   ├── HomeView.axaml/.cs                    # MOVE from root
│   ├── ConnectionsView.axaml/.cs             # MOVE from root
│   ├── LogViewerView.axaml/.cs               # MOVE from root
│   ├── AboutView.axaml/.cs                   # MOVE from root
│   ├── PlcInputView.axaml/.cs                # MOVE from root
│   └── LoggingTestView.axaml/.cs             # MOVE from root
│
├── Profiles/                      # Profile views (EXISTING - enhanced)
│   ├── SerialProfileDetailView.axaml/.cs          # EXISTING
│   ├── SerialProfileDetailedView.axaml/.cs        # EXISTING
│   ├── SocatProfileDetailView.axaml/.cs           # EXISTING
│   ├── PowerSupplyProfileDetailView.axaml/.cs     # EXISTING
│   ├── SerialProfileEditContent.axaml/.cs         # MOVE from root
│   ├── SocatProfileEditContent.axaml/.cs          # MOVE from root
│   └── PowerSupplyProfileEditContent.axaml/.cs    # MOVE from root
│
├── Settings/                      # Settings views
│   ├── SettingsView.axaml/.cs                     # MOVE from root
│   ├── SettingsCategoriesView.axaml/.cs           # MOVE from root
│   ├── SettingsConfigView.axaml/.cs               # MOVE from root
│   ├── GeneralSettingsView.axaml/.cs              # MOVE from root
│   ├── AppearanceSettingsView.axaml/.cs           # MOVE from root
│   ├── AdvancedSettingsView.axaml/.cs             # MOVE from root
│   ├── LoggingSettingsView.axaml/.cs              # MOVE from root
│   ├── SerialPortsSettingsView.axaml/.cs          # MOVE from root
│   ├── SocatSettingsView.axaml/.cs                # MOVE from root
│   └── PowerSupplySettingsView.axaml/.cs          # MOVE from root
│
└── Tasks/                         # Task management views
    ├── TaskManagerView.axaml/.cs              # MOVE from root
    ├── TaskManagerSidebarView.axaml/.cs       # MOVE from root
    ├── TaskCreatorView.axaml/.cs              # MOVE from root
    ├── TaskRunnerView.axaml/.cs               # MOVE from root
    ├── ActiveTasksView.axaml/.cs              # MOVE from root
    ├── ScheduledTasksView.axaml/.cs           # MOVE from root
    └── HistoryTasksView.axaml/.cs             # MOVE from root
```

---

## Category Definitions

### 1. **Base/**
Base classes and abstractions that all ViewModels inherit from.
- `ViewModelBase.cs` - Root base class
- `ProfileManagementViewModelBase.cs` - Profile management pattern base

### 2. **Controls/**
Reusable UI control ViewModels and supporting types that can be used in multiple views.

**ViewModels/Controls/**
- `SerialPortDiscoveryViewModel.cs` - Port discovery control logic
- `PropertyDisplayItem.cs` - Property display helper (data model for UI)

**Views/Controls/**
- `SerialPortDiscoveryControl.axaml/.cs` - EXISTING in Views/Controls/
- `SidebarSection.axaml/.cs` - MOVE from src/S7Tools/Controls/ (mislocated)
- `PropertyDisplayItem.axaml/.cs` - MOVE from Views/ root
- `CloseApplicationBehavior.cs` - MOVE from Views/ root

### 3. **Dialogs/**
ViewModels for modal dialogs and popup windows.
- `ConfirmationDialogViewModel.cs` - Yes/No confirmations
- `InputDialogViewModel.cs` - Text input dialogs

### 4. **Jobs/**
Everything related to job creation, management, and wizard.
- `JobInfoDisplayViewModel.cs` - Job details panel
- `JobsManagementViewModel.cs` - Main jobs management (extends ProfileManagementViewModelBase)
- `JobWizardViewModel.cs` - Multi-step job creation wizard
- `JobWizardPlaceholderViewModel.cs` - Placeholder when wizard not available

### 5. **Layout/**
Main application shell, navigation, panels, and layout management.
- `MainWindowViewModel.cs` - Main window coordinator
- `NavigationViewModel.cs` - Sidebar navigation and content switching
- `NavigationItemViewModel.cs` - Navigation item model
- `BottomPanelViewModel.cs` - Bottom panel with tabs
- `TabViewModel.cs` - Tab model
- `SettingsManagementViewModel.cs` - Settings coordination (layout concern, not a settings page)

### 6. **Pages/**
Main content pages displayed in the main editor area.
- `HomeViewModel.cs` - Explorer/Home page
- `ConnectionsViewModel.cs` - PLC connections page
- `LogViewerViewModel.cs` - Log viewer page
- `AboutViewModel.cs` - About page
- `PlcInputViewModel.cs` - PLC input page

### 7. **Profiles/**
Profile-specific ViewModels for viewing and editing profiles.

**Note**: `IProfileDetailsViewModel` stays here (not in Core) because:
- It's a **ViewModel interface** (UI layer contract), not a service interface
- Service interfaces belong in `S7Tools.Core/Interfaces/Services/`
- ViewModel interfaces belong in `S7Tools/ViewModels/` (application layer)

Files:
- `IProfileDetailsViewModel.cs` - EXISTING - ViewModel interface for profile details display
- `ProfileDetailsViewModel.cs` - EXISTING - Generic profile details display implementation
- `SerialPortProfileViewModel.cs` - MOVE from root - Serial port profile editor
- `SocatProfileViewModel.cs` - MOVE from root - Socat profile editor
- `PowerSupplyProfileViewModel.cs` - MOVE from root - Power supply profile editor

### 8. **Settings/**
Settings pages ViewModels (category-based settings UI).
- `SettingsViewModel.cs` - Settings shell with category selection
- `GeneralSettingsViewModel.cs` - General settings page
- `AppearanceSettingsViewModel.cs` - UI appearance settings
- `AdvancedSettingsViewModel.cs` - Advanced settings
- `LoggingSettingsViewModel.cs` - Logging configuration
- `SerialPortsSettingsViewModel.cs` - Serial ports settings with profile management
- `SocatSettingsViewModel.cs` - Socat servers settings with profile management
- `PowerSupplySettingsViewModel.cs` - Power supply settings with profile management

### 9. **Tasks/**
Task scheduling, execution, and management.
- `TaskManagerShellViewModel.cs` - Task manager shell with category navigation
- `TaskManagerViewModel.cs` - Core task management logic
- `TaskCreatorViewModel.cs` - Task creation wrapper
- `TaskRunnerViewModel.cs` - Task execution runner
- `ActiveTasksViewModel.cs` - Active/running tasks view
- `ScheduledTasksViewModel.cs` - Scheduled tasks view
- `HistoryTasksViewModel.cs` - Task history view

---

## Migration Impact Analysis

### Current Mislocated Files

**`src/S7Tools/Controls/`** - This folder should not exist at this level!
- `SidebarSection.axaml/.cs` - Should be in `Views/Controls/` (it's a UI control, not a service)
- This is a UI control (UserControl with XAML), belongs with other View controls

### Namespace Changes Required

All moved files will need namespace updates:
- ViewModels: `S7Tools.ViewModels.{Category}`
- Views: `S7Tools.Views.{Category}`
- **Special case**: `SidebarSection` currently uses `S7Tools.Controls` namespace, should become `S7Tools.Views.Controls`

### Files to Update

1. **XAML Files** - Update namespace references in:
   - DataTemplate definitions
   - x:Class declarations
   - ViewLocator patterns

2. **Code-Behind Files** - Update:
   - Namespace declarations
   - Using statements

3. **ViewLocator** - Update type resolution logic

4. **ServiceCollectionExtensions.cs** - Update registration (if needed)

5. **Tests** - Update test project references

---

## Benefits

✅ **Logical Organization** - Files grouped by feature/responsibility
✅ **Easy Navigation** - Clear folder names match intent
✅ **Scalability** - Easy to add new features
✅ **Clean Architecture** - Separation of concerns maintained
✅ **Discoverability** - Intuitive file locations
✅ **Consistency** - Follows established patterns (Jobs, Profiles already organized)
✅ **Maintainability** - Related files grouped together
✅ **Fixes Mislocations** - Corrects `src/S7Tools/Controls/` which shouldn't exist at root level

---

## Next Steps

1. ✅ **Plan Created** - Comprehensive reorganization plan documented
2. ⏸️ **Create Migration Script** - Shell script to move files
3. ⏸️ **Update Namespaces** - Automated namespace updates
4. ⏸️ **Update References** - XAML, code-behind, ViewLocator
5. ⏸️ **Test Build** - Verify compilation after changes
6. ⏸️ **Test Application** - Verify runtime functionality
7. ⏸️ **Update Documentation** - Update architecture docs

---

## Notes

- This reorganization aligns with Clean Architecture principles
- Follows existing patterns (Jobs/, Profiles/ already partially organized in Views)
- **Fixes**: `src/S7Tools/Controls/` should not exist - UI controls belong in `Views/Controls/`
- Maintains MVVM separation with parallel View/ViewModel structures
- Provides clear boundaries between different application concerns
- Makes codebase more maintainable and navigable
- Clear distinction: `ViewModels/Controls/` = logic, `Views/Controls/` = UI (XAML + code-behind)

### Interface Location Guidelines:
- **Service interfaces** → `S7Tools.Core/Interfaces/Services/` (domain layer)
- **ViewModel interfaces** → `S7Tools/ViewModels/{Category}/` (application layer)
- Example: `IProfileDetailsViewModel` is a ViewModel interface, stays in `ViewModels/Profiles/`
- Example: `IProfileManager<T>` is a service interface, lives in `S7Tools.Core/`
