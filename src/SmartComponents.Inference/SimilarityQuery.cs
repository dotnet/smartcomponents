// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SmartComponents.Inference;

public readonly struct SimilarityQuery
{
    public string SearchText { get; init; }
    public int MaxResults { get; init; }
    public float? MinSimilarity { get; init; }
}
