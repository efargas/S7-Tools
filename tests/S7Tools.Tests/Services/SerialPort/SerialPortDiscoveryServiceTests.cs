using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public class SerialPortDiscoveryServiceTests : IDisposable
{
    private readonly Mock<ILogger<SerialPortDiscoveryService>> _loggerMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly Mock<IShellCommandExecutor> _shellExecutorMock;
    private readonly SerialPortDiscoveryService _service;
    private readonly string _tempPath;

    public SerialPortDiscoveryServiceTests()
    {
        _loggerMock = new Mock<ILogger<SerialPortDiscoveryService>>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _shellExecutorMock = new Mock<IShellCommandExecutor>();

        _timeProviderMock.Setup(t => t.GetLocalNow()).Returns(DateTime.Now);

        _service = new SerialPortDiscoveryService(
            _loggerMock.Object,
            _timeProviderMock.Object,
            _shellExecutorMock.Object);

        _tempPath = Path.Combine(Path.GetTempPath(), "S7Tools_Tests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);
    }

    [Fact]
    public void GetPortType_KnownPatterns_ReturnsCorrectType()
    {
        _service.GetPortType("/dev/ttyUSB0").Should().Be(SerialPortType.Usb);
        _service.GetPortType("/dev/ttyACM0").Should().Be(SerialPortType.Acm);
        _service.GetPortType("/dev/ttyS0").Should().Be(SerialPortType.Standard);
        _service.GetPortType("/dev/pts/1").Should().Be(SerialPortType.Virtual);
        _service.GetPortType("/dev/unknown").Should().Be(SerialPortType.Unknown);
    }

    [Fact]
    public async Task IsPortAccessibleAsync_FileDoesNotExist_ReturnsFalse()
    {
        // Act
        bool result = await _service.IsPortAccessibleAsync("/non/existent/port");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPortInfoAsync_ValidPort_ReturnsPopulatedInfo()
    {
        // Arrange
        string portPath = Path.Combine(_tempPath, "ttyUSB0");
        File.WriteAllText(portPath, ""); // Create dummy file

        // Back to testing ExecuteDirectAsync
        _shellExecutorMock.Setup(e => e.ExecuteDirectAsync("stty", It.Is<IEnumerable<string>>(args => args.Contains(portPath)), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandResult(true, 0, "stty output", ""));

        _shellExecutorMock.Setup(e => e.ExecuteDirectAsync("lsof", It.Is<IEnumerable<string>>(args => args.Contains(portPath)), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandResult(false, 1, "", "")); // Not in use

        // Act
        var info = await _service.GetPortInfoAsync(portPath, 1000);

        // Assert
        info.Should().NotBeNull();
        info.PortPath.Should().Be(portPath);
        info.PortType.Should().Be(SerialPortType.Usb);
        info.IsAccessible.Should().BeTrue();
        info.IsInUse.Should().BeFalse();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }
    }
}
