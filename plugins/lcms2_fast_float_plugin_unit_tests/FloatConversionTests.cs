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

using System.Resources;

namespace lcms2.FastFloatPlugin.tests;
public class FloatConversionTests
{
    private static readonly Context _pluginCtx = new();
    private static readonly Context _rawCtx = new();

    [OneTimeSetUp]
    public void Setup() =>
        _pluginCtx.RegisterPlugin(cmsFastFloatExtensions());

    [OneTimeTearDown]
    public void Cleanup()
    {
    }

    [Test]
    public void TestTransformCreationFailureWhenUsingMismatchedChannelsAndCopyAlpha()
    {
        _pluginCtx.SetLoggerFactory(BuildNullLogger());

        var hsRGB = cmsCreate_sRGBProfileTHR(_pluginCtx)!;

        var xform = cmsCreateTransformTHR(_pluginCtx, hsRGB, TYPE_RGB_FLT, hsRGB, TYPE_RGBA_FLT, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA);
        cmsCloseProfile(hsRGB);

        Assert.That(xform, Is.Null);
    }

    [TestCase("test5", "test0", INTENT_PERCEPTUAL, false, Description = "Without Alpha Copy")]
    [TestCase("test5", "test0", INTENT_PERCEPTUAL, true, Description = "With Alpha Copy")]
    [TestCase("test0", "test0", INTENT_PERCEPTUAL, false, Description = "Without Alpha Copy")]
    [TestCase("test0", "test0", INTENT_PERCEPTUAL, true, Description = "With Alpha Copy")]
    public void TestAllFloatValuesWithAlphaTransformParity(string profileInName, string profileOutName, int Intent, bool copyAlpha)
    {
        var flags = cmsFLAGS_NOCACHE | (copyAlpha ? cmsFLAGS_COPY_ALPHA : 0);
        var resources = new ResourceManager("lcms2.FastFloatPlugin.tests.TestProfiles", typeof(FloatConversionTests).Assembly);

        var profileIn = cmsOpenProfileFromMemTHR(_pluginCtx, (byte[])resources.GetObject(profileInName)!)!;
        var profileOut = cmsOpenProfileFromMemTHR(_pluginCtx, (byte[])resources.GetObject(profileOutName)!)!;

        var xformRaw = cmsCreateTransformTHR(_rawCtx, profileIn, TYPE_RGBA_FLT, profileOut, TYPE_RGBA_FLT, (uint)Intent, flags)!;
        var xformPlugin = cmsCreateTransformTHR(_pluginCtx, profileIn, TYPE_RGBA_FLT, profileOut, TYPE_RGBA_FLT, (uint)Intent, flags)!;

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        Assert.Multiple(() =>
        {
            Assert.That(xformRaw, Is.Not.Null);
            Assert.That(xformPlugin, Is.Not.Null);
        });

        const int nPixels = 256 * 256 * 256;

        var bufferIn = new Scanline_rgbaFloat[nPixels];
        var bufferRawOut = new Scanline_rgbaFloat[nPixels];
        var bufferPluginOut = new Scanline_rgbaFloat[nPixels];

        // Same input to both transforms
        var j = 0;
        for (var r = 0; r < 256; r++)
            for (var g = 0; g < 256; g++)
                for (var b = 0; b < 256; b++)
                {
                    bufferIn[j].r = r / 255.0f;
                    bufferIn[j].g = g / 255.0f;
                    bufferIn[j].b = b / 255.0f;
                    bufferIn[j].a = 1.0f;

                    j++;
                }

        // Different transforms, different output buffers
        cmsDoTransform(xformRaw, bufferIn, bufferRawOut, nPixels);
        cmsDoTransform(xformPlugin, bufferIn, bufferPluginOut, nPixels);

        // Let's compare results
        Assert.Multiple(() =>
        {
            for (j = 0; j < nPixels; j++)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(bufferPluginOut[j].r, Is.EqualTo(bufferRawOut[j].r).Within(EPSILON_FLOAT_TESTS));
                    Assert.That(bufferPluginOut[j].g, Is.EqualTo(bufferRawOut[j].g).Within(EPSILON_FLOAT_TESTS));
                    Assert.That(bufferPluginOut[j].b, Is.EqualTo(bufferRawOut[j].b).Within(EPSILON_FLOAT_TESTS));
                    Assert.That(bufferPluginOut[j].a, Is.EqualTo(bufferRawOut[j].a).Within(EPSILON_FLOAT_TESTS));
                });
            }
        });
    }

    //[TestCase("test5", "test3", INTENT_PERCEPTUAL)]
    //[TestCase("test5", "test0", INTENT_PERCEPTUAL)]
    public void TestAllUncommonFloatValueTransformParity(string profileInName, string profileOutName, int Intent)
    {
        var sub_pos = new Sub();
        var sub_neg = new Sub();

        const uint nPixels = 100;

        var resources = new ResourceManager("lcms2.FastFloatPlugin.tests.TestProfiles", typeof(FloatConversionTests).Assembly);

        var profileIn = cmsOpenProfileFromMemTHR(_pluginCtx, (byte[])resources.GetObject(profileInName)!)!;
        var profileOut = cmsOpenProfileFromMemTHR(_pluginCtx, (byte[])resources.GetObject(profileOutName)!)!;

        var xformPlugin = cmsCreateTransformTHR(_pluginCtx, profileIn, TYPE_RGB_FLT, profileOut, TYPE_RGB_FLT, (uint)Intent, 0)!;

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        sub_pos.Int = 0x00000002;
        sub_neg.Int = 0x80000002;

        Assert.That(xformPlugin, Is.Not.Null);

        var bufferIn = new Scanline_rgbFloat[nPixels];
        var bufferOut = new Scanline_rgbFloat[nPixels];

        for (var i = 0; i < nPixels; i++)
        {
            bufferIn[i].r = (i / 40f) - 0.5f;
            bufferIn[i].g = (i / 20f) - 0.5f;
            bufferIn[i].b = (i / 60f) - 0.5f;
        }

        cmsDoTransform(xformPlugin, bufferIn, bufferOut, nPixels);

        bufferIn[0].r = float.NaN;
        bufferIn[0].g = float.NaN;
        bufferIn[0].b = float.NaN;

        bufferIn[1].r = float.PositiveInfinity;
        bufferIn[1].g = float.PositiveInfinity;
        bufferIn[1].b = float.PositiveInfinity;

        bufferIn[2].r = sub_pos.Subnormal;
        bufferIn[2].g = sub_pos.Subnormal;
        bufferIn[2].b = sub_pos.Subnormal;

        bufferIn[3].r = sub_neg.Subnormal;
        bufferIn[3].g = sub_neg.Subnormal;
        bufferIn[3].b = sub_neg.Subnormal;

        cmsDoTransform(xformPlugin, bufferIn, bufferOut, 4);

        cmsDeleteTransform(xformPlugin);
    }
    private class Sub
    {
        public uint Int { get; set; }
        public float Subnormal
        {
            get => BitConverter.UInt32BitsToSingle(Int);
            set => Int = BitConverter.SingleToUInt32Bits(value);
        }
    }
}
