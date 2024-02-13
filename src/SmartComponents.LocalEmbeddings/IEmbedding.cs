using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartComponents.LocalEmbeddings;

public interface IEmbedding<TEmbedding>
{
    float Similarity(TEmbedding other);
}

internal interface IEmbedding<TEmbedding, TData> : IEmbedding<TEmbedding>
    where TEmbedding: IEmbedding<TEmbedding, TData>
{
    static abstract TEmbedding FromFloats(ReadOnlySpan<float> input, Memory<TData> buffer);
    ReadOnlyMemory<TData> Values { get; }
    int ByteLength { get; }
    ValueTask WriteToAsync(Stream destination);
}
