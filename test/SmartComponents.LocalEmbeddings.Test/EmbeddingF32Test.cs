// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Numerics.Tensors;
using System.Text.Json;

namespace SmartComponents.LocalEmbeddings.Test;

public class EmbeddingF32Test
{
    [Fact]
    public void CanLoadModel()
    {
        using var embedder = new LocalEmbedder();
        Assert.Equal(384, embedder.Embed("test").Values.Length);
    }

    [Fact]
    public void ProducesExpectedEmbeddingValues()
    {
        using var embedder = new LocalEmbedder();
        foreach (var (text, expectedEmbedding) in TestData.BgeMicroV2Samples)
        {
            var actualEmbedding = embedder.Embed(text);
            AssertCosineEqual(expectedEmbedding, actualEmbedding.Values);
            Assert.Equal(actualEmbedding.Values.Length * 4, actualEmbedding.Buffer.Length); // 4 bytes per value
            Assert.Equal(actualEmbedding.Buffer.Length, EmbeddingF32.GetBufferByteLength(actualEmbedding.Values.Length));
        }
    }

    [Fact]
    public void WorksCorrectlyWhenSharedAcrossManyThreads()
    {
        // It's threadsafe because OnnxRuntime itself is threadsafe, and we have a pool of tokenizers so
        // each unit of work is working against different buffers (when get reused on subsequent calls).
        using var embedder = new LocalEmbedder();
        var allKeys = TestData.BgeMicroV2Samples.Keys.ToArray();
        var allResults = new ConcurrentBag<(string, ReadOnlyMemory<float>)>();
        var threads = Enumerable.Range(0, 100).Select(i =>
        {
            var thread = new Thread(() =>
            {
                var input = allKeys[Random.Shared.Next(allKeys.Length)];
                var value = embedder.Embed<EmbeddingF32>(input);
                allResults.Add((input, value.Values));
            });
            thread.Start();
            return thread;
        }).ToArray();

        // Wait for them all
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Check the outputs are all correct
        foreach (var (input, actualEmbedding) in allResults)
        {
            var expectedEmbedding = TestData.BgeMicroV2Samples[input];
            AssertCosineEqual(expectedEmbedding, actualEmbedding);
        }
    }

    [Fact]
    public void Similarity_ItemsAreExactlyRelatedToThemselves()
    {
        using var embedder = new LocalEmbedder();
        var cat = embedder.Embed<EmbeddingF32>("cat");
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(cat, cat), 3));
    }

    [Fact]
    public void Similarity_CanSwapInputOrderAndGetSameResults()
    {
        using var embedder = new LocalEmbedder();
        var cat = embedder.Embed<EmbeddingF32>("cat");
        var dog = embedder.Embed<EmbeddingF32>("dog");
        Assert.Equal(
            LocalEmbedder.Similarity(cat, dog),
            LocalEmbedder.Similarity(dog, cat));
    }

    [Fact]
    public void Similarity_ProducesExpectedResults()
    {
        using var embedder = new LocalEmbedder();

        var cat = embedder.Embed<EmbeddingF32>("cat");
        string[] sentences = [
            "dog",
            "kitten!",
            "Cats are good",
            "Cats are bad",
            "Tiger",
            "Wolf",
            "Grimsby Town FC",
            "Elephants are here",
        ];
        var sentencesRankedBySimilarity = sentences.OrderByDescending(
            s => LocalEmbedder.Similarity(cat, embedder.Embed<EmbeddingF32>(s))).ToArray();

        Assert.Equal([
            "Cats are good",
            "kitten!",
            "Cats are bad",
            "Tiger",
            "dog",
            "Wolf",
            "Elephants are here",
            "Grimsby Town FC",
        ], sentencesRankedBySimilarity.AsSpan());
    }

    [Fact]
    public void CanRoundTripThroughJson()
    {
        using var embedder = new LocalEmbedder();
        var cat = embedder.Embed<EmbeddingF32>("cat");
        var json = JsonSerializer.Serialize(cat);
        var deserializedCat = JsonSerializer.Deserialize<EmbeddingF32>(json);

        Assert.Equal(cat.Values.ToArray(), deserializedCat.Values.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(cat, deserializedCat), 3));
    }

    [Fact]
    public void CanRoundTripThroughByteBuffer()
    {
        using var embedder = new LocalEmbedder();
        var cat1 = embedder.Embed<EmbeddingF32>("cat");
        var cat2 = new EmbeddingF32(cat1.Buffer.ToArray());

        Assert.Equal(cat1.Buffer.ToArray(), cat2.Buffer.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(cat1, cat2), 3));
    }

    private static void AssertCosineEqual(ReadOnlyMemory<float> expectedValues, ReadOnlyMemory<float> actualValues)
    {
        Assert.Equal(expectedValues.Length, actualValues.Length);

        // We're not looking for exact equality, since there are floating point imprecisions, and calculations
        // may vary across platforms or even runs on the same platform. However the cosine similarity should be
        // high to count as "equal" for the purpose of the tests.
        var cosineSimilarity = TensorPrimitives.CosineSimilarity(expectedValues.Span, actualValues.Span);
        Assert.InRange(MathF.Min(cosineSimilarity, 1f), 0.99f, 1f);

        // We don't make assertions about the exact values of individual vector components because in practice
        // they vary a lot between runs (e.g., by over 30% in some cases)
    }
}
