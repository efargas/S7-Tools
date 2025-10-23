namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Defines the type of resource in the application
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// A directory/folder resource
        /// </summary>
        Directory,

        /// <summary>
        /// A file resource
        /// </summary>
        File,

        /// <summary>
        /// A template file that creates content
        /// </summary>
        Template,

        /// <summary>
        /// A profile-specific resource
        /// </summary>
        Profile
    }
}
