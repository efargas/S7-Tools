using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Services.Adapters
{
    /// <summary>
    /// Real implementation of PlcTransportAdapter using TcpClient.
    /// Wraps socket communication logic instead of referencing an external Transport implementation.
    /// </summary>
    public sealed class PlcTransportAdapter : IPlcTransport
    {
        private readonly ILogger<PlcTransportAdapter> _logger;
        private TcpClient _client;
        private NetworkStream? _stream;
        private string _host = string.Empty;
        private int _port;

        public PlcTransportAdapter(ILogger<PlcTransportAdapter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = new TcpClient();
        }

        public bool IsConnected => _client?.Connected ?? false;

        public bool DataAvailable => _stream?.DataAvailable ?? false;

        public void Configure(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_host) || _port == 0)
            {
                throw new InvalidOperationException("Transport not configured. Call Configure() first.");
            }

            if (_client?.Connected == true)
            {
                return;
            }

            _logger.LogInformation("Connecting to PLC via Socat at {Host}:{Port}...", _host, _port);

            // Dispose previous stream and client; always use a fresh TcpClient for each connection attempt
            _stream?.Dispose();
            _stream = null;
            _client?.Dispose();
            _client = new TcpClient();

            try
            {
                _client.NoDelay = true;
                await _client.ConnectAsync(_host, _port, cancellationToken);
                _logger.LogInformation("TcpClient.NoDelay set to: {Value}", _client.NoDelay);
                _stream = _client.GetStream();
                _logger.LogInformation("Connected successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to PLC transport.");
                throw;
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Disconnecting transport...");
            _stream?.Dispose();
            _stream = null;
            _client?.Dispose();
            _client = new TcpClient(); // Reset for next use
            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Transport not connected.");
            }
            return await _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Transport not connected.");
            }
            await _stream.WriteAsync(buffer, offset, count, cancellationToken);
            // await _stream.FlushAsync(cancellationToken); // Removed to prevent packet fragmentation logic interference
        }

        public Stream? GetStream() => _stream;

        public ValueTask DisposeAsync()
        {
            _stream?.Dispose();
            _client?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
