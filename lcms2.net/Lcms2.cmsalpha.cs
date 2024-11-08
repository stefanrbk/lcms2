//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright ©️ 1998-2024 Marti Maria Saguer
//              2022-2024 Stefan Kewatt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software
// is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//---------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

using static System.BitConverter;

namespace lcms2;

public static partial class Lcms2
{
    // CHANGE_ENDIAN already defined in Lcms2.cmspack.cs

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte _cmsQuickSaturateByte(double d) =>
        (byte)QuickSaturateWord(Math.Max(Math.Min(d + 0.5, 255), 0));

    private static uint trueBytesSize(uint Format) =>
        PixelSize(Format);

    private static void copy8(Span<byte> dst, ReadOnlySpan<byte> src) =>
        dst[0] = src[0];

    private static void from8to16(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, FROM_8_TO_16(src[0]));

    private static void from8to16SE(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, CHANGE_ENDIAN(FROM_8_TO_16(src[0])));

    private static void from8toFLT(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, src[0] / 255f);

    private static void from8toDBL(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, src[0] / 255d);

    private static void from8toHLF(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (Half)(src[0] / 255f));

    private static void from16to8(Span<byte> dst, ReadOnlySpan<byte> src) =>
        dst[0] = FROM_16_TO_8(ToUInt16(src));

    private static void from16SEto8(Span<byte> dst, ReadOnlySpan<byte> src) =>
        dst[0] = FROM_16_TO_8(CHANGE_ENDIAN(ToUInt16(src)));

    private static void copy16(Span<byte> dst, ReadOnlySpan<byte> src) =>
        src[..2].CopyTo(dst);

    private static void from16to16(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, CHANGE_ENDIAN(ToUInt16(src)));

    private static void from16toFLT(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, ToUInt16(src) / 255f);

    private static void from16SEtoFLT(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, CHANGE_ENDIAN(ToUInt16(src)) / 255f);

    private static void from16toDBL(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, ToUInt16(src) / 255d);

    private static void from16SEtoDBL(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, CHANGE_ENDIAN(ToUInt16(src)) / 255f);

    private static void from16toHLF(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (Half)(ToUInt16(src) / 255f));

    private static void from16SEtoHLF(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (Half)(CHANGE_ENDIAN(ToUInt16(src)) / 255f));

    private static void fromFLTto8(Span<byte> dst, ReadOnlySpan<byte> src) =>
        dst[0] = _cmsQuickSaturateByte(ToSingle(src) * 255.0);

    private static void fromFLTto16(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, QuickSaturateWord(ToSingle(src) * 65535f));

    private static void fromFLTto16SE(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, CHANGE_ENDIAN(QuickSaturateWord(ToSingle(src) * 65535f)));

    private static void copy32(Span<byte> dst, ReadOnlySpan<byte> src) =>
        src[..4].CopyTo(dst);

    private static void fromFLTtoDBL(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, ToSingle(src));

    private static void fromFLTtoHLF(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (Half)ToSingle(src));

    private static void fromHLFto8(Span<byte> dst, ReadOnlySpan<byte> src) =>
        dst[0] = _cmsQuickSaturateByte((double)ToHalf(src) * 255.0);

    private static void fromHLFto16(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, QuickSaturateWord((float)ToHalf(src) * 65535f));

    private static void fromHLFto16SE(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, CHANGE_ENDIAN(QuickSaturateWord((float)ToHalf(src) * 65535f)));

    private static void fromHLFtoFLT(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (float)ToHalf(src));

    private static void fromHLFtoDBL(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (double)ToHalf(src));

    private static void fromDBLto8(Span<byte> dst, ReadOnlySpan<byte> src) =>
        dst[0] = _cmsQuickSaturateByte(ToDouble(src) * 255.0);

    private static void fromDBLto16(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, QuickSaturateWord(ToDouble(src) * 65535f));

    private static void fromDBLto16SE(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, CHANGE_ENDIAN(QuickSaturateWord(ToDouble(src) * 65535f)));

    private static void fromDBLtoFLT(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (float)ToDouble(src));

    private static void fromDBLtoHLF(Span<byte> dst, ReadOnlySpan<byte> src) =>
        TryWriteBytes(dst, (Half)ToDouble(src));

    private static void copy64(Span<byte> dst, ReadOnlySpan<byte> src) =>
        src[..8].CopyTo(dst);

    private static int FormatterPos(uint frm) =>
        (T_BYTES(frm), T_FLOAT(frm) is not 0) switch
        {
            (0, true) => 5, // DBL
            (2, true) => 3, // HLF
            (4, true) => 4, // FLT
            (2, false) =>
                T_ENDIAN16(frm) is not 0
                    ? 2      // 16SE
                    : 1,     // 16
            (1, false) => 0, // 8
            _          => -1 // not recognized
        };

    private static readonly FormatterAlphaFn[,] FormattersAlpha = new FormatterAlphaFn[,]
    {
        { copy8, from8to16, from8to16SE, from8toHLF, from8toFLT, from8toDBL },
        { from16to8, copy16, from16to16, from16toHLF, from16toFLT, from16toDBL },
        { from16SEto8, from16to16, copy16, from16SEtoHLF, from16SEtoFLT, from16SEtoDBL },
        { fromHLFto8, fromHLFto16, fromHLFto16SE, copy16, fromHLFtoFLT, fromHLFtoDBL },
        { fromFLTto8, fromFLTto16, fromFLTto16SE, fromFLTtoHLF, copy32, fromFLTtoDBL },
        { fromDBLto8, fromDBLto16, fromDBLto16SE, fromDBLtoHLF, fromDBLtoFLT, copy64 },
    };

    internal static FormatterAlphaFn? _cmsGetFormatterAlpha(Context? id, uint @in, uint @out)
    {
        var in_n = FormatterPos(@in);
        var out_n = FormatterPos(@out);

        if (in_n is < 0 or > 5 || out_n is < 0 or > 5)
        {
            Context.LogError(id, cmsERROR_UNKNOWN_EXTENSION, "Unrecognized alpha channel width");
            return null;
        }

        return FormattersAlpha[in_n, out_n];
    }

    private static bool ComputeIncrementsForChunky(uint Format,
                                                   Span<uint> ComponentStartingOrder,
                                                   Span<uint> ComponentPointerIncrements)
    {
        Span<uint> channels = stackalloc uint[Context.MaxChannels];
        var extra = T_EXTRA(Format);
        var nchannels = T_CHANNELS(Format);
        var total_chans = nchannels + extra;
        var channelSize = (int)PixelSize(Format);
        var pixelSize = channelSize * total_chans;

        // Sanity check
        if (total_chans is <= 0 or >= Context.MaxChannels)
            return false;

        //memset(channels, 0, cmsMAXCHANNELS * sizeof(uint));

        // Separation is independent of starting point and only depends on channel size
        for (var i = 0; i < extra; i++)
            ComponentPointerIncrements[i] = (uint)pixelSize;

        // Handle do swap
        for (var i = 0; i < total_chans; i++)
            channels[i] = (uint)(T_DOSWAP(Format) is not 0 ? total_chans - i - 1 : i);

        // Handle swap first (ROL of positions), example CMYK -> KCMY | 0123 ->3012
        if (T_SWAPFIRST(Format) is not 0 && total_chans > 1)
        {
            var tmp = channels[0];
            for (var i = 0; i < total_chans - 1; i++)
                channels[i] = channels[i + 1];
            channels[total_chans - 1] = tmp;
        }

        // Handle size
        if (channelSize > 1)
        {
            for (var i = 0; i < total_chans; i++)
                channels[i] *= (uint)channelSize;
        }

        for (var i = 0; i < extra; i++)
            ComponentStartingOrder[i] = channels[i + nchannels];

        return true;
    }

    private static bool ComputeIncrementsForPlanar(uint Format,
                                                   uint BytesPerPlane,
                                                   Span<uint> ComponentStartingOrder,
                                                   Span<uint> ComponentPointerIncrements)
    {
        Span<uint> channels = stackalloc uint[Context.MaxChannels];
        var extra = T_EXTRA(Format);
        var nchannels = T_CHANNELS(Format);
        var total_chans = nchannels + extra;
        var channelSize = PixelSize(Format);

        // Sanity check
        if (total_chans is <= 0 or >= Context.MaxChannels)
            return false;

        //memset(channels, 0, cmsMAXCHANNELS * sizeof(uint));

        // Separation is independent of starting point and only depends on channel size
        for (var i = 0; i < extra; i++)
            ComponentPointerIncrements[i] = channelSize;

        // Handle do swap
        for (var i = 0u; i < total_chans; i++)
            channels[(int)i] = (uint)(T_DOSWAP(Format) is not 0 ? total_chans - i - 1 : i);

        // Handle swap first (ROL of positions), example CMYK -> KCMY | 0123 ->3012
        if (T_SWAPFIRST(Format) is not 0 && total_chans > 1)
        {
            var tmp = channels[0];
            for (var i = 0; i < total_chans - 1; i++)
                channels[i] = channels[i + 1];
            channels[(int)total_chans - 1] = tmp;
        }

        // Handle size
        for (var i = 0; i < total_chans; i++)
            channels[i] *= BytesPerPlane;

        for (var i = 0; i < extra; i++)
            ComponentStartingOrder[i] = channels[i + (int)nchannels];

        return true;
    }

    private static bool ComputeComponentIncrements(uint Format,
                                                   uint BytesPerPlane,
                                                   Span<uint> ComponentStartingOrder,
                                                   Span<uint> ComponentPointerIncrements)
    {
        if (T_PLANAR(Format) is not 0)
        {
            return ComputeIncrementsForPlanar(
                Format,
                BytesPerPlane,
                ComponentStartingOrder,
                ComponentPointerIncrements);
        }
        else
        {
            return ComputeIncrementsForChunky(Format, ComponentStartingOrder, ComponentPointerIncrements);
        }
    }

    internal static void _cmsHandleExtraChannels(Transform p,
                                                 ReadOnlySpan<byte> @in,
                                                 Span<byte> @out,
                                                 uint PixelsPerLine,
                                                 uint LineCount,
                                                 Stride Stride)
    {
        Span<uint> SourceStartingOrder = stackalloc uint[Context.MaxChannels];
        Span<uint> SourceIncrements = stackalloc uint[Context.MaxChannels];
        Span<uint> DestStartingOrder = stackalloc uint[Context.MaxChannels];
        Span<uint> DestIncrements = stackalloc uint[Context.MaxChannels];

        // Make sure we need some copy
        if ((p.dwOriginalFlags & cmsFLAGS_COPY_ALPHA) is 0)
            return;

        // Exit early if in-place color-management is occurring - no need to copy extra channels to themselves.
        if (p.InputFormat == p.OutputFormat && @in == @out)
            return;

        // Make sure we have same number of alpha channels. If not, just return as this should be checked at transform creation time.
        var nExtra = T_EXTRA(p.InputFormat);
        if (nExtra != T_EXTRA(p.OutputFormat))
            return;

        // Anything to do?
        if (nExtra is 0)
            return;

        // Compute the increments
        if (!ComputeComponentIncrements(p.InputFormat, Stride.BytesPerPlaneIn, SourceStartingOrder, SourceIncrements))
            return;
        if (!ComputeComponentIncrements(p.OutputFormat, Stride.BytesPerPlaneOut, DestStartingOrder, DestIncrements))
            return;

        // Check for conversions 8, 16, half, float, double
        var copyValueFn = _cmsGetFormatterAlpha(p.ContextID, p.InputFormat, p.OutputFormat);
        if (copyValueFn is null)
            return;

        if (nExtra is 1)    // Optimized routine for copying a single extra channel quickly
        {
            nuint SourceStrideIncrement = 0;
            nuint DestStrideIncrement = 0;

            // The loop itself
            for (var i = 0; i < LineCount; i++)
            {
                // Prepare pointers for the loop
                var SourcePtr = (int)(SourceStartingOrder[0] + SourceStrideIncrement);
                var DestPtr = (int)(DestStartingOrder[0] + DestStrideIncrement);

                for (var j = 0; j < PixelsPerLine; j++)
                {
                    copyValueFn(@out[DestPtr..], @in[SourcePtr..]);

                    SourcePtr += (int)SourceIncrements[0];
                    DestPtr += (int)DestIncrements[0];
                }

                SourceStrideIncrement += Stride.BytesPerLineIn;
                DestStrideIncrement += Stride.BytesPerPlaneOut;
            }
        }
        else            // General case with more than one extra channel
        {
            Span<int> SourcePtr = stackalloc int[Context.MaxChannels];
            Span<int> DestPtr = stackalloc int[Context.MaxChannels];
            Span<nuint> SourceStrideIncrements = stackalloc nuint[Context.MaxChannels];
            Span<nuint> DestStrideIncrements = stackalloc nuint[Context.MaxChannels];

            //memset(SourceStrideIncrements, 0, sizeof(uint) * cmsMAXCHANNELS);
            //memset(DestStrideIncrements, 0, sizeof(uint) * cmsMAXCHANNELS);

            // The loop itself
            for (var i = 0; i < LineCount; i++)
            {
                // Prepare pointers for the loop
                for (var j = 0; j < nExtra; j++)
                {
                    SourcePtr[j] = (int)(SourceStartingOrder[j] + SourceStrideIncrements[j]);
                    DestPtr[j] = (int)(DestStartingOrder[j] + DestStrideIncrements[j]);
                }

                for (var j = 0; j < PixelsPerLine; j++)
                {
                    for (var k = 0; k < nExtra; k++)
                    {
                        copyValueFn(@out[DestPtr[k]..], @in[SourcePtr[k]..]);

                        SourcePtr[k] += (int)SourceIncrements[k];
                        DestPtr[k] += (int)DestIncrements[k];
                    }
                }

                for (var j = 0; j < nExtra; j++)
                {
                    SourceStrideIncrements[j] += Stride.BytesPerLineIn;
                    DestStrideIncrements[j] += Stride.BytesPerLineOut;
                }
            }
        }
    }
}
