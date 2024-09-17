// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace SmartComponents.LocalEmbeddings.Test;

public class EmbeddingI1Test
{
    [Fact]
    public void BitRepresentationIsSameAsFloatsButThresholdedAtZero()
    {
        using var embedder = new LocalEmbedder();
        var testSentence = "This is my test sentence";
        var floats = embedder.Embed(testSentence);
        var bitRepresentation = embedder.Embed<EmbeddingI1>(testSentence);

        // Check it's the same length (1 byte per 8 values)
        Assert.Equal(floats.Values.Length / 8, bitRepresentation.Buffer.Length);
        Assert.Equal(bitRepresentation.Buffer.Length, EmbeddingI1.GetBufferByteLength(floats.Values.Length));

        // Check the bits match the thresholded floats
        for (var i = 0; i < floats.Values.Length; i++)
        {
            var expectedBitIsSet = floats.Values.Span[i] >= 0;
            var actualByte = bitRepresentation.Buffer.Span[i / 8];
            var actualBitIsSet = (actualByte & (1 << (7 - (i % 8)))) != 0;
            Assert.Equal(expectedBitIsSet, actualBitIsSet);
        }
    }

    [Fact]
    public void Similarity_ItemsAreExactlyRelatedToThemselves()
    {
        using var embedder = new LocalEmbedder();
        var testSentence = "This is my test sentence";
        var values = embedder.Embed<EmbeddingI1>(testSentence);
        Assert.Equal(1, LocalEmbedder.Similarity(values, values));
    }

    [Fact]
    public void Similarity_CanSwapInputOrderAndGetSameResults()
    {
        using var embedder = new LocalEmbedder();
        var cat = embedder.Embed<EmbeddingI1>("cat");
        var dog = embedder.Embed<EmbeddingI1>("dog");
        Assert.Equal(
            LocalEmbedder.Similarity(cat, dog),
            LocalEmbedder.Similarity(dog, cat));
    }

    [Fact]
    public void Similarity_ProducesExpectedResults()
    {
        using var embedder = new LocalEmbedder();

        var cat = embedder.Embed<EmbeddingI1>("cat");
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
            s => LocalEmbedder.Similarity(cat, embedder.Embed<EmbeddingI1>(s))).ToArray();

        // This ordering is close to, but not exactly the same as, the true ordering produced by the unquantized embeddings
        Assert.Equal([
            "kitten!",
            "Cats are good",
            "Cats are bad",
            "Tiger",
            "dog",
            "Elephants are here",
            "Wolf",
            "Grimsby Town FC",
        ], sentencesRankedBySimilarity.AsSpan());
    }

    [Fact]
    public void CanRoundTripThroughJson()
    {
        using var embedder = new LocalEmbedder();
        var cat = embedder.Embed<EmbeddingI1>("cat");
        var json = JsonSerializer.Serialize(cat);
        var deserializedCat = JsonSerializer.Deserialize<EmbeddingI1>(json);

        Assert.Equal(cat.Buffer.ToArray(), deserializedCat.Buffer.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(cat, deserializedCat), 3));
    }

    [Fact]
    public void CanRoundTripThroughByteBuffer()
    {
        using var embedder = new LocalEmbedder();
        var cat1 = embedder.Embed<EmbeddingI1>("cat");
        var cat2 = new EmbeddingI1(cat1.Buffer.ToArray());

        Assert.Equal(cat1.Buffer.ToArray(), cat2.Buffer.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(cat1, cat2), 3));
    }
}
