using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;

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
                throw new InvalidOperationException("Transport not configured. Call Configure() first.");

            _logger.LogInformation("Connecting to PLC via Socat at {Host}:{Port}...", _host, _port);

            // Re-create TcpClient if disposed or previously used
            if (_client == null || _client.Client == null || !_client.Connected && _client.Client.Connected)
            {
                _client?.Dispose();
                _client = new TcpClient();
            }
            // Handle case where client is already connected or in weird state
            if (_client.Connected)
                return;

            try
            {
                await _client.ConnectAsync(_host, _port, cancellationToken);
                _stream = _client.GetStream();
                _logger.LogInformation("Connected successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to PLC transport.");
                throw;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Disconnecting transport...");
            _stream?.Close();
            _client?.Close();
            _client = new TcpClient(); // Reset for next use
            await Task.CompletedTask;
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
                throw new InvalidOperationException("Transport not connected.");
            return await _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
                throw new InvalidOperationException("Transport not connected.");
            await _stream.WriteAsync(buffer, offset, count, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            _stream?.Dispose();
            _client?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
