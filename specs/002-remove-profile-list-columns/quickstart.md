# Quickstart Guide: Remove Options and Flags Columns

**Date**: 2025-10-21

## Implementation Overview

This guide provides a step-by-step approach to remove Options and Flags columns from all profile list viewers in S7Tools.

## Prerequisites

- S7Tools development environment set up
- Visual Studio Code or IDE with XAML editing support
- Basic understanding of Avalonia DataGrid controls

## Quick Implementation Steps

### 1. Locate Target Files
Navigate to the following files in the S7Tools project:
```
src/S7Tools/Views/SerialPortsSettingsView.axaml
src/S7Tools/Views/SocatSettingsView.axaml
src/S7Tools/Views/PowerSupplySettingsView.axaml
src/S7Tools/Views/JobsMainContentView.axaml
```

### 2. Remove Options Column
In each file, locate and delete the Options column definition:
```xml
<!-- DELETE THIS ENTIRE BLOCK -->
<DataGridTextColumn Binding="{Binding Options}" Width="150" MinWidth="100">
  <DataGridTextColumn.Header>
    <TextBlock Text="Options" Margin="0,0,8,0" TextTrimming="CharacterEllipsis" />
  </DataGridTextColumn.Header>
</DataGridTextColumn>
```

### 3. Remove Flags Column
In each file, locate and delete the Flags column definition:
```xml
<!-- DELETE THIS ENTIRE BLOCK -->
<DataGridTextColumn Binding="{Binding Flags}" Width="120" MinWidth="80">
  <DataGridTextColumn.Header>
    <TextBlock Text="Flags" Margin="0,0,8,0" TextTrimming="CharacterEllipsis" />
  </DataGridTextColumn.Header>
</DataGridTextColumn>
```

### 4. Verification Steps
1. Build the project: `dotnet build src/S7Tools.sln`
2. Run the application: `dotnet run --project src/S7Tools`
3. Navigate to each profile management section
4. Verify Options and Flags columns no longer appear
5. Test Edit dialogs still show Options and Flags fields

## Expected Results

**Before**: Profile lists show Options and Flags columns with command-line text
**After**: Profile lists show cleaner layout without Options and Flags columns
**Preserved**: Edit dialogs continue to show Options and Flags for editing

## Rollback Instructions

To rollback, restore the deleted column definitions from git history:
```bash
git checkout HEAD~1 -- src/S7Tools/Views/*SettingsView.axaml
git checkout HEAD~1 -- src/S7Tools/Views/JobsMainContentView.axaml
```

## Testing Checklist

- [ ] SerialPortsSettingsView: Options and Flags columns removed
- [ ] SocatSettingsView: Options and Flags columns removed
- [ ] PowerSupplySettingsView: Options and Flags columns removed
- [ ] JobsMainContentView: Options and Flags columns removed
- [ ] All Edit dialogs: Options and Flags fields still editable
- [ ] Profile CRUD operations: All functionality preserved
- [ ] No build errors or runtime exceptions

## Troubleshooting

**Issue**: Build errors after removing columns
**Solution**: Ensure entire column definition block is removed, including headers

**Issue**: Edit dialogs missing Options/Flags
**Solution**: Verify only DataGrid columns were removed, not Edit dialog controls

**Issue**: Runtime binding errors
**Solution**: Confirm ViewModel properties for Options and Flags are unchanged
