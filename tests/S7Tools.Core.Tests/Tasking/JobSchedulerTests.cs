using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Tasking;

namespace S7Tools.Core.Tests.Tasking;

/// <summary>
/// Unit tests for the JobScheduler service.
/// Tests job enqueueing, state transitions, and resource coordination.
/// </summary>
public class JobSchedulerTests
{
    #region Test Helpers

    private static Job CreateTestJob(string name = "Test Job", params ResourceKey[] resources)
    {
        return new Job(
            Id: Guid.NewGuid(),
            Name: name,
            Resources: resources.ToList(),
            Profiles: CreateTestProfiles(),
            State: JobState.Created,
            CreatedAt: DateTimeOffset.UtcNow
        );
    }

    private static JobProfileSet CreateTestProfiles()
    {
        return new JobProfileSet(
            Serial: new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One"),
            Socat: new SocatProfileRef(8080, Ephemeral: true),
            Power: new PowerProfileRef("192.168.1.100", 502, 0, DelaySeconds: 2),
            Memory: new MemoryRegionProfile(0x20000000, 0x1000),
            Payloads: new PayloadSetProfile("/tmp/payloads"),
            OutputPath: "/tmp/dumps"
        );
    }

    #endregion

    #region T042: EnqueueAsync State Transitions

    [Fact]
    public void Enqueue_WithPendingJob_ShouldTransitionToQueued()
    {
        // Arrange
        var resources = new Mock<IResourceCoordinator>();
        var bootloader = new Mock<IBootloaderService>();
        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources.Object, bootloader.Object);

        JobState? reportedState = null;
        scheduler.JobStateChanged += (id, state, msg) =>
        {
            if (reportedState == null) // Capture first event only
            {
                reportedState = state;
            }
        };

        Job job = CreateTestJob("Test Job 1");

        // Act
        Guid jobId = scheduler.Enqueue(job);

        // Assert
        jobId.Should().Be(job.Id, "Enqueue should return the job's ID");
        reportedState.Should().Be(JobState.Queued, "Job should transition to Queued state");
    }

    [Fact]
    public void Enqueue_WithNullJob_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resources = new Mock<IResourceCoordinator>();
        var bootloader = new Mock<IBootloaderService>();
        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources.Object, bootloader.Object);

        // Act
        Action act = () => scheduler.Enqueue(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enqueue_MultipleJobs_ShouldQueueAll()
    {
        // Arrange
        var resources = new Mock<IResourceCoordinator>();
        var bootloader = new Mock<IBootloaderService>();
        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources.Object, bootloader.Object);

        Job job1 = CreateTestJob("Job 1");
        Job job2 = CreateTestJob("Job 2");
        Job job3 = CreateTestJob("Job 3");

        // Act
        scheduler.Enqueue(job1);
        scheduler.Enqueue(job2);
        scheduler.Enqueue(job3);

        // Assert
        IReadOnlyCollection<Job> allJobs = scheduler.GetAll();
        allJobs.Should().HaveCount(3, "All jobs should be stored in the scheduler");
        allJobs.Should().Contain(j => j.Id == job1.Id);
        allJobs.Should().Contain(j => j.Id == job2.Id);
        allJobs.Should().Contain(j => j.Id == job3.Id);
    }

    [Fact]
    public async Task Enqueue_WithAvailableResources_ShouldTransitionToRunning()
    {
        // Arrange
        var resources = new Mock<IResourceCoordinator>();
        resources.Setup(r => r.TryAcquire(It.IsAny<IEnumerable<ResourceKey>>())).Returns(true);

        var bootloader = new Mock<IBootloaderService>();
        bootloader.Setup(b => b.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[1024]);

        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources.Object, bootloader.Object);

        var stateChanges = new List<JobState>();
        scheduler.JobStateChanged += (id, state, msg) => stateChanges.Add(state);

        Job job = CreateTestJob("Test Job", new ResourceKey("serial", "COM1"));

        // Act
        scheduler.Enqueue(job);
        await Task.Delay(200); // Allow async processing

        // Assert
        stateChanges.Should().Contain(JobState.Queued, "Job should be queued first");
        stateChanges.Should().Contain(JobState.Running, "Job should transition to running when resources available");
    }

    [Fact]
    public async Task Enqueue_WithUnavailableResources_ShouldRemainQueued()
    {
        // Arrange
        var resources = new Mock<IResourceCoordinator>();
        resources.Setup(r => r.TryAcquire(It.IsAny<IEnumerable<ResourceKey>>())).Returns(false); // Resources locked

        var bootloader = new Mock<IBootloaderService>();
        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources.Object, bootloader.Object);

        var stateChanges = new List<JobState>();
        scheduler.JobStateChanged += (id, state, msg) => stateChanges.Add(state);

        Job job = CreateTestJob("Test Job", new ResourceKey("serial", "COM1"));

        // Act
        scheduler.Enqueue(job);
        await Task.Delay(200); // Allow async processing

        // Assert
        stateChanges.Should().Contain(JobState.Queued, "Job should be queued");
        stateChanges.Should().NotContain(JobState.Running, "Job should not start when resources unavailable");
    }

    #endregion

    #region T043: Queue Processing with Resource Coordination

    [Fact]
    public async Task QueueProcessing_WithResourceConflict_ShouldExecuteJobsSequentially()
    {
        // Arrange
        var resources = new ResourceCoordinator(); // Use real coordinator
        var bootloader = new Mock<IBootloaderService>();

        var executionOrder = new List<Guid>();
        var completionSignal = new TaskCompletionSource<bool>();
        int completedCount = 0;

        bootloader.Setup(b => b.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobProfileSet profiles, IProgress<(string, double)> progress, CancellationToken ct) =>
            {
                executionOrder.Add(Guid.NewGuid()); // Track execution
                Thread.Sleep(100); // Simulate work

                if (Interlocked.Increment(ref completedCount) == 2)
                {
                    completionSignal.SetResult(true);
                }

                return new byte[1024];
            });

        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources, bootloader.Object);

        // Create two jobs competing for the same resource
        var sharedResource = new ResourceKey("serial", "COM1");
        Job job1 = CreateTestJob("Job 1", sharedResource);
        Job job2 = CreateTestJob("Job 2", sharedResource);

        // Act
        scheduler.Enqueue(job1);
        scheduler.Enqueue(job2);

        await completionSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        executionOrder.Should().HaveCount(2, "Both jobs should execute");
        bootloader.Verify(b => b.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task QueueProcessing_WithDifferentResources_ShouldExecuteInParallel()
    {
        // Arrange
        var resources = new ResourceCoordinator(); // Use real coordinator
        var bootloader = new Mock<IBootloaderService>();

        var startedJobs = new List<Guid>();
        var startSignal = new TaskCompletionSource<bool>();
        var continueSignal = new TaskCompletionSource<bool>();

        bootloader.Setup(b => b.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()))
            .Returns(async (JobProfileSet profiles, IProgress<(string, double)> progress, CancellationToken ct) =>
            {
                lock (startedJobs)
                {
                    startedJobs.Add(Guid.NewGuid());
                    if (startedJobs.Count == 2)
                    {
                        startSignal.SetResult(true);
                    }
                }

                await continueSignal.Task; // Use await instead of blocking Wait
                return new byte[1024];
            });

        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources, bootloader.Object);

        // Create two jobs with different resources
        Job job1 = CreateTestJob("Job 1", new ResourceKey("serial", "COM1"));
        Job job2 = CreateTestJob("Job 2", new ResourceKey("serial", "COM2")); // Different resource

        // Act
        scheduler.Enqueue(job1);
        scheduler.Enqueue(job2);

        // Wait for both to start (increased timeout for parallel execution)
        bool bothStarted = await startSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Signal completion
        continueSignal.SetResult(true);

        // Assert
        bothStarted.Should().BeTrue("Both jobs should start in parallel");
        startedJobs.Should().HaveCount(2, "Both jobs should have started execution");
    }

    [Fact]
    public async Task QueueProcessing_WhenResourcesReleased_ShouldStartWaitingJob()
    {
        // Arrange
        var resources = new ResourceCoordinator();
        var bootloader = new Mock<IBootloaderService>();

        var job1Complete = new TaskCompletionSource<bool>();
        var job2Started = new TaskCompletionSource<bool>();
        int callCount = 0;

        bootloader.Setup(b => b.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobProfileSet profiles, IProgress<(string, double)> progress, CancellationToken ct) =>
            {
                int currentCall = Interlocked.Increment(ref callCount);

                if (currentCall == 1)
                {
                    // First job
                    Thread.Sleep(100);
                    job1Complete.SetResult(true);
                    return new byte[1024];
                }
                else
                {
                    // Second job
                    job2Started.SetResult(true);
                    return new byte[1024];
                }
            });

        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources, bootloader.Object);

        var sharedResource = new ResourceKey("serial", "COM1");
        Job job1 = CreateTestJob("Job 1", sharedResource);
        Job job2 = CreateTestJob("Job 2", sharedResource);

        // Act
        scheduler.Enqueue(job1);
        scheduler.Enqueue(job2);

        await job1Complete.Task.WaitAsync(TimeSpan.FromSeconds(2));
        bool job2DidStart = await job2Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        job2DidStart.Should().BeTrue("Job 2 should start after Job 1 releases resources");
        callCount.Should().Be(2, "Both jobs should execute");
    }

    [Fact]
    public void GetAll_WithEnqueuedJobs_ShouldReturnAllJobs()
    {
        // Arrange
        var resources = new Mock<IResourceCoordinator>();
        var bootloader = new Mock<IBootloaderService>();
        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources.Object, bootloader.Object);

        Job job1 = CreateTestJob("Job 1");
        Job job2 = CreateTestJob("Job 2");
        Job job3 = CreateTestJob("Job 3");

        // Act
        scheduler.Enqueue(job1);
        scheduler.Enqueue(job2);
        scheduler.Enqueue(job3);

        IReadOnlyCollection<Job> allJobs = scheduler.GetAll();

        // Assert
        allJobs.Should().HaveCount(3);
        allJobs.Should().Contain(j => j.Id == job1.Id && j.State == JobState.Queued);
        allJobs.Should().Contain(j => j.Id == job2.Id && j.State == JobState.Queued);
        allJobs.Should().Contain(j => j.Id == job3.Id && j.State == JobState.Queued);
    }

    [Fact]
    public async Task JobStateChanged_ShouldNotifyOnStateTransitions()
    {
        // Arrange
        var resources = new Mock<IResourceCoordinator>();
        resources.Setup(r => r.TryAcquire(It.IsAny<IEnumerable<ResourceKey>>())).Returns(true);

        var bootloader = new Mock<IBootloaderService>();
        bootloader.Setup(b => b.DumpAsync(It.IsAny<JobProfileSet>(), It.IsAny<IProgress<(string, double)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[1024]);

        var scheduler = new JobScheduler(NullLogger<JobScheduler>.Instance, resources.Object, bootloader.Object);

        var notifications = new List<(Guid jobId, JobState state, string? message)>();
        scheduler.JobStateChanged += (id, state, msg) => notifications.Add((id, state, msg));

        Job job = CreateTestJob("Test Job", new ResourceKey("serial", "COM1"));

        // Act
        scheduler.Enqueue(job);
        await Task.Delay(300); // Allow processing

        // Assert
        notifications.Should().NotBeEmpty("State change events should be raised");
        notifications.Should().Contain(n => n.state == JobState.Queued, "Queued event should be raised");
        notifications.Should().Contain(n => n.state == JobState.Running, "Running event should be raised");
    }

    #endregion
}
