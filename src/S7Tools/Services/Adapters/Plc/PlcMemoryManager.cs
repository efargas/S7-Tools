using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace S7Tools.Services.Adapters.Plc
{
    internal class PlcMemoryManager
    {
        private readonly PlcProtocolHandler _protocol;
        private readonly MemoryDumpOrchestrator _orchestrator;
        private readonly Microsoft.Extensions.Logging.ILogger<PlcMemoryManager> _logger;

        public PlcMemoryManager(
            PlcProtocolHandler protocol,
            MemoryDumpOrchestrator orchestrator,
            Microsoft.Extensions.Logging.ILogger<PlcMemoryManager> logger)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task WriteToIramAsync(uint address, byte[] data, CancellationToken cancellationToken)
        {
            // Enter Subprotocol 0x80 (IRAM Mode)
            // Payload: [Magic for IRAM (Big Endian)]
            var magicBytes = BitConverter.GetBytes(PlcConstants.SUBPROT_80_MODE_MAGICS[PlcConstants.SUBPROT_80_MODE_IRAM]);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(magicBytes);
            }

            await _protocol.InvokePrimaryHandlerAsync(0x80, magicBytes, true, cancellationToken);

            // Write Chunks
            int chunkSize = 16;
            for (int i = 0; i < data.Length; i += chunkSize)
            {
                int size = Math.Min(chunkSize, data.Length - i);
                var chunk = new byte[size];
                Array.Copy(data, i, chunk, 0, size);
                uint targetArg = address + (uint)i - 0x10000000; // Voodoo from Ref MemoryManager L200

                // 1. Mask (FF)
                await RawSubprotocolWriteAsync(targetArg, Enumerable.Repeat((byte)0xFF, size).ToArray(), cancellationToken);
                // 2. Write
                await RawSubprotocolWriteAsync(targetArg, chunk, cancellationToken);
            }

            // Leave Subprot
            await _protocol.SendPacketAsync(new byte[] { 0x81, 0xD0, 0x67 }, null, cancellationToken: cancellationToken);
            await _protocol.ReceivePacketAsync(cancellationToken);
        }

        private async Task RawSubprotocolWriteAsync(uint address, byte[] data, CancellationToken cancellationToken)
        {
            // Ref MemoryManager L173
            // Payload: [0x84, 0x5a, 0x2e] + [Addr(4)] + [Data]
            var payload = new byte[7 + data.Length];
            payload[0] = 0x84;
            payload[1] = 0x5a;
            payload[2] = 0x2e;
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(address), 0, payload, 3, 4);
            Array.Copy(data, 0, payload, 7, data.Length);

            await _protocol.SendPacketAsync(payload, null, cancellationToken: cancellationToken);
            await _protocol.ReceivePacketAsync(cancellationToken);
        }

        public async Task<byte[]> InvokeDumperAsync(uint address, uint length, IProgress<long> progress, CancellationToken cancellationToken)
        {
            // Protocol: 'A' + Addr + Len
            var args = new byte[9];
            args[0] = (byte)'A';
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(address), 0, args, 1, 4);
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(length), 0, args, 5, 4);

            // Send Command (Invoke Hook 2)
            var response = await _protocol.InvokeAddHookAsync(PlcConstants.DEFAULT_SECOND_ADD_HOOK_IND, args, true, cancellationToken);

            if (response == null || !System.Text.Encoding.ASCII.GetString(response).StartsWith("Ok"))
            {
                throw new Exception("Dumper invocation failed.");
            }

            // Receive Data
            var data = await ReceiveManyAsync(progress, cancellationToken);
            return data;
        }

        /// <summary>
        /// Invokes the dumper with streaming using high-performance DumperService and Orchestrator.
        /// Data is streamed incrementally via callback while also being dispatched to UI for real-time visualization.
        /// </summary>
        public async Task InvokeDumperStreamAsync(
            uint address,
            uint length,
            Func<ReadOnlyMemory<byte>, ValueTask> dataCallback,
            IProgress<long> progress,
            string? socatHost,
            int socatPort,
            CancellationToken cancellationToken)
        {
            // Configure orchestrator
            _orchestrator.Configure(socatHost ?? "127.0.0.1", socatPort);

            _logger.LogInformation("Preparing to invoke dumper for address 0x{Address:X8}, length {Length} bytes", address, length);

            // Protocol: 'A' + Addr + Len
            var args = new byte[9];
            args[0] = (byte)'A';
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(address), 0, args, 1, 4);
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(length), 0, args, 5, 4);

            long totalReceived = 0;

            try
            {
                // We share the existing session to keep socat ALIVE
                Stream? currentStream = _protocol.GetStream();
                _logger.LogDebug("Sharing existing protocol stream with dumper session.");

                // Use the orchestrator to send the command AND handle the streaming
                await _orchestrator.InvokeDumpCommandAsync(
                    (byte)PlcConstants.DEFAULT_SECOND_ADD_HOOK_IND,
                    args,
                    address,
                    length,
                    async block =>
                    {
                        await dataCallback(block.Data).ConfigureAwait(false);
                        totalReceived += block.Data.Length;
                        progress?.Report(totalReceived);
                    },
                    cancellationToken,
                    currentStream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dumper streaming operation failed.");
                throw;
            }
            finally
            {
                _logger.LogInformation("Dumper streaming session completed/terminated for segment 0x{Address:X8}", address);
            }

            if (totalReceived < length)
            {
                throw new Exception($"Dump incomplete for segment 0x{address:X8}. Received {totalReceived} of {length} bytes.");
            }
        }

        public async Task<byte[]> ReceiveManyAsync(IProgress<long> progress, CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();
            while (true)
            {
                var chunk = await _protocol.ReceivePacketAsync(cancellationToken);
                if (chunk == null || chunk.Length == 0)
                {
                    break;
                }
                await ms.WriteAsync(chunk, 0, chunk.Length);
                progress?.Report(ms.Length);
            }
            return ms.ToArray();
        }
    }
}
