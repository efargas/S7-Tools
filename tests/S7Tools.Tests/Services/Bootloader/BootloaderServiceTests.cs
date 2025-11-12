using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Bootloader;

namespace S7Tools.Tests.Services.Bootloader;

/// <summary>
/// Integration tests for the BootloaderService.
/// Tests the 7-stage workflow execution and resource failure handling.
/// </summary>
public class BootloaderServiceTests
{
    #region Test Helpers

    private static JobProfileSet CreateTestProfiles(int socatPort = 8080, string serialDevice = "/dev/ttyUSB0")
    {
        return new JobProfileSet(
            Serial: new SerialProfileRef(serialDevice, 115200, "None", 8, "One"),
            Socat: new SocatProfileRef(socatPort, Ephemeral: true),
            Power: new PowerProfileRef("192.168.1.100", 502, 0, DelaySeconds: 2),
            Memory: new MemoryRegionProfile(0x20000000, 0x1000),
            Payloads: new PayloadSetProfile("/tmp/payloads"),
            OutputPath: "/tmp/dumps"
        );
    }

    #endregion

    #region T044: 7-Stage Workflow Execution

    [Fact]
    public async Task DumpAsync_WithValidConfiguration_ShouldExecute7StageWorkflow()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01, 0x02, 0x03 });
        payloads.GetMemoryDumperAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x04, 0x05, 0x06 });

        ISocatService socatService = Substitute.For<ISocatService>();
        var socatProcessInfo = new SocatProcessInfo
        {
            ProcessId = 1234,
            TcpPort = 8080,
            TcpHost = "127.0.0.1",
            SerialDevice = "/dev/ttyUSB0",
            IsRunning = true
        };
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(socatProcessInfo);

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<PowerSupplyConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(true);
        power.PowerCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.HandshakeAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        plcClient.InstallStagerAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        plcClient.DumpMemoryAsync(
                Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new byte[256]);

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        byte[] result = await service.DumpAsync(profiles, progress, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(256); // Expected dump size

        // Verify all stages executed
        await plcClient.Received(1).DumpMemoryAsync(
            Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>());

        // Verify 7-stage workflow execution order
        await socatService.Received(1).StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await power.Received(1).ConnectAsync(Arg.Any<PowerSupplyConfiguration>(), Arg.Any<CancellationToken>());
        await power.Received(1).PowerCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await plcClient.Received(1).HandshakeAsync(Arg.Any<CancellationToken>());
        await plcClient.Received(1).InstallStagerAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await plcClient.Received(1).DumpMemoryAsync(
            Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DumpAsync_ShouldReportProgressForEachStage()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });
        payloads.GetMemoryDumperAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x02 });

        ISocatService socatService = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.DumpMemoryAsync(
                Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new byte[1024]);

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        var progressReports = new List<(string stage, double percent)>();
        var progress = new Progress<(string stage, double percent)>(p => progressReports.Add(p));
        JobProfileSet profiles = CreateTestProfiles();

        // Act
        await service.DumpAsync(profiles, progress, CancellationToken.None);

        // Assert
        progressReports.Should().NotBeEmpty("Progress should be reported");
        progressReports.Should().Contain(p => p.stage == "socat_setup", "Should report socat stage");
        progressReports.Should().Contain(p => p.stage == "power_cycle", "Should report power cycle stage");

        // Verify progress percentages increase
        var percentages = progressReports.Select(p => p.percent).ToList();
        percentages.Should().BeInAscendingOrder("Progress percentages should increase");
    }

    [Fact]
    public async Task DumpAsync_WithNullProfiles_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            Substitute.For<IPayloadProvider>(),
            Substitute.For<ISocatService>(),
            Substitute.For<IPowerSupplyService>(),
            _ => Substitute.For<IPlcClient>()
        );

        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(null!, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DumpAsync_WithNullProgress_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            Substitute.For<IPayloadProvider>(),
            Substitute.For<ISocatService>(),
            Substitute.For<IPowerSupplyService>(),
            _ => Substitute.For<IPlcClient>()
        );

        JobProfileSet profiles = CreateTestProfiles();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region T045: Resource Acquisition Failure Handling

    [Fact]
    public async Task DumpAsync_WhenPowerSupplyConnectionFails_ShouldThrowException()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        ISocatService socatService = Substitute.For<ISocatService>();

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(false); // Connection fails

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*power supply*");
    }

    [Fact]
    public async Task DumpAsync_WhenSocatStartFails_ShouldThrowException()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();

        ISocatService socatService = Substitute.For<ISocatService>();
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<SocatProcessInfo>>(_ => Task.FromException<SocatProcessInfo>(
                new InvalidOperationException("Socat failed to start")));

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        IPlcClient plcClient = Substitute.For<IPlcClient>();
        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Socat*");
    }

    [Fact]
    public async Task DumpAsync_WhenPlcHandshakeFails_ShouldThrowException()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });

        ISocatService socatService = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.HandshakeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Handshake timeout")));

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Handshake*");
    }

    [Fact]
    public async Task DumpAsync_WhenCancellationRequested_ShouldStopGracefully()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        ISocatService socatService = Substitute.For<ISocatService>();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Configure socatService to throw when cancellation is detected
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                token.ThrowIfCancellationRequested();
                return new SocatProcessInfo { ProcessId = 1234, TcpPort = 8080 };
            });

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<PowerSupplyConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(true);
        power.PowerCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DumpAsync_WhenStagerInstallationFails_ShouldThrowException()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });

        ISocatService socatService = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.InstallStagerAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Stager installation failed")));

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Stager*");
    }

    [Fact]
    public async Task DumpAsync_WhenMemoryDumpFails_ShouldThrowException()
    {
        // Arrange
        IPayloadProvider payloads = Substitute.For<IPayloadProvider>();
        payloads.GetStagerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x01 });
        payloads.GetMemoryDumperAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x02 });

        ISocatService socatService = Substitute.For<ISocatService>();
        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.DumpMemoryAsync(Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns<Task<byte[]>>(_ => Task.FromException<byte[]>(
                new InvalidOperationException("Memory dump read timeout")));

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        var service = new BootloaderService(
            NullLogger<BootloaderService>.Instance,
            payloads,
            socatService,
            power,
            ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Memory dump*");
    }

    #endregion
}
