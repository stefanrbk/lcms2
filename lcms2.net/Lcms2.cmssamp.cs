﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2022 Marti Maria Saguer
//                2022-2023 Stefan Kewatt
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
//

using lcms2.types;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace lcms2;

public static unsafe partial class Lcms2
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T cmsmin<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => (a < b) ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T cmsmax<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => (a > b) ? a : b;

    private static Transform* CreateRoundtripXForm(Profile* hProfile, uint nIntent)
    {
        var ContextID = cmsGetProfileContextID(hProfile);
        var hLab = cmsCreateLab4ProfileTHR(ContextID, null);
        var BPC = stackalloc bool[4] { false, false, false, false };
        var States = stackalloc double[4] { 1, 1, 1, 1 };
        var hProfiles = stackalloc Profile*[4];
        var Intents = stackalloc uint[4];

        hProfiles[0] = hLab; hProfiles[1] = hProfile; hProfiles[2] = hProfile; hProfiles[3] = hLab;
        Intents[0] = Intents[2] = Intents[3] = INTENT_RELATIVE_COLORIMETRIC; Intents[1] = nIntent;

        var xform = cmsCreateExtendedTransform(ContextID, 4, hProfiles, BPC, Intents,
            States, null, 0, TYPE_Lab_DBL, TYPE_Lab_DBL, cmsFLAGS_NOCACHE | cmsFLAGS_NOOPTIMIZE);

        cmsCloseProfile(hLab);
        return xform;
    }

    private static bool BlackPointAsDarkerColorant(Profile* hInput, uint Intent, CIEXYZ* BlackPoint, uint _)
    {
        ushort* Black;
        uint nChannels;
        CIELab Lab;
        CIEXYZ BlackXYZ;
        var ContextID = cmsGetProfileContextID(hInput);

        // If the profile does not support input direction, assume Black point 0
        if (!cmsIsIntentSupported(hInput, Intent, LCMS_USED_AS_INPUT))
            goto Fail;

        // Create a formatter which has n channels and no floating point
        var dwFormat = cmsFormatterForColorspaceOfProfile(hInput, 2, false);

        // Try to get black by using black colorant
        var Space = cmsGetColorSpace(hInput);

        // This function returns darker colorant in 16 bits for several spaces
        if (!_cmsEndPointsBySpace(Space, null, &Black, &nChannels))
            goto Fail;

        if (nChannels != T_CHANNELS(dwFormat))
            goto Fail;

        // Lab will be used as the output space, but lab2 will avoid recursion
        var hLab = cmsCreateLab2ProfileTHR(ContextID, null);
        if (hLab is null)
            goto Fail;

        // Create the transform
        var xform = cmsCreateTransformTHR(ContextID, hInput, dwFormat, hLab, TYPE_Lab_DBL, Intent, cmsFLAGS_NOOPTIMIZE | cmsFLAGS_NOCACHE);
        cmsCloseProfile(hLab);

        if (xform is null)
            goto Fail;

        // Convert black to Lab
        cmsDoTransform(xform, Black, &Lab, 1);

        // Force it to be neutral, clip to max. L* of 50
        Lab.a = Lab.b = 0;
        Lab.L = cmsmin(Lab.L, 50);

        // Free the resources
        cmsDeleteTransform(xform);

        // Convert from Lab (which is now clipped) to XYZ
        cmsLab2XYZ(null, &BlackXYZ, &Lab);

        if (BlackPoint is not null)
            *BlackPoint = BlackXYZ;

        return true;

    Fail:
        if (BlackPoint is not null)
            BlackPoint->X = BlackPoint->Y = BlackPoint->Z = 0;
        return false;
    }

    private static bool BlackPointUsingPerceptualBlack(CIEXYZ* BlackPoint, Profile* hProfile)
    {
        CIELab LabIn, LabOut;
        CIEXYZ BlackXYZ;

        // Is the intent supported by the profile?
        if (!cmsIsIntentSupported(hProfile, INTENT_PERCEPTUAL, LCMS_USED_AS_INPUT))
            goto Fail;

        var hRoundTrip = CreateRoundtripXForm(hProfile, INTENT_PERCEPTUAL);
        if (hRoundTrip is null)
            goto Fail;

        LabIn.L = LabIn.a = LabIn.b = 0;
        cmsDoTransform(hRoundTrip, &LabIn, &LabOut, 1);

        // Clip Lab to reasonable limits
        LabOut.L = cmsmin(LabOut.L, 50);
        LabOut.a = LabOut.b = 0;

        cmsDeleteTransform(hRoundTrip);

        // Convert it to XYZ
        cmsLab2XYZ(null, &BlackXYZ, &LabOut);

        if (BlackPoint is not null)
            *BlackPoint = BlackXYZ;

        return true;

    Fail:
        if (BlackPoint is not null)
            BlackPoint->X = BlackPoint->Y = BlackPoint->Z = 0;
        return false;
    }

    public static bool cmsDetectBlackPoint(CIEXYZ* BlackPoint, Profile* hProfile, uint Intent, uint dwFlags)
    {
        // Make sure the device class is adequate
        var devClass = cmsGetDeviceClass(hProfile);
        if ((uint)devClass is cmsSigLinkClass or cmsSigAbstractClass or cmsSigNamedColorClass)
            goto Fail;

        // Make sure intent is adequate
        if (Intent is not INTENT_PERCEPTUAL or INTENT_RELATIVE_COLORIMETRIC or INTENT_SATURATION)
            goto Fail;

        // v4 + perceptual & saturation intents does have its own black point, and it is
        // well specified enough to use it. Black point tag is deprecated in V4.
        if ((cmsGetEncodedICCVersion(hProfile) >= 0x04000000) &&
            (Intent is INTENT_PERCEPTUAL or INTENT_SATURATION))
        {
            // Matrix shaper share MRC + perceptual intents
            if (cmsIsMatrixShaper(hProfile))
                return BlackPointAsDarkerColorant(hProfile, INTENT_RELATIVE_COLORIMETRIC, BlackPoint, 0);

            // Get Perceptual black out of v4 profiles. That is fixed for perceptual & saturation intents
            if (BlackPoint is not null)
            {
                BlackPoint->X = cmsPERCEPTUAL_BLACK_X;
                BlackPoint->Y = cmsPERCEPTUAL_BLACK_Y;
                BlackPoint->Z = cmsPERCEPTUAL_BLACK_Z;
            }

            return true;
        }

        // If output profile, discount ink-limiting and that's all
        if (Intent is INTENT_RELATIVE_COLORIMETRIC &&
            ((uint)cmsGetDeviceClass(hProfile) is cmsSigOutputClass) &&
            ((uint)cmsGetColorSpace(hProfile) is cmsSigCmykData))
        { return BlackPointUsingPerceptualBlack(BlackPoint, hProfile); }

        // Nope, compute BP using current intent.
        return BlackPointAsDarkerColorant(hProfile, Intent, BlackPoint, dwFlags);

    Fail:
        if (BlackPoint is not null)
            BlackPoint->X = BlackPoint->Y = BlackPoint->Z = 0;
        return false;
    }

    private static double RootOfLeastSquaresFitQuadraticCurve(int n, double* x, double* y)
    {
        double sum_x = 0, sum_x2 = 0, sum_x3 = 0, sum_x4 = 0;
        double sum_y = 0, sum_yx = 0, sum_yx2 = 0;
        MAT3 m;
        VEC3 v, res;

        if (n < 4) return 0;

        for (var i = 0; i < n; i++)
        {
            var xn = x[i];
            var yn = y[i];

            sum_x += xn;
            sum_x2 += xn * xn;
            sum_x3 += xn * xn * xn;
            sum_x4 += xn * xn * xn * xn;

            sum_y += yn;
            sum_yx += yn * xn;
            sum_yx2 += yn * xn * xn;
        }

        _cmsVEC3init(&((VEC3*)m.v)[0], n, sum_x, sum_x2);
        _cmsVEC3init(&((VEC3*)m.v)[1], sum_x, sum_x2, sum_x3);
        _cmsVEC3init(&((VEC3*)m.v)[2], sum_x2, sum_x3, sum_x4);

        _cmsVEC3init(&v, sum_y, sum_yx, sum_yx2);

        if (!_cmsMAT3solve(&res, &m, &v)) return 0;

        var a = res.n[2];
        var b = res.n[1];
        var c = res.n[0];

        if (Math.Abs(a) < 1e-10)
        {
            return cmsmin(0, cmsmax(50, -c / b));
        }
        else
        {
            var d = b * b - 4.0 * a * c;
            if (d <= 0)
            {
                return 0;
            }
            else
            {
                var rt = (-b + Math.Sqrt(d)) / (2 * a);
                return cmsmax(0, cmsmin(50, rt));
            }
        }
    }

    public static bool cmsDetectDestinationBlackPoint(CIEXYZ* BlackPoint, Profile* hProfile, uint Intent, uint dwFlags)
    {
        Transform* hRoundTrip = null;
        CIELab InitialLab, destLab, Lab;
        var inRamp = stackalloc double[256];
        var outRamp = stackalloc double[256];
        var yRamp = stackalloc double[256];
        var x = stackalloc double[256];
        var y = stackalloc double[256];
        var NearlyStraightMidrange = true;

        // Make sure the device class is adequate
        var devClass = cmsGetDeviceClass(hProfile);
        if ((uint)devClass is cmsSigLinkClass or cmsSigAbstractClass or cmsSigNamedColorClass)
            goto Fail;

        // Make sure intent is adequate
        if (Intent is not INTENT_PERCEPTUAL or INTENT_RELATIVE_COLORIMETRIC or INTENT_SATURATION)
            goto Fail;

        // v4 + perceptual & saturation itents do have their own black point, and it is
        // well specified enough to use it. Black point tag is deprecated in V4.
        if ((cmsGetEncodedICCVersion(hProfile) >= 0x04000000) &&
            (Intent is INTENT_PERCEPTUAL or INTENT_SATURATION))
        {
            // Matrix shaper share MRC & perceptual intents
            if (cmsIsMatrixShaper(hProfile))
                return BlackPointAsDarkerColorant(hProfile, INTENT_RELATIVE_COLORIMETRIC, BlackPoint, 0);

            // Get Perceptual black out of v4 profiles. That is fixed for perceptual & saturation intents
            if (BlackPoint is not null)
            {
                BlackPoint->X = cmsPERCEPTUAL_BLACK_X;
                BlackPoint->Y = cmsPERCEPTUAL_BLACK_Y;
                BlackPoint->Z = cmsPERCEPTUAL_BLACK_Z;
            }
            return true;
        }

        // Check if the profile is lut based and gray, rgb, or cmyk (7.2 in Adobe's document)
        var ColorSpace = cmsGetColorSpace(hProfile);
        if (!cmsIsCLUT(hProfile, Intent, LCMS_USED_AS_OUTPUT) ||
            ((uint)ColorSpace is not cmsSigGrayData or cmsSigRgbData or cmsSigCmykData))
        {
            // In this case, handle as input case
            return cmsDetectBlackPoint(BlackPoint, hProfile, Intent, dwFlags);
        }

        // It is one of the valid cases! Use Adobe algorithm

        // Set a first guess, that should work on good profiles.
        if (Intent is INTENT_RELATIVE_COLORIMETRIC)
        {
            CIEXYZ IniXYZ;

            // calculate initial Lab as source black point
            if (!cmsDetectBlackPoint(&IniXYZ, hProfile, Intent, dwFlags))
                return false;

            // convert the XYZ to Lab
            cmsXYZ2Lab(null, &InitialLab, &IniXYZ);
        }
        else
        {
            // set the initial Lab to zero, that should be the black point for perceptual and saturation
            InitialLab.L = InitialLab.a = InitialLab.b = 0;
        }

        // Step 2
        // ======

        // Create a roundtrip. Define a Transform BT for all x in L*a*b*
        hRoundTrip = CreateRoundtripXForm(hProfile, Intent);
        if (hRoundTrip is null) goto Fail;

        // Compute ramps
        for (var l = 0; l < 256; l++)
        {
            Lab.L = l * 100.0 / 255.0;
            Lab.a = cmsmin(50, cmsmax(-50, InitialLab.a));
            Lab.b = cmsmin(50, cmsmax(-50, InitialLab.b));

            cmsDoTransform(hRoundTrip, &Lab, &destLab, 1);

            inRamp[l] = Lab.L;
            outRamp[l] = destLab.L;
        }

        // Make monotonic
        for (var l = 254; l > 0; --l)
            outRamp[l] = cmsmin(outRamp[l], outRamp[l + 1]);

        // Check
        if (!(outRamp[0] < outRamp[255]))
        {
            cmsDeleteTransform(hRoundTrip);
            goto Fail;
        }

        // Test for mid range straight (only on relative colorimetric)
        NearlyStraightMidrange = true;
        var MinL = outRamp[0];
        var MaxL = outRamp[255];
        if (Intent is INTENT_RELATIVE_COLORIMETRIC)
        {
            for (var l = 0; l < 256; l++)
            {
                if (!((inRamp[l] <= MinL + 0.2 * (MaxL - MinL)) ||
                    (Math.Abs(inRamp[l] - outRamp[l]) < 4.0)))
                { NearlyStraightMidrange = false; }
            }

            // If the mid range is straight (as determined above) then the
            // DestinationBlackPoint shall be the same as initialLab.
            // Otherwise, the DestinationBlackPoint shall be determined
            // using curve fitting.
            if (NearlyStraightMidrange)
            {
                cmsLab2XYZ(null, BlackPoint, &InitialLab);
                cmsDeleteTransform(hRoundTrip);
                return true;
            }
        }

        // curve fitting: the round-trip curve normally looks like a nearly constant section at the black point,
        // with a corner and a nearly straight line to the white point.
        for (var l = 0; l < 256; l++)
            yRamp[l] = (outRamp[l] - MinL) / (MaxL - MinL);

        // find the black point using the least squares error quadratic curve fitting
        var (lo, hi) = (Intent is INTENT_RELATIVE_COLORIMETRIC)
            ? (0.1, 0.5)
            // Perceptual and saturation
            : (0.03, 0.25);

        // Capture shadow points for the fitting.
        var n = 0;
        for (var l = 0; l < 256; l++)
        {
            var ff = yRamp[l];

            if (ff >= lo && ff < hi)
            {
                x[n] = inRamp[l];
                y[n] = yRamp[l];
                n++;
            }
        }

        // No suitable points
        if (n < 3)
        {
            cmsDeleteTransform(hRoundTrip);
            goto Fail;
        }

        // fit and get the vertex of quadratic curve
        Lab.L = cmsmax(0, RootOfLeastSquaresFitQuadraticCurve(n, x, y));
        Lab.a = InitialLab.a;
        Lab.b = InitialLab.b;

        cmsLab2XYZ(null, BlackPoint, &Lab);

        cmsDeleteTransform(hRoundTrip);
        return true;

    Fail:
        if (BlackPoint is not null)
            BlackPoint->X = BlackPoint->Y = BlackPoint->Z = 0;
        return false;
    }
}
