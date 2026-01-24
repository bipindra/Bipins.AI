using BenchmarkDotNet.Running;
using Bipins.AI.Benchmarks;

namespace Bipins.AI.Benchmarks;

/// <summary>
/// Entry point for performance benchmarks.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
