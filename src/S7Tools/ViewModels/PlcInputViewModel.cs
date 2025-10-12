using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S7Tools.Core.Factories;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Validation;
using System.Linq;

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
    /// Initializes a new instance of the <see cref="PlcInputViewModel"/> class.
    /// </summary>
    /// <param name="validatorFactory">The factory to create validators.</param>
    public PlcInputViewModel(IKeyedFactory<string, IValidator> validatorFactory)
    {
        _validatorFactory = validatorFactory;
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
