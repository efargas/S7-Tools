using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Configuration.StrongSettings;

/// <summary>
/// Represents the root application settings, loaded from <c>appsettings.json</c>.
/// Provides strongly-typed configuration for all subsystems within S7Tools.
/// </summary>
public class AppSettings
{
    /// <summary>Gets or sets the logging configuration.</summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>Gets or sets the UI configuration.</summary>
    public UiSettings Ui { get; set; } = new();

    /// <summary>Gets or sets the directory path configuration.</summary>
    public PathSettings Paths { get; set; } = new();

    /// <summary>Gets or sets the profile persistence configuration.</summary>
    public ProfileSettings Profiles { get; set; } = new();

    /// <summary>Gets or sets the memory region profile configuration.</summary>
    public MemoryRegionSettings MemoryRegion { get; set; } = new();

    /// <summary>Gets or sets the log export configuration.</summary>
    public ExportSettings Export { get; set; } = new();

    /// <summary>Gets or sets the PLC connection configuration.</summary>
    public PlcSettings Plc { get; set; } = new();

    /// <summary>Gets or sets the job persistence configuration.</summary>
    public JobSettings Jobs { get; set; } = new();

    /// <summary>Gets or sets the task persistence configuration.</summary>
    public TaskSettings Tasks { get; set; } = new();

    /// <summary>Gets or sets the serial port configuration.</summary>
    public SerialSettings Serial { get; set; } = new();

    /// <summary>Gets or sets the network configuration.</summary>
    public NetworkSettings Network { get; set; } = new();

    /// <summary>Gets or sets the socat process configuration.</summary>
    public SocatSettings Socat { get; set; } = new();

    /// <summary>Gets or sets the power supply configuration.</summary>
    public PowerSupplySettings PowerSupply { get; set; } = new();

    /// <summary>Gets or sets the memory dump operation configuration.</summary>
    public MemoryDumpSettings MemoryDump { get; set; } = new();
}

/// <summary>
/// Configures the memory dump operation behaviour, including delays and the default output folder.
/// </summary>
public class MemoryDumpSettings
{
    /// <summary>Gets or sets the default output folder for memory dump files.</summary>
    public string DefaultFolder { get; set; } = "";

    /// <summary>
    /// Gets or sets the delay in milliseconds between individual segment dumps.
    /// </summary>
    /// <value>A value between 0 and 60000 ms. The default is 5000 ms.</value>
    [Range(0, 60000)]
    public int SegmentDumpDelayMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the delay in milliseconds between multi-dump iterations.
    /// </summary>
    /// <value>A value between 0 and 60000 ms. The default is 5000 ms.</value>
    [Range(0, 60000)]
    public int IterationDumpDelayMilliseconds { get; set; } = 5000;
}

/// <summary>
/// Configures the application logging subsystem, including level, file rotation, and output directories.
/// </summary>
public class LoggingSettings
{
    /// <summary>Gets or sets the minimum log level (e.g., <c>Information</c>, <c>Debug</c>, <c>Warning</c>).</summary>
    [Required]
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Gets or sets a value indicating whether log output should be written to files on disk.
    /// </summary>
    /// <value><see langword="true"/> if file logging is enabled; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum size in bytes of a single log file before it is rotated.
    /// </summary>
    /// <value>A value between 1 KB and 1 GB. The default is 10485760 (10 MB).</value>
    [Range(1024, 1073741824)]
    public long MaxFileSize { get; set; } = 10485760;

    /// <summary>
    /// Gets or sets the maximum number of rotated log files to retain on disk.
    /// </summary>
    /// <value>A value between 1 and 100. The default is 5.</value>
    [Range(1, 100)]
    public int MaxFiles { get; set; } = 5;

    /// <summary>Gets or sets the directory where application log files are written.</summary>
    [Required]
    public string LogDirectory { get; set; } = "Resources/Logs/Main";

    /// <summary>Gets or sets the directory where exported log files are saved.</summary>
    [Required]
    public string ExportDirectory { get; set; } = "Resources/Logs/Exported";
}

/// <summary>
/// Configures the application user-interface behaviour, including theme, window state, and log display options.
/// </summary>
public class UiSettings
{
    /// <summary>
    /// Gets or sets the UI theme name (e.g., <c>Light</c>, <c>Dark</c>, <c>System</c>).
    /// </summary>
    [Required]
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Gets or sets a value indicating whether the application window starts minimized.
    /// </summary>
    /// <value><see langword="true"/> to start minimized; otherwise, <see langword="false"/>.</value>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// Gets or sets the interval in milliseconds at which UI data is refreshed automatically.
    /// </summary>
    /// <value>A value between 100 and 60000 ms. The default is 2000 ms.</value>
    [Range(100, 60000)]
    public int AutoRefreshInterval { get; set; } = 2000;

    /// <summary>
    /// Gets or sets a value indicating whether the log view automatically scrolls to the latest entry.
    /// </summary>
    public bool AutoScrollLogs { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether timestamps are shown in log entries.</summary>
    public bool ShowTimestampInLogs { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the category column is shown in log entries.</summary>
    public bool ShowCategoryInLogs { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the log level column is shown in log entries.</summary>
    public bool ShowLogLevelInLogs { get; set; } = true;
}

/// <summary>
/// Configures the directory paths used by S7Tools for resources, profiles, logs, and output.
/// </summary>
public class PathSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether missing directories are created automatically at startup.
    /// </summary>
    public bool AutoCreateDirectories { get; set; } = true;

    /// <summary>Gets or sets the root resources directory path.</summary>
    [Required]
    public string ResourcesDirectory { get; set; } = "Resources";

    /// <summary>Gets or sets the directory path for stored profile files.</summary>
    [Required]
    public string ProfilesDirectory { get; set; } = "Resources/Profiles";

    /// <summary>Gets or sets the directory path for log files.</summary>
    [Required]
    public string LogsDirectory { get; set; } = "Resources/Logs";

    /// <summary>Gets or sets the directory path for job definition files.</summary>
    [Required]
    public string JobsDirectory { get; set; } = "Resources/Jobs";

    /// <summary>Gets or sets the directory path for task definition files.</summary>
    [Required]
    public string TasksDirectory { get; set; } = "Resources/Tasks";

    /// <summary>Gets or sets the directory path for payload binary files.</summary>
    [Required]
    public string PayloadsDirectory { get; set; } = "Resources/Payloads";

    /// <summary>Gets or sets the directory path for memory dump output files.</summary>
    [Required]
    public string DumpsDirectory { get; set; } = "Resources/Dumps";
}

/// <summary>
/// Configures profile file paths and auto-save behaviour for all profile types.
/// </summary>
public class ProfileSettings
{
    /// <summary>Gets or sets a value indicating whether profiles are saved automatically after each change.</summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether a backup is created before each save operation.</summary>
    public bool BackupOnSave { get; set; } = true;

    /// <summary>Gets or sets the file path for the serial port profiles JSON store.</summary>
    public string SerialPath { get; set; } = "Resources/Profiles/Serial/SerialProfiles.json";

    /// <summary>Gets or sets the file path for the socat profiles JSON store.</summary>
    public string SocatPath { get; set; } = "Resources/Profiles/Socat/SocatProfiles.json";

    /// <summary>Gets or sets the file path for the power supply profiles JSON store.</summary>
    public string PowerSupplyPath { get; set; } = "Resources/Profiles/PowerSupply/PowerSupplyProfiles.json";

    /// <summary>Gets or sets the file path for the memory region profiles JSON store.</summary>
    public string MemoryRegionPath { get; set; } = "Resources/Profiles/MemoryRegion/MemoryRegionProfiles.json";
}

/// <summary>
/// Configures the memory region profile management subsystem.
/// </summary>
public class MemoryRegionSettings
{
    /// <summary>
    /// Gets or sets the maximum number of memory region profiles that can be stored.
    /// </summary>
    /// <value>A value between 1 and 1000. The default is 100.</value>
    [Range(1, 1000)]
    public int MaxProfiles { get; set; } = 100;

    /// <summary>Gets or sets a value indicating whether the default memory region profile is loaded on startup.</summary>
    public bool AutoLoadDefaultProfile { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether profiles are saved automatically after each change.</summary>
    public bool AutoSaveProfiles { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether profile validation rules are enforced.</summary>
    public bool ValidationEnabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the <c>.bss</c> segment is pre-selected when a profile is loaded.</summary>
    public bool AutoSelectBssSegment { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of profiles shown in the selection dropdown.
    /// </summary>
    /// <value>A value between 1 and 500. The default is 50.</value>
    [Range(1, 500)]
    public int MaxProfilesInDropdown { get; set; } = 50;

    /// <summary>Gets or sets a value indicating whether template importing is enabled.</summary>
    public bool EnableTemplateImport { get; set; } = true;

    /// <summary>Gets or sets the comma-separated list of supported export formats (e.g., <c>JSON</c>).</summary>
    public string ExportFormats { get; set; } = "JSON";

    /// <summary>
    /// Gets or sets the maximum number of memory segments allowed per profile.
    /// </summary>
    /// <value>A value between 1 and 1000. The default is 50.</value>
    [Range(1, 1000)]
    public int MaxSegmentsPerProfile { get; set; } = 50;

    /// <summary>Gets or sets a value indicating whether overlapping segment detection is enabled.</summary>
    public bool EnableOverlapDetection { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether profile CRUD operations are logged.</summary>
    public bool LogProfileOperations { get; set; } = true;
}

/// <summary>
/// Configures log file export paths for different output formats.
/// </summary>
public class ExportSettings
{
    /// <summary>Gets or sets the default export format (e.g., <c>JSON</c>, <c>CSV</c>, <c>TXT</c>).</summary>
    [Required]
    public string DefaultFormat { get; set; } = "JSON";

    /// <summary>Gets or sets the output directory for CSV-formatted log exports.</summary>
    public string CsvDirectory { get; set; } = "Resources/Logs/Exported/CSV";

    /// <summary>Gets or sets the output directory for plain-text log exports.</summary>
    public string TxtDirectory { get; set; } = "Resources/Logs/Exported/TXT";

    /// <summary>Gets or sets the output directory for JSON-formatted log exports.</summary>
    public string JsonDirectory { get; set; } = "Resources/Logs/Exported/JSON";
}

/// <summary>
/// Configures the PLC connection parameters such as timeouts and retry behaviour.
/// </summary>
public class PlcSettings
{
    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    /// <value>A value between 100 and 60000 ms. The default is 5000 ms.</value>
    [Range(100, 60000)]
    public int ConnectionTimeout { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the read operation timeout in milliseconds.
    /// </summary>
    /// <value>A value between 100 and 60000 ms. The default is 2000 ms.</value>
    [Range(100, 60000)]
    public int ReadTimeout { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed PLC operations.
    /// </summary>
    /// <value>A value between 0 and 10. The default is 3.</value>
    [Range(0, 10)]
    public int RetryAttempts { get; set; } = 3;
}

/// <summary>
/// Configures persistence settings for job definitions.
/// </summary>
public class JobSettings
{
    /// <summary>Gets or sets the file path for the job profiles JSON store.</summary>
    public string ProfilesPath { get; set; } = "Resources/Jobs/Jobs.json";
}

/// <summary>
/// Configures persistence settings for task executions.
/// </summary>
public class TaskSettings
{
    /// <summary>Gets or sets the file path for the task definitions JSON store.</summary>
    public string ProfilesPath { get; set; } = "Resources/Tasks/Tasks.json";

    /// <summary>
    /// Gets or sets the interval in milliseconds at which task state is auto-saved.
    /// </summary>
    /// <value>A value between 1000 and 3600000 ms. The default is 10000 ms.</value>
    [Range(1000, 3600000)]
    public int AutoSaveInterval { get; set; } = 10000;
}

/// <summary>
/// Configures serial port discovery and default communication parameters.
/// </summary>
public class SerialSettings
{
    /// <summary>
    /// Gets or sets the default baud rate for new serial port connections.
    /// </summary>
    /// <value>A value between 300 and 4000000. The default is 9600.</value>
    [Range(300, 4000000)]
    public int DefaultBaudRate { get; set; } = 9600;

    /// <summary>
    /// Gets or sets the default number of data bits per character.
    /// </summary>
    /// <value>A value between 5 and 8. The default is 8.</value>
    [Range(5, 8)]
    public int DefaultDataBits { get; set; } = 8;

    /// <summary>Gets or sets the default parity mode (e.g., <c>None</c>, <c>Even</c>, <c>Odd</c>).</summary>
    public string DefaultParity { get; set; } = "None";

    /// <summary>Gets or sets the default stop bits setting (e.g., <c>One</c>, <c>Two</c>).</summary>
    public string DefaultStopBits { get; set; } = "One";

    /// <summary>Gets or sets a value indicating whether USB serial ports (<c>/dev/ttyUSB*</c>) are included during port discovery.</summary>
    public bool IncludeUsbPorts { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether ACM serial ports (<c>/dev/ttyACM*</c>) are included during port discovery.</summary>
    public bool IncludeAcmPorts { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether standard serial ports (<c>/dev/ttyS*</c>) are included during port discovery.</summary>
    public bool IncludeStandardPorts { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of serial ports to scan during discovery.
    /// </summary>
    /// <value>A value between 1 and 256. The default is 32.</value>
    [Range(1, 256)]
    public int MaxScanPorts { get; set; } = 32;

    /// <summary>
    /// Gets or sets the interval in seconds between automatic port discovery scans.
    /// </summary>
    /// <value>A value between 1 and 60 seconds. The default is 5.</value>
    [Range(1, 60)]
    public int ScanIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for testing a single port during discovery.
    /// </summary>
    /// <value>A value between 100 and 30000 ms. The default is 1000 ms.</value>
    [Range(100, 30000)]
    public int PortTestTimeoutMs { get; set; } = 1000;
}

/// <summary>
/// Configures network defaults for socat TCP bridging and Modbus power supply connectivity.
/// </summary>
public class NetworkSettings
{
    /// <summary>
    /// Gets or sets the default TCP port used by socat for serial-to-TCP bridging.
    /// </summary>
    /// <value>A value between 1 and 65535. The default is 2023.</value>
    [Range(1, 65535)]
    public int DefaultSocatPort { get; set; } = 2023;

    /// <summary>
    /// Gets or sets the default Modbus TCP port for power supply communication.
    /// </summary>
    /// <value>A value between 1 and 65535. The default is 502.</value>
    [Range(1, 65535)]
    public int PowerSupplyPort { get; set; } = 502;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed TCP connection attempts.
    /// </summary>
    /// <value>A value between 0 and 10. The default is 3.</value>
    [Range(0, 10)]
    public int ConnectionRetries { get; set; } = 3;
}

/// <summary>
/// Configures the socat process lifecycle management, including concurrency limits and output capture.
/// </summary>
public class SocatSettings
{
    /// <summary>
    /// Gets or sets the maximum number of socat instances that can run concurrently.
    /// </summary>
    /// <value>A value between 1 and 100. The default is 5.</value>
    [Range(1, 100)]
    public int MaxConcurrentInstances { get; set; } = 5;

    /// <summary>Gets or sets a value indicating whether the serial device is auto-configured when launching socat.</summary>
    public bool AutoConfigureSerialDevice { get; set; } = true;

    /// <summary>
    /// Gets or sets the graceful shutdown timeout in seconds when stopping a socat process.
    /// </summary>
    /// <value>A value between 1 and 60 seconds. The default is 5.</value>
    [Range(1, 60)]
    public int ProcessShutdownTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the interval in seconds at which socat process status is refreshed.
    /// </summary>
    /// <value>A value between 1 and 60 seconds. The default is 2.</value>
    [Range(1, 60)]
    public int StatusRefreshIntervalSeconds { get; set; } = 2;

    /// <summary>Gets or sets a value indicating whether stdout/stderr output from socat is captured and logged.</summary>
    public bool CaptureProcessOutput { get; set; } = true;
}

/// <summary>
/// Configures the power supply subsystem, including connection handling, power state notifications, and logging.
/// </summary>
public class PowerSupplySettings
{
    /// <summary>
    /// Gets or sets the maximum number of power supply profiles that can be stored.
    /// </summary>
    /// <value>A value between 1 and 1000. The default is 100.</value>
    [Range(1, 1000)]
    public int MaxProfiles { get; set; } = 100;

    /// <summary>Gets or sets a value indicating whether the default power supply profile is loaded on startup.</summary>
    public bool AutoLoadDefaultProfile { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether profiles are saved automatically after each change.</summary>
    public bool AutoSaveProfiles { get; set; } = true;

    /// <summary>
    /// Gets or sets the Modbus TCP connection timeout in milliseconds.
    /// </summary>
    /// <value>A value between 100 and 60000 ms. The default is 5000 ms.</value>
    [Range(100, 60000)]
    public int DefaultConnectionTimeoutMs { get; set; } = 5000;

    /// <summary>Gets or sets a value indicating whether connection pooling is enabled for Modbus TCP sessions.</summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether automatic reconnection is attempted on connection loss.</summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay in milliseconds between automatic reconnection attempts.
    /// </summary>
    /// <value>A value between 100 and 60000 ms. The default is 2000 ms.</value>
    [Range(100, 60000)]
    public int ReconnectDelayMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum number of automatic reconnection attempts before giving up.
    /// </summary>
    /// <value>A value between 0 and 20. The default is 5.</value>
    [Range(0, 20)]
    public int MaxReconnectAttempts { get; set; } = 5;

    /// <summary>Gets or sets a value indicating whether a confirmation prompt is displayed before powering off the supply.</summary>
    public bool ConfirmPowerOff { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether a confirmation prompt is displayed before powering on the supply.</summary>
    public bool ConfirmPowerOn { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds after a power state change before the next operation proceeds.
    /// </summary>
    /// <value>A value between 100 and 10000 ms. The default is 1000 ms.</value>
    [Range(100, 10000)]
    public int PowerStateChangeDelayMs { get; set; } = 1000;

    /// <summary>Gets or sets a value indicating whether the power state is read automatically after connecting.</summary>
    public bool AutoReadStateAfterConnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in milliseconds between automatic power supply status refresh polls.
    /// </summary>
    /// <value>A value between 100 and 60000 ms. The default is 5000 ms.</value>
    [Range(100, 60000)]
    public int StatusRefreshIntervalMs { get; set; } = 5000;

    /// <summary>Gets or sets a value indicating whether UI notifications are shown on power state changes.</summary>
    public bool ShowPowerStateNotifications { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether UI notifications are shown on connection state changes.</summary>
    public bool ShowConnectionNotifications { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether individual Modbus TCP operations are logged at Debug level.</summary>
    public bool LogModbusOperations { get; set; }

    /// <summary>Gets or sets a value indicating whether connection state transitions are logged.</summary>
    public bool LogConnectionStateChanges { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether power state transitions are logged.</summary>
    public bool LogPowerStateChanges { get; set; } = true;
}
