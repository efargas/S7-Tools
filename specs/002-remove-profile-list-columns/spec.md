# Feature Specification: Remove Options and Flags Columns from Profile List Viewers

**Feature ID**: 002-remove-profile-list-columns
**Date**: 2025-10-21
**Status**: Planning

## Summary

Remove the "Options" and "Flags" columns from the DataGrid list viewers in all profile management views (Serial, Socat, PowerSupply, and Jobs) to simplify the user interface and improve usability.

## User Story

**As a** user managing profiles in S7Tools
**I want** to have a cleaner, more focused list view of my profiles
**So that** I can quickly identify and select profiles without visual clutter from technical implementation details

## Background

Currently, all profile management views display "Options" and "Flags" columns in their DataGrid components. These columns:
- Display technical command-line options and flags that are rarely needed during normal profile selection
- Add horizontal scrolling and visual clutter to the interface
- Are more relevant during profile editing rather than profile browsing
- Contain information that's already accessible through the Edit dialog when needed

## Functional Requirements

### FR-1: Remove Options Column
- Remove the "Options" DataGridTextColumn from all profile list viewers
- Remove associated binding `{Binding Options}`
- Remove column header and styling

### FR-2: Remove Flags Column
- Remove the "Flags" DataGridTextColumn from all profile list viewers
- Remove associated binding `{Binding Flags}`
- Remove column header and styling

### FR-3: Affected Views
The following view files must be updated:
- `src/S7Tools/Views/SerialPortsSettingsView.axaml`
- `src/S7Tools/Views/SocatSettingsView.axaml`
- `src/S7Tools/Views/PowerSupplySettingsView.axaml`
- `src/S7Tools/Views/JobsMainContentView.axaml`

### FR-4: Preserve Data Access
- Options and Flags data must remain accessible through profile Edit dialogs
- No changes to underlying data models or business logic
- No changes to profile persistence or serialization

## Non-Functional Requirements

### NFR-1: User Experience
- Reduced horizontal scrolling in profile lists
- Cleaner, more focused visual presentation
- Faster profile identification and selection

### NFR-2: Maintainability
- Consistent column layout across all profile types
- Simplified XAML with fewer column definitions
- No impact on existing functionality

### NFR-3: Backward Compatibility
- No breaking changes to data models
- No impact on existing profiles or persistence
- Options and Flags remain fully functional in edit scenarios

## Success Criteria

1. **Visual Verification**: All four profile list views no longer display Options or Flags columns
2. **Functionality Preservation**: Options and Flags remain editable in profile Edit dialogs
3. **No Regressions**: All existing profile management operations continue to work
4. **Improved UX**: Profile lists are visually cleaner and easier to navigate

## Out of Scope

- Changes to profile data models (IProfileBase interface)
- Changes to profile Edit dialogs or forms
- Changes to profile persistence or serialization logic
- Modifications to business logic or validation rules

## Technical Notes

This is a pure UI change affecting only XAML DataGrid column definitions. The underlying reactive properties and data binding for Options and Flags will remain unchanged to preserve functionality in Edit dialogs.

## Risk Assessment

- **Low Risk**: Only removes UI columns, no business logic changes
- **No Data Loss**: Options and Flags data remains intact and accessible
- **Easy Rollback**: Changes can be reverted by restoring column definitions

## Dependencies

- None - this is an isolated UI improvement
- No external system changes required
- No database or persistence changes needed
