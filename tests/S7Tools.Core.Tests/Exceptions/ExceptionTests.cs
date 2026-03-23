using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using S7Tools.Core.Exceptions;
using Xunit;

namespace S7Tools.Core.Tests.Exceptions;

/// <summary>
/// Unit tests for custom domain exception classes.
/// Verifies exception hierarchy, properties, and message formatting.
/// </summary>
public class ExceptionTests
{
    #region S7ToolsException Tests

    [Fact]
    public void S7ToolsException_DefaultConstructor_CreatesException()
    {
        // Act
        var exception = new S7ToolsException();

        // Assert
        exception.Should().NotBeNull();
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void S7ToolsException_WithMessage_SetsMessage()
    {
        // Arrange
        const string Message = "Test error message";

        // Act
        var exception = new S7ToolsException(Message);

        // Assert
        exception.Message.Should().Be(Message);
    }

    [Fact]
    public void S7ToolsException_WithInnerException_SetsInnerException()
    {
        // Arrange
        const string Message = "Test error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new S7ToolsException(Message, innerException);

        // Assert
        exception.Message.Should().Be(Message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region ProfileException Tests

    [Fact]
    public void ProfileException_WithProfileId_SetsProfileId()
    {
        // Arrange
        const int ProfileId = 42;
        const string Message = "Profile error";

        // Act
        var exception = new ProfileException(Message, ProfileId);

        // Assert
        exception.Message.Should().Be(Message);
        exception.ProfileId.Should().Be(ProfileId);
    }

    [Fact]
    public void ProfileException_WithProfileIdAndName_SetsBothProperties()
    {
        // Arrange
        const int ProfileId = 42;
        const string ProfileName = "Test Profile";
        const string Message = "Profile error";

        // Act
        var exception = new ProfileException(Message, ProfileId, ProfileName);

        // Assert
        exception.Message.Should().Be(Message);
        exception.ProfileId.Should().Be(ProfileId);
        exception.ProfileName.Should().Be(ProfileName);
    }

    [Fact]
    public void ProfileException_InheritsFromS7ToolsException()
    {
        // Act
        var exception = new ProfileException("Test");

        // Assert
        Assert.IsAssignableFrom<S7ToolsException>(exception);
    }

    #endregion

    #region ProfileNotFoundException Tests

    [Fact]
    public void ProfileNotFoundException_WithProfileId_FormatsMessage()
    {
        // Arrange
        const int ProfileId = 123;

        // Act
        var exception = new ProfileNotFoundException(ProfileId);

        // Assert
        Assert.Contains("123", exception.Message);
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        exception.ProfileId.Should().Be(ProfileId);
    }

    [Fact]
    public void ProfileNotFoundException_WithProfileIdAndName_FormatsMessageWithBoth()
    {
        // Arrange
        const int ProfileId = 123;
        const string ProfileName = "MyProfile";

        // Act
        var exception = new ProfileNotFoundException(ProfileId, ProfileName);

        // Assert
        Assert.Contains("123", exception.Message);
        Assert.Contains("MyProfile", exception.Message);
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        exception.ProfileId.Should().Be(ProfileId);
        exception.ProfileName.Should().Be(ProfileName);
    }

    [Fact]
    public void ProfileNotFoundException_InheritsFromProfileException()
    {
        // Act
        var exception = new ProfileNotFoundException(1);

        // Assert
        Assert.IsAssignableFrom<ProfileException>(exception);
    }

    #endregion

    #region DuplicateProfileNameException Tests

    [Fact]
    public void DuplicateProfileNameException_WithProfileName_FormatsMessage()
    {
        // Arrange
        const string ProfileName = "DuplicateName";

        // Act
        var exception = new DuplicateProfileNameException(ProfileName);

        // Assert
        Assert.Contains(ProfileName, exception.Message);
        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        exception.DuplicateName.Should().Be(ProfileName);
        exception.ProfileName.Should().Be(ProfileName);
    }

    [Fact]
    public void DuplicateProfileNameException_InheritsFromProfileException()
    {
        // Act
        var exception = new DuplicateProfileNameException("Test");

        // Assert
        Assert.IsAssignableFrom<ProfileException>(exception);
    }

    #endregion

    #region DefaultProfileDeletionException Tests

    [Fact]
    public void DefaultProfileDeletionException_WithProfileId_FormatsMessage()
    {
        // Arrange
        const int ProfileId = 1;

        // Act
        var exception = new DefaultProfileDeletionException(ProfileId);

        // Assert
        Assert.Contains("1", exception.Message);
        Assert.Contains("default", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot delete", exception.Message, StringComparison.OrdinalIgnoreCase);
        exception.ProfileId.Should().Be(ProfileId);
    }

    [Fact]
    public void DefaultProfileDeletionException_WithProfileIdAndName_FormatsMessageWithBoth()
    {
        // Arrange
        const int ProfileId = 1;
        const string ProfileName = "DefaultProfile";

        // Act
        var exception = new DefaultProfileDeletionException(ProfileId, ProfileName);

        // Assert
        Assert.Contains("1", exception.Message);
        Assert.Contains("DefaultProfile", exception.Message);
        Assert.Contains("default", exception.Message, StringComparison.OrdinalIgnoreCase);
        exception.ProfileId.Should().Be(ProfileId);
        exception.ProfileName.Should().Be(ProfileName);
    }

    [Fact]
    public void DefaultProfileDeletionException_InheritsFromProfileException()
    {
        // Act
        var exception = new DefaultProfileDeletionException(1);

        // Assert
        Assert.IsAssignableFrom<ProfileException>(exception);
    }

    #endregion

    #region ReadOnlyProfileModificationException Tests

    [Fact]
    public void ReadOnlyProfileModificationException_WithProfileId_FormatsMessage()
    {
        // Arrange
        const int profileId = 99;

        // Act
        var exception = new ReadOnlyProfileModificationException(profileId);

        // Assert
        Assert.Contains("99", exception.Message);
        Assert.Contains("read-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot modify", exception.Message, StringComparison.OrdinalIgnoreCase);
        exception.ProfileId.Should().Be(profileId);
    }

    [Fact]
    public void ReadOnlyProfileModificationException_WithProfileIdAndName_FormatsMessageWithBoth()
    {
        // Arrange
        const int profileId = 99;
        const string profileName = "SystemProfile";

        // Act
        var exception = new ReadOnlyProfileModificationException(profileId, profileName);

        // Assert
        Assert.Contains("99", exception.Message);
        Assert.Contains("SystemProfile", exception.Message);
        Assert.Contains("read-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        exception.ProfileId.Should().Be(profileId);
        exception.ProfileName.Should().Be(profileName);
    }

    [Fact]
    public void ReadOnlyProfileModificationException_InheritsFromProfileException()
    {
        // Act
        var exception = new ReadOnlyProfileModificationException(1);

        // Assert
        Assert.IsAssignableFrom<ProfileException>(exception);
    }

    #endregion

    #region ConnectionException Tests

    [Fact]
    public void ConnectionException_WithConnectionDetails_SetsProperties()
    {
        // Arrange
        const string message = "Connection failed";
        const string target = "192.168.1.100";
        const string type = "PLC";

        // Act
        var exception = new ConnectionException(message, target, type);

        // Assert
        exception.Message.Should().Be(message);
        exception.ConnectionTarget.Should().Be(target);
        exception.ConnectionType.Should().Be(type);
    }

    [Fact]
    public void ConnectionException_InheritsFromS7ToolsException()
    {
        // Act
        var exception = new ConnectionException("Test");

        // Assert
        Assert.IsAssignableFrom<S7ToolsException>(exception);
    }

    #endregion

    #region ValidationException Tests

    [Fact]
    public void ValidationException_WithSingleError_SetsMessage()
    {
        // Arrange
        const string error = "Field is required";

        // Act
        var exception = new ValidationException(error);

        // Assert
        exception.Message.Should().Be(error);
        exception.ValidationErrors.Should().ContainSingle();
        exception.ValidationErrors[0].Should().Be(error);
    }

    [Fact]
    public void ValidationException_WithMultipleErrors_FormatsMessage()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        Assert.Contains("3 error(s)", exception.Message);
        exception.ValidationErrors.Count.Should().Be(3);
        exception.ValidationErrors.Should().Be(errors);
    }

    [Fact]
    public void ValidationException_WithPropertyName_FormatsMessageWithProperty()
    {
        // Arrange
        const string propertyName = "EmailAddress";
        const string error = "Invalid format";

        // Act
        var exception = new ValidationException(propertyName, error);

        // Assert
        Assert.Contains(propertyName, exception.Message);
        Assert.Contains(error, exception.Message);
        exception.PropertyName.Should().Be(propertyName);
    }

    [Fact]
    public void ValidationException_WithEmptyErrors_UsesDefaultMessage()
    {
        // Arrange
        var errors = new List<string>();

        // Act
        var exception = new ValidationException(errors);

        // Assert
        Assert.Contains("Validation failed", exception.Message);
        exception.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationException_InheritsFromS7ToolsException()
    {
        // Act
        var exception = new ValidationException("Test");

        // Assert
        Assert.IsAssignableFrom<S7ToolsException>(exception);
    }

    #endregion

    #region ConfigurationException Tests

    [Fact]
    public void ConfigurationException_WithSettingName_SetsProperty()
    {
        // Arrange
        const string message = "Invalid configuration";
        const string settingName = "ConnectionTimeout";

        // Act
        var exception = new ConfigurationException(message, settingName);

        // Assert
        exception.Message.Should().Be(message);
        exception.SettingName.Should().Be(settingName);
    }

    [Fact]
    public void ConfigurationException_InheritsFromS7ToolsException()
    {
        // Act
        var exception = new ConfigurationException("Test");

        // Assert
        Assert.IsAssignableFrom<S7ToolsException>(exception);
    }

    #endregion

    #region Exception Hierarchy Tests

    [Fact]
    public void AllCustomExceptions_InheritFromS7ToolsException()
    {
        // Arrange & Act
        var exceptions = new Exception[]
        {
            new ProfileException("Test"),
            new ProfileNotFoundException(1),
            new DuplicateProfileNameException("Test"),
            new DefaultProfileDeletionException(1),
            new ReadOnlyProfileModificationException(1),
            new ConnectionException("Test"),
            new ValidationException("Test"),
            new ConfigurationException("Test")
        };

        // Assert
        foreach (Exception exception in exceptions)
        {
            Assert.IsAssignableFrom<S7ToolsException>(exception);
        }
    }

    [Fact]
    public void AllProfileExceptions_InheritFromProfileException()
    {
        // Arrange & Act
        var exceptions = new Exception[]
        {
            new ProfileNotFoundException(1),
            new DuplicateProfileNameException("Test"),
            new DefaultProfileDeletionException(1),
            new ReadOnlyProfileModificationException(1)
        };

        // Assert
        foreach (Exception exception in exceptions)
        {
            Assert.IsAssignableFrom<ProfileException>(exception);
        }
    }

    #endregion
}
