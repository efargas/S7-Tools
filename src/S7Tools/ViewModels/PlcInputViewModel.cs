using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S7Tools.Core.Factories;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Validation;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel de ejemplo para entrada y validación de direcciones PLC usando factoría de validadores.
/// </summary>
public partial class PlcInputViewModel : ObservableObject
{
    private readonly IKeyedFactory<string, IValidator> _validatorFactory;

    [ObservableProperty]
    private string? address;

    [ObservableProperty]
    private string? validationError;

    /// <summary>
    /// Initializes a new instance of the PlcInputViewModel class.
    /// </summary>
    /// <param name="validatorFactory">The validator factory.</param>
    public PlcInputViewModel(IKeyedFactory<string, IValidator> validatorFactory)
    {
        _validatorFactory = validatorFactory;
    }

    /// <summary>
    /// Initializes a new instance of the PlcInputViewModel class for design-time.
    /// </summary>
    public PlcInputViewModel() : this(new DesignTimeValidatorFactory())
    {
        // Initialize with sample data for better design-time experience
        Address = "DB1.DBW10";
        ValidationError = null;
    }

    [RelayCommand]
    private void Validate()
    {
        var validator = _validatorFactory.Create("PlcAddress");
        var result = PlcAddress.Create(Address ?? string.Empty);
        if (result.IsSuccess)
        {
            var validation = validator.Validate(result.Value);
            ValidationError = validation.IsValid ? null : string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        }
        else
        {
            ValidationError = result.Error;
        }
    }
}

/// <summary>
/// Design-time implementation of IKeyedFactory for validators.
/// </summary>
internal class DesignTimeValidatorFactory : IKeyedFactory<string, IValidator>
{
    public IValidator Create(string key)
    {
        return new DesignTimeValidator();
    }

    public IEnumerable<string> GetAvailableKeys()
    {
        return new[] { "PlcAddress" };
    }

    public bool CanCreate(string key)
    {
        return key == "PlcAddress";
    }
}

/// <summary>
/// Design-time implementation of IValidator for XAML preview.
/// </summary>
internal class DesignTimeValidator : IValidator
{
    public ValidationResult Validate(object instance)
    {
        // Return a valid result for design-time
        return new ValidationResult { IsValid = true };
    }

    public bool CanValidate(Type type)
    {
        return true;
    }

    public Task<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(instance));
    }
}
