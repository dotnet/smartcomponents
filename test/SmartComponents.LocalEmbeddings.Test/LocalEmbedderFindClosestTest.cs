namespace SmartComponents.LocalEmbeddings.Test;

public class LocalEmbedderFindClosestTest
{
    [Fact]
    public void CanFindClosestMatches_Static()
    {
        using var embedder = new LocalEmbedder();
        string[] candidates = ["Tea", "Latte", "Coffee", "Cherryade", "David Hasselhoff"];
        var embeddedCandidates = embedder.EmbedRange(candidates);

        var closest = LocalEmbedder.FindClosest(embedder.Embed("beans"), embeddedCandidates, 2);
        var closestWithScore = LocalEmbedder.FindClosestWithScore(embedder.Embed("beans"), embeddedCandidates, 2);

        Assert.Equal(["Coffee", "Latte"], closest.Take(2));
        Assert.Collection(closestWithScore.Take(2),
            result => { Assert.Equal("Coffee", result.Item); Assert.InRange(result.Similarity, 0, 1.01f); },
            result => { Assert.Equal("Latte", result.Item); Assert.InRange(result.Similarity, 0, 1.01f); });
    }

    [Fact]
    public void CanFindClosestMatches_Instance()
    {
        using var embedder = new LocalEmbedder();
        string[] candidates = ["Tea", "Latte", "Coffee", "Cherryade", "David Hasselhoff"];
        var embeddedCandidates = embedder.EmbedRange(candidates);

        var closest = embedder.FindClosest(new() { SearchText = "beans", MaxResults = 2 }, embeddedCandidates);
        var closestWithScore = embedder.FindClosestWithScore(new() { SearchText = "beans", MaxResults = 2 }, embeddedCandidates);

        Assert.Equal(["Coffee", "Latte"], closest.Take(2));
        Assert.Collection(closestWithScore.Take(2),
            result => { Assert.Equal("Coffee", result.Item); Assert.InRange(result.Similarity, 0, 1.01f); },
            result => { Assert.Equal("Latte", result.Item); Assert.InRange(result.Similarity, 0, 1.01f); });
    }

    [Fact]
    public void CanSpecifySimilarityThreshold_Static()
    {
        using var embedder = new LocalEmbedder();
        string[] candidates = ["Tea", "Latte", "Coffee", "Cherryade", "David Hasselhoff"];
        var embeddedCandidates = embedder.EmbedRange(candidates);

        var closest = LocalEmbedder.FindClosest(embedder.Embed("coffee"), embeddedCandidates, 2, minSimilarity: 0.95f);
        var closestWithScore = LocalEmbedder.FindClosestWithScore(embedder.Embed("coffee"), embeddedCandidates, 2, minSimilarity: 0.95f);

        Assert.Equal(["Coffee"], closest);
        Assert.Collection(closestWithScore,
            result => { Assert.Equal("Coffee", result.Item); Assert.InRange(result.Similarity, 0.95f, 1.01f); });
    }
}
