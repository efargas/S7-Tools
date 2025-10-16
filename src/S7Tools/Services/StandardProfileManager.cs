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

    /// <summary>
    /// Logger instance for this manager.
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// Semaphore used to ensure thread-safety across profile operations.
    /// </summary>
    protected readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// In-memory cache of profiles. Access must be protected by <see cref="_semaphore"/>.
    /// </summary>
    protected readonly List<T> _profiles = new();

    /// <summary>
    /// Absolute path to the JSON file where profiles are persisted.
    /// </summary>
    protected readonly string _profilesPath;

    /// <summary>
    /// Indicates whether profiles have been loaded into memory.
    /// </summary>
    protected bool _isLoaded;

    /// <summary>
    /// Indicates whether this instance has been disposed.
    /// </summary>
    protected bool _disposed;

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
        Console.WriteLine($"🚀🚀🚀 ENTERED StandardProfileManager.CreateAsync for profile: {profile?.Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"🚀🚀🚀 ENTERED StandardProfileManager.CreateAsync for profile: {profile?.Name ?? "NULL"}");

        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        Console.WriteLine($"📝 Step 1: Logging entry for profile: {profile.Name}");
        _logger.LogInformation("🚀 StandardProfileManager.CreateAsync ENTRY for profile: {ProfileName}", profile.Name);
        _logger.LogInformation("🔒 Waiting for semaphore in CreateAsync...");

        Console.WriteLine($"📝 Step 2: About to wait for semaphore...");
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"📝 Step 3: Semaphore acquired for profile: {profile.Name}");
        _logger.LogInformation("✅ Semaphore acquired in CreateAsync for profile: {ProfileName}", profile.Name);

        try
        {
            Console.WriteLine($"📝 Step 4: About to call EnsureLoadedAsync...");
            _logger.LogInformation("📂 Calling EnsureLoadedAsync...");
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"📝 Step 5: EnsureLoadedAsync completed, profiles count: {_profiles.Count}");
            _logger.LogInformation("✅ EnsureLoadedAsync completed, profiles loaded: {Count}", _profiles.Count);

            // Validate business rules
            Console.WriteLine($"📝 Step 6: Validating profile name: '{profile.Name}'");
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                Console.WriteLine($"❌ ERROR: Profile name is empty!");
                _logger.LogError("Profile name validation failed: name is empty");
                throw new InvalidOperationException("Profile name cannot be empty.");
            }

            // Inline name uniqueness check while holding the semaphore to avoid nested WaitAsync calls
            Console.WriteLine($"📝 Step 7: Checking name uniqueness for '{profile.Name}'...");
            _logger.LogInformation("🔍 Checking name uniqueness for '{ProfileName}'...", profile.Name);
            if (_profiles.Any(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"❌ ERROR: Profile name '{profile.Name}' already exists!");
                _logger.LogError("Profile name uniqueness validation failed: profile with name '{ProfileName}' already exists. Existing profiles: {ExistingNames}",
                    profile.Name, string.Join(", ", _profiles.Select(p => p.Name)));
                throw new InvalidOperationException($"A profile with the name '{profile.Name}' already exists.");
            }
            Console.WriteLine($"📝 Step 8: Name '{profile.Name}' is unique ✓");
            _logger.LogDebug("✅ Name '{ProfileName}' is unique", profile.Name);

            // Clone the profile to avoid modifying the input
            Console.WriteLine($"📝 Step 9: Cloning profile...");
            _logger.LogDebug("Cloning profile...");
            T newProfile = CloneProfile(profile);
            Console.WriteLine($"📝 Step 10: Profile cloned successfully");

            // Assign new ID and timestamps
            Console.WriteLine($"📝 Step 11: Getting next available ID...");
            _logger.LogDebug("Getting next available ID...");
            newProfile.Id = GetNextAvailableIdCore(); // Use non-locking version since we already hold the semaphore
            Console.WriteLine($"📝 Step 12: Assigned ID: {newProfile.Id}");
            _logger.LogDebug("Assigned ID: {ProfileId}", newProfile.Id);

            Console.WriteLine($"📝 Step 13: Setting timestamps and version...");
            newProfile.CreatedAt = DateTime.UtcNow;
            newProfile.ModifiedAt = DateTime.UtcNow;
            newProfile.Version = "1.0";
            Console.WriteLine($"📝 Step 14: Timestamps set ✓");

            // Handle default profile business rule
            Console.WriteLine($"📝 Step 15: Checking if profile is default (IsDefault={newProfile.IsDefault})...");
            if (newProfile.IsDefault)
            {
                Console.WriteLine($"📝 Step 16: Profile is default, clearing other default flags...");
                _logger.LogDebug("Profile is marked as default, clearing other default flags...");
                await ClearAllDefaultFlagsAsync(cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"📝 Step 17: Default flags cleared ✓");
            }
            else
            {
                Console.WriteLine($"📝 Step 16-17: Profile is not default, skipping flag clear");
            }

            // Add to collection
            Console.WriteLine($"📝 Step 18: Adding profile to collection...");
            _logger.LogDebug("Adding profile to collection...");
            _profiles.Add(newProfile);
            Console.WriteLine($"📝 Step 19: Sorting collection...");
            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));
            Console.WriteLine($"📝 Step 20: Profile added, collection now has {_profiles.Count} profiles");
            _logger.LogDebug("Profile added, collection now has {Count} profiles", _profiles.Count);

            // Persist changes
            Console.WriteLine($"📝 Step 21: Saving profiles to disk...");
            _logger.LogDebug("💾 Saving profiles to disk...");
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"📝 Step 22: Profiles saved successfully ✓");
            _logger.LogDebug("✅ Profiles saved successfully");

            Console.WriteLine($"📝 Step 23: Profile creation complete!");
            _logger.LogInformation("✅ Created {ProfileType} profile: {ProfileName} (ID: {ProfileId})",
                ProfileTypeName, newProfile.Name, newProfile.Id);

            Console.WriteLine($"📝 Step 24: Cloning profile for return...");
            T clonedProfile = CloneProfile(newProfile);
            Console.WriteLine($"📝 Step 25: About to return cloned profile with ID: {clonedProfile.Id}");
            return clonedProfile;
        }
        finally
        {
            Console.WriteLine($"📝 Step 26 (FINALLY): Releasing semaphore...");
            _logger.LogDebug("🔓 Releasing semaphore in CreateAsync");
            _semaphore.Release();
            Console.WriteLine($"📝 Step 27 (FINALLY): Semaphore released ✓");
        }
    }

    /// <inheritdoc/>
    public async Task<T> UpdateAsync(T profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (profile.Id <= 0)
        {
            throw new ArgumentException("Profile ID must be greater than zero.", nameof(profile));
        }

        _logger.LogDebug("StandardProfileManager.UpdateAsync called for profile: {ProfileName}, ID: {ProfileId}", profile.Name, profile.Id);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            T? existingProfile = _profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existingProfile == null)
            {
                _logger.LogError("Profile not found: ID {ProfileId}", profile.Id);
                throw new ArgumentException($"Profile with ID {profile.Id} not found.");
            }

            if (!existingProfile.CanModify())
            {
                _logger.LogError("Cannot modify read-only profile: {ProfileName} (ID: {ProfileId})", existingProfile.Name, existingProfile.Id);
                throw new InvalidOperationException("Cannot modify a read-only profile.");
            }

            // Validate business rules
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                _logger.LogError("Profile name validation failed: name is empty for ID {ProfileId}", profile.Id);
                throw new InvalidOperationException("Profile name cannot be empty.");
            }

            // Inline name uniqueness check while holding the semaphore
            if (_profiles.Any(p => p.Id != profile.Id && string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogError("Profile name uniqueness validation failed: profile with name '{ProfileName}' already exists (excluding ID {ProfileId}). Existing profiles: {ExistingNames}",
                    profile.Name, profile.Id, string.Join(", ", _profiles.Select(p => $"{p.Name} (ID: {p.Id})")));
                throw new InvalidOperationException($"A profile with the name '{profile.Name}' already exists.");
            }

            // Clone the profile to avoid modifying the input
            T updatedProfile = CloneProfile(profile);

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
        if (profileId <= 0) {throw new ArgumentException("Profile ID must be greater than zero.", nameof(profileId));}

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            T? profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null) {return false;}

            if (!profile.CanDelete()) {throw new InvalidOperationException("Cannot delete a read-only profile.");}

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
        if (sourceProfileId <= 0) {throw new ArgumentException("Source profile ID must be greater than zero.", nameof(sourceProfileId));}

        if (string.IsNullOrWhiteSpace(newName)) { throw new ArgumentException("New name cannot be empty.", nameof(newName));}

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            T sourceProfile = _profiles.FirstOrDefault(p => p.Id == sourceProfileId) ?? throw new ArgumentException($"Source profile with ID {sourceProfileId} not found.");

            // Ensure the new name is unique - use Core version to avoid deadlock
            string uniqueName = EnsureUniqueNameCore(newName, excludeId: null);

            // Clone the source profile
            T duplicateProfile = CloneProfile(sourceProfile);

            // Assign new identity and clear flags
            duplicateProfile.Id = GetNextAvailableIdCore(); // Use non-locking version since we already hold the semaphore
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
            T? profile = _profiles.FirstOrDefault(p => p.Id == profileId);
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
            T? defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
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
        if (profileId <= 0)
        {
            throw new ArgumentException("Profile ID must be greater than zero.", nameof(profileId));
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            T? profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
            {
                throw new ArgumentException($"Profile with ID {profileId} not found.");
            }

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

            T? defaultProfile = _profiles.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile != null)
            {
                return CloneProfile(defaultProfile);
            }

            // Create system default
            T systemDefault = CreateSystemDefault();
            systemDefault.Id = GetNextAvailableIdCore(); // Use non-locking version since we already hold the semaphore
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
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

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
        {
            throw new ArgumentException("Base name cannot be empty.", nameof(baseName));
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return EnsureUniqueNameCore(baseName, excludeId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Ensures a profile name is unique by appending a counter if necessary.
    /// This is the non-locking version that assumes the caller already holds the semaphore.
    /// </summary>
    /// <param name="baseName">The base name to make unique.</param>
    /// <param name="excludeId">Optional profile ID to exclude from uniqueness check (for updates).</param>
    /// <returns>A unique profile name.</returns>
    private string EnsureUniqueNameCore(string baseName, int? excludeId = null)
    {
        var candidateName = baseName;
        var counter = 1;

        // Check uniqueness directly against the in-memory collection
        // Caller must already hold the semaphore
        while (_profiles.Any(p => p.Id != excludeId && string.Equals(p.Name, candidateName, StringComparison.OrdinalIgnoreCase)))
        {
            candidateName = $"{baseName}_{counter}";
            counter++;

            // Prevent infinite loops
            if (counter > 1000)
            {
                throw new InvalidOperationException("Unable to generate unique name after 1000 attempts.");
            }
        }

        return candidateName;
    }

    /// <inheritdoc/>
    public async Task<int> GetNextAvailableIdAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return GetNextAvailableIdCore();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the next available profile ID without acquiring the semaphore.
    /// This method assumes the caller already holds the semaphore lock.
    /// </summary>
    private int GetNextAvailableIdCore()
    {
        if (!_profiles.Any())
        {
            return 1;
        }

        var existingIds = _profiles.Select(p => p.Id).Where(id => id > 0).OrderBy(id => id).ToList();

        // Find the first gap in the sequence
        for (int i = 1; i <= existingIds.Count + 1; i++)
        {
            if (!existingIds.Contains(i))
            {
                return i;
            }
        }

        return existingIds.Count + 1;
    }

    #endregion

    #region Import/Export Operations

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> ImportAsync(IEnumerable<T> profiles, bool replaceExisting = false, CancellationToken cancellationToken = default)
    {
        if (profiles == null)
        {
            throw new ArgumentNullException(nameof(profiles));
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var importedProfiles = new List<T>();

            foreach (T profile in profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                try
                {
                    T importProfile = CloneProfile(profile);

                    // Handle name conflicts
                    if (!replaceExisting)
                    {
                        // Use Core version to avoid deadlock since we already hold the semaphore
                        importProfile.Name = EnsureUniqueNameCore(importProfile.Name, excludeId: null);
                    }
                    else
                    {
                        // Remove existing profile with same name
                        T? existingProfile = _profiles.FirstOrDefault(p =>
                            string.Equals(p.Name, importProfile.Name, StringComparison.OrdinalIgnoreCase));
                        if (existingProfile != null && existingProfile.CanDelete())
                        {
                            _profiles.Remove(existingProfile);
                        }
                    }

                    // Assign new ID and update timestamps
                    importProfile.Id = GetNextAvailableIdCore(); // Use non-locking version since we already hold the semaphore
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
                _logger.LogInformation("Profile file not found, creating default profiles: {Path}", _profilesPath);
                await CreateDefaultProfilesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var json = await File.ReadAllTextAsync(_profilesPath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogInformation("Profile file is empty, creating default profiles: {Path}", _profilesPath);
                await CreateDefaultProfilesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            List<T>? profiles = JsonSerializer.Deserialize<List<T>>(json, options);
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
                WriteIndented = true,
                IncludeFields = false
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
            foreach (T? profile in _profiles.Where(p => p.IsDefault))
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

    #region Abstract Methods for Default Profiles

    /// <summary>
    /// Creates default profiles when no profiles file exists.
    /// Derived classes should implement this to create appropriate default profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected abstract Task CreateDefaultProfilesAsync(CancellationToken cancellationToken);

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
