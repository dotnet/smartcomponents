using BenchmarkDotNet.Attributes;
using static SmartComponents.LocalEmbeddings.Benchmark.SampleData;

namespace SmartComponents.LocalEmbeddings.Benchmark.Benchmarks;

public class FindClosest
{
    [Benchmark]
    public void FindClosestF32()
        => LocalEmbedder.FindClosest(EmbeddingsF32[NextSampleIndex].Embedding, EmbeddingsF32, 10);

    [Benchmark]
    public void FindClosestI8()
        => LocalEmbedder.FindClosest(EmbeddingsI8[NextSampleIndex].Embedding, EmbeddingsI8, 10);

    [Benchmark]
    public void FindClosestI1()
        => LocalEmbedder.FindClosest(EmbeddingsI1[NextSampleIndex].Embedding, EmbeddingsI1, 10);
}
