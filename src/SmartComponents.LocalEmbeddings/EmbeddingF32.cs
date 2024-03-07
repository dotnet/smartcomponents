// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartComponents.LocalEmbeddings;

/// <summary>
/// Represents an embedded value using a <see cref="float"/> for each dimension.
/// 
/// This is the raw, unquantized output from the embedding model. For the default 384-dimensional
/// embedding model, each embedded value takes 1536 bytes.
/// </summary>
[JsonConverter(typeof(FloatEmbeddingJsonConverter))]
public readonly struct EmbeddingF32 : IEmbedding<EmbeddingF32>
{
    private readonly ReadOnlyMemory<byte> _buffer;
    private readonly ReadOnlyMemory<float> _values;

    /// <summary>
    /// Gets the buffer holding the embedded value's data.
    /// </summary>
    public ReadOnlyMemory<byte> Buffer => _buffer;

    /// <summary>
    /// Gets the numerical components of the embedding vector.
    /// </summary>
    public ReadOnlyMemory<float> Values => _values;

    /// <summary>
    /// Constructs an instance of <see cref="EmbeddingF32"/> using existing data. This can be
    /// data previously supplied by <see cref="Buffer"/>.
    /// </summary>
    /// <param name="buffer">A buffer holding existing <see cref="EmbeddingF32"/> data.</param>
    public EmbeddingF32(ReadOnlyMemory<byte> buffer)
    {
        _buffer = buffer;
        _values = Utils.Cast<byte, float>(MemoryMarshal.AsMemory(buffer));
    }

    /// <inheritdoc />
    public static EmbeddingF32 FromModelOutput(ReadOnlySpan<float> input, Memory<byte> buffer)
    {
        var requiredBufferLength = GetBufferByteLength(input.Length);
        if (buffer.Length != requiredBufferLength)
        {
            throw new InvalidOperationException($"For an input with {input.Length} dimensions, the buffer length must be equal to {requiredBufferLength}, but it was {buffer.Length}.");
        }

        MemoryMarshal.AsBytes(input).CopyTo(buffer.Span);
        return new EmbeddingF32(buffer);
    }

    /// <summary>
    /// Computes the similarity between this embedding and another. For <see cref="EmbeddingF32"/>,
    /// this uses cosine similarity.
    /// </summary>
    /// <param name="other">The other embedding.</param>
    /// <returns>A similarity score, approximately in the range 0 to 1. Higher values indicate higher similarity.</returns>
    public float Similarity(EmbeddingF32 other)
        => TensorPrimitives.CosineSimilarity(_values.Span, other._values.Span);

    /// <inheritdoc />
    public static int GetBufferByteLength(int dimensions)
        => dimensions * sizeof(float);

    sealed class FloatEmbeddingJsonConverter : JsonConverter<EmbeddingF32>
    {
        public override EmbeddingF32 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new EmbeddingF32(reader.GetBytesFromBase64());

        public override void Write(Utf8JsonWriter writer, EmbeddingF32 value, JsonSerializerOptions options)
            => writer.WriteBase64StringValue(value.Buffer.Span);
    }

    // From https://stackoverflow.com/a/54512940
    internal static class Utils
    {
        public static Memory<TTo> Cast<TFrom, TTo>(Memory<TFrom> from)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            // avoid the extra allocation/indirection, at the cost of a gen-0 box
            if (typeof(TFrom) == typeof(TTo))
            {
                return (Memory<TTo>)(object)from;
            }

            return new CastMemoryManager<TFrom, TTo>(from).Memory;
        }

        private sealed class CastMemoryManager<TFrom, TTo> : MemoryManager<TTo>
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            private readonly Memory<TFrom> _from;

            public CastMemoryManager(Memory<TFrom> from) => _from = from;

            public override Span<TTo> GetSpan()
                => MemoryMarshal.Cast<TFrom, TTo>(_from.Span);

            protected override void Dispose(bool disposing) { }
            public override MemoryHandle Pin(int elementIndex = 0)
                => throw new NotSupportedException();
            public override void Unpin()
                => throw new NotSupportedException();
        }
    }
}
