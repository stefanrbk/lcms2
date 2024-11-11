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

using System.Numerics;

namespace lcms2.FastFloatPlugin.tests;
public class Lab2RGBTests
{
    private static readonly Context _pluginCtx = cmsCreateContext()!;
    private static readonly Context _rawCtx = cmsCreateContext()!;

    [OneTimeSetUp]
    public void Setup() =>
        cmsPluginTHR(_pluginCtx, cmsFastFloatExtensions());

    [OneTimeTearDown]
    public void Cleanup()
    {
        cmsDeleteContext(_rawCtx);
        cmsDeleteContext(_pluginCtx);
    }

    [Test]
    public void ReportInsideAndOutsideGamutValuesWithAndWithoutThePlugin()
    {
        var hLab = cmsCreateLab4ProfileTHR(_pluginCtx, null)!;
        var hRGB = cmsOpenProfileFromMemTHR(_pluginCtx, TestProfiles.test3)!;

        var hXformNoPlugin = cmsCreateTransformTHR(_rawCtx, hLab, TYPE_Lab_FLT, hRGB, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_NOCACHE)!;
        var hXformPlugin = cmsCreateTransformTHR(_pluginCtx, hLab, TYPE_Lab_FLT, hRGB, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, cmsFLAGS_NOCACHE)!;

        cmsCloseProfile(hLab);
        cmsCloseProfile(hRGB);

        var Lab = new float[3];
        var RGB = new float[3];
        var RGB2 = new float[3];

        var maxInside = 0f;
        var maxOutside = 0f;

        ref var L = ref Lab[0];
        ref var a = ref Lab[1];
        ref var b = ref Lab[2];

        for (L = 4; L <= 100; L++)
        {
            for (a = -30; a < +30; a++)
            {
                for (b = -30; b < +30; b++)
                {
                    cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                    cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                    var d = Distance<float>(RGB, RGB2);
                    if (d > maxInside)
                        maxInside = d;
                }
            }
        }

        for (L = 1; L <= 100; L += 5)
        {
            for (a = -100; a < +100; a += 5)
            {
                for (b = -100; b < +100; b += 5)
                {
                    cmsDoTransform(hXformNoPlugin, Lab, RGB, 1);
                    cmsDoTransform(hXformPlugin, Lab, RGB2, 1);

                    var d = Distance<float>(RGB, RGB2);
                    if (d > maxOutside)
                        maxOutside = d;
                }
            }
        }

        TestContext.Out.WriteLine($"Max distance: Inside gamut {MathF.Sqrt(maxInside):F6}, Outside gamut {MathF.Sqrt(maxOutside):F6}");

        cmsDeleteTransform(hXformNoPlugin);
        cmsDeleteTransform(hXformPlugin);
    }

    private static T Distance<T>(ReadOnlySpan<T> rgb1, ReadOnlySpan<T> rgb2) where T : INumber<T>
    {
        var dr = rgb2[0] - rgb1[0];
        var dg = rgb2[1] - rgb1[1];
        var db = rgb2[2] - rgb1[2];

        return (dr * dr) + (dg * dg) + (db * db);
    }
}
