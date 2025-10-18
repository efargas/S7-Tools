using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the in-content Job Creator wizard hosted in Jobs area.
/// </summary>
public class JobWizardViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<JobWizardViewModel> _logger;
    private readonly ISerialPortProfileService _serialService;
    private readonly ISocatProfileService _socatService;
    private readonly IPowerSupplyProfileService _powerService;
    private readonly IJobManager _jobManager;
    private readonly IUIThreadService _uiThreadService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly CompositeDisposable _disposables = new();
    private readonly IViewModelFactory _vmFactory;
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
    // Removed power device scan from wizard per UX guidance

    // Memory
    private uint _memoryStart = 0x20000000;
    private uint _memoryLength = 0x1000;
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
        IJobManager jobManager,
        IUIThreadService uiThreadService,
        IFileDialogService? fileDialogService = null,
        IViewModelFactory? viewModelFactory = null)
    {
        _logger = logger;
        _serialService = serialService;
        _socatService = socatService;
        _powerService = powerService;
        _jobManager = jobManager;
        _uiThreadService = uiThreadService;
        _fileDialogService = fileDialogService;
        _vmFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

        SerialProfiles = new ObservableCollection<SerialPortProfile>();
        SocatProfiles = new ObservableCollection<SocatProfile>();
        PowerProfiles = new ObservableCollection<PowerSupplyProfile>();
        // Initialize memory presets
        MemoryPresets.Add(new MemoryPreset("4KB Boot Sector", 0x20000000u, 0x1000u));
        MemoryPresets.Add(new MemoryPreset("8KB Region", 0x20001000u, 0x2000u));
        MemoryPresets.Add(new MemoryPreset("16KB Region", 0x20002000u, 0x4000u));

    // Create serial scanner child VM for UI embedding
    SerialScanner = _vmFactory.Create<SerialPortScannerViewModel>();

    // Commands
        IObservable<bool> canBack = this.WhenAnyValue(x => x.CurrentStep)
            .Select(step => step != WizardStep.Serial);

        IObservable<bool> canNext = this.WhenAnyValue(
            x => x.CurrentStep,
            x => x.SelectedSerial,
            x => x.SelectedSocat,
            x => x.SelectedPower,
            x => x.MemoryStart,
            x => x.MemoryLength,
            (step, serial, socat, power, start, length) =>
            {
                return step switch
                {
                    WizardStep.Serial => serial != null,
                    WizardStep.Socat => socat != null,
                    WizardStep.Power => power != null,
                    WizardStep.Memory => start >= 0 && length > 0,
                    WizardStep.TimingOutput => !string.IsNullOrWhiteSpace(OutputPath) && !string.IsNullOrWhiteSpace(PayloadsBasePath) && PowerOnTimeMs >= 0 && PowerOffDelayMs >= 0,
                    WizardStep.Review => false,
                    _ => false
                };
            });

        IObservable<bool> canFinish = this.WhenAnyValue(
            x => x.SelectedSerial,
            x => x.SelectedSocat,
            x => x.SelectedPower,
            x => x.MemoryStart,
            x => x.MemoryLength,
            x => x.OutputPath,
            x => x.PayloadsBasePath,
            x => x.PowerOnTimeMs,
            x => x.PowerOffDelayMs,
            x => x.CurrentStep,
            (serial, socat, power, start, length, outPath, payloads, onMs, offMs, step) =>
                serial != null && socat != null && power != null && length > 0 &&
                !string.IsNullOrWhiteSpace(outPath) && !string.IsNullOrWhiteSpace(payloads) && onMs >= 0 && offMs >= 0 && step == WizardStep.Review);

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
    // Serial device scanner VM for UI embedding
    public SerialPortScannerViewModel SerialScanner { get; }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> NextCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> FinishCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowsePayloadsPathCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseOutputPathCommand { get; }

    public bool CancelRequested { get; private set; }

    public WizardStep CurrentStep
    {
        get => _currentStep;
        set => this.RaiseAndSetIfChanged(ref _currentStep, value);
    }

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
            // Notify dependent computed properties
            this.RaisePropertyChanged(nameof(PowerConfigurationType));
            this.RaisePropertyChanged(nameof(PowerConfigurationHost));
            this.RaisePropertyChanged(nameof(PowerConfigurationPort));
        }
    }

    // Removed power scan public properties

    public uint MemoryStart
    {
        get => _memoryStart;
        set => this.RaiseAndSetIfChanged(ref _memoryStart, value);
    }

    public uint MemoryLength
    {
        get => _memoryLength;
        set => this.RaiseAndSetIfChanged(ref _memoryLength, value);
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

    public int PowerOnTimeMs
    {
        get => _powerOnTimeMs;
        set => this.RaiseAndSetIfChanged(ref _powerOnTimeMs, value);
    }

    public int PowerOffDelayMs
    {
        get => _powerOffDelayMs;
        set => this.RaiseAndSetIfChanged(ref _powerOffDelayMs, value);
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
            Status = "Loading profiles...";
            _logger.LogInformation("Loading wizard profile lists");

            Task<IEnumerable<SerialPortProfile>> serialTask = _serialService.GetAllAsync();
            Task<IEnumerable<SocatProfile>> socatTask = _socatService.GetAllAsync();
            Task<IEnumerable<PowerSupplyProfile>> powerTask = _powerService.GetAllAsync();

            await Task.WhenAll(serialTask, socatTask, powerTask).ConfigureAwait(false);

            var serials = (await serialTask.ConfigureAwait(false)).ToList();
            var socats = (await socatTask.ConfigureAwait(false)).ToList();
            var powers = (await powerTask.ConfigureAwait(false)).ToList();

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
            Status = $"Error loading profiles: {ex.Message}";
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
            Status = "Creating job...";

            if (SelectedSerial == null || SelectedSocat == null || SelectedPower == null)
            {
                Status = "Please select all required profiles";
                return;
            }

            string baseName = string.IsNullOrWhiteSpace(JobName) ? "New Job" : JobName.Trim();
            var job = JobProfile.CreateUserProfile(await _jobManager.EnsureUniqueNameAsync(baseName).ConfigureAwait(false), JobDescription?.Trim() ?? string.Empty);
            job.SerialProfileId = SelectedSerial.Id;
            job.SocatProfileId = SelectedSocat.Id;
            job.PowerSupplyProfileId = SelectedPower.Id;
            job.MemoryRegion = new MemoryRegionProfile(MemoryStart, MemoryLength);
            job.Payloads = new PayloadSetProfile(PayloadsBasePath);
            job.OutputPath = OutputPath;
            job.PowerOnTimeMs = PowerOnTimeMs;
            job.PowerOffDelayMs = PowerOffDelayMs;

            JobProfile created = await _jobManager.CreateAsync(job).ConfigureAwait(false);
            CreatedJobId = created.Id;
            Status = "Job created";
            _logger.LogInformation("Job created via wizard: {JobId} {JobName}", created.Id, created.Name);
            Completed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job via wizard");
            Status = $"Error: {ex.Message}";
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

    public bool Completed { get; private set; }
    public int? CreatedJobId { get; private set; }

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
            // no resources
        }
    }
}
