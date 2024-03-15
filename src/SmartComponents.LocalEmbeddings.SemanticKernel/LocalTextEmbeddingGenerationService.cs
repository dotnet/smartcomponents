// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace SmartComponents.LocalEmbeddings.SemanticKernel;

/// <summary>
/// A text embedding service that computes embeddings locally using <see cref="LocalEmbedder"/>.
/// </summary>
public class LocalTextEmbeddingGenerationService : ITextEmbeddingGenerationService, IDisposable
{
    private readonly LocalEmbedder _embedder;
    private readonly int _maximumTokens;

    /// <summary>
    /// Constructs an instance of <see cref="LocalTextEmbeddingGenerationService"/>.
    /// </summary>
    /// <param name="modelName">The name of the model to load. See documentation for <see cref="LocalEmbedder"/>.</param>
    /// <param name="caseSensitive">True if text should be handled case sensitively, otherwise false.</param>
    /// <param name="maximumTokens">The maximum number of tokens to include in the generated embeddings. This limits the amount of processing by truncating longer strings when the limit is reached..</param>
    public LocalTextEmbeddingGenerationService(string? modelName = default, bool caseSensitive = false, int maximumTokens = 512)
    {
        _embedder = new(modelName ?? "default", caseSensitive);
        _maximumTokens = maximumTokens;
    }

    // Attributes is unused
    private static readonly IReadOnlyDictionary<string, object?> _emptyDict = new Dictionary<string, object?>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes => _emptyDict;

    /// <inheritdoc />
    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        var results = new ReadOnlyMemory<float>[data.Count];
        for (var i = 0; i < data.Count; i++)
        {
            results[i] = _embedder.Embed(data[i], _maximumTokens).Values;
        }

        return Task.FromResult((IList<ReadOnlyMemory<float>>)results);
    }

    /// <inheritdoc />
    public void Dispose()
        => _embedder.Dispose();
}
