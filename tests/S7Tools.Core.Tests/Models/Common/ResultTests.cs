using S7Tools.Core.Models;

namespace S7Tools.Core.Tests.Models.Common;

/// <summary>
/// Unit tests for the Result pattern implementation.
/// Tests both generic Result&lt;T&gt; and non-generic Result.
/// </summary>
public class ResultTests
{
    #region Result<T> Tests

    /// <summary>
    /// Verifies that a successful generic result is created with the correct value.
    /// </summary>
    [Fact]
    public void Success_WithValue_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Error.Should().BeEmpty();
        result.Exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a failed generic result is created with the correct error message.
    /// </summary>
    [Fact]
    public void Generic_Failure_WithErrorMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var result = Result<string>.Failure(errorMessage);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(errorMessage);
        result.Exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetValueOrThrow returns the value for a successful result.
    /// </summary>
    [Fact]
    public void GetValueOrThrow_WithSuccessfulResult_ShouldReturnValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var value = result.GetValueOrThrow();

        // Assert
        value.Should().Be(42);
    }

    /// <summary>
    /// Verifies that GetValueOrThrow throws an exception for a failed result.
    /// </summary>
    [Fact]
    public void GetValueOrThrow_WithFailedResult_ShouldThrowException()
    {
        // Arrange
        var result = Result<int>.Failure("Error occurred");

        // Act & Assert
        var act = () => result.GetValueOrThrow();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Result is in a failure state: Error occurred");
    }

    #endregion

    #region Result (non-generic) Tests

    /// <summary>
    /// Verifies that a successful non-generic result is created correctly.
    /// </summary>
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
        result.Exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a failed non-generic result is created with the correct error message.
    /// </summary>
    [Fact]
    public void NonGeneric_Failure_WithErrorMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var result = Result.Failure(errorMessage);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.Exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that an implicit conversion from true produces a successful result.
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromTrue_ShouldCreateSuccessfulResult()
    {
        // Act
        Result result = true;

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that an implicit conversion from false produces a failed result.
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromFalse_ShouldCreateFailedResult()
    {
        // Act
        Result result = false;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Operation failed");
    }

    #endregion
}