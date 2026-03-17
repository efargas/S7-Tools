---
title: Reusable Control Pattern (UserControl Extraction)
version: 1.0.7
created: '2025-11-10'
last-updated: '2026-03-17'
status: current
tags:
- pattern
- ui
- usercontrol
- reusability
- avalonia
- mvvm
related:
- docs/patterns/system-patterns.md
- docs/architecture/mvvm-patterns.md
- docs/architecture/overview.md
supersedes: []
---
# Reusable Control Pattern (UserControl Extraction)

## Problem Statement

**Context**: S7Tools UI has recurring sections that appear in multiple views (serial port discovery, memory region selector, profile selection dialogs).

**Problems with Code Duplication**:
- **XAML duplication**: Same UI elements copied across 3+ views
- **Inconsistent behavior**: Changes don't propagate to all instances
- **Maintenance burden**: Bug fixes require updates in multiple files
- **Testing difficulty**: Each duplicate requires separate test coverage

**Example** (duplicated XAML):
```xml
<!-- SerialPortsSettingsView.axaml -->
<StackPanel>
    <TextBlock Text="Available Ports:" />
    <ListBox ItemsSource="{Binding AvailablePorts}" />
    <Button Command="{Binding ScanPortsCommand}">Scan</Button>
    <Button Command="{Binding RefreshPortsCommand}">Refresh</Button>
</StackPanel>

<!-- SocatSettingsView.axaml -->
<StackPanel>
    <!-- ❌ Exact duplicate (305 lines total) -->
    <TextBlock Text="Available Ports:" />
    <ListBox ItemsSource="{Binding AvailablePorts}" />
    <Button Command="{Binding ScanPortsCommand}">Scan</Button>
    <Button Command="{Binding RefreshPortsCommand}">Refresh</Button>
</StackPanel>

<!-- JobWizardView.axaml -->
<StackPanel>
    <!-- ❌ Another duplicate -->
    <TextBlock Text="Available Ports:" />
    <ListBox ItemsSource="{Binding AvailablePorts}" />
    <Button Command="{Binding ScanPortsCommand}">Scan</Button>
    <Button Command="{Binding RefreshPortsCommand}">Refresh</Button>
</StackPanel>
```

## Solution

**Extract reusable UI sections into UserControls** that:
1. Encapsulate UI structure and styling
2. Bind to dedicated ViewModel (injected via DI)
3. Can be reused across multiple parent views
4. Maintain state isolation between instances

## Pattern Structure

### 1. Create UserControl (XAML)

**File**: `src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:S7Tools.ViewModels.Controls"
             x:Class="S7Tools.Views.Controls.SerialPortDiscoveryControl"
             x:DataType="vm:SerialPortDiscoveryViewModel">

    <Design.DataContext>
        <vm:SerialPortDiscoveryViewModel />
    </Design.DataContext>

    <StackPanel Spacing="8">
        <!-- Header -->
        <TextBlock Text="Serial Port Discovery"
                   FontWeight="Bold"
                   FontSize="14" />

        <!-- Available ports list -->
        <TextBlock Text="Available Ports:"
                   Foreground="{DynamicResource SystemAccentColor}" />

        <ListBox ItemsSource="{Binding AvailablePorts}"
                 SelectedItem="{Binding SelectedPort}"
                 Height="150"
                 BorderBrush="{DynamicResource SystemControlForegroundBaseMediumBrush}"
                 BorderThickness="1">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding .}" />
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- Status message -->
        <TextBlock Text="{Binding StatusMessage}"
                   Foreground="{DynamicResource SystemAccentColor}"
                   FontStyle="Italic" />

        <!-- Action buttons -->
        <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Content="Scan Ports"
                    Command="{Binding ScanPortsCommand}"
                    Width="120" />

            <Button Content="Refresh"
                    Command="{Binding RefreshPortsCommand}"
                    Width="120" />

            <Button Content="Clear"
                    Command="{Binding ClearPortsCommand}"
                    Width="120" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

### 2. Create Dedicated ViewModel

**File**: `src/S7Tools/ViewModels/Controls/SerialPortDiscoveryViewModel.cs`

```csharp
### Control ViewModel Pattern

**File**: `src/S7Tools/ViewModels/Controls/SerialPortDiscoveryViewModel.cs`

```csharp
namespace S7Tools.ViewModels.Controls;

public class SerialPortDiscoveryViewModel : ReactiveObject
{
    private readonly ISerialPortDiscoveryService _discoveryService;
    private readonly ILogger&lt;SerialPortDiscoveryViewModel&gt; _logger;

    public SerialPortDiscoveryViewModel(
        ISerialPortDiscoveryService discoveryService,
        ILogger&lt;SerialPortDiscoveryViewModel&gt; logger)
    {
        _discoveryService = discoveryService;
        _logger = logger;

        // Initialize properties
        AvailablePorts = new ObservableCollection&lt;string&gt;();

        // Create commands
        ScanPortsCommand = ReactiveCommand.CreateFromTask(ScanPortsAsync);
        RefreshPortsCommand = ReactiveCommand.CreateFromTask(RefreshPortsAsync);
        ClearPortsCommand = ReactiveCommand.Create(ClearPorts);

        // Auto-scan on initialization
        _ = ScanPortsAsync();
    }

    // Properties
    public ObservableCollection&lt;string&gt; AvailablePorts { get; }

    private string? _selectedPort;
    public string? SelectedPort
    {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> ScanPortsCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshPortsCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearPortsCommand { get; }

    // Command implementations
    private async Task ScanPortsAsync()
    {
        try
        {
            StatusMessage = "🔍 Scanning for serial ports...";

            var ports = await _discoveryService.GetAvailablePortsAsync();

            AvailablePorts.Clear();

            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }

            StatusMessage = $"✅ Found {ports.Count} port(s)";

            _logger.LogInformation("Serial port scan complete: {PortCount} ports found", ports.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Scan failed: {ex.Message}";
            _logger.LogError(ex, "Serial port scan failed");
        }
    }

    private Task RefreshPortsAsync() => ScanPortsAsync();

    private void ClearPorts()
    {
        AvailablePorts.Clear();
        SelectedPort = null;
        StatusMessage = "Ports cleared";
    }
}
```

### 3. Register Services (DI)

**File**: `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddS7ToolsControlViewModels(this IServiceCollection services)
{
    // ✅ CRITICAL: Register as Transient for state isolation
    // Each parent ViewModel instance gets its own SerialPortDiscoveryViewModel
    services.TryAddTransient&lt;SerialPortDiscoveryViewModel&gt;();

    // Other control ViewModels
    services.TryAddTransient&lt;MemoryRegionSelectorViewModel&gt;();
    services.TryAddTransient&lt;ProfileSelectorViewModel&gt;();

    return services;
}
```

**Why Transient?**
- Each parent ViewModel gets its own instance
- State isolation prevents conflicts (e.g., two wizards with different port selections)
- Lifetime matches parent ViewModel lifecycle

### 4. Use in Parent Views

**File**: `src/S7Tools/Views/Settings/SerialPortsSettingsView.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="using:S7Tools.Views.Controls"
             xmlns:vm="using:S7Tools.ViewModels.Settings"
             x:Class="S7Tools.Views.Settings.SerialPortsSettingsView"
             x:DataType="vm:SerialPortsSettingsViewModel">

    <StackPanel Spacing="16">
        <TextBlock Text="Serial Port Settings" FontSize="16" FontWeight="Bold" />

        <!-- ✅ Reusable control with DataContext binding -->
        <controls:SerialPortDiscoveryControl
            DataContext="{Binding PortScanner}"
            Height="200" />

        <!-- Other settings UI -->
        &lt;StackPanel&gt;
            <TextBlock Text="Port Configuration" />
            <!-- Baud rate, parity, etc. -->
        </StackPanel>
    </StackPanel>
</UserControl>
```

**Parent ViewModel**:

```csharp
namespace S7Tools.ViewModels.Settings;

public class SerialPortsSettingsViewModel : ViewModelBase
{
    private readonly SerialPortDiscoveryViewModel _portScanner;

    public SerialPortsSettingsViewModel(SerialPortDiscoveryViewModel portScanner)
    {
        _portScanner = portScanner;

        // Subscribe to port selection changes
        _portScanner.WhenAnyValue(x => x.SelectedPort)
            .Subscribe(port => OnPortSelected(port))
            .DisposeWith(_disposables);
    }

    // Expose child ViewModel for binding
    public SerialPortDiscoveryViewModel PortScanner => _portScanner;

    private void OnPortSelected(string? port)
    {
        if (port != null)
        {
            _logger.LogInformation("Port selected: {Port}", port);

            // Update parent settings
            DefaultSerialPort = port;
        }
    }
}
```

## Real-World Example: SerialPortDiscoveryControl

### Before Extraction (Spec 006)

**Duplicate XAML across 3 views**:
- `SerialPortsSettingsView.axaml` - 105 lines
- `SocatSettingsView.axaml` - 100 lines
- `JobWizardView.axaml` - 100 lines

**Total**: 305 lines of duplicated code

### After Extraction

**Single UserControl**: `SerialPortDiscoveryControl.axaml` - 80 lines

**Usage across 3 views**:
```xml
<!-- Each view uses 1 line -->
<controls:SerialPortDiscoveryControl DataContext="{Binding PortScanner}" Height="200" />
```

**Results**:
- ✅ Eliminated 225 lines of duplicate code (74% reduction)
- ✅ Single source of truth for port discovery UI
- ✅ Consistent behavior across all views
- ✅ One place to fix bugs

## Benefits

### 1. Code Reuse

**Before**: 305 lines duplicated across 3 files
**After**: 80 lines in 1 file + 3 lines usage = 83 lines total
**Savings**: 73% code reduction

### 2. Consistency

All instances use the same UI structure, styling, and behavior:
- ✅ Same button labels
- ✅ Same layout
- ✅ Same command bindings
- ✅ Same error handling

### 3. Maintainability

**Change once, applies everywhere**:
```xml
<!-- Update control once -->
<controls:SerialPortDiscoveryControl ... />

<!-- Change propagates to all 3 parent views automatically -->
```

### 4. State Isolation

Each parent ViewModel gets its own control instance:
```csharp
// Wizard Step 1 has its own port scanner
var wizard1 = new JobWizardViewModel(portScanner1);

// Wizard Step 2 has a different port scanner
var wizard2 = new JobWizardViewModel(portScanner2);

// No state conflicts!
```

### 5. Testability

**Single test suite** covers all usage scenarios:
```csharp
[Fact]
public async Task ScanPorts_Should_Populate_AvailablePorts()
{
    // Arrange
    var viewModel = new SerialPortDiscoveryViewModel(_discoveryService, _logger);

    // Act
    await viewModel.ScanPortsCommand.Execute();

    // Assert
    Assert.NotEmpty(viewModel.AvailablePorts);
    Assert.Contains("/dev/ttyUSB0", viewModel.AvailablePorts);
}
```

## Common Use Cases

### 1. Profile Selector Control

**Reusable profile selection dropdown**:
```xml
<controls:ProfileSelectorControl
    DataContext="{Binding ProfileSelector}"
    ProfileType="SerialPort" />
```

**Used in**:
- Job Wizard (select serial profile)
- Socat Settings (select socat profile)
- Power Supply Settings (select power profile)

### 2. Memory Region Selector

**Reusable memory region configuration**:
```xml
<controls:MemoryRegionSelectorControl
    DataContext="{Binding MemoryRegionSelector}"
    AllowMultiSelect="True" />
```

**Used in**:
- Job Creation Dialog
- Memory Dump Dialog
- Memory Region Profile Editor

### 3. Expandable Details Panel

**Reusable details expander**:
```xml
<controls:ExpandableDetailsControl
    DataContext="{Binding DetailsPanel}"
    Header="Job Details"
    IsExpanded="True" />
```

**Used in**:
- Job Management View
- Task Manager View
- Profile Details View

## Anti-Patterns

### ❌ Don't: Duplicate XAML

```xml
<!-- BAD: Duplicate control in each view -->
&lt;UserControl&gt;
    &lt;StackPanel&gt;
        <!-- 100 lines of XAML -->
    </StackPanel>
</UserControl>
```

**Fix**: Extract into reusable UserControl:
```xml
<!-- GOOD: One UserControl, reused everywhere -->
<controls:MyReusableControl DataContext="{Binding Child}" />
```

### ❌ Don't: Register Child ViewModel as Singleton

```csharp
// BAD: Singleton causes state conflicts
services.TryAddSingleton&lt;SerialPortDiscoveryViewModel&gt;();

// Two parent instances share the same scanner!
var parent1 = new JobWizardViewModel(scanner);  // ❌ Same scanner
var parent2 = new JobWizardViewModel(scanner);  // ❌ Same scanner
```

**Fix**: Register as Transient:
```csharp
// GOOD: Each parent gets its own instance
services.TryAddTransient&lt;SerialPortDiscoveryViewModel&gt;();
```

### ❌ Don't: Duplicate ViewModel Logic

```csharp
// BAD: Duplicate port scanning logic in each parent ViewModel
public class JobWizardViewModel
{
    public async Task ScanPortsAsync() { /* duplicate */ }
}

public class SocatSettingsViewModel
{
    public async Task ScanPortsAsync() { /* duplicate */ }
}
```

**Fix**: Delegate to child ViewModel:
```csharp
// GOOD: Reuse child ViewModel logic
public class JobWizardViewModel
{
    private readonly SerialPortDiscoveryViewModel _portScanner;

    public SerialPortDiscoveryViewModel PortScanner => _portScanner;

    // Delegate to child ViewModel
    public Task ScanPortsAsync() => _portScanner.ScanPortsCommand.Execute();
}
```

## Testing

### Test UserControl ViewModel

```csharp
public class SerialPortDiscoveryViewModelTests
{
    private readonly Mock&lt;ISerialPortDiscoveryService&gt; _mockDiscoveryService;
    private readonly Mock<ILogger&lt;SerialPortDiscoveryViewModel&gt;> _mockLogger;
    private readonly SerialPortDiscoveryViewModel _viewModel;

    public SerialPortDiscoveryViewModelTests()
    {
        _mockDiscoveryService = new Mock&lt;ISerialPortDiscoveryService&gt;();
        _mockLogger = new Mock<ILogger&lt;SerialPortDiscoveryViewModel&gt;>();
        _viewModel = new SerialPortDiscoveryViewModel(_mockDiscoveryService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ScanPorts_Should_Populate_AvailablePorts()
    {
        // Arrange
        var expectedPorts = new[] { "/dev/ttyUSB0", "/dev/ttyUSB1" };
        _mockDiscoveryService.Setup(s => s.GetAvailablePortsAsync())
            .ReturnsAsync(expectedPorts);

        // Act
        await _viewModel.ScanPortsCommand.Execute();

        // Assert
        Assert.Equal(2, _viewModel.AvailablePorts.Count);
        Assert.Contains("/dev/ttyUSB0", _viewModel.AvailablePorts);
        Assert.Contains("/dev/ttyUSB1", _viewModel.AvailablePorts);
        Assert.Contains("Found 2 port(s)", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task ScanPorts_Error_Should_Update_StatusMessage()
    {
        // Arrange
        _mockDiscoveryService.Setup(s => s.GetAvailablePortsAsync())
            .ThrowsAsync(new IOException("Port access denied"));

        // Act
        await _viewModel.ScanPortsCommand.Execute();

        // Assert
        Assert.Empty(_viewModel.AvailablePorts);
        Assert.Contains("Scan failed", _viewModel.StatusMessage);
    }
}
```

## Related Patterns

- [MVVM Patterns](../architecture/mvvm-patterns.md) - ViewModel structure and ReactiveUI
- [Profile Management](profile-management.md) - Reusable profile selection controls
- [Clean Architecture](../architecture/clean-architecture.md) - Separation of UI and logic

## Implementation Files

**UserControls** (`src/S7Tools/Views/Controls/`):
- `SerialPortDiscoveryControl.axaml` - Serial port discovery UI
- `MemoryRegionSelectorControl.axaml` - Memory region selection
- `ProfileSelectorControl.axaml` - Profile dropdown selector

**Control ViewModels** (`src/S7Tools/ViewModels/Controls/`):
- `SerialPortDiscoveryViewModel.cs` - Port scanning logic
- `MemoryRegionSelectorViewModel.cs` - Memory region selection logic
- `ProfileSelectorViewModel.cs` - Profile selection logic

**DI Registration**:
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` - Transient registration for control ViewModels

**Usage Examples**:
- `src/S7Tools/Views/Settings/SerialPortsSettingsView.axaml` - Settings page usage
- `src/S7Tools/Views/Settings/SocatSettingsView.axaml` - Socat settings usage
- `src/S7Tools/Views/Jobs/JobWizardView.axaml` - Wizard usage

---

**Last Updated**: 2025-11-10
**Status**: Current implementation standard (Spec 006)
**Code Reduction**: 73% (305 lines → 83 lines)

## Related Documentation

- [Attribute_Based_Display](../ATTRIBUTE_BASED_DISPLAY.md)
- [Index](../INDEX.md)
- [Ui_Integration_Workflow](../UI_INTEGRATION_WORKFLOW.md)
- [Clean Architecture](../architecture/clean-architecture.md)
- [Mvvm Patterns](../architecture/mvvm-patterns.md)
- [Overview](../architecture/overview.md)
- [Attribute_Based_Display](../archive/ATTRIBUTE_BASED_DISPLAY.md)
- [_Index](_index.md)
- [Profile Management](profile-management.md)
- [System Patterns](system-patterns.md)
- [_Index](../reviews/_index.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
