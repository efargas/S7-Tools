using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;

namespace S7Tools.Infrastructure.Logging.Tests.Core.Storage;

/// <summary>
/// Unit tests for the LogDataStore circular buffer implementation.
/// Tests thread safety, circular buffer behavior, and real-time notifications.
/// </summary>
public sealed class LogDataStoreTests : IDisposable
{
    private readonly LogDataStoreOptions _options;
    private readonly LogDataStore _dataStore;

    public LogDataStoreTests()
    {
        _options = new LogDataStoreOptions { MaxEntries = 5 };
        _dataStore = new LogDataStore(_options);
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldInitializeCorrectly()
    {
        // Act & Assert
        _dataStore.Count.Should().Be(0);
        _dataStore.MaxEntries.Should().Be(5);
        _dataStore.IsFull.Should().BeFalse();
        _dataStore.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<LogDataStore> act = () => new LogDataStore(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddEntry_WithValidEntry_ShouldAddToBuffer()
    {
        // Arrange
        LogModel logEntry = CreateLogEntry("Test message", LogLevel.Information);

        // Act
        _dataStore.AddEntry(logEntry);

        // Assert
        _dataStore.Count.Should().Be(1);
        _dataStore.IsFull.Should().BeFalse();
        _dataStore.Entries.Should().HaveCount(1);
        _dataStore.Entries.First().Should().Be(logEntry);
    }

    [Fact]
    public void AddEntry_WithNullEntry_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _dataStore.AddEntry(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddEntry_WhenBufferIsFull_ShouldOverwriteOldestEntry()
    {
        // Arrange
        var entries = new List<LogModel>();
        for (int i = 0; i < 6; i++) // One more than buffer size
        {
            entries.Add(CreateLogEntry($"Message {i}", LogLevel.Information));
        }

        // Act
        foreach (LogModel entry in entries)
        {
            _dataStore.AddEntry(entry);
        }

        // Assert
        _dataStore.Count.Should().Be(5); // Buffer size
        _dataStore.IsFull.Should().BeTrue();
        _dataStore.Entries.Should().HaveCount(5);

        // First entry should be overwritten, so we should have entries 1-5
        _dataStore.Entries.Should().NotContain(e => e.Message == entries[0].Message);
        _dataStore.Entries.Should().Contain(e => e.Message == entries[1].Message);
        _dataStore.Entries.Should().Contain(e => e.Message == entries[5].Message);
    }

    [Fact]
    public void AddEntry_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        LogModel logEntry = CreateLogEntry("Test message", LogLevel.Information);
        var propertyChangedEvents = new List<string>();

        _dataStore.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _dataStore.AddEntry(logEntry);

        // Assert
        propertyChangedEvents.Should().Contain(nameof(LogDataStore.Count));
        propertyChangedEvents.Should().Contain(nameof(LogDataStore.IsFull));
        propertyChangedEvents.Should().Contain(nameof(LogDataStore.Entries));
    }

    [Fact]
    public void AddEntry_ShouldRaiseCollectionChangedEvent()
    {
        // Arrange
        LogModel logEntry = CreateLogEntry("Test message", LogLevel.Information);
        NotifyCollectionChangedEventArgs? collectionChangedArgs = null;

        _dataStore.CollectionChanged += (sender, e) => collectionChangedArgs = e;

        // Act
        _dataStore.AddEntry(logEntry);

        // Assert
        collectionChangedArgs.Should().NotBeNull();
        collectionChangedArgs!.Action.Should().Be(NotifyCollectionChangedAction.Add);
        var newItems = collectionChangedArgs.NewItems as System.Collections.IList;
        Assert.NotNull(newItems);
        Assert.True(newItems.Contains(logEntry));
    }

    [Fact]
    public void AddEntries_WithValidEntries_ShouldAddAllToBuffer()
    {
        // Arrange
        LogModel[] entries = new[]
        {
            CreateLogEntry("Message 1", LogLevel.Information),
            CreateLogEntry("Message 2", LogLevel.Warning),
            CreateLogEntry("Message 3", LogLevel.Error)
        };

        // Act
        _dataStore.AddEntries(entries);

        // Assert
        _dataStore.Count.Should().Be(3);
        _dataStore.Entries.Should().HaveCount(3);
        _dataStore.Entries.Should().Contain(entries);
    }

    [Fact]
    public void AddEntries_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _dataStore.AddEntries(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddEntries_WithEmptyCollection_ShouldNotChangeBuffer()
    {
        // Act
        _dataStore.AddEntries(Array.Empty<LogModel>());

        // Assert
        _dataStore.Count.Should().Be(0);
        _dataStore.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        LogModel[] entries = new[]
        {
            CreateLogEntry("Message 1", LogLevel.Information),
            CreateLogEntry("Message 2", LogLevel.Warning)
        };
        _dataStore.AddEntries(entries);

        // Act
        _dataStore.Clear();

        // Assert
        _dataStore.Count.Should().Be(0);
        _dataStore.IsFull.Should().BeFalse();
        _dataStore.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRaiseNotificationEvents()
    {
        // Arrange
        _dataStore.AddEntry(CreateLogEntry("Test", LogLevel.Information));
        var propertyChangedEvents = new List<string>();
        NotifyCollectionChangedEventArgs? collectionChangedArgs = null;

        _dataStore.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);
        _dataStore.CollectionChanged += (sender, e) => collectionChangedArgs = e;

        // Act
        _dataStore.Clear();

        // Assert
        propertyChangedEvents.Should().Contain(nameof(LogDataStore.Count));
        propertyChangedEvents.Should().Contain(nameof(LogDataStore.IsFull));
        propertyChangedEvents.Should().Contain(nameof(LogDataStore.Entries));

        collectionChangedArgs.Should().NotBeNull();
        collectionChangedArgs!.Action.Should().Be(NotifyCollectionChangedAction.Reset);
    }

    [Fact]
    public void GetFilteredEntries_WithValidFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        LogModel[] entries = new[]
        {
            CreateLogEntry("Info message", LogLevel.Information),
            CreateLogEntry("Warning message", LogLevel.Warning),
            CreateLogEntry("Error message", LogLevel.Error)
        };
        _dataStore.AddEntries(entries);

        // Act
        IEnumerable<LogModel> filteredEntries = _dataStore.GetFilteredEntries(e => e.Level == LogLevel.Warning);

        // Assert
        filteredEntries.Should().HaveCount(1);
        filteredEntries.First().Message.Should().Be("Warning message");
    }

    [Fact]
    public void GetFilteredEntries_WithNullFilter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<IEnumerable<LogModel>> act = () => _dataStore.GetFilteredEntries(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetEntriesInTimeRange_WithValidRange_ShouldReturnMatchingEntries()
    {
        // Arrange
        DateTimeOffset baseTime = DateTimeOffset.Now;
        LogModel[] entries = new[]
        {
            CreateLogEntry("Message 1", LogLevel.Information, baseTime.AddMinutes(-10)),
            CreateLogEntry("Message 2", LogLevel.Information, baseTime),
            CreateLogEntry("Message 3", LogLevel.Information, baseTime.AddMinutes(10))
        };
        _dataStore.AddEntries(entries);

        // Act
        IEnumerable<LogModel> filteredEntries = _dataStore.GetEntriesInTimeRange(
            baseTime.AddMinutes(-5),
            baseTime.AddMinutes(5));

        // Assert
        filteredEntries.Should().HaveCount(1);
        filteredEntries.First().Message.Should().Be("Message 2");
    }

    [Fact]
    public async Task ExportAsync_WithTextFormat_ShouldReturnFormattedText()
    {
        // Arrange
        LogModel entry = CreateLogEntry("Test message", LogLevel.Information);
        _dataStore.AddEntry(entry);

        // Act
        string result = await _dataStore.ExportAsync("txt");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Test message");
        result.Should().Contain("[Information]");
    }

    [Fact]
    public async Task ExportAsync_WithJsonFormat_ShouldReturnValidJson()
    {
        // Arrange
        LogModel entry = CreateLogEntry("Test message", LogLevel.Information);
        _dataStore.AddEntry(entry);

        // Act
        string result = await _dataStore.ExportAsync("json");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("\"message\": \"Test message\"");
        result.Should().Contain("\"level\": \"Information\"");
    }

    [Fact]
    public async Task ExportAsync_WithCsvFormat_ShouldReturnCsvData()
    {
        // Arrange
        LogModel entry = CreateLogEntry("Test message", LogLevel.Information);
        _dataStore.AddEntry(entry);

        // Act
        string result = await _dataStore.ExportAsync("csv");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Timestamp,Level,Category,Message");
        result.Should().Contain("\"Test message\"");
        result.Should().Contain("\"Information\"");
    }

    [Fact]
    public async Task ExportAsync_WithInvalidFormat_ShouldDefaultToText()
    {
        // Arrange
        LogModel entry = CreateLogEntry("Test message", LogLevel.Information);
        _dataStore.AddEntry(entry);

        // Act
        string result = await _dataStore.ExportAsync("invalid");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Test message");
        result.Should().Contain("[Information]");
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAddOperations_ShouldHandleCorrectly()
    {
        // Arrange
        const int threadCount = 10;
        const int entriesPerThread = 100;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < entriesPerThread; j++)
                {
                    LogModel entry = CreateLogEntry($"Thread {threadId} Message {j}", LogLevel.Information);
                    _dataStore.AddEntry(entry);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        _dataStore.Count.Should().Be(_options.MaxEntries); // Should be at max capacity
        _dataStore.IsFull.Should().BeTrue();
        _dataStore.Entries.Should().HaveCount(_options.MaxEntries);
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        _dataStore.AddEntry(CreateLogEntry("Test", LogLevel.Information));

        // Act
        _dataStore.Dispose();

        // Assert
        // Should not throw when adding after dispose
        Action act = () => _dataStore.AddEntry(CreateLogEntry("After dispose", LogLevel.Information));
        act.Should().NotThrow();

        // Count should remain unchanged after dispose
        _dataStore.Count.Should().Be(0);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        Action act = () =>
        {
            _dataStore.Dispose();
            _dataStore.Dispose();
            _dataStore.Dispose();
        };

        act.Should().NotThrow();
    }

    private static LogModel CreateLogEntry(string message, LogLevel level, DateTimeOffset? timestamp = null)
    {
        return new LogModel
        {
            Id = Guid.NewGuid(),
            Timestamp = (timestamp ?? DateTimeOffset.Now).UtcDateTime,
            Level = level,
            Category = "Test.Category",
            Message = message,
            EventId = new EventId(1, "TestEvent"),
            Scope = "TestScope",
            Properties = new Dictionary<string, object?>()
        };
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        _dataStore?.Dispose();
        GC.SuppressFinalize(this);
    }
}
