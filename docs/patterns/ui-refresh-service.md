---
title: "UI Refresh Service Pattern"
version: "1.0.0"
created: "2025-11-20"
last-updated: "2025-11-20"
status: "current"
tags: ["pattern", "ui", "refresh", "reactive", "synchronization"]
related:
  - "docs/architecture/mvvm-patterns.md"
  - "docs/patterns/system-patterns.md"
  - "docs/guides/ui-integration.md"
---

# UI Refresh Service Pattern

## Problem Statement

**Context**: In complex MVVM applications like S7Tools, UI state synchronization can become challenging:
- **Missed Notifications**: Property changes might not propagate if triggered from background threads
- **Stale UI**: "CanExecute" states (buttons enabled/disabled) often lag behind actual state
- **Complex Dependencies**: Computed properties depending on deep state might not update automatically
- **Boilerplate**: Implementing periodic refresh timers in every ViewModel leads to code duplication

**Example**: A "Save" button depends on `CanSave` property, which depends on `IsValid` and `HasChanges`. If `HasChanges` updates but doesn't trigger a notification chain, the button remains disabled even when it should be enabled.

## Solution

The **UI Refresh Service** (`IUIRefreshService`) provides a centralized, reactive mechanism to ensure UI synchronization across ViewModels. It offers:

1. **Automatic Monitoring**: Watches key property patterns (Can*, Is*, Status)
2. **Periodic Refresh**: Configurable background heartbeat to catch missed updates
3. **Reactive Triggers**: Refresh on specific property changes
4. **Thread Safety**: Marshals all updates to the UI thread
5. **Clean API**: Extension methods for easy integration in ViewModels

## Architecture

```
┌─────────────────────────────────────────┐
│           ViewModel (ReactiveObject)    │
│  - Has `CompositeDisposable`            │
│  - Uses `UIRefreshService`              │
└─────────────────────────────────────────┘
              ↓ (registers)
┌─────────────────────────────────────────┐
│           UIRefreshService              │
│  ┌───────────────────────────────────┐  │
│  │  Property Monitor (Reactive)      │  │
│  │  - Subscribes to PropertyChanged  │  │
│  │  - Triggers on specific props     │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │  Periodic Timer (Observable)      │  │
│  │  - Configurable interval          │  │
│  │  - Heartbeat refresh              │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
              ↓ (invokes)
┌─────────────────────────────────────────┐
│           IUIThreadService              │
│  - Marshals to Main Thread              │
│  - Ensures thread safety                │
└─────────────────────────────────────────┘
```

## Key Features

### 1. Automatic Property Detection
The service automatically identifies and refreshes properties matching common patterns:
- **Commands**: `CanExecute`, `CanSave`, `CanDelete`
- **State**: `IsEnabled`, `IsVisible`, `IsLoading`, `IsValid`
- **Selection**: `IsSelected`, `IsExpanded`, `IsChecked`
- **Status**: `StatusMessage`, `ValidationMessage`

### 2. Configurable Profiles
Pre-defined configuration profiles for different needs:
- `Default`: 2s interval, mixed mode
- `HighFrequency`: 1s interval, for active monitoring
- `LowFrequency`: 5s interval, for background dashboards
- `ReactiveOnly`: No timer, event-driven only

### 3. Reactive Extension Methods
Fluent API for integration within ViewModel constructors.

## Usage Guide

### Basic Setup (Auto-Refresh)

In your ViewModel constructor:

```csharp
public class MyViewModel : ViewModelBase
{
    public MyViewModel(IUIRefreshService refreshService)
    {
        // Setup automatic refresh with default settings
        // Monitors common properties and adds 2s heartbeat
        this.SetupAutoRefresh(refreshService, Disposables);
    }
}
```

### Custom Configuration

```csharp
public MyViewModel(IUIRefreshService refreshService)
{
    // Use high frequency profile for real-time dashboard
    this.SetupAutoRefresh(refreshService, Disposables,
        UIRefreshOptions.HighFrequency);
}
```

### Monitoring Specific Properties

Trigger a custom action when specific properties change:

```csharp
public MyViewModel(IUIRefreshService refreshService)
{
    // Re-evaluate command availability when Selection or State changes
    this.MonitorProperties(refreshService, Disposables,
        () => MyCommand.RaiseCanExecuteChanged(),
        nameof(SelectedItem), nameof(CurrentState));
}
```

### Forced Refresh

Manually trigger a refresh logic (e.g., after a background operation):

```csharp
public async Task LoadDataAsync()
{
    await _dataService.LoadAsync();

    // Force UI update immediately
    _refreshService.ForceRefresh(this, nameof(IsLoading), nameof(StatusMessage));
}
```

## Implementation Details

### UIRefreshOptions

```csharp
public class UIRefreshOptions
{
    public double PeriodicRefreshIntervalSeconds { get; set; } = 2.0;
    public bool EnablePeriodicRefresh { get; set; } = true;
    public bool SkipInitialValue { get; set; } = true;
    public bool EnableLogging { get; set; }
    public IReadOnlyList<string>? MonitoredProperties { get; set; }

    // Static profiles: Default, HighFrequency, LowFrequency, ReactiveOnly
}
```

### Property Monitoring Logic

The service uses `Observable.FromEventPattern` to create a low-overhead subscription to `PropertyChanged` events, filtering by property name.

```csharp
IObservable<Unit> propertyChange = Observable.FromEventPattern<PropertyChangedEventArgs>(
    viewModel, nameof(INotifyPropertyChanged.PropertyChanged))
    .Where(e => e.EventArgs.PropertyName == propertyName)
    .Select(_ => Unit.Default);
```

### Reflection-Based Refresh

The internal `RefreshViewModel` method uses reflection to find properties matching standard naming conventions, reducing the need for manual configuration.

```csharp
// Common patterns automatically refreshed
string[] commonProperties = new[]
{
    "CanExecute", "IsEnabled", "IsVisible", "IsLoading", "IsValid",
    "CanToggle", "CanEdit", "CanDelete", "CanCreate", "CanSave",
    "IsSelected", "IsExpanded", "IsChecked", "IsActive",
    "Status", "StatusMessage", "ErrorMessage"
};
```

## Best Practices

1. **Dispose Correctly**: Always pass the ViewModel's `CompositeDisposable` to ensure timers and subscriptions stop when the ViewModel is closed.
2. **Use ReactiveObject**: Inherit from `ReactiveObject` for best compatibility. The service handles `INotifyPropertyChanged` but works best with ReactiveUI.
3. **Avoid Over-Refresh**: Use `LowFrequency` for ViewModels that don't change often.
4. **Combine Approaches**: Use `ReactiveOnly` monitoring for immediate feedback + `LowFrequency` timer for safety net.

## Related Components

- **IUIThreadService**: Underlying service for thread marshaling
- **ViewModelBase**: Base class providing `Disposables`
- **ReactiveUI**: Framework providing `ReactiveObject` and observables

---

**Last Updated**: 2025-11-20
**Status**: Production Ready
**Coverage**: Used in core ViewModels (Jobs, Tasks, Profiles)
