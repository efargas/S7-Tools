using System;
using System.Collections.Generic;
using System.Linq;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when validation of input data fails.
/// Can contain multiple validation errors for comprehensive error reporting.
/// </summary>
public class ValidationException : S7ToolsException
{
    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Gets the name of the property or field that failed validation, if applicable.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a single error message.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public ValidationException(string message)
        : base(message)
    {
        ValidationErrors = new List<string> { message };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with multiple validation errors.
    /// </summary>
    /// <param name="validationErrors">The collection of validation error messages.</param>
    public ValidationException(IEnumerable<string> validationErrors)
        : base(BuildMessage(validationErrors))
    {
        ValidationErrors = validationErrors?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a property name and error message.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    public ValidationException(string propertyName, string message)
        : base($"{propertyName}: {message}")
    {
        PropertyName = propertyName;
        ValidationErrors = new List<string> { message };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ValidationErrors = new List<string> { message };
    }

    /// <summary>
    /// Builds a composite error message from multiple validation errors.
    /// </summary>
    /// <param name="errors">The collection of validation errors.</param>
    /// <returns>A formatted error message.</returns>
    private static string BuildMessage(IEnumerable<string>? errors)
    {
        if (errors == null || !errors.Any())
        {
            return "Validation failed.";
        }

        var errorList = errors.ToList();
        if (errorList.Count == 1)
        {
            return errorList[0];
        }

        return $"Validation failed with {errorList.Count} error(s): {string.Join("; ", errorList)}";
    }
}
