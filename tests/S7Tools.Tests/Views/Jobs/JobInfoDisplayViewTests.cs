using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Jobs;
using Xunit;

namespace S7Tools.Tests.Views.Jobs;

/// <summary>
/// Integration tests for JobInfoDisplayView to verify ViewModel integration and data binding.
/// Tests the complete integration between the ViewModel and service layer without UI dependencies.
/// </summary>
public sealed class JobInfoDisplayViewModelIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IProfileDetailsService> _mockProfileDetailsService;
    private readonly Mock<ISerialPortProfileService> _mockSerialService;
    private readonly Mock<ISocatProfileService> _mockSocatService;
    private readonly Mock<IPowerSupplyProfileService> _mockPowerService;
    private readonly Mock<IMemoryRegionProfileService> _mockMemoryRegionService;
    private readonly Mock<ILogger<JobInfoDisplayViewModel>> _mockLogger;
    private readonly JobInfoDisplayViewModel _viewModel;

    public JobInfoDisplayViewModelIntegrationTests()
    {
        // Setup mocks
        _mockProfileDetailsService = new Mock<IProfileDetailsService>();
        _mockSerialService = new Mock<ISerialPortProfileService>();
        _mockSocatService = new Mock<ISocatProfileService>();
        _mockPowerService = new Mock<IPowerSupplyProfileService>();
        _mockMemoryRegionService = new Mock<IMemoryRegionProfileService>();
        _mockLogger = new Mock<ILogger<JobInfoDisplayViewModel>>();

        // Setup service provider for dependency injection
        var services = new ServiceCollection();
        services.AddSingleton(_mockProfileDetailsService.Object);
        services.AddSingleton(_mockSerialService.Object);
        services.AddSingleton(_mockSocatService.Object);
        services.AddSingleton(_mockPowerService.Object);
        services.AddSingleton(_mockMemoryRegionService.Object);
        services.AddSingleton(_mockLogger.Object);
        _serviceProvider = services.BuildServiceProvider();

        // Create ViewModel with mocked dependencies
        _viewModel = new JobInfoDisplayViewModel(
            _mockProfileDetailsService.Object,
            _mockSerialService.Object,
            _mockSocatService.Object,
            _mockPowerService.Object,
            _mockMemoryRegionService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void ViewModel_WhenJobIsNull_ShouldDisplayNoJobSelectedMessage()
    {
        // Arrange & Act
        _viewModel.SelectedJob = null;

        // Assert
        _viewModel.JobBasicInfo.Should().Be("No job selected");
        _viewModel.HasMissingProfiles.Should().BeFalse();
        _viewModel.SerialProfileDetails.Should().BeNull();
        _viewModel.SocatProfileDetails.Should().BeNull();
        _viewModel.PowerSupplyProfileDetails.Should().BeNull();
        _viewModel.MemoryRegionProfileDetails.Should().BeNull();
    }

    [Fact]
    public async Task ViewModel_WhenJobIsSelected_ShouldUpdateBasicInfo()
    {
        // Arrange
        JobProfile testJob = CreateTestJob("Integration Test Job", "Test job for integration testing");

        // Act
        _viewModel.SelectedJob = testJob;

        // Wait for any async operations to complete
        await Task.Delay(100);

        // Assert
        _viewModel.JobBasicInfo.Should().Contain("Integration Test Job");
        _viewModel.JobBasicInfo.Should().Contain("Test job for integration testing");
    }

    [Fact]
    public void RefreshCommand_ShouldBeExecutable()
    {
        // Arrange
        JobProfile testJob = CreateTestJob("Test Job", "Description");
        _viewModel.SelectedJob = testJob;

        // Act & Assert
        _viewModel.RefreshCommand.CanExecute.Subscribe(canExecute =>
        {
            canExecute.Should().BeTrue();
        });

        // The command should be executable (doesn't need to await the execution)
        _viewModel.RefreshCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task ViewModel_WithMissingProfiles_ShouldShowWarnings()
    {
        // Arrange
        JobProfile jobWithMissingProfiles = CreateTestJob("Job with Missing Profiles", "Test job");
        jobWithMissingProfiles.SerialProfileId = 999; // Non-existent profile
        jobWithMissingProfiles.SocatProfileId = 999; // Non-existent profile

        // Setup mocks to return null for missing profiles
        _mockSerialService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SerialPortProfile?)null);
        _mockSocatService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocatProfile?)null);

        // Act
        _viewModel.SelectedJob = jobWithMissingProfiles;

        // Wait for async operations
        await Task.Delay(200);

        // Assert
        _viewModel.HasMissingProfiles.Should().BeTrue();
        _viewModel.MissingProfileWarnings.Should().NotBeEmpty();
    }

    [Fact]
    public void Disposal_ShouldCleanupResources()
    {
        // Arrange
        var disposedViewModel = new JobInfoDisplayViewModel(
            _mockProfileDetailsService.Object,
            _mockSerialService.Object,
            _mockSocatService.Object,
            _mockPowerService.Object,
            _mockMemoryRegionService.Object,
            _mockLogger.Object);

        // Act
        disposedViewModel.Dispose();

        // Assert - No exceptions should be thrown
        // This verifies that dispose is properly implemented
    }

    [Fact]
    public void ViewModel_ShouldProperlyInitializeAllProperties()
    {
        // Assert - Verify initial state
        _viewModel.Should().NotBeNull();
        _viewModel.SelectedJob.Should().BeNull();
        _viewModel.JobBasicInfo.Should().Be("No job selected");
        _viewModel.RefreshCommand.Should().NotBeNull();
        _viewModel.MissingProfileWarnings.Should().NotBeNull();
        _viewModel.MissingProfileWarnings.Should().BeEmpty();
    }

    [Fact]
    public void ViewModel_WhenJobCleared_ShouldResetAllProperties()
    {
        // Arrange
        JobProfile testJob = CreateTestJob("Test Job", "Description");
        _viewModel.SelectedJob = testJob;

        // Act
        _viewModel.SelectedJob = null;

        // Assert
        _viewModel.SelectedJob.Should().BeNull();
        _viewModel.JobBasicInfo.Should().Be("No job selected");
        _viewModel.SerialProfileDetails.Should().BeNull();
        _viewModel.SocatProfileDetails.Should().BeNull();
        _viewModel.PowerSupplyProfileDetails.Should().BeNull();
        _viewModel.MemoryRegionProfileDetails.Should().BeNull();
        _viewModel.HasMissingProfiles.Should().BeFalse();
        _viewModel.MissingProfileWarnings.Should().BeEmpty();
    }

    private static JobProfile CreateTestJob(string name, string description)
    {
        return new JobProfile
        {
            Id = 1,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsDefault = false,
            IsReadOnly = false,
            IsTemplate = false,
            SerialProfileId = 1,
            SocatProfileId = 1,
            PowerSupplyProfileId = 1
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}
