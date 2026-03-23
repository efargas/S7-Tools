using FluentAssertions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using Xunit;

namespace S7Tools.Core.Tests.Models;

/// <summary>
/// Unit tests for Job entity validation and core behavior.
/// </summary>
public class JobTests
{
    private static SerialPortConfiguration CreateDefaultSerialConfig() => new()
    {
        BaudRate = 115200,
        Parity = ParityMode.None,
        CharacterSize = 8,
        StopBits = StopBits.One,
        RawMode = true,
        DisableEcho = true
    };

    private static SocatConfiguration CreateDefaultSocatConfig() => new()
    {
        TcpPort = 10102,
        Verbose = true,
        EnableFork = true,
        EnableReuseAddr = true
    };

    private static PowerSupplyConfiguration CreateDefaultPowerConfig() => new ModbusTcpConfiguration
    {
        Host = "192.168.1.100",
        Port = 502,
        DeviceId = 1,
        OnOffCoil = 1,
        AddressingMode = ModbusAddressingMode.Base0
    };

    [Fact]
    public void Job_Creation_Should_Set_Timestamps_And_Default_State()
    {
        // Arrange
        DateTime beforeCreate = DateTime.UtcNow;
        JobProfileSet profileSet = CreateTestProfileSet();

        // Act
        var job = new Job
        {
            Id = 1,
            Name = "Test Job",
            Description = "Test job description",
            ProfileSet = profileSet,
            OutputPath = "/tmp/dumps"
        };
        DateTime afterCreate = DateTime.UtcNow;

        // Assert
        job.Id.Should().Be(1);
        job.Name.Should().Be("Test Job");
        job.Description.Should().Be("Test job description");
        job.ProfileSet.Should().NotBeNull();
        job.OutputPath.Should().Be("/tmp/dumps");
        job.State.Should().Be(JobState.Created);
        Assert.InRange(job.CreatedAt, beforeCreate, afterCreate);
        Assert.InRange(job.ModifiedAt, beforeCreate, afterCreate);
        job.QueuedAt.Should().BeNull();
        job.StartedAt.Should().BeNull();
        job.CompletedAt.Should().BeNull();
        job.Progress.Should().Be(0.0);
        job.CurrentOperation.Should().Be(string.Empty);
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Job_Clone_Should_Create_Independent_Instance()
    {
        // Arrange
        JobProfileSet profileSet = CreateTestProfileSet();
        var original = new Job
        {
            Id = 1,
            Name = "Original Job",
            Description = "Original description",
            ProfileSet = profileSet,
            OutputPath = "/tmp/original",
            State = JobState.Running,
            Progress = 50.0,
            CurrentOperation = "Installing stager",
            StartedAt = DateTime.UtcNow
        };

        // Act
        Job clone = original with
        {
            Id = 2,
            Name = "Cloned Job",
            Description = "Cloned description",
            OutputPath = "/tmp/clone"
        };

        // Modify clone
        Job modifiedClone = clone with
        {
            Progress = 75.0,
            CurrentOperation = "Dumping memory"
        };

        // Assert
        Assert.NotEqual(original.Id, modifiedClone.Id);
        Assert.NotEqual(original.Name, modifiedClone.Name);
        Assert.NotEqual(original.Description, modifiedClone.Description);
        Assert.NotEqual(original.OutputPath, modifiedClone.OutputPath);
        Assert.NotEqual(original.Progress, modifiedClone.Progress);
        Assert.NotEqual(original.CurrentOperation, modifiedClone.CurrentOperation);

        // Original unchanged
        original.Id.Should().Be(1);
        original.Name.Should().Be("Original Job");
        original.Progress.Should().Be(50.0);
        original.CurrentOperation.Should().Be("Installing stager");
    }

    private static JobProfileSet CreateTestProfileSet()
    {
        return new JobProfileSet(
            Serial: new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One", CreateDefaultSerialConfig()),
            Socat: new SocatProfileRef(10102, Ephemeral: false, CreateDefaultSocatConfig()),
            Power: new PowerProfileRef("192.168.1.100", 502, 1, 5, CreateDefaultPowerConfig()),
            Memory: new MemoryRegionProfile("0x20000000", 0x10000),
            Payloads: new PayloadSetProfile { BasePath = "/payloads" },
            OutputPath: "/tmp/dumps"
        );
    }
}
