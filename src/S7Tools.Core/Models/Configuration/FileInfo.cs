namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Represents metadata about a required file
    /// </summary>
    public sealed class FileInfo
    {
        /// <summary>
        /// File name including extension
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Path relative to base directory
        /// </summary>
        public required string RelativePath { get; init; }

        /// <summary>
        /// Computed absolute path
        /// </summary>
        public string AbsolutePath { get; set; } = string.Empty;

        /// <summary>
        /// Content to write if file doesn't exist
        /// </summary>
        public string DefaultContent { get; init; } = string.Empty;

        /// <summary>
        /// Whether file should be created from template
        /// </summary>
        public bool IsTemplate { get; init; }

        /// <summary>
        /// Human-readable description of the file's purpose
        /// </summary>
        public string Purpose { get; init; } = string.Empty;
    }
}
