using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents a bootloader job configuration that can be saved, loaded, and applied.
/// Jobs provide a convenient way to manage different PLC memory dumping configurations for various scenarios.
/// This model integrates with the unified profile management system for consistent CRUD operations.
/// </summary>
public class JobProfile : IProfileBase
{
    #region Properties

    /// <summary>
    /// Gets or sets the unique identifier for this job profile.
    /// </summary>
    /// <value>The profile ID. Auto-generated when creating new profiles.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name for this job profile.
    /// </summary>
    /// <value>The profile name. Must be unique and not exceed 100 characters.</value>
    [Required(ErrorMessage = "Job name is required")]
    [StringLength(100, ErrorMessage = "Job name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of this job profile.
    /// </summary>
    /// <value>An optional description explaining the purpose or use case of this job.</value>
    [StringLength(500, ErrorMessage = "Job description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command-line options specific to this job.
    /// </summary>
    /// <value>Additional command-line parameters or configuration settings for job execution.</value>
    /// <remarks>
    /// Options contain additional parameters that can be applied when executing the job. This might include:
    /// - Timeout values and retry parameters
    /// - Debug flags and verbose output options
    /// - Custom bootloader parameters
    /// - Memory dump format options
    /// Example: "--timeout=30000 --retries=3 --verbose" or "--debug-mode=true"
    /// </remarks>
    public string Options { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional flags specific to this job type.
    /// </summary>
    /// <value>Additional flags or parameters for job-specific operations.</value>
    /// <remarks>
    /// Flags provide extensibility for job-specific features without breaking the interface.
    /// For job profiles, this might include settings like:
    /// - auto-retry-on-failure=true
    /// - skip-version-check=false
    /// - enable-progress-logging=true
    /// - save-intermediate-files=false
    /// Format: key=value pairs separated by semicolons
    /// </remarks>
    public string Flags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this job profile was created.
    /// </summary>
    /// <value>The creation timestamp in UTC.</value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this job profile was last modified.
    /// </summary>
    /// <value>The last modification timestamp in UTC.</value>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the version of this job profile format.
    /// </summary>
    /// <value>The profile format version. Default is "1.0".</value>
    [StringLength(10, ErrorMessage = "Version cannot exceed 10 characters")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets a value indicating whether this is the default job profile.
    /// </summary>
    /// <value>True if this is the default profile used for new jobs, false otherwise.</value>
    /// <remarks>Only one job profile can be marked as default at a time.</remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this job profile is read-only.
    /// </summary>
    /// <value>True if this profile cannot be modified or deleted, false otherwise.</value>
    /// <remarks>
    /// Read-only profiles are typically system-defined profiles that ensure critical functionality.
    /// The default S7Tools job profile is read-only to prevent accidental modification.
    /// </remarks>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this profile is a template.
    /// </summary>
    /// <value>True if this profile can be used as a template for creating new jobs, false otherwise.</value>
    /// <remarks>Templates provide a convenient way to create new jobs with predefined configurations.</remarks>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for this job profile.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs for extensibility.</value>
    /// <remarks>
    /// Metadata can be used to store additional information such as:
    /// - Author information
    /// - Use case tags
    /// - Target hardware model and version
    /// - Physical location or test setup
    /// - Custom application-specific data
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }

    #endregion

    #region Job Configuration Properties

    /// <summary>
    /// Gets or sets the serial profile reference for this job.
    /// </summary>
    /// <value>The ID of the serial port profile to use for PLC communication.</value>
    public int SerialProfileId { get; set; }

    /// <summary>
    /// Gets or sets the socat profile reference for this job.
    /// </summary>
    /// <value>The ID of the socat profile to use for serial-to-TCP bridging.</value>
    public int SocatProfileId { get; set; }

    /// <summary>
    /// Gets or sets the power supply profile reference for this job.
    /// </summary>
    /// <value>The ID of the power supply profile to use for PLC power control.</value>
    public int PowerSupplyProfileId { get; set; }

    /// <summary>
    /// Gets or sets the memory region configuration for this job.
    /// </summary>
    /// <value>The memory region parameters for the dump operation.</value>
    [Required(ErrorMessage = "Memory region configuration is required")]
    public MemoryRegionProfile MemoryRegion { get; set; } = new(0x20000000, 0x1000);

    /// <summary>
    /// Gets or sets the payload configuration for this job.
    /// </summary>
    /// <value>The payload files and configuration for the bootloader operation.</value>
    [Required(ErrorMessage = "Payload configuration is required")]
    public PayloadSetProfile Payloads { get; set; } = new("./bootloader-payloads");

    /// <summary>
    /// Gets or sets the output path for dump files.
    /// </summary>
    /// <value>The directory path where memory dump files will be saved.</value>
    [Required(ErrorMessage = "Output path is required")]
    [StringLength(500, ErrorMessage = "Output path cannot exceed 500 characters")]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the power on time in milliseconds.
    /// </summary>
    /// <value>The duration to wait after powering on the PLC before beginning operations.</value>
    public int PowerOnTimeMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the power off delay in milliseconds.
    /// </summary>
    /// <value>The duration to wait after powering off the PLC before powering on again.</value>
    public int PowerOffDelayMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets a value indicating whether to use default output path naming.
    /// </summary>
    /// <value>True to use automatic path generation, false to use the specified OutputPath.</value>
    public bool UseDefaultPath { get; set; } = true;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates the default S7Tools job profile with standard configuration.
    /// </summary>
    /// <returns>A new JobProfile instance configured with default settings.</returns>
    public static JobProfile CreateDefaultProfile()
    {
        return new JobProfile
        {
            Id = 1,
            Name = "S7Tools Default",
            Description = "Default job profile for S7Tools application with standard memory dump settings. This profile cannot be modified or deleted.",
            SerialProfileId = 1, // Default serial profile
            SocatProfileId = 1, // Default socat profile
            PowerSupplyProfileId = 1, // Default power supply profile
            MemoryRegion = new MemoryRegionProfile(0x20000000, 0x1000), // Default 4KB dump from start of user memory
            Payloads = new PayloadSetProfile("./bootloader-payloads"), // Default payload configuration
            OutputPath = "./dumps",
            PowerOnTimeMs = 5000,
            PowerOffDelayMs = 2000,
            UseDefaultPath = true,
            IsDefault = true,
            IsReadOnly = true,
            IsTemplate = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "System",
                ["Purpose"] = "S7Tools Memory Dump Operation",
                ["Author"] = "S7Tools",
                ["Modifiable"] = "False"
            }
        };
    }

    /// <summary>
    /// Creates a new user job profile with default settings.
    /// </summary>
    /// <param name="name">The name for the new job profile.</param>
    /// <param name="description">Optional description for the profile.</param>
    /// <returns>A new JobProfile instance with default settings.</returns>
    public static JobProfile CreateUserProfile(string name, string description = "")
    {
        return new JobProfile
        {
            Id = 0, // Will be assigned by the service
            Name = name,
            Description = description,
            SerialProfileId = 1, // Default to first available profile
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            MemoryRegion = new MemoryRegionProfile(0x20000000, 0x1000),
            Payloads = new PayloadSetProfile("./bootloader-payloads"),
            OutputPath = "./dumps",
            PowerOnTimeMs = 5000,
            PowerOffDelayMs = 2000,
            UseDefaultPath = true,
            IsDefault = false,
            IsReadOnly = false,
            IsTemplate = false,
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

    /// <summary>
    /// Creates a job template from this profile.
    /// </summary>
    /// <param name="templateName">The name for the template.</param>
    /// <returns>A new JobProfile marked as a template.</returns>
    public JobProfile CreateTemplate(string templateName)
    {
        return new JobProfile
        {
            Id = 0, // Will be assigned by the service
            Name = templateName,
            Description = $"Template based on {Name}",
            SerialProfileId = SerialProfileId,
            SocatProfileId = SocatProfileId,
            PowerSupplyProfileId = PowerSupplyProfileId,
            MemoryRegion = MemoryRegion,
            Payloads = Payloads,
            OutputPath = OutputPath,
            PowerOnTimeMs = PowerOnTimeMs,
            PowerOffDelayMs = PowerOffDelayMs,
            UseDefaultPath = UseDefaultPath,
            IsDefault = false,
            IsReadOnly = false,
            IsTemplate = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = Version,
            Options = Options,
            Flags = Flags,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a deep copy of this job profile while preserving the profile Id and flags.
    /// </summary>
    /// <returns>A new JobProfile instance with identical settings including Id.</returns>
    public JobProfile ClonePreserveId()
    {
        return new JobProfile
        {
            Id = Id,
            Name = Name,
            Description = Description,
            SerialProfileId = SerialProfileId,
            SocatProfileId = SocatProfileId,
            PowerSupplyProfileId = PowerSupplyProfileId,
            MemoryRegion = MemoryRegion,
            Payloads = Payloads,
            OutputPath = OutputPath,
            PowerOnTimeMs = PowerOnTimeMs,
            PowerOffDelayMs = PowerOffDelayMs,
            UseDefaultPath = UseDefaultPath,
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            IsTemplate = IsTemplate,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Version = Version,
            Options = Options,
            Flags = Flags,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Creates a copy of this job profile with a new name.
    /// </summary>
    /// <param name="newName">The name for the duplicated profile.</param>
    /// <returns>A new JobProfile instance with the specified name.</returns>
    public JobProfile Duplicate(string newName)
    {
        return new JobProfile
        {
            Id = 0, // Will be assigned by the service
            Name = newName,
            Description = Description,
            SerialProfileId = SerialProfileId,
            SocatProfileId = SocatProfileId,
            PowerSupplyProfileId = PowerSupplyProfileId,
            MemoryRegion = MemoryRegion,
            Payloads = Payloads,
            OutputPath = OutputPath,
            PowerOnTimeMs = PowerOnTimeMs,
            PowerOffDelayMs = PowerOffDelayMs,
            UseDefaultPath = UseDefaultPath,
            IsDefault = false, // Duplicates are never default
            IsReadOnly = false, // Duplicates are never read-only
            IsTemplate = false, // Duplicates are not templates by default
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = Version,
            Options = Options,
            Flags = Flags,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Validates this job profile and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Job name is required");
        }
        else if (Name.Length > 100)
        {
            errors.Add("Job name cannot exceed 100 characters");
        }

        if (!string.IsNullOrEmpty(Description) && Description.Length > 500)
        {
            errors.Add("Job description cannot exceed 500 characters");
        }

        if (SerialProfileId <= 0)
        {
            errors.Add("Valid serial profile must be selected");
        }

        if (SocatProfileId <= 0)
        {
            errors.Add("Valid socat profile must be selected");
        }

        if (PowerSupplyProfileId <= 0)
        {
            errors.Add("Valid power supply profile must be selected");
        }

        if (MemoryRegion == null)
        {
            errors.Add("Memory region configuration is required");
        }
        else if (MemoryRegion.Length == 0)
        {
            errors.Add("Memory region length must be greater than 0");
        }

        if (Payloads == null)
        {
            errors.Add("Payload configuration is required");
        }

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            errors.Add("Output path is required");
        }
        else if (OutputPath.Length > 500)
        {
            errors.Add("Output path cannot exceed 500 characters");
        }

        if (PowerOnTimeMs < 0)
        {
            errors.Add("Power on time cannot be negative");
        }

        if (PowerOffDelayMs < 0)
        {
            errors.Add("Power off delay cannot be negative");
        }

        return errors;
    }

    /// <summary>
    /// Converts this job profile to the execution Job record format.
    /// </summary>
    /// <returns>A Job record suitable for scheduler execution.</returns>
    public Job ToExecutionJob()
    {
        var serialRef = new SerialProfileRef("", 9600, "None", 8, "One"); // Will be populated from actual profile
        var socatRef = new SocatProfileRef(0, true); // Will be populated from actual profile
        var powerRef = new PowerProfileRef("", 0, 0, PowerOffDelayMs / 1000); // Will be populated from actual profile

        var profileSet = new JobProfileSet(
            serialRef,
            socatRef,
            powerRef,
            MemoryRegion,
            Payloads,
            OutputPath
        );

        var resources = GenerateResourceKeys();

        return new Job(
            Guid.NewGuid(),
            Name,
            resources,
            profileSet,
            JobState.Created,
            DateTimeOffset.UtcNow
        );
    }

    /// <summary>
    /// Generates resource keys for this job configuration.
    /// </summary>
    /// <returns>A list of resource keys required by this job.</returns>
    private IReadOnlyList<ResourceKey> GenerateResourceKeys()
    {
        var resources = new List<ResourceKey>
        {
            new("serial", SerialProfileId.ToString()),
            new("tcp", SocatProfileId.ToString()),
            new("power", PowerSupplyProfileId.ToString())
        };

        return resources;
    }

    #endregion

    #region IProfileBase Implementation

    /// <summary>
    /// Determines whether this job profile can be modified.
    /// </summary>
    /// <returns>True if the profile can be modified, false if it's read-only.</returns>
    public bool CanModify()
    {
        return !IsReadOnly;
    }

    /// <summary>
    /// Determines whether this job profile can be deleted.
    /// </summary>
    /// <returns>True if the profile can be deleted, false if it's read-only or default.</returns>
    public bool CanDelete()
    {
        return !IsReadOnly && !IsDefault;
    }

    /// <summary>
    /// Gets a summary of this job profile's key settings.
    /// </summary>
    /// <returns>A string summarizing the profile's configuration.</returns>
    public string GetSummary()
    {
        var summary = $"{Name}: Memory[{MemoryRegion?.Start:X}-{MemoryRegion?.Start + MemoryRegion?.Length:X}]";

        if (IsTemplate)
        {
            summary += " (Template)";
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
    /// Updates the modification timestamp to the current time.
    /// </summary>
    public void Touch()
    {
        ModifiedAt = DateTime.UtcNow;
    }

    #endregion
}
