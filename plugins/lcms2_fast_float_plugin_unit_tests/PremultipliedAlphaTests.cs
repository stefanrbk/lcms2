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

//[Parallelizable(ParallelScope.All)]
public class PremultipliedAlphaTests
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
    public void TestPremultipliedAlphaParity()
    {
        ReadOnlySpan<byte> BGRA8 = [255, 192, 160, 128];
        var bgrA8_1 = new byte[4];
        var bgrA8_2 = new byte[4];

        var srgb1 = cmsCreate_sRGBProfileTHR(_pluginCtx);
        var srgb2 = cmsCreate_sRGBProfileTHR(_pluginCtx);

        var xform1 = cmsCreateTransformTHR(_rawCtx, srgb1, TYPE_BGRA_8, srgb2, TYPE_BGRA_8_PREMUL, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA);
        var xform2 = cmsCreateTransformTHR(_pluginCtx, srgb1, TYPE_BGRA_8, srgb2, TYPE_BGRA_8_PREMUL, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA);

        cmsCloseProfile(srgb1);
        cmsCloseProfile(srgb2);

        cmsDoTransform(xform1, BGRA8, bgrA8_1, 1);
        cmsDoTransform(xform2, BGRA8, bgrA8_2, 1);

        cmsDeleteTransform(xform1);
        cmsDeleteTransform(xform2);

        Assert.That(bgrA8_1, Is.EquivalentTo(bgrA8_2));
    }
}
