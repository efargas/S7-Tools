// ResourceKey.cs
namespace S7Tools.Core.Models.Jobs;
public readonly record struct ResourceKey(string Kind, string Id) {
    public override string ToString() => $"{Kind}:{Id}";
}