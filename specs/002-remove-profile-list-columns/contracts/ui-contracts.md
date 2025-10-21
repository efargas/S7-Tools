# UI Contracts: Profile List Viewer Changes

**Date**: 2025-10-21

## Overview

This feature modifies UI presentation contracts only. No API or service contracts are affected.

## Affected UI Contracts

### DataGrid Column Contract Changes

**Before (Current State)**:
```xml
<DataGrid.Columns>
  <DataGridTextColumn Binding="{Binding Id}" ... />
  <DataGridTextColumn Binding="{Binding Name}" ... />
  <DataGridTextColumn Binding="{Binding Description}" ... />
  <DataGridTextColumn Binding="{Binding Options}" ... />      <!-- REMOVED -->
  <DataGridTextColumn Binding="{Binding Flags}" ... />        <!-- REMOVED -->
  <DataGridTextColumn Binding="{Binding CreatedAt}" ... />
  <!-- ... other columns ... -->
</DataGrid.Columns>
```

**After (Target State)**:
```xml
<DataGrid.Columns>
  <DataGridTextColumn Binding="{Binding Id}" ... />
  <DataGridTextColumn Binding="{Binding Name}" ... />
  <DataGridTextColumn Binding="{Binding Description}" ... />
  <DataGridTextColumn Binding="{Binding CreatedAt}" ... />
  <!-- ... other columns (Options and Flags columns removed) ... -->
</DataGrid.Columns>
```

## Preserved Contracts

### ViewModel Data Binding Contracts
All ViewModel properties remain unchanged to preserve Edit dialog functionality:

```csharp
// These contracts MUST be preserved
public string Options { get; set; }    // Required for Edit dialogs
public string Flags { get; set; }      // Required for Edit dialogs
```

### Edit Dialog Contracts
All Edit dialog bindings remain unchanged:

```xml
<!-- Edit dialogs preserve full functionality -->
<TextBox Text="{Binding Options}" />
<TextBox Text="{Binding Flags}" />
```

## No Breaking Changes

- No public API changes
- No service interface changes
- No data model changes
- No persistence contract changes
- No business logic contract changes

This is purely a UI presentation modification with zero contract breaking changes.
