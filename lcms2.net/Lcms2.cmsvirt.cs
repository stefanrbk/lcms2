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
    private static bool SetTextTags(Profile Profile, string Description)
    {
        Mlu? DescriptionMLU, CopyrightMLU;
        var rc = false;
        var ContextID = cmsGetProfileContextID(Profile);
        var en = "en"u8;
        var us = "US"u8;
        var copyright = "No copyright, use freely";

        DescriptionMLU = cmsMLUalloc(ContextID, 1);
        CopyrightMLU = cmsMLUalloc(ContextID, 1);

        if (DescriptionMLU is null || CopyrightMLU is null)
            goto Error;

        if (!cmsMLUsetWide(DescriptionMLU, en, us, Description))
            goto Error;
        if (!cmsMLUsetWide(CopyrightMLU, en, us, copyright))
            goto Error;

        if (!cmsWriteTag(Profile, Signature.Tag.ProfileDescription, DescriptionMLU))
            goto Error;
        if (!cmsWriteTag(Profile, Signature.Tag.Copyright, CopyrightMLU))
            goto Error;

        rc = true;

    Error:
        if (DescriptionMLU is not null)
            cmsMLUfree(DescriptionMLU);
        if (CopyrightMLU is not null)
            cmsMLUfree(CopyrightMLU);
        return rc;
    }

    private static bool SetSeqDescTag(Profile Profile, ReadOnlySpan<byte> Model)
    {
        var rc = false;
        var ContextID = cmsGetProfileContextID(Profile);
        var Seq = cmsAllocProfileSequenceDescription(ContextID, 1);
        var name = "Little CMS"u8;

        if (Seq is null)
            return false;

        Seq.seq[0].deviceMfg = default;
        Seq.seq[0].deviceModel = default;

        Seq.seq[0].attributes = 0;

        Seq.seq[0].technology = default;

        cmsMLUsetASCII(Seq.seq[0].Manufacturer, cmsNoLanguage, cmsNoCountry, name);
        cmsMLUsetASCII(Seq.seq[0].Model, cmsNoLanguage, cmsNoCountry, Model);

        if (!_cmsWriteProfileSequence(Profile, Seq))
            goto Error;

        rc = true;

    Error:
        if (Seq is not null)
            cmsFreeProfileSequenceDescription(Seq);

        return rc;
    }

    public static Profile? cmsCreateRGBProfileTHR(Context? ContextID,
                                                  CIExyY? WhitePoint,
                                                  CIExyYTRIPLE? Primaries,
                                                  ReadOnlySpan<ToneCurve> TransferFunction)
    {
        CIEXYZ WhitePointXYZ;
        MAT3 CHAD;
        //var pool = Context.GetPool<double>(ContextID);
        double[] chad;

        var hICC = cmsCreateProfilePlaceholder(ContextID);
        if (hICC is null)       // can't allocate
            return null;

        cmsSetProfileVersion(hICC, 4.4);

        cmsSetDeviceClass(hICC, Signature.ProfileClass.Display);
        cmsSetColorSpace(hICC, Signature.Colorspace.Rgb);
        cmsSetPCS(hICC, Signature.Colorspace.XYZ);

        cmsSetHeaderRenderingIntent(hICC, INTENT_PERCEPTUAL);

        // Implement profile using following tags:
        //
        //  1 cmsSigProfileDescriptionTag
        //  2 cmsSigMediaWhitePointTag
        //  3 cmsSigRedColorantTag
        //  4 cmsSigGreenColorantTag
        //  5 cmsSigBlueColorantTag
        //  6 cmsSigRedTRCTag
        //  7 cmsSigGreenTRCTag
        //  8 cmsSigBlueTRCTag
        //  9 Chromatic adaptation Tag
        // This conforms a standard RGB DisplayProfile as says ICC, and then I add (As per addendum II)
        // 10 cmsSigChromaticityTag

        if (!SetTextTags(hICC, "RGB built-in"))
            goto Error;

        if (WhitePoint is not null)
        {
            if (!cmsWriteTag(hICC, Signature.Tag.MediaWhitePoint, new Box<CIEXYZ>(CIEXYZ.D50)))
                goto Error;

            WhitePointXYZ = WhitePoint.Value.AsXYZ;
            CHAD = lcms2.CHAD.AdaptationMatrix(null, WhitePointXYZ, CIEXYZ.D50);

            // This is a V4 tag, but many CMM does read and understand it no matter which version
            chad = CHAD.AsArray( /*pool*/);
            if (!cmsWriteTag(hICC, Signature.Tag.ChromaticAdaptation, chad))
                goto Error /*2*/;
            //ReturnArray(pool, chad);

            if (Primaries is not null)
            {
                CIEXYZTRIPLE Colorants;
                CIExyY MaxWhite;
                MAT3 MColorants = new();

                MaxWhite.x = WhitePoint.Value.x;
                MaxWhite.y = WhitePoint.Value.y;
                MaxWhite.Y = 1.0;

                if (!_cmsBuildRGB2XYZtransferMatrix(ref MColorants, MaxWhite, Primaries.Value))
                    goto Error;

                Colorants.Red.X = MColorants.X.X;
                Colorants.Red.Y = MColorants.Y.X;
                Colorants.Red.Z = MColorants.Z.X;

                Colorants.Green.X = MColorants.X.Y;
                Colorants.Green.Y = MColorants.Y.Y;
                Colorants.Green.Z = MColorants.Z.Y;

                Colorants.Blue.X = MColorants.X.Z;
                Colorants.Blue.Y = MColorants.Y.Z;
                Colorants.Blue.Z = MColorants.Z.Z;

                if (!cmsWriteTag(hICC, Signature.Tag.RedColorant, new Box<CIEXYZ>(Colorants.Red)))
                    goto Error;
                if (!cmsWriteTag(hICC, Signature.Tag.BlueColorant, new Box<CIEXYZ>(Colorants.Blue)))
                    goto Error;
                if (!cmsWriteTag(hICC, Signature.Tag.GreenColorant, new Box<CIEXYZ>(Colorants.Green)))
                    goto Error;
            }
        }

        if (!TransferFunction.IsEmpty)
        {
            // Tries to minimize space. Thanks to Richard Hughes for this nice idea
            if (!cmsWriteTag(hICC, Signature.Tag.RedTRC, TransferFunction[0]))
                goto Error;

            if (TransferFunction[1] == TransferFunction[0])
            {
                if (!cmsLinkTag(hICC, Signature.Tag.GreenTRC, Signature.Tag.RedTRC))
                    goto Error;
            }
            else
            {
                if (!cmsWriteTag(hICC, Signature.Tag.GreenTRC, TransferFunction[1]))
                    goto Error;
            }

            if (TransferFunction[2] == TransferFunction[0])
            {
                if (!cmsLinkTag(hICC, Signature.Tag.BlueTRC, Signature.Tag.RedTRC))
                    goto Error;
            }
            else
            {
                if (!cmsWriteTag(hICC, Signature.Tag.BlueTRC, TransferFunction[2]))
                    goto Error;
            }
        }

        if (Primaries is not null &&
            !cmsWriteTag(hICC, Signature.Tag.Chromaticity, new Box<CIExyYTRIPLE>(Primaries.Value)))
            goto Error;

        return hICC;

        //Error2:
        //    ReturnArray(pool, chad);
    Error:
        if (hICC is not null)
            cmsCloseProfile(hICC);
        return null;
    }

    public static Profile? cmsCreateRGBProfile(CIExyY? WhitePoint,
                                               CIExyYTRIPLE? Primaries,
                                               ReadOnlySpan<ToneCurve> TransferFunction) =>
        cmsCreateRGBProfileTHR(null, WhitePoint, Primaries, TransferFunction);

    public static Profile? cmsCreateGrayProfileTHR(Context? ContextID, CIExyY? WhitePoint, ToneCurve TransferFunction)
    {
        CIEXYZ tmp;

        var hICC = cmsCreateProfilePlaceholder(ContextID);
        if (hICC is null)       // can't allocate
            return null;

        cmsSetProfileVersion(hICC, 4.4);

        cmsSetDeviceClass(hICC, Signature.ProfileClass.Display);
        cmsSetColorSpace(hICC, Signature.Colorspace.Gray);
        cmsSetPCS(hICC, Signature.Colorspace.XYZ);
        cmsSetHeaderRenderingIntent(hICC, INTENT_PERCEPTUAL);

        // Implement profile using following tags:
        //
        //  1 cmsSigProfileDescriptionTag
        //  2 cmsSigMediaWhitePointTag
        //  3 cmsSigGrayTRCTag

        // This conforms a standard Gray DisplayProfile

        // Fill-in the tags

        if (!SetTextTags(hICC, "gray built-in"))
            goto Error;

        if (WhitePoint is not null)
        {
            tmp = WhitePoint.Value.AsXYZ;
            if (!cmsWriteTag(hICC, Signature.Tag.MediaWhitePoint, new Box<CIEXYZ>(tmp)))
                goto Error;
        }

        if (TransferFunction is not null && !cmsWriteTag(hICC, Signature.Tag.GrayTRC, TransferFunction))
            goto Error;

        return hICC;

    Error:
        if (hICC is not null)
            cmsCloseProfile(hICC);
        return null;
    }

    public static Profile? cmsCreateGrayProfile(CIExyY? WhitePoint, ToneCurve TransferFunction) =>
        cmsCreateGrayProfileTHR(null, WhitePoint, TransferFunction);

    public static Profile? cmsCreateLinearizationDeviceLinkTHR(Context? ContextID,
                                                               Signature ColorSpace,
                                                               ReadOnlySpan<ToneCurve> TransferFunctions)
    {
        var hICC = cmsCreateProfilePlaceholder(ContextID);
        if (hICC is null)
            return null;

        cmsSetProfileVersion(hICC, 4.4);

        cmsSetDeviceClass(hICC, Signature.ProfileClass.Link);
        cmsSetColorSpace(hICC, ColorSpace);
        cmsSetPCS(hICC, ColorSpace);

        cmsSetHeaderRenderingIntent(hICC, INTENT_PERCEPTUAL);

        // Set up channels
        var nChannels = (uint)cmsChannelsOfColorSpace(ColorSpace);

        // Creates a Pipeline with prelinearization step only
        var Pipeline = cmsPipelineAlloc(ContextID, nChannels, nChannels);
        if (Pipeline is null)
            goto Error;

        // Copy tables to Pipeline
        if (!cmsPipelineInsertStage(
                Pipeline,
                StageLoc.AtBegin,
                cmsStageAllocToneCurves(ContextID, nChannels, TransferFunctions)))
            goto Error;

        // Create tags
        if (!SetTextTags(hICC, "Linearization built-in"))
            goto Error;
        if (!cmsWriteTag(hICC, Signature.Tag.AToB0, Pipeline))
            goto Error;
        if (!SetSeqDescTag(hICC, "Linearization built-in"u8))
            goto Error;

        // Pipeline is already on virtual profile
        cmsPipelineFree(Pipeline);

        // Ok, done
        return hICC;

    Error:
        cmsPipelineFree(Pipeline);
        if (hICC is not null)
            cmsCloseProfile(hICC);
        return null;
    }

    public static Profile? cmsCreateLinearizationDeviceLink(Signature ColorSpace,
                                                            ReadOnlySpan<ToneCurve> TransferFunctions) =>
        cmsCreateLinearizationDeviceLinkTHR(null, ColorSpace, TransferFunctions);

    private static bool InkLimitingSampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        // Ink-limiting algorithm
        //
        //  Sum = C + M + Y + K
        //  If Sum > InkLimit
        //        Ratio= 1 - (Sum - InkLimit) / (C + M + Y)
        //        if Ratio <0
        //              Ratio=0
        //        endif
        //     Else
        //         Ratio=1
        //     endif
        //
        //     C = Ratio * C
        //     M = Ratio * M
        //     Y = Ratio * Y
        //     K: Does not change

        if (Cargo is not Box<double> limit)
            return false;

        var InkLimit = limit * 655.35;

        var SumCMY = (double)In[0] + In[1] + In[2];
        var SumCMYK = SumCMY + In[3];

        var Ratio = Math.Max(0, SumCMYK > InkLimit ? 1 - ((SumCMYK - InkLimit) / SumCMY) : 1);

        Out[0] = _cmsQuickSaturateWord(In[0] * Ratio); // C
        Out[1] = _cmsQuickSaturateWord(In[1] * Ratio); // M
        Out[2] = _cmsQuickSaturateWord(In[2] * Ratio); // Y

        Out[3] = In[3];                                 // K (untouched)

        return true;
    }

    public static Profile? cmsCreateInkLimitingDeviceLinkTHR(Context? ContextID, Signature ColorSpace, double Limit)
    {
        if (ColorSpace != Signature.Colorspace.Cmyk)
        {
            LogError(ContextID, cmsERROR_COLORSPACE_CHECK, "InkLimiting: Only CMYK currently supported");
            return null;
        }

        if (Limit is < 1 or > 400)
        {
            LogError(ContextID, cmsERROR_RANGE, "InkLimiting: Limit should be between 0..400");
            Limit = Math.Max(1, Math.Min(400, Limit));
        }

        var hICC = cmsCreateProfilePlaceholder(ContextID);
        if (hICC is null)          // can't allocate
            return null;

        cmsSetProfileVersion(hICC, 4.4);

        cmsSetDeviceClass(hICC, Signature.ProfileClass.Link);
        cmsSetColorSpace(hICC, ColorSpace);
        cmsSetPCS(hICC, ColorSpace);

        cmsSetHeaderRenderingIntent(hICC, INTENT_PERCEPTUAL);

        // Creates a Pipeline with 3D grid only
        var LUT = cmsPipelineAlloc(ContextID, 4, 4);
        if (LUT is null)
            goto Error;

        var nChannels = (uint)cmsChannelsOfColorSpace(ColorSpace);

        var CLUT = cmsStageAllocCLut16bit(ContextID, 17, nChannels, nChannels, null);
        if (CLUT is null)
            goto Error;

        if (!cmsStageSampleCLut16bit(CLUT, InkLimitingSampler, new Box<double>(Limit), 0))
            goto Error;

        if (!cmsPipelineInsertStage(LUT, StageLoc.AtBegin, _cmsStageAllocIdentityCurves(ContextID, nChannels)) ||
            !cmsPipelineInsertStage(LUT, StageLoc.AtEnd, CLUT) ||
            !cmsPipelineInsertStage(LUT, StageLoc.AtEnd, _cmsStageAllocIdentityCurves(ContextID, nChannels)))
            goto Error;

        // Create tags
        if (!SetTextTags(hICC, "ink-limiting built-in"))
            goto Error;
        if (!cmsWriteTag(hICC, Signature.Tag.AToB0, LUT))
            goto Error;
        if (!SetSeqDescTag(hICC, "ink-limiting built-in"u8))
            goto Error;

        // Pipeline is already on virtual profile
        cmsPipelineFree(LUT);

        // Ok, done
        return hICC;

    Error:
        if (LUT is not null)
            cmsPipelineFree(LUT);

        if (hICC is not null)
            cmsCloseProfile(hICC);

        return null;
    }

    public static Profile? cmsCreateInkLimitingDeviceLink(Signature ColorSpace, double Limit) =>
        cmsCreateInkLimitingDeviceLinkTHR(null, ColorSpace, Limit);

    public static Profile? cmsCreateLab2ProfileTHR(Context? ContextID, CIExyY? WhitePoint)
    {
        Pipeline? LUT = null;

        var Profile = cmsCreateRGBProfileTHR(ContextID, WhitePoint is null ? CIExyY.D50 : WhitePoint, null, null);
        if (Profile is null)
            return null;

        cmsSetProfileVersion(Profile, 2.1);

        cmsSetDeviceClass(Profile, Signature.ProfileClass.Abstract);
        cmsSetColorSpace(Profile, Signature.Colorspace.Lab);
        cmsSetPCS(Profile, Signature.Colorspace.Lab);

        if (!SetTextTags(Profile, "Lab identity build-in"))
            goto Error;

        // An identity LUT is all we need
        LUT = cmsPipelineAlloc(ContextID, 3, 3);
        if (LUT is null)
            goto Error;

        if (!cmsPipelineInsertStage(LUT, StageLoc.AtBegin, _cmsStageAllocIdentityCLut(ContextID, 3)))
            goto Error;

        if (!cmsWriteTag(Profile, Signature.Tag.AToB0, LUT))
            goto Error;
        cmsPipelineFree(LUT);

        return Profile;

    Error:
        if (LUT is not null)
            cmsPipelineFree(LUT);
        if (Profile is not null)
            cmsCloseProfile(Profile);

        return null;
    }

    public static Profile? cmsCreateLab2Profile(CIExyY? WhitePoint) =>
        cmsCreateLab2ProfileTHR(null, WhitePoint);

    public static Profile? cmsCreateLab4ProfileTHR(Context? ContextID, CIExyY? WhitePoint)
    {
        Pipeline? LUT = null;

        var Profile = cmsCreateRGBProfileTHR(ContextID, WhitePoint is null ? CIExyY.D50 : WhitePoint, null, null);
        if (Profile is null)
            return null;

        cmsSetProfileVersion(Profile, 4.4);

        cmsSetDeviceClass(Profile, Signature.ProfileClass.Abstract);
        cmsSetColorSpace(Profile, Signature.Colorspace.Lab);
        cmsSetPCS(Profile, Signature.Colorspace.Lab);

        if (!SetTextTags(Profile, "Lab identity build-in"))
            goto Error;

        // An empty LUT is all we need
        LUT = cmsPipelineAlloc(ContextID, 3, 3);
        if (LUT is null)
            goto Error;

        if (!cmsPipelineInsertStage(LUT, StageLoc.AtBegin, _cmsStageAllocIdentityCurves(ContextID, 3)))
            goto Error;

        if (!cmsWriteTag(Profile, Signature.Tag.AToB0, LUT))
            goto Error;
        cmsPipelineFree(LUT);

        return Profile;

    Error:
        if (LUT is not null)
            cmsPipelineFree(LUT);
        if (Profile is not null)
            cmsCloseProfile(Profile);

        return null;
    }

    public static Profile? cmsCreateLab4Profile(CIExyY? WhitePoint) =>
        cmsCreateLab4ProfileTHR(null, WhitePoint);

    public static Profile? cmsCreateXYZProfileTHR(Context? ContextID)
    {
        Pipeline? LUT = null;

        var Profile = cmsCreateRGBProfileTHR(ContextID, CIExyY.D50, null, null);
        if (Profile is null)
            return null;

        cmsSetProfileVersion(Profile, 4.4);

        cmsSetDeviceClass(Profile, Signature.ProfileClass.Abstract);
        cmsSetColorSpace(Profile, Signature.Colorspace.XYZ);
        cmsSetPCS(Profile, Signature.Colorspace.XYZ);

        if (!SetTextTags(Profile, "XYZ identity build-in"))
            goto Error;

        // An identity LUT is all we need
        LUT = cmsPipelineAlloc(ContextID, 3, 3);
        if (LUT is null)
            goto Error;

        if (!cmsPipelineInsertStage(LUT, StageLoc.AtBegin, _cmsStageAllocIdentityCurves(ContextID, 3)))
            goto Error;

        if (!cmsWriteTag(Profile, Signature.Tag.AToB0, LUT))
            goto Error;
        cmsPipelineFree(LUT);

        return Profile;

    Error:
        if (LUT is not null)
            cmsPipelineFree(LUT);
        if (Profile is not null)
            cmsCloseProfile(Profile);

        return null;
    }

    public static Profile? cmsCreateXYZProfile() =>
        cmsCreateXYZProfileTHR(null);

    private static ToneCurve? Build_sRGBGamma(Context? ContextID)
    {
        Span<double> Parameters = stackalloc double[5] { 2.4, 1 / 1.055, 0.055 / 1.055, 1 / 12.92, 0.04045 };

        return cmsBuildParametricToneCurve(ContextID, 4, Parameters);
    }

    public static Profile? cmsCreate_sRGBProfileTHR(Context? ContextID)
    {
        var D65 = new CIExyY() { x = 0.3127, y = 0.3290, Y = 1.0 };
        var Rec709Primaries = new CIExyYTRIPLE()
        {
            Red = new() { x = 0.6400, y = 0.3300, Y = 1.0 },
            Green = new() { x = 0.3000, y = 0.6000, Y = 1.0 },
            Blue = new() { x = 0.1500, y = 0.0600, Y = 1.0 },
        };
        //var pool = Context.GetPool<ToneCurve>(ContextID);
        //ToneCurve[] Gamma22 = pool.Rent(3);
        var Gamma22 = new ToneCurve[3];

        // cmsWhitePointFromTemp(&D65, 6504);
        Gamma22[0] = Gamma22[1] = Gamma22[2] = Build_sRGBGamma(ContextID)!;
        if (Gamma22[0] is null)
            goto Error;

        var hsRGB = cmsCreateRGBProfileTHR(ContextID, D65, Rec709Primaries, Gamma22);
        cmsFreeToneCurve(Gamma22[0]);
        if (hsRGB is null)
            goto Error;

        if (!SetTextTags(hsRGB, "sRGB build-in"))
        {
            cmsCloseProfile(hsRGB);
            goto Error;
        }

        return hsRGB;

    Error:
        //ReturnArray(pool, Gamma22);
        return null;
    }

    public static Profile? cmsCreate_sRGBProfile() =>
        cmsCreate_sRGBProfileTHR(null);

    public static Profile? cmsCreate_OkLabProfile(Context? ctx)
    {
        var XYZPCS = _cmsStageNormalizeFromXyzFloat(ctx);
        var PCSXYZ = _cmsStageNormalizeToXYZFloat(ctx);

        double[] M_D65_D50 =
        [
            1.047886, 0.022919, -0.050216, 0.029582, 0.990484, -0.017079, -0.009252, 0.015073,  0.751678,
        ];

        double[] M_D50_D65 =
        [
            0.955512609517083,
            -0.023073214184645,
            0.063308961782107,
            -0.028324949364887,
            1.009942432477107,
            0.021054814890112,
            0.012328875695483,
            -0.020535835374141,
            1.330713916450354,
        ];

        var D65toD50 = cmsStageAllocMatrix(ctx, 3, 3, M_D65_D50, null);
        var D50toD65 = cmsStageAllocMatrix(ctx, 3, 3, M_D50_D65, null);

        double[] M_D65_LMS =
        [
            0.8189330101,
            0.3618667424,
            -0.1288597137,
            0.0329845436,
            0.9293118715,
            0.0361456387,
            0.0482003018,
            0.2643662691,
            0.6338517070,
        ];

        double[] M_LMS_D65 =
        [
            1.227013851103521,
            -0.557799980651822,
            0.281256148966468,
            -0.040580178423281,
            1.112256869616830,
            -0.071676678665601,
            -0.076381284505707,
            -0.421481978418013,
            1.586163220440795,
        ];

        var D65toLMS = cmsStageAllocMatrix(ctx, 3, 3, M_D65_LMS, null);
        var LMStoD65 = cmsStageAllocMatrix(ctx, 3, 3, M_LMS_D65, null);

        var CubeRoot = cmsBuildGamma(ctx, 1.0 / 3.0);
        if (CubeRoot is null)
            return null;
        var Cube = cmsBuildGamma(ctx, 3.0);
        if (Cube is null)
            return null;

        ToneCurve[] Roots = [ CubeRoot, CubeRoot, CubeRoot ];
        ToneCurve[] Cubes = [ Cube, Cube, Cube ];

        var NonLinearityFw = cmsStageAllocToneCurves(ctx, 3, Roots);
        var NonLinearityRv = cmsStageAllocToneCurves(ctx, 3, Cubes);

        double[] M_LMSprime_OkLab =
        [
            0.2104542553,
            0.7936177850,
            -0.0040720468,
            1.9779984951,
            -2.4285922050,
            0.4505937099,
            0.0259040371,
            0.7827717662,
            -0.8086757660,
        ];

        double[] M_OkLab_LMSprime =
        [
            0.999999998450520,
            0.396337792173768,
            0.215803758060759,
            1.000000008881761,
            -0.105561342323656,
            -0.063854174771706,
            1.000000054672411,
            -0.089484182094966,
            -1.291485537864092,
        ];

        var LMSprime_OkLab = cmsStageAllocMatrix(ctx, 3, 3, M_LMSprime_OkLab, null);
        var OkLab_LMSprime = cmsStageAllocMatrix(ctx, 3, 3, M_OkLab_LMSprime, null);

        var AToB = cmsPipelineAlloc(ctx, 3, 3);
        var BToA = cmsPipelineAlloc(ctx, 3, 3);

        var hProfile = cmsCreateProfilePlaceholder(ctx);
        if (hProfile is null)            // can't allocate
            goto error;

        cmsSetProfileVersion(hProfile, 4.4);

        cmsSetDeviceClass(hProfile, Signature.ProfileClass.ColorSpace);
        cmsSetColorSpace(hProfile, Signature.Colorspace.Color3);
        cmsSetPCS(hProfile, Signature.Colorspace.XYZ);

        cmsSetHeaderRenderingIntent(hProfile, INTENT_RELATIVE_COLORIMETRIC);

        /**
        * Conversion PCS (XYZ/D50) to OkLab
        */
        if (!cmsPipelineInsertStage(BToA, StageLoc.AtEnd, PCSXYZ))
            goto error;
        if (!cmsPipelineInsertStage(BToA, StageLoc.AtEnd, D50toD65))
            goto error;
        if (!cmsPipelineInsertStage(BToA, StageLoc.AtEnd, D65toLMS))
            goto error;
        if (!cmsPipelineInsertStage(BToA, StageLoc.AtEnd, NonLinearityFw))
            goto error;
        if (!cmsPipelineInsertStage(BToA, StageLoc.AtEnd, LMSprime_OkLab))
            goto error;

        if (!cmsWriteTag(hProfile, Signature.Tag.BToA0, BToA))
            goto error;

        if (!cmsPipelineInsertStage(AToB, StageLoc.AtEnd, OkLab_LMSprime))
            goto error;
        if (!cmsPipelineInsertStage(AToB, StageLoc.AtEnd, NonLinearityRv))
            goto error;
        if (!cmsPipelineInsertStage(AToB, StageLoc.AtEnd, LMStoD65))
            goto error;
        if (!cmsPipelineInsertStage(AToB, StageLoc.AtEnd, D65toD50))
            goto error;
        if (!cmsPipelineInsertStage(AToB, StageLoc.AtEnd, XYZPCS))
            goto error;

        if (!cmsWriteTag(hProfile, Signature.Tag.AToB0, AToB))
            goto error;

        cmsPipelineFree(BToA);
        cmsPipelineFree(AToB);

        cmsFreeToneCurve(CubeRoot);
        cmsFreeToneCurve(Cube);

        return hProfile;

    error:
        cmsPipelineFree(BToA);
        cmsPipelineFree(AToB);

        cmsFreeToneCurve(CubeRoot);
        cmsFreeToneCurve(Cube);
        cmsCloseProfile(hProfile);

        return null;
    }

    private struct BCHSWADJUSTS
    {
        public double Brightness, Contrast, Hue, Saturation;
        public bool lAdjustWP;
        public CIEXYZ WPsrc, WPdest;
    }

    private static bool bchswSampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        CIELab LabIn, LabOut;
        CIELCh LChIn, LChOut;
        CIEXYZ XYZ;

        if (Cargo is not Box<BCHSWADJUSTS> bchsw)
            return false;

        LabIn = In.LabEncodedToFloat();

        LChIn = LabIn.AsLCh;

        // Do some adjusts on LCh

        LChOut.L = (LChIn.L * bchsw.Value.Contrast) + bchsw.Value.Brightness;
        LChOut.C = LChIn.C + bchsw.Value.Saturation;
        LChOut.h = LChIn.h + bchsw.Value.Hue;

        LabOut = LChOut.AsLab;

        // Move white point in Lab
        if (bchsw.Value.lAdjustWP)
        {
            XYZ = LabOut.AsXYZ(bchsw.Value.WPsrc);
            LabOut = XYZ.AsLab(bchsw.Value.WPdest);
        }

        // Back to encoded
        LabOut.FloatToEncoded(Out);
        return true;
    }

    public static Profile? cmsCreateBCHSWabstractProfileTHR(Context? ContextID,
                                                            uint nLUTPoints,
                                                            double Bright,
                                                            double Contrast,
                                                            double Hue,
                                                            double Saturation,
                                                            uint TempSrc,
                                                            uint TempDest)
    {
        Span<uint> Dimensions = stackalloc uint[MAX_INPUT_DIMENSIONS];
        BCHSWADJUSTS bchsw = new();
        Pipeline? Pipeline = null;

        bchsw.Brightness = Bright;
        bchsw.Contrast = Contrast;
        bchsw.Hue = Hue;
        bchsw.Saturation = Saturation;
        if (TempSrc == TempDest)
            bchsw.lAdjustWP = false;
        else
        {
            bchsw.lAdjustWP = true;
            bchsw.WPsrc = ((CIExyY)WhitePoint.FromTemp(TempSrc)).AsXYZ;
            bchsw.WPdest = ((CIExyY)WhitePoint.FromTemp(TempDest)).AsXYZ;
        }

        var hICC = cmsCreateProfilePlaceholder(ContextID);
        if (hICC is null)
            return null;

        cmsSetDeviceClass(hICC, Signature.ProfileClass.Abstract);
        cmsSetColorSpace(hICC, Signature.Colorspace.Lab);
        cmsSetPCS(hICC, Signature.Colorspace.Lab);

        cmsSetHeaderRenderingIntent(hICC, INTENT_PERCEPTUAL);

        // Creates a Pipeline with 3D grid only
        Pipeline = cmsPipelineAlloc(ContextID, 3, 3);
        if (Pipeline is null)
        {
            cmsCloseProfile(hICC);
            return null;
        }

        for (var i = 0; i < MAX_INPUT_DIMENSIONS; i++)
            Dimensions[i] = nLUTPoints;
        var CLUT = cmsStageAllocCLut16bitGranular(ContextID, Dimensions, 3, 3, null);
        if (CLUT is null)
            goto Error;

        if (!cmsStageSampleCLut16bit(CLUT, bchswSampler, new Box<BCHSWADJUSTS>(bchsw), SamplerFlag.None))
            goto Error;

        if (!cmsPipelineInsertStage(Pipeline, StageLoc.AtEnd, CLUT))
            goto Error;

        // Create tags
        if (!SetTextTags(hICC, "BCHS build-in"))
            goto Error;

        if (!cmsWriteTag(hICC, Signature.Tag.MediaWhitePoint, new Box<CIEXYZ>(CIEXYZ.D50)))
            goto Error;
        if (!cmsWriteTag(hICC, Signature.Tag.AToB0, Pipeline))
            goto Error;

        // Pipeline is already on virtual profile
        cmsPipelineFree(Pipeline);

        // Ok, done
        return hICC;

    Error:
        cmsPipelineFree(Pipeline);
        cmsCloseProfile(hICC);

        return null;
    }

    public static Profile? cmsCreateBCHSWabstractProfile(uint nLUTPoints,
                                                         double Bright,
                                                         double Contrast,
                                                         double Hue,
                                                         double Saturation,
                                                         uint TempSrc,
                                                         uint TempDest) =>
        cmsCreateBCHSWabstractProfileTHR(null, nLUTPoints, Bright, Contrast, Hue, Saturation, TempSrc, TempDest);

    public static Profile? cmsCreateNULLProfileTHR(Context? ContextID)
    {
        Span<ushort> Zero = stackalloc ushort[2] { 0, 0 };
        ReadOnlySpan<double> PickLstarMatrix = stackalloc double[] { 1, 0, 0 };
        ToneCurve[]? EmptyTab = null;
        Pipeline? LUT = null;

        //var pool = Context.GetPool<ToneCurve>(ContextID);

        var Profile = cmsCreateProfilePlaceholder(ContextID);
        if (Profile is null)
            return null;

        cmsSetProfileVersion(Profile, 4.4);

        if (!SetTextTags(Profile, "NULL profile build-in"))
            goto Error;

        cmsSetDeviceClass(Profile, Signature.ProfileClass.Output);
        cmsSetColorSpace(Profile, Signature.Colorspace.Gray);
        cmsSetPCS(Profile, Signature.Colorspace.Lab);

        // Create a valid ICC 4 structure
        LUT = cmsPipelineAlloc(ContextID, 3, 1);
        if (LUT is null)
            goto Error;

        //EmptyTab = pool.Rent(3);
        EmptyTab = new ToneCurve[3];
        EmptyTab[0] = EmptyTab[1] = EmptyTab[2] = cmsBuildTabulatedToneCurve16(ContextID, 2, Zero)!;
        var PostLin = cmsStageAllocToneCurves(ContextID, 3, EmptyTab);
        var OutLin = cmsStageAllocToneCurves(ContextID, 1, EmptyTab);
        cmsFreeToneCurve(EmptyTab[0]);

        if (!cmsPipelineInsertStage(LUT, StageLoc.AtEnd, PostLin))
            goto Error;

        if (!cmsPipelineInsertStage(LUT, StageLoc.AtEnd, cmsStageAllocMatrix(ContextID, 1, 3, PickLstarMatrix, null)))
            goto Error;

        if (!cmsPipelineInsertStage(LUT, StageLoc.AtEnd, OutLin))
            goto Error;

        if (!cmsWriteTag(Profile, Signature.Tag.BToA0, LUT))
            goto Error;
        if (!cmsWriteTag(Profile, Signature.Tag.MediaWhitePoint, new Box<CIEXYZ>(CIEXYZ.D50)))
            goto Error;

        //ReturnArray(pool, EmptyTab);
        cmsPipelineFree(LUT);
        return Profile;

    Error:
        //if (EmptyTab is not null)
        //    ReturnArray(pool, EmptyTab);
        if (LUT is not null)
            cmsPipelineFree(LUT);
        if (Profile is not null)
            cmsCloseProfile(Profile);

        return null;
    }

    public static Profile? cmsCreateNULLProfile() =>
        cmsCreateNULLProfileTHR(null);

    private static bool IsPCS(Signature ColorSpace) =>
        ColorSpace == Signature.Colorspace.XYZ ||
        ColorSpace == Signature.Colorspace.Lab;

    private static void FixColorSpaces(Profile Profile, Signature ColorSpace, Signature PCS, uint dwFlags)
    {
        var (cls, cp, pcs) = ((dwFlags & cmsFLAGS_GUESSDEVICECLASS) is not 0, IsPCS(ColorSpace), IsPCS(PCS)) switch
                             {
                                 (true, true, true)  => (Signature.ProfileClass.Abstract, ColorSpace, PCS),
                                 (true, true, false) => (Signature.ProfileClass.Output, PCS, ColorSpace),
                                 (true, false, true) => (Signature.ProfileClass.Input, ColorSpace, PCS),
                                 _                   => (Signature.ProfileClass.Link, ColorSpace, PCS),
                             };

        cmsSetDeviceClass(Profile, cls);
        cmsSetColorSpace(Profile, cp);
        cmsSetPCS(Profile, pcs);
    }

    private static Profile? CreateNamedColorDevicelink(Transform xform)
    {
        Span<int> i = stackalloc int[1];
        var v = xform;
        Profile? hICC = null;
        NamedColorList? nc2 = null, Original = null;

        // Create an empty placeholder
        hICC = cmsCreateProfilePlaceholder(v.ContextID);
        if (hICC is null)
            return null;

        // Critical information
        cmsSetDeviceClass(hICC, Signature.ProfileClass.NamedColor);
        cmsSetColorSpace(hICC, v.ExitColorSpace);
        cmsSetPCS(hICC, Signature.Colorspace.Lab);

        // Tag profile with information
        if (!SetTextTags(hICC, "Named color devicelink"))
            goto Error;

        Original = cmsGetNamedColorList(xform);
        if (Original is null)
            goto Error;

        var nColors = cmsNamedColorCount(Original);
        nc2 = cmsDupNamedColorList(Original);
        if (nc2 is null)
            goto Error;

        // Colorant count now depends on the output space
        nc2.ColorantCount = cmsPipelineOutputChannels(v.Lut);

        // Make sure we have proper formatters
        cmsChangeBuffersFormat(
            xform,
            TYPE_NAMED_COLOR_INDEX,
            FLOAT_SH(0) | COLORSPACE_SH((uint)_cmsLCMScolorSpace(v.ExitColorSpace)) |
            BYTES_SH(2) | CHANNELS_SH((uint)cmsChannelsOfColorSpace(v.ExitColorSpace)));

        // Apply the transform to colorants.
        for (i[0] = 0; i[0] < nColors; i[0]++)
            cmsDoTransform(xform, i, nc2.List[i[0]].DeviceColorant, 1);

        if (!cmsWriteTag(hICC, Signature.Tag.NamedColor2, nc2))
            goto Error;
        cmsFreeNamedColorList(nc2);

        return hICC;

    Error:
        if (hICC is not null)
            cmsCloseProfile(hICC);
        return null;
    }

    private struct AllowedLUT
    {
        public bool IsV4;
        public Signature RequiredTag;
        public Signature LutType;
        public int nTypes;
        public Signature[] MpeTypes = new Signature[5];

        public AllowedLUT(bool isV4, Signature requiredTag, Signature lutType, params Signature[] mpeTypes)
        {
            IsV4 = isV4;
            RequiredTag = requiredTag;
            LutType = lutType;
            nTypes = mpeTypes.Length;
            for (var i = 0; i < mpeTypes.Length && i < 5; i++)
                MpeTypes[i] = mpeTypes[i];
        }
    }

    private static readonly AllowedLUT[] AllowedLUTTypes = new AllowedLUT[]
    {
        new(
            false,
            default,
            Signature.TagType.Lut16,
            Signature.Stage.MatrixElem,
            Signature.Stage.CurveSetElem,
            Signature.Stage.CLutElem,
            Signature.Stage.CurveSetElem),
        new(
            false,
            default,
            Signature.TagType.Lut16,
            Signature.Stage.CurveSetElem,
            Signature.Stage.CLutElem,
            Signature.Stage.CurveSetElem),
        new(false, default, Signature.TagType.Lut16, Signature.Stage.CurveSetElem, Signature.Stage.CLutElem),
        new(true, default, Signature.TagType.LutAtoB, Signature.Stage.CurveSetElem),
        new(
            true,
            Signature.Tag.AToB0,
            Signature.TagType.LutAtoB,
            Signature.Stage.CurveSetElem,
            Signature.Stage.MatrixElem,
            Signature.Stage.CurveSetElem),
        new(
            true,
            Signature.Tag.AToB0,
            Signature.TagType.LutAtoB,
            Signature.Stage.CurveSetElem,
            Signature.Stage.CLutElem,
            Signature.Stage.CurveSetElem),
        new(
            true,
            Signature.Tag.AToB0,
            Signature.TagType.LutAtoB,
            Signature.Stage.CurveSetElem,
            Signature.Stage.CLutElem,
            Signature.Stage.CurveSetElem,
            Signature.Stage.MatrixElem,
            Signature.Stage.CurveSetElem),
        new(true, Signature.Tag.BToA0, Signature.TagType.LutBtoA, Signature.Stage.CurveSetElem),
        new(
            true,
            Signature.Tag.BToA0,
            Signature.TagType.LutBtoA,
            Signature.Stage.CurveSetElem,
            Signature.Stage.MatrixElem,
            Signature.Stage.CurveSetElem),
        new(
            true,
            Signature.Tag.BToA0,
            Signature.TagType.LutBtoA,
            Signature.Stage.CurveSetElem,
            Signature.Stage.CLutElem,
            Signature.Stage.CurveSetElem),
        new(
            true,
            Signature.Tag.BToA0,
            Signature.TagType.LutBtoA,
            Signature.Stage.CurveSetElem,
            Signature.Stage.MatrixElem,
            Signature.Stage.CurveSetElem,
            Signature.Stage.CLutElem,
            Signature.Stage.CurveSetElem),
    };

    private const uint SIZE_OF_ALLOWED_LUT = 11;

    private static bool CheckOne(AllowedLUT Tab, Pipeline Lut)
    {
        Stage? mpe;
        int n;

        for (n = 0, mpe = Lut.Elements; mpe is not null; mpe = mpe.Next, n++)
        {
            if (n >= Tab.nTypes)
                return false;
            if (cmsStageType(mpe) != Tab.MpeTypes[n])
                return false;
        }

        return n == Tab.nTypes;
    }

    private static AllowedLUT FindCombination(Pipeline Lut, bool IsV4, Signature DestinationTag)
    {
        for (var n = 0u; n < SIZE_OF_ALLOWED_LUT; n++)
        {
            var Tab = AllowedLUTTypes[n];
            if (IsV4 ^ Tab.IsV4)
                continue;
            if ((uint)Tab.RequiredTag is not 0 && Tab.RequiredTag != DestinationTag)
                continue;

            if (CheckOne(Tab, Lut))
                return Tab;
        }

        return default;
    }

    public static Profile? cmsTransform2DeviceLink(Transform hTransform, double Version, uint dwFlags)
    {
        Profile? Profile = null;
        var xform = hTransform;
        Pipeline? LUT = null;
        var ContextID = cmsGetTransformContextID(hTransform);

        _cmsAssert(hTransform);

        // Check if the pipeline holding is valid
        if (xform.Lut is null)
            return null;

        // Get the first mpe to check for named color
        var mpe = cmsPipelineGetPtrToFirstStage(xform.Lut);

        // Check if it is a named color transform
        if (mpe is not null)
        {
            if (cmsStageType(mpe) == Signature.Stage.NamedColorElem)
                return CreateNamedColorDevicelink(hTransform);
        }

        // First thing to do is to get a copy of the transformation
        LUT = cmsPipelineDup(xform.Lut);
        if (LUT is null)
            return null;

        // Time to fix the Lab2/Lab4 issue.
        if (xform.EntryColorSpace == Signature.Colorspace.Lab && Version < 4.0)
        {
            if (!cmsPipelineInsertStage(LUT, StageLoc.AtBegin, _cmsStageAllocLabV2ToV4curves(ContextID)))
                goto Error;
        }

        // On the output side too. Note that due to V2/V4 PCS encoding on lab we cannot fix white misalignments
        if (xform.ExitColorSpace == Signature.Colorspace.Lab && Version < 4.0)
        {
            dwFlags |= cmsFLAGS_NOWHITEONWHITEFIXUP;
            if (!cmsPipelineInsertStage(LUT, StageLoc.AtEnd, _cmsStageAllocLabV4ToV2(ContextID)))
                goto Error;
        }

        Profile = cmsCreateProfilePlaceholder(ContextID);
        if (Profile is null)
            goto Error;       // Can't allocate

        cmsSetProfileVersion(Profile, Version);

        FixColorSpaces(Profile, xform.EntryColorSpace, xform.ExitColorSpace, dwFlags);

        // Optimize the LUT and precalculate a devicelink
        var ChansIn = (uint)cmsChannelsOfColorSpace(xform.EntryColorSpace);
        var ChansOut = (uint)cmsChannelsOfColorSpace(xform.ExitColorSpace);

        var ColorSpaceBitsIn = _cmsLCMScolorSpace(xform.EntryColorSpace);
        var ColorSpaceBitsOut = _cmsLCMScolorSpace(xform.ExitColorSpace);

        var FrmIn = COLORSPACE_SH((uint)ColorSpaceBitsIn) | CHANNELS_SH(ChansIn) | BYTES_SH(2);
        var FrmOut = COLORSPACE_SH((uint)ColorSpaceBitsOut) | CHANNELS_SH(ChansOut) | BYTES_SH(2);

        var deviceClass = cmsGetDeviceClass(Profile);

        var DestinationTag = (Signature)(deviceClass == Signature.ProfileClass.Output
                                             ? Signature.Tag.BToA0
                                             : Signature.Tag.AToB0);

        // Check if the profile/version can store the result
        var AllowedLUT = (dwFlags & cmsFLAGS_FORCE_CLUT) is 0
                             ? FindCombination(LUT, Version >= 4.0, DestinationTag)
                             : default;

        if (AllowedLUT.MpeTypes is null)
        {
            // Try to optimize
            _cmsOptimizePipeline(ContextID, ref LUT, xform.RenderingIntent, ref FrmIn, ref FrmOut, ref dwFlags);
            AllowedLUT = FindCombination(LUT, Version >= 4.0, DestinationTag);
        }

        // If no way, then force CLUT that for sure can be written
        if (AllowedLUT.MpeTypes is null)
        {
            dwFlags |= cmsFLAGS_FORCE_CLUT;
            _cmsOptimizePipeline(ContextID, ref LUT, xform.RenderingIntent, ref FrmIn, ref FrmOut, ref dwFlags);

            // Put identity curves if needed
            var FirstStage = cmsPipelineGetPtrToFirstStage(LUT);
            if (FirstStage is not null &&
                FirstStage.Type != Signature.Stage.CurveSetElem &&
                !cmsPipelineInsertStage(LUT, StageLoc.AtBegin, _cmsStageAllocIdentityCurves(ContextID, ChansIn)))
                goto Error;

            var LastStage = cmsPipelineGetPtrToLastStage(LUT);
            if (LastStage is not null &&
                LastStage.Type != Signature.Stage.CurveSetElem &&
                !cmsPipelineInsertStage(LUT, StageLoc.AtEnd, _cmsStageAllocIdentityCurves(ContextID, ChansOut)))
                goto Error;

            AllowedLUT = FindCombination(LUT, Version >= 4.0, DestinationTag);
        }

        // Something is wrong...
        if (AllowedLUT.MpeTypes is null)
            goto Error;

        if ((dwFlags & cmsFLAGS_8BITS_DEVICELINK) is not 0)
            cmsPipelineSetSaveAs8bitsFlag(LUT, true);

        // Tag profile with information
        if (!SetTextTags(Profile, "devicelink"))
            goto Error;

        // Store result
        if (!cmsWriteTag(Profile, DestinationTag, LUT))
            goto Error;

        if ((xform.InputColorant is not null &&
             !cmsWriteTag(Profile, Signature.Tag.ColorantTable, xform.InputColorant)) ||
            (xform.OutputColorant is not null &&
             !cmsWriteTag(Profile, Signature.Tag.ColorantTableOut, xform.OutputColorant)) ||
            (deviceClass == Signature.ProfileClass.Link &&
             xform.Sequence is not null && !_cmsWriteProfileSequence(Profile, xform.Sequence)))
            goto Error;

        // Set the white point
        if (deviceClass == Signature.ProfileClass.Input)
        {
            if (!cmsWriteTag(Profile, Signature.Tag.MediaWhitePoint, new Box<CIEXYZ>(xform.EntryWhitePoint)))
                goto Error;
        }
        else
        {
            if (!cmsWriteTag(Profile, Signature.Tag.MediaWhitePoint, new Box<CIEXYZ>(xform.ExitWhitePoint)))
                goto Error;
        }

        // Per 7.2.14 in spec 4.3
        cmsSetHeaderRenderingIntent(Profile, xform.RenderingIntent);

        cmsPipelineFree(LUT);
        return Profile;

    Error:
        if (LUT is not null)
            cmsPipelineFree(LUT);
        cmsCloseProfile(Profile);
        return null;
    }
}
