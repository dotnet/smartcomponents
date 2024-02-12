using FastBertTokenizer;
using Microsoft.Extensions.ObjectPool;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics.Tensors;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SmartComponents.LocalEmbeddings;

public partial class LocalEmbeddings : IDisposable
{
    private static readonly ArrayPool<float> _outputBufferPool = ArrayPool<float>.Create();

    private readonly ObjectPool<BertTokenizer> _tokenizersPool;
    private readonly InferenceSession _onnxSession;
    private readonly RunOptions _runOptions = new RunOptions();

    public int OutputLength { get; }

    public LocalEmbeddings(string modelName = "default", bool caseSensitive = false)
    {
        _onnxSession = new InferenceSession(GetFullPathToModelFile(modelName, "model.onnx"));
        _tokenizersPool = new DefaultObjectPool<BertTokenizer>(new BertTokenizerPoolPolicy(modelName, caseSensitive), maximumRetained: 32);
        OutputLength = _onnxSession.OutputMetadata.First().Value.Dimensions.Last(); // 384 for the supported models
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

    public ByteEmbedding EmbedAsBytes(string inputText, Memory<sbyte>? outputBuffer = null, int maximumTokens = 512)
        => ComputeForType<ByteEmbedding, sbyte>(inputText, outputBuffer ?? new sbyte[OutputLength], maximumTokens);

    public FloatEmbedding EmbedAsFloats(string inputText, Memory<float>? outputBuffer = null, int maximumTokens = 512)
        => ComputeForType<FloatEmbedding, float>(inputText, outputBuffer ?? new float[OutputLength], maximumTokens);

    public static float Similarity(ByteEmbedding a, ByteEmbedding b)
        => ByteEmbedding.Similarity(a, b);

    public static float Similarity(FloatEmbedding a, FloatEmbedding b)
        => FloatEmbedding.Similarity(a, b);

    private TEmbedding ComputeForType<TEmbedding, TData>(string inputText, Memory<TData> resultBuffer, int maximumTokens)
        where TEmbedding: IEmbedding<TEmbedding, TData>
    {
        if (resultBuffer.Length != OutputLength)
        {
            throw new InvalidOperationException($"Result buffer length must be {OutputLength} for this model, but the supplied buffer is of length {resultBuffer.Length}");
        }

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

            return PoolSum<TEmbedding, TData>(outputs[0].GetTensorDataAsSpan<float>(), resultBuffer);
        }
        finally
        {
            _tokenizersPool.Return(tokenizer);
        }
    }

    private static TEmbedding PoolSum<TEmbedding, TData>(ReadOnlySpan<float> input, Memory<TData> resultBuffer)
        where TEmbedding: IEmbedding<TEmbedding, TData>
    {
        var outputLength = resultBuffer.Length;
        if (input.Length % outputLength != 0)
        {
            throw new ArgumentException($"Input length ({input.Length}) must be a multiple of output length ({outputLength}), but is not", nameof(input));
        }

        var floatBuffer = _outputBufferPool.Rent(outputLength);
        try
        {
            var floatBufferSpan = floatBuffer.AsSpan(0, outputLength);
            for (var pos = 0; pos < input.Length; pos += outputLength)
            {
                var tokenEmbedding = input.Slice(pos, outputLength);
                TensorPrimitives.Add(floatBufferSpan, tokenEmbedding, floatBufferSpan);
            }

            return TEmbedding.FromFloats(floatBufferSpan, resultBuffer);
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
}
