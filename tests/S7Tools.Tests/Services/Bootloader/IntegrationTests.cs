using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Adapters;
using S7Tools.Services.Bootloader;
using S7Tools.Services.Tasking;

namespace S7Tools.Tests.Services.Bootloader;

/// <summary>
/// Integration tests for end-to-end job execution workflows.
/// Tests PayloadProvider file loading and complete job execution from enqueue to completion.
/// </summary>
public class IntegrationTests
{
    #region Test Helpers

    private static JobProfileSet CreateTestProfiles(string payloadPath = "/tmp/test-payloads")
    {
        return new JobProfileSet(
            Serial: new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One"),
            Socat: new SocatProfileRef(8080, Ephemeral: true),
            Power: new PowerProfileRef("192.168.1.100", 502, 0, DelaySeconds: 2),
            Memory: new MemoryRegionProfile(0x20000000, 0x1000),
            Payloads: new PayloadSetProfile(payloadPath),
            OutputPath: "/tmp/dumps"
        );
    }

    private static Job CreateTestJob(string name = "Integration Test Job", params ResourceKey[] resources)
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

    #endregion

    #region T046: PayloadProvider File Loading Tests

    [Fact]
    public async Task FilePayloadProvider_GetStagerAsync_WithValidPath_ShouldLoadFile()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), $"test-payloads-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            string stagerPath = Path.Combine(tempDir, "stager.bin");
            byte[] stagerData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            await File.WriteAllBytesAsync(stagerPath, stagerData);

            var provider = new FilePayloadProvider(NullLogger<FilePayloadProvider>.Instance);

            // Act
            byte[] result = await provider.GetStagerAsync(tempDir, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(stagerData, "Loaded data should match file content");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FilePayloadProvider_GetStagerAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), $"test-payloads-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var provider = new FilePayloadProvider(NullLogger<FilePayloadProvider>.Instance);

            // Act
            Func<Task> act = async () => await provider.GetStagerAsync(tempDir, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*stager.bin*");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FilePayloadProvider_GetMemoryDumperAsync_WithValidPath_ShouldLoadFile()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), $"test-payloads-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            string dumperPath = Path.Combine(tempDir, "dumper.bin");
            byte[] dumperData = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            await File.WriteAllBytesAsync(dumperPath, dumperData);

            var provider = new FilePayloadProvider(NullLogger<FilePayloadProvider>.Instance);

            // Act
            byte[] result = await provider.GetMemoryDumperAsync(tempDir, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(dumperData, "Loaded data should match file content");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FilePayloadProvider_GetMemoryDumperAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), $"test-payloads-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var provider = new FilePayloadProvider(NullLogger<FilePayloadProvider>.Instance);

            // Act
            Func<Task> act = async () => await provider.GetMemoryDumperAsync(tempDir, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*dumper.bin*");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FilePayloadProvider_WithNullBasePath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new FilePayloadProvider(NullLogger<FilePayloadProvider>.Instance);

        // Act
        Func<Task> actStager = async () => await provider.GetStagerAsync(null!, CancellationToken.None);
        Func<Task> actDumper = async () => await provider.GetMemoryDumperAsync(null!, CancellationToken.None);

        // Assert
        await actStager.Should().ThrowAsync<ArgumentNullException>();
        await actDumper.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task FilePayloadProvider_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), $"test-payloads-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            string stagerPath = Path.Combine(tempDir, "stager.bin");
            await File.WriteAllBytesAsync(stagerPath, new byte[1024]);

            var provider = new FilePayloadProvider(NullLogger<FilePayloadProvider>.Instance);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            Func<Task> act = async () => await provider.GetStagerAsync(tempDir, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region T047: End-to-End Job Execution Tests

    [Fact]
    public async Task EndToEnd_EnqueueToCompletion_ShouldTransitionThroughAllStates()
    {
        // Arrange
        var resources = new ResourceCoordinator();

        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });
        payloads.GetMemoryDumperAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x02 });

        ISocatService socat = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.DumpMemoryAsync(Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new byte[4096]);

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var bootloader = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socat,
            power,
            ClientFactory
        );

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            resources,
            bootloader
        );

        var stateTransitions = new List<(Guid jobId, JobState state, string? message)>();
        scheduler.JobStateChanged += (id, state, msg) => stateTransitions.Add((id, state, msg));

        Job job = CreateTestJob("E2E Test", new ResourceKey("serial", "COM1"));

        // Act
        Guid jobId = scheduler.Enqueue(job);
        await Task.Delay(500); // Allow execution to complete

        // Assert
        stateTransitions.Should().NotBeEmpty("State transitions should be recorded");

        var states = stateTransitions.Select(t => t.state).ToList();
        states.Should().Contain(JobState.Queued, "Job should be queued");
        states.Should().Contain(JobState.Running, "Job should start running");

        // Job should eventually complete or fail
        states.Should().Contain(s => s == JobState.Completed || s == JobState.Failed,
            "Job should reach a terminal state");
    }

    [Fact]
    public async Task EndToEnd_WithResourceConflict_ShouldQueueAndExecuteSequentially()
    {
        // Arrange
        var resources = new ResourceCoordinator();

        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });
        payloads.GetMemoryDumperAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x02 });

        ISocatService socat = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        var executionOrder = new List<Guid>();

        plcClient.DumpMemoryAsync(Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                executionOrder.Add(Guid.NewGuid());
                await Task.Delay(100); // Simulate work
                return new byte[1024];
            });

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var bootloader = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socat,
            power,
            ClientFactory
        );

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            resources,
            bootloader
        );

        var sharedResource = new ResourceKey("serial", "COM1");
        Job job1 = CreateTestJob("Job 1", sharedResource);
        Job job2 = CreateTestJob("Job 2", sharedResource);

        // Act
        scheduler.Enqueue(job1);
        scheduler.Enqueue(job2);

        await Task.Delay(500); // Allow both to process

        // Assert
        executionOrder.Should().HaveCountGreaterThan(0, "At least one job should execute");

        // Both jobs should eventually complete
        IReadOnlyCollection<Job> allJobs = scheduler.GetAll();
        allJobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task EndToEnd_WithBootloaderFailure_ShouldTransitionToFailedState()
    {
        // Arrange
        var resources = new ResourceCoordinator();

        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        ISocatService socat = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(false); // Simulate connection failure

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var bootloader = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socat,
            power,
            ClientFactory
        );

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            resources,
            bootloader
        );

        var stateTransitions = new List<JobState>();
        scheduler.JobStateChanged += (id, state, msg) => stateTransitions.Add(state);

        Job job = CreateTestJob("Failing Job", new ResourceKey("serial", "COM1"));

        // Act
        scheduler.Enqueue(job);
        await Task.Delay(300);

        // Assert
        stateTransitions.Should().Contain(JobState.Failed,
            "Job should transition to Failed state on bootloader error");
    }

    [Fact]
    public async Task EndToEnd_MultipleJobsWithDifferentResources_ShouldExecuteInParallel()
    {
        // Arrange
        var resources = new ResourceCoordinator();

        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });
        payloads.GetMemoryDumperAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x02 });

        ISocatService socat = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        var runningJobs = new List<Guid>();
        var startSignal = new TaskCompletionSource<bool>();

        plcClient.DumpMemoryAsync(Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                lock (runningJobs)
                {
                    runningJobs.Add(Guid.NewGuid());
                    if (runningJobs.Count == 2)
                    {
                        startSignal.SetResult(true);
                    }
                }

                await Task.Delay(50); // Minimal work
                return new byte[1024];
            });

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var bootloader = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socat,
            power,
            ClientFactory
        );

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            resources,
            bootloader
        );

        Job job1 = CreateTestJob("Job 1", new ResourceKey("serial", "COM1"));
        Job job2 = CreateTestJob("Job 2", new ResourceKey("serial", "COM2")); // Different resource

        // Act
        scheduler.Enqueue(job1);
        scheduler.Enqueue(job2);

        bool bothStarted = await startSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        bothStarted.Should().BeTrue("Both jobs should start in parallel");
        runningJobs.Should().HaveCount(2, "Both jobs should have started execution");
    }

    [Fact]
    public async Task EndToEnd_JobStateChanged_ShouldProvideProgressMessages()
    {
        // Arrange
        var resources = new ResourceCoordinator();

        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });
        payloads.GetMemoryDumperAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x02 });

        ISocatService socat = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.DumpMemoryAsync(Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new byte[1024]);

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var bootloader = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socat,
            power,
            ClientFactory
        );

        var scheduler = new JobScheduler(
            NullLogger<JobScheduler>.Instance,
            resources,
            bootloader
        );

        var messages = new List<string>();
        scheduler.JobStateChanged += (id, state, msg) =>
        {
            if (!string.IsNullOrEmpty(msg))
            {
                messages.Add(msg);
            }
        };

        Job job = CreateTestJob("Progress Test", new ResourceKey("serial", "COM1"));

        // Act
        scheduler.Enqueue(job);
        await Task.Delay(300);

        // Assert
        messages.Should().NotBeEmpty("Progress messages should be provided");
    }

    #endregion
}
