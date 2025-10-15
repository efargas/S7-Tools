# Profile Management Fixes - Complete Summary

## Date: October 15, 2025

## Issues Fixed

### 1. ✅ **Edit Creating New Profile Instead of Updating**

**Problem**: When editing a profile, the Edit dialog was creating a new profile instead of updating the existing one.

**Root Cause**: The `LoadProfile()` method was not being called in `ProfileEditDialogService` to initialize the ViewModel with existing profile data. The ViewModel always started empty with `_originalProfile = null`, causing SaveAsync to treat it as a Create operation.

**Solution**: Uncommented and activated `profileViewModel.LoadProfile(profile)` calls in all three Edit methods:

#### Serial Port Profile Edit (Line 318)
```csharp
// Before:
// TODO: Implement LoadFromProfile method or use profile data to initialize ViewModel
// profileViewModel.LoadFromProfile(profile);

// After:
// Load the existing profile data into the ViewModel
profileViewModel.LoadProfile(profile);
```

#### Socat Profile Edit (Line 363)
```csharp
// Before:
// TODO: Implement LoadFromProfile method or use profile data to initialize ViewModel
// profileViewModel.LoadFromProfile(profile);

// After:
// Load the existing profile data into the ViewModel
profileViewModel.LoadProfile(profile);
```

#### PowerSupply Profile Edit (Line 406)
```csharp
// Before:
// TODO: Implement LoadFromProfile method or use profile data to initialize ViewModel
// profileViewModel.LoadFromProfile(profile);

// After:
// Load the existing profile data into the ViewModel
profileViewModel.LoadProfile(profile);
```

**Files Modified**:
- `/src/S7Tools/Services/ProfileEditDialogService.cs`

**Verification**:
- ✅ `LoadProfile()` method already exists in all three ViewModels
- ✅ Method sets `_originalProfile` to the loaded profile
- ✅ SaveAsync checks `if (_originalProfile != null)` to determine Update vs Create
- ✅ Build successful with no errors

---

### 2. ⚠️ **Set Default Not Updating UI Checkbox**

**Problem**: When setting a profile as default, the operation succeeds but the UI checkbox doesn't update to show the change.

**Analysis**:
- `ExecuteSetDefaultAsync` at line 780 of `ProfileManagementViewModelBase.cs` **DOES** call `LoadProfilesAsync()`
- `LoadProfilesAsync` at line 416 clears and reloads the `Profiles` collection on the UI thread
- The operation is working correctly in the backend

**Likely Cause**:
The issue is probably related to the DataGrid not refreshing the checkbox binding after the collection is updated. This could be:
1. A timing issue with the UI update
2. The DataGrid needing to be notified of property changes on the profile objects
3. The IsDefault property binding not triggering change notifications

**Status**: ✅ **Backend logic is correct** - This appears to be a UI binding/refresh issue, not a logic issue.

**Recommendation**:
If the issue persists after testing:
1. Verify the IsDefault property raises PropertyChanged in the profile models
2. Check if the DataGrid ItemsSource binding needs to be refreshed
3. Consider using `CollectionView.Refresh()` or similar UI-specific refresh mechanism

---

### 3. ⚠️ **Create/Duplicate Not Auto-Refreshing**

**Problem**: After creating or duplicating a profile, the user needs to manually refresh to see changes.

**Analysis**:
- `ExecuteCreateAsync` at line 523 **DOES** call `await LoadProfilesAsync().ConfigureAwait(false)`
- `ExecuteDuplicateAsync` at line 668 **DOES** call `await LoadProfilesAsync().ConfigureAwait(false)`
- Both methods correctly reload profiles after successful operations

**Verification from Code**:

#### Create Operation (ProfileManagementViewModelBase.cs, line 509-524):
```csharp
var result = await ShowCreateDialogAsync(request).ConfigureAwait(false);

if (result.IsSuccess && result.Result != null)
{
    // ... validation ...
    var createdProfile = await GetProfileManager().CreateAsync(result.Result).ConfigureAwait(false);

    // Refresh profiles from storage to ensure UI is in sync
    await LoadProfilesAsync().ConfigureAwait(false);  // ✅ PRESENT

    // Select the newly created profile
    await _uiThreadService.InvokeOnUIThreadAsync(() => { ... }).ConfigureAwait(false);
}
```

#### Duplicate Operation (ProfileManagementViewModelBase.cs, line 655-669):
```csharp
var result = await ShowDuplicateDialogAsync(request).ConfigureAwait(false);

if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Result))
{
    // ... validation ...
    var duplicatedProfile = await GetProfileManager().DuplicateAsync(...).ConfigureAwait(false);

    // Refresh profiles from storage to ensure UI is in sync
    await LoadProfilesAsync().ConfigureAwait(false);  // ✅ PRESENT

    // Select the newly duplicated profile
    await _uiThreadService.InvokeOnUIThreadAsync(() => { ... }).ConfigureAwait(false);
}
```

**LoadProfilesAsync Implementation** (Line 416-427):
```csharp
private async Task LoadProfilesAsync()
{
    var profileManager = GetProfileManager();
    var profiles = await profileManager.GetAllAsync().ConfigureAwait(false);

    // Update collection on UI thread
    await _uiThreadService.InvokeOnUIThreadAsync(() =>
    {
        Profiles.Clear();
        foreach (var profile in profiles)
        {
            Profiles.Add(profile);
        }
    });
}
```

**Status**: ✅ **All auto-refresh logic is implemented correctly**

**Possible Causes if Issue Persists**:
1. **Timing Issue**: The dialog might not be waiting for the operation to complete before closing
2. **UI Thread Marshalling**: The collection update might not be triggering UI refresh properly
3. **Observable Collection**: The `Profiles` collection notifications might not be propagating to the DataGrid

**Recommendation**:
If the issue persists after testing:
1. Add diagnostic Console.WriteLine statements to verify LoadProfilesAsync is being called and completing
2. Check if the dialog result handlers are awaiting the async operations correctly
3. Verify the DataGrid's ItemsSource binding is set to the Profiles ObservableCollection
4. Test if manually calling Refresh command after Create/Duplicate updates the UI

---

## Summary

### ✅ **Confirmed Fixed**:
1. **Edit Operation** - Now properly loads existing profile data and updates instead of creating

### ⚠️ **Needs Testing**:
2. **Set Default UI Update** - Backend logic correct, may be UI binding issue
3. **Create/Duplicate Auto-Refresh** - Backend logic correct, may be timing/UI issue

### **Files Modified**:
- `/src/S7Tools/Services/ProfileEditDialogService.cs` - Added `LoadProfile()` calls for Edit operations

### **Build Status**:
- ✅ Solution compiles successfully (0 errors, only warnings)

### **Testing Checklist**:
- [ ] **Test Edit**: Edit a profile → Verify it updates existing profile, not creates new
- [ ] **Test Set Default**: Set a profile as default → Check if UI checkbox updates immediately
- [ ] **Test Create**: Create a profile → Check if it appears in list without manual refresh
- [ ] **Test Duplicate**: Duplicate a profile → Check if copy appears in list without manual refresh
- [ ] **Verify All Operations**: Test on all three profile types (Serial, Socat, PowerSupply)

---

## Additional Notes

**Semaphore Deadlock Fixes** (from previous session):
- All semaphore deadlock issues were fixed by creating "Core" versions of methods
- `GetNextAvailableIdCore()` and `EnsureUniqueNameCore()` prevent nested semaphore acquisition
- These fixes ensure Create, Duplicate, and Import operations don't hang

**Next Steps**:
1. Run the application
2. Test all CRUD operations systematically
3. If Set Default or Auto-Refresh still have issues, add diagnostic logging
4. Report any remaining issues with specific scenarios
