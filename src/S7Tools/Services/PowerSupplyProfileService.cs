using System;
using System.IO;
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