// IBootloaderService.cs
namespace S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Models.Jobs;
public interface IBootloaderService {
    Task<byte[]> DumpAsync(JobProfileSet profiles, IProgress<(string stage, double pct)> progress, CancellationToken ct = default);
}