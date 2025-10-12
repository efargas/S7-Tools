// SocatProfileRef.cs
namespace S7Tools.Core.Models.Profiles;
public sealed record SocatProfileRef(int Port, bool Ephemeral = true);