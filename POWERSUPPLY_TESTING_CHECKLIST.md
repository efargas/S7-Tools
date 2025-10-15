# PowerSupply Profile System Testing Checklist

## Issues Addressed in This Session

### 🔧 **Critical Fixes Applied**
1. **JSON Serialization Issue**: Added `JsonDerivedType` attributes to `PowerSupplyConfiguration.cs`
   - Fixes: `System.NotSupportedException` when deserializing abstract types
   - Location: `src/S7Tools.Core/Models/PowerSupplyConfiguration.cs`

2. **Dialog Save Button**: Enhanced `ProfileEditDialog.axaml.cs`
   - Added PowerSupplyProfileViewModel handling in `OnSaveButtonClick()`
   - Added PowerSupplyProfileViewModel handling in `IsProfileValid()`
   - Location: `src/S7Tools/Views/ProfileEditDialog.axaml.cs`

3. **Corrupted JSON Cleanup**: Removed corrupted PowerSupply profiles.json
   - File deleted: `resources/PowerSupplyProfiles/profiles.json`
   - Will be regenerated with proper serialization on first run

### 🎯 **Testing Plan**

## Phase 1: Basic Functionality Test
**Objective**: Verify PowerSupply profile system works end-to-end

### Step 1: Application Startup
- [ ] Run: `cd src && dotnet run --project S7Tools/S7Tools.csproj`
- [ ] Verify application starts without errors
- [ ] Check LOG VIEWER for any startup errors related to PowerSupply

### Step 2: Navigation Test
- [ ] Navigate to Settings → Power Supply
- [ ] Verify PowerSupply settings page loads
- [ ] Check if default profile is created automatically
- [ ] Verify profile list shows at least one default profile

### Step 3: Profile Creation Test
- [ ] Click "Create" button
- [ ] Verify PowerSupply profile dialog opens
- [ ] Fill in profile details:
  - Name: "Test PowerSupply 1"
  - Description: "Test profile creation"
  - Type: Modbus TCP (default)
- [ ] Click "Save" button
- [ ] Verify profile is created and appears in list
- [ ] Check LOG VIEWER for success messages

### Step 4: Profile Editing Test
- [ ] Select an existing profile
- [ ] Click "Edit" button
- [ ] Verify PowerSupply profile dialog opens with existing data
- [ ] Modify profile details:
  - Change name to "Test PowerSupply 1 - Modified"
  - Update description
- [ ] Click "Save" button
- [ ] Verify changes are saved and reflected in list

### Step 5: Profile Duplication Test
- [ ] Select an existing profile
- [ ] Click "Duplicate" button
- [ ] Verify duplication dialog appears
- [ ] Enter new name: "Test PowerSupply 1 - Copy"
- [ ] Click "OK" or "Save"
- [ ] Verify duplicated profile appears in list

## Phase 2: Error Scenarios Test
**Objective**: Verify error handling works correctly

### Step 6: Validation Test
- [ ] Try creating profile with empty name
- [ ] Verify validation error appears
- [ ] Try creating profile with duplicate name
- [ ] Verify duplicate name error appears

### Step 7: Cancel Operations Test
- [ ] Open Create dialog and click "Cancel"
- [ ] Open Edit dialog and click "Cancel"
- [ ] Verify no changes are made to profile list

## Phase 3: File System Verification
**Objective**: Verify JSON file operations work correctly

### Step 8: JSON File Verification
- [ ] After creating profiles, check: `resources/PowerSupplyProfiles/profiles.json`
- [ ] Verify file is created and contains valid JSON
- [ ] Verify polymorphic serialization works (Configuration property contains type discriminator)

### Step 9: Application Restart Test
- [ ] Close application
- [ ] Restart application
- [ ] Navigate to Settings → Power Supply
- [ ] Verify all created profiles are loaded correctly

## Phase 4: Integration Test
**Objective**: Verify PowerSupply works with other profile types

### Step 10: Cross-Profile Comparison
- [ ] Compare PowerSupply profile functionality with:
  - Settings → Serial Ports (working reference)
  - Settings → Socat (working reference)
- [ ] Verify all three have consistent UI behavior
- [ ] Verify all three support Create/Edit/Duplicate/Delete operations

## Expected Log Messages
**Success Indicators in LOG VIEWER:**

```
[INFO] Power supply profile created successfully: Test PowerSupply 1
[INFO] Power supply profile edited successfully: Test PowerSupply 1 - Modified
[INFO] Power supply profile duplicate name entered: Test PowerSupply 1 - Copy
[INFO] PowerSupply profile JSON file created at: resources/PowerSupplyProfiles/profiles.json
```

**Error Indicators to Watch For:**
```
[ERROR] System.NotSupportedException: Deserialization of interface types is not supported
[ERROR] Failed to create profile: ...
[ERROR] Profile ID must be greater than zero
[WARN] Power supply profile operation failed: ...
```

## Troubleshooting Guide

### If PowerSupply Profiles Don't Appear:
1. Check LOG VIEWER for JSON deserialization errors
2. Verify `JsonDerivedType` attributes in PowerSupplyConfiguration.cs
3. Delete corrupted profiles.json and restart application

### If Dialog Save Button Doesn't Work:
1. Check LOG VIEWER for dialog-related errors
2. Verify PowerSupplyProfileViewModel is handled in ProfileEditDialog.axaml.cs
3. Check validation logic in dialog code

### If Create/Duplicate Commands Don't Work:
1. Check command binding in PowerSupplySettingsViewModel
2. Verify ProfileManagementViewModelBase template methods are implemented
3. Check UnifiedProfileDialogService PowerSupply methods

## Success Criteria
✅ **All tests pass without errors**
✅ **PowerSupply profiles can be created, edited, and duplicated**
✅ **JSON serialization works correctly with polymorphic types**
✅ **UI behavior matches Serial and Socat profile functionality**
✅ **Application restart preserves all PowerSupply profiles**

---

**Last Updated**: 2025-01-27
**Session Context**: Critical bug fixes for PowerSupply profile system
**Key Files Modified**:
- `src/S7Tools.Core/Models/PowerSupplyConfiguration.cs`
- `src/S7Tools/Views/ProfileEditDialog.axaml.cs`
- `src/S7Tools/Services/StandardProfileManager.cs`
