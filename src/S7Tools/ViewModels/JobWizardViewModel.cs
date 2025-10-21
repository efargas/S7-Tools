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
using System.IO;
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

    // Port scanning
    private bool _isScanning;
    private string? _selectedPort;
    private readonly CancellationTokenSource _scanCancellationTokenSource = new();

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
        AvailablePorts = new ObservableCollection<string>();
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
    public ObservableCollection<string> AvailablePorts { get; }
    // Serial device scanner VM for UI embedding
    public SerialPortScannerViewModel SerialScanner { get; }

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
            var oldValue = _currentStep;
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
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSerial, value);
            // Notify all serial profile detail properties
            this.RaisePropertyChanged(nameof(SerialBaudRate));
            this.RaisePropertyChanged(nameof(SerialCharacterSize));
            this.RaisePropertyChanged(nameof(SerialParity));
            this.RaisePropertyChanged(nameof(SerialStopBits));
            this.RaisePropertyChanged(nameof(SerialEnableReceiver));
            this.RaisePropertyChanged(nameof(SerialDisableHardwareFlowControl));
            this.RaisePropertyChanged(nameof(SerialParityEnabled));
            this.RaisePropertyChanged(nameof(SerialOddParity));
            this.RaisePropertyChanged(nameof(SerialIgnoreBreak));
            this.RaisePropertyChanged(nameof(SerialDisableBreakInterrupt));
            this.RaisePropertyChanged(nameof(SerialDisableMapCRtoNL));
            this.RaisePropertyChanged(nameof(SerialDisableBellOnQueueFull));
            this.RaisePropertyChanged(nameof(SerialDisableXonXoffFlowControl));
            this.RaisePropertyChanged(nameof(SerialDisableOutputProcessing));
            this.RaisePropertyChanged(nameof(SerialDisableMapNLtoCRNL));
            this.RaisePropertyChanged(nameof(SerialDisableCanonicalMode));
            this.RaisePropertyChanged(nameof(SerialDisableSignalGeneration));
            this.RaisePropertyChanged(nameof(SerialDisableExtendedProcessing));
            this.RaisePropertyChanged(nameof(SerialDisableEcho));
            this.RaisePropertyChanged(nameof(SerialDisableEchoErase));
            this.RaisePropertyChanged(nameof(SerialDisableEchoKill));
            this.RaisePropertyChanged(nameof(SerialDisableEchoControl));
            this.RaisePropertyChanged(nameof(SerialDisableEchoKillErase));
            this.RaisePropertyChanged(nameof(SerialRawMode));
            this.RaisePropertyChanged(nameof(SerialVersion));
            this.RaisePropertyChanged(nameof(SerialCreatedAt));
            this.RaisePropertyChanged(nameof(SerialModifiedAt));
            this.RaisePropertyChanged(nameof(SerialIsReadOnly));
            this.RaisePropertyChanged(nameof(SerialIsDefault));
        }
    }

    public SocatProfile? SelectedSocat
    {
        get => _selectedSocat;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSocat, value);
            // Notify all socat profile detail properties
            this.RaisePropertyChanged(nameof(SocatTcpPort));
            this.RaisePropertyChanged(nameof(SocatTcpHost));
            this.RaisePropertyChanged(nameof(SocatEnableFork));
            this.RaisePropertyChanged(nameof(SocatEnableReuseAddr));
            this.RaisePropertyChanged(nameof(SocatVerbose));
            this.RaisePropertyChanged(nameof(SocatHexDump));
            this.RaisePropertyChanged(nameof(SocatBlockSize));
            this.RaisePropertyChanged(nameof(SocatDebugLevel));
            this.RaisePropertyChanged(nameof(SocatSerialRawMode));
            this.RaisePropertyChanged(nameof(SocatSerialDisableEcho));
            this.RaisePropertyChanged(nameof(SocatAutoConfigureSerial));
            this.RaisePropertyChanged(nameof(SocatConnectionTimeout));
            this.RaisePropertyChanged(nameof(SocatAutoRestart));
            this.RaisePropertyChanged(nameof(SocatVersion));
            this.RaisePropertyChanged(nameof(SocatCreatedAt));
            this.RaisePropertyChanged(nameof(SocatModifiedAt));
            this.RaisePropertyChanged(nameof(SocatIsReadOnly));
            this.RaisePropertyChanged(nameof(SocatIsDefault));
        }
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
            this.RaisePropertyChanged(nameof(SelectedPowerHost));
            this.RaisePropertyChanged(nameof(SelectedPowerPort));
            this.RaisePropertyChanged(nameof(SelectedPowerDeviceId));
            // Notify all power profile detail properties
            this.RaisePropertyChanged(nameof(PowerHost));
            this.RaisePropertyChanged(nameof(PowerPort));
            this.RaisePropertyChanged(nameof(PowerDeviceId));
            this.RaisePropertyChanged(nameof(PowerAddressingMode));
            this.RaisePropertyChanged(nameof(PowerConnectionTimeoutMs));
            this.RaisePropertyChanged(nameof(PowerReadTimeoutMs));
            this.RaisePropertyChanged(nameof(PowerWriteTimeoutMs));
            this.RaisePropertyChanged(nameof(PowerOnOffCoil));
            this.RaisePropertyChanged(nameof(PowerEnableAutoReconnect));
            this.RaisePropertyChanged(nameof(PowerMaxRetryAttempts));
            this.RaisePropertyChanged(nameof(PowerVersion));
            this.RaisePropertyChanged(nameof(PowerCreatedAt));
            this.RaisePropertyChanged(nameof(PowerModifiedAt));
            this.RaisePropertyChanged(nameof(PowerIsReadOnly));
            this.RaisePropertyChanged(nameof(PowerIsDefault));
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
        }
    }

    public uint MemoryLength
    {
        get => _memoryLength;
        set
        {
            this.RaiseAndSetIfChanged(ref _memoryLength, value);
            this.RaisePropertyChanged(nameof(MemoryEndAddress));
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
                var endAddress = MemoryStart + MemoryLength;
                return $"0x{endAddress:X}";
            }
            catch
            {
                return "Not calculated";
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
            return "N/A";
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
            return "N/A";
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
            return "N/A";
        }
    }

    #region Serial Profile Detail Properties

    /// <summary>
    /// Gets the baud rate from the selected serial profile configuration.
    /// </summary>
    public string SerialBaudRate => SelectedSerial?.Configuration?.BaudRate.ToString() ?? "N/A";

    /// <summary>
    /// Gets the character size from the selected serial profile configuration.
    /// </summary>
    public string SerialCharacterSize => SelectedSerial?.Configuration?.CharacterSize.ToString() ?? "N/A";

    /// <summary>
    /// Gets the parity mode from the selected serial profile configuration.
    /// </summary>
    public string SerialParity => SelectedSerial?.Configuration?.Parity.ToString() ?? "N/A";

    /// <summary>
    /// Gets the stop bits from the selected serial profile configuration.
    /// </summary>
    public string SerialStopBits => SelectedSerial?.Configuration?.StopBits.ToString() ?? "N/A";

    /// <summary>
    /// Gets the enable receiver flag from the selected serial profile configuration.
    /// </summary>
    public string SerialEnableReceiver => SelectedSerial?.Configuration?.EnableReceiver == true ? "Yes" :
                                         SelectedSerial?.Configuration?.EnableReceiver == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable hardware flow control flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableHardwareFlowControl => SelectedSerial?.Configuration?.DisableHardwareFlowControl == true ? "Yes" :
                                                      SelectedSerial?.Configuration?.DisableHardwareFlowControl == false ? "No" : "N/A";

    /// <summary>
    /// Gets the parity enabled flag from the selected serial profile configuration.
    /// </summary>
    public string SerialParityEnabled => SelectedSerial?.Configuration?.ParityEnabled == true ? "Yes" :
                                        SelectedSerial?.Configuration?.ParityEnabled == false ? "No" : "N/A";

    /// <summary>
    /// Gets the odd parity flag from the selected serial profile configuration.
    /// </summary>
    public string SerialOddParity => SelectedSerial?.Configuration?.OddParity == true ? "Yes" :
                                    SelectedSerial?.Configuration?.OddParity == false ? "No" : "N/A";

    /// <summary>
    /// Gets the ignore break flag from the selected serial profile configuration.
    /// </summary>
    public string SerialIgnoreBreak => SelectedSerial?.Configuration?.IgnoreBreak == true ? "Yes" :
                                      SelectedSerial?.Configuration?.IgnoreBreak == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable break interrupt flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableBreakInterrupt => SelectedSerial?.Configuration?.DisableBreakInterrupt == true ? "Yes" :
                                                SelectedSerial?.Configuration?.DisableBreakInterrupt == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable map CR to NL flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableMapCRtoNL => SelectedSerial?.Configuration?.DisableMapCRtoNL == true ? "Yes" :
                                          SelectedSerial?.Configuration?.DisableMapCRtoNL == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable bell on queue full flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableBellOnQueueFull => SelectedSerial?.Configuration?.DisableBellOnQueueFull == true ? "Yes" :
                                                 SelectedSerial?.Configuration?.DisableBellOnQueueFull == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable XON/XOFF flow control flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableXonXoffFlowControl => SelectedSerial?.Configuration?.DisableXonXoffFlowControl == true ? "Yes" :
                                                    SelectedSerial?.Configuration?.DisableXonXoffFlowControl == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable output processing flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableOutputProcessing => SelectedSerial?.Configuration?.DisableOutputProcessing == true ? "Yes" :
                                                  SelectedSerial?.Configuration?.DisableOutputProcessing == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable map NL to CR-NL flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableMapNLtoCRNL => SelectedSerial?.Configuration?.DisableMapNLtoCRNL == true ? "Yes" :
                                            SelectedSerial?.Configuration?.DisableMapNLtoCRNL == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable canonical mode flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableCanonicalMode => SelectedSerial?.Configuration?.DisableCanonicalMode == true ? "Yes" :
                                              SelectedSerial?.Configuration?.DisableCanonicalMode == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable signal generation flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableSignalGeneration => SelectedSerial?.Configuration?.DisableSignalGeneration == true ? "Yes" :
                                                  SelectedSerial?.Configuration?.DisableSignalGeneration == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable extended processing flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableExtendedProcessing => SelectedSerial?.Configuration?.DisableExtendedProcessing == true ? "Yes" :
                                                    SelectedSerial?.Configuration?.DisableExtendedProcessing == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable echo flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableEcho => SelectedSerial?.Configuration?.DisableEcho == true ? "Yes" :
                                      SelectedSerial?.Configuration?.DisableEcho == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable echo erase flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableEchoErase => SelectedSerial?.Configuration?.DisableEchoErase == true ? "Yes" :
                                          SelectedSerial?.Configuration?.DisableEchoErase == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable echo kill flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableEchoKill => SelectedSerial?.Configuration?.DisableEchoKill == true ? "Yes" :
                                         SelectedSerial?.Configuration?.DisableEchoKill == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable echo control flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableEchoControl => SelectedSerial?.Configuration?.DisableEchoControl == true ? "Yes" :
                                            SelectedSerial?.Configuration?.DisableEchoControl == false ? "No" : "N/A";

    /// <summary>
    /// Gets the disable echo kill erase flag from the selected serial profile configuration.
    /// </summary>
    public string SerialDisableEchoKillErase => SelectedSerial?.Configuration?.DisableEchoKillErase == true ? "Yes" :
                                              SelectedSerial?.Configuration?.DisableEchoKillErase == false ? "No" : "N/A";

    /// <summary>
    /// Gets the raw mode flag from the selected serial profile configuration.
    /// </summary>
    public string SerialRawMode => SelectedSerial?.Configuration?.RawMode == true ? "Yes" :
                                 SelectedSerial?.Configuration?.RawMode == false ? "No" : "N/A";

    /// <summary>
    /// Gets the version from the selected serial profile configuration.
    /// </summary>
    public string SerialVersion => SelectedSerial?.Version ?? "N/A";

    /// <summary>
    /// Gets the created at timestamp from the selected serial profile.
    /// </summary>
    public string SerialCreatedAt => SelectedSerial?.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

    /// <summary>
    /// Gets the modified at timestamp from the selected serial profile.
    /// </summary>
    public string SerialModifiedAt => SelectedSerial?.ModifiedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

    /// <summary>
    /// Gets the read-only status from the selected serial profile.
    /// </summary>
    public string SerialIsReadOnly => SelectedSerial?.IsReadOnly == true ? "Yes" :
                                    SelectedSerial?.IsReadOnly == false ? "No" : "N/A";

    /// <summary>
    /// Gets the default status from the selected serial profile.
    /// </summary>
    public string SerialIsDefault => SelectedSerial?.IsDefault == true ? "Yes" :
                                   SelectedSerial?.IsDefault == false ? "No" : "N/A";

    #endregion

    #region Socat Profile Detail Properties

    /// <summary>
    /// Gets the TCP port from the selected socat profile configuration.
    /// </summary>
    public string SocatTcpPort => SelectedSocat?.Configuration?.TcpPort.ToString() ?? "N/A";

    /// <summary>
    /// Gets the TCP host from the selected socat profile configuration.
    /// </summary>
    public string SocatTcpHost => SelectedSocat?.Configuration?.TcpHost ?? "N/A";

    /// <summary>
    /// Gets the enable fork flag from the selected socat profile configuration.
    /// </summary>
    public string SocatEnableFork => SelectedSocat?.Configuration?.EnableFork == true ? "Yes" :
                                   SelectedSocat?.Configuration?.EnableFork == false ? "No" : "N/A";

    /// <summary>
    /// Gets the enable reuse address flag from the selected socat profile configuration.
    /// </summary>
    public string SocatEnableReuseAddr => SelectedSocat?.Configuration?.EnableReuseAddr == true ? "Yes" :
                                        SelectedSocat?.Configuration?.EnableReuseAddr == false ? "No" : "N/A";

    /// <summary>
    /// Gets the verbose flag from the selected socat profile configuration.
    /// </summary>
    public string SocatVerbose => SelectedSocat?.Configuration?.Verbose == true ? "Yes" :
                                SelectedSocat?.Configuration?.Verbose == false ? "No" : "N/A";

    /// <summary>
    /// Gets the hex dump flag from the selected socat profile configuration.
    /// </summary>
    public string SocatHexDump => SelectedSocat?.Configuration?.HexDump == true ? "Yes" :
                                SelectedSocat?.Configuration?.HexDump == false ? "No" : "N/A";

    /// <summary>
    /// Gets the block size from the selected socat profile configuration.
    /// </summary>
    public string SocatBlockSize => SelectedSocat?.Configuration?.BlockSize.ToString() ?? "N/A";

    /// <summary>
    /// Gets the debug level from the selected socat profile configuration.
    /// </summary>
    public string SocatDebugLevel => SelectedSocat?.Configuration?.DebugLevel.ToString() ?? "N/A";

    /// <summary>
    /// Gets the serial raw mode flag from the selected socat profile configuration.
    /// </summary>
    public string SocatSerialRawMode => SelectedSocat?.Configuration?.SerialRawMode == true ? "Yes" :
                                      SelectedSocat?.Configuration?.SerialRawMode == false ? "No" : "N/A";

    /// <summary>
    /// Gets the serial disable echo flag from the selected socat profile configuration.
    /// </summary>
    public string SocatSerialDisableEcho => SelectedSocat?.Configuration?.SerialDisableEcho == true ? "Yes" :
                                          SelectedSocat?.Configuration?.SerialDisableEcho == false ? "No" : "N/A";

    /// <summary>
    /// Gets the auto configure serial flag from the selected socat profile configuration.
    /// </summary>
    public string SocatAutoConfigureSerial => SelectedSocat?.Configuration?.AutoConfigureSerial == true ? "Yes" :
                                            SelectedSocat?.Configuration?.AutoConfigureSerial == false ? "No" : "N/A";

    /// <summary>
    /// Gets the connection timeout from the selected socat profile configuration.
    /// </summary>
    public string SocatConnectionTimeout => SelectedSocat?.Configuration?.ConnectionTimeout.ToString() ?? "N/A";

    /// <summary>
    /// Gets the auto restart flag from the selected socat profile configuration.
    /// </summary>
    public string SocatAutoRestart => SelectedSocat?.Configuration?.AutoRestart == true ? "Yes" :
                                     SelectedSocat?.Configuration?.AutoRestart == false ? "No" : "N/A";

    /// <summary>
    /// Gets the version from the selected socat profile configuration.
    /// </summary>
    public string SocatVersion => SelectedSocat?.Configuration?.Version ?? "N/A";

    /// <summary>
    /// Gets the created at timestamp from the selected socat profile.
    /// </summary>
    public string SocatCreatedAt => SelectedSocat?.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

    /// <summary>
    /// Gets the modified at timestamp from the selected socat profile.
    /// </summary>
    public string SocatModifiedAt => SelectedSocat?.ModifiedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

    /// <summary>
    /// Gets the read-only status from the selected socat profile.
    /// </summary>
    public string SocatIsReadOnly => SelectedSocat?.IsReadOnly == true ? "Yes" :
                                   SelectedSocat?.IsReadOnly == false ? "No" : "N/A";

    /// <summary>
    /// Gets the default status from the selected socat profile.
    /// </summary>
    public string SocatIsDefault => SelectedSocat?.IsDefault == true ? "Yes" :
                                  SelectedSocat?.IsDefault == false ? "No" : "N/A";

    #endregion

    #region Power Supply Profile Detail Properties

    /// <summary>
    /// Gets the host from the selected power supply profile configuration.
    /// </summary>
    public string PowerHost => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.Host ?? "N/A";

    /// <summary>
    /// Gets the port from the selected power supply profile configuration.
    /// </summary>
    public string PowerPort => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.Port.ToString() ?? "N/A";

    /// <summary>
    /// Gets the device ID from the selected power supply profile configuration.
    /// </summary>
    public string PowerDeviceId => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.DeviceId.ToString() ?? "N/A";

    /// <summary>
    /// Gets the addressing mode from the selected power supply profile configuration.
    /// </summary>
    public string PowerAddressingMode => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.AddressingMode.ToString() ?? "N/A";

    /// <summary>
    /// Gets the connection timeout from the selected power supply profile configuration.
    /// </summary>
    public string PowerConnectionTimeoutMs => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.ConnectionTimeoutMs.ToString() ?? "N/A";

    /// <summary>
    /// Gets the read timeout from the selected power supply profile configuration.
    /// </summary>
    public string PowerReadTimeoutMs => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.ReadTimeoutMs.ToString() ?? "N/A";

    /// <summary>
    /// Gets the write timeout from the selected power supply profile configuration.
    /// </summary>
    public string PowerWriteTimeoutMs => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.WriteTimeoutMs.ToString() ?? "N/A";

    /// <summary>
    /// Gets the on/off coil address from the selected power supply profile configuration.
    /// </summary>
    public string PowerOnOffCoil => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.OnOffCoil.ToString() ?? "N/A";

    /// <summary>
    /// Gets the enable auto reconnect flag from the selected power supply profile configuration.
    /// </summary>
    public string PowerEnableAutoReconnect => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.EnableAutoReconnect == true ? "Yes" :
                                             (SelectedPower?.Configuration as ModbusTcpConfiguration)?.EnableAutoReconnect == false ? "No" : "N/A";

    /// <summary>
    /// Gets the max retry attempts from the selected power supply profile configuration.
    /// </summary>
    public string PowerMaxRetryAttempts => (SelectedPower?.Configuration as ModbusTcpConfiguration)?.MaxRetryAttempts.ToString() ?? "N/A";

    /// <summary>
    /// Gets the version from the selected power supply profile.
    /// </summary>
    public string PowerVersion => SelectedPower?.Version ?? "N/A";

    /// <summary>
    /// Gets the created at timestamp from the selected power supply profile.
    /// </summary>
    public string PowerCreatedAt => SelectedPower?.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

    /// <summary>
    /// Gets the modified at timestamp from the selected power supply profile.
    /// </summary>
    public string PowerModifiedAt => SelectedPower?.ModifiedAt.ToString("yyyy-MM-dd HH:mm") ?? "N/A";

    /// <summary>
    /// Gets the read-only status from the selected power supply profile.
    /// </summary>
    public string PowerIsReadOnly => SelectedPower?.IsReadOnly == true ? "Yes" :
                                   SelectedPower?.IsReadOnly == false ? "No" : "N/A";

    /// <summary>
    /// Gets the default status from the selected power supply profile.
    /// </summary>
    public string PowerIsDefault => SelectedPower?.IsDefault == true ? "Yes" :
                                  SelectedPower?.IsDefault == false ? "No" : "N/A";

    #endregion

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

    private async Task ScanPortsAsync()
    {
        if (IsScanning)
        {
            return;
        }

        try
        {
            IsScanning = true;
            Status = "Scanning for ports...";

            // Execute the scanner's scan command and wait for completion
            await SerialScanner.ScanPortsCommand.Execute();

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                AvailablePorts.Clear();
                foreach (var port in SerialScanner.DiscoveredPorts)
                {
                    AvailablePorts.Add(port.PortName);
                }

                Status = $"Found {AvailablePorts.Count} port(s)";
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan ports in job wizard");
            Status = $"Error scanning ports: {ex.Message}";
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
            SerialScanner?.Dispose();
            // no resources
        }
    }
}
