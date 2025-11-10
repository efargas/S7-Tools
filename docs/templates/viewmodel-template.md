---
title: "ViewModel Template"
created: "2025-11-10"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - template
  - code
  - viewmodel
  - mvvm
related:
  - docs/patterns/mvvm-patterns.md
  - docs/guides/development-workflow.md
  - docs/architecture/mvvm-patterns.md
---

# ViewModel Template

Template for creating ViewModels following S7Tools MVVM patterns with ReactiveUI.

## Usage

1. Copy the code below to `src/S7Tools/ViewModels/{Category}/YourViewModel.cs`
2. Replace `[FEATURE_NAME]` with your feature name (PascalCase)
3. Replace `[CATEGORY]` with your category
4. Add your specific properties, commands, and business logic
5. Implement IDisposable cleanup for subscriptions

## Template Code

```csharp
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using S7Tools.Services;

namespace S7Tools.ViewModels.[CATEGORY];

/// <summary>
/// ViewModel for [FEATURE_NAME] feature.
/// </summary>
public class [FEATURE_NAME]ViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILoggingService _loggingService;

    #region Properties

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> LoadDataCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    #endregion

    /// <summary>
    /// Runtime constructor with dependency injection.
    /// </summary>
    public [FEATURE_NAME]ViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

        // Command: Load Data
        var canLoad = this.WhenAnyValue(x => x.IsLoading, loading => !loading);
        LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync, canLoad);
        LoadDataCommand.DisposeWith(_disposables);

        // Command: Save
        var canSave = this.WhenAnyValue(
            x => x.IsLoading,
            x => x.StatusMessage,
            (loading, status) => !loading && !string.IsNullOrEmpty(status)
        );
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
        SaveCommand.DisposeWith(_disposables);

        // Error handling
        LoadDataCommand.ThrownExceptions
            .Subscribe(ex => HandleError("Load failed", ex))
            .DisposeWith(_disposables);

        SaveCommand.ThrownExceptions
            .Subscribe(ex => HandleError("Save failed", ex))
            .DisposeWith(_disposables);

        // Subscribe to property changes
        this.WhenAnyValue(x => x.StatusMessage)
            .Skip(1) // Skip initial value
            .Subscribe(_ => OnStatusChanged())
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public [FEATURE_NAME]ViewModel()
    {
        StatusMessage = "Design-time data";
    }

    #region Command Handlers

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading...";

            // TODO: Implement data loading logic
            await Task.Delay(1000); // Placeholder

            StatusMessage = "Data loaded successfully";
            _loggingService.LogInformation("[FEATURE_NAME]: Data loaded");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Saving...";

            // TODO: Implement save logic
            await Task.Delay(500); // Placeholder

            StatusMessage = "Saved successfully";
            _loggingService.LogInformation("[FEATURE_NAME]: Data saved");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Event Handlers

    private void OnStatusChanged()
    {
        // React to status message changes
        _loggingService.LogDebug($"[FEATURE_NAME] Status: {StatusMessage}");
    }

    private void HandleError(string context, Exception exception)
    {
        StatusMessage = $"Error: {exception.Message}";
        _loggingService.LogError(exception, $"[FEATURE_NAME] {context}: {{Message}}", exception.Message);
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        _disposables?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    #endregion
}
```

## Key Patterns

### ReactiveUI Properties
```csharp
private string _value = string.Empty;
public string Value
{
    get => _value;
    set => this.RaiseAndSetIfChanged(ref _value, value);
}
```

### Commands with Validation
```csharp
var canExecute = this.WhenAnyValue(x => x.IsValid);
MyCommand = ReactiveCommand.CreateFromTask(ExecuteAsync, canExecute);
```

### Subscription Cleanup
```csharp
this.WhenAnyValue(x => x.Property)
    .Subscribe(_ => OnChanged())
    .DisposeWith(_disposables);
```

## See Also

- Service Template - `service-template.md`
- Test Template - `test-template.md`
- [Development Workflow](../guides/development-workflow.md)
