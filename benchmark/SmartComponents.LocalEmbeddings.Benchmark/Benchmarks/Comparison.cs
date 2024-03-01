using BenchmarkDotNet.Attributes;
using static SmartComponents.LocalEmbeddings.Benchmark.SampleData;

namespace SmartComponents.LocalEmbeddings.Benchmark.Benchmarks;

public class Comparison
{
    [Benchmark]
    public void CompareI8()
        => EmbeddingsI8[NextSampleIndex].Embedding.Similarity(EmbeddingsI8[NextSampleIndex].Embedding);

    [Benchmark]
    public void CompareI1()
        => EmbeddingsI1[NextSampleIndex].Embedding.Similarity(EmbeddingsI1[NextSampleIndex].Embedding);
}
