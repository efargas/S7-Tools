using System;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Adapters.Plc
{
    internal class PlcProtocolHandler(IPlcProtocol protocol)
    {
        private readonly IPlcProtocol _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await _protocol.DisconnectAsync(cancellationToken);
        }

        public Stream? GetStream() => _protocol.GetStream();

        public async Task<byte[]?> InvokePrimaryHandlerAsync(byte handlerIndex, byte[] args, bool awaitResponse, CancellationToken cancellationToken)
        {
            byte[] payload = new byte[1 + args.Length];
            payload[0] = handlerIndex;
            Array.Copy(args, 0, payload, 1, args.Length);
            await _protocol.SendPacketAsync(payload, cancellationToken: cancellationToken);

            if (!awaitResponse)
            {
                return null;
            }
            return await _protocol.ReceivePacketAsync(cancellationToken);
        }

        public async Task<byte[]?> InvokeAddHookAsync(int hookNo, byte[] args, bool awaitResponse, CancellationToken cancellationToken)
        {
            byte[] payload = new byte[1 + args.Length];
            payload[0] = (byte)hookNo;
            Array.Copy(args, 0, payload, 1, args.Length);
            return await InvokePrimaryHandlerAsync(0x1C, payload, awaitResponse, cancellationToken);
        }

        public async Task SendPacketAsync(byte[] payload, int? maxChunk, CancellationToken cancellationToken)
        {
            await _protocol.SendPacketAsync(payload, maxChunk, cancellationToken);
        }

        public async Task<byte[]> ReceivePacketAsync(CancellationToken cancellationToken)
        {
            return await _protocol.ReceivePacketAsync(cancellationToken);
        }

        public async Task RawWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _protocol.RawWriteAsync(buffer, offset, count, cancellationToken);
        }

        public async Task<int> RawReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await _protocol.RawReadAsync(buffer, offset, count, cancellationToken);
        }

        public bool DataAvailable => _protocol.DataAvailable;

        public async Task PerformHandshakeAsync(CancellationToken cancellationToken)
        {
            const int MaxHandshakeAttempts = 50; // Maximum attempts before giving up

            byte[] magic = System.Text.Encoding.ASCII.GetBytes("MFGT1");
            byte[] padding = System.Text.Encoding.ASCII.GetBytes("AAAA");
            byte[] handshakePayload = new byte[padding.Length + magic.Length];
            Array.Copy(padding, 0, handshakePayload, 0, padding.Length);
            Array.Copy(magic, 0, handshakePayload, padding.Length, magic.Length);

            for (int attempt = 0; attempt < MaxHandshakeAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _protocol.RawWriteAsync(handshakePayload, 0, handshakePayload.Length, cancellationToken);

                // Wait for response
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var responseBuffer = new System.Collections.Generic.List<byte>();
                while (sw.ElapsedMilliseconds < 300)
                {
                    if (_protocol.DataAvailable)
                    {
                        byte[] tmpBuf = new byte[1024];
                        int bytesRead = await _protocol.RawReadAsync(tmpBuf, 0, tmpBuf.Length, cancellationToken);
                        if (bytesRead > 0)
                        {
                            responseBuffer.AddRange(System.Linq.Enumerable.Take(tmpBuf, bytesRead));
                            string ascii = System.Text.Encoding.ASCII.GetString([.. responseBuffer]);
                            if (ascii.Contains("-CPU"))
                            {
                                return;
                            }
                        }
                    }
                    await Task.Delay(50, cancellationToken);
                }
                await Task.Delay(10, cancellationToken);
            }
            throw new Exception($"Handshake failed after {MaxHandshakeAttempts} attempts");
        }

        public async Task<byte[]> GetVersionAsync(CancellationToken cancellationToken)
        {
            // Handler 0x00 = Get Info
            byte[]? response = await InvokePrimaryHandlerAsync(0x00, [], true, cancellationToken);
            return response ?? [];
        }
    }
}
