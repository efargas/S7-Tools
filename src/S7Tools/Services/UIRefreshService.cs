using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Implementation of IUIRefreshService providing automatic UI refresh functionality.
/// This service helps maintain UI synchronization across all ViewModels in the application.
/// </summary>
public sealed class UIRefreshService : IUIRefreshService
{
    private readonly IUIThreadService _uiThreadService;
    private readonly ILogger<UIRefreshService> _logger;

    /// <summary>
    /// Initializes a new instance of the UIRefreshService.
    /// </summary>
    /// <param name="uiThreadService">Service for UI thread operations.</param>
    /// <param name="logger">Logger for this service.</param>
    public UIRefreshService(IUIThreadService uiThreadService, ILogger<UIRefreshService> logger)
    {
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void SetupAutoRefresh<T>(T viewModel, CompositeDisposable disposables, UIRefreshOptions? options = null)
        where T : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(disposables);

        options ??= UIRefreshOptions.Default;

        if (options.EnableLogging)
        {
            _logger.LogInformation("Setting up auto-refresh for ViewModel: {ViewModelType}", typeof(T).Name);
        }

        // Set up property monitoring if specified
        if (options.MonitoredProperties?.Count > 0)
        {
            SetupPropertyMonitoring(viewModel, options.MonitoredProperties.ToArray(),
                () => RefreshViewModel(viewModel, options), disposables, options.SkipInitialValue);
        }

        // Set up periodic refresh if enabled
        if (options.EnablePeriodicRefresh && options.PeriodicRefreshIntervalSeconds > 0)
        {
            SetupPeriodicRefresh(() => RefreshViewModel(viewModel, options),
                options.PeriodicRefreshIntervalSeconds, disposables);
        }
    }

    /// <inheritdoc/>
    public void SetupPropertyMonitoring<T>(T viewModel, string[] propertyNames, Action refreshAction,
        CompositeDisposable disposables, bool skipInitialValue = true)
        where T : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(propertyNames);
        ArgumentNullException.ThrowIfNull(refreshAction);
        ArgumentNullException.ThrowIfNull(disposables);

        if (propertyNames.Length == 0)
        {
            _logger.LogWarning("No properties specified for monitoring in ViewModel: {ViewModelType}", typeof(T).Name);
            return;
        }

        try
        {
            // Create a reactive subscription that monitors multiple properties
            var propertyChanges = Observable.Empty<Unit>();

            // Combine all property change observables
            foreach (var propertyName in propertyNames)
            {
                var propertyChange = Observable.FromEventPattern<PropertyChangedEventArgs>(
                    viewModel, nameof(INotifyPropertyChanged.PropertyChanged))
                    .Where(e => e.EventArgs.PropertyName == propertyName)
                    .Select(_ => Unit.Default);

                propertyChanges = propertyChanges.Merge(propertyChange);
            }

            // Set up the subscription
            var subscription = propertyChanges;
            if (skipInitialValue)
            {
                subscription = subscription.Skip(1);
            }

            subscription
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    try
                    {
                        refreshAction();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing refresh action for ViewModel: {ViewModelType}", typeof(T).Name);
                    }
                })
                .DisposeWith(disposables);

            _logger.LogDebug("Property monitoring set up for {PropertyCount} properties in ViewModel: {ViewModelType}",
                propertyNames.Length, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up property monitoring for ViewModel: {ViewModelType}", typeof(T).Name);
        }
    }

    /// <inheritdoc/>
    public void SetupPeriodicRefresh(Action refreshAction, double intervalSeconds, CompositeDisposable disposables)
    {
        ArgumentNullException.ThrowIfNull(refreshAction);
        ArgumentNullException.ThrowIfNull(disposables);

        if (intervalSeconds <= 0)
        {
            _logger.LogWarning("Invalid interval specified for periodic refresh: {Interval}s", intervalSeconds);
            return;
        }

        try
        {
            Observable.Interval(TimeSpan.FromSeconds(intervalSeconds))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    try
                    {
                        refreshAction();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing periodic refresh action");
                    }
                })
                .DisposeWith(disposables);

            _logger.LogDebug("Periodic refresh set up with interval: {Interval}s", intervalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up periodic refresh with interval: {Interval}s", intervalSeconds);
        }
    }

    /// <inheritdoc/>
    public void ForceRefresh(INotifyPropertyChanged viewModel, params string[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(propertyNames);

        if (propertyNames.Length == 0)
        {
            _logger.LogWarning("No properties specified for force refresh");
            return;
        }

        try
        {
            _uiThreadService.InvokeOnUIThread(() =>
            {
                if (viewModel is ReactiveObject reactiveViewModel)
                {
                    foreach (var propertyName in propertyNames)
                    {
                        reactiveViewModel.RaisePropertyChanged(propertyName);
                    }
                }
                else if (viewModel is INotifyPropertyChanged notifyViewModel)
                {
                    // For non-ReactiveObject ViewModels, try to use reflection to trigger PropertyChanged
                    var propertyChangedField = viewModel.GetType()
                        .GetEvent(nameof(INotifyPropertyChanged.PropertyChanged));

                    if (propertyChangedField != null)
                    {
                        foreach (var propertyName in propertyNames)
                        {
                            // This is a fallback - ideally ViewModels should inherit from ReactiveObject
                            _logger.LogWarning("Attempting to force refresh non-ReactiveObject ViewModel property: {PropertyName}", propertyName);
                        }
                    }
                }
            });

            _logger.LogDebug("Force refresh completed for {PropertyCount} properties", propertyNames.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during force refresh of {PropertyCount} properties", propertyNames.Length);
        }
    }

    /// <summary>
    /// Internal method to refresh a ViewModel based on its type and configuration.
    /// This method uses reflection to find common UI-related properties and refresh them.
    /// </summary>
    /// <param name="viewModel">The ViewModel to refresh.</param>
    /// <param name="options">Configuration options.</param>
    private void RefreshViewModel(INotifyPropertyChanged viewModel, UIRefreshOptions options)
    {
        try
        {
            // Common property patterns to refresh across ViewModels
            var commonProperties = new[]
            {
                // Common UI state properties
                "CanExecute", "IsEnabled", "IsVisible", "IsLoading", "IsValid",

                // Toggle/interaction properties
                "CanToggle", "CanEdit", "CanDelete", "CanCreate", "CanSave", "CanCancel",

                // State properties
                "IsSelected", "IsExpanded", "IsChecked", "IsActive",

                // Status properties
                "Status", "StatusMessage", "ErrorMessage", "ValidationMessage"
            };

            // Get all properties of the ViewModel that match common patterns
            var viewModelType = viewModel.GetType();
            var propertiesToRefresh = new List<string>();

            foreach (var property in viewModelType.GetProperties())
            {
                var propertyName = property.Name;

                // Check if property matches common patterns
                if (commonProperties.Any(pattern => propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                {
                    propertiesToRefresh.Add(propertyName);
                }

                // Also check for Can* properties specifically (common in ViewModels)
                if (propertyName.StartsWith("Can", StringComparison.OrdinalIgnoreCase) &&
                    property.PropertyType == typeof(bool))
                {
                    propertiesToRefresh.Add(propertyName);
                }
            }

            if (propertiesToRefresh.Count > 0)
            {
                ForceRefresh(viewModel, propertiesToRefresh.ToArray());

                if (options.EnableLogging)
                {
                    _logger.LogDebug("Refreshed {PropertyCount} properties for ViewModel: {ViewModelType}",
                        propertiesToRefresh.Count, viewModelType.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ViewModel refresh for type: {ViewModelType}", viewModel.GetType().Name);
        }
    }
}

/// <summary>
/// Extension methods to make UIRefreshService easier to use in ViewModels.
/// </summary>
public static class UIRefreshServiceExtensions
{
    /// <summary>
    /// Extension method to easily set up auto-refresh on any ReactiveObject-based ViewModel.
    /// </summary>
    /// <typeparam name="T">The ViewModel type.</typeparam>
    /// <param name="viewModel">The ViewModel instance.</param>
    /// <param name="refreshService">The UI refresh service.</param>
    /// <param name="disposables">CompositeDisposable for cleanup.</param>
    /// <param name="options">Optional configuration.</param>
    public static void SetupAutoRefresh<T>(this T viewModel, IUIRefreshService refreshService,
        CompositeDisposable disposables, UIRefreshOptions? options = null)
        where T : ReactiveObject, INotifyPropertyChanged
    {
        refreshService.SetupAutoRefresh(viewModel, disposables, options);
    }

    /// <summary>
    /// Extension method to set up monitoring for specific properties with a custom refresh action.
    /// </summary>
    /// <typeparam name="T">The ViewModel type.</typeparam>
    /// <param name="viewModel">The ViewModel instance.</param>
    /// <param name="refreshService">The UI refresh service.</param>
    /// <param name="disposables">CompositeDisposable for cleanup.</param>
    /// <param name="refreshAction">Custom action to execute on property changes.</param>
    /// <param name="propertyNames">Properties to monitor.</param>
    public static void MonitorProperties<T>(this T viewModel, IUIRefreshService refreshService,
        CompositeDisposable disposables, Action refreshAction, params string[] propertyNames)
        where T : ReactiveObject, INotifyPropertyChanged
    {
        refreshService.SetupPropertyMonitoring(viewModel, propertyNames, refreshAction, disposables);
    }
}
