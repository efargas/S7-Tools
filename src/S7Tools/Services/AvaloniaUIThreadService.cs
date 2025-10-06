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
            throw new ArgumentNullException(nameof(action));

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
            throw new ArgumentNullException(nameof(action));

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
            throw new ArgumentNullException(nameof(function));

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
            throw new ArgumentNullException(nameof(function));

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
            throw new ArgumentNullException(nameof(asyncAction));

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
            throw new ArgumentNullException(nameof(asyncFunction));

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
            throw new ArgumentNullException(nameof(action));

        if (IsUIThread)
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    /// <inheritdoc />
    public bool TryInvokeOnUIThread(Action action, TimeSpan timeout)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (IsUIThread)
        {
            action();
            return true;
        }

        try
        {
            var task = Dispatcher.UIThread.InvokeAsync(action);
            task.Wait(timeout);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryInvokeOnUIThread<T>(Func<T> function, TimeSpan timeout, out T result)
    {
        result = default(T)!;

        if (function == null)
            throw new ArgumentNullException(nameof(function));

        if (IsUIThread)
        {
            try
            {
                result = function();
                return true;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            var task = Dispatcher.UIThread.InvokeAsync(function);
            task.Wait(timeout);
            result = task.Result;
            return true;
        }
        catch
        {
            // Ignore exceptions and return false
        }

        return false;
    }
}