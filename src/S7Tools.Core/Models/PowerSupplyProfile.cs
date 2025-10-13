using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents a named profile for power supply configuration that can be saved, loaded, and applied.
/// Profiles provide a convenient way to manage different power supply configurations for various devices and scenarios.
/// </summary>
public class PowerSupplyProfile
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
    /// Gets or sets the power supply configuration for this profile.
    /// </summary>
    /// <value>The configuration settings that define how to connect to and control the power supply.</value>
    /// <remarks>
    /// This property holds the polymorphic configuration (ModbusTcpConfiguration, ModbusRtuConfiguration, etc.).
    /// The actual type depends on the power supply device type.
    /// </remarks>
    [Required(ErrorMessage = "Profile configuration is required")]
    public PowerSupplyConfiguration Configuration { get; set; } = new ModbusTcpConfiguration();

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
    /// The default S7Tools power supply profile is read-only to prevent accidental modification.
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
    /// Gets or sets additional metadata for this profile.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs for extensibility.</value>
    /// <remarks>
    /// Metadata can be used to store additional information such as:
    /// - Author information
    /// - Use case tags
    /// - Hardware model and manufacturer
    /// - Physical location
    /// - Custom application-specific data
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates the default S7Tools power supply profile with the required configuration.
    /// </summary>
    /// <returns>A new PowerSupplyProfile instance configured with default settings.</returns>
    /// <remarks>
    /// This profile is marked as read-only and default to prevent accidental modification.
    /// It uses Modbus TCP with standard settings suitable for most power supply devices.
    /// </remarks>
    public static PowerSupplyProfile CreateDefaultProfile()
    {
        return new PowerSupplyProfile
        {
            Id = 1, // Default profile always has ID 1
            Name = "S7Tools Default",
            Description = "Default profile for S7Tools application with standard Modbus TCP settings for power supply control. This profile cannot be modified or deleted.",
            Configuration = new ModbusTcpConfiguration
            {
                Type = PowerSupplyType.ModbusTcp,
                Host = "192.168.1.100",
                Port = 502,
                DeviceId = 1,
                OnOffCoil = 0,
                AddressingMode = ModbusAddressingMode.Base0,
                ConnectionTimeoutMs = 5000,
                ReadTimeoutMs = 3000,
                WriteTimeoutMs = 3000,
                EnableAutoReconnect = true,
                MaxRetryAttempts = 3
            },
            IsDefault = true,
            IsReadOnly = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "System",
                ["Purpose"] = "S7Tools Power Supply Control",
                ["Author"] = "S7Tools",
                ["Modifiable"] = "False"
            }
        };
    }

    /// <summary>
    /// Creates a new user profile with default settings.
    /// </summary>
    /// <param name="name">The name for the new profile.</param>
    /// <param name="description">Optional description for the profile.</param>
    /// <returns>A new PowerSupplyProfile instance with default settings.</returns>
    public static PowerSupplyProfile CreateUserProfile(string name, string description = "")
    {
        return new PowerSupplyProfile
        {
            Id = 0, // Will be assigned by the service
            Name = name,
            Description = description,
            Configuration = new ModbusTcpConfiguration
            {
                Type = PowerSupplyType.ModbusTcp,
                Host = "192.168.1.100",
                Port = 502,
                DeviceId = 1,
                OnOffCoil = 0,
                AddressingMode = ModbusAddressingMode.Base0,
                ConnectionTimeoutMs = 5000,
                ReadTimeoutMs = 3000,
                WriteTimeoutMs = 3000,
                EnableAutoReconnect = true,
                MaxRetryAttempts = 3
            },
            IsDefault = false,
            IsReadOnly = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "User",
                ["Author"] = Environment.UserName
            }
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a deep copy of this profile while preserving the profile Id and flags.
    /// This is intended for read/export operations where the persisted identity must be retained.
    /// </summary>
    /// <returns>A new PowerSupplyProfile instance with identical settings including Id.</returns>
    public PowerSupplyProfile ClonePreserveId()
    {
        return new PowerSupplyProfile
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
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Creates a copy of this profile with a new name.
    /// </summary>
    /// <param name="newName">The name for the duplicated profile.</param>
    /// <returns>A new PowerSupplyProfile instance with the specified name.</returns>
    public PowerSupplyProfile Duplicate(string newName)
    {
        return new PowerSupplyProfile
        {
            Id = 0, // Will be assigned by the service
            Name = newName,
            Description = Description,
            Configuration = Configuration.Clone(),
            IsDefault = false, // Duplicates are never default
            IsReadOnly = false, // Duplicates are never read-only
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = Version,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Creates a clone of this profile.
    /// </summary>
    /// <returns>A new PowerSupplyProfile instance with new ID to be assigned.</returns>
    public PowerSupplyProfile Clone()
    {
        return new PowerSupplyProfile
        {
            Id = 0, // Will be assigned by the service
            Name = Name,
            Description = Description,
            Configuration = Configuration.Clone(),
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Version = Version,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Validates this profile and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

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

        if (Configuration == null)
        {
            errors.Add("Profile configuration is required");
        }
        else
        {
            // Validate the configuration
            errors.AddRange(Configuration.Validate());
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

    #endregion
}
