// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace SmartComponents.LocalEmbeddings.Benchmark;

internal sealed class SampleData
{
    private static readonly string sampleDataPath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "sampledata.txt");

    private static long index;

    public static readonly string LongString = File.ReadAllText(sampleDataPath);

    public static readonly string[] SampleStrings = LongString
        .Split('\n')
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Take(1000)
        .ToArray();

    public static readonly IList<(string Item, EmbeddingF32 Embedding)> EmbeddingsF32
        = new LocalEmbedder().EmbedRange(SampleStrings);

    public static readonly IList<(string Item, EmbeddingI8 Embedding)> EmbeddingsI8
        = EmbeddingsF32.Select(x => (x.Item, EmbeddingI8.FromModelOutput(x.Embedding.Values.Span, new byte[EmbeddingI8.GetBufferByteLength(x.Embedding.Values.Length)]))).ToList();

    public static readonly IList<(string Item, EmbeddingI1 Embedding)> EmbeddingsI1
        = EmbeddingsF32.Select(x => (x.Item, EmbeddingI1.FromModelOutput(x.Embedding.Values.Span, new byte[EmbeddingI1.GetBufferByteLength(x.Embedding.Values.Length)]))).ToList();

    public static int NextSampleIndex
        => (int)(Interlocked.Increment(ref index) % SampleStrings.Length);
}
