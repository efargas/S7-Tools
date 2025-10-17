using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Validation;

namespace S7Tools.Services;

/// <summary>
/// Implementation of validation service that manages validators for different types.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly ConcurrentDictionary<Type, object> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ValidationService(ILogger<ValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validators = new ConcurrentDictionary<Type, object>();
    }

    /// <inheritdoc/>
    public ValidationResult Validate<T>(T instance)
    {
        if (instance == null)
        {
            _logger.LogWarning("Validation instance is null for type {Type}", typeof(T).Name);
            return ValidationResult.Failure("Instance", "Instance cannot be null", "NULL_INSTANCE");
        }

        var type = typeof(T);

        if (!_validators.TryGetValue(type, out var validatorObj))
        {
            _logger.LogWarning("No validator registered for type {Type}", type.Name);
            return ValidationResult.Success(); // No validator means no validation errors
        }

        if (validatorObj is not IValidator<T> validator)
        {
            _logger.LogError("Invalid validator type for {Type}", type.Name);
            return ValidationResult.Failure("Validator", "Invalid validator configuration", "INVALID_VALIDATOR");
        }

        try
        {
            _logger.LogDebug("Validating instance of type {Type}", type.Name);
            var result = validator.Validate(instance);

            _logger.LogDebug("Validation completed for {Type}. Valid: {IsValid}, Errors: {ErrorCount}",
                type.Name, result.IsValid, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation for type {Type}", type.Name);
            return ValidationResult.Failure("Validation", "Validation failed due to an error", "VALIDATION_ERROR");
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        if (instance == null)
        {
            _logger.LogWarning("Validation instance is null for type {Type}", typeof(T).Name);
            return ValidationResult.Failure("Instance", "Instance cannot be null", "NULL_INSTANCE");
        }

        var type = typeof(T);

        if (!_validators.TryGetValue(type, out var validatorObj))
        {
            _logger.LogWarning("No validator registered for type {Type}", type.Name);
            return ValidationResult.Success(); // No validator means no validation errors
        }

        if (validatorObj is not IValidator<T> validator)
        {
            _logger.LogError("Invalid validator type for {Type}", type.Name);
            return ValidationResult.Failure("Validator", "Invalid validator configuration", "INVALID_VALIDATOR");
        }

        try
        {
            _logger.LogDebug("Validating instance of type {Type} asynchronously", type.Name);
            var result = await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Async validation completed for {Type}. Valid: {IsValid}, Errors: {ErrorCount}",
                type.Name, result.IsValid, result.Errors.Count);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Validation was cancelled for type {Type}", type.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during async validation for type {Type}", type.Name);
            return ValidationResult.Failure("Validation", "Validation failed due to an error", "VALIDATION_ERROR");
        }
    }

    /// <inheritdoc/>
    public void RegisterValidator<T>(IValidator<T> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        var type = typeof(T);
        _validators.AddOrUpdate(type, validator, (_, _) => validator);

        _logger.LogDebug("Registered validator for type {Type}", type.Name);
    }

    /// <inheritdoc/>
    public bool UnregisterValidator<T>()
    {
        var type = typeof(T);
        var removed = _validators.TryRemove(type, out _);

        if (removed)
        {
            _logger.LogDebug("Unregistered validator for type {Type}", type.Name);
        }
        else
        {
            _logger.LogWarning("No validator found to unregister for type {Type}", type.Name);
        }

        return removed;
    }

    /// <summary>
    /// Gets the number of registered validators.
    /// </summary>
    /// <returns>The number of registered validators.</returns>
    public int GetValidatorCount()
    {
        return _validators.Count;
    }

    /// <summary>
    /// Determines whether a validator is registered for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>true if a validator is registered; otherwise, false.</returns>
    public bool HasValidator<T>()
    {
        return _validators.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Clears all registered validators.
    /// </summary>
    public void ClearValidators()
    {
        var count = _validators.Count;
        _validators.Clear();
        _logger.LogInformation("Cleared {Count} validators", count);
    }
}
