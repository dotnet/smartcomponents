using System.Collections.Concurrent;
using System.Text.Json;

namespace SmartComponents.LocalEmbeddings.Test;

public class EmbedAsFloatsTest
{
    [Fact]
    public void CanLoadModel()
    {
        using var embeddings = new LocalEmbeddings();
        Assert.Equal(384, embeddings.OutputLength);
    }

    [Fact]
    public void ProducesExpectedEmbeddingValues()
    {
        using var embeddings = new LocalEmbeddings();
        foreach (var (text, expectedEmbedding) in TestData.BgeMicroV2Samples)
        {
            var actualEmbedding = embeddings.EmbedAsFloats(text);
            AssertEqualEmbeddings(expectedEmbedding, actualEmbedding.Values);
            Assert.Equal(actualEmbedding.Values.Length * 4, actualEmbedding.ByteLength); // 4 bytes per value
        }
    }

    [Fact]
    public void WorksCorrectlyWhenSharedAcrossManyThreads()
    {
        // It's threadsafe because OnnxRuntime itself is threadsafe, and we have a pool of tokenizers so
        // each unit of work is working against different buffers (when get reused on subsequent calls).
        using var embeddings = new LocalEmbeddings();
        var allKeys = TestData.BgeMicroV2Samples.Keys.ToArray();
        var allResults = new ConcurrentBag<(string, ReadOnlyMemory<float>)>();
        var threads = Enumerable.Range(0, 100).Select(i =>
        {
            var thread = new Thread(() =>
            {
                var input = allKeys[Random.Shared.Next(allKeys.Length)];
                var value = embeddings.EmbedAsFloats(input);
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
            AssertEqualEmbeddings(expectedEmbedding, actualEmbedding);
        }
    }

    [Fact]
    public void Similarity_ItemsAreExactlyRelatedToThemselves()
    {
        using var embeddings = new LocalEmbeddings();
        var cat = embeddings.EmbedAsFloats("cat");
        Assert.Equal(1, MathF.Round(LocalEmbeddings.Similarity(cat, cat), 3));
    }

    [Fact]
    public void Similarity_CanSwapInputOrderAndGetSameResults()
    {
        using var embeddings = new LocalEmbeddings();
        var cat = embeddings.EmbedAsFloats("cat");
        var dog = embeddings.EmbedAsFloats("dog");
        Assert.Equal(
            LocalEmbeddings.Similarity(cat, dog),
            LocalEmbeddings.Similarity(dog, cat));
    }

    [Fact]
    public void Similarity_ProducesExpectedResults()
    {
        using var embeddings = new LocalEmbeddings();

        var cat = embeddings.EmbedAsFloats("cat");
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
            s => LocalEmbeddings.Similarity(cat, embeddings.EmbedAsFloats(s))).ToArray();

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
        var cat = embeddings.EmbedAsFloats("cat");
        var json = JsonSerializer.Serialize(cat);
        var deserializedCat = JsonSerializer.Deserialize<FloatEmbedding>(json);

        Assert.Equal(cat.Values.ToArray(), deserializedCat.Values.ToArray());
        Assert.Equal(1, MathF.Round(LocalEmbeddings.Similarity(cat, deserializedCat), 3));
    }

    private static void AssertEqualEmbeddings(ReadOnlyMemory<float> expectedValues, ReadOnlyMemory<float> actualValues)
    {
        Assert.Equal(expectedValues.Length, actualValues.Length);
        for (var i = 0; i < actualValues.Length; i++)
        {
            // Compare to 2 signficant digits, since the test data was only output to a certain precision.
            var actual = actualValues.Span[i].ToString("G2");
            var expected = expectedValues.Span[i].ToString("G2");
            Assert.Equal(expected, actual);
        }
    }
}
