<!-- markdownlint-disable-file -->
# Task Details: VSCode-like UI Implementation

## Research Reference

**Source Research**: #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md

## Phase 1: Foundation & Services

### Task 1.1: Create Resource Management System

Implement comprehensive resource management system for string localization and theme resources.

- **Files**:
  - Resources/Strings/UIStrings.resx - Main UI strings resource file
  - Resources/Strings/UIStrings.Designer.cs - Generated resource accessor
  - Services/Interfaces/ILocalizationService.cs - Localization service interface
  - Services/LocalizationService.cs - Localization service implementation
- **Success**:
  - All hardcoded strings moved to resource files
  - Localization service properly registered in DI container
  - Resource files generate strongly-typed accessors
  - Culture switching functionality implemented
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 180-220) - Resource management patterns
  - #file: ./.copilot-tracking/reviews/20251006-ui-thread-safety-resources-review.md (Lines 250-290) - Resource implementation requirements
- **Dependencies**:
  - System.Resources.ResourceManager
  - System.Globalization.CultureInfo

### Task 1.2: Implement UI Thread Service

Create thread-safe UI operation service for cross-thread UI updates.

- **Files**:
  - Services/Interfaces/IUIThreadService.cs - UI thread service interface
  - Services/AvaloniaUIThreadService.cs - Avalonia-specific implementation
- **Success**:
  - UI thread marshalling service implemented
  - All async operations use ConfigureAwait(false)
  - Cross-thread UI updates handled safely
  - Service registered as singleton in DI container
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 221-250) - Thread safety implementation
  - #file: ./.copilot-tracking/reviews/20251006-ui-thread-safety-resources-review.md (Lines 80-120) - Thread safety requirements
- **Dependencies**:
  - Avalonia.Threading.Dispatcher
  - System.Threading.Tasks

### Task 1.3: Create Layout Management Services

Implement services for managing layout state and behavior.

- **Files**:
  - Services/Interfaces/ILayoutService.cs - Layout management interface
  - Services/LayoutService.cs - Layout service implementation
  - Services/Interfaces/IActivityBarService.cs - Activity bar management interface
  - Services/ActivityBarService.cs - Activity bar service implementation
  - ViewModels/ActivityBarItemViewModel.cs - Activity bar item view model
- **Success**:
  - Layout state management (sidebar, panel visibility/sizes)
  - Activity bar item management and selection
  - Reactive property notifications for UI binding
  - Proper service lifetime management
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 251-290) - Service architecture
  - #file: ./.copilot-tracking/reviews/20251006-dotnet-design-pattern-review.md (Lines 180-220) - Service pattern implementation
- **Dependencies**:
  - ReactiveUI for property notifications
  - System.Collections.ObjectModel

### Task 1.4: Implement Theme Management Service

Create comprehensive theme management system with VSCode color scheme.

- **Files**:
  - Services/Interfaces/IThemeService.cs - Theme service interface
  - Services/ThemeService.cs - Theme service implementation
  - Resources/Themes/VSCodeDarkTheme.axaml - VSCode dark theme resources
  - Resources/Themes/VSCodeLightTheme.axaml - VSCode light theme resources
- **Success**:
  - Theme switching functionality
  - VSCode color scheme implemented
  - Runtime theme changes supported
  - Theme persistence across application restarts
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 350-390) - Theme implementation
- **Dependencies**:
  - Avalonia.Styling.ThemeVariant
  - Avalonia.Markup.Xaml.Styling

## Phase 2: Core Layout Structure

### Task 2.1: Create New MainWindow Layout

Replace current FluentAvalonia NavigationView with VSCode-like layout structure.

- **Files**:
  - Views/MainWindow.axaml - Complete layout restructure
  - Views/MainWindow.axaml.cs - Updated code-behind
  - ViewModels/MainWindowViewModel.cs - Simplified main window view model
- **Success**:
  - DockPanel root layout with menu, status bar, and main content
  - Grid-based main content with activity bar, sidebar, and editor areas
  - Proper column/row definitions for resizable areas
  - Clean separation of layout concerns
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 80-140) - Layout structure analysis
  - #githubRepo:"AvaloniaUI/Avalonia layout panels" - Layout implementation patterns
- **Dependencies**:
  - Avalonia.Controls.DockPanel
  - Avalonia.Controls.Grid
  - Avalonia.Controls.GridSplitter

### Task 2.2: Implement Menu Bar with Key Bindings

Create comprehensive menu system with proper keyboard shortcuts.

- **Files**:
  - Views/Components/MenuBarView.axaml - Menu bar user control
  - Views/Components/MenuBarView.axaml.cs - Menu bar code-behind
  - ViewModels/MenuBarViewModel.cs - Menu bar view model
  - Commands/ApplicationCommands.cs - Global application commands
- **Success**:
  - File, Edit, View menus with standard items
  - Key bindings for all menu items (Ctrl+N, Ctrl+O, etc.)
  - Menu items properly bound to commands
  - InputGesture text displayed in menu items
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 141-179) - Menu implementation
  - #githubRepo:"AvaloniaUI/Avalonia keyboard shortcuts" - Key binding patterns
- **Dependencies**:
  - Avalonia.Controls.Menu
  - Avalonia.Input.KeyBinding
  - System.Windows.Input.ICommand

### Task 2.3: Create Activity Bar Component

Implement VSCode-style activity bar with icon selection and hover states.

- **Files**:
  - Views/Components/ActivityBarView.axaml - Activity bar user control
  - Views/Components/ActivityBarView.axaml.cs - Activity bar code-behind
  - ViewModels/ActivityBarViewModel.cs - Activity bar view model
  - Models/ActivityBarItem.cs - Activity bar item model
- **Success**:
  - Fixed 48px width vertical icon bar
  - Icon selection states (gray/white/hover)
  - Selection indicator rectangle
  - Tooltip support for each item
  - Click behavior for sidebar toggle
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 291-330) - Activity bar implementation
- **Dependencies**:
  - Projektanker.Icons.Avalonia.FontAwesome
  - Avalonia.Controls.ToolTip

### Task 2.4: Implement Collapsible Sidebar

Create resizable sidebar that responds to activity bar selection.

- **Files**:
  - Views/Components/SidebarView.axaml - Sidebar user control
  - Views/Components/SidebarView.axaml.cs - Sidebar code-behind
  - ViewModels/SidebarViewModel.cs - Sidebar view model
  - Views/Components/SidebarContentView.axaml - Dynamic content container
- **Success**:
  - Collapsible sidebar with smooth animations
  - Dynamic content based on activity bar selection
  - Resizable width with GridSplitter
  - Proper visibility binding to layout service
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 331-349) - Sidebar implementation
- **Dependencies**:
  - Avalonia.Controls.GridSplitter
  - Avalonia.Animation

## Phase 3: Interactive Components

### Task 3.1: Create Resizable Bottom Panel

Implement collapsible bottom panel with tab control for output/logs.

- **Files**:
  - Views/Components/BottomPanelView.axaml - Bottom panel user control
  - Views/Components/BottomPanelView.axaml.cs - Bottom panel code-behind
  - ViewModels/BottomPanelViewModel.cs - Bottom panel view model
  - ViewModels/PanelTabViewModel.cs - Panel tab view model
- **Success**:
  - Collapsible panel with tab control
  - Resizable height with horizontal GridSplitter
  - Tab headers visible when collapsed
  - Output, Problems, Terminal, Debug Console tabs
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 60-79) - Panel layout analysis
- **Dependencies**:
  - Avalonia.Controls.TabControl
  - Avalonia.Controls.GridSplitter

### Task 3.2: Implement Status Bar

Create fixed-height status bar with application status information.

- **Files**:
  - Views/Components/StatusBarView.axaml - Status bar user control
  - Views/Components/StatusBarView.axaml.cs - Status bar code-behind
  - ViewModels/StatusBarViewModel.cs - Status bar view model
- **Success**:
  - Fixed 22px height status bar
  - VSCode accent color background
  - Status text and information display
  - Proper docking at window bottom
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 55-59) - Status bar requirements
- **Dependencies**:
  - Avalonia.Controls.Border
  - Avalonia.Controls.TextBlock

### Task 3.3: Add Activity Bar Selection Logic

Implement activity bar item selection and sidebar toggle behavior.

- **Files**:
  - Services/ActivityBarService.cs - Enhanced selection logic
  - ViewModels/ActivityBarItemViewModel.cs - Selection state management
  - Commands/ActivityBarCommands.cs - Activity bar specific commands
- **Success**:
  - Click unselected item expands sidebar
  - Click selected item collapses sidebar
  - First item selected on application startup
  - Proper selection state visual feedback
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 25-35) - Activity bar behavior
- **Dependencies**:
  - ReactiveUI command system
  - Layout service integration

### Task 3.4: Implement Sidebar Content Management

Create dynamic content system for sidebar based on activity bar selection.

- **Files**:
  - Services/Interfaces/ISidebarContentService.cs - Content management interface
  - Services/SidebarContentService.cs - Content service implementation
  - ViewModels/SidebarContentViewModels/ - Content-specific view models
  - Views/SidebarContent/ - Content-specific views
- **Success**:
  - Dynamic content loading based on selection
  - Proper view model lifecycle management
  - Content caching for performance
  - Smooth content transitions
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 391-420) - Content management
- **Dependencies**:
  - Avalonia.Controls.ContentControl
  - ViewLocator pattern

## Phase 4: Styling & Theming

### Task 4.1: Create VSCode Theme Resources

Implement comprehensive VSCode color scheme and styling resources.

- **Files**:
  - Resources/Themes/VSCodeColors.axaml - Color resource dictionary
  - Resources/Themes/VSCodeBrushes.axaml - Brush resource dictionary
  - Resources/Themes/VSCodeSizes.axaml - Size and spacing resources
- **Success**:
  - Complete VSCode color palette implemented
  - Consistent brush resources for all components
  - Proper spacing and sizing constants
  - Theme resource organization
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 421-460) - VSCode color scheme
- **Dependencies**:
  - Avalonia.Markup.Xaml.Styling
  - ResourceDictionary system

### Task 4.2: Implement Component Styles

Create comprehensive styles for all UI components.

- **Files**:
  - Styles/ActivityBarStyles.axaml - Activity bar component styles
  - Styles/SidebarStyles.axaml - Sidebar component styles
  - Styles/MenuStyles.axaml - Menu and menu item styles
  - Styles/PanelStyles.axaml - Bottom panel styles
- **Success**:
  - Consistent styling across all components
  - Proper use of theme resources
  - Component-specific style organization
  - Style inheritance and composition
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 461-490) - Component styling
- **Dependencies**:
  - Avalonia.Styling.Style
  - Avalonia.Styling.Selector

### Task 4.3: Add Hover and Selection States

Implement interactive visual states for all clickable elements.

- **Files**:
  - Styles/InteractionStyles.axaml - Hover, pressed, selected states
  - Behaviors/HoverBehavior.cs - Custom hover behavior
- **Success**:
  - Hover states for all interactive elements
  - Selection states with proper visual feedback
  - Smooth state transitions
  - Consistent interaction patterns
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 25-35) - Interaction states
- **Dependencies**:
  - Avalonia.Xaml.Interactivity
  - Avalonia.Animation

### Task 4.4: Implement Smooth Animations

Add smooth animations for layout changes and state transitions.

- **Files**:
  - Animations/LayoutAnimations.axaml - Layout transition animations
  - Animations/StateAnimations.axaml - State change animations
- **Success**:
  - Smooth sidebar collapse/expand animations
  - Panel resize animations
  - State transition animations
  - Performance-optimized animations
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 491-510) - Animation implementation
- **Dependencies**:
  - Avalonia.Animation
  - Avalonia.Animation.Easings

## Phase 5: Integration & Testing

### Task 5.1: Refactor MainWindowViewModel

Simplify and refactor MainWindowViewModel to use new service architecture.

- **Files**:
  - ViewModels/MainWindowViewModel.cs - Refactored view model
  - ViewModels/ViewModelBase.cs - Enhanced base class
- **Success**:
  - Single responsibility principle compliance
  - Proper service dependency injection
  - Simplified command handling
  - Clean separation of concerns
- **Research References**:
  - #file: ./.copilot-tracking/reviews/20251006-ui-thread-safety-resources-review.md (Lines 180-220) - SRP compliance
  - #file: ./.copilot-tracking/reviews/20251006-dotnet-design-pattern-review.md (Lines 220-260) - ViewModel refactoring
- **Dependencies**:
  - All implemented services
  - ReactiveUI patterns

### Task 5.2: Update Dependency Injection Configuration

Update DI container configuration for all new services and components.

- **Files**:
  - Program.cs - Enhanced service registration
  - Extensions/ServiceCollectionExtensions.cs - Service registration extensions
- **Success**:
  - All services properly registered
  - Correct service lifetimes configured
  - ViewModels registered with dependencies
  - Service validation on startup
- **Research References**:
  - #file: ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md (Lines 511-535) - DI configuration
- **Dependencies**:
  - Microsoft.Extensions.DependencyInjection
  - All service interfaces and implementations

### Task 5.3: Implement Error Handling and Logging

Add comprehensive error handling and structured logging throughout the application.

- **Files**:
  - Services/Interfaces/ILoggingService.cs - Logging service interface
  - Services/LoggingService.cs - Logging service implementation
  - Extensions/LoggingExtensions.cs - Logging helper extensions
- **Success**:
  - Structured logging implemented
  - Error handling in all services
  - User-friendly error messages
  - Debug logging for troubleshooting
- **Research References**:
  - #file: ./.copilot-tracking/reviews/20251006-ui-thread-safety-resources-review.md (Lines 320-360) - Error handling requirements
- **Dependencies**:
  - Microsoft.Extensions.Logging
  - Serilog (recommended)

### Task 5.4: Add Unit Tests for Services

Create comprehensive unit test suite for all new services.

- **Files**:
  - Tests/Services/LayoutServiceTests.cs - Layout service tests
  - Tests/Services/ActivityBarServiceTests.cs - Activity bar service tests
  - Tests/Services/ThemeServiceTests.cs - Theme service tests
  - Tests/ViewModels/MainWindowViewModelTests.cs - Main view model tests
- **Success**:
  - 80%+ code coverage for services
  - All service methods tested
  - Mock dependencies properly configured
  - Integration test scenarios covered
- **Research References**:
  - #file: ./.copilot-tracking/reviews/20251006-dotnet-design-pattern-review.md (Lines 300-340) - Testing requirements
- **Dependencies**:
  - xUnit testing framework
  - Moq or NSubstitute for mocking
  - Microsoft.Extensions.DependencyInjection.Testing

## Performance Considerations

### Memory Management
- Implement proper disposal patterns for all ViewModels
- Use weak event subscriptions to prevent memory leaks
- Cache frequently accessed resources
- Optimize image and icon loading

### UI Responsiveness
- Use virtualization for large collections
- Implement lazy loading for sidebar content
- Optimize animation performance
- Use background threads for heavy operations

### Testing Strategy

### Unit Tests
- Service behavior and interaction testing
- ViewModel command and property testing
- Layout service state management testing

### Integration Tests
- Service integration and dependency resolution
- Layout behavior and state persistence
- Theme switching and resource loading

### UI Tests
- Activity bar interaction and selection
- Sidebar collapse/expand behavior
- Panel resizing and tab switching
- Key binding functionality

## Deployment Considerations

### Configuration Management
- Theme preference persistence
- Layout state persistence
- User customization settings
- Performance optimization settings

### Platform Compatibility
- Windows-specific key binding testing
- Linux desktop environment compatibility
- macOS menu bar integration
- High DPI display support