using System.IO;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Text.Json;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartComponents.LocalEmbeddings;

[JsonConverter(typeof(FloatEmbeddingJsonConverter))]
public readonly struct FloatEmbedding : IEmbedding<FloatEmbedding, float>
{
    private readonly ReadOnlyMemory<float> values;

    private FloatEmbedding(ReadOnlyMemory<float> values)
    {
        this.values = values;
    }

    public static FloatEmbedding FromFloats(ReadOnlySpan<float> input, Memory<float> buffer)
    {
        if (input.Length != buffer.Length)
        {
            throw new InvalidOperationException($"Buffer length {buffer.Length} must be equal to input length {input.Length}");
        }

        input.CopyTo(buffer.Span);
        return new FloatEmbedding(buffer);
    }

    public static float Similarity(FloatEmbedding lhs, FloatEmbedding rhs)
        => TensorPrimitives.CosineSimilarity(lhs.values.Span, rhs.values.Span);

    public int ByteLength => values.Length * sizeof(float);

    public ReadOnlyMemory<float> Values => values;

    public async ValueTask WriteToAsync(Stream destination)
    {
        var chunkBytes = new byte[1024];
        var chunkFloatLength = chunkBytes.Length / sizeof(float);

        for (var floatPos = 0; floatPos < values.Length; floatPos += chunkFloatLength)
        {
            var numFloats = Math.Min(chunkFloatLength, values.Length - floatPos);
            CopyFloats(values.Span.Slice(floatPos, numFloats), chunkBytes);
            await destination.WriteAsync(chunkBytes, 0, numFloats * sizeof(float));
        }
    }

    private static void CopyFloats(ReadOnlySpan<float> source, byte[] destination)
    {
        var destinationAsFloats = MemoryMarshal.Cast<byte, float>(destination.AsSpan());
        source.CopyTo(destinationAsFloats);
    }

    class FloatEmbeddingJsonConverter : JsonConverter<FloatEmbedding>
    {
        public override FloatEmbedding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var bytes = reader.GetBytesFromBase64().AsMemory();
            var floats = MemoryMarshal.Cast<byte, float>(bytes.Span).ToArray();
            return new FloatEmbedding(floats);
        }

        public override void Write(Utf8JsonWriter writer, FloatEmbedding value, JsonSerializerOptions options)
        {
            var bytes = MemoryMarshal.AsBytes(value.values.Span);
            writer.WriteBase64StringValue(bytes);
        }
    }
}
