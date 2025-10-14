# Ejemplo completo: Factoría de Validadores y su integración en UI

## 1. Implementación de una factoría multiclase de validadores

```csharp
using Microsoft.Extensions.Logging;
using S7Tools.Core.Factories;
using S7Tools.Core.Validation;
using S7Tools.Core.Models.Validators;

public class ValidatorFactory : BaseKeyedFactory<string, IValidator>
{
    public ValidatorFactory(ILogger logger) : base(logger) { }
    protected override void RegisterFactories()
    {
        RegisterFactory("PlcAddress", () => new PlcAddressValidator());
        // Agrega aquí más validadores según necesidad
    }
}
```

---

## 2. Registro en DI

```csharp
services.TryAddSingleton<IKeyedFactory<string, IValidator>, ValidatorFactory>();
```

---

## 3. Ejemplo de uso en un ViewModel

```csharp
using S7Tools.Core.Validation;
using S7Tools.Core.Factories;

public class PlcInputViewModel : ObservableObject
{
    private readonly IKeyedFactory<string, IValidator> _validatorFactory;
    public PlcInputViewModel(IKeyedFactory<string, IValidator> validatorFactory)
    {
        _validatorFactory = validatorFactory;
    }
    public string Address { get; set; }
    public string? ValidationError { get; private set; }
    public void Validate()
    {
        var validator = _validatorFactory.Create("PlcAddress");
        var result = PlcAddress.Create(Address);
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
```

---

## 4. Test unitario AAA para la factoría y el ViewModel

```csharp
using Xunit;
using S7Tools.Core.Factories;
using S7Tools.Core.Models.Validators;
using S7Tools.Core.Validation;
using Microsoft.Extensions.Logging.Abstractions;

public class ValidatorFactoryTests
{
    [Fact]
    public void Create_Should_Return_PlcAddressValidator_Given_PlcAddress_Key()
    {
        // Arrange
        var factory = new ValidatorFactory(NullLogger.Instance);
        // Act
        var validator = factory.Create("PlcAddress");
        // Assert
        Assert.NotNull(validator);
        Assert.IsType<PlcAddressValidator>(validator);
    }
}

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

---

## 5. Resumen de integración
- Factoría multiclase lista para registrar y extender.
- ViewModel desacoplado, usa factoría para obtener validador.
- Test unitario AAA para factoría y ViewModel.
- Todo preparado para escalar y automatizar el registro de validadores.