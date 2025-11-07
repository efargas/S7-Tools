# UI Integration Workflow

**Last Updated**: 2025-11-07
**Version**: 1.1

This document explains the S7Tools UI integration pattern: how components connect from the Activity Bar through Side Panels to Main Content Views.

## Table of Contents

1. [Overview](#overview)
2. [Architecture Pattern](#architecture-pattern)
3. [Component Flow](#component-flow)
4. [ViewLocator Pattern](#viewlocator-pattern)
5. [Implementation Guide](#implementation-guide)
6. [Code Templates](#code-templates)

## Overview

S7Tools uses a VSCode-style UI architecture with three main areas:

```
┌─────────────────────────────────────────────────────────┐
│  ┌──────┬──────────┬────────────────────────────────┐   │
│  │      │          │                                │   │
│  │  A   │    S     │             M                  │   │
│  │  c   │    i     │             a                  │   │
│  │  t   │    d     │             i                  │   │
│  │  i   │    e     │             n                  │   │
│  │  v   │          │                                │   │
│  │  i   │    P     │             C                  │   │
│  │  t   │    a     │             o                  │   │
│  │  y   │    n     │             n                  │   │
│  │      │    e     │             t                  │   │
│  │  B   │    l     │             e                  │   │
│  │  a   │          │             n                  │   │
│  │  r   │          │             t                  │   │
│  │      │          │                                │   │
│  └──────┴──────────┴────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### Key Components

1. **Activity Bar** (Left, 48px): Icon-based navigation
2. **Side Panel** (240-300px, collapsible): Category navigation or filters
3. **Main Content** (Flexible): Primary feature interface
4. **Right Panel** (Optional): Contextual details or properties

## Architecture Pattern

### Data Flow Diagram

```mermaid
graph TB
    A[Activity Bar Item Click] --> B[NavigationViewModel]
    B --> C[ActivityBarService.SelectItem]
    C --> D[OnActivityBarSelectionChanged Event]
    D --> E[NavigateToActivityBarItemContent]
    E --> F{Switch by ItemId}
    F --> G[Create Feature ViewModel]
    G --> H[Set CurrentContent for Sidebar]
    G --> I[Set MainContent for Main Area]
    H --> J[ViewLocator Maps to Sidebar View]
    I --> K[ViewLocator Maps to Main Content View]
    J --> L[DataTemplate Binding]
    K --> L
    L --> M[UI Rendered]
```

### ViewModel Hierarchy

```
NavigationViewModel (MainWindow)
  ├─ CurrentContent → Sidebar View
  │   └─ Via ViewLocator: FeatureViewModel → FeatureSidebarView
  │
  └─ MainContent → Main Area View
      └─ Via ViewLocator: FeatureViewModel → FeatureMainView
```

## Component Flow

### 1. Activity Bar Selection

**Location**: `MainWindow.axaml` Grid.Column="0"

```xaml
<ItemsControl ItemsSource="{Binding Navigation.ActivityBarItems}">
  <ItemsControl.ItemTemplate>
    <DataTemplate>
      <Button Command="{Binding $parent[Window].DataContext.Navigation.SelectActivityBarItemCommand}"
              CommandParameter="{Binding Id}">
        <icons:Icon Value="{Binding IconPath}" />
      </Button>
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

**Handler**: `NavigationViewModel.SelectActivityBarItem(string itemId)`

### 2. Navigation Logic

**Location**: `NavigationViewModel.cs`

```csharp
private void NavigateToActivityBarItemContent(string itemId)
{
    switch (itemId)
    {
        case "myfeature":
            SidebarTitle = "My Feature";
            MainContentTitle = "My Feature Management";

            // Create ViewModel
            MyFeatureViewModel? viewModel = CreateViewModel<MyFeatureViewModel>();

            // Set for sidebar and main content
            CurrentContent = viewModel;  // Sidebar
            MainContent = viewModel;      // Main area
            break;
    }
}
```

### 3. ViewLocator Resolution

**Location**: `ViewLocator.cs`

The ViewLocator automatically maps ViewModels to Views with category support:

```
MyFeatureViewModel (in Pages category) → MyFeatureView (in Pages category)
MyFeatureSidebarViewModel (in Layout category) → MyFeatureSidebarView (in Layout category)
```

**Naming Convention**:
- Remove "ViewModel" suffix
- Replace with "View" suffix
- Match namespace: `ViewModels.{Category}` → `Views.{Category}`
- Category preserved during transformation

### 4. Data Template Binding

**Location**: `MainWindow.axaml`

```xaml
<!-- Sidebar Content -->
<ContentControl Grid.Row="1" Content="{Binding Navigation.CurrentContent}">
  <ContentControl.DataTemplates>
    <!-- Specific template for sidebar -->
    <DataTemplate DataType="vm:MyFeatureViewModel">
      <views:MyFeatureSidebarView />
    </DataTemplate>
  </ContentControl.DataTemplates>
</ContentControl>

<!-- Main Content -->
<ContentControl Content="{Binding Navigation.MainContent}">
  <!-- ViewLocator handles default mapping -->
</ContentControl>
```

## ViewLocator Pattern

### How It Works

1. **Type Resolution**: ViewLocator receives a ViewModel instance
2. **Name Transformation**:
   - Full type name: `S7Tools.ViewModels.Pages.MyFeatureViewModel`
   - Replace namespace: `S7Tools.Views.Pages.MyFeatureView`
   - Category (`Pages`) is preserved in the transformation
   - Replace suffix: `ViewModel` → `View`
3. **Type Loading**: Uses reflection to find the View type
4. **Instance Creation**: Creates View instance via `Activator.CreateInstance`
5. **Binding**: Avalonia automatically sets DataContext to the ViewModel

### Cache Optimization

ViewLocator uses `ConcurrentDictionary` to cache type mappings:

```csharp
private static readonly ConcurrentDictionary<Type, Type?> ViewTypeCache = new();

public Control? Build(object? param)
{
    Type vmType = param.GetType();
    Type? type = ViewTypeCache.GetOrAdd(vmType, ResolveViewType);

    if (type != null)
    {
        return (Control)Activator.CreateInstance(type)!;
    }

    return new TextBlock { Text = "Not Found: " + viewName };
}
```

## Implementation Guide

### Step-by-Step: Adding a New Feature

#### Step 1: Register Activity Bar Item

**File**: `Services/ActivityBarService.cs`

```csharp
public IEnumerable<ActivityBarItem> GetDefaultItems()
{
    return
    [
        // ... existing items ...
        new ActivityBarItem("myfeature", "My Feature", "My Feature Description", "fa-solid fa-icon")
        {
            Order = 6
        }
    ];
}
```

#### Step 2: Create ViewModels

**File**: `ViewModels/MyFeatureViewModel.cs`

```csharp
namespace S7Tools.ViewModels;

public class MyFeatureViewModel : ViewModelBase
{
    // Sidebar categories
    public ObservableCollection<string> Categories { get; } = new();
    private string? _selectedCategory;

    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            UpdateMainContent();
        }
    }

    // Main content ViewModel
    private object? _selectedContentViewModel;
    public object? SelectedContentViewModel
    {
        get => _selectedContentViewModel;
        set => this.RaiseAndSetIfChanged(ref _selectedContentViewModel, value);
    }

    private void UpdateMainContent()
    {
        // Switch main content based on sidebar selection
        SelectedContentViewModel = _selectedCategory switch
        {
            "Overview" => new MyFeatureOverviewViewModel(),
            "Details" => new MyFeatureDetailsViewModel(),
            _ => null
        };
    }
}
```

#### Step 3: Create Views

**Sidebar View**: `Views/MyFeatureSidebarView.axaml`

```xaml
#### Step 3: Create Views

**Sidebar View**: `Views/{Category}/MyFeatureSidebarView.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:S7Tools.ViewModels.{Category}"
             x:Class="S7Tools.Views.{Category}.MyFeatureSidebarView"
```

**Main Content View**: `Views/MyFeatureMainView.axaml`

```xaml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:S7Tools.ViewModels"
             x:DataType="vm:MyFeatureViewModel">

  <!-- Content switches based on sidebar selection -->
  <ContentControl Content="{Binding SelectedContentViewModel}" />
</UserControl>
```

#### Step 4: Register Navigation

**File**: `ViewModels/Layout/NavigationViewModel.cs`

```csharp
private void NavigateToActivityBarItemContent(string itemId)
{
    switch (itemId)
    {
        // ... existing cases ...

        case "myfeature":
            SidebarTitle = "My Feature";
            MainContentTitle = "My Feature Management";
            ShowMainContentHeader = true;

            MyFeatureViewModel? featureViewModel = CreateViewModel<MyFeatureViewModel>();
            CurrentContent = featureViewModel;  // Sidebar
            MainContent = featureViewModel;      // Main content
            DetailContent = featureViewModel;
            ShowLogStats = false;
            _logger.LogDebug("Navigated to My Feature");
            break;
    }
}
```

#### Step 5: Add Data Template (Optional)

**File**: `Views/Base/MainWindow.axaml`

If you need a specific sidebar template different from the main view:

```xaml
<ContentControl Grid.Row="1" Content="{Binding Navigation.CurrentContent}">
  <ContentControl.DataTemplates>
    <DataTemplate DataType="vm:MyFeatureViewModel">
      <views:MyFeatureSidebarView />
    </DataTemplate>
  </ContentControl.DataTemplates>
</ContentControl>
```

### Right Panel Pattern (Optional)

For features requiring a right panel (e.g., properties, details):

```csharp
public class MyFeatureViewModel : ViewModelBase
{
    // Right panel content
    private object? _rightPanelContent;
    public object? RightPanelContent
    {
        get => _rightPanelContent;
        set => this.RaiseAndSetIfChanged(ref _rightPanelContent, value);
    }

    public void ShowDetails(MyItem item)
    {
        RightPanelContent = new MyItemDetailsViewModel(item);
    }
}
```

## Code Templates

See the `docs/templates/ui-integration/` folder for complete scaffolded templates:

- **Feature ViewModel Template**: Complete ViewModel with sidebar integration
- **Sidebar View Template**: XAML template for sidebar categories
- **Main Content View Template**: XAML template for main content area
- **Right Panel View Template**: XAML template for optional right panel
- **Service Template**: Background service template
- **Integration Checklist**: Step-by-step implementation guide

## Best Practices

### 1. ViewModel Design

- **Single ViewModel**: Use one ViewModel for both sidebar and main content
- **Content Switching**: Use `SelectedContentViewModel` property for dynamic content
- **State Management**: Keep state in the parent ViewModel, not child content VMs

### 2. View Naming

- **Consistency**: Follow `{Feature}SidebarView` and `{Feature}MainView` patterns
- **Namespace**: Keep ViewModels and Views in matching namespace structures
- **DataType**: Always specify `x:DataType` for compiled bindings

### 3. Performance

- **ViewLocator Cache**: Automatic caching prevents repeated reflection
- **Lazy Loading**: Create child ViewModels only when needed
- **Disposal**: Implement `IDisposable` for ViewModels with subscriptions

### 4. Navigation

- **Activity Bar IDs**: Use lowercase, hyphenated strings (`"my-feature"`)
- **Sidebar Title**: Use proper case strings from `UIStrings`
- **Content Null Checks**: Always check for null before setting content

## Troubleshooting

### View Not Found

**Error**: `Not Found: S7Tools.Views.MyFeatureView`

**Solution**:
- Check ViewModel namespace matches `S7Tools.ViewModels.{Category}` (e.g., `S7Tools.ViewModels.Pages`)
- Check View namespace matches `S7Tools.Views.{Category}` (e.g., `S7Tools.Views.Pages`)
- Verify category folders exist and match between ViewModels and Views
- Verify View class name: `MyFeatureViewModel` → `MyFeatureView`

### Sidebar Not Updating

**Problem**: Sidebar doesn't reflect selection changes

**Solution**:
- Ensure `RaiseAndSetIfChanged` is called on property changes
- Verify `TwoWay` binding mode on `SelectedItem`
- Check `ObservableCollection` is used for dynamic lists

### Main Content Empty

**Problem**: Main content area is blank

**Solution**:
- Verify `MainContent` is set in `NavigateToActivityBarItemContent`
- Check ViewLocator can resolve the ViewModel → View mapping
- Ensure `ContentControl.Content` binding is correct

## Related Documentation

- **Architecture Blueprint**: `docs/Project_Architecture_Blueprint.md`
- **Folder Structure**: `docs/Project_Folders_Structure_Blueprint.md`
- **Code Templates**: `docs/templates/ui-integration/`
- **ViewLocator Implementation**: `src/S7Tools/ViewLocator.cs`
- **Navigation ViewModel**: `src/S7Tools/ViewModels/NavigationViewModel.cs`

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-24 | Initial documentation with ViewLocator pattern |
