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

using System.Runtime.InteropServices;
using System.Text;

using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

namespace lcms2.testbed;

internal static partial class Testbed
{
    private static bool CheckOneRGB_f(Transform xform, int R, int G, int B, double X, double Y, double Z, double err)
    {
        Span<float> RGB = stackalloc float[3];
        Span<double> Out = stackalloc double[3];

        RGB[0] = (float)(R / 255.0);
        RGB[1] = (float)(G / 255.0);
        RGB[2] = (float)(B / 255.0);

        cmsDoTransform(xform, RGB, Out, 1);

        return IsGoodVal("X", X, Out[0], err) &&
               IsGoodVal("Y", Y, Out[1], err) &&
               IsGoodVal("Z", Z, Out[2], err);
    }

    internal static bool Check_sRGB_Float()
    {
        var hsRGB = cmsCreate_sRGBProfileTHR(DbgThread())!;
        var hXYZ = cmsCreateXYZProfileTHR(DbgThread())!;
        var hLab = cmsCreateLab4ProfileTHR(DbgThread(), null)!;

        var xform1 = cmsCreateTransformTHR(
            DbgThread(),
            hsRGB,
            TYPE_RGB_FLT,
            hXYZ,
            TYPE_XYZ_DBL,
            INTENT_RELATIVE_COLORIMETRIC,
            0)!;

        var xform2 = cmsCreateTransformTHR(
            DbgThread(),
            hsRGB,
            TYPE_RGB_FLT,
            hLab,
            TYPE_Lab_DBL,
            INTENT_RELATIVE_COLORIMETRIC,
            0)!;

        cmsCloseProfile(hsRGB);
        cmsCloseProfile(hXYZ);
        cmsCloseProfile(hLab);

        MaxErr = 0;

        bool rc;

        using (logger.BeginScope("8 bits to XYZ"))
        {
            rc = CheckOneRGB_f(xform1, 1, 1, 1, 0.0002927, 0.0003035, 0.000250, 0.0001);
            rc &= CheckOneRGB_f(xform1, 127, 127, 127, 0.2046329, 0.212230, 0.175069, 0.0001);
            rc &= CheckOneRGB_f(xform1, 12, 13, 15, 0.0038364, 0.0039928, 0.003853, 0.0001);
            rc &= CheckOneRGB_f(xform1, 128, 0, 0, 0.0941240, 0.0480256, 0.003005, 0.0001);
            rc &= CheckOneRGB_f(xform1, 190, 25, 210, 0.3204592, 0.1605926, 0.468213, 0.0001);
        }

        using (logger.BeginScope("8 bits to Lab"))
        {
            rc &= CheckOneRGB_f(xform2, 1, 1, 1, 0.2741748, 0, 0, 0.01);
            rc &= CheckOneRGB_f(xform2, 127, 127, 127, 53.192776, 0, 0, 0.01);
            rc &= CheckOneRGB_f(xform2, 190, 25, 210, 47.052136, 74.565610, -56.883274, 0.01);
            rc &= CheckOneRGB_f(xform2, 128, 0, 0, 26.164701, 48.478171, 39.4384713, 0.01);
        }

        cmsDeleteTransform(xform1);
        cmsDeleteTransform(xform2);
        return rc;
    }

    private static bool CheckGray(Transform xform, byte g, double L)
    {
        cmsDoTransform(xform, g, out CIELab Lab, 1);

        if (!IsGoodVal("a axis on gray", 0, Lab.a, 0.001))
            return false;
        if (!IsGoodVal("b axis on gray", 0, Lab.b, 0.001))
            return false;

        return IsGoodVal("Gray value", L, Lab.L, 0.01);
    }

    internal static bool CheckInputGray()
    {
        var hGray = Create_Gray22();
        var hLab = cmsCreateLab4Profile(null);

        if (hGray is null || hLab is null)
            return false;

        var xform = cmsCreateTransform(hGray, TYPE_GRAY_8, hLab, TYPE_Lab_DBL, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(hGray);
        cmsCloseProfile(hLab);

        if (!CheckGray(xform, 0, 0))
            return false;
        if (!CheckGray(xform, 125, 52.768))
            return false;
        if (!CheckGray(xform, 200, 81.069))
            return false;
        if (!CheckGray(xform, 255, 100.0))
            return false;

        cmsDeleteTransform(xform);
        return true;
    }

    internal static bool CheckLabInputGray()
    {
        var hGray = Create_GrayLab();
        var hLab = cmsCreateLab4Profile(null);

        if (hGray is null || hLab is null)
            return false;

        var xform = cmsCreateTransform(hGray, TYPE_GRAY_8, hLab, TYPE_Lab_DBL, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(hGray);
        cmsCloseProfile(hLab);

        if (!CheckGray(xform, 0, 0))
            return false;
        if (!CheckGray(xform, 125, 49.019))
            return false;
        if (!CheckGray(xform, 200, 78.431))
            return false;
        if (!CheckGray(xform, 255, 100.0))
            return false;

        cmsDeleteTransform(xform);
        return true;
    }

    private static bool CheckOutGray(Transform xform, double L, byte g)
    {
        var Lab = new CIELab(
            L,
            0,
            0);

        cmsDoTransform(xform, Lab, out byte g_out, 1);

        return IsGoodVal("Gray value", g, g_out, 1);
    }

    internal static bool CheckOutputGray()
    {
        var hGray = Create_Gray22();
        var hLab = cmsCreateLab4Profile(null);

        if (hGray is null || hLab is null)
            return false;

        var xform = cmsCreateTransform(hLab, TYPE_Lab_DBL, hGray, TYPE_GRAY_8, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(hGray);
        cmsCloseProfile(hLab);

        if (!CheckOutGray(xform, 0, 0))
            return false;
        if (!CheckOutGray(xform, 100, 255))
            return false;
        if (!CheckOutGray(xform, 20, 52))
            return false;
        if (!CheckOutGray(xform, 50, 118))
            return false;

        cmsDeleteTransform(xform);
        return true;
    }

    internal static bool CheckLabOutputGray()
    {
        var hGray = Create_GrayLab();
        var hLab = cmsCreateLab4Profile(null);

        if (hGray is null || hLab is null)
            return false;

        var xform = cmsCreateTransform(hLab, TYPE_Lab_DBL, hGray, TYPE_GRAY_8, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(hGray);
        cmsCloseProfile(hLab);

        if (!CheckOutGray(xform, 0, 0))
            return false;
        if (!CheckOutGray(xform, 100, 255))
            return false;

        for (var i = 0; i < 100; i++)
        {
            var g = (byte)Math.Floor(((i * 255.0) / 100.0) + 0.5);

            if (!CheckOutGray(xform, i, g))
                return false;
        }

        cmsDeleteTransform(xform);
        return true;
    }

    private static bool CheckCMYK(int Intent, byte[] Profile1, byte[] Profile2)
    {
        var hSWOP = cmsOpenProfileFromMemTHR(DbgThread(), Profile1)!;
        var hFOGRA = cmsOpenProfileFromMemTHR(DbgThread(), Profile2)!;

        Span<float> CMYK1 = stackalloc float[4];
        Span<float> CMYK2 = stackalloc float[4];

        var hLab = cmsCreateLab4ProfileTHR(DbgThread(), null)!;

        var xform = cmsCreateTransformTHR(DbgThread(), hSWOP, TYPE_CMYK_FLT, hFOGRA, TYPE_CMYK_FLT, (uint)Intent, 0)!;

        var swop_lab = cmsCreateTransformTHR(DbgThread(), hSWOP, TYPE_CMYK_FLT, hLab, TYPE_Lab_DBL, (uint)Intent, 0)!;
        var fogra_lab = cmsCreateTransformTHR(DbgThread(), hFOGRA, TYPE_CMYK_FLT, hLab, TYPE_Lab_DBL, (uint)Intent, 0)!;

        var Max = 0.0;
        for (var i = 0; i <= 100; i++)
        {
            CMYK1[0] = 10;
            CMYK1[1] = 20;
            CMYK1[2] = 30;
            CMYK1[3] = i;

            cmsDoTransform(swop_lab, CMYK1, out CIELab Lab1, 1);
            cmsDoTransform(xform, CMYK1, CMYK2, 1);
            cmsDoTransform(fogra_lab, CMYK2, out CIELab Lab2, 1);

            var DeltaL = Math.Abs(Lab1.L - Lab2.L);

            Max = Math.Max(Max, DeltaL);
        }

        cmsDeleteTransform(xform);

        xform = cmsCreateTransformTHR(DbgThread(), hFOGRA, TYPE_CMYK_FLT, hSWOP, TYPE_CMYK_FLT, (uint)Intent, 0)!;

        for (var i = 0; i <= 100; i++)
        {
            CMYK1[0] = 10;
            CMYK1[1] = 20;
            CMYK1[2] = 30;
            CMYK1[3] = i;

            cmsDoTransform(fogra_lab, CMYK1, out CIELab Lab1, 1);
            cmsDoTransform(xform, CMYK1, CMYK2, 1);
            cmsDoTransform(swop_lab, CMYK2, out CIELab Lab2, 1);

            var DeltaL = Math.Abs(Lab1.L - Lab2.L);

            Max = Math.Max(Max, DeltaL);
        }

        cmsCloseProfile(hSWOP);
        cmsCloseProfile(hFOGRA);
        cmsCloseProfile(hLab);

        cmsDeleteTransform(xform);
        cmsDeleteTransform(swop_lab);
        cmsDeleteTransform(fogra_lab);

        return Max < 3.0;
    }

    internal static bool CheckCMYKRoundtrip() =>
        CheckCMYK(INTENT_RELATIVE_COLORIMETRIC, TestProfiles.test1, TestProfiles.test1);

    internal static bool CheckCMYKPerceptual() =>
        CheckCMYK(INTENT_PERCEPTUAL, TestProfiles.test1, TestProfiles.test2);

#if false
    internal static bool CheckCMYKRelCol() =>
        CheckCMYK(INTENT_RELATIVE_COLORIMETRIC, TestProfiles.test1, TestProfiles.test2);
#endif

    internal static bool CheckProofingXFORMFloat()
    {
        var hAbove = Create_AboveRGB()!;
        var xform = cmsCreateProofingTransformTHR(
            DbgThread(),
            hAbove,
            TYPE_RGB_FLT,
            hAbove,
            TYPE_RGB_FLT,
            hAbove,
            INTENT_RELATIVE_COLORIMETRIC,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_SOFTPROOFING)!;

        cmsCloseProfile(hAbove);
        var rc = CheckFloatLinearXFORM(xform, 3);
        cmsDeleteTransform(xform);
        return rc;
    }

    internal static bool CheckProofingXFORM16()
    {
        var hAbove = Create_AboveRGB()!;
        var xform = cmsCreateProofingTransformTHR(
            DbgThread(),
            hAbove,
            TYPE_RGB_16,
            hAbove,
            TYPE_RGB_16,
            hAbove,
            INTENT_RELATIVE_COLORIMETRIC,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_SOFTPROOFING | cmsFLAGS_NOCACHE)!;

        cmsCloseProfile(hAbove);
        var rc = Check16linearXFORM(xform, 3);
        cmsDeleteTransform(xform);
        return rc;
    }

    internal static bool CheckGamutCheck()
    {
        var Alarm = new ushort[16];
        Alarm[0] = 0xDEAD;
        Alarm[1] = 0xBABE;
        Alarm[2] = 0xFACE;

        // Set alarm codes to fancy values so we could check the out of gamut condition
        cmsSetAlarmCodes(Alarm);

        // Create the profiles
        var hSRGB = cmsCreate_sRGBProfileTHR(DbgThread());
        var hAbove = Create_AboveRGB();

        if (hSRGB is null || hAbove is null)
            return false;   // Failed

        using (logger.BeginScope("Gamut check on floating point"))
        {
            // Create a gamut checker in the same space. No value should be out of gamut
            var xform = cmsCreateProofingTransformTHR(
                DbgThread(),
                hAbove,
                TYPE_RGB_FLT,
                hAbove,
                TYPE_RGB_FLT,
                hAbove,
                INTENT_RELATIVE_COLORIMETRIC,
                INTENT_RELATIVE_COLORIMETRIC,
                cmsFLAGS_GAMUTCHECK);

            if (!CheckFloatLinearXFORM(xform, 3))
            {
                cmsCloseProfile(hSRGB);
                cmsCloseProfile(hAbove);
                cmsDeleteTransform(xform);
                logger.LogWarning("Gamut check on same profile failed");
                return false;
            }

            cmsDeleteTransform(xform);
        }

        using (logger.BeginScope("Gamut check on 16 bits"))
        {
            var xform = cmsCreateProofingTransformTHR(
                DbgThread(),
                hAbove,
                TYPE_RGB_16,
                hAbove,
                TYPE_RGB_16,
                hSRGB,
                INTENT_RELATIVE_COLORIMETRIC,
                INTENT_RELATIVE_COLORIMETRIC,
                cmsFLAGS_GAMUTCHECK);

            cmsCloseProfile(hSRGB);
            cmsCloseProfile(hAbove);

            if (!Check16linearXFORM(xform, 3))
            {
                cmsDeleteTransform(xform);
                logger.LogWarning("Gamut check on 16 bits failed");
                return false;
            }

            cmsDeleteTransform(xform);
        }

        return true;
    }

    internal static bool CheckKOnlyBlackPreserving()
    {
        var hSWOP = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test1)!;
        var hFOGRA = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test2)!;
        var CMYK1 = new float[4];
        var CMYK2 = new float[4];

        var hLab = cmsCreateLab4ProfileTHR(DbgThread(), null)!;

        var xform = cmsCreateTransformTHR(
            DbgThread(),
            hSWOP,
            TYPE_CMYK_FLT,
            hFOGRA,
            TYPE_CMYK_FLT,
            INTENT_PRESERVE_K_ONLY_PERCEPTUAL,
            0)!;

        var swop_lab =
            cmsCreateTransformTHR(DbgThread(), hSWOP, TYPE_CMYK_FLT, hLab, TYPE_Lab_DBL, INTENT_PERCEPTUAL, 0)!;
        var fogra_lab =
            cmsCreateTransformTHR(DbgThread(), hFOGRA, TYPE_CMYK_FLT, hLab, TYPE_Lab_DBL, INTENT_PERCEPTUAL, 0)!;

        var Max = 0.0;

        using (logger.BeginScope("SWOP to FOGRA"))
        {
            for (var i = 0; i <= 100; i++)
            {
                CMYK1[0] = 0;
                CMYK1[1] = 0;
                CMYK1[2] = 0;
                CMYK1[3] = i;

                // SWOP CMYK to Lab1
                cmsDoTransform(swop_lab, CMYK1, out CIELab Lab1, 1);

                // SWOP To FOGRA using black preservation
                cmsDoTransform(xform, CMYK1, CMYK2, 1);

                // Obtained FOGRA CMYK to Lab2
                cmsDoTransform(fogra_lab, CMYK2, out CIELab Lab2, 1);

                // We care only on L*
                var DeltaL = Math.Abs(Lab1.L - Lab2.L);

                if (DeltaL > Max)
                    Max = DeltaL;
            }

            cmsDeleteTransform(xform);

            if (Max >= 3.0)
            {
                logger.LogWarning("Delta was >= 3.0 ({delta})", Max);

                cmsCloseProfile(hSWOP);
                cmsCloseProfile(hFOGRA);
                cmsCloseProfile(hLab);

                cmsDeleteTransform(swop_lab);
                cmsDeleteTransform(fogra_lab);

                return false;
            }
        }

        using (logger.BeginScope("FOGRA to SWOP"))
        {
            Max = 0;

            xform = cmsCreateTransformTHR(
                DbgThread(),
                hFOGRA,
                TYPE_CMYK_FLT,
                hSWOP,
                TYPE_CMYK_FLT,
                INTENT_PRESERVE_K_ONLY_PERCEPTUAL,
                0)!;

            for (var i = 0; i <= 100; i++)
            {
                CMYK1[0] = 0;
                CMYK1[1] = 0;
                CMYK1[2] = 0;
                CMYK1[3] = i;

                cmsDoTransform(fogra_lab, CMYK1, out CIELab Lab1, 1);
                cmsDoTransform(xform, CMYK1, CMYK2, 1);
                cmsDoTransform(swop_lab, CMYK2, out CIELab Lab2, 1);

                var DeltaL = Math.Abs(Lab1.L - Lab2.L);

                if (DeltaL > Max)
                    Max = DeltaL;
            }

            cmsDeleteTransform(xform);

            if (Max >= 3.0)
            {
                logger.LogWarning("Delta was >= 3.0 ({delta})", Max);

                cmsCloseProfile(hSWOP);
                cmsCloseProfile(hFOGRA);
                cmsCloseProfile(hLab);

                cmsDeleteTransform(swop_lab);
                cmsDeleteTransform(fogra_lab);

                return false;
            }
        }

        return true;
    }

    internal static bool CheckKPlaneBlackPreserving()
    {
        var hSWOP = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test1)!;
        var hFOGRA = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test2)!;
        var CMYK1 = new float[4];
        var CMYK2 = new float[4];

        var hLab = cmsCreateLab4ProfileTHR(DbgThread(), null)!;

        var xform = cmsCreateTransformTHR(
            DbgThread(),
            hSWOP,
            TYPE_CMYK_FLT,
            hFOGRA,
            TYPE_CMYK_FLT,
            INTENT_PERCEPTUAL,
            0)!;

        var swop_lab =
            cmsCreateTransformTHR(DbgThread(), hSWOP, TYPE_CMYK_FLT, hLab, TYPE_Lab_DBL, INTENT_PERCEPTUAL, 0)!;
        var fogra_lab =
            cmsCreateTransformTHR(DbgThread(), hFOGRA, TYPE_CMYK_FLT, hLab, TYPE_Lab_DBL, INTENT_PERCEPTUAL, 0)!;

        var Max = 0.0;

        using (logger.BeginScope("SWOP to FOGRA"))
        {
            for (var i = 0; i <= 100; i++)
            {
                CMYK1[0] = 0;
                CMYK1[1] = 0;
                CMYK1[2] = 0;
                CMYK1[3] = i;

                cmsDoTransform(swop_lab, CMYK1, out CIELab Lab1, 1);
                cmsDoTransform(xform, CMYK1, CMYK2, 1);
                cmsDoTransform(fogra_lab, CMYK2, out CIELab Lab2, 1);

                var DeltaE = lcms2.DeltaE.De76(Lab1, Lab2);

                if (DeltaE > Max)
                    Max = DeltaE;
            }

            cmsDeleteTransform(xform);

            if (Max >= 30.0)
            {
                logger.LogWarning("Delta was >= 30.0 ({delta})", Max);

                cmsCloseProfile(hSWOP);
                cmsCloseProfile(hFOGRA);
                cmsCloseProfile(hLab);

                cmsDeleteTransform(swop_lab);
                cmsDeleteTransform(fogra_lab);

                return false;
            }
        }

        using (logger.BeginScope("FOGRA to SWOP"))
        {
            Max = 0;

            xform = cmsCreateTransformTHR(
                DbgThread(),
                hFOGRA,
                TYPE_CMYK_FLT,
                hSWOP,
                TYPE_CMYK_FLT,
                INTENT_PRESERVE_K_PLANE_PERCEPTUAL,
                0)!;

            for (var i = 0; i <= 100; i++)
            {
                CMYK1[0] = 30;
                CMYK1[1] = 20;
                CMYK1[2] = 10;
                CMYK1[3] = i;

                cmsDoTransform(fogra_lab, CMYK1, out CIELab Lab1, 1);
                cmsDoTransform(xform, CMYK1, CMYK2, 1);
                cmsDoTransform(swop_lab, CMYK2, out CIELab Lab2, 1);

                var DeltaE = lcms2.DeltaE.De76(Lab1, Lab2);

                if (DeltaE > Max)
                    Max = DeltaE;
            }

            cmsDeleteTransform(xform);

            if (Max >= 30.0)
            {
                logger.LogWarning("Delta was >= 30.0 ({delta})", Max);

                cmsCloseProfile(hSWOP);
                cmsCloseProfile(hFOGRA);
                cmsCloseProfile(hLab);

                cmsDeleteTransform(swop_lab);
                cmsDeleteTransform(fogra_lab);

                return false;
            }
        }

        return true;
    }

    internal static bool CheckV4gamma()
    {
        var Lin = new ushort[] { 0, 0xFFFF };
        var g = cmsBuildTabulatedToneCurve16(DbgThread(), 2, Lin);

        var h = cmsOpenProfileFromFileTHR(DbgThread(), "v4gamma.icc", "w");
        if (h is null)
            return false;

        cmsSetProfileVersion(h, 4.3);

        if (!cmsWriteTag(h, Signature.Tag.GrayTRC, g))
            return false;
        cmsCloseProfile(h);

        cmsFreeToneCurve(g);
        remove("v4gamma.icc");
        return true;
    }

    internal static bool CheckBlackPoint()
    {
        var hProfile = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test5)!;
        var Black = cmsDetectDestinationBlackPoint(hProfile, INTENT_RELATIVE_COLORIMETRIC);
        cmsCloseProfile(hProfile);

        hProfile = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test1)!;
        Black = cmsDetectDestinationBlackPoint(hProfile, INTENT_RELATIVE_COLORIMETRIC);
        var Lab = Black.AsLab();
        cmsCloseProfile(hProfile);

        hProfile = cmsOpenProfileFromFileTHR(DbgThread(), "lcms2cmyk.icc", "r")!;
        Black = cmsDetectDestinationBlackPoint(hProfile, INTENT_RELATIVE_COLORIMETRIC);
        Lab = Black.AsLab();
        cmsCloseProfile(hProfile);

        hProfile = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test2)!;
        Black = cmsDetectDestinationBlackPoint(hProfile, INTENT_RELATIVE_COLORIMETRIC);
        Lab = Black.AsLab();
        cmsCloseProfile(hProfile);

        hProfile = cmsOpenProfileFromMemTHR(DbgThread(), TestProfiles.test1)!;
        Black = cmsDetectDestinationBlackPoint(hProfile, INTENT_PERCEPTUAL);
        Lab = Black.AsLab();
        cmsCloseProfile(hProfile);

        return true;
    }

    internal static bool CheckOneTAC(double InkLimit)
    {
        var h = CreateFakeCMYK(InkLimit, true)!;
        cmsSaveProfileToFile(h, "lcmstac.icc");
        cmsCloseProfile(h);

        h = cmsOpenProfileFromFile("lcmstac.icc", "r")!;
        var d = cmsDetectTAC(h);
        cmsCloseProfile(h);

        remove("lcmstac.icc");

        return Math.Abs(d - InkLimit) <= 5;
    }

    internal static bool CheckTAC()
    {
        if (!CheckOneTAC(180))
            return false;
        if (!CheckOneTAC(220))
            return false;
        if (!CheckOneTAC(286))
            return false;
        if (!CheckOneTAC(310))
            return false;
        if (!CheckOneTAC(330))
            return false;
        return true;
    }

    private static string PrintArray<T>(Span<T> values)
    {
        if (values.Length is 0)
            return String.Empty;

        var sb = new StringBuilder();
        foreach (var v in values)
            sb.AppendFormat(", {0}", v);

        return sb.Remove(0, 2).ToString();
    }

    private delegate CIELab LabFn(Span<int> values);

    private static bool CheckOneGBD(LabFn Lab,
                                    params (SteppedRange<int> build, SteppedRange<int>? test)[] vals)
    {
        if (vals.Length is 0)
            return false;

        Span<int> values = stackalloc int[vals.Length];
        var build = vals.Select(v => v.build).ToArray();
        var test = vals.Any(v => !v.test.HasValue)
                       ? null
                       : vals.Select(v => v.test!.Value).ToArray();

        var h = cmsGBDAlloc(DbgThread());
        if (h is null)
        {
            logger.LogWarning("Failed to create GBD object");
            return false;
        }

        using (logger.BeginScope("Filling gamut"))
        {
            if (!@foreach(build, values, String.Format("Failed to add value ({0})", PrintArray(values))))
            {
                cmsGBDFree(h);
                return false;
            }
        }

        using (logger.BeginScope("GBD compute"))
        {
            if (!cmsGBDCompute(h, 0))
            {
                logger.LogWarning("GBD failed to compute");
                cmsGBDFree(h);
                return false;
            }
        }

        using (logger.BeginScope("Checking gamut"))
        {
            if (test is not null && !@foreach(
                    test,
                    values,
                    String.Format("Gamut check failed with value ({0})", PrintArray(values))))
            {
                cmsGBDFree(h);
                return false;
            }
        }

        cmsGBDFree(h);
        return true;

        bool @foreach(ReadOnlySpan<SteppedRange<int>> range, Span<int> values, string warning, int index = 0)
        {
            foreach (var x in vals[index].build)
            {
                values[index] = x;
                if (vals.Length - 1 == index)
                {
                    if (!cmsGBDAddPoint(h, Lab(values)))
                    {
                        logger.LogWarning("{warning}", warning);
                        cmsGBDFree(h);
                        return false;
                    }
                }
                else
                {
                    if (!@foreach(range, values, warning, ++index))
                        return false;
                }
            }

            return true;
        }
    }

    internal static bool CheckGBD()
    {
        var rc = false;

        using (logger.BeginScope("RAW/Lab gamut"))
        {
            rc |= !CheckOneGBD(
                      (v) => new(v[0], v[1], v[2]),
                      (SteppedRange<int>.CreateInclusive(0, 100, 10),
                       SteppedRange<int>.CreateInclusive(10, 90, 25)),
                      (SteppedRange<int>.CreateInclusive(-128, 128, 5),
                       SteppedRange<int>.CreateInclusive(-120, 120, 25)),
                      (SteppedRange<int>.CreateInclusive(-128, 128, 5),
                       SteppedRange<int>.CreateInclusive(-120, 120, 25)));
        }

        using (logger.BeginScope("sRGB gamut"))
        {
            var hsRGB = cmsCreate_sRGBProfile()!;
            var hLab = cmsCreateLab4Profile(null)!;

            var xform = cmsCreateTransform(
                hsRGB,
                TYPE_RGB_8,
                hLab,
                TYPE_Lab_DBL,
                INTENT_RELATIVE_COLORIMETRIC,
                cmsFLAGS_NOCACHE)!;
            cmsCloseProfile(hsRGB);
            cmsCloseProfile(hLab);

            rc |= !CheckOneGBD(
                      (v) =>
                      {
                          cmsDoTransform(xform, v, out CIELab Lab, 1);

                          return Lab;
                      },
                      (SteppedRange<int>.CreateExclusive(0, 256, 5),
                       SteppedRange<int>.CreateExclusive(10, 200, 10)),
                      (SteppedRange<int>.CreateExclusive(0, 256, 5),
                       SteppedRange<int>.CreateExclusive(10, 200, 10)),
                      (SteppedRange<int>.CreateExclusive(0, 256, 5),
                       SteppedRange<int>.CreateExclusive(10, 200, 10)));
            cmsDeleteTransform(xform);
        }

        using (logger.BeginScope("LCh chroma ring"))
        {
            rc |= !CheckOneGBD(
                      v => new CIELCh(70, 60, v[0]).AsLab,
                      (SteppedRange<int>.CreateExclusive(0, 360, 1), null));
        }

        return !rc;
    }

    internal static bool CheckMD5()
    {
        var rc = false;

        using (logger.BeginScope("TestProfiles.test1"))
            rc |= !check(cmsOpenProfileFromMem(TestProfiles.test1)!);

        using (logger.BeginScope("TestProfiles.test2"))
            rc |= !check(cmsOpenProfileFromMem(TestProfiles.test2)!);

        using (logger.BeginScope("TestProfiles.test3"))
            rc |= !check(cmsOpenProfileFromMem(TestProfiles.test3)!);

        using (logger.BeginScope("TestProfiles.test4"))
            rc |= !check(cmsOpenProfileFromMem(TestProfiles.test4)!);

        return !rc;

        static bool check(Profile h)
        {
            Span<byte> profileID1 = stackalloc byte[16];
            Span<byte> profileID2 = stackalloc byte[16];

            cmsGetHeaderProfileID(h, profileID1);
            if (!cmsMD5computeID(h))
            {
                logger.LogWarning("Failed to compute Profile ID");
                return false;
            }
            else
            {
                cmsGetHeaderProfileID(h, profileID2);
                for (var i = 0; i < 16; i++)
                {
                    if (profileID1[i] != profileID2[i])
                        return false;
                }
            }

            return true;
        }
    }

    internal static bool CheckLinking()
    {
        // Create a CLUT based profile
        var h = cmsCreateInkLimitingDeviceLinkTHR(DbgThread(), Signature.Colorspace.Cmyk, 150)!;

        // link a second tag
        cmsLinkTag(h, Signature.Tag.AToB1, Signature.Tag.AToB0);

        // Save the linked devicelink
        if (!cmsSaveProfileToFile(h, "lcms2link.icc"))
            return false;
        cmsCloseProfile(h);

        // Now open the profile and read the pipeline
        h = cmsOpenProfileFromFile("lcms2link.icc", "r");
        if (h is null)
            return false;

        if (cmsReadTag(h, Signature.Tag.AToB1) is not Pipeline pipeline)
            return false;

        pipeline = cmsPipelineDup(pipeline)!;

        // extract stage from pipeline
        cmsPipelineUnlinkStage(pipeline, cmsAT_BEGIN, out var stageBegin);
        cmsPipelineUnlinkStage(pipeline, cmsAT_END, out var stageEnd);
        cmsPipelineInsertStage(pipeline, cmsAT_END, stageEnd);
        cmsPipelineInsertStage(pipeline, cmsAT_BEGIN, stageBegin);

        if (cmsTagLinkedTo(h, Signature.Tag.AToB1) != Signature.Tag.AToB0)
            return false;

        cmsCloseProfile(h);

        return true;
    }

    private static Profile IdentityMatrixProfile(Signature dataSpace)
    {
        Context? ctx = null;
        Span<double> zero = stackalloc double[] { 0, 0, 0 };
        var identityProfile = cmsCreateProfilePlaceholder(ctx)!;

        cmsSetProfileVersion(identityProfile, 4.3);

        cmsSetDeviceClass(identityProfile, Signature.ProfileClass.ColorSpace);
        cmsSetColorSpace(identityProfile, dataSpace);
        cmsSetPCS(identityProfile, Signature.Colorspace.XYZ);

        cmsSetHeaderRenderingIntent(identityProfile, INTENT_RELATIVE_COLORIMETRIC);

        cmsWriteTag(identityProfile, Signature.Tag.MediaWhitePoint, new Box<CIEXYZ>(CIEXYZ.D50));

        var identity = MAT3.Identity.AsArray( /*Context.GetPool<double>(null)*/);

        // build forward transform.... (RGB to PCS)
        var forward = cmsPipelineAlloc(ctx, 3, 3);
        cmsPipelineInsertStage(forward, cmsAT_END, cmsStageAllocMatrix(ctx, 3, 3, identity, zero));
        cmsWriteTag(identityProfile, Signature.Tag.DToB1, forward);

        cmsPipelineFree(forward);

        var reverse = cmsPipelineAlloc(ctx, 3, 3);
        cmsPipelineInsertStage(reverse, cmsAT_END, cmsStageAllocMatrix(ctx, 3, 3, identity, zero));
        cmsWriteTag(identityProfile, Signature.Tag.BToD1, reverse);

        cmsPipelineFree(reverse);

        return identityProfile;
    }

    private static uint TYPE_XYZA_FLT =>
        FLOAT_SH(1) | COLORSPACE_SH(PT_XYZ) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4);

    internal static bool CheckFloatXYZ()
    {
        var xyzProfile = cmsCreateXYZProfile()!;
        Span<float> @in = stackalloc float[4] { 1.0f, 1.0f, 1.0f, 0.5f };
        Span<float> @out = stackalloc float[4];

        // RGB to XYZ
        var input = IdentityMatrixProfile(Signature.Colorspace.Rgb);

        var xform = cmsCreateTransform(input, TYPE_RGB_FLT, xyzProfile, TYPE_XYZ_FLT, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(input);

        cmsDoTransform(xform, @in, @out, 1);
        cmsDeleteTransform(xform);

        if (!IsGoodVal("Float RGB->XYZ", @in[0], @out[0], FLOAT_PRECISION) ||
            !IsGoodVal("Float RGB->XYZ", @in[1], @out[1], FLOAT_PRECISION) ||
            !IsGoodVal("Float RGB->XYZ", @in[2], @out[2], FLOAT_PRECISION))
            return false;

        // XYZ to XYZ
        input = IdentityMatrixProfile(Signature.Colorspace.XYZ);

        xform = cmsCreateTransform(input, TYPE_XYZ_FLT, xyzProfile, TYPE_XYZ_FLT, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(input);

        cmsDoTransform(xform, @in, @out, 1);

        cmsDeleteTransform(xform);

        if (!IsGoodVal("Float XYZ->XYZ", @in[0], @out[0], FLOAT_PRECISION) ||
            !IsGoodVal("Float XYZ->XYZ", @in[1], @out[1], FLOAT_PRECISION) ||
            !IsGoodVal("Float XYZ->XYZ", @in[2], @out[2], FLOAT_PRECISION))
            return true;

        input = IdentityMatrixProfile(Signature.Colorspace.XYZ);

        xform = cmsCreateTransform(
            input,
            TYPE_XYZA_FLT,
            xyzProfile,
            TYPE_XYZA_FLT,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_COPY_ALPHA)!;
        cmsCloseProfile(input);

        cmsDoTransform(xform, @in, @out, 1);

        cmsDeleteTransform(xform);

        if (!IsGoodVal("Float XYZA->XYZA", @in[0], @out[0], FLOAT_PRECISION) ||
            !IsGoodVal("Float XYZA->XYZA", @in[1], @out[1], FLOAT_PRECISION) ||
            !IsGoodVal("Float XYZA->XYZA", @in[2], @out[2], FLOAT_PRECISION) ||
            !IsGoodVal("Float XYZA->XYZA", @in[3], @out[3], FLOAT_PRECISION))
            return false;

        // XYZ to RGB
        input = IdentityMatrixProfile(Signature.Colorspace.Rgb);

        xform = cmsCreateTransform(xyzProfile, TYPE_XYZ_FLT, input, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(input);

        cmsDoTransform(xform, @in, @out, 1);

        cmsDeleteTransform(xform);

        if (!IsGoodVal("Float XYZ->RGB", @in[0], @out[0], FLOAT_PRECISION) ||
            !IsGoodVal("Float XYZ->RGB", @in[1], @out[1], FLOAT_PRECISION) ||
            !IsGoodVal("Float XYZ->RGB", @in[2], @out[2], FLOAT_PRECISION))
            return false;

        // Now the optimizer should remove a stage

        // XYZ to RGB
        input = IdentityMatrixProfile(Signature.Colorspace.Rgb);

        xform = cmsCreateTransform(input, TYPE_RGB_FLT, input, TYPE_RGB_FLT, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        cmsCloseProfile(input);

        cmsDoTransform(xform, @in, @out, 1);

        cmsDeleteTransform(xform);

        if (!IsGoodVal("Float RGB->RGB", @in[0], @out[0], FLOAT_PRECISION) ||
            !IsGoodVal("Float RGB->RGB", @in[1], @out[1], FLOAT_PRECISION) ||
            !IsGoodVal("Float RGB->RGB", @in[2], @out[2], FLOAT_PRECISION))
            return false;

        cmsCloseProfile(xyzProfile);

        return true;
    }

    internal static bool ChecksRGB2LabFLT()
    {
        var hSRGB = cmsCreate_sRGBProfile()!;
        var hLab = cmsCreateLab4Profile(null)!;

        var xform1 = cmsCreateTransform(
            hSRGB,
            TYPE_RGBA_FLT,
            hLab,
            TYPE_LabA_FLT,
            0,
            cmsFLAGS_NOCACHE | cmsFLAGS_NOOPTIMIZE)!;
        var xform2 = cmsCreateTransform(
            hLab,
            TYPE_LabA_FLT,
            hSRGB,
            TYPE_RGBA_FLT,
            0,
            cmsFLAGS_NOCACHE | cmsFLAGS_NOOPTIMIZE)!;

        Span<float> RGBA1 = stackalloc float[4];
        Span<float> RGBA2 = stackalloc float[4];
        Span<float> LabA = stackalloc float[4];

        for (var i = 0; i <= 100; i++)
        {
            RGBA1[0] = i / 100.0F;
            RGBA1[1] = i / 100.0F;
            RGBA1[2] = i / 100.0F;
            RGBA1[3] = 0;

            cmsDoTransform(xform1, RGBA1, LabA, 1);
            cmsDoTransform(xform2, LabA, RGBA2, 1);

            if (!IsGoodVal("Float RGB->RGB", RGBA1[0], RGBA2[0], FLOAT_PRECISION) ||
                !IsGoodVal("Float RGB->RGB", RGBA1[1], RGBA2[1], FLOAT_PRECISION) ||
                !IsGoodVal("Float RGB->RGB", RGBA1[2], RGBA2[2], FLOAT_PRECISION))
                return false;
        }

        cmsDeleteTransform(xform1);
        cmsDeleteTransform(xform2);
        cmsCloseProfile(hSRGB);
        cmsCloseProfile(hLab);

        return true;
    }

    private static double Rec709(double L)
    {
        if (L < 0.018)
            return 4.5 * L;
        else
        {
            var a = 1.099 * Math.Pow(L, 0.45);

            return a - 0.099;
        }
    }

    internal static bool CheckParametricRec709()
    {
        Span<double> @params = stackalloc double[7];

        @params[0] = 0.45;                        /* y */
        @params[1] = Math.Pow(1.099, 1.0 / 0.45); /* a */
        @params[2] = 0.0;                         /* b */
        @params[3] = 4.5;                         /* c */
        @params[4] = 0.018;                       /* d */
        @params[5] = -0.099;                      /* e */
        @params[6] = 0.0;                         /* f */

        var t = cmsBuildParametricToneCurve(null, 5, @params)!;

        for (var i = 0; i < 256; i++)
        {
            var n = i / 255.0F;
            var f1 = (ushort)Math.Floor((255.0 * cmsEvalToneCurveFloat(t, n)) + 0.5);
            var f2 = (ushort)Math.Floor((255.0 * Rec709(i / 255.0)) + 0.5);

            if (f1 != f2)
            {
                cmsFreeToneCurve(t);
                return false;
            }
        }

        cmsFreeToneCurve(t);
        return true;
    }

    private const byte kNumPoints = 10;

    private static float StraightLine(float x) =>
        (float)(0.1 + (0.9 * x));

    private static bool TestCurve(string label, ToneCurve curve, Func<float, float> fn)
    {
        var ok = true;

        for (var i = 0; i < kNumPoints; i++)
        {
            var x = (float)i / ((kNumPoints * 3) - 1);
            var expectedY = fn(x);
            var @out = cmsEvalToneCurveFloat(curve, x);

            if (!IsGoodVal(label, expectedY, @out, FLOAT_PRECISION))
                ok = false;
        }

        return ok;
    }

    internal static bool CheckFloatSamples()
    {
        //var y = GetArray<float>(null, kNumPoints);
        var y = new float[kNumPoints];

        for (var i = 0; i < kNumPoints; i++)
            y[i] = StraightLine((float)i / (kNumPoints - 1));

        var curve = cmsBuildTabulatedToneCurveFloat(null, kNumPoints, y)!;
        var ok = TestCurve("Float Samples", curve, StraightLine);
        cmsFreeToneCurve(curve);
        return ok;
    }

    internal static bool CheckFloatSegments()
    {
        var y = new float[kNumPoints];

        var ok = true;

        // build a segmented curve with a sampled section...
        var Seg = new CurveSegment[3];

        // Initialize segmented curve part up to 0.1
        Seg[0].x0 = -1e22f;      // -infinity
        Seg[0].x1 = 0.1f;
        Seg[0].Type = 6;             // Y = (a * X + b) ^ Gamma + c
        Seg[0].Params = new double[10];
        Seg[0].Params[0] = 1.0f; // gamma
        Seg[0].Params[1] = 0.9f; // a
        Seg[0].Params[2] = 0.0f; // b
        Seg[0].Params[3] = 0.1f; // c
        Seg[0].Params[4] = 0.0f;

        // From zero to 1
        Seg[1].x0 = 0.1f;
        Seg[1].x1 = 0.9f;
        Seg[1].Type = 0;

        Seg[1].nGridPoints = kNumPoints;
        Seg[1].SampledPoints = y;

        for (var i = 0; i < kNumPoints; i++)
        {
            var x = (float)(0.1 + (((float)i / (kNumPoints - 1)) * (0.9 - 0.1)));
            y[i] = StraightLine(x);
        }

        // from 1 to +infinity
        Seg[2].x0 = 0.9f;
        Seg[2].x1 = 1e22f;   // +infinity
        Seg[2].Type = 6;

        Seg[2].Params = new double[10];
        Seg[2].Params[0] = 1.0f;
        Seg[2].Params[1] = 0.9f;
        Seg[2].Params[2] = 0.0f;
        Seg[2].Params[3] = 0.1f;
        Seg[2].Params[4] = 0.0f;

        var curve = cmsBuildSegmentedToneCurve(null, 3, Seg)!;

        ok = TestCurve("Float Segmented Curve", curve, StraightLine);

        cmsFreeToneCurve(curve);

        return ok;
    }

    internal static bool CheckReadRAW()
    {
        var buffer = new byte[37009];
        using (logger.BeginScope("RAW read on on-disk"))
        {
            var hProfile = cmsOpenProfileFromMem(TestProfiles.test1);

            if (hProfile is null)
                return false;

            var tag_size1 = cmsReadRawTag(hProfile, Signature.Tag.Gamut, null, 0);
            var tag_size = cmsReadRawTag(hProfile, Signature.Tag.Gamut, buffer, 37009);

            cmsCloseProfile(hProfile);

            if (tag_size is not 37009)
                return false;

            if (tag_size1 is not 37009)
                return false;
        }

        using (logger.BeginScope("RAW read on in-memory created profiles"))
        {
            var hProfile = cmsCreate_sRGBProfile()!;
            var tag_size1 = cmsReadRawTag(hProfile, Signature.Tag.GreenColorant, null, 0);
            var tag_size = cmsReadRawTag(hProfile, Signature.Tag.GreenColorant, buffer, 20);

            cmsCloseProfile(hProfile);

            if (tag_size is not 20)
                return false;
            if (tag_size1 is not 20)
                return false;
        }

        return true;
    }

    internal static bool CheckMeta()
    {
        /* open file */
        var p = cmsOpenProfileFromMem(TestProfiles.ibm_t61);
        if (p is null)
            return false;

        /* read dictionary, but don't do anything with the value */
        //COMMENT OUT THE NEXT THREE LINES AND IT WORKS FINE!!!
        var dict = cmsReadTag(p, Signature.Tag.Meta);
        if (dict is null)
            return false;

        /* serialize profile to memory */
        var rc = cmsSaveProfileToMem(p, null, out var clen);
        if (!rc)
            return false;

        var data = new byte[(int)clen];
        rc = cmsSaveProfileToMem(p, data, out clen);
        if (!rc)
            return false;

        /* write the memory blob to a file */
        //NOTE: The crash does not happen if cmsSaveProfileToFile() is used */
        var fp = fopen("new.icc", "wb")!;
        fwrite(data, 1, clen, fp);
        fclose(fp);
        //free(data);

        cmsCloseProfile(p);

        /* open newly created file and read metadata */
        p = cmsOpenProfileFromFile("new.icc", "r")!;
        //ERROR: Bad dictionary Name/Value
        //ERROR: Corrupted tag 'meta'
        //test: test.c:59: main: Assertion `dict' failed.
        dict = cmsReadTag(p, Signature.Tag.Meta);
        if (dict is null)
            return false;

        cmsCloseProfile(p);
        return true;
    }

    // Bug on applying null transforms on floating point buffers
    internal static bool CheckFloatNULLxform()
    {
        Span<float> @in = stackalloc float[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Span<float> @out = stackalloc float[10];

        var xform = cmsCreateTransform(
            null,
            TYPE_GRAY_FLT,
            null,
            TYPE_GRAY_FLT,
            INTENT_PERCEPTUAL,
            cmsFLAGS_NULLTRANSFORM);

        if (xform is null)
        {
            logger.LogWarning("Unable to create float null transform");
            return false;
        }

        cmsDoTransform(xform, @in, @out, 10);

        cmsDeleteTransform(xform);
        for (var i = 0; i < 10; i++)
        {
            if (!IsGoodVal("float nullxform", @in[i], @out[i], 0.001))
                return false;
        }

        return true;
    }

    internal static bool CheckRemoveTag()
    {
        var p = cmsCreate_sRGBProfileTHR(null)!;

        /* set value */
        var mlu = cmsMLUalloc(null, 1);
        var ret = cmsMLUsetASCII(mlu, "en"u8, "US"u8, "bar"u8);
        if (!ret)
            return false;

        ret = cmsWriteTag(p, Signature.Tag.DeviceMfgDesc, mlu);
        if (!ret)
            return false;

        cmsMLUfree(mlu);

        /* remove the tag  */
        ret = cmsWriteTag(p, Signature.Tag.DeviceMfgDesc, null);
        if (!ret)
            return false;

        /* THIS EXPLODES */
        cmsCloseProfile(p);
        return true;
    }

    internal static bool CheckMatrixSimplify()
    {
        Span<byte> buf = stackalloc byte[3] { 127, 32, 64 };

        var pIn = cmsCreate_sRGBProfile()!;
        var pOut = cmsOpenProfileFromMem(TestProfiles.ibm_t61)!;
        if (pIn is null || pOut is null)
            return false;

        var t = cmsCreateTransform(pIn, TYPE_RGB_8, pOut, TYPE_RGB_8, INTENT_PERCEPTUAL, 0)!;
        cmsDoTransformStride(t, buf, buf, 1, 1);
        cmsDeleteTransform(t);
        cmsCloseProfile(pIn);
        cmsCloseProfile(pOut);

        return buf[0] == 144 && buf[1] == 0 && buf[2] == 69;
    }

    internal static bool CheckTransformLineStride()
    {
        // Our buffer is formed by 4 RGB8 lines, each line is 2 pixels wide plus a padding of one byte
        ReadOnlySpan<byte> buf1 = stackalloc byte[]
        {
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0,
        };

        // Our buffer2 is formed by 4 RGBA lines, each line is 2 pixels wide plus a padding of one byte
        ReadOnlySpan<byte> buf2 = stackalloc byte[]
        {
            0xff,
            0xff,
            0xff,
            1,
            0xff,
            0xff,
            0xff,
            1,
            0,
            0xff,
            0xff,
            0xff,
            1,
            0xff,
            0xff,
            0xff,
            1,
            0,
            0xff,
            0xff,
            0xff,
            1,
            0xff,
            0xff,
            0xff,
            1,
            0,
            0xff,
            0xff,
            0xff,
            1,
            0xff,
            0xff,
            0xff,
            1,
            0,
        };

        // Our buffer3 is formed by 4 RGBA16 lines, each line is 2 pixels wide plus a padding of two bytes
        ReadOnlySpan<ushort> buf3 = stackalloc ushort[]
        {
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0,
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0,
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0,
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0xffff,
            0xffff,
            0xffff,
            0x0101,
            0,
        };
        var buf3AsBytes = MemoryMarshal.Cast<ushort, byte>(buf3);

        Span<byte> @out = stackalloc byte[1024];

        var pIn = cmsCreate_sRGBProfile()!;
        var pOut = cmsOpenProfileFromMem(TestProfiles.ibm_t61)!;
        if (pIn is null || pOut is null)
            return false;

        var t = cmsCreateTransform(pIn, TYPE_RGB_8, pOut, TYPE_RGB_8, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA)!;

        cmsDoTransformLineStride(t, buf1, @out, 2, 4, 7, 7, 0, 0);
        cmsDeleteTransform(t);

        if (memcmp(@out, buf1) != 0)
        {
            logger.LogWarning("Failed transform line stride on RGB8");
            cmsCloseProfile(pIn);
            cmsCloseProfile(pOut);
            return false;
        }

        @out.Clear();

        t = cmsCreateTransform(pIn, TYPE_RGBA_8, pOut, TYPE_RGBA_8, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA)!;

        cmsDoTransformLineStride(t, buf2, @out, 2, 4, 9, 9, 0, 0);

        cmsDeleteTransform(t);

        if (memcmp(@out, buf2) != 0)
        {
            cmsCloseProfile(pIn);
            cmsCloseProfile(pOut);
            logger.LogWarning("Failed transform line stride on RGBA8");
            return false;
        }

        @out.Clear();

        t = cmsCreateTransform(pIn, TYPE_RGBA_16, pOut, TYPE_RGBA_16, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA)!;

        cmsDoTransformLineStride(t, buf3AsBytes, @out, 2, 4, 18, 18, 0, 0);

        cmsDeleteTransform(t);

        if (memcmp(@out, buf3AsBytes) != 0)
        {
            cmsCloseProfile(pIn);
            cmsCloseProfile(pOut);
            logger.LogWarning("Failed transform line stride on RGBA16");
            return false;
        }

        @out.Clear();

        // From 8 to 16
        t = cmsCreateTransform(pIn, TYPE_RGBA_8, pOut, TYPE_RGBA_16, INTENT_PERCEPTUAL, cmsFLAGS_COPY_ALPHA)!;

        cmsDoTransformLineStride(t, buf2, @out, 2, 4, 9, 18, 0, 0);

        cmsDeleteTransform(t);

        if (memcmp(@out, buf2) != 0)
        {
            cmsCloseProfile(pIn);
            cmsCloseProfile(pOut);
            logger.LogWarning("Failed transform line stride on RGBA16");
            return false;
        }

        cmsCloseProfile(pIn);
        cmsCloseProfile(pOut);

        return true;
    }

    internal static bool CheckPlanar8opt()
    {
        var aboveRGB = Create_AboveRGB()!;
        var sRGB = cmsCreate_sRGBProfile()!;

        var transform = cmsCreateTransform(
            sRGB,
            TYPE_RGB_8_PLANAR,
            aboveRGB,
            TYPE_RGB_8_PLANAR,
            INTENT_PERCEPTUAL,
            0)!;

        cmsDeleteTransform(transform);
        cmsCloseProfile(aboveRGB);
        cmsCloseProfile(sRGB);

        return true;
    }

    private static uint TYPE_RGB_FLT_PLANAR =
        FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(4) | PLANAR_SH(1);

    internal static bool CheckPlanarFloat2int()
    {
        var sRGB = cmsCreate_sRGBProfile()!;

        var transform = cmsCreateTransform(sRGB, TYPE_RGB_FLT_PLANAR, sRGB, TYPE_RGB_16_PLANAR, INTENT_PERCEPTUAL, 0)!;

        ReadOnlySpan<float> input = [ 0.0f, 0.4f, 0.8f, 0.1f, 0.5f, 0.9f, 0.2f, 0.6f, 1.0f, 0.3f, 0.7f, 1.0f ];
        Span<ushort> output = stackalloc ushort[12];

        cmsDoTransform(transform, input, output, 4);

        cmsDeleteTransform(transform);
        cmsCloseProfile(sRGB);

        return true;
    }

    internal static bool CheckSE()
    {
        var input_profile = Create_AboveRGB()!;
        var output_profile = cmsCreate_sRGBProfile()!;

        var tr = cmsCreateTransform(
            input_profile,
            TYPE_RGBA_8,
            output_profile,
            TYPE_RGBA_16_SE,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_COPY_ALPHA)!;

        ReadOnlySpan<byte> rgba = stackalloc byte[4] { 40, 41, 41, 0xfa };
        Span<ushort> @out = stackalloc ushort[4];

        cmsDoTransform(tr, rgba, @out, 1);
        cmsCloseProfile(input_profile);
        cmsCloseProfile(output_profile);
        cmsDeleteTransform(tr);

        if (@out[0] != 0xf622 || @out[1] != 0x7f24 || @out[2] != 0x7f24)
            return false;

        return true;
    }

    internal static bool CheckForgedMPE()
    {
        const uint intent = 0;
        const uint flags = 0;
        Span<byte> output = stackalloc byte[4];

        var srcProfile = cmsOpenProfileFromMem(TestProfiles.bad_mpe);
        if (srcProfile is null)
            return false;

        var dstProfile = cmsCreate_sRGBProfile();
        if (dstProfile is null)
        {
            cmsCloseProfile(srcProfile);
            return false;
        }

        var srcCS = cmsGetColorSpace(srcProfile);
        var nSrcComponents = (uint)cmsChannelsOfColorSpace(srcCS);

        var srcFormat = srcCS == Signature.Colorspace.Lab
                            ? COLORSPACE_SH(PT_Lab) | CHANNELS_SH(nSrcComponents) | BYTES_SH(0)
                            : COLORSPACE_SH(PT_ANY) | CHANNELS_SH(nSrcComponents) | BYTES_SH(1);

        Context.Shared.SetLoggerFactory(BuildNullLogger());

        var hTransform = cmsCreateTransform(
            srcProfile,
            srcFormat,
            dstProfile,
            TYPE_BGR_8,
            intent,
            flags)!;
        cmsCloseProfile(srcProfile);
        cmsCloseProfile(dstProfile);

        Context.Shared.SetLoggerFactory(BuildDebugLogger());

        // Transform should NOT be created
        if (hTransform is null)
            return true;

        // Never should reach here
        if (T_BYTES(srcFormat) == 0)
        {  // 0 means double
            Span<double> input = stackalloc double[128];
            for (var i = 0; i < nSrcComponents; i++)
                input[i] = 0.5f;
            cmsDoTransform(hTransform, input, output, 1);
        }
        else
        {
            Span<double> input = stackalloc double[128];
            for (var i = 0; i < nSrcComponents; i++)
                input[i] = 128;
            cmsDoTransform(hTransform, input, output, 1);
        }

        cmsDeleteTransform(hTransform);

        return false;
    }

    internal static bool CheckProofingIntersection()
    {
        var hnd1 = cmsCreate_sRGBProfile()!;
        var hnd2 = Create_AboveRGB()!;

        var profile_null = cmsCreateNULLProfileTHR(DbgThread())!;
        var transform = cmsCreateProofingTransformTHR(
            DbgThread(),
            hnd1,
            TYPE_RGB_FLT,
            profile_null,
            TYPE_GRAY_FLT,
            hnd2,
            INTENT_ABSOLUTE_COLORIMETRIC,
            INTENT_ABSOLUTE_COLORIMETRIC,
            cmsFLAGS_GAMUTCHECK |
            cmsFLAGS_SOFTPROOFING)!;

        cmsCloseProfile(hnd1);
        cmsCloseProfile(hnd2);
        cmsCloseProfile(profile_null);

        // Failed?
        if (transform is null)
            return false;

        cmsDeleteTransform(transform);
        return true;
    }

    internal static bool CheckEmptyMLUC()
    {
        var context = new Context();
        var white = new CIExyY(0.31271, 0.32902, 1.0);
        var primaries = new CIExyYTRIPLE(
            new(0.640, 0.330, 1.0),
            new(0.300, 0.600, 1.0),
            new(0.150, 0.060, 1.0));

        Span<double> parameters = stackalloc double[10] { 2.6, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
        var toneCurve = cmsBuildParametricToneCurve(context, 1, parameters)!;
        var toneCurves = new ToneCurve[3] { toneCurve, toneCurve, toneCurve };

        var profile = cmsCreateRGBProfileTHR(context, white, primaries, toneCurves)!;

        Context.Shared.SetLoggerFactory(BuildDebugLogger());

        cmsFreeToneCurve(toneCurve);

        // Set an empty copyright tag. This should log an error.
        var mlu = cmsMLUalloc(context, 1);

        cmsMLUsetASCII(mlu, "en"u8, "AU"u8, ""u8);
        cmsMLUsetWide(mlu, "en"u8, "EN"u8, "");
        cmsWriteTag(profile, Signature.Tag.Copyright, mlu);
        cmsMLUfree(mlu);

        // This will cause a crash after setting an empty copyright tag.
        cmsMD5computeID(profile);

        // Cleanup
        cmsCloseProfile(profile);

        return true;
    }

    private static double distance(ReadOnlySpan<ushort> a, ReadOnlySpan<ushort> b)
    {
        double d1 = a[0] - b[0];
        double d2 = a[1] - b[1];
        double d3 = a[2] - b[2];

        return Math.Sqrt((d1 * d1) + (d2 * d2) + (d3 * d3));
    }

    internal static bool Check_sRGB_Rountrips()
    {
        Span<ushort> rgb = stackalloc ushort[3];
        Span<ushort> seed = stackalloc ushort[3];

        var hsRGB = cmsCreate_sRGBProfile()!;
        var hLab = cmsCreateLab4Profile(null)!;

        var hBack = cmsCreateTransform(hLab, TYPE_Lab_DBL, hsRGB, TYPE_RGB_16, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        var hForth = cmsCreateTransform(hsRGB, TYPE_RGB_16, hLab, TYPE_Lab_DBL, INTENT_RELATIVE_COLORIMETRIC, 0)!;

        cmsCloseProfile(hLab);
        cmsCloseProfile(hsRGB);

        var maxErr = 0.0;
        for (var r = 0; r <= 255; r += 16)
        for (var g = 0; g <= 255; g += 16)
        for (var b = 0; b <= 255; b += 16)
        {
            seed[0] = rgb[0] = (ushort)((r << 8) | r);
            seed[1] = rgb[1] = (ushort)((g << 8) | g);
            seed[2] = rgb[2] = (ushort)((b << 8) | b);

            for (var i = 0; i < 50; i++)
            {
                cmsDoTransform(hForth, rgb, out CIELab Lab, 1);
                cmsDoTransform(hBack, Lab, rgb, 1);
            }

            var err = distance(seed, rgb);

            if (err > maxErr)
                maxErr = err;
        }

        cmsDeleteTransform(hBack);
        cmsDeleteTransform(hForth);

        if (maxErr > 20.0)
        {
            logger.LogWarning("Maximum sRGB roundtrip error {maxErr}!", maxErr);
            return false;
        }

        return true;
    }

    internal static bool Check_OkLab()
    {
        var hOkLab = cmsCreate_OkLabProfile(null)!;
        var hXYZ = cmsCreateXYZProfile()!;

        var TYPE_OKLAB_DBL = FLOAT_SH(1) | COLORSPACE_SH(PT_MCH3) | CHANNELS_SH(3) | BYTES_SH(0);

        var xform = cmsCreateTransform(hXYZ, TYPE_XYZ_DBL, hOkLab, TYPE_OKLAB_DBL, INTENT_RELATIVE_COLORIMETRIC, 0)!;
        var xform2 = cmsCreateTransform(hOkLab, TYPE_OKLAB_DBL, hXYZ, TYPE_XYZ_DBL, INTENT_RELATIVE_COLORIMETRIC, 0)!;

        // D50 should be converted to white by PCS definition
        var xyz = new CIEXYZ(0.9642, 1.0, 0.8249);
        cmsDoTransform(xform, xyz, out CIELab okLab, 1);
        cmsDoTransform(xform2, okLab, out CIEXYZ xyz2, 1);

        xyz.X = 1.0;
        xyz.Y = 0.0;
        xyz.Z = 0.0;
        cmsDoTransform(xform, xyz, out okLab, 1);
        cmsDoTransform(xform2, okLab, out xyz2, 1);

        xyz.X = 0.0;
        xyz.Y = 1.0;
        xyz.Z = 0.0;
        cmsDoTransform(xform, xyz, out okLab, 1);
        cmsDoTransform(xform2, okLab, out xyz2, 1);

        xyz.X = 0.0;
        xyz.Y = 0.0;
        xyz.Z = 1.0;
        cmsDoTransform(xform, xyz, out okLab, 1);
        cmsDoTransform(xform2, okLab, out xyz2, 1);

        xyz.X = 0.143046;
        xyz.Y = 0.060610;
        xyz.Z = 0.713913;
        cmsDoTransform(xform, xyz, out okLab, 1);
        cmsDoTransform(xform2, okLab, out xyz2, 1);

        cmsDeleteTransform(xform);
        cmsDeleteTransform(xform2);
        cmsCloseProfile(hOkLab);
        cmsCloseProfile(hXYZ);

        return true;
    }

    internal static bool Check_OkLab2()
    {
        Span<ushort> rgb = stackalloc ushort[3];
        Span<float> lab = stackalloc float[4];

        var labProfile = cmsCreate_OkLabProfile(null)!;
        var rgbProfile = cmsCreate_sRGBProfile()!;

        var TYPE_LABA_F32 = FLOAT_SH(1) | COLORSPACE_SH(PT_MCH3) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4);

        var hBack = cmsCreateTransform(
            labProfile,
            TYPE_LABA_F32,
            rgbProfile,
            TYPE_RGB_16,
            INTENT_RELATIVE_COLORIMETRIC,
            0)!;
        var hForth = cmsCreateTransform(
            rgbProfile,
            TYPE_RGB_16,
            labProfile,
            TYPE_LABA_F32,
            INTENT_RELATIVE_COLORIMETRIC,
            0)!;

        cmsCloseProfile(labProfile);
        cmsCloseProfile(rgbProfile);

        rgb[0] = 0;
        rgb[1] = 0;
        rgb[2] = 65535;

        cmsDoTransform(hForth, rgb, lab, 1);
        cmsDoTransform(hBack, lab, rgb, 1);

        cmsDeleteTransform(hBack);
        cmsDeleteTransform(hForth);

        if (rgb[0] is not 0 || rgb[1] is not 0 || rgb[2] is not 65535)
            return false;

        return true;
    }

    private static Profile? createRgbGamma(double g)
    {
        var D65 = new CIExyY(0.3127, 0.3290, 1.0);
        var Rec709Primaries = new CIExyYTRIPLE(
            new(0.6400, 0.3300, 1.0),
            new(0.3000, 0.6000, 1.0),
            new(0.1500, 0.0600, 1.0));
        var Gamma = new ToneCurve[3];

        Gamma[0] = Gamma[1] = Gamma[2] = cmsBuildGamma(null, g)!;
        if (Gamma[0] is null)
            return null;

        var hRGB = cmsCreateRGBProfile(D65, Rec709Primaries, Gamma)!;
        cmsFreeToneCurve(Gamma[0]);
        return hRGB;
    }

    internal static bool CheckGammaSpaceDetection()
    {
        for (var i = 0.5; i < 3; i += 0.1)
        {
            var hProfile = createRgbGamma(i)!;

            var gamma = cmsDetectRGBProfileGamma(hProfile, 0.01);

            cmsCloseProfile(hProfile);

            if (Math.Abs(gamma - i) > 0.1)
            {
                logger.LogWarning("Failed profile gamma detection of {expected} (got {actual})", i, gamma);
                return false;
            }
        }

        return true;
    }

    internal static bool CheckIntToFloatTransform()
    {
        var hAbove = Create_AboveRGB()!;
        var hsRGB = cmsCreate_sRGBProfile()!;

        var xform = cmsCreateTransform(hAbove, TYPE_RGB_8, hsRGB, TYPE_RGB_DBL, INTENT_PERCEPTUAL, 0)!;

        var rgb8 = new byte[] { 12, 253, 21 };
        var rgbDBL = new double[3];

        cmsCloseProfile(hAbove);
        cmsCloseProfile(hsRGB);

        cmsDoTransform(xform, rgb8, rgbDBL, 1);

        cmsDeleteTransform(xform);

        if (rgbDBL[0] < 0 && rgbDBL[2] < 0)
            return true;

        logger.LogWarning("Unbounded transforms with integer input failed");

        return false;
    }

    internal static bool CheckSaveLinearizationDeviceLink()
    {
        float[] table = [ 0, 0.5f, 1.0f ];

        var tone = cmsBuildTabulatedToneCurveFloat(null, 3, table)!;

        var rgb_curves = new ToneCurve[] { tone, tone, tone };

        var hDeviceLink = cmsCreateLinearizationDeviceLink(Signature.Colorspace.Rgb, rgb_curves)!;

        cmsFreeToneCurve(tone);

        var result = cmsSaveProfileToFile(hDeviceLink, "lin_rgb.icc");

        cmsCloseProfile(hDeviceLink);

        if (!result)
        {
            remove("lin_rgb.icc");
            logger.LogWarning("Couldn't save linearization devicelink");
            return false;
        }

        hDeviceLink = cmsOpenProfileFromFile("lin_rgb.icc", "r");

        if (hDeviceLink is null)
        {
            remove("lin_rgb.icc");
            logger.LogWarning("Couldn't open devicelink");
            return false;
        }

        var xform = cmsCreateTransform(hDeviceLink, TYPE_RGB_8, null, TYPE_RGB_8, INTENT_PERCEPTUAL, 0)!;
        cmsCloseProfile(hDeviceLink);

        Span<byte> rgb_in = stackalloc byte[3];
        Span<byte> rgb_out = stackalloc byte[3];
        for (var i = 0; i < 256; i++)
        {
            rgb_in[0] = rgb_in[1] = rgb_in[2] = (byte)i;
            rgb_out[0] = rgb_out[1] = rgb_out[2] = 0;

            cmsDoTransform(xform, rgb_in, rgb_out, 1);

            if (rgb_in[0] != rgb_out[0] ||
                rgb_in[1] != rgb_out[1] ||
                rgb_in[2] != rgb_out[2])
            {
                remove("lin_rgb.icc");
                logger.LogWarning("Saved devicelink was not working");
            }
        }

        cmsDeleteTransform(xform);
        remove("lin_rgb.icc");

        return true;
    }

    internal static bool CheckInducedCorruption()
    {
        var garbage = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        var hsrgb = cmsCreate_sRGBProfile()!;
        var hLab = cmsCreateLab4Profile(null);

        Context.Shared.SetLoggerFactory(BuildNullLogger());
        cmsWriteRawTag(hsrgb, Signature.Tag.BlueColorant, garbage, (uint)garbage.Length);

        var xform0 = cmsCreateTransform(hsrgb, TYPE_RGB_16, hLab, TYPE_Lab_16, INTENT_RELATIVE_COLORIMETRIC, 0);

        if (xform0 is not null)
            cmsDeleteTransform(xform0);

        cmsCloseProfile(hsrgb);
        cmsCloseProfile(hLab);

        Context.Shared.SetLoggerFactory(BuildDebugLogger());
        return true;
    }
}
