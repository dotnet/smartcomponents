// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace SmartComponents.LocalEmbeddings;

internal static class VectorCompat
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<byte> Vector128Load(byte* ptr)
    {
#if NET8_0_OR_GREATER
        return Vector128.Load(ptr);
#else
        return Vector128.Create(((long*)ptr)[0], ((long*)ptr)[1]).AsByte();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<byte> Vector256Load(byte* ptr)
    {
#if NET8_0_OR_GREATER
        return Vector256.Load(ptr);
#else
        return Vector256.Create(((long*)ptr)[0], ((long*)ptr)[1], ((long*)ptr)[2], ((long*)ptr)[3]).AsByte();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<float> Vector256Load(float* ptr)
    {
#if NET8_0_OR_GREATER
        return Vector256.Load(ptr);
#else
        return Vector256.Create(((long*)ptr)[0], ((long*)ptr)[1], ((long*)ptr)[2], ((long*)ptr)[3]).AsSingle();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<byte> Vector128Xor(Vector128<byte> lhs, Vector128<byte> rhs)
    {
#if NET8_0_OR_GREATER
        return Vector128.Xor(lhs, rhs);
#else
        return Vector.Xor(lhs.AsVector(), rhs.AsVector()).AsVector128();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<byte> Vector256Xor(Vector256<byte> lhs, Vector256<byte> rhs)
    {
#if NET8_0_OR_GREATER
        return Vector256.Xor(lhs, rhs);
#else
        return Vector.Xor(lhs.AsVector(), rhs.AsVector()).AsVector256();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<float> Vector256Multiply(Vector256<float> vector, float value)
    {
#if NET8_0_OR_GREATER
        return vector * value;
#else
        return Vector.Multiply(vector.AsVector(), value).AsVector256();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<int> Vector256Add(Vector256<int> lhs, Vector256<int> rhs)
    {
#if NET8_0_OR_GREATER
        return lhs + rhs;
#else
        return Vector.Add(lhs.AsVector(), rhs.AsVector()).AsVector256();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<int> Vector256Multiply(Vector256<int> lhs, Vector256<int> rhs)
    {
#if NET8_0_OR_GREATER
        return lhs * rhs;
#else
        return Vector.Multiply(lhs.AsVector(), rhs.AsVector()).AsVector256();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Vector256Sum(Vector256<int> vector)
    {
#if NET8_0_OR_GREATER
        return Vector256.Sum(vector);
#else
        return Vector.Sum(vector.AsVector());
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<int> Vector256ConvertToInt32(Vector256<float> vector)
    {
#if NET8_0_OR_GREATER
        return Vector256.ConvertToInt32(vector);
#else
        return Vector256.Create(
            (int)vector.GetElement(0),
            (int)vector.GetElement(1),
            (int)vector.GetElement(2),
            (int)vector.GetElement(3),
            (int)vector.GetElement(4),
            (int)vector.GetElement(5),
            (int)vector.GetElement(6),
            (int)vector.GetElement(7));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Vector64Store<T>(Vector64<T> vector, T* destination) where T: unmanaged
    {
#if NET8_0_OR_GREATER
        Vector64.Store(vector, destination);
#else
        *((long*)destination) = vector.AsInt64().GetElement(0);
#endif
    }
}
