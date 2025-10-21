# API Contracts: Enhanced Job Information Display

**Generated**: 2025-10-21
**Context**: Phase 1 API contracts for job information display implementation

## Overview

This document defines the internal API contracts for the Enhanced Job Information Display feature. Since this is a desktop application using MVVM, these are not HTTP APIs but rather interfaces and contracts between UI and business logic layers.

## Service Contracts

### IProfileDetailsService

```csharp
namespace S7Tools.Services
{
    /// <summary>
    /// Service for creating formatted profile detail ViewModels
    /// </summary>
    public interface IProfileDetailsService
    {
        /// <summary>
        /// Creates a profile details ViewModel for the specified profile
        /// </summary>
        /// <typeparam name="T">Profile type implementing IProfileBase</typeparam>
        /// <param name="profile">Profile to create details for</param>
        /// <returns>Profile details ViewModel or null if profile is null</returns>
        IProfileDetailsViewModel? CreateProfileDetailsViewModel<T>(T? profile)
            where T : class, IProfileBase;

        /// <summary>
        /// Creates profile details ViewModel with error handling for missing profiles
        /// </summary>
        /// <param name="profileId">Profile ID to load</param>
        /// <param name="profileType">Type of profile expected</param>
        /// <param name="profileService">Service to load profile data</param>
        /// <returns>Profile details ViewModel with error state if loading fails</returns>
        Task<IProfileDetailsViewModel> CreateProfileDetailsViewModelAsync<T>(
            Guid? profileId,
            string profileType,
            IProfileService<T> profileService)
            where T : class, IProfileBase;

        /// <summary>
        /// Formats a property value for display with appropriate type handling
        /// </summary>
        /// <param name="value">Property value to format</param>
        /// <param name="propertyType">Type of the property</param>
        /// <returns>Formatted string representation</returns>
        string FormatPropertyValue(object? value, Type propertyType);

        /// <summary>
        /// Validates profile data and returns validation state
        /// </summary>
        /// <typeparam name="T">Profile type</typeparam>
        /// <param name="profile">Profile to validate</param>
        /// <returns>Validation result with state and messages</returns>
        ProfileValidationResult ValidateProfileData<T>(T profile)
            where T : class, IProfileBase;
    }
}
```

### IProfileDetailsViewModel

```csharp
namespace S7Tools.ViewModels.Profiles
{
    /// <summary>
    /// Interface for profile details display ViewModels
    /// </summary>
    public interface IProfileDetailsViewModel : IDisposable
    {
        /// <summary>
        /// Display name of the profile
        /// </summary>
        string ProfileName { get; }

        /// <summary>
        /// Human-readable profile type (e.g., "Serial Port", "Socat Bridge")
        /// </summary>
        string ProfileType { get; }

        /// <summary>
        /// Basic configuration properties for display
        /// </summary>
        ObservableCollection<PropertyDisplayItem> BasicProperties { get; }

        /// <summary>
        /// Main configuration properties for display
        /// </summary>
        ObservableCollection<PropertyDisplayItem> ConfigurationProperties { get; }

        /// <summary>
        /// Advanced/optional properties for display
        /// </summary>
        ObservableCollection<PropertyDisplayItem> AdvancedProperties { get; }

        /// <summary>
        /// True if profile data is valid and complete
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Error or warning message if profile is invalid
        /// </summary>
        string? ValidationMessage { get; }

        /// <summary>
        /// True if this represents a missing/deleted profile
        /// </summary>
        bool IsMissing { get; }

        /// <summary>
        /// Updates the profile data and refreshes display properties
        /// </summary>
        /// <param name="profile">New profile data</param>
        Task UpdateProfileAsync(IProfileBase? profile);
    }
}
```

## Data Transfer Objects

### PropertyDisplayItem

```csharp
namespace S7Tools.ViewModels.Profiles
{
    /// <summary>
    /// Represents a single profile property for display in the UI
    /// </summary>
    public class PropertyDisplayItem : ReactiveObject
    {
        private string _label = string.Empty;
        private string _value = string.Empty;
        private string? _tooltip;
        private bool _isHighlighted;
        private PropertyValidationState _validationState;

        /// <summary>
        /// Human-readable property name/label
        /// </summary>
        public string Label
        {
            get => _label;
            set => this.RaiseAndSetIfChanged(ref _label, value);
        }

        /// <summary>
        /// Formatted property value for display
        /// </summary>
        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }

        /// <summary>
        /// Optional tooltip with additional information
        /// </summary>
        public string? Tooltip
        {
            get => _tooltip;
            set => this.RaiseAndSetIfChanged(ref _tooltip, value);
        }

        /// <summary>
        /// True for important properties that should be emphasized
        /// </summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => this.RaiseAndSetIfChanged(ref _isHighlighted, value);
        }

        /// <summary>
        /// Validation state for visual styling
        /// </summary>
        public PropertyValidationState ValidationState
        {
            get => _validationState;
            set => this.RaiseAndSetIfChanged(ref _validationState, value);
        }
    }

    /// <summary>
    /// Validation state for property display items
    /// </summary>
    public enum PropertyValidationState
    {
        Valid,
        Warning,
        Error
    }
}
```

### ProfileValidationResult

```csharp
namespace S7Tools.Services
{
    /// <summary>
    /// Result of profile validation with detailed information
    /// </summary>
    public class ProfileValidationResult
    {
        /// <summary>
        /// Overall validation state
        /// </summary>
        public ProfileValidationState State { get; init; }

        /// <summary>
        /// Validation messages (warnings, errors)
        /// </summary>
        public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Property-specific validation results
        /// </summary>
        public IReadOnlyDictionary<string, PropertyValidationState> PropertyStates { get; init; }
            = new Dictionary<string, PropertyValidationState>();

        /// <summary>
        /// True if profile is valid (no errors)
        /// </summary>
        public bool IsValid => State != ProfileValidationState.Error;

        /// <summary>
        /// True if profile has warnings
        /// </summary>
        public bool HasWarnings => State == ProfileValidationState.Warning || Messages.Any();
    }

    /// <summary>
    /// Overall profile validation state
    /// </summary>
    public enum ProfileValidationState
    {
        Valid,
        Warning,
        Error,
        Missing
    }
}
```

## Command Contracts

### IJobInfoDisplayViewModel Commands

```csharp
namespace S7Tools.ViewModels.Jobs
{
    /// <summary>
    /// Commands available in job information display
    /// </summary>
    public interface IJobInfoDisplayViewModel
    {
        /// <summary>
        /// Command to refresh job information from data sources
        /// </summary>
        ReactiveCommand<Unit, Unit> RefreshCommand { get; }

        /// <summary>
        /// Command to edit the selected job
        /// </summary>
        ReactiveCommand<Unit, Unit> EditJobCommand { get; }

        /// <summary>
        /// Command to duplicate the selected job
        /// </summary>
        ReactiveCommand<Unit, Unit> DuplicateJobCommand { get; }

        /// <summary>
        /// Command to delete the selected job
        /// </summary>
        ReactiveCommand<Unit, Unit> DeleteJobCommand { get; }

        /// <summary>
        /// Command to fix missing profile references
        /// </summary>
        ReactiveCommand<Unit, Unit> FixMissingProfilesCommand { get; }
    }
}
```

## Event Contracts

### Profile Update Events

```csharp
namespace S7Tools.Events
{
    /// <summary>
    /// Event published when profile data changes
    /// </summary>
    public class ProfileUpdatedEvent
    {
        public Guid ProfileId { get; init; }
        public string ProfileType { get; init; } = string.Empty;
        public string ProfileName { get; init; } = string.Empty;
        public ProfileUpdateType UpdateType { get; init; }
    }

    /// <summary>
    /// Type of profile update
    /// </summary>
    public enum ProfileUpdateType
    {
        Created,
        Updated,
        Deleted,
        Renamed
    }

    /// <summary>
    /// Event published when job selection changes
    /// </summary>
    public class JobSelectionChangedEvent
    {
        public Guid? PreviousJobId { get; init; }
        public Guid? NewJobId { get; init; }
        public JobProfile? SelectedJob { get; init; }
    }
}
```

## Error Handling Contracts

### IErrorDisplayService

```csharp
namespace S7Tools.Services
{
    /// <summary>
    /// Service for consistent error display across the application
    /// </summary>
    public interface IErrorDisplayService
    {
        /// <summary>
        /// Creates a user-friendly error message for missing profiles
        /// </summary>
        /// <param name="profileId">Missing profile ID</param>
        /// <param name="profileType">Type of missing profile</param>
        /// <param name="lastKnownName">Last known profile name</param>
        /// <returns>Formatted error message</returns>
        string CreateMissingProfileMessage(Guid profileId, string profileType, string? lastKnownName = null);

        /// <summary>
        /// Creates an error message for corrupted profile data
        /// </summary>
        /// <param name="profileName">Profile name</param>
        /// <param name="profileType">Profile type</param>
        /// <param name="validationErrors">Specific validation errors</param>
        /// <returns>Formatted error message</returns>
        string CreateCorruptedProfileMessage(string profileName, string profileType, IEnumerable<string> validationErrors);

        /// <summary>
        /// Creates an error message for service unavailable scenarios
        /// </summary>
        /// <param name="serviceName">Name of unavailable service</param>
        /// <param name="operation">Operation that failed</param>
        /// <returns>Formatted error message</returns>
        string CreateServiceUnavailableMessage(string serviceName, string operation);
    }
}
```

## View Binding Contracts

### Job Information Display View Bindings

```csharp
// XAML Data Context Contract
public interface IJobInfoDisplayViewBindings
{
    // Properties for binding
    JobProfile? SelectedJob { get; }
    string JobBasicInfo { get; }
    IProfileDetailsViewModel? SerialProfileDetails { get; }
    IProfileDetailsViewModel? SocatProfileDetails { get; }
    IProfileDetailsViewModel? PowerSupplyProfileDetails { get; }
    IProfileDetailsViewModel? MemoryRegionProfileDetails { get; }
    bool HasMissingProfiles { get; }
    ObservableCollection<string> MissingProfileWarnings { get; }

    // Commands for binding
    ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    ReactiveCommand<Unit, Unit> EditJobCommand { get; }
    ReactiveCommand<Unit, Unit> DuplicateJobCommand { get; }
    ReactiveCommand<Unit, Unit> DeleteJobCommand { get; }
}
```

## Threading Contracts

### UI Thread Requirements

```csharp
namespace S7Tools.Threading
{
    /// <summary>
    /// Threading requirements for profile details operations
    /// </summary>
    public interface IProfileDetailsThreadingContract
    {
        /// <summary>
        /// Profile loading operations MUST run on background threads
        /// </summary>
        Task<IProfileDetailsViewModel> LoadProfileDetailsAsync(Guid profileId);

        /// <summary>
        /// UI property updates MUST be marshaled to UI thread
        /// </summary>
        Task UpdateUIPropertiesAsync(Action uiUpdate);

        /// <summary>
        /// Validation operations CAN run on background threads
        /// </summary>
        Task<ProfileValidationResult> ValidateProfileAsync(IProfileBase profile);
    }
}
```

## Configuration Contracts

### Display Settings

```csharp
namespace S7Tools.Configuration
{
    /// <summary>
    /// Configuration for job information display behavior
    /// </summary>
    public class JobInfoDisplaySettings
    {
        /// <summary>
        /// Default expansion state for property groups
        /// </summary>
        public PropertyGroupExpansionSettings DefaultExpansion { get; set; } = new();

        /// <summary>
        /// Maximum number of cached profile details
        /// </summary>
        public int MaxCachedProfiles { get; set; } = 50;

        /// <summary>
        /// Timeout for profile loading operations
        /// </summary>
        public TimeSpan ProfileLoadTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Enable/disable automatic refresh when profiles change
        /// </summary>
        public bool AutoRefreshOnProfileChange { get; set; } = true;
    }

    public class PropertyGroupExpansionSettings
    {
        public bool BasicInfoExpanded { get; set; } = true;
        public bool ConfigurationExpanded { get; set; } = true;
        public bool AdvancedExpanded { get; set; } = false;
    }
}
```

These contracts define the interfaces and data structures needed to implement the Enhanced Job Information Display feature while maintaining clean separation between layers and following established S7Tools patterns.
