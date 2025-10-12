// PayloadManager.cs (Stub)
namespace SiemensS7Bootloader.S7.Net
{
    using System.Threading.Tasks;

    public class PayloadManager
    {
        public PayloadManager(string baseDir) { }
        public Task<byte[]> GetStagerPayloadAsync(string basePath) => Task.FromResult(new byte[0]);
        public Task<byte[]> GetMemoryDumperPayloadAsync(string basePath) => Task.FromResult(new byte[0]);
    }
}