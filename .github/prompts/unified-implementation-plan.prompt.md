---
mode: 'agent'
description: 'Unified implementation plan for S7Tools VSCode UI transformation with integrated LogViewer functionality. This plan merges the VSCode-like interface implementation with comprehensive logging capabilities.'
tools: ['changes', 'codebase', 'editFiles', 'extensions', 'fetch', 'githubRepo', 'openSimpleBrowser', 'problems', 'runTasks', 'search', 'searchResults', 'terminalLastCommand', 'terminalSelection', 'testFailure', 'usages', 'vscodeAPI']
---

# Unified S7Tools Implementation Plan

## Primary Directive

You are an AI agent tasked with implementing the unified S7Tools transformation that combines VSCode-like UI design with integrated LogViewer functionality. Your implementation must be systematic, maintainable, and preserve all existing functionality while delivering a modern user experience.

## Execution Context

This prompt is designed for comprehensive application transformation involving:
- Complete UI restructuring from FluentAvalonia NavigationView to VSCode-like layout
- Integration of real-time logging infrastructure with Microsoft.Extensions.Logging
- Service-oriented architecture with proper dependency injection
- Comprehensive theming system with VSCode color schemes
- Advanced UI features including resizable panels, activity bar, and dynamic content

## Implementation Plan Reference

**Primary Plan**: `./.copilot-tracking/details/unified-s7tools-implementation-plan.md`
**Tracking Document**: `./.copilot-tracking/tracking/unified-s7tools-implementation-tracking.md`
**Project Structure**: `./.copilot-tracking/details/unified-project-structure.md`

## Core Requirements

### UI Transformation Requirements
- Transform MainWindow from FluentAvalonia NavigationView to VSCode-like DockPanel layout
- Implement activity bar with icon selection states and hover effects
- Create collapsible sidebar with dynamic content based on activity bar selection
- Add resizable bottom panel with tab control for logs and output
- Implement comprehensive menu system with keyboard shortcuts
- Create status bar with application information

### LogViewer Integration Requirements
- Create S7Tools.Infrastructure.Logging project with Microsoft.Extensions.Logging integration
- Implement thread-safe circular buffer for high-performance log storage
- Create LogViewerControl with real-time display, search, and filtering
- Add LogViewer as activity bar item and sidebar content
- Implement log export functionality and configuration management

### Service Architecture Requirements
- Implement ILayoutService for UI state management
- Create IActivityBarService for activity bar behavior
- Add IThemeService for VSCode theme management
- Implement IUIThreadService for cross-thread operations
- Create ILocalizationService for multi-language support

## Implementation Phases

### Phase 1: Foundation & Infrastructure (5 days)
**Goal**: Establish logging infrastructure and core services

**Key Tasks**:
1. Create S7Tools.Infrastructure.Logging project
2. Implement LogModel, LogDataStore, and DataStoreLogger
3. Create foundation services (Layout, Theme, UIThread, Localization)
4. Set up resource management system

**Success Criteria**:
- Logging infrastructure integrates with Microsoft.Extensions.Logging
- All foundation services are thread-safe and properly registered
- Resource management supports localization
- Theme service can switch between VSCode themes

### Phase 2: Core UI Structure (6 days)
**Goal**: Transform main application layout to VSCode-like interface

**Key Tasks**:
1. Restructure MainWindow with DockPanel layout
2. Create activity bar, sidebar, and bottom panel components
3. Implement menu bar with keyboard shortcuts
4. Add status bar component

**Success Criteria**:
- VSCode-like layout fully functional
- Activity bar selection states work correctly
- Sidebar collapses/expands smoothly
- Menu system with working shortcuts
- All panels resize correctly

### Phase 3: LogViewer Integration (4 days)
**Goal**: Integrate comprehensive logging capabilities

**Key Tasks**:
1. Create LogViewer components and converters
2. Implement LogViewerControlViewModel with search/filtering
3. Add LogViewer to activity bar and sidebar
4. Configure logging in appsettings.json

**Success Criteria**:
- LogViewer displays real-time logs with color coding
- Search and filtering work correctly
- Auto-scroll functionality works
- Export functionality saves logs
- Seamless integration with VSCode UI

### Phase 4: Advanced Features & Styling (5 days)
**Goal**: Implement advanced UI features and theming

**Key Tasks**:
1. Create comprehensive VSCode theme resources
2. Implement component-specific styles
3. Add hover/selection states with animations
4. Create dynamic sidebar content management

**Success Criteria**:
- Complete VSCode visual theme implemented
- Smooth animations for all transitions
- Activity bar behavior matches VSCode
- Advanced features enhance usability

### Phase 5: Service Integration & Testing (4 days)
**Goal**: Complete integration and comprehensive testing

**Key Tasks**:
1. Refactor MainWindowViewModel for new architecture
2. Update dependency injection configuration
3. Integrate logging into existing services
4. Create comprehensive test suite

**Success Criteria**:
- All services properly integrated
- Unit test coverage above 80%
- Performance requirements met
- No regressions in existing functionality

### Phase 6: Documentation & Deployment (2 days)
**Goal**: Complete documentation and deployment preparation

**Key Tasks**:
1. Update XML documentation
2. Create user and developer guides
3. Perform final optimization
4. Validate cross-platform compatibility

**Success Criteria**:
- Complete documentation
- Clean, production-ready code
- Successful cross-platform deployment
- Clear migration path documented

## Technical Specifications

### Technology Stack
- **.NET 8.0**: Target framework for latest performance features
- **Avalonia 11.3.6**: Cross-platform UI framework
- **ReactiveUI 20.1.1**: MVVM framework for reactive programming
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **Microsoft.Extensions.DependencyInjection**: Service container

### Architecture Patterns
- **Service-Oriented Architecture**: Clear separation of concerns
- **MVVM Pattern**: Model-View-ViewModel with ReactiveUI
- **Command Pattern**: All user interactions through commands
- **Observer Pattern**: Real-time log updates and UI notifications
- **Factory Pattern**: Dynamic content creation and management

### Performance Requirements
- **UI Responsiveness**: Layout changes < 100ms
- **Log Display**: Real-time updates < 100ms latency
- **Memory Usage**: < 100MB for 10k log entries
- **Search Performance**: < 500ms for 10k entries
- **Startup Impact**: < 100ms additional startup time

## File Structure Overview

### New Projects
```
S7Tools.Infrastructure.Logging/
├── Core/Models/                    # LogModel, LogEntryColor, LogDataStoreOptions
├── Core/Storage/                   # ILogDataStore, LogDataStore
├── Core/Configuration/             # DataStoreLoggerConfiguration
├── Providers/Microsoft/            # DataStoreLogger, DataStoreLoggerProvider
└── Providers/Extensions/           # LoggingServiceCollectionExtensions
```

### Enhanced Main Application
```
S7Tools/
├── Views/Components/               # ActivityBar, Sidebar, BottomPanel, LogViewer
├── Views/SidebarContent/           # Dynamic content views
├── ViewModels/Components/          # Component ViewModels
├── Services/                       # Layout, Theme, UIThread, Localization services
├── Resources/Strings/              # Localization resources
├── Resources/Themes/               # VSCode theme resources
├── Styles/                         # Component-specific styles
├── Converters/                     # Log level converters
├── Commands/                       # Application and component commands
└── Models/                         # UI models (ActivityBarItem, PanelTabItem)
```

## Quality Gates

### Code Quality Standards
- All public APIs have comprehensive XML documentation
- All async methods use ConfigureAwait(false)
- Proper disposal patterns for all ViewModels and services
- Static analysis passes without warnings
- EditorConfig compliance

### Performance Standards
- UI thread never blocked by long-running operations
- Memory usage stable over extended operation
- No memory leaks detected in profiling
- Responsive UI with large datasets (10k+ entries)
- Smooth animations (60fps target)

### Testing Standards
- Unit test coverage > 80% for new components
- Integration tests for all major workflows
- Performance tests for critical paths
- Cross-platform compatibility validation
- Regression tests for existing functionality

## Risk Mitigation

### High Priority Risks
1. **Complex Integration**: Phased implementation with incremental testing
2. **Performance Impact**: Circular buffer, virtualization, background processing
3. **Breaking Changes**: Additive approach with comprehensive regression testing

### Medium Priority Risks
1. **Memory Management**: Proper disposal patterns and memory profiling
2. **Cross-Platform Issues**: Platform-specific testing and conditional code

## Success Criteria

### Functional Success
- VSCode-like interface fully operational
- Real-time logging with search and filtering
- All existing functionality preserved
- Smooth animations and responsive UI
- Comprehensive keyboard shortcuts

### Technical Success
- Clean, maintainable code architecture
- Proper service separation and dependency injection
- Thread-safe operations throughout
- Comprehensive test coverage
- Cross-platform compatibility

### User Experience Success
- Intuitive VSCode-like navigation
- Efficient log monitoring and analysis
- Consistent visual design and theming
- Responsive performance with large datasets
- Seamless workflow integration

## Implementation Guidelines

### Development Approach
1. **Incremental Implementation**: Build and test each phase completely
2. **Preserve Existing Functionality**: Additive changes only
3. **Service-First Design**: Implement services before UI components
4. **Test-Driven Development**: Write tests alongside implementation
5. **Performance Monitoring**: Profile and optimize continuously

### Code Standards
- Follow established MVVM patterns with ReactiveUI
- Use dependency injection for all service dependencies
- Implement proper error handling and logging
- Maintain comprehensive XML documentation
- Follow project style guidelines and EditorConfig

### Testing Strategy
- Unit tests for all services and ViewModels
- Integration tests for component interactions
- Performance tests for critical operations
- UI tests for user interaction scenarios
- Cross-platform validation testing

This unified implementation plan provides a comprehensive roadmap for transforming S7Tools into a modern, professional application with advanced logging capabilities while maintaining all existing functionality and ensuring excellent user experience.