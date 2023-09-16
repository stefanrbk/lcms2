﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright (c) 1998-2022 Marti Maria Saguer, all rights reserved
//  Copyright (c) 2022-2023 Stefan Kewatt, all rights reserved
//
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
using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

using System;

namespace lcms2.FastFloatPlugin.testbed;
internal static partial class Testbed
{
    private struct Scanline_rgb8bits(byte r, byte g, byte b) { public byte r = r, g = g, b = b; }

    private struct Scanline_rgba8bits(byte r, byte g, byte b, byte a) { public byte r = r, g = g, b = b, a = a; }

    private struct Scanline_cmyk8bits(byte c, byte m, byte y, byte k) { public byte c = c, m = m, y = y, k = k; }

    private struct Scanline_rgb16bits(ushort r, ushort g, ushort b) { public ushort r = r, g = g, b = b; }

    private struct Scanline_rgba16bits(ushort r, ushort g, ushort b, ushort a) { public ushort r = r, g = g, b = b, a = a; }

    private struct Scanline_cmyk16bits(ushort c, ushort m, ushort y, ushort k) { public ushort c = c, m = m, y = y, k = k; }

    private struct Scanline_Lab16bits(ushort L, ushort a, ushort b) { public ushort L = L, a = a, b = b; }

    private struct Scanline_rgb15bits(ushort r, ushort g, ushort b) { public ushort r = r, g = g, b = b; }

    private struct Scanline_rgba15bits(ushort r, ushort g, ushort b, ushort a) { public ushort r = r, g = g, b = b, a = a; }

    private struct Scanline_cmyk15bits(ushort c, ushort m, ushort y, ushort k) { public ushort c = c, m = m, y = y, k = k; }

    private struct Scanline_rgbFloat(float r, float g, float b) { public float r = r, g = g, b = b; }

    private struct Scanline_rgbaFloat(float r, float g, float b, float a) { public float r = r, g = g, b = b, a = a; }

    private struct Scanline_cmykFloat(float c, float m, float y, float k) { public float c = c, m = m, y = y, k = k; }

    private struct Scanline_LabFloat(float L, float a, float b) { public float L = L, a = a, b = b; }

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

    private static bool checkSingleComputeIncrements(uint Format, uint planeStride, uint ExpectedChannels, uint ExpectedAlpha, params uint[] args)
    {
        Span<uint> ComponentStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> ComponentPointerIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(Format, planeStride, out var nChannels, out var nAlpha, ComponentStartingOrder, ComponentPointerIncrements);

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

            for (var i = 0; i < 256; i++)
            {
                var n = FROM_8_TO_15((byte)i);
                var m = FROM_15_TO_8(n);

                if (m != i)
                    Fail("Failed on {0} (->{1}->{2})", i, n, m);
            }

            trace("Passed");
        }
    }

    private static void TryAllValues15(Profile profileIn, Profile profileOut, int Intent)
    {
        var xform15 = cmsCreateTransform(profileIn, TYPE_RGB_15, profileOut, TYPE_RGB_15, (uint)Intent, cmsFLAGS_NOCACHE);
        var xform8 = cmsCreateTransform(profileIn, TYPE_RGB_8, profileOut, TYPE_RGB_8, (uint)Intent, cmsFLAGS_NOCACHE);

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (xform15 is null || xform8 is null)
            Fail("Null transforms on check for 15 bit conversions");

        const int npixelsThreaded = 256 * 256;
        const int npixels = npixelsThreaded * 256;  // All RGB cube in 8 bits
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

        var tasks = new Task[256];

        for (var i = 0; i < 256; i++)
        {
            tasks[i] = Task.Factory.StartNew(o =>
            {
                var offset = (int)o!;

                cmsDoTransform(xform15, buffer15in.AsSpan((offset * npixelsThreaded)..), buffer15out.AsSpan((offset * npixelsThreaded)..), npixelsThreaded);
                cmsDoTransform(xform8, buffer8in.AsSpan((offset * npixelsThreaded)..), buffer8out.AsSpan((offset * npixelsThreaded)..), npixelsThreaded);
            }, i);
        }

        Task.WaitAll(tasks);

        if (tasks.Select(t => t.IsCompletedSuccessfully).Contains(false))
            Fail("Multithreading failure");

        var failed = 0;
        j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    // Check the results
                    if (!Valid15(buffer15out[j].r, buffer8out[j].r) ||
                        !Valid15(buffer15out[j].g, buffer8out[j].g) ||
                        !Valid15(buffer15out[j].b, buffer8out[j].b))
                    {
                        failed++;
                    }

                    j++;
                }
            }
        }
        if (failed is not 0)
            Fail("{0} failed", failed);

        cmsDeleteTransform(xform15);
        cmsDeleteTransform(xform8);
    }

    private static void TryAllValues15NonThreaded(Profile profileIn, Profile profileOut, int Intent)
    {
        var xform15 = cmsCreateTransform(profileIn, TYPE_RGB_15, profileOut, TYPE_RGB_15, (uint)Intent, cmsFLAGS_NOCACHE);
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
                for ( var b = 0; b < 256; b++)
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

        cmsDoTransform(xform15, buffer15in, buffer15out, npixels);
        cmsDoTransform(xform8, buffer8in, buffer8out, npixels);

        j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    // Check the results
                    if (!Valid15(buffer15out[j].r, buffer8out[j].r) ||
                        !Valid15(buffer15out[j].g, buffer8out[j].g) ||
                        !Valid15(buffer15out[j].b, buffer8out[j].b))
                    {
                        Fail("Conversion failed at ({0} {1} {2}) != ({3} {4} {5})", buffer8out[j].r, buffer8out[j].g, buffer8out[j].b,
                            FROM_15_TO_8(buffer15out[j].r), FROM_15_TO_8(buffer15out[j].g), FROM_15_TO_8(buffer15out[j].b));
                    }

                    j++;
                }
            }
        }

        cmsDeleteTransform(xform15);
        cmsDeleteTransform(xform8);
    }

    public static void Check15bitsConversion()
    {
        Check15bitMacros();

        using (logger.BeginScope("Checking accuracy of 15 bits on CLUT"))
        {
            TryAllValues15(cmsOpenProfileFromMem(TestProfiles.test5)!, cmsOpenProfileFromMem(TestProfiles.test3)!, INTENT_PERCEPTUAL);
            trace("Passed");
        }

        using (logger.BeginScope("Checking accuracy of 15 bits on same profile"))
        {
            TryAllValues15(cmsOpenProfileFromMem(TestProfiles.test0)!, cmsOpenProfileFromMem(TestProfiles.test0)!, INTENT_PERCEPTUAL);
            trace("Passed");
        }

        using (logger.BeginScope("Checking accuracy of 15 bits on Matrix"))
        {
            TryAllValues15(cmsOpenProfileFromMem(TestProfiles.test5)!, cmsOpenProfileFromMem(TestProfiles.test0)!, INTENT_PERCEPTUAL);
            trace("Passed");
        }

        trace("All 15 bit tests passed");
    }

    private static void TryAllValues16(Profile profileIn, Profile profileOut, int Intent)
    {
        var Raw = cmsCreateContext();
        var Plugin = cmsCreateContext(cmsFastFloatExtensions(), null);

        var xformRaw = cmsCreateTransformTHR(Raw, profileIn, TYPE_RGBA_16, profileOut, TYPE_RGBA_16, (uint)Intent, cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);
        var xformPlugin = cmsCreateTransformTHR(Plugin, profileIn, TYPE_RGBA_16, profileOut, TYPE_RGBA_16, (uint)Intent, cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (xformRaw is null || xformPlugin is null)
            Fail("Null transforms on check for float conversions");

        const int npixelsThreaded = 256 * 256;
        const int npixels = npixelsThreaded * 256;  // All RGB cube in 8 bits
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

        var tasks = new Task[256];

        for (var i = 0; i < 256; i++)
        {
            tasks[i] = Task.Factory.StartNew(o =>
            {
                var offset = (int)o!;

                // Different transforms, different output buffers
                cmsDoTransform(xformRaw, bufferIn.AsSpan((offset * npixelsThreaded)..), bufferRawOut.AsSpan((offset * npixelsThreaded)..), npixelsThreaded);
                cmsDoTransform(xformPlugin, bufferIn.AsSpan((offset * npixelsThreaded)..), bufferPluginOut.AsSpan((offset * npixelsThreaded)..), npixelsThreaded);
            }, i);
        }

        Task.WaitAll(tasks);

        if (tasks.Select(t => t.IsCompletedSuccessfully).Contains(false))
            Fail("Multithreading failure");

        // Lets compare results
        var failed = 0;
        j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    if (bufferRawOut[j].r != bufferPluginOut[j].r ||
                        bufferRawOut[j].g != bufferPluginOut[j].g ||
                        bufferRawOut[j].b != bufferPluginOut[j].b ||
                        bufferRawOut[j].a != bufferPluginOut[j].a)
                    {
                        failed++;
                    }

                    j++;
                }
            }
        }
        if (failed is not 0)
            Fail("{0} failed", failed);

        cmsDeleteTransform(xformRaw);
        cmsDeleteTransform(xformPlugin);

        cmsDeleteContext(Plugin);
        cmsDeleteContext(Raw);
    }

    private static void TryAllValues16NonThreaded(Profile profileIn, Profile profileOut, int Intent)
    {
        var Raw = cmsCreateContext();
        var Plugin = cmsCreateContext(cmsFastFloatExtensions(), null);

        var xformRaw = cmsCreateTransformTHR(Raw, profileIn, TYPE_RGBA_16, profileOut, TYPE_RGBA_16, (uint)Intent, cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);
        var xformPlugin = cmsCreateTransformTHR(Plugin, profileIn, TYPE_RGBA_16, profileOut, TYPE_RGBA_16, (uint)Intent, cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);

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

        // Different transforms, different output buffers
        cmsDoTransform(xformRaw, bufferIn, bufferRawOut, npixels);
        cmsDoTransform(xformPlugin, bufferIn, bufferPluginOut, npixels);

        // Lets compare results
        j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    if (bufferRawOut[j].r != bufferPluginOut[j].r ||
                        bufferRawOut[j].g != bufferPluginOut[j].g ||
                        bufferRawOut[j].b != bufferPluginOut[j].b ||
                        bufferRawOut[j].a != bufferPluginOut[j].a)
                    {
                        Fail("Conversion failed at [{0} {1} {2} {3}] ({4} {5} {6} {7}) != ({8} {9} {10} {11})",
                            bufferIn[j].r, bufferIn[j].g, bufferIn[j].b, bufferIn[j].a,
                            bufferRawOut[j].r, bufferRawOut[j].g, bufferRawOut[j].b, bufferRawOut[j].a,
                            bufferPluginOut[j].r, bufferPluginOut[j].g, bufferPluginOut[j].b, bufferPluginOut[j].a);
                    }

                    j++;
                }
            }
        }

        cmsDeleteTransform(xformRaw);
        cmsDeleteTransform(xformPlugin);

        cmsDeleteContext(Plugin);
        cmsDeleteContext(Raw);
    }

    public static void CheckAccuracy16Bits()
    {
        // CLUT should be as 16 bits or better
        using (logger.BeginScope("Checking accuracy of 16 bits on CLUT"))
        {
            TryAllValues16/*NonThreaded*/(cmsOpenProfileFromMem(TestProfiles.test5)!, cmsOpenProfileFromMem(TestProfiles.test3)!, INTENT_PERCEPTUAL);
            trace("Passed");
        }

        trace("All 16 bit tests passed");
    }

    // CheckUncommonValues

    // lab8toLab

    // CheckToEncodedLab

    // CheckToFloatLab

    private static bool ValidFloat(float a, float b) =>
        MathF.Abs(a - b) < EPSILON_FLOAT_TESTS;

    // TryAllValuesFloat

    // TryAllValuesFloatAlpha

    // Valid16Float

    // TryAllValuesFloatVs16

    public static void CheckChangeFormat()
    {
        var rgb8 = new Scanline_rgb8bits(10, 120, 40);
        var rgb16 = new Scanline_rgb16bits(10 * 257, 120 * 257, 40 * 257);

        using (logger.BeginScope("Checking change format feature"))
        {
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
        }
    }

    // ValidInt

    // CheckLab2Roundtrip

    // CheckConversionFloat

    // ValidFloat2

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
        var noPlugin = cmsCreateContext();

        var hXformNoPlugin = cmsCreateTransformTHR(noPlugin, hLab, TYPE_Lab_FLT, hRGB, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_NOCACHE)!;
        var hXformPlugin = cmsCreateTransform(hLab, TYPE_Lab_FLT, hRGB, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_NOCACHE)!;

        using (logger.BeginScope("Checking Lab -> RGB"))
        {
            var tasks = new Task<float>[2][];
            tasks[0] = new Task<float>[97];
            tasks[1] = new Task<float>[20];
            for (var i = 0; i < 97; i++)
            {
                tasks[0][i] = Task.Factory.StartNew(o =>
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
                            Lab[0] = L; Lab[1] = a; Lab[2] = b;
                            cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                            cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                            var d = distance(RGB, RGB2);
                            if (d > maxInside)
                                maxInside = d;
                        }
                    }

                    return maxInside;
                }, i+4);
            }

            for (var i = 0; i < 20; i++)
            {
                tasks[1][i] = Task.Factory.StartNew(o =>
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
                            Lab[0] = L; Lab[1] = a; Lab[2] = b;
                            cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                            cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                            var d = distance(RGB, RGB2);
                            if (d > maxOutside)
                                maxOutside = d;
                        }
                    }

                    return maxOutside;
                }, i);
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

            trace("Max distance: Inside gamut {0}, Outside gamut {1}", MathF.Sqrt(maxInside), MathF.Sqrt(maxOutside));
        }

        cmsDeleteTransform(hXformNoPlugin);
        cmsDeleteTransform(hXformPlugin);

        cmsDeleteContext(noPlugin);
    }

    public static void CheckLab2RGBNonThreaded()
    {
        var hLab = cmsCreateLab4Profile(null)!;
        var hRGB = cmsOpenProfileFromMem(TestProfiles.test3)!;
        var noPlugin = cmsCreateContext();

        var hXformNoPlugin = cmsCreateTransformTHR(noPlugin, hLab, TYPE_Lab_FLT, hRGB, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_NOCACHE)!;
        var hXformPlugin = cmsCreateTransform(hLab, TYPE_Lab_FLT, hRGB, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_NOCACHE)!;

        Span<float> Lab = stackalloc float[3];
        Span<float> RGB = stackalloc float[3];
        Span<float> RGB2 = stackalloc float[3];

        var maxInside = 0f;
        var maxOutside = 0f;

        using (logger.BeginScope("Checking Lab -> RGB"))
        {
            for (var L = 4; L <= 100; L++)
            {
                for (var a = -30; a < +30; a++)
                {
                    for (var b = -30; b < +30; b++)
                    {
                        Lab[0] = L; Lab[1] = a; Lab[2] = b;
                        cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                        cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                        var d = distance(RGB, RGB2);
                        if (d > maxInside)
                            maxInside = d;
                    }
                }
            }

            for (var L = 1; L <= 100; L += 5)
            {
                for (var a = -100; a < +100; a += 5)
                {
                    for (var b = -100; b < +100; b += 5)
                    {
                        Lab[0] = L; Lab[1] = a; Lab[2] = b;
                        cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                        cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                        var d = distance(RGB, RGB2);
                        if (d > maxOutside)
                            maxOutside = d;
                    }
                }
            }

            trace("Max distance: Inside gamut {0}, Outside gamut {1}", MathF.Sqrt(maxInside), MathF.Sqrt(maxOutside));
        }

        cmsDeleteTransform(hXformNoPlugin);
        cmsDeleteTransform(hXformPlugin);

        cmsDeleteContext(noPlugin);
    }

    public static void CheckSoftProofing()
    {
        using (logger.BeginScope("Check soft proofing and gamut check"))
        {
            var hRGB1 = cmsOpenProfileFromMem(TestProfiles.test5)!;
            var hRGB2 = cmsOpenProfileFromMem(TestProfiles.test3)!;
            var noPlugin = cmsCreateContext();

            var xformNoPlugin = cmsCreateProofingTransformTHR(noPlugin, hRGB1, TYPE_RGB_FLT, hRGB1, TYPE_RGB_FLT, hRGB2, INTENT_RELATIVE_COLORIMETRIC, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;
            var xformPlugin = cmsCreateProofingTransform(hRGB1, TYPE_RGB_FLT, hRGB1, TYPE_RGB_FLT, hRGB2, INTENT_RELATIVE_COLORIMETRIC, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;

            cmsCloseProfile(hRGB1);
            cmsCloseProfile(hRGB2);

            const int npixelsThreaded = 256 * 256;
            const int Mb = npixelsThreaded * 256;
            var In = new Scanline_rgbFloat[Mb];
            var Out1 = new Scanline_rgbFloat[Mb];
            var Out2 = new Scanline_rgbFloat[Mb];

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

            var tasks = new Task[256];

            for (var i = 0; i < 256; i++)
            {
                tasks[i] = Task.Factory.StartNew(o =>
                {
                    var offset = (int)o!;

                    // Different transforms, different output buffers
                    cmsDoTransform(xformNoPlugin, In.AsSpan((offset * npixelsThreaded)..), Out1.AsSpan((offset * npixelsThreaded)..), npixelsThreaded);
                    cmsDoTransform(xformPlugin, In.AsSpan((offset * npixelsThreaded)..), Out2.AsSpan((offset * npixelsThreaded)..), npixelsThreaded);
                }, i);
            }

            Task.WaitAll(tasks);

            if (tasks.Select(t => t.IsCompletedSuccessfully).Contains(false))
                Fail("Multithreading failure");

            var failed = 0;
            j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        // Check for same values
                        if (!ValidFloat(Out1[j].r, Out2[j].r) ||
                            !ValidFloat(Out1[j].g, Out2[j].g) ||
                            !ValidFloat(Out1[j].b, Out2[j].b))
                        {
                            failed++;
                        }
                        j++;
                    }
                }
            }
            if (failed is not 0)
                Fail("{0} failed", failed);

            cmsDeleteTransform(xformNoPlugin);
            cmsDeleteTransform(xformPlugin);

            cmsDeleteContext(noPlugin);

            trace("Passed");
        }
    }

    public static void CheckSoftProofingNonThreaded()
    {
        using (logger.BeginScope("Check soft proofing and gamut check"))
        {
            var hRGB1 = cmsOpenProfileFromMem(TestProfiles.test5)!;
            var hRGB2 = cmsOpenProfileFromMem(TestProfiles.test3)!;
            var noPlugin = cmsCreateContext();

            var xformNoPlugin = cmsCreateProofingTransformTHR(noPlugin, hRGB1, TYPE_RGB_FLT, hRGB1, TYPE_RGB_FLT, hRGB2, INTENT_RELATIVE_COLORIMETRIC, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;
            var xformPlugin = cmsCreateProofingTransform(hRGB1, TYPE_RGB_FLT, hRGB1, TYPE_RGB_FLT, hRGB2, INTENT_RELATIVE_COLORIMETRIC, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;

            cmsCloseProfile(hRGB1);
            cmsCloseProfile(hRGB2);

            var Mb = 256 * 256 * 256;
            var In = new Scanline_rgbFloat[Mb];
            var Out1 = new Scanline_rgbFloat[Mb];
            var Out2 = new Scanline_rgbFloat[Mb];

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

            cmsDoTransform(xformNoPlugin, In, Out1, (uint)Mb);
            cmsDoTransform(xformPlugin, In, Out2, (uint)Mb);

            j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        // Check for same values
                        if (!ValidFloat(Out1[j].r, Out2[j].r) ||
                            !ValidFloat(Out1[j].g, Out2[j].g) ||
                            !ValidFloat(Out1[j].b, Out2[j].b))
                        {
                            Fail("Conversion failed at ({0} {1} {2}) != ({3} {4} {5})",
                                Out1[j].r, Out1[j].g, Out1[j].b,
                                Out2[j].r, Out2[j].g, Out2[j].b);
                        }
                        j++;
                    }
                }
            }

            cmsDeleteTransform(xformNoPlugin);
            cmsDeleteTransform(xformPlugin);

            cmsDeleteContext(noPlugin);

            trace("Passed");
        }
    }
}
