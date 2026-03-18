#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Configuration.StrongSettings
{
    public class AppSettings
    {
        public LoggingSettings Logging { get; set; } = new();
        public UiSettings Ui { get; set; } = new();
        public PathSettings Paths { get; set; } = new();
        public ProfileSettings Profiles { get; set; } = new();
        public MemoryRegionSettings MemoryRegion { get; set; } = new();
        public ExportSettings Export { get; set; } = new();
        public PlcSettings Plc { get; set; } = new();
        public JobSettings Jobs { get; set; } = new();
        public TaskSettings Tasks { get; set; } = new();
        public SerialSettings Serial { get; set; } = new();
        public NetworkSettings Network { get; set; } = new();
        public SocatSettings Socat { get; set; } = new();
        public PowerSupplySettings PowerSupply { get; set; } = new();
        public MemoryDumpSettings MemoryDump { get; set; } = new();
    }

    public class MemoryDumpSettings
    {
        public string DefaultFolder { get; set; } = "";
    }

    public class LoggingSettings
    {
        [Required]
        public string Level { get; set; } = "Information";
        public bool EnableFileLogging { get; set; } = true;
        [Range(1024, 1073741824)]
        public long MaxFileSize { get; set; } = 10485760; // 10MB default
        [Range(1, 100)]
        public int MaxFiles { get; set; } = 5;
        [Required]
        public string LogDirectory { get; set; } = "Resources/Logs/Main";
        [Required]
        public string ExportDirectory { get; set; } = "Resources/Logs/Exported";
    }

    public class UiSettings
    {
        [Required]
        public string Theme { get; set; } = "System";
        public bool StartMinimized { get; set; }
        [Range(100, 60000)]
        public int AutoRefreshInterval { get; set; } = 2000;
        public bool AutoScrollLogs { get; set; } = true;
        public bool ShowTimestampInLogs { get; set; } = true;
        public bool ShowCategoryInLogs { get; set; } = true;
        public bool ShowLogLevelInLogs { get; set; } = true;
    }

    public class PathSettings
    {
        public bool AutoCreateDirectories { get; set; } = true;
        [Required]
        public string ResourcesDirectory { get; set; } = "Resources";
        [Required]
        public string ProfilesDirectory { get; set; } = "Resources/Profiles";
        [Required]
        public string LogsDirectory { get; set; } = "Resources/Logs";
        [Required]
        public string JobsDirectory { get; set; } = "Resources/Jobs";
        [Required]
        public string TasksDirectory { get; set; } = "Resources/Tasks";
        [Required]
        public string PayloadsDirectory { get; set; } = "Resources/Payloads";
        [Required]
        public string DumpsDirectory { get; set; } = "Resources/Dumps";
    }

    public class ProfileSettings
    {
        public bool AutoSave { get; set; } = true;
        public bool BackupOnSave { get; set; } = true;
        public string SerialPath { get; set; } = "Resources/Profiles/Serial/SerialProfiles.json";
        public string SocatPath { get; set; } = "Resources/Profiles/Socat/SocatProfiles.json";
        public string PowerSupplyPath { get; set; } = "Resources/Profiles/PowerSupply/PowerSupplyProfiles.json";
        public string MemoryRegionPath { get; set; } = "Resources/Profiles/MemoryRegions/profiles.json";
    }

    public class MemoryRegionSettings
    {
        [Range(1, 1000)]
        public int MaxProfiles { get; set; } = 100;
        public bool AutoLoadDefaultProfile { get; set; } = true;
        public bool AutoSaveProfiles { get; set; } = true;
        public bool ValidationEnabled { get; set; } = true;
        public bool AutoSelectBssSegment { get; set; } = true;
        [Range(1, 500)]
        public int MaxProfilesInDropdown { get; set; } = 50;
        public bool EnableTemplateImport { get; set; } = true;
        public string ExportFormats { get; set; } = "JSON";
        [Range(1, 1000)]
        public int MaxSegmentsPerProfile { get; set; } = 50;
        public bool EnableOverlapDetection { get; set; } = true;
        public bool LogProfileOperations { get; set; } = true;
    }

    public class ExportSettings
    {
        [Required]
        public string DefaultFormat { get; set; } = "JSON";
        public string CsvDirectory { get; set; } = "Resources/Logs/Exported/CSV";
        public string TxtDirectory { get; set; } = "Resources/Logs/Exported/TXT";
        public string JsonDirectory { get; set; } = "Resources/Logs/Exported/JSON";
    }

    public class PlcSettings
    {
        [Range(100, 60000)]
        public int ConnectionTimeout { get; set; } = 5000;
        [Range(100, 60000)]
        public int ReadTimeout { get; set; } = 2000;
        [Range(0, 10)]
        public int RetryAttempts { get; set; } = 3;
    }

    public class JobSettings
    {
        public string ProfilesPath { get; set; } = "Resources/Jobs/Jobs.json";
    }

    public class TaskSettings
    {
        public string ProfilesPath { get; set; } = "Resources/Tasks/Tasks.json";
        [Range(1000, 3600000)]
        public int AutoSaveInterval { get; set; } = 10000;
    }

    public class SerialSettings
    {
        [Range(300, 4000000)]
        public int DefaultBaudRate { get; set; } = 9600;
        [Range(5, 8)]
        public int DefaultDataBits { get; set; } = 8;
        public string DefaultParity { get; set; } = "None";
        public string DefaultStopBits { get; set; } = "One";
        public bool IncludeUsbPorts { get; set; } = true;
        public bool IncludeAcmPorts { get; set; } = true;
        public bool IncludeStandardPorts { get; set; } = true;
        [Range(1, 256)]
        public int MaxScanPorts { get; set; } = 32;
        [Range(1, 60)]
        public int ScanIntervalSeconds { get; set; } = 5;
        [Range(100, 30000)]
        public int PortTestTimeoutMs { get; set; } = 1000;
    }

    public class NetworkSettings
    {
        [Range(1, 65535)]
        public int DefaultSocatPort { get; set; } = 2023;
        [Range(1, 65535)]
        public int PowerSupplyPort { get; set; } = 502;
        [Range(0, 10)]
        public int ConnectionRetries { get; set; } = 3;
    }

    public class SocatSettings
    {
        [Range(1, 100)]
        public int MaxConcurrentInstances { get; set; } = 5;
        public bool AutoConfigureSerialDevice { get; set; } = true;
        [Range(1, 60)]
        public int ProcessShutdownTimeoutSeconds { get; set; } = 5;
        [Range(1, 60)]
        public int StatusRefreshIntervalSeconds { get; set; } = 2;
        public bool CaptureProcessOutput { get; set; } = true;
    }

    public class PowerSupplySettings
    {
        [Range(1, 1000)]
        public int MaxProfiles { get; set; } = 100;
        public bool AutoLoadDefaultProfile { get; set; } = true;
        public bool AutoSaveProfiles { get; set; } = true;
        [Range(100, 60000)]
        public int DefaultConnectionTimeoutMs { get; set; } = 5000;
        public bool EnableConnectionPooling { get; set; } = true;
        public bool EnableAutoReconnect { get; set; } = true;
        [Range(100, 60000)]
        public int ReconnectDelayMs { get; set; } = 2000;
        [Range(0, 20)]
        public int MaxReconnectAttempts { get; set; } = 5;
        public bool ConfirmPowerOff { get; set; } = true;
        public bool ConfirmPowerOn { get; set; }
        [Range(100, 10000)]
        public int PowerStateChangeDelayMs { get; set; } = 1000;
        public bool AutoReadStateAfterConnect { get; set; } = true;
        [Range(100, 60000)]
        public int StatusRefreshIntervalMs { get; set; } = 5000;
        public bool ShowPowerStateNotifications { get; set; } = true;
        public bool ShowConnectionNotifications { get; set; } = true;
        public bool LogModbusOperations { get; set; }
        public bool LogConnectionStateChanges { get; set; } = true;
        public bool LogPowerStateChanges { get; set; } = true;
    }
}
