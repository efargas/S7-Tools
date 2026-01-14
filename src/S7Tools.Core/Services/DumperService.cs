using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.MemoryDump;
using S7Tools.Core.Protocol;

namespace S7Tools.Core.Services;

/// <summary>
/// High-performance service for ingesting PLC memory dumps via a UART-TCP bridge (socat).
/// Uses System.IO.Pipelines for zero-copy network ingestion and System.Threading.Channels
/// for efficient producer-consumer decoupling.
/// </summary>
public sealed class DumperService : IDisposable
{
    private readonly ILogger<DumperService> _logger;
    private Socket? _socket;
    private readonly Pipe _pipe;
    private CancellationTokenSource? _cts;
    private readonly Channel<MemoryBlock> _outputChannel;

    // Connection configuration
    private string _host = "127.0.0.1";
    private int _port = 3333;

    // Protocol configuration
    private int _blockSize = ProtocolParser.DefaultBlockSize;
    private uint _startAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="DumperService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public DumperService(ILogger<DumperService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize pipe with default options that use shared MemoryPool
        _pipe = new Pipe();

        // Unbounded channel for maximum ingestion throughput
        // UI layer is responsible for consumption rate management
        _outputChannel = Channel.CreateUnbounded<MemoryBlock>(new UnboundedChannelOptions
        {
            SingleWriter = true,  // Only the parser task writes
            SingleReader = false  // May have multiple UI consumers
        });
    }

    /// <summary>
    /// Gets the channel reader for consuming parsed memory blocks.
    /// </summary>
    public ChannelReader<MemoryBlock> DataReader => _outputChannel.Reader;

    /// <summary>
    /// Gets a value indicating whether the service is currently connected and dumping.
    /// </summary>
    public bool IsRunning => _cts?.IsCancellationRequested == false;

    /// <summary>
    /// Configures connection parameters for the socat TCP bridge.
    /// </summary>
    /// <param name="host">TCP host address (typically localhost).</param>
    /// <param name="port">TCP port number where socat is listening.</param>
    /// <param name="startAddress">Starting memory address to assign to incoming data.</param>
    /// <param name="blockSize">Size of memory blocks for parsing (default 16 bytes).</param>
    public void Configure(string host, int port, uint startAddress = 0x0000, int blockSize = ProtocolParser.DefaultBlockSize)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535");
        }

        if (blockSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be positive");
        }

        _host = host;
        _port = port;
        _startAddress = startAddress;
        _blockSize = blockSize;
    }

    /// <summary>
    /// Starts the memory dump ingestion process asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartDumpingAsync(CancellationToken cancellationToken = default)
    {
        if (_socket != null)
        {
            throw new InvalidOperationException("Dumper service is already running. Call StopAsync first.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            _logger.LogInformation("Connecting to socat bridge at {Host}:{Port}...", _host, _port);

            // Establish TCP connection to socat
            await _socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(_host), _port), _cts.Token)
                .ConfigureAwait(false);

            _logger.LogInformation("Connected to socat bridge. Starting pipeline ingestion...");

            // Start concurrent producer (socket→pipe) and consumer (pipe→channel) tasks
            Task fillTask = FillPipeAsync(_socket, _pipe.Writer, _cts.Token);
            Task parseTask = ProcessPipeAsync(_pipe.Reader, _outputChannel.Writer, _cts.Token);

            // Wait for both tasks to complete
            await Task.WhenAll(fillTask, parseTask).ConfigureAwait(false);

            _logger.LogInformation("Memory dump ingestion completed");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Memory dump operation cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during memory dump ingestion");
            throw;
        }
        finally
        {
            await StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Fills the pipe with data from the socket (Producer).
    /// This runs in a tight loop, reading from the kernel socket buffer and writing
    /// directly into pipe memory without intermediate allocations.
    /// </summary>
    private async Task FillPipeAsync(Socket socket, PipeWriter writer, CancellationToken cancellationToken)
    {
        const int MinBufferSize = 4096;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Request memory from the pipe's pool (zero allocation)
                Memory<byte> memory = writer.GetMemory(MinBufferSize);

                try
                {
                    // Read directly into pipe memory
                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        // Socket closed by remote end (EOF)
                        _logger.LogDebug("Socket closed by socat (EOF received)");
                        break;
                    }

                    // Notify writer of actual bytes written
                    writer.Advance(bytesRead);
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Socket error during receive");
                    break;
                }

                // Make data available to reader
                FlushResult result = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCompleted)
                {
                    _logger.LogDebug("Pipe reader has stopped reading");
                    break;
                }
            }
        }
        finally
        {
            // Signal end of stream to reader
            await writer.CompleteAsync().ConfigureAwait(false);
            _logger.LogDebug("Pipe filling completed");
        }
    }

    /// <summary>
    /// Processes data from the pipe and parses into memory blocks (Consumer).
    /// Handles fragmentation transparently using Pipe's AdvanceTo mechanism.
    /// </summary>
    private async Task ProcessPipeAsync(PipeReader reader, ChannelWriter<MemoryBlock> writer, CancellationToken cancellationToken)
    {
        uint currentAddress = _startAddress;
        List<MemoryBlock> batchBuffer = new(capacity: 100);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;

                if (buffer.Length == 0 && result.IsCompleted)
                {
                    _logger.LogDebug("Pipe completed with no remaining data");
                    break;
                }

                // Parse all complete blocks from current buffer
                SequencePosition consumed = ProtocolParser.ParseMemoryDump(
                    buffer,
                    ref currentAddress,
                    _blockSize,
                    batchBuffer);

                // Write parsed blocks to output channel
                foreach (MemoryBlock block in batchBuffer)
                {
                    await writer.WriteAsync(block, cancellationToken).ConfigureAwait(false);
                }

                if (batchBuffer.Count > 0)
                {
                    _logger.LogTrace("Parsed and published {Count} memory blocks", batchBuffer.Count);
                    batchBuffer.Clear();
                }

                // Tell pipe what we consumed and what we examined
                // consumed: data we successfully processed (freed)
                // buffer.End: we examined everything but couldn't process incomplete blocks
                reader.AdvanceTo(consumed, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pipe processing");
        }
        finally
        {
            // Signal completion to channel consumers
            writer.TryComplete();
            await reader.CompleteAsync().ConfigureAwait(false);
            _logger.LogDebug("Pipe processing completed");
        }
    }

    /// <summary>
    /// Stops the memory dump ingestion process.
    /// </summary>
    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_socket != null)
        {
            if (_socket.Connected)
            {
                try
                {
                    await _socket.DisconnectAsync(false).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disconnecting socket");
                }
            }

            _socket.Dispose();
            _socket = null;
        }

        _cts?.Dispose();
        _cts = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}
