using System;
using System.IO;

namespace S7Tools.Core.Models;

/// <summary>
/// Settings for power supply profile management and device operations.
/// </summary>
/// <remarks>
/// This class contains configuration for power supply profile storage, connection management,
/// and operational parameters. These settings are persisted as part of the application configuration.
/// </remarks>
public class PowerSupplySettings
{
    #region Profile Management Settings

    /// <summary>
    /// Gets or sets the path where power supply profiles are stored.
    /// </summary>
    /// <value>The profiles directory path. Default is "resources/PowerSupplyProfiles" relative to application directory.</value>
    /// <remarks>
    /// Profiles are stored as JSON files in this directory.
    /// The directory is created automatically if it doesn't exist.
    /// </remarks>
    public string ProfilesPath { get; set; } = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "resources",
        "PowerSupplyProfiles");

    /// <summary>
    /// Gets or sets the maximum number of profiles that can be stored.
    /// </summary>
    /// <value>The maximum profile count. Default is 100.</value>
    /// <remarks>
    /// This limit prevents excessive profile creation and helps maintain performance.
    /// Users should export and back up profiles if they need to manage more than this limit.
    /// </remarks>
    public int MaxProfiles { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to automatically load the default profile on application startup.
    /// </summary>
    /// <value>True to auto-load default profile, false otherwise. Default is true.</value>
    public bool AutoLoadDefaultProfile { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically save profile changes.
    /// </summary>
    /// <value>True to auto-save changes, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, profile changes are immediately persisted to disk.
    /// When disabled, changes are only saved when explicitly requested.
    /// </remarks>
    public bool AutoSaveProfiles { get; set; } = true;

    #endregion

    #region Connection Management Settings

    /// <summary>
    /// Gets or sets the default connection timeout for all operations in milliseconds.
    /// </summary>
    /// <value>The default connection timeout. Default is 5000ms (5 seconds).</value>
    /// <remarks>
    /// This value is used as the default for new profiles.
    /// Individual profiles can override this setting.
    /// </remarks>
    public int DefaultConnectionTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to enable connection pooling.
    /// </summary>
    /// <value>True to enable connection pooling, false otherwise. Default is true.</value>
    /// <remarks>
    /// Connection pooling reuses TCP connections for better performance.
    /// Disable this if you experience connection stability issues.
    /// </remarks>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically reconnect on connection loss.
    /// </summary>
    /// <value>True to enable auto-reconnect, false otherwise. Default is true.</value>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay between reconnection attempts in milliseconds.
    /// </summary>
    /// <value>The reconnection delay. Default is 2000ms (2 seconds).</value>
    public int ReconnectDelayMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum number of reconnection attempts.
    /// </summary>
    /// <value>The maximum reconnect attempts. Default is 5. Set to 0 for unlimited attempts.</value>
    public int MaxReconnectAttempts { get; set; } = 5;

    #endregion

    #region Power Control Settings

    /// <summary>
    /// Gets or sets whether to require confirmation before turning power off.
    /// </summary>
    /// <value>True to require confirmation, false otherwise. Default is true.</value>
    /// <remarks>
    /// When enabled, users must confirm before turning power off to prevent accidental shutdowns.
    /// </remarks>
    public bool ConfirmPowerOff { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require confirmation before turning power on.
    /// </summary>
    /// <value>True to require confirmation, false otherwise. Default is false.</value>
    public bool ConfirmPowerOn { get; set; } = false;

    /// <summary>
    /// Gets or sets the delay after power state changes in milliseconds.
    /// </summary>
    /// <value>The post-operation delay. Default is 1000ms (1 second).</value>
    /// <remarks>
    /// This delay allows the power supply to stabilize before reading the state again.
    /// Some power supplies need time to update their status after state changes.
    /// </remarks>
    public int PowerStateChangeDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to automatically read power state after connection.
    /// </summary>
    /// <value>True to auto-read state, false otherwise. Default is true.</value>
    public bool AutoReadStateAfterConnect { get; set; } = true;

    #endregion

    #region UI Settings

    /// <summary>
    /// Gets or sets the status refresh interval in milliseconds.
    /// </summary>
    /// <value>The refresh interval. Default is 5000ms (5 seconds). Set to 0 to disable auto-refresh.</value>
    /// <remarks>
    /// When connected, the UI will automatically refresh the power state at this interval.
    /// </remarks>
    public int StatusRefreshIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to show notifications for power state changes.
    /// </summary>
    /// <value>True to show notifications, false otherwise. Default is true.</value>
    public bool ShowPowerStateNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show notifications for connection state changes.
    /// </summary>
    /// <value>True to show notifications, false otherwise. Default is true.</value>
    public bool ShowConnectionNotifications { get; set; } = true;

    #endregion

    #region Logging Settings

    /// <summary>
    /// Gets or sets whether to log all Modbus operations.
    /// </summary>
    /// <value>True to enable operation logging, false otherwise. Default is false.</value>
    /// <remarks>
    /// When enabled, all Modbus read/write operations are logged for debugging purposes.
    /// This can generate significant log volume in high-traffic scenarios.
    /// </remarks>
    public bool LogModbusOperations { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log connection state changes.
    /// </summary>
    /// <value>True to log connection changes, false otherwise. Default is true.</value>
    public bool LogConnectionStateChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log power state changes.
    /// </summary>
    /// <value>True to log power changes, false otherwise. Default is true.</value>
    public bool LogPowerStateChanges { get; set; } = true;

    #endregion

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    /// <returns>A new PowerSupplySettings instance with the same values.</returns>
    public PowerSupplySettings Clone()
    {
        return new PowerSupplySettings
        {
            ProfilesPath = ProfilesPath,
            MaxProfiles = MaxProfiles,
            AutoLoadDefaultProfile = AutoLoadDefaultProfile,
            AutoSaveProfiles = AutoSaveProfiles,
            DefaultConnectionTimeoutMs = DefaultConnectionTimeoutMs,
            EnableConnectionPooling = EnableConnectionPooling,
            EnableAutoReconnect = EnableAutoReconnect,
            ReconnectDelayMs = ReconnectDelayMs,
            MaxReconnectAttempts = MaxReconnectAttempts,
            ConfirmPowerOff = ConfirmPowerOff,
            ConfirmPowerOn = ConfirmPowerOn,
            PowerStateChangeDelayMs = PowerStateChangeDelayMs,
            AutoReadStateAfterConnect = AutoReadStateAfterConnect,
            StatusRefreshIntervalMs = StatusRefreshIntervalMs,
            ShowPowerStateNotifications = ShowPowerStateNotifications,
            ShowConnectionNotifications = ShowConnectionNotifications,
            LogModbusOperations = LogModbusOperations,
            LogConnectionStateChanges = LogConnectionStateChanges,
            LogPowerStateChanges = LogPowerStateChanges
        };
    }
}
