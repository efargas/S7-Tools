using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Modern implementation of PLC data services with proper error handling, logging, and state management.
/// Implements both ITagRepository and IS7ConnectionProvider interfaces using the latest .NET patterns.
/// </summary>
public sealed class PlcDataService : ITagRepository, IS7ConnectionProvider, IDisposable
{
    private readonly ILogger<PlcDataService> _logger;
    private readonly ConcurrentDictionary<string, Tag> _managedTags = new();
    private readonly Random _random = new(); // For simulation purposes
    private readonly object _stateLock = new();

    private ConnectionState _state = ConnectionState.Disconnected;
    private S7ConnectionConfig _configuration = new("127.0.0.1");
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the PlcDataService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PlcDataService(ILogger<PlcDataService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("PlcDataService initialized");
    }

    #region IS7ConnectionProvider Implementation

    /// <inheritdoc />
    public ConnectionState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task<Result> ConnectAsync(S7ConnectionConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to connect to PLC at {IpAddress}:{Port}", config.IpAddress, config.Port);

            ChangeState(ConnectionState.Connecting);

            // Simulate connection delay
            await Task.Delay(1000, cancellationToken);

            lock (_stateLock)
            {
                _configuration = config;
            }

            ChangeState(ConnectionState.Connected);

            _logger.LogInformation("Successfully connected to PLC at {IpAddress}:{Port}", config.IpAddress, config.Port);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Connection attempt was cancelled");
            ChangeState(ConnectionState.Disconnected);
            return Result.Failure("Connection attempt was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to PLC at {IpAddress}:{Port}", config.IpAddress, config.Port);
            ChangeState(ConnectionState.Error, ex.Message);
            return Result.Failure($"Connection failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disconnecting from PLC");

            // Simulate disconnection delay
            await Task.Delay(500, cancellationToken);

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

    /// <inheritdoc />
    public async Task<Result> TestConnectionAsync(S7ConnectionConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to PLC at {IpAddress}:{Port}", config.IpAddress, config.Port);

            // Simulate connection test
            await Task.Delay(500, cancellationToken);

            // For simulation, assume connection is successful if IP is not empty
            var success = !string.IsNullOrWhiteSpace(config.IpAddress);

            if (success)
            {
                _logger.LogInformation("Connection test successful for {IpAddress}:{Port}", config.IpAddress, config.Port);
                return Result.Success();
            }
            else
            {
                _logger.LogWarning("Connection test failed for {IpAddress}:{Port}", config.IpAddress, config.Port);
                return Result.Failure("Connection test failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection test");
            return Result.Failure($"Connection test failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PlcInfo>> GetPlcInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (State != ConnectionState.Connected)
            {
                return Result<PlcInfo>.Failure("Not connected to PLC");
            }

            _logger.LogDebug("Retrieving PLC information");

            // Simulate delay for reading PLC info
            await Task.Delay(200, cancellationToken);

            // Simulate PLC info (in real implementation, this would come from the actual PLC)
            var plcInfo = new PlcInfo(
                CpuType: "CPU 1516-3 PN/DP",
                SerialNumber: "S C-X4U421302009",
                ModuleName: "CPU 1516-3 PN/DP",
                ModuleTypeName: "CPU",
                FirmwareVersion: "V2.8.3");

            _logger.LogDebug("Retrieved PLC info: {PlcInfo}", plcInfo);
            return Result<PlcInfo>.Success(plcInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PLC information");
            return Result<PlcInfo>.Failure($"Failed to get PLC info: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to reconnect to PLC");

        ChangeState(ConnectionState.Reconnecting);

        var disconnectResult = await DisconnectAsync(cancellationToken);
        if (disconnectResult.IsFailure)
        {
            _logger.LogWarning("Failed to disconnect during reconnect: {Error}", disconnectResult.Error);
        }

        await Task.Delay(1000, cancellationToken); // Wait before reconnecting

        return await ConnectAsync(Configuration, cancellationToken);
    }

    #endregion

    #region ITagRepository Implementation

    /// <inheritdoc />
    public async Task<Result<Tag>> ReadTagAsync(PlcAddress address, CancellationToken cancellationToken = default)
    {
        try
        {
            if (State != ConnectionState.Connected)
            {
                return Result<Tag>.Failure("Not connected to PLC");
            }

            _logger.LogDebug("Reading tag from address {Address}", address);

            // Simulate read delay
            await Task.Delay(50, cancellationToken);

            // Generate simulated value based on address type
            var simulatedValue = GenerateSimulatedValue(address);
            var tagResult = Tag.Create($"Tag_{address}", address.Value, simulatedValue);

            if (tagResult.IsFailure)
            {
                _logger.LogError("Failed to create tag for address {Address}: {Error}", address, tagResult.Error);
                return tagResult;
            }

            _logger.LogDebug("Successfully read tag {TagName} from {Address} with value {Value}",
                tagResult.Value.Name, address, tagResult.Value.GetDisplayValue());

            return tagResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading tag from address {Address}", address);
            return Result<Tag>.Failure($"Failed to read tag: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Tag>> ReadTagByNameAsync(string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return Result<Tag>.Failure("Tag name cannot be null or empty");
            }

            if (_managedTags.TryGetValue(tagName, out var existingTag))
            {
                // Read the current value for the existing tag
                var readResult = await ReadTagAsync(existingTag.Address, cancellationToken);
                if (readResult.IsSuccess)
                {
                    var updatedTag = existingTag.WithValue(readResult.Value.Value.RawValue);
                    _managedTags.TryUpdate(tagName, updatedTag, existingTag);
                    return Result<Tag>.Success(updatedTag);
                }
                return readResult;
            }

            return Result<Tag>.Failure($"Tag '{tagName}' not found in managed tags");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading tag by name {TagName}", tagName);
            return Result<Tag>.Failure($"Failed to read tag by name: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyCollection<Tag>>> ReadTagsAsync(IEnumerable<PlcAddress> addresses, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = addresses.Select(address => ReadTagAsync(address, cancellationToken));
            var results = await Task.WhenAll(tasks);

            var successfulTags = new List<Tag>();
            var errors = new List<string>();

            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    successfulTags.Add(result.Value);
                }
                else
                {
                    errors.Add(result.Error);
                }
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Some tags failed to read: {Errors}", string.Join(", ", errors));
            }

            return Result<IReadOnlyCollection<Tag>>.Success(successfulTags.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading multiple tags");
            return Result<IReadOnlyCollection<Tag>>.Failure($"Failed to read tags: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteTagAsync(PlcAddress address, object? value, CancellationToken cancellationToken = default)
    {
        try
        {
            if (State != ConnectionState.Connected)
            {
                return Result.Failure("Not connected to PLC");
            }

            _logger.LogDebug("Writing value {Value} to address {Address}", value, address);

            // Simulate write delay
            await Task.Delay(50, cancellationToken);

            _logger.LogDebug("Successfully wrote value {Value} to address {Address}", value, address);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to address {Address}", address);
            return Result.Failure($"Failed to write tag: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteTagByNameAsync(string tagName, object? value, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return Result.Failure("Tag name cannot be null or empty");
            }

            if (_managedTags.TryGetValue(tagName, out var tag))
            {
                return await WriteTagAsync(tag.Address, value, cancellationToken);
            }

            return Result.Failure($"Tag '{tagName}' not found in managed tags");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing tag by name {TagName}", tagName);
            return Result.Failure($"Failed to write tag by name: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteTagsAsync(IEnumerable<(PlcAddress Address, object? Value)> tagWrites, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = tagWrites.Select(tw => WriteTagAsync(tw.Address, tw.Value, cancellationToken));
            var results = await Task.WhenAll(tasks);

            var errors = results.Where(r => r.IsFailure).Select(r => r.Error).ToList();

            if (errors.Count > 0)
            {
                _logger.LogError("Some tag writes failed: {Errors}", string.Join(", ", errors));
                return Result.Failure($"Some writes failed: {string.Join(", ", errors)}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing multiple tags");
            return Result.Failure($"Failed to write tags: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task<Result> AddTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        try
        {
            _managedTags.AddOrUpdate(tag.Name, tag, (_, _) => tag);
            _logger.LogDebug("Added tag {TagName} to managed tags", tag.Name);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag {TagName}", tag.Name);
            return Task.FromResult(Result.Failure($"Failed to add tag: {ex.Message}", ex));
        }
    }

    /// <inheritdoc />
    public Task<Result> RemoveTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_managedTags.TryRemove(tagName, out _))
            {
                _logger.LogDebug("Removed tag {TagName} from managed tags", tagName);
                return Task.FromResult(Result.Success());
            }

            return Task.FromResult(Result.Failure($"Tag '{tagName}' not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag {TagName}", tagName);
            return Task.FromResult(Result.Failure($"Failed to remove tag: {ex.Message}", ex));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyCollection<Tag>>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = _managedTags.Values.ToList().AsReadOnly();
            return Task.FromResult(Result<IReadOnlyCollection<Tag>>.Success(tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tags");
            return Task.FromResult(Result<IReadOnlyCollection<Tag>>.Failure($"Failed to get tags: {ex.Message}", ex));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyCollection<Tag>>> GetTagsByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = _managedTags.Values
                .Where(t => string.Equals(t.Group, group, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();

            return Task.FromResult(Result<IReadOnlyCollection<Tag>>.Success(tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags by group {Group}", group);
            return Task.FromResult(Result<IReadOnlyCollection<Tag>>.Failure($"Failed to get tags by group: {ex.Message}", ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ValidateAddressAsync(PlcAddress address, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simulate validation delay
            await Task.Delay(100, cancellationToken);

            // For simulation, all addresses are considered valid
            _logger.LogDebug("Address {Address} is valid", address);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating address {Address}", address);
            return Result.Failure($"Address validation failed: {ex.Message}", ex);
        }
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

    private object GenerateSimulatedValue(PlcAddress address)
    {
        // Generate different types of simulated values based on address type
        return address.AddressType switch
        {
            PlcAddressType.DataBlockBit or PlcAddressType.Memory or PlcAddressType.Input or PlcAddressType.Output => _random.Next(0, 2) == 1,
            PlcAddressType.DataBlockByte => (byte)_random.Next(0, 256),
            PlcAddressType.DataBlockWord => (short)_random.Next(-32768, 32767),
            PlcAddressType.DataBlockDWord => _random.Next(-2147483648, 2147483647),
            PlcAddressType.Timer => TimeSpan.FromSeconds(_random.Next(0, 3600)),
            PlcAddressType.Counter => _random.Next(0, 9999),
            _ => _random.Next(0, 100)
        };
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing PlcDataService");

            // Disconnect if connected
            if (State == ConnectionState.Connected)
            {
                _ = DisconnectAsync().ConfigureAwait(false);
            }

            _disposed = true;
        }
    }

    #endregion
}
