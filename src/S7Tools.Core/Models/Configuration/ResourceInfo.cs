namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Represents optional resources that may be created on-demand
    /// </summary>
    public sealed class ResourceInfo
    {
        /// <summary>
        /// Type of resource (Directory, File, Template, Profile)
        /// </summary>
        public required ResourceType Type { get; init; }

        /// <summary>
        /// Resource name
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Location path relative to base directory
        /// </summary>
        public required string Path { get; init; }

        /// <summary>
        /// Other resources this depends on
        /// </summary>
        public List<string> Dependencies { get; init; } = new();

        /// <summary>
        /// When/how to create this resource
        /// </summary>
        public CreationStrategy CreationStrategy { get; init; } = CreationStrategy.OnDemand;

        /// <summary>
        /// Human-readable description of the resource's purpose
        /// </summary>
        public string Purpose { get; init; } = string.Empty;
    }
}
