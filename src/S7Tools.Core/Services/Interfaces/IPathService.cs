using System.Threading.Tasks;

namespace S7Tools.Core.Services.Interfaces
{
    public interface IPathService
    {
        string AppSettingsPath { get; }
        string SerialProfilesPath { get; }
        string SocatProfilesPath { get; }
        string PowerSupplyProfilesPath { get; }
        string JobsProfilesPath { get; }
        string TasksPath { get; }
        string ExtractedFwPath { get; }
        string DumpsPath { get; }
        string LogsPath { get; }
        string PayloadsPath { get; }
        string ExportLogsPath { get; }

        Task InitializeDirectoriesAsync();
    }
}
