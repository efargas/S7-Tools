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
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

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

            // Pre-allocate StringBuilder capacity assuming an average line length
            // to reduce re-allocations.
            const int averageLineLength = 120;
            var sb = new StringBuilder(_count * averageLineLength);

            // If the buffer is not full, start at 0.
            // If the buffer IS full, start at _head (which is the oldest element after wrap-around).
            int start = (_count < _capacity) ? 0 : _head;

            for (int i = 0; i < _count; i++)
            {
                int index = (start + i) % _capacity;
                sb.AppendLine(_buffer[index]);
            }

            return sb.ToString();
        }
    }
}
