// JobProfileSet.cs
namespace S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Profiles;

public sealed record JobProfileSet(
    SerialProfileRef Serial,
    SocatProfileRef Socat,
    PowerProfileRef Power,
    MemoryRegionProfile Memory,
    PayloadSetProfile Payloads,
    string OutputPath
);