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
        private readonly Pipe _pipe;
        private CancellationTokenSource? _cts;
        private Channel<MemoryBlock>? _outputChannel;
        private bool _isExternalStream;

        public DumperService(ILogger<DumperService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize Pipe with default options (uses shared MemoryPool)
            _pipe = new Pipe();
        }

        // Expose the reader for the consumer
        public ChannelReader<MemoryBlock> DataReader => _outputChannel?.Reader ?? throw new InvalidOperationException("Dumper session not started");

        public async Task StartDumpingAsync(string host, int port, CancellationToken token, Stream? existingStream = null)
        {
            // Ensure any previous session is stopped and state cleared
            await StopAsync();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _isExternalStream = existingStream != null;

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
                    _logger.LogInformation("Using existing stream for dumper session.");
                    _stream = existingStream;
                }
                else
                {
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _logger.LogInformation("Connecting to socat at {Host}:{Port}...", host, port);
                    await _socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), port), _cts.Token);
                    _stream = new NetworkStream(_socket, ownsSocket: true);
                }

                _logger.LogInformation("Connection established. Starting ingestion pipeline.");

                var fillTask = FillPipeAsync(_stream, _pipe.Writer, _cts.Token);
                var readTask = ProcessPipeAsync(_pipe.Reader, _outputChannel.Writer, _cts.Token);

                await Task.WhenAll(fillTask, readTask);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Dump operation canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during memory dump.");
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
                }
                _stream = null;
                _socket = null;
            }
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
                        break; // EOF
                    }

                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving data from stream.");
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

        private volatile bool _expectingGreeting;

        public void ExpectGreeting()
        {
            _expectingGreeting = true;
        }

        private async Task ProcessPipeAsync(PipeReader reader, ChannelWriter<MemoryBlock> writer, CancellationToken token)
        {
            uint currentAddress = 0;
            SequencePosition consumed = default;
            SequencePosition examined = default;

            while (!token.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(token);
                ReadOnlySequence<byte> buffer = result.Buffer;

                if (buffer.Length == 0 && result.IsCompleted)
                    break;

                consumed = buffer.Start;
                examined = buffer.End;

                if (_expectingGreeting)
                {
                    if (TrySkipGreeting(buffer, out long skipBytes))
                    {
                        _logger.LogInformation("Found and skipping {Count} bytes of sync greeting.", skipBytes);
                        _expectingGreeting = false;

                        consumed = buffer.GetPosition(skipBytes);
                        buffer = buffer.Slice(skipBytes);

                        if (buffer.Length == 0)
                        {
                            reader.AdvanceTo(consumed, examined);
                            continue;
                        }
                    }
                    else
                    {
                        // Not found yet. Keep enough to not miss split greeting (at least 2 bytes if present)
                        long keep = Math.Min(buffer.Length, 2);
                        consumed = buffer.GetPosition(buffer.Length - keep);
                        reader.AdvanceTo(consumed, examined);
                        continue;
                    }
                }

                consumed = ParseProtocol(buffer, ref currentAddress, writer);
                reader.AdvanceTo(consumed, examined);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.TryComplete();
            await reader.CompleteAsync();
        }

        private bool TrySkipGreeting(ReadOnlySequence<byte> buffer, out long skipBytes)
        {
            var seqReader = new SequenceReader<byte>(buffer);
            skipBytes = 0;

            while (!seqReader.End)
            {
                if (seqReader.TryRead(out byte b) && b == 0x4F) // 'O'
                {
                    if (seqReader.Remaining >= 1)
                    {
                        if (seqReader.TryRead(out byte b2) && b2 == 0x6B) // 'k'
                        {
                            long posO = seqReader.Consumed - 2;

                            // Determine skip size: 6 for framed [05 Ok 00 00 CS], 3 or 4 for raw
                            // We check if the byte before 'O' is 0x05 (length byte of framed packet)
                            bool isFramed = false;
                            if (posO >= 1)
                            {
                                var slice = buffer.Slice(posO - 1, 1);
                                if (slice.FirstSpan[0] == 0x05)
                                    isFramed = true;
                            }

                            if (isFramed)
                                skipBytes = posO + 5; // Skip up to CS (inclusive) -> 6 bytes total from 05
                            else
                                skipBytes = posO + 3; // Skip Ok and likely one null byte

                            return true;
                        }
                    }
                }
            }

            return false;
        }


        private SequencePosition ParseProtocol(ReadOnlySequence<byte> buffer, ref uint currentAddress, ChannelWriter<MemoryBlock> writer)
        {
            var seqReader = new SequenceReader<byte>(buffer);
            const int BlockSize = 16; // 16 bytes per line

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
                _logger.LogError(ex, "Error sending data through dumper stream.");
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
