// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics.Tensors;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#else
using System.Runtime.InteropServices;
#endif
using System.Text.Json;
using System.Text.Json.Serialization;
using static SmartComponents.LocalEmbeddings.VectorCompat;

namespace SmartComponents.LocalEmbeddings;

/// <summary>
/// Represents an embedded value using a <see cref="byte"/> for each dimension, plus an extra
/// 4 bytes to hold a scale factor.
/// 
/// For the default 384-dimensional embedding model, this representation takes 388 bytes per embedding.
/// </summary>
[JsonConverter(typeof(ByteEmbeddingJsonConverter))]
public readonly struct EmbeddingI8 : IEmbedding<EmbeddingI8>
{
    private readonly ReadOnlyMemory<byte> _buffer;
    private readonly ReadOnlyMemory<sbyte> _values;
    private readonly float _magnitude;

    /// <summary>
    /// Gets the buffer holding the embedded value's data.
    /// </summary>
    public ReadOnlyMemory<byte> Buffer => _buffer;

    /// <summary>
    /// Gets the numerical components of the embedding vector.
    /// </summary>
    public ReadOnlyMemory<sbyte> Values => _values;

    /// <summary>
    /// Gets the magnitude of the embedding vector.
    /// </summary>
    public float Magnitude => _magnitude;

    /// <summary>
    /// Constructs an instance of <see cref="EmbeddingI8"/> using existing data. This can be
    /// data previously supplied by <see cref="Buffer"/>.
    /// </summary>
    /// <param name="buffer">A buffer holding existing <see cref="EmbeddingI8"/> data.</param>
    public EmbeddingI8(ReadOnlyMemory<byte> buffer)
    {
        _buffer = buffer;
        _magnitude = BitConverter.ToSingle(buffer.Span);

#if NET8_0_OR_GREATER
        // No allocation
        _values = Unsafe.BitCast<ReadOnlyMemory<byte>, ReadOnlyMemory<sbyte>>(buffer.Slice(4));
#else
        _values = EmbeddingF32.Utils.Cast<byte, sbyte>(MemoryMarshal.AsMemory(buffer.Slice(4)));
#endif
    }

    /// <inheritdoc />
    public static unsafe EmbeddingI8 FromModelOutput(ReadOnlySpan<float> input, Memory<byte> buffer)
    {
        var length = input.Length;
        var blockLength = Vector256<float>.Count;

        var requiredBufferLength = GetBufferByteLength(input.Length);
        if (buffer.Length != requiredBufferLength)
        {
            throw new InvalidOperationException($"For an input with {input.Length} dimensions, the buffer length must be equal to {requiredBufferLength}, but it was {buffer.Length}.");
        }

        fixed (float* inputPtr = input)
        {
            // First determine the scale factor
            var maxComponent = MathF.Abs(TensorPrimitives.MaxMagnitude(input));
            var scaleFactor = 127f / maxComponent;

            // Use it to represent as sbyte
            Vector256<int> magnitudeSquareds = default;
            fixed (byte* bufferPtr = buffer.Span.Slice(4))
            {
                for (var pos = 0; pos < length; pos += blockLength)
                {
                    var block = Vector256Multiply(Vector256Load(inputPtr + pos), scaleFactor);
                    var blockInt = Vector256ConvertToInt32(block);
                    Vector64<sbyte> packedSByte;

                    if (Sse2.IsSupported)
                    {
                        var packedShort = Sse2.PackSignedSaturate(blockInt.GetLower(), blockInt.GetUpper());
                        packedSByte = Sse2.PackSignedSaturate(packedShort, packedShort).GetLower();
                    }
                    else if (AdvSimd.IsSupported)
                    {
                        var packedShort = Vector128.Create(
                            AdvSimd.ExtractNarrowingLower(blockInt.GetLower()),
                            AdvSimd.ExtractNarrowingLower(blockInt.GetUpper()));
                        packedSByte = AdvSimd.ExtractNarrowingLower(packedShort);
                    }
                    else
                    {
                        var blockIntByte = blockInt.AsSByte();
                        packedSByte = Vector64.Create(
                            blockIntByte.GetElement(0),
                            blockIntByte.GetElement(4),
                            blockIntByte.GetElement(8),
                            blockIntByte.GetElement(12),
                            blockIntByte.GetElement(16),
                            blockIntByte.GetElement(20),
                            blockIntByte.GetElement(24),
                            blockIntByte.GetElement(28));
                    }

                    Vector64Store(packedSByte.AsByte(), bufferPtr + pos);
                    magnitudeSquareds = Vector256Add(magnitudeSquareds, Vector256Multiply(blockInt, blockInt));
                }
            }

            var magnitudeSquared = Vector256Sum(magnitudeSquareds);
            BitConverter.TryWriteBytes(buffer.Span, MathF.Sqrt(magnitudeSquared));
            return new EmbeddingI8(buffer);
        }
    }

    private const int Vec256ByteLength = 32; // Vector256<sbyte>.Count;

    /// <summary>
    /// Computes the similarity between this embedding and another. For <see cref="EmbeddingI8"/>,
    /// this uses cosine similarity.
    /// </summary>
    /// <param name="other">The other embedding.</param>
    /// <returns>A similarity score, approximately in the range 0 to 1. Higher values indicate higher similarity.</returns>
    public unsafe float Similarity(EmbeddingI8 other)
    {
        var length = _values.Length;
        if (other._values.Length != length)
        {
            throw new InvalidOperationException($"This is of length {_values.Length}, whereas {nameof(other)} is of length {other._values.Length}. They must be equal length.");
        }

        if (length % Vec256ByteLength != 0)
        {
            // Otherwise LoadVector256 will read beyond the end of the buffer for the last block, and we'd have to
            // explicitly do something to avoid that (or zero out the leftover vector slots)
            throw new InvalidOperationException($"The vector length must be a multiple of {Vec256ByteLength}. Received vector of length {length}");
        }

        Vector256<int> sumsOfProducts = default;

        fixed (sbyte* thisPtr = _values.Span)
        fixed (sbyte* otherPtr = other._values.Span)
        {
            for (var pos = 0; pos < length; pos += Vec256ByteLength)
            {
                var thisVecSByte = Vector256Load(thisPtr + pos);
                var otherVecSByte = Vector256Load(otherPtr + pos);

                // Multiply the lower halves
                var thisVecShort = Vector256WidenLower(thisVecSByte);
                var otherVecShort = Vector256WidenLower(otherVecSByte);
                if (Avx2.IsSupported)
                {
                    sumsOfProducts = Vector256Add(sumsOfProducts, Avx2.MultiplyAddAdjacent(thisVecShort, otherVecShort));
                }
                else
                {
                    // We know the multiply won't overflow because the values are all in the range -128 to 127
                    var products = Vector256Multiply(thisVecShort, otherVecShort);
                    sumsOfProducts = Vector256Add(sumsOfProducts,
                        Vector256Add(Vector256WidenLower(products), Vector256WidenUpper(products)));
                }

                // Multiply the upper halves
                thisVecShort = Vector256WidenUpper(thisVecSByte);
                otherVecShort = Vector256WidenUpper(otherVecSByte);
                if (Avx2.IsSupported)
                {
                    sumsOfProducts = Vector256Add(sumsOfProducts, Avx2.MultiplyAddAdjacent(thisVecShort, otherVecShort));
                }
                else
                {
                    // We know the multiply won't overflow because the values are all in the range -128 to 127
                    var products = Vector256Multiply(thisVecShort, otherVecShort);
                    sumsOfProducts = Vector256Add(sumsOfProducts,
                        Vector256Add(Vector256WidenLower(products), Vector256WidenUpper(products)));
                }
            }

            var totalsFloats = Vector256ConvertToSingle(sumsOfProducts);
            return Vector256Sum(totalsFloats) / (_magnitude * other._magnitude);
        }
    }

    /// <inheritdoc />
    public static int GetBufferByteLength(int dimensions)
        => 4 + dimensions; // Magnitude, then the bytes

    sealed class ByteEmbeddingJsonConverter : JsonConverter<EmbeddingI8>
    {
        public override EmbeddingI8 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new EmbeddingI8(reader.GetBytesFromBase64());

        public override void Write(Utf8JsonWriter writer, EmbeddingI8 value, JsonSerializerOptions options)
            => writer.WriteBase64StringValue(value.Buffer.Span);
    }
}
