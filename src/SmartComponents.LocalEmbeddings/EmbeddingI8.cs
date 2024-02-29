using System;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text.Json.Serialization;
using System.Text.Json;

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
        _values = Unsafe.BitCast<ReadOnlyMemory<byte>, ReadOnlyMemory<sbyte>>(buffer.Slice(4));
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
                    var block = Avx.LoadVector256(inputPtr + pos) * scaleFactor;
                    var blockInt = Avx.ConvertToVector256Int32(block);
                    var packedShort = Avx.PackSignedSaturate(blockInt.GetLower(), blockInt.GetUpper());
                    var packedByte = Avx.PackSignedSaturate(packedShort, default);
                    var packedByteLower = packedByte.GetLower();
                    packedByteLower.AsByte().Store(bufferPtr + pos);

                    magnitudeSquareds += blockInt * blockInt;
                }
            }

            var magnitudeSquared = Vector256.Sum(magnitudeSquareds);
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
                var thisVecSByte = Avx.LoadVector256(thisPtr + pos);
                var otherVecSByte = Avx.LoadVector256(otherPtr + pos);

                // Multiply the lower halves
                var thisVecShort = Avx2.ConvertToVector256Int16(thisVecSByte.GetLower());
                var otherVecShort = Avx2.ConvertToVector256Int16(otherVecSByte.GetLower());
                var products = Avx2.MultiplyAddAdjacent(thisVecShort, otherVecShort);
                sumsOfProducts += products;

                // Multiply the upper halves
                thisVecShort = Avx2.ConvertToVector256Int16(thisVecSByte.GetUpper());
                otherVecShort = Avx2.ConvertToVector256Int16(otherVecSByte.GetUpper());
                products = Avx2.MultiplyAddAdjacent(thisVecShort, otherVecShort);
                sumsOfProducts += products;
            }
        }

        var totalsFloats = Avx.ConvertToVector256Single(sumsOfProducts);
        return Vector256.Sum(totalsFloats) / (_magnitude * other._magnitude);
    }

    /// <inheritdoc />
    public static int GetBufferByteLength(int dimensions)
        => 4 + dimensions; // Magnitude, then the bytes

    class ByteEmbeddingJsonConverter : JsonConverter<EmbeddingI8>
    {
        public override EmbeddingI8 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new EmbeddingI8(reader.GetBytesFromBase64());

        public override void Write(Utf8JsonWriter writer, EmbeddingI8 value, JsonSerializerOptions options)
            => writer.WriteBase64StringValue(value.Buffer.Span);
    }
}
