// ResourceCoordinatorTests.cs
using S7Tools.Core.Models.Jobs;
using S7Tools.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ResourceCoordinatorTests
{
    [Fact]
    public void TryAcquire_WithNoLocks_ShouldSucceed()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resources = new[] { new ResourceKey("serial", "/dev/ttyUSB0") };

        // Act
        var result = coordinator.TryAcquire(resources);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryAcquire_WithExistingLock_ShouldFail()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resource = new ResourceKey("serial", "/dev/ttyUSB0");
        coordinator.TryAcquire(new[] { resource });

        // Act
        var result = coordinator.TryAcquire(new[] { resource });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryAcquire_WithDisjointLocks_ShouldSucceed()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resource1 = new ResourceKey("serial", "/dev/ttyUSB0");
        var resource2 = new ResourceKey("serial", "/dev/ttyUSB1");
        coordinator.TryAcquire(new[] { resource1 });

        // Act
        var result = coordinator.TryAcquire(new[] { resource2 });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Release_ShouldAllowReacquiringLock()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resource = new ResourceKey("serial", "/dev/ttyUSB0");
        coordinator.TryAcquire(new[] { resource });
        coordinator.Release(new[] { resource });

        // Act
        var result = coordinator.TryAcquire(new[] { resource });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryAcquire_IsThreadSafe()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resource = new ResourceKey("serial", "/dev/ttyUSB0");
        var tasks = new List<Task<bool>>();
        int successCount = 0;

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => coordinator.TryAcquire(new[] { resource })));
        }
        Task.WhenAll(tasks).Wait();
        successCount = tasks.Count(t => t.Result);


        // Assert
        Assert.Equal(1, successCount);
    }
}