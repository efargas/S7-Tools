using System;
using System.Collections.Generic;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Base interface for all profile types in the S7Tools application.
/// Defines common properties and behaviors shared across SerialPortProfile, SocatProfile, and PowerSupplyProfile.
/// </summary>
/// <remarks>
/// This interface establishes the unified contract for profile management, ensuring consistency
/// across different profile types while supporting the domain's ubiquitous language.
///
/// Design decisions:
/// - CreatedAt/ModifiedAt for audit trail and compliance
/// - Options/Flags for command-line argument storage
/// - Version for profile format evolution
/// - Metadata for extensibility without breaking changes
/// </remarks>
public interface IProfileBase
{
    #region Core Identity Properties

    /// <summary>
    /// Gets or sets the unique identifier for this profile within its type.
    /// </summary>
    /// <value>The profile ID. Must be greater than 0 for persisted profiles.</value>
    /// <remarks>
    /// IDs are assigned sequentially within each profile type, filling gaps when possible.
    /// A value of 0 indicates a new profile that hasn't been persisted yet.
    /// </remarks>
    int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name for this profile.
    /// </summary>
    /// <value>The profile name. Must be unique within the profile type and not exceed 100 characters.</value>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of this profile.
    /// </summary>
    /// <value>An optional description explaining the purpose or use case of this profile.</value>
    string Description { get; set; }

    #endregion

    #region Status and Behavior Properties

    /// <summary>
    /// Gets or sets a value indicating whether this is the default profile for its type.
    /// </summary>
    /// <value>True if this is the default profile, false otherwise.</value>
    /// <remarks>
    /// Business rule: Only one profile per type can be marked as default.
    /// Setting a profile as default automatically clears the default flag from other profiles of the same type.
    /// </remarks>
    bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this profile is read-only.
    /// </summary>
    /// <value>True if this profile cannot be modified or deleted, false otherwise.</value>
    /// <remarks>
    /// Read-only profiles are typically system-defined profiles that ensure critical functionality.
    /// Business rule: Read-only profiles cannot be modified or deleted through standard operations.
    /// </remarks>
    bool IsReadOnly { get; set; }

    #endregion

    #region Command and Configuration Properties

    /// <summary>
    /// Gets or sets command-line options specific to this profile type.
    /// </summary>
    /// <value>Command options or flags used when applying this profile.</value>
    /// <remarks>
    /// For SerialPortProfile: Additional stty options
    /// For SocatProfile: Additional socat command options
    /// For PowerSupplyProfile: Modbus-specific options
    /// </remarks>
    string Options { get; set; }

    /// <summary>
    /// Gets or sets additional flags specific to this profile type.
    /// </summary>
    /// <value>Additional flags or parameters for profile-specific operations.</value>
    /// <remarks>
    /// This property provides extensibility for profile-specific flags without breaking the interface.
    /// Each profile type can define its own flag semantics.
    /// </remarks>
    string Flags { get; set; }

    #endregion

    #region Audit and Versioning Properties

    /// <summary>
    /// Gets or sets the timestamp when this profile was created.
    /// </summary>
    /// <value>The creation timestamp in UTC.</value>
    /// <remarks>
    /// Used for audit trails and compliance requirements.
    /// Set automatically when a profile is first created.
    /// </remarks>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this profile was last modified.
    /// </summary>
    /// <value>The last modification timestamp in UTC.</value>
    /// <remarks>
    /// Updated automatically whenever profile properties are changed.
    /// Used for audit trails and change tracking.
    /// </remarks>
    DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the version of this profile format.
    /// </summary>
    /// <value>The profile format version. Default is "1.0".</value>
    /// <remarks>
    /// Enables profile format evolution and migration strategies.
    /// Used for backward compatibility when profile structures change.
    /// </remarks>
    string Version { get; set; }

    #endregion

    #region Extensibility Properties

    /// <summary>
    /// Gets or sets additional metadata for this profile.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs for extensibility.</value>
    /// <remarks>
    /// Metadata provides extensibility without breaking the interface contract.
    /// Common uses: Author information, tags, hardware compatibility notes, custom application data.
    /// </remarks>
    Dictionary<string, string>? Metadata { get; set; }

    #endregion

    #region Business Methods

    /// <summary>
    /// Determines whether this profile can be modified.
    /// </summary>
    /// <returns>True if the profile can be modified, false if it's read-only.</returns>
    /// <remarks>
    /// Business rule: Read-only profiles cannot be modified.
    /// Used by UI and service layers to enforce business constraints.
    /// </remarks>
    bool CanModify();

    /// <summary>
    /// Determines whether this profile can be deleted.
    /// </summary>
    /// <returns>True if the profile can be deleted, false if it's read-only or default.</returns>
    /// <remarks>
    /// Business rule: Read-only profiles and default profiles cannot be deleted.
    /// Some implementations may allow default profiles to be deleted if another profile becomes default.
    /// </remarks>
    bool CanDelete();

    /// <summary>
    /// Updates the modification timestamp to the current time.
    /// </summary>
    /// <remarks>
    /// Should be called whenever profile properties are modified.
    /// Maintains audit trail consistency across all profile types.
    /// </remarks>
    void Touch();

    /// <summary>
    /// Gets a summary of this profile's key settings.
    /// </summary>
    /// <returns>A string summarizing the profile's configuration.</returns>
    /// <remarks>
    /// Used for display purposes in UI lists and logging.
    /// Each profile type implements its own summary format.
    /// </remarks>
    string GetSummary();

    #endregion
}
