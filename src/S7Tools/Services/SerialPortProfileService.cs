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
/// Service for managing serial port profiles with JSON-based persistence, thread-safe operations, and comprehensive error handling.
/// This service provides complete CRUD operations, import/export functionality, and validation for serial port profiles.
/// </summary>
public class SerialPortProfileService : ISerialPortProfileService, IDisposable
{
    private readonly ILogger<SerialPortProfileService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<SerialPortProfile> _profiles = new();
    private bool _isInitialized;
    private bool _disposed;
    private int _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the SerialPortProfileService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="settingsService">The settings service for accessing application settings.</param>
    /// <param name="dialogService">The dialog service for user interactions.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger, settingsService, or dialogService is null.</exception>
    public SerialPortProfileService(
        ILogger<SerialPortProfileService> logger,
        ISettingsService settingsService,
        IDialogService dialogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _logger.LogDebug("SerialPortProfileService initialized");
    }

    #region Profile CRUD Operations

    /// <inheritdoc />
    public async Task<IEnumerable<SerialPortProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogDebug("Retrieved {Count} profiles", _profiles.Count);
            return _profiles.Select(p => p.ClonePreserveId()).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SerialPortProfile?> GetProfileByIdAsync(int profileId, CancellationToken cancellationToken = default)
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
            _logger.LogDebug("Retrieved profile by ID {ProfileId}: {Found}", profileId, profile != null);
            return profile?.ClonePreserveId();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SerialPortProfile?> GetProfileByNameAsync(string profileName, CancellationToken cancellationToken = default)
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
            _logger.LogDebug("Retrieved profile by name '{ProfileName}': {Found}", profileName, profile != null);
            return profile?.ClonePreserveId();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SerialPortProfile> CreateProfileAsync(SerialPortProfile profile, CancellationToken cancellationToken = default)
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
            // Ensure profile name is unique with smart naming strategy
            var uniqueName = await EnsureUniqueProfileNameAsync(profile.Name, cancellationToken).ConfigureAwait(false);
            if (uniqueName == null)
            {
                // User cancelled the operation
                throw new OperationCanceledException("Profile creation was cancelled by the user");
            }

            // Check maximum profiles limit
            var settings = _settingsService.Settings.SerialPorts;
            if (_profiles.Count >= settings.MaxProfiles)
            {
                throw new InvalidOperationException($"Maximum number of profiles ({settings.MaxProfiles}) has been reached");
            }

            // Create new profile with assigned ID
            var newProfile = profile.Clone();
            newProfile.Name = uniqueName; // Use the unique name
            newProfile.Id = _nextId++;
            newProfile.CreatedAt = DateTime.UtcNow;
            newProfile.ModifiedAt = DateTime.UtcNow;
            // If the new profile requests to be the default, clear any existing default flags.
            if (newProfile.IsDefault)
            {
                foreach (var p in _profiles)
                {
                    p.IsDefault = false; // intentionally clear even read-only previous defaults
                }
            }

            _profiles.Add(newProfile);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created new profile '{ProfileName}' with ID {ProfileId}", newProfile.Name, newProfile.Id);
            return newProfile.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SerialPortProfile> UpdateProfileAsync(SerialPortProfile profile, CancellationToken cancellationToken = default)
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
                    // Clear existing defaults - do this regardless of read-only status so only one default remains
                    p.IsDefault = false;
                }
            }

            var index = _profiles.IndexOf(existingProfile);
            _profiles[index] = updatedProfile;

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated profile '{ProfileName}' with ID {ProfileId}", updatedProfile.Name, updatedProfile.Id);
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
                _logger.LogWarning("Attempted to delete non-existent profile with ID {ProfileId}", profileId);
                return false;
            }

            if (profile.IsReadOnly)
            {
                throw new InvalidOperationException("Cannot delete read-only profile");
            }

            if (profile.IsDefault)
            {
                throw new InvalidOperationException("Cannot delete default profile");
            }

            _profiles.Remove(profile);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deleted profile '{ProfileName}' with ID {ProfileId}", profile.Name, profileId);
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
    public async Task<SerialPortProfile> DuplicateProfileAsync(int sourceProfileId, string newName, CancellationToken cancellationToken = default)
    {
        if (sourceProfileId <= 0)
        {
            throw new ArgumentException("Source profile ID must be greater than zero", nameof(sourceProfileId));
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("New name cannot be null or empty", nameof(newName));
        }

        var sourceProfile = await GetProfileByIdAsync(sourceProfileId, cancellationToken).ConfigureAwait(false);
        if (sourceProfile == null)
        {
            throw new InvalidOperationException($"Source profile with ID {sourceProfileId} does not exist");
        }

        // Try to create duplicated profile; if the name already exists, attempt unique variants
        var baseName = newName?.Trim() ?? throw new ArgumentException("New name cannot be null or empty", nameof(newName));
        const int maxAttempts = 50;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // First try the desired base name, then append an underscore and a number on collisions
            var candidateName = attempt == 1 ? baseName : $"{baseName}_{attempt}";
            var duplicatedProfile = sourceProfile.Duplicate(candidateName);

            try
            {
                return await CreateProfileAsync(duplicatedProfile, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                // If name conflict, try next candidate; otherwise rethrow
                if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    // continue to next attempt
                    continue;
                }
                throw;
            }
        }

        throw new InvalidOperationException($"Failed to duplicate profile: could not find an available name after {maxAttempts} attempts");
    }

    /// <inheritdoc />
    public async Task<SerialPortProfile> GetDefaultProfileAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile == null)
            {
                throw new InvalidOperationException("No default profile found");
            }

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
        if (profileId < 0)
        {
            throw new ArgumentException("Profile ID must be greater than or equal to zero", nameof(profileId));
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

            // Clear existing default
            foreach (var p in _profiles)
            {
                p.IsDefault = false;
            }

            // Set new default
            profile.IsDefault = true;
            profile.ModifiedAt = DateTime.UtcNow;

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Set profile '{ProfileName}' with ID {ProfileId} as default", profile.Name, profileId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SerialPortProfile> EnsureDefaultProfileExistsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile != null)
            {
                return defaultProfile.Clone();
            }

            // Create system default profile
            var systemDefault = SerialPortProfile.CreateDefaultProfile();
            systemDefault.Id = _nextId++;
            _profiles.Add(systemDefault);

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created system default profile with ID {ProfileId}", systemDefault.Id);
            return systemDefault.ClonePreserveId();
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
        var profile = await GetProfileByIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile with ID {profileId} does not exist");
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(profile, options);
    }

    /// <inheritdoc />
    public async Task<string> ExportProfilesToJsonAsync(IEnumerable<int> profileIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileIds, nameof(profileIds));

        var profiles = new List<SerialPortProfile>();
        foreach (var id in profileIds)
        {
            var profile = await GetProfileByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (profile == null)
            {
                throw new InvalidOperationException($"Profile with ID {id} does not exist");
            }
            profiles.Add(profile);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(profiles, options);
    }

    /// <inheritdoc />
    public async Task<string> ExportAllProfilesToJsonAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await GetAllProfilesAsync(cancellationToken).ConfigureAwait(false);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(profiles, options);
    }

    /// <inheritdoc />
    public async Task<SerialPortProfile> ImportProfileFromJsonAsync(string jsonData, bool overwriteExisting = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            throw new ArgumentException("JSON data cannot be null or empty", nameof(jsonData));
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var profile = JsonSerializer.Deserialize<SerialPortProfile>(jsonData, options);
            if (profile == null)
            {
                throw new InvalidOperationException("Failed to deserialize profile from JSON");
            }

            // Check if profile with same name exists
            var existingProfile = await GetProfileByNameAsync(profile.Name, cancellationToken).ConfigureAwait(false);
            if (existingProfile != null && !overwriteExisting)
            {
                throw new InvalidOperationException($"A profile with the name '{profile.Name}' already exists");
            }

            if (existingProfile != null && overwriteExisting)
            {
                // Update existing profile
                profile.Id = existingProfile.Id;
                profile.CreatedAt = existingProfile.CreatedAt;
                return await UpdateProfileAsync(profile, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Create new profile
                profile.Id = 0; // Will be assigned during creation
                return await CreateProfileAsync(profile, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON data: {ex.Message}", nameof(jsonData), ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SerialPortProfile>> ImportProfilesFromJsonAsync(string jsonData, bool overwriteExisting = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            throw new ArgumentException("JSON data cannot be null or empty", nameof(jsonData));
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var profiles = JsonSerializer.Deserialize<List<SerialPortProfile>>(jsonData, options);
            if (profiles == null)
            {
                throw new InvalidOperationException("Failed to deserialize profiles from JSON");
            }

            var importedProfiles = new List<SerialPortProfile>();
            foreach (var profile in profiles)
            {
                var importedProfile = await ImportProfileFromJsonAsync(JsonSerializer.Serialize(profile, options), overwriteExisting, cancellationToken).ConfigureAwait(false);
                importedProfiles.Add(importedProfile);
            }

            return importedProfiles;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON data: {ex.Message}", nameof(jsonData), ex);
        }
    }

    #endregion

    #region Validation Operations

    /// <inheritdoc />
    public IEnumerable<string> ValidateProfile(SerialPortProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        return profile.Validate();
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
            return !_profiles.Any(p => p.Id != excludeProfileId && string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase));
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

        var settings = _settingsService.Settings.SerialPorts;
        var currentCount = await GetProfileCountAsync(cancellationToken).ConfigureAwait(false);

        return currentCount >= settings.MaxProfiles;
    }

    #endregion

    #region Storage Operations

    /// <inheritdoc />
    public async Task InitializeStorageAsync(CancellationToken cancellationToken = default)
    {
        // We need to avoid holding the semaphore while calling EnsureDefaultProfileExistsAsync
        // because that method also acquires the same semaphore. To prevent deadlocks we
        // will release the semaphore briefly, call EnsureDefaultProfileExistsAsync, then
        // reacquire to set the _isInitialized flag.

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        var shouldRelease = true;
        try
        {
            if (_isInitialized)
            {
                return;
            }

            var settings = _settingsService.Settings.SerialPorts;
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
                _logger.LogError(ex, "Failed to create profiles directory: {ProfilesPath}", profilesPath);
                throw;
            }

            // Load existing profiles while we still hold the semaphore
            _logger.LogDebug("Loading existing profiles from {FilePath}", GetProfilesFilePath());
            await LoadProfilesAsync(cancellationToken).ConfigureAwait(false);

            // Mark initialized now so EnsureDefaultProfileExistsAsync won't attempt to re-enter initialization
            _isInitialized = true;

            // Release semaphore before ensuring default profile exists to avoid deadlock
            _semaphore.Release();
            shouldRelease = false;

            _logger.LogDebug("Ensuring default profile exists (outside semaphore)");
            await EnsureDefaultProfileExistsAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Profile storage initialized at {ProfilesPath}", profilesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize profile storage");
            throw new InvalidOperationException("Failed to initialize profile storage", ex);
        }
        finally
        {
            if (shouldRelease)
            {
                _semaphore.Release();
            }
        }
    }

    /// <inheritdoc />
    public async Task PerformMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("Starting profile storage maintenance");

            // Update next ID to prevent conflicts
            if (_profiles.Any())
            {
                _nextId = _profiles.Max(p => p.Id) + 1;
            }

            // Save profiles to ensure consistency
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Profile storage maintenance completed");
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
                ["MaxProfiles"] = _settingsService.Settings.SerialPorts.MaxProfiles,
                ["DefaultProfileId"] = _profiles.FirstOrDefault(p => p.IsDefault)?.Id ?? 0,
                ["NextId"] = _nextId,
                ["IsInitialized"] = _isInitialized,
                ["DirectoryExists"] = Directory.Exists(profilesPath),
                ["FileExists"] = File.Exists(profilesFile)
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

    #region Private Methods

    /// <summary>
    /// Ensures the profile name is unique by adding a counter suffix if needed, with user interaction as fallback.
    /// </summary>
    /// <param name="baseName">The base name to make unique.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A unique name, or null if the user cancelled the operation.</returns>
    private async Task<string?> EnsureUniqueProfileNameAsync(string baseName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            throw new ArgumentException("Base name cannot be null or empty", nameof(baseName));
        }

        var candidateName = baseName.Trim();

        // Check if the original name is available
        if (!_profiles.Any(p => string.Equals(p.Name, candidateName, StringComparison.OrdinalIgnoreCase)))
        {
            return candidateName;
        }

        // Try up to 3 iterations with counter suffix
        for (int counter = 1; counter <= 3; counter++)
        {
            candidateName = $"{baseName}_{counter}";
            if (!_profiles.Any(p => string.Equals(p.Name, candidateName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Profile name '{OriginalName}' already exists, using '{UniqueName}' instead", baseName, candidateName);
                return candidateName;
            }
        }

        // After 3 attempts, ask the user for a new name
        _logger.LogWarning("Could not generate unique name for '{BaseName}' after 3 attempts", baseName);

        // Note: In a real UI application, this would show a dialog to the user
        // For now, we'll implement a simple fallback strategy
        return await HandleNameConflictWithUserInteractionAsync(baseName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures the profile name is unique for updates by adding a counter suffix if needed, excluding the current profile.
    /// </summary>
    /// <param name="baseName">The base name to make unique.</param>
    /// <param name="excludeProfileId">The ID of the profile to exclude from the uniqueness check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A unique name, or null if the user cancelled the operation.</returns>
    private async Task<string?> EnsureUniqueProfileNameForUpdateAsync(string baseName, int excludeProfileId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            throw new ArgumentException("Base name cannot be null or empty", nameof(baseName));
        }

        var candidateName = baseName.Trim();

        // Check if the original name is available (excluding the current profile)
        if (!_profiles.Any(p => p.Id != excludeProfileId && string.Equals(p.Name, candidateName, StringComparison.OrdinalIgnoreCase)))
        {
            return candidateName;
        }

        // Try up to 3 iterations with counter suffix
        for (int counter = 1; counter <= 3; counter++)
        {
            candidateName = $"{baseName}_{counter}";
            if (!_profiles.Any(p => p.Id != excludeProfileId && string.Equals(p.Name, candidateName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Profile name '{OriginalName}' already exists for update, using '{UniqueName}' instead", baseName, candidateName);
                return candidateName;
            }
        }

        // After 3 attempts, use the same fallback strategy
        _logger.LogWarning("Could not generate unique name for update '{BaseName}' after 3 attempts", baseName);
        return await HandleNameConflictWithUserInteractionAsync(baseName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles name conflicts by providing user interaction options.
    /// </summary>
    /// <param name="baseName">The base name that has conflicts.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A unique name chosen by the user, or null if cancelled.</returns>
    private async Task<string?> HandleNameConflictWithUserInteractionAsync(string baseName, CancellationToken cancellationToken)
    {
        // Show dialog to user asking for a new name
        var message = $"The profile name '{baseName}' already exists after multiple attempts to generate a unique name.\n\nPlease enter a new name for the profile:";
        var title = "Profile Name Conflict";
        var placeholder = "Enter a new profile name...";

        _logger.LogInformation("Showing name conflict dialog for profile '{BaseName}'", baseName);

        try
        {
            var result = await _dialogService.ShowInputAsync(title, message, baseName, placeholder).ConfigureAwait(false);

            if (result.IsCancelled || string.IsNullOrWhiteSpace(result.Value))
            {
                _logger.LogInformation("User cancelled name conflict resolution for '{BaseName}'", baseName);
                return null; // User cancelled
            }

            var newName = result.Value.Trim();

            // Validate the new name doesn't conflict
            if (_profiles.Any(p => string.Equals(p.Name, newName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("User provided name '{NewName}' also conflicts, showing error dialog", newName);

                // Show error and ask user to try again or cancel
                var errorMessage = $"The name '{newName}' also already exists. Please choose a different name.";
                var retryTitle = "Name Still Conflicts";

                var shouldRetry = await _dialogService.ShowConfirmationAsync(retryTitle, errorMessage + "\n\nWould you like to try again?").ConfigureAwait(false);

                if (shouldRetry)
                {
                    // Recursive call to try again
                    return await HandleNameConflictWithUserInteractionAsync(baseName, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("User chose not to retry after name conflict");
                    return null; // User chose not to retry
                }
            }

            _logger.LogInformation("User provided valid unique name: '{NewName}' for original '{BaseName}'", newName, baseName);
            return newName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during name conflict resolution dialog for '{BaseName}'", baseName);

            // Fallback to timestamp-based approach if dialog fails
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fallbackName = $"{baseName}_{timestamp}";

            if (!_profiles.Any(p => string.Equals(p.Name, fallbackName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Dialog failed, using fallback name: '{FallbackName}'", fallbackName);
                return fallbackName;
            }

            // If even fallback fails, generate random suffix
            var random = new Random();
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var randomSuffix = random.Next(1000, 9999);
                var randomName = $"{baseName}_{timestamp}_{randomSuffix}";

                if (!_profiles.Any(p => string.Equals(p.Name, randomName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Using random fallback name after dialog error: '{RandomName}'", randomName);
                    return randomName;
                }
            }

            _logger.LogError("Could not generate any unique name for '{BaseName}' even after dialog error", baseName);
            return null;
        }
    }

    /// <summary>
    /// Ensures the service is initialized before performing operations.
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
    /// Gets the directory path for storing profiles.
    /// </summary>
    /// <returns>The profiles directory path.</returns>
    private string GetProfilesDirectoryPath()
    {
        var settings = _settingsService.Settings.SerialPorts;
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return settings.GetAbsoluteProfilesPath(baseDirectory);
    }

    /// <summary>
    /// Gets the file path for the profiles JSON file.
    /// </summary>
    /// <returns>The profiles file path.</returns>
    private string GetProfilesFilePath()
    {
        return Path.Combine(GetProfilesDirectoryPath(), "profiles.json");
    }

    /// <summary>
    /// Loads profiles from the JSON file.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task LoadProfilesAsync(CancellationToken cancellationToken)
    {
        var filePath = GetProfilesFilePath();

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Profiles file does not exist, starting with empty profile list");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var profiles = JsonSerializer.Deserialize<List<SerialPortProfile>>(json, options);
            if (profiles != null)
            {
                _profiles.Clear();
                _profiles.AddRange(profiles);

                if (_profiles.Any())
                {
                    _nextId = _profiles.Max(p => p.Id) + 1;
                }

                _logger.LogInformation("Loaded {Count} profiles from {FilePath}", _profiles.Count, filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles from {FilePath}", filePath);
            // Continue with empty profile list
        }
    }

    /// <summary>
    /// Saves profiles to the JSON file.
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
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to a temporary file first, then atomically replace the target file.
            // This reduces the risk of leaving a partially written/corrupted file if the process is killed.
            var tempFile = filePath + ".tmp";

            try
            {
                _logger.LogDebug("Saving profiles: targetFile={FilePath}, tempFile={TempFile}, count={Count}", filePath, tempFile, _profiles.Count);

                await File.WriteAllTextAsync(tempFile, json, cancellationToken).ConfigureAwait(false);

                // Move over the existing file. Use overwrite=true where supported.
                // File.Move with overwrite is atomic on most platforms for same-filesystem moves.
                if (File.Exists(filePath))
                {
                    // Attempt an overwrite move; if not supported, fall back to Replace.
                    try
                    {
                        File.Move(tempFile, filePath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Fallback to File.Replace which provides an atomic replace with backup.
                        var backup = filePath + ".bak";
                        File.Replace(tempFile, filePath, backup, ignoreMetadataErrors: true);
                        if (File.Exists(backup))
                        {
                            try
                            { File.Delete(backup); }
                            catch { }
                        }
                    }
                }
                else
                {
                    File.Move(tempFile, filePath);
                }

                // Log file size and timestamp after move
                try
                {
                    var fi = new FileInfo(filePath);
                    _logger.LogInformation("Saved {Count} profiles to {FilePath} (size={Size}, lastWrite={LastWrite})", _profiles.Count, filePath, fi.Length, fi.LastWriteTimeUtc);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Saved profiles but failed to read file info for {FilePath}", filePath);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Save profiles operation was canceled before completion");
                // Clean up temp file if it exists
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                }

                throw;
            }
            catch (Exception)
            {
                // If an error occurs during the temp->final move, try to remove the temp file and rethrow
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profiles to {FilePath}", filePath);
            throw;
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose pattern implementation.
    /// </summary>
    /// <param name="disposing">True when called from Dispose, false from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // dispose managed resources
            _semaphore?.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
