namespace S7Tools.Core.Models.Validation;

/// <summary>
/// Represents a validation error for a specific field.
/// </summary>
/// <param name="Field">The field name that failed validation.</param>
/// <param name="Message">The error message describing why validation failed.</param>
public record ValidationError(string Field, string Message);
