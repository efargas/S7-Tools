# Unified S7Tools Project Structure

This document outlines the complete project structure after implementing the unified VSCode UI with integrated LogViewer functionality.

## Project Overview

The unified S7Tools application combines a modern VSCode-like user interface with comprehensive logging capabilities, maintaining the existing PLC connectivity features while providing an enhanced user experience.

## Directory Structure

```
S7-Tools/
├── src/
│   ├── S7Tools/                                    # Main Avalonia application
│   │   ├── Views/
│   │   │   ├── MainWindow.axaml                    # Main VSCode-like layout
│   │   │   ├── MainWindow.axaml.cs
│   │   │   ├── Components/                         # Reusable UI components
│   │   │   │   ├── ActivityBarView.axaml           # Left activity bar
│   │   │   │   ├── ActivityBarView.axaml.cs
│   │   │   │   ├── SidebarView.axaml               # Collapsible sidebar
│   │   │   │   ├── SidebarView.axaml.cs
│   │   │   │   ├── BottomPanelView.axaml           # Resizable bottom panel
│   │   │   │   ├── BottomPanelView.axaml.cs
│   │   │   │   ├── MenuBarView.axaml               # Menu bar with shortcuts
│   │   │   │   ├── MenuBarView.axaml.cs
│   │   │   │   ├── StatusBarView.axaml             # Status bar
│   │   │   │   ├── StatusBarView.axaml.cs
│   │   │   │   ├── LogViewerControl.axaml          # LogViewer component
│   │   │   │   └── LogViewerControl.axaml.cs
│   │   │   ├── SidebarContent/                     # Dynamic sidebar content
│   │   │   │   ├── ExplorerView.axaml              # File explorer (future)
│   │   │   │   ├── ExplorerView.axaml.cs
│   │   │   │   ├── ConnectionsView.axaml           # PLC connections (moved)
│   │   │   │   ├── ConnectionsView.axaml.cs
│   │   │   │   ├── LoggingView.axaml               # Logging sidebar content
│   │   │   │   ├── LoggingView.axaml.cs
│   │   │   │   ├── SettingsView.axaml              # Settings (moved)
│   │   │   │   └── SettingsView.axaml.cs
│   │   │   ├── Dialogs/                            # Modal dialogs
│   │   │   │   ├── ConfirmationDialog.axaml        # Existing
│   │   │   │   ├── ConfirmationDialog.axaml.cs
│   │   │   │   ├── AboutView.axaml                 # Existing (moved)
│   │   │   │   └── AboutView.axaml.cs
│   │   │   └── Legacy/                             # Legacy views (deprecated)
│   │   │       ├── HomeView.axaml                  # To be removed
│   │   │       └── HomeView.axaml.cs
│   │   ├── ViewModels/
│   │   │   ├── MainWindowViewModel.cs              # Refactored main VM
│   │   │   ├── ViewModelBase.cs                    # Enhanced base class
│   │   │   ├── Components/                         # Component ViewModels
│   │   │   │   ├── ActivityBarViewModel.cs         # Activity bar logic
│   │   │   │   ├── ActivityBarItemViewModel.cs     # Individual items
│   │   │   │   ├── SidebarViewModel.cs             # Sidebar management
│   │   │   │   ├── BottomPanelViewModel.cs         # Bottom panel logic
│   │   │   │   ├── PanelTabViewModel.cs            # Panel tab items
│   │   │   │   ├── MenuBarViewModel.cs             # Menu bar logic
│   │   │   │   ├── StatusBarViewModel.cs           # Status bar info
│   │   │   │   └── LogViewerControlViewModel.cs    # LogViewer functionality
│   │   │   ├── SidebarContent/                     # Sidebar content VMs
│   │   │   │   ├── ExplorerViewModel.cs            # File explorer VM
│   │   │   │   ├── ConnectionsViewModel.cs         # Existing (enhanced)
│   │   │   │   ├── LoggingViewModel.cs             # Logging content VM
│   │   │   │   └── SettingsViewModel.cs            # Existing (enhanced)
│   │   │   ├── Dialogs/                            # Dialog ViewModels
│   │   │   │   ├── ConfirmationDialogViewModel.cs  # Existing
│   │   │   │   └── AboutViewModel.cs               # Existing
│   │   │   └── Legacy/                             # Legacy ViewModels
│   │   │       ├── HomeViewModel.cs                # To be removed
│   │   │       ├── NavigationItemViewModel.cs     # To be removed
│   │   ��       └── TabViewModel.cs                 # To be removed
│   │   ├── Services/
│   │   │   ├── Interfaces/                         # Service contracts
│   │   │   │   ├── ILayoutService.cs               # Layout state management
│   │   │   │   ├── IActivityBarService.cs          # Activity bar management
│   │   │   │   ├── IThemeService.cs                # Theme management
│   │   │   │   ├── IUIThreadService.cs             # UI thread operations
│   │   │   │   ├── ILocalizationService.cs         # String localization
│   │   │   │   ├── ISidebarContentService.cs       # Dynamic content management
│   │   │   │   ├── IClipboardService.cs            # Existing
│   │   │   │   ├── IDialogService.cs               # Existing
│   │   │   │   └── IGreetingService.cs             # Existing
│   │   │   ├── LayoutService.cs                    # Layout state implementation
│   │   │   ├── ActivityBarService.cs               # Activity bar implementation
│   │   │   ├── ThemeService.cs                     # Theme management implementation
│   │   │   ├── AvaloniaUIThreadService.cs          # UI thread service
│   │   │   ├── LocalizationService.cs              # Localization implementation
│   │   │   ├── SidebarContentService.cs            # Content management
│   │   │   ├── ClipboardService.cs                 # Existing
│   │   │   ├── DialogService.cs                    # Existing
│   │   │   ├── GreetingService.cs                  # Existing
│   │   │   └── PlcDataService.cs                   # Existing (enhanced with logging)
│   │   ├── Resources/
│   │   │   ├── Strings/                            # Localization resources
│   │   │   │   ├── UIStrings.resx                  # Main UI strings
│   │   │   │   ├── UIStrings.Designer.cs           # Generated accessor
│   │   │   │   ├── UIStrings.de.resx               # German localization
│   │   │   │   └── UIStrings.es.resx               # Spanish localization
│   │   │   ├── Themes/                             # Theme resources
│   │   │   │   ├── VSCodeDarkTheme.axaml           # Dark theme
│   │   │   │   ├── VSCodeLightTheme.axaml          # Light theme
│   │   │   │   ├── VSCodeColors.axaml              # Color definitions
│   │   │   │   ├── VSCodeBrushes.axaml             # Brush resources
│   │   │   │   └── VSCodeSizes.axaml               # Size constants
│   │   │   └── Icons/                              # Icon resources
│   │   │       ├── activity-bar/                   # Activity bar icons
│   │   │       ├── menu/                           # Menu icons
│   │   │       └── status/                         # Status icons
│   │   ├── Styles/
│   │   │   ├── Styles.axaml                        # Main style file (existing)
│   │   │   ├── ActivityBarStyles.axaml             # Activity bar styles
│   │   │   ├── SidebarStyles.axaml                 # Sidebar styles
│   │   │   ├── MenuStyles.axaml                    # Menu styles
│   │   │   ├── LogViewerStyles.axaml               # LogViewer styles
│   │   │   ├── PanelStyles.axaml                   # Bottom panel styles
│   │   │   ├── InteractionStyles.axaml             # Hover/selection states
│   │   │   └── AnimationStyles.axaml               # Animation definitions
│   │   ├── Converters/
│   │   │   ├── BooleanToInverseBooleanConverter.cs # Existing
│   │   │   ├── BooleanToVisibilityConverter.cs     # Existing
│   │   │   ├── GridLengthToIconConverter.cs        # Existing
│   │   │   ├── ObjectConverters.cs                 # Existing
│   │   │   ├── LogLevelToColorConverter.cs         # Log level colors
│   │   │   ├── LogLevelToIconConverter.cs          # Log level icons
│   │   │   └── EventIdConverter.cs                 # Event ID formatting
│   │   ├── Commands/
│   │   │   ├── ApplicationCommands.cs              # Global app commands
│   │   │   ├── ActivityBarCommands.cs              # Activity bar commands
│   │   │   ├── MenuCommands.cs                     # Menu commands
│   │   │   └── LogViewerCommands.cs                # LogViewer commands
│   │   ├── Models/
│   │   │   ├── ActivityBarItem.cs                  # Activity bar item model
│   │   │   ├── PanelTabItem.cs                     # Panel tab model
│   │   │   ├── SidebarContentItem.cs               # Sidebar content model
│   │   │   └── MenuItemModel.cs                    # Menu item model
│   │   ├── Behaviors/
│   │   │   ├── CloseApplicationBehavior.cs         # Existing
│   │   │   ├── HoverBehavior.cs                    # Hover state behavior
│   │   │   ├── SelectionBehavior.cs                # Selection behavior
│   │   │   └── AnimationBehavior.cs                # Animation triggers
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs     # DI service registration
│   │   │   ├── LoggingExtensions.cs                # Logging helpers
│   │   │   └── ThemeExtensions.cs                  # Theme utilities
│   │   ├── Assets/
│   │   │   └── avalonia-logo.ico                   # Existing
│   │   ├── App.axaml                               # Application definition
│   │   ├── App.axaml.cs                            # Application code-behind
│   │   ├── Program.cs                              # Entry point with DI setup
│   │   ├── ViewLocator.cs                          # View resolution
│   │   ├── appsettings.json                        # Application configuration
│   │   ├── app.manifest                            # Application manifest
│   │   └── S7Tools.csproj                          # Project file
│   ├── S7Tools.Core/                               # Core business logic (existing)
│   │   ├── Models/
│   │   │   └── Tag.cs                              # Existing
│   │   ├── Services/
│   │   │   └── Interfaces/
│   │   │       ├── IS7ConnectionProvider.cs        # Existing
│   │   │       └── ITagRepository.cs               # Existing
│   │   └── S7Tools.Core.csproj                     # Existing
│   ├── S7Tools.Infrastructure.Logging/             # New logging infrastructure
│   │   ├── Core/
│   │   │   ├── Models/
│   │   │   │   ├── LogModel.cs                     # Core log entry model
│   │   │   │   ├── LogEntryColor.cs                # Color configuration
│   │   │   │   └── LogDataStoreOptions.cs          # Storage options
│   │   │   ├── Storage/
│   │   │   │   ├── ILogDataStore.cs                # Storage interface
│   │   │   │   └── LogDataStore.cs                 # Thread-safe storage
│   │   │   └── Configuration/
│   │   │       └── DataStoreLoggerConfiguration.cs # Logger config
│   │   ├── Providers/
│   │   │   ├── Microsoft/
│   │   │   │   ├── DataStoreLogger.cs              # ILogger implementation
│   │   │   │   └── DataStoreLoggerProvider.cs      # Logger provider
│   │   │   └── Extensions/
│   │   │       └── LoggingServiceCollectionExtensions.cs # DI extensions
│   │   └── S7Tools.Infrastructure.Logging.csproj   # Project file
│   └── S7Tools.sln                                 # Solution file
├── tests/                                          # Test projects
│   ├── S7Tools.Tests/                              # Main application tests
│   │   ├── Services/
│   │   │   ├── LayoutServiceTests.cs               # Layout service tests
│   │   │   ├── ActivityBarServiceTests.cs          # Activity bar tests
│   │   │   ├── ThemeServiceTests.cs                # Theme service tests
│   │   │   ├── UIThreadServiceTests.cs             # UI thread tests
│   │   │   └── LocalizationServiceTests.cs         # Localization tests
│   │   ├── ViewModels/
│   │   │   ├── MainWindowViewModelTests.cs         # Main VM tests
│   │   │   ├── ActivityBarViewModelTests.cs        # Activity bar VM tests
│   │   │   └── LogViewerControlViewModelTests.cs   # LogViewer VM tests
│   │   ├── Converters/
│   │   │   ├── LogLevelToColorConverterTests.cs    # Converter tests
│   │   │   └── LogLevelToIconConverterTests.cs     # Icon converter tests
│   │   └── S7Tools.Tests.csproj                    # Test project file
│   ├── S7Tools.Infrastructure.Logging.Tests/       # Logging infrastructure tests
│   │   ├── Storage/
│   │   │   └── LogDataStoreTests.cs                # Storage tests
│   │   ├── Providers/
│   │   │   ├── DataStoreLoggerTests.cs             # Logger tests
│   │   │   └── DataStoreLoggerProviderTests.cs     # Provider tests
│   │   └── S7Tools.Infrastructure.Logging.Tests.csproj # Test project
│   └── S7Tools.Integration.Tests/                  # Integration tests
│       ├── UIIntegrationTests.cs                   # UI integration tests
│       ├── LoggingIntegrationTests.cs              # Logging integration
│       └── S7Tools.Integration.Tests.csproj        # Integration test project
├── docs/                                           # Documentation
│   ├── architecture/
│   │   ├── adr-001-vscode-ui-architecture.md       # Architecture decision
│   │   ├── adr-002-logging-infrastructure.md       # Logging architecture
│   │   └── adr-003-service-architecture.md         # Service architecture
│   ├── user-guide/
│   │   ├── getting-started.md                      # User getting started
│   │   ├── logging-features.md                     # Logging documentation
│   │   └── keyboard-shortcuts.md                   # Shortcut reference
│   └── developer-guide/
│       ├── contributing.md                         # Contribution guidelines
│       ├── building.md                             # Build instructions
│       └── testing.md                              # Testing guidelines
├── .copilot-tracking/                              # Implementation tracking
│   ├── details/
│   │   ├── unified-s7tools-implementation-plan.md  # Main implementation plan
│   │   ├── unified-project-structure.md            # This document
│   │   └── 20251006-vscode-ui-implementation-details.md # VSCode UI details
│   ├── tracking/
│   │   ├── unified-s7tools-implementation-tracking.md # Main tracking
│   │   └── logviewer-implementation-tracking.md    # Original LogViewer tracking
│   ├── plans/
│   │   └── 20251006-vscode-ui-implementation-plan.instructions.md # VSCode plan
│   ├── research/
│   │   ├── logviewer-implementation-research.md    # LogViewer research
│   │   └── 20251006-vscode-ui-implementation-research.md # VSCode research
│   └── reviews/
│       ├── 20251006-dotnet-design-pattern-review.md # Design patterns
│       └── 20251006-ui-thread-safety-resources-review.md # UI safety
├── .github/
│   ├── workflows/
│   │   ├── build.yml                               # CI/CD pipeline
│   │   └── test.yml                                # Test automation
│   └── prompts/
│       └── update-implementation-plan.prompt.md    # Implementation prompt
├── .editorconfig                                   # Code style configuration
├── .gitattributes                                  # Git attributes
├── .gitignore                                      # Git ignore rules
├── README.md                                       # Project README
└── CHANGELOG.md                                    # Change log
```

## Key Architectural Changes

### 1. Main Application Structure (S7Tools)

#### Views Reorganization
- **MainWindow**: Complete restructure to VSCode-like layout
- **Components**: New reusable UI components for modular design
- **SidebarContent**: Dynamic content system for activity bar integration
- **Dialogs**: Centralized dialog management
- **Legacy**: Deprecated views marked for removal

#### ViewModels Enhancement
- **Component-based**: Separate ViewModels for each UI component
- **Service Integration**: Heavy use of injected services for business logic
- **Reactive Patterns**: Enhanced ReactiveUI usage for real-time updates

#### Services Architecture
- **Layout Management**: Services for managing UI state and behavior
- **Theme Management**: Comprehensive theming system
- **Localization**: Multi-language support infrastructure
- **UI Threading**: Safe cross-thread UI operations

### 2. New Logging Infrastructure (S7Tools.Infrastructure.Logging)

#### Core Components
- **Models**: Strongly-typed log entry models with color configuration
- **Storage**: Thread-safe circular buffer for high-performance logging
- **Configuration**: Flexible configuration system for logging behavior

#### Microsoft.Extensions.Logging Integration
- **Provider Pattern**: Custom logger provider for seamless integration
- **Service Extensions**: Easy DI registration and configuration

### 3. Enhanced Testing Strategy

#### Test Organization
- **Unit Tests**: Comprehensive service and ViewModel testing
- **Integration Tests**: End-to-end functionality validation
- **UI Tests**: User interaction and visual behavior testing

### 4. Documentation Structure

#### Architecture Documentation
- **ADRs**: Architecture Decision Records for major decisions
- **User Guides**: Comprehensive user documentation
- **Developer Guides**: Technical documentation for contributors

## Migration Strategy

### Phase 1: Foundation
1. Create new logging infrastructure project
2. Implement core services (Layout, Theme, UI Thread)
3. Set up resource management system

### Phase 2: UI Transformation
1. Restructure MainWindow layout
2. Implement activity bar and sidebar components
3. Create menu and status bar systems

### Phase 3: LogViewer Integration
1. Integrate LogViewer components
2. Connect to logging infrastructure
3. Add to activity bar and sidebar

### Phase 4: Enhancement
1. Apply comprehensive theming
2. Add advanced interactions
3. Implement animations and polish

### Phase 5: Testing & Documentation
1. Complete test suite implementation
2. Validate performance and compatibility
3. Finalize documentation

## Compatibility Considerations

### Existing Functionality Preservation
- All existing services maintain their interfaces
- Legacy views remain functional during transition
- Gradual migration path for user workflows

### Cross-Platform Support
- Avalonia framework ensures cross-platform compatibility
- Platform-specific testing and optimization
- Consistent behavior across Windows, Linux, and macOS

### Performance Optimization
- Circular buffer for efficient log storage
- Virtualization for large data sets
- Background processing for non-UI operations
- Memory management and leak prevention

## Future Extensibility

### Plugin Architecture
- Service-based design enables easy extension
- Dynamic content system supports new sidebar panels
- Command system allows for custom actions

### Theming System
- Comprehensive resource system supports custom themes
- Runtime theme switching capability
- User customization support

### Localization Support
- Resource-based string management
- Multi-language support infrastructure
- Cultural formatting and display options

This unified project structure provides a solid foundation for the modern S7Tools application while maintaining compatibility with existing functionality and enabling future enhancements.