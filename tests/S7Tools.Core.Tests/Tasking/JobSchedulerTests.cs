using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using S7Tools.Services.Tasking;

namespace S7Tools.Core.Tests.Tasking;

/// <summary>
/// T042-T043: JobScheduler unit tests for bootloader integration.
/// Constitutional test-first requirement - tests created before implementation.
/// </summary>
public class JobSchedulerTests
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

    private static PowerSupplyConfiguration CreateDefaultPowerConfig() => new ModbusTcpConfiguration
    {
        Host = "192.168.1.100",
        Port = 502,
        DeviceId = 1,
        OnOffCoil = 0,
        AddressingMode = ModbusAddressingMode.Base0
    };

    private static JobProfileSet CreateTestProfileSet()
    {
        return new JobProfileSet(
            Serial: new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One", CreateDefaultSerialConfig()),
            Socat: new SocatProfileRef(8080, Ephemeral: true, CreateDefaultSocatConfig()),
            Power: new PowerProfileRef("192.168.1.100", 502, 0, DelaySeconds: 2, CreateDefaultPowerConfig()),
            Memory: new MemoryRegionProfile("0x20000000", 0x1000),
            Payloads: new PayloadSetProfile { BasePath = "/tmp/payloads" },
            OutputPath: "/tmp/dumps"
        );
    }

    /// <summary>
    /// T042: EnqueueAsync - validates state transition from Created → Queued with event notification.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_WithCreatedJob_ShouldTransitionToQueued()
    {
        // Arrange
        var mockResources = new Mock<IResourceCoordinator>();
        var mockBootloader = new Mock<IBootloaderService>();
        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.GetLocalNow()).Returns(() => DateTime.Now);
        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            mockResources.Object,
            mockBootloader.Object,
            mockTime.Object
        );

        Job testJob = new()
        {
            Id = 1,
            Name = "Test Bootloader Job",
            Description = "Test job for scheduler",
            ProfileSet = CreateTestProfileSet(),
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        JobStateChangedEventArgs? eventArgs = null;
        scheduler.JobStateChanged += (sender, args) =>
        {
            eventArgs = args;
        };

        // Act
        Job enqueuedJob = await scheduler.EnqueueAsync(testJob);

        // Assert
        enqueuedJob.Should().NotBeNull();
        enqueuedJob.State.Should().Be(JobState.Queued);
        enqueuedJob.QueuedAt.Should().NotBeNull();
        enqueuedJob.QueuedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));

        eventArgs.Should().NotBeNull();
        eventArgs!.JobId.Should().Be(1);
        eventArgs.PreviousState.Should().Be(JobState.Created);
        eventArgs.NewState.Should().Be(JobState.Queued);
    }

    /// <summary>
    /// T043: ProcessQueueAsync - validates background processing loop picks up queued jobs.
    /// Tests scheduler Start/Stop lifecycle.
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldBeginQueueProcessing()
    {
        // Arrange
        var mockResources = new Mock<IResourceCoordinator>();
        var mockBootloader = new Mock<IBootloaderService>();
        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.GetLocalNow()).Returns(() => DateTime.Now);
        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            mockResources.Object,
            mockBootloader.Object,
            mockTime.Object
        );

        // Act
        await scheduler.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Allow scheduler to start
        await scheduler.StopAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert - scheduler should start and stop without throwing
        // (Actual queue processing tested in integration tests)
    }

    /// <summary>
    /// T044: ExecuteJobAsync - when DumpAsync throws PartialDumpException the job transitions
    /// Running → Failed, ErrorMessage includes the partial-file count, and JobStateChanged is raised
    /// with the correct states and a non-null ErrorMessage.
    /// </summary>
    [Fact]
    public async Task ExecuteJobAsync_WhenDumpThrowsPartialDumpException_TransitionsToFailedWithPartialFileInfo()
    {
        // Arrange
        var mockResources = new Mock<IResourceCoordinator>();
        mockResources.Setup(r => r.TryAcquire(It.IsAny<ResourceKey[]>())).Returns(true);

        var partialFiles = new List<string> { "/tmp/dumps/MemoryDump_partial.partial.bin" };
        var partialResult = new BootloaderResult(partialFiles);
        var innerEx = new InvalidOperationException("Connection lost during dump");
        var partialEx = new PartialDumpException(
            "Dump interrupted at iteration 1/1: Connection lost during dump",
            innerEx,
            partialResult);

        var mockBootloader = new Mock<IBootloaderService>();
        mockBootloader
            .Setup(b => b.DumpAsync(
                It.IsAny<JobProfileSet>(),
                It.IsAny<IProgress<(string, double, long?, long?)>>(),
                It.IsAny<Microsoft.Extensions.Logging.ILogger?>(),
                It.IsAny<Microsoft.Extensions.Logging.ILogger?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(partialEx);

        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.GetLocalNow()).Returns(() => DateTime.Now);

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            mockResources.Object,
            mockBootloader.Object,
            mockTime.Object
        );

        Job testJob = new()
        {
            Id = 42,
            Name = "Partial Dump Test Job",
            Description = "Tests PartialDumpException handling",
            ProfileSet = CreateTestProfileSet(),
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        var failedTcs = new TaskCompletionSource<JobStateChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        scheduler.JobStateChanged += (_, args) =>
        {
            if (args.NewState == JobState.Failed)
            {
                failedTcs.TrySetResult(args);
            }
        };

        // Act: enqueue and start the scheduler, then wait for the Failed transition
        await scheduler.EnqueueAsync(testJob);
        await scheduler.StartAsync(CancellationToken.None);

        using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        JobStateChangedEventArgs failedArgs = await failedTcs.Task.WaitAsync(waitCts.Token);

        await scheduler.StopAsync(TimeSpan.FromSeconds(2), CancellationToken.None);

        // Assert: Running → Failed transition was raised
        failedArgs.Should().NotBeNull("job must transition to Failed when PartialDumpException is thrown");
        failedArgs.PreviousState.Should().Be(JobState.Running,
            "job must transition Running→Failed when PartialDumpException is thrown");
        failedArgs.JobId.Should().Be(42);
        failedArgs.ErrorMessage.Should().NotBeNull();
        failedArgs.ErrorMessage.Should().Contain("dump file",
            "error message should mention the preserved dump file(s)");
        failedArgs.ErrorMessage.Should().Contain("1",
            "error message should include the count of partial files");
    }
}
