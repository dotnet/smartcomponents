using System.Reflection;

namespace SmartComponents.LocalEmbeddings.Benchmark;

internal sealed class SampleData
{
    private static readonly string sampleDataPath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "sampledata.txt");

    private static long index;

    public readonly static string[] SampleStrings = File.ReadAllLines(
        sampleDataPath)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Take(1000)
        .ToArray();

    public readonly static IList<(string Item, EmbeddingF32 Embedding)> EmbeddingsF32
        = new LocalEmbedder().EmbedRange(SampleStrings);

    public readonly static IList<(string Item, EmbeddingI8 Embedding)> EmbeddingsI8
        = EmbeddingsF32.Select(x => (x.Item, EmbeddingI8.FromModelOutput(x.Embedding.Values.Span, new byte[EmbeddingI8.GetBufferByteLength(x.Embedding.Values.Length)]))).ToList();

    public readonly static IList<(string Item, EmbeddingI1 Embedding)> EmbeddingsI1
        = EmbeddingsF32.Select(x => (x.Item, EmbeddingI1.FromModelOutput(x.Embedding.Values.Span, new byte[EmbeddingI1.GetBufferByteLength(x.Embedding.Values.Length)]))).ToList();

    public static int NextSampleIndex
        => (int)(Interlocked.Increment(ref index) % SampleStrings.Length);
}
