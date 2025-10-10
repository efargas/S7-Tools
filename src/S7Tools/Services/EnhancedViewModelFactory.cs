using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Factories;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;

namespace S7Tools.Services;

/// <summary>
/// Parameters for creating ViewModels with specific configurations.
/// </summary>
public class ViewModelCreationParameters
{
    /// <summary>
    /// Gets or sets additional parameters for ViewModel creation.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to use cached instances.
    /// </summary>
    public bool UseCachedInstance { get; set; }

    /// <summary>
    /// Gets or sets the scope for ViewModel creation.
    /// </summary>
    public string? Scope { get; set; }
}

/// <summary>
/// Enhanced factory for creating ViewModels with advanced features.
/// </summary>
public class EnhancedViewModelFactory : BaseKeyedFactory<Type, ViewModelBase, ViewModelCreationParameters>, IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, ViewModelBase> _cachedInstances;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedViewModelFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">The logger instance.</param>
    public EnhancedViewModelFactory(IServiceProvider serviceProvider, ILogger<EnhancedViewModelFactory> logger)
        : base(logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cachedInstances = new Dictionary<Type, ViewModelBase>();
    }

    /// <inheritdoc/>
    public T Create<T>() where T : ViewModelBase
    {
        return (T)Create(typeof(T));
    }

    /// <inheritdoc/>
    public ViewModelBase Create(Type viewModelType)
    {
        return Create(viewModelType, new ViewModelCreationParameters());
    }

    /// <summary>
    /// Creates a ViewModel with specific parameters.
    /// </summary>
    /// <param name="viewModelType">The type of ViewModel to create.</param>
    /// <param name="parameters">The creation parameters.</param>
    /// <returns>A new or cached instance of the ViewModel.</returns>
    public override ViewModelBase Create(Type viewModelType, ViewModelCreationParameters parameters)
    {
        if (viewModelType == null)
        {
            throw new ArgumentNullException(nameof(viewModelType));
        }

        if (!typeof(ViewModelBase).IsAssignableFrom(viewModelType))
        {
            throw new ArgumentException($"Type {viewModelType.Name} must inherit from ViewModelBase", nameof(viewModelType));
        }

        parameters ??= new ViewModelCreationParameters();

        // Check for cached instance if requested
        if (parameters.UseCachedInstance && _cachedInstances.TryGetValue(viewModelType, out var cachedInstance))
        {
            Logger.LogDebug("Returning cached instance of {ViewModelType}", viewModelType.Name);
            return cachedInstance;
        }

        try
        {
            Logger.LogDebug("Creating new instance of {ViewModelType}", viewModelType.Name);

            // Use the factory if registered, otherwise fall back to service provider
            if (CanCreate(viewModelType))
            {
                var instance = base.Create(viewModelType, parameters);

                // Cache the instance if requested
                if (parameters.UseCachedInstance)
                {
                    _cachedInstances[viewModelType] = instance;
                }

                return instance;
            }
            else
            {
                // Fall back to direct service provider resolution
                var instance = (ViewModelBase)_serviceProvider.GetRequiredService(viewModelType);

                // Cache the instance if requested
                if (parameters.UseCachedInstance)
                {
                    _cachedInstances[viewModelType] = instance;
                }

                return instance;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create ViewModel of type {ViewModelType}", viewModelType.Name);
            throw;
        }
    }

    /// <summary>
    /// Creates a ViewModel with typed parameters.
    /// </summary>
    /// <typeparam name="T">The type of ViewModel to create.</typeparam>
    /// <param name="parameters">The creation parameters.</param>
    /// <returns>A new or cached instance of the ViewModel.</returns>
    public T Create<T>(ViewModelCreationParameters parameters) where T : ViewModelBase
    {
        return (T)Create(typeof(T), parameters);
    }

    /// <summary>
    /// Clears all cached ViewModel instances.
    /// </summary>
    public void ClearCache()
    {
        Logger.LogDebug("Clearing ViewModel cache with {Count} instances", _cachedInstances.Count);

        // Dispose cached instances if they implement IDisposable
        foreach (var instance in _cachedInstances.Values)
        {
            if (instance is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error disposing cached ViewModel instance of type {Type}",
                        instance.GetType().Name);
                }
            }
        }

        _cachedInstances.Clear();
    }

    /// <summary>
    /// Removes a specific ViewModel type from the cache.
    /// </summary>
    /// <param name="viewModelType">The type of ViewModel to remove from cache.</param>
    /// <returns>true if the instance was removed; otherwise, false.</returns>
    public bool RemoveFromCache(Type viewModelType)
    {
        if (viewModelType == null)
        {
            return false;
        }

        if (_cachedInstances.TryGetValue(viewModelType, out var instance))
        {
            if (instance is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error disposing cached ViewModel instance of type {Type}",
                        viewModelType.Name);
                }
            }

            _cachedInstances.Remove(viewModelType);
            Logger.LogDebug("Removed {ViewModelType} from cache", viewModelType.Name);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void RegisterFactories()
    {
        // Register custom factory functions for ViewModels that need special creation logic
        // This can be extended as needed for specific ViewModels

        RegisterFactory(typeof(MainWindowViewModel), parameters =>
        {
            // Example of custom creation logic
            var instance = _serviceProvider.GetRequiredService<MainWindowViewModel>();

            // Apply any custom initialization based on parameters
            if (parameters.Parameters.TryGetValue("InitialView", out var initialView))
            {
                // Custom initialization logic here
                Logger.LogDebug("Initializing MainWindowViewModel with initial view: {InitialView}", initialView);
            }

            return (ViewModelBase)instance;
        });

        // Add more custom factory registrations as needed
        Logger.LogDebug("Registered {Count} custom ViewModel factories", Factories.Count);
    }
}
