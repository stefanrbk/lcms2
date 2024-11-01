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

using NUnit.Framework.Internal;

namespace lcms2.FastFloatPlugin.tests;
public class SoftProofingTests
{
    private static readonly Context _pluginCtx = new();
    private static readonly Context _rawCtx = new();

    [OneTimeSetUp]
    public void Setup() =>
        cmsPluginTHR(_pluginCtx, cmsFastFloatExtensions());

    [OneTimeTearDown]
    public void Cleanup()
    {
    }

    [TestCaseSource(nameof(TestSoftProofingTransformParityCaseGenerator))]
    public void TestSoftProofingTransformParity(object oIn)
    {
        var hRGB1 = cmsOpenProfileFromMemTHR(_pluginCtx, TestProfiles.test5)!;
        var hRGB2 = cmsOpenProfileFromMemTHR(_pluginCtx, TestProfiles.test3)!;

        var xformNoPlugin = cmsCreateProofingTransformTHR(_rawCtx, hRGB1, TYPE_RGB_FLT, hRGB1, TYPE_RGB_FLT, hRGB2, INTENT_RELATIVE_COLORIMETRIC, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;
        var xformPlugin = cmsCreateProofingTransformTHR(_pluginCtx, hRGB1, TYPE_RGB_FLT, hRGB1, TYPE_RGB_FLT, hRGB2, INTENT_RELATIVE_COLORIMETRIC, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_GAMUTCHECK | cmsFLAGS_SOFTPROOFING)!;

        cmsCloseProfile(hRGB1);
        cmsCloseProfile(hRGB2);

        uint nPixels = 256 * 256 * 4;
        var rawIn = (float[][])oIn;
        var In = new Scanline_rgbFloat[nPixels];
        var Out1 = new Scanline_rgbFloat[nPixels];
        var Out2 = new Scanline_rgbFloat[nPixels];

        for (var j = 0; j < nPixels; j++)
            In[j] = new(rawIn[j][0], rawIn[j][1], rawIn[j][2]);

        cmsDoTransform(xformNoPlugin, In, Out1, nPixels);
        cmsDoTransform(xformPlugin, In, Out2, nPixels);

        Assert.Multiple(() =>
        {
            for (var j = 0; j < nPixels; j++)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(Out2[j].r, Is.EqualTo(Out1[j].r).Within(EPSILON_FLOAT_TESTS));
                    Assert.That(Out2[j].g, Is.EqualTo(Out1[j].g).Within(EPSILON_FLOAT_TESTS));
                    Assert.That(Out2[j].b, Is.EqualTo(Out1[j].b).Within(EPSILON_FLOAT_TESTS));
                });
            }
        });

        cmsDeleteTransform(xformNoPlugin);
        cmsDeleteTransform(xformPlugin);
    }

    internal static IEnumerable<object> TestSoftProofingTransformParityCaseGenerator()
    {
        var rand = TestContext.CurrentContext.Random;

        var values = new float[256 * 256 * 4][];

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < values.Length; j++)
                values[j] = [rand.Next(0, 255) / 255f, rand.Next(0, 255) / 255f, rand.Next(0, 255) / 255f];
            yield return values;
        }

        rand = new Randomizer();

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < values.Length; j++)
                values[j] = [rand.Next(0, 255) / 255f, rand.Next(0, 255) / 255f, rand.Next(0, 255) / 255f];
            yield return values;
        }
    }
}
