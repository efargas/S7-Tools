# Data Model: Remove Options and Flags Columns from Profile List Viewers

**Date**: 2025-10-21
**Status**: Complete

## Overview

This feature requires **no data model changes**. All existing data models, interfaces, and business logic remain unchanged. This is purely a UI presentation layer modification.

## Existing Data Models (Unchanged)

The following models remain fully intact with all properties preserved:

### IProfileBase Interface
```csharp
public interface IProfileBase
{
    // Core properties (unchanged)
    int Id { get; set; }
    string Name { get; set; }
    string Description { get; set; }

    // Options and Flags properties REMAIN UNCHANGED
    string Options { get; set; }     // Still accessible in Edit dialogs
    string Flags { get; set; }       // Still accessible in Edit dialogs

    // Other properties (unchanged)
    DateTime CreatedAt { get; set; }
    DateTime ModifiedAt { get; set; }
    bool IsDefault { get; set; }
    bool IsReadOnly { get; set; }
    // ... additional properties remain unchanged
}
```

### Profile Implementations (Unchanged)
- **SerialPortProfile**: All properties including Options and Flags preserved
- **SocatProfile**: All properties including Options and Flags preserved
- **PowerSupplyProfile**: All properties including Options and Flags preserved
- **JobProfile**: All properties including Options and Flags preserved

## Data Access Patterns (Unchanged)

### ViewModel Properties
All reactive properties in ViewModels remain unchanged to preserve Edit dialog functionality:

```csharp
// These properties MUST remain unchanged in all profile ViewModels
public string Options
{
    get => _options;
    set => this.RaiseAndSetIfChanged(ref _options, value);
}

public string Flags
{
    get => _flags;
    set => this.RaiseAndSetIfChanged(ref _flags, value);
}
```

### Data Binding Contracts
- Edit dialogs continue to bind to Options and Flags properties
- Profile managers continue to persist Options and Flags data
- All CRUD operations remain fully functional
- No changes to serialization or data persistence

## Data Flow Diagram

```
UI Layer Changes:
List View DataGrid Columns → [REMOVED] → Options & Flags not displayed

Edit Dialog Controls → [UNCHANGED] → Options & Flags fully editable

Data Layer (No Changes):
Profile Models → [UNCHANGED] → Options & Flags properties preserved
ViewModels → [UNCHANGED] → Options & Flags reactive properties preserved
Persistence → [UNCHANGED] → Options & Flags saved/loaded normally
```

## Validation Rules (Unchanged)

All existing validation rules for Options and Flags remain in effect:
- Field length validation preserved
- Content validation preserved
- Business rule validation preserved

## Migration Strategy

**No migration required** - this is a pure presentation change with no data impact.

## Data Integrity Guarantee

This implementation guarantees:
- **Zero data loss**: All Options and Flags data preserved
- **Zero functional loss**: All editing capabilities preserved
- **Zero persistence impact**: All save/load operations unchanged
- **Full rollback capability**: Column display can be restored without data loss

## Testing Data Scenarios

Since no data changes occur, testing focuses on data preservation:
1. Verify existing Options and Flags data remains intact
2. Confirm Edit dialogs can still modify Options and Flags
3. Validate save/load operations preserve all data
4. Test that column removal doesn't affect data binding
