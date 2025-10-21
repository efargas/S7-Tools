# Quickstart: Enhanced Wizard Step Profile Details

**Branch**: `005-wizard-step-details` | **Date**: 2025-10-21
**Purpose**: Quick implementation guide for enhanced profile detail display in wizard steps

## Overview

This enhancement adds comprehensive profile configuration details to existing wizard step profile detail sections. Users will see complete configuration information inline within the current wizard layout, matching the detail level shown in the main job management view.

## Implementation Priority

### Phase 1: Core ViewModel Enhancement (Day 1)
1. **Extend JobWizardViewModel** with computed properties for profile details
2. **Implement property change notifications** for immediate UI updates
3. **Add null handling** and fallback values for all computed properties

### Phase 2: XAML Enhancement (Day 1-2)
1. **Enhance existing profile detail sections** in JobWizardView.axaml
2. **Add ScrollViewer containers** for extensive property lists
3. **Implement organized property sections** with consistent styling

### Phase 3: Testing & Validation (Day 2)
1. **Unit tests** for computed properties and null handling
2. **Integration tests** for UI updates and performance
3. **Manual testing** with real profile configurations

## Quick Start Steps

### 1. Enhance JobWizardViewModel

Add computed properties to `src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs`:

```csharp
// Serial Profile Details - Basic Settings
public string SerialBaudRate => SelectedSerial?.Configuration?.BaudRate.ToString() ?? "N/A";
public string SerialCharacterSize => SelectedSerial?.Configuration?.CharacterSize.ToString() ?? "N/A";
public string SerialParity => SelectedSerial?.Configuration?.Parity.ToString() ?? "N/A";
public string SerialStopBits => SelectedSerial?.Configuration?.StopBits.ToString() ?? "N/A";

// Serial Profile Details - Control Flags
public string SerialEnableReceiver => SelectedSerial?.Configuration?.EnableReceiver == true ? "Yes" :
                                     SelectedSerial?.Configuration?.EnableReceiver == false ? "No" : "N/A";
public string SerialDisableHardwareFlowControl => SelectedSerial?.Configuration?.DisableHardwareFlowControl == true ? "Yes" :
                                                 SelectedSerial?.Configuration?.DisableHardwareFlowControl == false ? "No" : "N/A";

// ... Continue for all profile properties
```

### 2. Enhance Profile Detail Sections in XAML

Update `src/S7Tools/Views/Jobs/JobWizardView.axaml` profile detail sections:

```xml
<!-- Enhanced Serial Profile Details -->
<Border Background="#2D2D2D" BorderBrush="#464647" BorderThickness="1" CornerRadius="4" Padding="12">
  <ScrollViewer MaxHeight="400" VerticalScrollBarVisibility="Auto">
    <StackPanel Spacing="8">
      <TextBlock Text="Serial Profile Details" FontWeight="SemiBold" Foreground="#CCCCCC" />

      <!-- Basic Settings Section -->
      <Border Background="Transparent" BorderBrush="#464647" BorderThickness="0,1,0,0" Padding="0,8,0,0">
        <StackPanel Spacing="4">
          <TextBlock Text="Basic Settings" FontWeight="Medium" Foreground="#CCCCCC" />
          <Grid ColumnDefinitions="150,*" RowDefinitions="Auto,Auto,Auto,Auto" ColumnSpacing="12" RowSpacing="4">
            <TextBlock Grid.Row="0" Grid.Column="0" Text="Baud Rate:" Foreground="#CCCCCC" />
            <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SerialBaudRate}" Foreground="#CCCCCC" />
            <TextBlock Grid.Row="1" Grid.Column="0" Text="Character Size:" Foreground="#CCCCCC" />
            <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding SerialCharacterSize}" Foreground="#CCCCCC" />
            <!-- Additional basic settings... -->
          </Grid>
        </StackPanel>
      </Border>

      <!-- Control Flags Section -->
      <Border Background="Transparent" BorderBrush="#464647" BorderThickness="0,1,0,0" Padding="0,8,0,0">
        <StackPanel Spacing="4">
          <TextBlock Text="Control Flags" FontWeight="Medium" Foreground="#CCCCCC" />
          <Grid ColumnDefinitions="150,*" RowDefinitions="Auto,Auto,Auto,Auto" ColumnSpacing="12" RowSpacing="4">
            <!-- Control flag properties... -->
          </Grid>
        </StackPanel>
      </Border>

      <!-- Additional sections for Input Flags, Output Flags, Local Flags, Special Modes, Metadata... -->
    </StackPanel>
  </ScrollViewer>
</Border>
```

### 3. Apply Pattern to All Profile Types

Repeat the enhancement pattern for:
- **Socat Profile Details** (Step 2): TCP Settings, Socat Flags, Serial Device Settings, Process Management, Metadata
- **Power Supply Profile Details** (Step 3): Connection Settings, Modbus Configuration, Control Settings, Metadata

## Key Implementation Notes

### Performance Considerations
- Computed properties are evaluated only when profile selection changes
- Use null-coalescing operators for safe property access
- No expensive operations in computed properties (all are simple property access + ToString())

### Styling Consistency
- Use existing wizard color scheme (#2D2D2D backgrounds, #464647 borders, #CCCCCC text)
- Maintain consistent spacing (8px between sections, 4px between properties)
- Apply consistent Grid column widths (150px for labels, * for values)

### Property Organization
Follow JobInfoDisplayView sectioning patterns:
1. **Basic Settings**: Core configuration properties
2. **Specialized Flags**: Protocol-specific flags and options
3. **Advanced Settings**: Timeout, retry, and performance settings
4. **Metadata**: Version, timestamps, read-only status

## Testing Strategy

### Unit Tests
```csharp
[Test]
public void SerialBaudRate_WhenSelectedSerialIsNull_ReturnsNA()
{
    // Arrange
    var viewModel = CreateJobWizardViewModel();
    viewModel.SelectedSerial = null;

    // Act
    var result = viewModel.SerialBaudRate;

    // Assert
    Assert.AreEqual("N/A", result);
}

[Test]
public void SerialBaudRate_WhenSelectedSerialHasBaudRate_ReturnsBaudRateString()
{
    // Arrange
    var viewModel = CreateJobWizardViewModel();
    var profile = CreateSerialProfile(baudRate: 9600);
    viewModel.SelectedSerial = profile;

    // Act
    var result = viewModel.SerialBaudRate;

    // Assert
    Assert.AreEqual("9600", result);
}
```

### Integration Tests
- Verify profile selection triggers property updates
- Confirm UI displays updated values immediately
- Test performance with profiles containing many properties

## Files to Modify

### Core Files
- `src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs` - Add computed properties
- `src/S7Tools/Views/Jobs/JobWizardView.axaml` - Enhance profile detail sections

### Test Files
- `tests/S7Tools.Tests/ViewModels/Jobs/JobWizardViewModelTests.cs` - Add property tests
- `tests/S7Tools.Tests/Views/Jobs/JobWizardViewTests.cs` - Add UI integration tests

### Optional Files
- `src/S7Tools/Services/ProfileDetailDisplayService.cs` - Shared formatting service (if needed)
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` - Service registration (if service created)

## Success Criteria Validation

- ✅ Complete profile configuration details visible in wizard steps
- ✅ Profile detail updates occur within 100ms when selection changes
- ✅ Visual consistency with main job information display
- ✅ Enhanced wizard maintains existing navigation flow
- ✅ All profile properties display with appropriate fallback values

## Common Pitfalls to Avoid

1. **Don't modify profile models** - Enhancement is purely presentation layer
2. **Don't break existing wizard navigation** - Integrate within current layout
3. **Don't add complex logic to computed properties** - Keep them simple and fast
4. **Don't forget null handling** - All computed properties must handle null profiles
5. **Don't skip performance testing** - Verify <100ms update requirement is met
