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

using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

namespace lcms2.testbed;

internal static partial class Testbed
{
    private static bool CheckBadProfiles()
    {
        var h = cmsOpenProfileFromFileTHR(DbgThread(), "IDoNotExist.icc", "r");
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromFileTHR(DbgThread(), "IAmIllFormed*.icc", "r");
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromFileTHR(DbgThread(), "", "r");
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromFileTHR(DbgThread(), "..", "r");
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromFileTHR(DbgThread(), "IHaveBadAccessMode.icc", "@");
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.bad);
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.toosmall);
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromMemTHR(DbgThread(), null, 3);
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        h = cmsOpenProfileFromMemTHR(DbgThread(), "123"u8.ToArray(), 3);
        if (h is not null)
        {
            cmsCloseProfile(h);
            return false;
        }

        return true;
    }

    internal static bool CheckErrReportingOnBadProfiles()
    {
        Context.Shared.SetLoggerFactory(BuildNullLogger());
        var rc = CheckBadProfiles();
        Context.Shared.SetLoggerFactory(BuildDebugLogger());

        return rc;
    }

    private static bool CheckBadTransforms()
    {
        var h1 = cmsCreate_sRGBProfile()!;

        var x1 = cmsCreateTransform(null!, 0, null!, 0, 0, 0);
        if (x1 is not null)
        {
            cmsDeleteTransform(x1);
            return false;
        }

        x1 = cmsCreateTransform(h1, TYPE_RGB_8, h1, TYPE_RGB_8, 12345, 0);
        if (x1 is not null)
        {
            cmsDeleteTransform(x1);
            return false;
        }

        x1 = cmsCreateTransform(h1, TYPE_CMYK_8, h1, TYPE_RGB_8, 0, 0);
        if (x1 is not null)
        {
            cmsDeleteTransform(x1);
            return false;
        }

        x1 = cmsCreateTransform(h1, TYPE_RGB_8, h1, TYPE_CMYK_8, 1, 0);
        if (x1 is not null)
        {
            cmsDeleteTransform(x1);
            return false;
        }

        x1 = cmsCreateTransform(h1, TYPE_RGB_8, null!, TYPE_Lab_8, 1, 0);
        if (x1 is not null)
        {
            cmsDeleteTransform(x1);
            return false;
        }

        cmsCloseProfile(h1);

        var hp1 = cmsOpenProfileFromMem(TestProfiles.test1)!;
        var hp2 = cmsCreate_sRGBProfile()!;

        x1 = cmsCreateTransform(hp1, TYPE_BGR_8, hp2, TYPE_BGR_8, INTENT_PERCEPTUAL, 0);

        cmsCloseProfile(hp1);
        cmsCloseProfile(hp2);
        if (x1 is not null)
        {
            cmsDeleteTransform(x1);
            return false;
        }

        return true;
    }

    internal static bool CheckErrReportingOnBadTransforms()
    {
        Context.Shared.SetLoggerFactory(BuildNullLogger());
        var rc = CheckBadTransforms();
        Context.Shared.SetLoggerFactory(BuildDebugLogger());

        return rc;
    }
}
