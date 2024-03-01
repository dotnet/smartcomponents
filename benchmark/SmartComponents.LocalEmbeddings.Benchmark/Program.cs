using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using SmartComponents.LocalEmbeddings.Benchmark;

Console.WriteLine($"Sample data contains {SampleData.SampleStrings.Length} lines");
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new FastRunConfig());
LogAllResultsToConsole();

static void LogAllResultsToConsole()
{
    // Emit the markdown output in a way that's easier to read in CI log output
    var markdownFiles = Directory.GetFiles(
        Path.Combine("BenchmarkDotNet.Artifacts", "results"),
        "*.md").OrderBy(x => x, StringComparer.Ordinal).ToArray();
    foreach (var filename in markdownFiles)
    {
        Console.WriteLine();
        Console.WriteLine("-----");
        Console.WriteLine();
        foreach (var line in File.ReadLines(filename).Where(l => l.StartsWith('|')))
        {
            Console.WriteLine(line);
        }
    }
}

Console.WriteLine();

public class FastRunConfig : ManualConfig
{
    public FastRunConfig()
    {
        Add(DefaultConfig.Instance);
        AddJob(Job.Default
            .WithLaunchCount(1)
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(3)
            .WithIterationCount(5));
    }
}
