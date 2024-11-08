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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using lcms2.types;

namespace lcms2;

public static partial class Lcms2
{
    internal static readonly FormattersPluginChunkType FormattersPluginChunk = new();

    internal static readonly FormattersPluginChunkType globalFormattersPluginChunk = new();

    internal static readonly Formatters16In[] InputFormatters16 =
    {
        new(TYPE_Lab_DBL,                                 ANYPLANAR | ANYEXTRA,   UnrollLabDoubleTo16),
        new(TYPE_XYZ_DBL,                                 ANYPLANAR | ANYEXTRA,   UnrollXYZDoubleTo16),
        new(TYPE_Lab_FLT,                                 ANYPLANAR | ANYEXTRA,   UnrollLabFloatTo16),
        new(TYPE_XYZ_FLT,                                 ANYPLANAR | ANYEXTRA,   UnrollXYZFloatTo16),
        new(TYPE_GRAY_DBL,                                                 0,   UnrollDouble1Chan),
        new(
            FLOAT_SH(1) | BYTES_SH(0),
            ANYCHANNELS | ANYPLANAR | ANYSWAPFIRST | ANYFLAVOR |
            ANYSWAP | ANYEXTRA | ANYSPACE,
            UnrollDoubleTo16),
        new(
            FLOAT_SH(1) | BYTES_SH(4),
            ANYCHANNELS | ANYPLANAR | ANYSWAPFIRST | ANYFLAVOR |
            ANYSWAP | ANYEXTRA | ANYSPACE,
            UnrollFloatTo16),
        new(
            FLOAT_SH(1) | BYTES_SH(2),
            ANYCHANNELS | ANYPLANAR | ANYSWAPFIRST | ANYFLAVOR |
            ANYEXTRA | ANYSWAP | ANYSPACE,
            UnrollHalfTo16),
        new(CHANNELS_SH(1) | BYTES_SH(1),                              ANYSPACE,  Unroll1Byte),
        new(CHANNELS_SH(1) | BYTES_SH(1) | EXTRA_SH(1),                  ANYSPACE,  Unroll1ByteSkip1),
        new(CHANNELS_SH(1) | BYTES_SH(1) | EXTRA_SH(2),                  ANYSPACE,  Unroll1ByteSkip2),
        new(CHANNELS_SH(1) | BYTES_SH(1) | FLAVOR_SH(1),                 ANYSPACE,  Unroll1ByteReversed),
        new(COLORSPACE_SH(PT_MCH2) | CHANNELS_SH(2) | BYTES_SH(1),              0,  Unroll2Bytes),
        new(TYPE_LabV2_8,                                                   0,  UnrollLabV2_8),
        new(TYPE_ALabV2_8,                                                  0,  UnrollALabV2_8),
        new(TYPE_LabV2_16,                                                  0,  UnrollLabV2_16),
        new(CHANNELS_SH(3) | BYTES_SH(1),                              ANYSPACE,  Unroll3Bytes),
        new(CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1),                 ANYSPACE,  Unroll3BytesSwap),
        new(CHANNELS_SH(3) | EXTRA_SH(1) | BYTES_SH(1) | DOSWAP_SH(1),     ANYSPACE,  Unroll3BytesSkip1Swap),
        new(CHANNELS_SH(3) | EXTRA_SH(1) | BYTES_SH(1) | SWAPFIRST_SH(1),  ANYSPACE,  Unroll3BytesSkip1SwapFirst),
        new(
            CHANNELS_SH(3) | EXTRA_SH(1) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1),
            ANYSPACE,
            Unroll3BytesSkip1SwapSwapFirst),
        new(CHANNELS_SH(4) | BYTES_SH(1),                              ANYSPACE,  Unroll4Bytes),
        new(CHANNELS_SH(4) | BYTES_SH(1) | FLAVOR_SH(1),                 ANYSPACE,  Unroll4BytesReverse),
        new(CHANNELS_SH(4) | BYTES_SH(1) | SWAPFIRST_SH(1),              ANYSPACE,  Unroll4BytesSwapFirst),
        new(CHANNELS_SH(4) | BYTES_SH(1) | DOSWAP_SH(1),                 ANYSPACE,  Unroll4BytesSwap),
        new(CHANNELS_SH(4) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1), ANYSPACE,  Unroll4BytesSwapSwapFirst),
        new(
            BYTES_SH(1) | PLANAR_SH(1),
            ANYFLAVOR | ANYSWAPFIRST | ANYPREMUL |
            ANYSWAP | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            UnrollPlanarBytes),
        new(
            BYTES_SH(1),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYPREMUL |
            ANYEXTRA | ANYCHANNELS | ANYSPACE,
            UnrollChunkyBytes),
        new(CHANNELS_SH(1) | BYTES_SH(2),                              ANYSPACE,  Unroll1Word),
        new(CHANNELS_SH(1) | BYTES_SH(2) | FLAVOR_SH(1),                 ANYSPACE,  Unroll1WordReversed),
        new(CHANNELS_SH(1) | BYTES_SH(2) | EXTRA_SH(3),                  ANYSPACE,  Unroll1WordSkip3),
        new(CHANNELS_SH(2) | BYTES_SH(2),                              ANYSPACE,  Unroll2Words),
        new(CHANNELS_SH(3) | BYTES_SH(2),                              ANYSPACE,  Unroll3Words),
        new(CHANNELS_SH(4) | BYTES_SH(2),                              ANYSPACE,  Unroll4Words),
        new(CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1),                 ANYSPACE,  Unroll3WordsSwap),
        new(CHANNELS_SH(3) | BYTES_SH(2) | EXTRA_SH(1) | SWAPFIRST_SH(1),  ANYSPACE,  Unroll3WordsSkip1SwapFirst),
        new(CHANNELS_SH(3) | BYTES_SH(2) | EXTRA_SH(1) | DOSWAP_SH(1),     ANYSPACE,  Unroll3WordsSkip1Swap),
        new(CHANNELS_SH(4) | BYTES_SH(2) | FLAVOR_SH(1),                 ANYSPACE,  Unroll4WordsReverse),
        new(CHANNELS_SH(4) | BYTES_SH(2) | SWAPFIRST_SH(1),              ANYSPACE,  Unroll4WordsSwapFirst),
        new(CHANNELS_SH(4) | BYTES_SH(2) | DOSWAP_SH(1),                 ANYSPACE,  Unroll4WordsSwap),
        new(CHANNELS_SH(4) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1), ANYSPACE,  Unroll4WordsSwapSwapFirst),
        new(
            BYTES_SH(2) | PLANAR_SH(1),
            ANYFLAVOR | ANYSWAP | ANYENDIAN | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            UnrollPlanarWords),
        new(
            BYTES_SH(2),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYENDIAN | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            UnrollAnyWords),
        new(
            BYTES_SH(2) | PLANAR_SH(1),
            ANYFLAVOR | ANYSWAP | ANYENDIAN | ANYEXTRA | ANYCHANNELS | ANYSPACE | PREMUL_SH(1),
            UnrollPlanarWordsPremul),
        new(
            BYTES_SH(2),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYENDIAN | ANYEXTRA | ANYCHANNELS | ANYSPACE | PREMUL_SH(1),
            UnrollAnyWordsPremul),
    };

    internal static readonly FormattersFloatIn[] InputFormattersFloat =
    {
        //    Type                                          Mask                  Function
        //  ----------------------------   ------------------------------------  ----------------------------
        new(     TYPE_Lab_DBL,                                ANYPLANAR | ANYEXTRA,   UnrollLabDoubleToFloat),
        new(     TYPE_Lab_FLT,                                ANYPLANAR | ANYEXTRA,   UnrollLabFloatToFloat),
        new(     TYPE_XYZ_DBL,                                ANYPLANAR | ANYEXTRA,   UnrollXYZDoubleToFloat),
        new(     TYPE_XYZ_FLT,                                ANYPLANAR | ANYEXTRA,   UnrollXYZFloatToFloat),
        new(
            FLOAT_SH(1) | BYTES_SH(4),
            ANYPLANAR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA |
            ANYPREMUL | ANYCHANNELS | ANYSPACE,
            UnrollFloatsToFloat),
        new(
            FLOAT_SH(1) | BYTES_SH(0),
            ANYPLANAR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA |
            ANYCHANNELS | ANYSPACE | ANYPREMUL,
            UnrollDoublesToFloat),
        new(     TYPE_LabV2_8,                                                   0,  UnrollLabV2_8ToFloat),
        new(     TYPE_ALabV2_8,                                                  0,  UnrollALabV2_8ToFloat),
        new(     TYPE_LabV2_16,                                                  0,  UnrollLabV2_16ToFloat),
        new(
            BYTES_SH(1),
            ANYPLANAR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA |
            ANYCHANNELS | ANYSPACE,
            Unroll8ToFloat),
        new(
            BYTES_SH(2),
            ANYPLANAR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA |
            ANYCHANNELS | ANYSPACE,
            Unroll16ToFloat),
        new(
            FLOAT_SH(1) | BYTES_SH(2),
            ANYPLANAR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA |
            ANYCHANNELS | ANYSPACE,
            UnrollHalfToFloat),
    };

    internal static readonly Formatters16Out[] OutputFormatters16 =
    {
        //    Type                                          Mask                  Function
        //  ----------------------------   ------------------------------------  ----------------------------

        new(TYPE_Lab_DBL,                                      ANYPLANAR | ANYEXTRA,  PackLabDoubleFrom16),
        new(TYPE_XYZ_DBL,                                      ANYPLANAR | ANYEXTRA,  PackXYZDoubleFrom16),
        new(TYPE_Lab_FLT,                                      ANYPLANAR | ANYEXTRA,  PackLabFloatFrom16),
        new(TYPE_XYZ_FLT,                                      ANYPLANAR | ANYEXTRA,  PackXYZFloatFrom16),
        new(
            FLOAT_SH(1) | BYTES_SH(0),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP |
            ANYCHANNELS | ANYPLANAR | ANYEXTRA | ANYSPACE,
            PackDoubleFrom16),
        new(
            FLOAT_SH(1) | BYTES_SH(4),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP |
            ANYCHANNELS | ANYPLANAR | ANYEXTRA | ANYSPACE,
            PackFloatFrom16),
        new(
            FLOAT_SH(1) | BYTES_SH(2),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP |
            ANYCHANNELS | ANYPLANAR | ANYEXTRA | ANYSPACE,
            PackHalfFrom16),
        new(CHANNELS_SH(1) | BYTES_SH(1),                                  ANYSPACE,  Pack1Byte),
        new(CHANNELS_SH(1) | BYTES_SH(1) | EXTRA_SH(1),                      ANYSPACE,  Pack1ByteSkip1),
        new(CHANNELS_SH(1) | BYTES_SH(1) | EXTRA_SH(1) | SWAPFIRST_SH(1),      ANYSPACE,  Pack1ByteSkip1SwapFirst),
        new(CHANNELS_SH(1) | BYTES_SH(1) | FLAVOR_SH(1),                     ANYSPACE,  Pack1ByteReversed),
        new(TYPE_LabV2_8,                                                       0,  PackLabV2_8),
        new(TYPE_ALabV2_8,                                                      0,  PackALabV2_8),
        new(TYPE_LabV2_16,                                                      0,  PackLabV2_16),
        new(CHANNELS_SH(3) | BYTES_SH(1) | OPTIMIZED_SH(1),                  ANYSPACE,  Pack3BytesOptimized),
        new(CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | OPTIMIZED_SH(1),      ANYSPACE,  Pack3BytesAndSkip1Optimized),
        new(
            CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | SWAPFIRST_SH(1) | OPTIMIZED_SH(1),
            ANYSPACE,
            Pack3BytesAndSkip1SwapFirstOptimized),
        new(
            CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1) | OPTIMIZED_SH(1),
            ANYSPACE,
            Pack3BytesAndSkip1SwapSwapFirstOptimized),
        new(
            CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | EXTRA_SH(1) | OPTIMIZED_SH(1),
            ANYSPACE,
            Pack3BytesAndSkip1SwapOptimized),
        new(CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | OPTIMIZED_SH(1),     ANYSPACE,  Pack3BytesSwapOptimized),
        new(CHANNELS_SH(3) | BYTES_SH(1),                                  ANYSPACE,  Pack3Bytes),
        new(CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1),                      ANYSPACE,  Pack3BytesAndSkip1),
        new(CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | SWAPFIRST_SH(1),      ANYSPACE,  Pack3BytesAndSkip1SwapFirst),
        new(
            CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1),
            ANYSPACE,
            Pack3BytesAndSkip1SwapSwapFirst),
        new(CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | EXTRA_SH(1),         ANYSPACE,  Pack3BytesAndSkip1Swap),
        new(CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1),                     ANYSPACE,  Pack3BytesSwap),
        new(CHANNELS_SH(4) | BYTES_SH(1),                                  ANYSPACE,  Pack4Bytes),
        new(CHANNELS_SH(4) | BYTES_SH(1) | FLAVOR_SH(1),                     ANYSPACE,  Pack4BytesReverse),
        new(CHANNELS_SH(4) | BYTES_SH(1) | SWAPFIRST_SH(1),                  ANYSPACE,  Pack4BytesSwapFirst),
        new(CHANNELS_SH(4) | BYTES_SH(1) | DOSWAP_SH(1),                     ANYSPACE,  Pack4BytesSwap),
        new(CHANNELS_SH(4) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1),     ANYSPACE,  Pack4BytesSwapSwapFirst),
        new(CHANNELS_SH(6) | BYTES_SH(1),                                  ANYSPACE,  Pack6Bytes),
        new(CHANNELS_SH(6) | BYTES_SH(1) | DOSWAP_SH(1),                     ANYSPACE,  Pack6BytesSwap),
        new(
            BYTES_SH(1),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA | ANYCHANNELS |
            ANYSPACE | ANYPREMUL,
            PackChunkyBytes),
        new(
            BYTES_SH(1) | PLANAR_SH(1),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA |
            ANYCHANNELS | ANYSPACE | ANYPREMUL,
            PackPlanarBytes),
        new(CHANNELS_SH(1) | BYTES_SH(2),                                  ANYSPACE,  Pack1Word),
        new(CHANNELS_SH(1) | BYTES_SH(2) | EXTRA_SH(1),                      ANYSPACE,  Pack1WordSkip1),
        new(CHANNELS_SH(1) | BYTES_SH(2) | EXTRA_SH(1) | SWAPFIRST_SH(1),      ANYSPACE,  Pack1WordSkip1SwapFirst),
        new(CHANNELS_SH(1) | BYTES_SH(2) | FLAVOR_SH(1),                     ANYSPACE,  Pack1WordReversed),
        new(CHANNELS_SH(1) | BYTES_SH(2) | ENDIAN16_SH(1),                   ANYSPACE,  Pack1WordBigEndian),
        new(CHANNELS_SH(3) | BYTES_SH(2),                                  ANYSPACE,  Pack3Words),
        new(CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1),                     ANYSPACE,  Pack3WordsSwap),
        new(CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1),                   ANYSPACE,  Pack3WordsBigEndian),
        new(CHANNELS_SH(3) | BYTES_SH(2) | EXTRA_SH(1),                      ANYSPACE,  Pack3WordsAndSkip1),
        new(CHANNELS_SH(3) | BYTES_SH(2) | EXTRA_SH(1) | DOSWAP_SH(1),         ANYSPACE,  Pack3WordsAndSkip1Swap),
        new(CHANNELS_SH(3) | BYTES_SH(2) | EXTRA_SH(1) | SWAPFIRST_SH(1),      ANYSPACE,  Pack3WordsAndSkip1SwapFirst),
        new(
            CHANNELS_SH(3) | BYTES_SH(2) | EXTRA_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1),
            ANYSPACE,
            Pack3WordsAndSkip1SwapSwapFirst),
        new(CHANNELS_SH(4) | BYTES_SH(2),                                  ANYSPACE,  Pack4Words),
        new(CHANNELS_SH(4) | BYTES_SH(2) | FLAVOR_SH(1),                     ANYSPACE,  Pack4WordsReverse),
        new(CHANNELS_SH(4) | BYTES_SH(2) | DOSWAP_SH(1),                     ANYSPACE,  Pack4WordsSwap),
        new(CHANNELS_SH(4) | BYTES_SH(2) | ENDIAN16_SH(1),                   ANYSPACE,  Pack4WordsBigEndian),
        new(CHANNELS_SH(6) | BYTES_SH(2),                                  ANYSPACE,  Pack6Words),
        new(CHANNELS_SH(6) | BYTES_SH(2) | DOSWAP_SH(1),                     ANYSPACE,  Pack6WordsSwap),
        new(
            BYTES_SH(2),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYENDIAN |
            ANYEXTRA | ANYCHANNELS | ANYSPACE | ANYPREMUL,
            PackChunkyWords),
        new(
            BYTES_SH(2) | PLANAR_SH(1),
            ANYFLAVOR | ANYENDIAN | ANYSWAP | ANYEXTRA |
            ANYCHANNELS | ANYSPACE | ANYPREMUL,
            PackPlanarWords),
    };

    internal static readonly FormattersFloatOut[] OutputFormattersFloat =
    {
        //    Type                                          Mask                                 Function
        //  ----------------------------   ---------------------------------------------------  ----------------------------
        new(
            TYPE_Lab_FLT,
            ANYPLANAR | ANYEXTRA,
            PackLabFloatFromFloat),
        new(
            TYPE_XYZ_FLT,
            ANYPLANAR | ANYEXTRA,
            PackXYZFloatFromFloat),
        new(
            TYPE_LabV2_8,
            ANYPLANAR | ANYEXTRA,
            PackEncodedBytesLabV2FromFloat),
        new(
            TYPE_LabV2_16,
            ANYPLANAR | ANYEXTRA,
            PackEncodedWordsLabV2FromFloat),
        new(
            TYPE_Lab_DBL,
            ANYPLANAR | ANYEXTRA,
            PackLabDoubleFromFloat),
        new(
            TYPE_XYZ_DBL,
            ANYPLANAR | ANYEXTRA,
            PackXYZDoubleFromFloat),
        new(
            FLOAT_SH(1) | BYTES_SH(4),
            ANYPLANAR |
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            PackFloatsFromFloat),
        new(
            FLOAT_SH(1) | BYTES_SH(0),
            ANYPLANAR |
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            PackDoublesFromFloat),
        new(
            BYTES_SH(2),
            ANYPLANAR |
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            PackWordsFromFloat),
        new(
            BYTES_SH(1),
            ANYPLANAR |
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            PackBytesFromFloat),
        new(
            FLOAT_SH(1) | BYTES_SH(2),
            ANYFLAVOR | ANYSWAPFIRST | ANYSWAP | ANYEXTRA | ANYCHANNELS | ANYSPACE,
            PackHalfFromFloat),
    };

    internal struct Formatters16In(uint Type, uint Mask, Formatter16In Frm)
    {
        public uint Type = Type;
        public uint Mask = Mask;
        public Formatter16In Frm = Frm;
    }

    internal struct FormattersFloatIn(uint Type, uint Mask, FormatterFloatIn Frm)
    {
        public uint Type = Type;
        public uint Mask = Mask;
        public FormatterFloatIn Frm = Frm;
    }

    internal struct Formatters16Out(uint Type, uint Mask, Formatter16Out Frm)
    {
        public uint Type = Type;
        public uint Mask = Mask;
        public Formatter16Out Frm = Frm;
    }

    internal struct FormattersFloatOut(uint Type, uint Mask, FormatterFloatOut Frm)
    {
        public uint Type = Type;
        public uint Mask = Mask;
        public FormatterFloatOut Frm = Frm;
    }

    private static uint ANYSPACE =>
        COLORSPACE_SH(31);

    private static uint ANYCHANNELS =>
        CHANNELS_SH(15u);

    private static uint ANYEXTRA =>
        EXTRA_SH(7u);

    private static uint ANYPLANAR =>
        PLANAR_SH(1);

    private static uint ANYENDIAN =>
        ENDIAN16_SH(1);

    private static uint ANYSWAP =>
        DOSWAP_SH(1);

    private static uint ANYSWAPFIRST =>
        SWAPFIRST_SH(1);

    private static uint ANYFLAVOR =>
        FLAVOR_SH(1);

    private static uint ANYPREMUL =>
        PREMUL_SH(1);

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort CHANGE_ENDIAN(ushort w) =>
        (ushort)((w << 8) | (w >> 8));

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte REVERSE_FLAVOR_8(byte x) =>
        (byte)(0xFF - x);

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort REVERSE_FLAVOR_16(ushort x) =>
        (ushort)(0xFFFF - x);

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort FomLabV2ToLabV4(ushort x)
    {
        var a = ((x << 8) | x) >> 8;
        return
            a > 0xFFFF
                ? (ushort)0xFFFF
                : (ushort)a;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort FomLabV4ToLabV2(ushort x) =>
        (ushort)(((x << 8) + 0x80) / 257);

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int nChan, bool DoSwap, bool Reverse, bool SwapFirst, int Extra, bool Planar, bool Premul, bool
        SwapEndian) T_BREAK(uint m) =>
        (T_CHANNELS(m), T_DOSWAP(m) is not 0, T_FLAVOR(m) is not 0, T_SWAPFIRST(m) is not 0, T_EXTRA(m),
         T_PLANAR(m) is not 0, T_PREMUL(m) is not 0, T_ENDIAN16(m) is not 0);

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PackSwapFirst<T>(Span<T> swap1, int nChan) where T : unmanaged
    {
        var tmp = swap1[nChan - 1];

        memmove(swap1[1..], swap1, (uint)nChan - 1);
        swap1[0] = tmp;
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnrollSwapFirst<T>(Span<T> wIn, int nChan) where T : unmanaged
    {
        var tmp = wIn[0];

        memmove(wIn, wIn[1..], (uint)nChan - 1);
        wIn[nChan - 1] = tmp;
    }

    private static ReadOnlySpan<byte> UnrollChunkyBytes(Transform info,
                                                        Span<ushort> wIn,
                                                        ReadOnlySpan<byte> accum,
                                                        uint _)
    {
        var nChan = T_CHANNELS(info.InputFormat);
        var DoSwap = T_DOSWAP(info.InputFormat) is not 0;
        var Reverse = T_FLAVOR(info.InputFormat) is not 0;
        var SwapFirst = T_SWAPFIRST(info.InputFormat) is not 0;
        var Extra = T_EXTRA(info.InputFormat);
        var Premul = T_PREMUL(info.InputFormat) is not 0;

        var ExtraFirst = DoSwap ^ SwapFirst;
        var alpha_factor = 1u;

        var ptr = 0;

        if (ExtraFirst)
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(accum[0]));

            ptr += (int)Extra;
        }
        else
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(accum[(int)nChan]));
        }

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? (int)nChan - i - 1 : i;

            var v = (uint)FROM_8_TO_16(accum[ptr]);
            v = Reverse ? REVERSE_FLAVOR_16((ushort)v) : v;

            if (Premul && alpha_factor > 0)
            {
                v = (v << 16) / alpha_factor;
                if (v > 0xFFFF)
                    v = 0xFFFF;
            }

            wIn[index] = (ushort)v;
            ptr++;
        }

        if (!ExtraFirst)
            ptr += (int)Extra;

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollPlanarBytes(Transform info,
                                                        Span<ushort> wIn,
                                                        ReadOnlySpan<byte> accum,
                                                        uint Stride)
    {
        var nChan = T_CHANNELS(info.InputFormat);
        var DoSwap = T_DOSWAP(info.InputFormat) is not 0;
        var SwapFirst = T_SWAPFIRST(info.InputFormat) is not 0;
        var Reverse = T_FLAVOR(info.InputFormat) is not 0;
        var ExtraFirst = DoSwap ^ SwapFirst;
        var Extra = T_EXTRA(info.InputFormat);
        var Premul = T_PREMUL(info.InputFormat) is not 0;
        var Init = accum;
        var alpha_factor = 1u;

        var ptr = 0;

        if (ExtraFirst)
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(accum[0]));

            ptr += (int)(Extra * Stride);
        }
        else
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(accum[(int)(nChan * Stride)]));
        }

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? (int)nChan - i - 1 : i;
            var v = (uint)FROM_8_TO_16(accum[ptr]);

            v = Reverse ? REVERSE_FLAVOR_16((ushort)v) : v;

            if (Premul && alpha_factor > 0)
            {
                v = (v << 16) / alpha_factor;
                if (v > 0xFFFF)
                    v = 0xFFFF;
            }

            wIn[index] = (ushort)v;
            ptr += (int)Stride;
        }

        return Init[1..];
    }

    private static ReadOnlySpan<byte> Unroll4Bytes(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // C
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // M
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // Y
        wIn[3] = FROM_8_TO_16(accum[ptr++]); // K

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll4BytesReverse(Transform _1,
                                                          Span<ushort> wIn,
                                                          ReadOnlySpan<byte> accum,
                                                          uint _2)
    {
        var ptr = 0;
        wIn[0] = FROM_8_TO_16(REVERSE_FLAVOR_8(accum[ptr++])); // C
        wIn[1] = FROM_8_TO_16(REVERSE_FLAVOR_8(accum[ptr++])); // M
        wIn[2] = FROM_8_TO_16(REVERSE_FLAVOR_8(accum[ptr++])); // Y
        wIn[3] = FROM_8_TO_16(REVERSE_FLAVOR_8(accum[ptr++])); // K

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll4BytesSwapFirst(Transform _1,
                                                            Span<ushort> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint _2)
    {
        var ptr = 0;
        wIn[3] = FROM_8_TO_16(accum[ptr++]); // K
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // C
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // M
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // Y

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll4BytesSwap(Transform _1,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint _2)
    {
        var ptr = 0;
        wIn[3] = FROM_8_TO_16(accum[ptr++]); // K
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // Y
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // M
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // C

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll4BytesSwapSwapFirst(Transform _1,
                                                                Span<ushort> wIn,
                                                                ReadOnlySpan<byte> accum,
                                                                uint _2)
    {
        var ptr = 0;
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // Y
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // M
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // C
        wIn[3] = FROM_8_TO_16(accum[ptr++]); // K

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll3Bytes(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // R
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // G
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // B

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll3BytesSkip1Swap(Transform _1,
                                                            Span<ushort> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint _2)
    {
        var ptr = 0;
        ptr++;                               // A
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // B
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // G
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // R

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll3BytesSkip1SwapSwapFirst(Transform _1,
                                                                     Span<ushort> wIn,
                                                                     ReadOnlySpan<byte> accum,
                                                                     uint _2)
    {
        var ptr = 0;
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // B
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // G
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // R
        ptr++;                               // A

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll3BytesSkip1SwapFirst(Transform _1,
                                                                 Span<ushort> wIn,
                                                                 ReadOnlySpan<byte> accum,
                                                                 uint _2)
    {
        var ptr = 0;
        ptr++;                               // A
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // R
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // G
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // B

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll3BytesSwap(Transform _1,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint _2)
    {
        var ptr = 0;
        wIn[2] = FROM_8_TO_16(accum[ptr++]); // B
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // G
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // R

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollLabV2_8(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        wIn[0] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // L
        wIn[1] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // a
        wIn[2] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // b

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollALabV2_8(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        ptr++;                                                // A
        wIn[0] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // R
        wIn[1] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // G
        wIn[2] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // B

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollLabV2_16(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        wIn[0] = FomLabV2ToLabV4(BitConverter.ToUInt16(accum[ptr..]));
        ptr += 2; // L
        wIn[1] = FomLabV2ToLabV4(BitConverter.ToUInt16(accum[ptr..]));
        ptr += 2; // a
        wIn[2] = FomLabV2ToLabV4(BitConverter.ToUInt16(accum[ptr..]));
        ptr += 2; // b

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll2Bytes(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        wIn[0] = FROM_8_TO_16(accum[ptr++]); // ch1
        wIn[1] = FROM_8_TO_16(accum[ptr++]); // ch2

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll1Byte(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        wIn[0] = wIn[1] = wIn[2] = FROM_8_TO_16(accum[ptr++]); // L

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll1ByteSkip1(Transform _1,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint _2)
    {
        var ptr = 0;
        wIn[0] = wIn[1] = wIn[2] = FROM_8_TO_16(accum[ptr++]); // L
        ptr++;

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll1ByteSkip2(Transform _1,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint _2)
    {
        var ptr = 0;
        wIn[0] = wIn[1] = wIn[2] = FROM_8_TO_16(accum[ptr]); // L
        ptr += 2;

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> Unroll1ByteReversed(Transform _1,
                                                          Span<ushort> wIn,
                                                          ReadOnlySpan<byte> accum,
                                                          uint _2)
    {
        var ptr = 0;
        wIn[0] = wIn[1] = wIn[2] = REVERSE_FLAVOR_16(FROM_8_TO_16(accum[ptr++])); // L

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollAnyWords(Transform info,
                                                     Span<ushort> wIn,
                                                     ReadOnlySpan<byte> accum,
                                                     uint _1)
    {
        var nChan = T_CHANNELS(info.InputFormat);
        var SwapEndian = T_ENDIAN16(info.InputFormat) is not 0;
        var DoSwap = T_DOSWAP(info.InputFormat) is not 0;
        var Reverse = T_FLAVOR(info.InputFormat) is not 0;
        var SwapFirst = T_SWAPFIRST(info.InputFormat) is not 0;
        var Extra = T_EXTRA(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;

        var ptr = 0;

        if (ExtraFirst)
            ptr += Extra * sizeof(ushort);

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;
            var v = (uint)BitConverter.ToUInt16(accum[ptr..]);

            if (SwapEndian)
                v = CHANGE_ENDIAN((ushort)v);

            wIn[index] = Reverse ? REVERSE_FLAVOR_16((ushort)v) : (ushort)v;

            ptr += sizeof(ushort);
        }

        if (!ExtraFirst)
            ptr += Extra * sizeof(ushort);

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollAnyWordsPremul(Transform info,
                                                           Span<ushort> wIn,
                                                           ReadOnlySpan<byte> accum,
                                                           uint _)
    {
        var nChan = T_CHANNELS(info.InputFormat);
        var SwapEndian = T_ENDIAN16(info.InputFormat) is not 0;
        var DoSwap = T_DOSWAP(info.InputFormat) is not 0;
        var Reverse = T_FLAVOR(info.InputFormat) is not 0;
        var SwapFirst = T_SWAPFIRST(info.InputFormat) is not 0;
        var ExtraFirst = DoSwap ^ SwapFirst;

        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        var alpha = ExtraFirst ? acc[0] : acc[nChan];
        var alpha_factor = (uint)ToFixedDomain(alpha);

        var ptr = 0;

        if (ExtraFirst)
            ptr += sizeof(ushort);

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;
            var v = (uint)BitConverter.ToUInt16(accum[ptr..]);

            if (SwapEndian)
                v = CHANGE_ENDIAN((ushort)v);

            if (alpha_factor > 0)
            {
                v = (v << 16) / alpha_factor;
                if (v > 0xFFFF)
                    v = 0xFFFF;
            }

            wIn[index] = Reverse ? REVERSE_FLAVOR_16((ushort)v) : (ushort)v;

            ptr += sizeof(ushort);
        }

        if (!ExtraFirst)
            ptr += sizeof(ushort);

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollPlanarWords(Transform info,
                                                        Span<ushort> wIn,
                                                        ReadOnlySpan<byte> accum,
                                                        uint Stride)
    {
        var nChan = T_CHANNELS(info.InputFormat);
        var SwapEndian = T_ENDIAN16(info.InputFormat) is not 0;
        var DoSwap = T_DOSWAP(info.InputFormat) is not 0;
        var Reverse = T_FLAVOR(info.InputFormat) is not 0;
        var Extra = T_EXTRA(info.InputFormat);
        var Init = accum;

        var ptr = 0;

        if (DoSwap)
            ptr += Extra * (int)Stride;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;
            var v = BitConverter.ToUInt16(accum[ptr..]);

            if (SwapEndian)
                v = CHANGE_ENDIAN(v);

            wIn[index] = Reverse ? REVERSE_FLAVOR_16(v) : v;

            ptr += (int)Stride;
        }

        return Init[sizeof(ushort)..];
    }

    private static ReadOnlySpan<byte> UnrollPlanarWordsPremul(Transform info,
                                                              Span<ushort> wIn,
                                                              ReadOnlySpan<byte> accum,
                                                              uint Stride)
    {
        var nChan = T_CHANNELS(info.InputFormat);
        var SwapEndian = T_ENDIAN16(info.InputFormat) is not 0;
        var DoSwap = T_DOSWAP(info.InputFormat) is not 0;
        var Reverse = T_FLAVOR(info.InputFormat) is not 0;
        var SwapFirst = T_SWAPFIRST(info.InputFormat) is not 0;
        var ExtraFirst = DoSwap ^ SwapFirst;
        var Init = accum;

        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        var alpha = ExtraFirst ? acc[0] : acc[(nChan * (int)Stride) / 2];
        var alpha_factor = (uint)ToFixedDomain(alpha);

        var ptr = 0;

        if (ExtraFirst)
            ptr += (int)Stride;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;
            var v = (uint)BitConverter.ToUInt16(accum[ptr..]);

            if (SwapEndian)
                v = CHANGE_ENDIAN((ushort)v);

            if (alpha_factor > 0)
            {
                v = (v << 16) / alpha_factor;
                if (v > 0xFFFF)
                    v = 0xFFFF;
            }

            wIn[index] = Reverse ? REVERSE_FLAVOR_16((ushort)v) : (ushort)v;

            ptr += (int)Stride;
        }

        return Init[sizeof(ushort)..];
    }

    private static ReadOnlySpan<byte> Unroll4Words(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[0] = acc[ptr++]; // C
        wIn[1] = acc[ptr++]; // M
        wIn[2] = acc[ptr++]; // Y
        wIn[3] = acc[ptr++]; // K

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll4WordsReverse(Transform _1,
                                                          Span<ushort> wIn,
                                                          ReadOnlySpan<byte> accum,
                                                          uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[0] = REVERSE_FLAVOR_16(acc[ptr++]); // C
        wIn[1] = REVERSE_FLAVOR_16(acc[ptr++]); // M
        wIn[2] = REVERSE_FLAVOR_16(acc[ptr++]); // Y
        wIn[3] = REVERSE_FLAVOR_16(acc[ptr++]); // K

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll4WordsSwapFirst(Transform _1,
                                                            Span<ushort> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[3] = acc[ptr++]; // K
        wIn[0] = acc[ptr++]; // C
        wIn[1] = acc[ptr++]; // M
        wIn[2] = acc[ptr++]; // Y

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll4WordsSwap(Transform _1,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[3] = acc[ptr++]; // K
        wIn[2] = acc[ptr++]; // Y
        wIn[1] = acc[ptr++]; // M
        wIn[0] = acc[ptr++]; // C

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll4WordsSwapSwapFirst(Transform _1,
                                                                Span<ushort> wIn,
                                                                ReadOnlySpan<byte> accum,
                                                                uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[2] = acc[ptr++]; // Y
        wIn[1] = acc[ptr++]; // M
        wIn[0] = acc[ptr++]; // C
        wIn[3] = acc[ptr++]; // K

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll3Words(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[0] = acc[ptr++]; // C R
        wIn[1] = acc[ptr++]; // M G
        wIn[2] = acc[ptr++]; // Y B

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll3WordsSwap(Transform _1,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[2] = acc[ptr++]; // Y B
        wIn[1] = acc[ptr++]; // M G
        wIn[0] = acc[ptr++]; // C R

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll3WordsSkip1Swap(Transform _1,
                                                            Span<ushort> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        ptr++;               // A
        wIn[2] = acc[ptr++]; // B
        wIn[1] = acc[ptr++]; // G
        wIn[0] = acc[ptr++]; // R

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll3WordsSkip1SwapFirst(Transform _1,
                                                                 Span<ushort> wIn,
                                                                 ReadOnlySpan<byte> accum,
                                                                 uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        ptr++;               // A
        wIn[0] = acc[ptr++]; // R
        wIn[1] = acc[ptr++]; // G
        wIn[2] = acc[ptr++]; // B

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll1Word(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[0] = wIn[1] = wIn[2] = acc[ptr++]; // L

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll1WordReversed(Transform _1,
                                                          Span<ushort> wIn,
                                                          ReadOnlySpan<byte> accum,
                                                          uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[0] = wIn[1] = wIn[2] = REVERSE_FLAVOR_16(acc[ptr++]); // L

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll1WordSkip3(Transform _1,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[0] = wIn[1] = wIn[2] = acc[ptr++];

        ptr += 3;

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> Unroll2Words(Transform _1, Span<ushort> wIn, ReadOnlySpan<byte> accum, uint _2)
    {
        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        wIn[0] = acc[ptr++]; // ch1
        wIn[1] = acc[ptr++]; // ch2

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static ReadOnlySpan<byte> UnrollLabDoubleTo16(Transform info,
                                                          Span<ushort> wIn,
                                                          ReadOnlySpan<byte> accum,
                                                          uint Stride)
    {
        if (T_PLANAR(info.InputFormat) is not 0)
        {
            CIELab Lab;

            var pos_L = accum;
            var pos_a = accum[(int)Stride..];
            var pos_b = accum[(int)(Stride * 2)..];

            Lab.L = BitConverter.ToDouble(pos_L);
            Lab.a = BitConverter.ToDouble(pos_a);
            Lab.b = BitConverter.ToDouble(pos_b);

            Lab.FloatToEncoded(wIn);
            return accum[sizeof(double)..];
        }
        else
        {
            MemoryMarshal.Read<CIELab>(accum).FloatToEncoded(wIn);
            var ptr = (sizeof(double) * 3) + (T_EXTRA(info.InputFormat) * sizeof(double));
            return accum[ptr..];
        }
    }

    private static ReadOnlySpan<byte> UnrollLabFloatTo16(Transform info,
                                                         Span<ushort> wIn,
                                                         ReadOnlySpan<byte> accum,
                                                         uint Stride)
    {
        CIELab Lab;

        if (T_PLANAR(info.InputFormat) is not 0)
        {
            var pos_L = accum;
            var pos_a = accum[(int)Stride..];
            var pos_b = accum[(int)(Stride * 2)..];

            Lab.L = BitConverter.ToSingle(pos_L);
            Lab.a = BitConverter.ToSingle(pos_a);
            Lab.b = BitConverter.ToSingle(pos_b);

            Lab.FloatToEncoded(wIn);
            return accum[sizeof(float)..];
        }
        else
        {
            var acc = MemoryMarshal.Cast<byte, float>(accum);
            Lab.L = acc[0];
            Lab.a = acc[1];
            Lab.b = acc[2];

            Lab.FloatToEncoded(wIn);
            var ptr = (3 + T_EXTRA(info.InputFormat)) * sizeof(float);
            return accum[ptr..];
        }
    }

    private static ReadOnlySpan<byte> UnrollXYZDoubleTo16(Transform info,
                                                          Span<ushort> wIn,
                                                          ReadOnlySpan<byte> accum,
                                                          uint Stride)
    {
        if (T_PLANAR(info.InputFormat) is not 0)
        {
            CIEXYZ XYZ;

            var pos_X = accum;
            var pos_Y = accum[(int)Stride..];
            var pos_Z = accum[(int)(Stride * 2)..];

            XYZ.X = BitConverter.ToDouble(pos_X);
            XYZ.Y = BitConverter.ToDouble(pos_Y);
            XYZ.Z = BitConverter.ToDouble(pos_Z);

            XYZ.FloatToEncoded(wIn);
            return accum[sizeof(double)..];
        }
        else
        {
            MemoryMarshal.Read<CIEXYZ>(accum).FloatToEncoded(wIn);
            var ptr = (sizeof(double) * 3) + (T_EXTRA(info.InputFormat) * sizeof(double));
            return accum[ptr..];
        }
    }

    private static ReadOnlySpan<byte> UnrollXYZFloatTo16(Transform info,
                                                         Span<ushort> wIn,
                                                         ReadOnlySpan<byte> accum,
                                                         uint Stride)
    {
        CIEXYZ XYZ;

        if (T_PLANAR(info.InputFormat) is not 0)
        {
            var pos_X = accum;
            var pos_Y = accum[(int)Stride..];
            var pos_Z = accum[(int)(Stride * 2)..];

            XYZ.X = BitConverter.ToSingle(pos_X);
            XYZ.Y = BitConverter.ToSingle(pos_Y);
            XYZ.Z = BitConverter.ToSingle(pos_Z);

            XYZ.FloatToEncoded(wIn);
            return accum[sizeof(float)..];
        }
        else
        {
            var pt = MemoryMarshal.Cast<byte, float>(accum);

            XYZ.X = pt[0];
            XYZ.Y = pt[1];
            XYZ.Z = pt[2];
            XYZ.FloatToEncoded(wIn);
            var ptr = (3 + T_EXTRA(info.InputFormat)) * sizeof(float);
            return accum[ptr..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInkSpace(uint Type) =>
        T_COLORSPACE(Type) switch
        {
            PT_CMY
             or PT_CMYK
             or PT_MCH5
             or PT_MCH6
             or PT_MCH7
             or PT_MCH8
             or PT_MCH9
             or PT_MCH10
             or PT_MCH11
             or PT_MCH12
             or PT_MCH13
             or PT_MCH14
             or PT_MCH15 => true,
            _ => false,
        };

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint PixelSize(uint Format)
    {
        var fmt_bytes = (uint)T_BYTES(Format);

        // For double, the T_BYTES field is zero
        if (fmt_bytes is 0)
            return sizeof(double);

        // Otherwise, it is already correct for all formats
        return fmt_bytes;
    }

    private static ReadOnlySpan<byte> UnrollDoubleTo16(Transform info,
                                                       Span<ushort> wIn,
                                                       ReadOnlySpan<byte> accum,
                                                       uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;
        var maximum = IsInkSpace(info.InputFormat) ? 655.35 : 65535.0;

        Stride /= PixelSize(info.InputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var acc = MemoryMarshal.Cast<byte, double>(accum);

            var v = (float)(Planar
                                ? acc[(i + start) * (int)Stride]
                                : acc[i + start]);

            var vi = QuickSaturateWord(v * maximum);

            if (Reverse)
                vi = REVERSE_FLAVOR_16(vi);

            wIn[index] = vi;
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(double))..];
    }

    private static ReadOnlySpan<byte> UnrollFloatTo16(Transform info,
                                                      Span<ushort> wIn,
                                                      ReadOnlySpan<byte> accum,
                                                      uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;
        var maximum = IsInkSpace(info.InputFormat) ? 655.35 : 65535.0;

        Stride /= PixelSize(info.InputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var acc = MemoryMarshal.Cast<byte, float>(accum);

            var v = Planar
                        ? acc[(i + start) * (int)Stride]
                        : acc[i + start];

            var vi = QuickSaturateWord(v * maximum);

            if (Reverse)
                vi = REVERSE_FLAVOR_16(vi);

            wIn[index] = vi;
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(float))..];
    }

    private static ReadOnlySpan<byte> UnrollDouble1Chan(Transform _1,
                                                        Span<ushort> wIn,
                                                        ReadOnlySpan<byte> accum,
                                                        uint _2)
    {
        var Inks = MemoryMarshal.Cast<byte, double>(accum);

        wIn[0] = wIn[1] = wIn[2] = QuickSaturateWord(Inks[0] * 65535.0);

        return accum[sizeof(double)..];
    }

    private static ReadOnlySpan<byte> Unroll8ToFloat(Transform info,
                                                     Span<float> wIn,
                                                     ReadOnlySpan<byte> accum,
                                                     uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;

        Stride /= PixelSize(info.InputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = (float)(Planar
                                ? accum[(i + start) * (int)Stride]
                                : accum[i + start]);

            v /= 255.0F;

            wIn[index] = Reverse ? 1 - v : v;
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(byte))..];
    }

    private static ReadOnlySpan<byte> Unroll16ToFloat(Transform info,
                                                      Span<float> wIn,
                                                      ReadOnlySpan<byte> accum,
                                                      uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;

        Stride /= PixelSize(info.InputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var acc = MemoryMarshal.Cast<byte, ushort>(accum);

            var v = (float)(Planar
                                ? acc[(i + start) * (int)Stride]
                                : acc[i + start]);

            v /= 65535.0F;

            wIn[index] = Reverse ? 1 - v : v;
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(ushort))..];
    }

    private static ReadOnlySpan<byte> UnrollFloatsToFloat(Transform info,
                                                          Span<float> wIn,
                                                          ReadOnlySpan<byte> accum,
                                                          uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, Premul, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;
        var maximum = IsInkSpace(info.InputFormat) ? 100.0F : 1.0F;
        var alpha_factor = 1.0F;
        var acc = MemoryMarshal.Cast<byte, float>(accum);

        Stride /= PixelSize(info.InputFormat);

        if (Premul && Extra is not 0)
            alpha_factor = (ExtraFirst ? acc[0] : acc[nChan * (Planar ? (int)Stride : 1)]) / maximum;

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = acc[(i + start) * (Planar ? (int)Stride : 1)];

            if (Premul && alpha_factor > 0)
                v /= alpha_factor;

            v /= maximum;

            wIn[index] = Reverse ? 1 - v : v;
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(float))..];
    }

    private static ReadOnlySpan<byte> UnrollDoublesToFloat(Transform info,
                                                           Span<float> wIn,
                                                           ReadOnlySpan<byte> accum,
                                                           uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, Premul, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;
        var maximum = IsInkSpace(info.InputFormat) ? 100.0 : 1.0;
        var alpha_factor = 1.0;
        var acc = MemoryMarshal.Cast<byte, double>(accum);

        Stride /= PixelSize(info.InputFormat);

        if (Premul && Extra is not 0)
            alpha_factor = (ExtraFirst ? acc[0] : acc[nChan * (Planar ? (int)Stride : 1)]) / maximum;

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = acc[(i + start) * (Planar ? (int)Stride : 1)];

            if (Premul && alpha_factor > 0)
                v /= alpha_factor;

            v /= maximum;

            wIn[index] = (float)(Reverse ? 1 - v : v);
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(double))..];
    }

    private static ReadOnlySpan<byte> UnrollLabDoubleToFloat(Transform info,
                                                             Span<float> wIn,
                                                             ReadOnlySpan<byte> accum,
                                                             uint Stride)
    {
        var acc = MemoryMarshal.Cast<byte, double>(accum);

        if (T_PLANAR(info.InputFormat) is not 0)
        {
            Stride /= PixelSize(info.InputFormat);

            wIn[0] = (float)(acc[0] / 100.0);
            wIn[1] = (float)((acc[(int)Stride] + 128) / 255.0);
            wIn[2] = (float)((acc[(int)Stride * 2] + 128) / 255.0);

            return accum[sizeof(double)..];
        }
        else
        {
            wIn[0] = (float)(acc[0] / 100.0);
            wIn[1] = (float)((acc[1] + 128) / 255.0);
            wIn[2] = (float)((acc[2] + 128) / 255.0);

            return accum[(sizeof(double) * (3 + T_EXTRA(info.InputFormat)))..];
        }
    }

    private static ReadOnlySpan<byte> UnrollLabFloatToFloat(Transform info,
                                                            Span<float> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint Stride)
    {
        var acc = MemoryMarshal.Cast<byte, float>(accum);

        if (T_PLANAR(info.InputFormat) is not 0)
        {
            Stride /= PixelSize(info.InputFormat);

            wIn[0] = (float)(acc[0] / 100.0);
            wIn[1] = (float)((acc[(int)Stride] + 128) / 255.0);
            wIn[2] = (float)((acc[(int)Stride * 2] + 128) / 255.0);

            return accum[sizeof(float)..];
        }
        else
        {
            wIn[0] = (float)(acc[0] / 100.0);
            wIn[1] = (float)((acc[1] + 128) / 255.0);
            wIn[2] = (float)((acc[2] + 128) / 255.0);

            return accum[(sizeof(float) * (3 + T_EXTRA(info.InputFormat)))..];
        }
    }

    private static ReadOnlySpan<byte> UnrollXYZDoubleToFloat(Transform info,
                                                             Span<float> wIn,
                                                             ReadOnlySpan<byte> accum,
                                                             uint Stride)
    {
        var acc = MemoryMarshal.Cast<byte, double>(accum);

        if (T_PLANAR(info.InputFormat) is not 0)
        {
            Stride /= PixelSize(info.InputFormat);

            wIn[0] = (float)(acc[0] / CIEXYZ.MaxEncodeableXYZ);
            wIn[1] = (float)(acc[(int)Stride] / CIEXYZ.MaxEncodeableXYZ);
            wIn[2] = (float)(acc[(int)Stride * 2] / CIEXYZ.MaxEncodeableXYZ);

            return accum[sizeof(double)..];
        }
        else
        {
            wIn[0] = (float)(acc[0] / CIEXYZ.MaxEncodeableXYZ);
            wIn[1] = (float)(acc[1] / CIEXYZ.MaxEncodeableXYZ);
            wIn[2] = (float)(acc[2] / CIEXYZ.MaxEncodeableXYZ);

            return accum[(sizeof(double) * (3 + T_EXTRA(info.InputFormat)))..];
        }
    }

    private static ReadOnlySpan<byte> UnrollXYZFloatToFloat(Transform info,
                                                            Span<float> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint Stride)
    {
        var acc = MemoryMarshal.Cast<byte, float>(accum);

        if (T_PLANAR(info.InputFormat) is not 0)
        {
            Stride /= PixelSize(info.InputFormat);

            wIn[0] = (float)(acc[0] / CIEXYZ.MaxEncodeableXYZ);
            wIn[1] = (float)(acc[(int)Stride] / CIEXYZ.MaxEncodeableXYZ);
            wIn[2] = (float)(acc[(int)Stride * 2] / CIEXYZ.MaxEncodeableXYZ);

            return accum[sizeof(float)..];
        }
        else
        {
            wIn[0] = (float)(acc[0] / CIEXYZ.MaxEncodeableXYZ);
            wIn[1] = (float)(acc[1] / CIEXYZ.MaxEncodeableXYZ);
            wIn[2] = (float)(acc[2] / CIEXYZ.MaxEncodeableXYZ);

            return accum[(sizeof(float) * (3 + T_EXTRA(info.InputFormat)))..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void lab4toFloat(Span<float> wIn, ReadOnlySpan<ushort> lab4)
    {
        var L = lab4[0] / 655.35F;
        var a = (lab4[1] / 257.0F) - 128.0F;
        var b = (lab4[2] / 257.0F) - 128.0F;

        wIn[0] = L / 100.0F;
        wIn[1] = (a + 128.0F) / 255.0F;
        wIn[2] = (b + 128.0F) / 255.0F;
    }

    private static ReadOnlySpan<byte> UnrollLabV2_8ToFloat(Transform _1,
                                                           Span<float> wIn,
                                                           ReadOnlySpan<byte> accum,
                                                           uint _2)
    {
        var ptr = 0;
        Span<ushort> lab4 = stackalloc ushort[3];

        lab4[0] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // L
        lab4[1] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // a
        lab4[2] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // b

        lab4toFloat(wIn, lab4);

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollALabV2_8ToFloat(Transform _1,
                                                            Span<float> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint _2)
    {
        Span<ushort> lab4 = stackalloc ushort[3];

        var ptr = 0;
        ptr++;                                                 // A
        lab4[0] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // L
        lab4[1] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // a
        lab4[2] = FomLabV2ToLabV4(FROM_8_TO_16(accum[ptr++])); // b

        lab4toFloat(wIn, lab4);

        return accum[ptr..];
    }

    private static ReadOnlySpan<byte> UnrollLabV2_16ToFloat(Transform _1,
                                                            Span<float> wIn,
                                                            ReadOnlySpan<byte> accum,
                                                            uint _2)
    {
        Span<ushort> lab4 = stackalloc ushort[3];

        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(accum);
        lab4[0] = FomLabV2ToLabV4(FROM_8_TO_16(acc[ptr++])); // L
        lab4[1] = FomLabV2ToLabV4(FROM_8_TO_16(acc[ptr++])); // a
        lab4[2] = FomLabV2ToLabV4(FROM_8_TO_16(acc[ptr++])); // b

        lab4toFloat(wIn, lab4);

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static Span<byte> PackChunkyBytes(Transform info, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, _, Premul, _) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var alpha_factor = 0u;

        var ptr = 0;

        if (ExtraFirst)
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(output[0]));

            ptr += Extra;
        }
        else
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(output[nChan]));
        }

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index];

            if (Reverse)
                v = REVERSE_FLAVOR_16(v);

            if (Premul && alpha_factor is not 0)
                v = (ushort)(((v * alpha_factor) + 0x8000) >> 16);

            output[ptr++] = FROM_16_TO_8(v);
        }

        if (!ExtraFirst)
            ptr += Extra;

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(output, nChan);

        return output[ptr..];
    }

    private static Span<byte> PackChunkyWords(Transform info, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, _, Premul, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var alpha_factor = 0u;

        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(output);

        if (ExtraFirst)
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(acc[ptr]);

            ptr += Extra;
        }
        else
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(acc[nChan]);
        }

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index];

            if (SwapEndian)
                v = CHANGE_ENDIAN(v);

            if (Reverse)
                v = REVERSE_FLAVOR_16(v);

            if (Premul && alpha_factor is not 0)
                v = (ushort)(((v * alpha_factor) + 0x8000) >> 16);

            acc[ptr++] = v;
        }

        if (!ExtraFirst)
            ptr += Extra;

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(acc, nChan);

        return MemoryMarshal.Cast<ushort, byte>(acc[ptr..]);
    }

    private static Span<byte> PackPlanarBytes(Transform info, ReadOnlySpan<ushort> wOut, Span<byte> output, uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, _, Premul, _) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var alpha_factor = 0u;

        var ptr = 0;

        if (ExtraFirst)
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(output[0]));

            ptr += Extra * (int)Stride;
        }
        else
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(FROM_8_TO_16(output[nChan * (int)Stride]));
        }

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index];

            if (Reverse)
                v = REVERSE_FLAVOR_16(v);

            if (Premul && alpha_factor is not 0)
                v = (ushort)(((v * alpha_factor) + 0x8000) >> 16);

            output[ptr] = FROM_16_TO_8(v);

            ptr += (int)Stride;
        }

        return output[1..];
    }

    private static Span<byte> PackPlanarWords(Transform info, ReadOnlySpan<ushort> wOut, Span<byte> output, uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, _, Premul, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var alpha_factor = 0u;

        Stride /= PixelSize(info.OutputFormat);

        var ptr = 0;
        var acc = MemoryMarshal.Cast<byte, ushort>(output);

        if (ExtraFirst)
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(acc[0]);

            ptr += Extra * (int)Stride;
        }
        else
        {
            if (Premul && Extra is not 0)
                alpha_factor = (uint)ToFixedDomain(acc[nChan * (int)Stride]);
        }

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index];

            if (SwapEndian)
                v = CHANGE_ENDIAN(v);

            if (Reverse)
                v = REVERSE_FLAVOR_16(v);

            if (Premul && alpha_factor is not 0)
                v = (ushort)(((v * alpha_factor) + 0x8000) >> 16);

            acc[ptr] = v;

            ptr += (int)Stride;
        }

        return output[sizeof(ushort)..];
    }

    private static Span<byte> Pack6Bytes(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[3]);
        output[ptr++] = FROM_16_TO_8(wOut[4]);
        output[ptr++] = FROM_16_TO_8(wOut[5]);

        return output[ptr..];
    }

    private static Span<byte> Pack6BytesSwap(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[5]);
        output[ptr++] = FROM_16_TO_8(wOut[4]);
        output[ptr++] = FROM_16_TO_8(wOut[3]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[0]);

        return output[ptr..];
    }

    private static Span<byte> Pack6Words(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[0];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[2];
        o[ptr++] = wOut[3];
        o[ptr++] = wOut[4];
        o[ptr++] = wOut[5];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack6WordsSwap(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[5];
        o[ptr++] = wOut[4];
        o[ptr++] = wOut[3];
        o[ptr++] = wOut[2];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[0];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack4Bytes(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[3]);

        return output[ptr..];
    }

    private static Span<byte> Pack4BytesReverse(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = REVERSE_FLAVOR_8(FROM_16_TO_8(wOut[0]));
        output[ptr++] = REVERSE_FLAVOR_8(FROM_16_TO_8(wOut[1]));
        output[ptr++] = REVERSE_FLAVOR_8(FROM_16_TO_8(wOut[2]));
        output[ptr++] = REVERSE_FLAVOR_8(FROM_16_TO_8(wOut[3]));

        return output[ptr..];
    }

    private static Span<byte> Pack4BytesSwapFirst(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[3]);
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);

        return output[ptr..];
    }

    private static Span<byte> Pack4BytesSwap(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[3]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[0]);

        return output[ptr..];
    }

    private static Span<byte> Pack4BytesSwapSwapFirst(Transform _1,
                                                      ReadOnlySpan<ushort> wOut,
                                                      Span<byte> output,
                                                      uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        output[ptr++] = FROM_16_TO_8(wOut[3]);

        return output[ptr..];
    }

    private static Span<byte> Pack4Words(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[0];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[2];
        o[ptr++] = wOut[3];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack4WordsReverse(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = REVERSE_FLAVOR_16(wOut[0]);
        o[ptr++] = REVERSE_FLAVOR_16(wOut[1]);
        o[ptr++] = REVERSE_FLAVOR_16(wOut[2]);
        o[ptr++] = REVERSE_FLAVOR_16(wOut[3]);

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack4WordsSwap(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[3];
        o[ptr++] = wOut[2];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[0];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack4WordsBigEndian(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = CHANGE_ENDIAN(wOut[0]);
        o[ptr++] = CHANGE_ENDIAN(wOut[1]);
        o[ptr++] = CHANGE_ENDIAN(wOut[2]);
        o[ptr++] = CHANGE_ENDIAN(wOut[3]);

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> PackLabV2_8(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(FomLabV4ToLabV2(wOut[0]));
        output[ptr++] = FROM_16_TO_8(FomLabV4ToLabV2(wOut[1]));
        output[ptr++] = FROM_16_TO_8(FomLabV4ToLabV2(wOut[2]));

        return output[ptr..];
    }

    private static Span<byte> PackALabV2_8(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        ptr++;
        output[ptr++] = FROM_16_TO_8(FomLabV4ToLabV2(wOut[0]));
        output[ptr++] = FROM_16_TO_8(FomLabV4ToLabV2(wOut[1]));
        output[ptr++] = FROM_16_TO_8(FomLabV4ToLabV2(wOut[2]));

        return output[ptr..];
    }

    private static Span<byte> PackLabV2_16(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = FomLabV4ToLabV2(wOut[0]);
        o[ptr++] = FomLabV4ToLabV2(wOut[1]);
        o[ptr++] = FomLabV4ToLabV2(wOut[2]);

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack3Bytes(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesOptimized(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = (byte)(wOut[0] & 0xFFu);
        output[ptr++] = (byte)(wOut[1] & 0xFFu);
        output[ptr++] = (byte)(wOut[2] & 0xFFu);

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesSwap(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[0]);

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesSwapOptimized(Transform _1,
                                                      ReadOnlySpan<ushort> wOut,
                                                      Span<byte> output,
                                                      uint _2)
    {
        var ptr = 0;
        output[ptr++] = (byte)(wOut[2] & 0xFFu);
        output[ptr++] = (byte)(wOut[1] & 0xFFu);
        output[ptr++] = (byte)(wOut[0] & 0xFFu);

        return output[ptr..];
    }

    private static Span<byte> Pack3Words(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[0];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[2];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack3WordsSwap(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[2];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[0];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack3WordsBigEndian(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = CHANGE_ENDIAN(wOut[0]);
        o[ptr++] = CHANGE_ENDIAN(wOut[1]);
        o[ptr++] = CHANGE_ENDIAN(wOut[2]);

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack3BytesAndSkip1(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        ptr++;

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesAndSkip1Optimized(Transform _1,
                                                          ReadOnlySpan<ushort> wOut,
                                                          Span<byte> output,
                                                          uint _2)
    {
        var ptr = 0;
        output[ptr++] = (byte)(wOut[0] & 0xFFu);
        output[ptr++] = (byte)(wOut[1] & 0xFFu);
        output[ptr++] = (byte)(wOut[2] & 0xFFu);
        ptr++;

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesAndSkip1SwapFirst(Transform _1,
                                                          ReadOnlySpan<ushort> wOut,
                                                          Span<byte> output,
                                                          uint _2)
    {
        var ptr = 0;
        ptr++;
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[2]);

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesAndSkip1SwapFirstOptimized(Transform _1,
                                                                   ReadOnlySpan<ushort> wOut,
                                                                   Span<byte> output,
                                                                   uint _2)
    {
        var ptr = 0;
        ptr++;
        output[ptr++] = (byte)(wOut[0] & 0xFFu);
        output[ptr++] = (byte)(wOut[1] & 0xFFu);
        output[ptr++] = (byte)(wOut[2] & 0xFFu);

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesAndSkip1Swap(Transform _1,
                                                     ReadOnlySpan<ushort> wOut,
                                                     Span<byte> output,
                                                     uint _2)
    {
        var ptr = 0;
        ptr++;
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[0]);

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesAndSkip1SwapOptimized(Transform _1,
                                                              ReadOnlySpan<ushort> wOut,
                                                              Span<byte> output,
                                                              uint _2)
    {
        var ptr = 0;
        ptr++;
        output[ptr++] = (byte)(wOut[2] & 0xFFu);
        output[ptr++] = (byte)(wOut[1] & 0xFFu);
        output[ptr++] = (byte)(wOut[0] & 0xFFu);

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesAndSkip1SwapSwapFirst(Transform _1,
                                                              ReadOnlySpan<ushort> wOut,
                                                              Span<byte> output,
                                                              uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[2]);
        output[ptr++] = FROM_16_TO_8(wOut[1]);
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        ptr++;

        return output[ptr..];
    }

    private static Span<byte> Pack3BytesAndSkip1SwapSwapFirstOptimized(Transform _1,
                                                                       ReadOnlySpan<ushort> wOut,
                                                                       Span<byte> output,
                                                                       uint _2)
    {
        var ptr = 0;
        output[ptr++] = (byte)(wOut[2] & 0xFFu);
        output[ptr++] = (byte)(wOut[1] & 0xFFu);
        output[ptr++] = (byte)(wOut[0] & 0xFFu);
        ptr++;

        return output[ptr..];
    }

    private static Span<byte> Pack3WordsAndSkip1(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[0];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[2];
        ptr++;

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack3WordsAndSkip1Swap(Transform _1,
                                                     ReadOnlySpan<ushort> wOut,
                                                     Span<byte> output,
                                                     uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        ptr++;
        o[ptr++] = wOut[2];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[0];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack3WordsAndSkip1SwapFirst(Transform _1,
                                                          ReadOnlySpan<ushort> wOut,
                                                          Span<byte> output,
                                                          uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        ptr++;
        o[ptr++] = wOut[0];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[2];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack3WordsAndSkip1SwapSwapFirst(Transform _1,
                                                              ReadOnlySpan<ushort> wOut,
                                                              Span<byte> output,
                                                              uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[2];
        o[ptr++] = wOut[1];
        o[ptr++] = wOut[0];
        ptr++;

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack1Byte(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[0]);

        return output[ptr..];
    }

    private static Span<byte> Pack1ByteReversed(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(REVERSE_FLAVOR_16(wOut[0]));

        return output[ptr..];
    }

    private static Span<byte> Pack1ByteSkip1(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        output[ptr++] = FROM_16_TO_8(wOut[0]);
        ptr++;

        return output[ptr..];
    }

    private static Span<byte> Pack1ByteSkip1SwapFirst(Transform _1,
                                                      ReadOnlySpan<ushort> wOut,
                                                      Span<byte> output,
                                                      uint _2)
    {
        var ptr = 0;
        ptr++;
        output[ptr++] = FROM_16_TO_8(wOut[0]);

        return output[ptr..];
    }

    private static Span<byte> Pack1Word(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[0];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack1WordReversed(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = REVERSE_FLAVOR_16(wOut[0]);

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack1WordBigEndian(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = CHANGE_ENDIAN(wOut[0]);

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack1WordSkip1(Transform _1, ReadOnlySpan<ushort> wOut, Span<byte> output, uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        o[ptr++] = wOut[0];
        ptr++;

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> Pack1WordSkip1SwapFirst(Transform _1,
                                                      ReadOnlySpan<ushort> wOut,
                                                      Span<byte> output,
                                                      uint _2)
    {
        var ptr = 0;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        ptr++;
        o[ptr++] = wOut[0];

        return MemoryMarshal.Cast<ushort, byte>(o[ptr..]);
    }

    private static Span<byte> PackLabDoubleFrom16(Transform info,
                                                  ReadOnlySpan<ushort> wOut,
                                                  Span<byte> output,
                                                  uint Stride)
    {
        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            var Out = MemoryMarshal.Cast<byte, double>(output);
            var Lab = wOut.LabEncodedToFloat();

            Out[0] = Lab.L;
            Out[(int)Stride] = Lab.a;
            Out[(int)Stride * 2] = Lab.b;

            return output[sizeof(double)..];
        }
        else
        {
            var value = wOut.LabEncodedToFloat();
            MemoryMarshal.Write(output, ref value);
            return output[((sizeof(double) * 3) + (T_EXTRA(info.OutputFormat) * sizeof(double)))..];
        }
    }

    private static Span<byte> PackLabFloatFrom16(Transform info,
                                                 ReadOnlySpan<ushort> wOut,
                                                 Span<byte> output,
                                                 uint Stride)
    {
        var Lab = wOut.LabEncodedToFloat();

        var Out = MemoryMarshal.Cast<byte, float>(output);

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Out[0] = (float)Lab.L;
            Out[(int)Stride] = (float)Lab.a;
            Out[(int)Stride * 2] = (float)Lab.b;

            return output[sizeof(float)..];
        }
        else
        {
            Out[0] = (float)Lab.L;
            Out[1] = (float)Lab.a;
            Out[2] = (float)Lab.b;

            return output[((3 + T_EXTRA(info.OutputFormat)) * sizeof(float))..];
        }
    }

    private static Span<byte> PackXYZDoubleFrom16(Transform info,
                                                  ReadOnlySpan<ushort> wOut,
                                                  Span<byte> output,
                                                  uint Stride)
    {
        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            var Out = MemoryMarshal.Cast<byte, double>(output);
            var XYZ = wOut.XYZEncodedToFloat();

            Out[0] = XYZ.X;
            Out[(int)Stride] = XYZ.Y;
            Out[(int)Stride * 2] = XYZ.Z;

            return output[sizeof(double)..];
        }
        else
        {
            var value = wOut.XYZEncodedToFloat();
            MemoryMarshal.Write(output, in value);
            return output[((sizeof(double) * 3) + (T_EXTRA(info.OutputFormat) * sizeof(double)))..];
        }
    }

    private static Span<byte> PackXYZFloatFrom16(Transform info,
                                                 ReadOnlySpan<ushort> wOut,
                                                 Span<byte> output,
                                                 uint Stride)
    {
        var XYZ = wOut.XYZEncodedToFloat();

        var Out = MemoryMarshal.Cast<byte, float>(output);

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Out[0] = (float)XYZ.X;
            Out[(int)Stride] = (float)XYZ.Y;
            Out[(int)Stride * 2] = (float)XYZ.Z;

            return output[sizeof(float)..];
        }
        else
        {
            Out[0] = (float)XYZ.X;
            Out[1] = (float)XYZ.Y;
            Out[2] = (float)XYZ.Z;

            return output[((3 + T_EXTRA(info.OutputFormat)) * sizeof(float))..];
        }
    }

    private static Span<byte> PackDoubleFrom16(Transform info,
                                               ReadOnlySpan<ushort> wOut,
                                               Span<byte> output,
                                               uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var maximum = IsInkSpace(info.OutputFormat) ? 655.35 : 65535.0;
        var o = MemoryMarshal.Cast<byte, double>(output);
        var swap1 = o;
        var start = 0;

        Stride /= PixelSize(info.OutputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] / maximum;

            if (Reverse)
                v = maximum - v;
            o[(i + start) * (Planar ? (int)Stride : 1)] = v;
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(double) * (Planar ? 1 : nChan + Extra))..];
    }

    private static Span<byte> PackFloatFrom16(Transform info, ReadOnlySpan<ushort> wOut, Span<byte> output, uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var maximum = IsInkSpace(info.OutputFormat) ? 655.35 : 65535.0;
        var o = MemoryMarshal.Cast<byte, float>(output);
        var swap1 = o;
        var start = 0;

        Stride /= PixelSize(info.OutputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] / maximum;

            if (Reverse)
                v = maximum - v;

            o[(i + start) * (Planar ? (int)Stride : 1)] = (float)v;
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(float) * (Planar ? 1 : nChan + Extra))..];
    }

    private static Span<byte> PackBytesFromFloat(Transform info,
                                                 ReadOnlySpan<float> wOut,
                                                 Span<byte> output,
                                                 uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var swap1 = output;
        var start = 0;

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] * 65535.0;

            if (Reverse)
                v = 65535.0 - v;

            output[(i + start) * (Planar ? (int)Stride : 1)] = FROM_16_TO_8(QuickSaturateWord(v));
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(byte) * (Planar ? 1 : nChan + Extra))..];
    }

    private static Span<byte> PackWordsFromFloat(Transform info,
                                                 ReadOnlySpan<float> wOut,
                                                 Span<byte> output,
                                                 uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var o = MemoryMarshal.Cast<byte, ushort>(output);
        var swap1 = o;
        var start = 0;

        Stride /= PixelSize(info.OutputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] * 65535.0;

            if (Reverse)
                v = 65535.0 - v;

            o[(i + start) * (Planar ? (int)Stride : 1)] = QuickSaturateWord(v);
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(ushort) * (Planar ? 1 : nChan + Extra))..];
    }

    private static Span<byte> PackFloatsFromFloat(Transform info,
                                                  ReadOnlySpan<float> wOut,
                                                  Span<byte> output,
                                                  uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var maximum = IsInkSpace(info.OutputFormat) ? 100.0 : 1.0;
        var o = MemoryMarshal.Cast<byte, float>(output);
        var swap1 = o;
        var start = 0;

        Stride /= PixelSize(info.OutputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] * maximum;

            if (Reverse)
                v = maximum - v;

            o[(i + start) * (Planar ? (int)Stride : 1)] = (float)v;
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(float) * (Planar ? 1 : nChan + Extra))..];
    }

    private static Span<byte> PackDoublesFromFloat(Transform info,
                                                   ReadOnlySpan<float> wOut,
                                                   Span<byte> output,
                                                   uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, SwapEndian) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var maximum = IsInkSpace(info.OutputFormat) ? 100.0 : 1.0;
        var o = MemoryMarshal.Cast<byte, double>(output);
        var swap1 = o;
        var start = 0;

        Stride /= PixelSize(info.OutputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] * maximum;

            if (Reverse)
                v = maximum - v;

            o[(i + start) * (Planar ? (int)Stride : 1)] = v;
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(double) * (Planar ? 1 : nChan + Extra))..];
    }

    private static Span<byte> PackLabFloatFromFloat(Transform info,
                                                    ReadOnlySpan<float> wOut,
                                                    Span<byte> output,
                                                    uint Stride)
    {
        var Out = MemoryMarshal.Cast<byte, float>(output);

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Stride /= PixelSize(info.OutputFormat);

            Out[0] = (float)(wOut[0] * 100.0);
            Out[(int)Stride] = (float)((wOut[1] * 255.0) - 128.0);
            Out[(int)Stride * 2] = (float)((wOut[2] * 255.0) - 128.0);

            return output[sizeof(float)..];
        }
        else
        {
            Out[0] = (float)(wOut[0] * 100.0);
            Out[1] = (float)((wOut[1] * 255.0) - 128.0);
            Out[2] = (float)((wOut[2] * 255.0) - 128.0);

            return output[((3 + T_EXTRA(info.OutputFormat)) * sizeof(float))..];
        }
    }

    private static Span<byte> PackEncodedBytesLabV2FromFloat(Transform info,
                                                             ReadOnlySpan<float> wOut,
                                                             Span<byte> output,
                                                             uint Stride)
    {
        CIELab Lab;
        Span<ushort> wlab = stackalloc ushort[3];

        Lab.L = wOut[0] * 100.0;
        Lab.a = (wOut[1] * 255.0) - 128.0;
        Lab.b = (wOut[2] * 255.0) - 128.0;

        Lab = wlab.LabEncodedToFloat();

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Stride /= PixelSize(info.OutputFormat);

            output[0] = (byte)(wlab[0] >> 8);
            output[(int)Stride] = (byte)(wlab[1] >> 8);
            output[(int)Stride * 2] = (byte)(wlab[2] >> 8);

            return output[1..];
        }
        else
        {
            output[0] = (byte)(wlab[0] >> 8);
            output[1] = (byte)(wlab[1] >> 8);
            output[2] = (byte)(wlab[2] >> 8);

            return output[(3 + T_EXTRA(info.OutputFormat))..];
        }
    }

    private static Span<byte> PackEncodedWordsLabV2FromFloat(Transform info,
                                                             ReadOnlySpan<float> wOut,
                                                             Span<byte> output,
                                                             uint Stride)
    {
        CIELab Lab;
        Span<ushort> wlab = stackalloc ushort[3];

        var Out = MemoryMarshal.Cast<byte, ushort>(output);

        Lab.L = wOut[0] * 100.0;
        Lab.a = (wOut[1] * 255.0) - 128.0;
        Lab.b = (wOut[2] * 255.0) - 128.0;

        Lab.FloatToEncoded(wlab);

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Stride /= PixelSize(info.OutputFormat);

            Out[0] = wlab[0];
            Out[(int)Stride] = wlab[1];
            Out[(int)Stride * 2] = wlab[2];

            return output[1..];
        }
        else
        {
            Out[0] = wlab[0];
            Out[1] = wlab[1];
            Out[2] = wlab[2];

            return output[((3 + T_EXTRA(info.OutputFormat)) * sizeof(ushort))..];
        }
    }

    private static Span<byte> PackLabDoubleFromFloat(Transform info,
                                                     ReadOnlySpan<float> wOut,
                                                     Span<byte> output,
                                                     uint Stride)
    {
        var Out = MemoryMarshal.Cast<byte, double>(output);

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Stride /= PixelSize(info.OutputFormat);

            Out[0] = wOut[0] * 100.0;
            Out[(int)Stride] = (wOut[1] * 255.0) - 128.0;
            Out[(int)Stride * 2] = (wOut[2] * 255.0) - 128.0;

            return output[sizeof(double)..];
        }
        else
        {
            Out[0] = wOut[0] * 100.0;
            Out[1] = (wOut[1] * 255.0) - 128.0;
            Out[2] = (wOut[2] * 255.0) - 128.0;

            return output[((3 + T_EXTRA(info.OutputFormat)) * sizeof(double))..];
        }
    }

    private static Span<byte> PackXYZFloatFromFloat(Transform info,
                                                    ReadOnlySpan<float> wOut,
                                                    Span<byte> output,
                                                    uint Stride)
    {
        var Out = MemoryMarshal.Cast<byte, float>(output);

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Stride /= PixelSize(info.OutputFormat);

            Out[0] = (float)(wOut[0] * CIEXYZ.MaxEncodeableXYZ);
            Out[(int)Stride] = (float)(wOut[1] * CIEXYZ.MaxEncodeableXYZ);
            Out[(int)Stride * 2] = (float)(wOut[2] * CIEXYZ.MaxEncodeableXYZ);

            return output[sizeof(float)..];
        }
        else
        {
            Out[0] = (float)(wOut[0] * CIEXYZ.MaxEncodeableXYZ);
            Out[1] = (float)(wOut[1] * CIEXYZ.MaxEncodeableXYZ);
            Out[2] = (float)(wOut[2] * CIEXYZ.MaxEncodeableXYZ);

            return output[((3 + T_EXTRA(info.OutputFormat)) * sizeof(float))..];
        }
    }

    private static Span<byte> PackXYZDoubleFromFloat(Transform info,
                                                     ReadOnlySpan<float> wOut,
                                                     Span<byte> output,
                                                     uint Stride)
    {
        var Out = MemoryMarshal.Cast<byte, double>(output);

        if (T_PLANAR(info.OutputFormat) is not 0)
        {
            Stride /= PixelSize(info.OutputFormat);

            Out[0] = wOut[0] * CIEXYZ.MaxEncodeableXYZ;
            Out[(int)Stride] = wOut[1] * CIEXYZ.MaxEncodeableXYZ;
            Out[(int)Stride * 2] = wOut[2] * CIEXYZ.MaxEncodeableXYZ;

            return output[sizeof(double)..];
        }
        else
        {
            Out[0] = wOut[0] * CIEXYZ.MaxEncodeableXYZ;
            Out[1] = wOut[1] * CIEXYZ.MaxEncodeableXYZ;
            Out[2] = wOut[2] * CIEXYZ.MaxEncodeableXYZ;

            return output[((3 + T_EXTRA(info.OutputFormat)) * sizeof(double))..];
        }
    }

    private static ReadOnlySpan<byte> UnrollHalfTo16(Transform info,
                                                     Span<ushort> wIn,
                                                     ReadOnlySpan<byte> accum,
                                                     uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;
        var maximum = IsInkSpace(info.InputFormat) ? 655.35F : 65535.0F;

        Stride /= PixelSize(info.InputFormat);
        var acc = MemoryMarshal.Cast<byte, Half>(accum);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var offset = (i + start) * (Planar ? (int)Stride : 1);
            var v = (float)acc[offset];

            if (Reverse)
                v = maximum - v;

            wIn[index] = QuickSaturateWord((double)v * maximum);
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(ushort))..];
    }

    private static ReadOnlySpan<byte> UnrollHalfToFloat(Transform info,
                                                        Span<float> wIn,
                                                        ReadOnlySpan<byte> accum,
                                                        uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.InputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var start = 0;
        var maximum = IsInkSpace(info.InputFormat) ? 100.0F : 1.0F;

        Stride /= PixelSize(info.InputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var acc = MemoryMarshal.Cast<byte, Half>(accum);

            var v = (float)acc[(i + start) * (Planar ? (int)Stride : 1)];

            v /= maximum;

            wIn[index] = Reverse ? 1 - v : v;
        }

        if (Extra is 0 && SwapFirst)
            UnrollSwapFirst(wIn, nChan);

        return accum[((T_PLANAR(info.InputFormat) is not 0 ? 1 : nChan + Extra) * sizeof(ushort))..];
    }

    private static Span<byte> PackHalfFrom16(Transform info, ReadOnlySpan<ushort> wOut, Span<byte> output, uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var maximum = IsInkSpace(info.OutputFormat) ? 655.35F : 65535.0F;
        var o = MemoryMarshal.Cast<byte, Half>(output);
        var swap1 = o;
        var start = 0;

        Stride /= PixelSize(info.OutputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] / maximum;

            if (Reverse)
                v = maximum - v;

            o[(i + start) * (Planar ? (int)Stride : 1)] = (Half)v;
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(ushort) * (Planar ? 1 : nChan + Extra))..];
    }

    private static Span<byte> PackHalfFromFloat(Transform info,
                                                ReadOnlySpan<float> wOut,
                                                Span<byte> output,
                                                uint Stride)
    {
        var (nChan, DoSwap, Reverse, SwapFirst, Extra, Planar, _, _) = T_BREAK(info.OutputFormat);
        var ExtraFirst = DoSwap ^ SwapFirst;
        var maximum = IsInkSpace(info.OutputFormat) ? 100.0F : 1.0F;
        var o = MemoryMarshal.Cast<byte, Half>(output);
        var swap1 = o;
        var start = 0;

        Stride /= PixelSize(info.OutputFormat);

        if (ExtraFirst)
            start = Extra;

        for (var i = 0; i < nChan; i++)
        {
            var index = DoSwap ? nChan - i - 1 : i;

            var v = wOut[index] * maximum;

            if (Reverse)
                v = maximum - v;

            o[(i + start) * (Planar ? (int)Stride : 1)] = (Half)v;
        }

        if (Extra is 0 && SwapFirst)
            PackSwapFirst(swap1, nChan);

        return output[(sizeof(ushort) * (Planar ? 1 : nChan + Extra))..];
    }

    internal static FormatterIn _cmsGetStockInputFormatter(uint dwInput, PackFlags dwFlags)
    {
        FormatterIn fr = default;

        switch (dwFlags)
        {
            case PackFlags.Ushort:
            {
                for (var i = 0; i < InputFormatters16.Length; i++)
                {
                    ref var f = ref InputFormatters16[i];

                    if ((dwInput & ~f.Mask) == f.Type)
                    {
                        fr.Fmt16 = f.Frm;
                        return fr;
                    }
                }

                break;
            }
            case PackFlags.Float:
            {
                for (var i = 0; i < InputFormattersFloat.Length; i++)
                {
                    ref var f = ref InputFormattersFloat[i];

                    if ((dwInput & ~f.Mask) == f.Type)
                    {
                        fr.FmtFloat = f.Frm;
                        return fr;
                    }
                }

                break;
            }
        }

        return default;
    }

    internal static FormatterOut _cmsGetStockOutputFormatter(uint dwInput, PackFlags dwFlags)
    {
        FormatterOut fr = default;

        // Optimization is only a hint
        dwInput &= ~OPTIMIZED_SH(1);

        switch (dwFlags)
        {
            case PackFlags.Ushort:
            {
                for (var i = 0; i < OutputFormatters16.Length; i++)
                {
                    ref var f = ref OutputFormatters16[i];

                    if ((dwInput & ~f.Mask) == f.Type)
                    {
                        fr.Fmt16 = f.Frm;
                        return fr;
                    }
                }

                break;
            }
            case PackFlags.Float:
            {
                for (var i = 0; i < OutputFormattersFloat.Length; i++)
                {
                    ref var f = ref OutputFormattersFloat[i];

                    if ((dwInput & ~f.Mask) == f.Type)
                    {
                        fr.FmtFloat = f.Frm;
                        return fr;
                    }
                }

                break;
            }
        }

        return default;
    }

    private static void DupFormatterFactoryList(ref FormattersPluginChunkType ctx, in FormattersPluginChunkType src) =>
        ctx = (FormattersPluginChunkType)((ICloneable)src).Clone();

    internal static void _cmsAllocFormattersPluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.FormattersPlugin
                       : FormattersPluginChunk;

        DupFormatterFactoryList(ref ctx.FormattersPlugin, from);
    }

    internal static FormatterIn _cmsGetFormatterIn(Context? ContextID, uint Type, PackFlags dwFlags)
    {
        var ctx = Context.Get(ContextID).FormattersPlugin;

        if (T_CHANNELS(Type) is 0)
            return default;

        foreach (var f in ctx.FactoryInList)
        {
            var fn = f(Type, (uint)dwFlags);
            if (fn.Fmt16 is not null)
                return fn;
        }

        // Revert to default
        return _cmsGetStockInputFormatter(Type, dwFlags);
    }

    internal static FormatterOut _cmsGetFormatterOut(Context? ContextID, uint Type, PackFlags dwFlags)
    {
        var ctx = Context.Get(ContextID).FormattersPlugin;

        if (T_CHANNELS(Type) is 0)
            return default;

        foreach (var f in ctx.FactoryOutList)
        {
            var fn = f(Type, (uint)dwFlags);
            if (fn.Fmt16 is not null)
                return fn;
        }

        // Revert to default
        return _cmsGetStockOutputFormatter(Type, dwFlags);
    }

    internal static bool _cmsFormatterIsFloat(uint Type) =>
        T_FLOAT(Type) is not 0;

    internal static bool _cmsFormatterIs8bit(uint Type) =>
        T_BYTES(Type) is 1;

    public static uint cmsFormatterForColorspaceOfProfile(Profile Profile, uint nBytes, bool lIsFloat)
    {
        var ColorSpace = cmsGetColorSpace(Profile);
        var ColorSpaceBits = (uint)_cmsLCMScolorSpace(ColorSpace);
        var nOutputChans = cmsChannelsOfColorSpace(ColorSpace);
        var Float = lIsFloat ? 1u : 0;

        // Unsupported color space?
        if (nOutputChans < 0)
            return 0;

        // Create a fake formatter for result
        return FLOAT_SH(Float) | COLORSPACE_SH(ColorSpaceBits) | BYTES_SH(nBytes) | CHANNELS_SH((uint)nOutputChans);
    }

    public static uint cmsFormatterForPCSOfProfile(Profile Profile, uint nBytes, bool lIsFloat)
    {
        var ColorSpace = cmsGetPCS(Profile);

        var ColorSpaceBits = (uint)_cmsLCMScolorSpace(ColorSpace);
        var nOutputChans = cmsChannelsOfColorSpace(ColorSpace);
        var Float = lIsFloat ? 1u : 0;

        // Unsupported color space?
        if (nOutputChans < 0)
            return 0;

        // Create a fake formatter for result
        return FLOAT_SH(Float) | COLORSPACE_SH(ColorSpaceBits) | BYTES_SH(nBytes) | CHANNELS_SH((uint)nOutputChans);
    }
}
