# PlcInputViewModelTests (AAA)

## Arrange
- Se crea una factoría de validadores con NullLogger.
- Se instancia el ViewModel con la factoría.

## Act
- Se asigna una dirección inválida ("M0.9") y se llama a `Validate()`.

## Assert
- Se espera que `ValidationError` contenga el mensaje de error de bit offset.

---

```csharp
using Xunit;
using S7Tools.Core.Factories;
using S7Tools.Core.Models.Validators;
using S7Tools.Core.Validation;
using S7Tools.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

public class PlcInputViewModelTests
{
    [Fact]
    public void Validate_Should_Set_ValidationError_When_Address_Is_Invalid()
    {
        // Arrange
        var factory = new ValidatorFactory(NullLogger.Instance);
        var vm = new PlcInputViewModel(factory);
        vm.Address = "M0.9"; // bit offset fuera de rango
        // Act
        vm.Validate();
        // Assert
        Assert.NotNull(vm.ValidationError);
        Assert.Contains("bit offset", vm.ValidationError);
    }
}
```
