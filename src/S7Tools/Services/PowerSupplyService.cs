using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NModbus;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for power supply control operations using NModbus TCP protocol.
/// Provides configuration-based connection management and power control capabilities.
/// </summary>
public class PowerSupplyService : IPowerSupplyService, IDisposable
{
    private readonly ILogger<PowerSupplyService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private TcpClient? _tcpClient;
    private IModbusMaster? _modbusMaster;
    private PowerSupplyConfiguration? _currentConfiguration;
    private bool _isConnected;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the PowerSupplyService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public PowerSupplyService(ILogger<PowerSupplyService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("PowerSupplyService initialized");
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public PowerSupplyConfiguration? CurrentConfiguration => _currentConfiguration;

    #region Connection Management

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(PowerSupplyConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isConnected)
            {
                // If already connected with the same configuration, treat as success (idempotent connect)
                if (_currentConfiguration is ModbusTcpConfiguration cur && configuration is ModbusTcpConfiguration req &&
                    string.Equals(cur.Host, req.Host, StringComparison.OrdinalIgnoreCase) &&
                    cur.Port == req.Port && cur.DeviceId == req.DeviceId &&
                    cur.OnOffCoil == req.OnOffCoil && cur.AddressingMode == req.AddressingMode)
                {
                    _logger.LogDebug("ConnectAsync called while already connected with identical configuration. Returning success.");
                    return true;
                }

                throw new InvalidOperationException("Already connected to a power supply device. Disconnect first.");
            }

            // Validate configuration
            List<string> validationErrors = configuration.Validate();
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", validationErrors)}");
            }

            // Currently only Modbus TCP is supported
            if (configuration is not ModbusTcpConfiguration modbusTcpConfig)
            {
                throw new NotSupportedException($"Power supply type {configuration.Type} is not yet supported. Currently only Modbus TCP is supported.");
            }

            _logger.LogInformation("Connecting to Modbus TCP power supply at {Host}:{Port}",
                modbusTcpConfig.Host, modbusTcpConfig.Port);

            try
            {
                // Create TCP client
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(modbusTcpConfig.Host, modbusTcpConfig.Port, cancellationToken).ConfigureAwait(false);

                // Configure timeouts
                _tcpClient.ReceiveTimeout = modbusTcpConfig.ReadTimeoutMs;
                _tcpClient.SendTimeout = modbusTcpConfig.WriteTimeoutMs;

                // Create Modbus master
                var factory = new ModbusFactory();
                _modbusMaster = factory.CreateMaster(_tcpClient);

                _currentConfiguration = configuration;
                _isConnected = true;

                _logger.LogInformation("Successfully connected to Modbus TCP power supply");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Modbus TCP power supply at {Host}:{Port}",
                    modbusTcpConfig.Host, modbusTcpConfig.Port);

                // Cleanup on failure
                CleanupConnection();
                throw new InvalidOperationException($"Failed to connect to power supply: {ex.Message}", ex);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_isConnected)
            {
                _logger.LogDebug("Already disconnected from power supply");
                return;
            }

            _logger.LogInformation("Disconnecting from power supply");
            CleanupConnection();
            _logger.LogInformation("Disconnected from power supply");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(PowerSupplyConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Validate configuration
        List<string> validationErrors = configuration.Validate();
        if (validationErrors.Count > 0)
        {
            _logger.LogWarning("Configuration validation failed: {Errors}", string.Join(", ", validationErrors));
            return false;
        }

        // Currently only Modbus TCP is supported
        if (configuration is not ModbusTcpConfiguration modbusTcpConfig)
        {
            _logger.LogWarning("Power supply type {Type} is not yet supported for testing", configuration.Type);
            return false;
        }

        _logger.LogInformation("Testing connection to Modbus TCP power supply at {Host}:{Port}",
            modbusTcpConfig.Host, modbusTcpConfig.Port);

        TcpClient? testClient = null;
        IModbusMaster? testMaster = null;

        try
        {
            // Create temporary TCP client
            testClient = new TcpClient();
            await testClient.ConnectAsync(modbusTcpConfig.Host, modbusTcpConfig.Port, cancellationToken).ConfigureAwait(false);

            testClient.ReceiveTimeout = modbusTcpConfig.ReadTimeoutMs;
            testClient.SendTimeout = modbusTcpConfig.WriteTimeoutMs;

            // Create temporary Modbus master
            var factory = new ModbusFactory();
            testMaster = factory.CreateMaster(testClient);

            // Try to read the coil to verify communication
            ushort coilAddress = modbusTcpConfig.ConvertToProtocolAddress(modbusTcpConfig.OnOffCoil);
            await testMaster.ReadCoilsAsync(modbusTcpConfig.DeviceId, coilAddress, 1).ConfigureAwait(false);

            _logger.LogInformation("Connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed: {Message}", ex.Message);
            return false;
        }
        finally
        {
            testMaster?.Dispose();
            testClient?.Close();
            testClient?.Dispose();
        }
    }

    #endregion

    #region Power Control Operations

    /// <inheritdoc />
    public async Task<bool> TurnOnAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected();

            ModbusTcpConfiguration modbusTcpConfig = (_currentConfiguration as ModbusTcpConfiguration)!;
            ushort coilAddress = modbusTcpConfig.ConvertToProtocolAddress(modbusTcpConfig.OnOffCoil);

            _logger.LogInformation("Turning power ON (Device: {DeviceId}, Coil: {Coil}, Mode: {Mode})",
                modbusTcpConfig.DeviceId, modbusTcpConfig.OnOffCoil, modbusTcpConfig.AddressingMode);

            await _modbusMaster!.WriteSingleCoilAsync(modbusTcpConfig.DeviceId, coilAddress, true).ConfigureAwait(false);

            _logger.LogInformation("Power turned ON successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to turn power ON: {Message}", ex.Message);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TurnOffAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected();

            ModbusTcpConfiguration modbusTcpConfig = (_currentConfiguration as ModbusTcpConfiguration)!;
            ushort coilAddress = modbusTcpConfig.ConvertToProtocolAddress(modbusTcpConfig.OnOffCoil);

            _logger.LogInformation("Turning power OFF (Device: {DeviceId}, Coil: {Coil}, Mode: {Mode})",
                modbusTcpConfig.DeviceId, modbusTcpConfig.OnOffCoil, modbusTcpConfig.AddressingMode);

            await _modbusMaster!.WriteSingleCoilAsync(modbusTcpConfig.DeviceId, coilAddress, false).ConfigureAwait(false);

            _logger.LogInformation("Power turned OFF successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to turn power OFF: {Message}", ex.Message);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReadPowerStateAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected();

            ModbusTcpConfiguration modbusTcpConfig = (_currentConfiguration as ModbusTcpConfiguration)!;
            ushort coilAddress = modbusTcpConfig.ConvertToProtocolAddress(modbusTcpConfig.OnOffCoil);

            _logger.LogDebug("Reading power state (Device: {DeviceId}, Coil: {Coil})",
                modbusTcpConfig.DeviceId, coilAddress);

            bool[] coils = await _modbusMaster!.ReadCoilsAsync(modbusTcpConfig.DeviceId, coilAddress, 1).ConfigureAwait(false);
            bool state = coils[0];

            _logger.LogDebug("Power state read: {State}", state ? "ON" : "OFF");
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read power state: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to read power state: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> PowerCycleAsync(int delayMs = 5000, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected();

            _logger.LogInformation("Starting power cycle with {Delay}ms delay", delayMs);

            // Turn off
            bool offResult = await TurnOffWithoutLockAsync(cancellationToken).ConfigureAwait(false);
            if (!offResult)
            {
                _logger.LogError("Power cycle failed: Could not turn power OFF");
                return false;
            }

            // Wait
            _logger.LogDebug("Waiting {Delay}ms before turning power back ON", delayMs);
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

            // Turn on
            bool onResult = await TurnOnWithoutLockAsync(cancellationToken).ConfigureAwait(false);
            if (!onResult)
            {
                _logger.LogError("Power cycle failed: Could not turn power ON");
                return false;
            }

            _logger.LogInformation("Power cycle completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Power cycle failed: {Message}", ex.Message);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion


    #region Private Helper Methods

    private void EnsureConnected()
    {
        if (!_isConnected || _modbusMaster == null || _tcpClient == null)
        {
            throw new InvalidOperationException("Not connected to power supply. Call ConnectAsync() first.");
        }
    }

    private async Task<bool> TurnOnWithoutLockAsync(CancellationToken cancellationToken)
    {
        // This method is called from PowerCycleAsync which already holds the lock
        try
        {
            ModbusTcpConfiguration modbusTcpConfig = (_currentConfiguration as ModbusTcpConfiguration)!;
            ushort coilAddress = modbusTcpConfig.ConvertToProtocolAddress(modbusTcpConfig.OnOffCoil);
            await _modbusMaster!.WriteSingleCoilAsync(modbusTcpConfig.DeviceId, coilAddress, true).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to turn power ON: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> TurnOffWithoutLockAsync(CancellationToken cancellationToken)
    {
        // This method is called from PowerCycleAsync which already holds the lock
        try
        {
            ModbusTcpConfiguration modbusTcpConfig = (_currentConfiguration as ModbusTcpConfiguration)!;
            ushort coilAddress = modbusTcpConfig.ConvertToProtocolAddress(modbusTcpConfig.OnOffCoil);
            await _modbusMaster!.WriteSingleCoilAsync(modbusTcpConfig.DeviceId, coilAddress, false).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to turn power OFF: {Message}", ex.Message);
            return false;
        }
    }

    private void CleanupConnection()
    {
        _isConnected = false;
        _currentConfiguration = null;

        _modbusMaster?.Dispose();
        _modbusMaster = null;

        _tcpClient?.Close();
        _tcpClient?.Dispose();
        _tcpClient = null;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases all resources used by the PowerSupplyService.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs the actual resource cleanup.
    /// </summary>
    /// <param name="disposing">True to dispose managed resources; false to release unmanaged resources only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Free managed resources
            CleanupConnection();
            _semaphore.Dispose();
        }

        // Free unmanaged resources (none currently)

        _disposed = true;
    }

    #endregion
}
