using S7Tools.Core.Validation;
using S7Tools.Core.Models.ValueObjects;
using Microsoft.Extensions.Logging;

namespace S7Tools.Core.Models.Validators;

/// <summary>
/// Validador concreto para direcciones PLC (PlcAddress).
/// </summary>
public class PlcAddressValidator : BaseValidator<PlcAddress>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlcAddressValidator"/> class.
    /// </summary>
    public PlcAddressValidator() : base(NullLogger.Instance) { }

    /// <summary>
    /// Configura las reglas de validación para PlcAddress.
    /// </summary>
    protected override void ConfigureRules()
    {
        AddRule(CreateRule(
            name: "NoEmptyValue",
            propertyName: nameof(PlcAddress.Value),
            predicate: addr => !string.IsNullOrWhiteSpace(addr.Value),
            errorMessage: "La dirección no puede estar vacía."
        ));

        AddRule(CreateRule(
            name: "OffsetNoNegativo",
            propertyName: nameof(PlcAddress.Offset),
            predicate: addr => addr.Offset >= 0,
            errorMessage: "El offset no puede ser negativo."
        ));

        AddRule(CreateRule(
            name: "BitOffsetEnRango",
            propertyName: nameof(PlcAddress.BitOffset),
            predicate: addr => !addr.BitOffset.HasValue || (addr.BitOffset.Value >= 0 && addr.BitOffset.Value <= 7),
            errorMessage: "El bit offset debe estar entre 0 y 7."
        ));
    }
}
