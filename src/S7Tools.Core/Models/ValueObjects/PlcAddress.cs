using System.Text.RegularExpressions;

namespace S7Tools.Core.Models.ValueObjects;

/// <summary>
/// Represents a type-safe PLC address value object with validation.
/// Supports S7 address formats like DB1.DBX0.0, M0.0, I0.0, Q0.0, etc.
/// </summary>
public readonly record struct PlcAddress
{
    private static readonly Regex AddressPattern = new(
        @"^(DB\d+\.(DBX|DBB|DBW|DBD)\d+(\.\d+)?|[MIQV]\d+\.\d+|T\d+|C\d+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets the raw address string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the address type (DB, M, I, Q, V, T, C).
    /// </summary>
    public PlcAddressType AddressType { get; }

    /// <summary>
    /// Gets the data block number (for DB addresses only).
    /// </summary>
    public int? DataBlockNumber { get; }

    /// <summary>
    /// Gets the offset within the memory area.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the bit offset (for bit addresses only).
    /// </summary>
    public int? BitOffset { get; }

    /// <summary>
    /// Initializes a new instance of the PlcAddress value object.
    /// </summary>
    /// <param name="address">The PLC address string.</param>
    /// <exception cref="ArgumentException">Thrown when the address format is invalid.</exception>
    public PlcAddress(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        if (!AddressPattern.IsMatch(address))
        {
            throw new ArgumentException($"Invalid PLC address format: {address}", nameof(address));
        }

        Value = address.ToUpperInvariant();
        (AddressType, DataBlockNumber, Offset, BitOffset) = ParseAddress(Value);
    }

    /// <summary>
    /// Creates a PlcAddress from a string with validation.
    /// </summary>
    /// <param name="address">The address string.</param>
    /// <returns>A Result containing the PlcAddress or an error.</returns>
    public static Result<PlcAddress> Create(string address)
    {
        try
        {
            return Result<PlcAddress>.Success(new PlcAddress(address));
        }
        catch (ArgumentException ex)
        {
            return Result<PlcAddress>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Parses the address string to extract components.
    /// </summary>
    private static (PlcAddressType type, int? dbNumber, int offset, int? bitOffset) ParseAddress(string address)
    {
        if (address.StartsWith("DB"))
        {
            // DB address: DB1.DBX0.0, DB1.DBB0, DB1.DBW0, DB1.DBD0
            var parts = address.Split('.');
            var dbNumber = int.Parse(parts[0][2..]);
            var dataType = parts[1][..3];
            var offset = int.Parse(parts[1][3..]);
            var bitOffset = parts.Length > 2 ? (int?)int.Parse(parts[2]) : null;

            var addressType = dataType switch
            {
                "DBX" => PlcAddressType.DataBlockBit,
                "DBB" => PlcAddressType.DataBlockByte,
                "DBW" => PlcAddressType.DataBlockWord,
                "DBD" => PlcAddressType.DataBlockDWord,
                _ => throw new ArgumentException($"Unknown DB data type: {dataType}")
            };

            return (addressType, dbNumber, offset, bitOffset);
        }

        if (address.StartsWith("M"))
        {
            // Memory address: M0.0
            var parts = address[1..].Split('.');
            return (PlcAddressType.Memory, null, int.Parse(parts[0]), int.Parse(parts[1]));
        }

        if (address.StartsWith("I"))
        {
            // Input address: I0.0
            var parts = address[1..].Split('.');
            return (PlcAddressType.Input, null, int.Parse(parts[0]), int.Parse(parts[1]));
        }

        if (address.StartsWith("Q"))
        {
            // Output address: Q0.0
            var parts = address[1..].Split('.');
            return (PlcAddressType.Output, null, int.Parse(parts[0]), int.Parse(parts[1]));
        }

        if (address.StartsWith("V"))
        {
            // Variable address: V0.0
            var parts = address[1..].Split('.');
            return (PlcAddressType.Variable, null, int.Parse(parts[0]), int.Parse(parts[1]));
        }

        if (address.StartsWith("T"))
        {
            // Timer address: T1
            return (PlcAddressType.Timer, null, int.Parse(address[1..]), (int?)null);
        }

        if (address.StartsWith("C"))
        {
            // Counter address: C1
            return (PlcAddressType.Counter, null, int.Parse(address[1..]), (int?)null);
        }

        throw new ArgumentException($"Unsupported address format: {address}");
    }

    /// <summary>
    /// Determines if this address represents a bit-level access.
    /// </summary>
    public bool IsBitAddress => BitOffset.HasValue;

    /// <summary>
    /// Determines if this address is within a data block.
    /// </summary>
    public bool IsDataBlockAddress => DataBlockNumber.HasValue;

    /// <summary>
    /// Implicit conversion from string to PlcAddress.
    /// </summary>
    public static implicit operator string(PlcAddress address) => address.Value;

    /// <summary>
    /// Returns the string representation of the address.
    /// </summary>
    public override string ToString() => Value;
}

/// <summary>
/// Defines the types of PLC addresses supported.
/// </summary>
public enum PlcAddressType
{
    /// <summary>Data block bit access (DBX).</summary>
    DataBlockBit,
    /// <summary>Data block byte access (DBB).</summary>
    DataBlockByte,
    /// <summary>Data block word access (DBW).</summary>
    DataBlockWord,
    /// <summary>Data block double word access (DBD).</summary>
    DataBlockDWord,
    /// <summary>Memory bit access (M).</summary>
    Memory,
    /// <summary>Input bit access (I).</summary>
    Input,
    /// <summary>Output bit access (Q).</summary>
    Output,
    /// <summary>Variable bit access (V).</summary>
    Variable,
    /// <summary>Timer access (T).</summary>
    Timer,
    /// <summary>Counter access (C).</summary>
    Counter
}