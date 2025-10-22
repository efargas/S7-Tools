namespace S7Tools.Core.Constants
{
    /// <summary>
    /// Constants for application resource paths to avoid hardcoded strings
    /// </summary>
    public static class ResourcePaths
    {
        // Root folder
        public const string ResourcesFolder = "Resources";

        // Application settings
        public const string AppSettingsFolder = "AppSettings";
        public const string AppSettingsFile = "AppSettings.json";

        // Profiles
        public const string ProfilesFolder = "Profiles";
        public const string SerialFolder = "Serial";
        public const string SerialProfilesFile = "SerialProfiles.json";
        public const string SocatFolder = "Socat";
        public const string SocatProfilesFile = "SocatProfiles.json";
        public const string PowerSupplyFolder = "PowerSupply";
        public const string PowerSupplyProfilesFile = "PowerSupplyProfiles.json";
        public const string MemoryRegionsFolder = "MemoryRegions";

        // Logs
        public const string LogsFolder = "Logs";
        public const string MainLogsFolder = "Main";
        public const string ExportedLogsFolder = "Exported";
        public const string CsvLogsFolder = "CSV";
        public const string TxtLogsFolder = "TXT";
        public const string JsonLogsFolder = "JSON";

        // Log file patterns
        public const string MainLogFilePattern = "MainLog_{0}_{1}.json";
        public const string ExportedLogFilePattern = "s7tools_logs_{0}.{1}";

        // Jobs and Tasks
        public const string JobsFolder = "Jobs";
        public const string JobsFile = "Jobs.json";
        public const string TasksFolder = "Tasks";
        public const string TasksFile = "Tasks.json";

        // Content folders
        public const string PayloadsFolder = "Payloads";
        public const string DumpsFolder = "Dumps";
    }

    /// <summary>
    /// Constants for timestamp and file naming
    /// </summary>
    public static class FileNaming
    {
        public const string TimestampFormat = "yyyyMMdd_HHmmss";
        public const string CsvExtension = "csv";
        public const string TxtExtension = "txt";
        public const string JsonExtension = "json";
    }
}
