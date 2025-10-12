// SocatModels.cs
using System;
using System.Collections.Generic;

namespace S7Tools.Core.Models
{
    public enum SocatProcessStatus { Running, Stopped, Error }
    public class SocatProcessEventArgs : EventArgs { public SocatProcessInfo ProcessInfo { get; set; } = new(); public Exception Error { get; set; } = new(); }
    public class SocatProcessErrorEventArgs : EventArgs { public SocatProcessInfo ProcessInfo { get; set; } = new(); public Exception Error { get; set; } = new(); }
    public class SocatConnectionEventArgs : EventArgs { public SocatProcessInfo ProcessInfo { get; set; } = new(); }
    public class SocatDataTransferEventArgs : EventArgs { }
    public class SocatCommandValidationResult { public List<string> Errors { get; set; } = new(); public bool IsValid { get; set; } public string ValidatedCommand { get; set; } = ""; public int? DetectedTcpPort { get; set; } public string? DetectedSerialDevice { get; set; } public List<string> Warnings { get; set; } = new(); public bool RequiresRoot { get; set; } }
    public class SocatProcessInfo { public int ProcessId { get; set; } public int TcpPort { get; set; } public string TcpHost { get; set; } = ""; public string SerialDevice { get; set; } = ""; public bool IsRunning { get; set; } public int ActiveConnections { get; set; } public SocatTransferStats TransferStats { get; set; } = new(); public S7Tools.Core.Models.SocatConfiguration? Configuration { get; set; } public S7Tools.Core.Models.SocatProfile? Profile { get; set; } public string? CommandLine { get; set; } public DateTime StartTime { get; set; } public SocatProcessStatus Status { get; set; } public DateTime LastUpdated { get; set; } public string? LastError { get; set; }}
    public class SocatConnectionInfo { public string LocalAddress { get; set; } = ""; public int LocalPort { get; set; } public string RemoteAddress { get; set; } = ""; public int RemotePort { get; set; } public DateTime EstablishedTime { get; set; } public long BytesSent { get; set; } public long BytesReceived { get; set; } }
    public class SocatTransferStats { public long BytesSerialToTcp { get; set; } public long BytesTcpToSerial { get; set; } public int TotalConnections { get; set; } public int ActiveConnections { get; set; } public DateTime LastUpdated { get; set; } public TimeSpan Uptime { get; set; } }
}