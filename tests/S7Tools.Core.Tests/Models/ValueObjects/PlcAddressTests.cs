using S7Tools.Core.Models;
using S7Tools.Core.Models.ValueObjects;

namespace S7Tools.Core.Tests.Models.ValueObjects;

/// <summary>
/// Unit tests for the PlcAddress value object.
/// Tests address validation, parsing, and type safety.
/// </summary>
public class PlcAddressTests
{
    /// <summary>
    /// Verifies that the constructor correctly parses various valid PLC addresses.
    /// </summary>
    /// <param name="address">The PLC address string to test.</param>
    /// <param name="expectedType">The expected address type.</param>
    /// <param name="expectedDbNumber">The expected data block number.</param>
    /// <param name="expectedOffset">The expected offset.</param>
    /// <param name="expectedBitOffset">The expected bit offset.</param>
    [Theory]
    [InlineData("DB1.DBX0.0", PlcAddressType.DataBlockBit, 1, 0, 0)]
    [InlineData("DB10.DBB5", PlcAddressType.DataBlockByte, 10, 5, null)]
    [InlineData("DB100.DBW20", PlcAddressType.DataBlockWord, 100, 20, null)]
    [InlineData("DB255.DBD100", PlcAddressType.DataBlockDWord, 255, 100, null)]
    [InlineData("M0.0", PlcAddressType.Memory, null, 0, 0)]
    [InlineData("I1.7", PlcAddressType.Input, null, 1, 7)]
    [InlineData("Q2.3", PlcAddressType.Output, null, 2, 3)]
    [InlineData("V10.5", PlcAddressType.Variable, null, 10, 5)]
    [InlineData("T1", PlcAddressType.Timer, null, 1, null)]
    [InlineData("C5", PlcAddressType.Counter, null, 5, null)]
    public void Constructor_WithValidAddress_ShouldParseCorrectly(
        string address, 
        PlcAddressType expectedType, 
        int? expectedDbNumber, 
        int expectedOffset, 
        int? expectedBitOffset)
    {
        // Act
        var plcAddress = new PlcAddress(address);

        // Assert
        plcAddress.Value.Should().Be(address.ToUpperInvariant());
        plcAddress.AddressType.Should().Be(expectedType);
        plcAddress.DataBlockNumber.Should().Be(expectedDbNumber);
        plcAddress.Offset.Should().Be(expectedOffset);
        plcAddress.BitOffset.Should().Be(expectedBitOffset);
    }

    /// <summary>
    /// Verifies that the constructor throws an ArgumentException for various invalid PLC addresses.
    /// </summary>
    /// <param name="invalidAddress">The invalid PLC address string to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID")]
    [InlineData("DB")]
    [InlineData("DB1")]
    [InlineData("DB1.DBX")]
    [InlineData("DB1.DBX0.")]
    [InlineData("DB1.DBX0.8")] // Bit offset > 7
    [InlineData("M")]
    [InlineData("M0")]
    [InlineData("M0.")] 
    [InlineData("M0.8")] // Bit offset > 7
    [InlineData("X1.0")] // Invalid memory type
    public void Constructor_WithInvalidAddress_ShouldThrowArgumentException(string invalidAddress)
    {
        // Act & Assert
        var act = () => new PlcAddress(invalidAddress);
        act.Should().Throw<ArgumentException>()
           .WithMessage($"Invalid PLC address format: {invalidAddress}*");
    }

    /// <summary>
    /// Verifies that the constructor throws an ArgumentException when the address is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAddress_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new PlcAddress(null!);
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that the constructor normalizes lowercase addresses to uppercase.
    /// </summary>
    /// <param name="input">The lowercase input address.</param>
    /// <param name="expected">The expected uppercase output address.</param>
    [Theory]
    [InlineData("db1.dbx0.0", "DB1.DBX0.0")] // Case insensitive
    [InlineData("m0.0", "M0.0")]
    [InlineData("i1.7", "I1.7")]
    public void Constructor_WithLowerCaseAddress_ShouldNormalizeToUpperCase(string input, string expected)
    {
        // Act
        var plcAddress = new PlcAddress(input);

        // Assert
        plcAddress.Value.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that the Create factory method returns a success result for valid addresses.
    /// </summary>
    /// <param name="address">The valid PLC address string.</param>
    [Theory]
    [InlineData("DB1.DBX0.0")]
    [InlineData("M0.0")]
    [InlineData("I1.7")]
    [InlineData("Q2.3")]
    [InlineData("V10.5")]
    public void Create_WithValidAddress_ShouldReturnSuccessResult(string address)
    {
        // Act
        var result = PlcAddress.Create(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(address.ToUpperInvariant());
    }

    /// <summary>
    /// Verifies that the Create factory method returns a failure result for invalid addresses.
    /// </summary>
    /// <param name="invalidAddress">The invalid PLC address string.</param>
    [Theory]
    [InlineData("")]
    [InlineData("INVALID")]
    [InlineData("DB1.DBX0.8")]
    public void Create_WithInvalidAddress_ShouldReturnFailureResult(string invalidAddress)
    {
        // Act
        var result = PlcAddress.Create(invalidAddress);

        // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().NotBeEmpty();
    result.Value.Should().Be(default(PlcAddress));
    }

    /// <summary>
    /// Verifies that the IsBitAddress property returns the correct value for various address types.
    /// </summary>
    /// <param name="address">The PLC address string.</param>
    /// <param name="expectedIsBitAddress">The expected value for IsBitAddress.</param>
    [Theory]
    [InlineData("DB1.DBX0.0", true)]
    [InlineData("M0.0", true)]
    [InlineData("I1.7", true)]
    [InlineData("Q2.3", true)]
    [InlineData("V10.5", true)]
    [InlineData("DB1.DBB5", false)]
    [InlineData("DB1.DBW20", false)]
    [InlineData("DB1.DBD100", false)]
    [InlineData("T1", false)]
    [InlineData("C5", false)]
    public void IsBitAddress_ShouldReturnCorrectValue(string address, bool expectedIsBitAddress)
    {
        // Arrange
        var plcAddress = new PlcAddress(address);

        // Act & Assert
        plcAddress.IsBitAddress.Should().Be(expectedIsBitAddress);
    }

    /// <summary>
    /// Verifies that the IsDataBlockAddress property returns the correct value for various address types.
    /// </summary>
    /// <param name="address">The PLC address string.</param>
    /// <param name="expectedIsDataBlockAddress">The expected value for IsDataBlockAddress.</param>
    [Theory]
    [InlineData("DB1.DBX0.0", true)]
    [InlineData("DB10.DBB5", true)]
    [InlineData("DB100.DBW20", true)]
    [InlineData("DB255.DBD100", true)]
    [InlineData("M0.0", false)]
    [InlineData("I1.7", false)]
    [InlineData("Q2.3", false)]
    [InlineData("V10.5", false)]
    [InlineData("T1", false)]
    [InlineData("C5", false)]
    public void IsDataBlockAddress_ShouldReturnCorrectValue(string address, bool expectedIsDataBlockAddress)
    {
        // Arrange
        var plcAddress = new PlcAddress(address);

        // Act & Assert
        plcAddress.IsDataBlockAddress.Should().Be(expectedIsDataBlockAddress);
    }

    /// <summary>
    /// Verifies that the implicit conversion to string returns the correct address value.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var plcAddress = new PlcAddress("DB1.DBX0.0");

        // Act
        string addressString = plcAddress;

        // Assert
        addressString.Should().Be("DB1.DBX0.0");
    }

    /// <summary>
    /// Verifies that the ToString method returns the correct address value.
    /// </summary>
    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var plcAddress = new PlcAddress("DB1.DBX0.0");

        // Act
        var result = plcAddress.ToString();

        // Assert
        result.Should().Be("DB1.DBX0.0");
    }

    /// <summary>
    /// Verifies that two PlcAddress instances with the same address are considered equal.
    /// </summary>
    [Fact]
    public void Equality_WithSameAddress_ShouldBeEqual()
    {
        // Arrange
        var address1 = new PlcAddress("DB1.DBX0.0");
        var address2 = new PlcAddress("db1.dbx0.0"); // Different case

        // Act & Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        (address1 != address2).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that two PlcAddress instances with different addresses are not considered equal.
    /// </summary>
    [Fact]
    public void Equality_WithDifferentAddress_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new PlcAddress("DB1.DBX0.0");
        var address2 = new PlcAddress("DB1.DBX0.1");

        // Act & Assert
        address1.Should().NotBe(address2);
        (address1 == address2).Should().BeFalse();
        (address1 != address2).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that two PlcAddress instances with the same address have the same hash code.
    /// </summary>
    [Fact]
    public void GetHashCode_WithSameAddress_ShouldReturnSameHashCode()
    {
        // Arrange
        var address1 = new PlcAddress("DB1.DBX0.0");
        var address2 = new PlcAddress("db1.dbx0.0"); // Different case

        // Act & Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    /// <summary>
    /// Verifies that the AddressType property is correctly determined for various address formats.
    /// </summary>
    /// <param name="address">The PLC address string.</param>
    /// <param name="expectedType">The expected address type.</param>
    [Theory]
    [InlineData("DB1.DBX0.0", PlcAddressType.DataBlockBit)]
    [InlineData("DB1.DBB0", PlcAddressType.DataBlockByte)]
    [InlineData("DB1.DBW0", PlcAddressType.DataBlockWord)]
    [InlineData("DB1.DBD0", PlcAddressType.DataBlockDWord)]
    [InlineData("M0.0", PlcAddressType.Memory)]
    [InlineData("I0.0", PlcAddressType.Input)]
    [InlineData("Q0.0", PlcAddressType.Output)]
    [InlineData("V0.0", PlcAddressType.Variable)]
    [InlineData("T1", PlcAddressType.Timer)]
    [InlineData("C1", PlcAddressType.Counter)]
    public void AddressType_ShouldBeCorrectlyDetermined(string address, PlcAddressType expectedType)
    {
        // Arrange
        var plcAddress = new PlcAddress(address);

        // Act & Assert
        plcAddress.AddressType.Should().Be(expectedType);
    }

    /// <summary>
    /// Verifies that the constructor succeeds with large but valid address values.
    /// </summary>
    /// <param name="address">The PLC address string with large values.</param>
    [Theory]
    [InlineData("DB999.DBX999.7")] // Large values
    [InlineData("M999.7")]
    [InlineData("I999.7")]
    [InlineData("Q999.7")]
    [InlineData("V999.7")]
    [InlineData("T999")]
    [InlineData("C999")]
    public void Constructor_WithLargeValidValues_ShouldSucceed(string address)
    {
        // Act
        var act = () => new PlcAddress(address);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that the constructor succeeds with zero-based address values.
    /// </summary>
    /// <param name="address">The zero-based PLC address string.</param>
    [Theory]
    [InlineData("DB0.DBX0.0")] // DB0 should be valid
    [InlineData("M0.0")]
    [InlineData("I0.0")]
    [InlineData("Q0.0")]
    [InlineData("V0.0")]
    [InlineData("T0")]
    [InlineData("C0")]
    public void Constructor_WithZeroValues_ShouldSucceed(string address)
    {
        // Act
        var act = () => new PlcAddress(address);

        // Assert
        act.Should().NotThrow();
    }
}