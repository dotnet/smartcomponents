// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics.Tensors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace SmartComponents.LocalEmbeddings.SemanticKernel.Test;

public class EmbeddingsTest
{
    [Fact]
    public async Task CanComputeEmbeddings()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddLocalTextEmbeddingGeneration();
        var kernel = builder.Build();

        var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();

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
        ], sentencesRankedBySimilarity);
    }

    [Fact]
    public async Task IsCaseInsensitiveByDefault()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddLocalTextEmbeddingGeneration();
        var kernel = builder.Build();

        var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
        var catLower = await embeddingGenerator.GenerateEmbeddingAsync("cat");
        var catUpper = await embeddingGenerator.GenerateEmbeddingAsync("CAT");
        var similarity = TensorPrimitives.CosineSimilarity(catLower.Span, catUpper.Span);
        Assert.Equal(1, MathF.Round(similarity, 3));
    }

    [Fact]
    public async Task CanBeConfiguredAsCaseSensitive()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddLocalTextEmbeddingGeneration(caseSensitive: true);
        var kernel = builder.Build();

        var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
        var catLower = await embeddingGenerator.GenerateEmbeddingAsync("cat");
        var catUpper = await embeddingGenerator.GenerateEmbeddingAsync("CAT");
        var similarity = TensorPrimitives.CosineSimilarity(catLower.Span, catUpper.Span);
        Assert.NotEqual(1, MathF.Round(similarity, 3));
    }
}
