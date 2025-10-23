using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Standard implementation of serial port profile management.
/// Provides unified profile management functionality for SerialPortProfile objects.
/// </summary>
/// <remarks>
/// This is a completely new standardized implementation that replaces the legacy SerialPortProfileService.
/// It inherits from StandardProfileManager to provide consistent behavior across all profile types.
/// </remarks>
public class SerialPortProfileService : StandardProfileManager<SerialPortProfile>, ISerialPortProfileService
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SerialPortProfileService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathService">The path service for resolving profile file paths.</param>
    public SerialPortProfileService(ILogger<SerialPortProfileService> logger, IPathService pathService)
        : base(pathService.SerialProfilesPath, logger)
    {
    }

    #endregion

    #region StandardProfileManager Implementation

    /// <inheritdoc/>
    protected override SerialPortProfile CreateSystemDefault()
    {
        return SerialPortProfile.CreateDefaultProfile();
    }

    /// <inheritdoc/>
    protected override string ProfileTypeName => "SerialPort";

    /// <inheritdoc/>
    protected override async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        // Create a default serial port profile using the centralized factory method
        // This ensures consistency between dialog defaults and saved profile defaults
        SerialPortProfile defaultProfile = SerialPortProfile.CreateDefaultProfile();

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

            _logger.LogInformation("Created default serial port profile: {ProfileName}", defaultProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default serial port profile");
            _profiles.Clear(); // Clear the in-memory profiles if save failed
        }
    }

    #endregion
}
