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

using lcms2.types;

using Microsoft.Extensions.Logging;

using System.Buffers;
using System.Diagnostics;

namespace lcms2.testbed;

internal static partial class Testbed
{
    private record struct Scanline_rgba8(byte r, byte g, byte b, byte a);
    private record struct Scanline_rgba16(ushort r, ushort g, ushort b, ushort a);
    private record struct Scanline_rgba32(float r, float g, float b, float a);
    private record struct Scanline_rgb8(byte r, byte g, byte b);
    private record struct Scanline_rgb16(ushort r, ushort g, ushort b);
    private record struct Scanline_rgb32(float r, float g, float b);

    private static string TitlePerformance(string Txt) =>
        $"{Txt,-45}";

    private static void PrintPerformance(uint Bytes, uint SizeOfPixel, double diff)
    {
        var seconds = diff / 1000;
        var mpix_sec = Bytes / (1024 * 1024 * seconds * SizeOfPixel);

        logger.LogInformation("{pps:g3} MPixel/sec.", mpix_sec);
    }

    private static void SpeedTest32bits(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut, int Intent)
    {
        const int Interval = 2;

        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_RGBA_FLT, lcmsProfileOut, TYPE_RGBA_FLT, (uint)Intent, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 / Interval * 256 / Interval * 256 / Interval;
            var Mb = (uint)NumPixels * 16;
            var In = ArrayPool<Scanline_rgba32>.Shared.Rent(NumPixels);

            var j = 0;
            for (var r = 0; r < 256; r += Interval)
            {
                for (var g = 0; g < 256; g += Interval)
                {
                    for (var b = 0; b < 256; b += Interval)
                    {
                        In[j].r = r / 256f;
                        In[j].g = g / 256f;
                        In[j].b = b / 256f;
                        In[j].a = (In[j].r + In[j].g + In[j].b) / 3;

                        j++;
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<Scanline_rgba32>.Shared.Return(In);

            PrintPerformance(Mb, 16, diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest16bits(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut, int Intent)
    {
        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_RGB_16, lcmsProfileOut, TYPE_RGB_16, (uint)Intent, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 * 256 * 256;
            var Mb = (uint)NumPixels * 6;

            var In = ArrayPool<Scanline_rgb16>.Shared.Rent(NumPixels);

            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        In[j].r = (ushort)((r << 8) | r);
                        In[j].g = (ushort)((g << 8) | g);
                        In[j].b = (ushort)((b << 8) | b);

                        j++;
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<Scanline_rgb16>.Shared.Return(In);

            PrintPerformance(Mb, 6, diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest32bitsCMYK(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut)
    {
        const int Interval = 2;

        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_CMYK_FLT, lcmsProfileOut, TYPE_CMYK_FLT, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 / Interval * 256 / Interval * 256 / Interval;
            var Mb = (uint)NumPixels * 16;

            var In = ArrayPool<Scanline_rgba32>.Shared.Rent(NumPixels);

            var j = 0;
            for (var r = 0; r < 256; r += Interval)
            {
                for (var g = 0; g < 256; g += Interval)
                {
                    for (var b = 0; b < 256; b += Interval)
                    {
                        In[j].r = r / 256f;
                        In[j].g = g / 256f;
                        In[j].b = b / 256f;
                        In[j].a = (In[j].r + In[j].g + In[j].b) / 3;

                        j++;
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<Scanline_rgba32>.Shared.Return(In);

            PrintPerformance(Mb, 16, diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest16bitsCMYK(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut)
    {
        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_CMYK_16, lcmsProfileOut, TYPE_CMYK_16, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 * 256 * 256;
            var Mb = (uint)NumPixels * 8;

            var In = ArrayPool<Scanline_rgba16>.Shared.Rent(NumPixels);

            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        In[j].r = (ushort)((r << 8) | r);
                        In[j].g = (ushort)((g << 8) | g);
                        In[j].b = (ushort)((b << 8) | b);

                        j++;
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<Scanline_rgba16>.Shared.Return(In);

            PrintPerformance(Mb, 8, diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest8bits(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut, int Intent)
    {
        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_RGB_8, lcmsProfileOut, TYPE_RGB_8, (uint)Intent, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 * 256 * 256;
            var Mb = (uint)NumPixels * 3;

            var In = ArrayPool<Scanline_rgb8>.Shared.Rent(NumPixels);

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

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<Scanline_rgb8>.Shared.Return(In);

            PrintPerformance(Mb, 3, diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest8bitsCMYK(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut)
    {
        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_CMYK_8, lcmsProfileOut, TYPE_CMYK_8, INTENT_PERCEPTUAL, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 * 256 * 256;
            var Mb = (uint)NumPixels * 4;

            var In = ArrayPool<Scanline_rgba8>.Shared.Rent(NumPixels);

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

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<Scanline_rgba8>.Shared.Return(In);

            PrintPerformance(Mb, 4, diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest32bitsGray(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut, int Intent)
    {
        const int Interval = 2;

        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_GRAY_FLT, lcmsProfileOut, TYPE_GRAY_FLT, (uint)Intent, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 / Interval * 256 / Interval * 256 / Interval;
            var Mb = (uint)NumPixels * sizeof(float);

            var In = ArrayPool<float>.Shared.Rent(NumPixels);

            var j = 0;
            for (var r = 0; r < 256; r += Interval)
            {
                for (var g = 0; g < 256; g += Interval)
                {
                    for (var b = 0; b < 256; b += Interval)
                    {
                        In[j] = (r + g + b) / 768f;

                        j++;
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<float>.Shared.Return(In);

            PrintPerformance(Mb, sizeof(float), diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest16bitsGray(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut, int Intent)
    {
        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_GRAY_16, lcmsProfileOut, TYPE_GRAY_16, (uint)Intent, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 * 256 * 256;
            var Mb = (uint)NumPixels * sizeof(ushort);

            var In = ArrayPool<ushort>.Shared.Rent(NumPixels);

            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        In[j] = (ushort)((r + g + b) / 3);

                        j++;
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<ushort>.Shared.Return(In);

            PrintPerformance(Mb, sizeof(ushort), diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static void SpeedTest8bitsGray(string Title, Profile lcmsProfileIn, Profile lcmsProfileOut, int Intent)
    {
        using (logger.BeginScope(TitlePerformance(Title)))
        {
            if (lcmsProfileIn is null || lcmsProfileOut is null)
                Die("Unable to open profiles");

            var lcmsxform = cmsCreateTransformTHR(DbgThread(), lcmsProfileIn, TYPE_GRAY_8, lcmsProfileOut, TYPE_GRAY_8, (uint)Intent, cmsFLAGS_NOCACHE);

            const int NumPixels = 256 * 256 * 256;
            var Mb = (uint)NumPixels * sizeof(byte);

            var In = ArrayPool<byte>.Shared.Rent(NumPixels);

            var j = 0;
            for (var r = 0; r < 256; r++)
            {
                for (var g = 0; g < 256; g++)
                {
                    for (var b = 0; b < 256; b++)
                    {
                        In[j] = (byte)r;

                        j++;
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            var atime = Stopwatch.StartNew();

            cmsDoTransform(lcmsxform, In, In, NumPixels);

            atime.Stop();
            var diff = atime.ElapsedMilliseconds;

            ArrayPool<byte>.Shared.Return(In);

            PrintPerformance(Mb, sizeof(byte), diff);
            cmsDeleteTransform(lcmsxform);
        }
    }

    private static Profile CreateCurves()
    {
        var Gamma = cmsBuildGamma(DbgThread(), 1.1)!;
        var Transfer = new ToneCurve[3];

        Transfer[0] = Transfer[1] = Transfer[2] = Gamma;
        var h = cmsCreateLinearizationDeviceLink(Signature.Colorspace.Rgb, Transfer);

        cmsFreeToneCurve(Gamma);

        return h;
    }

    public static void SpeedTest()
    {
        Console.WriteLine();
        Console.WriteLine();

        using (logger.BeginScope("Speed Test"))
        {
            logger.LogInformation("P E R F O R M A N C E   T E S T S");
            logger.LogInformation("=================================");

            Thread.Sleep(10);
            Console.WriteLine();

            var test1 = OpenBuiltInProfile(TestProfiles.test1);
            var test2 = OpenBuiltInProfile(TestProfiles.test2);
            var test3 = OpenBuiltInProfile(TestProfiles.test3);
            var test5 = OpenBuiltInProfile(TestProfiles.test5);

            SpeedTest8bits("8 bits on CLUT profiles", test5, test3, INTENT_PERCEPTUAL);

            SpeedTest16bits("16 bits on CLUT profiles", test5, test3, INTENT_PERCEPTUAL);

            SpeedTest32bits("32 bits on CLUT profiles", test5, test3, INTENT_PERCEPTUAL);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            var aRGBlcms2 = cmsOpenProfileFromFile("aRGBlcms2.icc", "r")!;

            SpeedTest8bits("8 bits on Matrix-Shaper profiles", test5, aRGBlcms2, INTENT_PERCEPTUAL);

            SpeedTest16bits("16 bits on Matrix-Shaper profiles", test5, aRGBlcms2, INTENT_PERCEPTUAL);

            SpeedTest32bits("32 bits on Matrix-Shaper profiles", test5, aRGBlcms2, INTENT_PERCEPTUAL);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            SpeedTest8bits("8 bits on SAME Matrix-Shaper profiles", test5, test5, INTENT_PERCEPTUAL);

            SpeedTest16bits("16 bits on SAME Matrix-Shaper profiles", aRGBlcms2, aRGBlcms2, INTENT_PERCEPTUAL);

            SpeedTest32bits("32 bits on SAME Matrix-Shaper profiles", aRGBlcms2, aRGBlcms2, INTENT_PERCEPTUAL);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            SpeedTest8bits("8 bits on Matrix-Shaper profiles (AbsCol)", test5, aRGBlcms2, INTENT_ABSOLUTE_COLORIMETRIC);

            SpeedTest16bits("16 bits on Matrix-Shaper profiles (AbsCol)", test5, aRGBlcms2, INTENT_ABSOLUTE_COLORIMETRIC);

            SpeedTest32bits("32 bits on Matrix-Shaper profiles (AbsCol)", test5, aRGBlcms2, INTENT_ABSOLUTE_COLORIMETRIC);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            SpeedTest8bits("8 bits on curves", CreateCurves(), CreateCurves(), INTENT_PERCEPTUAL);

            SpeedTest16bits("16 bits on curves", CreateCurves(), CreateCurves(), INTENT_PERCEPTUAL);

            SpeedTest32bits("32 bits on curves", CreateCurves(), CreateCurves(), INTENT_PERCEPTUAL);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            SpeedTest8bitsCMYK("8 bits on CMYK profiles", test1, test2);

            SpeedTest16bitsCMYK("16 bits on CMYK profiles", test1, test2);

            SpeedTest32bitsCMYK("32 bits on CMYK profiles", test1, test2);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            var graylcms2 = cmsOpenProfileFromFile("graylcms2.icc", "r")!;
            var gray3lcms2 = cmsOpenProfileFromFile("gray3lcms2.icc", "r")!;

            SpeedTest8bitsGray("8 bits on gray to gray", gray3lcms2, graylcms2, INTENT_RELATIVE_COLORIMETRIC);

            SpeedTest16bitsGray("16 bits on gray to gray", gray3lcms2, graylcms2, INTENT_RELATIVE_COLORIMETRIC);

            SpeedTest32bitsGray("32 bits on gray to gray", gray3lcms2, graylcms2, INTENT_RELATIVE_COLORIMETRIC);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            var glablcms2 = cmsOpenProfileFromFile("glablcms2.icc", "r")!;

            SpeedTest8bitsGray("8 bits on gray to lab gray", graylcms2, glablcms2, INTENT_RELATIVE_COLORIMETRIC);

            SpeedTest16bitsGray("16 bits on gray to lab gray", graylcms2, glablcms2, INTENT_RELATIVE_COLORIMETRIC);

            SpeedTest32bitsGray("32 bits on gray to lab gray", graylcms2, glablcms2, INTENT_RELATIVE_COLORIMETRIC);

            Console.WriteLine();

            // - - - - - - - - - - - - - - - - - - - - - - - - - - -

            SpeedTest8bitsGray("8 bits on SAME gray to gray", graylcms2, graylcms2, INTENT_RELATIVE_COLORIMETRIC);

            SpeedTest16bitsGray("16 bits on SAME gray to gray", graylcms2, graylcms2, INTENT_RELATIVE_COLORIMETRIC);

            SpeedTest32bitsGray("32 bits on SAME gray to gray", graylcms2, graylcms2, INTENT_RELATIVE_COLORIMETRIC);

            Console.WriteLine();

            cmsCloseProfile(glablcms2);
            cmsCloseProfile(gray3lcms2);
            cmsCloseProfile(graylcms2);
            cmsCloseProfile(aRGBlcms2);
            cmsCloseProfile(test5);
            cmsCloseProfile(test3);
            cmsCloseProfile(test2);
            cmsCloseProfile(test1);
        }
    }

    private static Profile OpenBuiltInProfile(byte[] data) =>
        cmsOpenProfileFromMem(data, (uint)data.Length)!;
}
