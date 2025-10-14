using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Unified interface for profile management operations across all profile types.
/// Provides consistent CRUD operations, validation, and business rule enforcement.
/// </summary>
/// <typeparam name="T">The profile type that implements IProfileBase.</typeparam>
/// <remarks>
/// This interface establishes a unified contract for profile management that:
/// - Ensures consistent behavior across SerialPortProfile, SocatProfile, and PowerSupplyProfile
/// - Encapsulates business rules (uniqueness, default profile constraints, ID assignment)
/// - Provides async operations for scalability
/// - Supports comprehensive error handling and validation
///
/// Design principles applied:
/// - Single Responsibility: Manages profiles of a specific type
/// - Open/Closed: Open for extension (new profile types), closed for modification
/// - Dependency Inversion: Depends on IProfileBase abstraction
/// - Interface Segregation: Focused interface for profile management operations
/// </remarks>
public interface IProfileManager<T> where T : class, IProfileBase
{
    #region Profile CRUD Operations

    /// <summary>
    /// Creates a new profile with business rule validation and ID assignment.
    /// </summary>
    /// <param name="profile">The profile to create. ID will be auto-assigned.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The created profile with assigned ID and timestamps.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when profile name already exists or validation fails.</exception>
    /// <remarks>
    /// Business rules enforced:
    /// - Profile name must be unique within the profile type
    /// - ID is auto-assigned to the first available value (gap-filling)
    /// - CreatedAt and ModifiedAt timestamps are set to current UTC time
    /// - If profile is marked as default, existing default flags are cleared
    /// </remarks>
    Task<T> CreateAsync(T profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing profile with validation and timestamp management.
    /// </summary>
    /// <param name="profile">The profile to update. ID must match an existing profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The updated profile with current ModifiedAt timestamp.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="ArgumentException">Thrown when profile ID is invalid or profile doesn't exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when profile is read-only or validation fails.</exception>
    /// <remarks>
    /// Business rules enforced:
    /// - Read-only profiles cannot be updated
    /// - Profile name must be unique within the profile type (excluding current profile)
    /// - ModifiedAt timestamp is updated to current UTC time
    /// - If profile is marked as default, existing default flags are cleared
    /// </remarks>
    Task<T> UpdateAsync(T profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile with business rule validation.
    /// </summary>
    /// <param name="profileId">The ID of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the profile was deleted, false if it didn't exist.</returns>
    /// <exception cref="ArgumentException">Thrown when profileId is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when profile is read-only or cannot be deleted.</exception>
    /// <remarks>
    /// Business rules enforced:
    /// - Read-only profiles cannot be deleted
    /// - Some implementations may prevent deletion of default profiles
    /// - Profile must exist to be deleted
    /// </remarks>
    Task<bool> DeleteAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a duplicate of an existing profile with a new name and ID.
    /// </summary>
    /// <param name="sourceProfileId">The ID of the profile to duplicate.</param>
    /// <param name="newName">The name for the duplicated profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The duplicated profile with new ID and name.</returns>
    /// <exception cref="ArgumentException">Thrown when sourceProfileId is invalid or newName is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when source profile doesn't exist or validation fails.</exception>
    /// <remarks>
    /// Business rules enforced:
    /// - Source profile must exist
    /// - New name must be unique within the profile type
    /// - New ID is auto-assigned to the first available value
    /// - Duplicated profile is never marked as default or read-only
    /// - CreatedAt and ModifiedAt are set to current UTC time
    /// </remarks>
    Task<T> DuplicateAsync(int sourceProfileId, string newName, CancellationToken cancellationToken = default);

    #endregion

    #region Profile Query Operations

    /// <summary>
    /// Retrieves all profiles of this type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A collection of all profiles, cloned for safety.</returns>
    /// <remarks>
    /// Returns cloned profiles to prevent accidental modification of service state.
    /// Profiles are returned in ascending order by ID.
    /// </remarks>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a profile by its ID.
    /// </summary>
    /// <param name="profileId">The ID of the profile to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The profile if found, null otherwise.</returns>
    /// <remarks>
    /// Returns a cloned profile to prevent accidental modification of service state.
    /// </remarks>
    Task<T?> GetByIdAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the default profile for this type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The default profile if one exists, null otherwise.</returns>
    /// <remarks>
    /// Business rule: Only one profile per type can be marked as default.
    /// Returns a cloned profile to prevent accidental modification of service state.
    /// </remarks>
    Task<T?> GetDefaultAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Default Profile Management

    /// <summary>
    /// Sets a profile as the default for this type.
    /// </summary>
    /// <param name="profileId">The ID of the profile to set as default.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <exception cref="ArgumentException">Thrown when profileId is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when profile doesn't exist.</exception>
    /// <remarks>
    /// Business rule: Only one profile per type can be marked as default.
    /// Automatically clears the default flag from any existing default profile.
    /// Updates the ModifiedAt timestamp of the affected profiles.
    /// </remarks>
    Task SetDefaultAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a default profile exists for this type, creating one if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The default profile, either existing or newly created.</returns>
    /// <remarks>
    /// Creates a system default profile if no default exists.
    /// The created profile uses standard naming: "SerialDefault", "SocatDefault", "PowerSupplyDefault".
    /// System default profiles are typically marked as read-only.
    /// </remarks>
    Task<T> EnsureDefaultExistsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Validation Operations

    /// <summary>
    /// Validates whether a profile name is unique within this profile type.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="excludeId">Optional profile ID to exclude from the uniqueness check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the name is unique, false otherwise.</returns>
    /// <remarks>
    /// Used for real-time validation in create and edit operations.
    /// Case-insensitive comparison is used for name uniqueness.
    /// </remarks>
    Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique name based on the provided base name.
    /// </summary>
    /// <param name="baseName">The base name to make unique.</param>
    /// <param name="excludeId">Optional profile ID to exclude from the uniqueness check.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A unique name, potentially with a numeric suffix.</returns>
    /// <remarks>
    /// If the base name is unique, it's returned as-is.
    /// Otherwise, numeric suffixes are tried: "Name_1", "Name_2", etc.
    /// Used for automatic name generation in duplicate operations.
    /// </remarks>
    Task<string> EnsureUniqueNameAsync(string baseName, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next available ID for a new profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The next available ID, filling gaps when possible.</returns>
    /// <remarks>
    /// Implements gap-filling algorithm: finds the first unused ID starting from 1.
    /// Ensures efficient ID usage and prevents ID exhaustion.
    /// </remarks>
    Task<int> GetNextAvailableIdAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Import/Export Operations

    /// <summary>
    /// Imports profiles from an external source with validation and conflict resolution.
    /// </summary>
    /// <param name="profiles">The profiles to import.</param>
    /// <param name="replaceExisting">Whether to replace existing profiles with the same name.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A collection of successfully imported profiles.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profiles is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when validation fails or conflicts occur.</exception>
    /// <remarks>
    /// Business rules enforced:
    /// - Profile names must be unique (or conflicts resolved based on replaceExisting)
    /// - IDs are reassigned to avoid conflicts
    /// - Imported profiles are validated according to profile type rules
    /// - Default profile constraints are maintained
    /// </remarks>
    Task<IEnumerable<T>> ImportAsync(IEnumerable<T> profiles, bool replaceExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all profiles for backup or transfer purposes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A collection of all profiles with preserved IDs and metadata.</returns>
    /// <remarks>
    /// Returns profiles with all original metadata preserved for accurate restoration.
    /// Used for backup, transfer, and synchronization scenarios.
    /// </remarks>
    Task<IEnumerable<T>> ExportAsync(CancellationToken cancellationToken = default);

    #endregion
}
