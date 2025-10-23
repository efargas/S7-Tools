namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Report containing the results of path diagnostics
    /// </summary>
    public sealed class PathDiagnosticReport
    {
        /// <summary>
        /// When the diagnostic started
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the diagnostic completed
        /// </summary>
        public DateTime EndTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Base directory being tested
        /// </summary>
        public string BaseDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Whether all diagnostics passed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// List of successful validations
        /// </summary>
        public List<string> Validations { get; init; } = new();

        /// <summary>
        /// List of warnings (non-critical issues)
        /// </summary>
        public List<string> Warnings { get; init; } = new();

        /// <summary>
        /// List of errors (critical issues)
        /// </summary>
        public List<string> Errors { get; init; } = new();

        /// <summary>
        /// Duration of the diagnostic run
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Total number of issues (warnings + errors)
        /// </summary>
        public int TotalIssues => Warnings.Count + Errors.Count;

        /// <summary>
        /// Whether the diagnostic has any issues
        /// </summary>
        public bool HasIssues => TotalIssues > 0;

        /// <summary>
        /// Summary of the diagnostic results
        /// </summary>
        public string Summary =>
            $"Diagnostics: {(Success ? "PASSED" : "FAILED")} - " +
            $"{Validations.Count} validations, {Warnings.Count} warnings, {Errors.Count} errors " +
            $"({Duration.TotalMilliseconds:F0}ms)";
    }
}
