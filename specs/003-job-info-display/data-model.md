# Data Model: Enhanced Job Information Display

**Generated**: 2025-10-21
**Context**: Phase 1 data model for job information display implementation

## Entities Overview

This feature leverages existing domain entities from S7Tools.Core without modification. New ViewModels and supporting classes are added to the UI layer for display purposes only.

## Existing Entities (No Changes)

### JobProfile
**Location**: `S7Tools.Core/Models/JobProfile.cs`
**Purpose**: Represents a complete job configuration with references to all selected profiles

**Key Properties**:
- `Id: Guid` - Unique identifier
- `Name: string` - Job display name
- `Description: string` - Job description
- `CreatedAt: DateTime` - Creation timestamp
- `SerialProfileId: Guid?` - Reference to selected serial profile
- `SocatProfileId: Guid?` - Reference to selected socat profile
- `PowerSupplyProfileId: Guid?` - Reference to selected power supply profile
- `MemoryRegionProfileId: Guid?` - Reference to selected memory region profile

**Relationships**:
- References profile entities via nullable Guid foreign keys
- Managed by `IJobProfileService` using `StandardProfileManager<JobProfile>` pattern

### Profile Entities

#### SerialPortProfile
**Location**: `S7Tools.Core/Models/SerialPortProfile.cs`
**Key Properties**:
- `BaudRate: int` - Communication speed
- `Parity: Parity` - Parity setting
- `StopBits: StopBits` - Stop bits configuration
- `FlowControl: FlowControl` - Flow control type
- `DataBits: int` - Data bits count
- `PortName: string` - Serial port identifier

#### SocatProfile
**Location**: `S7Tools.Core/Models/SocatProfile.cs`
**Key Properties**:
- `TcpPort: int` - TCP port number
- `Host: string` - Target host address
- `Flags: string` - Socat command flags
- `Options: string` - Additional socat options

#### PowerSupplyProfile
**Location**: `S7Tools.Core/Models/PowerSupplyProfile.cs`
**Key Properties**:
- `Host: string` - Power supply host
- `Port: int` - Communication port
- `DeviceId: string` - Device identifier
- `Protocol: string` - Communication protocol
- `Timeout: TimeSpan` - Communication timeout

#### MemoryRegionProfile
**Location**: `S7Tools.Core/Models/MemoryRegionProfile.cs`
**Key Properties**:
- `StartAddress: uint` - Memory start address
- `Length: uint` - Memory region length
- `Regions: List<MemoryRegion>` - Defined memory regions

## New UI Layer Entities

### JobInfoDisplayViewModel
**Location**: `S7Tools/ViewModels/Jobs/JobInfoDisplayViewModel.cs`
**Purpose**: ViewModel for displaying complete job information in main jobs view

**Properties**:
- `SelectedJob: JobProfile?` - Currently selected job
- `JobBasicInfo: string` - Formatted basic job information
- `SerialProfileDetails: IProfileDetailsViewModel?` - Serial profile display data
- `SocatProfileDetails: IProfileDetailsViewModel?` - Socat profile display data
- `PowerSupplyProfileDetails: IProfileDetailsViewModel?` - Power supply profile display data
- `MemoryRegionProfileDetails: IProfileDetailsViewModel?` - Memory region profile display data
- `HasMissingProfiles: bool` - True if any referenced profiles are missing
- `MissingProfileWarnings: ObservableCollection<string>` - List of missing profile messages

**State Transitions**:
1. `No Selection` → Job selected → `Display Job Details`
2. `Display Job Details` → Different job selected → `Update Display`
3. `Display Job Details` → Job deselected → `No Selection`

**Validation Rules**:
- Handle null/missing profile references gracefully
- Display warning messages for corrupted profile data
- Validate profile service responses

### IProfileDetailsViewModel
**Location**: `S7Tools/ViewModels/Profiles/IProfileDetailsViewModel.cs`
**Purpose**: Interface for consistent profile details display across all profile types

**Properties**:
- `ProfileName: string` - Display name of the profile
- `ProfileType: string` - Type description (e.g., "Serial Port", "Socat Bridge")
- `BasicProperties: ObservableCollection<PropertyDisplayItem>` - Basic configuration items
- `ConfigurationProperties: ObservableCollection<PropertyDisplayItem>` - Main configuration items
- `AdvancedProperties: ObservableCollection<PropertyDisplayItem>` - Advanced/optional items
- `IsValid: bool` - True if profile data is valid and complete
- `ValidationMessage: string?` - Error/warning message if profile is invalid

### PropertyDisplayItem
**Location**: `S7Tools/ViewModels/Profiles/PropertyDisplayItem.cs`
**Purpose**: Represents a single profile property for display

**Properties**:
- `Label: string` - Human-readable property name
- `Value: string` - Formatted property value
- `Tooltip: string?` - Additional information for complex properties
- `IsHighlighted: bool` - True for important properties
- `ValidationState: PropertyValidationState` - Valid/Warning/Error state

**Validation Rules**:
- `Label` must not be null or empty
- `Value` should handle null source values gracefully
- `ValidationState` determines display styling

### ProfileDetailsService
**Location**: `S7Tools/Services/ProfileDetailsService.cs`
**Purpose**: Service for formatting profile data into display ViewModels

**Key Methods**:
- `CreateProfileDetailsViewModel<T>(T profile) where T : IProfileBase` - Factory method for profile ViewModels
- `FormatPropertyValue(object value, Type propertyType)` - Consistent value formatting
- `ValidateProfileData<T>(T profile)` - Profile validation and error detection

**Business Rules**:
- Use consistent formatting for similar property types (e.g., TimeSpan, IP addresses)
- Group properties logically (Basic/Configuration/Advanced)
- Handle enum values with human-readable descriptions
- Support localization for property labels

## State Management

### Profile Loading State
```
Not Loaded → Loading → Loaded (with data)
                   → Error (with message)
                   → Missing (profile not found)
```

### Job Selection State
```
No Job Selected → Job Selected → Profile Details Loading → Profile Details Loaded
                             → Profile Load Error → Show Warning
```

### Validation State
```
Unknown → Validating → Valid
                   → Invalid (with specific errors)
                   → Warning (with recommendations)
```

## Error Handling Strategy

### Missing Profile References
- **Scenario**: JobProfile references a profile ID that no longer exists
- **Handling**: Display warning with profile type and last known name
- **User Action**: Option to remove reference or select replacement profile

### Corrupted Profile Data
- **Scenario**: Profile exists but contains invalid data
- **Handling**: Display partial information with validation warnings
- **User Action**: Option to edit profile or view raw data

### Service Unavailable
- **Scenario**: Profile service throws exception during loading
- **Handling**: Log error and display generic "Unable to load" message
- **User Action**: Retry option or fallback to basic job information

## Performance Considerations

### Lazy Loading Strategy
- Profile details loaded only when job is selected
- Cache formatted display data until profile changes
- Use weak references to allow garbage collection

### Memory Management
- Dispose of ViewModels when jobs are deselected
- Limit cached profile details to reasonable number (e.g., 50 most recent)
- Clear cache when profile services report data changes

### Update Optimization
- Use ReactiveUI observables to minimize unnecessary UI updates
- Batch property changes during profile loading
- Debounce rapid selection changes to prevent thrashing

## Integration Points

### Existing Services
- `IJobProfileService` - Job data access
- `ISerialPortProfileService` - Serial profile data
- `ISocatProfileService` - Socat profile data
- `IPowerSupplyProfileService` - Power supply profile data
- `IMemoryRegionProfileService` - Memory region profile data

### UI Integration
- Job list selection binding to `JobInfoDisplayViewModel.SelectedJob`
- Wizard step ViewModels enhanced with profile details display
- Consistent styling across all profile detail displays

### Testing Integration
- Unit tests for all new ViewModels and services
- Mock profile services for predictable test scenarios
- Performance tests for large datasets and rapid selection changes
