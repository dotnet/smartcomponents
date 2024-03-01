using BenchmarkDotNet.Attributes;
using static SmartComponents.LocalEmbeddings.Benchmark.SampleData;

namespace SmartComponents.LocalEmbeddings.Benchmark.Benchmarks;

public class Quantization
{
    private readonly Memory<byte> bufferForEmbeddingI8 = new byte[388];
    private readonly Memory<byte> bufferForEmbeddingI1 = new byte[48];

    [Benchmark]
    public void QuantizeToI8()
        => EmbeddingI8.FromModelOutput(EmbeddingsF32[NextSampleIndex].Embedding.Values.Span, bufferForEmbeddingI8);

    [Benchmark]
    public void QuantizeToI1()
        => EmbeddingI1.FromModelOutput(EmbeddingsF32[NextSampleIndex].Embedding.Values.Span, bufferForEmbeddingI1);
}
