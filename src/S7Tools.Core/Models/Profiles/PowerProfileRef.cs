// PowerProfileRef.cs
namespace S7Tools.Core.Models.Profiles;
public sealed record PowerProfileRef(string Host, int Port, int Coil, int DelaySeconds);