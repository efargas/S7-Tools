using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

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
    public PowerSupplyProfileService(ILogger<PowerSupplyProfileService> logger)
        : base(GetDefaultProfilesPath(), logger)
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
        // Create a default power supply profile
        var defaultProfile = new PowerSupplyProfile
        {
            Name = "PowerSupplyDefault",
            Description = "Default power supply profile created automatically",
            Configuration = new ModbusTcpConfiguration
            {
                Host = "192.168.1.100",
                Port = 502,
                DeviceId = 1
            },
            IsDefault = true,
            IsReadOnly = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Id = 1
        };

        _profiles.Add(defaultProfile);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_profilesPath);
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

            var json = JsonSerializer.Serialize(_profiles, options);
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

    #region Private Helper Methods

    /// <summary>
    /// Gets the default path for power supply profiles.
    /// </summary>
    private static string GetDefaultProfilesPath()
    {
        var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "PowerSupplyProfiles");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "profiles.json");
    }

    #endregion
}
