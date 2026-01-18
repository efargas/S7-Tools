using Xunit;
using S7Tools.ViewModels.Hex;
using FluentAssertions;

namespace S7Tools.Tests.ViewModels.Hex;

public class DataInspectorViewModelTests
{
    [Fact]
    public void Update_ShouldConvertValuesCorrectly_BigEndian()
    {
        // Arrange
        var vm = new DataInspectorViewModel();
        vm.IsBigEndian = true;
        
        // 0x01 0x02 0x03 0x04 0x05 0x06 0x07 0x08
        byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        // Act
        vm.Update(data);

        // Assert
        vm.Binary8.Should().Be("00000001"); // 0x01
        vm.UInt8.Should().Be("1");
        vm.Int8.Should().Be("1");

        // UInt16: 0x0102 = 258
        vm.UInt16.Should().Be("258");
        vm.Int16.Should().Be("258");

        // UInt32: 0x01020304 = 16909060
        vm.UInt32.Should().Be("16909060");
        vm.Int32.Should().Be("16909060");
        
        // Float32: 0x01020304 ~ 2.387939E-38
        vm.Float32.Should().NotBe("-");
    }

    [Fact]
    public void Update_ShouldConvertValuesCorrectly_LittleEndian()
    {
        // Arrange
        var vm = new DataInspectorViewModel();
        vm.IsBigEndian = false;
        
        // 0x01 0x02 0x03 0x04
        byte[] data = { 0x01, 0x02, 0x03, 0x04 };

        // Act
        vm.Update(data);

        // Assert
        // UInt16: 0x0201 = 513
        vm.UInt16.Should().Be("513");
        
        // UInt32: 0x04030201 = 67305985
        vm.UInt32.Should().Be("67305985");
    }
}
