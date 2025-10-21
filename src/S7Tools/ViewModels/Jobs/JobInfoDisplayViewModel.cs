using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using S7Tools.ViewModels;
using S7Tools.ViewModels.Profiles;

namespace S7Tools.ViewModels.Jobs;

/// <summary>
/// ViewModel for displaying detailed information about a selected job.
/// Shows job basic info and all associated profile details.
/// </summary>
public class JobInfoDisplayViewModel : ViewModelBase, IDisposable
{
    private readonly IProfileDetailsService _profileDetailsService;
    private readonly ISerialPortProfileService _serialService;
    private readonly ISocatProfileService _socatService;
    private readonly IPowerSupplyProfileService _powerService;
    private readonly ILogger<JobInfoDisplayViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private JobProfile? _selectedJob;
    private string _jobBasicInfo = "No job selected";
    private IProfileDetailsViewModel? _serialProfileDetails;
    private IProfileDetailsViewModel? _socatProfileDetails;
    private IProfileDetailsViewModel? _powerSupplyProfileDetails;
    private IProfileDetailsViewModel? _memoryRegionProfileDetails;
    private bool _hasMissingProfiles;
    private readonly ObservableCollection<string> _missingProfileWarnings = new();

    public JobInfoDisplayViewModel(
        IProfileDetailsService profileDetailsService,
        ISerialPortProfileService serialService,
        ISocatProfileService socatService,
        IPowerSupplyProfileService powerService,
        ILogger<JobInfoDisplayViewModel> logger)
    {
        _profileDetailsService = profileDetailsService ?? throw new ArgumentNullException(nameof(profileDetailsService));
        _serialService = serialService ?? throw new ArgumentNullException(nameof(serialService));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _powerService = powerService ?? throw new ArgumentNullException(nameof(powerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize reactive commands
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);

        // Set up reactive property subscriptions
        this.WhenAnyValue(x => x.SelectedJob)
            .Where(job => job != null)
            .SelectMany(job => Observable.FromAsync(() => LoadJobDetailsAsync(job!)))
            .Subscribe()
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.SelectedJob)
            .Where(job => job == null)
            .Subscribe(_ => ClearJobDetails())
            .DisposeWith(_disposables);
    }

    #region Properties

    /// <summary>
    /// Gets or sets the currently selected job for display.
    /// </summary>
    public JobProfile? SelectedJob
    {
        get => _selectedJob;
        set => this.RaiseAndSetIfChanged(ref _selectedJob, value);
    }

    /// <summary>
    /// Gets the basic job information string for display.
    /// </summary>
    public string JobBasicInfo
    {
        get => _jobBasicInfo;
        private set => this.RaiseAndSetIfChanged(ref _jobBasicInfo, value);
    }

    /// <summary>
    /// Gets the serial port profile details ViewModel.
    /// </summary>
    public IProfileDetailsViewModel? SerialProfileDetails
    {
        get => _serialProfileDetails;
        private set => this.RaiseAndSetIfChanged(ref _serialProfileDetails, value);
    }

    /// <summary>
    /// Gets the socat profile details ViewModel.
    /// </summary>
    public IProfileDetailsViewModel? SocatProfileDetails
    {
        get => _socatProfileDetails;
        private set => this.RaiseAndSetIfChanged(ref _socatProfileDetails, value);
    }

    /// <summary>
    /// Gets the power supply profile details ViewModel.
    /// </summary>
    public IProfileDetailsViewModel? PowerSupplyProfileDetails
    {
        get => _powerSupplyProfileDetails;
        private set => this.RaiseAndSetIfChanged(ref _powerSupplyProfileDetails, value);
    }

    /// <summary>
    /// Gets the memory region profile details ViewModel.
    /// Note: Memory region is embedded in JobProfile, not a separate profile service.
    /// </summary>
    public IProfileDetailsViewModel? MemoryRegionProfileDetails
    {
        get => _memoryRegionProfileDetails;
        private set => this.RaiseAndSetIfChanged(ref _memoryRegionProfileDetails, value);
    }

    /// <summary>
    /// Gets whether there are missing profiles referenced by the job.
    /// </summary>
    public bool HasMissingProfiles
    {
        get => _hasMissingProfiles;
        private set => this.RaiseAndSetIfChanged(ref _hasMissingProfiles, value);
    }

    /// <summary>
    /// Gets the collection of missing profile warning messages.
    /// </summary>
    public ObservableCollection<string> MissingProfileWarnings => _missingProfileWarnings;

    #endregion

    #region Commands

    /// <summary>
    /// Command to refresh the current job details.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    #endregion

    #region Private Methods

    private async Task LoadJobDetailsAsync(JobProfile job)
    {
        try
        {
            _logger.LogDebug("Loading job details for job {JobId}: {JobName}", job.Id, job.Name);

            // Update basic job info
            UpdateJobBasicInfo(job);

            // Clear previous state
            ClearProfileDetails();

            // Load profile details in parallel
            var loadTasks = new Task[]
            {
                LoadSerialProfileDetailsAsync(job),
                LoadSocatProfileDetailsAsync(job),
                LoadPowerSupplyProfileDetailsAsync(job),
                LoadMemoryRegionDetailsAsync(job)
            };

            await Task.WhenAll(loadTasks);

            // Update missing profiles status
            UpdateMissingProfilesStatus();

            _logger.LogDebug("Successfully loaded job details for job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading job details for job {JobId}: {JobName}", job.Id, job.Name);

            // Set error state
            JobBasicInfo = $"Error loading job details: {ex.Message}";
            ClearProfileDetails();
        }
    }

    private void UpdateJobBasicInfo(JobProfile job)
    {
        var info = $"Job: {job.Name}";

        if (!string.IsNullOrEmpty(job.Description))
        {
            info += $"\nDescription: {job.Description}";
        }

        info += $"\nCreated: {job.CreatedAt:yyyy-MM-dd HH:mm}";

        if (job.ModifiedAt != job.CreatedAt)
        {
            info += $"\nModified: {job.ModifiedAt:yyyy-MM-dd HH:mm}";
        }

        JobBasicInfo = info;
    }

    private async Task LoadSerialProfileDetailsAsync(JobProfile job)
    {
        if (job.SerialProfileId == 0)
        {
            SerialProfileDetails = null;
            return;
        }

        try
        {
            var profile = await _serialService.GetByIdAsync(job.SerialProfileId);
            SerialProfileDetails = _profileDetailsService.CreateProfileDetailsViewModel(profile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load serial profile {ProfileId} for job {JobId}",
                job.SerialProfileId, job.Id);
            SerialProfileDetails = null;
        }
    }

    private async Task LoadSocatProfileDetailsAsync(JobProfile job)
    {
        if (job.SocatProfileId == 0)
        {
            SocatProfileDetails = null;
            return;
        }

        try
        {
            var profile = await _socatService.GetByIdAsync(job.SocatProfileId);
            SocatProfileDetails = _profileDetailsService.CreateProfileDetailsViewModel(profile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load socat profile {ProfileId} for job {JobId}",
                job.SocatProfileId, job.Id);
            SocatProfileDetails = null;
        }
    }

    private async Task LoadPowerSupplyProfileDetailsAsync(JobProfile job)
    {
        if (job.PowerSupplyProfileId == 0)
        {
            PowerSupplyProfileDetails = null;
            return;
        }

        try
        {
            var profile = await _powerService.GetByIdAsync(job.PowerSupplyProfileId);
            PowerSupplyProfileDetails = _profileDetailsService.CreateProfileDetailsViewModel(profile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load power supply profile {ProfileId} for job {JobId}",
                job.PowerSupplyProfileId, job.Id);
            PowerSupplyProfileDetails = null;
        }
    }

    private Task LoadMemoryRegionDetailsAsync(JobProfile job)
    {
        // Memory region is embedded in JobProfile, so we'll create a simple display for it
        try
        {
            var basicProperties = new ObservableCollection<PropertyDisplayItem>
            {
                new PropertyDisplayItem
                {
                    Label = "Start Address",
                    Value = $"0x{job.MemoryRegion.Start:X8}",
                    Tooltip = "Starting memory address for dump operation"
                },
                new PropertyDisplayItem
                {
                    Label = "Length",
                    Value = $"{job.MemoryRegion.Length} bytes ({job.MemoryRegion.Length / 1024.0:F1} KB)",
                    Tooltip = "Number of bytes to dump"
                },
                new PropertyDisplayItem
                {
                    Label = "End Address",
                    Value = $"0x{job.MemoryRegion.Start + job.MemoryRegion.Length:X8}",
                    Tooltip = "Ending memory address (exclusive)"
                }
            };

            MemoryRegionProfileDetails = new ProfileDetailsViewModel(
                "Memory Region",
                "Memory Dump Configuration",
                basicProperties,
                new ObservableCollection<PropertyDisplayItem>(),
                new ObservableCollection<PropertyDisplayItem>(),
                true,
                null,
                false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create memory region details for job {JobId}", job.Id);
            MemoryRegionProfileDetails = null;
        }

        return Task.CompletedTask;
    }

    private void UpdateMissingProfilesStatus()
    {
        _missingProfileWarnings.Clear();

        // Check for missing profiles by checking if profile details are null when profile ID is set
        if (_selectedJob != null)
        {
            if (_selectedJob.SerialProfileId != 0 && SerialProfileDetails == null)
            {
                _missingProfileWarnings.Add($"Serial Port profile not found (ID: {_selectedJob.SerialProfileId})");
            }

            if (_selectedJob.SocatProfileId != 0 && SocatProfileDetails == null)
            {
                _missingProfileWarnings.Add($"Socat Bridge profile not found (ID: {_selectedJob.SocatProfileId})");
            }

            if (_selectedJob.PowerSupplyProfileId != 0 && PowerSupplyProfileDetails == null)
            {
                _missingProfileWarnings.Add($"Power Supply profile not found (ID: {_selectedJob.PowerSupplyProfileId})");
            }
        }

        HasMissingProfiles = _missingProfileWarnings.Count > 0;
    }

    private void ClearJobDetails()
    {
        JobBasicInfo = "No job selected";
        ClearProfileDetails();
    }

    private void ClearProfileDetails()
    {
        SerialProfileDetails = null;
        SocatProfileDetails = null;
        PowerSupplyProfileDetails = null;
        MemoryRegionProfileDetails = null;
        HasMissingProfiles = false;
        _missingProfileWarnings.Clear();
    }

    private async Task RefreshAsync()
    {
        if (SelectedJob != null)
        {
            await LoadJobDetailsAsync(SelectedJob);
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
            _disposables?.Dispose();
        }
    }

    #endregion
}
