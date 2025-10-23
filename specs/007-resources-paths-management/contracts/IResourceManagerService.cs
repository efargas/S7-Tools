using S7Tools.Core.Models.Configuration;

namespace S7Tools.Core.Interfaces.Services
{
    /// <summary>
    /// Service for managing application resources and their validation
    /// </summary>
    public interface IResourceManagerService
    {
        /// <summary>
        /// Initializes all required application resources
        /// </summary>
        /// <returns>Resource initialization result</returns>
        Task<ResourceInitializationResult> InitializeResourcesAsync();

        /// <summary>
        /// Validates that all required resources exist and are accessible
        /// </summary>
        /// <returns>Resource validation result</returns>
        Task<ResourceValidationResult> ValidateResourcesAsync();

        /// <summary>
        /// Creates missing resources based on the resource manifest
        /// </summary>
        /// <returns>Resource creation result</returns>
        Task<ResourceCreationResult> CreateMissingResourcesAsync();

        /// <summary>
        /// Gets the resource manifest defining required resources
        /// </summary>
        /// <returns>Current resource manifest</returns>
        ResourceManifest GetResourceManifest();

        /// <summary>
        /// Creates default profile files if they don't exist
        /// </summary>
        /// <returns>True if profiles were created or already existed</returns>
        Task<bool> EnsureDefaultProfilesExistAsync();

        /// <summary>
        /// Validates that a specific resource exists
        /// </summary>
        /// <param name="resourcePath">Path to resource</param>
        /// <returns>True if resource exists and is accessible</returns>
        Task<bool> ResourceExistsAsync(string resourcePath);

        /// <summary>
        /// Creates a resource from template if it doesn't exist
        /// </summary>
        /// <param name="resourceInfo">Resource information</param>
        /// <returns>True if resource was created or already existed</returns>
        Task<bool> EnsureResourceExistsAsync(ResourceInfo resourceInfo);
    }

    /// <summary>
    /// Result of resource initialization operations
    /// </summary>
    public class ResourceInitializationResult
    {
        public bool Success { get; set; }
        public List<string> CreatedResources { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Result of resource validation operations
    /// </summary>
    public class ResourceValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> MissingResources { get; set; } = new();
        public List<string> InaccessibleResources { get; set; } = new();
        public List<string> CorruptedResources { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Result of resource creation operations
    /// </summary>
    public class ResourceCreationResult
    {
        public bool Success { get; set; }
        public List<string> CreatedResources { get; set; } = new();
        public List<string> FailedResources { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }
}
