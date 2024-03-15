// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Embeddings;
using SmartComponents.LocalEmbeddings.SemanticKernel;

namespace Microsoft.SemanticKernel;

public static class LocalTextEmbeddingKernelBuilderServiceCollectionExtensions
{
    /// <summary>
    /// Adds a local text embedding generation service.
    /// </summary>
    /// <param name="builder">The <see cref="IKernelBuilder"/> instance to augment.</param>
    /// <param name="modelName">The name of the model to load. See documentation for <see cref="LocalEmbedder"/>.</param>
    /// <param name="caseSensitive">True if text should be handled case sensitively, otherwise false.</param>
    /// <param name="maximumTokens">The maximum number of tokens to include in the generated embeddings. This limits the amount of processing by truncating longer strings when the limit is reached.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static IKernelBuilder AddLocalTextEmbeddingGeneration(
        this IKernelBuilder builder,
        string? modelName = default,
        bool caseSensitive = false,
        int maximumTokens = 512)
    {
        var instance = new LocalTextEmbeddingGenerationService(modelName, caseSensitive, maximumTokens);
        builder.Services.AddSingleton<ITextEmbeddingGenerationService>(instance);
        return builder;
    }
}
