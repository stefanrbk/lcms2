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

using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

namespace lcms2.FastFloatPlugin.testbed;

internal static partial class Testbed
{
    [DebuggerDisplay("[r: {r}, g: {g}, b: {b}]")]
    internal struct Scanline_rgb8bits(byte r, byte g, byte b)
    {
        public byte r = r, g = g, b = b;
    }

    [DebuggerDisplay("[r: {r}, g: {g}, b: {b}, a: {a}]")]
    internal struct Scanline_rgba8bits(byte r, byte g, byte b, byte a)
    {
        public byte r = r, g = g, b = b, a = a;
    }

    [DebuggerDisplay("[c: {c}, m: {m}, y: {y}, k: {k}]")]
    internal struct Scanline_cmyk8bits(byte c, byte m, byte y, byte k)
    {
        public byte c = c, m = m, y = y, k = k;
    }

    [DebuggerDisplay("[r: {r}, g: {g}, b: {b}]")]
    internal struct Scanline_rgb16bits(ushort r, ushort g, ushort b)
    {
        public ushort r = r, g = g, b = b;
    }

    [DebuggerDisplay("[r: {r}, g: {g}, b: {b}, a: {a}]")]
    internal struct Scanline_rgba16bits(ushort r, ushort g, ushort b, ushort a)
    {
        public ushort r = r, g = g, b = b, a = a;
    }

    [DebuggerDisplay("[c: {c}, m: {m}, y: {y}, k: {k}]")]
    internal struct Scanline_cmyk16bits(ushort c, ushort m, ushort y, ushort k)
    {
        public ushort c = c, m = m, y = y, k = k;
    }

    [DebuggerDisplay("[L: {L}, a: {a}, b: {b}]")]
    internal struct Scanline_Lab16bits(ushort L, ushort a, ushort b)
    {
        public ushort L = L, a = a, b = b;
    }

    [DebuggerDisplay("[r: {r}, g: {g}, b: {b}]")]
    internal struct Scanline_rgb15bits(ushort r, ushort g, ushort b)
    {
        public ushort r = r, g = g, b = b;
    }

    [DebuggerDisplay("[r: {r}, g: {g}, b: {b}, a: {a}]")]
    internal struct Scanline_rgba15bits(ushort r, ushort g, ushort b, ushort a)
    {
        public ushort r = r, g = g, b = b, a = a;
    }

    [DebuggerDisplay("[c: {c}, m: {m}, y: {y}, k: {k}]")]
    internal struct Scanline_cmyk15bits(ushort c, ushort m, ushort y, ushort k)
    {
        public ushort c = c, m = m, y = y, k = k;
    }

    [DebuggerDisplay("[r: {r:F6}, g: {g:F6}, b: {b:F6}]")]
    internal struct Scanline_rgbFloat(float r, float g, float b)
    {
        public float r = r, g = g, b = b;
    }

    [DebuggerDisplay("[r: {r:F6}, g: {g:F6}, b: {b:F6}, a: {a:F6}]")]
    internal struct Scanline_rgbaFloat(float r, float g, float b, float a)
    {
        public float r = r, g = g, b = b, a = a;
    }

    [DebuggerDisplay("[c: {c:F6}, m: {m:F6}, y: {y:F6}, k: {k:F6}]")]
    internal struct Scanline_cmykFloat(float c, float m, float y, float k)
    {
        public float c = c, m = m, y = y, k = k;
    }

    [DebuggerDisplay("[L: {L:F6}, a: {a:F6}, b: {b:F6}]")]
    internal struct Scanline_LabFloat(float L, float a, float b)
    {
        public float L = L, a = a, b = b;
    }

    private static void CheckSingleFormatter15(Context? _, uint Type, string Text)
    {
        Span<ushort> Values = stackalloc ushort[cmsMAXCHANNELS];
        Span<byte> Buffer = stackalloc byte[1024];

        var info = new _xform_head(Type, Type);

        // Get functions to go back and forth
        var f = Formatter_15Bit_Factory_In(Type, (uint)PackFlags.Ushort);
        var b = Formatter_15Bit_Factory_Out(Type, (uint)PackFlags.Ushort);

        if (f.Fmt16 is null || b.Fmt16 is null)
        {
            Fail("No formatter for {s}", Text);
            return;
        }

        var nChannels = T_CHANNELS(Type);
        var bytes = T_BYTES(Type);

        for (var j = 0; j < 5; j++)
        {
            for (var i = 0; i < nChannels; i++)
            {
                Values[i] = (ushort)((i + j) << 1);
            }

            b.Fmt16((Transform)info, Values, Buffer, 1);
            Values.Clear();
            f.Fmt16((Transform)info, Values, Buffer, 1);

            for (var i = 0; i < nChannels; i++)
            {
                if (Values[i] != ((i + j) << 1))
                {
                    Fail("{0} failed", Text);
                    return;
                }
            }
        }
    }

    public static void CheckFormatters15()
    {
        C(nameof(TYPE_GRAY_15));
        C(nameof(TYPE_GRAY_15_REV));
        C(nameof(TYPE_GRAY_15_SE));
        C(nameof(TYPE_GRAYA_15));
        C(nameof(TYPE_GRAYA_15_SE));
        C(nameof(TYPE_GRAYA_15_PLANAR));
        C(nameof(TYPE_RGB_15));
        C(nameof(TYPE_RGB_15_PLANAR));
        C(nameof(TYPE_RGB_15_SE));
        C(nameof(TYPE_BGR_15));
        C(nameof(TYPE_BGR_15_PLANAR));
        C(nameof(TYPE_BGR_15_SE));
        C(nameof(TYPE_RGBA_15));
        C(nameof(TYPE_RGBA_15_PLANAR));
        C(nameof(TYPE_RGBA_15_SE));
        C(nameof(TYPE_ARGB_15));
        C(nameof(TYPE_ABGR_15));
        C(nameof(TYPE_ABGR_15_PLANAR));
        C(nameof(TYPE_ABGR_15_SE));
        C(nameof(TYPE_BGRA_15));
        C(nameof(TYPE_BGRA_15_SE));
        C(nameof(TYPE_YMC_15));
        C(nameof(TYPE_CMY_15));
        C(nameof(TYPE_CMY_15_PLANAR));
        C(nameof(TYPE_CMY_15_SE));
        C(nameof(TYPE_CMYK_15));
        C(nameof(TYPE_CMYK_15_REV));
        C(nameof(TYPE_CMYK_15_PLANAR));
        C(nameof(TYPE_CMYK_15_SE));
        C(nameof(TYPE_KYMC_15));
        C(nameof(TYPE_KYMC_15_SE));
        C(nameof(TYPE_KCMY_15));
        C(nameof(TYPE_KCMY_15_REV));
        C(nameof(TYPE_KCMY_15_SE));

        static void C(string a)
        {
            var field = typeof(FastFloat).GetProperty(a) ?? typeof(Lcms2).GetProperty(a);
            var value = (uint)field!.GetValue(null)!;

            CheckSingleFormatter15(null, value, a);
        }
    }

    private static bool checkSingleComputeIncrements(uint Format,
                                                     uint planeStride,
                                                     uint ExpectedChannels,
                                                     uint ExpectedAlpha,
                                                     params uint[] args)
    {
        Span<uint> ComponentStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> ComponentPointerIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(Format,
                                       planeStride,
                                       out var nChannels,
                                       out var nAlpha,
                                       ComponentStartingOrder,
                                       ComponentPointerIncrements);

        if (nChannels != ExpectedChannels ||
            nAlpha != ExpectedAlpha)
        {
            return false;
        }

        var nTotal = nAlpha + nChannels;

        var argIndex = 0;
        for (var i = 0; i < nTotal; i++)
        {
            var so = args[argIndex++];
            if (so != ComponentStartingOrder[i])
                return false;
        }

        for (var i = 0; i < nTotal; i++)
        {
            var so = args[argIndex++];
            if (so != ComponentPointerIncrements[i])
                return false;
        }

        return true;
    }

    public static void CheckComputeIncrements()
    {
        using (logger.BeginScope("Check compute increments"))
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
#endif
            CHECK(nameof(TYPE_GRAY_8), 0, 1, 0, /**/ 0,    /**/ 1);
            CHECK(nameof(TYPE_GRAYA_8), 0, 1, 1, /**/ 0, 1, /**/ 2, 2);
            CHECK(nameof(TYPE_AGRAY_8), 0, 1, 1, /**/ 1, 0, /**/ 2, 2);
            CHECK(nameof(TYPE_GRAY_16), 0, 1, 0, /**/ 0,    /**/ 2);
            CHECK(nameof(TYPE_GRAYA_16), 0, 1, 1, /**/ 0, 2, /**/ 4, 4);
            CHECK(nameof(TYPE_AGRAY_16), 0, 1, 1, /**/ 2, 0, /**/ 4, 4);

            CHECK(nameof(TYPE_GRAY_FLT), 0, 1, 0, /**/ 0,    /**/ 4);
            CHECK(nameof(TYPE_GRAYA_FLT), 0, 1, 1, /**/ 0, 4, /**/ 8, 8);
            CHECK(nameof(TYPE_AGRAY_FLT), 0, 1, 1, /**/ 4, 0, /**/ 8, 8);

            CHECK(nameof(TYPE_GRAY_DBL), 0, 1, 0, /**/ 0,      /**/ 8);
            CHECK(nameof(TYPE_AGRAY_DBL), 0, 1, 1, /**/ 8, 0,   /**/ 16, 16);

            CHECK(nameof(TYPE_RGB_8), 0, 3, 0, /**/ 0, 1, 2,     /**/ 3, 3, 3);
            CHECK(nameof(TYPE_RGBA_8), 0, 3, 1, /**/ 0, 1, 2, 3,  /**/ 4, 4, 4, 4);
            CHECK(nameof(TYPE_ARGB_8), 0, 3, 1, /**/ 1, 2, 3, 0,  /**/ 4, 4, 4, 4);

            CHECK(nameof(TYPE_RGB_16), 0, 3, 0, /**/ 0, 2, 4,     /**/ 6, 6, 6);
            CHECK(nameof(TYPE_RGBA_16), 0, 3, 1, /**/ 0, 2, 4, 6,  /**/ 8, 8, 8, 8);
            CHECK(nameof(TYPE_ARGB_16), 0, 3, 1, /**/ 2, 4, 6, 0,  /**/ 8, 8, 8, 8);

            CHECK(nameof(TYPE_RGB_FLT), 0, 3, 0, /**/ 0, 4, 8,     /**/ 12, 12, 12);
            CHECK(nameof(TYPE_RGBA_FLT), 0, 3, 1, /**/ 0, 4, 8, 12,  /**/ 16, 16, 16, 16);
            CHECK(nameof(TYPE_ARGB_FLT), 0, 3, 1, /**/ 4, 8, 12, 0,  /**/ 16, 16, 16, 16);

            CHECK(nameof(TYPE_BGR_8), 0, 3, 0, /**/ 2, 1, 0,     /**/ 3, 3, 3);
            CHECK(nameof(TYPE_BGRA_8), 0, 3, 1, /**/ 2, 1, 0, 3,  /**/ 4, 4, 4, 4);
            CHECK(nameof(TYPE_ABGR_8), 0, 3, 1, /**/ 3, 2, 1, 0,  /**/ 4, 4, 4, 4);

            CHECK(nameof(TYPE_BGR_16), 0, 3, 0, /**/ 4, 2, 0,     /**/ 6, 6, 6);
            CHECK(nameof(TYPE_BGRA_16), 0, 3, 1, /**/ 4, 2, 0, 6,  /**/ 8, 8, 8, 8);
            CHECK(nameof(TYPE_ABGR_16), 0, 3, 1, /**/ 6, 4, 2, 0,  /**/ 8, 8, 8, 8);

            CHECK(nameof(TYPE_BGR_FLT), 0, 3, 0,  /**/ 8, 4, 0,     /**/  12, 12, 12);
            CHECK(nameof(TYPE_BGRA_FLT), 0, 3, 1, /**/ 8, 4, 0, 12,  /**/ 16, 16, 16, 16);
            CHECK(nameof(TYPE_ABGR_FLT), 0, 3, 1, /**/ 12, 8, 4, 0,  /**/ 16, 16, 16, 16);

            CHECK(nameof(TYPE_CMYK_8), 0, 4, 0, /**/ 0, 1, 2, 3,     /**/ 4, 4, 4, 4);
            CHECK(nameof(TYPE_CMYKA_8), 0, 4, 1, /**/ 0, 1, 2, 3, 4,  /**/ 5, 5, 5, 5, 5);
            CHECK(nameof(TYPE_ACMYK_8), 0, 4, 1, /**/ 1, 2, 3, 4, 0,  /**/ 5, 5, 5, 5, 5);

            CHECK(nameof(TYPE_KYMC_8), 0, 4, 0, /**/ 3, 2, 1, 0,     /**/ 4, 4, 4, 4);
            CHECK(nameof(TYPE_KYMCA_8), 0, 4, 1, /**/ 3, 2, 1, 0, 4,  /**/ 5, 5, 5, 5, 5);
            CHECK(nameof(TYPE_AKYMC_8), 0, 4, 1, /**/ 4, 3, 2, 1, 0,  /**/ 5, 5, 5, 5, 5);

            CHECK(nameof(TYPE_KCMY_8), 0, 4, 0, /**/ 1, 2, 3, 0,      /**/ 4, 4, 4, 4);

            CHECK(nameof(TYPE_CMYK_16), 0, 4, 0, /**/ 0, 2, 4, 6,      /**/ 8, 8, 8, 8);
            CHECK(nameof(TYPE_CMYKA_16), 0, 4, 1, /**/ 0, 2, 4, 6, 8,  /**/ 10, 10, 10, 10, 10);
            CHECK(nameof(TYPE_ACMYK_16), 0, 4, 1, /**/ 2, 4, 6, 8, 0,  /**/ 10, 10, 10, 10, 10);

            CHECK(nameof(TYPE_KYMC_16), 0, 4, 0,  /**/ 6, 4, 2, 0,     /**/ 8, 8, 8, 8);
            CHECK(nameof(TYPE_KYMCA_16), 0, 4, 1, /**/ 6, 4, 2, 0, 8,  /**/ 10, 10, 10, 10, 10);
            CHECK(nameof(TYPE_AKYMC_16), 0, 4, 1, /**/ 8, 6, 4, 2, 0,  /**/ 10, 10, 10, 10, 10);

            CHECK(nameof(TYPE_KCMY_16), 0, 4, 0, /**/ 2, 4, 6, 0,      /**/ 8, 8, 8, 8);

            // Planar

            CHECK(nameof(TYPE_GRAYA_8_PLANAR), 100, 1, 1, /**/ 0, 100,  /**/ 1, 1);
            CHECK(nameof(TYPE_AGRAY_8_PLANAR), 100, 1, 1, /**/ 100, 0,  /**/ 1, 1);

            CHECK(nameof(TYPE_GRAYA_16_PLANAR), 100, 1, 1, /**/ 0, 100,   /**/ 2, 2);
            CHECK(nameof(TYPE_AGRAY_16_PLANAR), 100, 1, 1, /**/ 100, 0,   /**/ 2, 2);

            CHECK(nameof(TYPE_GRAYA_FLT_PLANAR), 100, 1, 1, /**/ 0, 100,   /**/ 4, 4);
            CHECK(nameof(TYPE_AGRAY_FLT_PLANAR), 100, 1, 1, /**/ 100, 0,   /**/ 4, 4);

            CHECK(nameof(TYPE_GRAYA_DBL_PLANAR), 100, 1, 1, /**/ 0, 100,   /**/ 8, 8);
            CHECK(nameof(TYPE_AGRAY_DBL_PLANAR), 100, 1, 1, /**/ 100, 0,   /**/ 8, 8);

            CHECK(nameof(TYPE_RGB_8_PLANAR), 100, 3, 0, /**/ 0, 100, 200,      /**/ 1, 1, 1);
            CHECK(nameof(TYPE_RGBA_8_PLANAR), 100, 3, 1, /**/ 0, 100, 200, 300, /**/ 1, 1, 1, 1);
            CHECK(nameof(TYPE_ARGB_8_PLANAR), 100, 3, 1, /**/ 100, 200, 300, 0,  /**/ 1, 1, 1, 1);

            CHECK(nameof(TYPE_BGR_8_PLANAR), 100, 3, 0, /**/ 200, 100, 0,       /**/ 1, 1, 1);
            CHECK(nameof(TYPE_BGRA_8_PLANAR), 100, 3, 1, /**/ 200, 100, 0, 300,  /**/ 1, 1, 1, 1);
            CHECK(nameof(TYPE_ABGR_8_PLANAR), 100, 3, 1, /**/ 300, 200, 100, 0,  /**/ 1, 1, 1, 1);

            CHECK(nameof(TYPE_RGB_16_PLANAR), 100, 3, 0, /**/ 0, 100, 200,      /**/ 2, 2, 2);
            CHECK(nameof(TYPE_RGBA_16_PLANAR), 100, 3, 1, /**/ 0, 100, 200, 300, /**/ 2, 2, 2, 2);
            CHECK(nameof(TYPE_ARGB_16_PLANAR), 100, 3, 1, /**/ 100, 200, 300, 0,  /**/ 2, 2, 2, 2);

            CHECK(nameof(TYPE_BGR_16_PLANAR), 100, 3, 0, /**/ 200, 100, 0,       /**/ 2, 2, 2);
            CHECK(nameof(TYPE_BGRA_16_PLANAR), 100, 3, 1, /**/ 200, 100, 0, 300,  /**/ 2, 2, 2, 2);
            CHECK(nameof(TYPE_ABGR_16_PLANAR), 100, 3, 1, /**/ 300, 200, 100, 0,  /**/ 2, 2, 2, 2);

            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }

        static bool CHECK(string frm, uint plane, uint chans, uint alpha, params uint[] args)
        {
            using (logger.BeginScope("{frm}", frm))
            {
                var field = typeof(FastFloat).GetProperty(frm) ?? typeof(Lcms2).GetProperty(frm);
                var value = (uint)field!.GetValue(null)!;

                if (!checkSingleComputeIncrements(value, plane, chans, alpha, args))
                {
                    logger.LogError("Format failed!");
                    return false;
                }

                return true;
            }
        }
    }

    private static bool Valid15(ushort a, byte b) =>
        Math.Abs(FROM_15_TO_8(a) - b) <= 2;

    private static void Check15bitMacros()
    {
        using (logger.BeginScope("Checking 15 bit <=> 8 bit conversions"))
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
#endif
            for (var i = 0; i < 256; i++)
            {
                var n = FROM_8_TO_15((byte)i);
                var m = FROM_15_TO_8(n);

                if (m != i)
                    Fail("Failed on {0} (->{1}->{2})", i, n, m);
            }

            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }
    }

    private static void TryAllValues15(Profile profileIn, Profile profileOut, int Intent)
    {
        var xform15 =
            cmsCreateTransform(profileIn, TYPE_RGB_15, profileOut, TYPE_RGB_15, (uint)Intent, cmsFLAGS_NOCACHE);
        var xform8 = cmsCreateTransform(profileIn, TYPE_RGB_8, profileOut, TYPE_RGB_8, (uint)Intent, cmsFLAGS_NOCACHE);

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (xform15 is null || xform8 is null)
            Fail("Null transforms on check for 15 bit conversions");

        const int npixels = 256 * 256 * 256;  // All RGB cube in 8 bits

        var buffer8in = new Scanline_rgb8bits[npixels];
        var buffer8out = new Scanline_rgb8bits[npixels];
        var buffer15in = new Scanline_rgb15bits[npixels];
        var buffer15out = new Scanline_rgb15bits[npixels];

        // Fill input values for 8 and 15 bits
        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    buffer8in[j].r = (byte)r;
                    buffer8in[j].g = (byte)g;
                    buffer8in[j].b = (byte)b;

                    buffer15in[j].r = FROM_8_TO_15((byte)r);
                    buffer15in[j].g = FROM_8_TO_15((byte)g);
                    buffer15in[j].b = FROM_8_TO_15((byte)b);

                    j++;
                }
            }
        }

#if NO_THREADS
        DoTransforms(0, npixels, xform8, xform15, buffer8in, buffer15in, buffer8out, buffer15out);
        var failed = CompareTransforms(0, npixels, buffer8out, buffer15out);

#else

        var nThreads = LargestPowerOf2(Environment.ProcessorCount);
        var nPixelsPerThread = npixels / nThreads;

        var tasks = new Task<int>[nThreads];

        int test(object? o)
        {
            var offset = (int)o!;

            using (logger.BeginScope("Range {RangeStart}..{RangeEnd}",
                                     offset * nPixelsPerThread,
                                     (offset + 1) * nPixelsPerThread))
            {
                DoTransforms(offset, nPixelsPerThread, xform8, xform15, buffer8in, buffer15in, buffer8out, buffer15out);
                return CompareTransforms(offset, nPixelsPerThread, buffer8out, buffer15out);
            }
        }

        for (var i = 0; i < nThreads; i++)
        {
            tasks[i] = Task.Factory.StartNew(test, i);
        }

        Task.WaitAll(tasks);

        var threadingFailed = tasks.Select(t => t.IsCompletedSuccessfully).Contains(false);
        var failed = tasks.Sum(s => s.Result);

        if (threadingFailed || failed > 0)
        {
            if (threadingFailed)
            {
                logger.LogWarning("Multithreading failure. Retrying single-threaded");
            }
            else
            {
                logger.LogWarning("{failed} failed. Retyring single-threaded", failed);
            }

            DoTransforms(0, npixels, xform8, xform15, buffer8in, buffer15in, buffer8out, buffer15out);
            failed = CompareTransforms(0, npixels, buffer8out, buffer15out);
        }

#endif

        cmsDeleteTransform(xform15);
        cmsDeleteTransform(xform8);

        if (failed is not 0)
            Fail("{0} failed", failed);

        static void DoTransforms(int offset,
                                 int nPixelsPerThread,
                                 Transform xform8,
                                 Transform xform15,
                                 Scanline_rgb8bits[] buffer8In,
                                 Scanline_rgb15bits[] buffer15In,
                                 Scanline_rgb8bits[] buffer8Out,
                                 Scanline_rgb15bits[] buffer15Out)
        {
            var start = offset * nPixelsPerThread;
            var b8In = buffer8In.AsSpan(start..)[..nPixelsPerThread];
            var b15In = buffer15In.AsSpan(start..)[..nPixelsPerThread];
            var b8Out = buffer8Out.AsSpan(start..)[..nPixelsPerThread];
            var b15Out = buffer15Out.AsSpan(start..)[..nPixelsPerThread];

            cmsDoTransform(xform15, b15In, b15Out, (uint)nPixelsPerThread);
            cmsDoTransform(xform8, b8In, b8Out, (uint)nPixelsPerThread);
        }

        static int CompareTransforms(int offset,
                                     int nPixelsPerThread,
                                     Scanline_rgb8bits[] buffer8out,
                                     Scanline_rgb15bits[] buffer15out)
        {
            // Let's compare results
            var start = offset * nPixelsPerThread;
            var end = (offset + 1) * nPixelsPerThread;

            var failed = 0;
            for (var j = start; j < end; j++)
            {
                // Check the results
                if (!Valid15(buffer15out[j].r, buffer8out[j].r) ||
                    !Valid15(buffer15out[j].g, buffer8out[j].g) ||
                    !Valid15(buffer15out[j].b, buffer8out[j].b))
                {
                    if (failed++ is 0)
                    {
                        logger.LogError("Conversion first failed at ({r8} {g8} {b8}) != ({r15} {g15} {b15})",
                                        buffer8out[j].r,
                                        buffer8out[j].g,
                                        buffer8out[j].b,
                                        FROM_15_TO_8(buffer15out[j].r),
                                        FROM_15_TO_8(buffer15out[j].g),
                                        FROM_15_TO_8(buffer15out[j].b));
                    }
                }
            }

            return failed;
        }
    }

    public static void Check15bitsConversion()
    {
        Check15bitMacros();

#if DEBUG
        var timer = new Stopwatch();
#endif
        using (logger.BeginScope("Checking accuracy of 15 bits on CLUT"))
        {
#if DEBUG
            timer.Restart();
#endif
            TryAllValues15(cmsOpenProfileFromMem(TestProfiles.test5)!,
                           cmsOpenProfileFromMem(TestProfiles.test3)!,
                           INTENT_PERCEPTUAL);
            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }

        using (logger.BeginScope("Checking accuracy of 15 bits on same profile"))
        {
#if DEBUG
            timer.Restart();
#endif
            TryAllValues15(cmsOpenProfileFromMem(TestProfiles.test0)!,
                           cmsOpenProfileFromMem(TestProfiles.test0)!,
                           INTENT_PERCEPTUAL);
            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }

        using (logger.BeginScope("Checking accuracy of 15 bits on Matrix"))
        {
#if DEBUG
            timer.Restart();
#endif
            TryAllValues15(cmsOpenProfileFromMem(TestProfiles.test5)!,
                           cmsOpenProfileFromMem(TestProfiles.test0)!,
                           INTENT_PERCEPTUAL);
            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }

        trace("All 15 bit tests passed");
    }

    private static void TryAllValues16(Profile profileIn, Profile profileOut, int Intent)
    {
        var Raw = new Context();
        var Plugin = new Context(cmsFastFloatExtensions());

        var xformRaw = cmsCreateTransformTHR(Raw,
                                             profileIn,
                                             TYPE_RGBA_16,
                                             profileOut,
                                             TYPE_RGBA_16,
                                             (uint)Intent,
                                             cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);
        var xformPlugin = cmsCreateTransformTHR(Plugin,
                                                profileIn,
                                                TYPE_RGBA_16,
                                                profileOut,
                                                TYPE_RGBA_16,
                                                (uint)Intent,
                                                cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (xformRaw is null || xformPlugin is null)
            Fail("Null transforms on check for float conversions");

        const int npixels = 256 * 256 * 256;  // All RGB cube in 8 bits

        var bufferIn = new Scanline_rgba16bits[npixels];
        var bufferRawOut = new Scanline_rgba16bits[npixels];
        var bufferPluginOut = new Scanline_rgba16bits[npixels];

        // Same input to both transforms
        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    bufferIn[j].r = FROM_8_TO_16((byte)r);
                    bufferIn[j].g = FROM_8_TO_16((byte)g);
                    bufferIn[j].b = FROM_8_TO_16((byte)b);
                    bufferIn[j].a = 0xffff;

                    j++;
                }
            }
        }

#if NO_THREADS
        DoTransforms(0, npixels, xformRaw, xformPlugin, bufferIn, bufferRawOut, bufferPluginOut);
        var failed = CompareTransforms(0, npixels, bufferRawOut, bufferPluginOut);

#else
        var nThreads = LargestPowerOf2(Environment.ProcessorCount);
        var nPixelsPerThread = npixels / nThreads;

        var tasks = new Task<int>[nThreads];

        int test(object? o)
        {
            var offset = (int)o!;

            using (logger.BeginScope("Range {RangeStart}..{RangeEnd}",
                                     offset * nPixelsPerThread,
                                     (offset + 1) * nPixelsPerThread))
            {
                DoTransforms(offset, nPixelsPerThread, xformRaw, xformPlugin, bufferIn, bufferRawOut, bufferPluginOut);
                return CompareTransforms(offset, nPixelsPerThread, bufferRawOut, bufferPluginOut);
            }
        }

        for (var i = 0; i < nThreads; i++)
        {
            tasks[i] = Task.Factory.StartNew(test, i);
        }

        Task.WaitAll(tasks);

        var threadingFailed = tasks.Select(t => t.IsCompletedSuccessfully).Contains(false);
        var failed = tasks.Sum(s => s.Result);

        if (threadingFailed || failed > 0)
        {
            if (threadingFailed)
            {
                logger.LogWarning("Multithreading failure. Retrying single-threaded");
            }
            else
            {
                logger.LogWarning("{failed} failed. Retyring single-threaded", failed);
            }

            DoTransforms(0, npixels, xformRaw, xformPlugin, bufferIn, bufferRawOut, bufferPluginOut);
            failed = CompareTransforms(0, npixels, bufferIn, bufferPluginOut);
        }

#endif

        cmsDeleteTransform(xformRaw);
        cmsDeleteTransform(xformPlugin);

        if (failed is not 0)
            Fail("{0} failed", failed);

        static void DoTransforms(int offset,
                                 int nPixelsPerThread,
                                 Transform xformRaw,
                                 Transform xformPlugin,
                                 Scanline_rgba16bits[] bufferIn,
                                 Scanline_rgba16bits[] bufferRawOut,
                                 Scanline_rgba16bits[] bufferPluginOut)
        {
            var start = offset * nPixelsPerThread;
            var bIn = bufferIn.AsSpan(start..)[..nPixelsPerThread];
            var bRawOut = bufferRawOut.AsSpan(start..)[..nPixelsPerThread];
            var bPluginOut = bufferPluginOut.AsSpan(start..)[..nPixelsPerThread];

            cmsDoTransform(xformRaw, bIn, bRawOut, (uint)nPixelsPerThread);
            cmsDoTransform(xformPlugin, bIn, bPluginOut, (uint)nPixelsPerThread);
        }

        static int CompareTransforms(int offset,
                                     int nPixelsPerThread,
                                     Scanline_rgba16bits[] bufferRawOut,
                                     Scanline_rgba16bits[] bufferPluginOut)
        {
            // Lets compare results
            var start = offset * nPixelsPerThread;
            var end = (offset + 1) * nPixelsPerThread;

            var failed = 0;
            for (var j = start; j < end; j++)
            {
                if (bufferRawOut[j].r != bufferPluginOut[j].r ||
                    bufferRawOut[j].g != bufferPluginOut[j].g ||
                    bufferRawOut[j].b != bufferPluginOut[j].b ||
                    bufferRawOut[j].a != bufferPluginOut[j].a)
                {
                    if (failed++ is 0)
                    {
                        logger.LogError(
                            "Conversion first failed at ({rRaw} {gRaw} {bRaw} {aRaw}) != ({rPlugin} {gPlugin} {bPlugin} {aPlugin})",
                            bufferRawOut[j].r,
                            bufferRawOut[j].g,
                            bufferRawOut[j].b,
                            bufferRawOut[j].a,
                            bufferPluginOut[j].r,
                            bufferPluginOut[j].g,
                            bufferPluginOut[j].b,
                            bufferPluginOut[j].a);
                    }
                }
            }

            return failed;
        }
    }

    public static void CheckAccuracy16Bits()
    {
        // CLUT should be as 16 bits or better
        using (logger.BeginScope("Checking accuracy of 16 bits on CLUT"))
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
#endif
            TryAllValues16(cmsOpenProfileFromMem(TestProfiles.test5)!,
                           cmsOpenProfileFromMem(TestProfiles.test3)!,
                           INTENT_PERCEPTUAL);
            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }

        trace("All 16 bit tests passed");
    }

    private class sub
    {
        public uint Int { get; set; }

        public float subnormal
        {
            get => BitConverter.UInt32BitsToSingle(Int);
            set => Int = BitConverter.SingleToUInt32Bits(value);
        }
    }

    private static void CheckUncommonValues(Profile profileIn, Profile profileOut, int Intent)
    {
        var sub_pos = new sub();
        var sub_neg = new sub();

        const uint npixels = 100;

        var Plugin = new Context(cmsFastFloatExtensions());

        var xformPlugin =
            cmsCreateTransformTHR(Plugin, profileIn, TYPE_RGB_FLT, profileOut, TYPE_RGB_FLT, (uint)Intent, 0);

        sub_pos.Int = 0x00000002;
        sub_neg.Int = 0x80000002;

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (xformPlugin is null)
            Fail("Null transform");

        var bufferIn = new Scanline_rgbFloat[npixels];
        var bufferPluginOut = new Scanline_rgbFloat[npixels];

        for (var i = 0; i < npixels; i++)
        {
            bufferIn[i].r = (float)((i / 40.0) - 0.5);
            bufferIn[i].g = (float)((i / 20.0) - 0.5);
            bufferIn[i].b = (float)((i / 60.0) - 0.5);
        }

        cmsDoTransform(xformPlugin, bufferIn, bufferPluginOut, npixels);

        bufferIn[0].r = float.NaN;
        bufferIn[0].g = float.NaN;
        bufferIn[0].b = float.NaN;

        bufferIn[1].r = float.PositiveInfinity;
        bufferIn[1].g = float.PositiveInfinity;
        bufferIn[1].b = float.PositiveInfinity;

        bufferIn[2].r = sub_pos.subnormal;
        bufferIn[2].g = sub_pos.subnormal;
        bufferIn[2].b = sub_pos.subnormal;

        bufferIn[3].r = sub_neg.subnormal;
        bufferIn[3].g = sub_neg.subnormal;
        bufferIn[3].b = sub_neg.subnormal;

        cmsDoTransform(xformPlugin, bufferIn, bufferPluginOut, 4);

        cmsDeleteTransform(xformPlugin);

        trace("Passed");
    }

    private static CIELab lab8toLab(ReadOnlySpan<byte> lab8)
    {
        Span<ushort> lab16 = stackalloc ushort[3]
        {
            FROM_8_TO_16(lab8[0]), FROM_8_TO_16(lab8[1]), FROM_8_TO_16(lab8[2])
        };

        return lab16.LabEncodedToFloat();
    }

    private static void CheckToEncodedLab()
    {
        using (logger.BeginScope("Lab encoding"))
        {
            var Plugin = new Context(cmsFastFloatExtensions());
            var Raw = new Context();

            var hsRGB = cmsCreate_sRGBProfile()!;
            var hLab = cmsCreateLab4Profile(null)!;

            var xform_plugin =
                cmsCreateTransformTHR(Plugin, hsRGB, TYPE_RGB_8, hLab, TYPE_Lab_8, INTENT_PERCEPTUAL, 0)!;
            var xform = cmsCreateTransformTHR(Raw, hsRGB, TYPE_RGB_8, hLab, TYPE_Lab_8, INTENT_PERCEPTUAL, 0)!;

            Span<byte> rgb = stackalloc byte[3];
            Span<byte> lab1 = stackalloc byte[3];
            Span<byte> lab2 = stackalloc byte[3];

            var err = 0.0;
            var maxErr = 0.0;

            for (var r = 0; r < 256; r += 5)
            {
                for (var g = 0; g < 256; g += 5)
                {
                    for (var b = 0; b < 256; b += 5)
                    {
                        rgb[0] = (byte)r;
                        rgb[1] = (byte)g;
                        rgb[2] = (byte)b;

                        cmsDoTransform(xform_plugin, rgb, lab1, 1);
                        cmsDoTransform(xform, rgb, lab2, 1);

                        var Lab1 = lab8toLab(lab1);
                        var Lab2 = lab8toLab(lab2);

                        err = DeltaE.De76(Lab1, Lab2);
                        if (err > maxErr)
                            maxErr = err;

                        if (err > 0.1)
                        {
                            trace("Error on lab encoded ({0}, {1}, {2}) <> ({3}, {4}, {5})",
                                  Lab1.L,
                                  Lab1.a,
                                  Lab1.b,
                                  Lab2.L,
                                  Lab2.a,
                                  Lab2.b);
                        }
                    }
                }
            }

            cmsDeleteTransform(xform);
            cmsCloseProfile(hsRGB);
            cmsCloseProfile(hLab);

            if (maxErr > 0.1)
                Fail("Failed");
            else
                trace("Passed");
        }
    }

    private static void CheckToFloatLab()
    {
        using (logger.BeginScope("Float Lab encoding"))
        {
            var Plugin = new Context(cmsFastFloatExtensions());
            var Raw = new Context();

            var hsRGB = cmsCreate_sRGBProfile()!;
            var hLab = cmsCreateLab4Profile(null)!;

            var xform_plugin =
                cmsCreateTransformTHR(Plugin, hsRGB, TYPE_RGB_8, hLab, TYPE_Lab_DBL, INTENT_PERCEPTUAL, 0)!;
            var xform = cmsCreateTransformTHR(Raw, hsRGB, TYPE_RGB_8, hLab, TYPE_Lab_DBL, INTENT_PERCEPTUAL, 0)!;

            Span<byte> rgb = stackalloc byte[3];

            var err = 0.0;
            var maxErr = 0.0;

            for (var r = 0; r < 256; r += 10)
            {
                for (var g = 0; g < 256; g += 10)
                {
                    for (var b = 0; b < 256; b += 10)
                    {
                        rgb[0] = (byte)r;
                        rgb[1] = (byte)g;
                        rgb[2] = (byte)b;

                        cmsDoTransform(xform_plugin, rgb, out CIELab Lab1, 1);
                        cmsDoTransform(xform, rgb, out CIELab Lab2, 1);

                        err = DeltaE.De76(Lab1, Lab2);
                        if (err > maxErr)
                            maxErr = err;

                        if (err > 0.1)
                        {
                            trace("Error on lab encoded ({0}, {1}, {2}) <> ({3}, {4}, {5})",
                                  Lab1.L,
                                  Lab1.a,
                                  Lab1.b,
                                  Lab2.L,
                                  Lab2.a,
                                  Lab2.b);
                        }
                    }
                }
            }

            cmsDeleteTransform(xform);
            cmsCloseProfile(hsRGB);
            cmsCloseProfile(hLab);

            if (maxErr > 0.1)
                Fail("Failed");
            else
                trace("Passed");
        }
    }

    private static bool ValidFloat(float a, float b) =>
        MathF.Abs(a - b) < EPSILON_FLOAT_TESTS;

    private static void TryAllValuesFloat(Profile profileIn, Profile profileOut, int Intent)
    {
        var Raw = new Context();
        var Plugin = new Context(cmsFastFloatExtensions());

        var xformRaw = cmsCreateTransformTHR(Raw,
                                             profileIn,
                                             TYPE_RGB_FLT,
                                             profileOut,
                                             TYPE_RGB_FLT,
                                             (uint)Intent,
                                             cmsFLAGS_NOCACHE);
        var xformPlugin = cmsCreateTransformTHR(Plugin,
                                                profileIn,
                                                TYPE_RGB_FLT,
                                                profileOut,
                                                TYPE_RGB_FLT,
                                                (uint)Intent,
                                                cmsFLAGS_NOCACHE);

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (xformRaw is null || xformPlugin is null)
            Fail("Null transform");

        const int npixels = 256 * 256 * 256;

        var bufferIn = new Scanline_rgbFloat[npixels];
        var bufferRawOut = new Scanline_rgbFloat[npixels];
        var bufferPluginOut = new Scanline_rgbFloat[npixels];

        // Same input to both transforms
        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    bufferIn[j].r = r / 255.0f;
                    bufferIn[j].g = g / 255.0f;
                    bufferIn[j].b = b / 255.0f;

                    j++;
                }
            }
        }

        var nThreads = LargestPowerOf2(Environment.ProcessorCount);
        var nPixelsPerThread = npixels / nThreads;

        var tasks = new Task<int>[nThreads];

        int test(object? o)
        {
            var offset = (int)o!;

            using (logger.BeginScope("Range {RangeStart}..{RangeEnd}",
                                     offset * nPixelsPerThread,
                                     (offset + 1) * nPixelsPerThread))
            {
                DoTransforms(offset, nPixelsPerThread, xformRaw, xformPlugin, bufferIn, bufferRawOut, bufferPluginOut);
                return CompareTransforms(offset, nPixelsPerThread, bufferRawOut, bufferPluginOut);
            }
        }

        for (var i = 0; i < nThreads; i++)
        {
            tasks[i] = Task.Factory.StartNew(test, i);
        }

        Task.WaitAll(tasks);

        var threadingFailed = tasks.Select(t => t.IsCompletedSuccessfully).Contains(false);
        var failed = tasks.Sum(s => s.Result);

        if (threadingFailed || failed > 0)
        {
            if (threadingFailed)
            {
                logger.LogWarning("Multithreading failure. Retrying single-threaded");
            }
            else
            {
                logger.LogWarning("{failed} failed. Retyring single-threaded", failed);
            }

            DoTransforms(0, npixels, xformRaw, xformPlugin, bufferIn, bufferRawOut, bufferPluginOut);
            failed = CompareTransforms(0, npixels, bufferIn, bufferPluginOut);
        }

        cmsDeleteTransform(xformRaw);
        cmsDeleteTransform(xformPlugin);

        if (failed > 0)
            Fail("{0} failed", failed);
        trace("Passed");

        static void DoTransforms(int offset,
                                 int nPixelsPerThread,
                                 Transform xformRaw,
                                 Transform xformPlugin,
                                 Scanline_rgbFloat[] bufferIn,
                                 Scanline_rgbFloat[] bufferRawOut,
                                 Scanline_rgbFloat[] bufferPluginOut)
        {
            var start = offset * nPixelsPerThread;
            var bIn = bufferIn.AsSpan(start..)[..nPixelsPerThread];
            var bRawOut = bufferRawOut.AsSpan(start..)[..nPixelsPerThread];
            var bPluginOut = bufferPluginOut.AsSpan(start..)[..nPixelsPerThread];

            cmsDoTransform(xformRaw, bIn, bRawOut, (uint)nPixelsPerThread);
            cmsDoTransform(xformPlugin, bIn, bPluginOut, (uint)nPixelsPerThread);
        }

        static int CompareTransforms(int offset,
                                     int nPixelsPerThread,
                                     Scanline_rgbFloat[] bufferRawOut,
                                     Scanline_rgbFloat[] bufferPluginOut)
        {
            // Lets compare results
            var start = offset * nPixelsPerThread;
            var end = (offset + 1) * nPixelsPerThread;

            var failed = 0;
            for (var j = start; j < end; j++)
            {
                if (!ValidFloat(bufferRawOut[j].r, bufferPluginOut[j].r) ||
                    !ValidFloat(bufferRawOut[j].g, bufferPluginOut[j].g) ||
                    !ValidFloat(bufferRawOut[j].b, bufferPluginOut[j].b))
                {
                    if (failed++ is 0)
                    {
                        logger.LogError(
                            "Conversion first failed at position [{j}]: ({rRaw} {gRaw} {bRaw}) != ({rPlugin} {gPlugin} {bPlugin})",
                            j,
                            bufferRawOut[j].r,
                            bufferRawOut[j].g,
                            bufferRawOut[j].b,
                            bufferPluginOut[j].r,
                            bufferPluginOut[j].g,
                            bufferPluginOut[j].b);
                    }
                }
            }

            return failed;
        }
    }

    private static void TryAllValuesFloatAlpha(Profile profileIn, Profile profileOut, int Intent, bool copyAlpha)
    {
        var Raw = new Context();
        var Plugin = new Context(cmsFastFloatExtensions());
        var flags = cmsFLAGS_NOCACHE | (copyAlpha ? cmsFLAGS_COPY_ALPHA : 0);

        var xformRaw =
            cmsCreateTransformTHR(Raw, profileIn, TYPE_RGBA_FLT, profileOut, TYPE_RGBA_FLT, (uint)Intent, flags);
        var xformPlugin = cmsCreateTransformTHR(Plugin,
                                                profileIn,
                                                TYPE_RGBA_FLT,
                                                profileOut,
                                                TYPE_RGBA_FLT,
                                                (uint)Intent,
                                                flags);

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (xformRaw is null || xformPlugin is null)
            Fail("Null transform");

        const int npixels = 256 * 256 * 256;

        var bufferIn = new Scanline_rgbaFloat[npixels];
        var bufferRawOut = new Scanline_rgbaFloat[npixels];
        var bufferPluginOut = new Scanline_rgbaFloat[npixels];

        // Same input to both transforms
        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    bufferIn[j].r = r / 255.0f;
                    bufferIn[j].g = g / 255.0f;
                    bufferIn[j].b = b / 255.0f;
                    bufferIn[j].a = 1.0f;

                    j++;
                }
            }
        }

        var nThreads = LargestPowerOf2(Environment.ProcessorCount);
        var nPixelsPerThread = npixels / nThreads;

        var tasks = new Task<int>[nThreads];

        int test(object? o)
        {
            var offset = (int)o!;

            using (logger.BeginScope("Range {RangeStart}..{RangeEnd}",
                                     offset * nPixelsPerThread,
                                     (offset + 1) * nPixelsPerThread))
            {
                DoTransforms(offset, nPixelsPerThread, xformRaw, xformPlugin, bufferIn, bufferRawOut, bufferPluginOut);
                return CompareTransforms(offset, nPixelsPerThread, bufferRawOut, bufferPluginOut);
            }
        }

        for (var i = 0; i < nThreads; i++)
        {
            tasks[i] = Task.Factory.StartNew(test, i);
        }

        Task.WaitAll(tasks);

        var threadingFailed = tasks.Select(t => t.IsCompletedSuccessfully).Contains(false);
        var failed = tasks.Sum(s => s.Result);

        if (threadingFailed || failed > 0)
        {
            if (threadingFailed)
            {
                logger.LogWarning("Multithreading failure. Retrying single-threaded");
            }
            else
            {
                logger.LogWarning("{failed} failed. Retyring single-threaded", failed);
            }

            DoTransforms(0, npixels, xformRaw, xformPlugin, bufferIn, bufferRawOut, bufferPluginOut);
            failed = CompareTransforms(0, npixels, bufferIn, bufferPluginOut);
        }

        cmsDeleteTransform(xformRaw);
        cmsDeleteTransform(xformPlugin);

        if (failed > 0)
            Fail("{0} failed", failed);
        trace("Passed");

        static void DoTransforms(int offset,
                                 int nPixelsPerThread,
                                 Transform xformRaw,
                                 Transform xformPlugin,
                                 Scanline_rgbaFloat[] bufferIn,
                                 Scanline_rgbaFloat[] bufferRawOut,
                                 Scanline_rgbaFloat[] bufferPluginOut)
        {
            var start = offset * nPixelsPerThread;
            var bIn = bufferIn.AsSpan(start..)[..nPixelsPerThread];
            var bRawOut = bufferRawOut.AsSpan(start..)[..nPixelsPerThread];
            var bPluginOut = bufferPluginOut.AsSpan(start..)[..nPixelsPerThread];

            cmsDoTransform(xformRaw, bIn, bRawOut, (uint)nPixelsPerThread);
            cmsDoTransform(xformPlugin, bIn, bPluginOut, (uint)nPixelsPerThread);
        }

        static int CompareTransforms(int offset,
                                     int nPixelsPerThread,
                                     Scanline_rgbaFloat[] bufferRawOut,
                                     Scanline_rgbaFloat[] bufferPluginOut)
        {
            // Lets compare results
            var start = offset * nPixelsPerThread;
            var end = (offset + 1) * nPixelsPerThread;

            var failed = 0;
            for (var j = start; j < end; j++)
            {
                if (!ValidFloat(bufferRawOut[j].r, bufferPluginOut[j].r) ||
                    !ValidFloat(bufferRawOut[j].g, bufferPluginOut[j].g) ||
                    !ValidFloat(bufferRawOut[j].b, bufferPluginOut[j].b) ||
                    !ValidFloat(bufferRawOut[j].a, bufferPluginOut[j].a))
                {
                    if (failed++ is 0)
                    {
                        logger.LogError(
                            "Conversion first failed at position [{j}]: ({rRaw} {gRaw} {bRaw} {aRaw}) != ({rPlugin} {gPlugin} {bPlugin} {aPlugin})",
                            j,
                            bufferRawOut[j].r,
                            bufferRawOut[j].g,
                            bufferRawOut[j].b,
                            bufferRawOut[j].a,
                            bufferPluginOut[j].r,
                            bufferPluginOut[j].g,
                            bufferPluginOut[j].b,
                            bufferPluginOut[j].a);
                    }
                }
            }

            return failed;
        }
    }

    private static bool Valid16Float(ushort a, float b) =>
        MathF.Abs(((float)a / 0xFFFF) - b) < EPSILON_FLOAT_TESTS;

    private static void TryAllValuesFloatVs16(Profile profileIn, Profile profileOut, int Intent)
    {
        using (logger.BeginScope("Check float vs 16 conversions"))
        {
            const int npixelsThreaded = 256 * 256;
            const int npixels = npixelsThreaded * 256;

            var xformRaw = cmsCreateTransform(profileIn,
                                              TYPE_RGB_16,
                                              profileOut,
                                              TYPE_RGB_16,
                                              (uint)Intent,
                                              cmsFLAGS_NOCACHE);
            var xformPlugin = cmsCreateTransform(profileIn,
                                                 TYPE_RGB_FLT,
                                                 profileOut,
                                                 TYPE_RGB_FLT,
                                                 (uint)Intent,
                                                 cmsFLAGS_NOCACHE);

            cmsCloseProfile(profileIn);
            cmsCloseProfile(profileOut);

            if (xformRaw is null || xformPlugin is null)
                Fail("Null transform");

            var bufferIn = new Scanline_rgbFloat[npixels];
            var bufferIn16 = new Scanline_rgb16bits[npixels];
            var bufferFloatOut = new Scanline_rgbFloat[npixels];
            var buffer16Out = new Scanline_rgb16bits[npixels];

            // Fill two equivalent input buffers
            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        bufferIn[j].r = r / 255.0f;
                        bufferIn[j].g = g / 255.0f;
                        bufferIn[j].b = b / 255.0f;

                        bufferIn16[j].r = FROM_8_TO_16((uint)r);
                        bufferIn16[j].g = FROM_8_TO_16((uint)g);
                        bufferIn16[j].b = FROM_8_TO_16((uint)b);

                        j++;
                    }
                }
            }

            var tasks = new Task[256];

            for (var i = 0; i < 256; i++)
            {
                tasks[i] = Task.Factory.StartNew(o =>
                                                 {
                                                     var offset = (int)o!;

                                                     cmsDoTransform(xformRaw,
                                                                    bufferIn16.AsSpan((offset * npixelsThreaded)..),
                                                                    buffer16Out.AsSpan((offset * npixelsThreaded)..),
                                                                    npixelsThreaded);
                                                     cmsDoTransform(xformPlugin,
                                                                    bufferIn.AsSpan((offset * npixelsThreaded)..),
                                                                    bufferFloatOut.AsSpan((offset * npixelsThreaded)..),
                                                                    npixelsThreaded);
                                                 },
                                                 i);
            }

            Task.WaitAll(tasks);

            if (tasks.Select(t => t.IsCompletedSuccessfully).Contains(false))
                Fail("Multithreading failure");

            var failed = 0;

            // Different transforms, different output buffers
            cmsDoTransform(xformRaw, bufferIn16, buffer16Out, npixels);
            cmsDoTransform(xformPlugin, bufferIn, bufferFloatOut, npixels);

            // Lets compare results
            j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        // Check for same values
                        if (!Valid16Float(buffer16Out[j].r, bufferFloatOut[j].r) ||
                            !Valid16Float(buffer16Out[j].g, bufferFloatOut[j].g) ||
                            !Valid16Float(buffer16Out[j].b, bufferFloatOut[j].b))
                        {
                            failed++;
                        }

                        j++;
                    }
                }
            }

            cmsDeleteTransform(xformRaw);
            cmsDeleteTransform(xformPlugin);

            if (failed is not 0)
                Fail("{0} failed", failed);
            trace("Passed");
        }
    }

    public static void CheckChangeFormat()
    {
        var rgb8 = new Scanline_rgb8bits(10, 120, 40);
        var rgb16 = new Scanline_rgb16bits(10 * 257, 120 * 257, 40 * 257);

        using (logger.BeginScope("Checking change format feature"))
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
#endif
            var hsRGB = cmsCreate_sRGBProfile()!;
            var hLab = cmsCreateLab4Profile(null)!;

            var xform = cmsCreateTransform(hsRGB, TYPE_RGB_16, hLab, TYPE_Lab_16, INTENT_PERCEPTUAL, 0)!;

            cmsCloseProfile(hsRGB);
            cmsCloseProfile(hLab);

            cmsDoTransform(xform, rgb16, out Scanline_Lab16bits lab16_1, 1);

            cmsChangeBuffersFormat(xform, TYPE_RGB_8, TYPE_Lab_16);

            cmsDoTransform(xform, rgb8, out Scanline_Lab16bits lab16_2, 1);
            cmsDeleteTransform(xform);

            if (!lab16_1.Equals(lab16_2))
                Fail("Change format failed!");

            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }
    }

    private static bool ValidInt(ushort a, ushort b) =>
        Math.Abs(a - b) <= 32;

    private static void CheckLab2Roundtrip()
    {
        using (logger.BeginScope("Check lab2 roudtrip"))
        {
            const uint npixels = 256 * 256 * 256;

            var hsRGB = cmsCreate_sRGBProfile()!;
            var hLab = cmsCreateLab2Profile(null)!;

            var xform = cmsCreateTransform(hsRGB,
                                           TYPE_RGB_8,
                                           hLab,
                                           TYPE_Lab_8,
                                           INTENT_RELATIVE_COLORIMETRIC,
                                           cmsFLAGS_NOOPTIMIZE | cmsFLAGS_BLACKPOINTCOMPENSATION)!;
            var xform2 = cmsCreateTransform(hLab,
                                            TYPE_Lab_8,
                                            hsRGB,
                                            TYPE_RGB_8,
                                            INTENT_RELATIVE_COLORIMETRIC,
                                            cmsFLAGS_NOOPTIMIZE | cmsFLAGS_BLACKPOINTCOMPENSATION)!;

            cmsCloseProfile(hsRGB);
            cmsCloseProfile(hLab);

            var In = new Scanline_rgb8bits[npixels];
            var Out = new Scanline_rgb8bits[npixels];
            Span<byte> lab = stackalloc byte[(int)npixels * 3];

            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        In[j].r = (byte)r;
                        In[j].g = (byte)g;
                        In[j].b = (byte)b;

                        j++;
                    }
                }
            }

            // Different transforms, different output buffers
            cmsDoTransform(xform, In, lab, npixels);
            cmsDoTransform(xform2, lab, Out, npixels);

            // Lets compare results
            j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        // Check for same values
                        if (!Valid16Float(In[j].r, Out[j].r) ||
                            !Valid16Float(In[j].g, Out[j].g) ||
                            !Valid16Float(In[j].b, Out[j].b))
                        {
                            Fail("Conversion failed at ({0}, {1}, {2}) != ({3}, {4}, {5})",
                                 In[j].r,
                                 In[j].g,
                                 In[j].b,
                                 Out[j].r,
                                 Out[j].g,
                                 Out[j].b);
                        }

                        j++;
                    }
                }
            }

            cmsDeleteTransform(xform);
            cmsDeleteTransform(xform2);

            trace("Passed");
        }
    }

    public static void CheckAlphaDetect()
    {
        using (logger.BeginScope("Check copy alpha with mismatched channels"))
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
#endif
            //var ctx = cmsCreateContext(cmsFastFloatExtensions());
            //cmsSetLogErrorHandlerTHR(ctx, BuildNullLogger());

            //var hsRGB = cmsCreate_sRGBProfileTHR(ctx)!;

            //var xform = cmsCreateTransformTHR(ctx, hsRGB, TYPE_RGB_FLT, hsRGB, TYPE_RGBA_FLT, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA);
            Context.Shared.SetLoggerFactory(BuildNullLogger());

            var hsRGB = cmsCreate_sRGBProfile()!;

            var xform = cmsCreateTransform(hsRGB,
                                           TYPE_RGB_FLT,
                                           hsRGB,
                                           TYPE_RGBA_FLT,
                                           INTENT_PERCEPTUAL,
                                           cmsFLAGS_COPY_ALPHA);
            cmsCloseProfile(hsRGB);

            if (xform is not null)
            {
                Fail("Copy alpha with mismatched channels should not succeed");
            }

            //cmsDeleteContext(ctx);
            trace("Passed");
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }
    }

    public static void CheckConversionFloat()
    {
        using (logger.BeginScope("Check alpha detection"))
            CheckAlphaDetect();

        using (logger.BeginScope("Crash test"))
        {
            using (logger.BeginScope("Part 1"))
                TryAllValuesFloatAlpha(cmsOpenProfileFromMem(TestProfiles.test5)!,
                                       cmsOpenProfileFromMem(TestProfiles.test0)!,
                                       INTENT_PERCEPTUAL,
                                       false);

            using (logger.BeginScope("Part 2"))
                TryAllValuesFloatAlpha(cmsOpenProfileFromMem(TestProfiles.test5)!,
                                       cmsOpenProfileFromMem(TestProfiles.test0)!,
                                       INTENT_PERCEPTUAL,
                                       true);
        }

        using (logger.BeginScope("Crash (II) test"))
        {
            using (logger.BeginScope("Part 1"))
                TryAllValuesFloatAlpha(cmsOpenProfileFromMem(TestProfiles.test0)!,
                                       cmsOpenProfileFromMem(TestProfiles.test0)!,
                                       INTENT_PERCEPTUAL,
                                       false);

            using (logger.BeginScope("Part 2"))
                TryAllValuesFloatAlpha(cmsOpenProfileFromMem(TestProfiles.test0)!,
                                       cmsOpenProfileFromMem(TestProfiles.test0)!,
                                       INTENT_PERCEPTUAL,
                                       true);
        }

        using (logger.BeginScope("Crash (III) test"))
        {
            using (logger.BeginScope("Part 1"))
                CheckUncommonValues(cmsOpenProfileFromMem(TestProfiles.test5)!,
                                    cmsOpenProfileFromMem(TestProfiles.test3)!,
                                    INTENT_PERCEPTUAL);

            using (logger.BeginScope("Part 2"))
                CheckUncommonValues(cmsOpenProfileFromMem(TestProfiles.test5)!,
                                    cmsOpenProfileFromMem(TestProfiles.test0)!,
                                    INTENT_PERCEPTUAL);
        }

        using (logger.BeginScope("Check conversion to Lab"))
        {
            CheckToEncodedLab();
            CheckToFloatLab();
        }

        using (logger.BeginScope("Check accuracy on Matrix-shaper"))
            TryAllValuesFloat(cmsOpenProfileFromMem(TestProfiles.test5)!,
                              cmsOpenProfileFromMem(TestProfiles.test0)!,
                              INTENT_PERCEPTUAL);

        using (logger.BeginScope("Check accuracy of CLUT"))
            TryAllValuesFloatVs16(cmsOpenProfileFromMem(TestProfiles.test5)!,
                                  cmsOpenProfileFromMem(TestProfiles.test3)!,
                                  INTENT_PERCEPTUAL);

        using (logger.BeginScope("Check accuracy on same profile"))
        {
            TryAllValuesFloatVs16(cmsOpenProfileFromMem(TestProfiles.test0)!,
                                  cmsOpenProfileFromMem(TestProfiles.test0)!,
                                  INTENT_PERCEPTUAL);
            TryAllValuesFloat(cmsOpenProfileFromMem(TestProfiles.test0)!,
                              cmsOpenProfileFromMem(TestProfiles.test0)!,
                              INTENT_PERCEPTUAL);
        }
    }

    private static float distance(ReadOnlySpan<float> rgb1, ReadOnlySpan<float> rgb2)
    {
        var dr = rgb2[0] - rgb1[0];
        var dg = rgb2[1] - rgb1[1];
        var db = rgb2[2] - rgb1[2];

        return (dr * dr) + (dg * dg) + (db * db);
    }

    public static void CheckLab2RGB()
    {
        var hLab = cmsCreateLab4Profile(null)!;
        var hRGB = cmsOpenProfileFromMem(TestProfiles.test3)!;
        var noPlugin = new Context();

        var hXformNoPlugin = cmsCreateTransformTHR(noPlugin,
                                                   hLab,
                                                   TYPE_Lab_FLT,
                                                   hRGB,
                                                   TYPE_RGB_FLT,
                                                   INTENT_RELATIVE_COLORIMETRIC,
                                                   cmsFLAGS_NOCACHE)!;
        var hXformPlugin = cmsCreateTransform(hLab,
                                              TYPE_Lab_FLT,
                                              hRGB,
                                              TYPE_RGB_FLT,
                                              INTENT_RELATIVE_COLORIMETRIC,
                                              cmsFLAGS_NOCACHE)!;

        using (logger.BeginScope("Checking Lab -> RGB"))
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
#endif
            var tasks = new Task<float>[2][];
            tasks[0] = new Task<float>[97];
            tasks[1] = new Task<float>[20];

            float test1(object? o)
            {
                var L = (int)o!;

                Span<float> Lab = stackalloc float[3];
                Span<float> RGB = stackalloc float[3];
                Span<float> RGB2 = stackalloc float[3];

                var maxInside = 0f;

                for (var a = -30; a < +30; a++)
                {
                    for (var b = -30; b < +30; b++)
                    {
                        Lab[0] = L;
                        Lab[1] = a;
                        Lab[2] = b;
                        cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                        cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                        var d = distance(RGB, RGB2);
                        if (d > maxInside)
                            maxInside = d;
                    }
                }

                return maxInside;
            }

            for (var i = 0; i < 97; i++)
            {
                tasks[0][i] = Task.Factory.StartNew(test1, i + 4);
            }

            float test2(object? o)
            {
                var L = ((int)o! * 5) + 1;

                Span<float> Lab = stackalloc float[3];
                Span<float> RGB = stackalloc float[3];
                Span<float> RGB2 = stackalloc float[3];

                var maxOutside = 0f;

                for (var a = -100; a < +100; a += 5)
                {
                    for (var b = -100; b < +100; b += 5)
                    {
                        Lab[0] = L;
                        Lab[1] = a;
                        Lab[2] = b;
                        cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                        cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                        var d = distance(RGB, RGB2);
                        if (d > maxOutside)
                            maxOutside = d;
                    }
                }

                return maxOutside;
            }

            for (var i = 0; i < 20; i++)
            {
                tasks[1][i] = Task.Factory.StartNew(test2, i);
            }

            Task.WaitAll(tasks[0]);
            Task.WaitAll(tasks[1]);

            if (tasks[0].Select(t => t.IsCompletedSuccessfully).Contains(false) ||
                tasks[1].Select(t => t.IsCompletedSuccessfully).Contains(false))
            {
                foreach (var t in tasks[0].Where(t => !t.IsCompletedSuccessfully))
                {
                    logger.LogError(t.Exception, "Multithreading failure");
                }

                Thread.Sleep(1000);
                Environment.Exit(1);
            }

            var maxInside = tasks[0].Select(t => t.Result).Max();
            var maxOutside = tasks[1].Select(t => t.Result).Max();

            trace("Max distance: Inside gamut {0:F6}, Outside gamut {1:F6}",
                  MathF.Sqrt(maxInside),
                  MathF.Sqrt(maxOutside));
#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }

        cmsDeleteTransform(hXformNoPlugin);
        cmsDeleteTransform(hXformPlugin);
    }

    public static void CheckSoftProofing()
    {
        using (logger.BeginScope("Check soft proofing and gamut check"))
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
#endif
            var hRGB1 = cmsOpenProfileFromMem(TestProfiles.test5)!;
            var hRGB2 = cmsOpenProfileFromMem(TestProfiles.test3)!;
            var noPlugin = new Context();

            var xformNoPlugin = cmsCreateProofingTransformTHR(noPlugin,
                                                              hRGB1,
                                                              TYPE_RGB_FLT,
                                                              hRGB1,
                                                              TYPE_RGB_FLT,
                                                              hRGB2,
                                                              INTENT_RELATIVE_COLORIMETRIC,
                                                              INTENT_RELATIVE_COLORIMETRIC,
                                                              cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;
            var xformPlugin = cmsCreateProofingTransform(hRGB1,
                                                         TYPE_RGB_FLT,
                                                         hRGB1,
                                                         TYPE_RGB_FLT,
                                                         hRGB2,
                                                         INTENT_RELATIVE_COLORIMETRIC,
                                                         INTENT_RELATIVE_COLORIMETRIC,
                                                         cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;

            cmsCloseProfile(hRGB1);
            cmsCloseProfile(hRGB2);

            const int npixels = 256 * 256 * 256;

            var In = new Scanline_rgbFloat[npixels];
            var Out1 = new Scanline_rgbFloat[npixels];
            var Out2 = new Scanline_rgbFloat[npixels];

            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        In[j].r = r / 255.0f;
                        In[j].g = g / 255.0f;
                        In[j].b = b / 255.0f;
                        j++;
                    }
                }
            }

#if NO_THREADS
            DoTransforms(0, npixels, xformNoPlugin, xformPlugin, In, Out1, Out2);
            var failed = CompareTransforms(0, npixels, Out1, Out2);

#else

            var nThreads = LargestPowerOf2(Environment.ProcessorCount);
            var nPixelsPerThread = npixels / nThreads;

            var tasks = new Task<int>[nThreads];

            int test(object? o)
            {
                var offset = (int)o!;

                using (logger.BeginScope("Range {RangeStart}..{RangeEnd}",
                                         offset * nPixelsPerThread,
                                         (offset + 1) * nPixelsPerThread))
                {
                    DoTransforms(offset, nPixelsPerThread, xformNoPlugin, xformPlugin, In, Out1, Out2);
                    return CompareTransforms(offset, nPixelsPerThread, Out1, Out2);
                }
            }

            for (var i = 0; i < nThreads; i++)
            {
                tasks[i] = Task.Factory.StartNew(test, i);
            }

            Task.WaitAll(tasks);

            var threadingFailed = tasks.Select(t => t.IsCompletedSuccessfully).Contains(false);
            var failed = tasks.Sum(s => s.Result);

            if (threadingFailed || failed > 0)
            {
                if (threadingFailed)
                {
                    logger.LogWarning("Multithreading failure. Retrying single-threaded");
                }
                else
                {
                    logger.LogWarning("{failed} failed. Retrying single-threaded", failed);
                }

                DoTransforms(0, npixels, xformNoPlugin, xformPlugin, In, Out1, Out2);
                failed = CompareTransforms(0, npixels, Out1, Out2);
            }

#endif

            cmsDeleteTransform(xformNoPlugin);
            cmsDeleteTransform(xformPlugin);

            if (failed is not 0)
                Fail("{0} failed", failed);

            trace("Passed");

#if DEBUG
            timer.Stop();
            LogTimer(timer);
#endif
        }

        static void DoTransforms(int offset,
                                 int nPixelsPerThread,
                                 Transform xformNoPlugin,
                                 Transform xformPlugin,
                                 Scanline_rgbFloat[] bufferIn,
                                 Scanline_rgbFloat[] bufferOut1,
                                 Scanline_rgbFloat[] bufferOut2)
        {
            var start = offset * nPixelsPerThread;
            var bIn = bufferIn.AsSpan(start..)[..nPixelsPerThread];
            var bOut1 = bufferOut1.AsSpan(start..)[..nPixelsPerThread];
            var bOut2 = bufferOut2.AsSpan(start..)[..nPixelsPerThread];

            // Different transforms, different output buffers
            cmsDoTransform(xformNoPlugin, bIn, bOut1, (uint)nPixelsPerThread);
            cmsDoTransform(xformPlugin, bIn, bOut2, (uint)nPixelsPerThread);
        }

        static int CompareTransforms(int offset,
                                     int nPixelsPerThread,
                                     Scanline_rgbFloat[] bufferOut1,
                                     Scanline_rgbFloat[] bufferOut2)
        {
            // Let's compare results
            var start = offset * nPixelsPerThread;
            var end = (offset + 1) * nPixelsPerThread;

            var failed = 0;
            for (var j = start; j < end; j++)
            {
                // Check for same values
                if (!ValidFloat(bufferOut1[j].r, bufferOut2[j].r) ||
                    !ValidFloat(bufferOut1[j].g, bufferOut2[j].g) ||
                    !ValidFloat(bufferOut1[j].b, bufferOut2[j].b))
                {
                    if (failed++ is 0)
                    {
                        logger.LogError(
                            "Conversion first failed at position [{j}]: ({r1} {g1} {b1}) != ({r2} {g2} {b2})",
                            j,
                            bufferOut1[j].r,
                            bufferOut1[j].g,
                            bufferOut1[j].b,
                            bufferOut2[j].r,
                            bufferOut2[j].g,
                            bufferOut2[j].b);
                    }
                }
            }

            return failed;
        }
    }

    public static void CheckPremultiplied()
    {
        ReadOnlySpan<byte> BGRA8 = [ 255, 192, 160, 128 ];
        Span<byte> bgrA8_1 = stackalloc byte[4];
        Span<byte> bgrA8_2 = stackalloc byte[4];

        var srgb1 = cmsCreate_sRGBProfile();
        var srgb2 = cmsCreate_sRGBProfile();

        var noPlugin = new Context();

        var xform1 = cmsCreateTransformTHR(noPlugin,
                                           srgb1,
                                           TYPE_BGRA_8,
                                           srgb2,
                                           TYPE_BGRA_8_PREMUL,
                                           INTENT_PERCEPTUAL,
                                           cmsFLAGS_COPY_ALPHA);
        var xform2 = cmsCreateTransform(srgb1,
                                        TYPE_BGRA_8,
                                        srgb2,
                                        TYPE_BGRA_8_PREMUL,
                                        INTENT_PERCEPTUAL,
                                        cmsFLAGS_COPY_ALPHA);

        cmsCloseProfile(srgb1);
        cmsCloseProfile(srgb2);

        cmsDoTransform(xform1, BGRA8, bgrA8_1, 1);
        cmsDoTransform(xform2, BGRA8, bgrA8_2, 1);

        cmsDeleteTransform(xform1);
        cmsDeleteTransform(xform2);

        for (var i = 0; i < 4; i++)
        {
            if (bgrA8_1[i] != bgrA8_2[i])
            {
                Fail("Premultiplied failed at ({0} {1} {2}) != ({3} {4} {5})",
                     bgrA8_1[0],
                     bgrA8_1[1],
                     bgrA8_1[2],
                     bgrA8_2[0],
                     bgrA8_2[1],
                     bgrA8_2[2]);
            }
        }
    }
}
