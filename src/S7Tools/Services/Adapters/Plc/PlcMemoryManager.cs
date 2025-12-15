using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Services.Adapters.Plc
{
    internal class PlcMemoryManager
    {
        private readonly PlcProtocolHandler _protocol;

        public PlcMemoryManager(PlcProtocolHandler protocol)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
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

        public async Task<byte[]> DumpMemoryAsync(uint address, uint length, byte[] dumpPayload, PlcStagerManager stager, IProgress<long> progress, CancellationToken cancellationToken)
        {
            // 1. Install Dumper using Stager (Hook 7 installs to Hook 2)
            await stager.InstallAddHookViaStagerAsync(PlcConstants.DUMPER_PAYLOAD_LOCATION, dumpPayload, PlcConstants.DEFAULT_SECOND_ADD_HOOK_IND, cancellationToken);

            // 2. Invoke Dumper (Hook 2)
            // Protocol: 'A' + Addr + Len
            var args = new byte[9];
            args[0] = (byte)'A';
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(address), 0, args, 1, 4);
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(length), 0, args, 5, 4);

            // Send Command
            var response = await _protocol.InvokeAddHookAsync(PlcConstants.DEFAULT_SECOND_ADD_HOOK_IND, args, true, cancellationToken);

            if (response == null || !System.Text.Encoding.ASCII.GetString(response).StartsWith("Ok"))
            {
                throw new Exception("Dumper invocation failed.");
            }

            // 3. Receive Data with progress tracking
            var data = await ReceiveManyAsync(length, progress, cancellationToken);
            return data;
        }

        public async Task<byte[]> ReceiveManyAsync(uint expectedLength, IProgress<long> progress, CancellationToken cancellationToken)
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
