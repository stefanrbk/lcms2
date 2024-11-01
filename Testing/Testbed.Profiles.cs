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

using Microsoft.Extensions.Logging;

using System.Text;

namespace lcms2.testbed;

internal static partial class Testbed
{
    private static Profile? Create_AboveRGB()
    {
        var Curve = new ToneCurve[3];
        CIExyYTRIPLE Primaries = new()
        {
            Red = new()
            {
                x = 0.64,
                y = 0.33,
                Y = 1
            },
            Green = new()
            {
                x = 0.21,
                y = 0.71,
                Y = 1
            },
            Blue = new()
            {
                x = 0.15,
                y = 0.06,
                Y = 1
            },
        };

        Curve[0] = Curve[1] = Curve[2] = cmsBuildGamma(DbgThread(), 2.19921875);

        var D65 = cmsWhitePointFromTemp(6504);
        var Profile = cmsCreateRGBProfileTHR(DbgThread(), D65, Primaries, Curve);
        cmsFreeToneCurve(Curve[0]);

        return Profile;
    }

    private static Profile? Create_Gray22()
    {
        var Curve = cmsBuildGamma(DbgThread(), 2.2);
        if (Curve is null) return null;

        var Profile = cmsCreateGrayProfileTHR(DbgThread(), CIExyY.D50, Curve);
        cmsFreeToneCurve(Curve);

        return Profile;
    }

    private static Profile? Create_Gray30()
    {
        var Curve = cmsBuildGamma(DbgThread(), 3.0);
        if (Curve is null) return null;

        var Profile = cmsCreateGrayProfileTHR(DbgThread(), CIExyY.D50, Curve);
        cmsFreeToneCurve(Curve);

        return Profile;
    }

    private static Profile? Create_GrayLab()
    {
        var Curve = cmsBuildGamma(DbgThread(), 1.0);
        if (Curve is null) return null;

        var Profile = cmsCreateGrayProfileTHR(DbgThread(), CIExyY.D50, Curve);
        cmsFreeToneCurve(Curve);

        cmsSetPCS(Profile, Signature.Colorspace.Lab);
        return Profile;
    }

    private static Profile? Create_CMYK_DeviceLink()
    {
        var Tab = new ToneCurve[4];
        var Curve = cmsBuildGamma(DbgThread(), 3.0);
        if (Curve is null) return null;

        Tab[0] = Tab[1] = Tab[2] = Tab[3] = Curve;

        var Profile = cmsCreateLinearizationDeviceLinkTHR(DbgThread(), Signature.Colorspace.Cmyk, Tab);
        cmsFreeToneCurve(Curve);

        return Profile;
    }

    private struct FakeCMYKParams
    {
        public Transform hLab2sRGB;
        public Transform sRGB2Lab;
        public Transform hIlimit;
    }

    private static double Clip(double v) =>
        Math.Max(Math.Min(v, 1), 0);

    private static bool ForwardSampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        Span<double> rgb = stackalloc double[3];
        Span<double> cmyk = stackalloc double[4];

        if (Cargo is not Box<FakeCMYKParams> p)
            return false;

        cmsDoTransform(p.Value.hLab2sRGB, In, rgb, 1);

        var c = 1 - rgb[0];
        var m = 1 - rgb[1];
        var y = 1 - rgb[2];

        var k = c < m ? cmsmin(c, y) : cmsmin(m, y);

        // NONSENSE WARNING!: I'm doing this just because this is a test
        // profile that may have ink limit up to 400%. There is no UCR here
        // so the profile is basically useless for anything but testing.

        cmyk[0] = c;
        cmyk[1] = m;
        cmyk[2] = y;
        cmyk[3] = k;

        cmsDoTransform(p.Value.hIlimit, cmyk, Out, 1);

        return true;
    }

    private static bool ReverseSampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        Span<double> rgb = stackalloc double[3];

        if (Cargo is not Box<FakeCMYKParams> p)
            return false;

        var c = In[0] / 65535.0;
        var m = In[1] / 65535.0;
        var y = In[2] / 65535.0;
        var k = In[3] / 65535.0;

        if (k is 0)
        {
            rgb[0] = Clip(1 - c);
            rgb[1] = Clip(1 - m);
            rgb[2] = Clip(1 - y);
        }
        else
        {
            if (k is 1)
            {
                rgb[0] = rgb[1] = rgb[2] = 0;
            }
            else
            {
                rgb[0] = Clip((1 - c) * (1 - k));
                rgb[1] = Clip((1 - m) * (1 - k));
                rgb[2] = Clip((1 - y) * (1 - k));
            }
        }

        cmsDoTransform(p.Value.sRGB2Lab, rgb, Out, 1);

        return true;
    }

    private static Profile? CreateFakeCMYK(double InkLimit, bool lUseAboveRGB)
    {
        FakeCMYKParams p;

        var hsRGB = lUseAboveRGB
            ? Create_AboveRGB()
            : cmsCreate_sRGBProfile();

        var hLab = cmsCreateLab4Profile(null);
        var hLimit = cmsCreateInkLimitingDeviceLink(Signature.Colorspace.Cmyk, InkLimit);

        var cmykfrm = FLOAT_SH(1) | BYTES_SH(0) | CHANNELS_SH(4);
        p.hLab2sRGB = cmsCreateTransform(hLab, TYPE_Lab_16, hsRGB, TYPE_RGB_DBL, INTENT_PERCEPTUAL, cmsFLAGS_NOOPTIMIZE | cmsFLAGS_NOCACHE);
        p.sRGB2Lab = cmsCreateTransform(hsRGB, TYPE_RGB_DBL, hLab, TYPE_Lab_16, INTENT_PERCEPTUAL, cmsFLAGS_NOOPTIMIZE | cmsFLAGS_NOCACHE);
        p.hIlimit = cmsCreateTransform(hLimit, cmykfrm, null, TYPE_CMYK_16, INTENT_PERCEPTUAL, cmsFLAGS_NOOPTIMIZE | cmsFLAGS_NOCACHE);
        var pPtr = new Box<FakeCMYKParams>(p);

        cmsCloseProfile(hLab); cmsCloseProfile(hsRGB); cmsCloseProfile(hLimit);

        var ContextID = DbgThread();
        var hICC = cmsCreateProfilePlaceholder(ContextID);
        if (hICC is null) return null;

        cmsSetProfileVersion(hICC, 4.3);

        cmsSetDeviceClass(hICC, Signature.ProfileClass.Output);
        cmsSetColorSpace(hICC, Signature.Colorspace.Cmyk);
        cmsSetPCS(hICC, Signature.Colorspace.Lab);

        var BToA0 = cmsPipelineAlloc(ContextID, 3, 4);
        if (BToA0 is null) return null;
        var CLUT = cmsStageAllocCLut16bit(ContextID, 17, 3, 4, null);
        if (CLUT is null) return null;
        if (!cmsStageSampleCLut16bit(CLUT, ForwardSampler, pPtr, 0)) return null;

        cmsPipelineInsertStage(BToA0, StageLoc.AtBegin, _cmsStageAllocIdentityCurves(ContextID, 3));
        cmsPipelineInsertStage(BToA0, StageLoc.AtEnd, CLUT);
        cmsPipelineInsertStage(BToA0, StageLoc.AtEnd, _cmsStageAllocIdentityCurves(ContextID, 4));

        if (!cmsWriteTag(hICC, Signature.Tag.BToA0, BToA0)) return null;
        cmsPipelineFree(BToA0);

        var AToB0 = cmsPipelineAlloc(ContextID, 4, 3);
        if (AToB0 is null) return null;
        CLUT = cmsStageAllocCLut16bit(ContextID, 17, 4, 3, null);
        if (CLUT is null) return null;
        if (!cmsStageSampleCLut16bit(CLUT, ReverseSampler, pPtr, 0)) return null;

        cmsPipelineInsertStage(AToB0, StageLoc.AtBegin, _cmsStageAllocIdentityCurves(ContextID, 4));
        cmsPipelineInsertStage(AToB0, StageLoc.AtEnd, CLUT);
        cmsPipelineInsertStage(AToB0, StageLoc.AtEnd, _cmsStageAllocIdentityCurves(ContextID, 3));

        if (!cmsWriteTag(hICC, Signature.Tag.AToB0, AToB0)) return null;
        cmsPipelineFree(AToB0);

        cmsDeleteTransform(p.hLab2sRGB);
        cmsDeleteTransform(p.sRGB2Lab);
        cmsDeleteTransform(p.hIlimit);

        cmsLinkTag(hICC, Signature.Tag.AToB1, Signature.Tag.AToB0);
        cmsLinkTag(hICC, Signature.Tag.AToB2, Signature.Tag.AToB0);
        cmsLinkTag(hICC, Signature.Tag.BToA1, Signature.Tag.BToA0);
        cmsLinkTag(hICC, Signature.Tag.BToA2, Signature.Tag.BToA0);

        return hICC;
    }

    private static bool OneVirtual(Profile h, string SubTestTxt, string FileName)
    {
        using (logger.BeginScope(SubTestTxt))
        {
            if (h is null) return false;

            if (!cmsSaveProfileToFile(h, FileName)) return false;
            cmsCloseProfile(h);

            h = cmsOpenProfileFromFile(FileName, "r");
            if (h is null) return false;

            cmsCloseProfile(h);
            return true;
        }
    }

    internal static bool CreateTestProfiles()
    {
        //StartAllocLogging();
        var h = cmsCreate_sRGBProfileTHR(DbgThread());
        //EndAllocLogging();
        if (!OneVirtual(h, "sRGB profile", "sRGBlcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = Create_AboveRGB();
        //EndAllocLogging();
        if (!OneVirtual(h, "aRGB profile", "aRGBlcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = Create_Gray22();
        //EndAllocLogging();
        if (!OneVirtual(h, "Gray profile", "graylcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = Create_Gray30();
        //EndAllocLogging();
        if (!OneVirtual(h, "Gray 3.0 profile", "gray3lcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = Create_GrayLab();
        //EndAllocLogging();
        if (!OneVirtual(h, "Gray Lab profile", "glablcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = Create_CMYK_DeviceLink();
        //EndAllocLogging();
        if (!OneVirtual(h, "Linearization profile", "linlcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = cmsCreateInkLimitingDeviceLinkTHR(DbgThread(), Signature.Colorspace.Cmyk, 150);
        //EndAllocLogging();
        if (!OneVirtual(h, "Ink-limiting profile", "limitlcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = cmsCreateLab2ProfileTHR(DbgThread(), null);
        //EndAllocLogging();
        if (!OneVirtual(h, "Lab 2 identity profile", "labv2lcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = cmsCreateLab4ProfileTHR(DbgThread(), null);
        //EndAllocLogging();
        if (!OneVirtual(h, "Lab 4 identity profile", "labv4lcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = cmsCreateXYZProfileTHR(DbgThread());
        //EndAllocLogging();
        if (!OneVirtual(h, "XYZ identity profile", "xyzlcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = cmsCreateNULLProfileTHR(DbgThread());
        //EndAllocLogging();
        if (!OneVirtual(h, "null profile", "nulllcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = cmsCreateBCHSWabstractProfileTHR(DbgThread(), 17, 0, 0, 0, 0, 5000, 6000);
        //EndAllocLogging();
        if (!OneVirtual(h, "BCHS profile", "bchslcms2.icc")) return false;

        // ----
        //StartAllocLogging();
        h = CreateFakeCMYK(300, false);
        //EndAllocLogging();
        if (!OneVirtual(h, "Fake CMYK profile", "lcms2cmyk.icc")) return false;

        // ----
        //StartAllocLogging();
        h = cmsCreateBCHSWabstractProfileTHR(DbgThread(), 17, 0, 1.2, 0, 3, 5000, 5000);
        //EndAllocLogging();
        if (!OneVirtual(h, "Brightness", "brightness.icc")) return false;

        return true;
    }

    internal static void RemoveTestProfiles()
    {
        File.Delete("sRGBlcms2.icc");
        File.Delete("aRGBlcms2.icc");
        File.Delete("graylcms2.icc");
        File.Delete("gray3lcms2.icc");
        File.Delete("linlcms2.icc");
        File.Delete("limitlcms2.icc");
        File.Delete("labv2lcms2.icc");
        File.Delete("labv4lcms2.icc");
        File.Delete("xyzlcms2.icc");
        File.Delete("nulllcms2.icc");
        File.Delete("bchslcms2.icc");
        File.Delete("lcms2cmyk.icc");
        File.Delete("glablcms2.icc");
        File.Delete("lcms2link.icc");
        File.Delete("lcms2link2.icc");
        File.Delete("brightness.icc");
    }

    // This is a very big test that checks every single tag
    public static bool CheckProfileCreation()
    {
        Profile? h;
        int Pass;

        using (logger.BeginScope("Profile setup"))
        {
            h = cmsCreateProfilePlaceholder(DbgThread());
            if (h == null) return false;

            cmsSetProfileVersion(h, 4.3);
            if (cmsGetTagCount(h) != 0)
            {
                logger.LogWarning("Empty profile with nonzero number of tags");
                goto Error;
            }
            if (cmsIsTag(h, Signature.Tag.AToB0))
            {
                logger.LogWarning("Found a tag in an empty profile");
                goto Error;
            }

            cmsSetColorSpace(h, Signature.Colorspace.Rgb);
            if (cmsGetColorSpace(h) != Signature.Colorspace.Rgb)
            {
                logger.LogWarning("Unable to set colorspace");
                goto Error;
            }

            cmsSetPCS(h, Signature.Colorspace.Lab);
            if (cmsGetPCS(h) != Signature.Colorspace.Lab)
            {
                logger.LogWarning("Unable to set colorspace");
                goto Error;
            }

            cmsSetDeviceClass(h, Signature.ProfileClass.Display);
            if (cmsGetDeviceClass(h) != Signature.ProfileClass.Display)
            {
                logger.LogWarning("Unable to set deviceclass");
                goto Error;
            }

            cmsSetHeaderRenderingIntent(h, INTENT_SATURATION);
            if (cmsGetHeaderRenderingIntent(h) != INTENT_SATURATION)
            {
                logger.LogWarning("Unable to set rendering intent");
                goto Error;
            }
        }

        for (Pass = 1; Pass <= 2 /*1*/; Pass++)
        {
            using (logger.BeginScope("Pass {num}", Pass))
            {
                using (logger.BeginScope("Tags holding XYZ"))
                {
                    if (!CheckXYZ(Pass, h, Signature.Tag.BlueColorant))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBlueColorantTag");
                        goto Error;
                    }
                    if (!CheckXYZ(Pass, h, Signature.Tag.GreenColorant))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigGreenColorantTag");
                        goto Error;
                    }
                    if (!CheckXYZ(Pass, h, Signature.Tag.RedColorant))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigRedColorantTag");
                        goto Error;
                    }
                    if (!CheckXYZ(Pass, h, Signature.Tag.MediaBlackPoint))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigMediaBlackPointTag");
                        goto Error;
                    }
                    if (!CheckXYZ(Pass, h, Signature.Tag.MediaWhitePoint))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigMediaWhitePointTag");
                        goto Error;
                    }
                    if (!CheckXYZ(Pass, h, Signature.Tag.Luminance))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigLuminanceTag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding curves"))
                {
                    if (!CheckGamma(Pass, h, Signature.Tag.BlueTRC))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBlueTRCTag");
                        goto Error;
                    }
                    if (!CheckGamma(Pass, h, Signature.Tag.GrayTRC))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigGrayTRCTag");
                        goto Error;
                    }
                    if (!CheckGamma(Pass, h, Signature.Tag.GreenTRC))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigGreenTRCTag");
                        goto Error;
                    }
                    if (!CheckGamma(Pass, h, Signature.Tag.RedTRC))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigRedTRCTag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding text"))
                {
                    if (!CheckTextSingle(Pass, h, Signature.Tag.CharTarget))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigCharTargetTag");
                        goto Error;
                    }
                    if (!CheckTextSingle(Pass, h, Signature.Tag.ScreeningDesc))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigScreeningDescTag");
                        goto Error;
                    }

                    if (!CheckText(Pass, h, Signature.Tag.Copyright))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigCopyrightTag");
                        goto Error;
                    }
                    if (!CheckText(Pass, h, Signature.Tag.ProfileDescription))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigProfileDescriptionTag");
                        goto Error;
                    }
                    if (!CheckText(Pass, h, Signature.Tag.DeviceMfgDesc))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigDeviceMfgDescTag");
                        goto Error;
                    }
                    if (!CheckText(Pass, h, Signature.Tag.DeviceModelDesc))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigDeviceModelDescTag");
                        goto Error;
                    }
                    if (!CheckText(Pass, h, Signature.Tag.ViewingCondDesc))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigViewingCondDescTag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding cmsICCData"))
                {
                    if (!CheckData(Pass, h, Signature.Tag.Ps2CRD0))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPs2CRD0Tag");
                        goto Error;
                    }
                    if (!CheckData(Pass, h, Signature.Tag.Ps2CRD1))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPs2CRD1Tag");
                        goto Error;
                    }
                    if (!CheckData(Pass, h, Signature.Tag.Ps2CRD2))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPs2CRD2Tag");
                        goto Error;
                    }
                    if (!CheckData(Pass, h, Signature.Tag.Ps2CRD3))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPs2CRD3Tag");
                        goto Error;
                    }
                    if (!CheckData(Pass, h, Signature.Tag.Ps2CSA))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPs2CSATag");
                        goto Error;
                    }
                    if (!CheckData(Pass, h, Signature.Tag.Ps2RenderingIntent))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPs2RenderingIntentTag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding signatures"))
                {
                    if (!CheckSignature(Pass, h, Signature.Tag.ColorimetricIntentImageState))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigColorimetricIntentImageStateTag");
                        goto Error;
                    }
                    if (!CheckSignature(Pass, h, Signature.Tag.PerceptualRenderingIntentGamut))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPerceptualRenderingIntentGamutTag");
                        goto Error;
                    }
                    if (!CheckSignature(Pass, h, Signature.Tag.SaturationRenderingIntentGamut))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigSaturationRenderingIntentGamutTag");
                        goto Error;
                    }
                    if (!CheckSignature(Pass, h, Signature.Tag.Technology))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigTechnologyTag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding date_time"))
                {
                    if (!CheckDateTime(Pass, h, Signature.Tag.CalibrationDateTime))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigCalibrationDateTimeTag");
                        goto Error;
                    }
                    if (!CheckDateTime(Pass, h, Signature.Tag.DateTime))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigDateTimeTag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding named color lists"))
                {
                    if (!CheckNamedColor(Pass, h, Signature.Tag.ColorantTable, 15, false))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigColorantTableTag");
                        goto Error;
                    }
                    if (!CheckNamedColor(Pass, h, Signature.Tag.ColorantTableOut, 15, false))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigColorantTableOutTag");
                        goto Error;
                    }
                    if (!CheckNamedColor(Pass, h, Signature.Tag.NamedColor2, 4096, true))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigNamedColor2Tag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding LUTs"))
                {
                    if (!CheckLUT(Pass, h, Signature.Tag.AToB0))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigAToB0Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.AToB1))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigAToB1Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.AToB2))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigAToB2Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.BToA0))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBToA0Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.BToA1))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBToA1Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.BToA2))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBToA2Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.Preview0))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPreview0Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.Preview1))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPreview1Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.Preview2))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigPreview2Tag");
                        goto Error;
                    }
                    if (!CheckLUT(Pass, h, Signature.Tag.Gamut))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigGamutTag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding CHAD"))
                    if (!CheckCHAD(Pass, h, Signature.Tag.ChromaticAdaptation))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigChromaticAdaptationTag");
                        goto Error;
                    }

                using (logger.BeginScope("Tags holding Chromaticity"))
                    if (!CheckChromaticity(Pass, h, Signature.Tag.Chromaticity))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigChromaticityTag");
                        goto Error;
                    }

                using (logger.BeginScope("Tags holding colorant order"))
                    if (!CheckColorantOrder(Pass, h, Signature.Tag.ColorantOrder))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigColorantOrderTag");
                        goto Error;
                    }

                using (logger.BeginScope("Tags holding measurement"))
                    if (!CheckMeasurement(Pass, h, Signature.Tag.Measurement))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigMeasurementTag");
                        goto Error;
                    }

                using (logger.BeginScope("Tags holding CRD info"))
                    if (!CheckCRDinfo(Pass, h, Signature.Tag.CrdInfo))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigCrdInfoTag");
                        goto Error;
                    }

                using (logger.BeginScope("Tags holding UCR/BG"))
                    if (!CheckUcrBg(Pass, h, Signature.Tag.UcrBg))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigUcrBgTag");
                        goto Error;
                    }

                using (logger.BeginScope("Tags holding MPE"))
                {
                    if (!CheckMPE(Pass, h, Signature.Tag.DToB0))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigDToB0Tag");
                        goto Error;
                    }
                    if (!CheckMPE(Pass, h, Signature.Tag.DToB1))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigDToB1Tag");
                        goto Error;
                    }
                    if (!CheckMPE(Pass, h, Signature.Tag.DToB2))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigDToB2Tag");
                        goto Error;
                    }
                    if (!CheckMPE(Pass, h, Signature.Tag.DToB3))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigDToB3Tag");
                        goto Error;
                    }
                    if (!CheckMPE(Pass, h, Signature.Tag.BToD0))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBToD0Tag");
                        goto Error;
                    }
                    if (!CheckMPE(Pass, h, Signature.Tag.BToD1))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBToD1Tag");
                        goto Error;
                    }
                    if (!CheckMPE(Pass, h, Signature.Tag.BToD2))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBToD2Tag");
                        goto Error;
                    }
                    if (!CheckMPE(Pass, h, Signature.Tag.BToD3))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigBToD3Tag");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags using screening"))
                    if (!CheckScreening(Pass, h, Signature.Tag.Screening))
                    {
                        logger.LogWarning("{tag} failed", "cmsSigScreeningTag");
                        goto Error;
                    }

                using (logger.BeginScope("Tags holding profile sequence description"))
                {
                    if (!CheckProfileSequenceTag(Pass, h))
                    {
                        logger.LogWarning("Oops");
                        goto Error;
                    }
                    if (!CheckProfileSequenceIDTag(Pass, h))
                    {
                        logger.LogWarning("Oops");
                        goto Error;
                    }
                }

                using (logger.BeginScope("Tags holding ICC viewing conditions"))
                    if (!CheckICCViewingConditions(Pass, h))
                    {
                        logger.LogWarning("Oops");
                        goto Error;
                    }

                using (logger.BeginScope("VCGT tags"))
                    if (!CheckVCGT(Pass, h))
                    {
                        logger.LogWarning("Oops");
                        goto Error;
                    }

                using (logger.BeginScope("RAW tags"))
                    if (!CheckRAWtags(Pass, h))
                    {
                        logger.LogWarning("Oops");
                        goto Error;
                    }

                using (logger.BeginScope("Dictionary meta tags"))
                {
                    // if (!CheckDictionary16(Pass, h)) goto Error;
                    if (!CheckDictionary24(Pass, h))
                    {
                        logger.LogWarning("Oops");
                        goto Error;
                    }
                }

                using (logger.BeginScope("cicp Video Signal Type"))
                    if (!Check_cicp(Pass, h))
                        goto Error;

                using (logger.BeginScope("Microsoft MHC2 tag"))
                    if (!Check_MHC2(Pass, h))
                        goto Error;
            }

            if (Pass == 1)
            {
                using (logger.BeginScope("Saving file"))
                    cmsSaveProfileToFile(h, "alltags.icc");
                using (logger.BeginScope("Closing Pass 1"))
                    cmsCloseProfile(h);
                using (logger.BeginScope("Opening file"))
                    h = cmsOpenProfileFromFileTHR(DbgThread(), "alltags.icc", "r");
            }
        }

        /*
        Not implemented (by design):

        cmsSigDataTag                           = 0x64617461,  // 'data'  -- Unused
        cmsSigDeviceSettingsTag                 = 0x64657673,  // 'devs'  -- Unused
        cmsSigNamedColorTag                     = 0x6E636f6C,  // 'ncol'  -- Don't use this one, deprecated by ICC
        cmsSigOutputResponseTag                 = 0x72657370,  // 'resp'  -- Possible patent on this
        */

        cmsCloseProfile(h);
        File.Delete("alltags.icc");
        return true;

    Error:
        cmsCloseProfile(h);
        File.Delete("alltags.icc");
        return false;
    }

    private static bool CheckXYZ(int Pass, Profile hProfile, Signature tag)
    {
        CIEXYZ XYZ;
        Box<CIEXYZ>? Pt;

        switch (Pass)
        {
            case 1:

                XYZ.X = 1.0; XYZ.Y = 1.1; XYZ.Z = 1.2;
                return cmsWriteTag(hProfile, tag, new Box<CIEXYZ>(XYZ));

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Box<CIEXYZ>;
                if (Pt == null) return false;
                return IsGoodFixed15_16("X", 1.0, Pt.Value.X) &&
                       IsGoodFixed15_16("Y", 1.1, Pt.Value.Y) &&
                       IsGoodFixed15_16("Z", 1.2, Pt.Value.Z);

            default:
                return false;
        }
    }

    private static bool CheckGamma(int Pass, Profile hProfile, Signature tag)
    {
        ToneCurve g, Pt;
        bool rc;

        switch (Pass)
        {
            case 1:

                g = cmsBuildGamma(DbgThread(), 1.0);
                rc = cmsWriteTag(hProfile, tag, g);
                cmsFreeToneCurve(g);
                return rc;

            case 2:
                Pt = (cmsReadTag(hProfile, tag) is ToneCurve curve) ? curve : null;
                if (Pt == null) return false;
                return cmsIsToneCurveLinear(Pt);

            default:
                return false;
        }
    }

    private static bool CheckTextSingle(int Pass, Profile hProfile, Signature tag)
    {
        Mlu? m, Pt;
        bool rc;
        Span<byte> Buffer = stackalloc byte[256];

        switch (Pass)
        {
            case 1:
                m = cmsMLUalloc(DbgThread(), 0);
                cmsMLUsetASCII(m, cmsNoLanguage, cmsNoCountry, "Test test"u8);
                rc = cmsWriteTag(hProfile, tag, m);
                cmsMLUfree(m);
                return rc;

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Mlu;
                if (Pt == null) return false;
                cmsMLUgetASCII(Pt, cmsNoLanguage, cmsNoCountry, Buffer);
                if (strcmp(Buffer, "Test test"u8) != 0) return false;
                return true;

            default:
                return false;
        }
    }

    private static bool CheckText(int Pass, Profile hProfile, Signature tag)
    {
        Mlu? m, Pt;
        bool rc;
        Span<byte> Buffer = stackalloc byte[256];

        switch (Pass)
        {
            case 1:
                m = cmsMLUalloc(DbgThread(), 0);
                cmsMLUsetASCII(m, cmsNoLanguage, cmsNoCountry, "Test test"u8);
                cmsMLUsetASCII(m, "en"u8, "US"u8, "1 1 1 1"u8);
                cmsMLUsetASCII(m, "es"u8, "ES"u8, "2 2 2 2"u8);
                cmsMLUsetASCII(m, "ct"u8, "ES"u8, "3 3 3 3"u8);
                cmsMLUsetASCII(m, "en"u8, "GB"u8, "444444444"u8);
                rc = cmsWriteTag(hProfile, tag, m);
                cmsMLUfree(m);
                return rc;

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Mlu;
                if (Pt == null) return false;
                cmsMLUgetASCII(Pt, cmsNoLanguage, cmsNoCountry, Buffer);
                if (strcmp(Buffer, "Test test"u8) != 0) return false;
                cmsMLUgetASCII(Pt, "en"u8, "US"u8, Buffer);
                if (strcmp(Buffer, "1 1 1 1"u8) != 0) return false;
                cmsMLUgetASCII(Pt, "es"u8, "ES"u8, Buffer);
                if (strcmp(Buffer, "2 2 2 2"u8) != 0) return false;
                cmsMLUgetASCII(Pt, "ct"u8, "ES"u8, Buffer);
                if (strcmp(Buffer, "3 3 3 3"u8) != 0) return false;
                cmsMLUgetASCII(Pt, "en"u8, "GB"u8, Buffer);
                if (strcmp(Buffer, "444444444"u8) != 0) return false;
                return true;

            default:
                return false;
        }
    }

    private static bool CheckData(int Pass, Profile hProfile, Signature tag)
    {
        Box<IccData>? Pt;
        var d = new IccData() { len = 1, flag = 0 };
        d.data = new byte[] { (byte)'?' };
        //bool rc;

        switch (Pass)
        {
            case 1:
                return cmsWriteTag(hProfile, tag, new Box<IccData>(d));

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Box<IccData>;
                if (Pt == null) return false;
                return (Pt.Value.data[0] == '?') && (Pt.Value.flag == 0) && (Pt.Value.len == 1);

            default:
                return false;
        }
    }

    private static bool CheckSignature(int Pass, Profile hProfile, Signature tag)
    {
        Box<Signature>? Pt;
        Signature Holder;

        switch (Pass)
        {
            case 1:
                Holder = Signature.Gamut.PerceptualReferenceMedium;
                return cmsWriteTag(hProfile, tag, new Box<Signature>(Holder));

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Box<Signature>;
                if (Pt == null) return false;
                return Pt.Value == Signature.Gamut.PerceptualReferenceMedium;

            default:
                return false;
        }
    }

    private static bool CheckDateTime(int Pass, Profile hProfile, Signature tag)
    {
        Box<DateTime>? Pt;
        DateTime Holder;

        switch (Pass)
        {
            case 1:

                Holder = new(2009, 5, 4, 1, 2, 3);
                return cmsWriteTag(hProfile, tag, new Box<DateTime>(Holder));

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Box<DateTime>;
                if (Pt == null) return false;

                return Pt.Value.Hour == 1 &&
                       Pt.Value.Minute == 2 &&
                       Pt.Value.Second == 3 &&
                       Pt.Value.Day == 4 &&
                       Pt.Value.Month == 5 &&
                       Pt.Value.Year == 2009;

            default:
                return false;
        }
    }

    private static bool CheckNamedColor(int Pass, Profile hProfile, Signature tag, int max_check, bool colorant_check)
    {
        NamedColorList? nc;
        int i, j;
        bool rc;
        Span<byte> Name = stackalloc byte[255];
        Span<ushort> PCS = stackalloc ushort[3];
        Span<ushort> Colorant = stackalloc ushort[cmsMAXCHANNELS];
        Span<byte> CheckName = stackalloc byte[255];
        Span<ushort> CheckPCS = stackalloc ushort[3];
        Span<ushort> CheckColorant = stackalloc ushort[cmsMAXCHANNELS];

        switch (Pass)
        {
            case 1:

                nc = cmsAllocNamedColorList(DbgThread(), 0, 4, "prefix"u8, "suffix"u8);
                if (nc == null) return false;

                for (i = 0; i < max_check; i++)
                {
                    PCS[0] = PCS[1] = PCS[2] = (ushort)i;
                    Colorant[0] = Colorant[1] = Colorant[2] = Colorant[3] = (ushort)(max_check - i);

                    sprintf(Name, "#{0}", i);
                    if (!cmsAppendNamedColor(nc, Name, PCS, Colorant)) { logger.LogWarning("Couldn't append named color"); return false; }
                }

                rc = cmsWriteTag(hProfile, tag, nc);
                cmsFreeNamedColorList(nc);
                return rc;

            case 2:

                nc = (cmsReadTag(hProfile, tag) is NamedColorList box) ? box : null;
                if (nc == null) return false;

                for (i = 0; i < max_check; i++)
                {
                    CheckPCS[0] = CheckPCS[1] = CheckPCS[2] = (ushort)i;
                    CheckColorant[0] = CheckColorant[1] = CheckColorant[2] = CheckColorant[3] = (ushort)(max_check - i);

                    sprintf(CheckName, "#{0}", i);
                    if (!cmsNamedColorInfo(nc, (uint)i, Name, null, null, PCS, Colorant)) { logger.LogWarning("Invalid string"); return false; }

                    for (j = 0; j < 3; j++)
                    {
                        if (CheckPCS[j] != PCS[j]) { logger.LogWarning("Invalid PCS"); return false; }
                    }

                    // This is only used on named color list
                    if (colorant_check)
                    {
                        for (j = 0; j < 4; j++)
                        {
                            if (CheckColorant[j] != Colorant[j]) { logger.LogWarning("Invalid Colorant"); return false; };
                        }
                    }

                    if (strcmp(Name, CheckName) != 0) { logger.LogWarning("Invalid Name"); return false; };
                }
                return true;

            default: return false;
        }
    }

    private static bool CheckLUT(int Pass, Profile hProfile, Signature tag)
    {
        Pipeline? Lut, Pt;
        bool rc;

        switch (Pass)
        {
            case 1:

                Lut = cmsPipelineAlloc(DbgThread(), 3, 3);
                if (Lut == null) return false;

                // Create an identity LUT
                cmsPipelineInsertStage(Lut, StageLoc.AtBegin, _cmsStageAllocIdentityCurves(DbgThread(), 3));
                cmsPipelineInsertStage(Lut, StageLoc.AtEnd, _cmsStageAllocIdentityCLut(DbgThread(), 3));
                cmsPipelineInsertStage(Lut, StageLoc.AtEnd, _cmsStageAllocIdentityCurves(DbgThread(), 3));

                rc = cmsWriteTag(hProfile, tag, Lut);
                cmsPipelineFree(Lut);
                return rc;

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Pipeline;
                if (Pt == null) return false;

                // Transform values, check for identity
                return Check16LUT(Pt);

            default:
                return false;
        }
    }

    private static bool CheckCHAD(int Pass, Profile hProfile, Signature tag)
    {
        double[]? Pt;
        var CHAD = new double[] { 0, .1, .2, .3, .4, .5, .6, .7, .8 };
        int i;

        switch (Pass)
        {
            case 1:
                return cmsWriteTag(hProfile, tag, CHAD);

            case 2:
                Pt = cmsReadTag(hProfile, tag) as double[];
                if (Pt == null) return false;

                for (i = 0; i < 9; i++)
                {
                    if (!IsGoodFixed15_16("CHAD", Pt[i], CHAD[i])) return false;
                }

                return true;

            default:
                return false;
        }
    }

    private static bool CheckChromaticity(int Pass, Profile hProfile, Signature tag)
    {
        Box<CIExyYTRIPLE>? Pt;
        var c = new CIExyYTRIPLE()
        {
            Red = new() { x = 0, y = .1, Y = 1 },
            Green = new() { x = .3, y = .4, Y = 1 },
            Blue = new() { x = .6, y = .7, Y = 1 }
        };

        switch (Pass)
        {
            case 1:
                return cmsWriteTag(hProfile, tag, new Box<CIExyYTRIPLE>(c));

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Box<CIExyYTRIPLE>;
                if (Pt == null) return false;

                if (!IsGoodFixed15_16("xyY", Pt.Value.Red.x, c.Red.x)) return false;
                if (!IsGoodFixed15_16("xyY", Pt.Value.Red.y, c.Red.y)) return false;
                if (!IsGoodFixed15_16("xyY", Pt.Value.Green.x, c.Green.x)) return false;
                if (!IsGoodFixed15_16("xyY", Pt.Value.Green.y, c.Green.y)) return false;
                if (!IsGoodFixed15_16("xyY", Pt.Value.Blue.x, c.Blue.x)) return false;
                if (!IsGoodFixed15_16("xyY", Pt.Value.Blue.y, c.Blue.y)) return false;
                return true;

            default:
                return false;
        }
    }

    private static bool CheckColorantOrder(int Pass, Profile hProfile, Signature tag)
    {
        byte[]? Pt;
        var c = new byte[cmsMAXCHANNELS];
        int i;

        switch (Pass)
        {
            case 1:
                for (i = 0; i < cmsMAXCHANNELS; i++) c[i] = (byte)(cmsMAXCHANNELS - i - 1);
                return cmsWriteTag(hProfile, tag, c);

            case 2:
                Pt = cmsReadTag(hProfile, tag) as byte[];
                if (Pt == null) return false;

                for (i = 0; i < cmsMAXCHANNELS; i++)
                {
                    if (Pt[i] != (cmsMAXCHANNELS - i - 1)) return false;
                }
                return true;

            default:
                return false;
        }
    }

    private static bool CheckMeasurement(int Pass, Profile hProfile, Signature tag)
    {
        Box<IccMeasurementConditions>? Pt;
        IccMeasurementConditions m;

        switch (Pass)
        {
            case 1:
                m.Backing.X = 0.1;
                m.Backing.Y = 0.2;
                m.Backing.Z = 0.3;
                m.Flare = 1.0;
                m.Geometry = 1;
                m.IlluminantType = IlluminantType.D50;
                m.Observer = 1;
                return cmsWriteTag(hProfile, tag, new Box<IccMeasurementConditions>(m));

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Box<IccMeasurementConditions>;
                if (Pt == null) return false;

                if (!IsGoodFixed15_16("Backing", Pt.Value.Backing.X, 0.1)) return false;
                if (!IsGoodFixed15_16("Backing", Pt.Value.Backing.Y, 0.2)) return false;
                if (!IsGoodFixed15_16("Backing", Pt.Value.Backing.Z, 0.3)) return false;
                if (!IsGoodFixed15_16("Flare", Pt.Value.Flare, 1.0)) return false;

                if (Pt.Value.Geometry != 1) return false;
                if (Pt.Value.IlluminantType != IlluminantType.D50) return false;
                if (Pt.Value.Observer != 1) return false;
                return true;

            default:
                return false;
        }
    }

    private static bool CheckUcrBg(int Pass, Profile hProfile, Signature tag)
    {
        Box<UcrBg>? Pt;
        UcrBg m;
        bool rc;
        Span<byte> Buffer = stackalloc byte[256];

        switch (Pass)
        {
            case 1:
                m.Ucr = cmsBuildGamma(DbgThread(), 2.4);
                m.Bg = cmsBuildGamma(DbgThread(), -2.2);
                m.Desc = cmsMLUalloc(DbgThread(), 1);
                cmsMLUsetASCII(m.Desc, cmsNoLanguage, cmsNoCountry, "test UCR/BG"u8);
                rc = cmsWriteTag(hProfile, tag, new Box<UcrBg>(m));
                cmsMLUfree(m.Desc);
                cmsFreeToneCurve(m.Bg);
                cmsFreeToneCurve(m.Ucr);
                return rc;

            case 2:
                Pt = (cmsReadTag(hProfile, tag) is Box<UcrBg> box) ? box : null;
                if (Pt == null) return false;

                cmsMLUgetASCII(Pt.Value.Desc, cmsNoLanguage, cmsNoCountry, Buffer);
                if (strcmp(Buffer, "test UCR/BG"u8) != 0) return false;
                return true;

            default:
                return false;
        }
    }

    private static bool CheckCRDinfo(int Pass, Profile hProfile, Signature tag)
    {
        Mlu? mlu;
        Span<byte> Buffer = stackalloc byte[256];
        bool rc;

        switch (Pass)
        {
            case 1:
                mlu = cmsMLUalloc(DbgThread(), 5);

                cmsMLUsetWide(mlu, "PS"u8, "nm"u8, "test postscript");
                cmsMLUsetWide(mlu, "PS"u8, "#0"u8, "perceptual");
                cmsMLUsetWide(mlu, "PS"u8, "#1"u8, "relative_colorimetric");
                cmsMLUsetWide(mlu, "PS"u8, "#2"u8, "saturation");
                cmsMLUsetWide(mlu, "PS"u8, "#3"u8, "absolute_colorimetric");
                rc = cmsWriteTag(hProfile, tag, mlu);
                cmsMLUfree(mlu);
                return rc;

            case 2:
                mlu = cmsReadTag(hProfile, tag) as Mlu;
                if (mlu == null) return false;

                cmsMLUgetASCII(mlu, "PS"u8, "nm"u8, Buffer);
                if (strcmp(Buffer, "test postscript"u8) != 0) return false;

                cmsMLUgetASCII(mlu, "PS"u8, "#0"u8, Buffer);
                if (strcmp(Buffer, "perceptual"u8) != 0) return false;

                cmsMLUgetASCII(mlu, "PS"u8, "#1"u8, Buffer);
                if (strcmp(Buffer, "relative_colorimetric"u8) != 0) return false;

                cmsMLUgetASCII(mlu, "PS"u8, "#2"u8, Buffer);
                if (strcmp(Buffer, "saturation"u8) != 0) return false;

                cmsMLUgetASCII(mlu, "PS"u8, "#3"u8, Buffer);
                if (strcmp(Buffer, "absolute_colorimetric"u8) != 0) return false;
                return true;

            default:
                return false;
        }
    }

    private static ToneCurve CreateSegmentedCurve()
    {
        var Seg = new CurveSegment[3];
        var Sampled = new float[2] { 0, 1 };

        Seg[0].Type = 6;
        Seg[0].Params = new double[10];
        Seg[0].Params[0] = 1;
        Seg[0].Params[1] = 0;
        Seg[0].Params[2] = 0;
        Seg[0].Params[3] = 0;
        Seg[0].x0 = -1E22F;
        Seg[0].x1 = 0;

        Seg[1].Type = 0;
        Seg[1].nGridPoints = 2;
        Seg[1].SampledPoints = Sampled;
        Seg[1].x0 = 0;
        Seg[1].x1 = 1;

        Seg[2].Type = 6;
        Seg[2].Params = new double[10];
        Seg[2].Params[0] = 1;
        Seg[2].Params[1] = 0;
        Seg[2].Params[2] = 0;
        Seg[2].Params[3] = 0;
        Seg[2].x0 = 1;
        Seg[2].x1 = 1E22F;

        return cmsBuildSegmentedToneCurve(DbgThread(), 3, Seg);
    }

    internal const StageLoc cmsAT_BEGIN = StageLoc.AtBegin;
    internal const StageLoc cmsAT_END = StageLoc.AtEnd;

    private static bool CheckMPE(int Pass, Profile hProfile, Signature tag)
    {
        Pipeline? Lut, Pt;
        var G = new ToneCurve[3];
        bool rc;

        switch (Pass)
        {
            case 1:

                Lut = cmsPipelineAlloc(DbgThread(), 3, 3);

                cmsPipelineInsertStage(Lut, cmsAT_BEGIN, _cmsStageAllocLabV2ToV4(DbgThread()));
                cmsPipelineInsertStage(Lut, cmsAT_END, _cmsStageAllocLabV4ToV2(DbgThread()));
                AddIdentityCLUTfloat(Lut);

                G[0] = G[1] = G[2] = CreateSegmentedCurve();
                cmsPipelineInsertStage(Lut, cmsAT_END, cmsStageAllocToneCurves(DbgThread(), 3, G));
                cmsFreeToneCurve(G[0]);

                rc = cmsWriteTag(hProfile, tag, Lut);
                cmsPipelineFree(Lut);
                return rc;

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Pipeline;
                if (Pt == null) return false;
                return CheckFloatLUT(Pt);

            default:
                return false;
        }
    }

    private static bool CheckScreening(int Pass, Profile hProfile, Signature tag)
    {
        Box<Screening>? Pt;
        Screening sc = new(hProfile.ContextID);
        bool rc;

        switch (Pass)
        {
            case 1:

                sc.Flag = 0;
                sc.nChannels = 1;
                sc.Channels[0].Frequency = 2.0;
                sc.Channels[0].ScreenAngle = 3.0;
                sc.Channels[0].SpotShape = cmsSPOT_ELLIPSE;

                rc = cmsWriteTag(hProfile, tag, new Box<Screening>(sc));
                return rc;

            case 2:
                Pt = cmsReadTag(hProfile, tag) as Box<Screening>;
                if (Pt == null) return false;

                if (Pt.Value.nChannels != 1) return false;
                if (Pt.Value.Flag != 0) return false;
                if (!IsGoodFixed15_16("Freq", Pt.Value.Channels[0].Frequency, 2.0)) return false;
                if (!IsGoodFixed15_16("Angle", Pt.Value.Channels[0].ScreenAngle, 3.0)) return false;
                if (Pt.Value.Channels[0].SpotShape != cmsSPOT_ELLIPSE) return false;
                return true;

            default:
                return false;
        }
    }

    private static bool CheckOneStr(Mlu mlu, int n)
    {
        Span<byte> Buffer = stackalloc byte[256];
        Span<byte> Buffer2 = stackalloc byte[256];

        cmsMLUgetASCII(mlu, "en"u8, "US"u8, Buffer);
        sprintf(Buffer2, "Hello, world {0}", n);
        if (strcmp(Buffer, Buffer2) != 0) return false;

        cmsMLUgetASCII(mlu, "es"u8, "ES"u8, Buffer);
        sprintf(Buffer2, "Hola, mundo {0}", n);
        if (strcmp(Buffer, Buffer2) != 0) return false;

        return true;
    }

    private static void SetOneStr(out Mlu mlu, string s1, string s2)
    {
        mlu = cmsMLUalloc(DbgThread(), 0);
        cmsMLUsetWide(mlu, "en"u8, "US"u8, s1);
        cmsMLUsetWide(mlu, "es"u8, "ES"u8, s2);
    }

    private static bool CheckProfileSequenceTag(int Pass, Profile hProfile)
    {
        Sequence s;
        int i;

        switch (Pass)
        {
            case 1:

                s = cmsAllocProfileSequenceDescription(DbgThread(), 3);
                if (s == null)
                    return false;

                SetOneStr(out s.seq[0].Manufacturer, "Hello, world 0", "Hola, mundo 0");
                SetOneStr(out s.seq[0].Model, "Hello, world 0", "Hola, mundo 0");
                SetOneStr(out s.seq[1].Manufacturer, "Hello, world 1", "Hola, mundo 1");
                SetOneStr(out s.seq[1].Model, "Hello, world 1", "Hola, mundo 1");
                SetOneStr(out s.seq[2].Manufacturer, "Hello, world 2", "Hola, mundo 2");
                SetOneStr(out s.seq[2].Model, "Hello, world 2", "Hola, mundo 2");

                s.seq[0].attributes = cmsTransparency | cmsMatte;
                s.seq[1].attributes = cmsReflective | cmsMatte;
                s.seq[2].attributes = cmsTransparency | cmsGlossy;

                if (!cmsWriteTag(hProfile, Signature.Tag.ProfileSequenceDesc, s))
                    return false;
                cmsFreeProfileSequenceDescription(s);
                return true;

            case 2:

                s = (cmsReadTag(hProfile, Signature.Tag.ProfileSequenceDesc) is Sequence box) ? box : null;
                if (s == null)
                    return false;

                if (s.n != 3)
                    return false;

                if (s.seq[0].attributes != (cmsTransparency | cmsMatte))
                    return false;
                if (s.seq[1].attributes != (cmsReflective | cmsMatte))
                    return false;
                if (s.seq[2].attributes != (cmsTransparency | cmsGlossy))
                    return false;

                // Check MLU
                for (i = 0; i < 3; i++)
                {
                    if (!CheckOneStr(s.seq[i].Manufacturer, i))
                        return false;
                    if (!CheckOneStr(s.seq[i].Model, i))
                        return false;
                }
                return true;

            default:
                return false;
        }
    }

    private static bool CheckProfileSequenceIDTag(int Pass, Profile hProfile)
    {
        Sequence? s;
        int i;

        switch (Pass)
        {
            case 1:

                s = cmsAllocProfileSequenceDescription(DbgThread(), 3);
                if (s == null) return false;

                s.seq[0].ProfileID = ProfileID.Set("0123456789ABCDEF"u8);
                s.seq[1].ProfileID = ProfileID.Set("1111111111111111"u8);
                s.seq[2].ProfileID = ProfileID.Set("2222222222222222"u8);

                SetOneStr(out s.seq[0].Description, "Hello, world 0", "Hola, mundo 0");
                SetOneStr(out s.seq[1].Description, "Hello, world 1", "Hola, mundo 1");
                SetOneStr(out s.seq[2].Description, "Hello, world 2", "Hola, mundo 2");

                if (!cmsWriteTag(hProfile, Signature.Tag.ProfileSequenceId, s)) return false;
                cmsFreeProfileSequenceDescription(s);
                return true;

            case 2:

                s = (cmsReadTag(hProfile, Signature.Tag.ProfileSequenceId) is Sequence seq) ? seq : null;
                if (s == null) return false;

                if (s.n != 3) return false;

                Span<byte> buf = stackalloc byte[16];
                s.seq[0].ProfileID.Get(buf);
                if (memcmp(buf, "0123456789ABCDEF"u8) != 0) return false;
                s.seq[1].ProfileID.Get(buf);
                if (memcmp(buf, "1111111111111111"u8) != 0) return false;
                s.seq[2].ProfileID.Get(buf);
                if (memcmp(buf, "2222222222222222"u8) != 0) return false;

                for (i = 0; i < 3; i++)
                {
                    if (!CheckOneStr(s.seq[i].Description, i)) return false;
                }

                return true;

            default:
                return false;
        }
    }

    private static bool CheckICCViewingConditions(int Pass, Profile hProfile)
    {
        Box<IccViewingConditions>? v;
        IccViewingConditions s;

        switch (Pass)
        {
            case 1:
                s.IlluminantType = IlluminantType.D50;
                s.IlluminantXYZ.X = 0.1;
                s.IlluminantXYZ.Y = 0.2;
                s.IlluminantXYZ.Z = 0.3;
                s.SurroundXYZ.X = 0.4;
                s.SurroundXYZ.Y = 0.5;
                s.SurroundXYZ.Z = 0.6;

                if (!cmsWriteTag(hProfile, Signature.Tag.ViewingConditions, new Box<IccViewingConditions>(s))) return false;
                return true;

            case 2:
                v = cmsReadTag(hProfile, Signature.Tag.ViewingConditions) as Box<IccViewingConditions>;
                if (v == null) return false;

                if (v.Value.IlluminantType != IlluminantType.D50) return false;
                if (!IsGoodVal("IlluminantXYZ.X", v.Value.IlluminantXYZ.X, 0.1, 0.001)) return false;
                if (!IsGoodVal("IlluminantXYZ.Y", v.Value.IlluminantXYZ.Y, 0.2, 0.001)) return false;
                if (!IsGoodVal("IlluminantXYZ.Z", v.Value.IlluminantXYZ.Z, 0.3, 0.001)) return false;

                if (!IsGoodVal("SurroundXYZ.X", v.Value.SurroundXYZ.X, 0.4, 0.001)) return false;
                if (!IsGoodVal("SurroundXYZ.Y", v.Value.SurroundXYZ.Y, 0.5, 0.001)) return false;
                if (!IsGoodVal("SurroundXYZ.Z", v.Value.SurroundXYZ.Z, 0.6, 0.001)) return false;

                return true;

            default:
                return false;
        }
    }

    private static bool CheckVCGT(int Pass, Profile hProfile)
    {
        var Curves = new ToneCurve[3];
        ToneCurve[] PtrCurve;

        switch (Pass)
        {
            case 1:
                Curves[0] = cmsBuildGamma(DbgThread(), 1.1);
                Curves[1] = cmsBuildGamma(DbgThread(), 2.2);
                Curves[2] = cmsBuildGamma(DbgThread(), 3.4);

                if (!cmsWriteTag(hProfile, Signature.Tag.Vcgt, Curves)) return false;

                cmsFreeToneCurveTriple(Curves);
                return true;

            case 2:

                PtrCurve = (cmsReadTag(hProfile, Signature.Tag.Vcgt) is ToneCurve[] curve) ? curve : null;
                if (PtrCurve == null) return false;
                if (!IsGoodVal("VCGT R", cmsEstimateGamma(PtrCurve[0], 0.01), 1.1, 0.001)) return false;
                if (!IsGoodVal("VCGT G", cmsEstimateGamma(PtrCurve[1], 0.01), 2.2, 0.001)) return false;
                if (!IsGoodVal("VCGT B", cmsEstimateGamma(PtrCurve[2], 0.01), 3.4, 0.001)) return false;
                return true;
        }

        return false;
    }

    // Only one of the two following may be used, as they share the same tag
    //static
    //cmsInt32Number CheckDictionary16(cmsInt32Number Pass, cmsHPROFILE hProfile)
    //{
    //    cmsHANDLE hDict;
    //    const cmsDICTentry* e;
    //    switch (Pass)
    //    {
    //        case 1:
    //            hDict = cmsDictAlloc(DbgThread());
    //            cmsDictAddEntry(hDict, L"Name0", null, null, null);
    //            cmsDictAddEntry(hDict, L"Name1", L"", null, null);
    //            cmsDictAddEntry(hDict, L"Name", L"String", null, null);
    //            cmsDictAddEntry(hDict, L"Name2", L"12", null, null);
    //            if (!cmsWriteTag(hProfile, cmsSigMetaTag, hDict)) return false;
    //            cmsDictFree(hDict);
    //            return true;

    //        case 2:

    //            hDict = cmsReadTag(hProfile, cmsSigMetaTag);
    //            if (hDict == null) return false;
    //            e = cmsDictGetEntryList(hDict);
    //            if (memcmp(e->Name, L"Name2", sizeof(wchar_t) * 5) != 0) return false;
    //            if (memcmp(e->Value, L"12", sizeof(wchar_t) * 2) != 0) return false;
    //            e = cmsDictNextEntry(e);
    //            if (memcmp(e->Name, L"Name", sizeof(wchar_t) * 4) != 0) return false;
    //            if (memcmp(e->Value, L"String", sizeof(wchar_t) * 5) != 0) return false;
    //            e = cmsDictNextEntry(e);
    //            if (memcmp(e->Name, L"Name1", sizeof(wchar_t) * 5) != 0) return false;
    //            if (e->Value == null) return false;
    //            if (*e->Value != 0) return false;
    //            e = cmsDictNextEntry(e);
    //            if (memcmp(e->Name, L"Name0", sizeof(wchar_t) * 5) != 0) return false;
    //            if (e->Value != null) return false;
    //            return true;

    //        default:;
    //    }

    //    return false;
    //}

    private static bool CheckDictionary24(int Pass, Profile hProfile)
    {
        Dictionary hDict;
        Dictionary.Entry e;
        Mlu DisplayName;
        Span<byte> Buffer = stackalloc byte[256];
        bool rc = true;

        switch (Pass)
        {
            case 1:
                hDict = cmsDictAlloc(DbgThread());

                DisplayName = cmsMLUalloc(DbgThread(), 0);

                cmsMLUsetWide(DisplayName, "en"u8, "US"u8, "Hello, world");
                cmsMLUsetWide(DisplayName, "es"u8, "ES"u8, "Hola, mundo");
                cmsMLUsetWide(DisplayName, "fr"u8, "FR"u8, "Bonjour, le monde");
                cmsMLUsetWide(DisplayName, "ca"u8, "CA"u8, "Hola, mon");

                cmsDictAddEntry(hDict, "Name", "String", DisplayName, null);
                cmsMLUfree(DisplayName);

                cmsDictAddEntry(hDict, "Name2", "12", null, null);
                if (!cmsWriteTag(hProfile, Signature.Tag.Meta, hDict)) return false;
                cmsDictFree(hDict);

                return true;

            case 2:

                hDict = (cmsReadTag(hProfile, Signature.Tag.Meta) as Dictionary)!;
                if (hDict == null) return false;

                e = cmsDictGetEntryList(hDict);
                if (String.CompareOrdinal(e.Name, "Name2") != 0) return false;
                if (String.CompareOrdinal(e.Value, "12") != 0) return false;
                e = cmsDictNextEntry(e);
                if (String.CompareOrdinal(e.Name, "Name") != 0) return false;
                if (String.CompareOrdinal(e.Value, "String") != 0) return false;

                if (e.DisplayName is null)
                    return false;

                cmsMLUgetASCII(e.DisplayName, "en"u8, "US"u8, Buffer);
                if (strcmp(Buffer, "Hello, world"u8) != 0) rc = false;

                cmsMLUgetASCII(e.DisplayName, "es"u8, "ES"u8, Buffer);
                if (strcmp(Buffer, "Hola, mundo"u8) != 0) rc = false;

                cmsMLUgetASCII(e.DisplayName, "fr"u8, "FR"u8, Buffer);
                if (strcmp(Buffer, "Bonjour, le monde"u8) != 0) rc = false;

                cmsMLUgetASCII(e.DisplayName, "ca"u8, "CA"u8, Buffer);
                if (strcmp(Buffer, "Hola, mon"u8) != 0) rc = false;

                if (!rc)
                    logger.LogWarning("Unexpected string '{str}'", Encoding.ASCII.GetString(Buffer));
                return true;
        }

        return false;
    }

    private static bool CheckRAWtags(int Pass, Profile hProfile)
    {
        switch (Pass)
        {
            case 1:
                return cmsWriteRawTag(hProfile, (Signature)0x31323334, "data123"u8, 7);

            case 2:
                var Buffer = new byte[16];
                if (cmsReadRawTag(hProfile, (Signature)0x31323334, Buffer, 7) is 0) return false;

                if (Buffer.AsSpan(..7).SequenceCompareTo("data123"u8) != 0) return false;
                return true;

            default:
                return false;
        }
    }

    private static bool Check_cicp(int Pass, Profile hProfile)
    {
        switch (Pass)
        {
            case 1:
                var s = new VideoSignalType()
                {
                    ColourPrimaries = 1,
                    TransferCharacteristics = 13,
                    MatrixCoefficients = 0,
                    VideoFullRangeFlag = 1
                };
                return cmsWriteTag(hProfile, Signature.Tag.cicp, new Box<VideoSignalType>(s));

            case 2:
                if (cmsReadTag(hProfile, Signature.Tag.cicp) is not Box<VideoSignalType> vs)
                    return false;
                var v = vs.Value;

                if (v.ColourPrimaries is not 1) return false;
                if (v.TransferCharacteristics is not 13) return false;
                if (v.MatrixCoefficients is not 0) return false;
                if (v.VideoFullRangeFlag is not 1) return false;

                return true;

            default:
                return false;
        }
    }

    private static bool Check_MHC2(int Pass, Profile hProfile)
    {
        double[] curve = [0.0, 0.5, 1.0];

        switch (Pass)
        {
            case 1:
                var s = new MHC2
                {
                    matrix = new double[12],
                    entries = 3,
                    redCurve = curve,
                    greenCurve = curve,
                    blueCurve = curve,
                    minLuminance = 0.1,
                    peakLuminance = 100.0
                };

                SetMHC2Matrix(s.matrix);

                if (!cmsWriteTag(hProfile, Signature.Tag.MHC2, new Box<MHC2>(s)))
                    return false;
                return true;

            case 2:
                if (cmsReadTag(hProfile, Signature.Tag.MHC2) is not Box<MHC2> v) return false;

                if (!IsOriginalMHC2Matrix(v.Value.matrix)) return false;
                if (v.Value.entries is not 3) return false;

                return true;

            default: return false;
        }
    }

    internal static bool CheckVersionHeaderWriting()
    {
        Span<float> test_versions = stackalloc float[]
        {
            2.3f,
            4.08f,
            4.09f,
            4.3f
        };

        for (var index = 0; index < test_versions.Length; index++)
        {
            var h = cmsCreateProfilePlaceholder(DbgThread());
            if (h is null)
                return false;

            cmsSetProfileVersion(h, test_versions[index]);

            cmsSaveProfileToFile(h, "versions.icc");
            cmsCloseProfile(h);

            h = cmsOpenProfileFromFileTHR(DbgThread(), "versions.icc", "r");

            // Only the first 3 digits are significant
            if (Math.Abs(cmsGetProfileVersion(h) - test_versions[index]) > 0.005)
            {
                logger.LogError("Version failed to round-trip: wrote {expected:f2}, read {actual:f2}", test_versions[index], cmsGetProfileVersion(h));

                return false;
            }

            cmsCloseProfile(h);
            File.Delete("versions.icc");
        }

        return true;
    }

    internal static bool CheckMultilocalizedProfile()
    {
        Span<byte> Buffer = stackalloc byte[256];
        var hProfile = cmsOpenProfileFromMem(TestProfiles.crayons)!;

        var Pt = (Mlu)cmsReadTag(hProfile, Signature.Tag.ProfileDescription)!;
        cmsMLUgetASCII(Pt, "en"u8, "GB"u8, Buffer);
        if (strcmp(Buffer, "Crayon Colours"u8) is not 0)
            return false;
        cmsMLUgetASCII(Pt, "en"u8, "US"u8, Buffer);
        if (strcmp(Buffer, "Crayon Colors"u8) is not 0)
            return false;

        cmsCloseProfile(hProfile);

        return true;
    }
}
