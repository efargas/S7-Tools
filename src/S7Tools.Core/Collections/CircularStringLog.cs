using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace S7Tools.Collections
{
    /// <summary>
    /// A fixed-size circular buffer for string data (lines).
    /// Used for capturing process output without unbounded memory growth.
    /// </summary>
    public class CircularStringLog
    {
        private readonly ConcurrentQueue<string> _queue = new();
        private readonly int _maxLines;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularStringLog"/> class.
        /// </summary>
        /// <param name="maxLines">The maximum number of lines to retain.</param>
        public CircularStringLog(int maxLines = 1000)
        {
            if (maxLines <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));
            _maxLines = maxLines;
        }

        /// <summary>
        /// Adds a line to the log, removing the oldest if full.
        /// </summary>
        /// <param name="line">The line to add.</param>
        public void AddLine(string line)
        {
            if (line == null) return;

            _queue.Enqueue(line);

            // Simple trim logic - doesn't need to be perfectly atomic for logging
            while (_queue.Count > _maxLines)
            {
                _queue.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Appends text that may contain newlines.
        /// </summary>
        /// <param name="text">The text to append.</param>
        public void Append(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                // Optionally filter empty lines or keep structure
                AddLine(line);
            }
        }

        /// <summary>
        /// Returns the entire log as a single string.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var line in _queue)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}
