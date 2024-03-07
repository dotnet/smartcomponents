// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
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
        // Assume the target platform at least supports 128-bit vectors
        return Vector256.Create(
            Vector128Xor(lhs.GetLower(), rhs.GetLower()),
            Vector128Xor(lhs.GetUpper(), rhs.GetUpper()));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<T> Vector256Multiply<T>(Vector256<T> vector, T value) where T : unmanaged
    {
#if NET8_0_OR_GREATER
        return vector * value;
#else
        // Assume the target platform at least supports 128-bit vectors
        var lower = Vector.Multiply(vector.GetLower().AsVector(), value).AsVector128();
        var upper = Vector.Multiply(vector.GetUpper().AsVector(), value).AsVector128();
        return Vector256.Create(lower.AsByte(), upper.AsByte()).As<byte, T>();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<int> Vector128Add(Vector128<int> lhs, Vector128<int> rhs)
    {
#if NET8_0_OR_GREATER
        return lhs + rhs;
#else
        // Assume the target platform at least supports 128-bit vectors
        return Vector.Add(lhs.AsVector(), rhs.AsVector()).AsVector128();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<int> Vector256Add(Vector256<int> lhs, Vector256<int> rhs)
    {
#if NET8_0_OR_GREATER
        return lhs + rhs;
#else
        return Vector256.Create(
            Vector128Add(lhs.GetLower(), rhs.GetLower()),
            Vector128Add(lhs.GetUpper(), rhs.GetUpper()));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<T> Vector128Multiply<T>(Vector128<T> lhs, Vector128<T> rhs) where T : unmanaged
    {
#if NET8_0_OR_GREATER
        return lhs * rhs;
#else
        // Assume the target platform at least supports 128-bit vectors
        return Vector.Multiply(lhs.AsVector(), rhs.AsVector()).AsVector128();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector256<T> Vector256Multiply<T>(Vector256<T> lhs, Vector256<T> rhs) where T: unmanaged
    {
#if NET8_0_OR_GREATER
        return lhs * rhs;
#else
        return Vector256.Create(
            Vector128Multiply(lhs.GetLower(), rhs.GetLower()).AsByte(),
            Vector128Multiply(lhs.GetUpper(), rhs.GetUpper()).AsByte()).As<byte, T>();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Vector128Sum<T>(Vector128<T> vector) where T : unmanaged
    {
#if NET8_0_OR_GREATER
        return Vector128.Sum(vector);
#else
        // Assume the target platform at least supports 128-bit vectors
        return Vector.Sum(vector.AsVector());
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Vector256Sum<T>(Vector256<T> vector) where T: unmanaged
    {
#if NET8_0_OR_GREATER
        return Vector256.Sum(vector);
#else
        // Assume the target platform at least supports 128-bit vectors
        return Vector.Sum(Vector.Add(
            vector.GetLower().AsVector(),
            vector.GetUpper().AsVector()));
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
        else if (AdvSimd.IsSupported)
        {
            return Vector256.Create(
                AdvSimd.ConvertToInt32RoundToZero(vector.GetLower()),
                AdvSimd.ConvertToInt32RoundToZero(vector.GetUpper()));
        }
        else
        {
            throw new PlatformNotSupportedException("This operation requires .NET 8, or a CPU that supports AVX (x86) or AdvSIMD (ARM).");
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
        else if (AdvSimd.IsSupported)
        {
            return Vector256.Create(
                AdvSimd.ConvertToSingle(vector.GetLower()),
                AdvSimd.ConvertToSingle(vector.GetUpper()));
        }
        else
        {
            throw new PlatformNotSupportedException("This operation requires .NET 8, or a CPU that supports AVX (x86) or AdvSIMD (ARM).");
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
            return Avx2.ConvertToVector256Int32(vector.GetLower());
        }
        else if (AdvSimd.IsSupported)
        {
            return Vector256.Create(
                AdvSimd.AddWideningLower(Vector128<int>.Zero, vector.GetLower().GetLower()),
                AdvSimd.AddWideningLower(Vector128<int>.Zero, vector.GetLower().GetUpper()));
        }
        else
        {
            throw new PlatformNotSupportedException("This operation requires .NET 8, or a CPU that supports AVX (x86) or AdvSIMD (ARM).");
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
        else if (AdvSimd.IsSupported)
        {
            return Vector256.Create(
                AdvSimd.AddWideningLower(Vector128<int>.Zero, vector.GetUpper().GetLower()),
                AdvSimd.AddWideningLower(Vector128<int>.Zero, vector.GetUpper().GetUpper()));
        }
        else
        {
            throw new PlatformNotSupportedException("This operation requires .NET 8, or a CPU that supports AVX (x86) or AdvSIMD (ARM).");
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
        else if (AdvSimd.IsSupported)
        {
            return Vector256.Create(
                AdvSimd.AddWideningLower(Vector128<short>.Zero, vector.GetLower().GetLower()),
                AdvSimd.AddWideningLower(Vector128<short>.Zero, vector.GetLower().GetUpper()));
        }
        else
        {
            throw new PlatformNotSupportedException("This operation requires .NET 8, or a CPU that supports AVX (x86) or AdvSIMD (ARM).");
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
        else if (AdvSimd.IsSupported)
        {
            return Vector256.Create(
                AdvSimd.AddWideningLower(Vector128<short>.Zero, vector.GetUpper().GetLower()),
                AdvSimd.AddWideningLower(Vector128<short>.Zero, vector.GetUpper().GetUpper()));
        }
        else
        {
            throw new PlatformNotSupportedException("This operation requires .NET 8, or a CPU that supports AVX (x86) or AdvSIMD (ARM).");
        }
#endif
    }
}
