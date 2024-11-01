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

namespace lcms2.ThreadedPlugin.testbed;
internal static partial class Testbed
{
    public static void CheckChangeFormat()
    {
        var rgb8 = new byte[3] { 10, 120, 40 };
        var rgb16 = new ushort[3] { 10 * 257, 120 * 257, 40 * 257 };
        var lab16_1 = new ushort[3];
        var lab16_2 = new ushort[3];

        trace("Checking change format feature");

        using (logger.BeginScope("Change format feature"))
        {
            var hsRGB = cmsCreate_sRGBProfile()!;
            var hLab = cmsCreateLab4Profile(null)!;

            var xform = cmsCreateTransform(hsRGB, TYPE_RGB_16, hLab, TYPE_Lab_16, INTENT_PERCEPTUAL, FLAGS)!;

            cmsCloseProfile(hsRGB);
            cmsCloseProfile(hLab);

            cmsDoTransform(xform, rgb16, lab16_1, 1);

            cmsChangeBuffersFormat(xform, TYPE_RGB_8, TYPE_Lab_16);

            cmsDoTransform(xform, rgb8, lab16_2, 1);
            cmsDeleteTransform(xform);

            for (var i = 0; i < 3; i++)
            {
                if (lab16_1[i] != lab16_2[i])
                    Fail("Change format failed!");
            }

            trace("Ok");
        }
    }

    private static void TryAllValues8bits(Profile hlcmsProfileIn, Profile hlcmsProfileOut, int Intent)
    {
        var Raw = new Context();
        var Plugin = new Context([cmsThreadedExtensions(CMS_THREADED_GUESS_MAX_THREADS, 0)], null);

        const int npixels = 256 * 256 * 256;

        var xformRaw = cmsCreateTransformTHR(Raw, hlcmsProfileIn, TYPE_RGBA_8, hlcmsProfileOut, TYPE_RGBA_8, (uint)Intent, FLAGS | cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);
        var xformPlugin = cmsCreateTransformTHR(Plugin, hlcmsProfileIn, TYPE_RGBA_8, hlcmsProfileOut, TYPE_RGBA_8, (uint)Intent, FLAGS | cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);

        cmsCloseProfile(hlcmsProfileIn);
        cmsCloseProfile(hlcmsProfileOut);

        if (xformRaw is null || xformPlugin is null)
            Fail("Null transforms on check float conversions");

        var bufferIn = new Scanline_rgba8bits[npixels];
        var bufferRawOut = new Scanline_rgba8bits[npixels];
        var bufferPluginOut = new Scanline_rgba8bits[npixels];

        // Same input to both transforms
        var j = 0;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0;  g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    bufferIn[j].r = (byte)r;
                    bufferIn[j].g = (byte)g;
                    bufferIn[j].b = (byte)b;
                    bufferIn[j].a = 0xff;

                    j++;
                }
            }
        }

        // Different transforms, different output buffers
        cmsDoTransform(xformRaw, bufferIn, bufferRawOut, (uint)npixels);
        cmsDoTransform(xformPlugin, bufferIn, bufferPluginOut, (uint)npixels);

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
                        Fail("Conversion failed at [{0} {1} {2} {3}] ({4} {5} {6} {7}) != ({8} {9} {10} {11}",
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
    }

    public static void CheckAccuracy8Bits()
    {
        trace("Checking accuracy of 8 bits CLUT");
        using (logger.BeginScope("8 bit CLUT accuracy"))
        {
            TryAllValues8bits(cmsOpenProfileFromMem(plugins.TestProfiles.test5)!, cmsOpenProfileFromMem(plugins.TestProfiles.test3)!, INTENT_PERCEPTUAL);
            trace("OK");
        }
    }

    private static void TryAllValues16bits(Profile hlcmsProfileIn, Profile hlcmsProfileOut, int Intent)
    {
        var Raw = new Context();
        var Plugin = new Context([cmsThreadedExtensions(CMS_THREADED_GUESS_MAX_THREADS, 0)], null);

        const int npixels = 256 * 256 * 256;

        var xformRaw = cmsCreateTransformTHR(Raw, hlcmsProfileIn, TYPE_RGBA_16, hlcmsProfileOut, TYPE_RGBA_16, (uint)Intent, FLAGS | cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);
        var xformPlugin = cmsCreateTransformTHR(Plugin, hlcmsProfileIn, TYPE_RGBA_16, hlcmsProfileOut, TYPE_RGBA_16, (uint)Intent, FLAGS | cmsFLAGS_NOCACHE | cmsFLAGS_COPY_ALPHA);

        cmsCloseProfile(hlcmsProfileIn);
        cmsCloseProfile(hlcmsProfileOut);

        if (xformRaw is null || xformPlugin is null)
            Fail("Null transforms on check float conversions");

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
                        Fail("Conversion failed at [{0} {1} {2} {3}] ({4} {5} {6} {7}) != ({8} {9} {10} {11}",
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
    }

    public static void CheckAccuracy16Bits()
    {
        trace("Checking accuracy of 16 bits CLUT");
        using (logger.BeginScope("16 bit CLUT accuracy"))
        {
            TryAllValues16bits(cmsOpenProfileFromMem(plugins.TestProfiles.test5)!, cmsOpenProfileFromMem(plugins.TestProfiles.test3)!, INTENT_PERCEPTUAL);
            trace("OK");
        }
    }
}
