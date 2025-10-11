using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Validation;

/// <summary>
/// Simple validation service for use with dependency injection and testing.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ConcurrentDictionary<Type, object> _validators = new();

    /// <summary>
    /// Validates the specified instance synchronously.
    /// </summary>
    /// <typeparam name="T">The type of instance to validate.</typeparam>
    /// <param name="instance">The instance to validate.</param>
    /// <returns>A validation result indicating success or failure with error details.</returns>
    public ValidationResult Validate<T>(T instance)
    {
        var validator = GetValidator<T>();
        return validator?.Validate(instance!) ?? ValidationResult.Success();
    }

    /// <summary>
    /// Validates the specified instance asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of instance to validate.</typeparam>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation containing the validation result.</returns>
    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var validator = GetValidator<T>();
        return validator != null
            ? await validator.ValidateAsync(instance!, cancellationToken).ConfigureAwait(false)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Registers a validator for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to register the validator for.</typeparam>
    /// <param name="validator">The validator instance to register.</param>
    public void RegisterValidator<T>(IValidator<T> validator)
    {
        _validators[typeof(T)] = validator;
    }

    /// <summary>
    /// Unregisters the validator for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to unregister the validator for.</typeparam>
    /// <returns>True if the validator was successfully removed; otherwise, false.</returns>
    public bool UnregisterValidator<T>()
    {
        return _validators.TryRemove(typeof(T), out _);
    }

    private IValidator<T>? GetValidator<T>()
    {
        if (_validators.TryGetValue(typeof(T), out var validator) && validator is IValidator<T> typed)
        {
            return typed;
        }

        return null;
    }
}
