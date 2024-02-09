using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartComponents.LocalEmbedding;

internal interface IEmbeddingData<TEmbeddingData, TValue> where TEmbeddingData: IEmbeddingData<TEmbeddingData, TValue>
{
    static abstract TEmbeddingData FromFloats(ReadOnlySpan<float> input, Memory<TValue> buffer);
    static abstract float Similarity(TEmbeddingData lhs, TEmbeddingData rhs);

    int ByteLength { get; }
    ReadOnlyMemory<TValue> Values { get; }
    ValueTask WriteToAsync(Stream destination);
}
