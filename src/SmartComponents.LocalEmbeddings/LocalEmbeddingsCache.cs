using SmartComponents.Inference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace SmartComponents.LocalEmbeddings;

public class LocalEmbeddingsCache : ISimilarityMatcher, IDisposable
{
    private bool _shouldDisposeEmbeddings;
    private readonly LocalEmbeddings _embeddings;
    private readonly MemoryCache _cachedEmbeddings;

    public CacheItemPolicy CacheItemPolicy { private get; init; }
        = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(5) };

    public LocalEmbeddingsCache() : this(null, null)
    {
    }

    public LocalEmbeddingsCache(LocalEmbeddings? embeddings = null, MemoryCache? cache = null)
    {
        if (embeddings is null)
        {
            embeddings = new();
            _shouldDisposeEmbeddings = true;
        }

        _embeddings = embeddings;
        _cachedEmbeddings = cache ?? new MemoryCache(nameof(LocalEmbeddingsCache));
    }

    public void Dispose()
    {
        if (_shouldDisposeEmbeddings)
        {
            _embeddings.Dispose();
        }
    }

    public IEnumerable<SimilarityResult> FindClosest(string target, IEnumerable<string> candidates, float? similarityThreshold = default)
    {
        // Be sure not to add this to the cache since it's not a "candidate" and may be user-supplied
        var targetEmbedding = _embeddings.EmbedAsFloats(target);

        // The returned enumerable contains everything, so it's up to the caller to use Take() to limit the results.
        // We filter by similarityThreshold first to avoid having to sort things that will be discarded anyway.
        similarityThreshold ??= 0.5f;
        var results = candidates
            .Select(candidate => new SimilarityResult(candidate, targetEmbedding.Similarity(GetOrAddEmbedding(candidate))))
            .Where(result => result.Similarity >= similarityThreshold)
            .OrderByDescending(result => result.Similarity);
        return results.AsEnumerable();
    }

    private FloatEmbedding GetOrAddEmbedding(string text)
    {
        if (_cachedEmbeddings.Get(text) is FloatEmbedding result)
        {
            return result;
        }

        var computedEmbedding = _embeddings.EmbedAsFloats(text);
        _cachedEmbeddings.Add(text, computedEmbedding, CacheItemPolicy);
        return computedEmbedding;
    }
}
