using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Collections;
using S7Tools.Core.Models;
using S7Tools.Services.Adapters.Plc;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Orchestrates real-time memory dump visualization by coordinating DumperService
/// with UI thread dispatch and batching for optimal rendering performance.
/// </summary>
public sealed class MemoryDumpOrchestrator : IDisposable
{
    private readonly DumperService _dumperService;
    private readonly IUIThreadService _uiThreadService;
    private readonly ILogger<MemoryDumpOrchestrator> _logger;

    // Batching configuration
    private const int DefaultBatchSize = 250;
    private int _batchSize = DefaultBatchSize;

    // Connection configuration
    private string _host = "127.0.0.1";
    private int _port;
    private uint _startAddress;

    private CancellationTokenSource? _cts;
    private Task? _consumptionTask;
    private Task? _dumpTask;

    // Segment coordination
    private readonly SemaphoreSlim _segmentLock = new(1, 1);
    private TaskCompletionSource? _segmentTcs;
    private uint _currentSegmentAddress;
    private long _remainingSegmentBytes;
    private Func<MemoryBlock, Task>? _segmentCallback;

    /// <summary>
    /// Gets the observable collection of memory blocks for UI binding.
    /// </summary>
    public FastObservableCollection<MemoryBlock> MemoryBlocks { get; } = new();

    /// <summary>
    /// Gets the total number of bytes received.
    /// </summary>
    public long TotalBytesReceived { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the orchestrator is currently active.
    /// </summary>
    public bool IsActive => _consumptionTask?.IsCompleted == false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDumpOrchestrator"/> class.
    /// </summary>
    /// <param name="dumperService">The dumper service for data ingestion.</param>
    /// <param name="uiThreadService">Service for dispatching to UI thread.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public MemoryDumpOrchestrator(
        DumperService dumperService,
        IUIThreadService uiThreadService,
        ILogger<MemoryDumpOrchestrator> logger)
    {
        _dumperService = dumperService ?? throw new ArgumentNullException(nameof(dumperService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Configures the orchestrator parameters.
    /// </summary>
    /// <param name="host">Socat TCP host.</param>
    /// <param name="port">Socat TCP port.</param>
    /// <param name="startAddress">Starting memory address.</param>
    /// <param name="batchSize">Number of blocks to batch before UI update (default 250).</param>
    public void Configure(string host, int port, uint startAddress = 0x0000, int batchSize = DefaultBatchSize)
    {
        _host = host;
        _port = port;
        _startAddress = startAddress;
        _batchSize = batchSize;
    }

    /// <summary>
    /// Starts the persistent memory dump session if not already running.
    /// </summary>
    public async Task StartSessionAsync(CancellationToken cancellationToken = default, Stream? existingStream = null)
    {
        if (IsActive)
            return;

        await _uiThreadService.InvokeOnUIThreadAsync(() => MemoryBlocks.Clear()).ConfigureAwait(false);
        TotalBytesReceived = 0;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _logger.LogInformation("Starting persistent memory dump session");

        // Start dumper service (producer) - no fixed start address here
        _dumpTask = _dumperService.StartDumpingAsync(_host, _port, _cts.Token, existingStream);

        // Start consumption and UI dispatch (consumer)
        _consumptionTask = ConsumeAndDispatchAsync(_dumperService.DataReader, _cts.Token);
    }

    /// <summary>
    /// Waits for a specific amount of data to be received for a segment.
    /// </summary>
    public async Task WaitForSegmentAsync(uint startAddress, long length, Func<MemoryBlock, Task>? callback, CancellationToken ct)
    {
        await _segmentLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _currentSegmentAddress = startAddress;
            _remainingSegmentBytes = length;
            _segmentCallback = callback;
            _segmentTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using var reg = ct.Register(() => _segmentTcs.TrySetCanceled());

            _logger.LogDebug("Waiting for segment: 0x{Addr:X8}, {Len} bytes", startAddress, length);
            await _segmentTcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _segmentLock.Release();
        }
    }

    /// <summary>
    /// Invokes the dumper command and waits for the segment.
    /// This method handles framing and sending the command through the dump connection.
    /// </summary>
    public async Task InvokeDumpCommandAsync(byte handlerIndex, byte[] args, uint startAddress, long length, Func<MemoryBlock, Task>? callback, CancellationToken ct, Stream? existingStream = null)
    {
        // Ensure session is started
        await StartSessionAsync(ct, existingStream).ConfigureAwait(false);

        // --- RELIABILITY FIX ---
        // Add a small settling delay to allow the PLC to clear its state 
        // and its UART buffers before the next command.
        _logger.LogDebug("Settling for 100ms before next iteration...");
        await Task.Delay(100, ct).ConfigureAwait(false);

        // Settlements and greetings are now handled exclusively by timing and Pipe logic

        // Tell dumper service to skip the next greeting
        _dumperService.ExpectGreeting();

        // Frame and send command
        // Wrapping in Hook framing: HookNo + Args
        byte[] hookPayload = new byte[1 + args.Length];
        hookPayload[0] = handlerIndex;
        Array.Copy(args, 0, hookPayload, 1, args.Length);

        // Wrapping in Primary Handler framing: HandlerIndex (0x1C) + Payload
        byte[] primaryPayload = new byte[1 + hookPayload.Length];
        primaryPayload[0] = 0x1C; // Invoke Add-Hook
        Array.Copy(hookPayload, 0, primaryPayload, 1, hookPayload.Length);

        // Final packet framing (Length + Data + Checksum)
        byte[] packet = Adapters.PlcProtocolAdapter.EncodePacket(primaryPayload);

        _logger.LogInformation("Sending dump command (framed) via dump connection...");
        await _dumperService.WriteAsync(packet, ct).ConfigureAwait(false);

        // For now, we assume the command succeeded if it was sent.
        // The DumperService will skip the "Ok" in its background loop.

        _logger.LogInformation("Dump command sent. Starting segment reception.");

        // Now wait for the segment data
        await WaitForSegmentAsync(startAddress, length, callback, ct).ConfigureAwait(false);
    }

    private async Task ConsumeAndDispatchAsync(
        ChannelReader<MemoryBlock> reader,
        CancellationToken cancellationToken)
    {
        List<MemoryBlock> batch = new(_batchSize);

        try
        {
            await foreach (MemoryBlock rawBlock in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                // Map to absolute address if we are in a segment
                MemoryBlock block = rawBlock;
                if (_remainingSegmentBytes > 0)
                {
                    block = new MemoryBlock(_currentSegmentAddress, rawBlock.Data);

                    if (_segmentCallback != null)
                    {
                        await _segmentCallback(block).ConfigureAwait(false);
                    }

                    _currentSegmentAddress += (uint)rawBlock.Size;
                    _remainingSegmentBytes -= rawBlock.Size;

                    if (_remainingSegmentBytes <= 0)
                    {
                        _segmentTcs?.TrySetResult();
                    }
                }

                batch.Add(block);
                TotalBytesReceived += block.Size;

                if (batch.Count >= _batchSize)
                {
                    MemoryBlock[] batchCopy = batch.ToArray();
                    batch.Clear();
                    await DispatchBatchToUIAsync(batchCopy).ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                await DispatchBatchToUIAsync(batch.ToArray()).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Consumption cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming memory blocks");
            _segmentTcs?.TrySetException(ex);
        }
    }

    /// <summary>
    /// Dispatches a batch of memory blocks to the UI thread.
    /// </summary>
    private async Task DispatchBatchToUIAsync(MemoryBlock[] batch)
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            MemoryBlocks.AddRange(batch);
        }).ConfigureAwait(false);

        _logger.LogTrace("Dispatched batch of {Count} blocks to UI", batch.Length);
    }

    /// <summary>
    /// Stops the current memory dump session.
    /// </summary>
    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_consumptionTask != null)
        {
            try
            {
                await _consumptionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            _consumptionTask = null;
        }

        await _dumperService.StopAsync().ConfigureAwait(false);

        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Clears all memory blocks from the collection.
    /// </summary>
    public async Task ClearAsync()
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            MemoryBlocks.Clear();
            TotalBytesReceived = 0;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _segmentLock.Dispose();
    }
}
