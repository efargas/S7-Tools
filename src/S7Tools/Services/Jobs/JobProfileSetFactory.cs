using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Jobs;

/// <summary>
/// Factory for creating JobProfileSet instances from profile references.
/// Resolves profile IDs to actual profile instances and builds complete JobProfileSet.
/// </summary>
/// <remarks>
/// This factory implements the Job Profile Set creation pattern by:
/// - Loading individual profiles from their respective managers
/// - Validating that all required profiles exist
/// - Building a complete JobProfileSet ready for job execution
/// - Providing clear error messages when profiles cannot be resolved
/// </remarks>
public interface IJobProfileSetFactory
{
    /// <summary>
    /// Creates a JobProfileSet from individual profile IDs.
    /// </summary>
    /// <param name="serialProfileId">The serial port profile ID.</param>
    /// <param name="socatProfileId">The socat configuration profile ID.</param>
    /// <param name="powerProfileId">The power supply profile ID.</param>
    /// <param name="memoryRegionProfileId">The memory region profile ID.</param>
    /// <param name="payloadSetProfileId">The payload set profile ID.</param>
    /// <param name="outputPath">Output directory path for dump files.</param>
    /// <param name="powerOnTimeMs">Time to wait after powering on PLC (milliseconds). Default 5000ms.</param>
    /// <param name="powerOffDelayMs">Time to wait after powering off PLC (milliseconds). Default 2000ms.</param>
    /// <param name="selectedMemorySegment">Optional name of a specific memory segment to dump. If specified, only this segment will be included.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete JobProfileSet with all profile instances loaded.</returns>
    /// <exception cref="ProfileNotFoundException">Thrown when any profile cannot be found by ID.</exception>
    Task<JobProfileSet> CreateFromProfileIdsAsync(
        int serialProfileId,
        int socatProfileId,
        int powerProfileId,
        int memoryRegionProfileId,
        int payloadSetProfileId,
        string outputPath,
        int powerOnTimeMs = 5000,
        int powerOffDelayMs = 2000,
        string? selectedMemorySegment = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of IJobProfileSetFactory that resolves profiles from managers.
/// </summary>
/// <param name="serialManager">The serial port profile manager.</param>
/// <param name="socatManager">The socat profile manager.</param>
/// <param name="powerManager">The power supply profile manager.</param>
/// <param name="memoryRegionManager">The memory region profile manager.</param>
/// <param name="payloadSetManager">The payload set profile manager.</param>
/// <param name="logger">The logger instance.</param>
public sealed class JobProfileSetFactory(
    IProfileManager<SerialPortProfile> serialManager,
    IProfileManager<SocatProfile> socatManager,
    IProfileManager<PowerSupplyProfile> powerManager,
    IProfileManager<MemoryMappingProfile> memoryRegionManager,
    IProfileManager<PayloadSetProfile> payloadSetManager,
    ILogger<JobProfileSetFactory> logger) : IJobProfileSetFactory
{
    private readonly IProfileManager<SerialPortProfile> _serialManager = serialManager ?? throw new ArgumentNullException(nameof(serialManager));
    private readonly IProfileManager<SocatProfile> _socatManager = socatManager ?? throw new ArgumentNullException(nameof(socatManager));
    private readonly IProfileManager<PowerSupplyProfile> _powerManager = powerManager ?? throw new ArgumentNullException(nameof(powerManager));
    private readonly IProfileManager<MemoryMappingProfile> _memoryRegionManager = memoryRegionManager ?? throw new ArgumentNullException(nameof(memoryRegionManager));
    private readonly IProfileManager<PayloadSetProfile> _payloadSetManager = payloadSetManager ?? throw new ArgumentNullException(nameof(payloadSetManager));
    private readonly ILogger<JobProfileSetFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<JobProfileSet> CreateFromProfileIdsAsync(
        int serialProfileId,
        int socatProfileId,
        int powerProfileId,
        int memoryRegionProfileId,
        int payloadSetProfileId,
        string outputPath,
        int powerOnTimeMs = 5000,
        int powerOffDelayMs = 2000,
        string? selectedMemorySegment = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Creating JobProfileSet from profile IDs: Serial={SerialId}, Socat={SocatId}, Power={PowerId}, MemoryRegion={MemoryId}, PayloadSet={PayloadId}, OutputPath={OutputPath}",
            serialProfileId, socatProfileId, powerProfileId, memoryRegionProfileId, payloadSetProfileId, outputPath);

        try
        {
            // Load all profiles in parallel for efficiency
            Task<SerialPortProfile?> serialTask = _serialManager.GetByIdAsync(serialProfileId, cancellationToken);
            Task<SocatProfile?> socatTask = _socatManager.GetByIdAsync(socatProfileId, cancellationToken);
            Task<PowerSupplyProfile?> powerTask = _powerManager.GetByIdAsync(powerProfileId, cancellationToken);
            Task<MemoryMappingProfile?> memoryTask = _memoryRegionManager.GetByIdAsync(memoryRegionProfileId, cancellationToken);
            Task<PayloadSetProfile?> payloadTask = _payloadSetManager.GetByIdAsync(payloadSetProfileId, cancellationToken);

            await Task.WhenAll(serialTask, socatTask, powerTask, memoryTask, payloadTask).ConfigureAwait(false);

            // Validate that all profiles were found
            SerialPortProfile? serialProfile = await serialTask.ConfigureAwait(false);
            SocatProfile? socatProfile = await socatTask.ConfigureAwait(false);
            PowerSupplyProfile? powerProfile = await powerTask.ConfigureAwait(false);
            MemoryMappingProfile? memoryProfile = await memoryTask.ConfigureAwait(false);
            PayloadSetProfile? payloadProfile = await payloadTask.ConfigureAwait(false);

            if (serialProfile == null)
            {
                throw new ProfileNotFoundException($"Serial port profile with ID {serialProfileId} not found");
            }

            if (socatProfile == null)
            {
                throw new ProfileNotFoundException($"Socat profile with ID {socatProfileId} not found");
            }

            if (powerProfile == null)
            {
                throw new ProfileNotFoundException($"Power supply profile with ID {powerProfileId} not found");
            }

            if (memoryProfile == null)
            {
                throw new ProfileNotFoundException($"Memory region profile with ID {memoryRegionProfileId} not found");
            }

            if (payloadProfile == null)
            {
                throw new ProfileNotFoundException($"Payload set profile with ID {payloadSetProfileId} not found");
            }

            // Convert profiles to references required by JobProfileSet
            // Note: Factory uses sensible defaults for runtime parameters (device path, delays)
            // In real job execution, these come from JobProfile
            string devicePath = "/dev/ttyUSB0"; // Default device - should come from job context in production
            var serialRef = SerialProfileRef.FromProfile(serialProfile, devicePath);
            var socatRef = SocatProfileRef.FromProfile(socatProfile, ephemeral: true);
            var powerRef = PowerProfileRef.FromProfile(powerProfile, delaySeconds: powerOffDelayMs / 1000);

            // Convert first segment of MemoryMappingProfile to legacy MemoryRegionProfile for compatibility
            MemorySegment? firstSegment = memoryProfile.Segments.FirstOrDefault();
            string startAddress = firstSegment?.StartAddress ?? "0x20000000";
            uint size = firstSegment != null ? (uint)firstSegment.Size : 0x10000;

            var memoryRegion = new MemoryRegionProfile(startAddress, size);

            // Filter MemoryMappingProfile if a specific segment is selected
            MemoryMappingProfile? filteredMemoryProfile = memoryProfile;
            if (!string.IsNullOrEmpty(selectedMemorySegment) && memoryProfile != null)
            {
                MemorySegment? selectedSegment = memoryProfile.Segments
                    .FirstOrDefault(s => s.Name == selectedMemorySegment);

                if (selectedSegment != null)
                {
                    // Create a new MemoryMappingProfile with only the selected segment
                    // Mark the selected segment as selected
                    var filteredSegment = new MemorySegment
                    {
                        Name = selectedSegment.Name,
                        StartAddress = selectedSegment.StartAddress,
                        Size = selectedSegment.Size,
                        Type = selectedSegment.Type,
                        IsSelected = true,
                        Description = selectedSegment.Description
                    };

                    filteredMemoryProfile = new MemoryMappingProfile
                    {
                        Id = memoryProfile.Id,
                        Name = memoryProfile.Name,
                        Description = $"{memoryProfile.Description} (Segment: {selectedMemorySegment})",
                        Segments = [filteredSegment]
                    };

                    // Also update the legacy MemoryRegionProfile to match the selected segment
                    memoryRegion = new MemoryRegionProfile(
                        selectedSegment.StartAddress ?? "0x20000000",
                        (uint)selectedSegment.Size);

                    _logger.LogDebug("Filtered MemoryMappingProfile to single segment: {SegmentName}", selectedMemorySegment);
                }
                else
                {
                    _logger.LogWarning("Selected memory segment '{SegmentName}' not found in profile, using all segments", selectedMemorySegment);
                }
            }

            // Build the complete JobProfileSet
            var profileSet = new JobProfileSet(
                serialRef,
                socatRef,
                powerRef,
                memoryRegion,
                payloadProfile,
                outputPath,
                powerOnTimeMs,
                powerOffDelayMs,
                filteredMemoryProfile); // Use filtered profile instead of original

            _logger.LogInformation(
                "Successfully created JobProfileSet with profiles: Serial={SerialName}, Socat={SocatName}, Power={PowerName}, MemoryRegion={MemoryName}, PayloadSet={PayloadName}, OutputPath={OutputPath}, SelectedSegment={SelectedSegment}",
                serialProfile.Name, socatProfile.Name, powerProfile.Name, filteredMemoryProfile!.Name, payloadProfile.Name, outputPath, selectedMemorySegment ?? "(all)");

            return profileSet;
        }
        catch (ProfileNotFoundException)
        {
            // Re-throw profile not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create JobProfileSet from profile IDs");
            throw new ConfigurationException("JobProfileSet", $"Failed to create job profile set: {ex.Message}");
        }
    }
}
