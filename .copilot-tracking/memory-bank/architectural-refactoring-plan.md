# S7Tools Architectural Refactoring Plan

**Priority**: CRITICAL  
**Status**: Implementation Required  
**Last Updated**: Current Session  

## Executive Summary

The S7Tools application has a solid architectural foundation but suffers from critical MVVM violations and architectural inconsistencies that must be addressed immediately. While components like `LogViewerViewModel` and `ActivityBarService` demonstrate excellent patterns, other parts of the codebase violate fundamental MVVM principles and bypass dependency injection.

## Critical Issues Identified

### 🚨 **HIGHEST PRIORITY - MVVM Violations**

#### **1. View-ViewModel Circular Dependencies**
**Location**: `DialogService.cs` and `ConfirmationDialogViewModel.cs`  
**Problem**: DialogService creates View instances and passes them to ViewModels, creating circular dependencies.

```csharp
// CURRENT (WRONG)
var dialog = new ConfirmationDialog();
dialog.DataContext = new ConfirmationDialogViewModel(dialog, title, message);
```

**Impact**: Makes code untestable, brittle, and violates MVVM separation of concerns.

#### **2. Direct View Instantiation in ViewModels**
**Location**: `MainWindowViewModel.cs` lines 580-582, 590-592  
**Problem**: ViewModels directly create View instances.

```csharp
// CURRENT (WRONG)
DetailContent = new LoggingTestView() { DataContext = this };
DetailContent = new SettingsConfigView() { DataContext = this };
```

**Impact**: Tight coupling between View and ViewModel layers, prevents proper testing.

#### **3. Bypassing Dependency Injection**
**Location**: `HomeViewModel.cs`, `ConnectionsViewModel.cs`  
**Problem**: ViewModels create other ViewModels using `new` keyword.

```csharp
// CURRENT (WRONG)
DetailContent = new AboutViewModel();
```

**Impact**: Child ViewModels don't receive their dependencies, leading to NullReferenceExceptions.

### 🔧 **HIGH PRIORITY - God Object Anti-Pattern**

#### **4. MainWindowViewModel Overloaded**
**Problem**: Single class managing layout, settings, clipboard, navigation, and logging tests.  
**Impact**: 600+ lines, difficult to test, violates Single Responsibility Principle.

### 🛡️ **MEDIUM PRIORITY - Type Safety Issues**

#### **5. Core Domain Model Type Safety**
**Location**: `S7Tools.Core/Models/Tag.cs`  
**Problem**: `Value` property is `object?`, sacrificing compile-time type safety.

```csharp
// CURRENT (UNSAFE)
public object? Value { get; set; }
```

**Impact**: Runtime casting errors, no compile-time validation.

## Refactoring Implementation Plan

### **Phase 1: Fix MVVM Violations (Week 1)**

#### **Task 1.1: Refactor Dialog System**
**Priority**: Critical  
**Estimated Effort**: 2-3 days  

**Implementation Steps**:

1. **Create Interaction-Based Dialog System**
   ```csharp
   // New approach using ReactiveUI Interactions
   public class MainWindowViewModel : ReactiveObject
   {
       public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; }
       
       public MainWindowViewModel()
       {
           ShowConfirmation = new Interaction<ConfirmationRequest, bool>();
       }
   }
   ```

2. **Refactor DialogService Interface**
   ```csharp
   public interface IDialogService
   {
       Task<bool> ShowConfirmationAsync(string title, string message);
       Task ShowErrorAsync(string title, string message);
   }
   
   // Implementation uses interactions instead of direct View creation
   ```

3. **Update ConfirmationDialogViewModel**
   ```csharp
   public class ConfirmationDialogViewModel : ViewModelBase
   {
       public string Title { get; }
       public string Message { get; }
       public ReactiveCommand<Unit, bool> OkCommand { get; }
       public ReactiveCommand<Unit, bool> CancelCommand { get; }
       
       // Remove Window dependency from constructor
       public ConfirmationDialogViewModel(string title, string message)
       {
           Title = title;
           Message = message;
           OkCommand = ReactiveCommand.Create(() => true);
           CancelCommand = ReactiveCommand.Create(() => false);
       }
   }
   ```

#### **Task 1.2: Remove Direct View Instantiation**
**Priority**: Critical  
**Estimated Effort**: 1-2 days  

**Implementation Steps**:

1. **Create ViewModel Factory Service**
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
   }
   ```

2. **Update MainWindowViewModel Navigation**
   ```csharp
   // BEFORE (WRONG)
   DetailContent = new LoggingTestView() { DataContext = this };
   
   // AFTER (CORRECT)
   DetailContent = _viewModelFactory.Create<LoggingTestViewModel>();
   ```

3. **Rely on ViewLocator for View Resolution**
   ```csharp
   // ViewLocator automatically maps ViewModels to Views
   // No direct View instantiation needed
   ```

#### **Task 1.3: Fix Dependency Injection Bypassing**
**Priority**: Critical  
**Estimated Effort**: 1 day  

**Implementation Steps**:

1. **Register All ViewModels in DI Container**
   ```csharp
   // ServiceCollectionExtensions.cs
   public static IServiceCollection AddViewModels(this IServiceCollection services)
   {
       services.AddTransient<HomeViewModel>();
       services.AddTransient<ConnectionsViewModel>();
       services.AddTransient<AboutViewModel>();
       services.AddTransient<SettingsViewModel>();
       services.AddTransient<LogViewerViewModel>();
       return services;
   }
   ```

2. **Update ViewModels to Use Factory**
   ```csharp
   public class HomeViewModel : ViewModelBase
   {
       private readonly IViewModelFactory _viewModelFactory;
       
       public HomeViewModel(IViewModelFactory viewModelFactory)
       {
           _viewModelFactory = viewModelFactory;
           DetailContent = _viewModelFactory.Create<AboutViewModel>();
       }
   }
   ```

### **Phase 2: Decompose MainWindowViewModel (Week 2)**

#### **Task 2.1: Create Specialized ViewModels**
**Priority**: High  
**Estimated Effort**: 3-4 days  

**Implementation Steps**:

1. **Create BottomPanelViewModel**
   ```csharp
   public class BottomPanelViewModel : ReactiveObject
   {
       public ObservableCollection<PanelTabItem> Tabs { get; }
       public PanelTabItem? SelectedTab { get; set; }
       public GridLength PanelHeight { get; set; }
       public bool IsExpanded => PanelHeight.Value > 35;
       
       public ReactiveCommand<Unit, Unit> TogglePanelCommand { get; }
       public ReactiveCommand<PanelTabItem, Unit> SelectTabCommand { get; }
   }
   ```

2. **Create NavigationViewModel**
   ```csharp
   public class NavigationViewModel : ReactiveObject
   {
       private readonly IActivityBarService _activityBarService;
       private readonly IViewModelFactory _viewModelFactory;
       
       public object? CurrentContent { get; set; }
       public object? DetailContent { get; set; }
       public string SidebarTitle { get; set; }
       public bool IsSidebarVisible { get; set; }
   }
   ```

3. **Create SettingsManagementViewModel**
   ```csharp
   public class SettingsManagementViewModel : ReactiveObject
   {
       // Move all settings-related properties and commands here
       public string DefaultLogPath { get; set; }
       public string ExportPath { get; set; }
       public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
       public ReactiveCommand<Unit, Unit> LoadSettingsCommand { get; }
   }
   ```

4. **Refactor MainWindowViewModel**
   ```csharp
   public class MainWindowViewModel : ReactiveObject
   {
       public NavigationViewModel Navigation { get; }
       public BottomPanelViewModel BottomPanel { get; }
       public SettingsManagementViewModel Settings { get; }
       
       public MainWindowViewModel(
           NavigationViewModel navigation,
           BottomPanelViewModel bottomPanel,
           SettingsManagementViewModel settings)
       {
           Navigation = navigation;
           BottomPanel = bottomPanel;
           Settings = settings;
       }
   }
   ```

### **Phase 3: Improve Core Domain Model (Week 3)**

#### **Task 3.1: Make Tag Type-Safe**
**Priority**: Medium  
**Estimated Effort**: 2-3 days  

**Implementation Steps**:

1. **Create Generic Tag Class**
   ```csharp
   public class Tag<T> where T : notnull
   {
       public string Name { get; init; } = string.Empty;
       public string Address { get; init; } = string.Empty;
       public T Value { get; init; } = default!;
       public DateTime Timestamp { get; init; } = DateTime.UtcNow;
       public TagQuality Quality { get; init; } = TagQuality.Good;
   }
   
   public enum TagQuality
   {
       Good,
       Bad,
       Uncertain
   }
   ```

2. **Create Strongly-Typed Tag Variants**
   ```csharp
   public sealed class BoolTag : Tag<bool> { }
   public sealed class IntTag : Tag<int> { }
   public sealed class FloatTag : Tag<float> { }
   public sealed class StringTag : Tag<string> { }
   ```

3. **Update ITagRepository Interface**
   ```csharp
   public interface ITagRepository
   {
       Task<Tag<T>?> ReadTagAsync<T>(string address) where T : notnull;
       Task<bool> WriteTagAsync<T>(string address, T value) where T : notnull;
       Task<IReadOnlyList<Tag<T>>> ReadTagsAsync<T>(IEnumerable<string> addresses) where T : notnull;
   }
   ```

#### **Task 3.2: Make Models Immutable**
**Priority**: Medium  
**Estimated Effort**: 1 day  

**Implementation Steps**:

1. **Convert to Records or Init-Only Properties**
   ```csharp
   public record Tag<T>(
       string Name,
       string Address,
       T Value,
       DateTime Timestamp,
       TagQuality Quality = TagQuality.Good
   ) where T : notnull;
   ```

### **Phase 4: Implement Comprehensive Testing (Week 4)**

#### **Task 4.1: Set Up Testing Infrastructure**
**Priority**: High  
**Estimated Effort**: 2-3 days  

**Implementation Steps**:

1. **Create Test Projects**
   ```
   tests/
   ├── S7Tools.Tests/
   ├── S7Tools.Core.Tests/
   ├── S7Tools.Infrastructure.Logging.Tests/
   └── S7Tools.Integration.Tests/
   ```

2. **Add Testing NuGet Packages**
   ```xml
   <PackageReference Include="xunit" Version="2.4.2" />
   <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
   <PackageReference Include="Moq" Version="4.20.69" />
   <PackageReference Include="FluentAssertions" Version="6.12.0" />
   <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
   ```

3. **Create Base Test Classes**
   ```csharp
   public abstract class ViewModelTestBase<T> where T : ViewModelBase
   {
       protected Mock<IServiceProvider> ServiceProviderMock { get; }
       protected Mock<IViewModelFactory> ViewModelFactoryMock { get; }
       
       protected ViewModelTestBase()
       {
           ServiceProviderMock = new Mock<IServiceProvider>();
           ViewModelFactoryMock = new Mock<IViewModelFactory>();
       }
   }
   ```

#### **Task 4.2: Write Unit Tests**
**Priority**: High  
**Estimated Effort**: 3-4 days  

**Implementation Steps**:

1. **Test ViewModels**
   ```csharp
   public class NavigationViewModelTests : ViewModelTestBase<NavigationViewModel>
   {
       [Fact]
       public void SelectActivityBarItem_ShouldUpdateCurrentContent()
       {
           // Arrange
           var activityBarService = new Mock<IActivityBarService>();
           var viewModel = new NavigationViewModel(activityBarService.Object, ViewModelFactoryMock.Object);
           
           // Act
           viewModel.SelectActivityBarItemCommand.Execute("explorer");
           
           // Assert
           viewModel.CurrentContent.Should().NotBeNull();
       }
   }
   ```

2. **Test Services**
   ```csharp
   public class ActivityBarServiceTests
   {
       [Fact]
       public void SelectItem_ShouldRaiseSelectionChangedEvent()
       {
           // Arrange
           var service = new ActivityBarService();
           var eventRaised = false;
           service.SelectionChanged += (s, e) => eventRaised = true;
           
           // Act
           service.SelectItem("explorer");
           
           // Assert
           eventRaised.Should().BeTrue();
       }
   }
   ```

## Implementation Timeline

### **✅ Week 1: Critical MVVM Fixes - COMPLETED**
- [x] Day 1-2: Refactor Dialog System (Task 1.1) ✅ COMPLETED
- [x] Day 3-4: Remove Direct View Instantiation (Task 1.2) ✅ COMPLETED
- [x] Day 5: Fix DI Bypassing (Task 1.3) ✅ COMPLETED

### **🔄 Week 2: Decompose MainWindowViewModel - READY TO START**
- [ ] Day 1-2: Create BottomPanelViewModel (Task 2.1a)
- [ ] Day 3-4: Create NavigationViewModel (Task 2.1b)
- [ ] Day 5: Create SettingsManagementViewModel (Task 2.1c)

### **📋 Week 3: Core Domain Improvements - PLANNED**
- [ ] Day 1-3: Implement Generic Tag System (Task 3.1)
- [ ] Day 4: Make Models Immutable (Task 3.2)
- [ ] Day 5: Update Repository Interfaces

### **📋 Week 4: Testing Infrastructure - PLANNED**
- [ ] Day 1-2: Set Up Test Projects (Task 4.1)
- [ ] Day 3-5: Write Comprehensive Unit Tests (Task 4.2)

## Success Criteria

### **Phase 1 Success Metrics**
- [ ] Zero direct View instantiation in ViewModels
- [ ] All ViewModels created through DI container
- [ ] Dialog system uses interaction pattern
- [ ] No circular dependencies between View and ViewModel

### **Phase 2 Success Metrics**
- [ ] MainWindowViewModel under 200 lines
- [ ] Each ViewModel has single responsibility
- [ ] Clear separation of concerns maintained

### **Phase 3 Success Metrics**
- [ ] Compile-time type safety for all Tag operations
- [ ] Immutable domain models
- [ ] No runtime casting required

### **Phase 4 Success Metrics**
- [ ] >80% code coverage
- [ ] All critical paths tested
- [ ] Automated test execution in CI/CD

## Risk Mitigation

### **High Risk: Breaking Changes**
**Mitigation**: Implement changes incrementally, maintain backward compatibility where possible.

### **Medium Risk: Testing Complexity**
**Mitigation**: Start with simple unit tests, gradually add integration tests.

### **Low Risk: Performance Impact**
**Mitigation**: Profile before and after changes, optimize if needed.

## Quality Gates

### **Before Each Phase**
- [ ] All existing tests pass
- [ ] Code compiles without warnings
- [ ] No new static analysis violations

### **After Each Phase**
- [ ] New functionality tested
- [ ] Documentation updated
- [ ] Code review completed
- [ ] Performance benchmarks maintained

---

**Document Owner**: Development Team  
**Review Frequency**: Weekly during implementation  
**Next Review**: After Phase 1 completion