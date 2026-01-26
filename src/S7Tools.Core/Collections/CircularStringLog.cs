        /// <summary>
        /// Adds a line to the log, removing the oldest if full.
        /// </summary>
        /// <param name="line">The line to add.</param>
        public void AddLine(string line)
        {
            if (line == null) return;

            lock (_lock)
            {
                _queue.Enqueue(line);

                // Simple trim logic - doesn't need to be perfectly atomic for logging
                while (_queue.Count > _maxLines)
                {
                    _queue.TryDequeue(out _);
                }
            }
        }
