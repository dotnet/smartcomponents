using BenchmarkDotNet.Attributes;
using static SmartComponents.LocalEmbeddings.Benchmark.SampleData;

namespace SmartComponents.LocalEmbeddings.Benchmark.Benchmarks;

public class ComputingEmbedding
{
    private readonly LocalEmbedder localEmbedder = new();

    [Benchmark]
    public void ComputeEmbedding()
        => localEmbedder.Embed<EmbeddingF32>(SampleStrings[NextSampleIndex]);
}
