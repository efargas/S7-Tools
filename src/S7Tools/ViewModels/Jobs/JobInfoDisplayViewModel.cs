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
using S7Tools.ViewModels.Controls;
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
    private readonly IMemoryRegionProfileService _memoryRegionService;
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
        IMemoryRegionProfileService memoryRegionService,
        ILogger<JobInfoDisplayViewModel> logger)
    {
        _profileDetailsService = profileDetailsService ?? throw new ArgumentNullException(nameof(profileDetailsService));
        _serialService = serialService ?? throw new ArgumentNullException(nameof(serialService));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _powerService = powerService ?? throw new ArgumentNullException(nameof(powerService));
        _memoryRegionService = memoryRegionService ?? throw new ArgumentNullException(nameof(memoryRegionService));
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
        string info = $"Job: {job.Name}";

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
            SerialPortProfile? profile = await _serialService.GetByIdAsync(job.SerialProfileId);
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
            SocatProfile? profile = await _socatService.GetByIdAsync(job.SocatProfileId);
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
            PowerSupplyProfile? profile = await _powerService.GetByIdAsync(job.PowerSupplyProfileId);
            PowerSupplyProfileDetails = _profileDetailsService.CreateProfileDetailsViewModel(profile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load power supply profile {ProfileId} for job {JobId}",
                job.PowerSupplyProfileId, job.Id);
            PowerSupplyProfileDetails = null;
        }
    }

    private async Task LoadMemoryRegionDetailsAsync(JobProfile job)
    {
        try
        {
            var basicProperties = new ObservableCollection<PropertyDisplayItem>();
            var profileProperties = new ObservableCollection<PropertyDisplayItem>();
            var segmentProperties = new ObservableCollection<PropertyDisplayItem>();

            // Try to load memory region profile if specified
            if (job.MemoryRegionProfileId > 0)
            {
                try
                {
                    MemoryMappingProfile? memoryProfile = await _memoryRegionService.GetByIdAsync(job.MemoryRegionProfileId);
                    if (memoryProfile != null)
                    {
                        // Profile information
                        basicProperties.Add(new PropertyDisplayItem
                        {
                            Label = "Profile",
                            Value = memoryProfile.Name,
                            Tooltip = "Selected memory region profile"
                        });

                        if (!string.IsNullOrEmpty(memoryProfile.Description))
                        {
                            basicProperties.Add(new PropertyDisplayItem
                            {
                                Label = "Description",
                                Value = memoryProfile.Description,
                                Tooltip = "Memory profile description"
                            });
                        }

                        // Segment statistics
                        var selectedSegments = memoryProfile.Segments.Where(s => s.IsSelected).ToList();
                        long totalSelectedSize = selectedSegments.Sum(s => s.Size);

                        profileProperties.Add(new PropertyDisplayItem
                        {
                            Label = "Total Segments",
                            Value = memoryProfile.Segments.Count.ToString(),
                            Tooltip = "Total number of memory segments in profile"
                        });

                        profileProperties.Add(new PropertyDisplayItem
                        {
                            Label = "Selected Segments",
                            Value = selectedSegments.Count.ToString(),
                            Tooltip = "Number of segments marked for memory dump"
                        });

                        profileProperties.Add(new PropertyDisplayItem
                        {
                            Label = "Total Selected Size",
                            Value = FormatSize(totalSelectedSize),
                            Tooltip = $"{totalSelectedSize} bytes total"
                        });

                        // Add detailed segment information
                        if (selectedSegments.Any())
                        {
                            for (int i = 0; i < selectedSegments.Count; i++)
                            {
                                MemorySegment segment = selectedSegments[i];
                                segmentProperties.Add(new PropertyDisplayItem
                                {
                                    Label = segment.Name,
                                    Value = $"{segment.AddressRange} ({segment.SizeFormatted})",
                                    Tooltip = $"Type: {segment.Type}, Size: {segment.Size} bytes"
                                });
                            }
                        }
                        else
                        {
                            segmentProperties.Add(new PropertyDisplayItem
                            {
                                Label = "Warning",
                                Value = "No segments selected for memory dump",
                                Tooltip = "No segments are currently selected. Configure this in the Job wizard."
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load memory region profile {ProfileId} for job {JobId}",
                        job.MemoryRegionProfileId, job.Id);
                    basicProperties.Add(new PropertyDisplayItem
                    {
                        Label = "Profile Status",
                        Value = $"Error loading profile ID {job.MemoryRegionProfileId}",
                        Tooltip = "The referenced memory region profile could not be loaded"
                    });
                }
            }
            else
            {
                // Fallback to basic memory region (legacy)
                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Configuration",
                    Value = "Basic Memory Region (Legacy)",
                    Tooltip = "Using job's basic memory region settings instead of a profile"
                });

                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Start Address",
                    Value = $"0x{job.MemoryRegion.Start:X8}",
                    Tooltip = "Starting memory address for dump operation"
                });

                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Length",
                    Value = FormatSize(job.MemoryRegion.Length),
                    Tooltip = $"{job.MemoryRegion.Length} bytes total"
                });

                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "End Address",
                    Value = $"0x{job.MemoryRegion.Start + job.MemoryRegion.Length:X8}",
                    Tooltip = "Ending memory address (exclusive)"
                });
            }

            MemoryRegionProfileDetails = new ProfileDetailsViewModel(
                "Memory Region",
                job.MemoryRegionProfileId > 0 ? "Memory Profile Configuration" : "Basic Memory Configuration",
                basicProperties,
                profileProperties,
                segmentProperties,
                true,
                null,
                false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create memory region details for job {JobId}", job.Id);
            MemoryRegionProfileDetails = null;
        }
    }

    private static string FormatSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return bytes switch
        {
            >= GB => $"{bytes / (double)GB:F1} GB ({bytes:N0} bytes)",
            >= MB => $"{bytes / (double)MB:F1} MB ({bytes:N0} bytes)",
            >= KB => $"{bytes / (double)KB:F1} KB ({bytes:N0} bytes)",
            _ => $"{bytes:N0} bytes"
        };
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

            // Note: MemoryRegionProfileDetails can be null but still functional (uses basic memory region)
            // Only warn if the memory region profile ID is set but the details indicate an error
            if (_selectedJob.MemoryRegionProfileId != 0 && MemoryRegionProfileDetails != null)
            {
                // Check if the profile details contain an error message
                PropertyDisplayItem? errorProperty = MemoryRegionProfileDetails.ConfigurationProperties
                    ?.FirstOrDefault(p => p.Label == "Profile Status" && p.Value.Contains("Error"));
                if (errorProperty != null)
                {
                    _missingProfileWarnings.Add($"Memory Region profile not found (ID: {_selectedJob.MemoryRegionProfileId})");
                }
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
