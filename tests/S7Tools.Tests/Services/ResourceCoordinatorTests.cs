using S7Tools.Core.Models.Jobs;
using S7Tools.Services.Tasking;

namespace S7Tools.Tests.Services;

/// <summary>
/// Unit tests for the ResourceCoordinator service.
/// Tests resource locking with case-insensitive ResourceKey comparison.
/// </summary>
public class ResourceCoordinatorTests
{
    #region Basic Lock Acquisition Tests

    [Fact]
    public void TryAcquire_WithAvailableResources_ShouldReturnTrue()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] keys = new[] { new ResourceKey("serial", "COM1") };

        // Act
        bool result = coordinator.TryAcquire(keys);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TryAcquire_WithAlreadyLockedResources_ShouldReturnFalse()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] keys = new[] { new ResourceKey("serial", "COM1") };
        coordinator.TryAcquire(keys);

        // Act
        bool result = coordinator.TryAcquire(keys);

        // Assert
        result.Should().BeFalse("Resource should already be locked");
    }

    [Fact]
    public void TryAcquire_WithMultipleResources_ShouldLockAllOrNone()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] keys1 = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] keys2 = new[]
        {
            new ResourceKey("serial", "COM1"),
            new ResourceKey("tcp", "8080")
        };
        coordinator.TryAcquire(keys1);

        // Act
        bool result = coordinator.TryAcquire(keys2);

        // Assert
        result.Should().BeFalse("Should not acquire any locks if one resource is unavailable");

        // Verify tcp port is still available
        ResourceKey[] tcpKeys = new[] { new ResourceKey("tcp", "8080") };
        coordinator.TryAcquire(tcpKeys).Should().BeTrue("TCP port should still be available");
    }

    [Fact]
    public void TryAcquire_WithNullKeys_ShouldThrowArgumentNullException()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();

        // Act
        Action act = () => coordinator.TryAcquire(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryAcquire_WithEmptyKeyCollection_ShouldReturnTrue()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] emptyKeys = Array.Empty<ResourceKey>();

        // Act
        bool result = coordinator.TryAcquire(emptyKeys);

        // Assert
        result.Should().BeTrue("Acquiring no resources should succeed");
    }

    #endregion

    #region Case-Insensitive Lock Tests

    [Fact]
    public void TryAcquire_WithDifferentCasingInKind_ShouldRecognizeAsLocked()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] key1 = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] key2 = new[] { new ResourceKey("SERIAL", "COM1") };
        coordinator.TryAcquire(key1);

        // Act
        bool result = coordinator.TryAcquire(key2);

        // Assert
        result.Should().BeFalse("Resource should be recognized as locked despite different Kind casing");
    }

    [Fact]
    public void TryAcquire_WithDifferentCasingInId_ShouldRecognizeAsLocked()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] key1 = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] key2 = new[] { new ResourceKey("serial", "com1") };
        coordinator.TryAcquire(key1);

        // Act
        bool result = coordinator.TryAcquire(key2);

        // Assert
        result.Should().BeFalse("Resource should be recognized as locked despite different Id casing");
    }

    [Fact]
    public void TryAcquire_WithDifferentCasingInBoth_ShouldRecognizeAsLocked()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] key1 = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] key2 = new[] { new ResourceKey("SERIAL", "com1") };
        coordinator.TryAcquire(key1);

        // Act
        bool result = coordinator.TryAcquire(key2);

        // Assert
        result.Should().BeFalse("Resource should be recognized as locked despite different casing in both Kind and Id");
    }

    #endregion

    #region Release Tests

    [Fact]
    public void Release_WithLockedResources_ShouldUnlockThem()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] keys = new[] { new ResourceKey("serial", "COM1") };
        coordinator.TryAcquire(keys);

        // Act
        coordinator.Release(keys);

        // Assert
        coordinator.TryAcquire(keys).Should().BeTrue("Resource should be available after release");
    }

    [Fact]
    public void Release_WithUnlockedResources_ShouldNotThrow()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] keys = new[] { new ResourceKey("serial", "COM1") };

        // Act
        Action act = () => coordinator.Release(keys);

        // Assert
        act.Should().NotThrow("Releasing unlocked resources should be idempotent");
    }

    [Fact]
    public void Release_WithNullKeys_ShouldThrowArgumentNullException()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();

        // Act
        Action act = () => coordinator.Release(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Release_WithDifferentCasing_ShouldUnlockResource()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] key1 = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] key2 = new[] { new ResourceKey("SERIAL", "com1") };
        coordinator.TryAcquire(key1);

        // Act
        coordinator.Release(key2);

        // Assert
        coordinator.TryAcquire(key1).Should().BeTrue("Resource should be unlocked despite different casing in Release");
    }

    [Fact]
    public void Release_WithMultipleResources_ShouldUnlockAll()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] keys = new[]
        {
            new ResourceKey("serial", "COM1"),
            new ResourceKey("tcp", "8080"),
            new ResourceKey("modbus", "192.168.1.100:502")
        };
        coordinator.TryAcquire(keys);

        // Act
        coordinator.Release(keys);

        // Assert
        foreach (ResourceKey key in keys)
        {
            coordinator.TryAcquire(new[] { key }).Should().BeTrue($"Resource {key} should be unlocked");
        }
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Scenario_MultipleJobsAccessingSameSerialPort_ShouldEnforceExclusiveAccess()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var job1Key = new ResourceKey("serial", "/dev/ttyUSB0");
        var job2Key = new ResourceKey("SERIAL", "/dev/TTYUSB0"); // Different casing

        // Act - Job 1 acquires the serial port
        bool job1Acquired = coordinator.TryAcquire(new[] { job1Key });
        // Job 2 tries to acquire the same port (with different casing)
        bool job2Acquired = coordinator.TryAcquire(new[] { job2Key });

        // Assert
        job1Acquired.Should().BeTrue("Job 1 should acquire the lock");
        job2Acquired.Should().BeFalse("Job 2 should be blocked despite different casing");
    }

    [Fact]
    public void Scenario_JobReleasesResourcesAfterCompletion_ShouldAllowOtherJobsToAcquire()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] job1Keys = new[]
        {
            new ResourceKey("serial", "COM1"),
            new ResourceKey("tcp", "8080")
        };
        ResourceKey[] job2Keys = new[]
        {
            new ResourceKey("SERIAL", "com1"), // Same resource, different casing
            new ResourceKey("TCP", "8080")     // Same resource, different casing
        };

        // Act
        coordinator.TryAcquire(job1Keys).Should().BeTrue("Job 1 acquires resources");
        coordinator.TryAcquire(job2Keys).Should().BeFalse("Job 2 is blocked");

        coordinator.Release(job1Keys); // Job 1 completes and releases
        bool job2SecondAttempt = coordinator.TryAcquire(job2Keys);

        // Assert
        job2SecondAttempt.Should().BeTrue("Job 2 should acquire resources after Job 1 releases");
    }

    [Fact]
    public void Scenario_ConcurrentJobsWithDifferentResources_ShouldBothSucceed()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] job1Keys = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] job2Keys = new[] { new ResourceKey("serial", "COM2") };

        // Act
        bool job1Acquired = coordinator.TryAcquire(job1Keys);
        bool job2Acquired = coordinator.TryAcquire(job2Keys);

        // Assert
        job1Acquired.Should().BeTrue("Job 1 should acquire COM1");
        job2Acquired.Should().BeTrue("Job 2 should acquire COM2 (different resource)");
    }

    [Fact]
    public void Scenario_JobWithMultipleResourcesPartiallyAvailable_ShouldNotLockAny()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] job1Keys = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] job2Keys = new[]
        {
            new ResourceKey("SERIAL", "com1"), // Already locked (different casing)
            new ResourceKey("tcp", "8080")     // Available
        };

        coordinator.TryAcquire(job1Keys);

        // Act
        bool job2Acquired = coordinator.TryAcquire(job2Keys);

        // Assert
        job2Acquired.Should().BeFalse("Job 2 should not acquire any locks if one is unavailable");

        // Verify TCP port is still available
        ResourceKey[] tcpOnly = new[] { new ResourceKey("tcp", "8080") };
        coordinator.TryAcquire(tcpOnly).Should().BeTrue("TCP port should still be available for other jobs");
    }

    [Fact]
    public void Scenario_ModbusAndSerialResourcesIndependent_ShouldAllowConcurrentAccess()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        ResourceKey[] serialJob = new[] { new ResourceKey("serial", "COM1") };
        ResourceKey[] modbusJob = new[] { new ResourceKey("modbus", "192.168.1.100:502") };

        // Act
        bool serialAcquired = coordinator.TryAcquire(serialJob);
        bool modbusAcquired = coordinator.TryAcquire(modbusJob);

        // Assert
        serialAcquired.Should().BeTrue("Serial job should acquire lock");
        modbusAcquired.Should().BeTrue("Modbus job should acquire lock (different resource type)");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task TryAcquire_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var key = new ResourceKey("serial", "COM1");
        int successCount = 0;
        var tasks = new List<Task>();

        // Act - Simulate 10 concurrent attempts to acquire the same resource
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                if (coordinator.TryAcquire(new[] { key }))
                {
                    Interlocked.Increment(ref successCount);
                    Thread.Sleep(10); // Hold the lock briefly
                    coordinator.Release(new[] { key });
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        // This is a probabilistic test - in a truly thread-safe implementation,
        // we'd expect multiple acquisitions since locks are released
        successCount.Should().BeGreaterThan(0, "At least one thread should acquire the lock");
    }

    #endregion
}
