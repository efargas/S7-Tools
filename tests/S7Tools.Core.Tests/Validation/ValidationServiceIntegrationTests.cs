using S7Tools.Core.Models.Validators;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Validation;
using Xunit;

namespace S7Tools.Core.Tests.Validation;

public class ValidationServiceIntegrationTests
{
    [Fact]
    public void RegisterValidator_And_Validate_Works_For_PlcAddress()
    {
        var service = new ValidationService();
        service.RegisterValidator(new PlcAddressValidator());
        var valid = PlcAddress.Create("DB1.DBX0.0");
        var invalid = PlcAddress.Create("M0.9"); // bit offset fuera de rango
        Assert.True(valid.IsSuccess);
        Assert.True(service.Validate(valid.Value).IsValid);
        // Si la creación falla, es correcto porque el value object ya valida el rango
        if (invalid.IsSuccess)
        {
            Assert.False(service.Validate(invalid.Value).IsValid);
        }
        else
        {
            Assert.False(invalid.IsSuccess); // El value object filtra la entrada inválida
        }
    }

    [Fact]
    public void UnregisterValidator_Removes_Validator()
    {
        var service = new ValidationService();
        service.RegisterValidator(new PlcAddressValidator());
        var valid = PlcAddress.Create("DB1.DBX0.0");
        Assert.True(valid.IsSuccess);
        Assert.True(service.Validate(valid.Value).IsValid);
        Assert.True(service.UnregisterValidator<PlcAddress>());
        // Sin validador, siempre es válido
        Assert.True(service.Validate(valid.Value).IsValid);
    }
}
