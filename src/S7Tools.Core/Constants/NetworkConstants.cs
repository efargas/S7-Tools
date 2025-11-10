namespace S7Tools.Core.Constants;

/// <summary>
/// Network-related constants for TCP/IP port validation and configuration.
/// </summary>
/// <remarks>
/// These constants define the valid range for TCP/UDP ports and provide
/// standardized error messages for port validation failures.
/// </remarks>
public static class NetworkConstants
{
    /// <summary>
    /// Minimum valid TCP/UDP port number.
    /// </summary>
    /// <remarks>
    /// Port 0 is reserved; valid user ports start at 1.
    /// </remarks>
    public const int MinPort = 1;

    /// <summary>
    /// Maximum valid TCP/UDP port number.
    /// </summary>
    /// <remarks>
    /// Port 65535 is the highest valid port number (16-bit unsigned integer maximum).
    /// </remarks>
    public const int MaxPort = 65535;

    /// <summary>
    /// Standard error message for port range validation.
    /// Use with string.Format(NetworkConstants.PortRangeError, portNumber).
    /// </summary>
    public const string PortRangeError = "TCP port {0} is not in valid range (1-65535)";

    /// <summary>
    /// Validates if a port number is within the valid range.
    /// </summary>
    /// <param name="port">The port number to validate.</param>
    /// <returns>True if the port is valid; otherwise, false.</returns>
    public static bool IsValidPort(int port) => port >= MinPort && port <= MaxPort;
}
