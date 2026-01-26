using System;
using System.Text;

namespace S7Tools.Collections;

/// <summary>
/// A thread-safe circular buffer for storing log lines.
/// When the buffer is full, new lines overwrite the oldest ones.
/// </summary>
public class CircularStringLog
{
    private readonly string[] _buffer;
    private int _head; // Index where the next item will be written
    private int _count; // Number of items currently in the buffer
    private readonly int _capacity;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularStringLog"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of lines to store.</param>
    public CircularStringLog(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        _capacity = capacity;
        _buffer = new string[capacity];
        _head = 0;
        _count = 0;
    }

    /// <summary>
    /// Adds a line to the log.
    /// </summary>
    /// <param name="line">The line to add.</param>
    public void AddLine(string line)
    {
        lock (_lock)
        {
            _buffer[_head] = line;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity)
            {
                _count++;
            }
        }
    }

    /// <summary>
    /// Returns the contents of the log as a single string.
    /// </summary>
    /// <returns>All lines in the log joined by newlines.</returns>
    public override string ToString()
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            if (_count < _capacity)
            {
                // Buffer is not full, items are from 0 to _count-1
                for (int i = 0; i < _count; i++)
                {
                    builder.AppendLine(_buffer[i]);
                }
            }
            else // Buffer is full
            {
                // Items are ordered from _head to end, then 0 to _head-1
                for (int i = 0; i < _capacity; i++)
                {
                    builder.AppendLine(_buffer[(_head + i) % _capacity]);
                }
            }
            return builder.ToString();
        }
    }
}
