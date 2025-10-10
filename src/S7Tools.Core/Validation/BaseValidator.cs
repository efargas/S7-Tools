using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace S7Tools.Core.Validation;

/// <summary>
/// Base class for validators with common functionality.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public abstract class BaseValidator<T> : IValidator<T>
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the validation rules.
    /// </summary>
    protected List<ValidationRule<T>> Rules { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseValidator{T}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected BaseValidator(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Rules = new List<ValidationRule<T>>();
        ConfigureRules();
    }

    /// <inheritdoc/>
    public virtual ValidationResult Validate(T instance)
    {
        if (instance == null)
        {
            Logger.LogWarning("Validation instance is null");
            return ValidationResult.Failure("Instance", "Instance cannot be null", "NULL_INSTANCE");
        }

        var errors = new List<ValidationError>();

        Logger.LogDebug("Starting validation for {Type}", typeof(T).Name);

        foreach (var rule in Rules)
        {
            try
            {
                var result = rule.Validate(instance);
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing validation rule: {RuleName}", rule.Name);
                errors.Add(new ValidationError("ValidationRule", $"Validation rule '{rule.Name}' failed", "RULE_ERROR"));
            }
        }

        var isValid = errors.Count == 0;
        Logger.LogDebug("Validation completed for {Type}. Valid: {IsValid}, Errors: {ErrorCount}",
            typeof(T).Name, isValid, errors.Count);

        return isValid ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    /// <inheritdoc/>
    public virtual async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        if (instance == null)
        {
            Logger.LogWarning("Validation instance is null");
            return ValidationResult.Failure("Instance", "Instance cannot be null", "NULL_INSTANCE");
        }

        var errors = new List<ValidationError>();

        Logger.LogDebug("Starting async validation for {Type}", typeof(T).Name);

        foreach (var rule in Rules)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await rule.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("Validation was cancelled for {Type}", typeof(T).Name);
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing async validation rule: {RuleName}", rule.Name);
                errors.Add(new ValidationError("ValidationRule", $"Validation rule '{rule.Name}' failed", "RULE_ERROR"));
            }
        }

        var isValid = errors.Count == 0;
        Logger.LogDebug("Async validation completed for {Type}. Valid: {IsValid}, Errors: {ErrorCount}",
            typeof(T).Name, isValid, errors.Count);

        return isValid ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    /// <summary>
    /// Configures the validation rules. Override this method in derived classes.
    /// </summary>
    protected abstract void ConfigureRules();

    /// <summary>
    /// Adds a validation rule.
    /// </summary>
    /// <param name="rule">The validation rule to add.</param>
    protected void AddRule(ValidationRule<T> rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        Rules.Add(rule);
        Logger.LogDebug("Added validation rule: {RuleName}", rule.Name);
    }

    /// <summary>
    /// Creates a simple validation rule.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="predicate">The validation predicate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <param name="errorCode">The error code if validation fails.</param>
    /// <returns>A validation rule.</returns>
    protected ValidationRule<T> CreateRule(
        string name,
        string propertyName,
        Func<T, bool> predicate,
        string errorMessage,
        string? errorCode = null)
    {
        return new ValidationRule<T>(name, propertyName, predicate, errorMessage, errorCode);
    }

    /// <summary>
    /// Creates an async validation rule.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="predicate">The async validation predicate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <param name="errorCode">The error code if validation fails.</param>
    /// <returns>A validation rule.</returns>
    protected ValidationRule<T> CreateAsyncRule(
        string name,
        string propertyName,
        Func<T, CancellationToken, Task<bool>> predicate,
        string errorMessage,
        string? errorCode = null)
    {
        return new ValidationRule<T>(name, propertyName, predicate, errorMessage, errorCode);
    }
}

/// <summary>
/// Represents a validation rule for objects of type T.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public class ValidationRule<T>
{
    private readonly Func<T, bool>? _syncPredicate;
    private readonly Func<T, CancellationToken, Task<bool>>? _asyncPredicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationRule{T}"/> class with a synchronous predicate.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="predicate">The validation predicate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <param name="errorCode">The error code if validation fails.</param>
    public ValidationRule(
        string name,
        string propertyName,
        Func<T, bool> predicate,
        string errorMessage,
        string? errorCode = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        _syncPredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationRule{T}"/> class with an asynchronous predicate.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="predicate">The async validation predicate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <param name="errorCode">The error code if validation fails.</param>
    public ValidationRule(
        string name,
        string propertyName,
        Func<T, CancellationToken, Task<bool>> predicate,
        string errorMessage,
        string? errorCode = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        _asyncPredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the name of the rule.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name of the property being validated.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the error message if validation fails.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the error code if validation fails.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Validates the specified instance.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate(T instance)
    {
        if (_syncPredicate != null)
        {
            var isValid = _syncPredicate(instance);
            return isValid ? ValidationResult.Success() :
                ValidationResult.Failure(PropertyName, ErrorMessage, ErrorCode);
        }

        if (_asyncPredicate != null)
        {
            // For sync validation, we'll run the async predicate synchronously
            var task = _asyncPredicate(instance, CancellationToken.None);
            var isValid = task.GetAwaiter().GetResult();
            return isValid ? ValidationResult.Success() :
                ValidationResult.Failure(PropertyName, ErrorMessage, ErrorCode);
        }

        throw new InvalidOperationException("No validation predicate configured");
    }

    /// <summary>
    /// Validates the specified instance asynchronously.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        if (_asyncPredicate != null)
        {
            var isValid = await _asyncPredicate(instance, cancellationToken).ConfigureAwait(false);
            return isValid ? ValidationResult.Success() :
                ValidationResult.Failure(PropertyName, ErrorMessage, ErrorCode);
        }

        if (_syncPredicate != null)
        {
            var isValid = _syncPredicate(instance);
            return isValid ? ValidationResult.Success() :
                ValidationResult.Failure(PropertyName, ErrorMessage, ErrorCode);
        }

        throw new InvalidOperationException("No validation predicate configured");
    }
}
