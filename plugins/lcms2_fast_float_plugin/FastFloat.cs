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
    internal const uint REQUIRED_LCMS_VERSION = 2160;

    internal static uint BIT15_SH(uint a) =>
        ((a) << 26);

    internal static uint T_BIT15(uint a) =>
        (((a) >> 26) & 1);

    internal static uint DITHER_SH(uint a) =>
        ((a) << 27);

    internal static uint T_DITHER(uint a)  =>
        (((a) >> 27) & 1);

    public static uint TYPE_GRAY_15 =>
        (COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(2) | BIT15_SH(1));

    public static uint TYPE_GRAY_15_REV =>
        (COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(2) | FLAVOR_SH(1) | BIT15_SH(1));

    public static uint TYPE_GRAY_15_SE =>
        (COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(2) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_GRAYA_15 =>
        (COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | BIT15_SH(1));

    public static uint TYPE_GRAYA_15_SE =>
        (COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_GRAYA_15_PLANAR =>
        (COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | PLANAR_SH(1) | BIT15_SH(1));

    public static uint TYPE_RGB_15 =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | BIT15_SH(1));

    public static uint TYPE_RGB_15_PLANAR =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1) | BIT15_SH(1));

    public static uint TYPE_RGB_15_SE =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_BGR_15 =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | BIT15_SH(1));

    public static uint TYPE_BGR_15_PLANAR =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | PLANAR_SH(1) | BIT15_SH(1));

    public static uint TYPE_BGR_15_SE =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_RGBA_15 =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | BIT15_SH(1));

    public static uint TYPE_RGBA_15_PLANAR =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1) | BIT15_SH(1));

    public static uint TYPE_RGBA_15_SE =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_ARGB_15 =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | SWAPFIRST_SH(1) | BIT15_SH(1));

    public static uint TYPE_ABGR_15 =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | BIT15_SH(1));

    public static uint TYPE_ABGR_15_PLANAR =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | PLANAR_SH(1) |
         BIT15_SH(1));

    public static uint TYPE_ABGR_15_SE =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1) |
         BIT15_SH(1));

    public static uint TYPE_BGRA_15 =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1) |
         BIT15_SH(1));

    public static uint TYPE_BGRA_15_SE =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1) | DOSWAP_SH(1) |
         SWAPFIRST_SH(1) | BIT15_SH(1));

    public static uint TYPE_CMY_15 =>
        (COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(2) | BIT15_SH(1));

    public static uint TYPE_YMC_15 =>
        (COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | BIT15_SH(1));

    public static uint TYPE_CMY_15_PLANAR =>
        (COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1) | BIT15_SH(1));

    public static uint TYPE_CMY_15_SE =>
        (COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_CMYK_15 =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | BIT15_SH(1));

    public static uint TYPE_CMYK_15_REV =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | FLAVOR_SH(1) | BIT15_SH(1));

    public static uint TYPE_CMYK_15_PLANAR =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | PLANAR_SH(1) | BIT15_SH(1));

    public static uint TYPE_CMYK_15_SE =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_KYMC_15 =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | DOSWAP_SH(1) | BIT15_SH(1));

    public static uint TYPE_KYMC_15_SE =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1) | BIT15_SH(1));

    public static uint TYPE_KCMY_15 =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | SWAPFIRST_SH(1) | BIT15_SH(1));

    public static uint TYPE_KCMY_15_REV =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | FLAVOR_SH(1) | SWAPFIRST_SH(1) | BIT15_SH(1));

    public static uint TYPE_KCMY_15_SE =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | ENDIAN16_SH(1) | SWAPFIRST_SH(1) | BIT15_SH(1));

    public static uint TYPE_GRAY_8_DITHER =>
        (COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(1) | DITHER_SH(1));

    public static uint TYPE_RGB_8_DITHER =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1) | DITHER_SH(1));

    public static uint TYPE_RGBA_8_DITHER =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | DITHER_SH(1));

    public static uint TYPE_BGR_8_DITHER =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | DITHER_SH(1));

    public static uint TYPE_ABGR_8_DITHER =>
        (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | EXTRA_SH(1) | DITHER_SH(1));

    public static uint TYPE_CMYK_8_DITHER =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1) | DITHER_SH(1));

    public static uint TYPE_KYMC_8_DITHER =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1) | DOSWAP_SH(1) | DITHER_SH(1));


    public static uint TYPE_AGRAY_8 =>
        (COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | DOSWAP_SH(1) | BYTES_SH(1));

    public static uint TYPE_AGRAY_16 =>
        (COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | DOSWAP_SH(1) | BYTES_SH(2));

    public static uint TYPE_AGRAY_FLT =>
        (FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | DOSWAP_SH(1) | BYTES_SH(4));

    public static uint TYPE_AGRAY_DBL =>
        (FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | DOSWAP_SH(1) | BYTES_SH(0));

    public static uint TYPE_ACMYK_8 =>
        (COLORSPACE_SH(PT_CMYK) | EXTRA_SH(1) | CHANNELS_SH(4) | BYTES_SH(1) | SWAPFIRST_SH(1));

    public static uint TYPE_KYMCA_8 =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | EXTRA_SH(1) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1));

    public static uint TYPE_AKYMC_8 =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | EXTRA_SH(1) | BYTES_SH(1) | DOSWAP_SH(1));

    public static uint TYPE_CMYKA_16 =>
        (COLORSPACE_SH(PT_CMYK) | EXTRA_SH(1) | CHANNELS_SH(4) | BYTES_SH(2));

    public static uint TYPE_ACMYK_16 =>
        (COLORSPACE_SH(PT_CMYK) | EXTRA_SH(1) | CHANNELS_SH(4) | BYTES_SH(2) | SWAPFIRST_SH(1));

    public static uint TYPE_KYMCA_16 =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | EXTRA_SH(1) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1));

    public static uint TYPE_AKYMC_16 =>
        (COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | EXTRA_SH(1) | BYTES_SH(2) | DOSWAP_SH(1));


    public static uint TYPE_AGRAY_8_PLANAR =>
        (COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(1) | PLANAR_SH(1) | SWAPFIRST_SH(1));

    public static uint TYPE_AGRAY_16_PLANAR =>
        (COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | PLANAR_SH(1) | SWAPFIRST_SH(1));

    public static uint TYPE_GRAYA_FLT_PLANAR =>
        (FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(4) | PLANAR_SH(1));

    public static uint TYPE_AGRAY_FLT_PLANAR =>
        (FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(4) | PLANAR_SH(1) |
         SWAPFIRST_SH(1));

    public static uint TYPE_GRAYA_DBL_PLANAR =>
        (FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(0) | PLANAR_SH(1));

    public static uint TYPE_AGRAY_DBL_PLANAR =>
        (FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(0) | PLANAR_SH(1) |
         SWAPFIRST_SH(1));

    public static uint TYPE_ARGB_16_PLANAR =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | SWAPFIRST_SH(1) | PLANAR_SH(1));

    public static uint TYPE_BGRA_16_PLANAR =>
        (COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | SWAPFIRST_SH(1) | DOSWAP_SH(1) |
         PLANAR_SH(1));

    //[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static ushort FROM_8_TO_16(byte rgb) =>
    //    (ushort)((rgb << 8) | rgb);

    //[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static byte FROM_16_TO_8(ushort rgb) =>
    //    (byte)((((rgb * 65281) + 8388608) >> 24) & 0xFF);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort CHANGE_ENDIAN(ushort w) =>
        (ushort)((w << 8) | (w >> 8));

    //[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static ushort REVERSE_FLAVOR(ushort x) =>
    //    (ushort)(0xffff-x);

    //[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static int FIXED_TO_INT(int x) =>
    //    x >> 16;

    //[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static int FIXED_REST_TO_INT(int x) =>
    //    x & 0xffff;

    private const uint cmsFLAGS_CAN_CHANGE_FORMATTER = 0x02000000;   // Allow change buffer format

    //[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static S15Fixed16Number _cmsToFixedDomain(int a) =>
    //    a + ((a + 0x7fff) / 0xffff);

    //[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static int _cmsFromFixedDomain(S15Fixed16Number a) =>
    //    a - ((a + 0x7fff) >> 16);

    internal record _xform_head(uint InputFormat, uint OutputFormat)
    {
        public static explicit operator _xform_head(Transform t) =>
            new(t.InputFormat, t.OutputFormat);

        public static explicit operator Transform(_xform_head t) =>
            new() { InputFormat = t.InputFormat, OutputFormat = t.OutputFormat };
    }

    internal const ushort MAX_NODES_IN_CURVE = 0x8001;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float fclamp(float v) =>
        ((v < 1.0e-9f) || float.IsNaN(v))
            ? 0.0f
            : Math.Min(1.0f, v);

    //    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    //    internal static int _cmsQuickFloor(double val)
    //    {
    //#if CMS_DONT_USE_FAST_FLOOR
    //        return (int)Math.Floor(val);
    //#else
    //        Span<byte> buffer = stackalloc byte[8];
    //        const double _lcms_double2fixmagic = 68719476736.0 * 1.5;
    //        BitConverter.TryWriteBytes(buffer, val + _lcms_double2fixmagic);

    //        return BitConverter.ToInt32(buffer) >> 16;
    //#endif
    //    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static ushort _cmsSaturateWord(double d) =>
        (d + 0.5) switch
        {
            <= 0       => 0,
            >= 65535.0 => 0xffff,
            _          => (ushort)Math.Floor(d + 0.5)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static float flerp(ReadOnlySpan<float> LutTable, float v)
    {
        unsafe
        {
            fixed (float* p = LutTable)
            {
                return flerp(p, v);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal unsafe static float flerp(float* LutTable, float v)
    {
        if (float.IsNaN(v))
        {
            return LutTable[0];
        }
        else if (v is < 1.0e-9f or >= 1.0f)
        {
            return v;
        }

        v *= MAX_NODES_IN_CURVE - 1;

        var cell0 = Conversions.QuickFloor(v);
        var cell1 = (int)Math.Ceiling(v);

        // Rest is 16 LSB bits
        var rest = v - cell0;

        var y0 = LutTable[cell0];
        var y1 = LutTable[cell1];

        return y0 + (y1 - y0) * rest;
    }

    private static void FreeDisposable(Context? _, object? Data)
    {
        if (Data is IDisposable p)
            p.Dispose();
    }
}
