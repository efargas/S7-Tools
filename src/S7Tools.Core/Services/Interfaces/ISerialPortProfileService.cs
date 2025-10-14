using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Standard contract for managing serial port profiles.
/// Inherits all functionality from the unified IProfileManager interface.
/// </summary>
/// <remarks>
/// This interface follows the new standardized approach where all profile services
/// implement the unified IProfileManager&lt;T&gt; interface for consistent behavior.
/// </remarks>
public interface ISerialPortProfileService : IProfileManager<SerialPortProfile>
{
    // No additional methods needed - all functionality is provided by IProfileManager<T>
    // This interface exists for dependency injection and type safety
}
