using System;
using System.Diagnostics;

namespace S7Tools.Services.Adapters.Plc
{
    /// <summary>
    /// Tracks detailed progress metrics for memory dump operations including speed and ETA calculations.
    /// Provides real-time feedback during long-running dump operations with human-readable formatting.
    /// 
    /// Features:
    /// - Speed calculation using exponential moving average (smooth, responsive)
    /// - ETA estimation based on current speed
    /// - Human-readable formatting for bytes (GB, MB, KB, B)
    /// - Human-readable formatting for time spans
    /// - Progress percentage tracking
    /// 
    /// Usage:
    /// <code>
    /// var tracker = new DumpProgressTracker(totalBytes);
    /// // In progress callback:
    /// tracker.Update(bytesReceived);
    /// logger.LogInformation("Progress: {Summary}", tracker.GetSummary());
    /// </code>
    /// </summary>
    internal class DumpProgressTracker
    {
        private readonly long _totalBytes;
        private readonly Stopwatch _stopwatch;
        private long _bytesReceived;
        private readonly DateTime _startTime;
        private DateTime _lastUpdateTime;
        private long _lastBytesReceived;
        private double _currentSpeed; // bytes per second
        private readonly object _updateLock = new object();

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
        /// Thread-safe: Can be accessed from multiple threads.
        /// </summary>
        public long BytesReceived
        {
            get
            {
                lock (_updateLock)
                {
                    return _bytesReceived;
                }
            }
        }

        /// <summary>
        /// Gets the number of bytes remaining to be received.
        /// Thread-safe: Can be accessed from multiple threads.
        /// </summary>
        public long BytesRemaining
        {
            get
            {
                lock (_updateLock)
                {
                    return _totalBytes - _bytesReceived;
                }
            }
        }

        /// <summary>
        /// Gets the current progress percentage (0-100).
        /// Thread-safe: Can be accessed from multiple threads.
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                lock (_updateLock)
                {
                    return _totalBytes > 0 ? (_bytesReceived * 100.0) / _totalBytes : 0;
                }
            }
        }

        /// <summary>
        /// Gets the current dump speed in bytes per second.
        /// Thread-safe: Can be accessed from multiple threads.
        /// </summary>
        public double SpeedBytesPerSecond
        {
            get
            {
                lock (_updateLock)
                {
                    return _currentSpeed;
                }
            }
        }

        /// <summary>
        /// Gets the elapsed time since the dump started.
        /// </summary>
        public TimeSpan ElapsedTime => _stopwatch.Elapsed;

        /// <summary>
        /// Gets the estimated time remaining to complete the dump.
        /// Returns null if speed is zero or cannot be calculated.
        /// Thread-safe: Can be accessed from multiple threads.
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining
        {
            get
            {
                lock (_updateLock)
                {
                    const double epsilon = 1e-6;
                    if (Math.Abs(_currentSpeed) < epsilon || _bytesReceived <= 0)
                    {
                        return null;
                    }

                    long bytesRemaining = _totalBytes - _bytesReceived;
                    if (bytesRemaining <= 0)
                    {
                        return TimeSpan.Zero;
                    }

                    double secondsRemaining = bytesRemaining / _currentSpeed;
                    return TimeSpan.FromSeconds(secondsRemaining);
                }
            }
        }

        /// <summary>
        /// Updates the progress tracker with new bytes received.
        /// Thread-safe: This method can be called from multiple threads.
        /// </summary>
        /// <param name="bytesReceived">The total number of bytes received so far.</param>
        public void Update(long bytesReceived)
        {
            lock (_updateLock)
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
                    const double epsilon = 1e-6;
                    _currentSpeed = Math.Abs(_currentSpeed) < epsilon
                        ? instantSpeed
                        : (0.3 * instantSpeed) + (0.7 * _currentSpeed);

                    _lastUpdateTime = now;
                    _lastBytesReceived = _bytesReceived;
                }
            }
        }

        /// <summary>
        /// Formats the current speed as a human-readable string.
        /// Thread-safe: Can be called from multiple threads.
        /// </summary>
        /// <returns>Formatted speed string (e.g., "1.5 MB/s", "245 KB/s").</returns>
        public string FormatSpeed()
        {
            lock (_updateLock)
            {
                return FormatSpeedInternal(_currentSpeed);
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
        /// Thread-safe: Can be called from multiple threads.
        /// </summary>
        /// <returns>Formatted summary string.</returns>
        public string GetSummary()
        {
            lock (_updateLock)
            {
                // Use existing properties and methods to avoid duplication
                double progressPercentage = _totalBytes > 0 ? (_bytesReceived * 100.0) / _totalBytes : 0;
                string progress = $"{FormatBytes(_bytesReceived)} / {FormatBytes(_totalBytes)} ({progressPercentage:F1}%)";
                
                // Reuse FormatSpeed logic by capturing current speed within lock
                string speed = FormatSpeedInternal(_currentSpeed);
                
                // Reuse ETA calculation logic
                string eta = FormatEtaInternal(_currentSpeed, _totalBytes - _bytesReceived, _bytesReceived);
                
                return $"{progress} | {speed} | ETA: {eta}";
            }
        }

        /// <summary>
        /// Formats speed value as human-readable string (internal helper, assumes lock is held).
        /// </summary>
        private static string FormatSpeedInternal(double speed)
        {
            if (speed <= 0)
            {
                return "0 B/s";
            }

            if (speed >= 1024 * 1024 * 1024) // GB/s
            {
                return $"{speed / (1024 * 1024 * 1024):F2} GB/s";
            }
            else if (speed >= 1024 * 1024) // MB/s
            {
                return $"{speed / (1024 * 1024):F2} MB/s";
            }
            else if (speed >= 1024) // KB/s
            {
                return $"{speed / 1024:F2} KB/s";
            }
            else // B/s
            {
                return $"{speed:F0} B/s";
            }
        }

        /// <summary>
        /// Formats ETA as human-readable string (internal helper, assumes lock is held).
        /// </summary>
        private static string FormatEtaInternal(double currentSpeed, long bytesRemaining, long bytesReceived)
        {
            const double epsilon = 1e-6;
            if (Math.Abs(currentSpeed) < epsilon || bytesReceived <= 0)
            {
                return "calculating...";
            }
            else if (bytesRemaining <= 0)
            {
                return FormatTimeSpan(TimeSpan.Zero);
            }
            else
            {
                double secondsRemaining = bytesRemaining / currentSpeed;
                return FormatTimeSpan(TimeSpan.FromSeconds(secondsRemaining));
            }
        }
    }
}
