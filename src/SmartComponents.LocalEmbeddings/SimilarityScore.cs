// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace SmartComponents.LocalEmbeddings;

public readonly struct SimilarityScore<T>(float similarity, T item)
{
    public float Similarity => similarity;
    public T Item => item;

    // This is to ensure results are considered distinct during FindClosest operations,
    // because SortedSet doesn't allow duplicates. When we try to remove the worst result
    // from the SortedSet, we need it to remove only 1 item, not all with the same similarity.
    private readonly long uniqueIndex;

    internal SimilarityScore(float similarity, T item, long uniqueIndex) : this(similarity, item)
    {
        this.uniqueIndex = uniqueIndex;
    }

    internal static readonly IComparer<SimilarityScore<T>> Comparer = Comparer<SimilarityScore<T>>.Create((a, b) =>
    {
        var comparison = b.Similarity.CompareTo(a.Similarity);
        return comparison == 0
            ? a.uniqueIndex.CompareTo(b.uniqueIndex)
            : comparison;
    });
}
