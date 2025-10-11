using S7Tools.Core.Models.Validators;
using S7Tools.Core.Models.ValueObjects;
using Xunit;

namespace S7Tools.Core.Tests.Models.Validators;

/// <summary>
/// Contains unit tests for the <see cref="PlcAddressValidator"/>.
/// </summary>
public class PlcAddressValidatorTests
{
    private readonly PlcAddressValidator _validator = new();

    /// <summary>
    /// Verifies that the validator correctly identifies valid and invalid PLC addresses.
    /// </summary>
    /// <param name="address">The PLC address string to validate.</param>
    /// <param name="expectedValid">The expected validation result.</param>
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
        var result = PlcAddress.Create(address);
        if (result.IsSuccess)
        {
            var validation = _validator.Validate(result.Value);
            Assert.Equal(expectedValid, validation.IsValid);
        }
        else
        {
            Assert.False(expectedValid); // Si no se puede crear, debe ser inválido
        }
    }
}
