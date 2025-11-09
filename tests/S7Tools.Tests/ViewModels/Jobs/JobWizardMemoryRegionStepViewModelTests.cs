using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
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
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel);
        Assert.NotNull(viewModel.AvailableProfiles);
        Assert.NotNull(viewModel.SelectedSegments);
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.IsStepValid);
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
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();
        var propertyChanged = false;

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedProfile))
                propertyChanged = true;
        };

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.True(propertyChanged);
        Assert.Equal(profile, viewModel.SelectedProfile);
    }

    [Fact]
    public void ProfileSummary_WithNoProfile_ShouldReturnNoProfileMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.Equal("No profile selected", viewModel.ProfileSummary);
    }

    [Fact]
    public void ProfileSummary_WithProfile_ShouldReturnProfileSummary()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.Contains(profile.Name, viewModel.ProfileSummary);
    }

    [Fact]
    public void SelectedSegmentCount_WithNoProfile_ShouldReturnZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.Equal(0, viewModel.SelectedSegmentCount);
    }

    [Fact]
    public void SelectedSegmentCount_WithProfile_ShouldReturnCorrectCount()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.Equal(1, viewModel.SelectedSegmentCount); // Only .bss is selected in sample profile
    }

    [Fact]
    public void TotalSelectedSize_WithNoProfile_ShouldReturnZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.Equal(0, viewModel.TotalSelectedSize);
    }

    [Fact]
    public void TotalSelectedSize_WithProfile_ShouldReturnCorrectSize()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.Equal(16 * 1024, viewModel.TotalSelectedSize); // .bss segment size
    }

    [Fact]
    public void TotalSelectedSizeFormatted_WithSmallSize_ShouldReturnBytesFormat()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();
        profile.Segments.First(s => s.IsSelected).Size = 512; // 512 bytes

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.Equal("512 bytes", viewModel.TotalSelectedSizeFormatted);
    }

    [Fact]
    public void TotalSelectedSizeFormatted_WithKilobyteSize_ShouldReturnKBFormat()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

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
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.False(viewModel.IsStepValid);
    }

    [Fact]
    public void IsStepValid_WithProfileAndSelectedSegments_ShouldReturnTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.True(viewModel.IsStepValid);
    }

    [Fact]
    public void ValidationMessage_WithNoProfile_ShouldReturnSelectProfileMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.Equal("Please select a memory region profile", viewModel.ValidationMessage);
    }

    [Fact]
    public void ValidationMessage_WithValidProfile_ShouldReturnValidMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.Equal("Memory region configuration is valid", viewModel.ValidationMessage);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void GetSelectedProfileId_WithNoProfile_ShouldReturnNull()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.GetSelectedProfileId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSelectedProfileId_WithProfile_ShouldReturnProfileId()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile(id: 42);

        // Act
        viewModel.SelectedProfile = profile;
        var result = viewModel.GetSelectedProfileId();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void SetSelectedProfileId_WithNullId_ShouldClearSelection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();
        viewModel.AvailableProfiles.Add(profile);
        viewModel.SelectedProfile = profile;

        // Act
        var result = viewModel.SetSelectedProfileId(null);

        // Assert
        Assert.True(result);
        Assert.Null(viewModel.SelectedProfile);
    }

    [Fact]
    public void SetSelectedProfileId_WithValidId_ShouldSelectProfile()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile(id: 42);
        viewModel.AvailableProfiles.Add(profile);

        // Act
        var result = viewModel.SetSelectedProfileId(42);

        // Assert
        Assert.True(result);
        Assert.Equal(profile, viewModel.SelectedProfile);
    }

    [Fact]
    public void SetSelectedProfileId_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile(id: 42);
        viewModel.AvailableProfiles.Add(profile);

        // Act
        var result = viewModel.SetSelectedProfileId(99);

        // Assert
        Assert.False(result);
        Assert.Null(viewModel.SelectedProfile);
    }

    [Fact]
    public void ValidateStep_WithNoProfile_ShouldReturnError()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var errors = viewModel.ValidateStep();

        // Assert
        Assert.Single(errors);
        Assert.Contains("Memory region profile must be selected", errors);
    }

    [Fact]
    public void ValidateStep_WithProfileButNoSelectedSegments_ShouldReturnError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();
        // Clear all segment selections
        foreach (var segment in profile.Segments)
        {
            segment.IsSelected = false;
        }

        // Act
        viewModel.SelectedProfile = profile;
        var errors = viewModel.ValidateStep();

        // Assert
        Assert.Contains("Selected profile must have at least one segment marked for dumping", errors);
    }

    [Fact]
    public void ValidateStep_WithValidProfileAndSelectedSegments_ShouldReturnNoErrors()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;
        var errors = viewModel.ValidateStep();

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region Reactive Property Tests

    [Fact]
    public void IsStepValid_ShouldUpdateWhenProfileChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var initialValid = viewModel.IsStepValid;
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.False(initialValid);
        Assert.True(viewModel.IsStepValid);
    }

    [Fact]
    public void SelectedSegments_ShouldUpdateWhenProfileChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var profile = CreateSampleProfile();

        // Act
        viewModel.SelectedProfile = profile;

        // Assert
        Assert.Single(viewModel.SelectedSegments);
        Assert.Equal(".bss", viewModel.SelectedSegments.First().Name);
    }

    #endregion

    #region Async Method Tests

    [Fact]
    public async Task RefreshProfilesAsync_ShouldCallLoadProfilesAsync()
    {
        // Arrange
        var profiles = new[] { CreateSampleProfile() };
        _memoryRegionService.GetAllAsync().Returns(profiles);
        _uiThreadService.InvokeOnUIThreadAsync(Arg.Any<Action>()).Returns(Task.CompletedTask);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.RefreshProfilesAsync();

        // Assert
        await _memoryRegionService.Received(2).GetAllAsync(); // Once in constructor, once in refresh
    }

    #endregion
}
