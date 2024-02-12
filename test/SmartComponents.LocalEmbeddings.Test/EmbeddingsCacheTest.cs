namespace SmartComponents.LocalEmbeddings.Test;

public class EmbeddingsCacheTest : IDisposable
{
    private readonly LocalEmbeddings localEmbeddings = new();
    private readonly EmbeddingsCache<ByteEmbedding> embeddingsCache;
    private readonly string[] testStrings =
    [
        "Tea", "Coffee", "Latte", "Cherryade", "David Hasselhoff"
    ];

    public EmbeddingsCacheTest()
    {
        embeddingsCache = new EmbeddingsCache<ByteEmbedding>();

        foreach (var text in testStrings)
        {
            embeddingsCache.TryAdd(text, localEmbeddings.EmbedAsBytes(text));
        }
    }

    [Fact]
    public void CanFindClosestMatches()
    {
        var target = localEmbeddings.EmbedAsBytes("beans");
        var closest = embeddingsCache.GetClosestMatches(target, maxResults: 2);

        Assert.Equal(["Coffee", "Latte"], closest.Select(x => x.Text));
    }

    [Fact]
    public void CanFindClosestMatchesWithSimilarityThreshold()
    {
        var target = localEmbeddings.EmbedAsBytes("beans");
        var closest = embeddingsCache.GetClosestMatches(target,
            maxResults: int.MaxValue,
            similarityThreshold: 0.7f);

        Assert.Equal(["Coffee"], closest.Select(x => x.Text));
    }

    [Fact]
    public void DefaultsToCaseInsensitiveKeys()
    {
        // Add an entry that differs from an existing one only by case
        var duplicateText = "cherryade";
        var duplicateEmbedding = localEmbeddings.EmbedAsBytes(duplicateText);
        Assert.False(embeddingsCache.TryAdd(duplicateText, duplicateEmbedding));

        // Show that the search results only see a single matching value
        var closest = embeddingsCache.GetClosestMatches(duplicateEmbedding, maxResults: int.MaxValue, similarityThreshold: 0.9f);
        Assert.Equal(["Cherryade"], closest.Select(x => x.Text));
    }

    [Fact]
    public void CanUseCaseSensitiveKeys()
    {
        var caseSensitiveCache = new EmbeddingsCache<ByteEmbedding>(StringComparer.Ordinal);
        foreach (var text in testStrings)
        {
            Assert.True(caseSensitiveCache.TryAdd(text, localEmbeddings.EmbedAsBytes(text)));
        }

        // Add an entry that differs from an existing one only by case
        var duplicateText = "cherryade";
        var duplicateEmbedding = localEmbeddings.EmbedAsBytes(duplicateText);
        Assert.True(caseSensitiveCache.TryAdd(duplicateText, duplicateEmbedding));

        // Show that the search results see both values
        var closest = caseSensitiveCache.GetClosestMatches(duplicateEmbedding, maxResults: int.MaxValue, similarityThreshold: 0.9f);
        var closestTexts = closest.Select(x => x.Text).ToArray();
        Assert.Equal(2, closestTexts.Length);
        Assert.Contains("cherryade", closestTexts, StringComparer.Ordinal);
        Assert.Contains("Cherryade", closestTexts, StringComparer.Ordinal);
    }

    public void Dispose()
    {
        localEmbeddings.Dispose();
    }
}
