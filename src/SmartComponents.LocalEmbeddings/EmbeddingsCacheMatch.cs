namespace SmartComponents.LocalEmbeddings;

public readonly struct EmbeddingsCacheMatch(string text, float similarity)
{
    public string Text => text;
    public float Similarity => similarity;
}
