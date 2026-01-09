namespace S7Tools.Constants;

/// <summary>
/// Common timeout constants used throughout the application.
/// All values are in milliseconds unless otherwise specified.
/// </summary>
public static class TimeoutConstants
{
    /// <summary>
    /// Standard delay for serial port operations (1 second).
    /// </summary>
    public const int SerialPortOperationMs = 1000;

    /// <summary>
    /// Timeout for serial port accessibility tests (1 second).
    /// </summary>
    public const int SerialPortTestMs = 1000;

    /// <summary>
    /// Timeout for command execution (2 seconds).
    /// </summary>
    public const int CommandExecutionShortMs = 2000;

    /// <summary>
    /// Timeout for network operations like finding socat processes (3 seconds).
    /// </summary>
    public const int NetworkOperationMs = 3000;

    /// <summary>
    /// Standard timeout for command execution (5 seconds).
    /// </summary>
    public const int CommandExecutionMs = 5000;

    /// <summary>
    /// Default power cycle delay (5 seconds).
    /// </summary>
    public const int PowerCycleDelayMs = 5000;

    /// <summary>
    /// Default TCP connection test timeout (5 seconds).
    /// </summary>
    public const int TcpConnectionTestMs = 5000;

    /// <summary>
    /// Extended timeout for long-running operations (10 seconds).
    /// </summary>
    public const int LongOperationMs = 10000;
}

/// <summary>
/// Power supply timing constants.
/// </summary>
public static class PowerConstants
{
    /// <summary>
    /// Default time to keep power on during bootloader operations (5 seconds).
    /// </summary>
    public const int DefaultPowerOnTimeMs = 5000;

    /// <summary>
    /// Default delay before powering off after operation (2 seconds).
    /// </summary>
    public const int DefaultPowerOffDelayMs = 2000;

    /// <summary>
    /// Milliseconds per second conversion factor.
    /// </summary>
    public const int MillisecondsPerSecond = 1000;
}

/// <summary>
/// Task scheduler constants.
/// </summary>
public static class SchedulerConstants
{
    /// <summary>
    /// Maximum number of execution times to track for statistics (rolling window).
    /// </summary>
    public const int MaxExecutionTimesCount = 1000;

    /// <summary>
    /// Standard polling interval for task processing (1 second).
    /// </summary>
    public const int TaskPollingIntervalMs = 1000;
}
