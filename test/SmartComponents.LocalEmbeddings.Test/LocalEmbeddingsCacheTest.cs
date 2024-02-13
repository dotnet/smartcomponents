using System.Runtime.Caching;

namespace SmartComponents.LocalEmbeddings.Test;

public class LocalEmbeddingsCacheTest
{
    [Fact]
    public void CanFindClosestMatches()
    {
        using var embeddingsCache = new LocalEmbeddingsCache();

        string[] candidates = ["Tea", "Latte", "Coffee", "Cherryade", "David Hasselhoff"];
        var closest = embeddingsCache.FindClosest("beans", candidates);

        Assert.Equal(["Coffee", "Latte"], closest.Take(2).Select(x => x.Text));
    }

    [Fact]
    public void CanSpecifySimilarityThreshold()
    {
        using var embeddingsCache = new LocalEmbeddingsCache();

        string[] candidates = ["Tea", "Latte", "Coffee", "Cherryade", "David Hasselhoff"];
        var closest = embeddingsCache.FindClosest("coffee", candidates, 0.95f);

        Assert.Equal(["Coffee"], closest.Select(x => x.Text));
    }

    [Fact]
    public async Task CanSupplyMemoryCacheAndSpecifyCacheItemPolicy()
    {
        var memoryCache = new MemoryCache("test");
        var didCallRemovalCallback = false;
        using var embeddingsCache = new LocalEmbeddingsCache(cache: memoryCache)
        {
            CacheItemPolicy = new()
            {
                SlidingExpiration = TimeSpan.FromSeconds(1),
                RemovedCallback = _ => didCallRemovalCallback = true,
            }
        };

        string[] candidates = ["Phantom", "Sabre", "Fury", "Viper"];
        var closest = embeddingsCache.FindClosest("sword", candidates);
        Assert.Equal("Sabre", closest.First().Text);

        // See that the cache is populated
        Assert.Equal(candidates.Length, memoryCache.GetCount());

        // ... and that it evicts using the given policy
        // Note that the removal callback is only called when you make subsequent
        // calls into the cache, so we have to do that until it fires.
        for (var i = 0; i < 10 && memoryCache.GetCacheItem(candidates[0]) is not null; i++)
        {
            await Task.Delay(1000);
        }

        Assert.True(didCallRemovalCallback);
    }
}
