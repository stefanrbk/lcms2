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

using lcms2.io;
using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace lcms2;

public static partial class Lcms2
{
    internal static readonly List<(FILE file, int count)> OpenFiles = new();
    //internal static readonly Dictionary<nuint, (Type type, nuint size, bool freed)> AllocList = new();
    internal static readonly Dictionary<LogErrorChunkType, ILogger> loggers = new();

    #region lcms2.h

    internal const ushort LCMS_VERSION = 2131;

    internal const ushort cmsMAX_PATH = 256;

    internal const double cmsD50X = 0.9642;
    internal const double cmsD50Y = 1.0;
    internal const double cmsD50Z = 0.8249;

    internal const double cmsPERCEPTUAL_BLACK_X = 0.00336;
    internal const double cmsPERCEPTUAL_BLACK_Y = 0.0034731;
    internal const double cmsPERCEPTUAL_BLACK_Z = 0.00287;

    internal const uint cmsMagicNumber = 0x61637370;
    internal const uint lcmsSignature = 0x6c636d73;

    internal const uint cmsSigChromaticityType = 0x6368726D;
    internal const uint cmsSigColorantOrderType = 0x636C726F;
    internal const uint cmsSigColorantTableType = 0x636C7274;
    internal const uint cmsSigCrdInfoType = 0x63726469;
    internal const uint cmsSigCurveType = 0x63757276;
    internal const uint cmsSigDataType = 0x64617461;
    internal const uint cmsSigDictType = 0x64696374;
    internal const uint cmsSigDateTimeType = 0x6474696D;
    internal const uint cmsSigDeviceSettingsType = 0x64657673;
    internal const uint cmsSigLut16Type = 0x6d667432;
    internal const uint cmsSigLut8Type = 0x6d667431;
    internal const uint cmsSigLutAtoBType = 0x6d414220;
    internal const uint cmsSigLutBtoAType = 0x6d424120;
    internal const uint cmsSigMeasurementType = 0x6D656173;
    internal const uint cmsSigMultiLocalizedUnicodeType = 0x6D6C7563;
    internal const uint cmsSigMultiProcessElementType = 0x6D706574;

    [Obsolete]
    internal const uint cmsSigNamedColorType = 0x6E636f6C;

    internal const uint cmsSigNamedColor2Type = 0x6E636C32;
    internal const uint cmsSigParametricCurveType = 0x70617261;
    internal const uint cmsSigProfileSequenceDescType = 0x70736571;
    internal const uint cmsSigProfileSequenceIdType = 0x70736964;
    internal const uint cmsSigResponseCurveSet16Type = 0x72637332;
    internal const uint cmsSigS15Fixed16ArrayType = 0x73663332;
    internal const uint cmsSigScreeningType = 0x7363726E;
    internal const uint cmsSigSignatureType = 0x73696720;
    internal const uint cmsSigTextType = 0x74657874;
    internal const uint cmsSigTextDescriptionType = 0x64657363;
    internal const uint cmsSigU16Fixed16ArrayType = 0x75663332;
    internal const uint cmsSigUcrBgType = 0x62666420;
    internal const uint cmsSigUInt16ArrayType = 0x75693136;
    internal const uint cmsSigUInt32ArrayType = 0x75693332;
    internal const uint cmsSigUInt64ArrayType = 0x75693634;
    internal const uint cmsSigUInt8ArrayType = 0x75693038;
    internal const uint cmsSigVcgtType = 0x76636774;
    internal const uint cmsSigViewingConditionsType = 0x76696577;
    internal const uint cmsSigXYZType = 0x58595A20;

    internal const uint cmsSigAToB0Tag = 0x41324230;
    internal const uint cmsSigAToB1Tag = 0x41324231;
    internal const uint cmsSigAToB2Tag = 0x41324232;
    internal const uint cmsSigBlueColorantTag = 0x6258595A;
    internal const uint cmsSigBlueMatrixColumnTag = 0x6258595A;
    internal const uint cmsSigBlueTRCTag = 0x62545243;
    internal const uint cmsSigBToA0Tag = 0x42324130;
    internal const uint cmsSigBToA1Tag = 0x42324131;
    internal const uint cmsSigBToA2Tag = 0x42324132;
    internal const uint cmsSigCalibrationDateTimeTag = 0x63616C74;
    internal const uint cmsSigCharTargetTag = 0x74617267;
    internal const uint cmsSigChromaticAdaptationTag = 0x63686164;
    internal const uint cmsSigChromaticityTag = 0x6368726D;
    internal const uint cmsSigColorantOrderTag = 0x636C726F;
    internal const uint cmsSigColorantTableTag = 0x636C7274;
    internal const uint cmsSigColorantTableOutTag = 0x636C6F74;
    internal const uint cmsSigColorimetricIntentImageStateTag = 0x63696973;
    internal const uint cmsSigCopyrightTag = 0x63707274;
    internal const uint cmsSigCrdInfoTag = 0x63726469;
    internal const uint cmsSigDataTag = 0x64617461;
    internal const uint cmsSigDateTimeTag = 0x6474696D;
    internal const uint cmsSigDeviceMfgDescTag = 0x646D6E64;
    internal const uint cmsSigDeviceModelDescTag = 0x646D6464;
    internal const uint cmsSigDeviceSettingsTag = 0x64657673;
    internal const uint cmsSigDToB0Tag = 0x44324230;
    internal const uint cmsSigDToB1Tag = 0x44324231;
    internal const uint cmsSigDToB2Tag = 0x44324232;
    internal const uint cmsSigDToB3Tag = 0x44324233;
    internal const uint cmsSigBToD0Tag = 0x42324430;
    internal const uint cmsSigBToD1Tag = 0x42324431;
    internal const uint cmsSigBToD2Tag = 0x42324432;
    internal const uint cmsSigBToD3Tag = 0x42324433;
    internal const uint cmsSigGamutTag = 0x67616D74;
    internal const uint cmsSigGrayTRCTag = 0x6b545243;
    internal const uint cmsSigGreenColorantTag = 0x6758595A;
    internal const uint cmsSigGreenMatrixColumnTag = 0x6758595A;
    internal const uint cmsSigGreenTRCTag = 0x67545243;
    internal const uint cmsSigLuminanceTag = 0x6C756d69;
    internal const uint cmsSigMeasurementTag = 0x6D656173;
    internal const uint cmsSigMediaBlackPointTag = 0x626B7074;
    internal const uint cmsSigMediaWhitePointTag = 0x77747074;
    internal const uint cmsSigNamedColorTag = 0x6E636f6C;
    internal const uint cmsSigNamedColor2Tag = 0x6E636C32;
    internal const uint cmsSigOutputResponseTag = 0x72657370;
    internal const uint cmsSigPerceptualRenderingIntentGamutTag = 0x72696730;
    internal const uint cmsSigPreview0Tag = 0x70726530;
    internal const uint cmsSigPreview1Tag = 0x70726531;
    internal const uint cmsSigPreview2Tag = 0x70726532;
    internal const uint cmsSigProfileDescriptionTag = 0x64657363;
    internal const uint cmsSigProfileDescriptionMLTag = 0x6473636d;
    internal const uint cmsSigProfileSequenceDescTag = 0x70736571;
    internal const uint cmsSigProfileSequenceIdTag = 0x70736964;
    internal const uint cmsSigPs2CRD0Tag = 0x70736430;
    internal const uint cmsSigPs2CRD1Tag = 0x70736431;
    internal const uint cmsSigPs2CRD2Tag = 0x70736432;
    internal const uint cmsSigPs2CRD3Tag = 0x70736433;
    internal const uint cmsSigPs2CSATag = 0x70733273;
    internal const uint cmsSigPs2RenderingIntentTag = 0x70733269;
    internal const uint cmsSigRedColorantTag = 0x7258595A;
    internal const uint cmsSigRedMatrixColumnTag = 0x7258595A;
    internal const uint cmsSigRedTRCTag = 0x72545243;
    internal const uint cmsSigSaturationRenderingIntentGamutTag = 0x72696732;
    internal const uint cmsSigScreeningDescTag = 0x73637264;
    internal const uint cmsSigScreeningTag = 0x7363726E;
    internal const uint cmsSigTechnologyTag = 0x74656368;
    internal const uint cmsSigUcrBgTag = 0x62666420;
    internal const uint cmsSigViewingCondDescTag = 0x76756564;
    internal const uint cmsSigViewingConditionsTag = 0x76696577;
    internal const uint cmsSigVcgtTag = 0x76636774;
    internal const uint cmsSigMetaTag = 0x6D657461;
    internal const uint cmsSigArgyllArtsTag = 0x61727473;

    internal const uint cmsSigDigitalCamera = 0x6463616D;
    internal const uint cmsSigFilmScanner = 0x6673636E;
    internal const uint cmsSigReflectiveScanner = 0x7273636E;
    internal const uint cmsSigInkJetPrinter = 0x696A6574;
    internal const uint cmsSigThermalWaxPrinter = 0x74776178;
    internal const uint cmsSigElectrophotographicPrinter = 0x6570686F;
    internal const uint cmsSigElectrostaticPrinter = 0x65737461;
    internal const uint cmsSigDyeSublimationPrinter = 0x64737562;
    internal const uint cmsSigPhotographicPaperPrinter = 0x7270686F;
    internal const uint cmsSigFilmWriter = 0x6670726E;
    internal const uint cmsSigVideoMonitor = 0x7669646D;
    internal const uint cmsSigVideoCamera = 0x76696463;
    internal const uint cmsSigProjectionTelevision = 0x706A7476;
    internal const uint cmsSigCRTDisplay = 0x43525420;
    internal const uint cmsSigPMDisplay = 0x504D4420;
    internal const uint cmsSigAMDisplay = 0x414D4420;
    internal const uint cmsSigPhotoCD = 0x4B504344;
    internal const uint cmsSigPhotoImageSetter = 0x696D6773;
    internal const uint cmsSigGravure = 0x67726176;
    internal const uint cmsSigOffsetLithography = 0x6F666673;
    internal const uint cmsSigSilkscreen = 0x73696C6B;
    internal const uint cmsSigFlexography = 0x666C6578;
    internal const uint cmsSigMotionPictureFilmScanner = 0x6D706673;
    internal const uint cmsSigMotionPictureFilmRecorder = 0x6D706672;
    internal const uint cmsSigDigitalMotionPictureCamera = 0x646D7063;
    internal const uint cmsSigDigitalCinemaProjector = 0x64636A70;

    internal const uint cmsSigXYZData = 0x58595A20;
    internal const uint cmsSigLabData = 0x4C616220;
    internal const uint cmsSigLuvData = 0x4C757620;
    internal const uint cmsSigYCbCrData = 0x59436272;
    internal const uint cmsSigYxyData = 0x59787920;
    internal const uint cmsSigRgbData = 0x52474220;
    internal const uint cmsSigGrayData = 0x47524159;
    internal const uint cmsSigHsvData = 0x48535620;
    internal const uint cmsSigHlsData = 0x484C5320;
    internal const uint cmsSigCmykData = 0x434D594B;
    internal const uint cmsSigCmyData = 0x434D5920;
    internal const uint cmsSigMCH1Data = 0x4D434831;
    internal const uint cmsSigMCH2Data = 0x4D434832;
    internal const uint cmsSigMCH3Data = 0x4D434833;
    internal const uint cmsSigMCH4Data = 0x4D434834;
    internal const uint cmsSigMCH5Data = 0x4D434835;
    internal const uint cmsSigMCH6Data = 0x4D434836;
    internal const uint cmsSigMCH7Data = 0x4D434837;
    internal const uint cmsSigMCH8Data = 0x4D434838;
    internal const uint cmsSigMCH9Data = 0x4D434839;
    internal const uint cmsSigMCHAData = 0x4D434841;
    internal const uint cmsSigMCHBData = 0x4D434842;
    internal const uint cmsSigMCHCData = 0x4D434843;
    internal const uint cmsSigMCHDData = 0x4D434844;
    internal const uint cmsSigMCHEData = 0x4D434845;
    internal const uint cmsSigMCHFData = 0x4D434846;
    internal const uint cmsSigNamedData = 0x6e6d636c;
    internal const uint cmsSig1colorData = 0x31434C52;
    internal const uint cmsSig2colorData = 0x32434C52;
    internal const uint cmsSig3colorData = 0x33434C52;
    internal const uint cmsSig4colorData = 0x34434C52;
    internal const uint cmsSig5colorData = 0x35434C52;
    internal const uint cmsSig6colorData = 0x36434C52;
    internal const uint cmsSig7colorData = 0x37434C52;
    internal const uint cmsSig8colorData = 0x38434C52;
    internal const uint cmsSig9colorData = 0x39434C52;
    internal const uint cmsSig10colorData = 0x41434C52;
    internal const uint cmsSig11colorData = 0x42434C52;
    internal const uint cmsSig12colorData = 0x43434C52;
    internal const uint cmsSig13colorData = 0x44434C52;
    internal const uint cmsSig14colorData = 0x45434C52;
    internal const uint cmsSig15colorData = 0x46434C52;
    internal const uint cmsSigLuvKData = 0x4C75764B;

    internal const uint cmsSigInputClass = 0x73636E72;
    internal const uint cmsSigDisplayClass = 0x6D6E7472;
    internal const uint cmsSigOutputClass = 0x70727472;
    internal const uint cmsSigLinkClass = 0x6C696E6B;
    internal const uint cmsSigAbstractClass = 0x61627374;
    internal const uint cmsSigColorSpaceClass = 0x73706163;
    internal const uint cmsSigNamedColorClass = 0x6e6d636c;

    internal const uint cmsSigMacintosh = 0x4150504C;
    internal const uint cmsSigMicrosoft = 0x4D534654;
    internal const uint cmsSigSolaris = 0x53554E57;
    internal const uint cmsSigSGI = 0x53474920;
    internal const uint cmsSigTaligent = 0x54474E54;
    internal const uint cmsSigUnices = 0x2A6E6978;

    internal const uint cmsSigPerceptualReferenceMediumGamut = 0x70726d67;

    internal const uint cmsSigSceneColorimetryEstimates = 0x73636F65;
    internal const uint cmsSigSceneAppearanceEstimates = 0x73617065;
    internal const uint cmsSigFocalPlaneColorimetryEstimates = 0x66706365;
    internal const uint cmsSigReflectionHardcopyOriginalColorimetry = 0x72686F63;
    internal const uint cmsSigReflectionPrintOutputColorimetry = 0x72706F63;

    internal const uint cmsSigCurveSetElemType = 0x63767374;
    internal const uint cmsSigMatrixElemType = 0x6D617466;
    internal const uint cmsSigCLutElemType = 0x636C7574;

    internal const uint cmsSigBAcsElemType = 0x62414353;
    internal const uint cmsSigEAcsElemType = 0x65414353;

    // Custom from here, not in the ICC Spec
    internal const uint cmsSigXYZ2LabElemType = 0x6C327820;

    internal const uint cmsSigLab2XYZElemType = 0x78326C20;
    internal const uint cmsSigNamedColorElemType = 0x6E636C20;
    internal const uint cmsSigLabV2toV4 = 0x32203420;
    internal const uint cmsSigLabV4toV2 = 0x34203220;

    // Identities
    internal const uint cmsSigIdentityElemType = 0x69646E20;

    // Float to floatPCS
    internal const uint cmsSigLab2FloatPCS = 0x64326C20;

    internal const uint cmsSigFloatPCS2Lab = 0x6C326420;
    internal const uint cmsSigXYZ2FloatPCS = 0x64327820;
    internal const uint cmsSigFloatPCS2XYZ = 0x78326420;
    internal const uint cmsSigClipNegativesElemType = 0x636c7020;

    internal const uint cmsSigFormulaCurveSeg = 0x70617266;
    internal const uint cmsSigSampledCurveSeg = 0x73616D66;
    internal const uint cmsSigSegmentedCurve = 0x63757266;

    internal const uint cmsSigStatusA = 0x53746141;
    internal const uint cmsSigStatusE = 0x53746145;
    internal const uint cmsSigStatusI = 0x53746149;
    internal const uint cmsSigStatusT = 0x53746154;
    internal const uint cmsSigStatusM = 0x5374614D;
    internal const uint cmsSigDN = 0x444E2020;
    internal const uint cmsSigDNP = 0x444E2050;
    internal const uint cmsSigDNN = 0x444E4E20;
    internal const uint cmsSigDNNP = 0x444E4E50;

    internal const uint cmsReflective = 0;
    internal const uint cmsTransparency = 1;
    internal const uint cmsGlossy = 0;
    internal const uint cmsMatte = 2;

    internal const byte cmsMAXCHANNELS = 16;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint PREMUL_SH(uint m) => m << 23;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint FLOAT_SH(uint m) => m << 22;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint OPTIMIZED_SH(uint m) => m << 21;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint COLORSPACE_SH(uint m) => m << 16;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint SWAPFIRST_SH(uint m) => m << 14;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint FLAVOR_SH(uint m) => m << 13;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint PLANAR_SH(uint m) => m << 12;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ENDIAN16_SH(uint m) => m << 11;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint DOSWAP_SH(uint m) => m << 10;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint EXTRA_SH(uint m) => m << 7;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint CHANNELS_SH(uint m) => m << 3;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint BYTES_SH(uint m) => m << 0;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_PREMUL(uint m) => (int)(m >> 23) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_FLOAT(uint m) => (int)(m >> 22) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_OPTIMIZED(uint m) => (int)(m >> 21) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_COLORSPACE(uint m) => (int)(m >> 16) & 31;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_SWAPFIRST(uint m) => (int)(m >> 14) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_FLAVOR(uint m) => (int)(m >> 13) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_PLANAR(uint m) => (int)(m >> 12) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_ENDIAN16(uint m) => (int)(m >> 11) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_DOSWAP(uint m) => (int)(m >> 10) & 1;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_EXTRA(uint m) => (int)(m >> 7) & 7;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_CHANNELS(uint m) => (int)(m >> 3) & 15;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int T_BYTES(uint m) => (int)(m >> 0) & 7;

    internal const ushort PT_ANY = 0;
    internal const ushort PT_GRAY = 3;
    internal const ushort PT_RGB = 4;
    internal const ushort PT_CMY = 5;
    internal const ushort PT_CMYK = 6;
    internal const ushort PT_YCbCr = 7;
    internal const ushort PT_YUV = 8;
    internal const ushort PT_XYZ = 9;
    internal const ushort PT_Lab = 10;
    internal const ushort PT_YUVK = 11;
    internal const ushort PT_HSV = 12;
    internal const ushort PT_HLS = 13;
    internal const ushort PT_Yxy = 14;
    internal const ushort PT_MCH1 = 15;
    internal const ushort PT_MCH2 = 16;
    internal const ushort PT_MCH3 = 17;
    internal const ushort PT_MCH4 = 18;
    internal const ushort PT_MCH5 = 19;
    internal const ushort PT_MCH6 = 20;
    internal const ushort PT_MCH7 = 21;
    internal const ushort PT_MCH8 = 22;
    internal const ushort PT_MCH9 = 23;
    internal const ushort PT_MCH10 = 24;
    internal const ushort PT_MCH11 = 25;
    internal const ushort PT_MCH12 = 26;
    internal const ushort PT_MCH13 = 27;
    internal const ushort PT_MCH14 = 28;
    internal const ushort PT_MCH15 = 29;
    internal const ushort PT_LabV2 = 30;

    public static uint TYPE_GRAY_8 => COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(1);
    public static uint TYPE_GRAY_8_REV => COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(1) | FLAVOR_SH(1);
    public static uint TYPE_GRAY_16 => COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(2);
    public static uint TYPE_GRAY_16_REV => COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(2) | FLAVOR_SH(1);
    public static uint TYPE_GRAY_16_SE => COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_GRAYA_8 => COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(1);
    public static uint TYPE_GRAYA_8_PREMUL => COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(1) | PREMUL_SH(1);
    public static uint TYPE_GRAYA_16 => COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2);
    public static uint TYPE_GRAYA_16_PREMUL => COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | PREMUL_SH(1);
    public static uint TYPE_GRAYA_16_SE => COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_GRAYA_8_PLANAR => COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_GRAYA_16_PLANAR => COLORSPACE_SH(PT_GRAY) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | PLANAR_SH(1);

    public static uint TYPE_RGB_8 => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1);
    public static uint TYPE_RGB_8_PLANAR => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_BGR_8 => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_BGR_8_PLANAR => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | PLANAR_SH(1);
    public static uint TYPE_RGB_16 => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_RGB_16_PLANAR => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_RGB_16_SE => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_BGR_16 => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_BGR_16_PLANAR => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | PLANAR_SH(1);
    public static uint TYPE_BGR_16_SE => COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);

    public static uint TYPE_RGBA_8 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1);
    public static uint TYPE_RGBA_8_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | PREMUL_SH(1);
    public static uint TYPE_RGBA_8_PLANAR => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_RGBA_16 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_RGBA_16_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | PREMUL_SH(1);
    public static uint TYPE_RGBA_16_PLANAR => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_RGBA_16_SE => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);

    public static uint TYPE_ARGB_8 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_ARGB_8_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | SWAPFIRST_SH(1) | PREMUL_SH(1);
    public static uint TYPE_ARGB_8_PLANAR => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | SWAPFIRST_SH(1) | PLANAR_SH(1);
    public static uint TYPE_ARGB_16 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | SWAPFIRST_SH(1);
    public static uint TYPE_ARGB_16_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | SWAPFIRST_SH(1) | PREMUL_SH(1);

    public static uint TYPE_ABGR_8 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_ABGR_8_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | PREMUL_SH(1);
    public static uint TYPE_ABGR_8_PLANAR => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | PLANAR_SH(1);
    public static uint TYPE_ABGR_16 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_ABGR_16_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | PREMUL_SH(1);
    public static uint TYPE_ABGR_16_PLANAR => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | PLANAR_SH(1);
    public static uint TYPE_ABGR_16_SE => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);

    public static uint TYPE_BGRA_8 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_BGRA_8_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1) | PREMUL_SH(1);
    public static uint TYPE_BGRA_8_PLANAR => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1) | PLANAR_SH(1);
    public static uint TYPE_BGRA_16 => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_BGRA_16_PREMUL => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1) | PREMUL_SH(1);
    public static uint TYPE_BGRA_16_SE => COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1);

    public static uint TYPE_CMY_8 => COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(1);
    public static uint TYPE_CMY_8_PLANAR => COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_CMY_16 => COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_CMY_16_PLANAR => COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_CMY_16_SE => COLORSPACE_SH(PT_CMY) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);

    public static uint TYPE_CMYK_8 => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1);
    public static uint TYPE_CMYKA_8 => COLORSPACE_SH(PT_CMYK) | EXTRA_SH(1) | CHANNELS_SH(4) | BYTES_SH(1);
    public static uint TYPE_CMYK_8_REV => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1) | FLAVOR_SH(1);
    public static uint TYPE_YUVK_8 => TYPE_CMYK_8_REV;
    public static uint TYPE_CMYK_8_PLANAR => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_CMYK_16 => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2);
    public static uint TYPE_CMYK_16_REV => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | FLAVOR_SH(1);
    public static uint TYPE_YUVK_16 => TYPE_CMYK_16_REV;
    public static uint TYPE_CMYK_16_PLANAR => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_CMYK_16_SE => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | ENDIAN16_SH(1);

    public static uint TYPE_KYMC_8 => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC_16 => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC_16_SE => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);

    public static uint TYPE_KCMY_8 => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_KCMY_8_REV => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(1) | FLAVOR_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_KCMY_16 => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | SWAPFIRST_SH(1);
    public static uint TYPE_KCMY_16_REV => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | FLAVOR_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_KCMY_16_SE => COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2) | ENDIAN16_SH(1) | SWAPFIRST_SH(1);

    public static uint TYPE_CMYK5_8 => COLORSPACE_SH(PT_MCH5) | CHANNELS_SH(5) | BYTES_SH(1);
    public static uint TYPE_CMYK5_16 => COLORSPACE_SH(PT_MCH5) | CHANNELS_SH(5) | BYTES_SH(2);
    public static uint TYPE_CMYK5_16_SE => COLORSPACE_SH(PT_MCH5) | CHANNELS_SH(5) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_KYMC5_8 => COLORSPACE_SH(PT_MCH5) | CHANNELS_SH(5) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC5_16 => COLORSPACE_SH(PT_MCH5) | CHANNELS_SH(5) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC5_16_SE => COLORSPACE_SH(PT_MCH5) | CHANNELS_SH(5) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);
    public static uint TYPE_CMYK6_8 => COLORSPACE_SH(PT_MCH6) | CHANNELS_SH(6) | BYTES_SH(1);
    public static uint TYPE_CMYK6_8_PLANAR => COLORSPACE_SH(PT_MCH6) | CHANNELS_SH(6) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_CMYK6_16 => COLORSPACE_SH(PT_MCH6) | CHANNELS_SH(6) | BYTES_SH(2);
    public static uint TYPE_CMYK6_16_PLANAR => COLORSPACE_SH(PT_MCH6) | CHANNELS_SH(6) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_CMYK6_16_SE => COLORSPACE_SH(PT_MCH6) | CHANNELS_SH(6) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_CMYK7_8 => COLORSPACE_SH(PT_MCH7) | CHANNELS_SH(7) | BYTES_SH(1);
    public static uint TYPE_CMYK7_16 => COLORSPACE_SH(PT_MCH7) | CHANNELS_SH(7) | BYTES_SH(2);
    public static uint TYPE_CMYK7_16_SE => COLORSPACE_SH(PT_MCH7) | CHANNELS_SH(7) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_KYMC7_8 => COLORSPACE_SH(PT_MCH7) | CHANNELS_SH(7) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC7_16 => COLORSPACE_SH(PT_MCH7) | CHANNELS_SH(7) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC7_16_SE => COLORSPACE_SH(PT_MCH7) | CHANNELS_SH(7) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);
    public static uint TYPE_CMYK8_8 => COLORSPACE_SH(PT_MCH8) | CHANNELS_SH(8) | BYTES_SH(1);
    public static uint TYPE_CMYK8_16 => COLORSPACE_SH(PT_MCH8) | CHANNELS_SH(8) | BYTES_SH(2);
    public static uint TYPE_CMYK8_16_SE => COLORSPACE_SH(PT_MCH8) | CHANNELS_SH(8) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_KYMC8_8 => COLORSPACE_SH(PT_MCH8) | CHANNELS_SH(8) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC8_16 => COLORSPACE_SH(PT_MCH8) | CHANNELS_SH(8) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC8_16_SE => COLORSPACE_SH(PT_MCH8) | CHANNELS_SH(8) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);
    public static uint TYPE_CMYK9_8 => COLORSPACE_SH(PT_MCH9) | CHANNELS_SH(9) | BYTES_SH(1);
    public static uint TYPE_CMYK9_16 => COLORSPACE_SH(PT_MCH9) | CHANNELS_SH(9) | BYTES_SH(2);
    public static uint TYPE_CMYK9_16_SE => COLORSPACE_SH(PT_MCH9) | CHANNELS_SH(9) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_KYMC9_8 => COLORSPACE_SH(PT_MCH9) | CHANNELS_SH(9) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC9_16 => COLORSPACE_SH(PT_MCH9) | CHANNELS_SH(9) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC9_16_SE => COLORSPACE_SH(PT_MCH9) | CHANNELS_SH(9) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);
    public static uint TYPE_CMYK10_8 => COLORSPACE_SH(PT_MCH10) | CHANNELS_SH(10) | BYTES_SH(1);
    public static uint TYPE_CMYK10_16 => COLORSPACE_SH(PT_MCH10) | CHANNELS_SH(10) | BYTES_SH(2);
    public static uint TYPE_CMYK10_16_SE => COLORSPACE_SH(PT_MCH10) | CHANNELS_SH(10) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_KYMC10_8 => COLORSPACE_SH(PT_MCH10) | CHANNELS_SH(10) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC10_16 => COLORSPACE_SH(PT_MCH10) | CHANNELS_SH(10) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC10_16_SE => COLORSPACE_SH(PT_MCH10) | CHANNELS_SH(10) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);
    public static uint TYPE_CMYK11_8 => COLORSPACE_SH(PT_MCH11) | CHANNELS_SH(11) | BYTES_SH(1);
    public static uint TYPE_CMYK11_16 => COLORSPACE_SH(PT_MCH11) | CHANNELS_SH(11) | BYTES_SH(2);
    public static uint TYPE_CMYK11_16_SE => COLORSPACE_SH(PT_MCH11) | CHANNELS_SH(11) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_KYMC11_8 => COLORSPACE_SH(PT_MCH11) | CHANNELS_SH(11) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC11_16 => COLORSPACE_SH(PT_MCH11) | CHANNELS_SH(11) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC11_16_SE => COLORSPACE_SH(PT_MCH11) | CHANNELS_SH(11) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);
    public static uint TYPE_CMYK12_8 => COLORSPACE_SH(PT_MCH12) | CHANNELS_SH(12) | BYTES_SH(1);
    public static uint TYPE_CMYK12_16 => COLORSPACE_SH(PT_MCH12) | CHANNELS_SH(12) | BYTES_SH(2);
    public static uint TYPE_CMYK12_16_SE => COLORSPACE_SH(PT_MCH12) | CHANNELS_SH(12) | BYTES_SH(2) | ENDIAN16_SH(1);
    public static uint TYPE_KYMC12_8 => COLORSPACE_SH(PT_MCH12) | CHANNELS_SH(12) | BYTES_SH(1) | DOSWAP_SH(1);
    public static uint TYPE_KYMC12_16 => COLORSPACE_SH(PT_MCH12) | CHANNELS_SH(12) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_KYMC12_16_SE => COLORSPACE_SH(PT_MCH12) | CHANNELS_SH(12) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);

    // Colorimetric
    public static uint TYPE_XYZ_16 => COLORSPACE_SH(PT_XYZ) | CHANNELS_SH(3) | BYTES_SH(2);

    public static uint TYPE_Lab_8 => COLORSPACE_SH(PT_Lab) | CHANNELS_SH(3) | BYTES_SH(1);
    public static uint TYPE_LabV2_8 => COLORSPACE_SH(PT_LabV2) | CHANNELS_SH(3) | BYTES_SH(1);

    public static uint TYPE_ALab_8 => COLORSPACE_SH(PT_Lab) | CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_ALabV2_8 => COLORSPACE_SH(PT_LabV2) | CHANNELS_SH(3) | BYTES_SH(1) | EXTRA_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_Lab_16 => COLORSPACE_SH(PT_Lab) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_LabV2_16 => COLORSPACE_SH(PT_LabV2) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_Yxy_16 => COLORSPACE_SH(PT_Yxy) | CHANNELS_SH(3) | BYTES_SH(2);

    // YCbCr
    public static uint TYPE_YCbCr_8 => COLORSPACE_SH(PT_YCbCr) | CHANNELS_SH(3) | BYTES_SH(1);

    public static uint TYPE_YCbCr_8_PLANAR => COLORSPACE_SH(PT_YCbCr) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_YCbCr_16 => COLORSPACE_SH(PT_YCbCr) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_YCbCr_16_PLANAR => COLORSPACE_SH(PT_YCbCr) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_YCbCr_16_SE => COLORSPACE_SH(PT_YCbCr) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);

    // YUV
    public static uint TYPE_YUV_8 => COLORSPACE_SH(PT_YUV) | CHANNELS_SH(3) | BYTES_SH(1);

    public static uint TYPE_YUV_8_PLANAR => COLORSPACE_SH(PT_YUV) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_YUV_16 => COLORSPACE_SH(PT_YUV) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_YUV_16_PLANAR => COLORSPACE_SH(PT_YUV) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_YUV_16_SE => COLORSPACE_SH(PT_YUV) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);

    // HLS
    public static uint TYPE_HLS_8 => COLORSPACE_SH(PT_HLS) | CHANNELS_SH(3) | BYTES_SH(1);

    public static uint TYPE_HLS_8_PLANAR => COLORSPACE_SH(PT_HLS) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_HLS_16 => COLORSPACE_SH(PT_HLS) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_HLS_16_PLANAR => COLORSPACE_SH(PT_HLS) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_HLS_16_SE => COLORSPACE_SH(PT_HLS) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);

    // HSV
    public static uint TYPE_HSV_8 => COLORSPACE_SH(PT_HSV) | CHANNELS_SH(3) | BYTES_SH(1);

    public static uint TYPE_HSV_8_PLANAR => COLORSPACE_SH(PT_HSV) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
    public static uint TYPE_HSV_16 => COLORSPACE_SH(PT_HSV) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_HSV_16_PLANAR => COLORSPACE_SH(PT_HSV) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
    public static uint TYPE_HSV_16_SE => COLORSPACE_SH(PT_HSV) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);

    // Named color index. Only 16 bits is allowed (don't check colorspace)
    public static uint TYPE_NAMED_COLOR_INDEX => CHANNELS_SH(1) | BYTES_SH(2);

    // Float formatters.
    public static uint TYPE_XYZ_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_XYZ) | CHANNELS_SH(3) | BYTES_SH(4);

    public static uint TYPE_Lab_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_Lab) | CHANNELS_SH(3) | BYTES_SH(4);
    public static uint TYPE_LabA_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_Lab) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4);
    public static uint TYPE_GRAY_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(4);
    public static uint TYPE_GRAYA_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(4) | EXTRA_SH(1);
    public static uint TYPE_GRAYA_FLT_PREMUL => FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(4) | EXTRA_SH(1) | PREMUL_SH(1);
    public static uint TYPE_RGB_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(4);

    public static uint TYPE_RGBA_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4);
    public static uint TYPE_RGBA_FLT_PREMUL => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4) | PREMUL_SH(1);
    public static uint TYPE_ARGB_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4) | SWAPFIRST_SH(1);
    public static uint TYPE_ARGB_FLT_PREMUL => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4) | SWAPFIRST_SH(1) | PREMUL_SH(1);
    public static uint TYPE_BGR_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(4) | DOSWAP_SH(1);
    public static uint TYPE_BGRA_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4) | DOSWAP_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_BGRA_FLT_PREMUL => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4) | DOSWAP_SH(1) | SWAPFIRST_SH(1) | PREMUL_SH(1);
    public static uint TYPE_ABGR_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4) | DOSWAP_SH(1);
    public static uint TYPE_ABGR_FLT_PREMUL => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(4) | DOSWAP_SH(1) | PREMUL_SH(1);

    public static uint TYPE_CMYK_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(4);

    // Floating point formatters.
    // NOTE THAT 'BYTES' FIELD IS SET TO ZERO ON DLB because 8 bytes overflows the bitfield
    public static uint TYPE_XYZ_DBL => FLOAT_SH(1) | COLORSPACE_SH(PT_XYZ) | CHANNELS_SH(3) | BYTES_SH(0);

    public static uint TYPE_Lab_DBL => FLOAT_SH(1) | COLORSPACE_SH(PT_Lab) | CHANNELS_SH(3) | BYTES_SH(0);
    public static uint TYPE_GRAY_DBL => FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(0);
    public static uint TYPE_RGB_DBL => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(0);
    public static uint TYPE_BGR_DBL => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(0) | DOSWAP_SH(1);
    public static uint TYPE_CMYK_DBL => FLOAT_SH(1) | COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(0);

    // IEEE 754-2008 "half"
    public static uint TYPE_GRAY_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_GRAY) | CHANNELS_SH(1) | BYTES_SH(2);

    public static uint TYPE_RGB_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_RGBA_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2);
    public static uint TYPE_CMYK_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_CMYK) | CHANNELS_SH(4) | BYTES_SH(2);

    public static uint TYPE_ARGB_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | SWAPFIRST_SH(1);
    public static uint TYPE_BGR_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1);
    public static uint TYPE_BGRA_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1);
    public static uint TYPE_ABGR_HALF_FLT => FLOAT_SH(1) | COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1);

    internal const uint cmsILLUMINANT_TYPE_UNKNOWN = 0x0000000;
    internal const uint cmsILLUMINANT_TYPE_D50 = 0x0000001;
    internal const uint cmsILLUMINANT_TYPE_D65 = 0x0000002;
    internal const uint cmsILLUMINANT_TYPE_D93 = 0x0000003;
    internal const uint cmsILLUMINANT_TYPE_F2 = 0x0000004;
    internal const uint cmsILLUMINANT_TYPE_D55 = 0x0000005;
    internal const uint cmsILLUMINANT_TYPE_A = 0x0000006;
    internal const uint cmsILLUMINANT_TYPE_E = 0x0000007;
    internal const uint cmsILLUMINANT_TYPE_F8 = 0x0000008;

    internal static EventId cmsERROR_UNDEFINED = ErrorCodes.Undefined;
    internal static EventId cmsERROR_FILE = ErrorCodes.File;
    internal static EventId cmsERROR_RANGE = ErrorCodes.Range;
    internal static EventId cmsERROR_INTERNAL = ErrorCodes.Internal;
    internal static EventId cmsERROR_NULL = ErrorCodes.Null;
    internal static EventId cmsERROR_READ = ErrorCodes.Read;
    internal static EventId cmsERROR_SEEK = ErrorCodes.Seek;
    internal static EventId cmsERROR_WRITE = ErrorCodes.Write;
    internal static EventId cmsERROR_UNKNOWN_EXTENSION = ErrorCodes.UnknownExtension;
    internal static EventId cmsERROR_COLORSPACE_CHECK = ErrorCodes.ColorspaceCheck;
    internal static EventId cmsERROR_ALREADY_DEFINED = ErrorCodes.AlreadyDefined;
    internal static EventId cmsERROR_BAD_SIGNATURE = ErrorCodes.BadSignature;
    internal static EventId cmsERROR_CORRUPTION_DETECTED = ErrorCodes.CorruptionDetected;
    internal static EventId cmsERROR_NOT_SUITABLE = ErrorCodes.NotSuitable;

    internal const uint AVG_SURROUND = 1;
    internal const uint DIM_SURROUND = 2;
    internal const uint DARK_SURROUND = 3;
    internal const uint CUTSHEET_SURROUND = 4;

    internal const double D_CALCULATE = -1.0;

    internal const uint SAMPLER_INSPECT = 0x01000000;

    internal static readonly byte[] cmsNoLanguage = "\0\0"u8.ToArray();
    internal static readonly byte[] cmsNoCountry = "\0\0"u8.ToArray();

    internal const ushort cmsPRINTER_DEFAULT_SCREENS = 0x0001;
    internal const ushort cmsFREQUENCE_UNITS_LINES_CM = 0x0000;
    internal const ushort cmsFREQUENCE_UNITS_LINES_INCH = 0x0002;

    internal const byte cmsSPOT_UNKNOWN = 0;
    internal const byte cmsSPOT_PRINTER_DEFAULT = 1;
    internal const byte cmsSPOT_ROUND = 2;
    internal const byte cmsSPOT_DIAMOND = 3;
    internal const byte cmsSPOT_ELLIPSE = 4;
    internal const byte cmsSPOT_LINE = 5;
    internal const byte cmsSPOT_SQUARE = 6;
    internal const byte cmsSPOT_CROSS = 7;

    internal const uint cmsEmbeddedProfileFalse = 0x00000000;
    internal const uint cmsEmbeddedProfileTrue = 0x00000001;
    internal const uint cmsUseAnywhere = 0x00000000;
    internal const uint cmsUseWithEmbeddedDataOnly = 0x00000002;

    internal const byte LCMS_USED_AS_INPUT = 0;
    internal const byte LCMS_USED_AS_OUTPUT = 1;
    internal const byte LCMS_USED_AS_PROOF = 2;

    internal const byte cmsInfoDescription = 0;
    internal const byte cmsInfoManufacturer = 1;
    internal const byte cmsInfoModel = 2;
    internal const byte cmsInfoCopyright = 3;

    // ICC Intents
    internal const byte INTENT_PERCEPTUAL = 0;

    internal const byte INTENT_RELATIVE_COLORIMETRIC = 1;
    internal const byte INTENT_SATURATION = 2;
    internal const byte INTENT_ABSOLUTE_COLORIMETRIC = 3;

    // Non-ICC intents
    internal const byte INTENT_PRESERVE_K_ONLY_PERCEPTUAL = 10;

    internal const byte INTENT_PRESERVE_K_ONLY_RELATIVE_COLORIMETRIC = 11;
    internal const byte INTENT_PRESERVE_K_ONLY_SATURATION = 12;
    internal const byte INTENT_PRESERVE_K_PLANE_PERCEPTUAL = 13;
    internal const byte INTENT_PRESERVE_K_PLANE_RELATIVE_COLORIMETRIC = 14;
    internal const byte INTENT_PRESERVE_K_PLANE_SATURATION = 15;

    // Flags

    internal const ushort cmsFLAGS_NOCACHE = 0x0040;
    internal const ushort cmsFLAGS_NOOPTIMIZE = 0x0100;
    internal const ushort cmsFLAGS_NULLTRANSFORM = 0x0200;

    // Proofing flags
    internal const ushort cmsFLAGS_GAMUTCHECK = 0x1000;

    internal const ushort cmsFLAGS_SOFTPROOFING = 0x4000;

    // Misc
    internal const ushort cmsFLAGS_BLACKPOINTCOMPENSATION = 0x2000;

    internal const ushort cmsFLAGS_NOWHITEONWHITEFIXUP = 0x0004;
    internal const ushort cmsFLAGS_HIGHRESPRECALC = 0x0400;
    internal const ushort cmsFLAGS_LOWRESPRECALC = 0x0800;

    // For devicelink creation
    internal const ushort cmsFLAGS_8BITS_DEVICELINK = 0x0008;

    internal const ushort cmsFLAGS_GUESSDEVICECLASS = 0x0020;
    internal const ushort cmsFLAGS_KEEP_SEQUENCE = 0x0080;

    // Specific to a particular optimizations
    internal const ushort cmsFLAGS_FORCE_CLUT = 0x0002;

    internal const ushort cmsFLAGS_CLUT_POST_LINEARIZATION = 0x0001;
    internal const ushort cmsFLAGS_CLUT_PRE_LINEARIZATION = 0x0010;

    // Specific to unbounded mode
    internal const ushort cmsFLAGS_NONEGATIVES = 0x8000;

    // Copy alpha channels when transforming
    internal const uint cmsFLAGS_COPY_ALPHA = 0x04000000;

    // Fine-tune control over number of gridpoints
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint cmsFLAGS_GRIDPOINTS(int n) =>
        (uint)(n & 0xFF) << 16;

    // CRD special
    internal const uint cmsFLAGS_NODEFAULTRESOURCEDEF = 0x01000000;

    #endregion lcms2.h

    #region lcms2_plugin.h

    internal const int VX = 0;
    internal const int VY = 1;
    internal const int VZ = 2;

    internal const uint cmsPluginMagicNumber = 0x61637070;              // 'acpp'

    internal const uint cmsPluginMemHandlerSig = 0x6D656D48;            // 'memH'
    internal const uint cmsPluginInterpolationSig = 0x696E7048;         // 'inpH'
    internal const uint cmsPluginParametricCurveSig = 0x70617248;       // 'parH'
    internal const uint cmsPluginFormattersSig = 0x66726D48;            // 'frmH
    internal const uint cmsPluginTagTypeSig = 0x74797048;               // 'typH'
    internal const uint cmsPluginTagSig = 0x74616748;                   // 'tagH'
    internal const uint cmsPluginRenderingIntentSig = 0x696E7448;       // 'intH'
    internal const uint cmsPluginMultiProcessElementSig = 0x6D706548;   // 'mpeH'
    internal const uint cmsPluginOptimizationSig = 0x6F707448;          // 'optH'
    internal const uint cmsPluginTransformSig = 0x7A666D48;             // 'xfmH'
    internal const uint cmsPluginMutexSig = 0x6D747A48;                 // 'mtxH'

    internal const byte MAX_TYPES_IN_LCMS_PLUGIN = 20;

    internal const byte MAX_INPUT_DIMENSIONS = 15;

    #endregion lcms2_plugin.h

    #region lcms2_internal.h

    internal const double M_PI = 3.14159265358979323846;
    internal const double M_LOG10E = 0.434294481903251827651;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T _cmsALIGNLONG<T>(T x) where T : IBitwiseOperators<T, uint, T>, IAdditionOperators<T, uint, T> =>
        (x + (sizeof(uint) - 1u)) & ~(sizeof(uint) - 1u);

    internal static ushort CMS_PTR_ALIGNMENT = (ushort)nint.Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T _cmsALIGNMEM<T>(T x) where T : IBitwiseOperators<T, uint, T>, IAdditionOperators<T, uint, T> =>
        (x + ((uint)CMS_PTR_ALIGNMENT - 1)) & ~((uint)CMS_PTR_ALIGNMENT - 1);

    internal const double MAX_ENCODEABLE_XYZ = 1 + (32767.0 / 32768);
    internal const double MIN_ENCODEABLE_ab2 = -128.0;
    internal const double MAX_ENCODEABLE_ab2 = (65535.0 / 256) - 128;
    internal const double MIN_ENCODEABLE_ab4 = -128.0;
    internal const double MAX_ENCODEABLE_ab4 = 127.0;

    internal const byte MAX_STAGE_CHANNELS = 128;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static ushort FROM_8_TO_16(uint rgb) => (ushort)((rgb << 8) | rgb);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static byte FROM_16_TO_8(uint rgb) => (byte)((((rgb * 65281u) + 8388608u) >> 24) & 0xFFu);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static void _cmsAssert(params bool[] args)
    {
        foreach (var arg in args)
            Debug.Assert(arg);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static void _cmsAssert(string str) =>
        Debug.Assert(!String.IsNullOrEmpty(str));

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static void _cmsAssert<T>(T[] array) =>
        Debug.Assert(array.Length is 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static void _cmsAssert<T>(ReadOnlySpan<T> span) =>
        Debug.Assert(!span.IsEmpty);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static void _cmsAssert<T>(params ReadOnlyMemory<T>[] args)
    {
        foreach (var arg in args)
            Debug.Assert(!arg.IsEmpty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static void _cmsAssert(params object?[] args)
    {
        foreach (var arg in args)
            Debug.Assert(arg is not null);
    }

    internal const double MATRIX_DET_TOLERANCE = 1e-4;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int FIXED_TO_INT(int x) => x >> 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int FIXED_REST_TO_INT(int x) => x & 0xFFFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int ROUND_FIXED_TO_INT(int x) => (x + 0x8000) >> 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int _cmsToFixedDomain(int a) => a + ((a + 0x7fff) / 0xffff);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int _cmsFromFixedDomain(int a) => a - ((a + 0x7fff) >> 16);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int _cmsQuickFloor(double val)
    {
        Span<byte> buffer = stackalloc byte[8];
#if CMS_DONT_USE_FAST_FLOOR
        (int)Math.Floor(val);
#else
        const double _lcms_double2fixmagic = 68719476736.0 * 1.5;
        BitConverter.TryWriteBytes(buffer, val + _lcms_double2fixmagic);

        return BitConverter.ToInt32(buffer) >> 16;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static ushort _cmsQuickFloorWord(double d) =>
        (ushort)(_cmsQuickFloor(d - 32767) + 32767);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static ushort _cmsQuickSaturateWord(double d)
    {
        d += 0.5;
        return d switch
        {
            <= 0 => 0,
            >= 65535.0 => 0xffff,
            _ => _cmsQuickFloorWord(d),
        };
    }

    internal const byte MAX_TABLE_TAG = 100;

    internal const uint cmsFLAGS_CAN_CHANGE_FORMATTER = 0x02000000;

#endregion lcms2_internal.h

    static Lcms2()
    {
        //AllocList = new();
        globalContext = cmsCreateContext(null, null)!;

        #region Context and plugins

        //var defaultTag = default(TagLinkedList);
        //var tagNextOffset = (nuint)(&defaultTag.Next) - (nuint)(&defaultTag);

        //var defaultTagType = default(TagTypeLinkedList);
        //var tagTypeNextOffset = (nuint)(&defaultTagType.Next) - (nuint)(&defaultTagType);

        // Error logger
        //fixed (LogErrorChunkType* plugin = &LogErrorChunk)
        //    globalLogErrorChunk = dup<LogErrorChunkType>(plugin);

        // Alarm Codes
        //fixed (AlarmCodesChunkType* plugin = &AlarmCodesChunk)
        //{
        //    plugin->AlarmCodes[0] = plugin->AlarmCodes[1] = plugin->AlarmCodes[2] = 0x7F00;

        //    globalAlarmCodesChunk = dup<AlarmCodesChunkType>(plugin);
        //}

        // Adaptation State
        //fixed (AdaptationStateChunkType* plugin = &AdaptationStateChunk)
        //    globalAdaptationStateChunk = dup<AdaptationStateChunkType>(plugin);

        // Memory Handler
        //globalMemPluginChunk = alloc<MemPluginChunkType>();
        //*globalMemPluginChunk = new()
        //{
        //    MallocPtr = _cmsMallocDefaultFn,
        //    MallocZeroPtr = _cmsMallocZeroDefaultFn,
        //    FreePtr = _cmsFreeDefaultFn,
        //    ReallocPtr = _cmsReallocDefaultFn,
        //    CallocPtr = _cmsCallocDefaultFn,
        //    DupPtr = _cmsDupDefaultFn
        //};

        // Interpolation Plugin
        //fixed (InterpPluginChunkType* plugin = &InterpPluginChunk)
        //    globalInterpPluginChunk = dup<InterpPluginChunkType>(plugin);

        // Curves Plugin
        //fixed (ParametricCurvesCollection* curves = &defaultCurves)
        //{
        //    fixed (int* defaultFunctionTypes = defaultCurvesFunctionTypes)
        //        memcpy(curves->FunctionTypes, defaultFunctionTypes, 10 * sizeof(int));
        //    fixed (uint* defaultParameterCount = defaultCurvesParameterCounts)
        //        memcpy(curves->ParameterCount, defaultParameterCount, 10 * sizeof(uint));
        //}
        //fixed (CurvesPluginChunkType* plugin = &CurvesPluginChunk)
        //    globalCurvePluginChunk = dup<CurvesPluginChunkType>(plugin);

        // Formatters Plugin
        //globalFormattersPluginChunk = alloc<FormattersPluginChunkType>();
        //*globalFormattersPluginChunk = new();

        // Tag Type Plugin


        //fixed (TagTypePluginChunkType* plugin = &TagTypePluginChunk)
        //    globalTagTypePluginChunk = dup(plugin);

        
        //fixed (TagPluginChunkType* plugin = &TagPluginChunk)
        //    globalTagPluginChunk = dup(plugin);

        // Intents Plugin
        //fixed (IntentsPluginChunkType* plugin = &IntentsPluginChunk)
        //    globalIntentsPluginChunk = dup(plugin);


        //fixed (TagTypePluginChunkType* plugin = &MPETypePluginChunk)
        //    globalMPETypePluginChunk = dup(plugin);

        // Optimization Plugin
        //fixed (OptimizationPluginChunkType* plugin = &OptimizationPluginChunk)
        //    globalOptimizationPluginChunk = dup(plugin);

        // Transform Plugin
        //fixed (TransformPluginChunkType* plugin = &TransformPluginChunk)
        //    globalTransformPluginChunk = dup(plugin);

        // Mutex Plugin
        //fixed (MutexPluginChunkType* plugin = &MutexChunk)
        //    globalMutexPluginChunk = dup(plugin);

        // Global Context
        //globalContext = (Context)alloc((nuint)sizeof(Context_struct));
        //*globalContext = new()
        //{
        //    Next = null,
        //    MemPool = null,
        //    DefaultMemoryManager = default,
        //};
        //globalContext->chunks.parent = globalContext;

        //globalContext->chunks[Chunks.UserPtr] = null;
        //globalContext->chunks[Chunks.Logger] = globalLogErrorChunk;
        //globalContext->chunks[Chunks.AlarmCodesContext] = globalAlarmCodesChunk;
        //globalContext->chunks[Chunks.AdaptationStateContext] = globalAdaptationStateChunk;
        //globalContext->chunks[Chunks.MemPlugin] = globalMemPluginChunk;
        //globalContext->chunks[Chunks.InterpPlugin] = globalInterpPluginChunk;
        //globalContext->chunks[Chunks.CurvesPlugin] = globalCurvePluginChunk;
        //globalContext->chunks[Chunks.FormattersPlugin] = globalFormattersPluginChunk;
        //globalContext->chunks[Chunks.TagTypePlugin] = globalTagTypePluginChunk;
        //globalContext->chunks[Chunks.TagPlugin] = globalTagPluginChunk;
        //globalContext->chunks[Chunks.IntentPlugin] = globalIntentsPluginChunk;
        //globalContext->chunks[Chunks.MPEPlugin] = globalMPETypePluginChunk;
        //globalContext->chunks[Chunks.OptimizationPlugin] = globalOptimizationPluginChunk;
        //globalContext->chunks[Chunks.TransformPlugin] = globalTransformPluginChunk;
        //globalContext->chunks[Chunks.MutexPlugin] = globalMutexPluginChunk;


        #endregion Context and plugins

        #region Optimization defaults

        //DefaultOptimization = calloc<OptimizationCollection>(4);
        //DefaultOptimization[0] = new() { OptimizePtr = OptimizeByJoiningCurves, Next = &DefaultOptimization[1] };
        //DefaultOptimization[1] = new() { OptimizePtr = OptimizeMatrixShaper, Next = &DefaultOptimization[2] };
        //DefaultOptimization[2] = new() { OptimizePtr = OptimizeByComputingLinearization, Next = &DefaultOptimization[3] };
        //DefaultOptimization[3] = new() { OptimizePtr = OptimizeByResampling, Next = null };

        #endregion Optimization defaults
    }

    //[DebuggerStepThrough]
    //internal static void* alloc(nuint size, Type type)
    //{
    //    if (debugAllocs)
    //    {
    //        lock (AllocList)
    //        {
    //            var result = NativeMemory.Alloc(size + 32);
    //            AllocList.Add((nuint)result + 16, (type, size, false));
    //            NativeMemory.Fill(result, size + 32, 0xAF);
    //            return (byte*)result + 16;
    //        }
    //    }
    //    else
    //    {
    //        return NativeMemory.Alloc(size);
    //    }
    //}

    //[DebuggerStepThrough]
    //internal static void* alloc(nint size, Type type) =>
    //    alloc((nuint)size, type);

    //[DebuggerStepThrough]
    //internal static T* alloc<T>() where T : struct =>
    //    (T*)alloc(_sizeof<T>(), typeof(T));

    //[DebuggerStepThrough]
    //internal static void* allocZeroed(nuint size, Type type)
    //{
    //    if (debugAllocs)
    //    {
    //        lock (AllocList)
    //        {
    //            var result = NativeMemory.Alloc(size + 32);
    //            NativeMemory.Fill(result, size + 32, 0xAF);
    //            result = (byte*)result + 16;
    //            NativeMemory.Clear(result, size);
    //            AllocList.Add((nuint)result, (type, size, false));
    //            return result;
    //        }
    //    }
    //    else
    //    {
    //        return NativeMemory.AllocZeroed(size);
    //    }
    //}

    //[DebuggerStepThrough]
    //internal static void* allocZeroed(nint size, Type type) =>
    //    allocZeroed((nuint)size, type);

    //[DebuggerStepThrough]
    //internal static T* allocZeroed<T>() where T : struct =>
    //    (T*)allocZeroed(_sizeof<T>(), typeof(T));

    //[DebuggerStepThrough]
    //internal static void* realloc(in void* org, nuint newSize)
    //{
    //    if (debugAllocs)
    //    {
    //        Type type;
    //        nuint size;
    //        lock (AllocList)
    //        {
    //            (type, size, _) = AllocList[(nuint)org];
    //        }
    //        var result = alloc(newSize, type);
    //        if (org is not null)
    //        {
    //            memmove(result, org, size);
    //            free(org);
    //        }
    //        return result;
    //    }
    //    else
    //    {
    //        return NativeMemory.Realloc(org, newSize);
    //    }
    //}

    //[DebuggerStepThrough]
    //internal static void* dup(in void* org, nint size, Type type) =>
    //    dup(org, (nuint)size, type);

    //[DebuggerStepThrough]
    //internal static void* dup(in void* org, nuint size, Type type)
    //{
    //    var value = alloc(size, type);
    //    memcpy(value, org, size);

    //    return value;
    //}

    //[DebuggerStepThrough]
    //internal static T* dup<T>(in T* org) where T : struct
    //{
    //    var value = alloc<T>();
    //    memcpy(value, org);

    //    return value;
    //}

    //[DebuggerStepThrough]
    //internal static void memset<T>(T* dst, int val) where T : struct =>
    //    memset(dst, val, _sizeof<T>());

    //[DebuggerStepThrough]
    //internal static void memset(void* dst, int val, nint size) =>
    //    NativeMemory.Fill(dst, (uint)size, (byte)val);

    //[DebuggerStepThrough]
    //internal static void memset(void* dst, int val, nuint size) =>
    //    NativeMemory.Fill(dst, size, (byte)val);

    [DebuggerStepThrough]
    internal static void memmove<T>(Span<T> dst, ReadOnlySpan<T> src, uint count) =>
        memcpy(dst, src, count);

    //[DebuggerStepThrough]
    //internal static void memmove<T>(T* dst, in T* src) where T : struct =>
    //    memcpy(dst, src);

    //[DebuggerStepThrough]
    //internal static void memmove(void* dst, in void* src, nuint size) =>
    //    memcpy(dst, src, size);

    //[DebuggerStepThrough]
    //internal static void memmove(void* dst, in void* src, nint size) =>
    //    memcpy(dst, src, size);

    [DebuggerStepThrough]
    internal static void memcpy<T>(T[] dst, T[] src) =>
        memcpy((Span<T>)dst, (ReadOnlySpan<T>)src);

    [DebuggerStepThrough]
    internal static void memcpy<T>(Span<T> dst, ReadOnlySpan<T> src) =>
        src.CopyTo(dst);

    [DebuggerStepThrough]
    internal static void memcpy<T>(Span<T> dst, ReadOnlySpan<T> src, uint count) =>
        src[..(int)count].CopyTo(dst);

    //[DebuggerStepThrough]
    //internal static void memcpy(byte* dst, ReadOnlySpan<byte> src)
    //{
    //    var buf = stackalloc byte[src.Length];
    //    for (var i = 0; i < src.Length; i++)
    //        buf[i] = src[i];

    //    memcpy(dst, buf, src.Length);
    //}

    //[DebuggerStepThrough]
    //internal static void memcpy<T>(T* dst, in T* src) where T : struct =>
    //    memcpy(dst, src, _sizeof<T>());

    //[DebuggerStepThrough]
    //internal static void memcpy(void* dst, in void* src, nuint size) =>
    //    NativeMemory.Copy(src, dst, size);

    //[DebuggerStepThrough]
    //internal static void memcpy(void* dst, in void* src, nint size) =>
    //    NativeMemory.Copy(src, dst, (nuint)size);

    [DebuggerStepThrough]
    internal static T memcmp<T>(T[] buf1, T[] buf2) where T : unmanaged, ISubtractionOperators<T, T, T> =>
        memcmp((ReadOnlySpan<T>)buf1, (ReadOnlySpan<T>)buf2);

    [DebuggerStepThrough]
    internal static T memcmp<T>(T[] buf1, Span<T> buf2) where T : unmanaged, ISubtractionOperators<T, T, T> =>
        memcmp((ReadOnlySpan<T>)buf1, (ReadOnlySpan<T>)buf2);

    [DebuggerStepThrough]
    internal static T memcmp<T>(Span<T> buf1, T[] buf2) where T : unmanaged, ISubtractionOperators<T, T, T> =>
        memcmp((ReadOnlySpan<T>)buf1, (ReadOnlySpan<T>)buf2);

    [DebuggerStepThrough]
    internal static T memcmp<T>(T[] buf1, ReadOnlySpan<T> buf2) where T : unmanaged, ISubtractionOperators<T, T, T> =>
        memcmp((ReadOnlySpan<T>)buf1, buf2);

    [DebuggerStepThrough]
    internal static T memcmp<T>(ReadOnlySpan<T> buf1, T[] buf2) where T : unmanaged, ISubtractionOperators<T, T, T> =>
        memcmp(buf1, (ReadOnlySpan<T>)buf2);

    [DebuggerStepThrough]
    internal static T memcmp<T>(Span<T> buf1, Span<T> buf2) where T : unmanaged, ISubtractionOperators<T, T, T> =>
        memcmp((ReadOnlySpan<T>)buf1, (ReadOnlySpan<T>)buf2);

    [DebuggerStepThrough]
    internal static T memcmp<T>(ReadOnlySpan<T> buf1, ReadOnlySpan<T> buf2) where T : unmanaged, ISubtractionOperators<T, T, T>
    {
        nint counter = 0;
        while (counter < buf2.Length && counter < buf1.Length)
        {
            var val = buf1[(int)counter] - buf2[(int)counter++];
            if (val is not 0)
                return val;
        }
        if (buf1.Length > buf2.Length)
            return buf1[(int)counter];
        if (buf1.Length < buf2.Length)
            return default(T) - buf2[(int)counter];
        return default;
    }

    //[DebuggerStepThrough]
    //internal static int memcmp(in char* buf1, ReadOnlySpan<char> buf2)
    //{
    //    var buf = stackalloc char[buf2.Length];
    //    for (var i = 0; i < buf2.Length; i++)
    //        buf[i] = buf2[i];

    //    return memcmp(buf1, buf, buf2.Length * _sizeof<char>());
    //}

    //[DebuggerStepThrough]
    //internal static int memcmp(in byte* buf1, ReadOnlySpan<byte> buf2)
    //{
    //    var buf = stackalloc byte[buf2.Length];
    //    for (var i = 0; i < buf2.Length; i++)
    //        buf[i] = buf2[i];

    //    return memcmp(buf1, buf, buf2.Length);
    //}

    //[DebuggerStepThrough]
    //internal static int memcmp(in void* buf1, in void* buf2, nint count)
    //{
    //    nint counter = 0;
    //    while (counter < count)
    //    {
    //        var val = ((byte*)buf1)[counter] - ((byte*)buf2)[counter++];
    //        if (val is not 0)
    //            return val;
    //    }
    //    return 0;
    //}

    //[DebuggerStepThrough]
    //internal static void free(void* ptr)
    //{
    //    if (debugAllocs)
    //    {
    //        lock (AllocList)
    //        {
    //            var item = AllocList[(nuint)ptr];
    //            ptr = (byte*)ptr - 16;
    //            for (var i = 0; i < 16; i++)
    //            {
    //                if (((byte*)ptr)[i] is not 0xAF || ((byte*)ptr)[i + 16 + (int)item.size] is not 0xAF)
    //                    throw new Exception($"{item.type} object of size {item.size} is corrupted. Object starts at 0x{(nuint)ptr + 16:X16}.");
    //            }
    //            NativeMemory.Fill(ptr, item.size + 32, 0x69);
    //            item.freed = true;
    //            AllocList[(nuint)ptr + 16] = item;
    //        }
    //    }
    //    else
    //    {
    //        NativeMemory.Free(ptr);
    //    }
    //}

    //[DebuggerStepThrough]
    //internal static void* calloc(uint num, nuint size, Type type) =>
    //    allocZeroed(num * size, type);

    //[DebuggerStepThrough]
    //internal static void* calloc(uint num, nint size, Type type) =>
    //    calloc(num, (nuint)size, type);

    //[DebuggerStepThrough]
    //internal static T* calloc<T>(uint num) where T : struct =>
    //    (T*)calloc(num, _sizeof<T>(), typeof(T));

    internal static nint strlen(ReadOnlySpan<byte> str)
    {
        var result = str.IndexOf((byte)0);

        if (result is -1)
            result = str.Length;

        return result;
    }
    //internal static nint strlen(in byte* str)
    //{
    //    var ptr = str;

    //    while (*ptr is not 0)
    //        ptr++;

    //    return (nint)(ptr - str);
    //}

    //internal static byte* strncpy(byte* dest, in string src, nuint n)
    //{
    //    for(var i = 0; (nuint)i < n; i++)
    //    {
    //        dest[i] = i < src.Length
    //            ? (byte)src[i]
    //            : (byte)0;
    //    }

    //    return dest;
    //}

    internal static Span<byte> strncpy(Span<byte> dest, ReadOnlySpan<char> src, nuint n)
    {
        if (dest.IsEmpty)
            return dest;

        if (n > (nuint)src.Length)
            n = (nuint)src.Length;

        if (n > (nuint)dest.Length)
            n = (nuint)dest.Length;

        Encoding.ASCII.GetBytes(src[..(int)n], dest[..(int)n]);

        return dest;
    }

    internal static Span<byte> strncpy(Span<byte> dest, ReadOnlySpan<byte> src, nuint n)
    {
        if (dest.IsEmpty)
            return dest;

        if (n > (nuint)src.Length)
            n = (nuint)src.Length;

        for (var i = 0; (nuint)i < n; i++)
        {
            dest[i] = src[i];

            if (src[i] is 0)
                break;
        }

        return dest;
    }

    //internal static byte* strncpy(byte* dest, in byte* src, nuint n)
    //{
    //    var srcEnd = false;
    //    for (var i = 0; (nuint)i < n; i++)
    //    {
    //        if (src[i] == 0)
    //            srcEnd = true;
    //        dest[i] = !srcEnd
    //            ? src[i]
    //            : (byte)0;
    //    }

    //    return dest;
    //}

    internal static Span<byte> strcat(Span<byte> strDestination, ReadOnlySpan<byte> strSource)
    {
        var i1 = 0;
        var i2 = 0;

        while (strDestination[i1] is not 0)
            i1++;

        do
            strDestination[i1] = strSource[i2];
        while (strSource[i2] is not 0 && ++i1 < strDestination.Length && ++i2 < strSource.Length);

        if (i1 >= strDestination.Length)
            i1 = strDestination.Length - 1;

        strDestination[i1] = 0;

        return strDestination;
    }

    //internal static byte* strcat(byte* strDestination, ReadOnlySpan<byte> strSource)
    //{
    //    var dst = strDestination;

    //    while (*dst is not 0)
    //        dst++;

    //    for (var i = 0; i < strSource.Length; i++)
    //        *dst++ = strSource[i];

    //    *dst = 0;

    //    return strDestination;
    //}

    //internal static byte* strcat(byte* strDestination, in byte* strSource)
    //{
    //    var src = strSource;
    //    var dst = strDestination;

    //    while (*dst is not 0)
    //        dst++;

    //    do
    //    {
    //        *dst = *src;
    //    } while (*src++ is not 0);

    //    return strDestination;
    //}

    internal static Span<byte> strcpy(Span<byte> dest, ReadOnlySpan<byte> src)
    {
        var i = 0;

        while (i < dest.Length && i < src.Length && src[i] is not 0)
            dest[i] = src[i++];

        if (i >= dest.Length)
            i = dest.Length - 1;

        dest[i] = 0;

        return dest;
    }

    //internal static byte* strcpy(byte* dest, in byte* src)
    //{
    //    var strSrc = src;
    //    var strDest = dest;

    //    do
    //    {
    //        *strDest++ = *strSrc;
    //    } while (*strSrc++ is not 0);

    //    return dest;
    //}

    internal static int strcmp(ReadOnlySpan<byte> sLeft, ReadOnlySpan<byte> sRight)
    {
        sLeft = TrimBuffer(sLeft);
        sRight = TrimBuffer(sRight);

        var end = cmsmin(sLeft.Length, sRight.Length);

        for (var i = 0; i < end; i++)
        {
            var val = sRight[i] - sLeft[i];

            if (val is not 0)
                return val;
        }

        if (sLeft.Length > sRight.Length)
            return -sLeft[end];
        if (sRight.Length > sLeft.Length)
            return sRight[end];
        return 0;
    }

    //internal static int strcmp(byte* sLeft, byte* sRight)
    //{
    //    int val;
    //    do
    //    {
    //        val = *sLeft - *sRight;
    //    } while (val is 0 && *sLeft++ is not 0 && *sRight++ is not 0);

    //    return val;
    //}

    internal static ReadOnlySpan<byte> strchr(ReadOnlySpan<byte> str, int c)
    {
        ReadOnlySpan<byte> first = null;

        while (str[0] is not 0)
        {
            if (str[0] == c)
            {
                first = str;
                break;
            }
            str = str[1..];
        }

        return first;
    }

    internal static Span<byte> strchr(Span<byte> str, int c)
    {
        Span<byte> first = null;

        while (str[0] is not 0)
        {
            if (str[0] == c)
            {
                first = str;
                break;
            }
            str = str[1..];
        }

        return first;
    }

    internal static Span<byte> strrchr(Span<byte> str, int c)
    {
        Span<byte> last = null;
        var i = 0;

        while(i < str.Length && str[i] is not 0)
        {
            if (str[i++] == c)
                last = str;
        }

        return last;
    }

    [DebuggerStepThrough]
    internal static Span<T> TrimBuffer<T>(T[] str) where T : IUnaryNegationOperators<T, T>, IEqualityOperators<T, T, bool> =>
        TrimBuffer(str.AsSpan());

    [DebuggerStepThrough]
    internal static Span<T> TrimBuffer<T>(Span<T> str) where T : IUnaryNegationOperators<T, T>, IEqualityOperators<T, T, bool>
    {
        for (var i = 0; i < str.Length; i++)
            if (str[i] == -str[i]) return str[..i];

        return str;
    }

    [DebuggerStepThrough]
    internal static ReadOnlySpan<T> TrimBuffer<T>(ReadOnlySpan<T> str) where T : IUnaryNegationOperators<T, T>, IEqualityOperators<T, T, bool>
    {
        for (var i = 0; i < str.Length; i++)
            if (str[i] == -str[i]) return str[..i];

        return str;
    }

    internal static int sprintf(Span<byte> buffer, string format, params object[] args)
    {
        var asString = String.Format(format, args);
        var len = Encoding.ASCII.GetBytes(asString, buffer);
        buffer[len] = 0;

        return len;
    }

    //internal static int sprintf(byte* buffer, string format, params object[] args)
    //{
    //    var str = String.Format(format, args).AsSpan();
    //    var result = str.Length;

    //    while (str.Length > 0)
    //    {
    //        *buffer++ = (byte)str[0];
    //        str = str[1..];
    //    }
    //    *buffer = 0;

    //    return result;
    //}

    //internal static int vsnprintf(byte* buffer, nuint count, byte* format, params object[] args) =>
    //    snprintf(buffer, count, format, args);

    internal static int snprintf(Span<byte> buffer, nuint count, ReadOnlySpan<byte> format, params object[] args)
    {
        buffer = buffer[..(int)count];
        var str = string.Format(Encoding.ASCII.GetString(format), args);
        return Encoding.ASCII.GetBytes(str, buffer);
    }

    //internal static int snprintf(byte* buffer, nuint count, byte* format, params object[] args)
    //{
    //    var len = (int)strlen(format);
    //    Span<char> str = stackalloc char[len];
    //    for (var i = 0; i < len; i++) str[i] = (char)format[i];
    //    var formatStr = new string(str);

        
    //    return snprintf(buffer, count, formatStr, args);
    //}

    //internal static int snprintf(byte* buffer, nuint count, string format, params object[] args)
    //{
    //    try
    //    {
    //        var str = String.Format(format, args);
    //        var pos = -1;

    //        while (str[++pos] is not '\0')
    //        {
    //            if ((nuint)pos < count - 1)
    //                buffer[pos] = (byte)str[pos];
    //        }
    //        if ((nuint)pos < count - 1)
    //            buffer[pos + 1] = 0;
    //        else
    //            buffer[count - 1] = 0;

    //        return pos;
    //    }
    //    catch
    //    {
    //        return -1;
    //    }
    //}

    internal static nint strspn(ReadOnlySpan<byte> str, ReadOnlySpan<byte> strCharSet)
    {
        for (var strPtr = 0; str[strPtr] != 0; strPtr++)
        {
            var found = false;
            var set = 0;
            while (strCharSet[set] != 0)
            {
                if (str[strPtr] == strCharSet[set++])
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return strPtr;
        }
        return 0;
    }

    internal static nuint fread(Span<byte> Buffer, nuint ElementSize, nuint ElementCount, FILE Stream)
    {
        var stream = Stream.Stream;

        //_cmsAssert(Buffer);
        _cmsAssert(stream);

        for (nuint i = 0; i < ElementCount; i++)
        {
            try
            {
                if (stream.Read(Buffer[(int)(i * ElementSize)..][..(int)ElementSize]) != (int)ElementSize)
                    return i;
            }
            catch (Exception)
            {
                return i;
            }

            //Buffer = (byte*)Buffer + ElementSize;
        }

        return ElementCount;
    }

    internal const byte SEEK_CUR = 1;
    internal const byte SEEK_END = 2;
    internal const byte SEEK_SET = 0;

    internal static int fseek(FILE stream, long offset, int origin)
    {
        var file = stream.Stream;

        try
        {
            file.Seek(offset, origin is SEEK_CUR ? SeekOrigin.Current : origin is SEEK_END ? SeekOrigin.End : SeekOrigin.Begin);
            return 0;
        }
        catch
        {
            return -1;
        }
    }

    internal static long ftell(FILE stream)
    {
        var file = stream.Stream;

        try
        {
            return file.Position;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    internal static nuint fwrite(ReadOnlySpan<byte> Buffer, nuint ElementSize, nuint ElementCount, FILE Stream)
    {
        var stream = Stream.Stream;

        _cmsAssert(Buffer);
        _cmsAssert(stream);

        for (nuint i = 0; i < ElementCount; i++)
        {
            try
            {
                stream.Write(Buffer[(int)(ElementSize * i)..][..(int)ElementSize]);
            }
            catch (Exception)
            {
                return i;
            }
        }

        return ElementCount;
    }

    //internal static nuint fwrite(in void* Buffer, nuint ElementSize, nuint ElementCount, FILE Stream)
    //{
    //    var stream = Stream.Stream;
    //    var buffer = (byte*)Buffer;

    //    _cmsAssert(Buffer);
    //    _cmsAssert(stream);

    //    for (nuint i = 0; i < ElementCount; i++)
    //    {
    //        try
    //        {
    //            stream.Write(new(buffer, (int)ElementSize));
    //        }
    //        catch (Exception)
    //        {
    //            return i;
    //        }

    //        buffer = buffer + ElementSize;
    //    }

    //    return ElementCount;
    //}

    internal static int fclose(FILE stream)
    {
        var file = stream.Stream;
        var filename = stream.Filename;
        //free(stream);

        var index = OpenFiles.FindIndex(i => i.file.Filename == filename);
        var f = OpenFiles[index];
        f.count--;
        if (f.count == 0)
        {
            OpenFiles.RemoveAt(index);
            try
            {
                file.Close();
            }
            catch (Exception)
            {
                return -1;
            }
        }
        else
        {
            OpenFiles[index] = f;
        }

        return 0;
    }
    
    internal static FILE? fopen(string filename, string mode)
    {
        Stream stream;
        int index = OpenFiles.FindIndex(i => i.file.Filename == filename);
        if (index is not -1)
        {
            var f = OpenFiles[index];
            f.count++;
            OpenFiles[index] = f;
            stream = f.file.Stream;
        }
        else
        {
            try
            {
                var options = new FileStreamOptions();
                if (mode.Contains('r'))
                {
                    options.Mode = FileMode.Open;
                    options.Access = FileAccess.Read;
                }
                else if (mode.Contains('w'))
                {
                    options.Mode = FileMode.Create;
                    options.Access = FileAccess.ReadWrite;
                }
                else
                {
                    return null;
                }
                stream = File.Open(filename, options);
            }
            catch
            {
                return null;
            }
        }
        var file = new FILE(stream, filename);

        if (index is -1)
        {
            OpenFiles.Add((file, 1));
        }

        return file;
    }

    //[DebuggerStepThrough]
    //internal static void CheckHeap()
    //{
    //    if (!debugAllocs)
    //        return;

    //    lock (AllocList)
    //    {
    //        foreach (var kvp in AllocList)
    //        {
    //            var ptr = (byte*)kvp.Key - 16;
    //            var (type, size, freed) = kvp.Value;

    //            if (freed)
    //            {
    //                for (var i = 0; i < (int)size + 32; i++)
    //                {
    //                    if (ptr[i] is not 0x69)
    //                        throw new Exception($"{type.Name} object of size {size} is corrupted. Object starts at 0x{(nuint)ptr:X16}.");
    //                }

    //                NativeMemory.Free(ptr);
    //            }
    //            else
    //            {
    //                for (var i = 0; i < 16; i++)
    //                {
    //                    if (ptr[i] is not 0xAF || ptr[16 + (int)size + i] is not 0xAF)
    //                        throw new Exception($"{type.Name} object of size {size} is corrupted. Object starts at 0x{(nuint)ptr:X16}.");
    //                }
    //            }
    //        }
    //        foreach (var kvp in AllocList.Where(kvp => kvp.Value.freed).ToArray())
    //            AllocList.Remove(kvp.Key);
    //    }
    //}

    [DebuggerStepThrough]
    internal static ILogger GetLogger(Context? context)
    {
        context = _cmsGetContext(context);

        if (loggers.TryGetValue(context.ErrorLogger, out var logger))
            return logger;

        logger = context.ErrorLogger.Factory.CreateLogger("Lcms2");
        loggers.Add(context.ErrorLogger, logger);
        return logger;
    }

    [DebuggerStepThrough]
    internal static void remove(string path) =>
        File.Delete(path);

    internal static void LogArrayPoolUsage(Context? context)
    {
        var logger = GetLogger(context);
        logger.LogInformation("Array Buffer Counts");
        foreach (var b in Context.GetBufferPoolCounts(context)
                                 .OrderBy(b => b.type.Name))
        {
            logger.LogInformation("\t{type}", b.type.Name);
            foreach (var (bufferSize, rentCount, maxRent, allocCount, maxAlloc) in b.Item2)
            {
                if (rentCount > 0)
                {
                    logger.LogWarning("\t\t{{\"bufferSize\": {bufferSize}, \"rentCount\": {rentCount}, " +
                        "\"maxRent\": {maxRent}, \"allocCount\": {allocCount}, \"maxAlloc\": {maxAlloc}}}",
                        bufferSize, rentCount, maxRent, allocCount, maxAlloc);
                }
                else
                {
                    logger.LogInformation("\t\t{{\"bufferSize\": {bufferSize}, \"rentCount\": {rentCount}, " +
                        "\"maxRent\": {maxRent}, \"allocCount\": {allocCount}, \"maxAlloc\": {maxAlloc}}}",
                        bufferSize, rentCount, maxRent, allocCount, maxAlloc);
                }
            }
        }
    }
}
