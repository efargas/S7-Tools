using System;
using System.IO;
using System.Text.Json;
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

    /// <inheritdoc/>
    protected override async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        // Create a default socat profile with typical TCP to serial bridge configuration
        var defaultProfile = new SocatProfile
        {
            Name = "SocatDefault",
            Description = "Default socat profile for TCP to serial bridge created automatically",
            Configuration = new SocatConfiguration
            {
                TcpPort = 2001,
                EnableFork = true,
                EnableReuseAddr = true,
                SerialRawMode = true,
                SerialDisableEcho = true
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

            _logger.LogInformation("Created default socat profile: {ProfileName}", defaultProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default socat profile");
            _profiles.Clear(); // Clear the in-memory profiles if save failed
        }
    }

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
