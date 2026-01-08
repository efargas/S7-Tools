using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Bootloader;

namespace S7Tools.Tests.Services.Bootloader;

/// <summary>
/// Integration tests for the EnhancedBootloaderService.
/// Tests the 7-stage workflow execution and resource failure handling.
/// </summary>
public class EnhancedBootloaderServiceTests
{
    #region Test Helpers

    private static JobProfileSet CreateTestProfiles(int socatPort = 8080, string serialDevice = "/dev/ttyUSB0")
    {
        // Create full configuration objects for testing
        var serialConfig = new SerialPortConfiguration
        {
            BaudRate = 115200,
            Parity = ParityMode.None,
            CharacterSize = 8,
            StopBits = StopBits.One,
            RawMode = true,
            DisableEcho = true,
            IgnoreBreak = true,
            DisableCanonicalMode = true,
            DisableSignalGeneration = true,
            DisableHardwareFlowControl = true,
            DisableXonXoffFlowControl = true
        };

        var socatConfig = new SocatConfiguration
        {
            TcpPort = socatPort,
            Verbose = true,
            HexDump = false,
            BlockSize = 4,
            DebugLevel = 2,
            EnableFork = true,
            EnableReuseAddr = true,
            SerialRawMode = true,
            SerialDisableEcho = true
        };

        var powerConfig = new ModbusTcpConfiguration
        {
            Host = "192.168.1.100",
            Port = 502,
            DeviceId = 1,
            OnOffCoil = 0,
            AddressingMode = ModbusAddressingMode.Base0
        };

        return new JobProfileSet(
            Serial: new SerialProfileRef(serialDevice, 115200, "None", 8, "One", serialConfig),
            Socat: new SocatProfileRef(socatPort, Ephemeral: true, socatConfig),
            Power: new PowerProfileRef("192.168.1.100", 502, 0, DelaySeconds: 2, powerConfig),
            Memory: new MemoryRegionProfile("0x20000000", 0x1000),
            Payloads: new PayloadSetProfile { BasePath = "/tmp/payloads" },
            OutputPath: "/tmp/dumps"
        );
    }

    private static EnhancedBootloaderService CreateService(
        IPayloadProvider? payloads = null,
        ISocatService? socatService = null,
        IPowerSupplyService? power = null,
        ISerialPortService? serialPort = null,
        Func<JobProfileSet, IPlcClient>? clientFactory = null,
        IResourceCoordinator? resourceCoordinator = null)
    {
        return new EnhancedBootloaderService(
            NullLogger<EnhancedBootloaderService>.Instance,
            payloads ?? Substitute.For<IPayloadProvider>(),
            socatService ?? Substitute.For<ISocatService>(),
            power ?? Substitute.For<IPowerSupplyService>(),
            serialPort ?? Substitute.For<ISerialPortService>(),
            clientFactory ?? (_ => Substitute.For<IPlcClient>()),
            resourceCoordinator ?? Substitute.For<IResourceCoordinator>()
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
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns(socatProcessInfo);

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<PowerSupplyConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(true);
        power.TurnOnAsync(Arg.Any<CancellationToken>())
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

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(
            payloads: payloads,
            socatService: socatService,
            power: power,
            serialPort: serialPort,
            clientFactory: ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        byte[] result = await service.DumpAsync(profiles, progress, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(256); // Expected dump size

        // Verify all stages executed
        await plcClient.Received(1).DumpMemoryAsync(
            Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>());

        // Verify 7-stage workflow execution order
        await socatService.Received(1).StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>());
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
        var socatProcessInfo = new SocatProcessInfo
        {
            ProcessId = 1234,
            TcpPort = 8080,
            TcpHost = "127.0.0.1",
            SerialDevice = "/dev/ttyUSB0",
            IsRunning = true
        };
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns(socatProcessInfo);

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);
        power.TurnOnAsync(Arg.Any<CancellationToken>()).Returns(true);
        power.PowerCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.DumpMemoryAsync(
                Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new byte[1024]);

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(
            payloads: payloads,
            socatService: socatService,
            power: power,
            serialPort: serialPort,
            clientFactory: ClientFactory
        );

        var progressReports = new List<(string stage, double percent)>();
        var progress = new Progress<(string stage, double percent)>(p => progressReports.Add(p));
        JobProfileSet profiles = CreateTestProfiles();

        // Act
        await service.DumpAsync(profiles, progress, null, CancellationToken.None);

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
        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(serialPort: serialPort);

        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(null!, progress, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DumpAsync_WithNullProgress_ShouldThrowArgumentNullException()
    {
        // Arrange
        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(serialPort: serialPort);

        JobProfileSet profiles = CreateTestProfiles();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, null!, null, CancellationToken.None);

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
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns(new SocatProcessInfo { ProcessId = 1234, TcpPort = 1238 });

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(false); // Connection fails

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(
            payloads: payloads,
            socatService: socatService,
            power: power,
            serialPort: serialPort,
            clientFactory: ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, null, CancellationToken.None);

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
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns<Task<SocatProcessInfo>>(_ => Task.FromException<SocatProcessInfo>(
                new InvalidOperationException("Socat failed to start")));

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        IPlcClient plcClient = Substitute.For<IPlcClient>();
        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(
            payloads: payloads,
            socatService: socatService,
            power: power,
            serialPort: serialPort,
            clientFactory: ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, null, CancellationToken.None);

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
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns(new SocatProcessInfo { ProcessId = 1234, TcpPort = 1238 });

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);
        power.TurnOnAsync(Arg.Any<CancellationToken>()).Returns(true);
        power.PowerCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.HandshakeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Handshake timeout")));

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(
            payloads: payloads,
            socatService: socatService,
            power: power,
            serialPort: serialPort,
            clientFactory: ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, null, CancellationToken.None);

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
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                CancellationToken token = callInfo.Arg<CancellationToken>();
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

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(
            payloads: payloads,
            socatService: socatService,
            power: power,
            serialPort: serialPort,
            clientFactory: ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, null, cts.Token);

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
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns(new SocatProcessInfo { ProcessId = 1234, TcpPort = 1238 });

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);
        power.TurnOnAsync(Arg.Any<CancellationToken>()).Returns(true);
        power.PowerCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.HandshakeAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        plcClient.GetBootloaderVersionAsync(Arg.Any<CancellationToken>()).Returns("1.0.0");
        plcClient.InstallStagerAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Stager installation failed")));

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = CreateService(
            payloads: payloads,
            socatService: socatService,
            power: power,
            serialPort: serialPort,
            clientFactory: ClientFactory
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, null, CancellationToken.None);

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
        socatService.StartSocatAsync(Arg.Any<SocatConfiguration>(), Arg.Any<string>(), Arg.Any<Microsoft.Extensions.Logging.ILogger?>(), Arg.Any<CancellationToken>())
            .Returns(new SocatProcessInfo { ProcessId = 1234, TcpPort = 1238 });

        IPowerSupplyService power = Substitute.For<IPowerSupplyService>();
        power.ConnectAsync(Arg.Any<ModbusTcpConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);
        power.TurnOnAsync(Arg.Any<CancellationToken>()).Returns(true);
        power.PowerCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        IPlcClient plcClient = Substitute.For<IPlcClient>();
        plcClient.HandshakeAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        plcClient.GetBootloaderVersionAsync(Arg.Any<CancellationToken>()).Returns("1.0.0");
        plcClient.InstallStagerAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        plcClient.DumpMemoryAsync(Arg.Any<uint>(), Arg.Any<uint>(), Arg.Any<byte[]>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns<Task<byte[]>>(_ => Task.FromException<byte[]>(
                new InvalidOperationException("Memory dump read timeout")));

        IPlcClient ClientFactory(JobProfileSet profiles) => plcClient;

        ISerialPortService serialPort = Substitute.For<ISerialPortService>();
        serialPort.ApplyConfigurationAsync(Arg.Any<string>(), Arg.Any<SerialPortConfiguration>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = new EnhancedBootloaderService(
            NullLogger<EnhancedBootloaderService>.Instance,
            payloads,
            socatService,
            power,
            serialPort,
            ClientFactory,
            Substitute.For<IResourceCoordinator>()
        );

        JobProfileSet profiles = CreateTestProfiles();
        var progress = new Progress<(string stage, double percent)>();

        // Act
        Func<Task> act = async () => await service.DumpAsync(profiles, progress, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Memory dump*");
    }

    #endregion
}
