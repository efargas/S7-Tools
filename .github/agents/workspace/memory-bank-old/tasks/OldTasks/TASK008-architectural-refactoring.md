# TASK008: Critical Architectural Refactoring

**Priority**: CRITICAL  
**Status**: PHASE 2 ARCHITECTURALLY COMPLETE  
**Estimated Effort**: 1-2 weeks  
**Assigned To**: Development Team  
**Created**: Current Session  
**Phase 1 Completed**: ✅ MVVM Violations Fixed  
**Phase 2 Completed**: ✅ MainWindowViewModel Decomposed (Architecturally)  

## Task Overview

Address critical MVVM violations and architectural inconsistencies that violate fundamental design patterns and make the codebase untestable and brittle. This task focuses on the highest priority architectural issues that must be resolved before any new feature development.

## Problem Statement

The S7Tools application contains severe architectural violations that contradict its otherwise excellent foundation:

1. **View-ViewModel Circular Dependencies**: DialogService creates Views and passes them to ViewModels
2. **Direct View Instantiation**: ViewModels directly create View instances
3. **Dependency Injection Bypassing**: ViewModels create other ViewModels with `new` keyword
4. **God Object Anti-Pattern**: MainWindowViewModel handles too many responsibilities

These issues make the code untestable, brittle, and violate MVVM and SOLID principles.

## Acceptance Criteria

### **Phase 1: Fix MVVM Violations (Days 1-3)**

#### **AC1.1: Refactor Dialog System**
- [ ] Remove View instance creation from DialogService
- [ ] Implement ReactiveUI Interaction pattern for dialogs
- [ ] Remove Window dependency from ConfirmationDialogViewModel constructor
- [ ] Update MainWindow to handle dialog interactions
- [ ] All dialog operations work without View-ViewModel coupling

#### **AC1.2: Remove Direct View Instantiation**
- [ ] Remove all `new ViewName()` calls from ViewModels
- [ ] Create IViewModelFactory service for ViewModel creation
- [ ] Register IViewModelFactory in DI container
- [ ] Update MainWindowViewModel to use ViewModelFactory
- [ ] Rely on ViewLocator for automatic View resolution

#### **AC1.3: Fix Dependency Injection Bypassing**
- [ ] Register all ViewModels in DI container
- [ ] Update HomeViewModel to use IViewModelFactory
- [ ] Update ConnectionsViewModel to use IViewModelFactory
- [ ] Remove all `new ViewModelName()` calls from ViewModels
- [ ] All ViewModels receive their dependencies through constructor injection

### **Phase 2: Decompose MainWindowViewModel (Days 4-6)**

#### **AC2.1: Create Specialized ViewModels**
- [ ] Create BottomPanelViewModel with tab management logic
- [ ] Create NavigationViewModel with sidebar and content management
- [ ] Create SettingsManagementViewModel with all settings-related logic
- [ ] Move clipboard operations to dedicated service or ViewModel
- [ ] MainWindowViewModel reduced to under 200 lines

#### **AC2.2: Update View Bindings**
- [ ] Update MainWindow.axaml to bind to new ViewModel structure
- [ ] Ensure all existing functionality continues to work
- [ ] Maintain all keyboard shortcuts and commands
- [ ] Preserve all UI behaviors and animations

### **Phase 3: Testing Infrastructure (Days 7-10)**

#### **AC3.1: Set Up Testing Framework**
- [ ] Create test projects for each main project
- [ ] Add xUnit, Moq, and FluentAssertions NuGet packages
- [ ] Create base test classes for ViewModels and Services
- [ ] Set up test data builders and mocks

#### **AC3.2: Write Critical Tests**
- [ ] Unit tests for all new ViewModels
- [ ] Unit tests for IViewModelFactory
- [ ] Unit tests for dialog interaction system
- [ ] Integration tests for navigation flow
- [ ] Achieve >70% code coverage for refactored components

## Technical Implementation Details

### **Dialog System Refactoring**

**Before (WRONG)**:
```csharp
public async Task<bool> ShowConfirmationAsync(string title, string message)
{
    var dialog = new ConfirmationDialog();
    dialog.DataContext = new ConfirmationDialogViewModel(dialog, title, message);
    return await dialog.ShowDialog<bool>(desktop.MainWindow);
}
```

**After (CORRECT)**:
```csharp
public class MainWindowViewModel : ReactiveObject
{
    public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; }
    
    private async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var request = new ConfirmationRequest(title, message);
        return await ShowConfirmation.Handle(request);
    }
}

// In MainWindow.axaml.cs
private void SetupInteractions()
{
    ViewModel.ShowConfirmation.RegisterHandler(async interaction =>
    {
        var dialog = new ConfirmationDialog();
        var viewModel = new ConfirmationDialogViewModel(
            interaction.Input.Title, 
            interaction.Input.Message);
        dialog.DataContext = viewModel;
        
        var result = await dialog.ShowDialog<bool>(this);
        interaction.SetOutput(result);
    });
}
```

### **ViewModel Factory Implementation**

```csharp
public interface IViewModelFactory
{
    T Create<T>() where T : ViewModelBase;
    ViewModelBase Create(Type viewModelType);
}

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public T Create<T>() where T : ViewModelBase
    {
        return _serviceProvider.GetRequiredService<T>();
    }
    
    public ViewModelBase Create(Type viewModelType)
    {
        return (ViewModelBase)_serviceProvider.GetRequiredService(viewModelType);
    }
}
```

### **Decomposed ViewModel Structure**

```csharp
public class MainWindowViewModel : ReactiveObject
{
    public NavigationViewModel Navigation { get; }
    public BottomPanelViewModel BottomPanel { get; }
    public SettingsManagementViewModel Settings { get; }
    
    public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    
    public MainWindowViewModel(
        NavigationViewModel navigation,
        BottomPanelViewModel bottomPanel,
        SettingsManagementViewModel settings)
    {
        Navigation = navigation;
        BottomPanel = bottomPanel;
        Settings = settings;
        
        ShowConfirmation = new Interaction<ConfirmationRequest, bool>();
        ExitCommand = ReactiveCommand.CreateFromTask(ExitAsync);
    }
    
    private async Task ExitAsync()
    {
        var result = await ShowConfirmation.Handle(
            new ConfirmationRequest("Exit Application", "Are you sure you want to exit?"));
        if (result)
        {
            // Handle application exit
        }
    }
}
```

## Files to Modify

### **Phase 1 Files**
- `src/S7Tools/Services/DialogService.cs` - Refactor to use interactions
- `src/S7Tools/Services/Interfaces/IDialogService.cs` - Update interface
- `src/S7Tools/ViewModels/ConfirmationDialogViewModel.cs` - Remove Window dependency
- `src/S7Tools/ViewModels/MainWindowViewModel.cs` - Add interactions, remove View creation
- `src/S7Tools/Views/MainWindow.axaml.cs` - Add interaction handlers
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` - Register ViewModelFactory and ViewModels
- `src/S7Tools/ViewModels/HomeViewModel.cs` - Use IViewModelFactory
- `src/S7Tools/ViewModels/ConnectionsViewModel.cs` - Use IViewModelFactory

### **Phase 2 Files**
- `src/S7Tools/ViewModels/NavigationViewModel.cs` - NEW FILE
- `src/S7Tools/ViewModels/BottomPanelViewModel.cs` - NEW FILE
- `src/S7Tools/ViewModels/SettingsManagementViewModel.cs` - NEW FILE
- `src/S7Tools/Views/MainWindow.axaml` - Update bindings
- `src/S7Tools/ViewModels/MainWindowViewModel.cs` - Major refactoring

### **Phase 3 Files**
- `tests/S7Tools.Tests/S7Tools.Tests.csproj` - NEW FILE
- `tests/S7Tools.Tests/ViewModels/NavigationViewModelTests.cs` - NEW FILE
- `tests/S7Tools.Tests/ViewModels/BottomPanelViewModelTests.cs` - NEW FILE
- `tests/S7Tools.Tests/Services/ViewModelFactoryTests.cs` - NEW FILE
- `tests/S7Tools.Tests/TestBase/ViewModelTestBase.cs` - NEW FILE

## Testing Strategy

### **Unit Tests**
- Test all new ViewModels in isolation
- Mock all dependencies using Moq
- Test command execution and property changes
- Verify interaction handling

### **Integration Tests**
- Test navigation flow between ViewModels
- Test dialog interaction system end-to-end
- Verify DI container resolves all dependencies correctly

### **Regression Tests**
- Ensure all existing UI functionality continues to work
- Test all keyboard shortcuts and commands
- Verify all animations and behaviors are preserved

## Risk Assessment

### **High Risk**
- **Breaking existing functionality**: Mitigation - Comprehensive testing before and after changes
- **Complex refactoring scope**: Mitigation - Implement in small, incremental steps

### **Medium Risk**
- **View binding issues**: Mitigation - Update bindings incrementally, test each change
- **DI configuration errors**: Mitigation - Validate DI container configuration with tests

### **Low Risk**
- **Performance impact**: Mitigation - Profile before and after, optimize if needed

## Definition of Done

- [ ] All acceptance criteria met
- [ ] All existing functionality preserved
- [ ] No MVVM violations remain in codebase
- [ ] All new code has unit tests with >70% coverage
- [ ] Code review completed and approved
- [ ] Documentation updated to reflect new architecture
- [ ] Performance benchmarks maintained or improved
- [ ] All static analysis warnings resolved

## Dependencies

### **Blocked By**
- None (can start immediately)

### **Blocks**
- Any new feature development should wait until architectural issues are resolved
- PLC communication implementation should use the new architecture patterns

## Success Metrics

### **Code Quality Metrics**
- MainWindowViewModel reduced from 600+ lines to <200 lines
- Zero direct View instantiation in ViewModels
- Zero circular dependencies between View and ViewModel layers
- All ViewModels created through DI container

### **Testing Metrics**
- >70% code coverage for all refactored components
- All critical user flows covered by integration tests
- Zero test failures in CI/CD pipeline

### **Architecture Metrics**
- All SOLID principles properly applied
- Clean separation of concerns maintained
- Dependency flow follows Clean Architecture principles

---

**Task Owner**: Development Team  
**Reviewer**: Senior Developer  
**Next Review**: After Phase 1 completion (Day 3)