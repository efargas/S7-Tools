using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Validation;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for enhanced bootloader orchestration operations with task integration.
/// Extends the basic bootloader service with TaskExecution integration, retry mechanisms, and detailed progress tracking.
/// </summary>
public interface IEnhancedBootloaderService : IBootloaderService
{
    /// <summary>
    /// Performs a complete memory dump operation with TaskExecution integration.
    /// Orchestrates socat bridge setup, power cycling, handshake, stager installation, and memory dumping
    /// while updating the provided TaskExecution with detailed progress and state changes.
    /// </summary>
    /// <param name="taskExecution">The task execution to update with progress and state changes.</param>
    /// <param name="profiles">Job profile set containing all configuration parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The dumped memory data.</returns>
    Task<byte[]> DumpWithTaskTrackingAsync(
        TaskExecution taskExecution,
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all required resources are available for a bootloader operation.
    /// </summary>
    /// <param name="profiles">Job profile set to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A validation result indicating success or failure with detailed error messages.</returns>
    Task<ValidationResult> ValidateResourcesAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a test connection to verify bootloader accessibility without running a full dump.
    /// </summary>
    /// <param name="profiles">Job profile set containing connection configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the bootloader is accessible, false otherwise.</returns>
    Task<bool> TestConnectionAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about the connected bootloader.
    /// </summary>
    /// <param name="profiles">Job profile set containing connection configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Bootloader information including version, capabilities, and memory layout.</returns>
    Task<BootloaderInfo> GetBootloaderInfoAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the time required for a memory dump operation.
    /// </summary>
    /// <param name="profiles">Job profile set containing memory region configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Estimated operation time, or null if it cannot be determined.</returns>
    Task<TimeSpan?> EstimateOperationTimeAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current retry configuration for bootloader operations.
    /// </summary>
    RetryConfiguration RetryConfiguration { get; }

    /// <summary>
    /// Updates the retry configuration for bootloader operations.
    /// </summary>
    /// <param name="configuration">The new retry configuration.</param>
    void UpdateRetryConfiguration(RetryConfiguration configuration);
}

/// <summary>
/// Contains information about a connected bootloader.
/// </summary>
public class BootloaderInfo
{
    /// <summary>
    /// Gets or sets the bootloader version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PLC model information.
    /// </summary>
    public string PlcModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the firmware version.
    /// </summary>
    public string FirmwareVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the available memory regions.
    /// </summary>
    public IReadOnlyList<MemoryRegion> AvailableMemoryRegions { get; set; } = new List<MemoryRegion>();

    /// <summary>
    /// Gets or sets the maximum transfer size for memory operations.
    /// </summary>
    public int MaxTransferSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bootloader supports pause/resume operations.
    /// </summary>
    public bool SupportsPauseResume { get; set; }

    /// <summary>
    /// Gets or sets additional capability flags.
    /// </summary>
    public BootloaderCapabilities Capabilities { get; set; } = BootloaderCapabilities.None;
}

/// <summary>
/// Represents an available memory region in the PLC.
/// </summary>
public class MemoryRegion
{
    /// <summary>
    /// Gets or sets the name of the memory region.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start address of the memory region.
    /// </summary>
    public uint StartAddress { get; set; }

    /// <summary>
    /// Gets or sets the size of the memory region in bytes.
    /// </summary>
    public uint Size { get; set; }

    /// <summary>
    /// Gets or sets the access permissions for this memory region.
    /// </summary>
    public MemoryAccessFlags AccessFlags { get; set; }

    /// <summary>
    /// Gets or sets a description of what this memory region contains.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets the end address of the memory region.
    /// </summary>
    public uint EndAddress => StartAddress + Size - 1;

    /// <summary>
    /// Checks if the specified address range is within this memory region.
    /// </summary>
    /// <param name="address">The start address to check.</param>
    /// <param name="length">The length of the range to check.</param>
    /// <returns>True if the range is within this memory region, false otherwise.</returns>
    public bool ContainsRange(uint address, uint length)
    {
        return address >= StartAddress && address + length <= StartAddress + Size;
    }
}

/// <summary>
/// Flags indicating bootloader capabilities.
/// </summary>
[Flags]
public enum BootloaderCapabilities
{
    /// <summary>
    /// No special capabilities.
    /// </summary>
    None = 0,

    /// <summary>
    /// Supports memory read operations.
    /// </summary>
    MemoryRead = 1 << 0,

    /// <summary>
    /// Supports memory write operations.
    /// </summary>
    MemoryWrite = 1 << 1,

    /// <summary>
    /// Supports flash memory operations.
    /// </summary>
    FlashAccess = 1 << 2,

    /// <summary>
    /// Supports real-time memory monitoring.
    /// </summary>
    RealTimeMonitoring = 1 << 3,

    /// <summary>
    /// Supports operation pause and resume.
    /// </summary>
    PauseResume = 1 << 4,

    /// <summary>
    /// Supports checksums for data integrity.
    /// </summary>
    Checksums = 1 << 5,

    /// <summary>
    /// Supports compressed data transfer.
    /// </summary>
    Compression = 1 << 6
}

/// <summary>
/// Flags indicating memory access permissions.
/// </summary>
[Flags]
public enum MemoryAccessFlags
{
    /// <summary>
    /// No access allowed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Read access allowed.
    /// </summary>
    Read = 1 << 0,

    /// <summary>
    /// Write access allowed.
    /// </summary>
    Write = 1 << 1,

    /// <summary>
    /// Execute access allowed.
    /// </summary>
    Execute = 1 << 2,

    /// <summary>
    /// Full access (read, write, execute).
    /// </summary>
    Full = Read | Write | Execute
}

/// <summary>
/// Configuration for retry mechanisms in bootloader operations.
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for connection operations.
    /// </summary>
    public int MaxConnectionRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for communication operations.
    /// </summary>
    public int MaxCommunicationRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for memory operations.
    /// </summary>
    public int MaxMemoryOperationRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay between retry attempts.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets a value indicating whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets the operations that should be retried automatically.
    /// </summary>
    public RetryableOperations RetryableOperations { get; set; } = RetryableOperations.All;

    /// <summary>
    /// Creates a default retry configuration.
    /// </summary>
    /// <returns>A new retry configuration with default values.</returns>
    public static RetryConfiguration Default => new();

    /// <summary>
    /// Creates a conservative retry configuration with fewer retries and longer delays.
    /// </summary>
    /// <returns>A new conservative retry configuration.</returns>
    public static RetryConfiguration Conservative => new()
    {
        MaxConnectionRetries = 2,
        MaxCommunicationRetries = 3,
        MaxMemoryOperationRetries = 2,
        InitialRetryDelay = TimeSpan.FromSeconds(2),
        MaxRetryDelay = TimeSpan.FromMinutes(1),
        BackoffMultiplier = 1.5
    };

    /// <summary>
    /// Creates an aggressive retry configuration with more retries and shorter delays.
    /// </summary>
    /// <returns>A new aggressive retry configuration.</returns>
    public static RetryConfiguration Aggressive => new()
    {
        MaxConnectionRetries = 5,
        MaxCommunicationRetries = 8,
        MaxMemoryOperationRetries = 5,
        InitialRetryDelay = TimeSpan.FromMilliseconds(500),
        MaxRetryDelay = TimeSpan.FromSeconds(10),
        BackoffMultiplier = 1.8
    };
}

/// <summary>
/// Flags indicating which operations should be retried automatically.
/// </summary>
[Flags]
public enum RetryableOperations
{
    /// <summary>
    /// No operations should be retried.
    /// </summary>
    None = 0,

    /// <summary>
    /// Connection establishment operations.
    /// </summary>
    Connection = 1 << 0,

    /// <summary>
    /// Handshake and initialization operations.
    /// </summary>
    Handshake = 1 << 1,

    /// <summary>
    /// Payload installation operations.
    /// </summary>
    PayloadInstallation = 1 << 2,

    /// <summary>
    /// Memory read operations.
    /// </summary>
    MemoryRead = 1 << 3,

    /// <summary>
    /// Power control operations.
    /// </summary>
    PowerControl = 1 << 4,

    /// <summary>
    /// Network operations (socat, TCP connections).
    /// </summary>
    Network = 1 << 5,

    /// <summary>
    /// All operations should be retried.
    /// </summary>
    All = Connection | Handshake | PayloadInstallation | MemoryRead | PowerControl | Network
}
