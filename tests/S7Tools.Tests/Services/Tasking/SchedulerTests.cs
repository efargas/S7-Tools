// SchedulerTests.cs
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Profiles;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Tasking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class SchedulerTests
{
    private readonly Mock<ILogger<JobScheduler>> _loggerMock;
    private readonly Mock<IResourceCoordinator> _resourceCoordinatorMock;
    private readonly Mock<IBootloaderService> _bootloaderServiceMock;
    private readonly JobScheduler _scheduler;

    public SchedulerTests()
    {
        _loggerMock = new Mock<ILogger<JobScheduler>>();
        _resourceCoordinatorMock = new Mock<IResourceCoordinator>();
        _bootloaderServiceMock = new Mock<IBootloaderService>();
        _scheduler = new JobScheduler(_loggerMock.Object, _resourceCoordinatorMock.Object, _bootloaderServiceMock.Object);
    }

    private Job CreateTestJob(string id, params ResourceKey[] resources)
    {
        var profiles = new JobProfileSet(
            new SerialProfileRef($"/dev/ttyUSB{id}", 115200, "None", 8, "One"),
            new SocatProfileRef(20000 + int.Parse(id), true),
            new PowerProfileRef("127.0.0.1", 502, int.Parse(id), 3),
            new MemoryRegionProfile(0x00000000, 0x00010000),
            new PayloadSetProfile("resources/payloads"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dumps"));
        return new Job(Guid.NewGuid(), $"Test Job {id}", resources, profiles, JobState.Created, DateTimeOffset.Now);
    }

    [Fact]
    public async Task Enqueue_ShouldAddJobToQueue()
    {
        // Arrange
        var job = CreateTestJob("1", new ResourceKey("serial", "1"));
        _resourceCoordinatorMock.Setup(rc => rc.TryAcquire(It.IsAny<IEnumerable<ResourceKey>>())).Returns(false);

        // Act
        _scheduler.Enqueue(job);

        // Give the scheduler a moment to attempt processing.
        await Task.Delay(250);

        // Assert
        Assert.Single(_scheduler.GetAll());
        Assert.Equal(JobState.Queued, _scheduler.GetAll().First().State);
    }

    [Fact]
    public async Task Scheduler_ShouldRunJob_WhenResourcesAreAvailable()
    {
        // Arrange
        var job = CreateTestJob("1", new ResourceKey("serial", "1"));
        var jobCompleted = new TaskCompletionSource<bool>();
        _resourceCoordinatorMock.Setup(rc => rc.TryAcquire(It.IsAny<IEnumerable<ResourceKey>>())).Returns(true);
        _bootloaderServiceMock.Setup(bs => bs.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[0]);

        _scheduler.JobStateChanged += (id, state, msg) =>
        {
            if (state == JobState.Completed)
            {
                jobCompleted.SetResult(true);
            }
        };

        // Act
        _scheduler.Enqueue(job);
        await jobCompleted.Task;

        // Assert
        _bootloaderServiceMock.Verify(bs => bs.DumpAsync(job.Profiles, It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(JobState.Completed, _scheduler.GetAll().First().State);
    }

    [Fact]
    public async Task Scheduler_ShouldQueueJob_WhenResourcesAreNotAvailable()
    {
        // Arrange
        var resource = new ResourceKey("serial", "1");
        var job1 = CreateTestJob("1", resource);
        var job2 = CreateTestJob("2", resource);
        var job1Completed = new TaskCompletionSource<bool>();
        var job2Completed = new TaskCompletionSource<bool>();

        _resourceCoordinatorMock.SetupSequence(rc => rc.TryAcquire(It.IsAny<IEnumerable<ResourceKey>>()))
            .Returns(true)  // job1 acquires lock
            .Returns(false) // job2 fails to acquire lock
            .Returns(true); // job2 acquires lock after release

        _bootloaderServiceMock.Setup(bs => bs.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => {
                await Task.Delay(10); // Simulate work
                return new byte[0];
            });

        _scheduler.JobStateChanged += (id, state, msg) =>
        {
            if (id == job1.Id && state == JobState.Completed) job1Completed.TrySetResult(true);
            if (id == job2.Id && state == JobState.Completed) job2Completed.TrySetResult(true);
        };

        // Act
        _scheduler.Enqueue(job1);
        _scheduler.Enqueue(job2);

        await job1Completed.Task;
        _resourceCoordinatorMock.Verify(rc => rc.Release(It.Is<IEnumerable<ResourceKey>>(r => r.SequenceEqual(job1.Resources))), Times.Once);
        await job2Completed.Task;


        // Assert
        Assert.Equal(JobState.Completed, _scheduler.GetAll().First(j => j.Id == job1.Id).State);
        Assert.Equal(JobState.Completed, _scheduler.GetAll().First(j => j.Id == job2.Id).State);
    }
}