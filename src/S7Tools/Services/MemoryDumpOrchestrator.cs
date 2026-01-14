using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.MemoryDump;
using S7Tools.Core.Services;
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

    private CancellationTokenSource? _cts;
    private Task? _consumptionTask;

    /// <summary>
    /// Gets the observable collection of memory blocks for UI binding.
    /// </summary>
    public ObservableCollection<MemoryBlock> MemoryBlocks { get; } = new();

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
        _dumperService.Configure(host, port, startAddress);
        _batchSize = batchSize;
    }

    /// <summary>
    /// Starts the memory dump session asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_consumptionTask != null)
        {
            throw new InvalidOperationException("Session is already running. Call StopAsync first.");
        }

        // Clear previous session data
        await _uiThreadService.InvokeOnUIThreadAsync(() => MemoryBlocks.Clear()).ConfigureAwait(false);
        TotalBytesReceived = 0;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _logger.LogInformation("Starting memory dump session");

        // Start dumper service (producer)
        Task dumpTask = _dumperService.StartDumpingAsync(_cts.Token);

        // Start consumption and UI dispatch (consumer)
        _consumptionTask = ConsumeAndDispatchAsync(_dumperService.DataReader, _cts.Token);

        try
        {
            // Wait for either task to complete
            Task completedTask = await Task.WhenAny(dumpTask, _consumptionTask).ConfigureAwait(false);

            // Propagate exceptions if any
            await completedTask.ConfigureAwait(false);

            _logger.LogInformation("Memory dump session completed. Total bytes: {Bytes}", TotalBytesReceived);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Memory dump session cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory dump session");
            throw;
        }
    }

    /// <summary>
    /// Consumes memory blocks from the channel and dispatches them to the UI thread in batches.
    /// </summary>
    private async Task ConsumeAndDispatchAsync(ChannelReader<MemoryBlock> reader, CancellationToken cancellationToken)
    {
        List<MemoryBlock> batch = new(_batchSize);

        try
        {
            await foreach (MemoryBlock block in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                batch.Add(block);
                TotalBytesReceived += block.Size;

                // When batch is full, dispatch to UI
                if (batch.Count >= _batchSize)
                {
                    MemoryBlock[] batchCopy = batch.ToArray();
                    batch.Clear();

                    await DispatchBatchToUIAsync(batchCopy).ConfigureAwait(false);
                }
            }

            // Process remaining blocks
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
        }
    }

    /// <summary>
    /// Dispatches a batch of memory blocks to the UI thread.
    /// </summary>
    private async Task DispatchBatchToUIAsync(MemoryBlock[] batch)
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            foreach (MemoryBlock block in batch)
            {
                MemoryBlocks.Add(block);
            }
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
        StopAsync().GetAwaiter().GetResult();
        _dumperService.Dispose();
    }
}
