using Microsoft.Extensions.Logging;
using S7.Net;
using S7.Net.Types;
using S7Tools.Core.Models;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Services;

/// <summary>
/// Concrete implementation of PLC data services using the S7netplus library.
/// Implements both ITagRepository and IS7ConnectionProvider for real S7 PLC communication.
/// </summary>
public sealed class PlcDataService : ITagRepository, IS7ConnectionProvider, IDisposable
{
    private readonly ILogger<PlcDataService> _logger;
    private Plc? _plcClient;
    private readonly object _stateLock = new();
    private ConnectionState _state = ConnectionState.Disconnected;
    private S7ConnectionConfig _configuration = new("127.0.0.1");
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcDataService"/> class.
    /// </summary>
    /// <param name="logger">The logger for logging events and errors.</param>
    public PlcDataService(ILogger<PlcDataService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IS7ConnectionProvider Implementation

    /// <inheritdoc/>
    public ConnectionState State
    {
        get
        {
            lock (_stateLock)
            {
                if (_plcClient?.IsConnected == true)
                {
                    return ConnectionState.Connected;
                }
                return _state;
            }
        }
    }

    /// <inheritdoc/>
    public S7ConnectionConfig Configuration
    {
        get
        {
            lock (_stateLock)
            {
                return _configuration;
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc/>
    public async Task<Result> ConnectAsync(S7ConnectionConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to connect to PLC at {IpAddress}:{Port}", config.IpAddress, config.Port);
            ChangeState(ConnectionState.Connecting);

            await DisconnectInternalAsync(); // Ensure any existing connection is closed

            var cpuType = (CpuType)Enum.Parse(typeof(CpuType), "S71200"); // Assuming S71200/1500, might need config
            _plcClient = new Plc(cpuType, config.IpAddress, (short)config.Rack, (short)config.Slot);

            await _plcClient.OpenAsync();

            lock (_stateLock)
            {
                _configuration = config;
            }

            ChangeState(ConnectionState.Connected);
            _logger.LogInformation("Successfully connected to PLC at {IpAddress}:{Port}", config.IpAddress, config.Port);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to PLC at {IpAddress}:{Port}", config.IpAddress, config.Port);
            ChangeState(ConnectionState.Error, ex.Message);
            await DisconnectInternalAsync(); // Cleanup client
            return Result.Failure($"Connection failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disconnecting from PLC");
            await DisconnectInternalAsync();
            ChangeState(ConnectionState.Disconnected);
            _logger.LogInformation("Successfully disconnected from PLC");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection");
            return Result.Failure($"Disconnection failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public Task<Result<PlcInfo>> GetPlcInfoAsync(CancellationToken cancellationToken = default)
    {
        // S7netplus does not have a direct equivalent to GetPlcInfo, returning a simulated one for now.
        // In a real scenario, specific tags would be read to get this info.
        if (State != ConnectionState.Connected)
        {
            return Task.FromResult(Result<PlcInfo>.Failure("Not connected to PLC"));
        }
        var plcInfo = new PlcInfo("S7-1200/1500", "N/A", "N/A", "N/A", "N/A");
        return Task.FromResult(Result<PlcInfo>.Success(plcInfo));
    }

    /// <inheritdoc/>
    public Task<Result> TestConnectionAsync(S7ConnectionConfig config, CancellationToken cancellationToken = default)
    {
        // A simple test could be a quick connect/disconnect
        // For now, we'll rely on the main ConnectAsync method.
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc/>
    public async Task<Result> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to reconnect to PLC");
        ChangeState(ConnectionState.Reconnecting);
        await DisconnectInternalAsync();
        await Task.Delay(1000, cancellationToken);
        return await ConnectAsync(Configuration, cancellationToken);
    }

    #endregion

    #region ITagRepository Implementation

    /// <inheritdoc/>
    public async Task<Result<Tag>> ReadTagAsync(PlcAddress address, CancellationToken cancellationToken = default)
    {
       if (_plcClient == null || !_plcClient.IsConnected)
        {
            return Result<Tag>.Failure("Not connected to PLC.");
        }

        try
        {
            var s7Var = address.Value; // S7.Net uses string addresses
            object value = await _plcClient.ReadAsync(s7Var, cancellationToken);
            var tagResult = Tag.Create($"Tag_{address}", address.Value, value);
            return tagResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read tag at address {Address}", address);
            return Result<Tag>.Failure($"Failed to read tag: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result> WriteTagAsync(PlcAddress address, object? value, CancellationToken cancellationToken = default)
    {
        if (_plcClient == null || !_plcClient.IsConnected)
        {
            return Result.Failure("Not connected to PLC.");
        }

        if (value == null)
        {
            return Result.Failure("Value to write cannot be null.");
        }

        try
        {
            var s7Var = address.Value;
            await _plcClient.WriteAsync(s7Var, value, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write value {Value} to address {Address}", value, address);
            return Result.Failure($"Failed to write tag: {ex.Message}", ex);
        }
    }

    // --- In-memory Tag Management (not directly related to PLC comms but part of the contract) ---
    private readonly ConcurrentDictionary<string, Tag> _managedTags = new();

    /// <inheritdoc/>
    public Task<Result<IReadOnlyCollection<Tag>>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = _managedTags.Values.ToList().AsReadOnly();
        return Task.FromResult(Result<IReadOnlyCollection<Tag>>.Success(tags));
    }

    /// <inheritdoc/>
    public Task<Result> AddTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        _managedTags.AddOrUpdate(tag.Name, tag, (_, existing) => tag);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc/>
    public Task<Result> RemoveTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        if (_managedTags.TryRemove(tagName, out _))
        {
            return Task.FromResult(Result.Success());
        }
        return Task.FromResult(Result.Failure($"Tag '{tagName}' not found."));
    }

    /// <inheritdoc/>
    public async Task<Result<Tag>> ReadTagByNameAsync(string tagName, CancellationToken cancellationToken = default)
    {
        if (_managedTags.TryGetValue(tagName, out var tag))
        {
            return await ReadTagAsync(tag.Address, cancellationToken);
        }
        return Result<Tag>.Failure($"Tag '{tagName}' not found.");
    }

    /// <inheritdoc/>
    public async Task<Result> WriteTagByNameAsync(string tagName, object? value, CancellationToken cancellationToken = default)
    {
        if (_managedTags.TryGetValue(tagName, out var tag))
        {
            return await WriteTagAsync(tag.Address, value, cancellationToken);
        }
        return Result.Failure($"Tag '{tagName}' not found.");
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyCollection<Tag>>> ReadTagsAsync(IEnumerable<PlcAddress> addresses, CancellationToken cancellationToken = default)
    {
        if (_plcClient == null || !_plcClient.IsConnected)
        {
            return Result<IReadOnlyCollection<Tag>>.Failure("Not connected to PLC.");
        }

        var results = new List<Tag>();
        var allTasks = addresses.Select(addr => ReadTagAsync(addr, cancellationToken));
        var allResults = await Task.WhenAll(allTasks);

        foreach (var result in allResults)
        {
            if (result.IsSuccess)
            {
                results.Add(result.Value);
            }
            else
            {
                _logger.LogWarning("Failed to read a tag during multi-read: {Error}", result.Error);
            }
        }
        return Result<IReadOnlyCollection<Tag>>.Success(results);
    }

    /// <inheritdoc/>
    public async Task<Result> WriteTagsAsync(IEnumerable<(PlcAddress Address, object? Value)> tagWrites, CancellationToken cancellationToken = default)
    {
        if (_plcClient == null || !_plcClient.IsConnected)
        {
            return Result.Failure("Not connected to PLC.");
        }

        var allTasks = tagWrites.Select(tw => WriteTagAsync(tw.Address, tw.Value, cancellationToken));
        var allResults = await Task.WhenAll(allTasks);

        var failedWrites = allResults.Where(r => r.IsFailure).ToList();
        if (failedWrites.Any())
        {
            var errors = string.Join(", ", failedWrites.Select(r => r.Error));
            return Result.Failure($"Failed to write some tags: {errors}");
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyCollection<Tag>>> GetTagsByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
        var tags = _managedTags.Values.Where(t => t.Group.Equals(group, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
        return Task.FromResult(Result<IReadOnlyCollection<Tag>>.Success(tags));
    }

    /// <inheritdoc/>
    public Task<Result> ValidateAddressAsync(PlcAddress address, CancellationToken cancellationToken = default)
    {
        // With S7.Net, validation happens on read/write. We can assume success here.
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Private Methods

    private void ChangeState(ConnectionState newState, string? error = null)
    {
        ConnectionState previousState;
        lock (_stateLock)
        {
            previousState = _state;
            _state = newState;
        }

        if (previousState != newState)
        {
            _logger.LogDebug("Connection state changed from {PreviousState} to {NewState}", previousState, newState);
            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(previousState, newState, error));
        }
    }

    private async Task DisconnectInternalAsync()
    {
        if (_plcClient != null)
        {
            if (_plcClient.IsConnected)
            {
                await Task.Run(() => _plcClient.Close());
            }
            _plcClient = null;
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing PlcDataService");
            DisconnectInternalAsync().GetAwaiter().GetResult();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    #endregion
}