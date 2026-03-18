using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Services;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for real-time PLC memory dump visualization.
/// Provides high-performance rendering using virtualization and batching.
/// </summary>
public partial class StreamedMemoryDumpViewModel : ViewModelBase, S7Tools.Core.Interfaces.ViewModels.IDockableViewModel, IDisposable
{
    // IDockableViewModel implementation
    public string DockId => "StreamedMemoryDump";
    public string DockTitle => "Streamed PLC Memory Viewer";
    public bool CanClose => true;
    public bool CanFloat => true;

    private readonly MemoryDumpOrchestrator _orchestrator;
    private readonly ILogger<StreamedMemoryDumpViewModel> _logger;

    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private string _host = "127.0.0.1";
    private int _port = 3333;
    private string _startAddress = "0x00000000";
    private long _totalBytes;
    private int _blockCount;

    private CancellationTokenSource? _cts;

    /// <summary>
    /// Gets or sets a value indicating whether the viewer is connected.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    /// <summary>
    /// Gets or sets the connection status message.
    /// </summary>
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    /// <summary>
    /// Gets or sets the socat host.
    /// </summary>
    public string Host
    {
        get => _host;
        set => this.RaiseAndSetIfChanged(ref _host, value);
    }

    /// <summary>
    /// Gets or sets the socat port.
    /// </summary>
    public int Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }

    /// <summary>
    /// Gets or sets the start address (hex format).
    /// </summary>
    public string StartAddress
    {
        get => _startAddress;
        set => this.RaiseAndSetIfChanged(ref _startAddress, value);
    }

    /// <summary>
    /// Gets or sets the total bytes received.
    /// </summary>
    public long TotalBytes
    {
        get => _totalBytes;
        set => this.RaiseAndSetIfChanged(ref _totalBytes, value);
    }

    /// <summary>
    /// Gets or sets the block count.
    /// </summary>
    public int BlockCount
    {
        get => _blockCount;
        set => this.RaiseAndSetIfChanged(ref _blockCount, value);
    }

    /// <summary>
    /// Gets the collection of memory blocks for display.
    /// </summary>
    public ObservableCollection<MemoryBlock> MemoryBlocks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamedMemoryDumpViewModel"/> class.
    /// </summary>
    public StreamedMemoryDumpViewModel(
        MemoryDumpOrchestrator orchestrator,
        ILogger<StreamedMemoryDumpViewModel> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Share the orchestrator's collection for zero-copy binding
        MemoryBlocks = orchestrator.MemoryBlocks;

        // Subscribe to collection changes for statistics
        MemoryBlocks.CollectionChanged += (s, e) =>
        {
            BlockCount = MemoryBlocks.Count;
            TotalBytes = _orchestrator.TotalBytesReceived;
        };
    }

    /// <summary>
    /// Connects to the socat bridge and starts memory dump ingestion.
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsConnected)
        {
            _logger.LogWarning("Already connected");
            return;
        }

        try
        {
            // Parse start address
            uint startAddr = ParseAddress(StartAddress);

            _logger.LogInformation("Connecting to {Host}:{Port} starting at 0x{Address:X8}", Host, Port, startAddr);

            // Configure orchestrator
            _orchestrator.Configure(Host, Port, startAddr);

            // Yield to UI thread to ensure status updates render
            await Task.Yield();

            _cts = new CancellationTokenSource();

            IsConnected = true;
            ConnectionStatus = $"Connected to {Host}:{Port}";

            // Start session in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _orchestrator.StartSessionAsync(cancellationToken: _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Memory dump session error");
                    await DisconnectInternalAsync();
                }
            }, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect");
            ConnectionStatus = $"Error: {ex.Message}";
            IsConnected = false;
        }
    }

    /// <summary>
    /// Disconnects from the socat bridge and stops ingestion.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await DisconnectInternalAsync();
    }

    private async Task DisconnectInternalAsync()
    {
        if (!IsConnected)
        {
            return;
        }

        _logger.LogInformation("Disconnecting from memory dump session");

        _cts?.Cancel();
        await _orchestrator.StopAsync();

        _cts?.Dispose();
        _cts = null;

        IsConnected = false;
        ConnectionStatus = "Disconnected";
    }

    /// <summary>
    /// Clears all memory blocks from the view.
    /// </summary>
    [RelayCommand]
    private async Task ClearAsync()
    {
        _logger.LogInformation("Clearing memory dump viewer");
        await _orchestrator.ClearAsync();
        TotalBytes = 0;
        BlockCount = 0;
    }

    /// <summary>
    /// Parses a hexadecimal address string.
    /// </summary>
    private static uint ParseAddress(string addressStr)
    {
        string cleanAddr = addressStr.Trim().Replace("0x", "").Replace("0X", "");
        return Convert.ToUInt32(cleanAddr, 16);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (IsConnected)
            {
                Task.Run(async () => await DisconnectInternalAsync()).Wait();
            }
            else
            {
                _cts?.Dispose();
            }
        }
    }
}
