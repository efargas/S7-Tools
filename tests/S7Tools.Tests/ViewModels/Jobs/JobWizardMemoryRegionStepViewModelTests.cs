using FluentAssertions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Jobs;
using Xunit;

namespace S7Tools.Tests.ViewModels.Jobs;

/// <summary>
/// Unit tests for JobWizardMemoryRegionStepViewModel.
/// </summary>
public class JobWizardMemoryRegionStepViewModelTests
{
    private readonly IMemoryRegionProfileService _memoryRegionService;
    private readonly IUIThreadService _uiThreadService;
    private readonly ILogger<JobWizardMemoryRegionStepViewModel> _logger;

    public JobWizardMemoryRegionStepViewModelTests()
    {
        _memoryRegionService = Substitute.For<IMemoryRegionProfileService>();
        _uiThreadService = Substitute.For<IUIThreadService>();
        _logger = NullLogger<JobWizardMemoryRegionStepViewModel>.Instance;
    }

    private JobWizardMemoryRegionStepViewModel CreateViewModel()
    {
        return new JobWizardMemoryRegionStepViewModel(_logger, _memoryRegionService, _uiThreadService);
    }

    private MemoryMappingProfile CreateSampleProfile(int id = 1, string name = "Test Profile", bool isDefault = true)
    {
        return new MemoryMappingProfile
        {
            Id = id,
            Name = name,
            Description = "Test profile description",
            IsDefault = isDefault,
            Segments = new System.Collections.Generic.List<MemorySegment>
            {
                new()
                {
                    Name = ".text",
                    StartAddress = "0x08000000",
                    Size = 128 * 1024,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Program code section"
                },
                new()
                {
                    Name = ".bss",
                    StartAddress = "0x20000000",
                    Size = 16 * 1024,
                    Type = MemorySegmentType.RAM,
                    IsSelected = true,
                    Description = "Uninitialized data section"
                }
            }
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Act
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.AvailableProfiles.Should().NotBeNull();
        viewModel.SelectedSegments.Should().NotBeNull();
        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsStepValid.Should().BeFalse();
        // Status may be set during profile loading, so we don't assert it's empty
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new JobWizardMemoryRegionStepViewModel(null!, _memoryRegionService, _uiThreadService));
    }

    [Fact]
    public void Constructor_WithNullMemoryRegionService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new JobWizardMemoryRegionStepViewModel(_logger, null!, _uiThreadService));
    }

    [Fact]
    public void Constructor_WithNullUIThreadService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new JobWizardMemoryRegionStepViewModel(_logger, _memoryRegionService, null!));
    }

    #endregion

    #region Property Tests

    [Fact]
    public void SelectedProfile_WhenSet_ShouldRaisePropertyChanged()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();
        bool propertyChanged = false;

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedProfile))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        propertyChanged.Should().BeTrue();
        viewModel.SelectedProfile.Should().Be(profile);
    }

    [Fact]
    public void ProfileSummary_WithNoProfile_ShouldReturnNoProfileMessage()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act & Assert
        viewModel.ProfileSummary.Should().Be("No profile selected");
    }

    [Fact]
    public void ProfileSummary_WithProfile_ShouldReturnProfileSummary()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.Contains(profile.Name, viewModel.ProfileSummary);
    }

    [Fact]
    public void SelectedSegmentCount_WithNoProfile_ShouldReturnZero()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act & Assert
        viewModel.SelectedSegmentCount.Should().Be(0);
    }

    [Fact]
    public void SelectedSegmentCount_WithProfile_ShouldReturnCorrectCount()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        viewModel.SelectedSegmentCount.Should().Be(1); // Only .bss is selected in sample profile
    }

    [Fact]
    public void TotalSelectedSize_WithNoProfile_ShouldReturnZero()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act & Assert
        viewModel.TotalSelectedSize.Should().Be(0);
    }

    [Fact]
    public void TotalSelectedSize_WithProfile_ShouldReturnCorrectSize()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        viewModel.TotalSelectedSize.Should().Be(16 * 1024); // .bss segment size
    }

    [Fact]
    public void TotalSelectedSizeFormatted_WithSmallSize_ShouldReturnBytesFormat()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();
        profile.Segments.First(s => s.IsSelected).Size = 512; // 512 bytes

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        viewModel.TotalSelectedSizeFormatted.Should().Be("512 bytes");
    }

    [Fact]
    public void TotalSelectedSizeFormatted_WithKilobyteSize_ShouldReturnKBFormat()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        // Accept both "16.0 KB" and "16,0 KB" depending on system culture
        Assert.True(viewModel.TotalSelectedSizeFormatted == "16.0 KB" ||
                   viewModel.TotalSelectedSizeFormatted == "16,0 KB",
                   $"Expected '16.0 KB' or '16,0 KB', but got '{viewModel.TotalSelectedSizeFormatted}'");
    }

    [Fact]
    public void IsStepValid_WithNoProfile_ShouldReturnFalse()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act & Assert
        viewModel.IsStepValid.Should().BeFalse();
    }

    [Fact]
    public void IsStepValid_WithProfileAndSelectedSegments_ShouldReturnTrue()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        viewModel.IsStepValid.Should().BeTrue();
    }

    [Fact]
    public void ValidationMessage_WithNoProfile_ShouldReturnSelectProfileMessage()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act & Assert
        viewModel.ValidationMessage.Should().Be("Please select a memory region profile");
    }

    [Fact]
    public void ValidationMessage_WithValidProfile_ShouldReturnValidMessage()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert - Valid message should indicate which segment is selected
        Assert.StartsWith("✓ Memory segment", viewModel.ValidationMessage);
        Assert.Contains("selected for dumping", viewModel.ValidationMessage);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void GetSelectedProfileId_WithNoProfile_ShouldReturnNull()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act
        int? result = viewModel.GetSelectedProfileId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetSelectedProfileId_WithProfile_ShouldReturnProfileId()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile(id: 42);

        // Act
        viewModel.SelectedProfile = profile;
        int? result = viewModel.GetSelectedProfileId();

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void SetSelectedProfileId_WithNullId_ShouldClearSelection()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();
        viewModel.AvailableProfiles.Add(profile);
        viewModel.SelectedProfile = profile;

        // Act
        bool result = viewModel.SetSelectedProfileId(null);

        // Assert
        result.Should().BeTrue();
        viewModel.SelectedProfile.Should().BeNull();
    }

    [Fact]
    public void SetSelectedProfileId_WithValidId_ShouldSelectProfile()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile(id: 42);
        viewModel.AvailableProfiles.Add(profile);

        // Act
        bool result = viewModel.SetSelectedProfileId(42);

        // Assert
        result.Should().BeTrue();
        viewModel.SelectedProfile.Should().Be(profile);
    }

    [Fact]
    public void SetSelectedProfileId_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile(id: 42);
        viewModel.AvailableProfiles.Add(profile);

        // Act
        bool result = viewModel.SetSelectedProfileId(99);

        // Assert
        result.Should().BeFalse();
        viewModel.SelectedProfile.Should().BeNull();
    }

    [Fact]
    public void ValidateStep_WithNoProfile_ShouldReturnError()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act
        List<string> errors = viewModel.ValidateStep();

        // Assert
        errors.Should().ContainSingle();
        Assert.Contains("Memory region profile must be selected", errors);
    }

    [Fact]
    public void ValidateStep_WithProfileButNoSelectedSegments_ShouldReturnError()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();
        // Clear all segment selections
        foreach (MemorySegment segment in profile.Segments)
        {
            segment.IsSelected = false;
        }

        // Act
        viewModel.SelectedProfile = profile;
        List<string> errors = viewModel.ValidateStep();

        // Assert
        Assert.Contains("Exactly one segment must be selected for dumping", errors);
    }

    [Fact]
    public void ValidateStep_WithValidProfileAndSelectedSegments_ShouldReturnNoErrors()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;
        List<string> errors = viewModel.ValidateStep();

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Reactive Property Tests

    [Fact]
    public void IsStepValid_ShouldUpdateWhenProfileChanges()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        bool initialValid = viewModel.IsStepValid;
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        initialValid.Should().BeFalse();
        viewModel.IsStepValid.Should().BeTrue();
    }

    [Fact]
    public void SelectedSegments_ShouldUpdateWhenProfileChanges()
    {
        // Arrange
        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();
        MemoryMappingProfile profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        viewModel.SelectedSegments.Should().ContainSingle();
        viewModel.SelectedSegments.First().Name.Should().Be(".bss");
    }

    #endregion

    #region Async Method Tests

    [Fact]
    public async Task RefreshProfilesAsync_ShouldCallLoadProfilesAsync()
    {
        // Arrange
        MemoryMappingProfile[] profiles = new[] { CreateSampleProfile() };
        _memoryRegionService.GetAllAsync().Returns(profiles);
        _uiThreadService.InvokeOnUIThreadAsync(Arg.Any<Action>()).Returns(Task.CompletedTask);

        JobWizardMemoryRegionStepViewModel viewModel = CreateViewModel();

        // Act
        await viewModel.RefreshProfilesAsync();

        // Assert
        await _memoryRegionService.Received(2).GetAllAsync(); // Once in constructor, once in refresh
    }

    #endregion
}
