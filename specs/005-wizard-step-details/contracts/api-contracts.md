# API Contracts: Enhanced Wizard Step Profile Details

**Branch**: `005-wizard-step-details` | **Date**: 2025-10-21
**Purpose**: Define public interfaces and contracts for enhanced profile detail display

## ViewModel Contracts

### IJobWizardViewModel Extensions

The existing JobWizardViewModel will be enhanced with computed properties for comprehensive profile detail display.

#### Serial Profile Detail Properties

```csharp
// Basic Settings
string SerialBaudRate { get; }
string SerialCharacterSize { get; }
string SerialParity { get; }
string SerialStopBits { get; }

// Control Flags
string SerialEnableReceiver { get; }
string SerialDisableHardwareFlowControl { get; }
string SerialParityEnabled { get; }
string SerialOddParity { get; }

// Input Flags
string SerialIgnoreBreak { get; }
string SerialDisableBreakInterrupt { get; }
string SerialDisableMapCRtoNL { get; }
string SerialDisableBellOnQueueFull { get; }
string SerialDisableXonXoffFlowControl { get; }

// Output Flags
string SerialDisableOutputProcessing { get; }
string SerialDisableMapNLtoCRNL { get; }

// Local Flags
string SerialDisableCanonicalMode { get; }
string SerialDisableSignalGeneration { get; }
string SerialDisableExtendedProcessing { get; }
string SerialDisableEcho { get; }
string SerialDisableEchoErase { get; }
string SerialDisableEchoKill { get; }
string SerialDisableEchoControl { get; }
string SerialDisableEchoKillErase { get; }

// Special Modes
string SerialRawMode { get; }

// Metadata
string SerialVersion { get; }
string SerialCreatedAt { get; }
string SerialModifiedAt { get; }
string SerialIsReadOnly { get; }
string SerialIsDefault { get; }
```

#### Socat Profile Detail Properties

```csharp
// TCP Settings
string SocatTcpPort { get; }
string SocatTcpHost { get; }
string SocatEnableFork { get; }
string SocatEnableReuseAddr { get; }

// Socat Flags
string SocatVerbose { get; }
string SocatHexDump { get; }
string SocatBlockSize { get; }
string SocatDebugLevel { get; }

// Serial Device Settings
string SocatSerialRawMode { get; }
string SocatSerialDisableEcho { get; }

// Process Management
string SocatAutoConfigureSerial { get; }
string SocatConnectionTimeout { get; }
string SocatAutoRestart { get; }

// Metadata
string SocatVersion { get; }
string SocatCreatedAt { get; }
string SocatModifiedAt { get; }
string SocatIsReadOnly { get; }
string SocatIsDefault { get; }
```

#### Power Supply Profile Detail Properties

```csharp
// Connection Settings
string PowerHost { get; }
string PowerPort { get; }
string PowerDeviceId { get; }

// Modbus Configuration
string PowerAddressingMode { get; }
string PowerConnectionTimeoutMs { get; }
string PowerReadTimeoutMs { get; }
string PowerWriteTimeoutMs { get; }
string PowerOnOffCoil { get; }
string PowerEnableAutoReconnect { get; }
string PowerMaxRetryAttempts { get; }

// Metadata
string PowerVersion { get; }
string PowerCreatedAt { get; }
string PowerModifiedAt { get; }
string PowerIsReadOnly { get; }
string PowerIsDefault { get; }
```

## Service Contracts

### IProfileDetailDisplayService (Optional)

If shared formatting logic is needed, this service can provide consistent property formatting across different contexts.

```csharp
public interface IProfileDetailDisplayService
{
    /// <summary>
    /// Formats a nullable value as a display string with fallback
    /// </summary>
    /// <param name="value">The value to format</param>
    /// <param name="fallback">Fallback text when value is null (default: "N/A")</param>
    /// <returns>Formatted display string</returns>
    string FormatValue<T>(T? value, string fallback = "N/A") where T : struct;

    /// <summary>
    /// Formats a nullable string with fallback for empty/null values
    /// </summary>
    /// <param name="value">The string value</param>
    /// <param name="fallback">Fallback text when value is null/empty (default: "N/A")</param>
    /// <returns>Formatted display string</returns>
    string FormatString(string? value, string fallback = "N/A");

    /// <summary>
    /// Formats a DateTime as display string with consistent format
    /// </summary>
    /// <param name="dateTime">The DateTime to format</param>
    /// <param name="fallback">Fallback text when DateTime is default (default: "N/A")</param>
    /// <returns>Formatted date string (yyyy-MM-dd HH:mm)</returns>
    string FormatDateTime(DateTime dateTime, string fallback = "N/A");

    /// <summary>
    /// Formats a boolean as Yes/No display string
    /// </summary>
    /// <param name="value">The boolean value</param>
    /// <param name="fallback">Fallback text when value is null (default: "N/A")</param>
    /// <returns>"Yes", "No", or fallback text</returns>
    string FormatBoolean(bool? value, string fallback = "N/A");
}
```

## View Contracts

### Enhanced Profile Detail Display Requirements

Each wizard step view must provide:

1. **Profile Detail Section Container**: ScrollViewer-wrapped Border element for profile details
2. **Property Grid Layout**: Consistent Grid with Name/Value column layout
3. **Section Organization**: Grouped properties with section headers
4. **Styling Consistency**: Matching colors, fonts, and spacing with existing wizard design
5. **Responsive Layout**: Proper handling of long property values and extensive lists

#### Required XAML Structure Pattern

```xml
<!-- Profile Details Section -->
<Border Background="#2D2D2D" BorderBrush="#464647" BorderThickness="1" CornerRadius="4" Padding="12">
  <ScrollViewer MaxHeight="400" VerticalScrollBarVisibility="Auto">
    <StackPanel Spacing="8">
      <!-- Section Header -->
      <TextBlock Text="[Profile Type] Profile Details" FontWeight="SemiBold" Foreground="#CCCCCC" />

      <!-- Basic Settings Section -->
      <Border Background="Transparent" BorderBrush="#464647" BorderThickness="0,1,0,0" Padding="0,8,0,0">
        <StackPanel Spacing="4">
          <TextBlock Text="Basic Settings" FontWeight="Medium" Foreground="#CCCCCC" />
          <Grid ColumnDefinitions="150,*" RowDefinitions="Auto,Auto,..." ColumnSpacing="12" RowSpacing="4">
            <!-- Property rows -->
            <TextBlock Grid.Row="0" Grid.Column="0" Text="Property Name:" Foreground="#CCCCCC" />
            <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding PropertyValue}" Foreground="#CCCCCC" />
            <!-- Additional property rows... -->
          </Grid>
        </StackPanel>
      </Border>

      <!-- Additional sections following same pattern... -->
    </StackPanel>
  </ScrollViewer>
</Border>
```

## Data Binding Contracts

### Property Binding Requirements

1. **Null Safety**: All computed properties must handle null profile selections
2. **Performance**: Property evaluation must complete within 100ms
3. **Consistency**: Use consistent fallback values across all properties
4. **Reactivity**: Properties must update immediately when profile selection changes
5. **Type Safety**: All property conversions must be safe and not throw exceptions

### Binding Patterns

```csharp
// Safe property access pattern
public string PropertyName => Profile?.Configuration?.Property?.ToString() ?? "N/A";

// Boolean formatting pattern
public string BooleanProperty => Profile?.Configuration?.BoolValue == true ? "Yes" :
                                Profile?.Configuration?.BoolValue == false ? "No" : "N/A";

// Date formatting pattern
public string DateProperty => Profile?.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

// Enum formatting pattern
public string EnumProperty => Profile?.Configuration?.EnumValue?.ToString() ?? "N/A";
```

## Performance Contracts

### Response Time Requirements

- **Profile Selection Change**: < 100ms for all detail properties to update
- **Property Calculation**: < 1ms per individual computed property
- **UI Update**: < 50ms for complete profile detail section refresh
- **Memory Usage**: Minimal additional allocation (no property caching required)

### Scalability Contracts

- **Property Count**: Support 50+ properties per profile without performance degradation
- **Profile Switching**: Maintain responsive UI when rapidly changing profile selections
- **Concurrent Updates**: Handle multiple wizard instances without interference
- **Resource Usage**: No memory leaks from property subscriptions or computed values

## Testing Contracts

### Unit Testing Requirements

- **Computed Properties**: Test each property with various profile configurations
- **Null Handling**: Verify all properties handle null profiles gracefully
- **Type Conversion**: Ensure all ToString() operations are safe
- **Fallback Values**: Confirm consistent fallback behavior across properties

### Integration Testing Requirements

- **Profile Selection**: Test UI updates when changing profile selections
- **Performance**: Verify update times meet <100ms requirement
- **Visual Consistency**: Confirm styling matches existing wizard design
- **Accessibility**: Ensure proper screen reader support for detail information
