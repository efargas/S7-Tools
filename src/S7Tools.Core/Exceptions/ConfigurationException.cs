using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when a configuration-related error occurs.
/// This includes invalid settings, missing configuration files, or configuration parsing errors.
/// </summary>
public class ConfigurationException : S7ToolsException
{
    /// <summary>
    /// Gets the name of the configuration setting that caused the error, if applicable.
    /// </summary>
    public string? SettingName { get; init; }

    /// <summary>
    /// Gets the configuration file path that caused the error, if applicable.
    /// </summary>
    public string? ConfigurationPath { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a setting name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="settingName">The name of the configuration setting that caused the error.</param>
    public ConfigurationException(string message, string settingName)
        : base(message)
    {
        SettingName = settingName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with setting name and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="settingName">The name of the configuration setting.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConfigurationException(string message, string settingName, Exception innerException)
        : base(message, innerException)
    {
        SettingName = settingName;
    }
}
