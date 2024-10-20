﻿//---------------------------------------------------------------------------------
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

namespace lcms2.types;

public class Intent(uint intent, string desc, IntentFn fn) : ICloneable
{
    private readonly uint _value = intent;
    public readonly string Description = desc;
    public readonly IntentFn Link = fn;

    public static implicit operator uint(Intent intent) =>
        intent._value;

    object ICloneable.Clone() =>
        Clone();

    public Intent Clone() =>
        new(_value, Description, Link);

    internal static Intent? Search(Context? ContextID, uint Intent) =>
        Context.Get(ContextID).IntentsPlugin.Intents
            .Concat(IntentsList.Default)
            .FirstOrDefault(i => i == Intent);

    public static Pipeline? ICCDefault(Context? ContextID,
                                       uint nProfiles,
                                       ReadOnlySpan<uint> TheIntents,
                                       Profile[] Profiles,
                                       ReadOnlySpan<bool> BPC,
                                       ReadOnlySpan<double> AdaptationStates,
                                       uint dwFlags)   // _cmsDefaultICCintents
    {
        Pipeline? Lut = null, Result;
        Profile Profile;
        MAT3 m;
        VEC3 off;
        Signature ColorSpaceIn, ColorSpaceOut = cmsSigLabData, CurrentColorSpace, ClassSig;
        uint Intent;

        // For safety
        if (nProfiles is 0) return null;

        // Allocate an empty LUT for holding the result. 0 as channel count means 'undefined'
        Result = cmsPipelineAlloc(ContextID, 0, 0);
        if (Result is null) return null;

        CurrentColorSpace = cmsGetColorSpace(Profiles[0]);

        for (var i = 0; i < nProfiles; i++)
        {
            Profile = Profiles[i];
            ClassSig = cmsGetDeviceClass(Profile);
            var isDeviceLink = (uint)ClassSig is cmsSigLinkClass or cmsSigAbstractClass;

            // First profile is used as input unless devicelink or abstract
            var isInput = (i is 0 && !isDeviceLink) ||
                // Else use profile in the input direction if current space is not PCS
                (uint)CurrentColorSpace is not cmsSigXYZData and not cmsSigLabData;

            Intent = TheIntents[i];

            (ColorSpaceIn, ColorSpaceOut) = isInput || isDeviceLink
                ? (cmsGetColorSpace(Profile), cmsGetPCS(Profile))
                : (cmsGetPCS(Profile), cmsGetColorSpace(Profile));

            if (!ColorSpaceIsCompatible(ColorSpaceIn, CurrentColorSpace))
            {
                LogError(ContextID, cmsERROR_COLORSPACE_CHECK, "ColorSpace mismatch");
                goto Error;
            }

            // If devicelink is found, then no custom intent is allowed and we can
            // read the LUT to be applied. Settings don't apply here
            if (isDeviceLink || ((uint)ClassSig is cmsSigNamedColorClass && nProfiles is 1))
            {
                // Get the involved LUT from the profile
                Lut = _cmsReadDevicelinkLUT(Profile, Intent);
                if (Lut is null) goto Error;

                // What about abstract profiles?
                if ((uint)ClassSig is cmsSigAbstractClass && i > 0)
                {
                    if (!ComputeConversion((uint)i, Profiles, Intent, BPC[i], AdaptationStates[i], out m, out off))
                        goto Error;
                }
                else
                {
                    m = MAT3.Identity;
                    off = new(0, 0, 0);
                }

                if (!AddConversion(Result, CurrentColorSpace, ColorSpaceIn, m, off))
                    goto Error;
            }
            else
            {
                if (isInput)
                {
                    // Input direction means non-pcs connection, so proceed like devicelinks
                    Lut = _cmsReadInputLUT(Profile, Intent);
                    if (Lut is null) goto Error;
                }
                else
                {
                    // Output direction means PCS connection. Intent may apply here
                    Lut = _cmsReadOutputLUT(Profile, Intent);
                    if (Lut is null) goto Error;

                    if (!ComputeConversion((uint)i, Profiles, Intent, BPC[i], AdaptationStates[i], out m, out off)) goto Error;
                    if (!AddConversion(Result, CurrentColorSpace, ColorSpaceIn, m, off)) goto Error;
                }
            }

            // Concatenate to the output LUT
            if (!cmsPipelineCat(Result, Lut))
                goto Error;

            cmsPipelineFree(Lut);
            Lut = null;

            // Update current space
            CurrentColorSpace = ColorSpaceOut;
        }

        // Check for non-negatives clip
        if ((dwFlags & cmsFLAGS_NONEGATIVES) is not 0)
        {
            if ((uint)ColorSpaceOut is cmsSigGrayData or cmsSigRgbData or cmsSigCmykData)
            {
                var clip = _cmsStageClipNegatives(Result.ContextID, (uint)cmsChannelsOfColorSpace(ColorSpaceOut));
                if (clip is null) goto Error;

                if (!cmsPipelineInsertStage(Result, StageLoc.AtEnd, clip))
                {
                    //cmsStageFree(clip);
                    goto Error;
                }
            }
        }

        return Result;

    Error:
        if (Lut is not null) cmsPipelineFree(Lut);
        if (Result is not null) cmsPipelineFree(Result);
        return null;
    }

    internal static Pipeline? BlackPreservingKOnly(Context? ContextID,
                                                   uint nProfiles,
                                                   ReadOnlySpan<uint> TheIntents,
                                                   Profile[] Profiles,
                                                   ReadOnlySpan<bool> BPC,
                                                   ReadOnlySpan<double> AdaptationStates,
                                                   uint dwFlags)
    {
        GrayOnlyParams bp = new();
        Pipeline? Result;
        Span<uint> ICCIntents = stackalloc uint[256];
        Stage? CLUT;
        uint nGridPoints, lastProfilePos, preservationProfilesCount;
        Profile hLastProfile;

        // Sanity check
        if (nProfiles is < 1 or > 255) return null;

        // Translate black-preserving intents to ICC ones
        for (var i = 0; i < nProfiles; i++)
            ICCIntents[i] = TranslateNonICCIntents(TheIntents[i]);

        // Trim all CMYK devicelinks at the end
        lastProfilePos = nProfiles - 1;
        hLastProfile = Profiles[lastProfilePos];

        // Skip CMYK->CMYK devicelinks on ending
        while (IsCmykDeviceLink(hLastProfile))
        {
            if (lastProfilePos < 2)
                break;

            hLastProfile = Profiles[--lastProfilePos];
        }

        preservationProfilesCount = lastProfilePos + 1;

        // Check for non-cmyk profiles
        if ((uint)cmsGetColorSpace(Profiles[0]) is not cmsSigCmykData ||
            !((uint)cmsGetColorSpace(hLastProfile) is cmsSigCmykData ||
            (uint)cmsGetDeviceClass(hLastProfile) is cmsSigOutputClass))
        { return ICCDefault(ContextID, nProfiles, ICCIntents, Profiles, BPC, AdaptationStates, dwFlags); }

        // Allocate an empty LUT for holding the result
        Result = cmsPipelineAlloc(ContextID, 4, 4);
        if (Result is null) return null;

        //memset(&bp, 0);

        // Create a LUT holding normal ICC transform
        bp.cmyk2cmyk = ICCDefault(ContextID, preservationProfilesCount, ICCIntents, Profiles, BPC, AdaptationStates, dwFlags);

        if (bp.cmyk2cmyk is null) goto Error;

        // Now, compute the tone curve
        bp.KTone = _cmsBuildKToneCurve(ContextID, 4096, preservationProfilesCount, ICCIntents, Profiles, BPC, AdaptationStates, dwFlags);

        if (bp.KTone is null) goto Error;

        // How many gridpoints are we going to use?
        nGridPoints = _cmsReasonableGridpointsByColorspace(cmsSigCmykData, dwFlags);

        // Create the CLUT. 16 bit
        CLUT = cmsStageAllocCLut16bit(ContextID, nGridPoints, 4, 4, null);
        if (CLUT is null) goto Error;

        // This is the one and only MPE in this LUT
        if (!cmsPipelineInsertStage(Result, StageLoc.AtBegin, CLUT))
            goto Error2;

        // Sample it. We cannot afford pre/post linearization this time.
        if (!cmsStageSampleCLut16bit(CLUT, BlackPreservingGrayOnlySampler, new Box<GrayOnlyParams>(bp), 0))
            goto Error;

        // Insert possible devicelinks at the end
        for (var i = lastProfilePos + 1; i < nProfiles; i++)
        {
            var devlink = _cmsReadDevicelinkLUT(Profiles[i], ICCIntents[(int)i]);
            if (devlink is null) goto Error;

            if (!cmsPipelineCat(Result, devlink))
            {
                cmsPipelineFree(devlink);
                goto Error;
            }
        }

        // Get rid of xform and tone curve
        cmsPipelineFree(bp.cmyk2cmyk);
        cmsFreeToneCurve(bp.KTone);

        return Result;

    Error2:
    Error:
        if (bp.cmyk2cmyk is not null) cmsPipelineFree(bp.cmyk2cmyk);
        if (bp.KTone is not null) cmsFreeToneCurve(bp.KTone);
        if (Result is not null) cmsPipelineFree(Result);
        return null;
    }

    internal static Pipeline? BlackPreservingKPlane(Context? ContextID,
                                                    uint nProfiles,
                                                    ReadOnlySpan<uint> TheIntents,
                                                    Profile[] Profiles,
                                                    ReadOnlySpan<bool> BPC,
                                                    ReadOnlySpan<double> AdaptationStates,
                                                    uint dwFlags)
    {
        Pipeline? Result = null;
        Span<uint> ICCIntents = stackalloc uint[256];
        Stage? CLUT;
        uint nGridPoints, lastProfilePos, preservationProfilesCount;
        Profile? hLastProfile, hLab;

        // Sanity check
        if (nProfiles is < 1 or > 255) return null;

        // Translate black-preserving intents to ICC ones
        for (var i = 0; i < nProfiles; i++)
            ICCIntents[i] = TranslateNonICCIntents(TheIntents[i]);

        // Trim all CMYK devicelinks at the end
        lastProfilePos = nProfiles - 1;
        hLastProfile = Profiles[lastProfilePos];

        // Skip CMYK->CMYK devicelinks on ending
        while (IsCmykDeviceLink(hLastProfile))
        {
            if (lastProfilePos < 2)
                break;

            hLastProfile = Profiles[--lastProfilePos];
        }

        preservationProfilesCount = lastProfilePos + 1;

        // Check for non-cmyk profiles
        if ((uint)cmsGetColorSpace(Profiles[0]) is not cmsSigCmykData ||
            !((uint)cmsGetColorSpace(hLastProfile) is cmsSigCmykData ||
            (uint)cmsGetDeviceClass(hLastProfile) is cmsSigOutputClass))
        { return ICCDefault(ContextID, nProfiles, ICCIntents, Profiles, BPC, AdaptationStates, dwFlags); }

        // Allocate an empty LUT for holding the result
        Result = cmsPipelineAlloc(ContextID, 4, 4);
        if (Result is null) return null;

        //memset(&bp, 0);
        var bp = new PreserveKPlaneParams();

        // We need the input LUT of the last profile, assuming this one is responsible of
        // black generation. This LUT will be searched in inverse order.
        bp.LabK2cmyk = _cmsReadInputLUT(hLastProfile, INTENT_RELATIVE_COLORIMETRIC);
        if (bp.LabK2cmyk is null) goto Cleanup;

        // Get total area coverage (in 0..1 domain)
        bp.MaxTAC = cmsDetectTAC(hLastProfile) / 100.0;
        if (bp.MaxTAC <= 0) goto Cleanup;

        // Create a LUT holding normal ICC transform
        bp.cmyk2cmyk = ICCDefault(ContextID, preservationProfilesCount, ICCIntents, Profiles, BPC, AdaptationStates, dwFlags);
        if (bp.cmyk2cmyk is null) goto Cleanup;

        // Now the tone curve
        bp.KTone = _cmsBuildKToneCurve(ContextID, 4096, preservationProfilesCount, ICCIntents, Profiles, BPC, AdaptationStates, dwFlags)!;
        if (bp.KTone is null) goto Cleanup;

        // To measure the output, Last profile to Lab
        hLab = cmsCreateLab4ProfileTHR(ContextID, null);
        bp.hProofOutput = cmsCreateTransformTHR(
            ContextID,
            hLastProfile,
            CHANNELS_SH(4) | BYTES_SH(2),
            hLab,
            TYPE_Lab_DBL,
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_NOCACHE | cmsFLAGS_NOOPTIMIZE);
        if (bp.hProofOutput is null) goto Cleanup;

        // Same as anterior, but lab in the 0..1 range
        bp.cmyk2Lab = cmsCreateTransformTHR(
            ContextID,
            hLastProfile,
            FLOAT_SH(1) | CHANNELS_SH(4) | BYTES_SH(4),
            hLab,
            FLOAT_SH(1) | CHANNELS_SH(3) | BYTES_SH(4),
            INTENT_RELATIVE_COLORIMETRIC,
            cmsFLAGS_NOCACHE | cmsFLAGS_NOOPTIMIZE);
        if (bp.cmyk2Lab is null) goto Cleanup;
        cmsCloseProfile(hLab);

        // Error estimation (for debug only)
        bp.MaxError = 0;

        // How many gridpoints are we going to use?
        nGridPoints = _cmsReasonableGridpointsByColorspace(cmsSigCmykData, dwFlags);

        CLUT = cmsStageAllocCLut16bit(ContextID, nGridPoints, 4, 4, null);
        if (CLUT is null) goto Cleanup;

        if (!cmsPipelineInsertStage(Result, StageLoc.AtBegin, CLUT))
            goto Cleanup;

        cmsStageSampleCLut16bit(CLUT, BlackPreservingSampler, bp, 0);

        // Insert possible devicelinks at the end
        for (var i = lastProfilePos + 1; i < nProfiles; i++)
        {
            var devlink = _cmsReadDevicelinkLUT(Profiles[i], ICCIntents[(int)i]);
            if (devlink is null) goto Cleanup;

            if (!cmsPipelineCat(Result, devlink))
            {
                cmsPipelineFree(devlink);
                goto Cleanup;
            }
        }

    Cleanup:
        if (bp.cmyk2cmyk is not null) cmsPipelineFree(bp.cmyk2cmyk);
        if (bp.cmyk2Lab is not null) cmsDeleteTransform(bp.cmyk2Lab);
        if (bp.hProofOutput is not null) cmsDeleteTransform(bp.hProofOutput);

        if (bp.KTone is not null) cmsFreeToneCurve(bp.KTone);
        if (bp.LabK2cmyk is not null) cmsPipelineFree(bp.LabK2cmyk);

        return Result;
    }

    private static bool ColorSpaceIsCompatible(Signature a, Signature b)
    {
        var A = (uint)a;
        var B = (uint)b;

        // If they are the same, they are compatible
        if (A == B) return true;

        // Check for MCH4 substitution of CMYK
        if (A is cmsSig4colorData && B is cmsSigCmykData) return true;
        if (A is cmsSigCmykData && B is cmsSig4colorData) return true;

        // Check for XYZ/Lab. Those spaces are interchangeable as they can be computed one from another
        if (A is cmsSigXYZData && B is cmsSigLabData) return true;
        if (A is cmsSigLabData && B is cmsSigXYZData) return true;

        return false;
    }

    private static bool ComputeConversion(uint i,
                                          Profile[] Profiles,
                                          uint Intent,
                                          bool BPC,
                                          double AdaptationState,
                                          out MAT3 m,
                                          out VEC3 off)
    {
        // m and off are set to identity and this is detected later on
        m = MAT3.Identity;
        off = new(0, 0, 0);

        // If intent is abs. colorimetric,
        if (Intent is INTENT_ABSOLUTE_COLORIMETRIC)
        {

            if (!_cmsReadMediaWhitePoint(out var WhitePointIn, Profiles[i - 1])) return false;
            if (!_cmsReadCHAD(out var ChromaticAdaptationMatrixIn, Profiles[i - 1])) return false;

            if (!_cmsReadMediaWhitePoint(out var WhitePointOut, Profiles[i])) return false;
            if (!_cmsReadCHAD(out var ChromaticAdaptationMatrixOut, Profiles[i])) return false;

            m = ComputeAbsoluteIntent(AdaptationState, WhitePointIn, ChromaticAdaptationMatrixIn, WhitePointOut, ChromaticAdaptationMatrixOut);
            if (m.IsNaN)
                return false;
        }
        else
        {
            // Rest of intents may apply BPC
            if (BPC)
            {
                CIEXYZ BlackPointIn, BlackPointOut;

                BlackPointIn = cmsDetectBlackPoint(Profiles[i - 1], Intent);
                BlackPointOut = cmsDetectDestinationBlackPoint(Profiles[i], Intent);

                if (BlackPointIn.IsNaN)
                    BlackPointIn = new(0, 0, 0);

                if (BlackPointOut.IsNaN)
                    BlackPointOut = new(0, 0, 0);

                // If black points are equal, then do nothing
                if (BlackPointIn.X != BlackPointOut.X ||
                    BlackPointIn.Y != BlackPointOut.Y ||
                    BlackPointIn.Z != BlackPointOut.Z)
                {
                    ComputeBlackPointCompensation(BlackPointIn, BlackPointOut, out m, out off);
                }
            }
        }

        // Offset should be adjusted because the encoding. We encode XYZ normalized to 0..1.0,
        // to do that, we divide by MAX_ENCODEABLE_XYZ. The conversion stage goes XYZ -> XYZ so
        // we have first to convert from encoded to XYZ and then convert back to encoded.
        // y = Mx + Off
        // x = x'c
        // y = M x'c + Off
        // y = y'c; y' = y / c
        // y' = (Mx'c + Off) /c = Mx' + (Off / c)

        for (var k = 0; k < 3; k++)
            off[k] /= MAX_ENCODEABLE_XYZ;

        return true;
    }

    private static bool AddConversion(Pipeline Result,
                                      Signature InPCS,
                                      Signature OutPCS,
                                      MAT3 m,
                                      VEC3 off)
    {
        var m_as_dbl = m.AsArray(/*pool*/);
        var off_as_dbl = off.AsArray(/*pool*/);

        // Handle PCS mismatches. A specialized stage is added to the LUT in such case
        switch ((uint)InPCS)
        {
            case cmsSigXYZData:     // Input profile operates in XYZ

                switch ((uint)OutPCS)
                {
                    case cmsSigXYZData:     // XYZ -> XYZ
                        if (!IsEmptyLayer(m, off) &&
                            !cmsPipelineInsertStage(Result, StageLoc.AtEnd, cmsStageAllocMatrix(Result.ContextID, 3, 3, m_as_dbl, off_as_dbl)))
                        { goto Error; }
                        break;

                    case cmsSigLabData:     // XYZ -> Lab
                        if (!IsEmptyLayer(m, off) &&
                            !cmsPipelineInsertStage(Result, StageLoc.AtEnd, cmsStageAllocMatrix(Result.ContextID, 3, 3, m_as_dbl, off_as_dbl)))
                        { goto Error; }
                        if (!cmsPipelineInsertStage(Result, StageLoc.AtEnd, _cmsStageAllocXYZ2Lab(Result.ContextID)))
                            goto Error;
                        break;

                    default:
                        goto Error;   // Colorspace mismatch
                }
                break;

            case cmsSigLabData:     // Input profile operates in Lab

                switch ((uint)OutPCS)
                {
                    case cmsSigXYZData:     // Lab -> XYZ
                        if (!cmsPipelineInsertStage(Result, StageLoc.AtEnd, _cmsStageAllocLab2XYZ(Result.ContextID)))
                            goto Error;
                        if (!IsEmptyLayer(m, off) &&
                            !cmsPipelineInsertStage(Result, StageLoc.AtEnd, cmsStageAllocMatrix(Result.ContextID, 3, 3, m_as_dbl, off_as_dbl)))
                        { goto Error; }
                        break;

                    case cmsSigLabData:     // Lab -> Lab
                        if (!IsEmptyLayer(m, off))
                        {
                            if (!cmsPipelineInsertStage(Result, StageLoc.AtEnd, _cmsStageAllocLab2XYZ(Result.ContextID)) ||
                                !cmsPipelineInsertStage(Result, StageLoc.AtEnd, cmsStageAllocMatrix(Result.ContextID, 3, 3, m_as_dbl, off_as_dbl)) ||
                                !cmsPipelineInsertStage(Result, StageLoc.AtEnd, _cmsStageAllocXYZ2Lab(Result.ContextID)))
                            { goto Error; }
                        }

                        break;

                    default:
                        goto Error;   // Colorspace mismatch
                }
                break;

            // On colorspaces other than PCS, check for same space
            default:
                if (InPCS != OutPCS) goto Error;
                break;
        }

        return true;

    Error:
        return false;
    }

    private static MAT3 ComputeAbsoluteIntent(double AdaptationState,
                                              CIEXYZ WhitePointIn,
                                              MAT3 ChromaticAdaptationMatrixIn,
                                              CIEXYZ WhitePointOut,
                                              MAT3 ChromaticAdaptationMatrixOut)
    {
        // TODO: Follow Marc Mahy's recommendation to check if CHAD is same by using M1*M2 == M2*M1. If so, do nothing.
        // TODO: Add support for ArgyllArts tag

        var Scale = new MAT3(
            x: new(WhitePointIn.X / WhitePointOut.X, 0, 0),
            y: new(0, WhitePointIn.Y / WhitePointOut.Y, 0),
            z: new(0, 0, WhitePointIn.Z / WhitePointOut.Z));

        // Adaptation state
        if (AdaptationState is 1.0)
        {
            // Observer is fully adapted. Keep chromatic adaptation.
            // That is the standard V4 behavior
            return Scale;
        }
        else if (AdaptationState is 0.0)
        {
            var m1 = ChromaticAdaptationMatrixOut;
            var m2 = m1 * Scale;
            // m2 holds CHAD from output white to D50 times abs. col. scaling

            // Observer is not adapted
            var m3 = ChromaticAdaptationMatrixIn;
            var m4 = m3.Inverse;

            return (m4.IsNaN)
                ? MAT3.NaN
                : m2 * m4;
        }
        else
        {
            var m1 = ChromaticAdaptationMatrixIn;
            var m2 = m1.Inverse;
            if (m2.IsNaN)
                return MAT3.NaN;

            var m3 = m2 * Scale;
            // m3 holds CHAD from input white to D50 times abs. col. scaling

            var TempSrc = CHAD2Temp(ChromaticAdaptationMatrixIn);
            var TempDest = CHAD2Temp(ChromaticAdaptationMatrixOut);

            if (TempSrc < 0.0 || TempDest < 0.0)
                return MAT3.NaN; // Something went wrong

            if (Scale.IsIdentity && Math.Abs(TempSrc - TempDest) < 1e-2)
                return MAT3.Identity;

            var Temp = ((1.0 - AdaptationState) * TempDest) + (AdaptationState * TempSrc);

            // Get a CHAD from whatever output temperature to D50. This replaces output CHAD
            var MixedCHAD = Temp2CHAD(Temp);

            return m3 * MixedCHAD;
        }
    }

    private static void ComputeBlackPointCompensation(CIEXYZ BlackPointIn,
                                                      CIEXYZ BlackPointOut,
                                                      out MAT3 m,
                                                      out VEC3 off)
    {
        // Now we need to compute a matrix plus an offset m and of such of
        // [m]*bpin + off = bpout
        // [m]*D50  + off = D50
        //
        // This is a linear scaling in the form ax+b, where
        // a = (bpout - D50) / (bpin - D50)
        // b = - D50* (bpout - bpin) / (bpin - D50)

        var tx = BlackPointIn.X - D50XYZ.X;
        var ty = BlackPointIn.Y - D50XYZ.Y;
        var tz = BlackPointIn.Z - D50XYZ.Z;

        var ax = (BlackPointOut.X - D50XYZ.X) / tx;
        var ay = (BlackPointOut.Y - D50XYZ.Y) / ty;
        var az = (BlackPointOut.Z - D50XYZ.Z) / tz;

        var bx = -D50XYZ.X * (BlackPointOut.X - BlackPointIn.X) / tx;
        var by = -D50XYZ.Y * (BlackPointOut.Y - BlackPointIn.Y) / ty;
        var bz = -D50XYZ.Z * (BlackPointOut.Z - BlackPointIn.Z) / tz;

        m = new(
            x: new(ax, 0, 0),
            y: new(0, ay, 0),
            z: new(0, 0, az));

        off = new(bx, by, bz);
    }

    private static bool IsEmptyLayer(MAT3? m, VEC3? off)
    {
        var diff = 0.0;

        if (m is null && off is null) return true;      // null is allowed as an empty layer
        if (m is null && off is not null) return false; // This is an internal error

        var Ident = MAT3.Identity;

        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
                diff += Math.Abs(m!.Value[x][y] - Ident[x][y]);
        }

        if (off is not null)
        {
            for (var i = 0; i < 3; i++)
                diff += Math.Abs(off.Value[i]);
        }

        return diff < 2e-3;
    }

    private static bool BlackPreservingGrayOnlySampler(ReadOnlySpan<ushort> In,
                                                       Span<ushort> Out,
                                                       object? Cargo)
    {
        if (Cargo is not Box<GrayOnlyParams> bp)
            return false;

        // If going across black only, keep black only
        if (In[0] is 0 && In[1] is 0 && In[2] is 0)
        {
            // TAC does not apply because it is black ink!
            Out[0] = Out[1] = Out[2] = 0;
            Out[3] = cmsEvalToneCurve16(bp.Value.KTone, In[3]);
            return true;
        }

        // Keep normal transform for other colors
        bp.Value.cmyk2cmyk?.Eval16Fn(In, Out, bp.Value.cmyk2cmyk?.Data);
        return true;
    }

    private static bool BlackPreservingSampler(ReadOnlySpan<ushort> In,
                                               Span<ushort> Out,
                                               object? Cargo)
    {
        Span<float> Inf = stackalloc float[4];
        Span<float> Outf = stackalloc float[4];
        Span<float> LabK = stackalloc float[4];
        double SumCMY, SumCMYK, Error, Ratio;
        CIELab ColorimetricLab = new();
        Span<CIELab> BlackPreservingLab = stackalloc CIELab[1];

        if (Cargo is not PreserveKPlaneParams bp)
            return false;

        // Convert from 16 bits to floating point
        for (var i = 0; i < 4; i++)
            Inf[i] = (float)(In[i] / 65535.0);

        // Get the K across Tone curve
        LabK[3] = cmsEvalToneCurveFloat(bp.KTone, Inf[3]);

        // If going across black only, keep black only
        if (In[0] is 0 && In[1] is 0 && In[2] is 0)
        {
            Out[0] = Out[1] = Out[2] = 0;
            Out[3] = _cmsQuickSaturateWord(LabK[3] * 65535.0);
            return true;
        }

        // Try the original transform.
        cmsPipelineEvalFloat(Inf, Outf, bp.cmyk2cmyk);

        // Store a copy of the floating point result into 16-bit
        for (var i = 0; i < 4; i++)
            Out[i] = _cmsQuickSaturateWord(Outf[i] * 65535.0);

        // Maybe K is already ok (mostly on K=0)
        if (MathF.Abs(Outf[3] - LabK[3]) < (3.0 / 65535.0))
            return true;

        // K differs, measure and keep Lab measurement for further usage
        // this is done in relative colorimetric intent
        cmsDoTransform(bp.cmyk2Lab, Outf, LabK, 1);

        // Obtain the corresponding CMY using reverse interpolation
        // (K is fixed in LabK[3])
        if (!cmsPipelineEvalReverseFloat(LabK, Outf, Outf, bp.LabK2cmyk))
        {
            // Cannot find a suitable value, so use colorimetric xform
            // which is already stored in Out[]
            return true;
        }

        // Make sure to pass through K (which is now fixed)
        Outf[3] = LabK[3];

        // Apply TAC if needed
        SumCMY = Outf[0] + Outf[1] + Outf[2];
        SumCMYK = SumCMY + Outf[3];

        Ratio = SumCMYK > bp.MaxTAC
            ? Math.Max(1 - ((SumCMYK - bp.MaxTAC) / SumCMY), 0)
            : 1.0;

        Out[0] = _cmsQuickSaturateWord(Outf[0] * Ratio * 65535.0);  // C
        Out[1] = _cmsQuickSaturateWord(Outf[1] * Ratio * 65535.0);  // M
        Out[2] = _cmsQuickSaturateWord(Outf[2] * Ratio * 65535.0);  // Y
        Out[3] = _cmsQuickSaturateWord(Outf[3] * 65535.0);

        // Estimate the error (this goes 16 bits to Lab DBL)
        cmsDoTransform(bp.hProofOutput, Out, BlackPreservingLab, 1);
        Error = cmsDeltaE(ColorimetricLab, BlackPreservingLab[0]);
        if (Error > bp.MaxError)
            bp.MaxError = Error;

        return true;
    }

    private static uint TranslateNonICCIntents(uint Intent) =>
        Intent switch
        {
            INTENT_PRESERVE_K_ONLY_PERCEPTUAL or
            INTENT_PRESERVE_K_PLANE_PERCEPTUAL =>
                INTENT_PERCEPTUAL,
            INTENT_PRESERVE_K_ONLY_RELATIVE_COLORIMETRIC or
            INTENT_PRESERVE_K_PLANE_RELATIVE_COLORIMETRIC =>
                INTENT_RELATIVE_COLORIMETRIC,
            INTENT_PRESERVE_K_ONLY_SATURATION or
            INTENT_PRESERVE_K_PLANE_SATURATION =>
                INTENT_SATURATION,
            _ => Intent,
        };

    private static bool IsCmykDeviceLink(Profile profile) =>
        cmsGetDeviceClass(profile) == cmsSigLinkClass &&
        cmsGetColorSpace(profile) == cmsSigCmykData;

    private static double CHAD2Temp(MAT3 Chad)
    {
        // Convert D50 across inverse CHAD to get the absolute white point

        var m1 = Chad;
        var m2 = m1.Inverse;
        if (m2.IsNaN)
            return -1.0;

        var s = D50XYZ.AsVec;

        var d = m2.Eval(s);

        var Dest = d.AsXYZ;

        var DestChromaticity = cmsXYZ2xyY(Dest);

        var TempK = cmsTempFromWhitePoint(DestChromaticity);
        if (double.IsNaN(TempK))
            return -1.0;

        return TempK;
    }

    private static MAT3 Temp2CHAD(double Temp)
    {
        var ChromaticityOfWhite = cmsWhitePointFromTemp(Temp);
        var White = cmsxyY2XYZ(ChromaticityOfWhite);
        return ChAd.AdaptationMatrix(null, White, D50XYZ);
    }

    private class PreserveKPlaneParams
    {
        public Pipeline? cmyk2cmyk;
        public Transform? hProofOutput;
        public Transform? cmyk2Lab;
        public ToneCurve KTone;
        public Pipeline? LabK2cmyk;
        public double MaxError;

        //public Transform* hRoundTrip;
        public double MaxTAC;
    }

    private struct GrayOnlyParams
    {
        public Pipeline? cmyk2cmyk;
        public ToneCurve KTone;
    }
}
