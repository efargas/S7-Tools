# Reusable Controls Pattern in S7Tools

**Last Updated**: 2025-11-07
**Category**: Controls
**Purpose**: Document reusable UI controls and their usage patterns

## Overview

S7Tools implements reusable controls in the `Controls` category to promote code reuse and consistency across features. These controls combine XAML views with ViewModels following MVVM principles.

## Control Categories

### 1. SerialPortDiscoveryControl

**Location**: 
- ViewModel: `src/S7Tools/ViewModels/Controls/SerialPortDiscoveryViewModel.cs`
- View: `src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml`

**Purpose**: Self-contained control for serial port discovery, selection, and management.

#### Features
- Automatic port scanning on initialization
- Real-time port availability updates
- Port details display (name, description, manufacturer)
- Manual refresh capability
- Status indicators (available/in-use)
- Error handling with user feedback

#### Architecture

```
SerialPortDiscoveryViewModel (ViewModels/Controls)
    ├─ ISerialPortService (for port operations)
    ├─ ILogger<T> (for logging)
    ├─ AvailablePorts (ObservableCollection)
    ├─ SelectedPort (string)
    ├─ RefreshCommand (ReactiveCommand)
    └─ ScanPortsAsync() (private)

SerialPortDiscoveryControl (Views/Controls)
    └─ Binds to SerialPortDiscoveryViewModel
```

#### Usage Example

**In a Feature ViewModel:**
```csharp
using S7Tools.ViewModels.Controls;

namespace S7Tools.ViewModels.Pages;

public class ConnectionsViewModel : ViewModelBase
{
    private SerialPortDiscoveryViewModel _portDiscovery;
    
    public SerialPortDiscoveryViewModel PortDiscovery
    {
        get => _portDiscovery;
        set => this.RaiseAndSetIfChanged(ref _portDiscovery, value);
    }
    
    public ConnectionsViewModel(
        ISerialPortService serialPortService,
        ILogger<SerialPortDiscoveryViewModel> portDiscoveryLogger)
    {
        // Create port discovery instance
        PortDiscovery = new SerialPortDiscoveryViewModel(
            serialPortService,
            portDiscoveryLogger);
    }
    
    // Access selected port
    public void ConnectToSelectedPort()
    {
        string? selectedPort = PortDiscovery.SelectedPort;
        if (selectedPort != null)
        {
            // Connect logic here
        }
    }
}
```

**In XAML:**
```xaml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:controls="using:S7Tools.Views.Controls"
             xmlns:vm="using:S7Tools.ViewModels.Pages"
             x:DataType="vm:ConnectionsViewModel">
    
    <StackPanel>
        <!-- Embed the control -->
        <controls:SerialPortDiscoveryControl 
            DataContext="{Binding PortDiscovery}" />
        
        <!-- Use the selected port -->
        <Button Content="Connect" 
                Command="{Binding ConnectCommand}"
                IsEnabled="{Binding PortDiscovery.SelectedPort, Converter={StaticResource NotNullConverter}}" />
    </StackPanel>
</UserControl>
```

#### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `AvailablePorts` | `ObservableCollection<string>` | List of discovered serial ports |
| `SelectedPort` | `string?` | Currently selected port |
| `IsScanning` | `bool` | Indicates if scanning is in progress |
| `StatusMessage` | `string` | User-facing status/error messages |
| `RefreshCommand` | `ReactiveCommand` | Command to manually refresh ports |

### 2. SidebarSection

**Location**: `src/S7Tools/Views/Controls/SidebarSection.axaml`

**Purpose**: Collapsible section control for organizing sidebar content with consistent styling.

#### Features
- Expandable/collapsible sections
- Icon support (Font Awesome)
- Header customization
- Consistent VSCode-style theme
- Animation support
- State persistence (optional)

#### Usage Example

```xaml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:controls="using:S7Tools.Views.Controls">
    
    <StackPanel>
        <!-- Basic section -->
        <controls:SidebarSection Header="Settings" 
                                 Icon="fa-solid fa-cog"
                                 IsExpanded="True">
            <StackPanel Spacing="4">
                <CheckBox Content="Auto-connect" />
                <CheckBox Content="Show notifications" />
            </StackPanel>
        </controls:SidebarSection>
        
        <!-- Advanced section with nested content -->
        <controls:SidebarSection Header="Advanced Options" 
                                 Icon="fa-solid fa-sliders"
                                 IsExpanded="False">
            <StackPanel Spacing="8">
                <controls:SidebarSection Header="Network" 
                                         Icon="fa-solid fa-network-wired"
                                         IsExpanded="True">
                    <StackPanel>
                        <TextBlock Text="Timeout:" />
                        <NumericUpDown Value="5000" />
                    </StackPanel>
                </controls:SidebarSection>
                
                <controls:SidebarSection Header="Security" 
                                         Icon="fa-solid fa-lock"
                                         IsExpanded="False">
                    <!-- Security settings -->
                </controls:SidebarSection>
            </StackPanel>
        </controls:SidebarSection>
    </StackPanel>
</UserControl>
```

#### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `Header` | `string` | Section title |
| `Icon` | `string` | Font Awesome icon class |
| `IsExpanded` | `bool` | Expansion state |
| `Content` | `object` | Section content |

### 3. PropertyDisplayItem

**Location**: 
- ViewModel: `src/S7Tools/ViewModels/Controls/PropertyDisplayItemViewModel.cs`
- View: `src/S7Tools/Views/Controls/PropertyDisplayItem.axaml` (implicit through bindings)

**Purpose**: Display property name/value pairs in a consistent format.

#### Usage Example

```csharp
// In ViewModel
public ObservableCollection<PropertyDisplayItemViewModel> Properties { get; } = new();

public void LoadProfile(SerialPortProfile profile)
{
    Properties.Clear();
    Properties.Add(new PropertyDisplayItemViewModel("Device", profile.Device));
    Properties.Add(new PropertyDisplayItemViewModel("Baud Rate", profile.BaudRate.ToString()));
    Properties.Add(new PropertyDisplayItemViewModel("Data Bits", profile.DataBits.ToString()));
    Properties.Add(new PropertyDisplayItemViewModel("Parity", profile.Parity.ToString()));
}
```

```xaml
<ItemsControl ItemsSource="{Binding Properties}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Margin="0,4">
                <TextBlock Text="{Binding Name}" 
                          Width="120" 
                          FontWeight="Bold" />
                <TextBlock Text="{Binding Value}" />
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

## Sidebar Views Pattern

**Purpose**: Feature-specific sidebar implementations that provide navigation and filtering within a feature.

### Examples

#### 1. JobsSidebarView

**Location**: `src/S7Tools/Views/Jobs/JobsSidebarView.axaml`
**ViewModel**: `src/S7Tools/ViewModels/Jobs/JobsManagementViewModel.cs`

**Features**:
- Job list with search/filter
- Job status indicators
- Quick actions (run, pause, delete)
- Job categories/grouping

#### 2. TaskManagerSidebarView

**Location**: `src/S7Tools/Views/Tasks/TaskManagerSidebarView.axaml`
**ViewModel**: `src/S7Tools/ViewModels/Tasks/TaskManagerViewModel.cs`

**Features**:
- Task state filtering (Active, Scheduled, Finished)
- Task list with status icons
- Quick task actions
- Task statistics summary

### Pattern Implementation

```csharp
// Feature ViewModel (in Jobs category)
namespace S7Tools.ViewModels.Jobs;

public class JobsManagementViewModel : ViewModelBase
{
    // Sidebar state
    private ObservableCollection<JobProfile> _jobs = new();
    private JobProfile? _selectedJob;
    private string _searchText = string.Empty;
    
    public ObservableCollection<JobProfile> Jobs
    {
        get => _jobs;
        set => this.RaiseAndSetIfChanged(ref _jobs, value);
    }
    
    public JobProfile? SelectedJob
    {
        get => _selectedJob;
        set => this.RaiseAndSetIfChanged(ref _selectedJob, value);
    }
    
    // Main content switching
    private object? _mainContent;
    public object? MainContent
    {
        get => _mainContent;
        set => this.RaiseAndSetIfChanged(ref _mainContent, value);
    }
    
    // Add a factory to the ViewModel's constructor
    private readonly Func<JobProfile, JobDetailsViewModel> _jobDetailsViewModelFactory;

    public JobsManagementViewModel(Func<JobProfile, JobDetailsViewModel> jobDetailsViewModelFactory)
    {
        _jobDetailsViewModelFactory = jobDetailsViewModelFactory;
    }

    private void OnJobSelected(JobProfile? job)
    {
        if (job != null)
        {
            // Use the factory to create the details ViewModel
            MainContent = _jobDetailsViewModelFactory(job);
        }
}
```

```xaml
<!-- JobsSidebarView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:S7Tools.ViewModels.Jobs"
             x:DataType="vm:JobsManagementViewModel">
    
    <StackPanel>
        <!-- Search -->
        <TextBox Text="{Binding SearchText}" 
                 Watermark="Search jobs..." />
        
        <!-- Jobs list -->
        <ListBox ItemsSource="{Binding Jobs}"
                 SelectedItem="{Binding SelectedJob}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <icons:Icon Value="{Binding StatusIcon}" />
                        <TextBlock Text="{Binding Name}" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </StackPanel>
</UserControl>
```

## Best Practices

### 1. Control Creation

**When to create a reusable control:**
- Logic/UI is used in 2+ places
- Component is self-contained
- Has clear responsibilities
- Can be tested independently

**Where to place:**
- ViewModel: `src/S7Tools/ViewModels/Controls/`
- View: `src/S7Tools/Views/Controls/`
- Namespace: `S7Tools.ViewModels.Controls` / `S7Tools.Views.Controls`

### 2. Dependency Injection

```csharp
// Control ViewModel with DI
public class MyControlViewModel : ViewModelBase
{
    private readonly IMyService _myService;
    private readonly ILogger<MyControlViewModel> _logger;
    
    public MyControlViewModel(
        IMyService myService,
        ILogger<MyControlViewModel> logger)
    {
        _myService = myService ?? throw new ArgumentNullException(nameof(myService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

// Registration in ServiceCollectionExtensions.cs
services.TryAddTransient<MyControlViewModel>();
```

### 3. Property Exposure

```csharp
// Feature ViewModel exposes control ViewModel
public class FeatureViewModel : ViewModelBase
{
    private MyControlViewModel _myControl;
    
    public MyControlViewModel MyControl
    {
        get => _myControl;
        set => this.RaiseAndSetIfChanged(ref _myControl, value);
    }
    
    public FeatureViewModel(MyControlViewModel myControl)
    {
        MyControl = myControl ?? throw new ArgumentNullException(nameof(myControl));
    }
}
```

### 4. Testing Controls

```csharp
public class SerialPortDiscoveryViewModelTests
{
    [Fact]
    public async Task ScanPortsAsync_WhenCalled_PopulatesAvailablePorts()
    {
        // Arrange
        var mockService = new Mock<ISerialPortService>();
        mockService.Setup(x => x.GetAvailablePortsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "COM1", "COM2" });
        
        var logger = Mock.Of<ILogger<SerialPortDiscoveryViewModel>>();
        var viewModel = new SerialPortDiscoveryViewModel(mockService.Object, logger);
        
        // Act
        await viewModel.RefreshCommand.Execute();
        
        // Assert
        Assert.Equal(2, viewModel.AvailablePorts.Count);
        Assert.Contains("COM1", viewModel.AvailablePorts);
        Assert.Contains("COM2", viewModel.AvailablePorts);
    }
}
```

## Related Documentation

- **UI Integration Workflow**: `docs/UI_INTEGRATION_WORKFLOW.md`
- **Category Guide**: `docs/templates/ui-integration/README.md`
- **Architecture Patterns**: `PATTERNS_REFERENCE.md`
- **ViewLocator**: `src/S7Tools/ViewLocator.cs`

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-07 | Initial documentation of reusable controls pattern |
