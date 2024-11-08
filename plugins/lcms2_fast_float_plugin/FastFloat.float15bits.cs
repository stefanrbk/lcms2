//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright ©️ 1998-2024 Marti Maria Saguer, all rights reserved
//              2022-2024 Stefan Kewatt, all rights reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//---------------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace lcms2.FastFloatPlugin;

public static partial class FastFloat
{
    //---------------------------------------------------------------------------------

    //  The internal photoshop 16 bit format range is 1.15 fixed point, which goes 0..32768 
    // (NOT 32767) that means:
    //
    //         16 bits encoding            15 bit Photoshop encoding
    //         ================            =========================
    // 
    //              0x0000                       0x0000
    //              0xFFFF                       0x8000
    //
    //  A nice (and fast) way to implement conversions is by using 64 bit values, which are
    // native CPU word size in most today architectures.
    // In CMYK, internal Photoshop format comes inverted, and this inversion happens after 
    // the resizing, so values 32769 to 65535 are never used in PhotoShop.

    //---------------------------------------------------------------------------------

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort From16To15(ushort x16) =>
        (ushort)(((ulong)x16 << 15) / 0xFFFFL);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort From15To16(ushort x15) =>
        (ushort)((((ulong)x15 * 0xFFFF) + 0x4000L) >> 15);

    private static ReadOnlySpan<byte> Unroll15bitsGray(Transform _1,
                                                       Span<ushort> Values,
                                                       ReadOnlySpan<byte> Buffer,
                                                       uint _2)
    {
        Values[0] = From15To16(BitConverter.ToUInt16(Buffer));

        return Buffer[sizeof(ushort)..];
    }

    private static Span<byte> Pack15bitsGray(Transform _1, ReadOnlySpan<ushort> Values, Span<byte> Buffer, uint _2)
    {
        BitConverter.TryWriteBytes(Buffer, From16To15(Values[0]));
        return Buffer[sizeof(ushort)..];
    }

    private static ReadOnlySpan<byte> Unroll15bitsRGB(Transform _1,
                                                      Span<ushort> Values,
                                                      ReadOnlySpan<byte> Buffer,
                                                      uint _2)
    {
        Values[0] = From15To16(BitConverter.ToUInt16(Buffer));
        Values[1] = From15To16(BitConverter.ToUInt16(Buffer[2..]));
        Values[2] = From15To16(BitConverter.ToUInt16(Buffer[4..]));

        return Buffer[6..];
    }

    private static Span<byte> Pack15bitsRGB(Transform _1, ReadOnlySpan<ushort> Values, Span<byte> Buffer, uint _2)
    {
        BitConverter.TryWriteBytes(Buffer, From16To15(Values[0]));
        BitConverter.TryWriteBytes(Buffer[2..], From16To15(Values[1]));
        BitConverter.TryWriteBytes(Buffer[4..], From16To15(Values[2]));
        return Buffer[6..];
    }

    private static ReadOnlySpan<byte> Unroll15bitsRGBA(Transform _1,
                                                       Span<ushort> Values,
                                                       ReadOnlySpan<byte> Buffer,
                                                       uint _2)
    {
        Values[0] = From15To16(BitConverter.ToUInt16(Buffer));
        Values[1] = From15To16(BitConverter.ToUInt16(Buffer[2..]));
        Values[2] = From15To16(BitConverter.ToUInt16(Buffer[4..]));

        // Skip [6..] for alpha
        return Buffer[8..];
    }

    private static Span<byte> Pack15bitsRGBA(Transform _1, ReadOnlySpan<ushort> Values, Span<byte> Buffer, uint _2)
    {
        BitConverter.TryWriteBytes(Buffer, From16To15(Values[0]));
        BitConverter.TryWriteBytes(Buffer[2..], From16To15(Values[1]));
        BitConverter.TryWriteBytes(Buffer[4..], From16To15(Values[2]));

        // Skip [6..] for alpha
        return Buffer[8..];
    }

    private static ReadOnlySpan<byte> Unroll15bitsBGR(Transform _1,
                                                      Span<ushort> Values,
                                                      ReadOnlySpan<byte> Buffer,
                                                      uint _2)
    {
        Values[2] = From15To16(BitConverter.ToUInt16(Buffer));
        Values[1] = From15To16(BitConverter.ToUInt16(Buffer[2..]));
        Values[0] = From15To16(BitConverter.ToUInt16(Buffer[4..]));

        return Buffer[6..];
    }

    private static Span<byte> Pack15bitsBGR(Transform _1, ReadOnlySpan<ushort> Values, Span<byte> Buffer, uint _2)
    {
        BitConverter.TryWriteBytes(Buffer, From16To15(Values[2]));
        BitConverter.TryWriteBytes(Buffer[2..], From16To15(Values[1]));
        BitConverter.TryWriteBytes(Buffer[4..], From16To15(Values[0]));
        return Buffer[6..];
    }

    private static ReadOnlySpan<byte> Unroll15bitsCMYK(Transform _1,
                                                       Span<ushort> Values,
                                                       ReadOnlySpan<byte> Buffer,
                                                       uint _2)
    {
        Values[0] = From15To16((ushort)(0x8000 - BitConverter.ToUInt16(Buffer)));
        Values[1] = From15To16((ushort)(0x8000 - BitConverter.ToUInt16(Buffer[2..])));
        Values[2] = From15To16((ushort)(0x8000 - BitConverter.ToUInt16(Buffer[4..])));
        Values[3] = From15To16((ushort)(0x8000 - BitConverter.ToUInt16(Buffer[6..])));

        return Buffer[8..];
    }

    private static Span<byte> Pack15bitsCMYK(Transform _1, ReadOnlySpan<ushort> Values, Span<byte> Buffer, uint _2)
    {
        BitConverter.TryWriteBytes(Buffer, (ushort)(0x8000 - From16To15(Values[0])));
        BitConverter.TryWriteBytes(Buffer[2..], (ushort)(0x8000 - From16To15(Values[1])));
        BitConverter.TryWriteBytes(Buffer[4..], (ushort)(0x8000 - From16To15(Values[2])));
        BitConverter.TryWriteBytes(Buffer[6..], (ushort)(0x8000 - From16To15(Values[3])));

        return Buffer[8..];
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort UnrollOne(ushort x, bool Reverse, bool SwapEndian) =>
        From15To16(
            (Reverse, SwapEndian) switch
            {
                (_, true) => (ushort)((x << 8) | (x >> 8)),
                (true, _) => (ushort)(0xffff - x),
                _         => x
            });

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort PackOne(ushort x, bool Reverse, bool SwapEndian) =>
        (Reverse, SwapEndian) switch
        {
            (true, _) => (ushort)(0xffff - From16To15(x)),
            (_, true) => CHANGE_ENDIAN(From16To15(x)),
            _         => From16To15(x)
        };

    private static ReadOnlySpan<byte> Unroll15bitsPlanar(Transform CMMcargo,
                                                         Span<ushort> wIn,
                                                         ReadOnlySpan<byte> accum,
                                                         uint Stride)
    {
        var head = (_xform_head)CMMcargo;
        var nChan = T_CHANNELS(head.InputFormat);
        var DoSwap = T_DOSWAP(head.InputFormat) is not 0;
        var Reverse = T_FLAVOR(head.InputFormat) is not 0;
        var SwapEndian = T_ENDIAN16(head.InputFormat) is not 0;
        var init = accum;

        if (DoSwap)
            accum = accum[(T_EXTRA(head.InputFormat) * (int)Stride * sizeof(ushort))..];

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? (nChan - i - 1) : i;

            wIn[index] = UnrollOne(BitConverter.ToUInt16(accum), Reverse, SwapEndian);

            accum = accum[(sizeof(ushort) * (int)Stride)..];
        }

        return init[sizeof(ushort)..];
    }

    private static Span<byte> Pack15bitsPlanar(Transform CMMcargo,
                                               ReadOnlySpan<ushort> wOut,
                                               Span<byte> output,
                                               uint Stride)
    {
        var head = (_xform_head)CMMcargo;
        var nChan = T_CHANNELS(head.OutputFormat);
        var DoSwap = T_DOSWAP(head.OutputFormat) is not 0;
        var Reverse = T_FLAVOR(head.OutputFormat) is not 0;
        var SwapEndian = T_ENDIAN16(head.OutputFormat) is not 0;
        var init = output;

        if (DoSwap)
            output = output[(T_EXTRA(head.OutputFormat) * (int)Stride * sizeof(ushort))..];

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? (nChan - i - 1) : i;

            BitConverter.TryWriteBytes(output, PackOne(wOut[index], Reverse, SwapEndian));

            output = output[(sizeof(ushort) * (int)Stride)..];
        }

        return init[sizeof(ushort)..];
    }

    private static ReadOnlySpan<byte> Unroll15bitsChunky(Transform CMMcargo,
                                                         Span<ushort> Values,
                                                         ReadOnlySpan<byte> Buffer,
                                                         uint _1)
    {
        var head = (_xform_head)CMMcargo;
        var nChan = T_CHANNELS(head.InputFormat);
        var DoSwap = T_DOSWAP(head.InputFormat) is not 0;
        var Reverse = T_FLAVOR(head.InputFormat) is not 0;
        var SwapEndian = T_ENDIAN16(head.InputFormat) is not 0;

        if (DoSwap)
            Buffer = Buffer[(T_EXTRA(head.InputFormat) * sizeof(ushort))..];

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? (nChan - i - 1) : i;

            Values[index] = UnrollOne(BitConverter.ToUInt16(Buffer), Reverse, SwapEndian);

            Buffer = Buffer[sizeof(ushort)..];
        }

        return Buffer;
    }

    private static Span<byte> Pack15bitsChunky(Transform CMMcargo,
                                               ReadOnlySpan<ushort> Values,
                                               Span<byte> Buffer,
                                               uint _1)
    {
        var head = (_xform_head)CMMcargo;
        var nChan = T_CHANNELS(head.OutputFormat);
        var DoSwap = T_DOSWAP(head.OutputFormat) is not 0;
        var Reverse = T_FLAVOR(head.OutputFormat) is not 0;
        var SwapEndian = T_ENDIAN16(head.OutputFormat) is not 0;

        if (DoSwap)
            Buffer = Buffer[(T_EXTRA(head.OutputFormat) * sizeof(ushort))..];

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? (nChan - i - 1) : i;

            BitConverter.TryWriteBytes(Buffer, PackOne(Values[index], Reverse, SwapEndian));

            Buffer = Buffer[sizeof(ushort)..];
        }

        return Buffer;
    }

    private static readonly int[] err = new int[Context.MaxChannels];

    private static Span<byte> PackNBytesDither(Transform CMMcargo,
                                               ReadOnlySpan<ushort> Values,
                                               Span<byte> Buffer,
                                               uint _1)
    {
        var info = (_xform_head)CMMcargo;
        var nChan = T_CHANNELS(info.OutputFormat);

        lock (err)
        {
            for (var i = 0; i < nChan; i++)
            {
                var n = Values[i] + err[i]; // Value

                var pe = n / 257; // Whole part
                var pf = n % 257; // Fractional part

                err[i] = pf;    // Store it for next pixel

                Buffer[0] = (byte)pe;

                Buffer = Buffer[1..];
            }
        }

        return Buffer[T_EXTRA(info.OutputFormat)..];
    }

    private static Span<byte> PackNBytesSwapDither(Transform CMMcargo,
                                                   ReadOnlySpan<ushort> Values,
                                                   Span<byte> Buffer,
                                                   uint _1)
    {
        var info = (_xform_head)CMMcargo;
        var nChan = T_CHANNELS(info.OutputFormat);

        lock (err)
        {
            for (var i = nChan - 1; i >= 0; i--)
            {
                var n = Values[i] + err[i]; // Value

                var pe = n / 257; // Whole part
                var pf = n % 257; // Fractional part

                err[i] = pf;    // Store it for next pixel

                Buffer[0] = (byte)pe;

                Buffer = Buffer[1..];
            }
        }

        return Buffer[T_EXTRA(info.OutputFormat)..];
    }

    internal static FormatterIn Formatter_15Bit_Factory_In(uint Type, uint dwFlags)
    {
        FormatterIn Result = default;

        // Simple Gray
        if (Type == TYPE_GRAY_15)
            Result.Fmt16 = Unroll15bitsGray;

        // 3 channels
        if (Type == TYPE_CMY_15 ||
            Type == TYPE_RGB_15)
        {
            Result.Fmt16 = Unroll15bitsRGB;
        }

        // 3 channels reversed
        if (Type == TYPE_YMC_15 ||
            Type == TYPE_BGR_15)
        {
            Result.Fmt16 = Unroll15bitsBGR;
        }

        // 3 Channels plus one alpha
        if (Type == TYPE_RGBA_15)
            Result.Fmt16 = Unroll15bitsRGBA;

        // 4 channels
        if (Type == TYPE_CMYK_15)
            Result.Fmt16 = Unroll15bitsCMYK;

        // Planar versions
        if (Type == TYPE_GRAYA_15_PLANAR ||
            Type == TYPE_RGB_15_PLANAR ||
            Type == TYPE_BGR_15_PLANAR ||
            Type == TYPE_RGBA_15_PLANAR ||
            Type == TYPE_ABGR_15_PLANAR ||
            Type == TYPE_CMY_15_PLANAR ||
            Type == TYPE_CMYK_15_PLANAR)
        {
            Result.Fmt16 = Unroll15bitsPlanar;
        }

        // Fallthrough for remaining (corner) cases
        if (Type == TYPE_GRAY_15_REV ||
            Type == TYPE_GRAY_15_SE ||
            Type == TYPE_GRAYA_15 ||
            Type == TYPE_GRAYA_15_SE ||
            Type == TYPE_RGB_15_SE ||
            Type == TYPE_BGR_15_SE ||
            Type == TYPE_RGBA_15_SE ||
            Type == TYPE_ARGB_15 ||
            Type == TYPE_ABGR_15 ||
            Type == TYPE_ABGR_15_SE ||
            Type == TYPE_BGRA_15 ||
            Type == TYPE_BGRA_15_SE ||
            Type == TYPE_CMY_15_SE ||
            Type == TYPE_CMYK_15_REV ||
            Type == TYPE_CMYK_15_SE ||
            Type == TYPE_KYMC_15 ||
            Type == TYPE_KYMC_15_SE ||
            Type == TYPE_KCMY_15 ||
            Type == TYPE_KCMY_15_REV ||
            Type == TYPE_KCMY_15_SE)
        {
            Result.Fmt16 = Unroll15bitsChunky;
        }

        return Result;
    }

    internal static FormatterOut Formatter_15Bit_Factory_Out(uint Type, uint dwFlags)
    {
        FormatterOut Result = default;

        // Simple Gray
        if (Type == TYPE_GRAY_15)
            Result.Fmt16 = Pack15bitsGray;

        // 3 channels
        if (Type == TYPE_CMY_15 ||
            Type == TYPE_RGB_15)
        {
            Result.Fmt16 = Pack15bitsRGB;
        }

        // 3 channels reversed
        if (Type == TYPE_YMC_15 ||
            Type == TYPE_BGR_15)
        {
            Result.Fmt16 = Pack15bitsBGR;
        }

        // 3 Channels plus one alpha
        if (Type == TYPE_RGBA_15)
            Result.Fmt16 = Pack15bitsRGBA;

        // 4 channels
        if (Type == TYPE_CMYK_15)
            Result.Fmt16 = Pack15bitsCMYK;

        // Planar versions
        if (Type == TYPE_GRAYA_15_PLANAR ||
            Type == TYPE_RGB_15_PLANAR ||
            Type == TYPE_BGR_15_PLANAR ||
            Type == TYPE_RGBA_15_PLANAR ||
            Type == TYPE_ABGR_15_PLANAR ||
            Type == TYPE_CMY_15_PLANAR ||
            Type == TYPE_CMYK_15_PLANAR)
        {
            Result.Fmt16 = Pack15bitsPlanar;
        }

        // Fallthrough for remaining (corner) cases
        if (Type == TYPE_GRAY_15_REV ||
            Type == TYPE_GRAY_15_SE ||
            Type == TYPE_GRAYA_15 ||
            Type == TYPE_GRAYA_15_SE ||
            Type == TYPE_RGB_15_SE ||
            Type == TYPE_BGR_15_SE ||
            Type == TYPE_RGBA_15_SE ||
            Type == TYPE_ARGB_15 ||
            Type == TYPE_ABGR_15 ||
            Type == TYPE_ABGR_15_SE ||
            Type == TYPE_BGRA_15 ||
            Type == TYPE_BGRA_15_SE ||
            Type == TYPE_CMY_15_SE ||
            Type == TYPE_CMYK_15_REV ||
            Type == TYPE_CMYK_15_SE ||
            Type == TYPE_KYMC_15 ||
            Type == TYPE_KYMC_15_SE ||
            Type == TYPE_KCMY_15 ||
            Type == TYPE_KCMY_15_REV ||
            Type == TYPE_KCMY_15_SE)
        {
            Result.Fmt16 = Pack15bitsChunky;
        }

        if (Type == TYPE_GRAY_8_DITHER ||
            Type == TYPE_RGB_8_DITHER ||
            Type == TYPE_RGBA_8_DITHER ||
            Type == TYPE_CMYK_8_DITHER)
        {
            Result.Fmt16 = PackNBytesDither;
        }

        if (Type == TYPE_ABGR_8_DITHER ||
            Type == TYPE_BGR_8_DITHER ||
            Type == TYPE_KYMC_8_DITHER)
        {
            Result.Fmt16 = PackNBytesSwapDither;
        }

        return Result;
    }
}
