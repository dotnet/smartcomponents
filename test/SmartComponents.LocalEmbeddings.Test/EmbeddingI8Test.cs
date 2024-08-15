// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics.Tensors;
using System.Text.Json;

namespace SmartComponents.LocalEmbeddings.Test;

public class EmbeddingI8Test
{
    // The correctness of the embeddings is covered in FloatEmbeddingTest. All we want to check here
    // is that the byte representation is what we expect in relation to the float representation,
    // and that cosine similarity still produces correct rankings.

    [Fact]
    public void ByteRepresentationIsSameAsFloatExceptScaled()
    {
        using var embedder = new LocalEmbedder();
        var testSentence = "This is my test sentence";
        var floats = embedder.Embed(testSentence);
        var bytes = embedder.Embed<EmbeddingI8>(testSentence);

        // Check it's the same length
        Assert.Equal(floats.Values.Length, bytes.Values.Length);
        Assert.Equal(bytes.Buffer.Length, bytes.Values.Length + 4); // 1 byte per value, plus 4 for magnitude
        Assert.Equal(bytes.Buffer.Length, EmbeddingI8.GetBufferByteLength(floats.Values.Length));

        // Work out how we expect the floats to be scaled
        var expectedScaleFactor = sbyte.MaxValue / Math.Abs(TensorPrimitives.MaxMagnitude(floats.Values.Span));
        var scaledFloats = new float[floats.Values.Length];
        TensorPrimitives.Multiply(floats.Values.Span, expectedScaleFactor, scaledFloats);

        // Check the bytes match this. We'll allow up to 1 off due to rounding differences.
        for (var i = 0; i < floats.Values.Length; i++)
        {
            var actualByte = bytes.Values.Span[i];
            var expectedByte = (sbyte)scaledFloats[i];
            Assert.InRange(actualByte, expectedByte - 1, expectedByte + 1);
        }
    }

    [Fact]
    public void Similarity_ItemsAreExactlyRelatedToThemselves()
    {
        using var embedder = new LocalEmbedder();
        var testSentence = "This is my test sentence";
        var values = embedder.Embed<EmbeddingI8>(testSentence);
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(values, values), 3));
    }

    [Fact]
    public void Similarity_CanSwapInputOrderAndGetSameResults()
    {
        using var embedder = new LocalEmbedder();
        var cat = embedder.Embed<EmbeddingI8>("cat");
        var dog = embedder.Embed<EmbeddingI8>("dog");
        Assert.Equal(
            LocalEmbedder.Similarity(cat, dog),
            LocalEmbedder.Similarity(dog, cat));
    }

    [Fact]
    public void Similarity_ProducesExpectedResults()
    {
        using var embedder = new LocalEmbedder();

        var cat = embedder.Embed<EmbeddingI8>("cat");
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
            s => LocalEmbedder.Similarity(cat, embedder.Embed<EmbeddingI8>(s))).ToArray();

        Assert.Equal([
            "Cats are good",
            "kitten!",
            "Cats are bad",
            "Tiger",
            "dog",
            "Wolf",
            "Elephants are here",
            "Grimsby Town FC",
        ], sentencesRankedBySimilarity);
    }

    [Fact]
    public void CanRoundTripThroughJson()
    {
        using var embedder = new LocalEmbedder();
        var cat = embedder.Embed<EmbeddingI8>("cat");
        var json = JsonSerializer.Serialize(cat);
        var deserializedCat = JsonSerializer.Deserialize<EmbeddingI8>(json);

        Assert.Equal(cat.Values.ToArray(), deserializedCat.Values.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(cat, deserializedCat), 3));
    }

    [Fact]
    public void CanRoundTripThroughByteBuffer()
    {
        using var embedder = new LocalEmbedder();
        var cat1 = embedder.Embed<EmbeddingI8>("cat");
        var cat2 = new EmbeddingI8(cat1.Buffer.ToArray());

        Assert.Equal(cat1.Buffer.ToArray(), cat2.Buffer.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbedder.Similarity(cat1, cat2), 3));
    }
}
