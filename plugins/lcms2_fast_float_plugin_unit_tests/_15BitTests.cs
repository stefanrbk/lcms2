﻿using lcms2.state;
using lcms2.types;

using System.Resources;

namespace lcms2.FastFloatPlugin.tests;
public class _15BitTests
{
    private readonly Context _ctx = cmsCreateContext()!;

    [SetUp]
    public void Setup() =>
        cmsPluginTHR(_ctx, cmsFastFloatExtensions());

    [TestCaseSource(nameof(Test15BitFormattersReturnInputsAfterRoundTripCases))]
    public void Test15BitFormattersReturnInputsAfterRoundTrip(string a)
    {
        var field = typeof(FastFloat).GetProperty(a) ?? typeof(Lcms2).GetProperty(a);
        var Type = (uint)field!.GetValue(null)!;

        var ValuesIn = new ushort[cmsMAXCHANNELS];
        var ValuesOut = new ushort[cmsMAXCHANNELS];
        var Buffer = new byte[1024];

        var info = new _xform_head(Type, Type);

        // Get functions to go back and forth
        var f = Formatter_15Bit_Factory_In(Type, (uint)PackFlags.Ushort);
        var b = Formatter_15Bit_Factory_Out(Type, (uint)PackFlags.Ushort);

        Assert.Multiple(() =>
        {
            Assert.That(f.Fmt16, Is.Not.Null, $"In formatter missing for {a}");
            Assert.That(b.Fmt16, Is.Not.Null, $"Out formatter missing for {a}");
        });

        var nChannels = T_CHANNELS(Type);
        var bytes = T_BYTES(Type);

        Assert.Multiple(() =>
        {
            for (var j = 0; j < 5; j++)
            {
                for (var i = 0; i < nChannels; i++)
                {
                    ValuesIn[i] = (ushort)((i + j) << 1);
                }

                b.Fmt16((Transform)info, ValuesIn, Buffer, 1);
                f.Fmt16((Transform)info, ValuesOut, Buffer, 1);

                Assert.That(ValuesOut, Is.EquivalentTo(ValuesIn));
            }
        });
    }

    [Test]
    public void TestInternal15BitMacrosReturnProperValuesAfterRoundTrip()
    {
        Assert.Multiple(() =>
        {
            for (var i = 0; i < 256; i++)
            {
                var n = FROM_8_TO_15((byte)i);
                var m = FROM_15_TO_8(n);

                Assert.That(m, Is.EqualTo(i));
            }
        });
    }

    [TestCase(nameof(TestProfiles.test5), nameof(TestProfiles.test3), INTENT_PERCEPTUAL, Description = nameof(INTENT_PERCEPTUAL))]
    public void TestConversionsOnAll15BitValues(string profileInName, string profileOutName, int Intent)
    {
        var resources = new ResourceManager("lcms2.FastFloatPlugin.tests.TestProfiles", typeof(_15BitTests).Assembly);

        var profileInData = (byte[])resources.GetObject(profileInName)!;
        var profileOutData = (byte[])resources.GetObject(profileOutName)!;

        var profileIn = cmsOpenProfileFromMemTHR(_ctx, profileInData)!;
        var profileOut = cmsOpenProfileFromMemTHR(_ctx, profileOutData)!;

        var xform15 = cmsCreateTransformTHR(_ctx, profileIn, TYPE_RGB_15, profileOut, TYPE_RGB_15, (uint)Intent, cmsFLAGS_NOCACHE);
        var xform8 = cmsCreateTransformTHR(_ctx, profileIn, TYPE_RGB_8, profileOut, TYPE_RGB_8, (uint)Intent, cmsFLAGS_NOCACHE);

        cmsCloseProfile(profileIn);
        cmsCloseProfile(profileOut);

        Assert.Multiple(() =>
        {
            Assert.That(xform15, Is.Not.Null);
            Assert.That(xform8, Is.Not.Null);
        });

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

        cmsDoTransform(xform15, buffer15in, buffer15out, npixels);
        cmsDoTransform(xform8, buffer8in, buffer8out, npixels);

        Assert.Multiple(() =>
        {
            for (var j = 0; j < npixels; j++)
            {
                // Check the results
                Assert.Multiple(() =>
                {
                    Assert.That(FROM_15_TO_8(buffer15out[j].r), Is.EqualTo(buffer8out[j].r).Within(2));
                    Assert.That(FROM_15_TO_8(buffer15out[j].g), Is.EqualTo(buffer8out[j].g).Within(2));
                    Assert.That(FROM_15_TO_8(buffer15out[j].b), Is.EqualTo(buffer8out[j].b).Within(2));
                });
            }
        });

    }

    internal static object[] Test15BitFormattersReturnInputsAfterRoundTripCases =
    {
        nameof(TYPE_GRAY_15),
        nameof(TYPE_GRAY_15_REV),
        nameof(TYPE_GRAY_15_SE),
        nameof(TYPE_GRAYA_15),
        nameof(TYPE_GRAYA_15_SE),
        nameof(TYPE_GRAYA_15_PLANAR),
        nameof(TYPE_RGB_15),
        nameof(TYPE_RGB_15_PLANAR),
        nameof(TYPE_RGB_15_SE),
        nameof(TYPE_BGR_15),
        nameof(TYPE_BGR_15_PLANAR),
        nameof(TYPE_BGR_15_SE),
        nameof(TYPE_RGBA_15),
        nameof(TYPE_RGBA_15_PLANAR),
        nameof(TYPE_RGBA_15_SE),
        nameof(TYPE_ARGB_15),
        nameof(TYPE_ABGR_15),
        nameof(TYPE_ABGR_15_PLANAR),
        nameof(TYPE_ABGR_15_SE),
        nameof(TYPE_BGRA_15),
        nameof(TYPE_BGRA_15_SE),
        nameof(TYPE_YMC_15),
        nameof(TYPE_CMY_15),
        nameof(TYPE_CMY_15_PLANAR),
        nameof(TYPE_CMY_15_SE),
        nameof(TYPE_CMYK_15),
        nameof(TYPE_CMYK_15_REV),
        nameof(TYPE_CMYK_15_PLANAR),
        nameof(TYPE_CMYK_15_SE),
        nameof(TYPE_KYMC_15),
        nameof(TYPE_KYMC_15_SE),
        nameof(TYPE_KCMY_15),
        nameof(TYPE_KCMY_15_REV),
        nameof(TYPE_KCMY_15_SE),
    };
}
