using BenchmarkDotNet.Running;

namespace S7Tools.Benchmarks;

/// <summary>
/// Entry point for S7Tools performance benchmarks.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point - runs all benchmarks.
    /// </summary>
    /// <param name="args">Command line arguments passed to BenchmarkDotNet.</param>
    public static void Main(string[] args)
    {
        // Run all benchmarks in the assembly
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
