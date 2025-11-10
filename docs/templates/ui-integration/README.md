---
title: "UI Integration Templates"
created: "2025-10-30"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - template
  - ui
  - mvvm
  - avalonia
related:
  - docs/guides/UI_INTEGRATION_WORKFLOW.md
  - docs/patterns/mvvm-patterns.md
  - docs/architecture/mvvm-patterns.md
---

# UI Integration Templates

This folder contains scaffolded templates for implementing new features following the S7Tools UI integration pattern.

## Available Templates

### 1. FeatureViewModel.template.cs
**Purpose**: Complete ViewModel with sidebar and main content integration

**Features**:
- Sidebar category management
- Main content switching based on sidebar selection
- ReactiveUI commands and properties
- Logging integration
- IDisposable implementation
- Design-time constructor

**Usage**: Replace `[FEATURE_NAME]` with your feature name (e.g., `Reports`, `Analytics`) and `[CATEGORY]` with your chosen category

### 2. FeatureSidebarView.template.axaml
**Purpose**: XAML template for the sidebar panel

**Features**:
- Category list with VSCode-style selection
- Hover and selected states
- Optional search and action buttons (commented out)
- Proper data binding with compiled bindings
- Category-aware namespace declarations

**Usage**: Replace `[FEATURE_NAME]` with your feature name and `[CATEGORY]` with your chosen category

### 3. FeatureMainView.template.axaml
**Purpose**: XAML template for the main content area

**Features**:
- Automatic ViewLocator-based content switching
- Alternative manual DataTemplate approach (commented out)
- Proper ViewModel binding
- Category-aware namespace declarations

**Usage**: Replace `[FEATURE_NAME]` with your feature name and `[CATEGORY]` with your chosen category

### 4. INTEGRATION_CHECKLIST.md
**Purpose**: Step-by-step implementation guide

**Usage**: Follow the checklist when adding a new feature to ensure all integration points are covered

## Quick Start

### Step 1: Copy Templates

```bash
# From repository root
# Choose appropriate category: Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks
CATEGORY="Pages"  # Example: for a page feature

cp docs/templates/ui-integration/FeatureViewModel.template.cs \
   src/S7Tools/ViewModels/${CATEGORY}/MyFeatureViewModel.cs

cp docs/templates/ui-integration/FeatureSidebarView.template.axaml \
   src/S7Tools/Views/${CATEGORY}/MyFeatureSidebarView.axaml

cp docs/templates/ui-integration/FeatureMainView.template.axaml \
   src/S7Tools/Views/${CATEGORY}/MyFeatureMainView.axaml
```

### Step 2: Replace Placeholders

In all files, replace:
- `[FEATURE_NAME]` → Your feature name (e.g., `Reports`, `Analytics`)
- `[CATEGORY]` → Your category name (e.g., `Pages`, `Settings`)

### Step 3: Add Code-Behind

Create code-behind files for views:

**MyFeatureSidebarView.axaml.cs**:
```csharp
using Avalonia.Controls;

namespace S7Tools.Views.Pages;  // Update category as needed

public partial class MyFeatureSidebarView : UserControl
{
    public MyFeatureSidebarView()
    {
        InitializeComponent();
    }
}
```

**MyFeatureMainView.axaml.cs**:
```csharp
using Avalonia.Controls;

namespace S7Tools.Views.Pages;  // Update category as needed

public partial class MyFeatureMainView : UserControl
{
    public MyFeatureMainView()
    {
        InitializeComponent();
    }
}
```

### Step 4: Register Activity Bar Item

**File**: `src/S7Tools/Services/ActivityBarService.cs`

```csharp
new ActivityBarItem("myfeature", "My Feature", "My Feature Description", "fa-solid fa-icon")
{
    Order = 6
}
```

### Step 5: Add Navigation Case

**File**: `src/S7Tools/ViewModels/NavigationViewModel.cs`

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
    break;
```

### Step 6: Register ViewModel in DI (if needed)

**File**: `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.TryAddTransient<MyFeatureViewModel>();
```

## Template Placeholders

All templates use the following placeholder that you must replace:

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `[FEATURE_NAME]` | Your feature name in PascalCase | `Reports`, `Analytics`, `Dashboard` |
| `[CATEGORY]` | Category folder name | `Base`, `Controls`, `Dialogs`, `Jobs`, `Layout`, `Pages`, `Profiles`, `Settings`, `Tasks` |

## Category Selection Guide

Choose the appropriate category for your feature:

| Category | Purpose | Examples | When to Use |
|----------|---------|----------|-------------|
| **Base** | Foundation classes | ViewModelBase, TabViewModel | Base classes, abstractions |
| **Controls** | Reusable UI controls | PropertyDisplayItem, SerialPortDiscovery | Reusable controls with ViewModels |
| **Dialogs** | Modal dialogs | ConfirmationDialog, InputDialog | User dialogs, prompts |
| **Jobs** | Job management | JobsManagement, JobWizard | Job-related features |
| **Layout** | Shell/navigation | MainWindow, Navigation, BottomPanel | Application shell, navigation |
| **Pages** | Main content pages | Home, Connections, LogViewer, About | Primary feature pages |
| **Profiles** | Profile management | SerialPort, Socat, PowerSupply | Profile editors |
| **Settings** | Application settings | General, Appearance, Logging | Settings pages |
| **Tasks** | Task management | TaskManager, TaskQueue | Task-related features |

**Decision Tree**:
1. Is it a reusable control? → **Controls**
2. Is it a dialog/prompt? → **Dialogs**
3. Is it part of the shell/navigation? → **Layout**
4. Is it a settings page? → **Settings**
5. Is it profile management? → **Profiles**
6. Is it job-related? → **Jobs**
7. Is it task-related? → **Tasks**
8. Is it a main feature page? → **Pages**
9. Is it a base class/abstraction? → **Base**

## Integration Patterns

### Pattern 1: Single ViewModel (Recommended)

Use one ViewModel for both sidebar and main content:

```
MyFeatureViewModel
  ├─ Categories (for sidebar)
  ├─ SelectedCategory (drives content switching)
  └─ SelectedContentViewModel (for main content)
```

**Pros**:
- Single source of truth
- Simpler state management
- Easier data sharing

### Pattern 2: Separate ViewModels

Use different ViewModels for sidebar and main content:

```
MyFeatureSidebarViewModel (for sidebar)
MyFeatureMainViewModel (for main content)
```

**Pros**:
- Better separation of concerns
- More modular

**Cons**:
- More complex state synchronization
- Additional coordination logic

## ViewLocator Naming Convention

The ViewLocator uses these rules to map ViewModels to Views:

| ViewModel Name | View Name |
|----------------|-----------|
| `MyFeatureViewModel` | `MyFeatureView` |
| `MyFeatureSidebarViewModel` | `MyFeatureSidebarView` |
| `MyFeatureDetailsViewModel` | `MyFeatureDetailsView` |

**Important**:
- Namespace must follow category structure: `S7Tools.ViewModels.{Category}` → `S7Tools.Views.{Category}`
- Category must match between ViewModels and Views folders
- Suffix must be `ViewModel` → `View`

## Data Template Override

If you need a different view for the sidebar than the main content, add a DataTemplate in `MainWindow.axaml`:

```xaml
<ContentControl Content="{Binding Navigation.CurrentContent}">
  <ContentControl.DataTemplates>
    <DataTemplate DataType="vm:MyFeatureViewModel">
      <views:MyFeatureSidebarView />
    </DataTemplate>
  </ContentControl.DataTemplates>
</ContentControl>
```

## Common Pitfalls

### 1. ViewLocator Can't Find View

**Problem**: Error shows "Not Found: S7Tools.Views.{Category}.MyFeatureView"

**Solution**:
- Verify ViewModel namespace is `S7Tools.ViewModels.{Category}`
- Verify View namespace is `S7Tools.Views.{Category}`
- Verify category folders exist and match
- Check file naming: `MyFeatureViewModel` → `MyFeatureView`

### 2. Sidebar Doesn't Update

**Problem**: Sidebar selection doesn't change main content

**Solution**:
- Use `RaiseAndSetIfChanged` in ViewModel properties
- Ensure `TwoWay` binding on `SelectedCategory`
- Verify `UpdateMainContent()` is called when selection changes

### 3. Content Shows Empty

**Problem**: Main content area is blank

**Solution**:
- Check `SelectedContentViewModel` is not null
- Verify ViewLocator can resolve the child ViewModel's View
- Ensure `ContentControl.Content` binding is correct

## Resources

- **Full Documentation**: `../../UI_INTEGRATION_WORKFLOW.md`
- **Architecture**: `../../Project_Architecture_Blueprint.md`
- **Implementation Checklist**: `./INTEGRATION_CHECKLIST.md`
- **ViewLocator Code**: `../../../src/S7Tools/ViewLocator.cs`

## Support

For questions or issues:
1. Review the full UI Integration Workflow documentation
2. Check existing features for reference (Jobs, Settings, TaskManager)
3. Consult the Integration Checklist for missed steps

## Related Documentation

- [Ui_Integration_Workflow](../../UI_INTEGRATION_WORKFLOW.md)
- [Mvvm Patterns](../../architecture/mvvm-patterns.md)
- [Integration_Checklist](INTEGRATION_CHECKLIST.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
