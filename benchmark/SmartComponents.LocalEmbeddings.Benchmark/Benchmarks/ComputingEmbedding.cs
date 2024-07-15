// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using static SmartComponents.LocalEmbeddings.Benchmark.SampleData;

namespace SmartComponents.LocalEmbeddings.Benchmark.Benchmarks;

public class ComputingEmbedding
{
    private readonly LocalEmbedder localEmbedder = new();

    [Benchmark]
    public void ComputeEmbedding()
        => localEmbedder.Embed<EmbeddingF32>(SampleStrings[NextSampleIndex]);

    [Benchmark]
    public void ComputeEmbeddingOfMassiveString()
        => localEmbedder.Embed<EmbeddingF32>(SampleData.LongString);
}
