// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Microsoft.SemanticKernel.Embeddings;

namespace SmartComponents.LocalEmbeddings;

public sealed partial class LocalEmbedder : IDisposable, ITextEmbeddingGenerationService
{
    private readonly BertOnnxTextEmbeddingGenerationService _embeddingGenerator;

    public IReadOnlyDictionary<string, object?> Attributes => _embeddingGenerator.Attributes;

    public LocalEmbedder(string modelName = "default", bool caseSensitive = false, int maximumTokens = 512)
        : this(modelName, new BertOnnxOptions { CaseSensitive = caseSensitive, MaximumTokens = maximumTokens })
    {
    }

    public LocalEmbedder(BertOnnxOptions options)
        : this("default", options)
    {
    }

    public LocalEmbedder(string modelName, BertOnnxOptions options)
    {
        _embeddingGenerator = BertOnnxTextEmbeddingGenerationService.Create(
            GetFullPathToModelFile(modelName, "model.onnx"),
            vocabPath: GetFullPathToModelFile(modelName, "vocab.txt"),
            options);
    }

    private static string GetFullPathToModelFile(string modelName, string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var fullPath = Path.Combine(baseDir, "LocalEmbeddingsModel", modelName, fileName);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Required file {fullPath} does not exist");
        }

        return fullPath;
    }

    public EmbeddingF32 Embed(string inputText)
        => Embed<EmbeddingF32>(inputText, null);

    public Task<EmbeddingF32> EmbedAsync(string inputText)
        => EmbedAsync<EmbeddingF32>(inputText, null);

    // This synchronous overload is for back-compat with older versions of LocalEmbeddings. It actually performs the same
    // at present since the underlying BertOnnxTextEmbeddingGenerationService completes synchronously in all cases (though
    // that's not guaranteed to remain the same forever).
    public TEmbedding Embed<TEmbedding>(string inputText, Memory<byte>? outputBuffer = default)
        where TEmbedding : IEmbedding<TEmbedding>
        => EmbedAsync<TEmbedding>(inputText, outputBuffer).Result;

    public async Task<TEmbedding> EmbedAsync<TEmbedding>(string inputText, Memory<byte>? outputBuffer = default)
        where TEmbedding : IEmbedding<TEmbedding>
    {
        var embedding = (await _embeddingGenerator.GenerateEmbeddingsAsync([inputText])).Single();
        return TEmbedding.FromModelOutput(embedding.Span, outputBuffer ?? new byte[TEmbedding.GetBufferByteLength(embedding.Span.Length)]);
    }

    // Note that all the following materialize the result as a list, even though the return type is IEnumerable<T>.
    // We don't want to recompute the embeddings every time the list is enumerated.

    public IList<(string Item, EmbeddingF32 Embedding)> EmbedRange(
        IEnumerable<string> items)
        => items.Select(item => (item, Embed<EmbeddingF32>(item))).ToList();

    public IEnumerable<(string Item, TEmbedding Embedding)> EmbedRange<TEmbedding>(
        IEnumerable<string> items)
        where TEmbedding : IEmbedding<TEmbedding>
        => items.Select(item => (item, Embed<TEmbedding>(item))).ToList();

    public IEnumerable<(TItem Item, EmbeddingF32 Embedding)> EmbedRange<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, string> textRepresentation)
        => items.Select(item => (item, Embed<EmbeddingF32>(textRepresentation(item)))).ToList();

    public IEnumerable<(TItem Item, TEmbedding Embedding)> EmbedRange<TItem, TEmbedding>(
        IEnumerable<TItem> items,
        Func<TItem, string> textRepresentation)
        where TEmbedding : IEmbedding<TEmbedding>
        => items.Select(item => (item, Embed<TEmbedding>(textRepresentation(item)))).ToList();

    public void Dispose()
        => _embeddingGenerator.Dispose();

    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null, CancellationToken cancellationToken = default)
        => _embeddingGenerator.GenerateEmbeddingsAsync(data, kernel, cancellationToken);
}
