using System;
using System.Collections.Generic;
using System.Linq;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using Xunit;

namespace S7Tools.Core.Tests.Models.Jobs;

/// <summary>
/// Unit tests for JobProfile with memory region integration.
/// </summary>
public class JobProfileTests
{
    #region Test Data Helpers

    private static MemoryMappingProfile CreateTestProfile()
    {
        return new MemoryMappingProfile
        {
            Id = 1,
            Name = "Test Profile",
            Description = "Test memory mapping profile",
            Segments = new List<MemorySegment>
            {
                new()
                {
                    Name = ".text",
                    StartAddress = "0x08000000",
                    Size = 128 * 1024,
                    Type = MemorySegmentType.Flash,
                    IsSelected = true,
                    Description = "Program code"
                },
                new()
                {
                    Name = ".bss",
                    StartAddress = "0x20000000",
                    Size = 16 * 1024,
                    Type = MemorySegmentType.RAM,
                    IsSelected = true,
                    Description = "Uninitialized data"
                }
            }
        };
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void JobProfile_Constructor_SetsDefaultMemoryRegionProfileId()
    {
        // Act
        var profile = new JobProfile();

        // Assert
        Assert.Equal(0, profile.MemoryRegionProfileId);  // Default is 0
        Assert.NotNull(profile.MemoryRegion);
    }

    [Fact]
    public void JobProfile_Constructor_SetsDefaultMemoryRegion()
    {
        // Act
        var profile = new JobProfile();

        // Assert
        Assert.Equal(0x20000000u, profile.MemoryRegion.Start);
        Assert.Equal(0x1000u, profile.MemoryRegion.Length);
    }

    #endregion

    #region Memory Region Properties Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(42)]
    public void MemoryRegionProfileId_SetValue_StoresCorrectly(int profileId)
    {
        // Arrange
        var profile = new JobProfile();

        // Act
        profile.MemoryRegionProfileId = profileId;

        // Assert
        Assert.Equal(profileId, profile.MemoryRegionProfileId);
    }

    [Fact]
    public void MemoryRegion_SetValue_StoresCorrectly()
    {
        // Arrange
        var profile = new JobProfile();
        var memoryRegion = new MemoryRegionProfile(0x10000000, 0x2000);

        // Act
        profile.MemoryRegion = memoryRegion;

        // Assert
        Assert.Equal(memoryRegion, profile.MemoryRegion);
        Assert.Equal(0x10000000u, profile.MemoryRegion.Start);
        Assert.Equal(0x2000u, profile.MemoryRegion.Length);
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void CreateDefaultProfile_SetsMemoryRegionFields()
    {
        // Act
        var profile = JobProfile.CreateDefaultProfile();

        // Assert
        Assert.Equal(1, profile.MemoryRegionProfileId);
        Assert.NotNull(profile.MemoryRegion);
        Assert.Equal(0x20000000u, profile.MemoryRegion.Start);
        Assert.Equal(0x1000u, profile.MemoryRegion.Length);
    }

    [Fact]
    public void CreateUserProfile_SetsMemoryRegionFields()
    {
        // Act
        var profile = JobProfile.CreateUserProfile("Test Job", "Test description");

        // Assert
        Assert.Equal(1, profile.MemoryRegionProfileId);
        Assert.NotNull(profile.MemoryRegion);
        Assert.Equal(0x20000000u, profile.MemoryRegion.Start);
        Assert.Equal(0x1000u, profile.MemoryRegion.Length);
        Assert.Equal("Test Job", profile.Name);
        Assert.Equal("Test description", profile.Description);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithInvalidMemoryRegionProfileId_ReturnsError()
    {
        // Arrange
        var profile = new JobProfile
        {
            Name = "Test Job",
            MemoryRegionProfileId = 0, // Invalid
            MemoryRegion = new MemoryRegionProfile(0x20000000, 0x1000)
        };

        // Act
        var errors = profile.Validate();

        // Assert
        Assert.Contains(errors, error => error.Contains("Valid memory region profile must be selected"));
    }

    [Fact]
    public void Validate_WithNullMemoryRegion_ReturnsError()
    {
        // Arrange
        var profile = new JobProfile
        {
            Name = "Test Job",
            MemoryRegionProfileId = 1,
            MemoryRegion = null!
        };

        // Act
        var errors = profile.Validate();

        // Assert
        Assert.Contains(errors, error => error.Contains("Memory region configuration is required"));
    }

    [Fact]
    public void Validate_WithZeroLengthMemoryRegion_ReturnsError()
    {
        // Arrange
        var profile = new JobProfile
        {
            Name = "Test Job",
            MemoryRegionProfileId = 1,
            MemoryRegion = new MemoryRegionProfile(0x20000000, 0) // Zero length
        };

        // Act
        var errors = profile.Validate();

        // Assert
        Assert.Contains(errors, error => error.Contains("Memory region length must be greater than 0"));
    }

    [Fact]
    public void Validate_WithValidMemoryRegion_NoMemoryErrors()
    {
        // Arrange
        var profile = new JobProfile
        {
            Name = "Test Job",
            MemoryRegionProfileId = 1,
            MemoryRegion = new MemoryRegionProfile(0x20000000, 0x1000),
            SerialProfileId = 1,
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            OutputPath = "/tmp/output"
        };

        // Act
        var errors = profile.Validate();

        // Assert
        // Should not contain memory-related errors
        Assert.DoesNotContain(errors, error => error.Contains("Memory region"));
    }

    #endregion

    #region ToExecutionJob Tests

    [Fact]
    public void ToExecutionJob_UsesMemoryRegionFromProfile()
    {
        // Arrange
        var memoryRegion = new MemoryRegionProfile(0x10000000, 0x2000);
        var profile = new JobProfile
        {
            Name = "Test Job",
            MemoryRegion = memoryRegion,
            MemoryRegionProfileId = 5,
            SerialProfileId = 1,
            SocatProfileId = 1,
            PowerSupplyProfileId = 1,
            OutputPath = "/tmp/output"
        };

        // Act
        var job = profile.ToExecutionJob();

        // Assert
        Assert.Equal(memoryRegion, job.Profiles.Memory);
        Assert.Equal(0x10000000u, job.Profiles.Memory.Start);
        Assert.Equal(0x2000u, job.Profiles.Memory.Length);
    }

    [Fact]
    public void ToExecutionJob_GeneratesResourceWithMemoryProfileId()
    {
        // Arrange
        var profile = new JobProfile
        {
            Name = "Test Job",
            MemoryRegionProfileId = 42,
            SerialProfileId = 1,
            SocatProfileId = 2,
            PowerSupplyProfileId = 3,
            OutputPath = "/tmp/output"
        };

        // Act
        var job = profile.ToExecutionJob();

        // Assert
        var memoryResources = job.Resources.Where(r => r.Kind == "memory").ToList();
        Assert.Single(memoryResources);
        Assert.Equal("42", memoryResources.First().Id);
    }

    #endregion

    #region Resource Generation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(999)]
    public void GenerateResourceKeys_IncludesMemoryResource(int memoryProfileId)
    {
        // Arrange
        var profile = new JobProfile
        {
            MemoryRegionProfileId = memoryProfileId,
            SerialProfileId = 1,
            SocatProfileId = 2,
            PowerSupplyProfileId = 3
        };

        // Act
        var job = profile.ToExecutionJob();
        var resources = job.Resources.ToList();

        // Assert
        var memoryResources = job.Resources.Where(r => r.Kind == "memory").ToList();
        Assert.Single(memoryResources);
        Assert.Equal(memoryProfileId.ToString(), memoryResources.First().Id);
    }

    [Fact]
    public void GenerateResourceKeys_ContainsAllRequiredResources()
    {
        // Arrange
        var profile = new JobProfile
        {
            MemoryRegionProfileId = 5,
            SerialProfileId = 1,
            SocatProfileId = 2,
            PowerSupplyProfileId = 3
        };

        // Act
        var job = profile.ToExecutionJob();
        var resourceKinds = job.Resources.Select(r => r.Kind).ToHashSet();

        // Assert
        Assert.Contains("serial", resourceKinds);
        Assert.Contains("tcp", resourceKinds);
        Assert.Contains("power", resourceKinds);
        Assert.Contains("memory", resourceKinds);
        Assert.Equal(4, resourceKinds.Count);
    }

    #endregion
}
