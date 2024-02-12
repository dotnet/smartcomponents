using System.Numerics.Tensors;
using System.Text.Json;

namespace SmartComponents.LocalEmbeddings.Test;

public class EmbedAsBytesTest
{
    // The correctness of the embeddings is covered in EmbedAsFloatsTest. All we want to check here
    // is that the byte representation is what we expect in relation to the float representation,
    // and that cosine similarity still produces correct rankings.

    [Fact]
    public void ByteRepresentationIsSameAsFloatExceptScaled()
    {
        using var embeddings = new LocalEmbeddings();
        var testSentence = "This is my test sentence";
        var floats = embeddings.EmbedAsFloats(testSentence);
        var bytes = embeddings.EmbedAsBytes(testSentence);

        // Check it's the same length
        Assert.Equal(floats.Values.Length, bytes.Values.Length);
        Assert.Equal(bytes.Values.Length, bytes.ByteLength); // 1 byte per value

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
        using var embeddings = new LocalEmbeddings();
        var testSentence = "This is my test sentence";
        var values = embeddings.EmbedAsBytes(testSentence);
        Assert.Equal(1, LocalEmbeddings.Similarity(values, values));
    }

    [Fact]
    public void Similarity_CanSwapInputOrderAndGetSameResults()
    {
        using var embeddings = new LocalEmbeddings();
        var cat = embeddings.EmbedAsBytes("cat");
        var dog = embeddings.EmbedAsBytes("dog");
        Assert.Equal(
            LocalEmbeddings.Similarity(cat, dog),
            LocalEmbeddings.Similarity(dog, cat));
    }

    [Fact]
    public void Similarity_ProducesExpectedResults()
    {
        using var embeddings = new LocalEmbeddings();

        var cat = embeddings.EmbedAsBytes("cat");
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
            s => LocalEmbeddings.Similarity(cat, embeddings.EmbedAsBytes(s))).ToArray();

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
        using var embeddings = new LocalEmbeddings();
        var cat = embeddings.EmbedAsBytes("cat");
        var json = JsonSerializer.Serialize(cat);
        var deserializedCat = JsonSerializer.Deserialize<ByteEmbedding>(json);

        Assert.Equal(cat.Values.ToArray(), deserializedCat.Values.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbeddings.Similarity(cat, deserializedCat), 3));
    }
}
