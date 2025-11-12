using FluentAssertions;
using S7Tools.Core.Models.Jobs;
using S7Tools.Services.Tasking;

namespace S7Tools.Core.Tests.Tasking;

public class ResourceCoordinatorTests
{
    [Fact]
    public void TryAcquire_WithAvailableResources_ReturnsTrue()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resources = new List<ResourceKey>
        {
            new("serial", "/dev/ttyUSB0"),
            new("tcp", "10102")
        };

        // Act
        bool result = coordinator.TryAcquire(resources);

        // Assert
        result.Should().BeTrue();
        coordinator.GetLockedResources().Should().HaveCount(2);
    }

    [Fact]
    public void TryAcquire_WithLockedResource_ReturnsFalse()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var serial = new ResourceKey("serial", "/dev/ttyUSB0");
        coordinator.TryAcquire(new[] { serial });

        // Act
        bool result = coordinator.TryAcquire(new[] { serial });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryAcquire_WithPartialConflict_AcquiresZeroResources()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var tcp = new ResourceKey("tcp", "10102");
        coordinator.TryAcquire(new[] { tcp });

        var resources = new List<ResourceKey>
        {
            new("serial", "/dev/ttyUSB0"),
            tcp // Already locked
        };

        // Act
        bool result = coordinator.TryAcquire(resources);

        // Assert
        result.Should().BeFalse();
        coordinator.GetLockedResources().Should().HaveCount(1); // Only TCP still locked
        coordinator.GetLockedResources().Should().NotContain(kvp => kvp.Key.Kind == "serial");
    }

    [Fact]
    public void Release_UnlocksResourcesAndRaisesEvent()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resources = new List<ResourceKey>
        {
            new("serial", "/dev/ttyUSB0"),
            new("tcp", "10102")
        };
        coordinator.TryAcquire(resources);

        ResourceLockChangedEventArgs? eventArgs = null;
        coordinator.ResourceLockChanged += (_, e) => eventArgs = e;

        // Act
        coordinator.Release(resources);

        // Assert
        coordinator.GetLockedResources().Should().BeEmpty();
        eventArgs.Should().NotBeNull();
        eventArgs!.Action.Should().Be(ResourceLockAction.Released);
    }
}
