// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using SmartComponents.Inference;

namespace SmartComponents.LocalEmbeddings;

public partial class LocalEmbedder
{
    public static float Similarity<TEmbedding>(TEmbedding a, TEmbedding b) where TEmbedding : IEmbedding<TEmbedding>
        => a.Similarity(b);

    /// <summary>
    /// Finds the closest <paramref name="maxResults"/> candidates to <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of the items being searched</typeparam>
    /// <typeparam name="TEmbedding">The type of the embeddings</typeparam>
    /// <param name="query">Specifies the .</param>
    /// <param name="candidates">The set of possible results.</param>
    /// <param name="maxResults">Specifies an upper limit on the number of results. If no limit is required, pass <see cref="int.MaxValue"/>.</param>
    /// <param name="minSimilarity">Specifies a lower limit on the similarity ranking for matching results.</param>
    /// <returns>An ordered array of <typeparamref name="TItem"/> values, starting from the most similar.</returns>
    public TItem[] FindClosest<TItem, TEmbedding>(
        SimilarityQuery query,
        IEnumerable<(TItem Item, TEmbedding Embedding)> candidates) where TEmbedding : IEmbedding<TEmbedding>
        => FindClosestCore(Embed<TEmbedding>(query.SearchText), candidates, query.MaxResults, query.MinSimilarity).Select(x => x.Item).ToArray();

    /// <summary>
    /// Finds the closest <paramref name="maxResults"/> candidates to <paramref name="target"/>,
    /// returning both the similarity score and the corresponding value.
    /// </summary>
    /// <typeparam name="TItem">The type of the items being searched</typeparam>
    /// <typeparam name="TEmbedding">The type of the embeddings</typeparam>
    /// <param name="text">The text to be searched for.</param>
    /// <param name="candidates">The set of possible results.</param>
    /// <param name="maxResults">Specifies an upper limit on the number of results. If no limit is required, pass <see cref="int.MaxValue"/>.</param>
    /// <param name="minSimilarity">Specifies a lower limit on the similarity ranking for matching results.</param>
    /// <returns>An ordered array of <see cref="SimilarityScore{T}"/> values that specify the similarity along with the corresponding <typeparamref name="TItem"/> value.</returns>
    public SimilarityScore<TItem>[] FindClosestWithScore<TItem, TEmbedding>(
        SimilarityQuery query,
        IEnumerable<(TItem Item, TEmbedding Embedding)> candidates) where TEmbedding : IEmbedding<TEmbedding>
        => FindClosestCore(Embed<TEmbedding>(query.SearchText), candidates, query.MaxResults, query.MinSimilarity).ToArray();

    /// <summary>
    /// Finds the closest <paramref name="maxResults"/> candidates to <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of the items being searched</typeparam>
    /// <typeparam name="TEmbedding">The type of the embeddings</typeparam>
    /// <param name="target">An embedding representing the value to be searched for.</param>
    /// <param name="candidates">The set of possible results.</param>
    /// <param name="maxResults">Specifies an upper limit on the number of results. If no limit is required, pass <see cref="int.MaxValue"/>.</param>
    /// <param name="minSimilarity">Specifies a lower limit on the similarity ranking for matching results.</param>
    /// <returns>An ordered array of <typeparamref name="TItem"/> values, starting from the most similar.</returns>
    public static TItem[] FindClosest<TItem, TEmbedding>(
        TEmbedding target,
        IEnumerable<(TItem Item, TEmbedding Embedding)> candidates,
        int maxResults,
        float? minSimilarity = null) where TEmbedding : IEmbedding<TEmbedding>
        => FindClosestCore(target, candidates, maxResults, minSimilarity).Select(x => x.Item).ToArray();

    /// <summary>
    /// Finds the closest <paramref name="maxResults"/> candidates to <paramref name="target"/>,
    /// returning both the similarity score and the corresponding value.
    /// </summary>
    /// <typeparam name="TItem">The type of the items being searched</typeparam>
    /// <typeparam name="TEmbedding">The type of the embeddings</typeparam>
    /// <param name="target">An embedding representing the value to be searched for.</param>
    /// <param name="candidates">The set of possible results.</param>
    /// <param name="maxResults">Specifies an upper limit on the number of results. If no limit is required, pass <see cref="int.MaxValue"/>.</param>
    /// <param name="minSimilarity">Specifies a lower limit on the similarity ranking for matching results.</param>
    /// <returns>An ordered array of <see cref="SimilarityScore{T}"/> values that specify the similarity along with the corresponding <typeparamref name="TItem"/> value.</returns>
    public static SimilarityScore<TItem>[] FindClosestWithScore<TItem, TEmbedding>(
        TEmbedding target,
        IEnumerable<(TItem Item, TEmbedding Embedding)> candidates,
        int maxResults,
        float? minSimilarity = null) where TEmbedding : IEmbedding<TEmbedding>
        => FindClosestCore(target, candidates, maxResults, minSimilarity).ToArray();

    private static SortedSet<SimilarityScore<TItem>> FindClosestCore<TItem, TEmbedding>(
        TEmbedding target,
        IEnumerable<(TItem Item, TEmbedding Embedding)> candidates,
        int maxResults,
        float? minSimilarity = null) where TEmbedding : IEmbedding<TEmbedding>
    {
        if (maxResults <= 0)
        {
            throw new ArgumentException($"{maxResults} must be greater than 0.");
        }

        var sortedTopK = new SortedSet<SimilarityScore<TItem>>(SimilarityScore<TItem>.Comparer);
        var candidatesEnumerator = candidates.GetEnumerator();
        var index = 0L;
        minSimilarity = minSimilarity ?? float.MinValue;

        // Populate the results with the first K candidates
        while (sortedTopK.Count < maxResults && candidatesEnumerator.MoveNext())
        {
            var candidate = candidatesEnumerator.Current;
            var similarity = target.Similarity(candidate.Embedding);
            if (similarity >= minSimilarity)
            {
                sortedTopK.Add(new SimilarityScore<TItem>(similarity, candidate.Item, index++));
            }
        }

        // Add remaining candidates only if they are better than the worst so far
        while (candidatesEnumerator.MoveNext())
        {
            var candidate = candidatesEnumerator.Current;
            var similarity = target.Similarity(candidate.Embedding);

            // By this point we know there's a nonzero number of elements in the set
            // so we can just compare against the worst so far ("Max"), and can ignore
            // the minSimilarity threshold
            if (similarity > sortedTopK.Max.Similarity)
            {
                sortedTopK.Remove(sortedTopK.Max);
                sortedTopK.Add(new SimilarityScore<TItem>(similarity, candidate.Item, index++));
            }
        }

        return sortedTopK;
    }
}
