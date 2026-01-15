using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;

namespace S7Tools.Services.Adapters.Plc
{
    /// <summary>
    /// High-performance service for PLC memory dump ingestion via UART-TCP bridge (socat).
    /// Uses System.IO.Pipelines for zero-allocation reading and Channels for async processing.
    /// </summary>
    public sealed class DumperService : IDisposable
    {
        private readonly ILogger<DumperService> _logger;
        private Socket? _socket;
        private Stream? _stream; // Can be internal or external
        private Pipe _pipe; // Not readonly - needs to be recreated for multi-iteration dumps
        private CancellationTokenSource? _cts;
        private Channel<MemoryBlock>? _outputChannel;
        private bool _isExternalStream;
        private int _consecutiveEofCount;
        private const int MaxEofRetries = 100; // 5 seconds at 50ms each
        private ILogger? _sessionLogger;
        private ILogger Logger => _sessionLogger ?? _logger;
        private bool _expectGreeting;

        public DumperService(ILogger<DumperService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize Pipe with default options (uses shared MemoryPool)
            _pipe = new Pipe();
        }

        // Expose the reader for the consumer
        public ChannelReader<MemoryBlock> DataReader => _outputChannel?.Reader ?? throw new InvalidOperationException("Dumper session not started");

        public async Task StartDumpingAsync(string host, int port, CancellationToken token, Stream? existingStream = null, ILogger? logger = null)
        {
            _sessionLogger = logger;
            // Always stop previous session to cancel old tasks (e.g. FillPipeAsync)
            // protecting the stream from concurrent reads.
            await StopAsync();

            if (existingStream != null)
            {
                // Recreate pipe for fresh iteration
                _pipe = new Pipe();
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _isExternalStream = existingStream != null;
            _consecutiveEofCount = 0; // Reset EOF counter for new session
            _expectGreeting = true; // Reset grammar state expecting greeting first

            // Recreate channel for each session since channels cannot be "reopened" after completion
            _outputChannel = Channel.CreateUnbounded<MemoryBlock>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true
            });

            try
            {
                if (existingStream != null)
                {
                    Logger.LogInformation("Using existing stream for dumper session.");
                    _stream = existingStream;
                }
                else
                {
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Logger.LogInformation("Connecting to socat at {Host}:{Port}...", host, port);
                    await _socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port), _cts.Token);
                    _stream = new NetworkStream(_socket, ownsSocket: true);
                }

                Logger.LogInformation("Connection established. Starting ingestion pipeline.");

                var fillTask = FillPipeAsync(_stream, _pipe.Writer, _cts.Token);
                var readTask = ProcessPipeAsync(_pipe.Reader, _outputChannel.Writer, _cts.Token);

                await Task.WhenAll(fillTask, readTask);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Dump operation canceled.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Critical error during memory dump.");
                throw;
            }
            finally
            {
                // Internal cleanup, but don't call full StopAsync yet as we want to preserve 
                // the channel state for final consumption if needed. 
                // However, ProcessPipeAsync already completes the writer.
                if (!_isExternalStream)
                {
                    _stream?.Dispose();
                    _socket?.Dispose();
                    _stream = null;
                    _socket = null;
                }
                // For external streams, don't set _stream to null - it's still valid for next iteration
            }
        }

        /// <summary>
        /// Resets the parser state for a new segment command.
        /// Called before sending a new 'A' command on an existing session.
        /// PLC sends "Ok" greeting for EACH dump command, so we need to expect it.
        /// </summary>
        public void ResetForNewSegment()
        {
            _expectGreeting = true;
            _consecutiveEofCount = 0;
            Logger.LogDebug("♻️ Dumper state reset for new segment (expecting greeting).");
        }

        /// <summary>
        /// Flushes any remaining data from the output channel.
        /// Call this AFTER a segment completes and BEFORE starting a new segment.
        /// This prevents leftover data from corrupting the next iteration.
        /// </summary>
        public async Task FlushRemainingDataAsync(CancellationToken token)
        {
            if (_outputChannel == null)
                return;

            int flushedCount = 0;
            while (_outputChannel.Reader.TryRead(out _))
            {
                flushedCount++;
            }

            if (flushedCount > 0)
            {
                Logger.LogDebug("🚿 Flushed {Count} remaining blocks from channel", flushedCount);
            }
            else
            {
                Logger.LogDebug("🚿 Channel was already empty, no flush needed");
            }

            // Small delay to let any in-flight data settle
            await Task.Delay(50, token).ConfigureAwait(false);
        }

        private async Task FillPipeAsync(Stream stream, PipeWriter writer, CancellationToken token)
        {
            const int MinBufferSize = 4096;

            while (!token.IsCancellationRequested)
            {
                Memory<byte> memory = writer.GetMemory(MinBufferSize);

                try
                {
                    int bytesRead = await stream.ReadAsync(memory, token);

                    if (bytesRead == 0)
                    {
                        // EOF received - but don't break immediately, PLC might send more data
                        // This is critical for multi-iteration dumps
                        _consecutiveEofCount++;
                        if (_consecutiveEofCount >= MaxEofRetries)
                        {
                            Logger.LogWarning("Max EOF retries reached ({Count}), stopping reader", MaxEofRetries);
                            break;
                        }
                        Logger.LogDebug("EOF detected ({Count}/{Max}), waiting for more data...",
                            _consecutiveEofCount, MaxEofRetries);
                        await Task.Delay(50, token);
                        continue;
                    }

                    // Reset EOF counter on successful read
                    _consecutiveEofCount = 0;
                    writer.Advance(bytesRead);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation during session reset
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error receiving data from stream.");
                    break;
                }

                FlushResult result = await writer.FlushAsync(token);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await writer.CompleteAsync();
        }



        private async Task ProcessPipeAsync(PipeReader reader, ChannelWriter<MemoryBlock> writer, CancellationToken token)
        {
            uint currentAddress = 0;

            while (!token.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(token);
                ReadOnlySequence<byte> buffer = result.Buffer;

                if (result.IsCanceled)
                    break;

                SequencePosition consumed = buffer.Start;
                SequencePosition examined = buffer.End;

                if (buffer.Length > 0)
                {
                    // VERBOSE TRACE: Print buffer head to diagnose alignment issues
                    var hexDump = BitConverter.ToString(buffer.Slice(0, Math.Min(buffer.Length, 16)).ToArray());
                    Logger.LogTrace("Buffer state: Length={Len}, Head=[{Hex}]",
                        buffer.Length, hexDump);

                    var seqReader = new SequenceReader<byte>(buffer);
                    bool processed = false;

                    if (_expectGreeting)
                    {
                        // Try to find the greeting in the buffer (skipping junk if necessary)
                        bool foundGreeting = TryConsumeGreeting(ref seqReader);

                        // CRITICAL FIX: Always update 'consumed' to where the reader ended up.
                        // TryConsumeGreeting now consumes junk bytes up to the potential greeting.
                        consumed = seqReader.Position;

                        if (foundGreeting)
                        {
                            _expectGreeting = false; // Greeting consumed, switch to data mode
                            processed = true;

                            Logger.LogInformation("✅ Greeting consumed. Switching to DATA mode.");

                            // Check if we consumed everything or have leftovers
                            if (seqReader.Remaining > 0)
                            {
                                Logger.LogTrace("  Greeting consumed, remaining: {Rem} bytes. Proceeding to data parse.", seqReader.Remaining);

                                // Re-slice to strip the greeting we just ate
                                buffer = buffer.Slice(consumed);

                                // Parse remaining as data protocol
                                ParseProtocol(buffer, ref currentAddress, writer);
                            }
                        }
                        else
                        {
                            // Greeting expected but not found yet.
                            // If we consumed some junk (consumed != start), processed is effectively true
                            if (!consumed.Equals(buffer.Start))
                            {
                                processed = true;
                            }
                            else
                            {
                                processed = false;
                            }
                        }
                    }
                    else
                    {
                        // Data Mode
                        processed = true;
                        consumed = ParseProtocol(buffer, ref currentAddress, writer);
                    }

                    if (!processed && buffer.Length > 0 && buffer.Length < 16)
                    {
                        // Potential STUCK STATE diagnostic
                    }
                }

                reader.AdvanceTo(consumed, examined);

                if (result.IsCompleted)
                {
                    // Check for leftovers on completion
                    if (buffer.Length > 0)
                    {
                        Logger.LogWarning("Pipe completed with {Len} unconsumed bytes remaining.", buffer.Length);
                    }
                    else
                    {
                        Logger.LogInformation("Pipe processing completed cleanly.");
                    }
                    break;
                }
            }

            writer.TryComplete();
            await reader.CompleteAsync();
        }

        /// <summary>
        /// Scans the buffer for the 'Ok' greeting. 
        /// Consumes (skips) any garbage bytes before the greeting.
        /// Returns true if full greeting consumed.
        /// Returns false if greeting not found (reader positioned at start of potential partial match or end).
        /// </summary>
        /// <summary>
        /// Scans the buffer for the 'Ok' greeting. 
        /// Consumes (skips) any garbage bytes before the greeting.
        /// Returns true if full greeting consumed.
        /// Returns false if greeting not found (reader positioned at start of potential partial match or end).
        /// </summary>
        private bool TryConsumeGreeting(ref SequenceReader<byte> reader)
        {
            // We need to look for 0x05 (Framed) or 'O' (Legacy)
            // Strategy: Read byte by byte. If match start, check rest. If fail, continue scanning.

            while (reader.Remaining > 0)
            {
                var originalPosition = reader.Position;
                if (!reader.TryPeek(out byte b))
                    break;

                // Candidate 1: Framed "\x05", "O", "k"
                if (b == 0x05)
                {
                    if (reader.Remaining < 5)
                    {
                        return false; // Potential partial match
                    }

                    var tempReader = reader; // Create a copy to peek ahead
                    tempReader.Advance(1); // Skip 0x05

                    if (tempReader.TryRead(out byte b2) && b2 == 'O' &&
                        tempReader.TryRead(out byte b3) && b3 == 'k')
                    {
                        // Matched! Advance the original reader
                        reader.Advance(6); // 05, O, k, 00, 00, CS
                        Logger.LogInformation("✅ Auto-detected and consumed Framed 'Ok' greeting");
                        return true;
                    }
                }

                // Candidate 2: Legacy "Ok"
                if (b == 'O')
                {
                    if (reader.Remaining < 2)
                    {
                        return false; // Potential partial match
                    }

                    var tempReader = reader; // Create a copy to peek ahead
                    tempReader.Advance(1); // Skip O
                    if (tempReader.TryRead(out byte b2) && b2 == 'k')
                    {
                        // Matched! Advance the original reader
                        reader.Advance(2);
                        Logger.LogInformation("✅ Auto-detected and consumed Legacy 'Ok' greeting");
                        return true;
                    }
                }

                // Not a start of a known greeting, or a failed partial match. Consume as junk.
                reader.Advance(1);
            }

            return false;
        }


        private SequencePosition ParseProtocol(ReadOnlySequence<byte> buffer, ref uint currentAddress, ChannelWriter<MemoryBlock> writer)
        {
            var seqReader = new SequenceReader<byte>(buffer);
            const int BlockSize = 16; // 16 bytes per line
            int blocksProcessed = 0;

            while (seqReader.Remaining >= BlockSize)
            {
                ReadOnlySequence<byte> blockSeq = seqReader.Sequence.Slice(seqReader.Position, BlockSize);

                // Copy to array for UI consumption (crosses thread boundary)
                byte[] data = blockSeq.ToArray();

                var memoryBlock = new MemoryBlock(currentAddress, data);

                if (!writer.TryWrite(memoryBlock))
                {
                    var task = writer.WriteAsync(memoryBlock).AsTask();
                    task.Wait();
                }

                currentAddress += BlockSize;
                seqReader.Advance(BlockSize);
                blocksProcessed++;
            }

            if (blocksProcessed > 0)
            {
                Logger.LogTrace("Parsed {Count} data blocks ({Bytes} bytes). New Addr: 0x{Addr:X}",
                    blocksProcessed, blocksProcessed * BlockSize, currentAddress);
            }

            return seqReader.Position;
        }

        public async Task WriteAsync(byte[] data, CancellationToken token)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Dumper service is not connected.");
            }

            try
            {
                await _stream.WriteAsync(data, token).ConfigureAwait(false);
                await _stream.FlushAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error sending data through dumper stream.");
                throw;
            }
        }

        public Task StopAsync()
        {
            _cts?.Cancel();
            if (!_isExternalStream && _stream != null)
            {
                try
                {
                    _stream.Dispose();
                }
                catch { }
            }
            _stream = null;
            _socket = null;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            if (!_isExternalStream)
            {
                _stream?.Dispose();
                _socket?.Dispose();
            }
        }
    }
}
