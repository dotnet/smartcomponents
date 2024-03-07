// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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
    public static unsafe Vector256<T> Vector256Load<T>(T* ptr) where T : unmanaged
    {
#if NET8_0_OR_GREATER
        return Vector256.Load(ptr);
#else
        return Vector256.Create(((long*)ptr)[0], ((long*)ptr)[1], ((long*)ptr)[2], ((long*)ptr)[3]).As<long, T>();
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
    public static unsafe Vector256<T> Vector256Multiply<T>(Vector256<T> vector, T value) where T : unmanaged
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
    public static unsafe Vector256<T> Vector256Multiply<T>(Vector256<T> lhs, Vector256<T> rhs) where T: unmanaged
    {
#if NET8_0_OR_GREATER
        return lhs * rhs;
#else
        return Vector.Multiply(lhs.AsVector(), rhs.AsVector()).AsVector256();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Vector256Sum<T>(Vector256<T> vector) where T : unmanaged
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
        if (Avx.IsSupported)
        {
            return Avx.ConvertToVector256Int32(vector);
        }
        else
        {
            return Vector256.Create(
                (int)vector.GetElement(0),
                (int)vector.GetElement(1),
                (int)vector.GetElement(2),
                (int)vector.GetElement(3),
                (int)vector.GetElement(4),
                (int)vector.GetElement(5),
                (int)vector.GetElement(6),
                (int)vector.GetElement(7));
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<float> Vector256ConvertToSingle(Vector256<int> vector)
    {
#if NET8_0_OR_GREATER
        return Vector256.ConvertToSingle(vector);
#else
        if (Avx.IsSupported)
        {
            return Avx.ConvertToVector256Single(vector);
        }
        else
        {
            return Vector256.Create(
                (float)vector.GetElement(0),
                (float)vector.GetElement(1),
                (float)vector.GetElement(2),
                (float)vector.GetElement(3),
                (float)vector.GetElement(4),
                (float)vector.GetElement(5),
                (float)vector.GetElement(6),
                (float)vector.GetElement(7));
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<int> Vector256WidenLower(Vector256<short> vector)
    {
#if NET8_0_OR_GREATER
        return Vector256.WidenLower(vector);
#else
        if (Avx.IsSupported)
        {
            // TODO: Is this right?
            return Avx2.ConvertToVector256Int32(vector.GetLower());
        }
        else
        {
            return Vector256.Create(
                (int)vector.GetElement(0),
                (int)vector.GetElement(1),
                (int)vector.GetElement(2),
                (int)vector.GetElement(3),
                (int)vector.GetElement(4),
                (int)vector.GetElement(5),
                (int)vector.GetElement(6),
                (int)vector.GetElement(7));
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<int> Vector256WidenUpper(Vector256<short> vector)
    {
#if NET8_0_OR_GREATER
        return Vector256.WidenUpper(vector);
#else
        if (Avx.IsSupported)
        {
            return Avx2.ConvertToVector256Int32(vector.GetUpper());
        }
        else
        {
            return Vector256.Create(
                (int)vector.GetElement(8),
                (int)vector.GetElement(9),
                (int)vector.GetElement(10),
                (int)vector.GetElement(11),
                (int)vector.GetElement(12),
                (int)vector.GetElement(13),
                (int)vector.GetElement(14),
                (int)vector.GetElement(15));
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<short> Vector256WidenLower(Vector256<sbyte> vector)
    {
#if NET8_0_OR_GREATER
        return Vector256.WidenLower(vector);
#else
        if (Avx.IsSupported)
        {
            return Avx2.ConvertToVector256Int16(vector.GetLower());
        }
        else
        {
            return Vector256.Create(
                (short)vector.GetElement(0),
                (short)vector.GetElement(1),
                (short)vector.GetElement(2),
                (short)vector.GetElement(3),
                (short)vector.GetElement(4),
                (short)vector.GetElement(5),
                (short)vector.GetElement(6),
                (short)vector.GetElement(7),
                (short)vector.GetElement(8),
                (short)vector.GetElement(9),
                (short)vector.GetElement(10),
                (short)vector.GetElement(11),
                (short)vector.GetElement(12),
                (short)vector.GetElement(13),
                (short)vector.GetElement(14),
                (short)vector.GetElement(15));
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<short> Vector256WidenUpper(Vector256<sbyte> vector)
    {
#if NET8_0_OR_GREATER
        return Vector256.WidenUpper(vector);
#else
        if (Avx.IsSupported)
        {
            return Avx2.ConvertToVector256Int16(vector.GetUpper());
        }
        else
        {
            return Vector256.Create(
                (short)vector.GetElement(16),
                (short)vector.GetElement(17),
                (short)vector.GetElement(18),
                (short)vector.GetElement(19),
                (short)vector.GetElement(20),
                (short)vector.GetElement(21),
                (short)vector.GetElement(22),
                (short)vector.GetElement(23),
                (short)vector.GetElement(24),
                (short)vector.GetElement(25),
                (short)vector.GetElement(26),
                (short)vector.GetElement(27),
                (short)vector.GetElement(28),
                (short)vector.GetElement(29),
                (short)vector.GetElement(30),
                (short)vector.GetElement(31));
        }
#endif
    }
}
