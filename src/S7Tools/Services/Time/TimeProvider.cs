using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Services.Time;

/// <summary>
/// Default implementation of <see cref="ITimeProvider"/> using system time.
/// </summary>
public class TimeProvider : ITimeProvider
{
    /// <inheritdoc />
    public DateTime GetUtcNow() => DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime GetLocalNow() => DateTime.Now; // Or DateTime.UtcNow.ToLocalTime()
}
