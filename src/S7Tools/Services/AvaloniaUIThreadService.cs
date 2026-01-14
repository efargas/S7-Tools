using Avalonia.Threading;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Avalonia-specific implementation of the UI thread service.
/// </summary>
public sealed class AvaloniaUIThreadService : IUIThreadService
{
    /// <inheritdoc />
    public bool IsUIThread => Dispatcher.UIThread.CheckAccess();

    /// <inheritdoc />
    public void InvokeOnUIThread(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsUIThread)
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Invoke(action);
        }
    }

    /// <inheritdoc />
    public async Task InvokeOnUIThreadAsync(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsUIThread)
        {
            action();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(action);
        }
    }

    /// <inheritdoc />
    public T InvokeOnUIThread<T>(Func<T> function)
    {
        if (function == null)
        {
            throw new ArgumentNullException(nameof(function));
        }

        if (IsUIThread)
        {
            return function();
        }
        else
        {
            return Dispatcher.UIThread.Invoke(function);
        }
    }

    /// <inheritdoc />
    public async Task<T> InvokeOnUIThreadAsync<T>(Func<T> function)
    {
        if (function == null)
        {
            throw new ArgumentNullException(nameof(function));
        }

        if (IsUIThread)
        {
            return function();
        }
        else
        {
            return await Dispatcher.UIThread.InvokeAsync(function);
        }
    }

    /// <inheritdoc />
    public async Task InvokeOnUIThreadAsync(Func<Task> asyncAction)
    {
        if (asyncAction == null)
        {
            throw new ArgumentNullException(nameof(asyncAction));
        }

        if (IsUIThread)
        {
            await asyncAction().ConfigureAwait(false);
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(asyncAction);
        }
    }

    /// <inheritdoc />
    public async Task<T> InvokeOnUIThreadAsync<T>(Func<Task<T>> asyncFunction)
    {
        if (asyncFunction == null)
        {
            throw new ArgumentNullException(nameof(asyncFunction));
        }

        if (IsUIThread)
        {
            return await asyncFunction().ConfigureAwait(false);
        }
        else
        {
            return await Dispatcher.UIThread.InvokeAsync(asyncFunction);
        }
    }

    /// <inheritdoc />
    public void PostToUIThread(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsUIThread)
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    /// <summary>
    /// [OBSOLETE - DANGEROUS] Attempts to invoke an action on the UI thread with a timeout.
    /// </summary>
    /// <remarks>
    /// This method is marked obsolete because it uses blocking .Wait() which can cause deadlocks.
    /// Use async alternatives instead.
    /// </remarks>
    [Obsolete("DANGEROUS: Uses blocking .Wait() which can cause deadlocks. This method will be removed in a future version. Use async patterns instead.", error: true)]
    public bool TryInvokeOnUIThread(Action action, TimeSpan timeout)
    {
        throw new NotSupportedException(
            "TryInvokeOnUIThread is obsolete due to deadlock risks from blocking .Wait() calls. " +
            "Please refactor your code to use async patterns and avoid the need for timeout-based UI thread invocation.");
    }

    /// <summary>
    /// [OBSOLETE - DANGEROUS] Attempts to invoke a function on the UI thread with a timeout.
    /// </summary>
    /// <remarks>
    /// This method is marked obsolete because it uses blocking .Wait() and .Result which can cause deadlocks.
    /// Use async alternatives instead.
    /// </remarks>
    [Obsolete("DANGEROUS: Uses blocking .Wait() and .Result which can cause deadlocks. This method will be removed in a future version. Use async patterns instead.", error: true)]
    public bool TryInvokeOnUIThread<T>(Func<T> function, TimeSpan timeout, out T result)
    {
        result = default!;
        throw new NotSupportedException(
            "TryInvokeOnUIThread<T> is obsolete due to deadlock risks from blocking .Wait() and .Result calls. " +
            "Please refactor your code to use async patterns and avoid the need for timeout-based UI thread invocation.");
    }
}
