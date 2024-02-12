using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartComponents.LocalEmbeddings;

internal interface IEmbedding<TEmbedding, TData> where TEmbedding: IEmbedding<TEmbedding, TData>
{
    static abstract TEmbedding FromFloats(ReadOnlySpan<float> input, Memory<TData> buffer);
    static abstract float Similarity(TEmbedding lhs, TEmbedding rhs);

    int ByteLength { get; }
    ReadOnlyMemory<TData> Values { get; }
    ValueTask WriteToAsync(Stream destination);
}
