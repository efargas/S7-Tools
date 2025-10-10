using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Validation;

/// <summary>
/// Represents a validation result.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = new List<ValidationError>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(params ValidationError[] errors) =>
        new() { IsValid = false, Errors = errors };

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(string propertyName, string errorMessage, string? errorCode = null) =>
        new() { IsValid = false, Errors = new[] { new ValidationError(propertyName, errorMessage, errorCode) } };
}

/// <summary>
/// Represents a validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    public ValidationError(string propertyName, string errorMessage, string? errorCode = null)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string? ErrorCode { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{PropertyName}: {ErrorMessage}";
}

/// <summary>
/// Defines a validator for objects of type T.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the specified object.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(T instance);

    /// <summary>
    /// Validates the specified object asynchronously.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a validator that can validate multiple types.
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Determines whether this validator can validate the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>true if the validator can validate the type; otherwise, false.</returns>
    bool CanValidate(Type type);

    /// <summary>
    /// Validates the specified object.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(object instance);

    /// <summary>
    /// Validates the specified object asynchronously.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    Task<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a validation service that can validate objects using registered validators.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates the specified object using the appropriate validator.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="instance">The object to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate<T>(T instance);

    /// <summary>
    /// Validates the specified object asynchronously using the appropriate validator.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="instance">The object to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a validator for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <param name="validator">The validator to register.</param>
    void RegisterValidator<T>(IValidator<T> validator);

    /// <summary>
    /// Unregisters the validator for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to unregister the validator for.</typeparam>
    /// <returns>true if the validator was removed; otherwise, false.</returns>
    bool UnregisterValidator<T>();
}
