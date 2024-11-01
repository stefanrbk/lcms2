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

namespace lcms2.FastFloatPlugin.tests;
public class ChangeFormatTests
{
    private static readonly Context _ctx = new();

    [OneTimeSetUp]
    public void Setup() =>
        cmsPluginTHR(_ctx, cmsFastFloatExtensions());

    [Test]
    public void TestChangeFormatFunctionWorksWithKnownValues()
    {
        var rgb8 = new Scanline_rgb8bits(10, 120, 40);
        var rgb16 = new Scanline_rgb16bits(10 * 257, 120 * 257, 40 * 257);

        var hsRGB = cmsCreate_sRGBProfileTHR(_ctx)!;
        var hLab = cmsCreateLab4ProfileTHR(_ctx, null)!;

        var xform = cmsCreateTransformTHR(_ctx, hsRGB, TYPE_RGB_16, hLab, TYPE_Lab_16, INTENT_PERCEPTUAL, 0)!;

        cmsCloseProfile(hsRGB);
        cmsCloseProfile(hLab);

        cmsDoTransform(xform, rgb16, out Scanline_Lab16bits lab16_1, 1);

        cmsChangeBuffersFormat(xform, TYPE_RGB_8, TYPE_Lab_16);

        cmsDoTransform(xform, rgb8, out Scanline_Lab16bits lab16_2, 1);
        cmsDeleteTransform(xform);

        Assert.That(lab16_2, Is.EqualTo(lab16_1));
    }
}
