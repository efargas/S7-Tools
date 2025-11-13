using System.Text.Json;
using S7Tools.Core.Models.Jobs;
using Xunit;

namespace S7Tools.Tests.Services.Jobs;

/// <summary>
/// Integration tests for job profile persistence (JSON serialization round-trip).
/// </summary>
public class JobPersistenceTests
{
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
        Assert.NotNull(loadedJob);
        Assert.Equal(originalJob.Id, loadedJob.Id);
        Assert.Equal(originalJob.Name, loadedJob.Name);
        Assert.Equal(originalJob.Description, loadedJob.Description);
        Assert.Equal(originalJob.OutputPath, loadedJob.OutputPath);
        Assert.Equal(originalJob.State, loadedJob.State);
        Assert.Equal(originalJob.Progress, loadedJob.Progress);

        // ProfileSet comparison
        Assert.NotNull(loadedJob.ProfileSet);
        Assert.Equal(originalJob.ProfileSet.Serial.Device, loadedJob.ProfileSet.Serial.Device);
        Assert.Equal(originalJob.ProfileSet.Socat.Port, loadedJob.ProfileSet.Socat.Port);
        Assert.Equal(originalJob.ProfileSet.Power.Host, loadedJob.ProfileSet.Power.Host);
        Assert.Equal(originalJob.ProfileSet.Memory.Start, loadedJob.ProfileSet.Memory.Start);
        Assert.Equal(originalJob.ProfileSet.Payloads.BasePath, loadedJob.ProfileSet.Payloads.BasePath);
    }

    private static JobProfileSet CreateTestProfileSet()
    {
        return new JobProfileSet(
            Serial: new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One"),
            Socat: new SocatProfileRef(10102, Ephemeral: false),
            Power: new PowerProfileRef("192.168.1.100", 502, 1, 5),
            Memory: new MemoryRegionProfile(0x20000000, 0x10000),
            Payloads: new PayloadSetProfile("/payloads"),
            OutputPath: "/tmp/dumps"
        );
    }
}
