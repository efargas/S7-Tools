using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Tests.Models.Jobs;

/// <summary>
/// Unit tests for the ResourceKey record struct.
/// Tests case-insensitive equality and hash code consistency.
/// </summary>
public class ResourceKeyTests
{
    #region Equality Tests

    [Fact]
    public void Equals_WithIdenticalValues_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("serial", "COM1");

        // Act
        var result = key1.Equals(key2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentCasingInKind_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "COM1");

        // Act
        var result = key1.Equals(key2);

        // Assert
        result.Should().BeTrue("Kind should be compared case-insensitively");
    }

    [Fact]
    public void Equals_WithDifferentCasingInId_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("serial", "com1");

        // Act
        var result = key1.Equals(key2);

        // Assert
        result.Should().BeTrue("Id should be compared case-insensitively");
    }

    [Fact]
    public void Equals_WithDifferentCasingInBoth_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");

        // Act
        var result = key1.Equals(key2);

        // Assert
        result.Should().BeTrue("Both Kind and Id should be compared case-insensitively");
    }

    [Fact]
    public void Equals_WithDifferentKind_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("tcp", "COM1");

        // Act
        var result = key1.Equals(key2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("serial", "COM2");

        // Act
        var result = key1.Equals(key2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_WithIdenticalValues_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("serial", "COM1");

        // Act
        var result = key1 == key2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentCasing_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");

        // Act
        var result = key1 == key2;

        // Assert
        result.Should().BeTrue("Equality operator should use case-insensitive comparison");
    }

    [Fact]
    public void InequalityOperator_WithDifferentValues_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("tcp", "8080");

        // Act
        var result = key1 != key2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_WithSameValuesDifferentCasing_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");

        // Act
        var result = key1 != key2;

        // Assert
        result.Should().BeFalse("Inequality operator should use case-insensitive comparison");
    }

    #endregion

    #region Hash Code Tests

    [Fact]
    public void GetHashCode_WithIdenticalValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("serial", "COM1");

        // Act
        var hash1 = key1.GetHashCode();
        var hash2 = key2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentCasingInKind_ShouldReturnSameHashCode()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "COM1");

        // Act
        var hash1 = key1.GetHashCode();
        var hash2 = key2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2, "Hash code should be case-insensitive for Kind");
    }

    [Fact]
    public void GetHashCode_WithDifferentCasingInId_ShouldReturnSameHashCode()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("serial", "com1");

        // Act
        var hash1 = key1.GetHashCode();
        var hash2 = key2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2, "Hash code should be case-insensitive for Id");
    }

    [Fact]
    public void GetHashCode_WithDifferentCasingInBoth_ShouldReturnSameHashCode()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");

        // Act
        var hash1 = key1.GetHashCode();
        var hash2 = key2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2, "Hash code should be case-insensitive for both Kind and Id");
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("tcp", "8080");

        // Act
        var hash1 = key1.GetHashCode();
        var hash2 = key2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2, "Different resource keys should have different hash codes (in most cases)");
    }

    #endregion

    #region HashSet Integration Tests

    [Fact]
    public void HashSet_WithDifferentCasingKeys_ShouldTreatAsSameKey()
    {
        // Arrange
        var set = new HashSet<ResourceKey>();
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");

        // Act
        set.Add(key1);
        var addResult = set.Add(key2);

        // Assert
        addResult.Should().BeFalse("HashSet should treat keys with different casing as duplicates");
        set.Count.Should().Be(1);
    }

    [Fact]
    public void HashSet_Contains_WithDifferentCasing_ShouldReturnTrue()
    {
        // Arrange
        var set = new HashSet<ResourceKey>();
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");
        set.Add(key1);

        // Act
        var result = set.Contains(key2);

        // Assert
        result.Should().BeTrue("HashSet.Contains should use case-insensitive comparison");
    }

    [Fact]
    public void HashSet_Remove_WithDifferentCasing_ShouldSucceed()
    {
        // Arrange
        var set = new HashSet<ResourceKey>();
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");
        set.Add(key1);

        // Act
        var removeResult = set.Remove(key2);

        // Assert
        removeResult.Should().BeTrue("HashSet.Remove should use case-insensitive comparison");
        set.Count.Should().Be(0);
    }

    #endregion

    #region Dictionary Integration Tests

    [Fact]
    public void Dictionary_WithDifferentCasingKeys_ShouldTreatAsSameKey()
    {
        // Arrange
        var dict = new Dictionary<ResourceKey, string>();
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");

        // Act
        dict[key1] = "value1";
        dict[key2] = "value2";

        // Assert
        dict.Count.Should().Be(1, "Dictionary should treat keys with different casing as the same");
        dict[key1].Should().Be("value2");
    }

    [Fact]
    public void Dictionary_TryGetValue_WithDifferentCasing_ShouldSucceed()
    {
        // Arrange
        var dict = new Dictionary<ResourceKey, string>();
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");
        dict[key1] = "test value";

        // Act
        var result = dict.TryGetValue(key2, out var value);

        // Assert
        result.Should().BeTrue("TryGetValue should use case-insensitive comparison");
        value.Should().Be("test value");
    }

    [Fact]
    public void Dictionary_ContainsKey_WithDifferentCasing_ShouldReturnTrue()
    {
        // Arrange
        var dict = new Dictionary<ResourceKey, string>();
        var key1 = new ResourceKey("serial", "COM1");
        var key2 = new ResourceKey("SERIAL", "com1");
        dict[key1] = "test value";

        // Act
        var result = dict.ContainsKey(key2);

        // Assert
        result.Should().BeTrue("ContainsKey should use case-insensitive comparison");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var key = new ResourceKey("serial", "COM1");

        // Act
        var result = key.ToString();

        // Assert
        result.Should().Be("serial:COM1");
    }

    [Fact]
    public void ToString_PreservesCasing()
    {
        // Arrange
        var key = new ResourceKey("SERIAL", "com1");

        // Act
        var result = key.ToString();

        // Assert
        result.Should().Be("SERIAL:com1", "ToString should preserve the original casing");
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Scenario_SerialPortLocking_DifferentCasing_ShouldPreventDuplicateLocks()
    {
        // Arrange
        var locks = new HashSet<ResourceKey>();
        var key1 = new ResourceKey("serial", "/dev/ttyUSB0");
        var key2 = new ResourceKey("SERIAL", "/dev/TTYUSB0");

        // Act - Attempt to lock the same resource with different casing
        var lock1Acquired = locks.Add(key1);
        var lock2Acquired = locks.Add(key2);

        // Assert
        lock1Acquired.Should().BeTrue("First lock should be acquired");
        lock2Acquired.Should().BeFalse("Second lock should fail due to case-insensitive matching");
        locks.Count.Should().Be(1);
    }

    [Fact]
    public void Scenario_TcpPortLocking_DifferentCasing_ShouldPreventDuplicateLocks()
    {
        // Arrange
        var locks = new HashSet<ResourceKey>();
        var key1 = new ResourceKey("tcp", "8080");
        var key2 = new ResourceKey("TCP", "8080");

        // Act
        var lock1Acquired = locks.Add(key1);
        var lock2Acquired = locks.Add(key2);

        // Assert
        lock1Acquired.Should().BeTrue("First lock should be acquired");
        lock2Acquired.Should().BeFalse("Second lock should fail due to case-insensitive matching");
        locks.Count.Should().Be(1);
    }

    [Fact]
    public void Scenario_ModbusLocking_DifferentCasing_ShouldPreventDuplicateLocks()
    {
        // Arrange
        var locks = new HashSet<ResourceKey>();
        var key1 = new ResourceKey("modbus", "192.168.1.100:502");
        var key2 = new ResourceKey("MODBUS", "192.168.1.100:502");

        // Act
        var lock1Acquired = locks.Add(key1);
        var lock2Acquired = locks.Add(key2);

        // Assert
        lock1Acquired.Should().BeTrue("First lock should be acquired");
        lock2Acquired.Should().BeFalse("Second lock should fail due to case-insensitive matching");
        locks.Count.Should().Be(1);
    }

    #endregion
}
