// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json;
using System.Text.Json.Serialization;
using static SmartComponents.LocalEmbeddings.VectorCompat;

namespace SmartComponents.LocalEmbeddings;

/// <summary>
/// Represents an embedded value using a single bit for each dimension.
///
/// For the default 384-dimensional embedding model, this representation takes 48 bytes per embedding,
/// since 8 dimensions are packed into each byte.
/// 
/// This representation is equivalent to the LSH (Locality Sensitive Hashing) index option in Faiss.
/// It is very fast and compact, at the cost of some precision.
/// </summary>
[JsonConverter(typeof(BitEmbeddingJsonConverter))]
public readonly struct EmbeddingI1 : IEmbedding<EmbeddingI1>
{
    private readonly ReadOnlyMemory<byte> _buffer;

    /// <summary>
    /// Gets the buffer holding the embedded value's data.
    /// </summary>
    public ReadOnlyMemory<byte> Buffer => _buffer;

    /// <summary>
    /// Constructs an instance of <see cref="EmbeddingI1"/> using existing data. This can be
    /// data previously supplied by <see cref="Buffer"/>.
    /// </summary>
    /// <param name="buffer">A buffer holding existing <see cref="EmbeddingI1"/> data.</param>
    public EmbeddingI1(ReadOnlyMemory<byte> buffer)
    {
        _buffer = buffer;
    }

    /// <inheritdoc />
    public static EmbeddingI1 FromModelOutput(ReadOnlySpan<float> input, Memory<byte> buffer)
    {
        var remainder = input.Length % 8;
        if (remainder != 0)
        {
            throw new InvalidOperationException("Input length must be a multiple of 8");
        }

        var expectedBufferLength = input.Length / 8;
        if (buffer.Length != expectedBufferLength)
        {
            throw new InvalidOperationException($"Buffer length was {buffer.Length}, but must be {expectedBufferLength} for an input with {input.Length} dimensions.");
        }

        Quantize(input, buffer.Span);

        return new EmbeddingI1(buffer);
    }

    private static void Quantize(ReadOnlySpan<float> input, Span<byte> result)
    {
        var inputLength = input.Length;
        for (var j = 0; j < inputLength; j += 8)
        {
            // Vectorized approaches don't seem to get even close to the
            // speed of doing it in this naive way
            var sources = input.Slice(j, 8);
            var sum = (byte)0;

#if NET8_0_OR_GREATER
            if (float.IsPositive(sources[0])) { sum |= 128; }
            if (float.IsPositive(sources[1])) { sum |= 64; }
            if (float.IsPositive(sources[2])) { sum |= 32; }
            if (float.IsPositive(sources[3])) { sum |= 16; }
            if (float.IsPositive(sources[4])) { sum |= 8; }
            if (float.IsPositive(sources[5])) { sum |= 4; }
            if (float.IsPositive(sources[6])) { sum |= 2; }
            if (float.IsPositive(sources[7])) { sum |= 1; }
#else
            if (sources[0] >= 0) { sum |= 128; }
            if (sources[1] >= 0) { sum |= 64; }
            if (sources[2] >= 0) { sum |= 32; }
            if (sources[3] >= 0) { sum |= 16; }
            if (sources[4] >= 0) { sum |= 8; }
            if (sources[5] >= 0) { sum |= 4; }
            if (sources[6] >= 0) { sum |= 2; }
            if (sources[7] >= 0) { sum |= 1; }
#endif
            result[j / 8] = sum;
        }
    }

    /// <summary>
    /// Computes the similarity between this embedding and another. For <see cref="EmbeddingI1"/>,
    /// this is based on Hamming distance.
    /// </summary>
    /// <param name="other">The other embedding.</param>
    /// <returns>A similarity score in the range 0 to 1. Higher values indicate higher similarity.</returns>
    public unsafe float Similarity(EmbeddingI1 other)
    {
        if (other._buffer.Length != _buffer.Length)
        {
            throw new InvalidOperationException($"Cannot compare a {nameof(EmbeddingI1)} of length {other._buffer.Length} against one of length {_buffer.Length}");
        }

        // The following approach to load the vectors is considerably
        // faster than using a "fixed" block
        ref var lhsRef = ref MemoryMarshal.AsMemory(_buffer).Span[0];
        var lhsPtr = (byte*)Unsafe.AsPointer(ref lhsRef);
        ref var rhsRef = ref MemoryMarshal.AsMemory(other.Buffer).Span[0];
        var rhsPtr = (byte*)Unsafe.AsPointer(ref rhsRef);
        var lhsPtrEnd = lhsPtr + _buffer.Length;
        var differences = 0;

        // Process as many Vector256 blocks as possible
        while (lhsPtr <= lhsPtrEnd - 32)
        {
            var lhsBlock = Vector256Load(lhsPtr);
            var rhsBlock = Vector256Load(rhsPtr);
            var xorBlock = Vector256Xor(lhsBlock, rhsBlock).AsUInt64();

            // This is 10x faster than any AVX2/SSE3 vectorized approach I could find (e.g.,
            // avx2-lookup from https://stackoverflow.com/a/50082218). However I didn't try
            // AVX512 approaches (vectorized popcnt) since hardware support is less common.
            differences +=
                BitOperations.PopCount(xorBlock.GetElement(0)) +
                BitOperations.PopCount(xorBlock.GetElement(1)) +
                BitOperations.PopCount(xorBlock.GetElement(2)) +
                BitOperations.PopCount(xorBlock.GetElement(3));

            lhsPtr += 32;
            rhsPtr += 32;
        }

        // Process as many Vector128 blocks as possible
        while (lhsPtr <= lhsPtrEnd - 16)
        {
            var lhsBlock = Vector128Load(lhsPtr);
            var rhsBlock = Vector128Load(rhsPtr);
            var xorBlock = Vector128Xor(lhsBlock, rhsBlock).AsUInt64();

            differences +=
                BitOperations.PopCount(xorBlock.GetElement(0)) +
                BitOperations.PopCount(xorBlock.GetElement(1));

            lhsPtr += 16;
            rhsPtr += 16;
        }

        // Process the remaining bytes
        while (lhsPtr < lhsPtrEnd)
        {
            var lhs = *lhsPtr;
            var rhs = *rhsPtr;
            var xor = (byte)(lhs ^ rhs);
            differences += BitOperations.PopCount(xor);
            lhsPtr++;
            rhsPtr++;
        }

        return 1 - (differences / (float)(_buffer.Length * 8));
    }

    /// <inheritdoc />
    public static int GetBufferByteLength(int dimensions)
        => dimensions / 8;

    sealed class BitEmbeddingJsonConverter : JsonConverter<EmbeddingI1>
    {
        public override EmbeddingI1 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new EmbeddingI1(reader.GetBytesFromBase64());

        public override void Write(Utf8JsonWriter writer, EmbeddingI1 value, JsonSerializerOptions options)
            => writer.WriteBase64StringValue(value.Buffer.Span);
    }
}
