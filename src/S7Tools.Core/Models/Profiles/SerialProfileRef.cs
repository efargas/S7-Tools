// SerialProfileRef.cs
namespace S7Tools.Core.Models.Profiles;
public sealed record SerialProfileRef(string Device, int Baud, string Parity, int DataBits, string StopBits);