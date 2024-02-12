using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartComponents.LocalEmbeddings;

/// <summary>
/// Provides a simple in-memory store for text embeddings that can be queried to find similar matches.
///
/// Since the search algorithm is linear, it is recommended only to use this for small
/// collections. Consider using a vector database for larger volumes of data.
/// </summary>
public class EmbeddingsCache<T> where T: IEmbedding<T>
{
    private readonly ConcurrentDictionary<string, T> _cachedEmbeddings;

    public EmbeddingsCache(IEqualityComparer<string>? keyComparer = null)
    {
        _cachedEmbeddings = new ConcurrentDictionary<string, T>(
            keyComparer ?? StringComparer.CurrentCultureIgnoreCase);
    }

    public bool TryAdd(string text, T embedding)
        => _cachedEmbeddings.TryAdd(text, embedding);

    public IReadOnlyList<EmbeddingsCacheMatch> GetClosestMatches(T embedding, int maxResults, float similarityThreshold = 0.5f)
    {
        return _cachedEmbeddings
            .Select(kvp => new EmbeddingsCacheMatch(kvp.Key, T.Similarity(embedding, kvp.Value)))
            .Where(candidate => candidate.Similarity >= similarityThreshold)
            .OrderByDescending(result => result.Similarity)
            .Take(maxResults)
            .ToList();
    }
}
