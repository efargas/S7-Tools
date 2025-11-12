using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Controls;
using S7Tools.ViewModels.Jobs;
using Xunit;

namespace S7Tools.Tests.ViewModels.Jobs;

/// <summary>
/// Unit tests for JobWizardViewModel, focusing on enhanced profile detail computed properties.
/// </summary>
public sealed class JobWizardViewModelTests : IDisposable
{
    private readonly Mock<ILogger<JobWizardViewModel>> _mockLogger;
    private readonly Mock<ISerialPortProfileService> _mockSerialService;
    private readonly Mock<ISocatProfileService> _mockSocatService;
    private readonly Mock<IPowerSupplyProfileService> _mockPowerService;
    private readonly Mock<IMemoryRegionProfileService> _mockMemoryRegionService;
    private readonly Mock<IJobManager> _mockJobManager;
    private readonly Mock<IUIThreadService> _mockUIThreadService;
    private readonly Mock<IFileDialogService> _mockFileDialogService;
    private readonly Mock<IViewModelFactory> _mockViewModelFactory;
    private readonly Mock<ISerialPortService> _mockSerialPortService;
    private readonly Mock<ILogger<SerialPortDiscoveryViewModel>> _mockScannerLogger;

    public JobWizardViewModelTests()
    {
        _mockLogger = new Mock<ILogger<JobWizardViewModel>>();
        _mockSerialService = new Mock<ISerialPortProfileService>();
        _mockSocatService = new Mock<ISocatProfileService>();
        _mockPowerService = new Mock<IPowerSupplyProfileService>();
        _mockMemoryRegionService = new Mock<IMemoryRegionProfileService>();
        _mockJobManager = new Mock<IJobManager>();
        _mockUIThreadService = new Mock<IUIThreadService>();
        _mockFileDialogService = new Mock<IFileDialogService>();
        _mockViewModelFactory = new Mock<IViewModelFactory>();
        _mockSerialPortService = new Mock<ISerialPortService>();
        _mockScannerLogger = new Mock<ILogger<SerialPortDiscoveryViewModel>>();
        var _mockUIRefreshService = new Mock<IUIRefreshService>();

        // Create a real instance for SerialPortDiscoveryViewModel (can't mock concrete class)
        var realSerialScanner = new SerialPortDiscoveryViewModel(
            _mockSerialPortService.Object,
            _mockUIThreadService.Object,
            _mockUIRefreshService.Object,
            _mockScannerLogger.Object);

        // Setup view model factory to return the real scanner
        _mockViewModelFactory.Setup(x => x.Create<SerialPortDiscoveryViewModel>())
            .Returns(realSerialScanner);

        // Setup UI thread service to execute synchronously for tests
        _mockUIThreadService.Setup(x => x.InvokeOnUIThreadAsync(It.IsAny<Action>()))
            .Returns((Action action) =>
            {
                action();
                return Task.CompletedTask;
            });

        // Setup empty collections for services
        _mockSerialService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SerialPortProfile>());
        _mockSocatService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SocatProfile>());
        _mockPowerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PowerSupplyProfile>());
    }

    #region Serial Profile Detail Properties Tests

    [Fact]
    public void SerialBaudRate_WhenSelectedSerialIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSerial = null;

        // Act
        string result = viewModel.SerialBaudRate;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SerialBaudRate_WhenSelectedSerialHasBaudRate_ReturnsBaudRateString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SerialPortProfile profile = CreateSerialProfile(baudRate: 9600);
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialBaudRate;

        // Assert
        result.Should().Be("9600");
    }

    [Fact]
    public void SerialCharacterSize_WhenSelectedSerialIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSerial = null;

        // Act
        string result = viewModel.SerialCharacterSize;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SerialCharacterSize_WhenSelectedSerialHasCharacterSize_ReturnsCharacterSizeString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SerialPortProfile profile = CreateSerialProfile(characterSize: 7);
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialCharacterSize;

        // Assert
        result.Should().Be("7");
    }

    [Fact]
    public void SerialParity_WhenSelectedSerialIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSerial = null;

        // Act
        string result = viewModel.SerialParity;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SerialParity_WhenSelectedSerialHasParity_ReturnsParityString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SerialPortProfile profile = CreateSerialProfile(parity: ParityMode.Odd);
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialParity;

        // Assert
        result.Should().Be("Odd");
    }

    [Fact]
    public void SerialStopBits_WhenSelectedSerialIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSerial = null;

        // Act
        string result = viewModel.SerialStopBits;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SerialStopBits_WhenSelectedSerialHasStopBits_ReturnsStopBitsString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SerialPortProfile profile = CreateSerialProfile(stopBits: StopBits.Two);
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialStopBits;

        // Assert
        result.Should().Be("Two");
    }

    [Fact]
    public void SerialEnableReceiver_WhenSelectedSerialIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSerial = null;

        // Act
        string result = viewModel.SerialEnableReceiver;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SerialEnableReceiver_WhenSelectedSerialHasTrueValue_ReturnsYes()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SerialPortProfile profile = CreateSerialProfile(enableReceiver: true);
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialEnableReceiver;

        // Assert
        result.Should().Be("Yes");
    }

    [Fact]
    public void SerialEnableReceiver_WhenSelectedSerialHasFalseValue_ReturnsNo()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SerialPortProfile profile = CreateSerialProfile(enableReceiver: false);
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialEnableReceiver;

        // Assert
        result.Should().Be("No");
    }

    [Fact]
    public void SerialVersion_WhenSelectedSerialIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSerial = null;

        // Act
        string result = viewModel.SerialVersion;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SerialVersion_WhenSelectedSerialHasVersion_ReturnsVersionString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SerialPortProfile profile = CreateSerialProfile(version: "2.1");
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialVersion;

        // Assert
        result.Should().Be("2.1");
    }

    [Fact]
    public void SerialCreatedAt_WhenSelectedSerialIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSerial = null;

        // Act
        string result = viewModel.SerialCreatedAt;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SerialCreatedAt_WhenSelectedSerialHasCreatedAt_ReturnsFormattedDate()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        var createdAt = new DateTime(2025, 10, 21, 14, 30, 0);
        SerialPortProfile profile = CreateSerialProfile(createdAt: createdAt);
        viewModel.SelectedSerial = profile;

        // Act
        string result = viewModel.SerialCreatedAt;

        // Assert
        result.Should().Be("2025-10-21 14:30");
    }

    #endregion

    #region Socat Profile Detail Properties Tests

    [Fact]
    public void SocatTcpPort_WhenSelectedSocatIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSocat = null;

        // Act
        string result = viewModel.SocatTcpPort;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SocatTcpPort_WhenSelectedSocatHasTcpPort_ReturnsTcpPortString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SocatProfile profile = CreateSocatProfile(tcpPort: 8080);
        viewModel.SelectedSocat = profile;

        // Act
        string result = viewModel.SocatTcpPort;

        // Assert
        result.Should().Be("8080");
    }

    [Fact]
    public void SocatTcpHost_WhenSelectedSocatIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSocat = null;

        // Act
        string result = viewModel.SocatTcpHost;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SocatTcpHost_WhenSelectedSocatHasTcpHost_ReturnsTcpHostString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SocatProfile profile = CreateSocatProfile(tcpHost: "192.168.1.100");
        viewModel.SelectedSocat = profile;

        // Act
        string result = viewModel.SocatTcpHost;

        // Assert
        result.Should().Be("192.168.1.100");
    }

    [Fact]
    public void SocatVerbose_WhenSelectedSocatIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedSocat = null;

        // Act
        string result = viewModel.SocatVerbose;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void SocatVerbose_WhenSelectedSocatHasTrueValue_ReturnsYes()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        SocatProfile profile = CreateSocatProfile(verbose: true);
        viewModel.SelectedSocat = profile;

        // Act
        string result = viewModel.SocatVerbose;

        // Assert
        result.Should().Be("Yes");
    }

    #endregion

    #region Power Supply Profile Detail Properties Tests

    [Fact]
    public void PowerHost_WhenSelectedPowerIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedPower = null;

        // Act
        string result = viewModel.PowerHost;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void PowerHost_WhenSelectedPowerHasModbusTcpHost_ReturnsHostString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        PowerSupplyProfile profile = CreatePowerProfile(host: "192.168.1.200");
        viewModel.SelectedPower = profile;

        // Act
        string result = viewModel.PowerHost;

        // Assert
        result.Should().Be("192.168.1.200");
    }

    [Fact]
    public void PowerPort_WhenSelectedPowerIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedPower = null;

        // Act
        string result = viewModel.PowerPort;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void PowerPort_WhenSelectedPowerHasModbusTcpPort_ReturnsPortString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        PowerSupplyProfile profile = CreatePowerProfile(port: 1502);
        viewModel.SelectedPower = profile;

        // Act
        string result = viewModel.PowerPort;

        // Assert
        result.Should().Be("1502");
    }

    [Fact]
    public void PowerDeviceId_WhenSelectedPowerIsNull_ReturnsNA()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        viewModel.SelectedPower = null;

        // Act
        string result = viewModel.PowerDeviceId;

        // Assert
        result.Should().Be("N/A");
    }

    [Fact]
    public void PowerDeviceId_WhenSelectedPowerHasModbusTcpDeviceId_ReturnsDeviceIdString()
    {
        // Arrange
        JobWizardViewModel viewModel = CreateJobWizardViewModel();
        PowerSupplyProfile profile = CreatePowerProfile(deviceId: 5);
        viewModel.SelectedPower = profile;

        // Act
        string result = viewModel.PowerDeviceId;

        // Assert
        result.Should().Be("5");
    }

    #endregion

    #region Helper Methods

    private JobWizardViewModel CreateJobWizardViewModel()
    {
        return new JobWizardViewModel(
            _mockLogger.Object,
            _mockSerialService.Object,
            _mockSocatService.Object,
            _mockPowerService.Object,
            _mockMemoryRegionService.Object,
            _mockJobManager.Object,
            _mockUIThreadService.Object,
            _mockFileDialogService.Object,
            _mockViewModelFactory.Object);
    }

    private static SerialPortProfile CreateSerialProfile(
        int baudRate = 38400,
        int characterSize = 8,
        ParityMode parity = ParityMode.Even,
        StopBits stopBits = StopBits.One,
        bool enableReceiver = true,
        string version = "1.0",
        DateTime? createdAt = null)
    {
        var config = new SerialPortConfiguration
        {
            BaudRate = baudRate,
            CharacterSize = characterSize,
            Parity = parity,
            StopBits = stopBits,
            EnableReceiver = enableReceiver,
            Version = version,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        return new SerialPortProfile
        {
            Id = 1,
            Name = "Test Serial Profile",
            Configuration = config,
            Version = version,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    private static SocatProfile CreateSocatProfile(
        int tcpPort = 1238,
        string tcpHost = "localhost",
        bool verbose = true)
    {
        var config = new SocatConfiguration
        {
            TcpPort = tcpPort,
            TcpHost = tcpHost,
            Verbose = verbose
        };

        return new SocatProfile
        {
            Id = 1,
            Name = "Test Socat Profile",
            Configuration = config,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PowerSupplyProfile CreatePowerProfile(
        string host = "192.168.1.100",
        int port = 502,
        byte deviceId = 1)
    {
        var config = new ModbusTcpConfiguration
        {
            Host = host,
            Port = port,
            DeviceId = deviceId
        };

        return new PowerSupplyProfile
        {
            Id = 1,
            Name = "Test Power Profile",
            Configuration = config,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion

    public void Dispose()
    {
        // Clean up any resources if needed
        GC.SuppressFinalize(this);
    }
}
