using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Factories;

/// <summary>
/// Defines a factory for creating objects of type T.
/// </summary>
/// <typeparam name="T">The type of object to create.</typeparam>
public interface IFactory<out T>
{
    /// <summary>
    /// Creates an instance of type T.
    /// </summary>
    /// <returns>A new instance of type T.</returns>
    T Create();
}

/// <summary>
/// Defines a factory for creating objects of type T with parameters.
/// </summary>
/// <typeparam name="T">The type of object to create.</typeparam>
/// <typeparam name="TParams">The type of parameters used for creation.</typeparam>
public interface IFactory<out T, in TParams>
{
    /// <summary>
    /// Creates an instance of type T using the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters to use for creation.</param>
    /// <returns>A new instance of type T.</returns>
    T Create(TParams parameters);
}

/// <summary>
/// Defines an async factory for creating objects of type T.
/// </summary>
/// <typeparam name="T">The type of object to create.</typeparam>
public interface IAsyncFactory<T>
{
    /// <summary>
    /// Creates an instance of type T asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the created instance.</returns>
    Task<T> CreateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines an async factory for creating objects of type T with parameters.
/// </summary>
/// <typeparam name="T">The type of object to create.</typeparam>
/// <typeparam name="TParams">The type of parameters used for creation.</typeparam>
public interface IAsyncFactory<T, in TParams>
{
    /// <summary>
    /// Creates an instance of type T asynchronously using the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters to use for creation.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the created instance.</returns>
    Task<T> CreateAsync(TParams parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a factory that can create multiple types based on a key.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify the object type to create.</typeparam>
/// <typeparam name="TBase">The base type of objects that can be created.</typeparam>
public interface IKeyedFactory<TKey, out TBase>
{
    /// <summary>
    /// Creates an instance of the type associated with the specified key.
    /// </summary>
    /// <param name="key">The key identifying the type to create.</param>
    /// <returns>A new instance of the type associated with the key.</returns>
    TBase Create(TKey key);

    /// <summary>
    /// Gets all available keys for object creation.
    /// </summary>
    /// <returns>A collection of available keys.</returns>
    IEnumerable<TKey> GetAvailableKeys();

    /// <summary>
    /// Determines whether the factory can create an object for the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>true if the factory can create an object for the key; otherwise, false.</returns>
    bool CanCreate(TKey key);
}

/// <summary>
/// Defines a factory that can create multiple types based on a key with parameters.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify the object type to create.</typeparam>
/// <typeparam name="TBase">The base type of objects that can be created.</typeparam>
/// <typeparam name="TParams">The type of parameters used for creation.</typeparam>
public interface IKeyedFactory<TKey, out TBase, in TParams>
{
    /// <summary>
    /// Creates an instance of the type associated with the specified key using parameters.
    /// </summary>
    /// <param name="key">The key identifying the type to create.</param>
    /// <param name="parameters">The parameters to use for creation.</param>
    /// <returns>A new instance of the type associated with the key.</returns>
    TBase Create(TKey key, TParams parameters);

    /// <summary>
    /// Gets all available keys for object creation.
    /// </summary>
    /// <returns>A collection of available keys.</returns>
    IEnumerable<TKey> GetAvailableKeys();

    /// <summary>
    /// Determines whether the factory can create an object for the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>true if the factory can create an object for the key; otherwise, false.</returns>
    bool CanCreate(TKey key);
}
