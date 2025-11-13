using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Tasking;

namespace S7Tools.Core.Tests.Tasking;

/// <summary>
/// T060-T064: JobScheduler parallel execution tests for User Story 2.
/// Constitutional test-first requirement - tests created BEFORE implementation.
/// Tests parallel job execution with resource coordination and conflict resolution.
/// </summary>
public class SchedulerParallelExecutionTests
{
    private static SerialPortConfiguration CreateDefaultSerialConfig() => new()
    {
        BaudRate = 115200,
        Parity = ParityMode.None,
        CharacterSize = 8,
        StopBits = StopBits.One,
        RawMode = true,
        DisableEcho = true
    };

    private static SocatConfiguration CreateDefaultSocatConfig() => new()
    {
        TcpPort = 8080,
        Verbose = true,
        EnableFork = true,
        EnableReuseAddr = true
    };

    private static PowerSupplyConfiguration CreateDefaultPowerConfig(string host) => new ModbusTcpConfiguration
    {
        Host = host,
        Port = 502,
        DeviceId = 1,
        OnOffCoil = 0,
        AddressingMode = ModbusAddressingMode.Base0
    };

    private static JobProfileSet CreateTestProfileSet(
        string serialDevice,
        int tcpPort,
        string modbusHost = "192.168.1.100")
    {
        return new JobProfileSet(
            Serial: new SerialProfileRef(serialDevice, 115200, "None", 8, "One", CreateDefaultSerialConfig()),
            Socat: new SocatProfileRef(tcpPort, Ephemeral: true, CreateDefaultSocatConfig()),
            Power: new PowerProfileRef(modbusHost, 502, 0, DelaySeconds: 2, CreateDefaultPowerConfig(modbusHost)),
            Memory: new MemoryRegionProfile("0x20000000", 0x1000),
            Payloads: new PayloadSetProfile("/tmp/payloads"),
            OutputPath: "/tmp/dumps"
        );
    }

    /// <summary>
    /// T060: Parallel execution with independent resources.
    /// Test 2 jobs with different serials run concurrently.
    /// Expected: Both jobs start within 1 second of each other.
    /// </summary>
    [Fact]
    public async Task ParallelExecution_WithIndependentResources_RunsConcurrently()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var mockBootloader = new Mock<IBootloaderService>();

        // Simulate 2-second execution time
        mockBootloader.Setup(b => b.DumpAsync(
            It.IsAny<JobProfileSet>(),
            It.IsAny<IProgress<(string stage, double percent)>>(),
            It.IsAny<Microsoft.Extensions.Logging.ILogger>(),
            It.IsAny<CancellationToken>()))
            .Returns(async (JobProfileSet profiles, IProgress<(string stage, double percent)> progress, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken ct) =>
            {
                await Task.Delay(2000, ct);
                return new byte[0x1000];
            });

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            coordinator,
            mockBootloader.Object
        );

        Job job1 = new()
        {
            Id = 1,
            Name = "Job 1 - USB0",
            Description = "First job on /dev/ttyUSB0",
            ProfileSet = CreateTestProfileSet("/dev/ttyUSB0", 10102, "192.168.1.100"),
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        Job job2 = new()
        {
            Id = 2,
            Name = "Job 2 - USB1",
            Description = "Second job on /dev/ttyUSB1",
            ProfileSet = CreateTestProfileSet("/dev/ttyUSB1", 10103, "192.168.1.101"), // Different power supply!
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        var startTimes = new Dictionary<int, DateTime>();
        scheduler.JobStateChanged += (sender, args) =>
        {
            if (args.NewState == JobState.Running)
            {
                lock (startTimes)
                {
                    startTimes[args.JobId] = DateTime.UtcNow;
                }
            }
        };

        // Act
        await scheduler.EnqueueAsync(job1);
        await scheduler.EnqueueAsync(job2);
        await scheduler.StartAsync(CancellationToken.None);

        // Wait for both jobs to start (with timeout)
        DateTime timeout = DateTime.UtcNow.AddSeconds(5);
        while (startTimes.Count < 2 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(100);
        }

        await scheduler.StopAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        // Assert
        startTimes.Should().HaveCount(2, "both jobs should start");

        DateTime job1Start = startTimes[1];
        DateTime job2Start = startTimes[2];
        double timeDifference = Math.Abs((job2Start - job1Start).TotalSeconds);

        timeDifference.Should().BeLessThan(1.0,
            "jobs with independent resources should start within 1 second of each other");
    }

    /// <summary>
    /// T061: Sequential execution with conflicting resources.
    /// Test 2 jobs with same serial run sequentially.
    /// Expected: Job 1 completes before Job 2 starts.
    /// </summary>
    [Fact]
    public async Task SequentialExecution_WithConflictingResources_RunsInOrder()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var mockBootloader = new Mock<IBootloaderService>();

        // Simulate 1-second execution time
        mockBootloader.Setup(b => b.DumpAsync(
            It.IsAny<JobProfileSet>(),
            It.IsAny<IProgress<(string stage, double percent)>>(),
            It.IsAny<Microsoft.Extensions.Logging.ILogger>(),
            It.IsAny<CancellationToken>()))
            .Returns(async (JobProfileSet profiles, IProgress<(string stage, double percent)> progress, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken ct) =>
            {
                await Task.Delay(1000, ct);
                return new byte[0x1000];
            });

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            coordinator,
            mockBootloader.Object
        );

        Job job1 = new()
        {
            Id = 1,
            Name = "Job 1 - USB0",
            Description = "First job on /dev/ttyUSB0",
            ProfileSet = CreateTestProfileSet("/dev/ttyUSB0", 10102),
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        Job job2 = new()
        {
            Id = 2,
            Name = "Job 2 - USB0 (conflict)",
            Description = "Second job on SAME /dev/ttyUSB0",
            ProfileSet = CreateTestProfileSet("/dev/ttyUSB0", 10103), // Same serial!
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        var timestamps = new Dictionary<int, (DateTime? start, DateTime? complete)>();
        scheduler.JobStateChanged += (sender, args) =>
        {
            lock (timestamps)
            {
                if (!timestamps.ContainsKey(args.JobId))
                {
                    timestamps[args.JobId] = (null, null);
                }

                if (args.NewState == JobState.Running)
                {
                    timestamps[args.JobId] = (DateTime.UtcNow, timestamps[args.JobId].complete);
                }
                else if (args.NewState == JobState.Completed)
                {
                    timestamps[args.JobId] = (timestamps[args.JobId].start, DateTime.UtcNow);
                }
            }
        };

        // Act
        await scheduler.EnqueueAsync(job1);
        await scheduler.EnqueueAsync(job2);
        await scheduler.StartAsync(CancellationToken.None);

        // Wait for both jobs to complete (with timeout)
        DateTime timeout = DateTime.UtcNow.AddSeconds(10);
        while ((!timestamps.ContainsKey(1) || !timestamps[1].complete.HasValue ||
                !timestamps.ContainsKey(2) || !timestamps[2].complete.HasValue) &&
               DateTime.UtcNow < timeout)
        {
            await Task.Delay(100);
        }

        await scheduler.StopAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        // Assert
        timestamps.Should().HaveCount(2, "both jobs should execute");
        timestamps[1].start.Should().NotBeNull("job1 should start");
        timestamps[1].complete.Should().NotBeNull("job1 should complete");
        timestamps[2].start.Should().NotBeNull("job2 should start");
        timestamps[2].complete.Should().NotBeNull("job2 should complete");

        // Job 1 must complete BEFORE Job 2 starts (sequential execution)
        timestamps[1].complete!.Value.Should().BeBefore(timestamps[2].start!.Value,
            "job1 must complete before job2 starts due to resource conflict");
    }

    /// <summary>
    /// T062: Resource cleanup after job completion.
    /// Test resources released after job finishes successfully.
    /// Expected: Resources available for new job after completion.
    /// </summary>
    [Fact]
    public async Task ResourceCleanup_AfterCompletion_ReleasesResources()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resources = new List<ResourceKey>
        {
            new("serial", "/dev/ttyUSB0"),
            new("tcp", "10102"),
            new("modbus", "192.168.1.100:502")
        };

        // Act - Acquire resources for job
        bool acquired = coordinator.TryAcquire(resources);
        acquired.Should().BeTrue("initial acquisition should succeed");
        coordinator.GetLockedResources().Should().HaveCount(3);

        // Simulate job completion and release
        coordinator.Release(resources);

        // Assert - Resources should be available again
        coordinator.GetLockedResources().Should().BeEmpty("resources should be released");

        bool reacquired = coordinator.TryAcquire(resources);
        reacquired.Should().BeTrue("resources should be available for new job");
    }

    /// <summary>
    /// T063: Resource cleanup after job failure.
    /// Test resources released when job fails with exception.
    /// Expected: Resources released even on exception, available for retry.
    /// </summary>
    [Fact]
    public async Task ResourceCleanup_AfterFailure_ReleasesResources()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var mockBootloader = new Mock<IBootloaderService>();

        // Simulate job failure with exception
        mockBootloader.Setup(b => b.DumpAsync(
            It.IsAny<JobProfileSet>(),
            It.IsAny<IProgress<(string stage, double percent)>>(),
            It.IsAny<Microsoft.Extensions.Logging.ILogger>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated bootloader failure"));

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            coordinator,
            mockBootloader.Object
        );

        Job failingJob = new()
        {
            Id = 1,
            Name = "Failing Job",
            Description = "Job that will fail",
            ProfileSet = CreateTestProfileSet("/dev/ttyUSB0", 10102),
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        bool jobFailed = false;
        scheduler.JobStateChanged += (sender, args) =>
        {
            if (args.NewState == JobState.Failed)
            {
                jobFailed = true;
            }
        };

        // Act
        await scheduler.EnqueueAsync(failingJob);
        await scheduler.StartAsync(CancellationToken.None);

        // Wait for job to fail (with timeout)
        DateTime timeout = DateTime.UtcNow.AddSeconds(5);
        while (!jobFailed && DateTime.UtcNow < timeout)
        {
            await Task.Delay(100);
        }

        await scheduler.StopAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        // Assert
        jobFailed.Should().BeTrue("job should transition to Failed state");
        coordinator.GetLockedResources().Should().BeEmpty(
            "resources should be released even after job failure");
    }

    /// <summary>
    /// T064: Integration test for 4+ concurrent jobs (SC-002 requirement).
    /// Test system can handle 4 jobs running simultaneously.
    /// Expected: All 4 jobs execute in parallel without blocking.
    /// </summary>
    [Fact]
    public async Task ConcurrentExecution_With4Jobs_AllRunSimultaneously()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var mockBootloader = new Mock<IBootloaderService>();

        // Simulate 2-second execution time
        mockBootloader.Setup(b => b.DumpAsync(
            It.IsAny<JobProfileSet>(),
            It.IsAny<IProgress<(string stage, double percent)>>(),
            It.IsAny<Microsoft.Extensions.Logging.ILogger>(),
            It.IsAny<CancellationToken>()))
            .Returns(async (JobProfileSet profiles, IProgress<(string stage, double percent)> progress, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken ct) =>
            {
                await Task.Delay(2000, ct);
                return new byte[0x1000];
            });

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            coordinator,
            mockBootloader.Object
        );

        // Create 4 jobs with unique serial ports, TCP ports, AND modbus hosts
        var jobs = new List<Job>
        {
            new() { Id = 1, Name = "Job 1", ProfileSet = CreateTestProfileSet("/dev/ttyUSB0", 10102, "192.168.1.100"), State = JobState.Created, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Job 2", ProfileSet = CreateTestProfileSet("/dev/ttyUSB1", 10103, "192.168.1.101"), State = JobState.Created, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "Job 3", ProfileSet = CreateTestProfileSet("/dev/ttyUSB2", 10104, "192.168.1.102"), State = JobState.Created, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Name = "Job 4", ProfileSet = CreateTestProfileSet("/dev/ttyUSB3", 10105, "192.168.1.103"), State = JobState.Created, CreatedAt = DateTime.UtcNow }
        };

        var runningJobs = new HashSet<int>();
        int maxConcurrent = 0;
        object lockObject = new object();

        scheduler.JobStateChanged += (sender, args) =>
        {
            lock (lockObject)
            {
                if (args.NewState == JobState.Running)
                {
                    runningJobs.Add(args.JobId);
                    maxConcurrent = Math.Max(maxConcurrent, runningJobs.Count);
                }
                else if (args.NewState == JobState.Completed || args.NewState == JobState.Failed)
                {
                    runningJobs.Remove(args.JobId);
                }
            }
        };

        // Act
        foreach (Job job in jobs)
        {
            await scheduler.EnqueueAsync(job);
        }

        await scheduler.StartAsync(CancellationToken.None);

        // Wait for all jobs to start running (with timeout)
        DateTime timeout = DateTime.UtcNow.AddSeconds(10);
        while (maxConcurrent < 4 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(100);
        }

        await scheduler.StopAsync(TimeSpan.FromSeconds(10), CancellationToken.None);

        // Assert
        maxConcurrent.Should().BeGreaterOrEqualTo(4,
            "SC-002 requirement: system must support 4+ concurrent jobs");
    }
}
