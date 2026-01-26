        /// <summary>
        /// Returns the entire log as a single string.
        /// </summary>
        public override string ToString()
        {
            lock (_lock)
            {
                return string.Join(Environment.NewLine, _queue);
            }
        }
