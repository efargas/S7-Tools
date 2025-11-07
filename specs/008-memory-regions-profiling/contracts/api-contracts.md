# API Contracts: Memory Regions Profiling System

**Feature**: 008-memory-regions-profiling
**Date**: 2025-11-07

## Public API Contracts

### Profile Management API

**Endpoint Pattern**: `IMemoryRegionProfileService`

**CRUD Operations**:
```csharp
// Create new profile
Task<MemoryRegionProfile> CreateAsync(MemoryRegionProfile profile);

// Get all profiles
Task<IEnumerable<MemoryRegionProfile>> GetAllAsync();

// Get profile by ID
Task<MemoryRegionProfile?> GetByIdAsync(Guid id);

// Update existing profile
Task<MemoryRegionProfile> UpdateAsync(MemoryRegionProfile profile);

// Delete profile
Task<bool> DeleteAsync(Guid id);

// Duplicate profile
Task<MemoryRegionProfile> DuplicateAsync(Guid sourceId, string newName);
```

**Specialized Operations**:
```csharp
// Template-based creation
Task<MemoryRegionProfile> CreateFromTemplateAsync(string templateName, string profileName);

// Validation
Task<ValidationResult> ValidateProfileAsync(MemoryRegionProfile profile);

// Segment selection
Task<MemoryRegionProfile> UpdateSegmentSelectionAsync(Guid profileId, string segmentName, bool isSelected);

// Export/Import
Task<ExportResult> ExportProfileAsync(Guid profileId, string filePath, ExportFormat format);
Task<MemoryRegionProfile> ImportProfileAsync(string filePath, ExportFormat format);
```

### Memory Segment Validation API

**Endpoint Pattern**: `IMemorySegmentValidator`

**Validation Operations**:
```csharp
// Segment collection validation
ValidationResult ValidateSegments(IEnumerable<MemorySegment> segments);

// Individual segment validation
ValidationResult ValidateSegment(MemorySegment segment);

// Address validation
bool IsValidAddress(string address);
long ParseAddress(string address);

// Overlap detection
bool DoSegmentsOverlap(MemorySegment segment1, MemorySegment segment2);
IEnumerable<(MemorySegment First, MemorySegment Second)> FindOverlappingSegments(IEnumerable<MemorySegment> segments);
```

### UI Integration Contracts

**ViewModel Contracts**:

**MemoryRegionProfilesViewModel (Pages Category)**:
```csharp
// Profile collection
ObservableCollection<MemoryRegionProfile> Profiles { get; }
MemoryRegionProfile? SelectedProfile { get; set; }

// Commands
ReactiveCommand<Unit, Unit> CreateProfileCommand { get; }
ReactiveCommand<Unit, Unit> EditProfileCommand { get; }
ReactiveCommand<Unit, Unit> DuplicateProfileCommand { get; }
ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; }

// Computed properties
bool HasSelection { get; }
string StatusMessage { get; set; }
bool IsLoading { get; set; }
```

**Dialog ViewModels (Dialogs Category)**:
```csharp
// CreateMemoryRegionProfileViewModel
string ProfileName { get; set; }
string Description { get; set; }
string SelectedTemplate { get; set; }
ObservableCollection<MemoryRegionTemplate> AvailableTemplates { get; }
ReactiveCommand<Unit, bool> SaveCommand { get; }

// EditMemoryRegionProfileViewModel
MemoryRegionProfile Profile { get; set; }
ObservableCollection<MemorySegment> Segments { get; }
ReactiveCommand<Unit, bool> SaveCommand { get; }

// DuplicateMemoryRegionProfileViewModel
string SourceProfileName { get; }
string NewProfileName { get; set; }
ReactiveCommand<Unit, bool> DuplicateCommand { get; }
```

### Job Wizard Integration

**Job Wizard Step Contract**:
```csharp
public class JobWizardMemoryRegionStepViewModel : StepViewModel
{
    // Profile selection
    ObservableCollection<MemoryRegionProfile> AvailableProfiles { get; }
    MemoryRegionProfile? SelectedProfile { get; set; }

    // Segment display
    ObservableCollection<MemorySegment> SelectedSegments { get; }

    // Validation
    protected override bool ValidateStep() =>
        SelectedProfile != null &&
        SelectedProfile.Segments.Any(s => s.IsSelected);
}
```

### Menu Integration Contract

**Menu Command Contract**:
```csharp
public class MainMenuViewModel : ReactiveObject
{
    // Navigation command
    ReactiveCommand<Unit, Unit> NavigateToMemoryRegionProfiles { get; }

    // Implementation
    NavigateToMemoryRegionProfiles = ReactiveCommand.CreateFromTask(async () =>
    {
        await _navigationService.NavigateToAsync<MemoryRegionProfilesViewModel>();
    });
}
```

### Storage Contract

**File Storage Format**:
```json
{
  "version": "1.0.0",
  "profiles": [
    {
      "id": "12345678-1234-1234-1234-123456789abc",
      "name": "STM32F4 Memory Map",
      "description": "Standard memory layout for STM32F4 series",
      "createdAt": "2025-11-07T10:00:00Z",
      "updatedAt": "2025-11-07T10:00:00Z",
      "isActive": true,
      "segments": [
        {
          "name": ".text",
          "startAddress": "0x08000000",
          "size": 131072,
          "type": 0,
          "isSelected": true,
          "description": "Program code section"
        }
      ]
    }
  ]
}
```

**Configuration Contract**:
```csharp
public class MemoryRegionProfileOptions
{
    public string ProfilesFilePath { get; set; } = "src/resources/MemoryRegionProfiles/profiles.json";
    public string DefaultProfileName { get; set; } = "Default Memory Regions";
    public int MaxProfileCount { get; set; } = 100;
    public bool ValidationEnabled { get; set; } = true;
    public bool AutoSelectBssSegment { get; set; } = true;
}
```

### Error Handling Contract

**Exception Hierarchy**:
```csharp
// Base exception
public class MemoryRegionException : Exception

// Specific exceptions
public class InvalidMemorySegmentException : MemoryRegionException
public class OverlappingMemorySegmentsException : MemoryRegionException
public class InvalidMemoryAddressException : MemoryRegionException
public class MemoryRegionProfileNotFoundException : MemoryRegionException
public class DuplicateMemoryRegionProfileNameException : MemoryRegionException
```

**Error Response Format**:
```csharp
public class MemoryRegionOperationResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public List<ValidationError>? ValidationErrors { get; set; }
}
```

## Integration Points

### Dependency Injection

**Service Registration**:
```csharp
// ServiceCollectionExtensions.cs
public static IServiceCollection AddMemoryRegionProfilingServices(this IServiceCollection services)
{
    services.TryAddSingleton<IMemoryRegionProfileService, MemoryRegionProfileService>();
    services.TryAddSingleton<IMemorySegmentValidator, MemorySegmentValidator>();

    services.Configure<MemoryRegionProfileOptions>(configuration.GetSection("MemoryRegionProfiles"));

    return services;
}
```

### Job System Integration

**JobProfile Extension**:
```csharp
public class JobProfile
{
    // Existing fields...
    public Guid? MemoryRegionProfileId { get; set; }

    [JsonIgnore]
    public MemoryRegionProfile? MemoryRegionProfile { get; set; }
}
```

### Settings Integration

**AppSettings Extension**:
```json
{
  "MemoryRegionProfiles": {
    "ProfilesFilePath": "src/resources/MemoryRegionProfiles/profiles.json",
    "DefaultProfileName": "Default Memory Regions",
    "MaxProfileCount": 100,
    "ValidationEnabled": true,
    "AutoSelectBssSegment": true
  }
}
```

## Versioning and Compatibility

### Schema Version

**Current Version**: 1.0.0
**Compatibility**: Forward compatible within major version

**Migration Strategy**:
- Minor version changes: Additive only, no breaking changes
- Major version changes: Include migration logic in service
- Backup original file before migration
- Support rollback to previous version

### Breaking Change Policy

**Major Version Triggers**:
- Changes to core MemoryRegionProfile structure
- Removal of public API methods
- Changes to storage format that require migration

**Minor Version Triggers**:
- Addition of new optional fields
- New public API methods
- Enhanced validation rules (non-breaking)

**Patch Version Triggers**:
- Bug fixes
- Performance improvements
- Documentation updates
