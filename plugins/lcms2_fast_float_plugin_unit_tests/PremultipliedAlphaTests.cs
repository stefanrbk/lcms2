﻿namespace lcms2.FastFloatPlugin.tests;
public class PremultipliedAlphaTests
{
    [Test]
    public void TestPremultipliedAlphaParity()
    {
        ReadOnlySpan<byte> BGRA8 = [255, 192, 160, 128];
        var bgrA8_1 = new byte[4];
        var bgrA8_2 = new byte[4];

        var srgb1 = cmsCreate_sRGBProfile();
        var srgb2 = cmsCreate_sRGBProfile();

        var noPlugin = cmsCreateContext();

        var xform1 = cmsCreateTransformTHR(noPlugin, srgb1, TYPE_BGRA_8, srgb2, TYPE_BGRA_8_PREMUL, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA);
        var xform2 = cmsCreateTransform(srgb1, TYPE_BGRA_8, srgb2, TYPE_BGRA_8_PREMUL, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA);

        cmsCloseProfile(srgb1);
        cmsCloseProfile(srgb2);

        cmsDoTransform(xform1, BGRA8, bgrA8_1, 1);
        cmsDoTransform(xform2, BGRA8, bgrA8_2, 1);

        cmsDeleteTransform(xform1);
        cmsDeleteTransform(xform2);

        Assert.That(bgrA8_1, Is.EquivalentTo(bgrA8_2));
    }
}
