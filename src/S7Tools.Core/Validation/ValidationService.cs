using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Validation;

/// <summary>
/// Servicio de validación simple para uso con DI y pruebas.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ConcurrentDictionary<Type, object> _validators = new();

    public ValidationResult Validate<T>(T instance)
    {
        var validator = GetValidator<T>();
        return validator?.Validate(instance!) ?? ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var validator = GetValidator<T>();
        return validator != null
            ? await validator.ValidateAsync(instance!, cancellationToken).ConfigureAwait(false)
            : ValidationResult.Success();
    }

    public void RegisterValidator<T>(IValidator<T> validator)
    {
        _validators[typeof(T)] = validator;
    }

    public bool UnregisterValidator<T>()
    {
        return _validators.TryRemove(typeof(T), out _);
    }

    private IValidator<T>? GetValidator<T>()
    {
        if (_validators.TryGetValue(typeof(T), out var validator) && validator is IValidator<T> typed)
            return typed;
        return null;
    }
}
