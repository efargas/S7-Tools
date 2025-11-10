---
title: "UI Integration Implementation Checklist"
created: "2025-10-30"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - checklist
  - template
  - ui
  - workflow
related:
  - docs/guides/UI_INTEGRATION_WORKFLOW.md
  - docs/templates/ui-integration/README.md
  - docs/patterns/mvvm-patterns.md
---

# UI Integration Implementation Checklist

Use this checklist when adding a new feature to S7Tools following the UI integration pattern.

## Pre-Implementation

- [ ] **Read Documentation**: Review `docs/UI_INTEGRATION_WORKFLOW.md`
- [ ] **Choose Feature Name**: Decide on PascalCase name (e.g., `Reports`, `Analytics`)
- [ ] **Choose Category**: Select appropriate category (Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks)
- [ ] **Choose Icon**: Select Font Awesome icon from [fontawesome.com](https://fontawesome.com)
- [ ] **Define Categories**: List sidebar categories/sections for your feature

## Step 1: Create ViewModels

### Main ViewModel

- [ ] Copy `FeatureViewModel.template.cs` to `src/S7Tools/ViewModels/{Category}/[Feature]ViewModel.cs`
- [ ] Replace `[FEATURE_NAME]` placeholder
- [ ] Replace `[CATEGORY]` placeholder with chosen category
- [ ] Update namespace to `S7Tools.ViewModels.{Category}`
- [ ] Update categories in `InitializeCategories()`
- [ ] Implement content switching logic in `UpdateMainContent()`
- [ ] Add feature-specific properties and commands
- [ ] Add DI dependencies to constructor if needed

### Child ViewModels (if needed)

- [ ] Create overview ViewModel: `[Feature]OverviewViewModel.cs`
- [ ] Create details ViewModel: `[Feature]DetailsViewModel.cs`
- [ ] Create settings ViewModel: `[Feature]SettingsViewModel.cs`

**Location**: `src/S7Tools/ViewModels/{Category}/`

## Step 2: Create Views

### Sidebar View

- [ ] Copy `FeatureSidebarView.template.axaml` to `src/S7Tools/Views/{Category}/[Feature]SidebarView.axaml`
- [ ] Replace `[FEATURE_NAME]` placeholder
- [ ] Replace `[CATEGORY]` placeholder in xmlns and x:Class
- [ ] Update namespace to `S7Tools.Views.{Category}`
- [ ] Create code-behind file: `[Feature]SidebarView.axaml.cs`
- [ ] Customize sidebar UI (search, filters, actions)

### Main View

- [ ] Copy `FeatureMainView.template.axaml` to `src/S7Tools/Views/{Category}/[Feature]MainView.axaml`
- [ ] Replace `[FEATURE_NAME]` placeholder
- [ ] Replace `[CATEGORY]` placeholder in xmlns and x:Class
- [ ] Update namespace to `S7Tools.Views.{Category}`
- [ ] Create code-behind file: `[Feature]MainView.axaml.cs`
- [ ] Choose ViewLocator or DataTemplate approach

### Child Views (if needed)

- [ ] Create overview view: `[Feature]OverviewView.axaml`
- [ ] Create details view: `[Feature]DetailsView.axaml`
- [ ] Create settings view: `[Feature]SettingsView.axaml`

**Location**: `src/S7Tools/Views/{Category}/`

### Code-Behind Template

```csharp
using Avalonia.Controls;

namespace S7Tools.Views.{Category};  // Update {Category} as needed

public partial class [Feature]SidebarView : UserControl
{
    public [Feature]SidebarView()
    {
        InitializeComponent();
    }
}
```

## Step 3: Register Activity Bar Item

- [ ] Open `src/S7Tools/Services/ActivityBarService.cs`
- [ ] Add new item in `GetDefaultItems()` method
- [ ] Set appropriate Order value
- [ ] Choose unique lowercase ID (e.g., `"myfeature"`)

```csharp
new ActivityBarItem("myfeature", "My Feature", "My Feature Description", "fa-solid fa-icon")
{
    Order = 6  // Adjust order as needed
}
```

## Step 4: Add Navigation Handler

- [ ] Open `src/S7Tools/ViewModels/NavigationViewModel.cs`
- [ ] Add new case in `NavigateToActivityBarItemContent()` switch statement
- [ ] Set sidebar title, main content title
- [ ] Create ViewModel instance
- [ ] Assign to CurrentContent, MainContent, DetailContent

```csharp
case "myfeature":
    SidebarTitle = "My Feature";
    MainContentTitle = "My Feature Management";
    ShowMainContentHeader = true;

    MyFeatureViewModel? viewModel = CreateViewModel<MyFeatureViewModel>();
    CurrentContent = viewModel;
    MainContent = viewModel;
    DetailContent = viewModel;
    ShowLogStats = false;
    _logger.LogDebug("Navigated to My Feature");
    break;
```

## Step 5: Register Services (if needed)

### ViewModel Registration

- [ ] Open `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`
- [ ] Register ViewModel as transient or singleton

```csharp
services.TryAddTransient<MyFeatureViewModel>();
```

### Custom Services

- [ ] Create service interface in `src/S7Tools.Core/Services/Interfaces/`
- [ ] Implement service in `src/S7Tools/Services/` or `src/S7Tools.Infrastructure.*/`
- [ ] Register in `ServiceCollectionExtensions.cs`

```csharp
services.TryAddSingleton<IMyFeatureService, MyFeatureService>();
```

## Step 6: Add Data Template (Optional)

Only if you need different views for sidebar vs main content:

- [ ] Open `src/S7Tools/Views/MainWindow.axaml`
- [ ] Find `<ContentControl Content="{Binding Navigation.CurrentContent}">`
- [ ] Add DataTemplate in `ContentControl.DataTemplates`

```xaml
<DataTemplate DataType="vm:MyFeatureViewModel">
  <views:MyFeatureSidebarView />
</DataTemplate>
```

## Step 7: Add Keyboard Shortcut (Optional)

- [ ] Open `src/S7Tools/Views/MainWindow.axaml`
- [ ] Find `<Window.KeyBindings>` section
- [ ] Add new KeyBinding

```xaml
<KeyBinding Gesture="Ctrl+Shift+M"
            Command="{Binding Navigation.NavigateToActivityBarItemCommand}"
            CommandParameter="myfeature" />
```

## Step 8: Add Resource Strings

- [ ] Open `src/S7Tools/Resources/UIStrings.resx`
- [ ] Add string entries for titles, tooltips, messages
- [ ] Use format: `[Feature]_[Property]`

Examples:
- `Navigation_MyFeature` = "My Feature"
- `MyFeature_OverviewTitle` = "Overview"
- `MyFeature_CreateSuccess` = "Item created successfully"

## Step 9: Testing

### Build

- [ ] Run `dotnet build src/S7Tools.sln`
- [ ] Verify no compilation errors
- [ ] Check for warnings

### Functional Testing

- [ ] Run application: `dotnet run --project src/S7Tools`
- [ ] Click activity bar icon
- [ ] Verify sidebar appears with categories
- [ ] Click each sidebar category
- [ ] Verify main content switches correctly
- [ ] Test keyboard shortcut (if added)
- [ ] Test sidebar collapse/expand
- [ ] Verify status messages display

### ViewLocator Testing

- [ ] Check console for "Not Found" errors
- [ ] Verify all views load correctly
- [ ] Test with design-time previews in IDE

## Step 10: Documentation

- [ ] Add feature to `README.md` if public-facing
- [ ] Update `docs/Project_Folders_Structure_Blueprint.md` if adding new folders
- [ ] Add inline XML documentation comments
- [ ] Update CHANGELOG.md with new feature

## Common Issues Checklist

### View Not Found

- [ ] ViewModel namespace is `S7Tools.ViewModels`
- [ ] View namespace is `S7Tools.Views`
- [ ] Class names follow naming convention
- [ ] Files are included in `.csproj`

### Sidebar Not Working

- [ ] `RaiseAndSetIfChanged` used for all properties
- [ ] `TwoWay` binding on `SelectedCategory`
- [ ] `ObservableCollection` used for Categories
- [ ] `UpdateMainContent()` called on selection change

### Navigation Not Working

- [ ] Activity bar item ID matches navigation case
- [ ] ViewModel created via `CreateViewModel<T>()`
- [ ] Both `CurrentContent` and `MainContent` set
- [ ] Logger calls added for debugging

### Dependency Injection Issues

- [ ] Service interfaces defined in Core
- [ ] Services registered in `ServiceCollectionExtensions.cs`
- [ ] Constructor parameters match registered types
- [ ] Lifetime (Transient/Scoped/Singleton) appropriate

## Quality Checks

### Code Quality

- [ ] Follows MVVM pattern
- [ ] Uses ReactiveUI properly
- [ ] Implements IDisposable if needed
- [ ] No memory leaks (subscriptions disposed)
- [ ] Logging added at appropriate levels
- [ ] Exception handling implemented

### UI/UX Quality

- [ ] Matches S7Tools design language
- [ ] VSCode-style visual consistency
- [ ] Responsive layout
- [ ] Keyboard navigation works
- [ ] Accessibility considered

### Performance

- [ ] ViewLocator cache utilized
- [ ] Lazy loading implemented
- [ ] No unnecessary ViewModel recreation
- [ ] Async operations use ConfigureAwait(false)

## Final Review

- [ ] All checklist items completed
- [ ] Code formatted (`dotnet format`)
- [ ] No compiler warnings
- [ ] Feature fully functional
- [ ] Documentation updated
- [ ] Ready for commit

## Notes

Use this space for implementation-specific notes:

```
Feature Name: _______________
Activity Bar ID: _______________
Keyboard Shortcut: _______________
Service Dependencies: _______________
Special Considerations: _______________
```

## Related Documentation

- [Readme](README.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
