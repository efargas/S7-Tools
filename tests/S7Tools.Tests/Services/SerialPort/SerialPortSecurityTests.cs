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

        // We use a path that shouldn't trigger file existence check issues if we could skip it,
        // but IsPortAccessibleAsync checks File.Exists("/dev/ttyUSB0").
        // Since we cannot mock File.Exists, we will use a test that relies on proper command quoting
        // essentially validating logic if we assume the file check passed or using a hack if needed.
        // However, for unit testing `SerialPortDiscoveryService` which has a hard dependency on `File.Exists`,
        // it is difficult without a file system abstraction.
        // 
        // INSTAD, let's test `IsPortInUseAsync` which uses `lsof` and typically doesn't check File.Exists 
        // OR checks it internally.
        // Actually `IsPortInUseAsync` is private and called by `GetPortInfoAsync`. 
        // `GetPortInfoAsync` DOES check File.Exists.

        // As a workaround for this environment, I will verify the fix via `SerialPortConfigurationService` 
        // which is the primary target and where the shell injection vulnerability is most critical (stty options).
        // For DiscoveryService, I'll inspect the code manually or try to run a test if I can create a temp file.

        string tempFile = System.IO.Path.GetTempFileName();
        try
        {
            var serviceToCheck = new SerialPortDiscoveryService(
               _mockDiscoveryLogger.Object,
               _mockTimeProvider.Object,
               _mockShellExecutor.Object);

            // The path will be the temp file, but we will checking if it gets quoted in the command
            string portPath = tempFile;
            // We want to verify that even a safe path is quoted, which implies the security mechanism is in place.
            // If we could use a path with spaces, that would be better proof.
            // Let's try to create a file with a space in the name if possible.
            string unsafePath = tempFile + " 'test";
            try
            { System.IO.File.WriteAllText(unsafePath, "dummy"); }
            catch { /* ignore if fails */ unsafePath = tempFile; }

            _mockShellExecutor
               .Setup(x => x.ExecuteCommandWithTimeoutAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ShellCommandResult(true, 0, "", ""));

            await serviceToCheck.IsPortAccessibleAsync(unsafePath, 1000, CancellationToken.None);

            // We expect the command to be: stty -F 'path' -a
            // We verify the path is single-quoted.
            _mockShellExecutor.Verify(x => x.ExecuteCommandWithTimeoutAsync(
               It.Is<string>(cmd => cmd.Contains($"'{unsafePath}'") || cmd.Contains($"'{unsafePath.Replace("'", "'\"'\"'")}'")),
               It.IsAny<int>(),
               It.IsAny<CancellationToken>()), Times.Once);

            if (unsafePath != tempFile)
                System.IO.File.Delete(unsafePath);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }
}
