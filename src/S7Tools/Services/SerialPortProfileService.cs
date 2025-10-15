using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    public SerialPortProfileService(ILogger<SerialPortProfileService> logger)
        : base(GetDefaultProfilesPath(), logger)
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
        // Create a default serial port profile with proper configuration
        var defaultProfile = new SerialPortProfile
        {
            Name = "SerialDefault",
            Description = "Default serial port profile created automatically",
            Configuration = new SerialPortConfiguration
            {
                BaudRate = 9600,
                CharacterSize = 8,
                Parity = ParityMode.None,
                StopBits = StopBits.One
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

            _logger.LogInformation("Created default serial port profile: {ProfileName}", defaultProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default serial port profile");
            _profiles.Clear(); // Clear the in-memory profiles if save failed
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets the default path for serial port profiles.
    /// </summary>
    private static string GetDefaultProfilesPath()
    {
        var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "SerialProfiles");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "profiles.json");
    }

    #endregion
}
