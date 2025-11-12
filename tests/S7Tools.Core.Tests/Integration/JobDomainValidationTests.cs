using FluentAssertions;
using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Tests.Integration;

/// <summary>
/// T046-T047: Integration tests verifying end-to-end domain model validation.
/// Constitutional test-first requirement - tests created before implementation.
/// </summary>
public class JobDomainValidationTests
{
    /// <summary>
    /// T046: Job state transition validation - verifies state machine rules.
    /// Tests: Created → Queued → Running → Completed with timestamps.
    /// </summary>
    [Fact]
    public void Job_StateTransition_ShouldFollowStateMachine()
    {
        // Arrange - Created state
        Job job = new()
        {
            Id = 1,
            Name = "Test Job",
            Description = "Integration test",
            ProfileSet = new JobProfileSet(
                Serial: new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One"),
                Socat: new SocatProfileRef(8080, Ephemeral: true),
                Power: new PowerProfileRef("192.168.1.100", 502, 0, DelaySeconds: 2),
                Memory: new MemoryRegionProfile(0x20000000, 0x1000),
                Payloads: new PayloadSetProfile("/tmp/payloads"),
                OutputPath: "/tmp/dumps"
            ),
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        // Act - Transition through states
        Job queued = job with { State = JobState.Queued, QueuedAt = DateTime.UtcNow };
        Job running = queued with { State = JobState.Running, StartedAt = DateTime.UtcNow };
        Job completed = running with { State = JobState.Completed, Progress = 100.0, CompletedAt = DateTime.UtcNow };

        // Assert
        job.State.Should().Be(JobState.Created);
        queued.State.Should().Be(JobState.Queued);
        queued.QueuedAt.Should().NotBeNull();
        running.State.Should().Be(JobState.Running);
        running.StartedAt.Should().NotBeNull();
        completed.State.Should().Be(JobState.Completed);
        completed.Progress.Should().Be(100.0);
        completed.CompletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// T047: Resource key extraction from JobProfileSet - verifies resource coordination integration.
    /// Tests: Job.Resources property correctly extracts serial/tcp/modbus keys.
    /// </summary>
    [Fact]
    public void Job_Resources_ShouldExtractKeysFromProfileSet()
    {
        // Arrange
        Job job = new()
        {
            Id = 1,
            Name = "Resource Test Job",
            Description = "Test resource extraction",
            ProfileSet = new JobProfileSet(
                Serial: new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One"),
                Socat: new SocatProfileRef(8080, Ephemeral: true),
                Power: new PowerProfileRef("192.168.1.100", 502, 0, DelaySeconds: 2),
                Memory: new MemoryRegionProfile(0x20000000, 0x1000),
                Payloads: new PayloadSetProfile("/tmp/payloads"),
                OutputPath: "/tmp/dumps"
            ),
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        IEnumerable<ResourceKey> resources = job.Resources;

        // Assert
        resources.Should().NotBeEmpty();
        resources.Should().Contain(k => k.Kind == "serial" && k.Id == "/dev/ttyUSB0");
        resources.Should().Contain(k => k.Kind == "tcp" && k.Id == "8080");
        resources.Should().Contain(k => k.Kind == "modbus" && k.Id == "192.168.1.100:502");
    }
}
