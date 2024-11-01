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

using lcms2.types;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace lcms2;

public static partial class Lcms2
{
    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T cmsmin<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => (a < b) ? a : b;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T cmsmax<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => (a > b) ? a : b;

    private static Transform? CreateRoundtripXForm(Profile Profile, uint nIntent)
    {
        var ContextID = cmsGetProfileContextID(Profile);
        var hLab = cmsCreateLab4ProfileTHR(ContextID, null);
        Span<bool> BPC = stackalloc bool[4] { false, false, false, false };
        Span<double> States = stackalloc double[4] { 1, 1, 1, 1 };
        var Profiles = new Profile[4] { hLab, Profile, Profile, hLab };
        Span<uint> Intents = stackalloc uint[4];

        Intents[0] = Intents[2] = Intents[3] = INTENT_RELATIVE_COLORIMETRIC; Intents[1] = nIntent;

        var xform = cmsCreateExtendedTransform(ContextID, 4, Profiles, BPC, Intents,
            States, null, 0, TYPE_Lab_DBL, TYPE_Lab_DBL, cmsFLAGS_NOCACHE | cmsFLAGS_NOOPTIMIZE);

        cmsCloseProfile(hLab);
        return xform;
    }

    private static CIEXYZ BlackPointAsDarkerColorant(Profile hInput, uint Intent)
    {
        Span<CIELab> Lab = stackalloc CIELab[1];
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
        if (!_cmsEndPointsBySpace(Space, out _, out var Black, out var nChannels))
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
        cmsDoTransform(xform, Black, Lab, 1);

        // Force it to be neutral, check for inconsistencies
        Lab[0].a = Lab[0].b = 0;
        if (Lab[0].L is > 50 or < 0)
            Lab[0].L = 0;

        // Free the resources
        cmsDeleteTransform(xform);

        // Convert from Lab (which is now clipped) to XYZ
        BlackXYZ = Lab[0].AsXYZ();

        return BlackXYZ;

    Fail:
        return new(0, 0, 0);
    }

    private static CIEXYZ BlackPointUsingPerceptualBlack(Profile Profile)
    {
        Span<CIELab> LabIn = stackalloc CIELab[1];
        Span<CIELab> LabOut = stackalloc CIELab[1];
        CIEXYZ BlackXYZ;

        // Is the intent supported by the profile?
        if (!cmsIsIntentSupported(Profile, INTENT_PERCEPTUAL, LCMS_USED_AS_INPUT))
            goto Fail;

        var hRoundTrip = CreateRoundtripXForm(Profile, INTENT_PERCEPTUAL);
        if (hRoundTrip is null)
            goto Fail;

        LabIn[0].L = LabIn[0].a = LabIn[0].b = 0;
        cmsDoTransform(hRoundTrip, LabIn, LabOut, 1);

        // Clip Lab to reasonable limits
        LabOut[0].L = cmsmin(LabOut[0].L, 50);
        LabOut[0].a = LabOut[0].b = 0;

        cmsDeleteTransform(hRoundTrip);

        // Convert it to XYZ
        BlackXYZ = LabOut[0].AsXYZ();

        return BlackXYZ;

    Fail:        
        return CIEXYZ.NaN;
    }

    public static CIEXYZ cmsDetectBlackPoint(Profile Profile, uint Intent)
    {
        // Make sure the device class is adequate
        var devClass = cmsGetDeviceClass(Profile);
        if (devClass == Signature.ProfileClass.Link ||
            devClass == Signature.ProfileClass.Abstract ||
            devClass == Signature.ProfileClass.NamedColor)
        {
            return new(0, 0, 0);
        }

        // Make sure intent is adequate
        if (Intent is not INTENT_PERCEPTUAL and not INTENT_RELATIVE_COLORIMETRIC and not INTENT_SATURATION)
            return new(0, 0, 0);

        // v4 + perceptual & saturation intents does have its own black point, and it is
        // well specified enough to use it. Black point tag is deprecated in V4.
        if ((cmsGetEncodedICCVersion(Profile) >= 0x04000000) &&
            (Intent is INTENT_PERCEPTUAL or INTENT_SATURATION))
        {
            // Matrix shaper share MRC + perceptual intents
            if (cmsIsMatrixShaper(Profile))
                return BlackPointAsDarkerColorant(Profile, INTENT_RELATIVE_COLORIMETRIC);

            // Get Perceptual black out of v4 profiles. That is fixed for perceptual & saturation intents
            return Profile.PerceptualBlack;
        }

        // If output profile, discount ink-limiting and that's all
        if (Intent is INTENT_RELATIVE_COLORIMETRIC &&
            (cmsGetDeviceClass(Profile) == Signature.ProfileClass.Output) &&
            (cmsGetColorSpace(Profile) == Signature.Colorspace.Cmyk))
        { return BlackPointUsingPerceptualBlack(Profile); }

        // Nope, compute BP using current intent.
        return BlackPointAsDarkerColorant(Profile, Intent);
    }

    private static double RootOfLeastSquaresFitQuadraticCurve(int n, ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        double sum_x = 0, sum_x2 = 0, sum_x3 = 0, sum_x4 = 0;
        double sum_y = 0, sum_yx = 0, sum_yx2 = 0;

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

        var m = new MAT3(
            x: new(n, sum_x, sum_x2),
            y: new(sum_x, sum_x2, sum_x3),
            z: new(sum_x2, sum_x3, sum_x4));

        var v = new VEC3(sum_y, sum_yx, sum_yx2);

        var res = m.Solve(v);
        if (res.IsNaN)
            return 0;

        var a = res[2];
        var b = res[1];
        var c = res[0];

        if (Math.Abs(a) < 1e-10)
        {
            if (Math.Abs(b) < 1e-10) return 0;
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
                if (Math.Abs(a) < 1e-10) return 0;

                var rt = (-b + Math.Sqrt(d)) / (2 * a);
                return cmsmax(0, cmsmin(50, rt));
            }
        }
    }

    public static CIEXYZ cmsDetectDestinationBlackPoint(Profile Profile, uint Intent)
    {
        Transform? hRoundTrip = null;
        CIELab InitialLab;
        Span<CIELab> destLab = stackalloc CIELab[1];
        Span<CIELab> Lab = stackalloc CIELab[1];
        Span<double> inRamp = stackalloc double[256];
        Span<double> outRamp = stackalloc double[256];
        Span<double> yRamp = stackalloc double[256];
        Span<double> x = stackalloc double[256];
        Span<double> y = stackalloc double[256];
        var NearlyStraightMidrange = true;

        // Make sure the device class is adequate
        var devClass = cmsGetDeviceClass(Profile);
        if (devClass == Signature.ProfileClass.Link ||
            devClass == Signature.ProfileClass.Abstract ||
            devClass == Signature.ProfileClass.NamedColor)
        {
            goto Fail;
        }

        // Make sure intent is adequate
        if (Intent is not INTENT_PERCEPTUAL and not INTENT_RELATIVE_COLORIMETRIC and not INTENT_SATURATION)
            goto Fail;

        // v4 + perceptual & saturation itents do have their own black point, and it is
        // well specified enough to use it. Black point tag is deprecated in V4.
        if ((cmsGetEncodedICCVersion(Profile) >= 0x04000000) &&
            (Intent is INTENT_PERCEPTUAL or INTENT_SATURATION))
        {
            // Matrix shaper share MRC & perceptual intents
            if (cmsIsMatrixShaper(Profile))
                return BlackPointAsDarkerColorant(Profile, INTENT_RELATIVE_COLORIMETRIC);

            // Get Perceptual black out of v4 profiles. That is fixed for perceptual & saturation intents
            return Profile.PerceptualBlack;
        }

        // Check if the profile is lut based and gray, rgb, or cmyk (7.2 in Adobe's document)
        var ColorSpace = cmsGetColorSpace(Profile);
        if (!cmsIsCLUT(Profile, Intent, LCMS_USED_AS_OUTPUT) ||
            (ColorSpace != Signature.Colorspace.Gray &&
             ColorSpace != Signature.Colorspace.Rgb &&
             ColorSpace != Signature.Colorspace.Cmyk))
        {
            // In this case, handle as input case
            return cmsDetectBlackPoint(Profile, Intent);
        }

        // It is one of the valid cases! Use Adobe algorithm

        // Set a first guess, that should work on good profiles.
        if (Intent is INTENT_RELATIVE_COLORIMETRIC)
        {
            CIEXYZ IniXYZ;

            // calculate initial Lab as source black point
            IniXYZ = cmsDetectBlackPoint(Profile, Intent);
            if (IniXYZ.IsNaN)
                return CIEXYZ.NaN;

            // convert the XYZ to Lab
            InitialLab = IniXYZ.AsLab();
        }
        else
        {
            // set the initial Lab to zero, that should be the black point for perceptual and saturation
            InitialLab.L = InitialLab.a = InitialLab.b = 0;
        }

        // Step 2
        // ======

        // Create a roundtrip. Define a Transform BT for all x in L*a*b*
        hRoundTrip = CreateRoundtripXForm(Profile, Intent);
        if (hRoundTrip is null) goto Fail;

        // Compute ramps
        for (var l = 0; l < 256; l++)
        {
            Lab[0].L = l * 100.0 / 255.0;
            Lab[0].a = cmsmin(50, cmsmax(-50, InitialLab.a));
            Lab[0].b = cmsmin(50, cmsmax(-50, InitialLab.b));

            cmsDoTransform(hRoundTrip, Lab, destLab, 1);

            inRamp[l] = Lab[0].L;
            outRamp[l] = destLab[0].L;
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
                var bp = InitialLab.AsXYZ();
                cmsDeleteTransform(hRoundTrip);
                return bp;
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
        Lab[0].L = cmsmax(0, RootOfLeastSquaresFitQuadraticCurve(n, x, y));
        Lab[0].a = InitialLab.a;
        Lab[0].b = InitialLab.b;

        var BlackPoint = Lab[0].AsXYZ();

        cmsDeleteTransform(hRoundTrip);
        return BlackPoint;

    Fail:
        return CIEXYZ.NaN;
    }
}
