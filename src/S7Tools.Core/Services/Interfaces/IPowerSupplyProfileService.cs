using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for managing power supply profiles including CRUD operations, import/export, and validation.
/// This service provides comprehensive profile management capabilities for power supply device configurations.
/// </summary>
public interface IPowerSupplyProfileService
{
    #region Profile CRUD Operations

    /// <summary>
    /// Retrieves all available power supply profiles.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all profiles.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the profile storage is not accessible.</exception>
    Task<IEnumerable<PowerSupplyProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the profile if found, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is less than or equal to zero.</exception>
    Task<PowerSupplyProfile?> GetProfileByIdAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a profile by its name.
    /// </summary>
    /// <param name="profileName">The name of the profile to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the profile if found, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when profileName is null or empty.</exception>
    Task<PowerSupplyProfile?> GetProfileByNameAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new power supply profile.
    /// </summary>
    /// <param name="profile">The profile to create. The ID will be automatically assigned.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created profile with assigned ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a profile with the same name already exists or maximum profiles limit is reached.</exception>
    Task<PowerSupplyProfile> CreateProfileAsync(PowerSupplyProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing power supply profile.
    /// </summary>
    /// <param name="profile">The profile to update. Must have a valid ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="ArgumentException">Thrown when profile ID is invalid or profile is read-only.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the profile doesn't exist or name conflicts with another profile.</exception>
    Task<PowerSupplyProfile> UpdateProfileAsync(PowerSupplyProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a power supply profile by its unique identifier.
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
    /// <param name="profileId">The unique identifier of the profile to duplicate.</param>
    /// <param name="newName">The name for the duplicated profile.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the duplicated profile.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is invalid or newName is null/empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source profile doesn't exist or newName already exists.</exception>
    Task<PowerSupplyProfile> DuplicateProfileAsync(int profileId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a profile as the default profile.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to set as default.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the profile doesn't exist.</exception>
    /// <remarks>Only one profile can be marked as default at a time. Setting a new default will unmark the current default profile.</remarks>
    Task SetDefaultProfileAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current default profile.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the default profile if one is set, otherwise null.</returns>
    Task<PowerSupplyProfile?> GetDefaultProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile name is available (not already in use).
    /// </summary>
    /// <param name="profileName">The profile name to check.</param>
    /// <param name="excludeProfileId">Optional profile ID to exclude from the check (useful when updating a profile).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the name is available.</returns>
    /// <exception cref="ArgumentException">Thrown when profileName is null or empty.</exception>
    Task<bool> IsProfileNameAvailableAsync(string profileName, int? excludeProfileId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Import/Export Operations

    /// <summary>
    /// Exports all profiles to a JSON file.
    /// </summary>
    /// <param name="filePath">The file path where profiles should be exported.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
    Task ExportProfilesToFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports profiles from a JSON file.
    /// </summary>
    /// <param name="filePath">The file path from which profiles should be imported.</param>
    /// <param name="replaceExisting">If true, replaces existing profiles; if false, merges with existing profiles.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of profiles imported.</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file format is invalid.</exception>
    Task<int> ImportProfilesFromFileAsync(string filePath, bool replaceExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports profiles to a JSON string.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the JSON string representation of all profiles.</returns>
    Task<string> ExportProfilesToJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports profiles from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing profiles to import.</param>
    /// <param name="replaceExisting">If true, replaces existing profiles; if false, merges with existing profiles.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of profiles imported.</returns>
    /// <exception cref="ArgumentException">Thrown when json is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the JSON format is invalid.</exception>
    Task<int> ImportProfilesFromJsonAsync(string json, bool replaceExisting = false, CancellationToken cancellationToken = default);

    #endregion

    #region Validation Operations

    /// <summary>
    /// Validates a profile and returns any validation errors.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of validation error messages, or an empty list if the profile is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    Task<List<string>> ValidateProfileAsync(PowerSupplyProfile profile, CancellationToken cancellationToken = default);

    #endregion
}
