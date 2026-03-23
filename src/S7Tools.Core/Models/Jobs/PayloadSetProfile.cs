using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Defines the configuration for bootloader payload files.
/// Specifies the base path where stager and dumper payloads are located.
/// </summary>
/// <remarks>
/// PayloadSetProfile is a profile that defines the location of bootloader payloads (stager and dumper).
/// It implements IProfileBase to participate in the unified profile management system.
/// </remarks>
public class PayloadSetProfile : IProfileBase
{
    #region IProfileBase Core Identity

    /// <inheritdoc/>
    public int Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Description { get; set; } = string.Empty;

    #endregion

    #region IProfileBase Status and Behavior

    /// <inheritdoc/>
    public bool IsDefault { get; set; }

    /// <inheritdoc/>
    public bool IsReadOnly { get; set; }

    #endregion

    #region IProfileBase Command and Configuration

    /// <inheritdoc/>
    public string Options { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Flags { get; set; } = string.Empty;

    #endregion

    #region IProfileBase Audit and Versioning

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string Version { get; set; } = "1.0";

    #endregion

    #region IProfileBase Extensibility

    /// <inheritdoc/>
    public Dictionary<string, string>? Metadata { get; set; }

    #endregion

    #region Payload-Specific Properties

    /// <summary>
    /// Gets or sets the base directory path containing payload files.
    /// </summary>
    /// <value>The base directory path where stager and dumper binaries are located.</value>
    /// <remarks>
    /// This path should contain:
    /// - stager.bin: First-stage bootloader payload
    /// - dumper.bin: Memory dumper payload
    /// Path can be absolute or relative to the application directory.
    /// </remarks>
    [Display(Name = "Payload Base Path", Order = 1)]
    public string BasePath { get; set; } = string.Empty;

    #endregion

    #region IProfileBase Business Methods

    /// <inheritdoc/>
    public bool CanModify()
    {
        return !IsReadOnly;
    }

    /// <inheritdoc/>
    public bool CanDelete()
    {
        return !IsReadOnly && !IsDefault;
    }

    /// <inheritdoc/>
    public void Touch()
    {
        ModifiedAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public string GetSummary()
    {
        return $"Payloads at: {BasePath}";
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a default payload set profile pointing to the standard payload directory.
    /// </summary>
    /// <returns>A new PayloadSetProfile with default settings.</returns>
    public static PayloadSetProfile CreateDefault()
    {
        return new PayloadSetProfile
        {
            Id = 0,
            Name = "Default Payloads",
            Description = "Default payload set profile",
            BasePath = "bootloader-payloads/payloads",
            IsDefault = true,
            IsReadOnly = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0"
        };
    }

    /// <summary>
    /// Creates a user-defined payload set profile with specified name and path.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new PayloadSetProfile with user-specified settings.</returns>
    public static PayloadSetProfile CreateUser(string name, string basePath, string? description = null)
    {
        return new PayloadSetProfile
        {
            Id = 0,
            Name = name,
            Description = description ?? $"User payload set: {name}",
            BasePath = basePath,
            IsDefault = false,
            IsReadOnly = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0"
        };
    }

    #endregion
}
