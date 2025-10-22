namespace S7Tools.Core.Models
{
    public class AppSettings
    {
        public string SerialProfilesPath { get; set; } = string.Empty;
        public string SocatProfilesPath { get; set; } = string.Empty;
        public string PowerSupplyProfilesPath { get; set; } = string.Empty;
        public string JobsProfilesPath { get; set; } = string.Empty;
        public string TasksPath { get; set; } = string.Empty;
        public string ExtractedFwPath { get; set; } = string.Empty;
        public string DumpsPath { get; set; } = string.Empty;
        public string LogsPath { get; set; } = string.Empty;
        public string PayloadsPath { get; set; } = string.Empty;
        public string ExportLogsPath { get; set; } = string.Empty;
    }
}
