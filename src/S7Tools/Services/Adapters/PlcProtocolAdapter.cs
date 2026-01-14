using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Adapters
{
    /// <summary>
    /// Real implementation of PlcProtocolAdapter.
    /// Combines logic from Reference Project's PlcProtocol.cs (Framing/Checksum) 
    /// and PlcProtocolHandler.cs (Handshake/High-Level Send).
    /// </summary>
    public sealed class PlcProtocolAdapter : IPlcProtocol
    {
        private readonly ILogger<PlcProtocolAdapter> _logger;
        private readonly IPlcTransport _transport;

        public PlcProtocolAdapter(IPlcTransport transport, ILogger<PlcProtocolAdapter> logger)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool DataAvailable => _transport.DataAvailable;

        public void Configure(string host, int port)
        {
            _transport.Configure(host, port);
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await _transport.ConnectAsync(cancellationToken);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            await _transport.DisconnectAsync(cancellationToken);
        }

        #region Protocol Utils (Encoding/Decoding)

        private static byte CalculateChecksum(byte[] packetData, int offset, int length)
        {
            int sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += packetData[offset + i];
            }
            return (byte)-sum;
        }

        public static byte[] EncodePacket(byte[] contents)
        {
            if (contents.Length > 254)
            {
                throw new ArgumentException("Packet contents too large. Max size is 254 bytes.", nameof(contents));
            }

            var packet = new byte[contents.Length + 2];
            packet[0] = (byte)(contents.Length + 1);
            Array.Copy(contents, 0, packet, 1, contents.Length);
            packet[packet.Length - 1] = CalculateChecksum(packet, 0, packet.Length - 1);
            return packet;
        }

        private static byte[] DecodePacket(byte[] packet)
        {
            if (packet.Length < 2)
            {
                throw new ArgumentException("Invalid packet length.");
            }

            var lengthByte = packet[0];
            if (lengthByte != packet.Length - 1)
            {
                throw new ArgumentException("Packet length mismatch.");
            }

            byte receivedChecksum = packet.Last();
            byte calculatedChecksum = CalculateChecksum(packet, 0, packet.Length - 1);

            if (receivedChecksum != calculatedChecksum)
            {
                throw new Exception("ChecksumMismatchException"); // Using general exception to avoid dependency hell
            }

            var contents = new byte[lengthByte - 1];
            Array.Copy(packet, 1, contents, 0, contents.Length);
            return contents;
        }

        #endregion

        public async Task SendPacketAsync(byte[] payload, int? maxChunk = 2, CancellationToken cancellationToken = default)
        {
            // Safety delay exactly as in reference
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            var packet = EncodePacket(payload);
            _logger.LogTrace("-> SEND: {Hex}", BitConverter.ToString(packet).Replace("-", ""));

            int step = maxChunk ?? 2;
            int sleepMs = 10;

            for (int i = 0; i < packet.Length; i += step)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesToSend = Math.Min(step, packet.Length - i);
                await _transport.WriteAsync(packet, i, bytesToSend, cancellationToken).ConfigureAwait(false);
                if (sleepMs > 0)
                {
                    await Task.Delay(sleepMs, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<byte[]> ReceivePacketAsync(CancellationToken cancellationToken = default)
        {
            var lengthByte = new byte[1];
            int lengthBytesRead = await _transport.ReadAsync(lengthByte, 0, 1, cancellationToken).ConfigureAwait(false);

            if (lengthBytesRead == 0)
            {
                throw new InvalidOperationException("Transport stream reached EOF while reading packet length");
            }

            int bytesToRead = lengthByte[0];

            if (bytesToRead == 0)
            {
                return Array.Empty<byte>();
            }

            var fullPacket = new byte[bytesToRead + 1];
            fullPacket[0] = lengthByte[0];

            int bytesRead = 0;
            while (bytesRead < bytesToRead)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int currentBytesRead = await _transport.ReadAsync(fullPacket, 1 + bytesRead, bytesToRead - bytesRead, cancellationToken).ConfigureAwait(false);

                if (currentBytesRead == 0)
                {
                    throw new InvalidOperationException($"Transport stream reached EOF while reading packet data. Expected {bytesToRead} bytes, got {bytesRead} bytes");
                }

                bytesRead += currentBytesRead;
            }

            _logger.LogTrace("<- RECV: {Hex}", BitConverter.ToString(fullPacket).Replace("-", ""));
            return DecodePacket(fullPacket);
        }

        public async Task RawWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            await _transport.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public async Task<int> RawReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            return await _transport.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public Stream? GetStream() => _transport.GetStream();
    }
}
