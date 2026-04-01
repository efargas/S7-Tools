namespace S7Tools.Extensions;

/// <summary>
/// Extension methods for SemaphoreSlim to reduce boilerplate code.
/// </summary>
public static class SemaphoreExtensions
{
    /// <summary>
    /// Executes an action within a semaphore lock, ensuring proper release.
    /// </summary>
    /// <param name="semaphore">The semaphore to use for synchronization.</param>
    /// <param name="action">The action to execute while holding the semaphore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// await _semaphore.ExecuteAsync(async () => 
    /// {
    ///     // Your protected code here
    ///     await DoSomethingAsync();
    /// }, cancellationToken);
    /// </code>
    /// </example>
    public static async Task ExecuteAsync(
        this SemaphoreSlim semaphore,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        ArgumentNullException.ThrowIfNull(action);

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Executes a function within a semaphore lock and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="semaphore">The semaphore to use for synchronization.</param>
    /// <param name="func">The function to execute while holding the semaphore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the function.</returns>
    /// <example>
    /// <code>
    /// var result = await _semaphore.ExecuteAsync(async () => 
    /// {
    ///     // Your protected code here
    ///     return await GetSomethingAsync();
    /// }, cancellationToken);
    /// </code>
    /// </example>
    public static async Task<T> ExecuteAsync<T>(
        this SemaphoreSlim semaphore,
        Func<Task<T>> func,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        ArgumentNullException.ThrowIfNull(func);

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await func().ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
