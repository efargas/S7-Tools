# Quickstart: Enhanced Job Information Display

**Generated**: 2025-10-21
**Context**: Phase 1 quickstart guide for implementing job information display

## Overview

This quickstart provides the essential steps to implement the Enhanced Job Information Display feature in S7Tools. The implementation follows Clean Architecture and MVVM patterns using ReactiveUI.

## Implementation Order

### Phase 1: Core Services and ViewModels (2-3 days)

1. **Create ProfileDetailsService**
   - Location: `src/S7Tools/Services/ProfileDetailsService.cs`
   - Purpose: Format profile data for display
   - Dependencies: Existing profile services

2. **Create Profile Display ViewModels**
   - `IProfileDetailsViewModel` interface
   - `ProfileDetailsViewModel` base implementation
   - Specific implementations for each profile type
   - `PropertyDisplayItem` class

3. **Create JobInfoDisplayViewModel**
   - Location: `src/S7Tools/ViewModels/Jobs/JobInfoDisplayViewModel.cs`
   - Handles job selection and profile details coordination
   - Reactive properties for UI binding

### Phase 2: UI Components (2-3 days)

4. **Create Profile Details Views**
   - `ProfileDetailsView.axaml` - Reusable profile display control
   - Data templates for different profile types
   - Collapsible sections for property groups

5. **Create Job Info Display View**
   - `JobInfoDisplayView.axaml` - Main job information panel
   - Integration with profile details views
   - Error/warning displays for missing profiles

6. **Update Main Jobs View**
   - Add resizable side panel for job information
   - Wire job selection to info display
   - Update layout with GridSplitter

### Phase 3: Job Wizard Enhancement (1-2 days)

7. **Enhance Wizard Step ViewModels**
   - Add profile details properties to existing step ViewModels
   - Integrate with ProfileDetailsService
   - Update reactive property chains

8. **Update Wizard Step Views**
   - Add profile details sections to each step
   - Consistent styling with main job info display
   - Handle profile selection changes

### Phase 4: Integration and Testing (1-2 days)

9. **Service Registration**
   - Register new services in `ServiceCollectionExtensions.cs`
   - Update DI container configuration

10. **Testing**
    - Unit tests for all new ViewModels and services
    - Integration tests for UI components
    - Manual testing with real profile data

## Key Files to Create

### Services
```
src/S7Tools/Services/
├── IProfileDetailsService.cs           # Interface
├── ProfileDetailsService.cs            # Implementation
└── IErrorDisplayService.cs             # Error handling interface
```

### ViewModels
```
src/S7Tools/ViewModels/
├── Profiles/
│   ├── IProfileDetailsViewModel.cs     # Interface
│   ├── ProfileDetailsViewModel.cs      # Base implementation
│   ├── PropertyDisplayItem.cs          # Display data class
│   └── ProfileValidationResult.cs      # Validation result class
└── Jobs/
    └── JobInfoDisplayViewModel.cs      # Main job info ViewModel
```

### Views
```
src/S7Tools/Views/
├── Profiles/
│   └── ProfileDetailsView.axaml        # Reusable profile display
└── Jobs/
    └── JobInfoDisplayView.axaml        # Job information panel
```

## Essential Code Patterns

### Service Registration Pattern
```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddJobInfoDisplayServices(this IServiceCollection services)
{
    services.TryAddSingleton<IProfileDetailsService, ProfileDetailsService>();
    services.TryAddSingleton<IErrorDisplayService, ErrorDisplayService>();
    return services;
}
```

### Reactive ViewModel Pattern
```csharp
public class JobInfoDisplayViewModel : ReactiveObject
{
    private JobProfile? _selectedJob;

    public JobProfile? SelectedJob
    {
        get => _selectedJob;
        set => this.RaiseAndSetIfChanged(ref _selectedJob, value);
    }

    // Computed property that updates when SelectedJob changes
    public string JobBasicInfo => this
        .WhenAnyValue(x => x.SelectedJob)
        .Select(job => job != null ? FormatJobBasicInfo(job) : "No job selected")
        .ToProperty(this, x => x.JobBasicInfo);
}
```

### Error Handling Pattern
```csharp
public async Task<IProfileDetailsViewModel> CreateProfileDetailsAsync(Guid? profileId)
{
    try
    {
        if (!profileId.HasValue)
            return new EmptyProfileDetailsViewModel();

        var profile = await _profileService.GetByIdAsync(profileId.Value);
        return profile != null
            ? CreateProfileDetails(profile)
            : new MissingProfileDetailsViewModel(profileId.Value);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create profile details for {ProfileId}", profileId);
        return new ErrorProfileDetailsViewModel(ex.Message);
    }
}
```

### UI Thread Marshaling Pattern
```csharp
private async Task UpdateProfileDetailsAsync(JobProfile job)
{
    // Background work
    var profileDetails = await LoadProfileDetailsAsync(job);

    // UI updates on UI thread
    await _uiThreadService.InvokeAsync(() =>
    {
        SerialProfileDetails = profileDetails.SerialProfile;
        SocatProfileDetails = profileDetails.SocatProfile;
        PowerSupplyProfileDetails = profileDetails.PowerProfile;
    });
}
```

## Testing Strategy

### Unit Test Structure
```csharp
[Fact]
public async Task CreateProfileDetailsAsync_WithValidProfile_ReturnsValidViewModel()
{
    // Arrange
    var profile = CreateTestSerialProfile();
    _mockProfileService.Setup(x => x.GetByIdAsync(profile.Id))
                      .ReturnsAsync(profile);

    // Act
    var result = await _profileDetailsService.CreateProfileDetailsAsync(profile.Id);

    // Assert
    result.Should().NotBeNull();
    result.IsValid.Should().BeTrue();
    result.ProfileName.Should().Be(profile.Name);
}
```

### Mock Setup Pattern
```csharp
private void SetupMockProfileServices()
{
    _mockSerialService = new Mock<ISerialPortProfileService>();
    _mockSocatService = new Mock<ISocatProfileService>();
    _mockPowerService = new Mock<IPowerSupplyProfileService>();

    // Setup common behaviors
    _mockSerialService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                     .ReturnsAsync((Guid id) => TestData.SerialProfiles.FirstOrDefault(p => p.Id == id));
}
```

## Configuration Requirements

### AppSettings Configuration
```json
{
  "JobInfoDisplay": {
    "MaxCachedProfiles": 50,
    "ProfileLoadTimeoutSeconds": 5,
    "AutoRefreshOnProfileChange": true,
    "DefaultExpansion": {
      "BasicInfoExpanded": true,
      "ConfigurationExpanded": true,
      "AdvancedExpanded": false
    }
  }
}
```

### XAML Resource Registration
```xml
<!-- In App.axaml -->
<Application.Resources>
    <ResourceDictionary>
        <!-- Profile display templates -->
        <DataTemplate x:Key="SerialProfileDetailsTemplate" DataType="vm:SerialProfileDetailsViewModel">
            <views:SerialProfileDetailsView />
        </DataTemplate>

        <!-- Add templates for other profile types -->
    </ResourceDictionary>
</Application.Resources>
```

## Performance Considerations

### Caching Strategy
- Cache formatted profile details until source data changes
- Use `ConditionalWeakTable` for automatic cleanup
- Limit cache size to prevent memory growth

### Reactive Optimization
- Use `Skip(1)` to avoid initial value processing
- Debounce rapid selection changes with `Throttle()`
- Dispose subscriptions properly in ViewModels

### Memory Management
```csharp
public void Dispose()
{
    _profileSubscriptions?.Dispose();
    _jobSubscriptions?.Dispose();
    _cachedProfileDetails.Clear();
}
```

## Common Pitfalls to Avoid

1. **Thread Safety**: Always use `IUIThreadService` for UI updates from background threads
2. **Memory Leaks**: Dispose all reactive subscriptions and clear caches
3. **Null Reference**: Handle missing profiles gracefully with appropriate ViewModels
4. **Performance**: Don't load all profile details eagerly - use lazy loading
5. **Validation**: Validate profile data before creating display ViewModels

## Verification Steps

### Manual Testing Checklist
- [ ] Job selection updates info panel immediately
- [ ] All profile types display correctly with proper formatting
- [ ] Missing profiles show warning messages
- [ ] Wizard steps show profile details when profiles are selected
- [ ] Panel is resizable and remembers size
- [ ] Performance is acceptable with large datasets

### Automated Testing Checklist
- [ ] Unit tests for all ViewModels (>90% coverage)
- [ ] Service tests with mocked dependencies
- [ ] Error handling tests for all failure scenarios
- [ ] Performance tests for large datasets
- [ ] Integration tests for UI components

This quickstart provides the essential roadmap for implementing the Enhanced Job Information Display feature while following S7Tools architectural patterns and best practices.
