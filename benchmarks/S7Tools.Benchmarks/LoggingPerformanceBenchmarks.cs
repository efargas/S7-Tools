using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;

namespace S7Tools.Benchmarks;

/// <summary>
/// Benchmarks for logging performance to measure circular buffer and notification overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class LoggingPerformanceBenchmarks
{
    private ILogDataStore _logDataStore = null!;
    private LogModel _testLogEntry = null!;

    /// <summary>
    /// Sets up the benchmark environment before each iteration.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var options = new LogDataStoreOptions
        {
            MaxEntries = 10000,
            AutoScroll = false // Disable auto-scroll for benchmarking
        };
        _logDataStore = new LogDataStore(options);

        _testLogEntry = new LogModel
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Category = "TestCategory",
            Message = "Test log message for benchmarking"
        };
    }

    /// <summary>
    /// Cleans up after benchmarks complete.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _logDataStore?.Dispose();
    }

    /// <summary>
    /// Benchmarks adding a single log entry.
    /// </summary>
    [Benchmark]
    public void AddSingleLogEntry()
    {
        _logDataStore.AddEntry(_testLogEntry);
    }

    /// <summary>
    /// Benchmarks adding multiple log entries in sequence.
    /// </summary>
    [Benchmark]
    public void AddMultipleLogEntries()
    {
        for (int i = 0; i < 100; i++)
        {
            var entry = new LogModel
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Information,
                Category = "TestCategory",
                Message = $"Test log message {i}"
            };
            _logDataStore.AddEntry(entry);
        }
    }

    /// <summary>
    /// Benchmarks retrieving all log entries.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<LogModel> GetAllLogEntries()
    {
        return _logDataStore.Entries;
    }

    /// <summary>
    /// Benchmarks filtering log entries by level.
    /// </summary>
    [Benchmark]
    public IEnumerable<LogModel> FilterLogEntriesByLevel()
    {
        return _logDataStore.GetFilteredEntries(log => log.Level == LogLevel.Information);
    }

    /// <summary>
    /// Benchmarks clearing all log entries.
    /// </summary>
    [Benchmark]
    public void ClearAllLogEntries()
    {
        _logDataStore.Clear();
    }

    /// <summary>
    /// Benchmarks concurrent log additions (simulates multi-threaded logging).
    /// </summary>
    [Benchmark]
    public void ConcurrentLogAdditions()
    {
        Parallel.For(0, 50, i =>
        {
            var entry = new LogModel
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Information,
                Category = "TestCategory",
                Message = $"Concurrent log message {i}"
            };
            _logDataStore.AddEntry(entry);
        });
    }
}
