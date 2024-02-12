using System;
using System.IO;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartComponents.LocalEmbeddings;

[JsonConverter(typeof(ByteEmbeddingJsonConverter))]
public readonly struct ByteEmbedding : IEmbedding<ByteEmbedding, sbyte>
{
    private readonly ReadOnlyMemory<sbyte> values;
    private readonly float magnitude;

    private ByteEmbedding(ReadOnlyMemory<sbyte> values, float magnitude)
    {
        this.values = values;
        this.magnitude = magnitude;
    }

    public static unsafe ByteEmbedding FromFloats(ReadOnlySpan<float> input, Memory<sbyte> buffer)
    {
        var length = input.Length;
        var blockLength = Vector256<float>.Count;

        if (buffer.Length != length)
        {
            throw new InvalidOperationException($"Buffer length {buffer.Length} must be equal to input length {length}");
        }

        fixed (float* inputPtr = input)
        {
            // First determine the scale factor
            var maxComponent = MathF.Abs(TensorPrimitives.MaxMagnitude(input));
            var scaleFactor = 127f / maxComponent;

            // Use it to represent as sbyte
            Vector256<int> magnitudeSquareds = default;
            fixed (sbyte* bufferPtr = buffer.Span)
            {
                for (var pos = 0; pos < length; pos += blockLength)
                {
                    var block = Avx.LoadVector256(inputPtr + pos) * scaleFactor;
                    var blockInt = Avx.ConvertToVector256Int32(block);
                    var packedShort = Avx.PackSignedSaturate(blockInt.GetLower(), blockInt.GetUpper());
                    var packedByte = Avx.PackSignedSaturate(packedShort, default);
                    var packedByteLower = packedByte.GetLower();
                    packedByteLower.Store(bufferPtr + pos);

                    magnitudeSquareds += blockInt * blockInt;
                }
            }

            var magnitudeSquared = Vector256.Sum(magnitudeSquareds);
            return new ByteEmbedding(buffer, MathF.Sqrt(magnitudeSquared));
        }
    }

    private const int Vec256ByteLength = 32; // Vector256<sbyte>.Count;

    public static unsafe float Similarity(ByteEmbedding lhs, ByteEmbedding rhs)
    {
        var length = lhs.values.Length;
        if (rhs.values.Length != length)
        {
            throw new InvalidOperationException($"LHS is of length {lhs.values.Length}, whereas RHS is of length {rhs.values.Length}. They must be equal length.");
        }

        if (length % Vec256ByteLength != 0)
        {
            // Otherwise LoadVector256 will read beyond the end of the buffer for the last block, and we'd have to
            // explicitly do something to avoid that (or zero out the leftover vector slots)
            throw new InvalidOperationException($"The vector length must be a multiple of {Vec256ByteLength}. Received vector of length {length}");
        }

        Vector256<int> sumsOfProducts = default;

        fixed (sbyte* lhsPtr = lhs.values.Span)
        fixed (sbyte* rhsPtr = rhs.values.Span)
            for (var pos = 0; pos < length; pos += Vec256ByteLength)
            {
                var lhsVecSByte = Avx.LoadVector256(lhsPtr + pos);
                var rhsVecSByte = Avx.LoadVector256(rhsPtr + pos);

                // Multiply the lower halves
                var lhsVecShort = Avx2.ConvertToVector256Int16(lhsVecSByte.GetLower());
                var rhsVecShort = Avx2.ConvertToVector256Int16(rhsVecSByte.GetLower());
                var products = Avx2.MultiplyAddAdjacent(lhsVecShort, rhsVecShort);
                sumsOfProducts += products;

                // Multiply the upper halves
                lhsVecShort = Avx2.ConvertToVector256Int16(lhsVecSByte.GetUpper());
                rhsVecShort = Avx2.ConvertToVector256Int16(rhsVecSByte.GetUpper());
                products = Avx2.MultiplyAddAdjacent(lhsVecShort, rhsVecShort);
                sumsOfProducts += products;
            }

        var totalsFloats = Avx.ConvertToVector256Single(sumsOfProducts);
        return Vector256.Sum(totalsFloats) / (lhs.magnitude * rhs.magnitude);
    }

    public int ByteLength => values.Length;

    public ReadOnlyMemory<sbyte> Values => values;

    public async ValueTask WriteToAsync(Stream destination)
    {
        ReadOnlyMemory<byte> data = Unsafe.BitCast<ReadOnlyMemory<sbyte>, ReadOnlyMemory<byte>>(values);
        await destination.WriteAsync(data);
    }

    class ByteEmbeddingJsonConverter : JsonConverter<ByteEmbedding>
    {
        public override ByteEmbedding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();
            var magnitude = reader.GetSingle();
            reader.Read();
            var bytes = reader.GetBytesFromBase64().AsMemory();
            reader.Read();

            var values = Unsafe.BitCast<Memory<byte>, Memory<sbyte>>(bytes); // Because sbyte length is same as byte
            return new ByteEmbedding(values, magnitude);
        }

        public override void Write(Utf8JsonWriter writer, ByteEmbedding value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.magnitude);
            var bytes = MemoryMarshal.AsBytes(value.values.Span);
            writer.WriteBase64StringValue(bytes);
            writer.WriteEndArray();
        }
    }
}