---
title: "Custom Domain Exceptions Pattern"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["pattern", "exceptions", "error-handling", "clean-architecture", "domain-driven"]
related:
  - "docs/patterns/system-patterns.md"
  - "docs/architecture/clean-architecture.md"
  - "docs/patterns/profile-management.md"
supersedes: []
---

# Custom Domain Exceptions Pattern

## Problem Statement

**Context**: S7Tools services throw exceptions when operations fail (profile not found, duplicate names, validation errors, connection failures).

**Problems with Generic Exceptions**:
- **Poor semantics**: `ArgumentException` doesn't indicate *what* failed
- **Loss of context**: No ProfileId, ProfileName, or other relevant data
- **Catch-all handling**: Can't distinguish between different error types
- **Debugging difficulty**: Generic messages provide little diagnostic value

**Example** (generic exceptions):
```csharp
// ❌ BAD: Generic exception with poor semantics
public async Task<T> UpdateAsync(T profile)
{
    var existing = _profiles.FirstOrDefault(p => p.Id == profile.Id);

    if (existing == null)
    {
        throw new ArgumentException($"Profile with ID {profile.Id} not found.");  // ❌ Vague
    }

    if (!existing.CanModify())
    {
        throw new InvalidOperationException("Cannot modify this profile.");  // ❌ No context
    }
}

// ViewModel can't distinguish error types
try
{
    await _manager.UpdateAsync(profile);
}
catch (Exception ex)  // ❌ Catch-all, no specific handling
{
    StatusMessage = $"Error: {ex.Message}";
}
```

## Solution

Use **domain-specific exceptions** that:
1. Clearly indicate the error type (e.g., `ProfileNotFoundException`)
2. Carry contextual data (ProfileId, ProfileName, etc.)
3. Inherit from a common base (`S7ToolsException`) for hierarchy
4. Enable targeted exception handling in ViewModels

## Exception Hierarchy

### Base Exception

```csharp
namespace S7Tools.Core.Exceptions;

/// <summary>
/// Base exception for all S7Tools domain errors
/// </summary>
public class S7ToolsException : Exception
{
    public S7ToolsException(string message) : base(message)
    {
    }

    public S7ToolsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

### Category Exceptions

```csharp
// Profile-related errors
public class ProfileException : S7ToolsException
{
    public int? ProfileId { get; init; }
    public string? ProfileName { get; init; }

    public ProfileException(string message, int? profileId = null, string? profileName = null)
        : base(message)
    {
        ProfileId = profileId;
        ProfileName = profileName;
    }
}

// Connection-related errors
public class ConnectionException : S7ToolsException
{
    public string? Host { get; init; }
    public int? Port { get; init; }

    public ConnectionException(string message, string? host = null, int? port = null)
        : base(message)
    {
        Host = host;
        Port = port;
    }
}

// Validation errors
public class ValidationException : S7ToolsException
{
    public string? PropertyName { get; init; }
    public object? InvalidValue { get; init; }
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    public ValidationException(string propertyName, string message, object? invalidValue = null)
        : base(message)
    {
        PropertyName = propertyName;
        InvalidValue = invalidValue;
    }

    public ValidationException(IEnumerable<string> errors)
        : base("Validation failed.")
    {
        ValidationErrors = errors.ToList();
    }
}

// Configuration errors
public class ConfigurationException : S7ToolsException
{
    public string? SettingKey { get; init; }

    public ConfigurationException(string message, string? settingKey = null)
        : base(message)
    {
        SettingKey = settingKey;
    }
}
```

### Full Hierarchy

```
S7ToolsException (base)
├── ProfileException
│   ├── ProfileNotFoundException
│   ├── DuplicateProfileNameException
│   ├── DefaultProfileDeletionException
│   └── ReadOnlyProfileModificationException
├── ConnectionException
│   ├── NetworkPortInUseException
│   └── SocatConnectionFailedException
├── ValidationException
│   ├── InvalidProfileNameException
│   └── InvalidMemoryRegionException
└── ConfigurationException
    ├── SettingNotFoundException
    └── InvalidSettingValueException
```

## Specific Exception Implementations

### Profile Exceptions

```csharp
public class ProfileNotFoundException : ProfileException
{
    public ProfileNotFoundException(int profileId)
        : base($"Profile with ID {profileId} was not found.", profileId, null)
    {
    }
}

public class DuplicateProfileNameException : ProfileException
{
    public string ExistingName { get; }
    public string DuplicateName { get; }

    public DuplicateProfileNameException(string existingName, string duplicateName)
        : base($"A profile named '{duplicateName}' already exists.", null, duplicateName)
    {
        ExistingName = existingName;
        DuplicateName = duplicateName;
    }
}

public class DefaultProfileDeletionException : ProfileException
{
    public DefaultProfileDeletionException(int profileId, string profileName)
        : base($"Cannot delete default profile '{profileName}' (ID: {profileId}).", profileId, profileName)
    {
    }
}

public class ReadOnlyProfileModificationException : ProfileException
{
    public ReadOnlyProfileModificationException(int profileId, string profileName)
        : base($"Cannot modify read-only profile '{profileName}' (ID: {profileId}).", profileId, profileName)
    {
    }
}
```

### Connection Exceptions

```csharp
public class NetworkPortInUseException : ConnectionException
{
    public NetworkPortInUseException(int port)
        : base($"Network port {port} is already in use.", null, port)
    {
    }
}

public class SocatConnectionFailedException : ConnectionException
{
    public string? ErrorDetails { get; init; }

    public SocatConnectionFailedException(string host, int port, string errorDetails)
        : base($"Socat connection failed: {host}:{port}", host, port)
    {
        ErrorDetails = errorDetails;
    }
}
```

### Validation Exceptions

```csharp
public class InvalidProfileNameException : ValidationException
{
    public InvalidProfileNameException(string name)
        : base("Name", $"Profile name '{name}' is invalid.", name)
    {
    }
}

public class InvalidMemoryRegionException : ValidationException
{
    public int StartAddress { get; }
    public int Length { get; }

    public InvalidMemoryRegionException(int startAddress, int length)
        : base("MemoryRegion",
            $"Invalid memory region: Start={startAddress:X}, Length={length}")
    {
        StartAddress = startAddress;
        Length = length;
    }
}
```

## Usage in Services

### Throwing Exceptions

```csharp
public class StandardProfileManager<T> where T : class, IProfileBase
{
    public async Task<T> UpdateAsync(T profile, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Find existing profile
            var existing = _profiles.FirstOrDefault(p => p.Id == profile.Id);

            // ✅ Specific exception with context
            if (existing == null)
            {
                throw new ProfileNotFoundException(profile.Id);
            }

            // ✅ Read-only check with profile details
            if (!existing.CanModify())
            {
                throw new ReadOnlyProfileModificationException(existing.Id, existing.Name);
            }

            // ✅ Validation with property name
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new ValidationException("Name", "Profile name cannot be empty.");
            }

            // ✅ Duplicate name check
            var duplicate = _profiles.FirstOrDefault(p =>
                p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase) &&
                p.Id != profile.Id);

            if (duplicate != null)
            {
                throw new DuplicateProfileNameException(duplicate.Name, profile.Name);
            }

            // Update profile
            existing.Name = profile.Name;
            existing.Description = profile.Description;
            existing.ModifiedAt = DateTime.UtcNow;

            await SaveToFileAsync(ct).ConfigureAwait(false);

            return (T)existing.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteAsync(int profileId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);

            if (profile == null)
            {
                throw new ProfileNotFoundException(profileId);
            }

            // ✅ Prevent default profile deletion with details
            if (profile.IsDefault)
            {
                throw new DefaultProfileDeletionException(profile.Id, profile.Name);
            }

            _profiles.Remove(profile);
            await SaveToFileAsync(ct).ConfigureAwait(false);

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### Handling Exceptions in ViewModels

```csharp
public class ProfileManagementViewModel : ViewModelBase
{
    private async Task SaveProfileAsync()
    {
        try
        {
            if (_editingProfile == null)
                return;

            StatusMessage = "💾 Saving profile...";

            await _profileManager.UpdateAsync(_editingProfile);

            StatusMessage = "✅ Profile saved successfully";

            // Refresh list
            await RefreshProfilesAsync();
        }
        catch (ProfileNotFoundException ex)
        {
            // Specific handling for profile not found
            StatusMessage = $"❌ Profile not found: ID {ex.ProfileId}";
            _logger.LogWarning(ex, "Profile not found during save: {ProfileId}", ex.ProfileId);
        }
        catch (DuplicateProfileNameException ex)
        {
            // Specific handling for duplicate name
            StatusMessage = $"❌ Name '{ex.DuplicateName}' already exists";
            _logger.LogWarning(ex, "Duplicate profile name: {Name}", ex.DuplicateName);

            // Re-enable editing to fix the name
            IsEditing = true;
        }
        catch (ReadOnlyProfileModificationException ex)
        {
            // Specific handling for read-only profile
            StatusMessage = $"❌ Cannot modify read-only profile '{ex.ProfileName}'";
            _logger.LogWarning(ex, "Attempted to modify read-only profile: {ProfileId}", ex.ProfileId);
        }
        catch (ValidationException ex)
        {
            // Specific handling for validation errors
            if (ex.ValidationErrors.Any())
            {
                StatusMessage = $"❌ Validation failed: {string.Join(", ", ex.ValidationErrors)}";
            }
            else
            {
                StatusMessage = $"❌ {ex.PropertyName}: {ex.Message}";
            }

            _logger.LogWarning(ex, "Validation failed for property: {PropertyName}", ex.PropertyName);
        }
        catch (ProfileException ex)
        {
            // Catch-all for other profile exceptions
            StatusMessage = $"❌ Profile error: {ex.Message}";
            _logger.LogError(ex, "Profile operation failed");
        }
        catch (S7ToolsException ex)
        {
            // Catch-all for other domain exceptions
            StatusMessage = $"❌ Operation failed: {ex.Message}";
            _logger.LogError(ex, "S7Tools operation failed");
        }
        catch (Exception ex)
        {
            // Unexpected exceptions
            StatusMessage = $"❌ Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error during profile save");
        }
    }
}
```

## Benefits

### 1. Clear Error Semantics

**Before** (generic exceptions):
```csharp
throw new ArgumentException("Invalid profile");  // ❌ What's invalid?
```

**After** (domain exceptions):
```csharp
throw new DuplicateProfileNameException(existing.Name, profile.Name);  // ✅ Clear!
```

### 2. Contextual Information

Exception properties provide debugging context:
```csharp
catch (ProfileNotFoundException ex)
{
    _logger.LogWarning("Profile {ProfileId} not found", ex.ProfileId);  // ✅ Structured logging
    StatusMessage = $"Profile #{ex.ProfileId} not found";  // ✅ User-friendly message
}
```

### 3. Targeted Exception Handling

```csharp
// ✅ Different handling for different exceptions
catch (ProfileNotFoundException ex) { /* Refresh list */ }
catch (DuplicateProfileNameException ex) { /* Re-enable editing */ }
catch (ReadOnlyProfileModificationException ex) { /* Show warning */ }
```

### 4. Type Safety

```csharp
// ✅ Compiler enforces exception type
var exception = await Assert.ThrowsAsync<ProfileNotFoundException>(
    () => manager.UpdateAsync(profile)
);
```

### 5. Clean Architecture Compliance

Exceptions defined in **Core** layer (`S7Tools.Core/Exceptions/`):
- ✅ No external dependencies
- ✅ Available to all layers (Infrastructure, Application)
- ✅ Domain-driven design

## Testing

### Test Specific Exception Types

```csharp
[Fact]
public async Task UpdateAsync_ProfileNotFound_ThrowsProfileNotFoundException()
{
    // Arrange
    var manager = new StandardProfileManager<TestProfile>();
    var profile = new TestProfile { Id = 999, Name = "Test" };

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ProfileNotFoundException>(
        () => manager.UpdateAsync(profile)
    );

    Assert.Equal(999, exception.ProfileId);
}

[Fact]
public async Task UpdateAsync_DuplicateName_ThrowsDuplicateProfileNameException()
{
    // Arrange
    var manager = new StandardProfileManager<TestProfile>();
    await manager.CreateAsync(new TestProfile { Name = "Existing" });

    var profile = new TestProfile { Id = 1, Name = "Existing" };

    // Act & Assert
    var exception = await Assert.ThrowsAsync<DuplicateProfileNameException>(
        () => manager.UpdateAsync(profile)
    );

    Assert.Equal("Existing", exception.DuplicateName);
}

[Fact]
public async Task DeleteAsync_DefaultProfile_ThrowsDefaultProfileDeletionException()
{
    // Arrange
    var manager = new StandardProfileManager<TestProfile>();
    var profile = await manager.CreateAsync(new TestProfile
    {
        Name = "Default",
        IsDefault = true
    });

    // Act & Assert
    var exception = await Assert.ThrowsAsync<DefaultProfileDeletionException>(
        () => manager.DeleteAsync(profile.Id)
    );

    Assert.Equal(profile.Id, exception.ProfileId);
    Assert.Equal("Default", exception.ProfileName);
}
```

### Test Exception Context

```csharp
[Fact]
public void ProfileNotFoundException_Should_Include_ProfileId()
{
    // Arrange
    int profileId = 42;

    // Act
    var exception = new ProfileNotFoundException(profileId);

    // Assert
    Assert.Equal(profileId, exception.ProfileId);
    Assert.Contains("42", exception.Message);
}

[Fact]
public void ValidationException_Should_Include_PropertyName_And_Value()
{
    // Arrange
    var propertyName = "Name";
    var invalidValue = "";

    // Act
    var exception = new ValidationException(propertyName, "Name is required", invalidValue);

    // Assert
    Assert.Equal(propertyName, exception.PropertyName);
    Assert.Equal(invalidValue, exception.InvalidValue);
}
```

## Anti-Patterns

### ❌ Don't: Use Generic Exceptions

```csharp
// BAD: No context, poor semantics
public async Task UpdateAsync(T profile)
{
    if (existingProfile == null)
    {
        throw new ArgumentException($"Profile {profile.Id} not found");  // ❌
    }
}
```

**Fix**: Use domain-specific exception:
```csharp
// GOOD: Clear semantics, context included
if (existingProfile == null)
{
    throw new ProfileNotFoundException(profile.Id);  // ✅
}
```

### ❌ Don't: Swallow Exceptions

```csharp
// BAD: Exception information lost
public async Task SaveAsync()
{
    try
    {
        await _manager.UpdateAsync(profile);
    }
    catch (Exception)  // ❌ Swallowed
    {
        return;
    }
}
```

**Fix**: Log and rethrow or handle specifically:
```csharp
// GOOD: Log before handling
public async Task SaveAsync()
{
    try
    {
        await _manager.UpdateAsync(profile);
    }
    catch (ProfileNotFoundException ex)
    {
        _logger.LogWarning(ex, "Profile not found: {ProfileId}", ex.ProfileId);
        StatusMessage = $"Profile {ex.ProfileId} not found";
    }
}
```

### ❌ Don't: Catch Base Exception Too Early

```csharp
// BAD: Catches all exceptions, prevents specific handling
public async Task SaveAsync()
{
    try
    {
        await _manager.UpdateAsync(profile);
    }
    catch (S7ToolsException ex)  // ❌ Too broad
    {
        StatusMessage = "Error occurred";
    }
}
```

**Fix**: Catch specific exceptions first:
```csharp
// GOOD: Specific handling before catch-all
public async Task SaveAsync()
{
    try
    {
        await _manager.UpdateAsync(profile);
    }
    catch (ProfileNotFoundException ex) { /* specific */ }
    catch (DuplicateProfileNameException ex) { /* specific */ }
    catch (S7ToolsException ex) { /* catch-all */ }
}
```

## Related Patterns

- [Profile Management](profile-management.md) - Uses custom exceptions extensively
- [Clean Architecture](../architecture/clean-architecture.md) - Exception layer organization
- [Internal Method Pattern](internal-method.md) - Exception handling in semaphore-protected code

## Implementation Files

**Core Exceptions** (`S7Tools.Core/Exceptions/`):
- `S7ToolsException.cs` - Base exception
- `ProfileException.cs` - Profile category exception
- `ProfileNotFoundException.cs` - Specific profile exception
- `DuplicateProfileNameException.cs` - Duplicate name exception
- `DefaultProfileDeletionException.cs` - Default profile deletion exception
- `ReadOnlyProfileModificationException.cs` - Read-only modification exception
- `ConnectionException.cs` - Connection category exception
- `ValidationException.cs` - Validation category exception
- `ConfigurationException.cs` - Configuration category exception

**Usage Examples**:
- `src/S7Tools/Services/StandardProfileManager.cs` - Throws profile exceptions
- `src/S7Tools/ViewModels/Profiles/*ViewModel.cs` - Handles exceptions with specific catch blocks

---

**Last Updated**: 2025-11-10
**Status**: Current implementation standard
**Test Coverage**: 6 exception tests (100% coverage of exception constructors)

## Related Documentation

- [Index](../INDEX.md)
- [Clean Architecture](../architecture/clean-architecture.md)
- [_Index](_index.md)
- [Internal Method](internal-method.md)
- [Profile Management](profile-management.md)
- [System Patterns](system-patterns.md)
- [_Index](../reviews/_index.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
