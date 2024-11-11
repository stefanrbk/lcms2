//---------------------------------------------------------------------------------
//
//  Little Color Management System, multithread extensions
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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace lcms2.ThreadedPlugin.testbed;
internal static partial class Testbed
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double MPixSec(double seconds) =>
        (256.0 * 256.0 * 256.0) / (1024.0 * 1024.0 * seconds);

    private delegate double perf_fn(Context? ct, Profile ProfileIn, Profile ProfileOut);

    private static Profile? loadProfile(string name)
    {
        if (name.StartsWith("*"))
        {
            if (name.CompareTo("*lab") is 0)
                return cmsCreateLab4Profile(null);
            if (name.CompareTo("*xyz") is 0)
                return cmsCreateXYZProfile();
            if (name.CompareTo("*curves") is 0)
                return CreateCurves();
            Fail("Unknown builtin '{0}'", name);
        }

        if (name.StartsWith("test"))
        {
            if (name.CompareTo("test0") is 0)
                return cmsOpenProfileFromMem(plugins.TestProfiles.test0);
            if (name.CompareTo("test1") is 0)
                return cmsOpenProfileFromMem(plugins.TestProfiles.test1);
            if (name.CompareTo("test2") is 0)
                return cmsOpenProfileFromMem(plugins.TestProfiles.test2);
            if (name.CompareTo("test3") is 0)
                return cmsOpenProfileFromMem(plugins.TestProfiles.test3);
            if (name.CompareTo("test5") is 0)
                return cmsOpenProfileFromMem(plugins.TestProfiles.test5);
            Fail("Unknown builtin '{0}'", name);
        }

        return cmsOpenProfileFromFile(name, "r");
    }

    private static double Performance(string Title, perf_fn fn, Context? ct, string inICC, string outICC, nint sz, double prev)
    {
        var ProfileIn = loadProfile(inICC)!;
        var ProfileOut = loadProfile(outICC)!;

        var n = fn(ct, ProfileIn, ProfileOut);

        if (prev > 0.0)
        {
            trace("{0}:\n" +
                "{1:F2} MPixel/sec.\n" +
                "{2:F2} MByte/sec.\n" +
                "Improvement of (x {3:F1})",
                Title, n, n * sz, n / prev);
        }
        else
        {
            trace("{0}:\n" +
                "{1:F2} MPixel/sec.\n" +
                "{2:F2} MByte/sec.",
                Title, n, n * sz);
        }

        fflush();
        return n;
    }

    private static void ComparativeCt(Context? ct1, Context? ct2, string Title, perf_fn fn1, perf_fn fn2, string inICC, string outICC)
    {
        var ProfileIn = string.IsNullOrEmpty(inICC)
            ? CreateCurves()!
            : loadProfile(inICC)!;
        var ProfileOut = string.IsNullOrEmpty(outICC)
            ? CreateCurves()!
            : loadProfile(outICC)!;

        var n1 = fn1(ct1, ProfileIn, ProfileOut);

        ProfileIn = string.IsNullOrEmpty(inICC)
            ? CreateCurves()!
            : loadProfile(inICC)!;
        ProfileOut = string.IsNullOrEmpty(outICC)
            ? CreateCurves()!
            : loadProfile(outICC)!;

        var n2 = fn2(ct2, ProfileIn, ProfileOut);

        trace("{0}:\n" +
            "cmsDoTransform()           {1:F2} MPixel/sec.\n" +
            "cmsDoTransformLineStride() {2:F2} MPixel/sec.",
            Title, n1, n2);
    }

    private static void Comparative(string Title, perf_fn fn1, perf_fn fn2, string inICC, string outICC) =>
        ComparativeCt(null, null, Title, fn1, fn2, inICC, outICC);

    private static double SpeedTest8bitsRGB(Context? ct, Profile? ProfileIn, Profile? ProfileOut)
    {
        if (ProfileIn is null || ProfileOut is null)
            Fail("Unable to open profiles");

        var xform = cmsCreateTransformTHR(ct, ProfileIn, TYPE_RGB_8, ProfileOut, TYPE_RGB_8, INTENT_PERCEPTUAL, FLAGS | cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(ProfileIn);
        cmsCloseProfile(ProfileOut);

        const int Mb = 256 * 256 * 256;
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

        MeasureTimeStart();

        cmsDoTransform(xform, In, In, Mb);

        var diff = MeasureTimeStop();

        cmsDeleteTransform(xform);

        return MPixSec(diff);
    }

    private static double SpeedTest8bitsRGBA(Context? ct, Profile? ProfileIn, Profile? ProfileOut)
    {
        if (ProfileIn is null || ProfileOut is null)
            Fail("Unable to open profiles");

        var xform = cmsCreateTransformTHR(ct, ProfileIn, TYPE_RGBA_8, ProfileOut, TYPE_RGBA_8, INTENT_PERCEPTUAL, FLAGS | cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(ProfileIn);
        cmsCloseProfile(ProfileOut);

        const int Mb = 256 * 256 * 256;
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

        MeasureTimeStart();

        cmsDoTransform(xform, In, In, Mb);

        var diff = MeasureTimeStop();

        cmsDeleteTransform(xform);

        return MPixSec(diff);
    }

    private static double SpeedTest16bitsRGB(Context? ct, Profile? ProfileIn, Profile? ProfileOut)
    {
        if (ProfileIn is null || ProfileOut is null)
            Fail("Unable to open profiles");

        var xform = cmsCreateTransformTHR(ct, ProfileIn, TYPE_RGB_16, ProfileOut, TYPE_RGB_16, INTENT_PERCEPTUAL, FLAGS | cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(ProfileIn);
        cmsCloseProfile(ProfileOut);

        const int Mb = 256 * 256 * 256;
        var In = new Scanline_rgb16bits[Mb];

        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    In[j].r = FROM_8_TO_16((byte)r);
                    In[j].g = FROM_8_TO_16((byte)g);
                    In[j].b = FROM_8_TO_16((byte)b);

                    j++;
                }
            }
        }

        MeasureTimeStart();

        cmsDoTransform(xform, In, In, Mb);

        var diff = MeasureTimeStop();

        cmsDeleteTransform(xform);

        return MPixSec(diff);
    }

    private static double SpeedTest16bitsCMYK(Context? ct, Profile? ProfileIn, Profile? ProfileOut)
    {
        if (ProfileIn is null || ProfileOut is null)
            Fail("Unable to open profiles");

        var xform = cmsCreateTransformTHR(ct, ProfileIn, TYPE_CMYK_16, ProfileOut, TYPE_CMYK_16, INTENT_PERCEPTUAL, FLAGS | cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(ProfileIn);
        cmsCloseProfile(ProfileOut);

        const int Mb = 256 * 256 * 256;
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

        MeasureTimeStart();

        cmsDoTransform(xform, In, In, Mb);

        var diff = MeasureTimeStop();

        cmsDeleteTransform(xform);

        return MPixSec(diff);
    }

    public static void SpeedTest8()
    {
        var noPlugin = cmsCreateContext();

        var t = new double[10];

        var sz = Unsafe.SizeOf<Scanline_rgb8bits>();

        using (logger.BeginScope("Multithreaded performance (8 bit)"))
        {
            using (logger.BeginScope("Default"))
            {
                trace("P E R F O R M A N C E   T E S T S   8  B I T S  (D E F A U L T)");

                t[0] = Performance("8 bits on CLUT profiles", SpeedTest8bitsRGB, noPlugin, "test5", "test3", sz, 0);
                t[1] = Performance("8 bits on Matrix-Shaper profiles", SpeedTest8bitsRGB, noPlugin, "test5", "test0", sz, 0);
                t[2] = Performance("8 bits on same Matrix-Shaper profile", SpeedTest8bitsRGB, noPlugin, "test0", "test0", sz, 0);
                t[3] = Performance("8 bits on curves", SpeedTest8bitsRGB, noPlugin, "*curves", "*curves", sz, 0);
            }

            // Note that context null has the plugin installed
            using (logger.BeginScope("Plugin"))
            {
                trace("P E R F O R M A N C E   T E S T S   8  B I T S  (P L U G I N)");

                Performance("8 bits on CLUT profiles", SpeedTest8bitsRGB, null, "test5", "test3", sz, t[0]);
                Performance("8 bits on Matrix-Shaper profiles", SpeedTest8bitsRGB, null, "test5", "test0", sz, t[1]);
                Performance("8 bits on same Matrix-Shaper profile", SpeedTest8bitsRGB, null, "test0", "test0", sz, t[2]);
                Performance("8 bits on curves", SpeedTest8bitsRGB, null, "*curves", "*curves", sz, t[3]);
            }

            cmsDeleteContext(noPlugin);
        }
    }

    public static void SpeedTest16()
    {
        var noPlugin = cmsCreateContext();

        var t = new double[10];

        var sz = Unsafe.SizeOf<Scanline_rgb16bits>();
        var szCmyk = Unsafe.SizeOf<Scanline_cmyk16bits>();

        using (logger.BeginScope("Multithreaded performance (16 bit)"))
        {
            using (logger.BeginScope("Default"))
            {
                trace("P E R F O R M A N C E   T E S T S   1 6  B I T S  (D E F A U L T)");

                t[0] = Performance("16 bits on CLUT profiles", SpeedTest16bitsRGB, noPlugin, "test5", "test3", sz, 0);
                t[1] = Performance("16 bits on Matrix-Shaper profiles", SpeedTest16bitsRGB, noPlugin, "test5", "test0", sz, 0);
                t[2] = Performance("16 bits on same Matrix-Shaper profile", SpeedTest16bitsRGB, noPlugin, "test0", "test0", sz, 0);
                t[3] = Performance("16 bits on curves", SpeedTest16bitsRGB, noPlugin, "*curves", "*curves", sz, 0);
                t[4] = Performance("16 bits CMYK CLUT profiles", SpeedTest16bitsCMYK, noPlugin, "test1", "test2", szCmyk, 0);
            }

            // Note that context null has the plugin installed
            using (logger.BeginScope("Plugin"))
            {
                trace("P E R F O R M A N C E   T E S T S   1 6  B I T S  (P L U G I N)");

                Performance("16 bits on CLUT profiles", SpeedTest16bitsRGB, null, "test5", "test3", sz, t[0]);
                Performance("16 bits on Matrix-Shaper profiles", SpeedTest16bitsRGB, null, "test5", "test0", sz, t[1]);
                Performance("16 bits on same Matrix-Shaper profile", SpeedTest16bitsRGB, null, "test0", "test0", sz, t[2]);
                Performance("16 bits on curves", SpeedTest16bitsRGB, null, "*curves", "*curves", sz, t[3]);
                Performance("16 bits on CMYK CLUT profiles", SpeedTest16bitsCMYK, null, "test1", "test2", szCmyk, t[4]);
            }

            cmsDeleteContext(noPlugin);
        }
    }

    private unsafe struct padded_line
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
        private fixed byte _line[256 * (256 * 256 * 4 + 4)];

        public padded_line* line(int i)
        {
            fixed (void* ptr = _line)
                return &((padded_line*)ptr)[i];
        }
    }

    private unsafe static double SpeedTest8bitDoTransform(Context? ct, Profile? ProfileIn, Profile? ProfileOut)
    {
        if (ProfileIn is null || ProfileOut is null)
            Fail("Unable to open profiles");

        var xform = cmsCreateTransformTHR(ct, ProfileIn, TYPE_RGBA_8, ProfileOut, TYPE_RGBA_8, INTENT_PERCEPTUAL, FLAGS | cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(ProfileIn);
        cmsCloseProfile(ProfileOut);

        // Our test bitmap is 256 x 256 padded lines
        var Mb = Unsafe.SizeOf<big_bitmap>();

        var In = (big_bitmap*)NativeMemory.AllocZeroed((nuint)Mb);
        var Out = (big_bitmap*)NativeMemory.AllocZeroed((nuint)Mb);

        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    var ptr = In->line(r)->pixels(g, b);
                    ptr->r = (byte)r;
                    ptr->g = (byte)g;
                    ptr->b = (byte)b;
                    ptr->a = 0;
                }
            }
        }

        MeasureTimeStart();

        for (var j = 0; j < 256; j++)
        {
            var inLine = In->line(j);
            var outLine = Out->line(j);

            cmsDoTransform(
                xform,
                new ReadOnlySpan<Scanline_rgba8bits>(inLine, 256 * 256),
                new Span<Scanline_rgba8bits>(outLine, 256 * 256),
                256 * 256);
        }

        var diff = MeasureTimeStop();

        NativeMemory.Free(In);
        NativeMemory.Free(Out);

        cmsDeleteTransform(xform);

        return MPixSec(diff);
    }

    private unsafe static double SpeedTest8bitLineStride(Context? ct, Profile? ProfileIn, Profile? ProfileOut)
    {
        if (ProfileIn is null || ProfileOut is null)
            Fail("Unable to open profiles");

        var xform = cmsCreateTransformTHR(ct, ProfileIn, TYPE_RGBA_8, ProfileOut, TYPE_RGBA_8, INTENT_PERCEPTUAL, FLAGS | cmsFLAGS_NOCACHE)!;
        cmsCloseProfile(ProfileIn);
        cmsCloseProfile(ProfileOut);

        // Our test bitmap is 256 x 256 padded lines
        var Mb = Unsafe.SizeOf<big_bitmap>();

        var In = (big_bitmap*)NativeMemory.AllocZeroed((nuint)Mb);
        var Out = (big_bitmap*)NativeMemory.AllocZeroed((nuint)Mb);

        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    var ptr = In->line(r)->pixels(g, b);
                    ptr->r = (byte)r;
                    ptr->g = (byte)g;
                    ptr->b = (byte)b;
                    ptr->a = 0;
                }
            }
        }

        MeasureTimeStart();

        cmsDoTransformLineStride(
            xform,
            new ReadOnlySpan<big_bitmap>(In, 1),
            new Span<big_bitmap>(Out, 1),
            256 * 256,
            256,
            (uint)Unsafe.SizeOf<padded_line>(),
            (uint)Unsafe.SizeOf<padded_line>(),
            0,
            0);

        var diff = MeasureTimeStop();

        cmsDeleteTransform(xform);

        NativeMemory.Free(In);
        NativeMemory.Free(Out);

        return MPixSec(diff);
    }

    public static void ComparativeLineStride8bits()
    {
        var noPlugin = cmsCreateContext();
        var Plugin = cmsCreateContext(cmsThreadedExtensions(CMS_THREADED_GUESS_MAX_THREADS, 0), null);

        using (logger.BeginScope("Multithreaded performance (comparisons)"))
        {
            trace("C O M P A R A T I V E cmsDoTransform() vs. cmsDoTransformLineStride()");

            ComparativeCt(noPlugin, Plugin, "CLUT profiles", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, "test5", "test3");
            ComparativeCt(noPlugin, Plugin, "CLUT 16 bits", SpeedTest16bitsRGB, SpeedTest16bitsRGB, "test5", "test3");
            ComparativeCt(noPlugin, Plugin, "Matrix-Shaper", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, "test5", "test0");
            ComparativeCt(noPlugin, Plugin, "same Matrix-Shaper", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, "test0", "test0");
            ComparativeCt(noPlugin, Plugin, "curves", SpeedTest8bitDoTransform, SpeedTest8bitLineStride, "", "");

            cmsDeleteContext(Plugin);
            cmsDeleteContext(noPlugin);
        }
    }
}
