// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics.Tensors;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;

namespace SmartComponents.LocalEmbeddings.SemanticKernel.Test;

public class EmbeddingsTest
{
    [Fact]
    public async Task CanComputeEmbeddings()
    {
        ITextEmbeddingGenerationService embeddingGenerator = new LocalEmbedder();

        var cat = await embeddingGenerator.GenerateEmbeddingAsync("cat");
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
        var sentenceEmbeddings = await embeddingGenerator.GenerateEmbeddingsAsync(sentences);
        var sentencesWithEmbeddings = sentences.Zip(sentenceEmbeddings, (s, e) => (Sentence: s, Embedding: e)).ToArray();

        var sentencesRankedBySimilarity = sentencesWithEmbeddings
            .OrderByDescending(s => TensorPrimitives.CosineSimilarity(cat.Span, s.Embedding.Span))
            .Select(s => s.Sentence)
            .ToArray();

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
    public async Task IsCaseInsensitiveByDefault()
    {
        ITextEmbeddingGenerationService embeddingGenerator = new LocalEmbedder();

        var catLower = await embeddingGenerator.GenerateEmbeddingAsync("cat");
        var catUpper = await embeddingGenerator.GenerateEmbeddingAsync("CAT");
        var similarity = TensorPrimitives.CosineSimilarity(catLower.Span, catUpper.Span);
        Assert.Equal(1, MathF.Round(similarity, 3));
    }

    [Fact]
    public async Task CanBeConfiguredAsCaseSensitive()
    {
        ITextEmbeddingGenerationService embeddingGenerator = new LocalEmbedder(caseSensitive: true);

        var catLower = await embeddingGenerator.GenerateEmbeddingAsync("cat");
        var catUpper = await embeddingGenerator.GenerateEmbeddingAsync("CAT");
        var similarity = TensorPrimitives.CosineSimilarity(catLower.Span, catUpper.Span);
        Assert.NotEqual(1, MathF.Round(similarity, 3));
    }

    [Fact]
    public async Task CanBeUsedWithSemanticTextMemory()
    {
        // Construct an in-memory SK SemanticTextMemory that uses LocalEmbedder
        var storage = new VolatileMemoryStore();
        using var embedder = new LocalEmbedder();
        var semanticTextMemory = new SemanticTextMemory(storage, embedder);

        // Populate the memory with some information
        await semanticTextMemory.SaveInformationAsync("animals", "Dog", "id_1");
        await semanticTextMemory.SaveInformationAsync("animals", "Cat", "id_2");
        await semanticTextMemory.SaveInformationAsync("animals", "Biscuit", "id_3");

        // Do a nearest-neighbour search
        MemoryQueryResult? first = null;
        await foreach (var item in semanticTextMemory.SearchAsync("animals", "Kitten"))
        {
            first = item;
            break;
        }

        // See that "Cat" was the closest to "Kitten"
        Assert.Equal("id_2", first?.Metadata.Id);
        Assert.Equal("Cat", first?.Metadata.Text);
    }
}
