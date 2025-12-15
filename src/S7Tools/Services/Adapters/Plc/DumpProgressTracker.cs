using System;
using System.Diagnostics;

namespace S7Tools.Services.Adapters.Plc
{
    /// <summary>
    /// Tracks detailed progress metrics for memory dump operations including speed and ETA calculations.
    /// </summary>
    internal class DumpProgressTracker
    {
        private readonly long _totalBytes;
        private readonly Stopwatch _stopwatch;
        private long _bytesReceived;
        private DateTime _startTime;
        private DateTime _lastUpdateTime;
        private long _lastBytesReceived;
        private double _currentSpeed; // bytes per second

        /// <summary>
        /// Initializes a new instance of the DumpProgressTracker class.
        /// </summary>
        /// <param name="totalBytes">Total number of bytes expected to be dumped.</param>
        public DumpProgressTracker(long totalBytes)
        {
            if (totalBytes <= 0)
            {
                throw new ArgumentException("Total bytes must be greater than zero", nameof(totalBytes));
            }

            _totalBytes = totalBytes;
            _stopwatch = Stopwatch.StartNew();
            _startTime = DateTime.UtcNow;
            _lastUpdateTime = _startTime;
            _bytesReceived = 0;
            _lastBytesReceived = 0;
            _currentSpeed = 0;
        }

        /// <summary>
        /// Gets the total number of bytes expected to be dumped.
        /// </summary>
        public long TotalBytes => _totalBytes;

        /// <summary>
        /// Gets the number of bytes received so far.
        /// </summary>
        public long BytesReceived => _bytesReceived;

        /// <summary>
        /// Gets the number of bytes remaining to be received.
        /// </summary>
        public long BytesRemaining => _totalBytes - _bytesReceived;

        /// <summary>
        /// Gets the current progress percentage (0-100).
        /// </summary>
        public double ProgressPercentage => _totalBytes > 0 ? (_bytesReceived * 100.0) / _totalBytes : 0;

        /// <summary>
        /// Gets the current dump speed in bytes per second.
        /// </summary>
        public double SpeedBytesPerSecond => _currentSpeed;

        /// <summary>
        /// Gets the elapsed time since the dump started.
        /// </summary>
        public TimeSpan ElapsedTime => _stopwatch.Elapsed;

        /// <summary>
        /// Gets the estimated time remaining to complete the dump.
        /// Returns null if speed is zero or cannot be calculated.
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining
        {
            get
            {
                if (_currentSpeed <= 0 || _bytesReceived <= 0)
                {
                    return null;
                }

                long bytesRemaining = BytesRemaining;
                if (bytesRemaining <= 0)
                {
                    return TimeSpan.Zero;
                }

                double secondsRemaining = bytesRemaining / _currentSpeed;
                return TimeSpan.FromSeconds(secondsRemaining);
            }
        }

        /// <summary>
        /// Updates the progress tracker with new bytes received.
        /// </summary>
        /// <param name="bytesReceived">The total number of bytes received so far.</param>
        public void Update(long bytesReceived)
        {
            if (bytesReceived < _bytesReceived)
            {
                throw new ArgumentException("Bytes received cannot decrease", nameof(bytesReceived));
            }

            _bytesReceived = bytesReceived;
            DateTime now = DateTime.UtcNow;
            TimeSpan timeSinceLastUpdate = now - _lastUpdateTime;

            // Calculate speed using moving average (only update if enough time has passed)
            if (timeSinceLastUpdate.TotalSeconds >= 0.1) // Update speed every 100ms
            {
                long bytesSinceLastUpdate = _bytesReceived - _lastBytesReceived;
                double instantSpeed = bytesSinceLastUpdate / timeSinceLastUpdate.TotalSeconds;

                // Use exponential moving average for smoother speed calculation
                // Weight: 30% new value, 70% previous value
                if (_currentSpeed == 0)
                {
                    _currentSpeed = instantSpeed;
                }
                else
                {
                    _currentSpeed = (0.3 * instantSpeed) + (0.7 * _currentSpeed);
                }

                _lastUpdateTime = now;
                _lastBytesReceived = _bytesReceived;
            }
        }

        /// <summary>
        /// Formats the current speed as a human-readable string.
        /// </summary>
        /// <returns>Formatted speed string (e.g., "1.5 MB/s", "245 KB/s").</returns>
        public string FormatSpeed()
        {
            if (_currentSpeed <= 0)
            {
                return "0 B/s";
            }

            if (_currentSpeed >= 1024 * 1024 * 1024) // GB/s
            {
                return $"{_currentSpeed / (1024 * 1024 * 1024):F2} GB/s";
            }
            else if (_currentSpeed >= 1024 * 1024) // MB/s
            {
                return $"{_currentSpeed / (1024 * 1024):F2} MB/s";
            }
            else if (_currentSpeed >= 1024) // KB/s
            {
                return $"{_currentSpeed / 1024:F2} KB/s";
            }
            else // B/s
            {
                return $"{_currentSpeed:F0} B/s";
            }
        }

        /// <summary>
        /// Formats a byte count as a human-readable string.
        /// </summary>
        /// <param name="bytes">Number of bytes.</param>
        /// <returns>Formatted string (e.g., "1.5 MB", "245 KB").</returns>
        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024) // GB
            {
                return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
            }
            else if (bytes >= 1024 * 1024) // MB
            {
                return $"{bytes / (1024.0 * 1024):F2} MB";
            }
            else if (bytes >= 1024) // KB
            {
                return $"{bytes / 1024.0:F2} KB";
            }
            else // B
            {
                return $"{bytes} B";
            }
        }

        /// <summary>
        /// Formats a time span as a human-readable string.
        /// </summary>
        /// <param name="timeSpan">Time span to format.</param>
        /// <returns>Formatted string (e.g., "2m 30s", "45s").</returns>
        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            }
            else
            {
                return $"{(int)timeSpan.TotalSeconds}s";
            }
        }

        /// <summary>
        /// Gets a summary of the current progress.
        /// </summary>
        /// <returns>Formatted summary string.</returns>
        public string GetSummary()
        {
            string progress = $"{FormatBytes(_bytesReceived)} / {FormatBytes(_totalBytes)} ({ProgressPercentage:F1}%)";
            string speed = FormatSpeed();
            string eta = EstimatedTimeRemaining.HasValue ? FormatTimeSpan(EstimatedTimeRemaining.Value) : "calculating...";
            
            return $"{progress} | {speed} | ETA: {eta}";
        }
    }
}
