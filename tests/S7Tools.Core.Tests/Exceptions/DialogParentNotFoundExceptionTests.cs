using FluentAssertions;
using S7Tools.Core.Exceptions;

namespace S7Tools.Core.Tests.Exceptions;

/// <summary>
/// Unit tests for <see cref="DialogParentNotFoundException"/>.
/// </summary>
public class DialogParentNotFoundExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesExceptionWithDefaultMessage()
    {
        // Arrange & Act
        var exception = new DialogParentNotFoundException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNull();
        Assert.IsAssignableFrom<S7ToolsException>(exception);
    }

    [Fact]
    public void Constructor_WithMessage_CreatesExceptionWithSpecifiedMessage()
    {
        // Arrange
        const string expectedMessage = "Dialog parent view model not found";

        // Act
        var exception = new DialogParentNotFoundException(expectedMessage);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(expectedMessage);
        Assert.IsAssignableFrom<S7ToolsException>(exception);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_CreatesExceptionWithBoth()
    {
        // Arrange
        const string expectedMessage = "Dialog parent view model not found";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new DialogParentNotFoundException(expectedMessage, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(expectedMessage);
        exception.InnerException.Should().BeSameAs(innerException);
        Assert.IsAssignableFrom<S7ToolsException>(exception);
    }

    [Fact]
    public void Exception_InheritsFromS7ToolsException()
    {
        // Arrange & Act
        var exception = new DialogParentNotFoundException();

        // Assert
        exception.Should().BeOfType<DialogParentNotFoundException>();
        Assert.IsAssignableFrom<S7ToolsException>(exception);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Exception_CanBeThrownAndCaught()
    {
        // Arrange
        const string expectedMessage = "Test message";
        DialogParentNotFoundException? caughtException = null;

        // Act
        try
        {
            throw new DialogParentNotFoundException(expectedMessage);
        }
        catch (DialogParentNotFoundException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void Exception_CanBeCaughtAsS7ToolsException()
    {
        // Arrange
        const string expectedMessage = "Test message";
        S7ToolsException? caughtException = null;

        // Act
        try
        {
            throw new DialogParentNotFoundException(expectedMessage);
        }
        catch (S7ToolsException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<DialogParentNotFoundException>();
        caughtException.Message.Should().Be(expectedMessage);
    }
}
