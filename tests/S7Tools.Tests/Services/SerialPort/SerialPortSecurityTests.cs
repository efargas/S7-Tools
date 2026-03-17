using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Services.Shell;
using S7Tools.Services.SerialPort;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace S7Tools.Tests.Services.SerialPort;

public class SerialPortSecurityTests
{
    private readonly Mock<ILogger<SerialPortConfigurationService>> _mockConfigLogger;
    private readonly Mock<ILogger<SerialPortDiscoveryService>> _mockDiscoveryLogger;
    private readonly Mock<ITimeProvider> _mockTimeProvider;
    private readonly Mock<IShellCommandExecutor> _mockShellExecutor;

    public SerialPortSecurityTests()
    {
        _mockConfigLogger = new Mock<ILogger<SerialPortConfigurationService>>();
        _mockDiscoveryLogger = new Mock<ILogger<SerialPortDiscoveryService>>();
        _mockTimeProvider = new Mock<ITimeProvider>();
        _mockShellExecutor = new Mock<IShellCommandExecutor>();
    }

    [Theory]
    [InlineData("/dev/ttyUSB0", "'/dev/ttyUSB0'")]
    [InlineData("/dev/ttyUSB0; rm -rf /", "'/dev/ttyUSB0; rm -rf /'")]
    [InlineData("valid_path'with_quote", "'valid_path'\"'\"'with_quote'")]
    public void GenerateSttyCommand_ShouldQuotePortPath(string inputPath, string expectedQuotedPath)
    {
        // Arrange
        var service = new SerialPortConfigurationService(
            _mockConfigLogger.Object,
            _mockTimeProvider.Object,
            _mockShellExecutor.Object);

        var config = new SerialPortConfiguration();

        // Act
        var command = service.GenerateSttyCommand(inputPath, config);

        // Assert
        command.Should().Contain($"-F {expectedQuotedPath}");
    }

    [Fact]
    public async Task ReadPortConfigurationAsync_ShouldQuotePortPath_WhenExecutingCommand()
    {
        // Arrange
        var service = new SerialPortConfigurationService(
            _mockConfigLogger.Object,
            _mockTimeProvider.Object,
            _mockShellExecutor.Object);

        string maliciousPath = "/dev/tty;reboot";
        string expectedCommandPart = "stty -F '/dev/tty;reboot' -a";

        _mockShellExecutor
            .Setup(x => x.ExecuteCommandWithTimeoutAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandResult(true, 0, "speed 9600 baud; line = 0;\n", ""));

        // Act
        await service.ReadPortConfigurationAsync(maliciousPath);

        // Assert
        _mockShellExecutor.Verify(x => x.ExecuteCommandWithTimeoutAsync(
            It.Is<string>(cmd => cmd.Contains(expectedCommandPart)),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsPortAccessibleAsync_ShouldQuotePortPath_WhenExecutingCommand()
    {
        // Arrange
        var service = new SerialPortDiscoveryService(
            _mockDiscoveryLogger.Object,
            _mockTimeProvider.Object,
            _mockShellExecutor.Object);

        string tempFile = System.IO.Path.GetTempFileName();
        try
        {
            var serviceToCheck = new SerialPortDiscoveryService(
               _mockDiscoveryLogger.Object,
               _mockTimeProvider.Object,
               _mockShellExecutor.Object);

            string unsafePath = tempFile + " test";
            try
            { System.IO.File.WriteAllText(unsafePath, "dummy"); }
            catch { unsafePath = tempFile; }

            // Here we verify ExecuteDirectAsync instead of ExecuteCommandWithTimeoutAsync
            _mockShellExecutor
               .Setup(x => x.ExecuteDirectAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ShellCommandResult(true, 0, "", ""));

            await serviceToCheck.IsPortAccessibleAsync(unsafePath, 1000, CancellationToken.None);

            // ExecuteDirectAsync is safe by design, we just need to verify it's passing the path as an argument.
            _mockShellExecutor.Verify(x => x.ExecuteDirectAsync(
               "stty",
               It.Is<IEnumerable<string>>(args => args.Contains(unsafePath)),
               It.IsAny<int>(),
               It.IsAny<CancellationToken>()), Times.Once);

            if (unsafePath != tempFile && System.IO.File.Exists(unsafePath))
                System.IO.File.Delete(unsafePath);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }
}
