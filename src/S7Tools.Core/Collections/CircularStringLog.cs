        /// <summary>
        /// Returns the entire log as a single string.
        /// </summary>
        public override string ToString()
        {
            string[] snapshot;
            lock (_lock)
            {
                snapshot = _queue.ToArray();
            }

            return string.Join(Environment.NewLine, snapshot);
        }
