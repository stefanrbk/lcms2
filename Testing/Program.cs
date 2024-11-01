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
using lcms2.testbed;

using Microsoft.Extensions.Logging;

var now = DateTime.Now;

logger.LogInformation("LittleCMS.net {version:#.##} test bed {now:MMM d yyyy HH:mm:ss}", Context.LibraryVersion / 1000.0, now);

Thread.Sleep(10);
Console.WriteLine();

var cliResult = CommandLine.Parser.Default.ParseArguments<CliOptions>(args);

if (cliResult is null)
    return 0;

var exhaustive = cliResult.Value.DoExhaustive;
var doSpeedTests = cliResult.Value.DoSpeed;
var noCheckTests = cliResult.Value.NoChecks;
var noPluginTests = cliResult.Value.NoPlugins;
var doZooTests = cliResult.Value.DoZoo;
timeTests = cliResult.Value.TimeTests;

if (exhaustive)
{
    logger.LogInformation("Running exhaustive tests (will take a while...)");
}

//using (logger.BeginScope("Installing debug memory plug-in"))
//    cmsPlugin(DebugMemHandler);

using (logger.BeginScope("Setting up error logger"))
    cmsSetLogErrorHandler(factory);

PrintSupportedIntents();

using (logger.BeginScope("Basic operations"))
{
    Check("Sanity check", CheckBaseTypes);
    Check("quick floor", CheckQuickFloor);
    Check("quick floor word", CheckQuickFloorWord);
    Check("Fixed point 15.16 representation", CheckFixedPoint15_16);
    Check("Fixed point 8.8 representation", CheckFixedPoint8_8);
    Check("D50 roundtrip", CheckD50Roundtrip);
}
//Create utility profiles
if (!noCheckTests || doSpeedTests)
{
    using (logger.BeginScope("Profiles"))
        Check("Creation of test profiles", CreateTestProfiles);
}

if (!noCheckTests)
{
    using (logger.BeginScope("Forward 1D interpolation"))
    {
        Check("1D interpolation in 2pt tables", Check1DLerp2);
        Check("1D interpolation in 3pt tables", Check1DLerp3);
        Check("1D interpolation in 4pt tables", Check1DLerp4);
        Check("1D interpolation in 6pt tables", Check1DLerp6);
        Check("1D interpolation in 18pt tables", Check1DLerp18);
        Check("1D interpolation in descending 2pt tables", Check1DLerp2Down);
        Check("1D interpolation in descending 3pt tables", Check1DLerp3Down);
        Check("1D interpolation in descending 4pt tables", Check1DLerp4Down);
        Check("1D interpolation in descending 6pt tables", Check1DLerp6Down);
        Check("1D interpolation in descending 18pt tables", Check1DLerp18Down);

        if (exhaustive)
        {
            Check("1D interpolation in n tables", ExhaustiveCheck1DLerp);
            Check("1D interpolation in descending tables", ExhaustiveCheck1DLerpDown);
        }
    }

    using (logger.BeginScope("Forward 3D interpolation"))
    {
        Check("3D interpolation Tetrahedral (float)", Check3DInterpolationFloatTetrahedral);
        Check("3D interpolation Trilinear (float)", Check3DInterpolationFloatTrilinear);
        Check("3D interpolation Tetrahedral (16)", Check3DInterpolationTetrahedral16);
        Check("3D interpolation Trilinear (16)", Check3DInterpolationTrilinear16);

        if (exhaustive)
        {
            Check("Exhaustive 3D interpolation Tetrahedral (float)", ExhaustiveCheck3DInterpolationFloatTetrahedral);
            Check("Exhaustive 3D interpolation Trilinear (float)", ExhaustiveCheck3DInterpolationFloatTrilinear);
            Check("Exhaustive 3D interpolation Tetrahedral (16)", ExhaustiveCheck3DInterpolationTetrahedral16);
            Check("Exhaustive 3D interpolation Trilinear (16)", ExhaustiveCheck3DInterpolationTrilinear16);
        }

        Check("Reverse interpolation 3 -> 3", CheckReverseInterpolation3x3);
        Check("Reverse interpolation 4 -> 3", CheckReverseInterpolation4x3);
    }

    using (logger.BeginScope("High dimensionality interpolation"))
    {
        Check("3D interpolation", Check3Dinterp);
        Check("3D interpolation with granularity", Check3DinterpGranular);
        Check("4D interpolation", Check4Dinterp);
        Check("4D interpolation with granularity", Check4DinterpGranular);
        Check("5D interpolation with granularity", Check5DinterpGranular);
        Check("6D interpolation with granularity", Check6DinterpGranular);
        Check("7D interpolation with granularity", Check7DinterpGranular);
        Check("8D interpolation with granularity", Check8DinterpGranular);
    }

    using (logger.BeginScope("Encoding of colorspaces"))
    {
        Check("Lab to LCh and back (float only)", CheckLab2LCh);
        Check("Lab to XYZ and back (float only)", CheckLab2XYZ);
        Check("Lab to xyY and back (float only)", CheckLab2xyY);
        Check("Lab V2 encoding", CheckLabV2encoding);
        Check("Lab V4 encoding", CheckLabV4encoding);
    }

    using (logger.BeginScope("BlackBody"))
        Check("Blackbody radiator", CheckTemp2CHRM);

    using (logger.BeginScope("Tone curves"))
    {
        Check("Linear gamma curves (16 bits)", CheckGammaCreation16);
        Check("Linear gamma curves (float)", CheckGammaCreationFlt);

        Check("Curve 1.8 (float)", CheckGamma18);
        Check("Curve 2.2 (float)", CheckGamma22);
        Check("Curve 3.0 (float)", CheckGamma30);

        Check("Curve 1.8 (table)", CheckGamma18Table);
        Check("Curve 2.2 (table)", CheckGamma22Table);
        Check("Curve 3.0 (table)", CheckGamma30Table);

        Check("Curve 1.8 (word table)", CheckGamma18TableWord);
        Check("Curve 2.2 (word table)", CheckGamma22TableWord);
        Check("Curve 3.0 (word table)", CheckGamma30TableWord);

        Check("Parametric curves", CheckParametricToneCurves);

        Check("Join curves", CheckJointCurves);
        Check("Join curves descending", CheckJointCurvesDescending);
        Check("Join curves degenerated", CheckReverseDegenerated);
        Check("Join curves sRGB (Float)", CheckJointFloatCurves_sRGB);
        Check("Join curves sRGB (16 bits)", CheckJoint16Curves_sRGB);
        Check("Join curves sigmoidal", CheckJointCurvesSShaped);
    }

    using (logger.BeginScope("LUT basics"))
    {
        Check("LUT creation & dup", CheckLUTcreation);
        Check("1 Stage LUT ", Check1StageLUT);
        Check("2 Stage LUT ", Check2StageLUT);
        Check("2 Stage LUT (16 bits)", Check2Stage16LUT);
        Check("3 Stage LUT ", Check3StageLUT);
        Check("3 Stage LUT (16 bits)", Check3Stage16LUT);
        Check("4 Stage LUT ", Check4StageLUT);
        Check("4 Stage LUT (16 bits)", Check4Stage16LUT);
        Check("5 Stage LUT ", Check5StageLUT);
        Check("5 Stage LUT (16 bits)", Check5Stage16LUT);
        Check("6 Stage LUT ", Check6StageLUT);
        Check("6 Stage LUT (16 bits)", Check6Stage16LUT);
    }

    using (logger.BeginScope("LUT operation"))
    {
        Check("Lab to Lab LUT (float only)", CheckLab2LabLUT);
        Check("XYZ to XYZ LUT (float only)", CheckXYZ2XYZLUT);
        Check("Lab to Lab MAT LUT (float only)", CheckLab2LabMatLUT);
        Check("Named Color LUT", CheckNamedColorLUT);
    }

    using (logger.BeginScope("Formatter basic operation"))
    {
        Check("Usual formatters", CheckFormatters16);
        Check("Floating point formatters", CheckFormattersFloat);
        Check("Half formatters", CheckFormattersHalf);
    }

    using (logger.BeginScope("Change buffers format"))
        Check("Change Buffers Format", CheckChangeBufferFormats);

    using (logger.BeginScope("MLU and named color lists"))
    {
        Check("Multilocalized Unicode", CheckMLU);
        Check("Multilocalized Unicode (II)", CheckMLU_UTF8);
        Check("Named color lists", CheckNamedColorList);
        Check("Create named color profile", CreateNamedColorProfile);
    }

    using (logger.BeginScope("Profile I/O"))
    {
        Check("Profile creation", CheckProfileCreation);
        Check("Header version", CheckVersionHeaderWriting);
        Check("Multilocalized profile", CheckMultilocalizedProfile);
    }

    using (logger.BeginScope("Error reporting"))
    {
        Check("Error reporting on bad profiles", CheckErrReportingOnBadProfiles);
        Check("Error reporting on bad transforms", CheckErrReportingOnBadTransforms);
    }

    using (logger.BeginScope("Transforms"))
    {
        Check("Curves only transforms", CheckCurvesOnlyTransforms);
        Check("Float Lab->Lab transforms", CheckFloatLabTransforms);
        Check("Encoded Lab->Lab transforms", CheckEncodedLabTransforms);
        Check("Stored identities", CheckStoredIdentities);

        Check("Matrix-shaper transform (float)", CheckMatrixShaperXFORMFloat);
        Check("Matrix-shaper transform (16 bits)", CheckMatrixShaperXFORM16);
        Check("Matrix-shaper transform (8 bits)", CheckMatrixShaperXFORM8);

        Check("Primaries of sRGB", CheckRGBPrimaries);
    }

    using (logger.BeginScope("Known values"))
    {
        Check("Known values across matrix-shaper", Check_sRGB_Float);
        Check("Gray input profile", CheckInputGray);
        Check("Gray Lab input profile", CheckLabInputGray);
        Check("Gray output profile", CheckOutputGray);
        Check("Gray Lab output profile", CheckLabOutputGray);

        Check("Matrix-shaper proofing transform (float)", CheckProofingXFORMFloat);
        Check("Matrix-shaper proofing transform (16 bits)", CheckProofingXFORM16);

        Check("Gamut check", CheckGamutCheck);

        Check("CMYK roundtrip on perceptual transform", CheckCMYKRoundtrip);

        Check("CMYK perceptual transform", CheckCMYKPerceptual);
        // Test disabled on original
        //Check("CMYK rel.col. transform", CheckCMYKRelCol);

        Check("Black ink only preservation", CheckKOnlyBlackPreserving);
        Check("Black plane preservation", CheckKPlaneBlackPreserving);

        Check("Deciding curve types", CheckV4gamma);

        Check("Black point detection", CheckBlackPoint);
        Check("TAC detection", CheckTAC);

        //Check("CGATS parser", CheckCGATS);
        //Check("CGATS parser on junk", CheckCGATS2);
        //Check("CGATS parser on overflow", CheckCGATS_Overflow);
        //Check("PostScript generator", CheckPostScript);
        Check("Segment maxima GBD", CheckGBD);
        Check("MD5 digest", CheckMD5);
        Check("Linking", CheckLinking);
        Check("floating point tags on XYZ", CheckFloatXYZ);
        Check("RGB->Lab->RGB with alpha on FLT", ChecksRGB2LabFLT);
        Check("Parametric curve on Rec709", CheckParametricRec709);
        Check("Floating Point sampled curve with non-zero start", CheckFloatSamples);
        Check("Floating Point segmented curve with short sampled segment", CheckFloatSegments);
        Check("Read RAW tags", CheckReadRAW);
        Check("Check MetaTag", CheckMeta);
        Check("Null transform on floats", CheckFloatNULLxform);
        Check("Set free a tag", CheckRemoveTag);
        Check("Matrix simplification", CheckMatrixSimplify);
        Check("Planar 8 optimization", CheckPlanar8opt);
        Check("Planar float to int16", CheckPlanarFloat2int);
        Check("Swap endian feature", CheckSE);
        Check("Transform line stride RGB", CheckTransformLineStride);
        Check("Forged MPE profile", CheckForgedMPE);
        Check("Proofing intersection", CheckProofingIntersection);
        Check("Empty MLUC", CheckEmptyMLUC);
        Check("sRGB round-trips", Check_sRGB_Rountrips);
        Check("OkLab color space", Check_OkLab);
        Check("OkLab color space (2)", Check_OkLab2);
        Check("Gamma space detection", CheckGammaSpaceDetection);
        Check("Unbounded mode w/ integer output", CheckIntToFloatTransform);
        Check("Corrupted built-in by using cmsWriteRawTag", CheckInducedCorruption);
        //Check("Bad CGATS file", CheckBadCGATS);
        Check("Saving linearization devicelink", CheckSaveLinearizationDeviceLink);
    }
}

if (!noPluginTests)
{
    using (logger.BeginScope("Plugin tests"))
    {
        Check("Simple context functionality", CheckSimpleContext);
        Check("Alarm codes context", CheckAlarmColorsContext);
        Check("Adaptation state context", CheckAdaptationStateContext);
        Check("1D interpolation plugin", CheckInterp1DPlugin);
        Check("3D interpolation plugin", CheckInterp3DPlugin);
        Check("Parametric curve plugin", CheckParametricCurvePlugin);
        Check("Formatters plugin", CheckFormattersPlugin);
        Check("Tag type plugin", CheckTagTypePlugin);
        Check("MPE type plugin", CheckMPEPlugin);
        Check("Optimization plugin", CheckOptimizationPlugin);
        Check("Rendering intent plugin", CheckIntentPlugin);
        Check("Full transform plugin", CheckTransformPlugin);
        Check("Mutex plugin", CheckMutexPlugin);
    }
}

if (doSpeedTests)
{
    SpeedTest();
}

if (doZooTests)
{
    // CheckProfileZOO();
}

//DebugMemPrintTotals();

Context.Shared.ClearAllPlugins();

// Cleanup
if (!noCheckTests || doSpeedTests)
    RemoveTestProfiles();

//LogArrayPoolUsage(null);

//if (timeTests)
//    PrintTestTimes();

return TotalFail;
