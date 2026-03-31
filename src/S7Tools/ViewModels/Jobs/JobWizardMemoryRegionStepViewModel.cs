using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Resources.Strings;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

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
    private int _dumpCount = 1;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JobWizardMemoryRegionStepViewModel"/> class.
    /// </summary>
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

            _logger.LogDebug("SelectedProfile changed to {ProfileName} (ID: {ProfileId}), SegmentCount: {SegmentCount}",
                value?.Name ?? "null",
                value?.Id ?? -1,
                value?.Segments?.Count ?? 0);

            // Subscribe to new profile's segments and update UI
            SubscribeToSegmentChanges();
            UpdateSelectedSegments();

            this.RaisePropertyChanged(nameof(ProfileSummary));
            this.RaisePropertyChanged(nameof(SegmentCount));
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
            this.RaisePropertyChanged(nameof(TotalSelectedSize));
            this.RaisePropertyChanged(nameof(HasContiguousSelection));
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

    /// <summary>
    /// Gets or sets the number of times to perform the memory dump.
    /// </summary>
    public int DumpCount
    {
        get => _dumpCount;
        set
        {
            this.RaiseAndSetIfChanged(ref _dumpCount, value);
            this.RaisePropertyChanged(nameof(ValidationMessage));
        }
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
    /// Gets the total number of segments in the selected profile.
    /// </summary>
    public int SegmentCount
    {
        get { return SelectedProfile?.SegmentCount ?? 0; }
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
                return "Please select exactly one segment to dump (click checkbox in Sel column)";
            }

            if (SelectedSegmentCount > 1)
            {
                return "Only one segment can be selected for this job";
            }

            return $"✓ Memory segment '{SelectedSegments.FirstOrDefault()?.Name}' selected for dumping ({DumpCount}x)";
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
            errors.Add(UIStrings.Validation_MemoryRegionProfileRequired);
            return errors;
        }

        if (SelectedSegmentCount == 0)
        {
            errors.Add(UIStrings.Validation_ExactlyOneSegmentRequired);
        }
        else if (SelectedSegmentCount > 1)
        {
            errors.Add(UIStrings.Validation_OnlyOneSegmentAllowed);
        }

        if (DumpCount < 1)
        {
            errors.Add("Dump count must be at least 1");
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
                x => x.SelectedSegmentCount,
                x => x.DumpCount)
            .Select(_ => ValidateStepConfiguration())
            .Subscribe(isValid => IsStepValid = isValid)
            .DisposeWith(_disposables);
    }

    private bool ValidateStepConfiguration()
    {
        // Valid only if exactly one segment is selected and DumpCount is valid
        return SelectedProfile != null && SelectedSegmentCount == 1 && DumpCount >= 1;
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            IsBusy = true;
            Status = UIStrings.Status_LoadingMemoryRegionProfiles;

            IEnumerable<MemoryMappingProfile> profiles = await _memoryRegionService.GetAllAsync().ConfigureAwait(false);

            if (profiles == null)
            {
                _logger.LogWarning("Service returned null profiles collection");
                Status = UIStrings.Status_NoProfilesAvailable;
                return;
            }

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                int? currentSelectionId = SelectedProfile?.Id;

                AvailableProfiles.Clear();
                foreach (MemoryMappingProfile profile in profiles)
                {
                    AvailableProfiles.Add(profile.ClonePreserveId());
                }

                _logger.LogDebug("Loaded {Count} profiles to AvailableProfiles collection", AvailableProfiles.Count);

                // Try to preserve selection, or select default, or select first
                if (currentSelectionId.HasValue)
                {
                    SelectedProfile = AvailableProfiles.FirstOrDefault(p => p.Id == currentSelectionId.Value);
                }

                if (SelectedProfile == null)
                {
                    MemoryMappingProfile? defaultProfile = AvailableProfiles.FirstOrDefault(p => p.IsDefault);
                    SelectedProfile = defaultProfile ?? AvailableProfiles.FirstOrDefault();

                    _logger.LogDebug("Auto-selected profile: {ProfileName} (ID: {ProfileId})",
                        SelectedProfile?.Name ?? "none", SelectedProfile?.Id ?? -1);
                }

                Status = $"Loaded {AvailableProfiles.Count} profile(s)";
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load memory region profiles");
            Status = string.Format(UIStrings.Status_ErrorLoadingProfiles, ex.Message);
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
    /// Enforces single-segment selection for job wizard (only one segment can be selected at a time).
    /// </summary>
    private void OnSegmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MemorySegment.IsSelected))
        {
            _logger.LogDebug("Segment IsSelected property changed, updating job wizard step properties");

            // SINGLE SELECTION MODE: If a segment was just selected, deselect all others
            if (sender is MemorySegment selectedSegment && selectedSegment.IsSelected && SelectedProfile?.Segments != null)
            {
                _logger.LogDebug("Enforcing single-segment selection: deselecting all except {SegmentName}", selectedSegment.Name);

                // Temporarily unsubscribe to avoid recursive calls
                foreach (MemorySegment segment in SelectedProfile.Segments)
                {
                    segment.PropertyChanged -= OnSegmentPropertyChanged;
                }

                // Deselect all segments except the one that was just selected
                foreach (MemorySegment segment in SelectedProfile.Segments)
                {
                    if (segment != selectedSegment && segment.IsSelected)
                    {
                        segment.IsSelected = false;
                    }
                }

                // Resubscribe to all segments
                foreach (MemorySegment segment in SelectedProfile.Segments)
                {
                    segment.PropertyChanged += OnSegmentPropertyChanged;
                }
            }

            // Update all computed properties that depend on segment selection
            UpdateSelectedSegments();
            this.RaisePropertyChanged(nameof(SegmentCount));
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
            this.RaisePropertyChanged(nameof(TotalSelectedSize));
            this.RaisePropertyChanged(nameof(HasContiguousSelection));
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
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
