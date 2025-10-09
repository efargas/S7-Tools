using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for managing socat profiles including CRUD operations, import/export, and validation.
/// This service provides comprehensive profile management capabilities for socat (Serial-to-TCP Proxy) configurations.
/// </summary>
public interface ISocatProfileService
{
    #region Profile CRUD Operations

    /// <summary>
    /// Retrieves all available socat profiles.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all profiles.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the profile storage is not accessible.</exception>
    Task<IEnumerable<SocatProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the profile if found, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is less than or equal to zero.</exception>
    Task<SocatProfile?> GetProfileByIdAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a profile by its name.
    /// </summary>
    /// <param name="profileName">The name of the profile to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the profile if found, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when profileName is null or empty.</exception>
    Task<SocatProfile?> GetProfileByNameAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new socat profile.
    /// </summary>
    /// <param name="profile">The profile to create. The ID will be automatically assigned.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created profile with assigned ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a profile with the same name already exists or maximum profiles limit is reached.</exception>
    Task<SocatProfile> CreateProfileAsync(SocatProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing socat profile.
    /// </summary>
    /// <param name="profile">The profile to update. Must have a valid ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="ArgumentException">Thrown when profile ID is invalid or profile is read-only.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the profile doesn't exist or name conflicts with another profile.</exception>
    Task<SocatProfile> UpdateProfileAsync(SocatProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a socat profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to delete a read-only or default profile.</exception>
    Task<bool> DeleteProfileAsync(int profileId, CancellationToken cancellationToken = default);

    #endregion

    #region Profile Management Operations

    /// <summary>
    /// Creates a duplicate of an existing profile with a new name.
    /// </summary>
    /// <param name="sourceProfileId">The ID of the profile to duplicate.</param>
    /// <param name="newName">The name for the duplicated profile.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the duplicated profile.</returns>
    /// <exception cref="ArgumentException">Thrown when sourceProfileId is invalid or newName is null/empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source profile doesn't exist or a profile with newName already exists.</exception>
    Task<SocatProfile> DuplicateProfileAsync(int sourceProfileId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default profile for new socat connections.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the default profile.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no default profile is found.</exception>
    Task<SocatProfile> GetDefaultProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a profile as the default profile for new socat connections.
    /// </summary>
    /// <param name="profileId">The ID of the profile to set as default.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the profile doesn't exist.</exception>
    Task SetDefaultProfileAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the system default profile exists and creates it if missing.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the system default profile.</returns>
    Task<SocatProfile> EnsureDefaultProfileExistsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Import/Export Operations

    /// <summary>
    /// Exports a profile to JSON format.
    /// </summary>
    /// <param name="profileId">The ID of the profile to export.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the JSON representation of the profile.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the profile doesn't exist.</exception>
    Task<string> ExportProfileToJsonAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports multiple profiles to JSON format.
    /// </summary>
    /// <param name="profileIds">The IDs of the profiles to export.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the JSON representation of the profiles.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profileIds is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when one or more profiles don't exist.</exception>
    Task<string> ExportProfilesToJsonAsync(IEnumerable<int> profileIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all profiles to JSON format.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the JSON representation of all profiles.</returns>
    Task<string> ExportAllProfilesToJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a profile from JSON format.
    /// </summary>
    /// <param name="jsonData">The JSON representation of the profile to import.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing profiles with the same name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the imported profile.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonData is null or invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a profile with the same name exists and overwriteExisting is false.</exception>
    Task<SocatProfile> ImportProfileFromJsonAsync(string jsonData, bool overwriteExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports multiple profiles from JSON format.
    /// </summary>
    /// <param name="jsonData">The JSON representation of the profiles to import.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing profiles with the same names.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the imported profiles.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonData is null or invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when profiles with the same names exist and overwriteExisting is false.</exception>
    Task<IEnumerable<SocatProfile>> ImportProfilesFromJsonAsync(string jsonData, bool overwriteExisting = false, CancellationToken cancellationToken = default);

    #endregion

    #region Validation Operations

    /// <summary>
    /// Validates a profile and returns any validation errors.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <returns>A collection of validation error messages. Empty if the profile is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    IEnumerable<string> ValidateProfile(SocatProfile profile);

    /// <summary>
    /// Checks if a profile name is available (not already in use).
    /// </summary>
    /// <param name="profileName">The profile name to check.</param>
    /// <param name="excludeProfileId">Optional profile ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the name is available.</returns>
    /// <exception cref="ArgumentException">Thrown when profileName is null or empty.</exception>
    Task<bool> IsProfileNameAvailableAsync(string profileName, int? excludeProfileId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of profiles currently stored.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total profile count.</returns>
    Task<int> GetProfileCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the maximum number of profiles has been reached.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the limit has been reached.</returns>
    Task<bool> IsMaxProfilesReachedAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Storage Operations

    /// <summary>
    /// Initializes the profile storage system and ensures required directories exist.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when storage initialization fails.</exception>
    Task InitializeStorageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs maintenance operations on the profile storage (cleanup, optimization, etc.).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PerformMaintenanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the profile storage system.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains storage information.</returns>
    Task<Dictionary<string, object>> GetStorageInfoAsync(CancellationToken cancellationToken = default);

    #endregion
}
