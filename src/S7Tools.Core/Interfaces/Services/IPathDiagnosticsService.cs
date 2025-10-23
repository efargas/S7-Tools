using S7Tools.Core.Models.Configuration;

namespace S7Tools.Core.Interfaces.Services
{
    /// <summary>
    /// Service for diagnosing path-related issues and providing detailed error information
    /// </summary>
    public interface IPathDiagnosticsService
    {
        /// <summary>
        /// Performs comprehensive path validation and reports any issues
        /// </summary>
        /// <returns>Diagnostic report with findings</returns>
        Task<PathDiagnosticReport> RunDiagnosticsAsync();

        /// <summary>
        /// Generates a detailed diagnostic report as formatted text
        /// </summary>
        /// <param name="report">Diagnostic report to format</param>
        /// <returns>Formatted diagnostic report</returns>
        string GenerateDetailedReport(PathDiagnosticReport report);
    }
}
