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

using lcms2.io;
using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

namespace lcms2.testbed;

internal static partial class Testbed
{
    const ushort TYPE_SIN = 1000;
    const ushort TYPE_COS = 1010;
    const ushort TYPE_TAN = 1020;
    const ushort TYPE_709 = 709;
    private static Context? DupContext(Context? src, object? Data)
    {
        var cpy = cmsDupContext(src, Data);

        //DebugMemDontCheckThis(cpy);

        return cpy;
    }

    private readonly static PluginInterpolation InterpPluginSample = new(cmsPluginMagicNumber, 2060, cmsPluginInterpolationSig, my_Interpolators_Factory);

    // This fake interpolation takes always the closest lower node in the interpolation table for 1D 
    private static void Fake1Dfloat(ReadOnlySpan<float> Value,
                                    Span<float> Output,
                                    InterpParams<float> p)
    {
        float val2;
        int cell;
        var LutTable = p.Table.Span;

           // Clip upper values
           if (Value[0] >= 1.0) {
               Output[0] = LutTable[(int)p.Domain[0]];
            return;
        }

        val2 = p.Domain[0] * Value[0];
        cell = (int) Math.Floor(val2);
        Output[0] =  LutTable[cell] ;
    }

    // This fake interpolation just uses scrambled negated indexes for output
    private static void Fake3D16(ReadOnlySpan<ushort> Input,
                                 Span<ushort> Output,
                                 InterpParams<ushort> _)
    {
        Output[0] = (ushort)(0xFFFF - Input[2]);
        Output[1] = (ushort)(0xFFFF - Input[1]);
        Output[2] = (ushort)(0xFFFF - Input[0]);
    }

    // The factory chooses interpolation routines on depending on certain conditions.
    private static InterpFunction? my_Interpolators_Factory(uint nInputChannels,
                                                           uint nOutputChannels,
                                                           uint dwFlags)
    {
        //InterpFunction Interpolation;
        var IsFloat = (dwFlags & (uint)LerpFlag.Float) is not 0;

        // Initialize the return to zero as a non-supported mark
        //memset(&Interpolation, 0);

        // For 1D to 1D and floating point
        //if (nInputChannels == 1 && nOutputChannels == 1 && IsFloat)
        //{

        //    Interpolation.LerpFloat = Fake1Dfloat;
        //}
        //else
        //if (nInputChannels == 3 && nOutputChannels == 3 && !IsFloat)
        //{

        //    // For 3D to 3D and 16 bits
        //    Interpolation.Lerp16 = Fake3D16;
        //}

        // Here is the interpolation 
        //return Interpolation;

        return (nInputChannels, nOutputChannels, IsFloat) switch
        {
            (1, 1, true) => new(Fake1Dfloat),
            (3, 3, false) => new(Fake3D16),
            _ => null
        };
    }


//public static bool CheckAllocContext()
//    {
//        var c1 = cmsCreateContext(null, null);      // This creates a context by using the normal malloc
//        //DebugMemDontCheckThis(c1);
//        cmsDeleteContext(c1);

//        //var c2 = cmsCreateContext(DebugMemHandler, null); // This creates a context by using the debug malloc
//        var c2 = cmsCreateContext(null, null);
//        //DebugMemDontCheckThis(c2);
//        cmsDeleteContext(c2);

//        c1 = cmsCreateContext(null, null);
//        //DebugMemDontCheckThis(c1);

//        //c2 = cmsCreateContext(DebugMemHandler, null);
//        c2 = cmsCreateContext(null, null);
//        //DebugMemDontCheckThis(c2);

//        //cmsPluginTHR(c1, DebugMemHandler);    // Now the context has custom allocators

//        var c3 = DupContext(c1, null);
//        var c4 = DupContext(c2, null);

//        cmsGetContextUserData(c1) = 1;
//        cmsGetContextUserData(c2) = 2;
//        cmsGetContextUserData(c3) = 3;
//        cmsGetContextUserData(c4) = 4;

//        cmsDeleteContext(c1);   // Should be deleted by using normal malloc
//        cmsDeleteContext(c2);   // Should be deleted by using debug malloc
//        cmsDeleteContext(c3);   // Should be deleted by using normal malloc
//        cmsDeleteContext(c4);   // Should be deleted by using debug malloc

//        return true;
//    }

    public static bool CheckSimpleContext()
    {
        var a = 1;
        var b = 32;

        // This function creates a context with a special
        // memory manager that checks allocation
        var c1 = WatchDogContext(a);
        cmsDeleteContext(c1);

        c1 = WatchDogContext(a);

        // Let's check duplication
        var c2 = DupContext(c1, null);
        var c3 = DupContext(c2, null);

        // User data should have been propagated
        bool rc = (int?)cmsGetContextUserData(c3) == 1;

        // Free resources
        cmsDeleteContext(c1);
        cmsDeleteContext(c2);
        cmsDeleteContext(c3);

        if (!rc)
        {
            logger.LogWarning("Creation of user data failed");
            return false;
        }

        // Back to create 3 levels of inheritance
        c1 = cmsCreateContext(UserData: a);
        //DebugMemDontCheckThis(c1);

        c2 = DupContext(c1, null);
        c3 = DupContext(c2, b);

        // New user data should be applied to c3
        rc = (int?)cmsGetContextUserData(c3) == 32;

        cmsDeleteContext(c1);
        cmsDeleteContext(c2);
        cmsDeleteContext(c3);

        if (!rc)
        {
            logger.LogWarning("Modification of user data failed");
            return false;
        }

        return true;
    }

    public static bool CheckAlarmColorsContext()
    {
        Span<ushort> codes = stackalloc ushort[16] { 0x0000, 0x1111, 0x2222, 0x3333, 0x4444, 0x5555, 0x6666, 0x7777, 0x8888, 0x9999, 0xaaaa, 0xbbbb, 0xcccc, 0xdddd, 0xeeee, 0xffff };
        Span<ushort> values = stackalloc ushort[16];

        var c1 = WatchDogContext(null);

        cmsSetAlarmCodesTHR(c1, codes);
        var c2 = DupContext(c1, null);
        var c3 = DupContext(c2, null);

        cmsGetAlarmCodesTHR(c3, values);

        var rc = true;

        for (var i = 0; i < 16; i++)
        {
            if (values[i] != codes[i])
            {
                logger.LogWarning("Bad alarm code #{i}: {value} != {code}", i, values[i], codes[i]);
                rc = false;
                break;
            }
        }

        cmsDeleteContext(c1);
        cmsDeleteContext(c2);
        cmsDeleteContext(c3);

        return rc;
    }

    public static bool CheckAdaptationStateContext()
    {
        var rc = false;
        var old1 = cmsSetAdaptationStateTHR(null, -1);

        var c1 = WatchDogContext(null);

        cmsSetAdaptationStateTHR(c1, 0.7);

        var c2 = DupContext(c1, null);
        var c3 = DupContext(c2, null);

        rc = IsGoodVal("Adaption state", cmsSetAdaptationStateTHR(c3, -1), 0.7, 0.001);

        cmsDeleteContext(c1);
        cmsDeleteContext(c2);
        cmsDeleteContext(c3);

        var old2 = cmsSetAdaptationStateTHR(null, -1);

        if (old1 != old2)
        {
            logger.LogWarning("Adaptation state has changed");
            return false;
        }

        return rc;
    }

    // This is the check code for 1D interpolation plug-in
    public static bool CheckInterp1DPlugin()
    {
        ToneCurve? Sampled1D = null;
        Context? cpy = null;
        var tab = new float[] { 0.0f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.00f };  // A straight line

        // 1st level context
        Context? ctx = WatchDogContext(null);
        if (ctx == null)
        {
            logger.LogWarning("Cannot create context");
            goto Error;
        }

        cmsPluginTHR(ctx, InterpPluginSample);

        cpy = DupContext(ctx, null);
        if (cpy == null)
        {
            logger.LogWarning("Cannot create context (2)");
            goto Error;
        }

        Sampled1D = cmsBuildTabulatedToneCurveFloat(cpy, 11, tab);
        if (Sampled1D == null)
        {
            logger.LogWarning("Cannot create tone curve (1)");
            goto Error;
        }

        // Do some interpolations with the plugin
        if (!IsGoodVal("0.10", cmsEvalToneCurveFloat(Sampled1D, 0.10f), 0.10, 0.01)) goto Error;
        if (!IsGoodVal("0.13", cmsEvalToneCurveFloat(Sampled1D, 0.13f), 0.10, 0.01)) goto Error;
        if (!IsGoodVal("0.55", cmsEvalToneCurveFloat(Sampled1D, 0.55f), 0.50, 0.01)) goto Error;
        if (!IsGoodVal("0.9999", cmsEvalToneCurveFloat(Sampled1D, 0.9999f), 0.90, 0.01)) goto Error;

        cmsFreeToneCurve(Sampled1D);
        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);

        // Now in global context
        Sampled1D = cmsBuildTabulatedToneCurveFloat(null, 11, tab);
        if (Sampled1D == null)
        {
            logger.LogWarning("Cannot create tone curve (2)");
            goto Error;
        }

        // Now without the plug-in
        if (!IsGoodVal("0.10", cmsEvalToneCurveFloat(Sampled1D, 0.10f), 0.10, 0.001)) goto Error;
        if (!IsGoodVal("0.13", cmsEvalToneCurveFloat(Sampled1D, 0.13f), 0.13, 0.001)) goto Error;
        if (!IsGoodVal("0.55", cmsEvalToneCurveFloat(Sampled1D, 0.55f), 0.55, 0.001)) goto Error;
        if (!IsGoodVal("0.9999", cmsEvalToneCurveFloat(Sampled1D, 0.9999f), 0.9999, 0.001)) goto Error;

        cmsFreeToneCurve(Sampled1D);
        return true;

    Error:
        if (ctx != null) cmsDeleteContext(ctx);
        if (cpy != null) cmsDeleteContext(ctx);
        if (Sampled1D != null) cmsFreeToneCurve(Sampled1D);
        return false;

    }

    // Checks the 3D interpolation
    public static bool CheckInterp3DPlugin()
    {

        Pipeline p;
        Stage clut;
        Context ctx;
        Span<ushort> In = stackalloc ushort[3];
        Span<ushort> Out = stackalloc ushort[3];
        Span<ushort> identity = stackalloc ushort[] {
            0,       0,       0,
            0,       0,       0xffff,
            0,       0xffff,  0,
            0,       0xffff,  0xffff,
            0xffff,  0,       0,
            0xffff,  0,       0xffff,
            0xffff,  0xffff,  0,
            0xffff,  0xffff,  0xffff
        };


        ctx = WatchDogContext(null);
        if (ctx == null)
        {
            logger.LogWarning("Cannot create context");
            return false;
        }

        cmsPluginTHR(ctx, InterpPluginSample);


        p = cmsPipelineAlloc(ctx, 3, 3);
        clut = cmsStageAllocCLut16bit(ctx, 2, 3, 3, identity)!;
        cmsPipelineInsertStage(p, StageLoc.AtBegin, clut);

        // Do some interpolations with the plugin

        In[0] = 0; In[1] = 0; In[2] = 0;
        cmsPipelineEval16(In, Out, p);

        if (!IsGoodWord("0", Out[0], 0xFFFF - 0)) goto Error;
        if (!IsGoodWord("1", Out[1], 0xFFFF - 0)) goto Error;
        if (!IsGoodWord("2", Out[2], 0xFFFF - 0)) goto Error;

        In[0] = 0x1234; In[1] = 0x5678; In[2] = 0x9ABC;
        cmsPipelineEval16(In, Out, p);

        if (!IsGoodWord("0", 0xFFFF - 0x9ABC, Out[0])) goto Error;
        if (!IsGoodWord("1", 0xFFFF - 0x5678, Out[1])) goto Error;
        if (!IsGoodWord("2", 0xFFFF - 0x1234, Out[2])) goto Error;

        cmsPipelineFree(p);
        cmsDeleteContext(ctx);

        // Now without the plug-in

        p = cmsPipelineAlloc(null, 3, 3);
        clut = cmsStageAllocCLut16bit(null, 2, 3, 3, identity)!;
        cmsPipelineInsertStage(p, StageLoc.AtBegin, clut);

        In[0] = 0; In[1] = 0; In[2] = 0;
        cmsPipelineEval16(In, Out, p);

        if (!IsGoodWord("0", 0, Out[0])) goto Error;
        if (!IsGoodWord("1", 0, Out[1])) goto Error;
        if (!IsGoodWord("2", 0, Out[2])) goto Error;

        In[0] = 0x1234; In[1] = 0x5678; In[2] = 0x9ABC;
        cmsPipelineEval16(In, Out, p);

        if (!IsGoodWord("0", 0x1234, Out[0])) goto Error;
        if (!IsGoodWord("1", 0x5678, Out[1])) goto Error;
        if (!IsGoodWord("2", 0x9ABC, Out[2])) goto Error;

        cmsPipelineFree(p);
        return true;

    Error:
        cmsPipelineFree(p);
        return false;

    }
    private static double my_fns(int Type,
                                 ReadOnlySpan<double> Params,
                                 double R) =>
        Type switch
        {
            TYPE_SIN => Params[0] * Math.Sin(R * M_PI),
            -TYPE_SIN => Math.Asin(R) / (M_PI * Params[0]),
            TYPE_COS => Params[0] * Math.Cos(R * M_PI),
            -TYPE_COS => Math.Acos(R) / (M_PI * Params[0]),
            _ => -1.0
        };

    private static double my_fns2(int Type, ReadOnlySpan<double> Params, double R) =>
        Type switch
        {
            TYPE_TAN => Params[0] * Math.Tan(R * M_PI),
            -TYPE_TAN => Math.Asin(R) / (M_PI * Params[0]),
            _ => -1.0
        };

    private static double Rec709Math(int Type, ReadOnlySpan<double> Params, double R) =>
        Type switch
        {
            709 =>
                (R <= (Params[3] * Params[4]))
                    ? R / Params[3]
                    : Math.Pow(((R - Params[2]) / Params[1]), Params[0]),
            -709 =>
                (R <= Params[4])
                    ? R * Params[3]
                    : Params[1] * Math.Pow(R, (1 / Params[0])) + Params[2],
            _ => 0
        };


    // Add nonstandard TRC curves -> Rec709

    private readonly static PluginParametricCurves Rec709Plugin = new(
        cmsPluginMagicNumber, 2060, cmsPluginParametricCurveSig, new (int, uint)[] { (TYPE_709, 5) }, Rec709Math);


    private readonly static PluginParametricCurves CurvePluginSample = new(
        cmsPluginMagicNumber, 2060, cmsPluginParametricCurveSig, new (int, uint)[] { (TYPE_SIN, 1), (TYPE_COS, 1) }, my_fns);

    private readonly static PluginParametricCurves CurvePluginSample2 = new(
        cmsPluginMagicNumber, 2060, cmsPluginParametricCurveSig, new (int, uint)[] { (TYPE_TAN, 1) }, my_fns2);

    // --------------------------------------------------------------------------------------------------
    // In this test, the DupContext function will be checked as well                      
    // --------------------------------------------------------------------------------------------------
    public static bool CheckParametricCurvePlugin()
    {
        ToneCurve sinus;
        ToneCurve cosinus;
        ToneCurve tangent;
        ToneCurve reverse_sinus;
        ToneCurve reverse_cosinus;
        Span<double> scale = stackalloc double[] { 1.0 };


        Context? ctx = WatchDogContext(null);

        cmsPluginTHR(ctx, CurvePluginSample);
        Context? cpy = DupContext(ctx, null);

        cmsPluginTHR(cpy, CurvePluginSample2);
        Context? cpy2 = DupContext(cpy, null);

        cmsPluginTHR(cpy2, Rec709Plugin);


        sinus = cmsBuildParametricToneCurve(cpy, TYPE_SIN, scale);
        cosinus = cmsBuildParametricToneCurve(cpy, TYPE_COS, scale);
        tangent = cmsBuildParametricToneCurve(cpy, TYPE_TAN, scale);
        reverse_sinus = cmsReverseToneCurve(sinus);
        reverse_cosinus = cmsReverseToneCurve(cosinus);


        if (!IsGoodVal("0.10", cmsEvalToneCurveFloat(sinus, 0.10f), Math.Sin(0.10 * M_PI), 0.001)) goto Error;
        if (!IsGoodVal("0.60", cmsEvalToneCurveFloat(sinus, 0.60f), Math.Sin(0.60 * M_PI), 0.001)) goto Error;
        if (!IsGoodVal("0.90", cmsEvalToneCurveFloat(sinus, 0.90f), Math.Sin(0.90 * M_PI), 0.001)) goto Error;

        if (!IsGoodVal("0.10", cmsEvalToneCurveFloat(cosinus, 0.10f), Math.Cos(0.10 * M_PI), 0.001)) goto Error;
        if (!IsGoodVal("0.60", cmsEvalToneCurveFloat(cosinus, 0.60f), Math.Cos(0.60 * M_PI), 0.001)) goto Error;
        if (!IsGoodVal("0.90", cmsEvalToneCurveFloat(cosinus, 0.90f), Math.Cos(0.90 * M_PI), 0.001)) goto Error;

        if (!IsGoodVal("0.10", cmsEvalToneCurveFloat(tangent, 0.10f), Math.Tan(0.10 * M_PI), 0.001)) goto Error;
        if (!IsGoodVal("0.60", cmsEvalToneCurveFloat(tangent, 0.60f), Math.Tan(0.60 * M_PI), 0.001)) goto Error;
        if (!IsGoodVal("0.90", cmsEvalToneCurveFloat(tangent, 0.90f), Math.Tan(0.90 * M_PI), 0.001)) goto Error;


        if (!IsGoodVal("0.10", cmsEvalToneCurveFloat(reverse_sinus, 0.10f), Math.Asin(0.10) / M_PI, 0.001)) goto Error;
        if (!IsGoodVal("0.60", cmsEvalToneCurveFloat(reverse_sinus, 0.60f), Math.Asin(0.60) / M_PI, 0.001)) goto Error;
        if (!IsGoodVal("0.90", cmsEvalToneCurveFloat(reverse_sinus, 0.90f), Math.Asin(0.90) / M_PI, 0.001)) goto Error;

        if (!IsGoodVal("0.10", cmsEvalToneCurveFloat(reverse_cosinus, 0.10f), Math.Acos(0.10) / M_PI, 0.001)) goto Error;
        if (!IsGoodVal("0.60", cmsEvalToneCurveFloat(reverse_cosinus, 0.60f), Math.Acos(0.60) / M_PI, 0.001)) goto Error;
        if (!IsGoodVal("0.90", cmsEvalToneCurveFloat(reverse_cosinus, 0.90f), Math.Acos(0.90) / M_PI, 0.001)) goto Error;

        cmsFreeToneCurve(sinus);
        cmsFreeToneCurve(cosinus);
        cmsFreeToneCurve(tangent);
        cmsFreeToneCurve(reverse_sinus);
        cmsFreeToneCurve(reverse_cosinus);

        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);
        cmsDeleteContext(cpy2);

        return true;

    Error:

        cmsFreeToneCurve(sinus);
        cmsFreeToneCurve(reverse_sinus);
        cmsFreeToneCurve(cosinus);
        cmsFreeToneCurve(reverse_cosinus);

        if (ctx != null) cmsDeleteContext(ctx);
        if (cpy != null) cmsDeleteContext(cpy);
        if (cpy2 != null) cmsDeleteContext(cpy2);
        return false;
    }

    // We define this special type as 0 bytes not float, and set the upper bit 

    private readonly static uint TYPE_RGB_565 = (COLORSPACE_SH(PT_RGB) | CHANNELS_SH(3) | BYTES_SH(0) | (1 << 23));

    private static ReadOnlySpan<byte> my_Unroll565(Transform _1,
                                                   Span<ushort> wIn,
                                                   ReadOnlySpan<byte> accum,
                                                   uint _2)
    {
        var pixel = BitConverter.ToUInt16(accum);  // Take whole pixel

        double r = Math.Floor(((pixel & 31) * 65535.0) / 31.0 + 0.5);
        double g = Math.Floor((((pixel >> 5) & 63) * 65535.0) / 63.0 + 0.5);
        double b = Math.Floor((((pixel >> 11) & 31) * 65535.0) / 31.0 + 0.5);

        wIn[2] = (ushort) r;
        wIn[1] = (ushort) g;
        wIn[0] = (ushort) b;
    
        return accum.Length <= 2 ? default : accum[2..];
    }

    private static Span<byte> my_Pack565(Transform _1,
                                         ReadOnlySpan<ushort> wOut,
                                         Span<byte> output,
                                         uint _2)
    {

        ushort pixel;
        int r, g, b;

        r = (int)Math.Floor((wOut[2] * 31) / 65535.0 + 0.5);
        g = (int)Math.Floor((wOut[1] * 63) / 65535.0 + 0.5);
        b = (int)Math.Floor((wOut[0] * 31) / 65535.0 + 0.5);


        pixel = (ushort)((r & 31) | ((g & 63) << 5) | ((b & 31) << 11));


        BitConverter.TryWriteBytes(output, pixel);
        return output.Length <= 2 ? default : output[2..];
    }

    private static FormatterIn my_FormatterFactory(uint Type, uint dwFlags)
    {
        FormatterIn Result = default;

        if ((Type == TYPE_RGB_565) &&
            (dwFlags & (uint)PackFlags.Float) is 0)
        {
            Result.Fmt16 = my_Unroll565;
        }
        return Result;
    }

    private static FormatterOut my_FormatterFactory2(uint Type, uint dwFlags)
    {
        FormatterOut Result = default;

        if ((Type == TYPE_RGB_565) &&
            (dwFlags & (uint)PackFlags.Float) is 0)
        {
            Result.Fmt16 = my_Pack565;
        }
        return Result;
    }

    private readonly static PluginFormatters FormattersPluginSample = new(cmsPluginMagicNumber, 2060, cmsPluginFormattersSig) 
    {
        FormattersFactoryIn = my_FormatterFactory
    };



    private readonly static PluginFormatters FormattersPluginSample2 = new(cmsPluginMagicNumber, 2060, cmsPluginFormattersSig) 
    {
        FormattersFactoryOut = my_FormatterFactory2
    };


    public static bool CheckFormattersPlugin()
    {
        Context? ctx = WatchDogContext(null);
        Span<ushort> stream = stackalloc ushort[] { (ushort)0xffffU, (ushort)0x1234U, (ushort)0x0000U, (ushort)0x33ddU };
        Span<ushort> result = stackalloc ushort[4];
        int i;

        cmsPluginTHR(ctx, FormattersPluginSample);

        var cpy = DupContext(ctx, null);

        cmsPluginTHR(cpy, FormattersPluginSample2);

        var cpy2 = DupContext(cpy, null);

        var xform = cmsCreateTransformTHR(cpy2, null, TYPE_RGB_565, null, TYPE_RGB_565, INTENT_PERCEPTUAL, cmsFLAGS_NULLTRANSFORM);

        cmsDoTransform<ushort, ushort>(xform, stream, result, 4);

        cmsDeleteTransform(xform);
        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);
        cmsDeleteContext(cpy2);

        for (i = 0; i < 4; i++)
            if (stream[i] != result[i]) return false;

        return true;
    }

    const uint SigIntType = 0x74747448;   //   'tttH'
    const uint SigInt = 0x74747448;       //   'tttH'

    private static Box<uint>? Type_int_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _)
    {
        nItems = 0;
        if (!io.ReadUint(out var value)) return null;
        nItems = 1;
        return new(value);
    }

    private static bool Type_int_Write(TagTypeHandler _1, IOHandler io, object Ptr, uint _2) =>
        io.Write(((Box<uint>)Ptr).Value);

    private static object? Type_int_Dup(TagTypeHandler self, object Ptr, uint n) =>
        new Box<uint>(((Box<uint>)Ptr).Value);

    private static void Type_int_Free(TagTypeHandler self, object? Ptr)
    { }


    private readonly static PluginTag HiddenTagPluginSample = new(
        cmsPluginMagicNumber,
        2060,
        cmsPluginTagSig,
        SigInt,
        new(
            1,
            new Signature[] { SigIntType },
            null));

    private readonly static PluginTagType FirstTagTypePluginSample = new(
        cmsPluginMagicNumber,
        2060,
        cmsPluginTagTypeSig,
        new(
            SigIntType,
            Type_int_Read,
            Type_int_Write,
            Type_int_Dup,
            Type_int_Free,
            null,
            0));

    private readonly static List<PluginBase> TagTypePluginSample = new() { FirstTagTypePluginSample, HiddenTagPluginSample };


    public static bool CheckTagTypePlugin()
    {
        Context? ctx = null;
        Context? cpy = null;
        Context? cpy2 = null;
        Profile? h = null;
        const uint myTag = 1234;
        bool rc = false;
        byte[]? data = null;
        Box<uint>? ptr = null;


        ctx = WatchDogContext(null);
        cmsPluginTHR(ctx, TagTypePluginSample);

        cpy = DupContext(ctx, null);
        cpy2 = DupContext(cpy, null);

        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);

        h = cmsCreateProfilePlaceholder(cpy2);
        if (h == null)
        {
            logger.LogWarning("Create placeholder failed");
            goto Error;
        }


        if (!cmsWriteTag(h, SigInt, new Box<uint>(myTag)))
        {
            logger.LogWarning("Plug-in failed");
            goto Error;
        }

        rc = cmsSaveProfileToMem(h, null, out var clen);
        if (!rc)
        {
            logger.LogWarning("Fetch mem size failed");
            goto Error;
        }


        data = new byte[(int)clen];
        //if (data == null)
        //{
        //    Fail("malloc failed ?!?");
        //    goto Error;
        //}


        rc = cmsSaveProfileToMem(h, data, out clen);
        if (!rc)
        {
            logger.LogWarning("Save to mem failed");
            goto Error;
        }

        cmsCloseProfile(h);

        cmsSetLogErrorHandler(BuildNullLogger());
        h = cmsOpenProfileFromMem(data, clen);
        if (h == null)
        {
            logger.LogWarning("Open profile failed");
            goto Error;
        }

        ptr = cmsReadTag(h, SigInt) as Box<uint>;
        if (ptr != null)
        {

            logger.LogWarning("read tag/context switching failed");
            goto Error;
        }

        cmsCloseProfile(h);
        cmsSetLogErrorHandler(BuildDebugLogger());

        h = cmsOpenProfileFromMemTHR(cpy2, data, clen);
        if (h == null)
        {
            logger.LogWarning("Open profile from mem failed");
            goto Error;
        }

        // Get rid of data
        /*free(data);*/ data = null;

        ptr = cmsReadTag(h, SigInt) as Box<uint>;
        if (ptr == null)
        {
            logger.LogWarning("Read tag/context switching failed (2)");
            return false;
        }

        rc = ptr.Value == 1234;

        cmsCloseProfile(h);

        cmsDeleteContext(cpy2);

        return rc;

    Error:

        if (h != null) cmsCloseProfile(h);
        if (ctx != null) cmsDeleteContext(ctx);
        if (cpy != null) cmsDeleteContext(cpy);
        if (cpy2 != null) cmsDeleteContext(cpy2);
        //if (data is not null) free(data);

        return false;
    }

    private const uint SigNegateType = 0x6E202020;

    private static void EvaluateNegate(ReadOnlySpan<float> In, Span<float> Out, Stage _)
    {
        Out[0] = 1.0f - In[0];
        Out[1] = 1.0f - In[1];
        Out[2] = 1.0f - In[2];
    }

    private static Stage? StageAllocNegate(Context? ContextID)
    {
        return new(ContextID,
                     SigNegateType, 3, 3, EvaluateNegate,
                     null, null, null);
    }

    private static Stage? Type_negate_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _)
    {
        nItems = 0;
        if (!io.ReadUshort(out var Chans)) return null;
        if (Chans != 3) return null;

        nItems = 1;
        return StageAllocNegate(self.ContextID);
    }

    private static bool Type_negate_Write(TagTypeHandler _1, IOHandler io, object? _2, uint _3)
    {
        return io.Write((ushort)3);
    }

    private readonly static PluginTagType MPEPluginSample = new(
        cmsPluginMagicNumber,
        2060,
        cmsPluginMultiProcessElementSig,
        new(
            SigNegateType,
            Type_negate_Read,
            Type_negate_Write,
            null,
            null,
            null,
            0));


    public static bool CheckMPEPlugin()
    {
        Context? ctx = null;
        Context? cpy = null;
        Context? cpy2 = null;
        Profile? h = null;
        bool rc = false;
        byte[]? data = null;
        Span<float> In = stackalloc float[3];
        Span<float> Out = stackalloc float[3];
        Pipeline? pipe;

        ctx = WatchDogContext(null);
        cmsPluginTHR(ctx, MPEPluginSample);

        cpy = DupContext(ctx, null);
        cpy2 = DupContext(cpy, null);

        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);

        h = cmsCreateProfilePlaceholder(cpy2);
        if (h == null)
        {
            logger.LogWarning("Create placeholder failed");
            goto Error;
        }

        pipe = cmsPipelineAlloc(cpy2, 3, 3);
        cmsPipelineInsertStage(pipe, StageLoc.AtBegin, StageAllocNegate(cpy2));


        In[0] = 0.3f; In[1] = 0.2f; In[2] = 0.9f;
        cmsPipelineEvalFloat(In, Out, pipe);

        rc = IsGoodVal("0", Out[0], 1.0 - In[0], 0.001) &&
             IsGoodVal("1", Out[1], 1.0 - In[1], 0.001) &&
             IsGoodVal("2", Out[2], 1.0 - In[2], 0.001);

        if (!rc)
        {
            logger.LogWarning("Pipeline failed");
            goto Error;
        }

        if (!cmsWriteTag(h, cmsSigDToB3Tag, pipe))
        {
            logger.LogWarning("Plug-in failed");
            goto Error;
        }

        // This cleans the stage as well
        cmsPipelineFree(pipe);

        rc = cmsSaveProfileToMem(h, null, out var clen);
        if (!rc)
        {
            logger.LogWarning("Fetch mem size failed");
            goto Error;
        }


        data = new byte[(int)clen];
        if (data == null)
        {
            logger.LogWarning("malloc failed ?!?");
            goto Error;
        }


        rc = cmsSaveProfileToMem(h, data, out clen);
        if (!rc)
        {
            logger.LogWarning("Save to mem failed");
            goto Error;
        }

        cmsCloseProfile(h);

        cmsSetLogErrorHandler(BuildNullLogger());
        h = cmsOpenProfileFromMem(data, clen);
        if (h == null)
        {
            logger.LogWarning("Open profile failed");
            goto Error;
        }

        pipe = cmsReadTag(h, cmsSigDToB3Tag) as Pipeline;
        if (pipe != null)
        {

            // Unsupported stage, should fail
            logger.LogWarning("read tag/context switching failed");
            goto Error;
        }

        cmsCloseProfile(h);

        cmsSetLogErrorHandler(BuildDebugLogger());

        h = cmsOpenProfileFromMemTHR(cpy2, data, clen);
        if (h == null)
        {
            logger.LogWarning("Open profile from mem failed");
            goto Error;
        }

        // Get rid of data
        /*free(data);*/ data = null;

        pipe = cmsReadTag(h, cmsSigDToB3Tag) as Pipeline;
        if (pipe == null)
        {
            logger.LogWarning("Read tag/context switching failed (2)");
            return false;
        }

        // Evaluate for negation
        In[0] = 0.3f; In[1] = 0.2f; In[2] = 0.9f;
        cmsPipelineEvalFloat(In, Out, pipe);

        rc = IsGoodVal("0", Out[0], 1.0 - In[0], 0.001) &&
             IsGoodVal("1", Out[1], 1.0 - In[1], 0.001) &&
             IsGoodVal("2", Out[2], 1.0 - In[2], 0.001);

        cmsCloseProfile(h);

        cmsDeleteContext(cpy2);

        return rc;

    Error:

        if (h != null) cmsCloseProfile(h);
        if (ctx != null) cmsDeleteContext(ctx);
        if (cpy != null) cmsDeleteContext(cpy);
        if (cpy2 != null) cmsDeleteContext(cpy2);
        //if (data != null) free(data);

        return false;
    }

    private static void FastEvaluateCurves(ReadOnlySpan<ushort> In, Span<ushort> Out, object? _)
    {
        Out[0] = In[0];
    }

    private static bool MyOptimize(ref Pipeline Lut, uint Intent, ref uint InputFormat, ref uint OutputFormat, ref uint dwFlags)
    {
        Stage? mpe;
        StageToneCurvesData? Data;

        //  Only curves in this LUT? All are identities?
        for (mpe = cmsPipelineGetPtrToFirstStage(Lut);
             mpe != null;
             mpe = cmsStageNext(mpe))
        {

            if (cmsStageType(mpe) != cmsSigCurveSetElemType) return false;

            // Check for identity
            Data = cmsStageData(mpe) as StageToneCurvesData;
            if (Data?.nCurves != 1) return false;
            if (cmsEstimateGamma(Data.TheCurves[0], 0.1) > 1.0) return false;

        }

        dwFlags |= cmsFLAGS_NOCACHE;
        Lut.SetOptimizationParameters(FastEvaluateCurves, null, null, null);

        return true;
    }

    private readonly static PluginOptimization OptimizationPluginSample = new(cmsPluginMagicNumber, 2060, cmsPluginOptimizationSig, MyOptimize);


    public static bool CheckOptimizationPlugin()
    {
        Context? ctx = WatchDogContext(null);
        Span<byte> In = stackalloc byte[] { 10, 20, 30, 40 };
        Span<byte> Out = stackalloc byte[4];
        var Linear = new ToneCurve[1];
        Profile? h;
        int i;

        cmsPluginTHR(ctx, OptimizationPluginSample);

        var cpy = DupContext(ctx, null);
        var cpy2 = DupContext(cpy, null);

        Linear[0] = cmsBuildGamma(cpy2, 1.0);
        h = cmsCreateLinearizationDeviceLinkTHR(cpy2, cmsSigGrayData, Linear);
        cmsFreeToneCurve(Linear[0]);

        var xform = cmsCreateTransformTHR(cpy2, h, TYPE_GRAY_8, h, TYPE_GRAY_8, INTENT_PERCEPTUAL, 0);
        cmsCloseProfile(h);

        cmsDoTransform(xform, In, Out, 4);

        cmsDeleteTransform(xform);
        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);
        cmsDeleteContext(cpy2);

        for (i = 0; i < 4; i++)
            if (In[i] != Out[i]) return false;

        return true;
    }

    private const uint INTENT_DECEPTIVE = 300;

    private static Pipeline? MyNewIntent(Context ContextID, uint nProfiles, ReadOnlySpan<uint> TheIntents, Profile[] hProfiles, ReadOnlySpan<bool> BPC, ReadOnlySpan<double> AdaptationStates, uint dwFlags)
    {
        Pipeline? Result;
        Span<uint> ICCIntents = stackalloc uint[256];

        for (var i = 0; i < nProfiles; i++)
            ICCIntents[i] = (TheIntents[i] == INTENT_DECEPTIVE) ? INTENT_PERCEPTUAL : TheIntents[i];

        if (cmsGetColorSpace(hProfiles[0]) != cmsSigGrayData || cmsGetColorSpace(hProfiles[(int)nProfiles - 1]) != cmsSigGrayData)
            return _cmsDefaultICCintents(ContextID, nProfiles, ICCIntents, hProfiles, BPC, AdaptationStates, dwFlags);

        Result = cmsPipelineAlloc(ContextID, 1, 1);
        if (Result == null) return null;

        cmsPipelineInsertStage(Result, StageLoc.AtBegin, cmsStageAllocIdentity(ContextID, 1));

        return Result;
    }

    private readonly static PluginRenderingIntent IntentPluginSample = new(
        cmsPluginMagicNumber,
        2060,
        cmsPluginRenderingIntentSig,
        INTENT_DECEPTIVE,
        MyNewIntent,
        "bypass gray to gray rendering intent");

    public static bool CheckIntentPlugin()
    {
        Context? ctx = WatchDogContext(null);
        Profile? h1, h2;
        ToneCurve Linear1;
        ToneCurve Linear2;
        Span<byte> In = stackalloc byte[] { 10, 20, 30, 40 };
        Span<byte> Out = stackalloc byte[4];
        int i;

        cmsPluginTHR(ctx, IntentPluginSample);

        var cpy = DupContext(ctx, null);
        var cpy2 = DupContext(cpy, null);

        Linear1 = cmsBuildGamma(cpy2, 3.0)!;
        Linear2 = cmsBuildGamma(cpy2, 0.1)!;
        h1 = cmsCreateLinearizationDeviceLinkTHR(cpy2, cmsSigGrayData, new ToneCurve[] { Linear1 });
        h2 = cmsCreateLinearizationDeviceLinkTHR(cpy2, cmsSigGrayData, new ToneCurve[] { Linear2 });

        cmsFreeToneCurve(Linear1);
        cmsFreeToneCurve(Linear2);

        var xform = cmsCreateTransformTHR(cpy2, h1, TYPE_GRAY_8, h2, TYPE_GRAY_8, INTENT_DECEPTIVE, 0);
        cmsCloseProfile(h1); cmsCloseProfile(h2);

        cmsDoTransform(xform, In, Out, 4);

        cmsDeleteTransform(xform);
        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);
        cmsDeleteContext(cpy2);

        for (i = 0; i < 4; i++)
            if (Out[i] != In[i]) return false;

        return true;
    }

    // This is a sample intent that only works for gray8 as output, and always returns '42'
    private static void TrancendentalTransform(Transform _1, ReadOnlySpan<byte> _2, Span<byte> OutputBuffer, uint Size, uint _3)
    {
        for (var i=0; i < Size; i++)
            OutputBuffer[i] = 0x42;

    }


    private static bool TransformFactory(out TransformFn xformPtr, out object? _1, out FreeUserDataFn? _2, ref Pipeline Lut, ref uint _3, ref uint OutputFormat, ref uint _4)

    {
        _1 = _2 = null;
        if (OutputFormat == TYPE_GRAY_8)
        {
            // *Lut holds the pipeline to be applied
            xformPtr = TrancendentalTransform;
            return true;
        }

        xformPtr = null!;
        return false;
    }


    // The Plug-in entry point
    private readonly static PluginTransform FullTransformPluginSample = new(
        cmsPluginMagicNumber,
        2060,
        cmsPluginTransformSig,
        new() { legacy_xform = TransformFactory });

    public static bool CheckTransformPlugin()
    {
        Context? ctx = WatchDogContext(null);
        Span<byte> In = stackalloc byte[] { 10, 20, 30, 40 };
        Span<byte> Out = stackalloc byte[4];
        ToneCurve Linear;
        Profile? h;
        int i;

        cmsPluginTHR(ctx, FullTransformPluginSample);

        var cpy = DupContext(ctx, null);
        var cpy2 = DupContext(cpy, null);

        Linear = cmsBuildGamma(cpy2, 1.0)!;
        h = cmsCreateLinearizationDeviceLinkTHR(cpy2, cmsSigGrayData, new ToneCurve[] { Linear });
        cmsFreeToneCurve(Linear);

        var xform = cmsCreateTransformTHR(cpy2, h, TYPE_GRAY_8, h, TYPE_GRAY_8, INTENT_PERCEPTUAL, 0);
        cmsCloseProfile(h);

        cmsDoTransform(xform, In, Out, 4);


        cmsDeleteTransform(xform);
        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);
        cmsDeleteContext(cpy2);

        for (i = 0; i < 4; i++)
            if (Out[i] != 0x42) return false;

        return true;
    }

    private struct MyMtx {
        public int nlocks;
    }


    private static Box<MyMtx> MyMtxCreate(Context id)
    {
        var mtx = new MyMtx()
        {
            nlocks = 0
        };
        return new(mtx);
    }

    private static void MyMtxDestroy(Context id, object mtx)
    {
        var mtx_ = mtx as Box<MyMtx>;

        if (mtx_?.Value.nlocks != 0)
        {
            Die("Locks != 0 when setting free a mutex");
        }

        //_cmsFree(id, mtx);

    }

    private static bool MyMtxLock(Context id, object? mtx)
    {
        var mtx_ = mtx as Box<MyMtx>;
        if (mtx_ is not null)
            mtx_.Value.nlocks++;

        return true;
    }

    private static void MyMtxUnlock(Context id, object? mtx)
    {
        var mtx_ = mtx as Box<MyMtx>;
        if (mtx_ is not null)
            mtx_.Value.nlocks--;

    }


    private readonly static PluginLegacyMutex MutexPluginSample = new(cmsPluginMagicNumber, 2060, cmsPluginMutexSig, MyMtxCreate, MyMtxDestroy, MyMtxLock, MyMtxUnlock);


    public static bool CheckMutexPlugin()
    {
        Context? ctx = WatchDogContext(null);
        Span<byte> In = stackalloc byte[] { 10, 20, 30, 40 };
        Span<byte> Out = stackalloc byte[4];
        ToneCurve Linear;
        Profile? h;
        int i;

        cmsPluginTHR(ctx, MutexPluginSample);

        var cpy = DupContext(ctx, null);
        var cpy2 = DupContext(cpy, null);

        Linear = cmsBuildGamma(cpy2, 1.0)!;
        h = cmsCreateLinearizationDeviceLinkTHR(cpy2, cmsSigGrayData, new ToneCurve[] { Linear });
        cmsFreeToneCurve(Linear);

        var xform = cmsCreateTransformTHR(cpy2, h, TYPE_GRAY_8, h, TYPE_GRAY_8, INTENT_PERCEPTUAL, 0);
        cmsCloseProfile(h);

        cmsDoTransform(xform, In, Out, 4);


        cmsDeleteTransform(xform);
        cmsDeleteContext(ctx);
        cmsDeleteContext(cpy);
        cmsDeleteContext(cpy2);

        for (i = 0; i < 4; i++)
            if (Out[i] != In[i]) return false;

        return true;
    }

}
