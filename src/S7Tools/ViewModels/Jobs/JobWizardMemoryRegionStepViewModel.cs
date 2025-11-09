using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Jobs;

/// <summary>
/// ViewModel for the memory region profile selection step in the job wizard.
/// </summary>
/// <remarks>
/// This step allows users to select a memory region profile which defines
/// the memory segments to be dumped during job execution. The step validates
/// that the selected profile has at least one selected segment for correlative
/// memory operations.
/// </remarks>
public class JobWizardMemoryRegionStepViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly ILogger<JobWizardMemoryRegionStepViewModel> _logger;
    private readonly IMemoryRegionProfileService _memoryRegionService;
    private readonly IUIThreadService _uiThreadService;
    private readonly CompositeDisposable _disposables = new();

    private bool _isBusy;
    private string _status = string.Empty;
    private MemoryMappingProfile? _selectedProfile;
    private bool _isStepValid;

    #endregion

    #region Constructor

    public JobWizardMemoryRegionStepViewModel(
        ILogger<JobWizardMemoryRegionStepViewModel> logger,
        IMemoryRegionProfileService memoryRegionService,
        IUIThreadService uiThreadService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryRegionService = memoryRegionService ?? throw new ArgumentNullException(nameof(memoryRegionService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));

        AvailableProfiles = new ObservableCollection<MemoryMappingProfile>();
        SelectedSegments = new ObservableCollection<MemorySegment>();

        SetupValidation();

        // Load profiles when initialized
        _ = LoadProfilesAsync();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the collection of available memory region profiles.
    /// </summary>
    public ObservableCollection<MemoryMappingProfile> AvailableProfiles { get; }

    /// <summary>
    /// Gets the collection of selected memory segments from the current profile.
    /// </summary>
    public ObservableCollection<MemorySegment> SelectedSegments { get; }

    /// <summary>
    /// Gets or sets the selected memory region profile.
    /// </summary>
    public MemoryMappingProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            // Unsubscribe from previous profile's segments
            UnsubscribeFromSegmentChanges();

            this.RaiseAndSetIfChanged(ref _selectedProfile, value);

            // Subscribe to new profile's segments and update UI
            SubscribeToSegmentChanges();
            UpdateSelectedSegments();

            this.RaisePropertyChanged(nameof(ProfileSummary));
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
            this.RaisePropertyChanged(nameof(TotalSelectedSize));
            this.RaisePropertyChanged(nameof(HasContiguousSelection));

            _logger.LogDebug("SelectedProfile changed to {ProfileName} (ID: {ProfileId}) with {SegmentCount} segments",
                value?.Name ?? "null", value?.Id ?? -1, value?.Segments?.Count ?? 0);
        }
    }

    /// <summary>
    /// Gets whether this step is currently busy loading or processing.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    /// <summary>
    /// Gets the current status message for this step.
    /// </summary>
    public string Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    /// <summary>
    /// Gets whether this step has valid selections and can proceed to the next step.
    /// </summary>
    public bool IsStepValid
    {
        get => _isStepValid;
        private set => this.RaiseAndSetIfChanged(ref _isStepValid, value);
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets a summary of the selected profile and its configuration.
    /// </summary>
    public string ProfileSummary
    {
        get
        {
            if (SelectedProfile == null)
            {
                return "No profile selected";
            }

            return SelectedProfile.GetSummary();
        }
    }

    /// <summary>
    /// Gets the number of selected segments in the current profile.
    /// </summary>
    public int SelectedSegmentCount
    {
        get { return SelectedProfile?.SelectedSegments.Count() ?? 0; }
    }

    /// <summary>
    /// Gets the total size of selected segments in bytes.
    /// </summary>
    public long TotalSelectedSize
    {
        get { return SelectedProfile?.TotalSelectedSize ?? 0; }
    }

    /// <summary>
    /// Gets a human-readable size of the total selected memory.
    /// </summary>
    public string TotalSelectedSizeFormatted
    {
        get
        {
            long size = TotalSelectedSize;
            if (size == 0)
            {
                return "0 bytes";
            }

            if (size < 1024)
            {
                return $"{size} bytes";
            }

            if (size < 1024 * 1024)
            {
                return $"{size / 1024.0:F1} KB";
            }

            return $"{size / (1024.0 * 1024.0):F1} MB";
        }
    }

    /// <summary>
    /// Gets whether the selected segments form a contiguous memory range.
    /// </summary>
    public bool HasContiguousSelection
    {
        get { return SelectedProfile?.HasContiguousSelection() ?? false; }
    }

    /// <summary>
    /// Gets a validation message for the current step state.
    /// </summary>
    public string ValidationMessage
    {
        get
        {
            if (SelectedProfile == null)
            {
                return "Please select a memory region profile";
            }

            if (SelectedSegmentCount == 0)
            {
                return "Selected profile has no segments marked for dumping";
            }

            if (!HasContiguousSelection)
            {
                return "Warning: Selected segments are not contiguous";
            }

            return "Memory region configuration is valid";
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the ID of the currently selected profile, or null if no profile is selected.
    /// </summary>
    /// <returns>The profile ID, or null if no selection.</returns>
    public int? GetSelectedProfileId()
    {
        return SelectedProfile?.Id;
    }

    /// <summary>
    /// Sets the selected profile by ID.
    /// </summary>
    /// <param name="profileId">The ID of the profile to select.</param>
    /// <returns>True if the profile was found and selected, false otherwise.</returns>
    public bool SetSelectedProfileId(int? profileId)
    {
        if (!profileId.HasValue)
        {
            SelectedProfile = null;
            return true;
        }

        MemoryMappingProfile? profile = AvailableProfiles.FirstOrDefault(p => p.Id == profileId.Value);
        if (profile != null)
        {
            SelectedProfile = profile;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Validates the current step configuration.
    /// </summary>
    /// <returns>A list of validation errors, or empty if valid.</returns>
    public List<string> ValidateStep()
    {
        var errors = new List<string>();

        if (SelectedProfile == null)
        {
            errors.Add("Memory region profile must be selected");
            return errors;
        }

        if (SelectedSegmentCount == 0)
        {
            errors.Add("Selected profile must have at least one segment marked for dumping");
        }

        // Validate the profile itself
        List<string> profileErrors = SelectedProfile.Validate();
        errors.AddRange(profileErrors);

        return errors;
    }

    /// <summary>
    /// Refreshes the available profiles from the service.
    /// </summary>
    /// <returns>A task representing the refresh operation.</returns>
    public async Task RefreshProfilesAsync()
    {
        await LoadProfilesAsync();
    }

    #endregion

    #region Private Methods

    private void SetupValidation()
    {
        // Update validation when profile selection changes
        this.WhenAnyValue(
                x => x.SelectedProfile,
                x => x.SelectedSegmentCount)
            .Select(_ => ValidateStepConfiguration())
            .Subscribe(isValid => IsStepValid = isValid)
            .DisposeWith(_disposables);
    }

    private bool ValidateStepConfiguration()
    {
        return SelectedProfile != null && SelectedSegmentCount > 0;
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            IsBusy = true;
            Status = "Loading memory region profiles...";
            _logger.LogInformation("Loading memory region profiles for wizard step");

            IEnumerable<MemoryMappingProfile> profiles = await _memoryRegionService.GetAllAsync().ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                int? currentSelectionId = SelectedProfile?.Id;

                AvailableProfiles.Clear();
                foreach (MemoryMappingProfile profile in profiles)
                {
                    AvailableProfiles.Add(profile);
                }

                // Try to preserve selection, or select default, or select first
                if (currentSelectionId.HasValue)
                {
                    SelectedProfile = AvailableProfiles.FirstOrDefault(p => p.Id == currentSelectionId.Value);
                }

                if (SelectedProfile == null)
                {
                    SelectedProfile = AvailableProfiles.FirstOrDefault(p => p.IsDefault)
                                   ?? AvailableProfiles.FirstOrDefault();
                }

                Status = $"Loaded {AvailableProfiles.Count} profile(s)";
                _logger.LogInformation("Loaded {ProfileCount} memory region profiles", AvailableProfiles.Count);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load memory region profiles for wizard step");
            Status = $"Error loading profiles: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateSelectedSegments()
    {
        SelectedSegments.Clear();

        if (SelectedProfile?.Segments != null)
        {
            foreach (MemorySegment segment in SelectedProfile.Segments.Where(s => s.IsSelected))
            {
                SelectedSegments.Add(segment);
            }
        }

        this.RaisePropertyChanged(nameof(TotalSelectedSizeFormatted));
        this.RaisePropertyChanged(nameof(ValidationMessage));

        _logger.LogDebug("UpdateSelectedSegments completed: {SelectedCount} segments selected", SelectedSegments.Count);
    }

    /// <summary>
    /// Subscribes to memory segment changes to update computed properties.
    /// </summary>
    private void SubscribeToSegmentChanges()
    {
        if (SelectedProfile?.Segments != null)
        {
            _logger.LogDebug("Subscribing to segment property changes for profile {ProfileName} with {SegmentCount} segments",
                SelectedProfile.Name, SelectedProfile.Segments.Count);

            foreach (MemorySegment segment in SelectedProfile.Segments)
            {
                segment.PropertyChanged += OnSegmentPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Unsubscribes from memory segment change events.
    /// </summary>
    private void UnsubscribeFromSegmentChanges()
    {
        if (SelectedProfile?.Segments != null)
        {
            foreach (MemorySegment segment in SelectedProfile.Segments)
            {
                segment.PropertyChanged -= OnSegmentPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Handles property changes in memory segments to update computed properties.
    /// </summary>
    private void OnSegmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MemorySegment.IsSelected))
        {
            _logger.LogDebug("Segment IsSelected property changed, updating job wizard step properties");

            // Update all computed properties that depend on segment selection
            UpdateSelectedSegments();
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
            this.RaisePropertyChanged(nameof(TotalSelectedSize));
            this.RaisePropertyChanged(nameof(HasContiguousSelection));
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeFromSegmentChanges();
            _disposables.Dispose();
        }
    }

    #endregion
}
