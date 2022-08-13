﻿using System.Runtime.CompilerServices;

namespace lcms2;
internal static class Helpers
{
    internal const float MinusInf = -1e22f;
    internal const float PlusInf = 1e22f;

    internal static uint Uipow(uint n, uint a, uint b)
    {
        var rv = (uint)1;

        if (a == 0) return 0;
        if (n == 0) return 0;

        for (; b > 0; b--) {
            rv *= a;

            // Check for overflow
            if (rv > UInt32.MaxValue / a) return unchecked((uint)-1);
        }

        var rc = rv * n;

        if (rv != rc / n) return unchecked((uint)-1);
        return rc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long AlignLong(long x) =>
        (x + (sizeof(uint) - 1)) & ~(sizeof(uint) - 1);

    internal static Lazy<long> AlignPtr = new(new Func<long>(() => { unsafe { return sizeof(nuint); } }), LazyThreadSafetyMode.ExecutionAndPublication);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long AlignMem(long x) =>
        (x + (AlignPtr.Value - 1)) & ~(AlignPtr.Value - 1);

    internal static ushort From8to16(byte rgb) =>
        (ushort)((rgb << 8) | rgb);

    internal static byte From16to8(ushort rgb) =>
        (byte)((((rgb * (uint)65281) + 8388608) >> 24) & 0xFF);
}
