---
title: "UI Integration Workflow"
version: "1.1.1"
created: "2025-01-15"
last-updated: "2025-11-22"
status: "current"
tags: ["guide", "ui", "integration", "workflow", "avalonia"]
related:
  - docs/architecture/mvvm-patterns.md
  - docs/patterns/reusable-controls.md
  - docs/templates/ui-integration/README.md
---

# UI Integration Workflow

**Last Updated**: 2025-11-22
**Version**: 1.1.1

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
│  │  v   │    P     │             C                  │   │
│  │  i   │    a     │             o                  │   │
│  │  t   │    n     │             n                  │   │
│  │  y   │    e     │             t                  │   │
│  │      │    l     │             e                  │   │
│  │  B   │          │             n                  │   │
│  │  a   │          │             t                  │   │
│  │  r   │          │                                │   │
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

## Reusable UI Controls

S7Tools provides reusable controls in the `Controls` category for common UI patterns:

### SerialPortDiscoveryControl

**Location**: `Views/Controls/SerialPortDiscoveryControl.axaml`
**ViewModel**: `ViewModels/Controls/SerialPortDiscoveryViewModel.cs`

A self-contained control for serial port discovery and selection.

**Features**:
- Automatic port scanning
- Real-time port availability updates
- Port details display (name, description, manufacturer)
- Refresh capability
- Status indicators

**Usage Example**:
```xaml
<UserControl xmlns:controls="using:S7Tools.Views.Controls">
  <controls:SerialPortDiscoveryControl />
</UserControl>
```

**ViewModel Integration**:
```csharp
// In your feature ViewModel
using S7Tools.ViewModels.Controls;

public class MyFeatureViewModel : ViewModelBase
{
    private SerialPortDiscoveryViewModel _portDiscovery;

    public SerialPortDiscoveryViewModel PortDiscovery
    {
        get => _portDiscovery;
        set => this.RaiseAndSetIfChanged(ref _portDiscovery, value);
    }

    // Prefer constructor injection for better testability and DI best practices
    public MyFeatureViewModel(SerialPortDiscoveryViewModel portDiscovery)
    {
        PortDiscovery = portDiscovery ?? throw new ArgumentNullException(nameof(portDiscovery));
    }
}
```

### SidebarSection

**Location**: `Views/Controls/SidebarSection.axaml`

A collapsible section control for organizing sidebar content.

**Features**:
- Expandable/collapsible sections
- Icon support
- Header customization
- Consistent styling with VSCode theme

**Usage Example**:
```xaml
<UserControl xmlns:controls="using:S7Tools.Views.Controls">
  <StackPanel>
    <controls:SidebarSection Header="Settings"
                             Icon="fa-solid fa-cog"
                             IsExpanded="True">
      <!-- Section content here -->
      <StackPanel>
        <TextBlock Text="Option 1" />
        <TextBlock Text="Option 2" />
      </StackPanel>
    </controls:SidebarSection>

    <controls:SidebarSection Header="Advanced"
                             Icon="fa-solid fa-sliders"
                             IsExpanded="False">
      <!-- Advanced options -->
    </controls:SidebarSection>
  </StackPanel>
</UserControl>
```

### Sidebar Views Pattern

**Pattern**: Feature-specific sidebar views in the feature's category

**Examples**:
- `Views/Jobs/JobsSidebarView.axaml` - Jobs feature sidebar
- `Views/Tasks/TaskManagerSidebarView.axaml` - Task manager sidebar

**Usage**: These views provide feature-specific navigation and filtering within the sidebar panel while the main content area displays detailed information.

## Code Templates

See the `docs/templates/ui-integration/` folder for complete scaffolded templates:

- **Feature ViewModel Template**: Complete ViewModel with sidebar integration (with category support)
- **Sidebar View Template**: XAML template for sidebar categories (with category namespaces)
- **Main Content View Template**: XAML template for main content area (with category namespaces)
- **Right Panel View Template**: XAML template for optional right panel
- **Service Template**: Background service template
- **Integration Checklist**: Step-by-step implementation guide (with category selection)

**Template Placeholders**:
- `[FEATURE_NAME]` - Replace with your feature name (e.g., Reports, Analytics)
- `[CATEGORY]` - Replace with chosen category (Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks)

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

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-24 | Initial documentation with ViewLocator pattern |
| 1.1 | 2025-11-22 | Updates for VSCode-style UI architecture |

## Related Documentation

- [Mvvm Patterns](../architecture/mvvm-patterns.md)
- [Reusable Controls](../patterns/reusable-controls.md)
- [Readme](../templates/ui-integration/README.md)
