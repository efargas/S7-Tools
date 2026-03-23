using FluentAssertions;
using System.Text.Json;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using Xunit;

namespace S7Tools.Tests.Services.Jobs;

/// <summary>
/// Integration tests for job profile persistence (JSON serialization round-trip).
/// </summary>
public class JobPersistenceTests
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
    public void Job_Profile_Persistence_Should_Save_And_Load_Correctly()
    {
        // Arrange
        var originalJob = new Job
        {
            Id = 1,
            Name = "Test Job",
            Description = "Test job for persistence",
            ProfileSet = CreateTestProfileSet(),
            OutputPath = "/tmp/dumps",
            State = JobState.Created,
            Progress = 0.0,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        // Act - Serialize to JSON
        string json = JsonSerializer.Serialize(originalJob, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Load from JSON
        Job? loadedJob = JsonSerializer.Deserialize<Job>(json);

        // Assert
        loadedJob.Should().NotBeNull();
        loadedJob.Id.Should().Be(originalJob.Id);
        loadedJob.Name.Should().Be(originalJob.Name);
        loadedJob.Description.Should().Be(originalJob.Description);
        loadedJob.OutputPath.Should().Be(originalJob.OutputPath);
        loadedJob.State.Should().Be(originalJob.State);
        loadedJob.Progress.Should().Be(originalJob.Progress);

        // ProfileSet comparison
        loadedJob.ProfileSet.Should().NotBeNull();
        loadedJob.ProfileSet.Serial.Device.Should().Be(originalJob.ProfileSet.Serial.Device);
        loadedJob.ProfileSet.Socat.Port.Should().Be(originalJob.ProfileSet.Socat.Port);
        loadedJob.ProfileSet.Power.Host.Should().Be(originalJob.ProfileSet.Power.Host);
        loadedJob.ProfileSet.Memory.Start.Should().Be(originalJob.ProfileSet.Memory.Start);
        loadedJob.ProfileSet.Payloads.BasePath.Should().Be(originalJob.ProfileSet.Payloads.BasePath);
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
