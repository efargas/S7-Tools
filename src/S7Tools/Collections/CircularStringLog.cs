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

            // If buffer is not full, items are from index 0 to _count-1
            if (_count < _capacity)
            {
                return string.Join(Environment.NewLine, _buffer, 0, _count) + Environment.NewLine;
            }

            // If buffer is full, the oldest item is at _head.
            // If _head is 0, the array is not wrapped, so just join everything.
            if (_head == 0)
            {
                return string.Join(Environment.NewLine, _buffer, 0, _capacity) + Environment.NewLine;
            }

            // The items wrap around.
            // Part 1: from _head to the end of the array.
            // Part 2: from the start of the array to _head-1.
            var part1 = string.Join(Environment.NewLine, _buffer, _head, _capacity - _head);
            var part2 = string.Join(Environment.NewLine, _buffer, 0, _head);

            // Combine the two parts, ensuring a newline separates them.
            return $"{part1}{Environment.NewLine}{part2}{Environment.NewLine}";
        }
    }
}
