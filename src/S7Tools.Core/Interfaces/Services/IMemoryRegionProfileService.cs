using S7Tools.Core.Models;

namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Standard contract for managing memory mapping profiles.
/// Inherits all functionality from the unified IProfileManager interface.
/// </summary>
/// <remarks>
/// This interface follows the standardized approach where all profile services
/// implement the unified IProfileManager&lt;T&gt; interface for consistent behavior.
/// Memory mapping profiles support firmware memory mapping with segment validation.
/// </remarks>
public interface IMemoryRegionProfileService : IProfileManager<MemoryMappingProfile>
{
    // No additional methods needed - all functionality is provided by IProfileManager<T>
    // This interface exists for dependency injection and type safety
}
