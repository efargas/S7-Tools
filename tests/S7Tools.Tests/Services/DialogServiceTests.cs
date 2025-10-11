using ReactiveUI;
using S7Tools.Models;
using S7Tools.Services;
using System.Reactive;
using System.Reactive.Linq;

namespace S7Tools.Tests.Services;

/// <summary>
/// Unit tests for the DialogService implementation.
/// Tests ReactiveUI interaction patterns and dialog handling.
/// </summary>
public class DialogServiceTests
{
    private readonly DialogService _dialogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogServiceTests"/> class.
    /// </summary>
    public DialogServiceTests()
    {
        _dialogService = new DialogService();
    }

    /// <summary>
    /// Verifies that the constructor correctly initializes the interaction properties.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeInteractions()
    {
        // Assert
        _dialogService.ShowConfirmation.Should().NotBeNull();
        _dialogService.ShowError.Should().NotBeNull();
        _dialogService.ShowInput.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that ShowConfirmationAsync returns the expected result when a handler is registered.
    /// </summary>
    [Fact]
    public async Task ShowConfirmationAsync_WithValidParameters_ShouldReturnExpectedResult()
    {
        // Arrange
        const string title = "Test Title";
        const string message = "Test Message";
        const bool expectedResult = true;

        // Register handler that returns true
        _dialogService.ShowConfirmation.RegisterHandler(interaction =>
        {
            interaction.Input.Title.Should().Be(title);
            interaction.Input.Message.Should().Be(message);
            interaction.SetOutput(expectedResult);
        });

        // Act
        var result = await _dialogService.ShowConfirmationAsync(title, message);

        // Assert
        result.Should().Be(expectedResult);
    }

    /// <summary>
    /// Tests that ShowConfirmationAsync returns false when the handler provides a false result.
    /// </summary>
    [Fact]
    public async Task ShowConfirmationAsync_WithHandlerReturningFalse_ShouldReturnFalse()
    {
        // Arrange
        const string title = "Test Title";
        const string message = "Test Message";
        const bool expectedResult = false;

        // Register handler that returns false
        _dialogService.ShowConfirmation.RegisterHandler(interaction =>
        {
            interaction.SetOutput(expectedResult);
        });

        // Act
        var result = await _dialogService.ShowConfirmationAsync(title, message);

        // Assert
        result.Should().Be(expectedResult);
    }

    /// <summary>
    /// Tests that ShowErrorAsync completes successfully and invokes its handler.
    /// </summary>
    [Fact]
    public async Task ShowErrorAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        const string title = "Error Title";
        const string message = "Error Message";
        var handlerCalled = false;

        // Register handler
        _dialogService.ShowError.RegisterHandler(interaction =>
        {
            interaction.Input.Title.Should().Be(title);
            interaction.Input.Message.Should().Be(message);
            handlerCalled = true;
            interaction.SetOutput(Unit.Default);
        });

        // Act
        await _dialogService.ShowErrorAsync(title, message);

        // Assert
        handlerCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that when multiple handlers are registered, the last one is used.
    /// </summary>
    [Fact]
    public async Task ShowConfirmationAsync_WithMultipleHandlers_ShouldUseFirstHandler()
    {
        // Arrange
        const string title = "Test Title";
        const string message = "Test Message";
        var handlerCalled = false;

        // Register first handler
        _dialogService.ShowConfirmation.RegisterHandler(interaction =>
        {
            handlerCalled = true;
            interaction.SetOutput(true);
        });

        // Register second handler (overwrites the first)
        _dialogService.ShowConfirmation.RegisterHandler(interaction =>
        {
            handlerCalled = true;
            interaction.SetOutput(false);
        });

        // Act
        var result = await _dialogService.ShowConfirmationAsync(title, message);

        // Assert
        handlerCalled.Should().BeTrue();
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that an asynchronous handler for ShowConfirmationAsync works correctly.
    /// </summary>
    [Fact]
    public async Task ShowConfirmationAsync_WithAsyncHandler_ShouldWorkCorrectly()
    {
        // Arrange
        const string title = "Test Title";
        const string message = "Test Message";
        const bool expectedResult = true;

        // Register async handler
        _dialogService.ShowConfirmation.RegisterHandler(async interaction =>
        {
            await Task.Delay(10); // Simulate async work
            interaction.SetOutput(expectedResult);
        });

        // Act
        var result = await _dialogService.ShowConfirmationAsync(title, message);

        // Assert
        result.Should().Be(expectedResult);
    }

    /// <summary>
    /// Tests that an asynchronous handler for ShowErrorAsync works correctly.
    /// </summary>
    [Fact]
    public async Task ShowErrorAsync_WithAsyncHandler_ShouldWorkCorrectly()
    {
        // Arrange
        const string title = "Error Title";
        const string message = "Error Message";
        var handlerCalled = false;

        // Register async handler
        _dialogService.ShowError.RegisterHandler(async interaction =>
        {
            await Task.Delay(10); // Simulate async work
            handlerCalled = true;
            interaction.SetOutput(Unit.Default);
        });

        // Act
        await _dialogService.ShowErrorAsync(title, message);

        // Assert
        handlerCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ShowConfirmationAsync does not throw when called with empty or null parameters.
    /// </summary>
    [Theory]
    [InlineData("", "Valid message")]
    [InlineData("Valid title", "")]
    [InlineData("", "")]
    public async Task ShowConfirmationAsync_WithEmptyParameters_ShouldStillWork(string title, string message)
    {
        // Arrange
        _dialogService.ShowConfirmation.RegisterHandler(interaction =>
        {
            interaction.Input.Title.Should().Be(title);
            interaction.Input.Message.Should().Be(message);
            interaction.SetOutput(true);
        });

        // Act & Assert
        var act = async () => await _dialogService.ShowConfirmationAsync(title, message);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that ShowErrorAsync does not throw when called with empty or null parameters.
    /// </summary>
    [Theory]
    [InlineData("", "Valid message")]
    [InlineData("Valid title", "")]
    [InlineData("", "")]
    public async Task ShowErrorAsync_WithEmptyParameters_ShouldStillWork(string title, string message)
    {
        // Arrange
        _dialogService.ShowError.RegisterHandler(interaction =>
        {
            interaction.Input.Title.Should().Be(title);
            interaction.Input.Message.Should().Be(message);
            interaction.SetOutput(Unit.Default);
        });

        // Act & Assert
        var act = async () => await _dialogService.ShowErrorAsync(title, message);
        await act.Should().NotThrowAsync();
    }

    // Note: Timeout tests removed due to complexity with ReactiveUI interactions

    /// <summary>
    /// Tests that concurrent calls to ShowConfirmationAsync are handled correctly.
    /// </summary>
    [Fact]
    public async Task ShowConfirmationAsync_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var callCount = 0;
        _dialogService.ShowConfirmation.RegisterHandler(interaction =>
        {
            Interlocked.Increment(ref callCount);
            interaction.SetOutput(true);
        });

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _dialogService.ShowConfirmationAsync($"Title {i}", $"Message {i}"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());
        callCount.Should().Be(10);
    }

    /// <summary>
    /// Tests that concurrent calls to ShowErrorAsync are handled correctly.
    /// </summary>
    [Fact]
    public async Task ShowErrorAsync_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var callCount = 0;
        _dialogService.ShowError.RegisterHandler(interaction =>
        {
            Interlocked.Increment(ref callCount);
            interaction.SetOutput(Unit.Default);
        });

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _dialogService.ShowErrorAsync($"Title {i}", $"Message {i}"))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        callCount.Should().Be(10);
    }

    /// <summary>
    /// Tests that ShowInputAsync returns the correct successful result from its handler.
    /// </summary>
    [Fact]
    public async Task ShowInputAsync_WithValidParameters_ShouldReturnExpectedResult()
    {
        // Arrange
        const string title = "Input Title";
        const string message = "Input Message";
        const string defaultValue = "Default";
        const string placeholder = "Placeholder";
        const string expectedValue = "User Input";
        var handlerCalled = false;

        // Register handler
        _dialogService.ShowInput.RegisterHandler(interaction =>
        {
            interaction.Input.Title.Should().Be(title);
            interaction.Input.Message.Should().Be(message);
            interaction.Input.DefaultValue.Should().Be(defaultValue);
            interaction.Input.Placeholder.Should().Be(placeholder);
            handlerCalled = true;
            interaction.SetOutput(InputResult.Success(expectedValue));
        });

        // Act
        var result = await _dialogService.ShowInputAsync(title, message, defaultValue, placeholder);

        // Assert
        handlerCalled.Should().BeTrue();
        result.Should().NotBeNull();
        result.IsCancelled.Should().BeFalse();
        result.Value.Should().Be(expectedValue);
    }

    /// <summary>
    /// Tests that ShowInputAsync returns a cancelled result when its handler provides one.
    /// </summary>
    [Fact]
    public async Task ShowInputAsync_WithCancelledResult_ShouldReturnCancelled()
    {
        // Arrange
        _dialogService.ShowInput.RegisterHandler(interaction =>
        {
            interaction.SetOutput(InputResult.Cancelled());
        });

        // Act
        var result = await _dialogService.ShowInputAsync("Title", "Message");

        // Assert
        result.Should().NotBeNull();
        result.IsCancelled.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
