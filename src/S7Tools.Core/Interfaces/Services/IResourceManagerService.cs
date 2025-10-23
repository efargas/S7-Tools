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
        /// <summary>
        /// Whether initialization was successful overall
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// List of resources that were created during initialization
        /// </summary>
        public List<string> CreatedResources { get; set; } = new();

        /// <summary>
        /// Critical errors that occurred during initialization
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Non-critical warnings from initialization
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Time taken to complete initialization
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Result of resource validation operations
    /// </summary>
    public class ResourceValidationResult
    {
        /// <summary>
        /// Whether all resources are valid and accessible
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Resources that are missing and need to be created
        /// </summary>
        public List<string> MissingResources { get; set; } = new();

        /// <summary>
        /// Resources that exist but cannot be accessed due to permissions
        /// </summary>
        public List<string> InaccessibleResources { get; set; } = new();

        /// <summary>
        /// Resources that are corrupted or malformed
        /// </summary>
        public List<string> CorruptedResources { get; set; } = new();

        /// <summary>
        /// Non-critical warnings from validation
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Result of resource creation operations
    /// </summary>
    public class ResourceCreationResult
    {
        /// <summary>
        /// Whether all resources were created successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Resources that were successfully created
        /// </summary>
        public List<string> CreatedResources { get; set; } = new();

        /// <summary>
        /// Resources that failed to be created
        /// </summary>
        public List<string> FailedResources { get; set; } = new();

        /// <summary>
        /// Errors that occurred during resource creation
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Time taken to complete resource creation
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
