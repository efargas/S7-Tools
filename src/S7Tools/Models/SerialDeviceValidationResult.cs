using System.Collections.Generic;

namespace S7Tools.Models
{
    public class SerialDeviceValidationResult
    {
        public bool IsValid { get; set; }
        public bool Exists { get; set; }
        public bool IsAccessible { get; set; }
        public bool IsInUse { get; set; }
        public string? DeviceInfo { get; set; }
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
    }
}