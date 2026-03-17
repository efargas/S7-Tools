---
title: "MVVM Patterns with ReactiveUI in S7Tools"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["architecture", "mvvm", "reactiveui", "viewmodels", "patterns"]
related:
  - docs/architecture/overview.md
  - docs/architecture/clean-architecture.md
  - docs/patterns/system-patterns.md
---
  - "docs/patterns/system-patterns.md"
supersedes: []
---

# MVVM Patterns with ReactiveUI in S7Tools

## Overview

S7Tools implements the **Model-View-ViewModel (MVVM)** pattern using **ReactiveUI**, a functional reactive programming framework for .NET. This document covers the MVVM architecture, ReactiveUI patterns, and best practices specific to S7Tools.

## MVVM Architecture

### Core Components

```
┌──────────┐         ┌──────────────┐         ┌─────────┐
│   View   │ binds   │  ViewModel   │ uses    │ Service │
│ (XAML)   │────────>│ (ReactiveUI) │────────>│ (Logic) │
└──────────┘         └──────────────┘         └─────────┘
     │                      │                       │
     │                      │                       │
     ↓                      ↓                       ↓
  Avalonia UI        ReactiveObject           Domain/App
  Controls           INotifyPropertyChanged    Services
```

**Constitutional Rule** (Article III):

> All ViewModels inherit from ReactiveObject. Properties use RaiseAndSetIfChanged pattern. Commands use ReactiveCommand with validation. No code-behind logic in Views.

### Responsibilities

| Component | Responsibility | Examples |
|-----------|---------------|----------|
| **View (XAML)** | Pure presentation, no logic | `JobListView.axaml`, `SettingsView.axaml` |
| **ViewModel** | UI state, commands, presentation logic | `JobListViewModel`, `SettingsViewModel` |
| **Model** | Domain entities, business rules | `JobProfile`, `SerialPortProfile` (in Core) |
| **Service** | Application/business logic | `IJobService`, `IProfileManager<T>` |

## ReactiveUI Fundamentals

### ViewModelBase

All ViewModels inherit from `ViewModelBase` which extends `ReactiveObject`:

```csharp
namespace S7Tools.ViewModels.Base;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
    protected readonly CompositeDisposable _disposables = new();
    private bool _isDisposed;

    public virtual void Dispose()
    {
        if (_isDisposed) return;

        _disposables?.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
```

**Benefits**:

- Automatic `INotifyPropertyChanged` implementation
- Reactive property change notifications
- Proper disposal pattern for subscriptions
- Consistent base for all ViewModels

### Reactive Properties

**Pattern**: Use `RaiseAndSetIfChanged` for all properties that affect UI:

```csharp
public class JobListViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    private ObservableCollection<JobProfile> _jobs = new();
    public ObservableCollection<JobProfile> Jobs
    {
        get => _jobs;
        set => this.RaiseAndSetIfChanged(ref _jobs, value);
    }

    private JobProfile? _selectedJob;
    public JobProfile? SelectedJob
    {
        get => _selectedJob;
        set => this.RaiseAndSetIfChanged(ref _selectedJob, value);
    }
}
```

**How it works**:

1. Property setter calls `RaiseAndSetIfChanged`
2. Value is compared with backing field
3. If changed, backing field updated and `PropertyChanged` event raised
4. UI automatically updates via data binding

### Reactive Commands

**Pattern**: Use `ReactiveCommand` for all user actions:

```csharp
public class JobListViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
    public ReactiveCommand<Unit, Unit> EditJobCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteJobCommand { get; }

    public JobListViewModel(IProfileManager<JobProfile> jobManager)
    {
        // Simple command
        CreateJobCommand = ReactiveCommand.CreateFromTask(CreateJobAsync);

        // Command with validation
        var canEdit = this.WhenAnyValue(x => x.SelectedJob)
            .Select(job => job != null);
        EditJobCommand = ReactiveCommand.CreateFromTask(EditJobAsync, canEdit);

        // Command with complex validation
        var canDelete = this.WhenAnyValue(
            x => x.SelectedJob,
            (job) => job != null && job.CanDelete());
        DeleteJobCommand = ReactiveCommand.CreateFromTask(DeleteJobAsync, canDelete);
    }

    private async Task CreateJobAsync()
    {
        // Command implementation
    }

    private async Task EditJobAsync()
    {
        if (SelectedJob == null) return;
        // Edit logic
    }

    private async Task DeleteJobAsync()
    {
        if (SelectedJob == null) return;
        // Delete logic
    }
}
```

**Command Patterns**:

| Pattern | When to Use | Example |
|---------|-------------|---------|
| `CreateFromTask` | Async operations | Saving data, loading resources |
| `Create` | Synchronous operations | Simple state changes |
| `CreateFromObservable` | Observable-based logic | Reactive pipelines |
| With `canExecute` | Conditional execution | Edit/Delete requiring selection |

## Property Change Monitoring

### WhenAnyValue Pattern

**Use**: React to property changes reactively

```csharp
public class SettingsViewModel : ViewModelBase
{
    public SettingsViewModel()
    {
        // Single property monitoring
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async searchText => await PerformSearchAsync(searchText))
            .DisposeWith(_disposables);

        // Multiple property monitoring (up to 12 properties)
        this.WhenAnyValue(
            x => x.SerialProfile,
            x => x.SocatProfile,
            x => x.PowerSupplyProfile,
            (serial, socat, power) =>
                serial != null && socat != null && power != null)
            .Subscribe(isValid => CanExecute = isValid)
            .DisposeWith(_disposables);
    }
}
```

**Best Practices**:

1. **Limit tuple size** - Max 12 properties per `WhenAnyValue`
2. **Use Skip(1)** - Skip initial value if not needed
3. **Always dispose** - Use `DisposeWith(_disposables)`
4. **Throttle user input** - Debounce search/filter operations
5. **ObserveOn MainThread** - For UI updates

### ReactiveUI Constraints

**Critical Limitation**: Large `WhenAnyValue` tuples (>12 properties) are costly

```csharp
// ❌ BAD: Too many properties in one WhenAnyValue
this.WhenAnyValue(
    x => x.Prop1, x => x.Prop2, x => x.Prop3, x => x.Prop4,
    x => x.Prop5, x => x.Prop6, x => x.Prop7, x => x.Prop8,
    x => x.Prop9, x => x.Prop10, x => x.Prop11, x => x.Prop12,
    x => x.Prop13, x => x.Prop14)  // Too many!
    .Subscribe(OnPropertiesChanged);

// ✅ GOOD: Individual subscriptions or Observable.Merge
this.WhenAnyValue(x => x.Prop1).Skip(1)
    .Subscribe(_ => OnPropertyChanged())
    .DisposeWith(_disposables);

this.WhenAnyValue(x => x.Prop2).Skip(1)
    .Subscribe(_ => OnPropertyChanged())
    .DisposeWith(_disposables);

// Or use Observable.Merge for related properties
Observable.Merge(
    this.WhenAnyValue(x => x.Prop1).Select(_ => Unit.Default),
    this.WhenAnyValue(x => x.Prop2).Select(_ => Unit.Default),
    this.WhenAnyValue(x => x.Prop3).Select(_ => Unit.Default))
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(_ => OnPropertiesChanged())
    .DisposeWith(_disposables);
```

## ViewModel Organization

### Categorized Structure

ViewModels are organized by functional category:

```
S7Tools/ViewModels/
├── Base/                   # Base classes
│   └── ViewModelBase.cs
├── Controls/               # Control ViewModels
│   └── SerialPortScannerViewModel.cs
├── Dialogs/                # Dialog ViewModels
│   ├── JobEditDialogViewModel.cs
│   └── ProfileEditDialogViewModel.cs
├── Jobs/                   # Job-related ViewModels
│   ├── JobListViewModel.cs
│   └── JobWizardViewModel.cs
├── Layout/                 # Layout ViewModels
│   ├── ActivityBarViewModel.cs
│   └── NavigationViewModel.cs
├── Pages/                  # Page ViewModels
│   ├── HomeViewModel.cs
│   └── SettingsViewModel.cs
├── Profiles/               # Profile ViewModels
│   ├── SerialPortProfileViewModel.cs
│   └── SocatProfileViewModel.cs
├── Settings/               # Settings ViewModels
│   └── ApplicationSettingsViewModel.cs
└── Tasks/                  # Task ViewModels
    └── TaskManagerViewModel.cs
```

**Namespace Convention**: `S7Tools.ViewModels.{Category}`

### ViewLocator Pattern

Views are automatically resolved from ViewModels:

```csharp
// ViewModels/Pages/HomeViewModel.cs
namespace S7Tools.ViewModels.Pages;

public class HomeViewModel : ViewModelBase
{
    // ViewModel implementation
}

// Views/Pages/HomeView.axaml.cs
namespace S7Tools.Views.Pages;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }
}
```

**Automatic Resolution**:
- Input: `S7Tools.ViewModels.Pages.HomeViewModel`
- Output: `S7Tools.Views.Pages.HomeView`
- Pattern: Replace `ViewModels.{Category}.{Name}ViewModel` → `Views.{Category}.{Name}View`

## Common ViewModel Patterns

### Pattern 1: List Management ViewModel

```csharp
public class JobListViewModel : ViewModelBase
{
    private readonly IProfileManager<JobProfile> _jobManager;
    private readonly ILogger<JobListViewModel> _logger;

    private ObservableCollection<JobProfile> _jobs = new();
    public ObservableCollection<JobProfile> Jobs
    {
        get => _jobs;
        set => this.RaiseAndSetIfChanged(ref _jobs, value);
    }

    private JobProfile? _selectedJob;
    public JobProfile? SelectedJob
    {
        get => _selectedJob;
        set => this.RaiseAndSetIfChanged(ref _selectedJob, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ReactiveCommand<Unit, Unit> LoadJobsCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public JobListViewModel(
        IProfileManager<JobProfile> jobManager,
        ILogger<JobListViewModel> logger)
    {
        _jobManager = jobManager;
        _logger = logger;

        // Commands
        LoadJobsCommand = ReactiveCommand.CreateFromTask(LoadJobsAsync);
        CreateJobCommand = ReactiveCommand.CreateFromTask(CreateJobAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshJobsAsync);

        // Search filtering
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ApplyFilter())
            .DisposeWith(_disposables);
    }

    private async Task LoadJobsAsync()
    {
        try
        {
            var jobs = await _jobManager.GetAllAsync();
            Jobs = new ObservableCollection<JobProfile>(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load jobs");
        }
    }

    private void ApplyFilter()
    {
        // Filter logic
    }
}
```

### Pattern 2: Dialog ViewModel with Validation

```csharp
public class JobEditDialogViewModel : ViewModelBase
{
    private readonly IProfileManager<JobProfile> _jobManager;
    private readonly ILogger<JobEditDialogViewModel> _logger;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public Interaction<Unit, Unit> CloseDialog { get; } = new();

    public JobEditDialogViewModel(
        IProfileManager<JobProfile> jobManager,
        ILogger<JobEditDialogViewModel> logger)
    {
        _jobManager = jobManager;
        _logger = logger;

        // Validation for Save command
        var canSave = this.WhenAnyValue(
            x => x.Name,
            name => !string.IsNullOrWhiteSpace(name));

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
        CancelCommand = ReactiveCommand.Create(() => CloseDialog.Handle(Unit.Default));
    }

    public void LoadJob(JobProfile job)
    {
        Name = job.Name;
        // Load other properties...
    }

    private async Task SaveAsync()
    {
        try
        {
            var job = new JobProfile { Name = Name /* ... */ };
            await _jobManager.CreateAsync(job);
            await CloseDialog.Handle(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save job");
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
```

### Pattern 3: Settings ViewModel with Refresh

```csharp
public class ApplicationSettingsViewModel : ViewModelBase
{
    private readonly IApplicationSettingsService _settings;
    private readonly ILogger<ApplicationSettingsViewModel> _logger;

    private string _logPath = string.Empty;
    public string LogPath
    {
        get => _logPath;
        set => this.RaiseAndSetIfChanged(ref _logPath, value);
    }

    public ReactiveCommand<Unit, Unit> BrowseLogPathCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

    public ApplicationSettingsViewModel(
        IApplicationSettingsService settings,
        ILogger<ApplicationSettingsViewModel> logger)
    {
        _settings = settings;
        _logger = logger;

        BrowseLogPathCommand = ReactiveCommand.CreateFromTask(BrowseLogPathAsync);
        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);

        // Subscribe to settings changes
        _settings.SettingsChanged += OnSettingsChanged;

        // Initial load
        RefreshFromSettings();
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.Key.StartsWith("logging."))
        {
            RefreshFromSettings();
        }
    }

    private void RefreshFromSettings()
    {
        LogPath = _settings.GetString("logging.path", string.Empty);
        // Refresh other settings...
    }

    public override void Dispose()
    {
        _settings.SettingsChanged -= OnSettingsChanged;
        base.Dispose();
    }
}
```

## Thread Safety and UI Updates

### UI Thread Service

**Rule**: All UI updates must happen on the UI thread

```csharp
public class BackgroundJobViewModel : ViewModelBase
{
    private readonly IUIThreadService _uiThread;
    private readonly IJobService _jobService;

    public BackgroundJobViewModel(
        IUIThreadService uiThread,
        IJobService jobService)
    {
        _uiThread = uiThread;
        _jobService = jobService;
    }

    private async Task ProcessJobAsync()
    {
        // Background work
        var result = await Task.Run(() => _jobService.ProcessData());

        // Update UI on UI thread
        await _uiThread.InvokeAsync(() =>
        {
            StatusMessage = "Processing complete";
            Progress = 100;
        });
    }
}
```

### Async/Await Best Practices

**Constitutional Rule** (Article V):

> Never block UI thread with I/O operations. Use proper async/await patterns with ConfigureAwait(false).

```csharp
// ✅ GOOD: Async all the way
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync().ConfigureAwait(false);

    await _uiThread.InvokeAsync(() =>
    {
        Data = new ObservableCollection<Item>(data);
    });
}

// ❌ BAD: Blocking UI thread
public void LoadData()
{
    var data = _service.GetDataAsync().Result;  // Blocks UI!
    Data = new ObservableCollection<Item>(data);
}
```

## Data Binding in XAML

### Binding Modes

Always specify binding mode explicitly:

```xml
<!-- Read-only binding (default) -->
<TextBlock Text="{Binding StatusMessage}" />

<!-- Two-way binding for user input -->
<TextBox Text="{Binding Name, Mode=TwoWay}" />

<!-- One-way to source (rare) -->
<Button Command="{Binding SaveCommand, Mode=OneWay}" />

<!-- One-time binding for static content -->
<TextBlock Text="{Binding CreatedAt, Mode=OneTime}" />
```

### Command Bindings

```xml
<!-- Simple command -->
<Button Content="Save" Command="{Binding SaveCommand}" />

<!-- Command with parameter -->
<Button Content="Delete"
        Command="{Binding DeleteCommand}"
        CommandParameter="{Binding SelectedItem}" />

<!-- Command with validation (button auto-disables) -->
<Button Content="Edit"
        Command="{Binding EditCommand}"
        IsEnabled="{Binding EditCommand.CanExecute}" />
```

### Collection Bindings

```xml
<!-- DataGrid with selection -->
<DataGrid ItemsSource="{Binding Jobs}"
          SelectedItem="{Binding SelectedJob, Mode=TwoWay}"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="ID" Binding="{Binding Id}" />
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" />
    </DataGrid.Columns>
</DataGrid>

<!-- ListBox with items -->
<ListBox ItemsSource="{Binding Profiles}"
         SelectedItem="{Binding SelectedProfile, Mode=TwoWay}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

## Best Practices

### ✅ DO

1. **Inherit from ViewModelBase** - Ensures consistent disposal pattern
2. **Use RaiseAndSetIfChanged** - For all bindable properties
3. **Create ReactiveCommands** - For all user actions
4. **Dispose subscriptions** - Use `DisposeWith(_disposables)`
5. **Validate inputs** - Use `canExecute` observables for commands
6. **Use ILogger&lt;T&gt;** - Structured logging in all ViewModels
7. **Keep ViewModels thin** - Delegate business logic to services
8. **Throttle user input** - Debounce search and filter operations
9. **Explicit binding modes** - Specify TwoWay, OneWay, etc.
10. **UI thread for updates** - Use `IUIThreadService` for cross-thread updates

### ❌ DON'T

1. **Put business logic in ViewModels** - Belongs in Domain/Services
2. **Block UI thread** - Use async/await, never `.Result` or `.Wait()`
3. **Large WhenAnyValue tuples** - Keep under 12 properties
4. **Forget to dispose** - Always dispose subscriptions and events
5. **Use code-behind** - All logic in ViewModel, not View.axaml.cs
6. **Direct file I/O** - Use service abstractions
7. **Nested semaphores** - Follow Internal Method Pattern
8. **Swallow exceptions** - Always log and handle properly
9. **Skip validation** - Validate all user input
10. **Implicit binding modes** - Always be explicit

## Testing ViewModels

### Unit Test Example

```csharp
public class JobListViewModelTests
{
    [Fact]
    public async Task LoadJobsCommand_Should_Populate_Jobs_Collection()
    {
        // Arrange
        var mockManager = new Mock<IProfileManager<JobProfile>>();
        var mockLogger = new Mock<ILogger<JobListViewModel>>();

        var jobs = new List<JobProfile>
        {
            new() { Id = 1, Name = "Job 1" },
            new() { Id = 2, Name = "Job 2" }
        };

        mockManager.Setup(m => m.GetAllAsync(default))
            .ReturnsAsync(jobs);

        var viewModel = new JobListViewModel(
            mockManager.Object,
            mockLogger.Object);

        // Act
        await viewModel.LoadJobsCommand.Execute();

        // Assert
        Assert.Equal(2, viewModel.Jobs.Count);
        Assert.Equal("Job 1", viewModel.Jobs[0].Name);
    }

    [Fact]
    public void EditCommand_Should_Be_Disabled_When_No_Selection()
    {
        // Arrange
        var mockManager = new Mock<IProfileManager<JobProfile>>();
        var mockLogger = new Mock<ILogger<JobListViewModel>>();

        var viewModel = new JobListViewModel(
            mockManager.Object,
            mockLogger.Object);

        // Act
        var canExecute = viewModel.EditCommand.CanExecute.FirstAsync().Wait();

        // Assert
        Assert.False(canExecute);
    }
}
```

## Related Documentation

- [Index](../INDEX.md)
- [Project_Architecture_Blueprint](../Project_Architecture_Blueprint.md)
- [Ui_Integration_Workflow](../UI_INTEGRATION_WORKFLOW.md)
- [_Index](_index.md)
- [Clean Architecture](clean-architecture.md)
- [0001 Ui Framework](decisions/0001-ui-framework.md)
- [Diagrams](diagrams.md)
- [Overview](overview.md)
- [Attribute_Based_Display](../archive/ATTRIBUTE_BASED_DISPLAY.md)
- [_Index](../patterns/_index.md)
- [Internal Method](../patterns/internal-method.md)
- [Profile Management](../patterns/profile-management.md)
- [Reusable Controls](../patterns/reusable-controls.md)
- [System Patterns](../patterns/system-patterns.md)
- [_Index](../templates/_index.md)
- [Integration_Checklist](../templates/ui-integration/INTEGRATION_CHECKLIST.md)
- [Readme](../templates/ui-integration/README.md)
- [Viewmodel Template](../templates/viewmodel-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
