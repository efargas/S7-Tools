---
title: "Profile Manager Pattern - Example"
created: "2025-11-10"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - example
  - pattern
  - profile-management
  - code
related:
  - docs/patterns/profile-management.md
  - docs/templates/service-template.md
---

# Profile Manager Pattern - Example

Concrete example demonstrating the `StandardProfileManager<T>` pattern for managing profiles in S7Tools.

## Overview

This example shows how to:
- Extend `StandardProfileManager<T>` for a custom profile type
- Implement template methods for type-specific behavior
- Register the service with dependency injection
- Use the profile manager in ViewModels

## Example Code

### Step 1: Define Profile Model

```csharp
// File: src/S7Tools.Core/Models/CustomProfile.cs
using System.ComponentModel;
using S7Tools.Core.Interfaces;

namespace S7Tools.Core.Models;

/// <summary>
/// Custom profile with validation and default values.
/// </summary>
public class CustomProfile : IProfileBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Profile";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Custom properties
    [DisplayName("Server URL")]
    public string ServerUrl { get; set; } = "https://localhost:8080";

    [DisplayName("Timeout (seconds)")]
    public int TimeoutSeconds { get; set; } = 30;

    [Browsable(false)]
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

### Step 2: Implement Profile Manager Service

```csharp
// File: src/S7Tools/Services/CustomProfileService.cs
using S7Tools.Core.Models;
using S7Tools.Core.Interfaces;
using S7Tools.Services.Profiles;

namespace S7Tools.Services;

/// <summary>
/// Service for managing Custom profiles using StandardProfileManager pattern.
/// </summary>
public class CustomProfileService : StandardProfileManager<CustomProfile>, ICustomProfileService
{
    public CustomProfileService(
        ILoggingService loggingService,
        IPathService pathService,
        ISettingsService settingsService)
        : base(loggingService, pathService, settingsService, "profiles.custom")
    {
    }

    /// <summary>
    /// Template method: Create default profile with custom initial values.
    /// </summary>
    protected override CustomProfile CreateDefaultProfile()
    {
        return new CustomProfile
        {
            Name = "Default Custom Profile",
            ServerUrl = "https://api.example.com",
            TimeoutSeconds = 60,
            Metadata = new Dictionary<string, string>
            {
                { "Environment", "Development" },
                { "Version", "1.0.0" }
            }
        };
    }

    /// <summary>
    /// Template method: Validate profile before save (optional override).
    /// </summary>
    protected override async Task<bool> ValidateProfileAsync(CustomProfile profile)
    {
        // Custom validation logic
        if (string.IsNullOrWhiteSpace(profile.ServerUrl))
        {
            LoggingService.LogWarning("CustomProfile: ServerUrl cannot be empty");
            return false;
        }

        if (profile.TimeoutSeconds < 1 || profile.TimeoutSeconds > 300)
        {
            LoggingService.LogWarning("CustomProfile: Timeout must be between 1-300 seconds");
            return false;
        }

        // Call base validation (checks for duplicate names, etc.)
        return await base.ValidateProfileAsync(profile);
    }
}
```

### Step 3: Define Interface

```csharp
// File: src/S7Tools.Core/Interfaces/ICustomProfileService.cs
using S7Tools.Core.Models;

namespace S7Tools.Core.Interfaces;

/// <summary>
/// Service interface for Custom profile management.
/// </summary>
public interface ICustomProfileService : IProfileManager<CustomProfile>
{
    // Inherits all CRUD operations from IProfileManager<T>:
    // - Task<IEnumerable<CustomProfile>> GetAllAsync()
    // - Task<CustomProfile?> GetByIdAsync(Guid id)
    // - Task<bool> SaveAsync(CustomProfile profile)
    // - Task<bool> DeleteAsync(Guid id)
    // - Task<CustomProfile> CreateNewAsync()
    // - etc.
}
```

### Step 4: Register Service

```csharp
// File: src/S7Tools/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddS7ToolsProfileServices(this IServiceCollection services)
{
    // ... other profile services ...

    services.TryAddSingleton<ICustomProfileService, CustomProfileService>();

    return services;
}
```

### Step 5: Use in ViewModel

```csharp
// File: src/S7Tools/ViewModels/Pages/CustomProfilesViewModel.cs
using System.Collections.ObjectModel;
using ReactiveUI;
using S7Tools.Core.Interfaces;
using S7Tools.Core.Models;

namespace S7Tools.ViewModels.Pages;

public class CustomProfilesViewModel : ReactiveObject, IDisposable
{
    private readonly ICustomProfileService _profileService;
    private readonly CompositeDisposable _disposables = new();

    public ObservableCollection<CustomProfile> Profiles { get; } = new();

    private CustomProfile? _selectedProfile;
    public CustomProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    public CustomProfilesViewModel(ICustomProfileService profileService)
    {
        _profileService = profileService;

        // Commands
        CreateCommand = ReactiveCommand.CreateFromTask(CreateNewProfileAsync);
        CreateCommand.DisposeWith(_disposables);

        var canSave = this.WhenAnyValue(x => x.SelectedProfile, p => p != null);
        SaveCommand = ReactiveCommand.CreateFromTask(SaveProfileAsync, canSave);
        SaveCommand.DisposeWith(_disposables);

        var canDelete = this.WhenAnyValue(x => x.SelectedProfile, p => p != null);
        DeleteCommand = ReactiveCommand.CreateFromTask(DeleteProfileAsync, canDelete);
        DeleteCommand.DisposeWith(_disposables);

        // Initial load
        _ = LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        var profiles = await _profileService.GetAllAsync();
        Profiles.Clear();
        foreach (var profile in profiles)
        {
            Profiles.Add(profile);
        }
    }

    private async Task CreateNewProfileAsync()
    {
        var newProfile = await _profileService.CreateNewAsync();
        await LoadProfilesAsync();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == newProfile.Id);
    }

    private async Task SaveProfileAsync()
    {
        if (SelectedProfile == null) return;

        var success = await _profileService.SaveAsync(SelectedProfile);
        if (success)
        {
            await LoadProfilesAsync();
        }
    }

    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null) return;

        var success = await _profileService.DeleteAsync(SelectedProfile.Id);
        if (success)
        {
            await LoadProfilesAsync();
            SelectedProfile = Profiles.FirstOrDefault();
        }
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}
```

## Key Benefits

### Unified Pattern
All profile types (Serial, Socat, PowerSupply, Job, Custom) use the same `StandardProfileManager<T>` base:
- Consistent CRUD operations
- Standardized validation
- Unified error handling
- Common persistence mechanism

### Template Method Pattern
Override only what's unique to your profile type:
```csharp
protected override CustomProfile CreateDefaultProfile() { /* custom defaults */ }
protected override Task<bool> ValidateProfileAsync(CustomProfile profile) { /* custom validation */ }
```

### Type Safety
Generic type parameter ensures compile-time type checking:
```csharp
ICustomProfileService service;  // Works with CustomProfile only
var profile = await service.GetByIdAsync(id);  // Returns CustomProfile, not object
```

## Related Patterns

- [Profile Management Pattern](../profile-management.md) - Full pattern documentation
- [Service Registration Pattern](../service-registration.md) - DI registration
- [Internal Method Pattern](../internal-method.md) - Thread-safe operations

## See Also

- [ViewModel Template](../../templates/viewmodel-template.md)
- [Service Template](../../templates/service-template.md)
- [Development Workflow](../../guides/development-workflow.md)

## Related Documentation

- [Profile Management](../profile-management.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
