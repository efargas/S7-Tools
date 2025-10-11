using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Validation;

namespace S7Tools.Core.Models.Validators;

/// <summary>
/// Concrete validator for PLC addresses (PlcAddress).
/// </summary>
public class PlcAddressValidator : BaseValidator<PlcAddress>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlcAddressValidator"/> class.
    /// </summary>
    public PlcAddressValidator() : base(NullLogger.Instance) { }

    /// <summary>
    /// Configures the validation rules for PlcAddress.
    /// </summary>
    protected override void ConfigureRules()
    {
        AddRule(CreateRule(
            name: "NoEmptyValue",
            propertyName: nameof(PlcAddress.Value),
            predicate: addr => !string.IsNullOrWhiteSpace(addr.Value),
            errorMessage: "The address cannot be empty."
        ));

        AddRule(CreateRule(
            name: "NonNegativeOffset",
            propertyName: nameof(PlcAddress.Offset),
            predicate: addr => addr.Offset >= 0,
            errorMessage: "The offset cannot be negative."
        ));

        AddRule(CreateRule(
            name: "BitOffsetInRange",
            propertyName: nameof(PlcAddress.BitOffset),
            predicate: addr => !addr.BitOffset.HasValue || (addr.BitOffset.Value >= 0 && addr.BitOffset.Value <= 7),
            errorMessage: "The bit offset must be between 0 and 7."
        ));
    }
}
