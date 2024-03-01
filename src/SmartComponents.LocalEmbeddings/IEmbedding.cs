// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SmartComponents.LocalEmbeddings;

/// <summary>
/// Implements a representation for an embedded value.
/// </summary>
public interface IEmbedding<TEmbedding>
{
    /// <summary>
    /// Gives the byte length of the buffer required to store a <typeparamref name="TEmbedding"/> with the given number of dimensions.
    /// </summary>
    /// <param name="dimensions">The number of dimensions.</param>
    /// <returns>The byte length of the buffer required to store the embedding as a <typeparamref name="TEmbedding"/>.</returns>
    static abstract int GetBufferByteLength(int dimensions);

    /// <summary>
    /// Converts an embedding model's raw output into a <typeparamref name="TEmbedding"/>.
    /// </summary>
    /// <param name="input">The raw output from the embedding model.</param>
    /// <param name="buffer">A buffer used to store the <typeparamref name="TEmbedding"/>'s data. Its length must match the output from <see cref="GetBufferByteLength(int)"/>.</param>
    /// <returns>The embedded value in the <typeparamref name="TEmbedding"/> representation.</returns>
    static abstract TEmbedding FromModelOutput(ReadOnlySpan<float> input, Memory<byte> buffer);

    /// <summary>
    /// Computes the similarity between this embedding and another. The similarity metric
    /// is determined by the embedding type.
    /// </summary>
    /// <param name="other">The other embedding.</param>
    /// <returns>A similarity score, approximately in the range 0 to 1. Higher values indicate higher similarity.</returns>
    float Similarity(TEmbedding other);
}
