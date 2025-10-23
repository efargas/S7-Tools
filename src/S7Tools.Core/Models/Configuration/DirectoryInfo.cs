namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Represents metadata about a required directory
    /// </summary>
    public sealed class DirectoryInfo
    {
        /// <summary>
        /// Directory name
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
        /// Whether directory needs write access
        /// </summary>
        public bool IsWritable { get; init; } = true;

        /// <summary>
        /// Whether to create if it doesn't exist
        /// </summary>
        public bool CreateIfMissing { get; init; } = true;

        /// <summary>
        /// Human-readable description of the directory's purpose
        /// </summary>
        public string Purpose { get; init; } = string.Empty;
    }
}
