using FluentAssertions;
using S7Tools.Core.Validation.Validators;
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
        Result<PlcAddress> valid = PlcAddress.Create("DB1.DBX0.0");
        Result<PlcAddress> invalid = PlcAddress.Create("M0.9"); // bit offset fuera de rango
        valid.IsSuccess.Should().BeTrue();
        service.Validate(valid.Value).IsValid.Should().BeTrue();
        // Si la creación falla, es correcto porque el value object ya valida el rango
        if (invalid.IsSuccess)
        {
            service.Validate(invalid.Value).IsValid.Should().BeFalse();
        }
        else
        {
            invalid.IsSuccess.Should().BeFalse(); // El value object filtra la entrada inválida
        }
    }

    [Fact]
    public void UnregisterValidator_Removes_Validator()
    {
        var service = new ValidationService();
        service.RegisterValidator(new PlcAddressValidator());
        Result<PlcAddress> valid = PlcAddress.Create("DB1.DBX0.0");
        valid.IsSuccess.Should().BeTrue();
        service.Validate(valid.Value).IsValid.Should().BeTrue();
        service.UnregisterValidator<PlcAddress>().Should().BeTrue();
        // Sin validador, siempre es válido
        service.Validate(valid.Value).IsValid.Should().BeTrue();
    }
}
