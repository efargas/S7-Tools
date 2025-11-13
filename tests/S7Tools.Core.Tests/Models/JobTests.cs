using S7Tools.Core.Models.Jobs;
using Xunit;

namespace S7Tools.Core.Tests.Models;

/// <summary>
/// Unit tests for Job entity validation and core behavior.
/// </summary>
public class JobTests
{
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
        Assert.Equal(1, job.Id);
        Assert.Equal("Test Job", job.Name);
        Assert.Equal("Test job description", job.Description);
        Assert.NotNull(job.ProfileSet);
        Assert.Equal("/tmp/dumps", job.OutputPath);
        Assert.Equal(JobState.Created, job.State);
        Assert.InRange(job.CreatedAt, beforeCreate, afterCreate);
        Assert.InRange(job.ModifiedAt, beforeCreate, afterCreate);
        Assert.Null(job.QueuedAt);
        Assert.Null(job.StartedAt);
        Assert.Null(job.CompletedAt);
        Assert.Equal(0.0, job.Progress);
        Assert.Equal(string.Empty, job.CurrentOperation);
        Assert.Null(job.ErrorMessage);
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
        Assert.Equal(1, original.Id);
        Assert.Equal("Original Job", original.Name);
        Assert.Equal(50.0, original.Progress);
        Assert.Equal("Installing stager", original.CurrentOperation);
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
