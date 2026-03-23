using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service interface for payload set profile management.
/// </summary>
public interface IPayloadSetProfileService : IProfileManager<PayloadSetProfile>
{
}

/// <summary>
/// Manages payload set profiles using the standardized profile management pattern.
/// </summary>
/// <remarks>
/// PayloadSetProfiles define the base directory where bootloader payloads (stager.bin, dumper.bin) are located.
/// This service provides:
/// - Thread-safe CRUD operations
/// - Persistence to JSON file (Resources/PayloadProfiles/profiles.json)
/// - Default profile management
/// - Name uniqueness validation
/// - Gap-filling ID assignment
/// </remarks>
public sealed class PayloadSetProfileService : StandardProfileManager<PayloadSetProfile>, IPayloadSetProfileService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadSetProfileService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathService">The path service for resolving profile storage location.</param>
    public PayloadSetProfileService(
        ILogger<PayloadSetProfileService> logger,
        IPathService pathService)
        : base(pathService.PayloadSetProfilesPath, logger)
    {
        _logger.LogInformation("PayloadSetProfileService initialized with path: {ProfilesPath}", _profilesPath);
    }

    /// <inheritdoc/>
    protected override PayloadSetProfile CreateSystemDefault()
    {
        return PayloadSetProfile.CreateDefault();
    }

    /// <inheritdoc/>
    protected override string ProfileTypeName => "PayloadSet";

    /// <inheritdoc/>
    protected override async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        // Create a default payload set profile using the centralized factory method
        PayloadSetProfile defaultProfile = PayloadSetProfile.CreateDefault();

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
            await File.WriteAllTextAsync(_profilesPath, json, cancellationToken);

            _logger.LogInformation("Created default {ProfileType} profile at {Path}",
                ProfileTypeName, _profilesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default {ProfileType} profiles", ProfileTypeName);
            throw;
        }
    }
}

