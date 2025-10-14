using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Standard implementation of IProfileManager&lt;T&gt; providing unified profile management functionality.
/// This class implements the complete IProfileManager contract with proper business rule enforcement,
/// thread safety, and consistent error handling patterns.
/// </summary>
/// <typeparam name="T">The profile type that implements IProfileBase.</typeparam>
/// <remarks>
/// This is a completely new standardized implementation that replaces all legacy profile services.
/// It provides:
/// - Thread-safe operations with SemaphoreSlim
/// - Comprehensive business rule enforcement
/// - Gap-filling ID assignment
/// - Automatic name uniqueness resolution
/// - JSON-based persistence with proper error handling
/// - Audit trail maintenance (CreatedAt/ModifiedAt)
/// - Default profile management
/// </remarks>
public abstract class StandardProfileManager<T> : IProfileManager<T>, IDisposable where T : class, IProfileBase, new()
{
    #region Private Fields

    protected readonly ILogger _logger;
    protected readonly SemaphoreSlim _semaphore = new(1, 1);
    protected readonly List<T> _profiles = new();
    protected readonly string _profilesPath;
    protected bool _isLoaded = false;
    protected bool _disposed = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the StandardProfileManager class.
    /// </summary>
    /// <param name="profilesPath">The file path where profiles are persisted.</param>
    /// <param name="logger">The logger instance for this manager.</param>
    protected StandardProfileManager(string profilesPath, ILogger logger)
    {
        _profilesPath = profilesPath ?? throw new ArgumentNullException(nameof(profilesPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(_profilesPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Creates a system default profile for this profile type.
    /// </summary>
    /// <returns>A new default profile with appropriate default values.</returns>
    protected abstract T CreateSystemDefault();

    /// <summary>
    /// Gets the display name for this profile type (used in logging and error messages).
    /// </summary>
    protected abstract string ProfileTypeName { get; }

    #endregion

    #region Profile CRUD Operations

    /// <inheritdoc/>
    public async Task<T> CreateAsync(T profile, CancellationToken cancellationToken = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            // Validate business rules
            if (string.IsNullOrWhiteSpace(profile.Name))
                throw new InvalidOperationException("Profile name cannot be empty.");

            if (!await IsNameUniqueAsync(profile.Name, excludeId: null, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException($"A profile with the name '{profile.Name}' already exists.");

            // Clone the profile to avoid modifying the input
            var newProfile = CloneProfile(profile);

            // Assign new ID and timestamps
            newProfile.Id = await GetNextAvailableIdAsync(cancellationToken).ConfigureAwait(false);
            newProfile.CreatedAt = DateTime.UtcNow;
            newProfile.ModifiedAt = DateTime.UtcNow;
            newProfile.Version = "1.0";

            // Handle default profile business rule
            if (newProfile.IsDefault)
            {
                await ClearAllDefaultFlagsAsync(cancellationToken).ConfigureAwait(false);
            }

            // Add to collection
            _profiles.Add(newProfile);
            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));

            // Persist changes
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created {ProfileType} profile: {ProfileName} (ID: {ProfileId})",
                ProfileTypeName, newProfile.Name, newProfile.Id);

            return CloneProfile(newProfile);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T> UpdateAsync(T profile, CancellationToken cancellationToken = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (profile.Id <= 0) throw new ArgumentException("Profile ID must be greater than zero.", nameof(profile));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var existingProfile = _profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existingProfile == null)
                throw new ArgumentException($"Profile with ID {profile.Id} not found.");

            if (!existingProfile.CanModify())
                throw new InvalidOperationException("Cannot modify a read-only profile.");

            // Validate business rules
            if (string.IsNullOrWhiteSpace(profile.Name))
                throw new InvalidOperationException("Profile name cannot be empty.");

            if (!await IsNameUniqueAsync(profile.Name, excludeId: profile.Id, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException($"A profile with the name '{profile.Name}' already exists.");

            // Clone the profile to avoid modifying the input
            var updatedProfile = CloneProfile(profile);

            // Preserve immutable properties
            updatedProfile.Id = existingProfile.Id;
            updatedProfile.CreatedAt = existingProfile.CreatedAt;
            updatedProfile.ModifiedAt = DateTime.UtcNow;

            // Handle default profile business rule
            if (updatedProfile.IsDefault && !existingProfile.IsDefault)
            {
                await ClearAllDefaultFlagsAsync(cancellationToken).ConfigureAwait(false);
            }

            // Replace the existing profile
            var index = _profiles.IndexOf(existingProfile);
            _profiles[index] = updatedProfile;

            // Persist changes
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated {ProfileType} profile: {ProfileName} (ID: {ProfileId})",
                ProfileTypeName, updatedProfile.Name, updatedProfile.Id);

            return CloneProfile(updatedProfile);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(int profileId, CancellationToken cancellationToken = default)
    {
        if (profileId <= 0) throw new ArgumentException("Profile ID must be greater than zero.", nameof(profileId));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null) return false;

            if (!profile.CanDelete())
                throw new InvalidOperationException("Cannot delete a read-only profile.");

            _profiles.Remove(profile);

            // Persist changes
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deleted {ProfileType} profile: {ProfileName} (ID: {ProfileId})",
                ProfileTypeName, profile.Name, profile.Id);

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T> DuplicateAsync(int sourceProfileId, string newName, CancellationToken cancellationToken = default)
    {
        if (sourceProfileId <= 0) throw new ArgumentException("Source profile ID must be greater than zero.", nameof(sourceProfileId));
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("New name cannot be empty.", nameof(newName));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var sourceProfile = _profiles.FirstOrDefault(p => p.Id == sourceProfileId);
            if (sourceProfile == null)
                throw new ArgumentException($"Source profile with ID {sourceProfileId} not found.");

            // Ensure the new name is unique
            var uniqueName = await EnsureUniqueNameAsync(newName, excludeId: null, cancellationToken).ConfigureAwait(false);

            // Clone the source profile
            var duplicateProfile = CloneProfile(sourceProfile);

            // Assign new identity and clear flags
            duplicateProfile.Id = await GetNextAvailableIdAsync(cancellationToken).ConfigureAwait(false);
            duplicateProfile.Name = uniqueName;
            duplicateProfile.IsDefault = false; // Duplicates are never default
            duplicateProfile.IsReadOnly = false; // Duplicates are never read-only
            duplicateProfile.CreatedAt = DateTime.UtcNow;
            duplicateProfile.ModifiedAt = DateTime.UtcNow;

            // Add to collection
            _profiles.Add(duplicateProfile);
            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));

            // Persist changes
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Duplicated {ProfileType} profile: {SourceName} -> {NewName} (ID: {ProfileId})",
                ProfileTypeName, sourceProfile.Name, duplicateProfile.Name, duplicateProfile.Id);

            return CloneProfile(duplicateProfile);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Profile Query Operations

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return _profiles.Select(CloneProfile).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetByIdAsync(int profileId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            return profile != null ? CloneProfile(profile) : null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            var defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            return defaultProfile != null ? CloneProfile(defaultProfile) : null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Default Profile Management

    /// <inheritdoc/>
    public async Task SetDefaultAsync(int profileId, CancellationToken cancellationToken = default)
    {
        if (profileId <= 0) throw new ArgumentException("Profile ID must be greater than zero.", nameof(profileId));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
                throw new ArgumentException($"Profile with ID {profileId} not found.");

            // Clear all default flags first
            await ClearAllDefaultFlagsAsync(cancellationToken).ConfigureAwait(false);

            // Set the new default
            profile.IsDefault = true;
            profile.ModifiedAt = DateTime.UtcNow;

            // Persist changes
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Set {ProfileType} profile as default: {ProfileName} (ID: {ProfileId})",
                ProfileTypeName, profile.Name, profile.Id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T> EnsureDefaultExistsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile != null)
            {
                return CloneProfile(defaultProfile);
            }

            // Create system default
            var systemDefault = CreateSystemDefault();
            systemDefault.Id = await GetNextAvailableIdAsync(cancellationToken).ConfigureAwait(false);
            systemDefault.IsDefault = true;
            systemDefault.IsReadOnly = true;
            systemDefault.CreatedAt = DateTime.UtcNow;
            systemDefault.ModifiedAt = DateTime.UtcNow;
            systemDefault.Version = "1.0";

            _profiles.Add(systemDefault);
            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created system default {ProfileType} profile: {ProfileName} (ID: {ProfileId})",
                ProfileTypeName, systemDefault.Name, systemDefault.Id);

            return CloneProfile(systemDefault);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Validation Operations

    /// <inheritdoc/>
    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return !_profiles.Any(p => p.Id != excludeId &&
                                      string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string> EnsureUniqueNameAsync(string baseName, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("Base name cannot be empty.", nameof(baseName));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var candidateName = baseName;
            var counter = 1;

            while (!await IsNameUniqueAsync(candidateName, excludeId, cancellationToken).ConfigureAwait(false))
            {
                candidateName = $"{baseName}_{counter}";
                counter++;

                // Prevent infinite loops
                if (counter > 1000)
                    throw new InvalidOperationException("Unable to generate unique name after 1000 attempts.");
            }

            return candidateName;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetNextAvailableIdAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            if (!_profiles.Any()) return 1;

            var existingIds = _profiles.Select(p => p.Id).Where(id => id > 0).OrderBy(id => id).ToList();

            // Find the first gap in the sequence
            for (int i = 1; i <= existingIds.Count + 1; i++)
            {
                if (!existingIds.Contains(i))
                    return i;
            }

            return existingIds.Count + 1;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Import/Export Operations

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> ImportAsync(IEnumerable<T> profiles, bool replaceExisting = false, CancellationToken cancellationToken = default)
    {
        if (profiles == null) throw new ArgumentNullException(nameof(profiles));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var importedProfiles = new List<T>();

            foreach (var profile in profiles)
            {
                if (profile == null) continue;

                try
                {
                    var importProfile = CloneProfile(profile);

                    // Handle name conflicts
                    if (!replaceExisting)
                    {
                        importProfile.Name = await EnsureUniqueNameAsync(importProfile.Name, excludeId: null, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Remove existing profile with same name
                        var existingProfile = _profiles.FirstOrDefault(p =>
                            string.Equals(p.Name, importProfile.Name, StringComparison.OrdinalIgnoreCase));
                        if (existingProfile != null && existingProfile.CanDelete())
                        {
                            _profiles.Remove(existingProfile);
                        }
                    }

                    // Assign new ID and update timestamps
                    importProfile.Id = await GetNextAvailableIdAsync(cancellationToken).ConfigureAwait(false);
                    importProfile.CreatedAt = DateTime.UtcNow;
                    importProfile.ModifiedAt = DateTime.UtcNow;
                    importProfile.IsDefault = false; // Imported profiles are never default
                    importProfile.IsReadOnly = false; // Imported profiles are never read-only

                    _profiles.Add(importProfile);
                    importedProfiles.Add(CloneProfile(importProfile));

                    _logger.LogInformation("Imported {ProfileType} profile: {ProfileName} (ID: {ProfileId})",
                        ProfileTypeName, importProfile.Name, importProfile.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to import {ProfileType} profile: {ProfileName}",
                        ProfileTypeName, profile.Name);
                }
            }

            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Imported {Count} {ProfileType} profiles", importedProfiles.Count, ProfileTypeName);

            return importedProfiles;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> ExportAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return _profiles.Select(CloneProfile).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Ensures that profiles are loaded from storage.
    /// </summary>
    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (!_isLoaded)
        {
            await LoadProfilesAsync(cancellationToken).ConfigureAwait(false);
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Loads profiles from the JSON file.
    /// </summary>
    private async Task LoadProfilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_profilesPath))
            {
                _logger.LogInformation("Profile file not found, starting with empty collection: {Path}", _profilesPath);
                return;
            }

            var json = await File.ReadAllTextAsync(_profilesPath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogInformation("Profile file is empty, starting with empty collection: {Path}", _profilesPath);
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var profiles = JsonSerializer.Deserialize<List<T>>(json, options);
            if (profiles != null)
            {
                _profiles.Clear();
                _profiles.AddRange(profiles);
                _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));

                _logger.LogInformation("Loaded {Count} {ProfileType} profiles from: {Path}",
                    _profiles.Count, ProfileTypeName, _profilesPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {ProfileType} profiles from: {Path}", ProfileTypeName, _profilesPath);
            // Don't rethrow - start with empty collection
        }
    }

    /// <summary>
    /// Saves profiles to the JSON file.
    /// </summary>
    private async Task SaveProfilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_profiles, options);
            await File.WriteAllTextAsync(_profilesPath, json, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Saved {Count} {ProfileType} profiles to: {Path}",
                _profiles.Count, ProfileTypeName, _profilesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {ProfileType} profiles to: {Path}", ProfileTypeName, _profilesPath);
            throw;
        }
    }

    /// <summary>
    /// Clears the default flag from all profiles.
    /// </summary>
    private async Task ClearAllDefaultFlagsAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            foreach (var profile in _profiles.Where(p => p.IsDefault))
            {
                profile.IsDefault = false;
                profile.ModifiedAt = DateTime.UtcNow;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a deep clone of a profile using JSON serialization.
    /// </summary>
    private static T CloneProfile(T profile)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(profile, options);
        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the manager and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the manager and releases resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
