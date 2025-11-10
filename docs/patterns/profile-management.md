---
title: "Unified Profile Management Pattern"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["pattern", "profile-management", "crud", "template-method", "standardization"]
related:
  - "docs/patterns/system-patterns.md"
  - "docs/patterns/_index.md"
  - "docs/architecture/clean-architecture.md"
  - "docs/architecture/mvvm-patterns.md"
supersedes: []
---

# Unified Profile Management Pattern

## Problem Statement

**Context**: S7Tools uses multiple profile types (SerialPort, Socat, PowerSupply, Job, MemoryRegion) that all require standard CRUD operations, validation, and persistence.

**Problems**:
- Duplicate CRUD logic across profile types
- Inconsistent validation and business rules
- Thread-safety issues with concurrent access
- Profile ID management and gap-filling complexity
- No standardized default profile handling

## Solution

Implement a **Unified Profile Management** architecture using:
1. Common interface (`IProfileBase`) for all profile types
2. Generic manager (`StandardProfileManager<T>`) with template method pattern
3. Thin service implementations extending the manager
4. Thread-safe operations with Internal Method Pattern

## Architecture

### Core Contracts

#### IProfileBase Interface

All profiles implement a common interface:

```csharp
namespace S7Tools.Core.Models;

public interface IProfileBase
{
    // Identity
    int Id { get; set; }
    string Name { get; set; }
    string Description { get; set; }

    // Type-specific data (serialized)
    string Options { get; set; }
    string Flags { get; set; }

    // Metadata
    DateTime CreatedAt { get; set; }
    DateTime ModifiedAt { get; set; }

    // Business rules
    bool IsDefault { get; set; }
    bool IsReadOnly { get; set; }

    // Operations
    bool CanModify();
    bool CanDelete();
    string GetSummary();
    IProfileBase Clone();
}
```

**Design Decisions**:
- `Options` and `Flags`: Type-specific data as serialized strings (flexibility)
- `IsReadOnly`: System profiles cannot be modified or deleted
- `IsDefault`: Only one default profile per type
- `CanModify()/CanDelete()`: Business rule encapsulation

#### IProfileManager<T> Interface

Standard CRUD operations for all profile types:

```csharp
namespace S7Tools.Core.Services.Interfaces;

public interface IProfileManager<T> where T : class, IProfileBase
{
    // Core CRUD
    Task<T> CreateAsync(T profile, CancellationToken ct = default);
    Task<T> UpdateAsync(T profile, CancellationToken ct = default);
    Task<bool> DeleteAsync(int profileId, CancellationToken ct = default);
    Task<T?> GetByIdAsync(int profileId, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    // Advanced operations
    Task<T> DuplicateAsync(int sourceProfileId, string newName, CancellationToken ct = default);
    Task<bool> SetDefaultAsync(int profileId, CancellationToken ct = default);
    Task<T?> GetDefaultAsync(CancellationToken ct = default);

    // Bulk operations
    Task<bool> ImportAsync(IEnumerable<T> profiles, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ExportAsync(CancellationToken ct = default);

    // Validation
    Task<bool> ExistsAsync(int profileId, CancellationToken ct = default);
    Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken ct = default);
}
```

### Implementation: StandardProfileManager<T>

**Generic base class** providing all CRUD logic:

```csharp
namespace S7Tools.Services;

public abstract class StandardProfileManager<T> : IProfileManager<T>
    where T : class, IProfileBase
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<T> _profiles = new();
    private readonly ILogger<StandardProfileManager<T>> _logger;

    protected StandardProfileManager(ILogger<StandardProfileManager<T>> logger)
    {
        _logger = logger;
    }

    // Template methods - subclasses must implement
    protected abstract T CreateDefaultProfile();
    protected abstract string GetStorageFileName();
    protected abstract void ValidateProfile(T profile);

    // Public API (acquires semaphore)
    public async Task<T> CreateAsync(T profile, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            return await CreateInternalAsync(profile, ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Internal (assumes semaphore held)
    private async Task<T> CreateInternalAsync(T profile, CancellationToken ct)
    {
        // Validation
        ValidateProfile(profile);
        await EnsureNameUniqueInternalAsync(profile.Name, null, ct);

        // ID assignment (gap-filling)
        profile.Id = GetNextAvailableId();

        // Timestamps
        profile.CreatedAt = DateTime.UtcNow;
        profile.ModifiedAt = DateTime.UtcNow;

        // Add to collection
        _profiles.Add(profile);

        // Persist
        await SaveToFileAsync(ct);

        _logger.LogInformation("Created profile: {ProfileId} - {ProfileName}",
            profile.Id, profile.Name);

        return (T)profile.Clone();
    }

    private int GetNextAvailableId()
    {
        if (_profiles.Count == 0) return 1;

        // Gap-filling: Find smallest missing ID
        var existingIds = _profiles.Select(p => p.Id).OrderBy(id => id).ToList();

        for (int i = 1; i <= existingIds.Count + 1; i++)
        {
            if (!existingIds.Contains(i))
                return i;
        }

        return existingIds.Max() + 1;
    }
}
```

### Template Method Pattern

Subclasses implement only type-specific behavior:

```csharp
public class SerialPortProfileService : StandardProfileManager<SerialPortProfile>
{
    public SerialPortProfileService(ILogger<StandardProfileManager<SerialPortProfile>> logger)
        : base(logger)
    {
    }

    protected override SerialPortProfile CreateDefaultProfile()
    {
        return new SerialPortProfile
        {
            Name = "Default Serial Port",
            Description = "Default serial port configuration",
            BaudRate = 115200,
            Parity = Parity.None,
            DataBits = 8,
            StopBits = StopBits.One,
            IsDefault = true,
            IsReadOnly = true
        };
    }

    protected override string GetStorageFileName() => "serial-profiles.json";

    protected override void ValidateProfile(SerialPortProfile profile)
    {
        if (profile.BaudRate <= 0)
            throw new ValidationException("Baud rate must be positive");

        if (profile.DataBits < 5 || profile.DataBits > 8)
            throw new ValidationException("Data bits must be 5-8");
    }
}
```

## Key Features

### 1. Thread-Safe Operations

**Pattern**: Internal Method Pattern prevents semaphore deadlocks

```csharp
// Public method (acquires semaphore)
public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken ct = default)
{
    await _semaphore.WaitAsync(ct);
    try
    {
        return await IsNameUniqueInternalAsync(name, excludeId, ct);
    }
    finally
    {
        _semaphore.Release();
    }
}

// Internal method (assumes semaphore held)
private Task<bool> IsNameUniqueInternalAsync(string name, int? excludeId, CancellationToken ct)
{
    var exists = _profiles.Any(p =>
        p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
        p.Id != excludeId);

    return Task.FromResult(!exists);
}
```

**Why**: Prevents deadlocks when public methods call each other internally.

### 2. Gap-Filling ID Assignment

**Feature**: Reuses IDs from deleted profiles

```csharp
private int GetNextAvailableId()
{
    if (_profiles.Count == 0) return 1;

    // Find smallest missing ID
    var existingIds = _profiles.Select(p => p.Id).OrderBy(id => id).ToList();

    for (int i = 1; i <= existingIds.Count + 1; i++)
    {
        if (!existingIds.Contains(i))
            return i;  // Reuse gap
    }

    return existingIds.Max() + 1;
}
```

**Example**:
- Profiles: [1, 2, 4, 5] → Next ID: 3 (fills gap)
- Profiles: [1, 2, 3, 4] → Next ID: 5 (sequential)

### 3. Default Profile Management

**Rules**:
- Only one default profile per type
- Setting new default clears previous default
- Default profiles are read-only (cannot be deleted)

```csharp
public async Task<bool> SetDefaultAsync(int profileId, CancellationToken ct = default)
{
    await _semaphore.WaitAsync(ct);
    try
    {
        // Clear existing default
        var currentDefault = _profiles.FirstOrDefault(p => p.IsDefault);
        if (currentDefault != null)
        {
            currentDefault.IsDefault = false;
        }

        // Set new default
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
            return false;

        profile.IsDefault = true;
        await SaveToFileAsync(ct);

        return true;
    }
    finally
    {
        _semaphore.Release();
    }
}
```

### 4. Clone-on-Return Pattern

**Pattern**: Return clones to prevent external mutation

```csharp
public async Task<T?> GetByIdAsync(int profileId, CancellationToken ct = default)
{
    await _semaphore.WaitAsync(ct);
    try
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        return profile != null ? (T)profile.Clone() : null;  // Clone!
    }
    finally
    {
        _semaphore.Release();
    }
}
```

**Why**: Protects internal collection from external modifications.

### 5. Import/Export Support

**Feature**: Bulk import with validation and conflict handling

```csharp
public async Task<bool> ImportAsync(IEnumerable<T> profiles, CancellationToken ct = default)
{
    await _semaphore.WaitAsync(ct);
    try
    {
        foreach (var profile in profiles)
        {
            // Validate
            ValidateProfile(profile);

            // Ensure unique name (rename if conflict)
            var uniqueName = await EnsureUniqueNameInternalAsync(profile.Name, null, ct);
            profile.Name = uniqueName;

            // Import
            await CreateInternalAsync(profile, ct);
        }

        return true;
    }
    finally
    {
        _semaphore.Release();
    }
}
```

## ViewModel Integration

### ProfileManagementViewModelBase<T>

ViewModels extend a base class implementing common UI patterns:

```csharp
public abstract class ProfileManagementViewModelBase<TProfile> : ViewModelBase
    where TProfile : class, IProfileBase
{
    protected readonly IProfileManager<TProfile> _manager;

    public ObservableCollection<TProfile> Profiles { get; }
    public TProfile? SelectedProfile { get; set; }

    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> EditCommand { get; }
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> SetDefaultCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    // Template methods for subclasses
    protected abstract Task<TProfile?> ShowCreateDialogAsync();
    protected abstract Task<TProfile?> ShowEditDialogAsync(TProfile profile);
    protected abstract TProfile CreateNewProfile();

    protected async Task RefreshProfilesAsync()
    {
        var profiles = await _manager.GetAllAsync();

        // Replace collection (triggers DataGrid refresh)
        Profiles.Clear();
        Profiles.AddRange(profiles);

        // Reselect (preserve user context)
        if (SelectedProfile != null)
        {
            SelectedProfile = profiles.FirstOrDefault(p => p.Id == SelectedProfile.Id);
        }
    }
}
```

## UI Standards

### CRUD Command Order

Consistent button order across all profile views:

```
[Create] [Edit] [Duplicate] [Set Default] [Delete] [Refresh]
```

### Dialog-Only Editing

**Rule**: All Create/Edit/Duplicate operations use dialogs (no inline editing)

**Rationale**:
- Consistent user experience
- Validation before commit
- Cancel without side effects

### DataGrid Columns

**Standard column order**:
1. ID (read-only, auto-generated)
2. Name (required, unique)
3. Description (optional)
4. Options (type-specific data)
5. Flags (type-specific flags)
6. Created (timestamp)
7. Modified (timestamp)
8. IsDefault (boolean indicator)

## Benefits

### 1. Code Reuse

**Before**: ~500 lines per profile type (5 types = 2,500 lines)
**After**: ~100 lines per profile type + 800 lines shared (1,300 lines total)

**Savings**: 48% code reduction

### 2. Consistency

All profile types have:
- ✅ Identical CRUD operations
- ✅ Same validation patterns
- ✅ Consistent error handling
- ✅ Uniform persistence logic

### 3. Thread Safety

- ✅ No race conditions
- ✅ No deadlocks (Internal Method Pattern)
- ✅ Proper cancellation support

### 4. Maintainability

**Changes propagate automatically**:
- Bug fix in `StandardProfileManager<T>` → All profile types fixed
- New feature → Available to all profile types
- Validation enhancement → Consistent across all types

## Anti-Patterns

### ❌ Don't: Bypass the Manager

```csharp
// BAD: Direct file manipulation
public class BadProfileService
{
    public async Task SaveProfile(SerialPortProfile profile)
    {
        var json = JsonSerializer.Serialize(profile);
        await File.WriteAllTextAsync("profile.json", json);  // ❌ No validation, no thread safety
    }
}

// GOOD: Use manager
public class GoodProfileService
{
    private readonly IProfileManager<SerialPortProfile> _manager;

    public async Task SaveProfile(SerialPortProfile profile)
    {
        await _manager.UpdateAsync(profile);  // ✅ Validated, thread-safe
    }
}
```

### ❌ Don't: Mutate Returned Profiles

```csharp
// BAD: Mutate returned profile
var profile = await _manager.GetByIdAsync(1);
profile.Name = "Changed";  // ❌ Doesn't affect stored profile (it's a clone)

// GOOD: Update through manager
var profile = await _manager.GetByIdAsync(1);
profile.Name = "Changed";
await _manager.UpdateAsync(profile);  // ✅ Properly updated
```

### ❌ Don't: Duplicate Manager Logic

```csharp
// BAD: Reimplementing name uniqueness check
public class BadViewModel
{
    public async Task<bool> IsNameUnique(string name)
    {
        var profiles = await _manager.GetAllAsync();
        return !profiles.Any(p => p.Name == name);  // ❌ Duplicate logic
    }
}

// GOOD: Use manager method
public class GoodViewModel
{
    public async Task<bool> IsNameUnique(string name)
    {
        return await _manager.IsNameUniqueAsync(name);  // ✅ Reuse
    }
}
```

## Testing

### Unit Test Example

```csharp
public class StandardProfileManagerTests
{
    [Fact]
    public async Task CreateAsync_Should_Assign_Gap_Filling_Id()
    {
        // Arrange
        var manager = new TestProfileManager();
        await manager.CreateAsync(new TestProfile { Name = "Profile1" });  // ID: 1
        await manager.CreateAsync(new TestProfile { Name = "Profile2" });  // ID: 2
        await manager.CreateAsync(new TestProfile { Name = "Profile3" });  // ID: 3
        await manager.DeleteAsync(2);  // Delete ID 2

        // Act
        var newProfile = await manager.CreateAsync(new TestProfile { Name = "Profile4" });

        // Assert
        Assert.Equal(2, newProfile.Id);  // Reused gap
    }

    [Fact]
    public async Task SetDefaultAsync_Should_Clear_Previous_Default()
    {
        // Arrange
        var manager = new TestProfileManager();
        var profile1 = await manager.CreateAsync(new TestProfile { Name = "P1", IsDefault = true });
        var profile2 = await manager.CreateAsync(new TestProfile { Name = "P2" });

        // Act
        await manager.SetDefaultAsync(profile2.Id);

        // Assert
        var allProfiles = await manager.GetAllAsync();
        Assert.Single(allProfiles.Where(p => p.IsDefault));
        Assert.Equal(profile2.Id, allProfiles.First(p => p.IsDefault).Id);
    }
}
```

## Related Patterns

- [Internal Method Pattern](internal-method.md) - Semaphore deadlock prevention
- [Clean Architecture](../architecture/clean-architecture.md) - Layer separation
- [MVVM Patterns](../architecture/mvvm-patterns.md) - ViewModel integration

## Implementation Files

**Core**:
- `src/S7Tools.Core/Models/IProfileBase.cs` - Interface
- `src/S7Tools.Core/Services/Interfaces/IProfileManager.cs` - Manager contract

**Implementation**:
- `src/S7Tools/Services/StandardProfileManager.cs` - Base implementation
- `src/S7Tools/Services/SerialPortProfileService.cs` - Serial port profiles
- `src/S7Tools/Services/SocatProfileService.cs` - Socat profiles
- `src/S7Tools/Services/PowerSupplyProfileService.cs` - Power supply profiles
- `src/S7Tools/Services/JobProfileService.cs` - Job profiles
- `src/S7Tools/Services/MemoryRegionProfileService.cs` - Memory region profiles

**ViewModels**:
- `src/S7Tools/ViewModels/Base/ProfileManagementViewModelBase.cs` - Base ViewModel
- `src/S7Tools/ViewModels/Profiles/*ProfileViewModel.cs` - Type-specific ViewModels

---

**Last Updated**: 2025-11-10
**Status**: Current implementation standard
**Pattern Maturity**: Production-proven (5 profile types)
