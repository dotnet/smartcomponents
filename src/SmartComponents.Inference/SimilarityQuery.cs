namespace SmartComponents.Inference;

public readonly struct SimilarityQuery
{
    public string SearchText { get; init; }
    public int MaxResults { get; init; }
    public float? MinSimilarity { get; init; }
}
