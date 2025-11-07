using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Standard implementation of memory region profile management.
/// Provides unified profile management functionality for MemoryMappingProfile objects.
/// </summary>
/// <remarks>
/// This service follows the standardized approach using StandardProfileManager&lt;T&gt; base class.
/// It provides consistent behavior with other profile types including thread safety,
/// JSON persistence, and comprehensive business rule enforcement.
/// </remarks>
public class MemoryRegionProfileService : StandardProfileManager<MemoryMappingProfile>, IMemoryRegionProfileService
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the MemoryRegionProfileService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathService">The path service for resolving profile file paths.</param>
    public MemoryRegionProfileService(ILogger<MemoryRegionProfileService> logger, IPathService pathService)
        : base(pathService.MemoryRegionProfilesPath, logger)
    {
    }

    #endregion

    #region StandardProfileManager Implementation

    /// <inheritdoc/>
    protected override MemoryMappingProfile CreateSystemDefault()
    {
        return MemoryMappingProfile.CreateDefaultProfile();
    }

    /// <inheritdoc/>
    protected override string ProfileTypeName => "Memory Region Profile";

    /// <inheritdoc/>
    protected override async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        // Create default memory region profile for S7-1200 PLC
        var defaultProfile = MemoryMappingProfile.CreateDefaultProfile();

        // Use the CreateAsync method to ensure proper validation and persistence
        await CreateAsync(defaultProfile, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created default memory region profile: {ProfileName}", defaultProfile.Name);
    }

    #endregion
}
