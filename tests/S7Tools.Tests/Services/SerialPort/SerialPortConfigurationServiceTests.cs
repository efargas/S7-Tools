using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Models;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.Shell;
using S7Tools.Services;
using S7Tools.Services.SerialPort;
using Xunit;

namespace S7Tools.Tests.Services.SerialPort;

public class SerialPortConfigurationServiceTests
{
    private readonly Mock<ILogger<SerialPortConfigurationService>> _loggerMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly Mock<IShellCommandExecutor> _shellExecutorMock;
    private readonly SerialPortConfigurationService _service;

    public SerialPortConfigurationServiceTests()
    {
        _loggerMock = new Mock<ILogger<SerialPortConfigurationService>>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _shellExecutorMock = new Mock<IShellCommandExecutor>();

        _timeProviderMock.Setup(t => t.GetLocalNow()).Returns(DateTime.Now);

        _service = new SerialPortConfigurationService(
            _loggerMock.Object,
            _timeProviderMock.Object,
            _shellExecutorMock.Object);
    }

    [Fact]
    public void GenerateSttyCommand_SimpleConfig_ReturnsCorrectString()
    {
        // Arrange
        var config = new SerialPortConfiguration
        {
            BaudRate = 115200,
            CharacterSize = 8,
            RawMode = true
        };
        string portPath = "/dev/ttyUSB0";

        // Act
        string command = _service.GenerateSttyCommand(portPath, config);

        // Assert
        Assert.Contains("stty -F '/dev/ttyUSB0'", command);
        Assert.Contains("115200", command);
        Assert.Contains("cs8", command);
        Assert.Contains("raw", command);
    }

    [Fact]
    public async Task ReadPortConfigurationAsync_ValidOutput_ParsesCorrectly()
    {
        // Arrange
        string portPath = "/dev/ttyUSB0";
        string sttyOutput = "speed 115200 baud; line = 0;\ncs8 -parenb -parodd ...";

        _shellExecutorMock.Setup(e => e.ExecuteCommandWithTimeoutAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandResult(true, 0, sttyOutput, ""));

        // Act
        var result = await _service.ReadPortConfigurationAsync(portPath);

        // Assert
        result.Should().NotBeNull();
        result.BaudRate.Should().Be(115200);
        result.CharacterSize.Should().Be(8);
        _shellExecutorMock.Verify(e => e.ExecuteCommandWithTimeoutAsync(It.Is<string>(c => c.Contains("-a")), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_Success_ReturnsTrue()
    {
        // Arrange
        string portPath = "/dev/ttyUSB0";
        var config = new SerialPortConfiguration();

        _shellExecutorMock.Setup(e => e.ExecuteCommandWithTimeoutAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandResult(true, 0, "", ""));

        // Act
        bool success = await _service.ApplyConfigurationAsync(portPath, config);

        // Assert
        success.Should().BeTrue();
        _shellExecutorMock.Verify(e => e.ExecuteCommandWithTimeoutAsync(It.Is<string>(c => c.Contains("stty")), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_Failure_ReturnsFalse()
    {
        // Arrange
        string portPath = "/dev/ttyUSB0";
        var config = new SerialPortConfiguration();

        _shellExecutorMock.Setup(e => e.ExecuteCommandWithTimeoutAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandResult(false, 1, "", "Error message"));

        // Act
        bool success = await _service.ApplyConfigurationAsync(portPath, config);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public void ValidateSttyCommand_ValidCommand_ReturnsValid()
    {
        // Act
        var result = _service.ValidateSttyCommand("stty -F /dev/ttyUSB0 115200");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSttyCommand_DangerousCommand_ReturnsInvalid()
    {
        // Act
        var result = _service.ValidateSttyCommand("stty -F /dev/ttyUSB0; rm -rf /");

        // Assert
        result.IsValid.Should().BeFalse();
        Assert.NotEmpty(result.Errors);
    }
}
