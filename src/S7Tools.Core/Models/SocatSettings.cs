using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents the settings for socat (Serial-to-TCP Proxy) management within the S7Tools application.
/// This class contains configuration options for profile storage, connection management, and default behaviors.
/// </summary>
public class SocatSettings
{
    #region Profile Management

    /// <summary>
    /// Gets or sets the path where socat profiles are stored.
    /// </summary>
    /// <value>The directory path for profile storage. Default is "resources/SocatProfiles".</value>
    /// <remarks>
    /// This path is relative to the application's base directory. The directory will be created
    /// automatically if it doesn't exist when profiles are first saved.
    /// </remarks>
    [Required(ErrorMessage = "Profiles path is required")]
    [StringLength(260, ErrorMessage = "Profiles path cannot exceed 260 characters")]
    public string ProfilesPath { get; set; } = Path.Combine("resources", "SocatProfiles");

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
    /// <value>The maximum number of profiles. Default is 50.</value>
    /// <remarks>
    /// This limit helps prevent excessive storage usage and maintains reasonable
    /// performance when loading and managing profiles.
    /// </remarks>
    [Range(1, 500, ErrorMessage = "Maximum profiles must be between 1 and 500")]
    public int MaxProfiles { get; set; } = 50;

    #endregion

    #region Connection Management

    /// <summary>
    /// Gets or sets a value indicating whether to automatically start socat when a profile is applied.
    /// </summary>
    /// <value>True to automatically start socat, false to require manual start. Default is false.</value>
    /// <remarks>
    /// When enabled, applying a profile to a serial device will automatically start the socat process
    /// with the configured settings.
    /// </remarks>
    public bool AutoStartOnApply { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically restart socat if it terminates unexpectedly.
    /// </summary>
    /// <value>True to enable automatic restart, false otherwise. Default is false.</value>
    /// <remarks>
    /// When enabled, the application will monitor socat processes and restart them if they
    /// terminate unexpectedly due to errors or external factors.
    /// </remarks>
    public bool AutoRestartOnFailure { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of restart attempts before giving up.
    /// </summary>
    /// <value>The maximum number of restart attempts. Default is 3.</value>
    /// <remarks>
    /// This prevents infinite restart loops in case of persistent configuration issues.
    /// After reaching the maximum attempts, manual intervention is required.
    /// </remarks>
    [Range(1, 10, ErrorMessage = "Maximum restart attempts must be between 1 and 10")]
    public int MaxRestartAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in seconds between restart attempts.
    /// </summary>
    /// <value>The restart delay in seconds. Default is 5 seconds.</value>
    /// <remarks>
    /// This delay helps prevent rapid restart cycles and allows time for transient
    /// issues to resolve.
    /// </remarks>
    [Range(1, 60, ErrorMessage = "Restart delay must be between 1 and 60 seconds")]
    public int RestartDelaySeconds { get; set; } = 5;

    #endregion

    #region Process Management

    /// <summary>
    /// Gets or sets the timeout in seconds for socat process startup.
    /// </summary>
    /// <value>The startup timeout in seconds. Default is 10 seconds.</value>
    /// <remarks>
    /// If socat doesn't start successfully within this timeout, it will be considered
    /// a startup failure and appropriate error handling will be triggered.
    /// </remarks>
    [Range(1, 120, ErrorMessage = "Startup timeout must be between 1 and 120 seconds")]
    public int ProcessStartupTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the timeout in seconds for socat process shutdown.
    /// </summary>
    /// <value>The shutdown timeout in seconds. Default is 5 seconds.</value>
    /// <remarks>
    /// If socat doesn't shut down gracefully within this timeout, it will be
    /// forcefully terminated.
    /// </remarks>
    [Range(1, 60, ErrorMessage = "Shutdown timeout must be between 1 and 60 seconds")]
    public int ProcessShutdownTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to capture socat output for logging.
    /// </summary>
    /// <value>True to capture output, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, socat's stdout and stderr will be captured and logged through
    /// the application's logging system for debugging and monitoring purposes.
    /// </remarks>
    public bool CaptureProcessOutput { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of socat instances that can run simultaneously.
    /// </summary>
    /// <value>The maximum number of concurrent instances. Default is 5.</value>
    /// <remarks>
    /// This limit prevents resource exhaustion and helps maintain system stability
    /// when multiple serial devices are being bridged to TCP.
    /// </remarks>
    [Range(1, 20, ErrorMessage = "Maximum instances must be between 1 and 20")]
    public int MaxConcurrentInstances { get; set; } = 5;

    #endregion

    #region Serial Device Integration

    /// <summary>
    /// Gets or sets a value indicating whether to automatically configure the serial device with stty before starting socat.
    /// </summary>
    /// <value>True to run stty configuration, false to skip. Default is true.</value>
    /// <remarks>
    /// When enabled, the serial device will be configured using the appropriate stty
    /// command based on the selected serial port profile before starting socat.
    /// </remarks>
    public bool AutoConfigureSerialDevice { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate serial device accessibility before starting socat.
    /// </summary>
    /// <value>True to validate device access, false to skip validation. Default is true.</value>
    /// <remarks>
    /// When enabled, the application will check if the serial device exists and is
    /// accessible before attempting to start socat.
    /// </remarks>
    public bool ValidateSerialDeviceAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for serial device validation.
    /// </summary>
    /// <value>The validation timeout in milliseconds. Default is 2000ms (2 seconds).</value>
    [Range(100, 10000, ErrorMessage = "Device validation timeout must be between 100 and 10000 milliseconds")]
    public int DeviceValidationTimeoutMs { get; set; } = 2000;

    #endregion

    #region User Interface

    /// <summary>
    /// Gets or sets a value indicating whether to show detailed connection information in the UI.
    /// </summary>
    /// <value>True to show detailed information, false for basic info only. Default is true.</value>
    /// <remarks>
    /// Detailed information includes process ID, TCP port status, data transfer statistics,
    /// and connection timestamps.
    /// </remarks>
    public bool ShowDetailedConnectionInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the generated socat command preview.
    /// </summary>
    /// <value>True to show command preview, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, the UI will display the exact socat command that will be executed
    /// when starting a connection with the selected profile.
    /// </remarks>
    public bool ShowSocatCommandPreview { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to confirm before starting socat processes.
    /// </summary>
    /// <value>True to show confirmation dialog, false to start immediately. Default is true.</value>
    /// <remarks>
    /// Confirmation dialogs help prevent accidental process starts that could
    /// conflict with existing connections or system services.
    /// </remarks>
    public bool ConfirmBeforeStart { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show notifications for connection status changes.
    /// </summary>
    /// <value>True to show notifications, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, the application will show notifications when socat processes
    /// start, stop, or encounter errors.
    /// </remarks>
    public bool ShowConnectionNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets the refresh interval in seconds for connection status updates.
    /// </summary>
    /// <value>The refresh interval in seconds. Default is 2 seconds.</value>
    /// <remarks>
    /// This controls how frequently the UI updates connection status, process information,
    /// and data transfer statistics.
    /// </remarks>
    [Range(1, 30, ErrorMessage = "Refresh interval must be between 1 and 30 seconds")]
    public int StatusRefreshIntervalSeconds { get; set; } = 2;

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
    /// Creates default socat settings optimized for S7Tools usage.
    /// </summary>
    /// <returns>A SocatSettings instance with default values.</returns>
    public static SocatSettings CreateDefault()
    {
        return new SocatSettings
        {
            ProfilesPath = Path.Combine("resources", "SocatProfiles"),
            DefaultProfileId = 1, // S7Tools default profile
            MaxProfiles = 50,
            AutoStartOnApply = false,
            AutoRestartOnFailure = false,
            MaxRestartAttempts = 3,
            RestartDelaySeconds = 5,
            ProcessStartupTimeoutSeconds = 10,
            ProcessShutdownTimeoutSeconds = 5,
            CaptureProcessOutput = true,
            MaxConcurrentInstances = 5,
            AutoConfigureSerialDevice = true,
            ValidateSerialDeviceAccess = true,
            DeviceValidationTimeoutMs = 2000,
            ShowDetailedConnectionInfo = true,
            ShowSocatCommandPreview = true,
            ConfirmBeforeStart = true,
            ShowConnectionNotifications = true,
            StatusRefreshIntervalSeconds = 2,
            Version = "1.0",
            LastModified = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates settings optimized for development and testing.
    /// </summary>
    /// <returns>A SocatSettings instance with development-friendly values.</returns>
    public static SocatSettings CreateDevelopmentSettings()
    {
        var settings = CreateDefault();
        settings.AutoStartOnApply = true; // Auto-start for faster testing
        settings.ConfirmBeforeStart = false; // Skip confirmations for faster testing
        settings.ProcessStartupTimeoutSeconds = 5; // Shorter timeout for development
        settings.DeviceValidationTimeoutMs = 1000; // Faster validation
        settings.StatusRefreshIntervalSeconds = 1; // More frequent updates for debugging
        return settings;
    }

    /// <summary>
    /// Creates settings optimized for production environments.
    /// </summary>
    /// <returns>A SocatSettings instance with production-ready values.</returns>
    public static SocatSettings CreateProductionSettings()
    {
        var settings = CreateDefault();
        settings.AutoRestartOnFailure = true; // Enable auto-restart in production
        settings.MaxRestartAttempts = 5; // More restart attempts
        settings.RestartDelaySeconds = 10; // Longer delay between restarts
        settings.ShowConnectionNotifications = false; // Reduce UI noise in production
        return settings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a deep copy of these settings.
    /// </summary>
    /// <returns>A new SocatSettings instance with identical values.</returns>
    public SocatSettings Clone()
    {
        return new SocatSettings
        {
            ProfilesPath = ProfilesPath,
            DefaultProfileId = DefaultProfileId,
            MaxProfiles = MaxProfiles,
            AutoStartOnApply = AutoStartOnApply,
            AutoRestartOnFailure = AutoRestartOnFailure,
            MaxRestartAttempts = MaxRestartAttempts,
            RestartDelaySeconds = RestartDelaySeconds,
            ProcessStartupTimeoutSeconds = ProcessStartupTimeoutSeconds,
            ProcessShutdownTimeoutSeconds = ProcessShutdownTimeoutSeconds,
            CaptureProcessOutput = CaptureProcessOutput,
            MaxConcurrentInstances = MaxConcurrentInstances,
            AutoConfigureSerialDevice = AutoConfigureSerialDevice,
            ValidateSerialDeviceAccess = ValidateSerialDeviceAccess,
            DeviceValidationTimeoutMs = DeviceValidationTimeoutMs,
            ShowDetailedConnectionInfo = ShowDetailedConnectionInfo,
            ShowSocatCommandPreview = ShowSocatCommandPreview,
            ConfirmBeforeStart = ConfirmBeforeStart,
            ShowConnectionNotifications = ShowConnectionNotifications,
            StatusRefreshIntervalSeconds = StatusRefreshIntervalSeconds,
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

        if (MaxProfiles < 1 || MaxProfiles > 500)
        {
            errors.Add("Maximum profiles must be between 1 and 500");
        }

        if (MaxRestartAttempts < 1 || MaxRestartAttempts > 10)
        {
            errors.Add("Maximum restart attempts must be between 1 and 10");
        }

        if (RestartDelaySeconds < 1 || RestartDelaySeconds > 60)
        {
            errors.Add("Restart delay must be between 1 and 60 seconds");
        }

        if (ProcessStartupTimeoutSeconds < 1 || ProcessStartupTimeoutSeconds > 120)
        {
            errors.Add("Startup timeout must be between 1 and 120 seconds");
        }

        if (ProcessShutdownTimeoutSeconds < 1 || ProcessShutdownTimeoutSeconds > 60)
        {
            errors.Add("Shutdown timeout must be between 1 and 60 seconds");
        }

        if (MaxConcurrentInstances < 1 || MaxConcurrentInstances > 20)
        {
            errors.Add("Maximum instances must be between 1 and 20");
        }

        if (DeviceValidationTimeoutMs < 100 || DeviceValidationTimeoutMs > 10000)
        {
            errors.Add("Device validation timeout must be between 100 and 10000 milliseconds");
        }

        if (StatusRefreshIntervalSeconds < 1 || StatusRefreshIntervalSeconds > 30)
        {
            errors.Add("Refresh interval must be between 1 and 30 seconds");
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
        return $"SocatSettings: {MaxProfiles} max profiles, {MaxConcurrentInstances} max instances, path: {ProfilesPath}";
    }

    #endregion
}
