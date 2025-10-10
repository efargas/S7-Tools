using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Models;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for managing socat profiles with JSON-based persistence, thread-safe operations, and comprehensive error handling.
/// This service provides complete CRUD operations, import/export functionality, and validation for socat profiles.
/// </summary>
public class SocatProfileService : ISocatProfileService, IDisposable
{
    private readonly ILogger<SocatProfileService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<SocatProfile> _profiles = new();
    private bool _isInitialized;
    private bool _disposed;
    private int _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the SocatProfileService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="settingsService">The settings service for accessing application settings.</param>
    /// <param name="dialogService">The dialog service for user interactions.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger, settingsService, or dialogService is null.</exception>
    public SocatProfileService(
        ILogger<SocatProfileService> logger,
        ISettingsService settingsService,
        IDialogService dialogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _logger.LogDebug("SocatProfileService initialized");
    }

    #region Profile CRUD Operations

    /// <inheritdoc />
    public async Task<IEnumerable<SocatProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogDebug("Retrieved {Count} socat profiles", _profiles.Count);
            return _profiles.Select(p => p.ClonePreserveId()).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProfile?> GetProfileByIdAsync(int profileId, CancellationToken cancellationToken = default)
    {
        if (profileId <= 0)
        {
            throw new ArgumentException("Profile ID must be greater than zero", nameof(profileId));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            _logger.LogDebug("Retrieved socat profile by ID {ProfileId}: {Found}", profileId, profile != null);
            return profile?.ClonePreserveId();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProfile?> GetProfileByNameAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = _profiles.FirstOrDefault(p => string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase));
            _logger.LogDebug("Retrieved socat profile by name '{ProfileName}': {Found}", profileName, profile != null);
            return profile?.ClonePreserveId();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProfile> CreateProfileAsync(SocatProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        var validationErrors = ValidateProfile(profile);
        if (validationErrors.Any())
        {
            throw new InvalidOperationException($"Profile validation failed: {string.Join(", ", validationErrors)}");
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check maximum profiles limit
            var settings = _settingsService.Settings.Socat;
            if (_profiles.Count >= settings.MaxProfiles)
            {
                throw new InvalidOperationException($"Maximum number of profiles ({settings.MaxProfiles}) has been reached");
            }

            // Ensure profile name is unique
            var uniqueName = await EnsureUniqueProfileNameAsync(profile.Name, cancellationToken).ConfigureAwait(false);
            if (uniqueName == null)
            {
                // User cancelled the operation
                throw new OperationCanceledException("Profile creation was cancelled by the user");
            }

            // Create new profile with assigned ID
            var newProfile = profile.Clone();
            newProfile.Id = _nextId++;
            newProfile.Name = uniqueName; // Use the unique name
            newProfile.CreatedAt = DateTime.UtcNow;
            newProfile.ModifiedAt = DateTime.UtcNow;

            // If the new profile requests to be the default, clear any existing default flags
            if (newProfile.IsDefault)
            {
                foreach (var p in _profiles)
                {
                    p.IsDefault = false; // Clear existing defaults
                }
            }

            _profiles.Add(newProfile);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created new socat profile '{ProfileName}' with ID {ProfileId}", newProfile.Name, newProfile.Id);
            return newProfile.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProfile> UpdateProfileAsync(SocatProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        if (profile.Id <= 0)
        {
            throw new ArgumentException("Profile must have a valid ID", nameof(profile));
        }

        var validationErrors = ValidateProfile(profile);
        if (validationErrors.Any())
        {
            throw new InvalidOperationException($"Profile validation failed: {string.Join(", ", validationErrors)}");
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existingProfile = _profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existingProfile == null)
            {
                throw new InvalidOperationException($"Profile with ID {profile.Id} does not exist");
            }

            if (existingProfile.IsReadOnly)
            {
                throw new ArgumentException("Cannot modify read-only profile", nameof(profile));
            }

            // Ensure profile name is unique (excluding the current profile)
            var uniqueName = await EnsureUniqueProfileNameForUpdateAsync(profile.Name, profile.Id, cancellationToken).ConfigureAwait(false);
            if (uniqueName == null)
            {
                // User cancelled the operation
                throw new OperationCanceledException("Profile update was cancelled by the user");
            }

            // Update existing profile
            var updatedProfile = profile.Clone();
            updatedProfile.Name = uniqueName; // Use the unique name
            updatedProfile.CreatedAt = existingProfile.CreatedAt; // Preserve creation time
            updatedProfile.ModifiedAt = DateTime.UtcNow;

            // If the updated profile is now marked as default, clear the flag on all other profiles
            if (updatedProfile.IsDefault)
            {
                foreach (var p in _profiles)
                {
                    p.IsDefault = false; // Clear existing defaults
                }
            }

            var index = _profiles.IndexOf(existingProfile);
            _profiles[index] = updatedProfile;

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated socat profile '{ProfileName}' with ID {ProfileId}", updatedProfile.Name, updatedProfile.Id);
            return updatedProfile.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProfileAsync(int profileId, CancellationToken cancellationToken = default)
    {
        if (profileId <= 0)
        {
            throw new ArgumentException("Profile ID must be greater than zero", nameof(profileId));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
            {
                _logger.LogWarning("Attempted to delete non-existent socat profile with ID {ProfileId}", profileId);
                return false;
            }

            if (profile.IsReadOnly)
            {
                throw new InvalidOperationException("Cannot delete read-only profile");
            }

            if (profile.IsDefault)
            {
                throw new InvalidOperationException("Cannot delete default profile. Set another profile as default first.");
            }

            _profiles.Remove(profile);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deleted socat profile '{ProfileName}' with ID {ProfileId}", profile.Name, profileId);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Profile Management Operations

    /// <inheritdoc />
    public async Task<SocatProfile> DuplicateProfileAsync(int sourceProfileId, string newName, CancellationToken cancellationToken = default)
    {
        if (sourceProfileId <= 0)
        {
            throw new ArgumentException("Source profile ID must be greater than zero", nameof(sourceProfileId));
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("New name cannot be null or empty", nameof(newName));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var sourceProfile = _profiles.FirstOrDefault(p => p.Id == sourceProfileId);
            if (sourceProfile == null)
            {
                throw new InvalidOperationException($"Source profile with ID {sourceProfileId} does not exist");
            }

            // Ensure new name is unique
            var uniqueName = await EnsureUniqueProfileNameAsync(newName, cancellationToken).ConfigureAwait(false);
            if (uniqueName == null)
            {
                // User cancelled the operation
                throw new OperationCanceledException("Profile duplication was cancelled by the user");
            }

            // Create duplicate
            var duplicate = sourceProfile.Duplicate(uniqueName);
            duplicate.Id = _nextId++;
            duplicate.CreatedAt = DateTime.UtcNow;
            duplicate.ModifiedAt = DateTime.UtcNow;
            duplicate.IsDefault = false; // Duplicates are never default
            duplicate.IsReadOnly = false; // Duplicates are always editable

            _profiles.Add(duplicate);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Duplicated socat profile '{SourceName}' to '{NewName}' with ID {ProfileId}", sourceProfile.Name, duplicate.Name, duplicate.Id);
            return duplicate.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProfile> GetDefaultProfileAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile == null)
            {
                throw new InvalidOperationException("No default socat profile found");
            }

            _logger.LogDebug("Retrieved default socat profile '{ProfileName}' with ID {ProfileId}", defaultProfile.Name, defaultProfile.Id);
            return defaultProfile.ClonePreserveId();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetDefaultProfileAsync(int profileId, CancellationToken cancellationToken = default)
    {
        if (profileId <= 0)
        {
            throw new ArgumentException("Profile ID must be greater than zero", nameof(profileId));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
            {
                throw new InvalidOperationException($"Profile with ID {profileId} does not exist");
            }

            // Clear existing default flags
            foreach (var p in _profiles)
            {
                p.IsDefault = false;
            }

            // Set new default
            profile.IsDefault = true;
            profile.ModifiedAt = DateTime.UtcNow;

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Set socat profile '{ProfileName}' with ID {ProfileId} as default", profile.Name, profileId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProfile> EnsureDefaultProfileExistsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile != null)
            {
                _logger.LogDebug("Default socat profile already exists: '{ProfileName}' with ID {ProfileId}", defaultProfile.Name, defaultProfile.Id);
                return defaultProfile.ClonePreserveId();
            }

            // Create default profile
            var systemDefault = SocatProfile.CreateDefaultProfile();
            systemDefault.Id = _nextId++;
            systemDefault.CreatedAt = DateTime.UtcNow;
            systemDefault.ModifiedAt = DateTime.UtcNow;

            _profiles.Add(systemDefault);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created system default socat profile '{ProfileName}' with ID {ProfileId}", systemDefault.Name, systemDefault.Id);
            return systemDefault.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Import/Export Operations

    /// <inheritdoc />
    public async Task<string> ExportProfileToJsonAsync(int profileId, CancellationToken cancellationToken = default)
    {
        if (profileId <= 0)
        {
            throw new ArgumentException("Profile ID must be greater than zero", nameof(profileId));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
            {
                throw new InvalidOperationException($"Profile with ID {profileId} does not exist");
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(profile, options);
            _logger.LogDebug("Exported socat profile '{ProfileName}' to JSON", profile.Name);
            return json;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportProfilesToJsonAsync(IEnumerable<int> profileIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileIds, nameof(profileIds));

        var ids = profileIds.ToList();
        if (!ids.Any())
        {
            throw new ArgumentException("Profile IDs collection cannot be empty", nameof(profileIds));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profilesToExport = new List<SocatProfile>();
            foreach (var id in ids)
            {
                var profile = _profiles.FirstOrDefault(p => p.Id == id);
                if (profile == null)
                {
                    throw new InvalidOperationException($"Profile with ID {id} does not exist");
                }
                profilesToExport.Add(profile);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(profilesToExport, options);
            _logger.LogDebug("Exported {Count} socat profiles to JSON", profilesToExport.Count);
            return json;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportAllProfilesToJsonAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_profiles, options);
            _logger.LogDebug("Exported all {Count} socat profiles to JSON", _profiles.Count);
            return json;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProfile> ImportProfileFromJsonAsync(string jsonData, bool overwriteExisting = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            throw new ArgumentException("JSON data cannot be null or empty", nameof(jsonData));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            SocatProfile importedProfile;
            try
            {
                importedProfile = JsonSerializer.Deserialize<SocatProfile>(jsonData, options)
                    ?? throw new InvalidOperationException("Failed to deserialize profile from JSON");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON data: {ex.Message}", nameof(jsonData), ex);
            }

            // Validate imported profile
            var validationErrors = ValidateProfile(importedProfile);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Imported profile validation failed: {string.Join(", ", validationErrors)}");
            }

            // Check if profile with same name exists
            var existingProfile = _profiles.FirstOrDefault(p => string.Equals(p.Name, importedProfile.Name, StringComparison.OrdinalIgnoreCase));
            if (existingProfile != null && !overwriteExisting)
            {
                throw new InvalidOperationException($"Profile with name '{importedProfile.Name}' already exists. Use overwriteExisting=true to replace it.");
            }

            if (existingProfile != null && overwriteExisting)
            {
                // Update existing profile
                if (existingProfile.IsReadOnly)
                {
                    throw new InvalidOperationException("Cannot overwrite read-only profile");
                }

                importedProfile.Id = existingProfile.Id;
                importedProfile.CreatedAt = existingProfile.CreatedAt;
                importedProfile.ModifiedAt = DateTime.UtcNow;

                var index = _profiles.IndexOf(existingProfile);
                _profiles[index] = importedProfile;

                _logger.LogInformation("Overwrote existing socat profile '{ProfileName}' with ID {ProfileId}", importedProfile.Name, importedProfile.Id);
            }
            else
            {
                // Create new profile
                importedProfile.Id = _nextId++;
                importedProfile.CreatedAt = DateTime.UtcNow;
                importedProfile.ModifiedAt = DateTime.UtcNow;
                importedProfile.IsDefault = false; // Imported profiles are never default

                _profiles.Add(importedProfile);

                _logger.LogInformation("Imported new socat profile '{ProfileName}' with ID {ProfileId}", importedProfile.Name, importedProfile.Id);
            }

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
            return importedProfile.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SocatProfile>> ImportProfilesFromJsonAsync(string jsonData, bool overwriteExisting = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            throw new ArgumentException("JSON data cannot be null or empty", nameof(jsonData));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            List<SocatProfile> importedProfiles;
            try
            {
                importedProfiles = JsonSerializer.Deserialize<List<SocatProfile>>(jsonData, options)
                    ?? throw new InvalidOperationException("Failed to deserialize profiles from JSON");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON data: {ex.Message}", nameof(jsonData), ex);
            }

            var resultProfiles = new List<SocatProfile>();

            foreach (var importedProfile in importedProfiles)
            {
                // Validate each imported profile
                var validationErrors = ValidateProfile(importedProfile);
                if (validationErrors.Any())
                {
                    _logger.LogWarning("Skipping invalid profile '{ProfileName}': {Errors}", importedProfile.Name, string.Join(", ", validationErrors));
                    continue;
                }

                // Check if profile with same name exists
                var existingProfile = _profiles.FirstOrDefault(p => string.Equals(p.Name, importedProfile.Name, StringComparison.OrdinalIgnoreCase));
                if (existingProfile != null && !overwriteExisting)
                {
                    _logger.LogWarning("Skipping profile '{ProfileName}' - already exists and overwrite not allowed", importedProfile.Name);
                    continue;
                }

                if (existingProfile != null && overwriteExisting)
                {
                    // Update existing profile
                    if (existingProfile.IsReadOnly)
                    {
                        _logger.LogWarning("Skipping read-only profile '{ProfileName}' - cannot overwrite", importedProfile.Name);
                        continue;
                    }

                    importedProfile.Id = existingProfile.Id;
                    importedProfile.CreatedAt = existingProfile.CreatedAt;
                    importedProfile.ModifiedAt = DateTime.UtcNow;

                    var index = _profiles.IndexOf(existingProfile);
                    _profiles[index] = importedProfile;

                    resultProfiles.Add(importedProfile);
                    _logger.LogInformation("Overwrote existing socat profile '{ProfileName}' with ID {ProfileId}", importedProfile.Name, importedProfile.Id);
                }
                else
                {
                    // Create new profile
                    importedProfile.Id = _nextId++;
                    importedProfile.CreatedAt = DateTime.UtcNow;
                    importedProfile.ModifiedAt = DateTime.UtcNow;
                    importedProfile.IsDefault = false; // Imported profiles are never default

                    _profiles.Add(importedProfile);
                    resultProfiles.Add(importedProfile);

                    _logger.LogInformation("Imported new socat profile '{ProfileName}' with ID {ProfileId}", importedProfile.Name, importedProfile.Id);
                }
            }

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully imported {Count} socat profiles", resultProfiles.Count);
            return resultProfiles.Select(p => p.Clone()).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Validation Operations

    /// <inheritdoc />
    public IEnumerable<string> ValidateProfile(SocatProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        var errors = profile.Validate();

        // Additional service-level validation can be added here

        return errors;
    }

    /// <inheritdoc />
    public async Task<bool> IsProfileNameAvailableAsync(string profileName, int? excludeProfileId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existingProfile = _profiles.FirstOrDefault(p =>
                string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase) &&
                (!excludeProfileId.HasValue || p.Id != excludeProfileId.Value));

            return existingProfile == null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> GetProfileCountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _profiles.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsMaxProfilesReachedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = _settingsService.Settings.Socat;
            return _profiles.Count >= settings.MaxProfiles;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Storage Operations

    /// <inheritdoc />
    public async Task InitializeStorageAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            _logger.LogDebug("Initializing socat profile storage");

            var profilesPath = GetProfilesDirectoryPath();

            _logger.LogDebug("InitializeStorageAsync: profilesPath={ProfilesPath}", profilesPath);

            // Ensure directory exists
            try
            {
                Directory.CreateDirectory(profilesPath);
                _logger.LogDebug("Ensure directory exists: {ProfilesPath}", profilesPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create socat profiles directory: {ProfilesPath}", profilesPath);
                throw new InvalidOperationException($"Failed to create profiles directory: {profilesPath}", ex);
            }

            // Load existing profiles
            _logger.LogDebug("Loading existing socat profiles from {FilePath}", GetProfilesFilePath());
            await LoadProfilesAsync(cancellationToken).ConfigureAwait(false);

            // Ensure default profile exists without re-entering the semaphore
            var defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile == null)
            {
                var systemDefault = SocatProfile.CreateDefaultProfile();
                systemDefault.Id = _nextId++;
                systemDefault.CreatedAt = DateTime.UtcNow;
                systemDefault.ModifiedAt = DateTime.UtcNow;
                _profiles.Add(systemDefault);
                await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Created system default socat profile '{ProfileName}' with ID {ProfileId}", systemDefault.Name, systemDefault.Id);
            }

            _isInitialized = true;
            _logger.LogInformation("Socat profile storage initialized at {ProfilesPath}", profilesPath);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task PerformMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogDebug("Performing socat profile storage maintenance");

            // Validate all profiles and remove invalid ones
            var invalidProfiles = new List<SocatProfile>();
            foreach (var profile in _profiles)
            {
                var errors = ValidateProfile(profile);
                if (errors.Any())
                {
                    invalidProfiles.Add(profile);
                    _logger.LogWarning("Found invalid socat profile '{ProfileName}': {Errors}", profile.Name, string.Join(", ", errors));
                }
            }

            foreach (var invalid in invalidProfiles)
            {
                _profiles.Remove(invalid);
            }

            if (invalidProfiles.Any())
            {
                await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Removed {Count} invalid socat profiles during maintenance", invalidProfiles.Count);
            }

            _logger.LogDebug("Socat profile storage maintenance completed");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>> GetStorageInfoAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profilesPath = GetProfilesDirectoryPath();
            var profilesFile = GetProfilesFilePath();

            var info = new Dictionary<string, object>
            {
                ["ProfilesDirectory"] = profilesPath,
                ["ProfilesFile"] = profilesFile,
                ["ProfileCount"] = _profiles.Count,
                ["DefaultProfile"] = _profiles.FirstOrDefault(p => p.IsDefault)?.Name ?? "None",
                ["MaxProfiles"] = _settingsService.Settings.Socat.MaxProfiles,
                ["DirectoryExists"] = Directory.Exists(profilesPath),
                ["FileExists"] = File.Exists(profilesFile),
                ["IsInitialized"] = _isInitialized,
                ["LastInitialized"] = DateTime.UtcNow
            };

            if (File.Exists(profilesFile))
            {
                var fileInfo = new FileInfo(profilesFile);
                info["FileSize"] = fileInfo.Length;
                info["LastModified"] = fileInfo.LastWriteTime;
            }

            return info;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Ensures the service is initialized.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_isInitialized)
        {
            await InitializeStorageAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the directory path for storing socat profiles.
    /// </summary>
    /// <returns>The profiles directory path.</returns>
    private string GetProfilesDirectoryPath()
    {
        var settings = _settingsService.Settings.Socat;
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return settings.GetAbsoluteProfilesPath(baseDirectory);
    }

    /// <summary>
    /// Gets the file path for the socat profiles JSON file.
    /// </summary>
    /// <returns>The profiles file path.</returns>
    private string GetProfilesFilePath()
    {
        return Path.Combine(GetProfilesDirectoryPath(), "profiles.json");
    }

    /// <summary>
    /// Loads socat profiles from the JSON file.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task LoadProfilesAsync(CancellationToken cancellationToken)
    {
        var filePath = GetProfilesFilePath();

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Socat profiles file does not exist, starting with empty profile list");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var profiles = JsonSerializer.Deserialize<List<SocatProfile>>(json, options);
            if (profiles != null)
            {
                _profiles.Clear();
                _profiles.AddRange(profiles);

                // Update next ID
                if (_profiles.Any())
                {
                    _nextId = _profiles.Max(p => p.Id) + 1;
                }

                _logger.LogInformation("Loaded {Count} socat profiles from {FilePath}", _profiles.Count, filePath);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse socat profiles JSON file: {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to load profiles from {filePath}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load socat profiles from {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to load profiles from {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves socat profiles to the JSON file.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task SaveProfilesAsync(CancellationToken cancellationToken)
    {
        var filePath = GetProfilesFilePath();

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_profiles, options);
            await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Saved {Count} socat profiles to {FilePath}", _profiles.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save socat profiles to {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to save profiles to {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ensures a unique profile name by adding suffixes if necessary.
    /// </summary>
    /// <param name="desiredName">The desired profile name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A unique profile name, or null if user cancelled.</returns>
    private async Task<string?> EnsureUniqueProfileNameAsync(string desiredName, CancellationToken cancellationToken)
    {
        if (await IsProfileNameAvailableAsync(desiredName, null, cancellationToken).ConfigureAwait(false))
        {
            return desiredName;
        }

        // Try automatic naming strategy with suffix
        for (int i = 1; i <= 999; i++)
        {
            var candidateName = $"{desiredName}_{i}";
            if (await IsProfileNameAvailableAsync(candidateName, null, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Profile name '{OriginalName}' conflicts, using '{NewName}'", desiredName, candidateName);
                return candidateName;
            }
        }

        // If automatic strategy fails, use timestamp-based unique name
        var timestampName = $"{desiredName}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (await IsProfileNameAvailableAsync(timestampName, null, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Profile name '{OriginalName}' conflicts, using timestamp-based name '{NewName}'", desiredName, timestampName);
            return timestampName;
        }

        // Last resort: ask user for input via dialog service
        var result = await _dialogService.ShowInputAsync(
            "Profile Name Conflict",
            $"Name '{desiredName}' already exists. Please enter a new name:",
            timestampName,
            "Enter unique profile name"
        ).ConfigureAwait(false);
        if (result.IsCancelled || string.IsNullOrWhiteSpace(result.Value))
        {
            _logger.LogInformation("User cancelled profile name resolution for '{OriginalName}'", desiredName);
            return null;
        }

        return result.Value.Trim();
    }

    /// <summary>
    /// Ensures a unique profile name for updates by adding suffixes if necessary.
    /// </summary>
    /// <param name="desiredName">The desired profile name.</param>
    /// <param name="excludeProfileId">The profile ID to exclude from name checking.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A unique profile name, or null if user cancelled.</returns>
    private async Task<string?> EnsureUniqueProfileNameForUpdateAsync(string desiredName, int excludeProfileId, CancellationToken cancellationToken)
    {
        if (await IsProfileNameAvailableAsync(desiredName, excludeProfileId, cancellationToken).ConfigureAwait(false))
        {
            return desiredName;
        }

        // Try automatic naming strategy with suffix
        for (int i = 1; i <= 999; i++)
        {
            var candidateName = $"{desiredName}_{i}";
            if (await IsProfileNameAvailableAsync(candidateName, excludeProfileId, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Profile name '{OriginalName}' conflicts during update, using '{NewName}'", desiredName, candidateName);
                return candidateName;
            }
        }

        // If automatic strategy fails, use timestamp-based unique name
        var timestampName = $"{desiredName}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (await IsProfileNameAvailableAsync(timestampName, excludeProfileId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Profile name '{OriginalName}' conflicts during update, using timestamp-based name '{NewName}'", desiredName, timestampName);
            return timestampName;
        }

        // Last resort: ask user for input via dialog service
        var inputRequest = new InputRequest(
            "Profile Name Conflict",
            $"Name '{desiredName}' already exists. Please enter a new name:",
            timestampName,
            "Enter unique profile name"
        );

        var result = await _dialogService.ShowInputAsync(
            inputRequest.Title,
            inputRequest.Message,
            inputRequest.DefaultValue,
            inputRequest.Placeholder
        ).ConfigureAwait(false);
        if (result.IsCancelled || string.IsNullOrWhiteSpace(result.Value))
        {
            _logger.LogInformation("User cancelled profile name resolution during update for '{OriginalName}'", desiredName);
            return null;
        }

        return result.Value.Trim();
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Releases all resources used by the SocatProfileService.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the SocatProfileService and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _semaphore?.Dispose();
            _disposed = true;
            _logger.LogDebug("SocatProfileService disposed");
        }
    }

    #endregion
}
