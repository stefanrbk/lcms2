using System.Diagnostics;
using System.Runtime.CompilerServices;
using lcms2.io;
using lcms2.state;
using lcms2.types;
using Microsoft.Extensions.Logging;

namespace lcms2.legacy;

[DebuggerStepThrough]
public class Lcms2
{
    // Version/release
    public const ushort LCMS_VERSION = Context.LibraryVersion;

    public const ushort cmsMAX_PATH = lcms2.Lcms2.MaxPath;

    // D50 XYZ normalized to Y=1.0
    public static readonly double cmsD50X = CIEXYZ.D50.X;
    public static readonly double cmsD50Y = CIEXYZ.D50.Y;
    public static readonly double cmsD50Z = CIEXYZ.D50.Z;

    // V4 perceptual black
    public static readonly double cmsPERCEPTUAL_BLACK_X = Profile.PerceptualBlack.X;
    public static readonly double cmsPERCEPTUAL_BLACK_Y = Profile.PerceptualBlack.Y;
    public static readonly double cmsPERCEPTUAL_BLACK_Z = Profile.PerceptualBlack.Z;

    // Definitions in ICC spec
    public const uint cmsMagicNumber = 0x61637370;
    public const uint lcmsSignature = 0x6c636d73;

    // Base ICC type definitions
    public const uint cmsSigChromaticityType = 0x6368726D;
    public const uint cmsSigcicpType = 0x63696370;
    public const uint cmsSigColorantOrderType = 0x636C726F;
    public const uint cmsSigColorantTableType = 0x636C7274;
    public const uint cmsSigCrdInfoType = 0x63726469;
    public const uint cmsSigCurveType = 0x63757276;
    public const uint cmsSigDataType = 0x64617461;
    public const uint cmsSigDictType = 0x64696374;
    public const uint cmsSigDateTimeType = 0x6474696D;
    public const uint cmsSigDeviceSettingsType = 0x64657673;
    public const uint cmsSigLut16Type = 0x6d667432;
    public const uint cmsSigLut8Type = 0x6d667431;
    public const uint cmsSigLutAtoBType = 0x6d414220;
    public const uint cmsSigLutBtoAType = 0x6d424120;
    public const uint cmsSigMeasurementType = 0x6D656173;
    public const uint cmsSigMultiLocalizedUnicodeType = 0x6D6C7563;
    public const uint cmsSigMultiProcessElementType = 0x6D706574;

    [Obsolete]
    public const uint cmsSigNamedColorType = 0x6E636f6C;

    public const uint cmsSigNamedColor2Type = 0x6E636C32;
    public const uint cmsSigParametricCurveType = 0x70617261;
    public const uint cmsSigProfileSequenceDescType = 0x70736571;
    public const uint cmsSigProfileSequenceIdType = 0x70736964;
    public const uint cmsSigResponseCurveSet16Type = 0x72637332;
    public const uint cmsSigS15Fixed16ArrayType = 0x73663332;
    public const uint cmsSigScreeningType = 0x7363726E;
    public const uint cmsSigSignatureType = 0x73696720;
    public const uint cmsSigTextType = 0x74657874;
    public const uint cmsSigTextDescriptionType = 0x64657363;
    public const uint cmsSigU16Fixed16ArrayType = 0x75663332;
    public const uint cmsSigUcrBgType = 0x62666420;
    public const uint cmsSigUInt16ArrayType = 0x75693136;
    public const uint cmsSigUInt32ArrayType = 0x75693332;
    public const uint cmsSigUInt64ArrayType = 0x75693634;
    public const uint cmsSigUInt8ArrayType = 0x75693038;
    public const uint cmsSigVcgtType = 0x76636774;
    public const uint cmsSigViewingConditionsType = 0x76696577;
    public const uint cmsSigXYZType = 0x58595A20;
    public const uint cmsSigMHC2Type = 0x4D484332;

    // Base ICC tag definitions
    public const uint cmsSigAToB0Tag = 0x41324230;
    public const uint cmsSigAToB1Tag = 0x41324231;
    public const uint cmsSigAToB2Tag = 0x41324232;
    public const uint cmsSigBlueColorantTag = 0x6258595A;
    public const uint cmsSigBlueMatrixColumnTag = 0x6258595A;
    public const uint cmsSigBlueTRCTag = 0x62545243;
    public const uint cmsSigBToA0Tag = 0x42324130;
    public const uint cmsSigBToA1Tag = 0x42324131;
    public const uint cmsSigBToA2Tag = 0x42324132;
    public const uint cmsSigCalibrationDateTimeTag = 0x63616C74;
    public const uint cmsSigCharTargetTag = 0x74617267;
    public const uint cmsSigChromaticAdaptationTag = 0x63686164;
    public const uint cmsSigChromaticityTag = 0x6368726D;
    public const uint cmsSigColorantOrderTag = 0x636C726F;
    public const uint cmsSigColorantTableTag = 0x636C7274;
    public const uint cmsSigColorantTableOutTag = 0x636C6F74;
    public const uint cmsSigColorimetricIntentImageStateTag = 0x63696973;
    public const uint cmsSigCopyrightTag = 0x63707274;
    public const uint cmsSigCrdInfoTag = 0x63726469;
    public const uint cmsSigDataTag = 0x64617461;
    public const uint cmsSigDateTimeTag = 0x6474696D;
    public const uint cmsSigDeviceMfgDescTag = 0x646D6E64;
    public const uint cmsSigDeviceModelDescTag = 0x646D6464;
    public const uint cmsSigDeviceSettingsTag = 0x64657673;
    public const uint cmsSigDToB0Tag = 0x44324230;
    public const uint cmsSigDToB1Tag = 0x44324231;
    public const uint cmsSigDToB2Tag = 0x44324232;
    public const uint cmsSigDToB3Tag = 0x44324233;
    public const uint cmsSigBToD0Tag = 0x42324430;
    public const uint cmsSigBToD1Tag = 0x42324431;
    public const uint cmsSigBToD2Tag = 0x42324432;
    public const uint cmsSigBToD3Tag = 0x42324433;
    public const uint cmsSigGamutTag = 0x67616D74;
    public const uint cmsSigGrayTRCTag = 0x6b545243;
    public const uint cmsSigGreenColorantTag = 0x6758595A;
    public const uint cmsSigGreenMatrixColumnTag = 0x6758595A;
    public const uint cmsSigGreenTRCTag = 0x67545243;
    public const uint cmsSigLuminanceTag = 0x6C756d69;
    public const uint cmsSigMeasurementTag = 0x6D656173;
    public const uint cmsSigMediaBlackPointTag = 0x626B7074;
    public const uint cmsSigMediaWhitePointTag = 0x77747074;
    public const uint cmsSigNamedColorTag = 0x6E636f6C;
    public const uint cmsSigNamedColor2Tag = 0x6E636C32;
    public const uint cmsSigOutputResponseTag = 0x72657370;
    public const uint cmsSigPerceptualRenderingIntentGamutTag = 0x72696730;
    public const uint cmsSigPreview0Tag = 0x70726530;
    public const uint cmsSigPreview1Tag = 0x70726531;
    public const uint cmsSigPreview2Tag = 0x70726532;
    public const uint cmsSigProfileDescriptionTag = 0x64657363;
    public const uint cmsSigProfileDescriptionMLTag = 0x6473636d;
    public const uint cmsSigProfileSequenceDescTag = 0x70736571;
    public const uint cmsSigProfileSequenceIdTag = 0x70736964;
    public const uint cmsSigPs2CRD0Tag = 0x70736430;
    public const uint cmsSigPs2CRD1Tag = 0x70736431;
    public const uint cmsSigPs2CRD2Tag = 0x70736432;
    public const uint cmsSigPs2CRD3Tag = 0x70736433;
    public const uint cmsSigPs2CSATag = 0x70733273;
    public const uint cmsSigPs2RenderingIntentTag = 0x70733269;
    public const uint cmsSigRedColorantTag = 0x7258595A;
    public const uint cmsSigRedMatrixColumnTag = 0x7258595A;
    public const uint cmsSigRedTRCTag = 0x72545243;
    public const uint cmsSigSaturationRenderingIntentGamutTag = 0x72696732;
    public const uint cmsSigScreeningDescTag = 0x73637264;
    public const uint cmsSigScreeningTag = 0x7363726E;
    public const uint cmsSigTechnologyTag = 0x74656368;
    public const uint cmsSigUcrBgTag = 0x62666420;
    public const uint cmsSigViewingCondDescTag = 0x76756564;
    public const uint cmsSigViewingConditionsTag = 0x76696577;
    public const uint cmsSigVcgtTag = 0x76636774;
    public const uint cmsSigMetaTag = 0x6D657461;
    public const uint cmsSigcicpTag = 0x63696370;
    public const uint cmsSigArgyllArtsTag = 0x61727473;
    public const uint cmsSigMHC2Tag = 0x4D484332;

    // ICC Technology tag
    public const uint cmsSigDigitalCamera = 0x6463616D;
    public const uint cmsSigFilmScanner = 0x6673636E;
    public const uint cmsSigReflectiveScanner = 0x7273636E;
    public const uint cmsSigInkJetPrinter = 0x696A6574;
    public const uint cmsSigThermalWaxPrinter = 0x74776178;
    public const uint cmsSigElectrophotographicPrinter = 0x6570686F;
    public const uint cmsSigElectrostaticPrinter = 0x65737461;
    public const uint cmsSigDyeSublimationPrinter = 0x64737562;
    public const uint cmsSigPhotographicPaperPrinter = 0x7270686F;
    public const uint cmsSigFilmWriter = 0x6670726E;
    public const uint cmsSigVideoMonitor = 0x7669646D;
    public const uint cmsSigVideoCamera = 0x76696463;
    public const uint cmsSigProjectionTelevision = 0x706A7476;
    public const uint cmsSigCRTDisplay = 0x43525420;
    public const uint cmsSigPMDisplay = 0x504D4420;
    public const uint cmsSigAMDisplay = 0x414D4420;
    public const uint cmsSigPhotoCD = 0x4B504344;
    public const uint cmsSigPhotoImageSetter = 0x696D6773;
    public const uint cmsSigGravure = 0x67726176;
    public const uint cmsSigOffsetLithography = 0x6F666673;
    public const uint cmsSigSilkscreen = 0x73696C6B;
    public const uint cmsSigFlexography = 0x666C6578;
    public const uint cmsSigMotionPictureFilmScanner = 0x6D706673;
    public const uint cmsSigMotionPictureFilmRecorder = 0x6D706672;
    public const uint cmsSigDigitalMotionPictureCamera = 0x646D7063;
    public const uint cmsSigDigitalCinemaProjector = 0x64636A70;

    // ICC Color spaces
    public const uint cmsSigXYZData = 0x58595A20;
    public const uint cmsSigLabData = 0x4C616220;
    public const uint cmsSigLuvData = 0x4C757620;
    public const uint cmsSigYCbCrData = 0x59436272;
    public const uint cmsSigYxyData = 0x59787920;
    public const uint cmsSigRgbData = 0x52474220;
    public const uint cmsSigGrayData = 0x47524159;
    public const uint cmsSigHsvData = 0x48535620;
    public const uint cmsSigHlsData = 0x484C5320;
    public const uint cmsSigCmykData = 0x434D594B;
    public const uint cmsSigCmyData = 0x434D5920;
    public const uint cmsSigMCH1Data = 0x4D434831;
    public const uint cmsSigMCH2Data = 0x4D434832;
    public const uint cmsSigMCH3Data = 0x4D434833;
    public const uint cmsSigMCH4Data = 0x4D434834;
    public const uint cmsSigMCH5Data = 0x4D434835;
    public const uint cmsSigMCH6Data = 0x4D434836;
    public const uint cmsSigMCH7Data = 0x4D434837;
    public const uint cmsSigMCH8Data = 0x4D434838;
    public const uint cmsSigMCH9Data = 0x4D434839;
    public const uint cmsSigMCHAData = 0x4D434841;
    public const uint cmsSigMCHBData = 0x4D434842;
    public const uint cmsSigMCHCData = 0x4D434843;
    public const uint cmsSigMCHDData = 0x4D434844;
    public const uint cmsSigMCHEData = 0x4D434845;
    public const uint cmsSigMCHFData = 0x4D434846;
    public const uint cmsSigNamedData = 0x6e6d636c;
    public const uint cmsSig1colorData = 0x31434C52;
    public const uint cmsSig2colorData = 0x32434C52;
    public const uint cmsSig3colorData = 0x33434C52;
    public const uint cmsSig4colorData = 0x34434C52;
    public const uint cmsSig5colorData = 0x35434C52;
    public const uint cmsSig6colorData = 0x36434C52;
    public const uint cmsSig7colorData = 0x37434C52;
    public const uint cmsSig8colorData = 0x38434C52;
    public const uint cmsSig9colorData = 0x39434C52;
    public const uint cmsSig10colorData = 0x41434C52;
    public const uint cmsSig11colorData = 0x42434C52;
    public const uint cmsSig12colorData = 0x43434C52;
    public const uint cmsSig13colorData = 0x44434C52;
    public const uint cmsSig14colorData = 0x45434C52;
    public const uint cmsSig15colorData = 0x46434C52;
    public const uint cmsSigLuvKData = 0x4C75764B;

    // ICC Profile Class
    public const uint cmsSigInputClass = 0x73636E72;
    public const uint cmsSigDisplayClass = 0x6D6E7472;
    public const uint cmsSigOutputClass = 0x70727472;
    public const uint cmsSigLinkClass = 0x6C696E6B;
    public const uint cmsSigAbstractClass = 0x61627374;
    public const uint cmsSigColorSpaceClass = 0x73706163;
    public const uint cmsSigNamedColorClass = 0x6e6d636c;

    // ICC Platforms
    public const uint cmsSigMacintosh = 0x4150504C;
    public const uint cmsSigMicrosoft = 0x4D534654;
    public const uint cmsSigSolaris = 0x53554E57;
    public const uint cmsSigSGI = 0x53474920;
    public const uint cmsSigTaligent = 0x54474E54;
    public const uint cmsSigUnices = 0x2A6E6978;

    // Reference gamut
    public const uint cmsSigPerceptualReferenceMediumGamut = 0x70726d67;

    // For cmsSigColorimetricIntentImageStateTag
    public const uint cmsSigSceneColorimetryEstimates = 0x73636F65;
    public const uint cmsSigSceneAppearanceEstimates = 0x73617065;
    public const uint cmsSigFocalPlaneColorimetryEstimates = 0x66706365;
    public const uint cmsSigReflectionHardcopyOriginalColorimetry = 0x72686F63;
    public const uint cmsSigReflectionPrintOutputColorimetry = 0x72706F63;

    // Multi process elements types
    public const uint cmsSigCurveSetElemType = 0x63767374;
    public const uint cmsSigMatrixElemType = 0x6D617466;
    public const uint cmsSigCLutElemType = 0x636C7574;

    public const uint cmsSigBAcsElemType = 0x62414353;
    public const uint cmsSigEAcsElemType = 0x65414353;

    // Custom from here, not in the ICC Spec
    public const uint cmsSigXYZ2LabElemType = 0x6C327820;
    public const uint cmsSigLab2XYZElemType = 0x78326C20;
    public const uint cmsSigNamedColorElemType = 0x6E636C20;
    public const uint cmsSigLabV2toV4 = 0x32203420;
    public const uint cmsSigLabV4toV2 = 0x34203220;

    // Identities
    public const uint cmsSigIdentityElemType = 0x69646E20;

    // Float to floatPCS
    public const uint cmsSigLab2FloatPCS = 0x64326C20;

    public const uint cmsSigFloatPCS2Lab = 0x6C326420;
    public const uint cmsSigXYZ2FloatPCS = 0x64327820;
    public const uint cmsSigFloatPCS2XYZ = 0x78326420;
    public const uint cmsSigClipNegativesElemType = 0x636c7020;

    // Types of CurveElements
    public const uint cmsSigFormulaCurveSeg = 0x70617266;
    public const uint cmsSigSampledCurveSeg = 0x73616D66;
    public const uint cmsSigSegmentedCurve = 0x63757266;

    // Used in ResponseCurveType
    public const uint cmsSigStatusA = 0x53746141;
    public const uint cmsSigStatusE = 0x53746145;
    public const uint cmsSigStatusI = 0x53746149;
    public const uint cmsSigStatusT = 0x53746154;
    public const uint cmsSigStatusM = 0x5374614D;
    public const uint cmsSigDN = 0x444E2020;
    public const uint cmsSigDNP = 0x444E2050;
    public const uint cmsSigDNN = 0x444E4E20;
    public const uint cmsSigDNNP = 0x444E4E50;

    // Get version
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int cmsGetEncodedCMMversion() =>
        Context.LibraryVersion;

    // Support of non-standard functions
    public static int cmsstrcasecmp(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
    {
        var us1 = Char.ToUpper((char)s1[0]);
        var us2 = Char.ToUpper((char)s2[0]);

        while (us1 == us2)
        {
            // If both spans reach a '0' at the same time...
            if (us1 is '\0')
                return 0;

            s1 = s1[1..];
            s2 = s2[1..];

            // If both spans are now empty...
            if (s1.Length is 0 && s2.Length is 0)
                return 0;

            // If the 1st span is empty and the 2nd is not...
            if (s1.Length is 0)
                return us2;

            // If the 2nd span is empty and the 1st is not...
            if (s2.Length is 0)
                return -us1;

            us1 = Char.ToUpper((char)s1[0]);
            us2 = Char.ToUpper((char)s2[0]);
        }

        return us1 - us2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long cmsfilelength(FILE f) =>
        f.Length();

    // Context handling

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Context cmsCreateContext(IEnumerable<PluginBase> Plugins, object? UserData = null) =>
        new(Plugins, UserData);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Context cmsDupContext(Context? context, object? NewUserData) =>
        context?.Clone(NewUserData) ?? Context.Shared.Clone(NewUserData);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref object? cmsGetContextUserData(Context? context) =>
        ref context is null ? ref Context.Shared.UserData : ref context.UserData;

    // Plugin registering

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool cmsPlugin(IEnumerable<PluginBase> Plugins)
    {
        Context.Shared.RegisterPlugin(Plugins);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool cmsPluginTHR(Context? id, IEnumerable<PluginBase> Plugins)
    {
        (id ?? Context.Shared).RegisterPlugin(Plugins);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsUnregisterPlugins() =>
        Context.Shared.ClearAllPlugins();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsUnregisterPluginsTHR(Context? context) =>
        (context ?? Context.Shared).ClearAllPlugins();

    // Error logger

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsSetLogErrorHandlerTHR(Context? context, ILoggerFactory? factory) =>
        (context ?? Context.Shared).SetLoggerFactory(factory ?? lcms2.Lcms2.DefaultLogErrorHandlerFunction());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsSetLogErrorHandler(ILoggerFactory? factory) =>
        Context.Shared.SetLoggerFactory(factory ?? lcms2.Lcms2.DefaultLogErrorHandlerFunction());

    // "Constant" structs

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIEXYZ cmsD50_XYZ() =>
        CIEXYZ.D50;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIExyY cmsD50_xyY() =>
        CIExyY.D50;

    // Colorimetric space conversions

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsXYZ2xyY(out CIExyY Dest, CIEXYZ Source) =>
        Dest = Source.As_xyY;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsxyY2XYZ(out CIEXYZ Dest, CIExyY Source) =>
        Dest = Source.AsXYZ;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsXYZ2Lab(CIEXYZ? WhitePoint, out CIELab Lab, CIEXYZ xyz) =>
        Lab = xyz.AsLab(WhitePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsLab2XYZ(CIEXYZ? WhitePoint, out CIEXYZ xyz, CIELab Lab) =>
        xyz = Lab.AsXYZ(WhitePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsLab2LCh(out CIELCh LCh, CIELab Lab) =>
        LCh = Lab.AsLCh;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void cmsLCh2Lab(out CIELab Lab, CIELCh LCh) =>
        Lab = LCh.AsLab;
}
