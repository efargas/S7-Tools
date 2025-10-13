// PayloadManager.cs
using System;
using System.Threading.Tasks;

namespace SiemensS7Bootloader.S7.Core
{
    public class PayloadManager
    {
        public PayloadManager(string basePath) { }
        public Task<byte[]> GetStagerPayloadAsync(string name) => Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> GetMemoryDumperPayloadAsync(string name) => Task.FromResult(Array.Empty<byte>());
    }
}