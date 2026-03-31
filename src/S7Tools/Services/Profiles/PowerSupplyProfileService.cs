using System.Text.Json;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;

namespace S7Tools.Services;

/// <summary>
/// Standard implementation of power supply profile management.
/// Provides unified profile management functionality for PowerSupplyProfile objects.
/// </summary>
/// <remarks>
/// This is a completely new standardized implementation that replaces the legacy PowerSupplyProfileService.
/// It inherits from StandardProfileManager to provide consistent behavior across all profile types.
/// </remarks>
public class PowerSupplyProfileService : StandardProfileManager<PowerSupplyProfile>, IPowerSupplyProfileService
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the PowerSupplyProfileService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathService">The path service for resolving profile file paths.</param>
    public PowerSupplyProfileService(ILogger<PowerSupplyProfileService> logger, IPathService pathService)
        : base(pathService.PowerSupplyProfilesPath, logger)
    {
    }

    #endregion

    #region StandardProfileManager Implementation

    /// <inheritdoc/>
    protected override PowerSupplyProfile CreateSystemDefault()
    {
        return PowerSupplyProfile.CreateDefaultProfile();
    }

    /// <inheritdoc/>
    protected override string ProfileTypeName => "PowerSupply";

    /// <inheritdoc/>
    protected override async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        // Create a default power supply profile using the centralized factory method
        // This ensures consistency between dialog defaults and saved profile defaults
        PowerSupplyProfile defaultProfile = PowerSupplyProfile.CreateDefaultProfile();

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

            _logger.LogInformation("Created default power supply profile: {ProfileName}", defaultProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default power supply profile");
            _profiles.Clear(); // Clear the in-memory profiles if save failed
        }
    }

    #endregion
}
