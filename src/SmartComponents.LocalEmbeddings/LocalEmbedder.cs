// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics.Tensors;
using System.Reflection;
using System.Runtime.InteropServices;
using FastBertTokenizer;
using Microsoft.Extensions.ObjectPool;
using Microsoft.ML.OnnxRuntime;

namespace SmartComponents.LocalEmbeddings;

public sealed partial class LocalEmbedder : IDisposable
{
    private static readonly ArrayPool<float> _outputBufferPool = ArrayPool<float>.Create();

    private readonly ObjectPool<BertTokenizer> _tokenizersPool;
    private readonly InferenceSession _onnxSession;
    private readonly RunOptions _runOptions = new RunOptions();

    public int Dimensions { get; }

    public LocalEmbedder(string modelName = "default", bool caseSensitive = false)
    {
        _onnxSession = new InferenceSession(GetFullPathToModelFile(modelName, "model.onnx"));
        _tokenizersPool = new DefaultObjectPool<BertTokenizer>(new BertTokenizerPoolPolicy(modelName, caseSensitive), maximumRetained: 32);
        Dimensions = _onnxSession.OutputMetadata.First().Value.Dimensions.Last(); // 384 for the supported models
    }

    private static string GetFullPathToModelFile(string modelName, string fileName)
    {
        var assembly = Assembly.GetEntryAssembly()!;
        var baseDir = Path.GetDirectoryName(assembly.Location)!;
        var fullPath = Path.Combine(baseDir, "LocalEmbeddingsModel", modelName, fileName);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Required file {fullPath} does not exist");
        }

        return fullPath;
    }

    const int DefaultMaximumTokens = 512;

    public EmbeddingF32 Embed(string inputText, int maximumTokens = DefaultMaximumTokens)
        => Embed<EmbeddingF32>(inputText, null, maximumTokens);

    public TEmbedding Embed<TEmbedding>(string inputText, Memory<byte>? outputBuffer = default, int maximumTokens = DefaultMaximumTokens)
        where TEmbedding : IEmbedding<TEmbedding>
    {
        var tokenizer = _tokenizersPool.Get();
        try
        {
            // While you might think you could return the tokenizer to the pool immediately after getting the tokens,
            // you actually can't because it reuses the same memory for the tokens each time. So we have to keep it until
            // we've finished getting the final result
            var tokens = tokenizer.Encode(inputText, maximumTokens: maximumTokens);

            var inputIdsOrtValue = OrtValue.CreateTensorValueFromMemory(
                OrtMemoryInfo.DefaultInstance,
                MemoryMarshal.AsMemory(tokens.InputIds),
                [1L, tokens.InputIds.Length]);
            var attMaskOrtValue = OrtValue.CreateTensorValueFromMemory(
                OrtMemoryInfo.DefaultInstance,
                MemoryMarshal.AsMemory(tokens.AttentionMask),
                [1, tokens.AttentionMask.Length]);
            var typeIdsOrtValue = OrtValue.CreateTensorValueFromMemory(
                OrtMemoryInfo.DefaultInstance,
                MemoryMarshal.AsMemory(tokens.TokenTypeIds),
                [1, tokens.TokenTypeIds.Length]);

            var inputs = new Dictionary<string, OrtValue>
            {
                { "input_ids", inputIdsOrtValue },
                { "attention_mask", attMaskOrtValue },
                { "token_type_ids", typeIdsOrtValue }
            };

            // InferenceSession.Run is thread-safe as per https://github.com/microsoft/onnxruntime/issues/114
            // so there's no need to maintain some kind of pool of sessions
            using var outputs = _onnxSession.Run(_runOptions, inputs, _onnxSession.OutputNames);

            return PoolSum<TEmbedding>(
                outputs[0].GetTensorDataAsSpan<float>(),
                Dimensions,
                outputBuffer ?? new byte[TEmbedding.GetBufferByteLength(Dimensions)]);
        }
        finally
        {
            _tokenizersPool.Return(tokenizer);
        }
    }

    private static TEmbedding PoolSum<TEmbedding>(ReadOnlySpan<float> input, int outputDimensions, Memory<byte> resultBuffer)
        where TEmbedding : IEmbedding<TEmbedding>
    {
        if (input.Length % outputDimensions != 0)
        {
            throw new ArgumentException($"Input length ({input.Length}) must be a multiple of output dimensions ({outputDimensions}), but is not", nameof(input));
        }

        var floatBuffer = _outputBufferPool.Rent(outputDimensions);
        try
        {
            var floatBufferSpan = floatBuffer.AsSpan(0, outputDimensions);
            for (var pos = 0; pos < input.Length; pos += outputDimensions)
            {
                var tokenEmbedding = input.Slice(pos, outputDimensions);
                TensorPrimitives.Add(floatBufferSpan, tokenEmbedding, floatBufferSpan);
            }

            return TEmbedding.FromModelOutput(floatBufferSpan, resultBuffer);
        }
        finally
        {
            _outputBufferPool.Return(floatBuffer, clearArray: true);
        }
    }

    public void Dispose()
    {
        _runOptions?.Dispose();
        _onnxSession?.Dispose();
    }

    // Note that all the following materialize the result as a list, even though the return type is IEnumerable<T>.
    // We don't want to recompute the embeddings every time the list is enumerated.

    public IList<(string Item, EmbeddingF32 Embedding)> EmbedRange(
        IEnumerable<string> items,
        int maximumTokens = DefaultMaximumTokens)
        => items.Select(item => (item, Embed<EmbeddingF32>(item, maximumTokens: maximumTokens))).ToList();

    public IEnumerable<(string Item, TEmbedding Embedding)> EmbedRange<TEmbedding>(
        IEnumerable<string> items,
        int maximumTokens = DefaultMaximumTokens)
        where TEmbedding : IEmbedding<TEmbedding>
        => items.Select(item => (item, Embed<TEmbedding>(item, maximumTokens: maximumTokens))).ToList();

    public IEnumerable<(TItem Item, EmbeddingF32 Embedding)> EmbedRange<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, string> textRepresentation,
        int maximumTokens = DefaultMaximumTokens)
        => items.Select(item => (item, Embed<EmbeddingF32>(textRepresentation(item), maximumTokens: maximumTokens))).ToList();

    public IEnumerable<(TItem Item, TEmbedding Embedding)> EmbedRange<TItem, TEmbedding>(
        IEnumerable<TItem> items,
        Func<TItem, string> textRepresentation,
        int maximumTokens = DefaultMaximumTokens)
        where TEmbedding : IEmbedding<TEmbedding>
        => items.Select(item => (item, Embed<TEmbedding>(textRepresentation(item), maximumTokens: maximumTokens))).ToList();
}
