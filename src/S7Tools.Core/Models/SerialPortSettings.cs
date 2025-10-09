using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents the settings for serial port management within the S7Tools application.
/// This class contains configuration options for profile storage, port scanning, and default behaviors.
/// </summary>
public class SerialPortSettings
{
    #region Profile Management

    /// <summary>
    /// Gets or sets the path where serial port profiles are stored.
    /// </summary>
    /// <value>The directory path for profile storage. Default is "Resources/SerialProfiles".</value>
    /// <remarks>
    /// This path is relative to the application's base directory. The directory will be created
    /// automatically if it doesn't exist when profiles are first saved.
    /// </remarks>
    [Required(ErrorMessage = "Profiles path is required")]
    [StringLength(260, ErrorMessage = "Profiles path cannot exceed 260 characters")]
    public string ProfilesPath { get; set; } = Path.Combine("resources", "SerialProfiles");

    /// <summary>
    /// Gets or sets the ID of the default profile to use for new connections.
    /// </summary>
    /// <value>The ID of the default profile. Default is 1 (S7Tools default profile).</value>
    /// <remarks>
    /// When set to 0 or a non-existent profile ID, the system will automatically
    /// select the first available profile marked as default.
    /// </remarks>
    [Range(0, int.MaxValue, ErrorMessage = "Default profile ID must be non-negative")]
    public int DefaultProfileId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of profiles that can be stored.
    /// </summary>
    /// <value>The maximum number of profiles. Default is 100.</value>
    /// <remarks>
    /// This limit helps prevent excessive storage usage and maintains reasonable
    /// performance when loading and managing profiles.
    /// </remarks>
    [Range(1, 1000, ErrorMessage = "Maximum profiles must be between 1 and 1000")]
    public int MaxProfiles { get; set; } = 100;

    #endregion

    #region Port Scanning

    /// <summary>
    /// Gets or sets a value indicating whether to automatically scan for available serial ports.
    /// </summary>
    /// <value>True to enable automatic port scanning, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, the application will periodically scan for new or removed serial ports
    /// and update the available ports list accordingly.
    /// </remarks>
    public bool AutoScanPorts { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in seconds between automatic port scans.
    /// </summary>
    /// <value>The scan interval in seconds. Default is 5 seconds.</value>
    /// <remarks>
    /// This setting only applies when AutoScanPorts is enabled. Shorter intervals provide
    /// more responsive port detection but may impact performance.
    /// </remarks>
    [Range(1, 300, ErrorMessage = "Scan interval must be between 1 and 300 seconds")]
    public int ScanIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to include USB serial ports in scans.
    /// </summary>
    /// <value>True to include USB serial ports (/dev/ttyUSB*), false otherwise. Default is true.</value>
    public bool IncludeUsbPorts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include ACM serial ports in scans.
    /// </summary>
    /// <value>True to include ACM serial ports (/dev/ttyACM*), false otherwise. Default is true.</value>
    /// <remarks>
    /// ACM (Abstract Control Model) ports are commonly used by Arduino and similar devices.
    /// </remarks>
    public bool IncludeAcmPorts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include standard serial ports in scans.
    /// </summary>
    /// <value>True to include standard serial ports (/dev/ttyS*), false otherwise. Default is true.</value>
    /// <remarks>
    /// Standard serial ports are typically built-in RS-232 ports on the system.
    /// </remarks>
    public bool IncludeStandardPorts { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of ports to scan.
    /// </summary>
    /// <value>The maximum number of ports to scan for each type. Default is 32.</value>
    /// <remarks>
    /// This limit prevents excessive scanning time when many ports are present.
    /// For example, it will scan /dev/ttyUSB0 through /dev/ttyUSB31.
    /// </remarks>
    [Range(1, 256, ErrorMessage = "Maximum scan ports must be between 1 and 256")]
    public int MaxScanPorts { get; set; } = 32;

    #endregion

    #region Port Configuration

    /// <summary>
    /// Gets or sets a value indicating whether to test port accessibility during scanning.
    /// </summary>
    /// <value>True to test port accessibility, false to only check existence. Default is true.</value>
    /// <remarks>
    /// When enabled, the scanner will attempt to open each port briefly to verify it's accessible.
    /// This provides more accurate availability information but may take longer.
    /// </remarks>
    public bool TestPortAccessibility { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for port accessibility tests.
    /// </summary>
    /// <value>The timeout for port tests in milliseconds. Default is 1000ms (1 second).</value>
    [Range(100, 10000, ErrorMessage = "Port test timeout must be between 100 and 10000 milliseconds")]
    public int PortTestTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to backup port configuration before applying changes.
    /// </summary>
    /// <value>True to backup configuration before changes, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, the current port configuration will be saved before applying a new profile,
    /// allowing for easy restoration if needed.
    /// </remarks>
    public bool BackupConfigurationBeforeApply { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate stty commands before execution.
    /// </summary>
    /// <value>True to validate commands before execution, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, stty commands will be validated for syntax and safety before execution.
    /// This helps prevent system issues from malformed commands.
    /// </remarks>
    public bool ValidateSttyCommands { get; set; } = true;

    #endregion

    #region User Interface

    /// <summary>
    /// Gets or sets a value indicating whether to show detailed port information in the UI.
    /// </summary>
    /// <value>True to show detailed information, false for basic info only. Default is true.</value>
    /// <remarks>
    /// Detailed information includes vendor ID, product ID, serial number, and other
    /// hardware-specific details when available.
    /// </remarks>
    public bool ShowDetailedPortInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the generated stty command preview.
    /// </summary>
    /// <value>True to show command preview, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, the UI will display the exact stty command that will be executed
    /// when applying a profile to a port.
    /// </remarks>
    public bool ShowSttyCommandPreview { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to confirm before applying profiles to ports.
    /// </summary>
    /// <value>True to show confirmation dialog, false to apply immediately. Default is true.</value>
    /// <remarks>
    /// Confirmation dialogs help prevent accidental configuration changes that could
    /// disrupt existing connections or system behavior.
    /// </remarks>
    public bool ConfirmBeforeApply { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show notifications for port changes.
    /// </summary>
    /// <value>True to show notifications, false otherwise. Default is false.</value>
    /// <remarks>
    /// When enabled, the application will show notifications when ports are added,
    /// removed, or change status.
    /// </remarks>
    public bool ShowPortChangeNotifications { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// Gets or sets the version of this settings format.
    /// </summary>
    /// <value>The settings format version. Default is "1.0".</value>
    [StringLength(10, ErrorMessage = "Version cannot exceed 10 characters")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the timestamp when these settings were last modified.
    /// </summary>
    /// <value>The last modification timestamp in UTC.</value>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates default serial port settings optimized for S7Tools usage.
    /// </summary>
    /// <returns>A SerialPortSettings instance with default values.</returns>
    public static SerialPortSettings CreateDefault()
    {
        return new SerialPortSettings
        {
            ProfilesPath = Path.Combine("resources", "SerialProfiles"),
            DefaultProfileId = 1, // S7Tools default profile
            MaxProfiles = 100,
            AutoScanPorts = true,
            ScanIntervalSeconds = 5,
            IncludeUsbPorts = true,
            IncludeAcmPorts = true,
            IncludeStandardPorts = true,
            MaxScanPorts = 32,
            TestPortAccessibility = true,
            PortTestTimeoutMs = 1000,
            BackupConfigurationBeforeApply = true,
            ValidateSttyCommands = true,
            ShowDetailedPortInfo = true,
            ShowSttyCommandPreview = true,
            ConfirmBeforeApply = true,
            ShowPortChangeNotifications = false,
            Version = "1.0",
            LastModified = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates settings optimized for development and testing.
    /// </summary>
    /// <returns>A SerialPortSettings instance with development-friendly values.</returns>
    public static SerialPortSettings CreateDevelopmentSettings()
    {
        var settings = CreateDefault();
        settings.ScanIntervalSeconds = 2; // Faster scanning for development
        settings.PortTestTimeoutMs = 500; // Shorter timeout for faster testing
        settings.ShowPortChangeNotifications = true; // Enable notifications for debugging
        settings.ConfirmBeforeApply = false; // Skip confirmations for faster testing
        return settings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a deep copy of these settings.
    /// </summary>
    /// <returns>A new SerialPortSettings instance with identical values.</returns>
    public SerialPortSettings Clone()
    {
        return new SerialPortSettings
        {
            ProfilesPath = ProfilesPath,
            DefaultProfileId = DefaultProfileId,
            MaxProfiles = MaxProfiles,
            AutoScanPorts = AutoScanPorts,
            ScanIntervalSeconds = ScanIntervalSeconds,
            IncludeUsbPorts = IncludeUsbPorts,
            IncludeAcmPorts = IncludeAcmPorts,
            IncludeStandardPorts = IncludeStandardPorts,
            MaxScanPorts = MaxScanPorts,
            TestPortAccessibility = TestPortAccessibility,
            PortTestTimeoutMs = PortTestTimeoutMs,
            BackupConfigurationBeforeApply = BackupConfigurationBeforeApply,
            ValidateSttyCommands = ValidateSttyCommands,
            ShowDetailedPortInfo = ShowDetailedPortInfo,
            ShowSttyCommandPreview = ShowSttyCommandPreview,
            ConfirmBeforeApply = ConfirmBeforeApply,
            ShowPortChangeNotifications = ShowPortChangeNotifications,
            Version = Version,
            LastModified = DateTime.UtcNow // Update modification time for clone
        };
    }

    /// <summary>
    /// Validates these settings and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ProfilesPath))
        {
            errors.Add("Profiles path is required");
        }
        else if (ProfilesPath.Length > 260)
        {
            errors.Add("Profiles path cannot exceed 260 characters");
        }

        if (DefaultProfileId < 0)
        {
            errors.Add("Default profile ID must be non-negative");
        }

        if (MaxProfiles < 1 || MaxProfiles > 1000)
        {
            errors.Add("Maximum profiles must be between 1 and 1000");
        }

        if (ScanIntervalSeconds < 1 || ScanIntervalSeconds > 300)
        {
            errors.Add("Scan interval must be between 1 and 300 seconds");
        }

        if (MaxScanPorts < 1 || MaxScanPorts > 256)
        {
            errors.Add("Maximum scan ports must be between 1 and 256");
        }

        if (PortTestTimeoutMs < 100 || PortTestTimeoutMs > 10000)
        {
            errors.Add("Port test timeout must be between 100 and 10000 milliseconds");
        }

        if (!string.IsNullOrEmpty(Version) && Version.Length > 10)
        {
            errors.Add("Version cannot exceed 10 characters");
        }

        return errors;
    }

    /// <summary>
    /// Updates the last modified timestamp to the current time.
    /// </summary>
    public void Touch()
    {
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the absolute path for profile storage.
    /// </summary>
    /// <param name="baseDirectory">The base directory to resolve relative paths against.</param>
    /// <returns>The absolute path where profiles should be stored.</returns>
    public string GetAbsoluteProfilesPath(string baseDirectory)
    {
        if (Path.IsPathRooted(ProfilesPath))
        {
            return ProfilesPath;
        }

        return Path.Combine(baseDirectory, ProfilesPath);
    }

    /// <summary>
    /// Returns a string representation of these settings.
    /// </summary>
    /// <returns>A string describing the key settings.</returns>
    public override string ToString()
    {
        return $"SerialPortSettings: {MaxProfiles} max profiles, scan every {ScanIntervalSeconds}s, path: {ProfilesPath}";
    }

    #endregion
}
