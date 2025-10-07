using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Factories;

/// <summary>
/// Base implementation for keyed factories with common functionality.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify the object type to create.</typeparam>
/// <typeparam name="TBase">The base type of objects that can be created.</typeparam>
public abstract class BaseKeyedFactory<TKey, TBase> : IKeyedFactory<TKey, TBase>
    where TKey : notnull
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the registered factory functions.
    /// </summary>
    protected Dictionary<TKey, Func<TBase>> Factories { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseKeyedFactory{TKey, TBase}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected BaseKeyedFactory(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Factories = new Dictionary<TKey, Func<TBase>>();
        RegisterFactories();
    }

    /// <inheritdoc/>
    public virtual TBase Create(TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!Factories.TryGetValue(key, out var factory))
        {
            var error = $"No factory registered for key: {key}";
            Logger.LogError(error);
            throw new InvalidOperationException(error);
        }

        try
        {
            Logger.LogDebug("Creating object for key: {Key}", key);
            var instance = factory();
            Logger.LogDebug("Successfully created object for key: {Key}, Type: {Type}", 
                key, instance?.GetType().Name ?? "null");
            return instance;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating object for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual IEnumerable<TKey> GetAvailableKeys()
    {
        return Factories.Keys.ToList();
    }

    /// <inheritdoc/>
    public virtual bool CanCreate(TKey key)
    {
        return key != null && Factories.ContainsKey(key);
    }

    /// <summary>
    /// Registers a factory function for the specified key.
    /// </summary>
    /// <param name="key">The key to associate with the factory function.</param>
    /// <param name="factory">The factory function.</param>
    protected void RegisterFactory(TKey key, Func<TBase> factory)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        if (Factories.ContainsKey(key))
        {
            Logger.LogWarning("Overriding existing factory for key: {Key}", key);
        }

        Factories[key] = factory;
        Logger.LogDebug("Registered factory for key: {Key}", key);
    }

    /// <summary>
    /// Registers all factory functions. Override this method in derived classes.
    /// </summary>
    protected abstract void RegisterFactories();
}

/// <summary>
/// Base implementation for keyed factories with parameters.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify the object type to create.</typeparam>
/// <typeparam name="TBase">The base type of objects that can be created.</typeparam>
/// <typeparam name="TParams">The type of parameters used for creation.</typeparam>
public abstract class BaseKeyedFactory<TKey, TBase, TParams> : IKeyedFactory<TKey, TBase, TParams>
    where TKey : notnull
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the registered factory functions.
    /// </summary>
    protected Dictionary<TKey, Func<TParams, TBase>> Factories { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseKeyedFactory{TKey, TBase, TParams}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected BaseKeyedFactory(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Factories = new Dictionary<TKey, Func<TParams, TBase>>();
        RegisterFactories();
    }

    /// <inheritdoc/>
    public virtual TBase Create(TKey key, TParams parameters)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!Factories.TryGetValue(key, out var factory))
        {
            var error = $"No factory registered for key: {key}";
            Logger.LogError(error);
            throw new InvalidOperationException(error);
        }

        try
        {
            Logger.LogDebug("Creating object for key: {Key} with parameters", key);
            var instance = factory(parameters);
            Logger.LogDebug("Successfully created object for key: {Key}, Type: {Type}", 
                key, instance?.GetType().Name ?? "null");
            return instance;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating object for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual IEnumerable<TKey> GetAvailableKeys()
    {
        return Factories.Keys.ToList();
    }

    /// <inheritdoc/>
    public virtual bool CanCreate(TKey key)
    {
        return key != null && Factories.ContainsKey(key);
    }

    /// <summary>
    /// Registers a factory function for the specified key.
    /// </summary>
    /// <param name="key">The key to associate with the factory function.</param>
    /// <param name="factory">The factory function.</param>
    protected void RegisterFactory(TKey key, Func<TParams, TBase> factory)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        if (Factories.ContainsKey(key))
        {
            Logger.LogWarning("Overriding existing factory for key: {Key}", key);
        }

        Factories[key] = factory;
        Logger.LogDebug("Registered factory for key: {Key}", key);
    }

    /// <summary>
    /// Registers all factory functions. Override this method in derived classes.
    /// </summary>
    protected abstract void RegisterFactories();
}