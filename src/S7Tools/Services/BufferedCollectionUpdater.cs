using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// A helper class to buffer valid items and update a collection in batches on the UI thread.
/// </summary>
/// <typeparam name="T">The type of items to buffer.</typeparam>
public sealed class BufferedCollectionUpdater<T> : IDisposable
{
    private readonly Action<IEnumerable<T>> _updateAction;
    private readonly IUIThreadService _uiThreadService;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedCollectionUpdater{T}"/> class.
    /// </summary>
    /// <param name="updateAction">The action to execute on the UI thread with the buffered items.</param>
    /// <param name="interval">The interval at which to process the buffer.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    public BufferedCollectionUpdater(Action<IEnumerable<T>> updateAction, TimeSpan interval, IUIThreadService uiThreadService)
    {
        _updateAction = updateAction ?? throw new ArgumentNullException(nameof(updateAction));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _timer = new PeriodicTimer(interval);
        _processTask = StartProcessingAsync();
    }

    /// <summary>
    /// Enqueues an item for processing.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
    }

    private async Task StartProcessingAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                ProcessQueue();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch
        {
            // Ignored to prevent crash
        }
    }

    private void ProcessQueue()
    {
        if (_queue.IsEmpty)
            return;

        var items = new List<T>();
        while (_queue.TryDequeue(out var item))
        {
            items.Add(item);
        }

        if (items.Any())
        {
            _uiThreadService.InvokeOnUIThread(() => _updateAction(items));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _cts.Cancel();
        _timer.Dispose();
        _cts.Dispose();
        _disposed = true;
    }
}
