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

using lcms2.state;
using lcms2.types;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace lcms2.FastFloatPlugin.testbed;
internal static partial class Testbed
{
    private static void ComparativeCt(Context? ct1, Context? ct2, string Title, string lbl1, string lbl2, perf_fn fn1, perf_fn fn2, Memory<byte> inICC, Memory<byte> outICC)
    {
        using (logger.BeginScope(Title))
        {
            var profileIn = inICC.IsEmpty ? CreateCurves() : cmsOpenProfileFromMem(inICC)!;
            var profileOut = outICC.IsEmpty ? CreateCurves() : cmsOpenProfileFromMem(outICC)!;

            var n1 = MPixSec(fn1(ct1, profileIn, profileOut).TotalMilliseconds);

            profileIn = inICC.IsEmpty ? CreateCurves() : cmsOpenProfileFromMem(inICC)!;
            profileOut = outICC.IsEmpty ? CreateCurves() : cmsOpenProfileFromMem(outICC)!;

            var n2 = MPixSec(fn2(ct2, profileIn, profileOut).TotalMilliseconds);

            trace("({2}) {0:F2} MPixel/sec. \t({3}) {1:F2} MPixel/sec.", n1, n2, lbl1, lbl2);
        }
    }

    private static void Comparative(string Title, string lbl1, string lbl2, perf_fn fn1, perf_fn fn2, Memory<byte> inICC, Memory<byte> outICC) =>
        ComparativeCt(null, null, Title, lbl1, lbl2, fn1, fn2, inICC, outICC);

    private static TimeSpan SpeedTest8bitsRGB(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_RGB_8, profileOut, TYPE_RGB_8, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_rgb8bits[Mb];

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

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private static TimeSpan SpeedTest8bitsRGBA(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_RGBA_8, profileOut, TYPE_RGBA_8, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_rgba8bits[Mb];

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
                    In[j].a = 0;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private static TimeSpan SpeedTest15bitsRGB(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_RGB_15, profileOut, TYPE_RGB_15, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_rgb15bits[Mb];

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In[j].r = (ushort)r;
                    In[j].g = (ushort)g;
                    In[j].b = (ushort)b;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private static TimeSpan SpeedTest15bitsRGBA(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_RGBA_15, profileOut, TYPE_RGBA_15, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_rgba15bits[Mb];

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In[j].r = (ushort)r;
                    In[j].g = (ushort)g;
                    In[j].b = (ushort)b;
                    In[j].a = 0;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private static TimeSpan SpeedTest15bitsCMYK(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_CMYK_15, profileOut, TYPE_CMYK_15, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_cmyk15bits[Mb];

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In[j].c = (ushort)r;
                    In[j].m = (ushort)g;
                    In[j].y = (ushort)b;
                    In[j].k = 0;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private static TimeSpan SpeedTest16bitsRGB(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_RGB_16, profileOut, TYPE_RGB_16, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_rgb16bits[Mb];

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In[j].r = FROM_8_TO_16((uint)r);
                    In[j].g = FROM_8_TO_16((uint)g);
                    In[j].b = FROM_8_TO_16((uint)b);
                                
                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private static TimeSpan SpeedTest16bitsCMYK(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_CMYK_16, profileOut, TYPE_CMYK_16, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_cmyk16bits[Mb];

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In[j].c = (ushort)r;
                    In[j].m = (ushort)g;
                    In[j].y = (ushort)b;
                    In[j].k = (ushort)r;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    public static void SpeedTest8()
    {
        var size_rgb8bits = Unsafe.SizeOf<Scanline_rgb8bits>();

        var noPlugin = cmsCreateContext();
        Thread.Sleep(10);
        Console.WriteLine();

        TimeSpan t0, t1, t2, t3;

        using (logger.BeginScope("8 bit performance"))
        {
            using (logger.BeginScope("Default"))
            {
                trace("P E R F O R M A N C E   T E S T S   8 B I T S  (D E F A U L T)");

                t0 = Performance("8 bits on CLUT profiles", SpeedTest8bitsRGB, noPlugin, TestProfiles.test5, TestProfiles.test3, size_rgb8bits, default);
                t1 = Performance("8 bits on Matrix-Shaper", SpeedTest8bitsRGB, noPlugin, TestProfiles.test5, TestProfiles.test0, size_rgb8bits, default);
                t2 = Performance("8 bits on same Matrix-Shaper", SpeedTest8bitsRGB, noPlugin, TestProfiles.test0, TestProfiles.test0, size_rgb8bits, default);
                t3 = Performance("8 bits on curves", SpeedTest8bitsRGB, noPlugin, "*curves", "*curves", size_rgb8bits, default);
            }

            Thread.Sleep(10);
            Console.WriteLine();

            // Note that context null has the plug-in installed

            using (logger.BeginScope("Plugin"))
            {
                trace("P E R F O R M A N C E   T E S T S   8 B I T S  (P L U G I N)");

                Performance("8 bits on CLUT profiles", SpeedTest8bitsRGB, null, TestProfiles.test5, TestProfiles.test3, size_rgb8bits, t0);
                Performance("8 bits on Matrix-Shaper", SpeedTest8bitsRGB, null, TestProfiles.test5, TestProfiles.test0, size_rgb8bits, t1);
                Performance("8 bits on same Matrix-Shaper", SpeedTest8bitsRGB, null, TestProfiles.test0, TestProfiles.test0, size_rgb8bits, t2);
                Performance("8 bits on curves", SpeedTest8bitsRGB, null, "*curves", "*curves", size_rgb8bits, t3);
            }
        }

        cmsDeleteContext(noPlugin);
    }

    public static void SpeedTest15()
    {
        var size_rgb15bits = Unsafe.SizeOf<Scanline_rgb15bits>();
        var size_cmyk15bits = Unsafe.SizeOf<Scanline_cmyk15bits>();

        var noPlugin = cmsCreateContext();
        Thread.Sleep(10);
        Console.WriteLine();

        using (logger.BeginScope("15 bit performance"))
        {
            using (logger.BeginScope("Plugin"))
            {
                trace("P E R F O R M A N C E   T E S T S   1 5  B I T S  (P L U G I N)");

                Performance("15 bits on CLUT profiles", SpeedTest15bitsRGB, null, TestProfiles.test5, TestProfiles.test3, size_rgb15bits, default);
                Performance("15 bits on Matrix-Shaper profiles", SpeedTest15bitsRGB, null, TestProfiles.test5, TestProfiles.test0, size_rgb15bits, default);
                Performance("15 bits on same Matrix-Shaper", SpeedTest15bitsRGB, null, TestProfiles.test0, TestProfiles.test0, size_rgb15bits, default);
                Performance("15 bits on curves", SpeedTest15bitsRGB, null, "*curves", "*curves", size_rgb15bits, default);
                Performance("15 bits on CMYK CLUT profiles", SpeedTest15bitsCMYK, null, TestProfiles.test1, TestProfiles.test2, size_cmyk15bits, default);
            }
        }

        cmsDeleteContext(noPlugin);
    }

    public static void SpeedTest16()
    {
        var size_rgb16bits = Unsafe.SizeOf<Scanline_rgb16bits>();
        var size_cmyk16bits = Unsafe.SizeOf<Scanline_cmyk16bits>();

        var noPlugin = cmsCreateContext();
        Thread.Sleep(10);
        Console.WriteLine();

        TimeSpan t0, t1, t2, t3, t4;

        using (logger.BeginScope("16 bit performance"))
        {
            using (logger.BeginScope("Default"))
            {
                trace("P E R F O R M A N C E   T E S T S   1 6  B I T S  (D E F A U L T)");

                t0 = Performance("16 bits on CLUT profiles",          SpeedTest16bitsRGB,  noPlugin, TestProfiles.test5, TestProfiles.test3, size_rgb16bits,  default);
                t1 = Performance("16 bits on Matrix-Shaper profiles", SpeedTest16bitsRGB,  noPlugin, TestProfiles.test5, TestProfiles.test0, size_rgb16bits,  default);
                t2 = Performance("16 bits on same Matrix-Shaper",     SpeedTest16bitsRGB,  noPlugin, TestProfiles.test0, TestProfiles.test0, size_rgb16bits,  default);
                t3 = Performance("16 bits on curves",                 SpeedTest16bitsRGB,  noPlugin, "*curves",     "*curves",   size_rgb16bits,  default);
                t4 = Performance("16 bits on CMYK CLUT profiles",     SpeedTest16bitsCMYK, noPlugin, TestProfiles.test1, TestProfiles.test2, size_cmyk16bits, default);
            }

            Thread.Sleep(10);
            Console.WriteLine();

            // Note that context null has the plug-in installed

            using (logger.BeginScope("Plugin"))
            {
                trace("P E R F O R M A N C E   T E S T S   1 6  B I T S  (P L U G I N)");

                Performance("16 bits on CLUT profiles",          SpeedTest16bitsRGB,  null, TestProfiles.test5, TestProfiles.test3, size_rgb16bits,  t0);
                Performance("16 bits on Matrix-Shaper profiles", SpeedTest16bitsRGB,  null, TestProfiles.test5, TestProfiles.test0, size_rgb16bits,  t1);
                Performance("16 bits on same Matrix-Shaper",     SpeedTest16bitsRGB,  null, TestProfiles.test0, TestProfiles.test0, size_rgb16bits,  t2);
                Performance("16 bits on curves",                 SpeedTest16bitsRGB,  null, "*curves",     "*curves",   size_rgb16bits,  t3);
                Performance("16 bits on CMYK CLUT profiles",     SpeedTest16bitsCMYK, null, TestProfiles.test1, TestProfiles.test2, size_cmyk16bits, t4);
            }
        }

        cmsDeleteContext(noPlugin);
    }

    private static TimeSpan SpeedTestFloatRGB(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var inFormatter = 0u;
        switch ((uint)cmsGetColorSpace(profileIn))
        {
            case cmsSigRgbData: inFormatter = TYPE_RGB_FLT; break;
            case cmsSigLabData: inFormatter = TYPE_Lab_FLT; break;

            default:
                Fail("Invalid colorspace");
                break;
        }

        var outFormatter = 0u;
        switch ((uint)cmsGetColorSpace(profileOut))
        {
            case cmsSigRgbData: outFormatter = TYPE_RGB_FLT; break;
            case cmsSigLabData: outFormatter = TYPE_Lab_FLT; break;
            case cmsSigXYZData: outFormatter = TYPE_XYZ_FLT; break;

            default:
                Fail("Invalid colorspace");
                break;
        }

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, inFormatter, profileOut, outFormatter, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        if (inFormatter == TYPE_RGB_FLT)
        {
            var Mb = 256 * 256 * 256;
            var fill = new Scanline_rgbFloat[Mb];

            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        fill[j].r = r / 255.0f;
                        fill[j].g = g / 255.0f;
                        fill[j].b = b / 255.0f;

                        j++;
                    }
                }
            }

            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, fill, fill, (uint)Mb);

            atime.Stop();

            cmsDeleteTransform(lcmsxform);

            return atime.Elapsed;
        }
        else
        {
            var Mb = 100 * 256 * 256;
            var fill = new Scanline_LabFloat[Mb];

            var j = 0;
            for (var L = 0; L < 100; L++)
            {
                for (var a = -127.0f; a < 127.0f; a++)
                {
                    for (var b = -127.0f; b < 127.0f; b++)
                    {
                        fill[j].L = L;
                        fill[j].a = a;
                        fill[j].b = b;

                        j++;
                    }
                }
            }

            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, fill, fill, (uint)Mb);

            atime.Stop();

            cmsDeleteTransform(lcmsxform);

            return atime.Elapsed;
        }
    }

    private static TimeSpan SpeedTestFloatCMYK(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_CMYK_FLT, profileOut, TYPE_CMYK_FLT, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 64 * 64 * 64 * 64;
        var In = new Scanline_cmykFloat[Mb];

        var j = 0;
        for (var c = 0; c < 256; c += 4)
        {
            for (var m = 0; m < 256; m += 4)
            {
                for (var y = 0; y < 256; y += 4)
                {
                    for (var k = 0; k < 256; k += 4)
                    {
                        In[j].c = (ushort)c;
                        In[j].m = (ushort)m;
                        In[j].y = (ushort)y;
                        In[j].k = (ushort)k;

                        j++;
                    }
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, In, In, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private static TimeSpan SpeedTestFloatLab(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        if ((uint)cmsGetColorSpace(profileIn) is not cmsSigLabData)
            Fail("Invalid colorspace");

        var outFormatter = 0u;
        switch ((uint)cmsGetColorSpace(profileOut))
        {
            case cmsSigRgbData: outFormatter = TYPE_RGB_FLT; break;
            case cmsSigLabData: outFormatter = TYPE_Lab_FLT; break;
            case cmsSigXYZData: outFormatter = TYPE_XYZ_FLT; break;

            default:
                Fail("Invalid colorspace");
                break;
        }

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_Lab_FLT, profileOut, outFormatter, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 100 * 256 * 256;
        var fill = new Scanline_LabFloat[Mb];

        var j = 0;
        for (var L = 0; L < 100; L++)
        {
            for (var a = -127.0f; a < 127.0f; a++)
            {
                for (var b = -127.0f; b < 127.0f; b++)
                {
                    fill[j].L = L;
                    fill[j].a = a;
                    fill[j].b = b;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransform(lcmsxform, fill, fill, (uint)Mb);

        atime.Stop();

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    public static void SpeedTestFloat()
    {
        var size_rgbFloat = Unsafe.SizeOf<Scanline_rgbFloat>();
        var size_cmykFloat = Unsafe.SizeOf<Scanline_cmykFloat>();
        var size_LabFloat = Unsafe.SizeOf<Scanline_LabFloat>();

        var noPlugin = cmsCreateContext();
        Thread.Sleep(10);
        Console.WriteLine();

        TimeSpan t0, t1, t2, t3, t4, t5, t6, t7;

        using (logger.BeginScope("Floating point performance"))
        {
            using (logger.BeginScope("Default"))
            {
                trace("P E R F O R M A N C E   T E S T S   F L O A T  (D E F A U L T)");

                t0 = Performance("Floating point on CLUT profiles",          SpeedTestFloatRGB,  noPlugin, TestProfiles.test5, TestProfiles.test3, size_rgbFloat, default);
                t1 = Performance("Floating point on Matrix-Shaper profiles", SpeedTestFloatRGB,  noPlugin, TestProfiles.test5, TestProfiles.test0, size_rgbFloat, default);
                t2 = Performance("Floating point on same Matrix-Shaper",     SpeedTestFloatRGB,  noPlugin, TestProfiles.test0, TestProfiles.test0, size_rgbFloat, default);
                t3 = Performance("Floating point on curves",                 SpeedTestFloatRGB,  noPlugin, "*curves", "*curves", size_rgbFloat, default);
                t4 = Performance("Floating point on RGB->Lab",               SpeedTestFloatRGB,  noPlugin, TestProfiles.test5, "*lab", size_rgbFloat, default);
                t5 = Performance("Floating point on RGB->XYZ",               SpeedTestFloatRGB,  noPlugin, TestProfiles.test3, "*xyz", size_rgbFloat, default);
                t6 = Performance("Floating point on CMYK->CMYK",             SpeedTestFloatCMYK, noPlugin, TestProfiles.test1, TestProfiles.test2, size_cmykFloat, default);
                t7 = Performance("Floating point on Lab->RGB",               SpeedTestFloatLab,  noPlugin, "*lab", TestProfiles.test3, size_LabFloat, default);
            }

            Thread.Sleep(10);
            Console.WriteLine();

            // Note that context null has the plug-in installed

            using (logger.BeginScope("Plugin"))
            {
                trace("P E R F O R M A N C E   T E S T S  F L O A T  (P L U G I N)");

                Performance("Floating point on CLUT profiles",          SpeedTestFloatRGB,  null, TestProfiles.test5, TestProfiles.test3, size_rgbFloat, t0);
                Performance("Floating point on Matrix-Shaper profiles", SpeedTestFloatRGB,  null, TestProfiles.test5, TestProfiles.test0, size_rgbFloat, t1);
                Performance("Floating point on same Matrix-Shaper",     SpeedTestFloatRGB,  null, TestProfiles.test0, TestProfiles.test0, size_rgbFloat, t2);
                Performance("Floating point on curves",                 SpeedTestFloatRGB,  null, "*curves",     "*curves", size_rgbFloat, t3);
                Performance("Floating point on RGB->Lab",               SpeedTestFloatRGB,  null, TestProfiles.test5, "*lab", size_rgbFloat, t4);
                Performance("Floating point on RGB->XYZ",               SpeedTestFloatRGB,  null, TestProfiles.test3, "*xyz", size_rgbFloat, t5);
                Performance("Floating point on CMYK->CMYK",             SpeedTestFloatCMYK, null, TestProfiles.test1, TestProfiles.test2, size_cmykFloat, t6);
                Performance("Floating point on Lab->RGB",               SpeedTestFloatLab,  null, "*lab",        TestProfiles.test3, size_LabFloat, t7);
            }
        }

        cmsDeleteContext(noPlugin);
    }

    private static TimeSpan SpeedTestFloatByUsing16BitsRGB(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var xform16 = cmsCreateTransform(profileIn, TYPE_RGB_16, profileOut, TYPE_RGB_16, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = 256 * 256 * 256;
        var In = new Scanline_rgbFloat[Mb];
        var tmp16 = new Scanline_rgb16bits[Mb];

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

        var atime = Stopwatch.StartNew();

        for (j = 0; j < 256 * 256 * 256; j++)
        {
            tmp16[j].r = (ushort)Math.Floor(In[j].r * 65535.0 + 0.5);
            tmp16[j].g = (ushort)Math.Floor(In[j].g * 65535.0 + 0.5);
            tmp16[j].b = (ushort)Math.Floor(In[j].b * 65535.0 + 0.5);

            j++; // TODO: why is this in the original?
        }

        cmsDoTransform(xform16, tmp16, tmp16, (uint)Mb);

        for (j = 0; j < 256 * 256 * 256; j++)
        {
            In[j].r = (float)(tmp16[j].r / 65535.0);
            In[j].g = (float)(tmp16[j].g / 65535.0);
            In[j].b = (float)(tmp16[j].b / 65535.0);

            j++; // TODO: why is this in the original?
        }

        atime.Stop();

        cmsDeleteTransform(xform16);

        return atime.Elapsed;
    }

    public static void ComparativeFloatVs16bits()
    {
        Thread.Sleep(10);
        Console.WriteLine();

        using (logger.BeginScope("Performance comparison"))
        {
            trace("C O M P A R A T I V E  converting to 16 bit vs. using float plug-in.\nvalues given in MegaPixels per second.");

            Comparative("Floating point on CLUT profiles",          "16 bit", "float", SpeedTestFloatByUsing16BitsRGB, SpeedTestFloatRGB, TestProfiles.test5, TestProfiles.test3);
            Comparative("Floating point on Matrix-Shaper profiles", "16 bit", "float", SpeedTestFloatByUsing16BitsRGB, SpeedTestFloatRGB, TestProfiles.test5, TestProfiles.test0);
            Comparative("Floating point on same Matrix-Shaper",     "16 bit", "float", SpeedTestFloatByUsing16BitsRGB, SpeedTestFloatRGB, TestProfiles.test0, TestProfiles.test0);
            Comparative("Floating point on curves",                 "16 bit", "float", SpeedTestFloatByUsing16BitsRGB, SpeedTestFloatRGB, default, default);
        }
    }

    private unsafe struct line
    {
        private fixed byte _pixels[256 * 256 * 4];
        private fixed byte padding[4];

        public Scanline_rgba8bits* pixels(int a, int b)
        {
            fixed (void* ptr = _pixels)
            return &((Scanline_rgba8bits*)ptr)[a * 256 + b];
        }
    }

    private unsafe struct big_bitmap
    {
        private fixed byte _line[256*(256*256*4+4)];

        public line* line(int i)
        {
            fixed (void* ptr = _line)
                return &((line*)ptr)[i];
        }
    }

    private unsafe static TimeSpan SpeedTest8bitDoTransform(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_RGBA_8, profileOut, TYPE_RGBA_8, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = Unsafe.SizeOf<big_bitmap>();

        var In = (big_bitmap*)NativeMemory.Alloc((nuint)Mb);
        var Out = (big_bitmap*)NativeMemory.Alloc((nuint)Mb);

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In->line(r)->pixels(g, b)->r = (byte)r;
                    In->line(r)->pixels(g, b)->g = (byte)g;
                    In->line(r)->pixels(g, b)->b = (byte)b;
                    In->line(r)->pixels(g, b)->a = 0;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        for (j = 0; j < 256; j++)
            cmsDoTransform(lcmsxform, new Span<Scanline_rgba8bits>(In->line(j)->pixels(0, 0),256*256), new Span<Scanline_rgba8bits>(Out->line(j)->pixels(0, 0), 256 * 256), 256*256);

        atime.Stop();

        NativeMemory.Free(In);
        NativeMemory.Free(Out);

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    private unsafe static TimeSpan SpeedTest8bitLineStride(Context? ct, Profile profileIn, Profile profileOut)
    {
        if (profileIn is null || profileOut is null)
            Fail("Unable to open profiles");

        var lcmsxform = cmsCreateTransformTHR(ct, profileIn, TYPE_RGBA_8, profileOut, TYPE_RGBA_8, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        var Mb = Unsafe.SizeOf<big_bitmap>();

        var In = (big_bitmap*)NativeMemory.Alloc((nuint)Mb);
        var Out = (big_bitmap*)NativeMemory.Alloc((nuint)Mb);

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In->line(r)->pixels(g, b)->r = (byte)r;
                    In->line(r)->pixels(g, b)->g = (byte)g;
                    In->line(r)->pixels(g, b)->b = (byte)b;
                    In->line(r)->pixels(g, b)->a = 0;

                    j++;
                }
            }
        }

        var atime = Stopwatch.StartNew();

        cmsDoTransformLineStride(lcmsxform, new Span<byte>(In, sizeof(big_bitmap)), new Span<byte>(Out, sizeof(big_bitmap)), 256 * 256, 256, (uint)sizeof(line), (uint)sizeof(line), 0, 0);

        atime.Stop();

        NativeMemory.Free(In);
        NativeMemory.Free(Out);

        cmsDeleteTransform(lcmsxform);

        return atime.Elapsed;
    }

    public static void ComparativeLineStride8bits()
    {
        var NoPlugin = cmsCreateContext();
        var Plugin = cmsCreateContext(cmsFastFloatExtensions());

        Thread.Sleep(10);
        Console.WriteLine();

        using (logger.BeginScope("cmsDoTransform vs cmsDoTransformLineStride"))
        {
            trace("C O M P A R A T I V E cmsDoTransform() vs. cmsDoTransformLineStride()\nvalues given in MegaPixels per second.");

            ComparativeCt(NoPlugin, Plugin, "CLUT profiles", "cmsDoTransform", "cmsDoTransformLineStride", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, TestProfiles.test5, TestProfiles.test3);
            ComparativeCt(NoPlugin, Plugin, "CLUT 16 bits", "cmsDoTransform", "cmsDoTransformLineStride", SpeedTest16bitsRGB, SpeedTest16bitsRGB, TestProfiles.test5, TestProfiles.test3);
            ComparativeCt(NoPlugin, Plugin, "Matrix-Shaper", "cmsDoTransform", "cmsDoTransformLineStride", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, TestProfiles.test5, TestProfiles.test0);
            ComparativeCt(NoPlugin, Plugin, "same Matrix-Shaper", "cmsDoTransform", "cmsDoTransformLineStride", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, TestProfiles.test0, TestProfiles.test0);
            ComparativeCt(NoPlugin, Plugin, "curves", "cmsDoTransform", "cmsDoTransformLineStride", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, default, default);
        }

        cmsDeleteContext(NoPlugin);
        cmsDeleteContext(Plugin);
    }

    public static void TestGrayTransformPerformance()
    {
        using (logger.BeginScope("Gray conversion using two gray profiles"))
        {
            var gamma18 = cmsBuildGamma(null, 1.8)!;
            var gamma22 = cmsBuildGamma(null, 2.2)!;

            var profileIn = cmsCreateGrayProfile(null, gamma18)!;
            var profileOut = cmsCreateGrayProfile(null, gamma22)!;

            cmsFreeToneCurve(gamma18);
            cmsFreeToneCurve(gamma22);

            var lcmsxform = cmsCreateTransform(profileIn, TYPE_GRAY_FLT | EXTRA_SH(1), profileOut, TYPE_GRAY_FLT | EXTRA_SH(1), INTENT_PERCEPTUAL, 0)!;
            cmsCloseProfile(profileIn);
            cmsCloseProfile(profileOut);

            var pixels = 256 * 256 * 256;
            var Mb = pixels * 2;
            var In = new float[Mb];

            for (var j = 0; j < Mb; j++)
                In[j] = j % 256 / 255.0f;

            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, (uint)pixels);

            atime.Stop();

            cmsDeleteTransform(lcmsxform);

            trace("{0:F2} MPixels/Sec.", MPixSec(atime.Elapsed.TotalMilliseconds));
        }
    }

    public static void TestGrayTransformPerformance1()
    {
        using (logger.BeginScope("Gray conversion using two devicelinks"))
        {
            var gamma18 = new ToneCurve[] { cmsBuildGamma(null, 1.8)! };
            var gamma22 = new ToneCurve[] { cmsBuildGamma(null, 2.2)! };

            var profileIn = cmsCreateLinearizationDeviceLink(cmsSigGrayData, gamma18)!;
            var profileOut = cmsCreateLinearizationDeviceLink(cmsSigGrayData, gamma22)!;

            cmsFreeToneCurve(gamma18[0]);
            cmsFreeToneCurve(gamma22[0]);

            var lcmsxform = cmsCreateTransform(profileIn, TYPE_GRAY_FLT, profileOut, TYPE_GRAY_FLT, INTENT_PERCEPTUAL, 0)!;
            cmsCloseProfile(profileIn);
            cmsCloseProfile(profileOut);

            var pixels = 256 * 256 * 256;
            var Mb = pixels;
            var In = new float[Mb];

            for (var j = 0; j < pixels; j++)
                In[j] = j % 256 / 255.0f;

            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, (uint)pixels);

            atime.Stop();

            cmsDeleteTransform(lcmsxform);

            trace("{0:F2} MPixels/Sec.", MPixSec(atime.Elapsed.TotalMilliseconds));
        }
    }
}
