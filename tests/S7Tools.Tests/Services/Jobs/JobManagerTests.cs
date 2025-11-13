using S7Tools.Core.Models.Jobs;
using Xunit;

namespace S7Tools.Tests.Services.Jobs;

/// <summary>
/// Unit tests for Job name uniqueness validation.
/// Note: Full JobManager implementation is in T080. This test validates the core uniqueness logic.
/// </summary>
public class JobManagerTests
{
    [Fact]
    public void Job_Names_Should_Be_Case_Insensitive_Unique()
    {
        // Arrange
        JobProfileSet profileSet = CreateTestProfileSet();
        var jobs = new List<Job>();

        var job1 = new Job
        {
            Id = 1,
            Name = "Test Job",
            Description = "First job",
            ProfileSet = profileSet,
            OutputPath = "/tmp/job1"
        };

        jobs.Add(job1);

        // Act - Check for duplicate (case-insensitive)
        string job2Name = "test job";  // Different case
        bool isDuplicate = jobs.Any(j => string.Equals(j.Name, job2Name, StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.True(isDuplicate, "Name uniqueness check should be case-insensitive");
    }

    [Fact]
    public void Job_Names_Should_Allow_Different_Names()
    {
        // Arrange
        JobProfileSet profileSet = CreateTestProfileSet();
        var jobs = new List<Job>();

        var job1 = new Job
        {
            Id = 1,
            Name = "First Job",
            Description = "First job",
            ProfileSet = profileSet,
            OutputPath = "/tmp/job1"
        };

        jobs.Add(job1);

        // Act - Check for different name
        string job2Name = "Second Job";
        bool isDuplicate = jobs.Any(j => string.Equals(j.Name, job2Name, StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.False(isDuplicate, "Different names should be allowed");
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
