---
applyTo: './.copilot-tracking/changes/20250107-ui-logging-fixes-changes.md'
---
<!-- markdownlint-disable-file -->
# Task Checklist: S7Tools UI Controls & Logging System Fixes

## Overview

Fix critical UI control issues and logging system integration problems identified in S7Tools application screenshots, including non-functional grid splitters, broken bottom panel behavior, missing LogViewer integration, and incomplete file dialog functionality.

## Objectives

- Fix GridSplitter controls for proper panel resizing functionality
- Implement correct bottom panel collapse/expand behavior showing tab headers when collapsed
- Connect LogViewer to logging infrastructure for real-time log display
- Add functional file/folder picker dialogs for settings page
- Fix DatePicker controls styling and binding issues
- Ensure proper UI thread marshalling for responsive interface
- Implement comprehensive error handling and user feedback

## Research Summary

### Project Files
- src/S7Tools/Views/MainWindow.axaml - Main UI layout with grid splitters and panels
- src/S7Tools/ViewModels/MainWindowViewModel.cs - Main window logic and commands
- src/S7Tools/Views/LogViewerView.axaml - LogViewer UI implementation
- src/S7Tools/ViewModels/LogViewerViewModel.cs - LogViewer business logic
- src/S7Tools/Extensions/ServiceCollectionExtensions.cs - Service registration

### External References
- #file: ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md - Comprehensive analysis of issues and solutions
- #githubRepo:"AvaloniaUI/Avalonia GridSplitter" - GridSplitter implementation patterns
- #fetch:https://docs.avaloniaui.net/docs/reference/controls/gridsplitter - Official GridSplitter documentation
- Reference project: .github/agents/workspace/referent-projects/LogViewerControl/ - LogViewer implementation patterns

### Standards References
- #file: ./.copilot-tracking/memory-bank/instructions.md - Project-specific patterns and conventions
- #file: ./AGENTS.md - Development workflow and architecture guidelines

## Implementation Checklist

### [ ] Phase 1: GridSplitter Fixes

- [ ] Task 1.1: Fix Bottom Panel GridSplitter Configuration
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 15-35)

- [ ] Task 1.2: Fix Sidebar GridSplitter Configuration  
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 36-56)

- [ ] Task 1.3: Implement Proper GridSplitter Styling
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 57-77)

### [ ] Phase 2: Bottom Panel Behavior Fixes

- [ ] Task 2.1: Fix Bottom Panel Collapse Logic
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 78-98)

- [ ] Task 2.2: Implement Tab Header Visibility When Collapsed
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 99-119)

- [ ] Task 2.3: Add Smooth Panel Animations
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 120-140)

### [ ] Phase 3: LogViewer Integration

- [ ] Task 3.1: Connect LogViewer to DataStore Infrastructure
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 141-161)

- [ ] Task 3.2: Fix LogViewer Real-time Updates
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 162-182)

- [ ] Task 3.3: Implement Proper UI Thread Marshalling
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 183-203)

- [ ] Task 3.4: Fix DatePicker Controls and Styling
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 204-224)

### [ ] Phase 4: File Dialog Integration

- [ ] Task 4.1: Implement IFileDialogService Interface
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 225-245)

- [ ] Task 4.2: Create AvaloniaFileDialogService Implementation
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 246-266)

- [ ] Task 4.3: Connect File Dialogs to Settings Commands
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 267-287)

- [ ] Task 4.4: Register File Dialog Service in DI Container
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 288-308)

### [ ] Phase 5: Testing and Validation

- [ ] Task 5.1: Manual Testing of All UI Controls
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 309-329)

- [ ] Task 5.2: Validate LogViewer Functionality
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 330-350)

- [ ] Task 5.3: Test File Dialog Integration
  - Details: ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md (Lines 351-371)

## Risk Assessment

### High Priority Risks
- **Risk**: GridSplitter changes may break existing layout behavior
  - **Impact**: High - Core UI functionality affected
  - **Probability**: Medium
  - **Mitigation**: Test thoroughly after each change, maintain backup of working configuration
  - **Contingency**: Revert to previous GridSplitter configuration if issues arise

- **Risk**: LogViewer integration may cause memory leaks with high-frequency updates
  - **Impact**: High - Application performance degradation
  - **Probability**: Medium  
  - **Mitigation**: Implement proper disposal patterns and UI thread throttling
  - **Contingency**: Add circuit breaker pattern for excessive log volume

### Medium Priority Risks
- **Risk**: File dialog implementation may have cross-platform compatibility issues
  - **Impact**: Medium - Settings functionality limited on some platforms
  - **Probability**: Low
  - **Mitigation**: Test on multiple platforms, implement platform-specific fallbacks

## Time Estimates

### Phase 1: GridSplitter Fixes - Estimated: 6 hours
- Task 1.1: Bottom Panel GridSplitter - 2h
- Task 1.2: Sidebar GridSplitter - 2h  
- Task 1.3: GridSplitter Styling - 2h

### Phase 2: Bottom Panel Behavior - Estimated: 8 hours
- Task 2.1: Collapse Logic - 3h
- Task 2.2: Tab Header Visibility - 3h
- Task 2.3: Panel Animations - 2h

### Phase 3: LogViewer Integration - Estimated: 12 hours
- Task 3.1: DataStore Connection - 4h
- Task 3.2: Real-time Updates - 4h
- Task 3.3: UI Thread Marshalling - 2h
- Task 3.4: DatePicker Fixes - 2h

### Phase 4: File Dialog Integration - Estimated: 8 hours
- Task 4.1: Service Interface - 2h
- Task 4.2: Service Implementation - 3h
- Task 4.3: Settings Integration - 2h
- Task 4.4: DI Registration - 1h

### Phase 5: Testing and Validation - Estimated: 6 hours
- Task 5.1: UI Controls Testing - 2h
- Task 5.2: LogViewer Validation - 2h
- Task 5.3: File Dialog Testing - 2h

### Assumptions
- Existing logging infrastructure is functional and properly configured
- Current service registration patterns will be maintained
- No major architectural changes required

### Buffers
- Complexity Buffer: 20% (8 hours)
- Integration Buffer: 15% (6 hours)

## Quality Gates

### Code Standards
- [ ] Code follows project EditorConfig style guidelines
- [ ] All new services registered in ServiceCollectionExtensions.cs
- [ ] XML documentation added for all public APIs
- [ ] Proper error handling with user-friendly messages
- [ ] No TODO comments in production code

### Testing Requirements
- [ ] Manual testing completed for all UI controls
- [ ] LogViewer real-time updates verified
- [ ] File dialog functionality tested on primary platform
- [ ] Memory usage monitored during log stress testing
- [ ] UI responsiveness maintained during operations

### Documentation
- [ ] Memory bank instructions updated with new patterns
- [ ] AGENTS.md updated with new service information
- [ ] Code comments added for complex UI logic

## .NET Specific Considerations

### Framework Compatibility
- **Target Framework**: .NET 8.0 - All implementations must be compatible
- **Avalonia Version**: 11.3.6 - Use version-specific APIs and patterns
- **ReactiveUI**: 20.1.1 - Follow reactive programming patterns for UI updates

### Architecture Pattern Alignment
- **MVVM Compliance**: All UI logic in ViewModels, Views handle only presentation
- **Clean Architecture**: Services in appropriate layers, proper dependency flow
- **Service Registration**: All new services registered via ServiceCollectionExtensions
- **Dependency Injection**: Constructor injection for all dependencies

### Performance Metrics
- **Memory Usage**: Monitor heap allocation during log updates, implement circular buffer
- **UI Responsiveness**: Keep UI operations under 100ms, use background threading for I/O
- **Startup Time**: Ensure service registration doesn't impact application startup

### Security Requirements
- **File Dialog Security**: Validate file paths and permissions before access
- **Input Validation**: Validate all user inputs in settings and file dialogs
- **Error Handling**: Don't expose internal errors to users, log detailed information

## Dependencies

- .NET 8.0 SDK
- Avalonia UI 11.3.6
- ReactiveUI 20.1.1
- Microsoft.Extensions.Logging 8.0.0
- Microsoft.Extensions.DependencyInjection 8.0.0
- Existing S7Tools logging infrastructure
- Reference LogViewerControl project patterns

## Success Criteria

- GridSplitters allow smooth resizing of sidebar and bottom panel
- Bottom panel properly collapses to 35px height showing tab headers
- LogViewer displays real-time log entries from test buttons with proper filtering
- File dialog buttons in settings open native dialogs and update paths
- DatePicker controls function properly with dark theme styling
- UI remains responsive during all operations
- Memory usage stays reasonable with continuous log updates
- All functionality works consistently across supported platforms