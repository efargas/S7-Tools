<!-- markdownlint-disable-file -->
# Task Details: S7Tools UI Controls & Logging System Fixes

## Research Reference

**Source Research**: #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md

## Phase 1: GridSplitter Fixes

### Task 1.1: Fix Bottom Panel GridSplitter Configuration

Fix the bottom panel GridSplitter to enable proper vertical resizing of the bottom panel area.

- **Files**:
  - src/S7Tools/Views/MainWindow.axaml - Update GridSplitter configuration
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Ensure proper property bindings
- **Success**:
  - User can drag the horizontal splitter to resize bottom panel height
  - Splitter provides visual feedback during drag operations
  - Panel maintains minimum and maximum size constraints
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 45-65) - GridSplitter configuration patterns
  - #githubRepo:"AvaloniaUI/Avalonia GridSplitter" - Implementation examples
- **Dependencies**:
  - Existing MainWindow.axaml layout structure
  - BottomPanelGridLength property in ViewModel

### Task 1.2: Fix Sidebar GridSplitter Configuration

Fix the sidebar GridSplitter to enable proper horizontal resizing of the sidebar area.

- **Files**:
  - src/S7Tools/Views/MainWindow.axaml - Update sidebar GridSplitter configuration
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Add sidebar width property if missing
- **Success**:
  - User can drag the vertical splitter to resize sidebar width
  - Splitter respects minimum and maximum width constraints (200px - 600px)
  - Sidebar maintains proper visibility state during resize
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 45-65) - GridSplitter configuration patterns
  - #fetch:https://docs.avaloniaui.net/docs/reference/controls/gridsplitter - Official documentation
- **Dependencies**:
  - Task 1.1 completion for consistent splitter behavior
  - IsSidebarVisible property functionality

### Task 1.3: Implement Proper GridSplitter Styling

Apply consistent VSCode-style theming to GridSplitter controls for professional appearance.

- **Files**:
  - src/S7Tools/Views/MainWindow.axaml - Update GridSplitter styling
  - src/S7Tools/Styles/Styles.axaml - Add GridSplitter style definitions if needed
- **Success**:
  - GridSplitters have consistent #464647 background color
  - Proper thickness (2px for vertical, 2px for horizontal)
  - Hover and active states provide visual feedback
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 66-86) - VSCode styling patterns
  - Reference project styling examples
- **Dependencies**:
  - Tasks 1.1 and 1.2 completion
  - Existing VSCode color scheme resources

## Phase 2: Bottom Panel Behavior Fixes

### Task 2.1: Fix Bottom Panel Collapse Logic

Implement proper VSCode-like bottom panel collapse behavior that shows tab headers when collapsed.

- **Files**:
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Update ToggleBottomPanelCommand logic
  - src/S7Tools/Views/MainWindow.axaml - Ensure proper panel structure for collapse behavior
- **Success**:
  - Collapsed state shows 35px height with tab headers visible
  - Expanded state shows full content with proper height
  - Toggle button icon changes based on state (up/down chevron)
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 87-107) - Collapse logic implementation
  - VSCode behavior analysis for reference
- **Dependencies**:
  - Phase 1 completion for proper splitter functionality
  - Existing bottom panel tab structure

### Task 2.2: Implement Tab Header Visibility When Collapsed

Ensure bottom panel tab headers remain visible and functional when panel is collapsed.

- **Files**:
  - src/S7Tools/Views/MainWindow.axaml - Update bottom panel layout structure
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Ensure tab selection works in collapsed state
- **Success**:
  - Tab headers always visible regardless of panel state
  - Clicking tab in collapsed state expands panel and shows content
  - Tab selection indicators work properly in both states
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 108-128) - Tab visibility patterns
  - Current MainWindow.axaml tab implementation
- **Dependencies**:
  - Task 2.1 completion
  - Existing tab control implementation

### Task 2.3: Add Smooth Panel Animations

Implement smooth animations for panel collapse/expand operations to match VSCode behavior.

- **Files**:
  - src/S7Tools/Views/MainWindow.axaml - Add animation resources and triggers
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Ensure smooth property transitions
- **Success**:
  - Panel collapse/expand has smooth animation (200-300ms duration)
  - No jarring or flickering during transitions
  - Animation respects user accessibility preferences
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 129-149) - Animation implementation patterns
  - Avalonia animation documentation
- **Dependencies**:
  - Tasks 2.1 and 2.2 completion
  - GridSplitter functionality from Phase 1

## Phase 3: LogViewer Integration

### Task 3.1: Connect LogViewer to DataStore Infrastructure

Establish proper connection between LogViewer UI and the existing logging infrastructure.

- **Files**:
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Update CreateLogViewerContent method
  - src/S7Tools/ViewModels/LogViewerViewModel.cs - Ensure proper DataStore integration
  - src/S7Tools/Extensions/ServiceCollectionExtensions.cs - Verify service registration
- **Success**:
  - LogViewer receives log entries from DataStore in real-time
  - Proper dependency injection of required services
  - Error handling for missing or unavailable services
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 150-170) - DataStore integration patterns
  - Reference project LogViewer implementation
- **Dependencies**:
  - Existing logging infrastructure (ILogDataStore, DataStoreLogger)
  - Service registration patterns

### Task 3.2: Fix LogViewer Real-time Updates

Implement proper real-time log entry display when test buttons are pressed.

- **Files**:
  - src/S7Tools/ViewModels/LogViewerViewModel.cs - Fix collection change handling
  - src/S7Tools/Views/LogViewerView.axaml - Ensure proper data binding
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Verify test button logging
- **Success**:
  - Log entries appear immediately when test buttons are pressed
  - Entries are properly formatted with timestamp, level, and message
  - Auto-scroll functionality works when enabled
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 171-191) - Real-time update patterns
  - ReactiveUI collection handling best practices
- **Dependencies**:
  - Task 3.1 completion
  - Existing test button command implementations

### Task 3.3: Implement Proper UI Thread Marshalling

Ensure all log updates are properly marshalled to the UI thread for responsive interface.

- **Files**:
  - src/S7Tools/ViewModels/LogViewerViewModel.cs - Update collection change handlers
  - src/S7Tools/Services/AvaloniaUIThreadService.cs - Verify implementation
- **Success**:
  - No cross-thread exceptions during log updates
  - UI remains responsive during high-frequency logging
  - Proper disposal of event subscriptions
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 192-212) - UI thread marshalling patterns
  - IUIThreadService implementation examples
- **Dependencies**:
  - Task 3.2 completion
  - Existing IUIThreadService implementation

### Task 3.4: Fix DatePicker Controls and Styling

Fix DatePicker controls in LogViewer for proper date range filtering functionality.

- **Files**:
  - src/S7Tools/Views/LogViewerView.axaml - Update DatePicker configuration and styling
  - src/S7Tools/ViewModels/LogViewerViewModel.cs - Ensure proper date binding
  - src/S7Tools/Styles/Styles.axaml - Add DatePicker dark theme styling
- **Success**:
  - DatePicker controls display properly with dark theme
  - Date selection updates filter criteria correctly
  - No binding errors in debug output
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 213-233) - DatePicker styling and binding
  - Avalonia DatePicker documentation
- **Dependencies**:
  - Task 3.3 completion
  - Existing LogViewer filtering logic

## Phase 4: File Dialog Integration

### Task 4.1: Implement IFileDialogService Interface

Create service interface for file and folder selection dialogs.

- **Files**:
  - src/S7Tools/Services/Interfaces/IFileDialogService.cs - New interface definition
- **Success**:
  - Interface defines methods for file, folder, and save dialogs
  - Proper async/await patterns for dialog operations
  - Comprehensive XML documentation
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 234-254) - File dialog service patterns
  - Avalonia file dialog API documentation
- **Dependencies**:
  - Existing service interface patterns
  - Project naming conventions

### Task 4.2: Create AvaloniaFileDialogService Implementation

Implement the file dialog service using Avalonia's native dialog APIs.

- **Files**:
  - src/S7Tools/Services/AvaloniaFileDialogService.cs - Service implementation
- **Success**:
  - Native file dialogs open properly on all supported platforms
  - Proper error handling for dialog cancellation
  - Integration with application main window
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 255-275) - Service implementation patterns
  - Avalonia dialog examples and best practices
- **Dependencies**:
  - Task 4.1 completion
  - Existing service implementation patterns

### Task 4.3: Connect File Dialogs to Settings Commands

Update settings page commands to use the new file dialog service.

- **Files**:
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Update browse command implementations
  - src/S7Tools/Views/SettingsConfigView.axaml - Verify button bindings
- **Success**:
  - Browse buttons open appropriate file/folder dialogs
  - Selected paths update in settings UI immediately
  - Proper error handling for invalid selections
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 276-296) - Command integration patterns
  - Existing settings command implementations
- **Dependencies**:
  - Task 4.2 completion
  - Existing settings page structure

### Task 4.4: Register File Dialog Service in DI Container

Add the file dialog service to the dependency injection container.

- **Files**:
  - src/S7Tools/Extensions/ServiceCollectionExtensions.cs - Add service registration
  - src/S7Tools/ViewModels/MainWindowViewModel.cs - Update constructor for service injection
- **Success**:
  - Service properly registered with appropriate lifetime
  - Constructor injection works without breaking existing functionality
  - Service available throughout application
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 297-317) - Service registration patterns
  - #file: ./.copilot-tracking/memory-bank/instructions.md - Service registration requirements
- **Dependencies**:
  - Task 4.3 completion
  - Existing service registration patterns

## Phase 5: Testing and Validation

### Task 5.1: Manual Testing of All UI Controls

Perform comprehensive manual testing of all fixed UI controls.

- **Files**:
  - Manual testing checklist execution
  - Documentation of any issues found
- **Success**:
  - All GridSplitters resize panels smoothly
  - Bottom panel collapse/expand works as expected
  - Sidebar resizing and visibility toggles function properly
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 318-338) - Manual testing checklist
  - VSCode behavior reference for comparison
- **Dependencies**:
  - Phases 1 and 2 completion
  - Functional application build

### Task 5.2: Validate LogViewer Functionality

Test LogViewer integration and real-time log display functionality.

- **Files**:
  - LogViewer functionality validation
  - Performance testing with high log volume
- **Success**:
  - Log entries appear immediately when test buttons pressed
  - Filtering works for all criteria (level, text, date)
  - Export functionality produces correct output
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 339-359) - LogViewer testing procedures
  - Reference project testing patterns
- **Dependencies**:
  - Phase 3 completion
  - Functional logging infrastructure

### Task 5.3: Test File Dialog Integration

Validate file dialog functionality in settings page.

- **Files**:
  - Settings page file dialog testing
  - Cross-platform compatibility verification
- **Success**:
  - File dialogs open and function on primary platform
  - Selected paths update settings UI correctly
  - Error handling works for invalid selections
- **Research References**:
  - #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md (Lines 360-380) - File dialog testing procedures
  - Platform-specific testing considerations
- **Dependencies**:
  - Phase 4 completion
  - Settings page functionality

## Dependencies

- .NET 8.0 SDK with Avalonia UI 11.3.6
- Existing S7Tools logging infrastructure
- ReactiveUI 20.1.1 for MVVM patterns
- Microsoft.Extensions.DependencyInjection for service registration
- Reference LogViewerControl project for implementation patterns

## Success Criteria

- All GridSplitters enable smooth panel resizing without UI glitches
- Bottom panel properly collapses to show tab headers (35px height) and expands to show content
- LogViewer displays real-time log entries from test buttons with proper formatting and filtering
- File dialog buttons in settings open native dialogs and update paths correctly
- DatePicker controls function properly with consistent dark theme styling
- UI remains responsive during all operations including high-frequency log updates
- Memory usage stays reasonable with continuous logging operations
- All functionality works consistently and matches VSCode-like behavior expectations