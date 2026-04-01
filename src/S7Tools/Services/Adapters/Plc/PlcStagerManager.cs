namespace S7Tools.Services.Adapters.Plc
{
    /// <summary>
    /// Represents the PlcStagerManager.
    /// </summary>
    internal class PlcStagerManager
    {
        private readonly PlcProtocolHandler _protocol;
        private readonly PlcMemoryManager _memory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlcStagerManager"/> class.
        /// </summary>
        public PlcStagerManager(PlcProtocolHandler protocol, PlcMemoryManager memory)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        /// <summary>
        /// Executes the InstallStagerAsync operation.
        /// </summary>
        public async Task InstallStagerAsync(byte[] stager, CancellationToken cancellationToken)
        {
            // 1. Write Stager Code to IRAM
            await _memory.WriteToIramAsync(PlcConstants.IRAM_STAGER_START, stager, cancellationToken).ConfigureAwait(false);

            // 2. Overwrite Hook Entry
            // Hook Entry Structure: [Unknown:2] [ArgCheck:2] [Address:4]
            // We write at Offset + 2 to skip Unknown.
            // Payload: [0x00, 0xFF] (Disable Arg Check) + [Address (Big Endian)]
            byte[] hookPayload = new byte[6];
            hookPayload[0] = 0x00;
            hookPayload[1] = 0xFF;
            byte[] addrBytes = PlcInternalHelpers.GetBigEndianBytes(PlcConstants.IRAM_STAGER_START);
            Array.Copy(addrBytes, 0, hookPayload, 2, 4);

            uint hookEntryAddr = PlcConstants.ADD_HOOK_TABLE_START + (8 * PlcConstants.DEFAULT_STAGER_ADDHOOK_IND) + 2;
            await _memory.WriteToIramAsync(hookEntryAddr, hookPayload, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the InstallAddHookViaStagerAsync operation.
        /// </summary>
        public async Task InstallAddHookViaStagerAsync(uint targetAddr, byte[] payload, int hookNo, CancellationToken cancellationToken)
        {
            // 1. Write Hook Entry
            // Ref StagerManager L174
            byte[] hookEntry = new byte[8];
            hookEntry[3] = 0xFF; // Variable
            Array.Copy(PlcInternalHelpers.GetBigEndianBytes(targetAddr), 0, hookEntry, 4, 4); // Addr at end

            uint tableAddr = PlcConstants.ADD_HOOK_TABLE_START + (uint)(8 * hookNo);

            // WRITE VIA STAGER (Hook 7)
            await WriteViaStagerAsync(tableAddr, hookEntry, cancellationToken).ConfigureAwait(false);

            // 2. Write Code
            await WriteViaStagerAsync(targetAddr, payload, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the WriteViaStagerAsync operation.
        /// </summary>
        public async Task WriteViaStagerAsync(uint address, byte[] data, CancellationToken cancellationToken)
        {
            // Ref StagerManager L157
            // Invoke Hook 7 with Address -> Then Send Data
            await _protocol.InvokeAddHookAsync(PlcConstants.DEFAULT_STAGER_ADDHOOK_IND, PlcInternalHelpers.GetBigEndianBytes(address), false, cancellationToken).ConfigureAwait(false);
            await SendFullMsgViaStagerAsync(data, cancellationToken).ConfigureAwait(false);
        }

        private async Task SendFullMsgViaStagerAsync(byte[] msg, CancellationToken cancellationToken)
        {
            // Stager XOR Protocol
            int maxChunk = PlcConstants.MAX_MSG_LEN;
            for (int i = 0; i < msg.Length; i += maxChunk)
            {
                int size = Math.Min(maxChunk, msg.Length - i);
                byte[] chunk = msg.Skip(i).Take(size).ToArray();
                byte[] encoded = PlcInternalHelpers.EncodeWithXor(chunk);

                await _protocol.SendPacketAsync(encoded, 8, cancellationToken: cancellationToken).ConfigureAwait(false);
                // Ack
                byte[] ack = await _protocol.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
                if (ack == null || ack.Length != 1)
                {
                    throw new Exception("Stager ACK fail");
                }
            }
            // End Packet
            await _protocol.SendPacketAsync(PlcInternalHelpers.EncodeWithXor(Array.Empty<byte>()), null, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _protocol.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
