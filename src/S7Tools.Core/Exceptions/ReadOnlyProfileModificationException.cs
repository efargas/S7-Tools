using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to modify or delete a read-only profile.
/// Read-only profiles are typically system-defined and cannot be changed.
/// </summary>
public class ReadOnlyProfileModificationException : ProfileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyProfileModificationException"/> class.
    /// </summary>
    /// <param name="profileId">The ID of the read-only profile.</param>
    public ReadOnlyProfileModificationException(int profileId)
        : base($"Cannot modify or delete read-only profile (ID: {profileId}).", profileId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyProfileModificationException"/> class with profile name.
    /// </summary>
    /// <param name="profileId">The ID of the read-only profile.</param>
    /// <param name="profileName">The name of the read-only profile.</param>
    public ReadOnlyProfileModificationException(int profileId, string profileName)
        : base($"Cannot modify or delete read-only profile '{profileName}' (ID: {profileId}).", profileId, profileName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyProfileModificationException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public ReadOnlyProfileModificationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyProfileModificationException"/> class with inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReadOnlyProfileModificationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
