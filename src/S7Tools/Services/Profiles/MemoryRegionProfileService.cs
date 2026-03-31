using System.Text.Json;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;

namespace S7Tools.Services;

/// <summary>
/// Standard implementation of memory region profile management.
/// Provides unified profile management functionality for MemoryMappingProfile objects.
/// </summary>
/// <remarks>
/// This service follows the standardized approach using StandardProfileManager&lt;T&gt; base class.
/// It provides consistent behavior with other profile types including thread safety,
/// JSON persistence, and comprehensive business rule enforcement.
/// </remarks>
public class MemoryRegionProfileService : StandardProfileManager<MemoryMappingProfile>, IMemoryRegionProfileService
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the MemoryRegionProfileService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathService">The path service for resolving profile file paths.</param>
    public MemoryRegionProfileService(ILogger<MemoryRegionProfileService> logger, IPathService pathService)
        : base(pathService.MemoryRegionProfilesPath, logger)
    {
    }

    #endregion

    #region StandardProfileManager Implementation

    /// <inheritdoc/>
    protected override MemoryMappingProfile CreateSystemDefault()
    {
        return MemoryMappingProfile.CreateDefaultProfile();
    }

    /// <inheritdoc/>
    protected override string ProfileTypeName => "Memory Region Profile";

    /// <inheritdoc/>
    protected override async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        // Create a default memory region profile using the centralized factory method
        // This ensures consistency between dialog defaults and saved profile defaults
        MemoryMappingProfile defaultProfile = MemoryMappingProfile.CreateDefaultProfile();

        // Override read-only to allow user modifications of the saved default profile
        defaultProfile.IsReadOnly = false;

        _profiles.Add(defaultProfile);

        // Ensure directory exists
        string? directory = Path.GetDirectoryName(_profilesPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save profiles using the same logic as SaveProfilesAsync
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(_profiles, options);
            await File.WriteAllTextAsync(_profilesPath, json, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created default memory region profile: {ProfileName}", defaultProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default memory region profile");
            _profiles.Clear(); // Clear the in-memory profiles if save failed
        }
    }

    #endregion
}
