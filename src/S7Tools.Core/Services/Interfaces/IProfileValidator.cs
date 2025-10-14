using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Validation result containing detailed validation information.
/// </summary>
/// <remarks>
/// Provides comprehensive validation feedback for UI display and error handling.
/// Supports both property-level and aggregate-level validation messages.
/// </remarks>
public class ProfileValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the primary validation error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets property-specific validation errors.
    /// </summary>
    /// <remarks>
    /// Key: Property name, Value: Error message for that property.
    /// Used for field-specific validation feedback in UI forms.
    /// </remarks>
    public Dictionary<string, string> PropertyErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warnings that don't prevent operation but should be shown to the user.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ProfileValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The primary error message.</param>
    public static ProfileValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a failed validation result with property-specific errors.
    /// </summary>
    /// <param name="propertyErrors">Property-specific validation errors.</param>
    public static ProfileValidationResult Failure(Dictionary<string, string> propertyErrors) => new()
    {
        IsValid = false,
        PropertyErrors = propertyErrors,
        ErrorMessage = "Validation failed for one or more properties"
    };
}

/// <summary>
/// Unified interface for profile validation operations across all profile types.
/// Provides comprehensive validation, business rule enforcement, and conflict detection.
/// </summary>
/// <typeparam name="T">The profile type that implements IProfileBase.</typeparam>
/// <remarks>
/// This interface encapsulates all validation logic for profile management:
/// - Name uniqueness validation with case-insensitive comparison
/// - Business rule validation (default profile constraints, read-only restrictions)
/// - Data annotation validation support
/// - Async operations for database/file-based validation
///
/// Design principles applied:
/// - Single Responsibility: Focused on validation concerns only
/// - Dependency Inversion: Depends on IProfileBase abstraction
/// - Open/Closed: Extensible for new validation rules without modification
/// </remarks>
public interface IProfileValidator<T> where T : class, IProfileBase
{
    #region Name Validation

    /// <summary>
    /// Validates whether a profile name is unique within the profile type.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="excludeId">Optional profile ID to exclude from uniqueness check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the name is unique, false otherwise.</returns>
    /// <remarks>
    /// Business rules:
    /// - Names are compared case-insensitively
    /// - Leading and trailing whitespace is ignored
    /// - Empty or null names are considered invalid
    /// - ExcludeId is used when updating existing profiles to prevent self-conflict
    /// </remarks>
    Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique name based on the provided base name.
    /// </summary>
    /// <param name="baseName">The base name to make unique.</param>
    /// <param name="excludeId">Optional profile ID to exclude from uniqueness check.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A unique name, potentially with a numeric suffix.</returns>
    /// <exception cref="ArgumentException">Thrown when baseName is null or empty.</exception>
    /// <remarks>
    /// Algorithm:
    /// 1. If baseName is unique, return it as-is
    /// 2. Try baseName_1, baseName_2, etc. until a unique name is found
    /// 3. Limit attempts to prevent infinite loops (e.g., 1000 attempts)
    /// Used for automatic name generation in duplicate operations.
    /// </remarks>
    Task<string> EnsureUniqueNameAsync(string baseName, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a profile name according to business rules and formatting requirements.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>Validation result with detailed error information.</returns>
    /// <remarks>
    /// Validation rules:
    /// - Required: Name cannot be null or empty
    /// - Length: Must not exceed maximum length (typically 100 characters)
    /// - Characters: May restrict certain characters (depends on profile type)
    /// - Format: May enforce naming patterns (depends on business requirements)
    /// </remarks>
    ProfileValidationResult ValidateName(string name);

    #endregion

    #region ID Validation and Assignment

    /// <summary>
    /// Gets the next available ID for a new profile using gap-filling algorithm.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The next available ID, filling gaps when possible.</returns>
    /// <remarks>
    /// Algorithm:
    /// 1. Get all existing IDs in ascending order
    /// 2. Find the first gap in the sequence (starting from 1)
    /// 3. If no gaps exist, return the next sequential ID
    /// 4. Ensures efficient ID usage and prevents ID exhaustion
    /// </remarks>
    Task<int> GetNextAvailableIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a profile ID is available for use.
    /// </summary>
    /// <param name="id">The ID to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the ID is available, false if it's already in use.</returns>
    /// <remarks>
    /// Used for manual ID assignment scenarios or import operations.
    /// IDs must be positive integers greater than 0.
    /// </remarks>
    Task<bool> IsIdAvailableAsync(int id, CancellationToken cancellationToken = default);

    #endregion

    #region Comprehensive Profile Validation

    /// <summary>
    /// Performs comprehensive validation of a profile for create operations.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Detailed validation result.</returns>
    /// <remarks>
    /// Validation includes:
    /// - Name uniqueness and format validation
    /// - Data annotation validation (Required, StringLength, etc.)
    /// - Business rule validation (default profile constraints)
    /// - Profile-specific configuration validation
    /// - Cross-property validation rules
    /// </remarks>
    Task<ProfileValidationResult> ValidateForCreateAsync(T profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs comprehensive validation of a profile for update operations.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Detailed validation result.</returns>
    /// <remarks>
    /// Validation includes:
    /// - Name uniqueness (excluding current profile)
    /// - Read-only protection validation
    /// - Data annotation validation
    /// - Business rule validation
    /// - Change impact validation (e.g., removing default status)
    /// </remarks>
    Task<ProfileValidationResult> ValidateForUpdateAsync(T profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs validation for delete operations.
    /// </summary>
    /// <param name="profileId">The ID of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Detailed validation result.</returns>
    /// <remarks>
    /// Validation includes:
    /// - Profile existence validation
    /// - Read-only protection validation
    /// - Business rule validation (e.g., cannot delete certain system profiles)
    /// - Dependency validation (e.g., profile not in use by other components)
    /// </remarks>
    Task<ProfileValidationResult> ValidateForDeleteAsync(int profileId, CancellationToken cancellationToken = default);

    #endregion

    #region Default Profile Validation

    /// <summary>
    /// Validates default profile assignment and constraints.
    /// </summary>
    /// <param name="profileId">The ID of the profile to set as default.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Detailed validation result.</returns>
    /// <remarks>
    /// Business rules validated:
    /// - Profile must exist
    /// - Only one profile per type can be default
    /// - Some profile types may have restrictions on which profiles can be default
    /// - Impact analysis of changing default profile
    /// </remarks>
    Task<ProfileValidationResult> ValidateDefaultAssignmentAsync(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that default profile constraints are maintained across the profile set.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Detailed validation result.</returns>
    /// <remarks>
    /// Validates:
    /// - Exactly one profile per type is marked as default (or zero if allowed)
    /// - Default profiles meet minimum requirements
    /// - No conflicting default assignments
    /// Used for consistency checks and data integrity validation.
    /// </remarks>
    Task<ProfileValidationResult> ValidateDefaultConsistencyAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Import/Export Validation

    /// <summary>
    /// Validates profiles for import operations with conflict detection.
    /// </summary>
    /// <param name="profiles">The profiles to validate for import.</param>
    /// <param name="replaceExisting">Whether existing profiles should be replaced.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Detailed validation result with import-specific information.</returns>
    /// <remarks>
    /// Validation includes:
    /// - Profile format and data validation
    /// - Name conflict detection and resolution
    /// - ID conflict detection and resolution
    /// - Default profile conflict resolution
    /// - Business rule compliance for all imported profiles
    /// </remarks>
    Task<ProfileValidationResult> ValidateForImportAsync(IEnumerable<T> profiles, bool replaceExisting = false, CancellationToken cancellationToken = default);

    #endregion
}
