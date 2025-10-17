using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested profile cannot be found.
/// </summary>
public class ProfileNotFoundException : ProfileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileNotFoundException"/> class with a profile ID.
    /// </summary>
    /// <param name="profileId">The ID of the profile that was not found.</param>
    public ProfileNotFoundException(int profileId)
        : base($"Profile with ID {profileId} was not found.", profileId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileNotFoundException"/> class with a profile ID and name.
    /// </summary>
    /// <param name="profileId">The ID of the profile that was not found.</param>
    /// <param name="profileName">The name of the profile that was not found.</param>
    public ProfileNotFoundException(int profileId, string profileName)
        : base($"Profile '{profileName}' (ID: {profileId}) was not found.", profileId, profileName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public ProfileNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileNotFoundException"/> class with a custom message
    /// and inner exception.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ProfileNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
