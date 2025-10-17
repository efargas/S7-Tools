using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Base exception class for profile-related errors.
/// Provides context about which profile caused the exception.
/// </summary>
public class ProfileException : S7ToolsException
{
    /// <summary>
    /// Gets the ID of the profile that caused the exception, if available.
    /// </summary>
    public int? ProfileId { get; init; }

    /// <summary>
    /// Gets the name of the profile that caused the exception, if available.
    /// </summary>
    public string? ProfileName { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class.
    /// </summary>
    public ProfileException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProfileException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message
    /// and profile ID.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileId">The ID of the profile that caused the exception.</param>
    public ProfileException(string message, int profileId)
        : base(message)
    {
        ProfileId = profileId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message,
    /// profile ID, and profile name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileId">The ID of the profile that caused the exception.</param>
    /// <param name="profileName">The name of the profile that caused the exception.</param>
    public ProfileException(string message, int profileId, string profileName)
        : base(message)
    {
        ProfileId = profileId;
        ProfileName = profileName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message
    /// and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProfileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
