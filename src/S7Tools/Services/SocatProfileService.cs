using System;
using System.IO;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Standard implementation of socat profile management.
/// Provides unified profile management functionality for SocatProfile objects.
/// </summary>
/// <remarks>
/// This is a completely new standardized implementation that replaces the legacy SocatProfileService.
/// It inherits from StandardProfileManager to provide consistent behavior across all profile types.
/// </remarks>
public class SocatProfileService : StandardProfileManager<SocatProfile>, ISocatProfileService
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SocatProfileService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SocatProfileService(ILogger<SocatProfileService> logger)
        : base(GetDefaultProfilesPath(), logger)
    {
    }

    #endregion

    #region StandardProfileManager Implementation

    /// <inheritdoc/>
    protected override SocatProfile CreateSystemDefault()
    {
        return SocatProfile.CreateDefaultProfile();
    }

    /// <inheritdoc/>
    protected override string ProfileTypeName => "Socat";

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets the default path for socat profiles.
    /// </summary>
    private static string GetDefaultProfilesPath()
    {
        var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "SocatProfiles");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "profiles.json");
    }

    #endregion
}