using System.Collections.Generic;

namespace SmartComponents.Inference;

public interface ISimilarityMatcher
{
    IEnumerable<SimilarityResult> FindClosest(string target, IEnumerable<string> candidates, float? similarityThreshold = default);
}

public readonly struct SimilarityResult(string text, float similarity)
{
    public string Text => text;
    public float Similarity => similarity;
}
