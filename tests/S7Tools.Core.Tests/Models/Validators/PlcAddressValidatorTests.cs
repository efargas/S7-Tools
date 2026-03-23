using FluentAssertions;
using S7Tools.Core.Validation.Validators;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Validation;
using Xunit;

namespace S7Tools.Core.Tests.Models.Validators;

public class PlcAddressValidatorTests
{
    private readonly PlcAddressValidator _validator = new();

    [Theory]
    [InlineData("DB1.DBX0.0", true)]
    [InlineData("M0.0", true)]
    [InlineData("I0.0", true)]
    [InlineData("Q0.0", true)]
    [InlineData("V0.0", true)]
    [InlineData("T1", true)]
    [InlineData("C1", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("DB1.DBX0.8", false)] // bit offset fuera de rango
    [InlineData("M0.9", false)] // bit offset fuera de rango
    [InlineData("DB1.DBX-1.0", false)] // offset negativo
    public void Validate_ValidAndInvalidAddresses_ReturnsExpectedResult(string address, bool expectedValid)
    {
        Result<PlcAddress> result = PlcAddress.Create(address);
        if (result.IsSuccess)
        {
            ValidationResult validation = _validator.Validate(result.Value);
            validation.IsValid.Should().Be(expectedValid);
        }
        else
        {
            expectedValid.Should().BeFalse(); // Si no se puede crear, debe ser inválido
        }
    }
}
