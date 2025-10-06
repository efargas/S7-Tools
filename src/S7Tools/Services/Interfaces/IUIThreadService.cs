namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for executing operations on the UI thread in a cross-platform manner.
/// </summary>
public interface IUIThreadService
{
    /// <summary>
    /// Gets a value indicating whether the current thread is the UI thread.
    /// </summary>
    bool IsUIThread { get; }

    /// <summary>
    /// Executes an action on the UI thread synchronously.
    /// If already on the UI thread, executes immediately.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    void InvokeOnUIThread(Action action);

    /// <summary>
    /// Executes an action on the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeOnUIThreadAsync(Action action);

    /// <summary>
    /// Executes a function on the UI thread synchronously and returns the result.
    /// If already on the UI thread, executes immediately.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <returns>The result of the function.</returns>
    T InvokeOnUIThread<T>(Func<T> function);

    /// <summary>
    /// Executes a function on the UI thread asynchronously and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<T> InvokeOnUIThreadAsync<T>(Func<T> function);

    /// <summary>
    /// Executes an async function on the UI thread.
    /// </summary>
    /// <param name="asyncAction">The async action to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeOnUIThreadAsync(Func<Task> asyncAction);

    /// <summary>
    /// Executes an async function on the UI thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="asyncFunction">The async function to execute.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<T> InvokeOnUIThreadAsync<T>(Func<Task<T>> asyncFunction);

    /// <summary>
    /// Posts an action to be executed on the UI thread without waiting for completion.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    void PostToUIThread(Action action);

    /// <summary>
    /// Executes an action on the UI thread with a timeout.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="timeout">The maximum time to wait for execution.</param>
    /// <returns>True if the action was executed within the timeout; otherwise, false.</returns>
    bool TryInvokeOnUIThread(Action action, TimeSpan timeout);

    /// <summary>
    /// Executes a function on the UI thread with a timeout.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <param name="timeout">The maximum time to wait for execution.</param>
    /// <param name="result">The result of the function if successful.</param>
    /// <returns>True if the function was executed within the timeout; otherwise, false.</returns>
    bool TryInvokeOnUIThread<T>(Func<T> function, TimeSpan timeout, out T result);
}