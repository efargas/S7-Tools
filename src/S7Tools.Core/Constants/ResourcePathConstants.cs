namespace S7Tools.Core.Constants
{
    /// <summary>
    /// Constants for application resource paths to avoid hardcoded strings
    /// </summary>
    public static class ResourcePaths
    {
        /// <summary>
        /// Root folder name for all application resources
        /// </summary>
        public const string ResourcesFolder = "Resources";

        /// <summary>
        /// Folder name for application settings
        /// </summary>
        public const string AppSettingsFolder = "AppSettings";

        /// <summary>
        /// File name for application settings JSON file
        /// </summary>
        public const string AppSettingsFile = "AppSettings.json";

        /// <summary>
        /// Folder name for all profile types
        /// </summary>
        public const string ProfilesFolder = "Profiles";

        /// <summary>
        /// Folder name for serial communication profiles
        /// </summary>
        public const string SerialFolder = "Serial";

        /// <summary>
        /// File name for serial profiles JSON file
        /// </summary>
        public const string SerialProfilesFile = "SerialProfiles.json";

        /// <summary>
        /// Folder name for socat profiles
        /// </summary>
        public const string SocatFolder = "Socat";

        /// <summary>
        /// File name for socat profiles JSON file
        /// </summary>
        public const string SocatProfilesFile = "SocatProfiles.json";

        /// <summary>
        /// Folder name for power supply profiles
        /// </summary>
        public const string PowerSupplyFolder = "PowerSupply";

        /// <summary>
        /// File name for power supply profiles JSON file
        /// </summary>
        public const string PowerSupplyProfilesFile = "PowerSupplyProfiles.json";

        /// <summary>
        /// Folder name for memory region profiles
        /// </summary>
        public const string MemoryRegionsFolder = "MemoryRegions";

        /// <summary>
        /// File name for memory region profiles JSON file
        /// </summary>
        public const string MemoryRegionProfilesFile = "MemoryRegionProfiles.json";

        /// <summary>
        /// Folder name for all log files
        /// </summary>
        public const string LogsFolder = "Logs";

        /// <summary>
        /// Folder name for main application logs
        /// </summary>
        public const string MainLogsFolder = "Main";

        /// <summary>
        /// Folder name for exported log files
        /// </summary>
        public const string ExportedLogsFolder = "Exported";

        /// <summary>
        /// Folder name for CSV exported logs
        /// </summary>
        public const string CsvLogsFolder = "CSV";

        /// <summary>
        /// Folder name for TXT exported logs
        /// </summary>
        public const string TxtLogsFolder = "TXT";

        /// <summary>
        /// Folder name for JSON exported logs
        /// </summary>
        public const string JsonLogsFolder = "JSON";

        /// <summary>
        /// File name pattern for main log files with timestamp and rolling number placeholders
        /// </summary>
        public const string MainLogFilePattern = "MainLog_{0}_{1}.json";

        /// <summary>
        /// File name pattern for exported log files with timestamp and extension placeholders
        /// </summary>
        public const string ExportedLogFilePattern = "s7tools_logs_{0}.{1}";

        /// <summary>
        /// Folder name for job definitions
        /// </summary>
        public const string JobsFolder = "Jobs";

        /// <summary>
        /// File name for jobs JSON file
        /// </summary>
        public const string JobsFile = "Jobs.json";

        /// <summary>
        /// Folder name for task definitions
        /// </summary>
        public const string TasksFolder = "Tasks";

        /// <summary>
        /// File name for tasks JSON file
        /// </summary>
        public const string TasksFile = "Tasks.json";

        /// <summary>
        /// Folder name for payload files
        /// </summary>
        public const string PayloadsFolder = "Payloads";

        /// <summary>
        /// Folder name for memory dump files
        /// </summary>
        public const string DumpsFolder = "Dumps";
    }

    /// <summary>
    /// Constants for timestamp and file naming
    /// </summary>
    public static class FileNaming
    {
        /// <summary>
        /// Standard timestamp format for file names
        /// </summary>
        public const string TimestampFormat = "yyyyMMdd_HHmmss";

        /// <summary>
        /// CSV file extension without period
        /// </summary>
        public const string CsvExtension = "csv";

        /// <summary>
        /// Text file extension without period
        /// </summary>
        public const string TxtExtension = "txt";

        /// <summary>
        /// JSON file extension without period
        /// </summary>
        public const string JsonExtension = "json";
    }
}
