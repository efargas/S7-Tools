using System;
using System.IO;
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
