using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to delete a profile that is marked as the default profile.
/// Default profiles cannot be deleted to ensure at least one profile always exists.
/// </summary>
public class DefaultProfileDeletionException : ProfileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultProfileDeletionException"/> class.
    /// </summary>
    /// <param name="profileId">The ID of the default profile that cannot be deleted.</param>
    public DefaultProfileDeletionException(int profileId)
        : base($"Cannot delete the default profile (ID: {profileId}). Set another profile as default first.", profileId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultProfileDeletionException"/> class with profile name.
    /// </summary>
    /// <param name="profileId">The ID of the default profile that cannot be deleted.</param>
    /// <param name="profileName">The name of the default profile.</param>
    public DefaultProfileDeletionException(int profileId, string profileName)
        : base($"Cannot delete the default profile '{profileName}' (ID: {profileId}). Set another profile as default first.", profileId, profileName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultProfileDeletionException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public DefaultProfileDeletionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultProfileDeletionException"/> class with inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DefaultProfileDeletionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
