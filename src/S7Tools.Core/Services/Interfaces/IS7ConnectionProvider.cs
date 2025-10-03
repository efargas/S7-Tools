namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines a contract for managing connections to an S7 PLC.
/// </summary>
public interface IS7ConnectionProvider
{
    /// <summary>
    /// Asynchronously establishes a connection to the PLC.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Asynchronously disconnects from the PLC.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
