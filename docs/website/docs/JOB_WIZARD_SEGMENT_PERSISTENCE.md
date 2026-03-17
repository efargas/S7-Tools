---
title: "Job Wizard Segment Persistence"
version: "1.1.0"
created: "2026-01-09"
last-updated: "2026-01-09"
status: "current"
tags: ["job-wizard", "memory", "persistence", "ux"]
related:
  - "docs/MEMORY_REGION_PROFILES.md"
  - "docs/patterns/_index.md"
---

# Job Wizard Segment Persistence Guide

## Overview

This document describes the segment selection persistence system implemented in version 1.1, which ensures that memory segment selections are correctly saved and restored when creating or editing jobs through the wizard interface.

## Problem Statement

Prior to v1.1, selected memory segments in the job wizard were not persisting correctly:

- Segment selections would revert to default (`.bss`) when reopening a job for editing
- Job Information panel showed incorrect segments from the profile's default state
- No synchronization between wizard and memory step ViewModels

## Solution Architecture

### Data Flow

```
┌─────────────────────────────────────────────────────────┐
│                    JobProfile (Database)                 │
│  - MemoryRegionProfileId: int                           │
│  - SelectedMemorySegment: string  ← NEW! Stores segment │
└─────────────────────────────────────────────────────────┘
                           ↓ ↑
          ┌────────────────┴─┴───────────────┐
          ↓                                   ↑
┌──────────────────────┐         ┌──────────────────────┐
│ JobWizardViewModel   │         │  Memory Step VM      │
│  (Clone A)           │◄────────┤  (Clone B)           │
│  - SelectedMemory    │  Sync   │  - SelectedProfile   │
│    Region            │         │  - AvailableProfiles │
│  - MemoryProfiles    │         │                      │
└──────────────────────┘         └──────────────────────┘
         │                                    │
         │ Forward Sync (Save)                │
         │ SyncMemoryRegionFromStepViewModel()│
         │                                    │
         │ Reverse Sync (Load)                │
         │ SyncMemoryRegionToStepViewModel()  │
         └────────────────────────────────────┘
```

### Key Components

#### 1. Profile Cloning

Both ViewModels clone `MemoryMappingProfile` instances independently to avoid shared state:

```csharp
// JobWizardViewModel.LoadAsync()
foreach (MemoryMappingProfile m in memoryProfiles)
{
    MemoryProfiles.Add(m.ClonePreserveId()); // Clone A
}

// JobWizardMemoryRegionStepViewModel.LoadProfilesAsync()
foreach (MemoryMappingProfile profile in profiles)
{
    AvailableProfiles.Add(profile.ClonePreserveId()); // Clone B
}
```

#### 2. Forward Synchronization (Save Direction)

Before saving, copy segment selection from memory step ViewModel to main wizard:

```csharp
private void SyncMemoryRegionFromStepViewModel()
{
    if (SelectedMemoryRegion == null || MemoryRegionStepViewModel?.SelectedProfile == null)
        return;

    // Find selected segment in memory step's clone
    MemorySegment? selectedInStep = MemoryRegionStepViewModel.SelectedProfile.Segments
        .FirstOrDefault(s => s.IsSelected);

    if (selectedInStep != null)
    {
        // Update wizard's clone with the same selection
        MemorySegment? wizardSegment = SelectedMemoryRegion.Segments
            .FirstOrDefault(s => s.Name == selectedInStep.Name);
            
        if (wizardSegment != null)
        {
            // Clear other selections
            foreach (MemorySegment seg in SelectedMemoryRegion.Segments)
            {
                seg.IsSelected = (seg.Name == selectedInStep.Name);
            }
        }
    }
}
```

Called in `ExecuteFinishAsync()` before saving.

#### 3. Reverse Synchronization (Load Direction)

After loading a job for editing, copy segment selection from wizard to memory step:

```csharp
private void SyncMemoryRegionToStepViewModel()
{
    if (SelectedMemoryRegion == null || MemoryRegionStepViewModel == null)
        return;

    // CRITICAL: First ensure memory step has the SAME profile selected
    bool profileSelected = MemoryRegionStepViewModel.SetSelectedProfileId(SelectedMemoryRegion.Id);
    
    if (!profileSelected)
        return;

    // Find selected segment in wizard's clone
    MemorySegment? selectedInWizard = SelectedMemoryRegion.Segments
        .FirstOrDefault(s => s.IsSelected);

    if (selectedInWizard != null && MemoryRegionStepViewModel.SelectedProfile != null)
    {
        // Update memory step's clone with the same selection
        MemorySegment? stepSegment = MemoryRegionStepViewModel.SelectedProfile.Segments
            .FirstOrDefault(s => s.Name == selectedInWizard.Name);
            
        if (stepSegment != null)
        {
            // Clear other selections
            foreach (MemorySegment seg in MemoryRegionStepViewModel.SelectedProfile.Segments)
            {
                seg.IsSelected = (seg.Name == selectedInWizard.Name);
            }
        }
    }
}
```

Called in:

- `InitializeFromJob()` - When using `SetJobToEdit()` + `LoadAsync()`
- `LoadJobForEditAsync()` - When clicking "Edit (Wizard)" button

#### 4. Job Information Panel Display

Updated to read from `job.SelectedMemorySegment` instead of profile's default state:

```csharp
// JobInfoDisplayViewModel.LoadMemoryRegionDetailsAsync()
MemorySegment? jobSelectedSegment = null;
if (!string.IsNullOrEmpty(job.SelectedMemorySegment))
{
    jobSelectedSegment = memoryProfile.Segments
        .FirstOrDefault(s => s.Name.Equals(job.SelectedMemorySegment, StringComparison.Ordinal));
}

if (jobSelectedSegment != null)
{
    // Display job's selected segment, not profile's default
    segmentProperties.Add(new PropertyDisplayItem
    {
        Label = jobSelectedSegment.Name,
        Value = $"{jobSelectedSegment.AddressRange} ({jobSelectedSegment.SizeFormatted})",
        Tooltip = $"Type: {jobSelectedSegment.Type}, Size: {jobSelectedSegment.Size} bytes"
    });
}
```

## UX Improvements

### Edit Button Behavior

The Edit button now opens the wizard interface instead of a simple dialog:

```csharp
// JobsManagementViewModel.SetupJobSpecificCommands()
EditCommand.Subscribe(_ =>
{
    if (SelectedProfile != null)
    {
        _logger.LogInformation("Edit button clicked: Navigating to Edit (Wizard) for job {JobId}", SelectedProfile.Id);
        SelectedSideMenuItem = "Edit (Wizard)";
    }
}).DisposeWith(_localDisposables);
```

### Auto-Navigation After Finish

Both Create and Edit wizards now automatically return to Main View:

```csharp
// CreateWizardViewModel() and CreateWizardViewModelFromSelected()
wizard.WhenAnyValue(w => w.Completed)
    .Where(completed => completed)
    .Take(1)
    .Subscribe(async _ =>
    {
        // Navigate back to Main View
        await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            SelectedSideMenuItem = "Main View";
        }).ConfigureAwait(false);

        // Refresh job list
        await Task.Delay(500);
        await RefreshJobsAsync();

        // Select the created/edited job
        if (wizard.CreatedJobId.HasValue)
        {
            await SelectCreatedJobAsync(wizard.CreatedJobId.Value);
        }
    });
```

## Testing Workflow

### Create New Job

1. Click "Create (Wizard)" or Create button
2. Configure job and select memory segment (e.g., `.exec_in_lomem`)
3. Click Finish
4. **Expected**: Auto-navigate to Main View, job selected, segment shown in Job Information panel

### Edit Existing Job

1. Select a job
2. Click "Edit" button or "Edit (Wizard)" menu
3. Navigate to Memory Region step
4. **Expected**: Previously selected segment is shown
5. Change segment selection
6. Click Finish
7. **Expected**: Auto-navigate to Main View, job selected, updated segment shown

### Persistence Verification

1. Edit job and change segment
2. Close application
3. Reopen application
4. Edit same job
5. **Expected**: Segment selection persisted across application restarts

## Implementation Summary

### Files Modified

- `JobWizardViewModel.cs`
    - Added `SyncMemoryRegionFromStepViewModel()` and `SyncMemoryRegionToStepViewModel()`
    - Modified `LoadAsync()` to clone profiles
    - Modified `ExecuteFinishAsync()` to call forward sync
    - Modified `InitializeFromJob()` and `LoadJobForEditAsync()` to call reverse sync

- `JobWizardMemoryRegionStepViewModel.cs`
    - Modified `LoadProfilesAsync()` to clone profiles

- `JobInfoDisplayViewModel.cs`
    - Modified `LoadMemoryRegionDetailsAsync()` to read from `job.SelectedMemorySegment`

- `JobsManagementViewModel.cs`
    - Modified `SetupJobSpecificCommands()` to intercept Edit button
    - Modified `CreateWizardViewModel()` and `CreateWizardViewModelFromSelected()` to add auto-navigation

### Database Schema

**JobProfile** model extended with:
```csharp
public string SelectedMemorySegment { get; set; } = string.Empty;
```

Stored in `Jobs.json`:
```json
{
  "id": 6,
  "name": "BSS End",
  "memoryRegionProfileId": 1,
  "selectedMemorySegment": ".exec_in_lomem"  // ← NEW!
}
```

## Related Documentation

- [Memory_Region_Profiles](MEMORY_REGION_PROFILES.md)
- [_Index](patterns/_index.md)
- [Profile Management](patterns/profile-management.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
