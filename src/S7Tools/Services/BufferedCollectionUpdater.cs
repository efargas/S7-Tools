using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

public class BufferedCollectionUpdater<T> : IDisposable
{
    private readonly ConcurrentQueue<T> _buffer = new();
    private readonly Action<IEnumerable<T>> _updateAction;
    private readonly Timer _timer;
    private readonly IUIThreadService _uiThreadService;

    public BufferedCollectionUpdater(Action<IEnumerable<T>> updateAction, TimeSpan interval, IUIThreadService uiThreadService)
    {
        _updateAction = updateAction;
        _uiThreadService = uiThreadService;
        _timer = new Timer(ProcessBuffer, null, interval, interval);
    }

    public void Enqueue(T item) => _buffer.Enqueue(item);

    private void ProcessBuffer(object? state)
    {
        if (_buffer.IsEmpty) return;
        var items = new List<T>();
        while (_buffer.TryDequeue(out var item))
        {
            items.Add(item);
        }
        if (items.Count > 0)
        {
            _uiThreadService.InvokeOnUIThread(() => _updateAction(items));
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
