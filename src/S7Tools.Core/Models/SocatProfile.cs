using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents a named profile for socat (Serial-to-TCP Proxy) configuration that can be saved, loaded, and applied.
/// Profiles provide a convenient way to manage different socat configurations for various network-to-serial bridge scenarios.
/// </summary>
public class SocatProfile : IProfileBase
{
    #region Properties

    /// <summary>
    /// Gets or sets the unique identifier for this profile.
    /// </summary>
    /// <value>The profile ID. Auto-generated when creating new profiles.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name for this profile.
    /// </summary>
    /// <value>The profile name. Must be unique and not exceed 100 characters.</value>
    [Required(ErrorMessage = "Profile name is required")]
    [StringLength(100, ErrorMessage = "Profile name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of this profile.
    /// </summary>
    /// <value>An optional description explaining the purpose or use case of this profile.</value>
    [StringLength(500, ErrorMessage = "Profile description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the socat configuration for this profile.
    /// </summary>
    /// <value>The configuration settings that define how socat should operate for the serial-to-TCP bridge.</value>
    [Required(ErrorMessage = "Profile configuration is required")]
    public SocatConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this is the default profile.
    /// </summary>
    /// <value>True if this is the default profile used for new connections, false otherwise.</value>
    /// <remarks>Only one profile can be marked as default at a time.</remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this profile is read-only.
    /// </summary>
    /// <value>True if this profile cannot be modified or deleted, false otherwise.</value>
    /// <remarks>
    /// Read-only profiles are typically system-defined profiles that ensure critical functionality.
    /// The default S7Tools socat profile is read-only to prevent accidental modification.
    /// </remarks>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this profile was created.
    /// </summary>
    /// <value>The creation timestamp in UTC.</value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this profile was last modified.
    /// </summary>
    /// <value>The last modification timestamp in UTC.</value>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the version of this profile format.
    /// </summary>
    /// <value>The profile format version. Default is "1.0".</value>
    [StringLength(10, ErrorMessage = "Version cannot exceed 10 characters")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets command-line options specific to this socat profile.
    /// </summary>
    /// <value>Additional socat command options used when applying this profile.</value>
    /// <remarks>
    /// Options contain additional command-line parameters that can be applied
    /// when starting socat with this profile configuration. This might include:
    /// - Custom debug options like -d, -D, -ly
    /// - Performance tuning parameters
    /// - Advanced socat features
    /// - Protocol-specific options
    /// Example: "-d -d" for double debug output or "-ly" for syslog output
    /// </remarks>
    public string Options { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional flags specific to this socat profile type.
    /// </summary>
    /// <value>Additional flags or parameters for socat-specific operations.</value>
    /// <remarks>
    /// Flags provide extensibility for socat-specific features without breaking the interface.
    /// For socat profiles, this might include settings like:
    /// - auto-restart-on-failure=true
    /// - enable-keepalive=false
    /// - use-ssl=false
    /// - log-transfers=true
    /// Format: key=value pairs separated by semicolons
    /// </remarks>
    public string Flags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata for this profile.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs for extensibility.</value>
    /// <remarks>
    /// Metadata can be used to store additional information such as:
    /// - Author information
    /// - Use case tags
    /// - Hardware compatibility notes
    /// - Network configuration notes
    /// - Custom application-specific data
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates the default S7Tools socat profile with the required configuration.
    /// </summary>
    /// <returns>A read-only profile configured for S7Tools socat functionality.</returns>
    /// <remarks>
    /// This profile generates the socat command required by S7Tools for PLC communication:
    /// socat -d -d -v -b 4 -x TCP-LISTEN:1238,fork,reuseaddr /dev/device,raw,echo=0
    /// </remarks>
    public static SocatProfile CreateDefaultProfile()
    {
        return new SocatProfile
        {
            Id = 1, // Default profile always has ID 1
            Name = "S7Tools Default",
            Description = "Default profile for S7Tools socat server with required settings for PLC communication over TCP. This profile cannot be modified or deleted.",
            Configuration = SocatConfiguration.CreateDefault(),
            IsDefault = true,
            IsReadOnly = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "System",
                ["Purpose"] = "S7Tools PLC Communication via TCP",
                ["Command"] = "socat -d -d -v -b 4 -x TCP-LISTEN:1238,fork,reuseaddr {device},raw,echo=0"
            }
        };
    }

    /// <summary>
    /// Creates a new user profile with default settings.
    /// </summary>
    /// <param name="name">The name for the new profile.</param>
    /// <param name="description">Optional description for the profile.</param>
    /// <returns>A new user profile with default configuration.</returns>
    public static SocatProfile CreateUserProfile(string name, string description = "")
    {
        return new SocatProfile
        {
            Id = 0, // Will be assigned when saved
            Name = name,
            Description = description,
            Configuration = SocatConfiguration.CreateDefault(),
            IsDefault = false,
            IsReadOnly = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0"
        };
    }

    /// <summary>
    /// Creates a profile optimized for high-speed data transfer.
    /// </summary>
    /// <param name="name">The name for the high-speed profile.</param>
    /// <returns>A profile configured for high-speed serial-to-TCP communication.</returns>
    public static SocatProfile CreateHighSpeedProfile(string name = "High Speed")
    {
        return new SocatProfile
        {
            Id = 0, // Will be assigned when saved
            Name = name,
            Description = "Profile optimized for high-speed serial-to-TCP communication with larger block sizes and reduced verbosity.",
            Configuration = SocatConfiguration.CreateHighSpeed(),
            IsDefault = false,
            IsReadOnly = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "User",
                ["Purpose"] = "High Speed Communication",
                ["Optimization"] = "Performance"
            }
        };
    }

    /// <summary>
    /// Creates a profile optimized for debugging with detailed logging.
    /// </summary>
    /// <param name="name">The name for the debug profile.</param>
    /// <returns>A profile configured for debugging with comprehensive logging.</returns>
    public static SocatProfile CreateDebugProfile(string name = "Debug")
    {
        return new SocatProfile
        {
            Id = 0, // Will be assigned when saved
            Name = name,
            Description = "Profile optimized for debugging with verbose logging, hex dumps, and detailed connection information.",
            Configuration = SocatConfiguration.CreateDebug(),
            IsDefault = false,
            IsReadOnly = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "User",
                ["Purpose"] = "Debug and Troubleshooting",
                ["LogLevel"] = "Verbose"
            }
        };
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a deep copy of this profile.
    /// </summary>
    /// <returns>A new SocatProfile instance with identical settings.</returns>
    /// <remarks>
    /// The cloned profile will have:
    /// - A new ID (0, to be assigned when saved)
    /// - Updated creation and modification timestamps
    /// - IsDefault set to false (only one profile can be default)
    /// - IsReadOnly set to false (clones are always editable)
    /// - All other properties copied exactly
    /// </remarks>
    public SocatProfile Clone()
    {
        return new SocatProfile
        {
            Id = 0, // New ID will be assigned when saved
            Name = Name,
            Description = Description,
            Configuration = Configuration.Clone(),
            IsDefault = false, // Clones are never default
            IsReadOnly = false, // Clones are always editable
            CreatedAt = DateTime.UtcNow, // New creation time
            ModifiedAt = DateTime.UtcNow, // New modification time
            Version = Version,
            Options = Options, // Copy options
            Flags = Flags, // Copy flags
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Creates a deep copy of this profile while preserving the profile Id and flags.
    /// This is intended for read/export operations where the persisted identity must be retained.
    /// </summary>
    /// <returns>A new SocatProfile instance with identical settings including Id.</returns>
    public SocatProfile ClonePreserveId()
    {
        return new SocatProfile
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Configuration = Configuration.Clone(),
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Version = Version,
            Options = Options, // Copy options
            Flags = Flags, // Copy flags
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Creates a copy of this profile with a new name.
    /// </summary>
    /// <param name="newName">The name for the duplicated profile.</param>
    /// <returns>A new SocatProfile instance with the specified name.</returns>
    public SocatProfile Duplicate(string newName)
    {
        var duplicate = Clone();
        duplicate.Name = newName;
        duplicate.Description = $"Copy of {Name}";
        return duplicate;
    }

    /// <summary>
    /// Validates this profile and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Profile name is required");
        }
        else if (Name.Length > 100)
        {
            errors.Add("Profile name cannot exceed 100 characters");
        }

        if (!string.IsNullOrEmpty(Description) && Description.Length > 500)
        {
            errors.Add("Profile description cannot exceed 500 characters");
        }

        if (!string.IsNullOrEmpty(Version) && Version.Length > 10)
        {
            errors.Add("Version cannot exceed 10 characters");
        }

        // Validate configuration
        if (Configuration == null)
        {
            errors.Add("Profile configuration is required");
        }
        else
        {
            var configErrors = Configuration.Validate();
            errors.AddRange(configErrors);
        }

        return errors;
    }

    /// <summary>
    /// Updates the modification timestamp to the current time.
    /// </summary>
    public void Touch()
    {
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines whether this profile can be modified.
    /// </summary>
    /// <returns>True if the profile can be modified, false if it's read-only.</returns>
    public bool CanModify()
    {
        return !IsReadOnly;
    }

    /// <summary>
    /// Determines whether this profile can be deleted.
    /// </summary>
    /// <returns>True if the profile can be deleted, false if it's read-only or default.</returns>
    public bool CanDelete()
    {
        return !IsReadOnly && !IsDefault;
    }

    /// <summary>
    /// Gets a summary of this profile's key settings.
    /// </summary>
    /// <returns>A string summarizing the profile's configuration.</returns>
    public string GetSummary()
    {
        var summary = $"{Name}: TCP port {Configuration.TcpPort}, Block size {Configuration.BlockSize}";

        if (Configuration.Verbose)
        {
            summary += " (Verbose)";
        }

        if (Configuration.HexDump)
        {
            summary += " (Hex dump)";
        }

        if (IsDefault)
        {
            summary += " (Default)";
        }

        if (IsReadOnly)
        {
            summary += " (Read-Only)";
        }

        return summary;
    }

    /// <summary>
    /// Returns a string representation of this profile.
    /// </summary>
    /// <returns>A string describing the profile.</returns>
    public override string ToString()
    {
        return GetSummary();
    }

    /// <summary>
    /// Determines whether the specified object is equal to this profile.
    /// </summary>
    /// <param name="obj">The object to compare with this profile.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not SocatProfile other)
        {
            return false;
        }

        return Id == other.Id && Name == other.Name;
    }

    /// <summary>
    /// Returns a hash code for this profile.
    /// </summary>
    /// <returns>A hash code based on the profile ID and name.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }

    #endregion
}
