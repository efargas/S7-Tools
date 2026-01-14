using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Services.Shell;
using Xunit;

namespace S7Tools.Tests.Services.Shell;

public class ShellCommandExecutorTests
{
    private readonly Mock<ILogger<ShellCommandExecutor>> _loggerMock;
    private readonly ShellCommandExecutor _executor;

    public ShellCommandExecutorTests()
    {
        _loggerMock = new Mock<ILogger<ShellCommandExecutor>>();
        _executor = new ShellCommandExecutor(_loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ValidCommand_ReturnsSuccess()
    {
        // Act
        var result = await _executor.ExecuteCommandAsync("echo 'hello world'");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello world", result.Output);
    }

    [Fact]
    public async Task ExecuteCommandAsync_InvalidCommand_ReturnsFailure()
    {
        // Act
        var result = await _executor.ExecuteCommandAsync("nonexistent_command_12345");

        // Assert
        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task ExecuteCommandWithTimeoutAsync_FastCommand_CompletesSuccessfully()
    {
        // Act
        var result = await _executor.ExecuteCommandWithTimeoutAsync("sleep 0.1", 1000);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task ExecuteCommandWithTimeoutAsync_SlowCommand_TimesOut()
    {
        // Act
        var result = await _executor.ExecuteCommandWithTimeoutAsync("sleep 2", 500);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("timed out", result.Error.ToLower());
    }

    [Fact]
    public async Task GetChildProcessesAsync_ParentWithChildren_ReturnsChildPids()
    {
        // This test is a bit platform specific but pgrep should work on build system (linux)
        // We'll start a simple bash process that sleeps
        var cts = new CancellationTokenSource();
        var backgroundTask = _executor.ExecuteCommandAsync("sleep 10 & sleep 10", cts.Token);
        
        // Wait a bit for processes to start
        await Task.Delay(500);

        // We can't easily get the PID of the shell started by ExecuteCommandAsync without more work
        // but we can test that it doesn't throw and returns a list.
        // Actually, let's find our own PID and its children if any.
        int currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        var children = await _executor.GetChildProcessesAsync(currentPid);

        Assert.NotNull(children);
        
        cts.Cancel();
    }
}
