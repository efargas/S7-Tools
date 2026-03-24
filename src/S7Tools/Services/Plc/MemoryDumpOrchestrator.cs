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
    /// Sets the batch size for UI updates.
    /// </summary>
    /// <param name="size">The new batch size.</param>
    public void SetBatchSize(int size)
    {
        _batchSize = size;
    }

    /// <summary>
    /// Starts a memory dump session for ONE segment/iteration.
    /// IMPORTANT: For multi-iteration dumps, this will stop any existing session
    /// and start fresh to ensure proper greeting detection and data separation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the session.</param>
    /// <param name="stream">Optional existing stream. If null, a new socket connection is created.</param>
    /// <param name="logger">Logger for the dump session.</param>
    public async Task StartSessionAsync(CancellationToken cancellationToken = default, Stream? stream = null, ILogger? logger = null)
    {
        // CRITICAL: Always stop previous session to ensure clean pipeline for each iteration.
        // The working version (pre-refactoring) used synchronous request-response for each dump.
        // We must replicate that behavior: complete stop → fresh start → wait for "Ok" → receive data.
        await StopAsync().ConfigureAwait(false);

        await _uiThreadService.InvokeOnUIThreadAsync(() => MemoryBlocks.Clear()).ConfigureAwait(false);
        TotalBytesReceived = 0;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _logger.LogInformation("🚀 Starting memory dump session (fresh pipeline)");

        // Start dumper service (producer) - this sets _expectGreeting = true
        _dumpTask = _dumperService.StartDumpingAsync(_host, _port, _cts.Token, stream, logger);

        // Start consumption and UI dispatch (consumer)
        _consumptionTask = ConsumeAndDispatchAsync(_dumperService.DataReader, _cts.Token);
    }

    /// <summary>
    /// Stops the current memory dump session.
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("🛑 Stopping continuous memory dump session");
        
        // Ensure dumper service sends cancellation byte (0x03) immediately before we stop
        await _dumperService.StopAsync().ConfigureAwait(false);

        if (_cts != null)
        {
            _cts.Cancel();
            try
            {
                await Task.WhenAll(_dumpTask ?? Task.CompletedTask, _consumptionTask ?? Task.CompletedTask).ConfigureAwait(false);
            }
            catch { }

            _cts.Dispose();
            _cts = null;
        }
    }

    // Gate for controlling data consumption between segments
    private volatile TaskCompletionSource _consumptionGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Waits for a specific amount of data to be received for a segment.
    /// </summary>
    public async Task WaitForSegmentAsync(uint startAddress, long length, Func<MemoryBlock, Task>? callback, CancellationToken ct)
    {
        try
        {
            await _segmentLock.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // If the lock is disposed, the orchestrator is shutting down.
            // Treat as cancellation to allow graceful unwind.
            throw new OperationCanceledException("Session usage cancelled (ObjectDisposed).");
        }
        try
        {
            _currentSegmentAddress = startAddress;
            _remainingSegmentBytes = length;
            _segmentCallback = callback;
            _segmentTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using var reg = ct.Register(() => _segmentTcs.TrySetCanceled());

            // Open the gate to allow consumption
            _consumptionGate.TrySetResult();

            _logger.LogDebug("Waiting for segment: 0x{Addr:X8}, {Len} bytes", startAddress, length);
            await _segmentTcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _segmentLock.Release();
        }
    }

    // ... (InvokeDumpCommandAsync remains unchanged) ...
    /// <summary>
    /// Sends a dump command and waits for the specific segment to complete.
    /// Assumes StartSessionAsync has already been called.
    /// </summary>
    public async Task InvokeDumpCommandAsync(byte[] args, uint startAddress, long length, Func<MemoryBlock, Task>? callback, CancellationToken ct)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Session not active. Call StartSessionAsync first.");
        }

        // --- CRITICAL: Flush any leftover data from previous segment ---
        // This must happen BEFORE resetting state to avoid data corruption
        await _dumperService.FlushRemainingDataAsync(ct).ConfigureAwait(false);

        // --- CRITICAL: Reset parser state BEFORE sending command ---
        // The PLC responds immediately. We must be in greeting-mode before
        // any response bytes can arrive, otherwise greeting is consumed as data.
        _dumperService.ResetForNewSegment();

        // --- RELIABILITY FIX ---
        // Add settling delay to let pipeline stabilize
        await Task.Delay(200, ct).ConfigureAwait(false);

        // Frame and send command
        byte[] hookPayload = new byte[1 + args.Length];
        hookPayload[0] = (byte)PlcConstants.DEFAULT_SECOND_ADD_HOOK_IND;
        Array.Copy(args, 0, hookPayload, 1, args.Length);

        // Wrapping in Primary Handler framing: HandlerIndex (0x1C) + Payload
        byte[] primaryPayload = new byte[1 + hookPayload.Length];
        primaryPayload[0] = 0x1C; // Invoke Add-Hook
        Array.Copy(hookPayload, 0, primaryPayload, 1, hookPayload.Length);

        // Final packet framing (Length + Data + Checksum)
        byte[] packet = S7Tools.Services.Plc.Adapters.PlcProtocolAdapter.EncodePacket(primaryPayload);

        _logger.LogInformation("Sending dump command (framed) via dump connection...");
        await _dumperService.WriteAsync(packet, ct).ConfigureAwait(false);

        // For now, we assume the command succeeded if it was sent.
        // The DumperService will skip the "Ok" in its background loop.

        _logger.LogInformation("Dump command sent. Waiting for segment data...");

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
            // Initial gate state: If we started with 0 bytes expectation, we should be closed.
            // But StartSessionAsync might be called before WaitForSegmentAsync.
            // Let's assume gate is closed by default (initialized above).

            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for the gate to open BEFORE reading from the channel
                // This prevents pulling data out of the channel that needs to be flushed
                await _consumptionGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

                // Wait for data to be available
                if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    break; // Channel closed
                }

                // Read the data
                if (!reader.TryRead(out MemoryBlock rawBlock))
                {
                    continue;
                }

                if (_remainingSegmentBytes <= 0)
                {
                    // Gate closed just as we read, shouldn't happen but discard to be safe
                    continue;
                }

                // Map to absolute address if we are in a segment
                MemoryBlock block = rawBlock;

                // Note: _remainingSegmentBytes is positive here because gate is open

                block = new MemoryBlock(_currentSegmentAddress, rawBlock.Data);

                if (_segmentCallback != null)
                {
                    await _segmentCallback(block).ConfigureAwait(false);
                }

                _currentSegmentAddress += (uint)rawBlock.Size;
                _remainingSegmentBytes -= rawBlock.Size;

                if (_remainingSegmentBytes <= 0)
                {
                    // Segment complete. Close the gate immediately for the NEXT block.
                    _consumptionGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    _segmentTcs?.TrySetResult();
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
