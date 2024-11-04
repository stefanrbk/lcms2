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

namespace lcms2;

public static partial class Lcms2
{
    internal static Transform? _cmsChain2Lab(Context? ContextID,
                                             uint nProfiles,
                                             uint InputFormat,
                                             uint OutputFormat,
                                             ReadOnlySpan<uint> Intents,
                                             in Profile[] Profiles,
                                             ReadOnlySpan<bool> BPC,
                                             ReadOnlySpan<double> AdaptationStates,
                                             uint dwFlags)
    {
        var ProfileList = new Profile[256];
        Span<bool> BPCList = stackalloc bool[256];
        Span<double> AdaptationList = stackalloc double[256];
        Span<uint> IntentList = stackalloc uint[256];

        // This is a rather big number and there is no need of dynamic memory
        // since we are adding a profile, 254 + 1 = 255 and this is the limit
        if (nProfiles > 254)
            return null;

        // The output space
        var hLab = cmsCreateLab4ProfileTHR(ContextID, null);
        if (hLab is null)
            return null;

        // Create a copy of parameters
        for (var i = 0; i < nProfiles; i++)
        {
            ProfileList[i] = Profiles[i];
            BPCList[i] = BPC[i];
            AdaptationList[i] = AdaptationStates[i];
            IntentList[i] = Intents[i];
        }

        // Place Lab identity at chain's end.
        ProfileList[nProfiles] = hLab;
        BPCList[(int)nProfiles] = false;
        AdaptationList[(int)nProfiles] = 1.0;
        IntentList[(int)nProfiles] = INTENT_RELATIVE_COLORIMETRIC;

        // Create the transform
        var xform = cmsCreateExtendedTransform(
            ContextID,
            nProfiles + 1,
            ProfileList,
            BPCList,
            IntentList,
            AdaptationList,
            null,
            0,
            InputFormat,
            OutputFormat,
            dwFlags);

        cmsCloseProfile(hLab);
        return xform;
    }

    private static ToneCurve? ComputeKToLstar(Context? ContextID,
                                              uint nPoints,
                                              uint nProfiles,
                                              ReadOnlySpan<uint> Intents,
                                              in Profile[] Profiles,
                                              ReadOnlySpan<bool> BPC,
                                              ReadOnlySpan<double> AdaptationStates,
                                              uint dwFlags)
    {
        Span<float> cmyk = stackalloc float[4];
        Span<CIELab> Lab = stackalloc CIELab[1];

        ToneCurve? @out = null;

        var xform = _cmsChain2Lab(
            ContextID,
            nProfiles,
            TYPE_CMYK_FLT,
            TYPE_Lab_DBL,
            Intents,
            Profiles,
            BPC,
            AdaptationStates,
            dwFlags);
        if (xform is null)
            return null;

        //var SampledPoints = _cmsCalloc<float>(ContextID, nPoints);
        //if (SampledPoints is null) goto Error;
        //var SampledPoints = Context.GetPool<float>(ContextID).Rent((int)nPoints);
        var SampledPoints = new float[nPoints];

        for (var i = 0; i < nPoints; i++)
        {
            cmyk[0] = 0;
            cmyk[1] = 0;
            cmyk[2] = 0;
            cmyk[3] = (float)((i * 100.0) / (nPoints - 1));

            cmsDoTransform<float, CIELab>(xform, cmyk, Lab, 1);
            SampledPoints[i] = (float)(1.0 - (Lab[0].L / 100.0));  // Negate K for easier operation
        }

        @out = cmsBuildTabulatedToneCurveFloat(ContextID, nPoints, SampledPoints);

        //Error:
        cmsDeleteTransform(xform);
        //if (SampledPoints is not null) ReturnArray(ContextID, SampledPoints);

        return @out;
    }

    internal static ToneCurve? _cmsBuildKToneCurve(Context? ContextID,
                                                   uint nPoints,
                                                   uint nProfiles,
                                                   ReadOnlySpan<uint> Intents,
                                                   in Profile[] Profiles,
                                                   ReadOnlySpan<bool> BPC,
                                                   ReadOnlySpan<double> AdaptationStates,
                                                   uint dwFlags)
    {
        // Make sure CMYK -> CMYK
        if (cmsGetColorSpace(Profiles[0]) != Signatures.Colorspace.Cmyk ||
            cmsGetColorSpace(Profiles[nProfiles - 1]) != Signatures.Colorspace.Cmyk)
            return null;

        // Make sure last is an output profile
        if (cmsGetDeviceClass(Profiles[nProfiles - 1]) != Signatures.ProfileClass.Output)
            return null;

        // Create individual curves. BPC works also as each K to L* is
        // computed as a BPC to zero black point in case of L*
        var @in = ComputeKToLstar(ContextID, nPoints, nProfiles - 1, Intents, Profiles, BPC, AdaptationStates, dwFlags);
        if (@in is null)
            return null;

        var @out = ComputeKToLstar(
            ContextID,
            nPoints,
            1,
            Intents[(int)(nProfiles - 1)..],
            Profiles[((int)nProfiles - 1)..],
            BPC[(int)(nProfiles - 1)..],
            AdaptationStates[(int)(nProfiles - 1)..],
            dwFlags);

        if (@out is null)
        {
            cmsFreeToneCurve(@in);
            return null;
        }

        // Build the relationship. This effectively limits the maximum accuracy to 16 bits, but
        // since this is used on black-preserving LUTs, we are not losing accuracy in any case
        var KTone = cmsJoinToneCurve(ContextID, @in, @out, nPoints);

        // Get rid of compontents
        cmsFreeToneCurve(@in);
        cmsFreeToneCurve(@out);

        // Something went wrong...
        if (KTone is null)
            return null;

        // Make sure it is monotonic
        if (!cmsIsToneCurveMonotonic(KTone))
        {
            cmsFreeToneCurve(KTone);
            return null;
        }

        return KTone;
    }

    private struct GamutChain
    {
        public Transform? hInput;
        public Transform? hForward, hReverse;
        public double Threshold;
    }

    private const double ERR_THRESHOLD = 5;

    private static bool GamutSampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        Span<CIELab> LabIn = stackalloc CIELab[2];
        Span<CIELab> LabOut = stackalloc CIELab[2];
        Span<ushort> Proof = stackalloc ushort[cmsMAXCHANNELS];
        Span<ushort> Proof2 = stackalloc ushort[cmsMAXCHANNELS];

        if (Cargo is not Box<GamutChain> t)
            return false;

        // Assume in-gamut by default. NEVER READ, USED FOR DEBUG PURPOSES.
        var ErrorRatio = 1.0;

        // Convert input to Lab
        cmsDoTransform(t.Value.hInput, In, LabIn, 1);

        // converts from PCS to colorant. This always
        // does return in-gamut values
        cmsDoTransform(t.Value.hForward, LabIn, Proof, 1);

        // Now, do the inverse, from colorant to PCS.
        cmsDoTransform(t.Value.hReverse, Proof, LabOut, 1);

        LabIn[1] = LabOut[0];

        // Try again, but this time taking Check as input
        cmsDoTransform(t.Value.hForward, LabOut, Proof2, 1);
        cmsDoTransform(t.Value.hReverse, Proof2, LabOut[1..], 1);

        // Take difference of direct value
        var dE1 = DeltaE.De76(LabIn[0], LabOut[0]);

        // Take difference of converted value
        var dE2 = DeltaE.De76(LabIn[1], LabOut[1]);

        // if dE1 is small and dE2 is small, value is likely to be in gamut
        if (dE1 < t.Value.Threshold && dE2 < t.Value.Threshold)
            Out[0] = 0;
        else
        {
            // if dE1 is small and dE2 is big, undefined. Assume in gamut
            if (dE1 < t.Value.Threshold && dE2 > t.Value.Threshold)
                Out[0] = 0;
            else
            {
                // dE1 is big and dE2 is small, clearly out of gamut
                if (dE1 > t.Value.Threshold && dE2 < t.Value.Threshold)
                    Out[0] = (ushort)_cmsQuickFloor((dE1 - t.Value.Threshold) + 0.5);
                else
                {
                    // dE1 is big and dE2 is also big, could be due to perceptual mapping
                    // so take error ratio
                    ErrorRatio = dE2 is 0 ? dE1 : dE1 / dE2;

                    Out[0] = (ushort)(ErrorRatio > t.Value.Threshold
                                          ? (ushort)_cmsQuickFloor((ErrorRatio - t.Value.Threshold) + 0.5)
                                          : 0);
                }
            }
        }

        return true;
    }

    internal static Pipeline? _cmsCreateGamutCheckPipeline(Context? ContextID,
                                                           Profile[] Profiles,
                                                           ReadOnlySpan<bool> BPC,
                                                           ReadOnlySpan<uint> Intents,
                                                           ReadOnlySpan<double> AdaptationStates,
                                                           uint nGamutPCSposition,
                                                           Profile hGamut)
    {
        var ProfileList = new Profile[256];
        Span<bool> BPCList = stackalloc bool[256];
        Span<double> AdaptationList = stackalloc double[256];
        Span<uint> IntentList = stackalloc uint[256];
        GamutChain Chain = new();
        Pipeline? Gamut;

        //memset(&Chain, 0);

        if (nGamutPCSposition is <= 0 or > 255)
        {
            LogError(ContextID, cmsERROR_RANGE, $"Wrong position of PCS. 1..255 expected, {nGamutPCSposition} found.");
            return null;
        }

        var hLab = cmsCreateLab4ProfileTHR(ContextID, null);
        if (hLab is null)
            return null;

        // The figure of merit. On matrix-shaper profiles, should be almost zero as
        // the conversion is pretty exact. On LUT based profiles, different resolutions
        // of input and output CLUT may result in differences.

        Chain.Threshold = cmsIsMatrixShaper(hGamut) ? 1.0 : ERR_THRESHOLD;

        // Create a copy of parameters
        for (var i = 0; i < nGamutPCSposition; i++)
        {
            ProfileList[i] = Profiles[i];
            BPCList[i] = BPC[i];
            AdaptationList[i] = AdaptationStates[i];
            IntentList[i] = Intents[i];
        }

        // Fill Lab identity
        ProfileList[nGamutPCSposition] = hLab;
        BPCList[(int)nGamutPCSposition] = false;
        AdaptationList[(int)nGamutPCSposition] = 1.0;
        IntentList[(int)nGamutPCSposition] = INTENT_RELATIVE_COLORIMETRIC;

        var ColorSpace = cmsGetColorSpace(hGamut);

        var nChannels = (uint)cmsChannelsOfColorSpace(ColorSpace);
        var nGridpoints = _cmsReasonableGridpointsByColorspace(ColorSpace, cmsFLAGS_HIGHRESPRECALC);
        var dwFormat = CHANNELS_SH(nChannels) | BYTES_SH(2);

        // 16 bits to Lab double
        Chain.hInput = cmsCreateExtendedTransform(
            ContextID,
            nGamutPCSposition + 1,
            ProfileList,
            BPCList,
            IntentList,
            AdaptationList,
            null,
            0,
            dwFormat,
            TYPE_Lab_DBL,
            cmsFLAGS_NOCACHE);

        // Does create the forward step. Lab double to device
        dwFormat = CHANNELS_SH(nChannels) | BYTES_SH(2);
        Chain.hForward = cmsCreateTransformTHR(
            ContextID,
            hLab,
            TYPE_Lab_DBL,
            hGamut,
            dwFormat,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_NOCACHE);

        // Does create the backwards step
        Chain.hReverse = cmsCreateTransformTHR(
            ContextID,
            hGamut,
            dwFormat,
            hLab,
            TYPE_Lab_DBL,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_NOCACHE);

        // All ok?
        if (Chain.hInput is not null && Chain.hForward is not null && Chain.hReverse is not null)
        {
            // Go on, try to compute gamut LUT from PCS. This consist on a single channel containing
            // dE when doing a transform back and forth on the colorimetric intent.

            Gamut = cmsPipelineAlloc(ContextID, 3, 1);
            if (Gamut is not null)
            {
                var CLUT = cmsStageAllocCLut16bit(ContextID, nGridpoints, nChannels, 1, null);
                if (!cmsPipelineInsertStage(Gamut, StageLoc.AtBegin, CLUT))
                {
                    cmsPipelineFree(Gamut);
                    Gamut = null;
                }
                else
                    cmsStageSampleCLut16bit(CLUT, GamutSampler, new Box<GamutChain>(Chain), 0);
            }
        }
        else
            Gamut = null; // Didn't work...

        // Free all needed stuff.
        if (Chain.hInput is not null)
            cmsDeleteTransform(Chain.hInput);
        if (Chain.hForward is not null)
            cmsDeleteTransform(Chain.hForward);
        if (Chain.hReverse is not null)
            cmsDeleteTransform(Chain.hReverse);
        if (hLab is not null)
            cmsCloseProfile(hLab);

        // And return computed hull
        return Gamut;
    }

    private struct TACestimator
    {
        public uint nOutputChans;
        public Transform hRoundTrip;
        public float MaxTAC;
        public float[] MaxInput;
    }

    private static bool EstimateTAC(ReadOnlySpan<ushort> In, Span<ushort> _, object? Cargo)
    {
        if (Cargo is not Box<TACestimator> bp)
            return false;

        Span<float> RoundTrip = stackalloc float[cmsMAXCHANNELS];
        uint i;
        float Sum;

        // Evalutate the xform
        cmsDoTransform(bp.Value.hRoundTrip, In, RoundTrip, 1);

        // All all amounts of ink
        for (Sum = 0, i = 0; i < bp.Value.nOutputChans; i++)
            Sum += RoundTrip[(int)i];

        // If abouve maximum, keep track of input values
        if (Sum > bp.Value.MaxTAC)
        {
            bp.Value.MaxTAC = Sum;

            for (i = 0; i < bp.Value.nOutputChans; i++)
                bp.Value.MaxInput[i] = In[(int)i];
        }

        return true;
    }

    public static double cmsDetectTAC(Profile Profile)
    {
        //var pool = Context.GetPool<float>(Profile.ContextID);
        Box<TACestimator> bp = new(
            new()
            {
                //MaxInput = pool.Rent(cmsMAXCHANNELS)
                MaxInput = new float[cmsMAXCHANNELS],
            });
        Span<uint> GridPoints = stackalloc uint[MAX_INPUT_DIMENSIONS];
        var ContextID = cmsGetProfileContextID(Profile);

        // TAC only works on output profiles
        if (cmsGetDeviceClass(Profile) != Signatures.ProfileClass.Output)
            return 0;

        // Create a fake formatter for result
        var dwFormatter = cmsFormatterForColorspaceOfProfile(Profile, 4, true);

        // Unsupported color space?
        if (dwFormatter is 0)
            return 0;

        bp.Value.nOutputChans = (uint)T_CHANNELS(dwFormatter);
        bp.Value.MaxTAC = 0;  // Initial TAC is 0

        // for safety
        if (bp.Value.nOutputChans >= cmsMAXCHANNELS)
            return 0;

        var hLab = cmsCreateLab4ProfileTHR(ContextID, null);
        if (hLab is null)
            return 0;
        // Setup a roundtrip on perceptual intent in output profile for TAC estimation
        bp.Value.hRoundTrip = cmsCreateTransformTHR(
            ContextID,
            hLab,
            TYPE_Lab_16,
            Profile,
            dwFormatter,
            INTENT_PERCEPTUAL,
            cmsFLAGS_NOOPTIMIZE | cmsFLAGS_NOCACHE)!;

        cmsCloseProfile(hLab);
        if (bp.Value.hRoundTrip is null)
            return 0;

        // For L* we only need black and white. For C* we need many points
        GridPoints[0] = 6;
        GridPoints[1] = 74;
        GridPoints[2] = 74;

        if (!cmsSliceSpace16(3, GridPoints, EstimateTAC, bp))
            bp.Value.MaxTAC = 0;

        cmsDeleteTransform(bp.Value.hRoundTrip);
        //ReturnArray(pool, bp.Value.MaxInput);

        // Results in %
        return bp.Value.MaxTAC;
    }

    public static bool cmsDesaturateLab(ref CIELab Lab, double amax, double amin, double bmax, double bmin)
    {
        // White Luma surface to zero

        if (Lab.L < 0)
        {
            Lab.L = Lab.a = Lab.b = 0;
            return false;
        }

        // Clamp white, DISCARD HIGHLIGHTS. This is done
        // in such way because icc spec doesn't allow the
        // use of L>100 as a highlight means.

        Lab.L = Math.Min(Lab.L, 100);

        // Check out gamut prism, on a, b faces

        if (Lab.a < amin || Lab.a > amax ||
            Lab.b < bmin || Lab.b > bmax)
        {
            CIELCh LCh;

            // Falls outside a, b limits. Transports to LCh space,
            // and then do the clipping

            if (Lab.a is 0)    // Is hue exactly 90?
            {
                // atan will not work, so clamp here
                Lab.b = Lab.b < 0 ? bmin : bmax;
                return true;
            }

            LCh = Lab.AsLCh;

            var slope = Lab.b / Lab.a;
            var h = LCh.h;

            // There are 4 zones;
            (Lab.a, Lab.b) = h switch
                             {
                                 (>= 0 and < 45) or (>= 315 and <= 360) =>
                                     // clip by amax
                                     (amax, amax * slope),
                                 >= 45 and < 135 =>
                                     // clip by bmax
                                     (bmax / slope, bmax),
                                 >= 135 and < 255 =>
                                     // clip by amin
                                     (amin, amin * slope),
                                 >= 255 and < 315 =>
                                     // clip by bmin
                                     (bmin / slope, bmin),
                                 _ =>
                                     (Double.NaN, Double.NaN),
                             };

            if (Double.IsNaN(Lab.a))
            {
                LogError(null, cmsERROR_RANGE, "Invalid angle");
                return false;
            }
        }

        return true;
    }

    private struct Rgb<T> where T : struct
    {
        public T R;
        public T G;
        public T B;
    }

    public static double cmsDetectRGBProfileGamma(Profile Profile, double threshold)

    {
        Span<Rgb<ushort>> rgb = stackalloc Rgb<ushort>[256];
        Span<CIEXYZ> XYZ = stackalloc CIEXYZ[256];
        //var Y_normalized = stackalloc float[256];
        //var pool = Context.GetPool<float>(Profile.ContextID);

        if (cmsGetColorSpace(Profile) != Signatures.Colorspace.Rgb)
            return -1;

        var cl = cmsGetDeviceClass(Profile);
        if (cl != Signatures.ProfileClass.Input &&
            cl != Signatures.ProfileClass.Display &&
            cl != Signatures.ProfileClass.Output &&
            cl != Signatures.ProfileClass.ColorSpace)
            return -1;

        var ContextID = cmsGetProfileContextID(Profile);
        var hXYZ = cmsCreateXYZProfileTHR(ContextID);
        if (hXYZ is null)
            return -1;
        var xform = cmsCreateTransformTHR(
            ContextID,
            Profile,
            TYPE_RGB_16,
            hXYZ,
            TYPE_XYZ_DBL,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_NOOPTIMIZE);

        if (xform is null)  // If not RGB or forward direction is not supported, regret with the previous error
        {
            cmsCloseProfile(hXYZ);
            return -1;
        }

        for (var i = 0; i < 256; i++)
            rgb[i].R = rgb[i].G = rgb[i].B = FROM_8_TO_16((uint)i);

        cmsDoTransform(xform, rgb, XYZ, 256);

        cmsDeleteTransform(xform);
        cmsCloseProfile(hXYZ);

        //var Y_normalized = pool.Rent(256);
        var Y_normalized = new float[256];
        for (var i = 0; i < 256; i++)
            Y_normalized[i] = (float)XYZ[i].Y;

        var Y_curve = cmsBuildTabulatedToneCurveFloat(ContextID, 256, Y_normalized);
        //ReturnArray(pool, Y_normalized);
        if (Y_curve is null)
            return -1;

        var gamma = cmsEstimateGamma(Y_curve, threshold);

        cmsFreeToneCurve(Y_curve);

        return gamma;
    }
}
