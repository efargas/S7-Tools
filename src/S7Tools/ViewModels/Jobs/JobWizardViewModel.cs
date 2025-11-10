using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Constants;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Resources.Strings;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Controls;

namespace S7Tools.ViewModels.Jobs;

/// <summary>
/// ViewModel for the in-content Job Creator wizard hosted in Jobs area.
/// </summary>
public class JobWizardViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<JobWizardViewModel> _logger;
    private readonly ISerialPortProfileService _serialService;
    private readonly ISocatProfileService _socatService;
    private readonly IPowerSupplyProfileService _powerService;
    private readonly IMemoryRegionProfileService _memoryRegionService;
    private readonly IJobManager _jobManager;
    private readonly IUIThreadService _uiThreadService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly CompositeDisposable _disposables = new();
    private readonly IViewModelFactory _vmFactory;

    // Resource strings (cached for performance)
    private static readonly string NotAvailable = UIStrings.ResourceManager.GetString("Value_NotAvailable") ?? "N/A";
    private static readonly string NotConfigured = UIStrings.ResourceManager.GetString("Value_NotConfigured") ?? "Not configured";

    // Removed power scan resources

    public enum WizardStep
    {
        Serial = 0,
        Socat = 1,
        Power = 2,
        Memory = 3,
        TimingOutput = 4,
        Review = 5
    }

    private WizardStep _currentStep = WizardStep.Serial;
    private bool _isBusy;
    private string _status = string.Empty;

    // Basic
    private string _jobName = string.Empty;
    private string _jobDescription = string.Empty;
    private bool _isEditMode;

    // Selections
    private SerialPortProfile? _selectedSerial;
    private SocatProfile? _selectedSocat;
    private PowerSupplyProfile? _selectedPower;
    private MemoryMappingProfile? _selectedMemoryRegion;
    // Removed power device scan from wizard per UX guidance

    // Port scanning
    private bool _isScanning;
    private string? _selectedPort;
    private readonly CancellationTokenSource _scanCancellationTokenSource = new();

    // Memory
    private uint _memoryStart = MemoryConstants.DefaultUserMemoryStart;
    private uint _memoryLength = MemoryConstants.DefaultDumpSize;
    public sealed record MemoryPreset(string Name, uint Start, uint Length);
    public ObservableCollection<MemoryPreset> MemoryPresets { get; } = new();
    private MemoryPreset? _selectedMemoryPreset;

    // Timing & Output
    private int _powerOnTimeMs = 5000;
    private int _powerOffDelayMs = 2000;
    private string _payloadsBasePath = "./bootloader-payloads";
    private string _outputPath = "./dumps";

    public JobWizardViewModel(
        ILogger<JobWizardViewModel> logger,
        ISerialPortProfileService serialService,
        ISocatProfileService socatService,
        IPowerSupplyProfileService powerService,
        IMemoryRegionProfileService memoryRegionService,
        IJobManager jobManager,
        IUIThreadService uiThreadService,
        IFileDialogService? fileDialogService = null,
        IViewModelFactory? viewModelFactory = null)
    {
        _logger = logger;
        _serialService = serialService;
        _socatService = socatService;
        _powerService = powerService;
        _memoryRegionService = memoryRegionService;
        _jobManager = jobManager;
        _uiThreadService = uiThreadService;
        _fileDialogService = fileDialogService;
        _vmFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

        SerialProfiles = new ObservableCollection<SerialPortProfile>();
        SocatProfiles = new ObservableCollection<SocatProfile>();
        PowerProfiles = new ObservableCollection<PowerSupplyProfile>();
        MemoryProfiles = new ObservableCollection<MemoryMappingProfile>();
        AvailablePorts = new ObservableCollection<string>();
        // Initialize memory presets
        MemoryPresets.Add(new MemoryPreset("4KB Boot Sector", MemoryConstants.DefaultUserMemoryStart, MemoryConstants.DefaultDumpSize));
        MemoryPresets.Add(new MemoryPreset("8KB Region", 0x20001000u, 0x2000u));
        MemoryPresets.Add(new MemoryPreset("16KB Region", 0x20002000u, 0x4000u));

        // Create port scanner child VM for UI embedding
        PortScanner = _vmFactory.Create<SerialPortDiscoveryViewModel>();

        // Create memory region step ViewModel
        MemoryRegionStepViewModel = _vmFactory.Create<JobWizardMemoryRegionStepViewModel>();
        _logger.LogInformation("Created JobWizardMemoryRegionStepViewModel via factory");

        // Commands
        IObservable<bool> canBack = this.WhenAnyValue(x => x.CurrentStep)
            .Select(step => step != WizardStep.Serial);

        IObservable<bool> canNext = this.WhenAnyValue(
            x => x.CurrentStep,
            x => x.SelectedSerial,
            x => x.SelectedSocat,
            x => x.SelectedPower,
            x => x.SelectedMemoryRegion,
            (step, serial, socat, power, memoryRegion) =>
            {
                return step switch
                {
                    WizardStep.Serial => serial != null,
                    WizardStep.Socat => socat != null,
                    WizardStep.Power => power != null,
                    WizardStep.Memory => memoryRegion != null && memoryRegion.HasSelectedSegments,
                    WizardStep.TimingOutput => !string.IsNullOrWhiteSpace(OutputPath) && !string.IsNullOrWhiteSpace(PayloadsBasePath) && PowerOnTimeMs >= 0 && PowerOffDelayMs >= 0,
                    WizardStep.Review => false,
                    _ => false
                };
            });

        IObservable<bool> canFinish = this.WhenAnyValue(
            x => x.SelectedSerial,
            x => x.SelectedSocat,
            x => x.SelectedPower,
            x => x.SelectedMemoryRegion,
            x => x.OutputPath,
            x => x.PayloadsBasePath,
            x => x.PowerOnTimeMs,
            x => x.PowerOffDelayMs,
            x => x.CurrentStep,
            (serial, socat, power, memoryRegion, outPath, payloads, onMs, offMs, step) =>
            {
                // Basic null checks and step requirements
                if (serial == null || socat == null || power == null || memoryRegion == null ||
                    string.IsNullOrWhiteSpace(outPath) || string.IsNullOrWhiteSpace(payloads) ||
                    onMs < 0 || offMs < 0 || step != WizardStep.Review)
                {
                    return false;
                }

                // Memory region specific validation
                if (!memoryRegion.HasSelectedSegments)
                {
                    return false;
                }

                // Additional validation - check for contiguous segments if required
                // Note: Non-contiguous segments are allowed but may produce warnings
                return true;
            });

        BackCommand = ReactiveCommand.Create<Unit, Unit>(_ =>
        {
            CurrentStep = (WizardStep)Math.Max(0, (int)CurrentStep - 1);
            return Unit.Default;
        }, canBack);
        NextCommand = ReactiveCommand.Create<Unit, Unit>(_ =>
        {
            CurrentStep = (WizardStep)Math.Min((int)WizardStep.Review, (int)CurrentStep + 1);
            return Unit.Default;
        }, canNext);
        CancelCommand = ReactiveCommand.Create<Unit, Unit>(_ =>
        {
            CancelRequested = true;
            return Unit.Default;
        });
        FinishCommand = ReactiveCommand.CreateFromTask(ExecuteFinishAsync, canFinish);

        // File/folder pickers
        BrowsePayloadsPathCommand = ReactiveCommand.CreateFromTask(BrowsePayloadsPathAsync);
        BrowseOutputPathCommand = ReactiveCommand.CreateFromTask(BrowseOutputPathAsync);

        // Port scanning
        ScanPortsCommand = ReactiveCommand.CreateFromTask(ScanPortsAsync);

        // Load data
        _ = LoadAsync();
    }

    // Optional preselection inputs (set by parent VM before showing wizard)
    public int? PreselectSerialId { get; set; }
    public int? PreselectSocatId { get; set; }
    public int? PreselectPowerId { get; set; }
    public string? PreselectJobName { get; set; }
    public string? PreselectJobDescription { get; set; }

    public ObservableCollection<SerialPortProfile> SerialProfiles { get; }
    public ObservableCollection<SocatProfile> SocatProfiles { get; }
    public ObservableCollection<PowerSupplyProfile> PowerProfiles { get; }
    public ObservableCollection<MemoryMappingProfile> MemoryProfiles { get; }
    public ObservableCollection<string> AvailablePorts { get; }
    // Port scanner VM for UI embedding
    public SerialPortDiscoveryViewModel PortScanner { get; }
    // Memory region step VM for wizard integration
    public JobWizardMemoryRegionStepViewModel MemoryRegionStepViewModel { get; }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> NextCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> FinishCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowsePayloadsPathCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseOutputPathCommand { get; }
    public ReactiveCommand<Unit, Unit> ScanPortsCommand { get; }

    public bool CancelRequested { get; private set; }

    public WizardStep CurrentStep
    {
        get => _currentStep;
        set
        {
            WizardStep oldValue = _currentStep;
            this.RaiseAndSetIfChanged(ref _currentStep, value);

            // Notify step visibility changes when the step actually changes
            if (oldValue != value)
            {
                this.RaisePropertyChanged(nameof(IsSerialStep));
                this.RaisePropertyChanged(nameof(IsSocatStep));
                this.RaisePropertyChanged(nameof(IsPowerStep));
                this.RaisePropertyChanged(nameof(IsMemoryStep));
                this.RaisePropertyChanged(nameof(IsTimingOutputStep));
                this.RaisePropertyChanged(nameof(IsReviewStep));
            }
        }
    }

    // Helper properties for step visibility
    public bool IsSerialStep => CurrentStep == WizardStep.Serial;
    public bool IsSocatStep => CurrentStep == WizardStep.Socat;
    public bool IsPowerStep => CurrentStep == WizardStep.Power;
    public bool IsMemoryStep => CurrentStep == WizardStep.Memory;
    public bool IsTimingOutputStep => CurrentStep == WizardStep.TimingOutput;
    public bool IsReviewStep => CurrentStep == WizardStep.Review;

    public string JobName
    {
        get => _jobName;
        set => this.RaiseAndSetIfChanged(ref _jobName, value);
    }

    public string JobDescription
    {
        get => _jobDescription;
        set => this.RaiseAndSetIfChanged(ref _jobDescription, value);
    }

    // Indicates whether the wizard is creating a new job or editing an existing one
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isEditMode, value);
            this.RaisePropertyChanged(nameof(WizardTitle));
        }
    }

    // Dynamic title bound in the view's header
    public string WizardTitle => IsEditMode ? "Edit Job Wizard" : "Create Job Wizard";

    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public string Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }

    public string? SelectedPort
    {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    public SerialPortProfile? SelectedSerial
    {
        get => _selectedSerial;
        set => this.RaiseAndSetIfChanged(ref _selectedSerial, value);
    }

    public SocatProfile? SelectedSocat
    {
        get => _selectedSocat;
        set => this.RaiseAndSetIfChanged(ref _selectedSocat, value);
    }

    public PowerSupplyProfile? SelectedPower
    {
        get => _selectedPower;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPower, value);
            // Notify dependent computed properties (used for power scanning)
            this.RaisePropertyChanged(nameof(PowerConfigurationType));
            this.RaisePropertyChanged(nameof(PowerConfigurationHost));
            this.RaisePropertyChanged(nameof(PowerConfigurationPort));
            this.RaisePropertyChanged(nameof(SelectedPowerHost));
            this.RaisePropertyChanged(nameof(SelectedPowerPort));
            this.RaisePropertyChanged(nameof(SelectedPowerDeviceId));
        }
    }

    public MemoryMappingProfile? SelectedMemoryRegion
    {
        get => _selectedMemoryRegion;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMemoryRegion, value);
            this.RaisePropertyChanged(nameof(MemoryRegionSummary));
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
            this.RaisePropertyChanged(nameof(TotalSelectedSize));
        }
    }

    // Removed power scan public properties

    public uint MemoryStart
    {
        get => _memoryStart;
        set
        {
            this.RaiseAndSetIfChanged(ref _memoryStart, value);
            this.RaisePropertyChanged(nameof(MemoryEndAddress));
            this.RaisePropertyChanged(nameof(MemoryRegionSummary));
        }
    }

    public uint MemoryLength
    {
        get => _memoryLength;
        set
        {
            this.RaiseAndSetIfChanged(ref _memoryLength, value);
            this.RaisePropertyChanged(nameof(MemoryEndAddress));
            this.RaisePropertyChanged(nameof(MemoryRegionSummary));
        }
    }

    /// <summary>
    /// Gets the calculated end address of the memory region (Start + Length).
    /// </summary>
    public string MemoryEndAddress
    {
        get
        {
            try
            {
                uint endAddress = MemoryStart + MemoryLength;
                return $"0x{endAddress:X}";
            }
            catch
            {
                return "Not calculated";
            }
        }
    }

    /// <summary>
    /// Gets a formatted summary of the memory region (Profile Name - X segments selected).
    /// </summary>
    public string MemoryRegionSummary
    {
        get
        {
            try
            {
                if (SelectedMemoryRegion == null)
                {
                    return "No memory region profile selected";
                }

                var selectedSegments = SelectedMemoryRegion.SelectedSegments.ToList();
                if (selectedSegments.Count == 0)
                {
                    return $"{SelectedMemoryRegion.Name} - No segments selected";
                }

                // Calculate total size
                long totalSize = selectedSegments.Sum(s => s.Size);
                string totalSizeFormatted = totalSize == 0 ? "0 bytes" :
                    totalSize < 1024 ? $"{totalSize} bytes" :
                    totalSize < 1024 * 1024 ? $"{totalSize / 1024.0:F1} KB" :
                    $"{totalSize / (1024.0 * 1024.0):F1} MB";

                // Get segment names
                string segmentNames = string.Join(", ", selectedSegments.Select(s => s.Name));

                // Check if contiguous
                string contiguityInfo = SelectedMemoryRegion.HasContiguousSelection()
                    ? ""
                    : UIStrings.ResourceManager.GetString("Warning_NonContiguousSegments") ?? " (Warning: Non-contiguous)";

                return $"{SelectedMemoryRegion.Name} - {selectedSegments.Count} segment(s) selected: {segmentNames} ({totalSizeFormatted}){contiguityInfo}";
            }
            catch
            {
                return NotConfigured;
            }
        }
    }

    /// <summary>
    /// Gets the number of selected segments in the memory region profile.
    /// </summary>
    public int SelectedSegmentCount
    {
        get { return SelectedMemoryRegion?.SelectedSegments.Count() ?? 0; }
    }

    /// <summary>
    /// Gets the total size of selected segments in bytes.
    /// </summary>
    public long TotalSelectedSize
    {
        get { return SelectedMemoryRegion?.TotalSelectedSize ?? 0; }
    }

    /// <summary>
    /// Gets a formatted summary of the power timing (On: Xms, Off: Yms).
    /// </summary>
    public string PowerTimingSummary
    {
        get
        {
            try
            {
                return $"On: {PowerOnTimeMs}ms, Off: {PowerOffDelayMs}ms";
            }
            catch
            {
                return NotConfigured;
            }
        }
    }

    public MemoryPreset? SelectedMemoryPreset
    {
        get => _selectedMemoryPreset;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMemoryPreset, value);
            if (value != null)
            {
                MemoryStart = value.Start;
                MemoryLength = value.Length;
            }
        }
    }

    /// <summary>
    /// Gets the host from the selected power supply configuration (if it's ModbusTcp).
    /// </summary>
    public string SelectedPowerHost
    {
        get
        {
            if (SelectedPower?.Configuration is ModbusTcpConfiguration modbusTcp)
            {
                return modbusTcp.Host;
            }
            return NotAvailable;
        }
    }

    /// <summary>
    /// Gets the port from the selected power supply configuration (if it's ModbusTcp).
    /// </summary>
    public string SelectedPowerPort
    {
        get
        {
            if (SelectedPower?.Configuration is ModbusTcpConfiguration modbusTcp)
            {
                return modbusTcp.Port.ToString();
            }
            return NotAvailable;
        }
    }

    /// <summary>
    /// Gets the device ID from the selected power supply configuration (if it's ModbusTcp).
    /// </summary>
    public string SelectedPowerDeviceId
    {
        get
        {
            if (SelectedPower?.Configuration is ModbusTcpConfiguration modbusTcp)
            {
                return modbusTcp.DeviceId.ToString();
            }
            return NotAvailable;
        }
    }

    public int PowerOnTimeMs
    {
        get => _powerOnTimeMs;
        set
        {
            this.RaiseAndSetIfChanged(ref _powerOnTimeMs, value);
            this.RaisePropertyChanged(nameof(PowerTimingSummary));
        }
    }

    public int PowerOffDelayMs
    {
        get => _powerOffDelayMs;
        set
        {
            this.RaiseAndSetIfChanged(ref _powerOffDelayMs, value);
            this.RaisePropertyChanged(nameof(PowerTimingSummary));
        }
    }

    public string PayloadsBasePath
    {
        get => _payloadsBasePath;
        set => this.RaiseAndSetIfChanged(ref _payloadsBasePath, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => this.RaiseAndSetIfChanged(ref _outputPath, value);
    }

    // Computed, type-safe accessors for power configuration shown in UI
    public string PowerConfigurationType => SelectedPower?.Configuration?.Type.ToString() ?? string.Empty;
    public string PowerConfigurationHost => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.Host ?? string.Empty;
    public int PowerConfigurationPort => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.Port ?? 0;

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            Status = UIStrings.Status_LoadingProfiles;
            _logger.LogInformation("Loading wizard profile lists");

            Task<IEnumerable<SerialPortProfile>> serialTask = _serialService.GetAllAsync();
            Task<IEnumerable<SocatProfile>> socatTask = _socatService.GetAllAsync();
            Task<IEnumerable<PowerSupplyProfile>> powerTask = _powerService.GetAllAsync();
            Task<IEnumerable<MemoryMappingProfile>> memoryTask = _memoryRegionService.GetAllAsync();

            await Task.WhenAll(serialTask, socatTask, powerTask, memoryTask).ConfigureAwait(false);

            var serials = (await serialTask.ConfigureAwait(false)).ToList();
            var socats = (await socatTask.ConfigureAwait(false)).ToList();
            var powers = (await powerTask.ConfigureAwait(false)).ToList();
            var memoryProfiles = (await memoryTask.ConfigureAwait(false)).ToList();

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                SerialProfiles.Clear();
                foreach (SerialPortProfile? s in serials)
                {
                    SerialProfiles.Add(s);
                }
                SocatProfiles.Clear();
                foreach (SocatProfile? s in socats)
                {
                    SocatProfiles.Add(s);
                }
                PowerProfiles.Clear();
                foreach (PowerSupplyProfile? p in powers)
                {
                    PowerProfiles.Add(p);
                }
                MemoryProfiles.Clear();
                foreach (MemoryMappingProfile? m in memoryProfiles)
                {
                    MemoryProfiles.Add(m);
                }

                // Apply preselection if provided, else fall back to defaults
                if (PreselectSerialId.HasValue)
                {
                    SelectedSerial = SerialProfiles.FirstOrDefault(x => x.Id == PreselectSerialId.Value)
                                     ?? SerialProfiles.FirstOrDefault(x => x.IsDefault)
                                     ?? SerialProfiles.FirstOrDefault();
                }
                else
                {
                    SelectedSerial = SerialProfiles.FirstOrDefault(x => x.IsDefault) ?? SerialProfiles.FirstOrDefault();
                }

                if (PreselectSocatId.HasValue)
                {
                    SelectedSocat = SocatProfiles.FirstOrDefault(x => x.Id == PreselectSocatId.Value)
                                    ?? SocatProfiles.FirstOrDefault(x => x.IsDefault)
                                    ?? SocatProfiles.FirstOrDefault();
                }
                else
                {
                    SelectedSocat = SocatProfiles.FirstOrDefault(x => x.IsDefault) ?? SocatProfiles.FirstOrDefault();
                }

                if (PreselectPowerId.HasValue)
                {
                    SelectedPower = PowerProfiles.FirstOrDefault(x => x.Id == PreselectPowerId.Value)
                                    ?? PowerProfiles.FirstOrDefault(x => x.IsDefault)
                                    ?? PowerProfiles.FirstOrDefault();
                }
                else
                {
                    SelectedPower = PowerProfiles.FirstOrDefault(x => x.IsDefault) ?? PowerProfiles.FirstOrDefault();
                }

                // Memory region profile selection
                SelectedMemoryRegion = MemoryProfiles.FirstOrDefault(x => x.IsDefault) ?? MemoryProfiles.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(PreselectJobName))
                {
                    JobName = PreselectJobName!;
                }
                if (!string.IsNullOrWhiteSpace(PreselectJobDescription))
                {
                    JobDescription = PreselectJobDescription!;
                }
            }).ConfigureAwait(false);

            Status = "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles for wizard");
            Status = string.Format(UIStrings.Status_ErrorLoadingProfiles, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteFinishAsync()
    {
        try
        {
            IsBusy = true;
            Status = UIStrings.Status_CreatingJob;

            if (SelectedSerial == null || SelectedSocat == null || SelectedPower == null || SelectedMemoryRegion == null)
            {
                Status = "Please select all required profiles"; // TODO: Add UIStrings.Status_AllRequiredProfilesNeeded
                return;
            }

            string baseName = string.IsNullOrWhiteSpace(JobName) ? "New Job" : JobName.Trim();
            var job = JobProfile.CreateUserProfile(await _jobManager.EnsureUniqueNameAsync(baseName).ConfigureAwait(false), JobDescription?.Trim() ?? string.Empty);
            job.SerialProfileId = SelectedSerial.Id;
            job.SocatProfileId = SelectedSocat.Id;
            job.PowerSupplyProfileId = SelectedPower.Id;
            job.MemoryRegionProfileId = SelectedMemoryRegion.Id;
            job.MemoryRegion = new MemoryRegionProfile(MemoryStart, MemoryLength); // Keep for backward compatibility
            job.Payloads = new PayloadSetProfile(PayloadsBasePath);
            job.OutputPath = OutputPath;
            job.PowerOnTimeMs = PowerOnTimeMs;
            job.PowerOffDelayMs = PowerOffDelayMs;

            JobProfile created = await _jobManager.CreateAsync(job).ConfigureAwait(false);
            CreatedJobId = created.Id;
            Status = UIStrings.Status_JobCreated;
            _logger.LogInformation("Job created via wizard: {JobId} {JobName}", created.Id, created.Name);
            Completed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job via wizard");
            Status = string.Format(UIStrings.Status_ErrorCreatingJob, ex.Message);
            Completed = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<string?> BrowseFolderAsync(string title, string? initial)
    {
        try
        {
            if (_fileDialogService == null)
            {
                return null;
            }
            return await _fileDialogService.ShowFolderBrowserDialogAsync(title, initial);
        }
        catch
        {
            return null;
        }
    }

    private async Task<Unit> BrowsePayloadsPathAsync()
    {
        string? folder = await BrowseFolderAsync("Select Payloads Folder", PayloadsBasePath);
        if (!string.IsNullOrEmpty(folder))
        {
            PayloadsBasePath = folder;
        }
        return Unit.Default;
    }

    private async Task<Unit> BrowseOutputPathAsync()
    {
        string? folder = await BrowseFolderAsync("Select Output Folder", OutputPath);
        if (!string.IsNullOrEmpty(folder))
        {
            OutputPath = folder;
        }
        return Unit.Default;
    }

    private async Task ScanPortsAsync()
    {
        if (IsScanning)
        {
            return;
        }

        try
        {
            IsScanning = true;
            Status = UIStrings.Status_ScanningForPorts;

            // Execute the scanner's scan command and wait for completion
            await PortScanner.ScanPortsCommand.Execute();

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                AvailablePorts.Clear();
                foreach (Controls.SerialPortInfo port in PortScanner.DiscoveredPorts)
                {
                    AvailablePorts.Add(port.PortName);
                }

                Status = $"Found {AvailablePorts.Count} port(s)";
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan ports in job wizard");
            Status = string.Format(UIStrings.Status_ErrorScanningPorts, ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    private bool _completed;
    private int? _createdJobId;

    public bool Completed
    {
        get => _completed;
        private set => this.RaiseAndSetIfChanged(ref _completed, value);
    }

    public int? CreatedJobId
    {
        get => _createdJobId;
        private set => this.RaiseAndSetIfChanged(ref _createdJobId, value);
    }

    #region Serial Profile Detail Computed Properties

    /// <summary>
    /// Gets the baud rate of the selected serial profile as a string.
    /// </summary>
    public string SerialBaudRate => SelectedSerial?.Configuration?.BaudRate.ToString() ?? NotAvailable;

    /// <summary>
    /// Gets the character size of the selected serial profile as a string.
    /// </summary>
    public string SerialCharacterSize => SelectedSerial?.Configuration?.CharacterSize.ToString() ?? NotAvailable;

    /// <summary>
    /// Gets the parity of the selected serial profile as a string.
    /// </summary>
    public string SerialParity => SelectedSerial?.Configuration?.Parity.ToString() ?? NotAvailable;

    /// <summary>
    /// Gets the stop bits of the selected serial profile as a string.
    /// </summary>
    public string SerialStopBits => SelectedSerial?.Configuration?.StopBits.ToString() ?? NotAvailable;

    /// <summary>
    /// Gets whether the receiver is enabled in the selected serial profile.
    /// </summary>
    public string SerialEnableReceiver => SelectedSerial?.Configuration?.EnableReceiver == true ? "Yes" :
                                         SelectedSerial?.Configuration?.EnableReceiver == false ? "No" : NotAvailable;

    /// <summary>
    /// Gets the version of the selected serial profile.
    /// </summary>
    public string SerialVersion => SelectedSerial?.Version ?? NotAvailable;

    /// <summary>
    /// Gets the creation date of the selected serial profile.
    /// </summary>
    public string SerialCreatedAt => SelectedSerial?.CreatedAt.ToString(DateTimeFormats.ShortDateTime) ?? NotAvailable;

    /// <summary>
    /// Gets the TCP port of the selected socat profile.
    /// </summary>
    public string SocatTcpPort => SelectedSocat?.Configuration?.TcpPort.ToString() ?? NotAvailable;

    /// <summary>
    /// Gets the TCP host of the selected socat profile.
    /// </summary>
    public string SocatTcpHost => SelectedSocat?.Configuration?.TcpHost ?? NotAvailable;

    /// <summary>
    /// Gets whether verbose mode is enabled in the selected socat profile.
    /// </summary>
    public string SocatVerbose => SelectedSocat?.Configuration?.Verbose == true ? "Yes" :
                                 SelectedSocat?.Configuration?.Verbose == false ? "No" : NotAvailable;

    /// <summary>
    /// Gets the host of the selected power supply profile.
    /// </summary>
    public string PowerHost => SelectedPower?.Configuration is ModbusTcpConfiguration modbusTcp ? modbusTcp.Host : NotAvailable;

    /// <summary>
    /// Gets the port of the selected power supply profile.
    /// </summary>
    public string PowerPort => SelectedPower?.Configuration is ModbusTcpConfiguration modbusTcp ? modbusTcp.Port.ToString() : NotAvailable;

    /// <summary>
    /// Gets the device ID of the selected power supply profile.
    /// </summary>
    public string PowerDeviceId => SelectedPower?.Configuration is ModbusTcpConfiguration modbusTcp ? modbusTcp.DeviceId.ToString() : NotAvailable;

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables.Dispose();
            _scanCancellationTokenSource?.Cancel();
            _scanCancellationTokenSource?.Dispose();
            PortScanner?.Dispose();
            // no resources
        }
    }
}
