using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to create or rename a profile with a name that already exists.
/// </summary>
public class DuplicateProfileNameException : ProfileException
{
    /// <summary>
    /// Gets the duplicate profile name that caused the exception.
    /// </summary>
    public string DuplicateName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateProfileNameException"/> class.
    /// </summary>
    /// <param name="profileName">The duplicate profile name.</param>
    public DuplicateProfileNameException(string profileName)
        : base($"A profile with the name '{profileName}' already exists.")
    {
        DuplicateName = profileName;
        ProfileName = profileName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateProfileNameException"/> class with a custom message.
    /// </summary>
    /// <param name="profileName">The duplicate profile name.</param>
    /// <param name="message">The custom error message.</param>
    public DuplicateProfileNameException(string profileName, string message)
        : base(message)
    {
        DuplicateName = profileName;
        ProfileName = profileName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateProfileNameException"/> class with inner exception.
    /// </summary>
    /// <param name="profileName">The duplicate profile name.</param>
    /// <param name="innerException">The inner exception.</param>
    public DuplicateProfileNameException(string profileName, Exception innerException)
        : base($"A profile with the name '{profileName}' already exists.", innerException)
    {
        DuplicateName = profileName;
        ProfileName = profileName;
    }
}
