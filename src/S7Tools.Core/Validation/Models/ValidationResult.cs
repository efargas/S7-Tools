namespace S7Tools.Core.Validation.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation errors (empty if IsValid is true).
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether validation passed.</param>
    /// <param name="errors">The validation errors.</param>
    private ValidationResult(bool isValid, IReadOnlyList<ValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors ?? Array.Empty<ValidationError>();
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A ValidationResult indicating success.</returns>
    public static ValidationResult Success() => new(true, Array.Empty<ValidationError>());

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A ValidationResult indicating failure.</returns>
    public static ValidationResult Failure(params ValidationError[] errors)
        => new(false, errors ?? Array.Empty<ValidationError>());

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A ValidationResult indicating failure.</returns>
    public static ValidationResult Failure(string field, string message)
        => new(false, new[] { new ValidationError(field, message) });
}
