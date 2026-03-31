using System.ComponentModel;
using System.Reactive.Disposables;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for managing automatic UI refresh functionality across ViewModels.
/// Provides reactive subscriptions and periodic updates to ensure UI synchronization.
/// </summary>
public interface IUIRefreshService
{
    /// <summary>
    /// Sets up automatic UI refresh for a ViewModel that implements INotifyPropertyChanged.
    /// This includes reactive subscriptions and periodic refresh capabilities.
    /// </summary>
    /// <typeparam name="T">The type of the ViewModel that implements INotifyPropertyChanged.</typeparam>
    /// <param name="viewModel">The ViewModel instance to set up auto-refresh for.</param>
    /// <param name="disposables">CompositeDisposable to manage subscription lifecycle.</param>
    /// <param name="options">Configuration options for the auto-refresh behavior.</param>
    void SetupAutoRefresh<T>(T viewModel, CompositeDisposable disposables, UIRefreshOptions? options = null)
        where T : class, INotifyPropertyChanged;

    /// <summary>
    /// Sets up reactive property monitoring for specific properties.
    /// When any of the monitored properties change, the specified refresh action is executed.
    /// </summary>
    /// <typeparam name="T">The type of the ViewModel that implements INotifyPropertyChanged.</typeparam>
    /// <param name="viewModel">The ViewModel instance to monitor.</param>
    /// <param name="propertyNames">Array of property names to monitor for changes.</param>
    /// <param name="refreshAction">Action to execute when properties change.</param>
    /// <param name="disposables">CompositeDisposable to manage subscription lifecycle.</param>
    /// <param name="skipInitialValue">Whether to skip the initial value notification.</param>
    void SetupPropertyMonitoring<T>(T viewModel, string[] propertyNames, Action refreshAction,
        CompositeDisposable disposables, bool skipInitialValue = true)
        where T : class, INotifyPropertyChanged;

    /// <summary>
    /// Sets up periodic UI refresh with configurable interval.
    /// Useful for ensuring UI stays synchronized even if property notifications are missed.
    /// </summary>
    /// <param name="refreshAction">Action to execute periodically.</param>
    /// <param name="intervalSeconds">Interval in seconds between refreshes.</param>
    /// <param name="disposables">CompositeDisposable to manage subscription lifecycle.</param>
    void SetupPeriodicRefresh(Action refreshAction, double intervalSeconds, CompositeDisposable disposables);

    /// <summary>
    /// Forces immediate refresh of specified properties on a ViewModel.
    /// Uses UI thread service to ensure thread-safe property notifications.
    /// </summary>
    /// <param name="viewModel">The ViewModel to refresh.</param>
    /// <param name="propertyNames">Array of property names to refresh.</param>
    void ForceRefresh(INotifyPropertyChanged viewModel, params string[] propertyNames);
}

/// <summary>
/// Configuration options for UI auto-refresh behavior.
/// </summary>
public class UIRefreshOptions
{
    /// <summary>
    /// Gets or sets the interval in seconds for periodic refresh.
    /// Set to 0 to disable periodic refresh.
    /// Default: 2 seconds.
    /// </summary>
    public double PeriodicRefreshIntervalSeconds { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets whether to enable periodic refresh.
    /// Default: true.
    /// </summary>
    public bool EnablePeriodicRefresh { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to skip the initial value when setting up reactive subscriptions.
    /// Default: true.
    /// </summary>
    public bool SkipInitialValue { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable detailed logging of refresh operations.
    /// Default: false.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets custom properties to monitor for changes.
    /// If null or empty, no specific property monitoring is set up.
    /// </summary>
    public IReadOnlyList<string>? MonitoredProperties { get; set; }

    /// <summary>
    /// Gets the default configuration for UI refresh.
    /// </summary>
    public static UIRefreshOptions Default => new();

    /// <summary>
    /// Gets a configuration optimized for high-frequency updates.
    /// </summary>
    public static UIRefreshOptions HighFrequency => new()
    {
        PeriodicRefreshIntervalSeconds = 1.0,
        EnablePeriodicRefresh = true,
        SkipInitialValue = true,
        EnableLogging = false
    };

    /// <summary>
    /// Gets a configuration for low-frequency updates to conserve resources.
    /// </summary>
    public static UIRefreshOptions LowFrequency => new()
    {
        PeriodicRefreshIntervalSeconds = 5.0,
        EnablePeriodicRefresh = true,
        SkipInitialValue = true,
        EnableLogging = false
    };

    /// <summary>
    /// Gets a configuration with only reactive monitoring, no periodic refresh.
    /// </summary>
    public static UIRefreshOptions ReactiveOnly => new()
    {
        PeriodicRefreshIntervalSeconds = 0,
        EnablePeriodicRefresh = false,
        SkipInitialValue = true,
        EnableLogging = false
    };
}
