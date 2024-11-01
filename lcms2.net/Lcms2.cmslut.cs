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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace lcms2;

public static partial class Lcms2
{
    private static void EvaluateIdentity(ReadOnlySpan<float> @in, Span<float> @out, Stage mpe)
    {
        memcpy(@out, @in, mpe.InputChannels * sizeof(float));
    }

    public static Stage? cmsStageAllocIdentity(Context? ContextID, uint nChannels) =>
        new(
            ContextID,
            Signature.Stage.IdentityElem,
            nChannels,
            nChannels,
            EvaluateIdentity,
            null,
            null,
            null);

    private static void FromFloatTo16(ReadOnlySpan<float> In, Span<ushort> Out, uint n)
    {
        for (var i = 0; i < n; i++)
        {
            Out[i] = _cmsQuickSaturateWord(In[i] * 65535.0f);
        }
    }

    private static void From16ToFloat(ReadOnlySpan<ushort> In, Span<float> Out, uint n)
    {
        for (var i = 0; i < n; i++)
        {
            Out[i] = In[i] / 65535.0f;
        }
    }

    public static bool cmsPipelineCheckAndRetrieveStages(
        Pipeline Lut, Signature sig1, [NotNullWhen(true)] out Stage? out1) =>
        cmsPipelineCheckAndRetrieveStages(
            Lut, sig1, out out1,
                 default, out var _,
                 default, out var _,
                 default, out var _,
                 default, out var _);

    public static bool cmsPipelineCheckAndRetrieveStages(
        Pipeline Lut, Signature sig1, [NotNullWhen(true)] out Stage? out1,
                      Signature sig2, [NotNullWhen(true)] out Stage? out2) =>
        cmsPipelineCheckAndRetrieveStages(
            Lut, sig1, out out1,
                 sig2, out out2,
                 default, out var _,
                 default, out var _,
                 default, out var _);

    public static bool cmsPipelineCheckAndRetrieveStages(
        Pipeline Lut, Signature sig1, [NotNullWhen(true)] out Stage? out1,
                      Signature sig2, [NotNullWhen(true)] out Stage? out2,
                      Signature sig3, [NotNullWhen(true)] out Stage? out3) =>
        cmsPipelineCheckAndRetrieveStages(
            Lut, sig1, out out1,
                 sig2, out out2,
                 sig3, out out3,
                 default, out var _,
                 default, out var _);

    public static bool cmsPipelineCheckAndRetrieveStages(
        Pipeline Lut, Signature sig1, [NotNullWhen(true)] out Stage? out1,
                      Signature sig2, [NotNullWhen(true)] out Stage? out2,
                      Signature sig3, [NotNullWhen(true)] out Stage? out3,
                      Signature sig4, [NotNullWhen(true)] out Stage? out4) =>
        cmsPipelineCheckAndRetrieveStages(
            Lut, sig1, out out1,
                 sig2, out out2,
                 sig3, out out3,
                 sig4, out out4,
                 default, out var _);

    public static bool cmsPipelineCheckAndRetrieveStages(
        Pipeline Lut, Signature sig1, [NotNullWhen(true)] out Stage? out1,
                      Signature sig2, [NotNullWhen(true)] out Stage? out2,
                      Signature sig3, [NotNullWhen(true)] out Stage? out3,
                      Signature sig4, [NotNullWhen(true)] out Stage? out4,
                      Signature sig5, [NotNullWhen(true)] out Stage? out5)
    {
        out1 = out2 = out3 = out4 = out5 = null!;

        var n = (uint)sig2 is 0
            ? 1
            : (uint)sig3 is 0
                ? 2
                : (uint)sig4 is 0
                    ? 3
                    : (uint)sig5 is 0
                        ? 4
                        : 5;

        Span<Signature> args = stackalloc Signature[] { sig1, sig2, sig3, sig4, sig5 };
        args = args[..n];

        // Make sure same number of elements
        if (cmsPipelineStageCount(Lut) != n) return false;

        // Iterate across asked types
        var mpe = Lut.Elements;
        for (var i = 0; i < n; i++)
        {
            // Get asked type.
            var Type = args[i];
            if (mpe?.Type != Type) return false;
            mpe = mpe.Next;
        }

        // Found a combination, fill pointers
        out1 = Lut.Elements!;

        if (out1.Next is not null)
        {
            out2 = out1.Next;
            if (out2.Next is not null)
            {
                out3 = out2.Next;
                if (out3.Next is not null)
                {
                    out4 = out3.Next;
                    if (out4.Next is not null)
                        out5 = out4.Next;
                }
            }
        }

        return true;
    }

    internal static Span<ToneCurve> _cmsStageGetPtrToCurveSet(Stage mpe) =>
        (mpe.Data is StageToneCurvesData Data)
            ? (Data is not null)
                ? Data.TheCurves.AsSpan(..(int)Data.nCurves)
                : null
            : null;

    private static void EvaluateCurves(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        if (mpe.Data is not StageToneCurvesData Data || Data.TheCurves is null)
            return;

        for (var i = 0; i < Data.nCurves; i++)
        {
            Out[i] = cmsEvalToneCurveFloat(Data.TheCurves[i], In[i]);
        }
    }

    private static void CurveSetElemTypeFree(Stage mpe)
    {
        if (mpe.Data is StageToneCurvesData Data)
            FreeCurveSetElems(mpe.ContextID, Data);
    }

    private static void FreeCurveSetElems(Context? ContextID, StageToneCurvesData Data)
    {
        if (Data.TheCurves is not null)
        {
            for (var i = 0; i < Data.nCurves; i++)
            {
                if (Data.TheCurves[i] is not null)
                {
                    cmsFreeToneCurve(Data.TheCurves[i]);
                }
            }
        }

        //ReturnArray(ContextID, Data.TheCurves);
    }

    private static object? CurveSetDup(Stage mpe)
    {
        if (mpe.Data is not StageToneCurvesData Data)
            return null;

        var NewElem = new StageToneCurvesData(mpe.ContextID, Data.nCurves);

        //if (NewElem is null) return null;

        //NewElem.TheCurves = _cmsCalloc2<ToneCurve>(mpe.ContextID, NewElem.nCurves);
        //if (NewElem.TheCurves is null) goto Error;

        for (var i = 0; i < NewElem.nCurves; i++)
        {
            // Duplicate each curve. It may fail.
            NewElem.TheCurves[i] = cmsDupToneCurve(Data.TheCurves[i])!;
            if (NewElem.TheCurves[i] is null) goto Error;
        }

        return NewElem;

    Error:
        FreeCurveSetElems(mpe.ContextID, NewElem);
        return null;
    }

    public static Stage? cmsStageAllocToneCurves(Context? ContextID, uint nChannels, ReadOnlySpan<ToneCurve> Curves)
    {
        var NewMPE = new Stage(ContextID, Signature.Stage.CurveSetElem, nChannels, nChannels, EvaluateCurves, CurveSetDup, CurveSetElemTypeFree, null);
        if (NewMPE is null) return null;

        var NewElem = new StageToneCurvesData(ContextID, nChannels);
        //if (NewElem is null)
        //{
        //    cmsStageFree(NewMPE);
        //    return null;
        //}

        NewMPE.Data = NewElem;

        //NewElem.nCurves = nChannels;
        //NewElem.TheCurves = _cmsCalloc2<ToneCurve>(ContextID, nChannels);
        //if (NewElem.TheCurves is null)
        //{
        //    cmsStageFree(NewMPE);
        //    return null;
        //}

        for (var i = 0; i < nChannels; i++)
        {
            NewElem.TheCurves[i] = !Curves.IsEmpty
                ? cmsDupToneCurve(Curves[i])!
                : cmsBuildGamma(ContextID, 1.0)!;

            if (NewElem.TheCurves[i] is null)
            {
                cmsStageFree(NewMPE);
                return null;
            }
        }

        return NewMPE;
    }

    internal static Stage? _cmsStageAllocIdentityCurves(Context? ContextID, uint nChannels)
    {
        var mpe = cmsStageAllocToneCurves(ContextID, nChannels, null);

        if (mpe is null) return null;
        mpe.Implements = Signature.Stage.IdentityElem;
        return mpe;
    }

    private static void EvaluateMatrix(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        if (mpe.Data is not StageMatrixData Data)
            return;

        // Input is already in 0..1.0 notation
        for (var i = 0; i < mpe.OutputChannels; i++)
        {
            var Tmp = 0.0;
            for (var j = 0; j < mpe.InputChannels; j++)
                Tmp += In[j] * Data.Double[(i * mpe.InputChannels) + j];

            if (Data.Offset is not null)
                Tmp += Data.Offset[i];

            Out[i] = (float)Tmp;
        }

        // Output in 0..1.0 domain
    }

    private static object? MatrixElemDup(Stage mpe)
    {
        if (mpe.Data is not StageMatrixData Data)
            return null;
        //var pool = _cmsGetContext(mpe.ContextID).GetBufferPool<double>();

        var sz = (int)(mpe.InputChannels * mpe.OutputChannels);

        var NewElem = Data.Offset is not null
            ? new StageMatrixData(Data.Double.AsSpan()[..sz], Data.Offset.AsSpan()[..(int)mpe.OutputChannels]/*, pool*/)
            : new StageMatrixData(Data.Double.AsSpan()[..sz], default/*, pool*/);

        //if (NewElem is null) return null;

        //NewElem.Double = _cmsDupMem<double>(mpe.ContextID, Data.Double, sz);

        //if (Data.Offset is not null)
        //{
        //    //NewElem.Offset = _cmsDupMem<double>(mpe.ContextID, Data.Offset, mpe.OutputChannels);
        //}

        return NewElem;
    }

    private static void MatrixElemTypeFree(Stage mpe)
    {
        if (mpe.Data is not StageMatrixData Data)
            return;

        Data.Dispose();
    }

    public static Stage? cmsStageAllocMatrix(Context? ContextID, uint Rows, uint Cols, MAT3 Matrix, VEC3 Offset)
    {
        //var pool = Context.GetPool<double>(ContextID);

        var mat = Matrix.AsArray(/*pool*/);
        var off = Offset.AsArray(/*pool*/);

        var result = cmsStageAllocMatrix(ContextID, Rows, Cols, mat, off);

        //pool.Return(mat);
        //pool.Return(off);

        return result;
    }

    public static Stage? cmsStageAllocMatrix(Context? ContextID, uint Rows, uint Cols, MAT3 Matrix, ReadOnlySpan<double> Offset)
    {
        //var pool = Context.GetPool<double>(ContextID);

        var mat = Matrix.AsArray(/*pool*/);

        var result = cmsStageAllocMatrix(ContextID, Rows, Cols, mat, Offset);

        //pool.Return(mat);

        return result;
    }

    public static Stage? cmsStageAllocMatrix(Context? ContextID, uint Rows, uint Cols, ReadOnlySpan<double> Matrix, ReadOnlySpan<double> Offset)
    {
        var n = (int)(Rows * Cols);

        // Check for overflow
        if (n is 0) return null;
        if (n >= uint.MaxValue / Cols) return null;
        if (n >= uint.MaxValue / Rows) return null;
        if (n < Rows || n < Cols) return null;

        var NewMPE = new Stage(ContextID, Signature.Stage.MatrixElem, Cols, Rows, EvaluateMatrix, MatrixElemDup, MatrixElemTypeFree, null);
        if (NewMPE is null) return null;

        //var NewElem = new StageMatrixData();
        //if (NewElem is null) goto Error;
        //var pool = Context.GetPool<double>(ContextID);
        var NewElem = Offset.Length >= Rows
            ? new StageMatrixData(Matrix, Offset[..(int)Rows]/*, pool*/)
            : new StageMatrixData(Matrix, default/*, pool*/);

        NewMPE.Data = NewElem;

        //NewElem.Double = _cmsCalloc<double>(ContextID, n);
        //if (NewElem.Double is null) goto Error;

        //for (var i = 0; i < n; i++)
        //    NewElem.Double[i] = Matrix[i];

        //if (Offset.Length >= Rows)
        //{
        //    //NewElem.Offset = _cmsCalloc<double>(ContextID, Rows);
        //    //if (NewElem.Offset is null) goto Error;

        //    //for (var i = 0; i < Rows; i++)
        //    //    NewElem.Offset[i] = Offset[i];
        //}

        return NewMPE;

        //Error:
        //    cmsStageFree(NewMPE);
        //    return null;
    }

    private static void EvaluateCLUTfloat(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        if (mpe.Data is not StageCLutData<float> Data)
            return;

        Data.Params.Interpolation.LerpFloat?.Invoke(In, Out, Data.Params);
    }

    private static void EvaluateCLUTfloatIn16(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        if (mpe.Data is not StageCLutData<ushort> Data)
            return;

        Span<ushort> In16 = stackalloc ushort[MAX_STAGE_CHANNELS];
        Span<ushort> Out16 = stackalloc ushort[MAX_STAGE_CHANNELS];

        _cmsAssert(mpe.InputChannels <= MAX_STAGE_CHANNELS);
        _cmsAssert(mpe.OutputChannels <= MAX_STAGE_CHANNELS);

        FromFloatTo16(In, In16, mpe.InputChannels);
        Data.Params.Interpolation.Lerp16?.Invoke(In16, Out16, Data.Params);
        From16ToFloat(Out16, Out, mpe.OutputChannels);
    }

    private static uint CubeSize(ReadOnlySpan<uint> Dims, uint b)
    {
        var rv = 0u;

        _cmsAssert(Dims);

        for (rv = 1; b > 0; b--)
        {
            var dim = Dims[(int)b - 1];
            if (dim <= 1) return 0;  // Error

            rv *= dim;

            // Check for overflow
            if (rv > uint.MaxValue / dim) return 0;
        }

        // Again, prevent overflow
        if (rv > uint.MaxValue / 15) return 0;

        return rv;
    }

    private static object? CLUTElemDup(Stage mpe)
    {
        if (mpe.Data is StageCLutData<float> DataF)
        {
            //var pool = Context.GetPool<float>(mpe.ContextID);

            var NewElem = new StageCLutData<float>
            {
                nEntries = DataF.nEntries
            };

            //if (NewElem is null) return null;

            if (DataF.Tab is not null)
            {
                //NewElem.Tab = _cmsDupMem<float>(mpe.ContextID, Data.TFloat, Data.nEntries);
                //if (NewElem.Tab is null)
                //    goto Error;
                //NewElem.Tab = pool.Rent((int)DataF.nEntries);
                NewElem.Tab = new float[DataF.nEntries];
                DataF.TFloat.CopyTo(NewElem.TFloat);
            }

            var @params = InterpParams<float>.Create(mpe.ContextID,
                                                     DataF.Params.nSamples,
                                                     DataF.Params.nInputs,
                                                     DataF.Params.nOutputs,
                                                     NewElem.Tab,
                                                     (LerpFlag)DataF.Params.dwFlags)!;
            if (@params is not null)
            {
                NewElem.Params = @params;
                return NewElem;
            }
        }
        if (mpe.Data is StageCLutData<ushort> Data)
        {
            //var pool = Context.GetPool<ushort>(mpe.ContextID);

            var NewElem = new StageCLutData<ushort>
            {
                nEntries = Data.nEntries
            };

            if (Data.Tab is not null)
            {
                //NewElem.Tab = _cmsDupMem<ushort>(mpe.ContextID, Data.T, Data.nEntries);
                //if (NewElem.Tab is null)
                //    goto Error;
                //NewElem.Tab = pool.Rent((int)Data.nEntries);
                NewElem.Tab = new ushort[Data.nEntries];
                Data.TUshort.CopyTo(NewElem.TUshort);
            }

            var @params = InterpParams<ushort>.Create(mpe.ContextID,
                                                      Data.Params.nSamples,
                                                      Data.Params.nInputs,
                                                      Data.Params.nOutputs,
                                                      NewElem.Tab,
                                                      (LerpFlag)Data.Params.dwFlags)!;
            if (@params is not null)
            {
                NewElem.Params = @params;
                return NewElem;
            }
        }

        //Error:
        //if (NewElem.Tab.T is not null)
        //    // This works for both types
        //    _cmsFree(mpe.ContextID, NewElem.Tab.T);
        return null;
    }

    private static void CLutElemTypeFree(Stage mpe)
    {
        // This works for both types
        //if (Data.Tab.T is not null)
        //    _cmsFree(mpe.ContextID, Data.Tab.T);

        if (mpe.Data is StageCLutData<float> DataF)
        {
            //ReturnArray(mpe.ContextID, DataF.Tab);
            DataF.Tab = null;

            DataF.Params.Dispose();
        }

        if (mpe.Data is StageCLutData<ushort> DataU)
        {
            //ReturnArray(mpe.ContextID, DataU.Tab);
            DataU.Tab = null;

            DataU.Params.Dispose();
        }
    }

    public static Stage? cmsStageAllocCLut16bitGranular(
        Context? ContextID,
        ReadOnlySpan<uint> clutPoints,
        uint inputChan,
        uint outputChan,
        ReadOnlySpan<ushort> Table)
    {
        _cmsAssert(clutPoints);

        if (inputChan > MAX_INPUT_DIMENSIONS)
        {
            LogError(ContextID, ErrorCodes.Range, $"Too many input channels ({inputChan} channels, max={MAX_INPUT_DIMENSIONS})");

            return null;
        }

        var NewMPE = new Stage(ContextID, Signature.Stage.CLutElem, inputChan, outputChan, EvaluateCLUTfloatIn16, CLUTElemDup, CLutElemTypeFree, null);

        if (NewMPE is null) return null;

        var NewElem = new StageCLutData<ushort>();
        if (NewElem is null)
        {
            cmsStageFree(NewMPE);
            return null;
        }

        NewMPE.Data = NewElem;

        var n = NewElem.nEntries = outputChan * CubeSize(clutPoints, inputChan);

        if (n is 0)
        {
            cmsStageFree(NewMPE);
            return null;
        }

        //NewElem.Tab = _cmsCalloc<ushort>(ContextID, n);
        //NewElem.Tab = Context.GetPool<ushort>(ContextID).Rent((int)n);
        //Array.Clear(NewElem.Tab);
        NewElem.Tab = new ushort[n];
        //if (NewElem.Tab is null)
        //{
        //    cmsStageFree(NewMPE);
        //    return null;
        //}

        if (!Table.IsEmpty)
        {
            for (var i = 0; i < n; i++)
            {
                NewElem.TUshort[i] = Table[i];
            }
        }

        NewElem.Params = InterpParams<ushort>.Create(ContextID, clutPoints, inputChan, outputChan, NewElem.Tab, LerpFlag.Ushort);
        if (NewElem.Params is null)
        {
            cmsStageFree(NewMPE);
            return null;
        }

        return NewMPE;
    }

    public static Stage? cmsStageAllocCLut16bit(
        Context? ContextID,
        uint nGridPoints,
        uint inputChan,
        uint outputChan,
        ReadOnlySpan<ushort> Table)
    {
        Span<uint> Dimensions = stackalloc uint[MAX_INPUT_DIMENSIONS];

        // Our resulting LUT would be same gridpoints on all dimensions
        for (var i = 0; i < MAX_INPUT_DIMENSIONS; i++)
            Dimensions[i] = nGridPoints;

        return cmsStageAllocCLut16bitGranular(ContextID, Dimensions, inputChan, outputChan, Table);
    }

    public static Stage? cmsStageAllocCLutFloat(
        Context? ContextID,
        uint nGridPoints,
        uint inputChan,
        uint outputChan,
        ReadOnlySpan<float> Table)
    {
        Span<uint> Dimensions = stackalloc uint[MAX_INPUT_DIMENSIONS];

        // Our resulting LUT would be same gridpoints on all dimensions
        for (var i = 0; i < MAX_INPUT_DIMENSIONS; i++)
            Dimensions[i] = nGridPoints;

        return cmsStageAllocCLutFloatGranular(ContextID, Dimensions, inputChan, outputChan, Table);
    }

    public static Stage? cmsStageAllocCLutFloatGranular(
        Context? ContextID,
        ReadOnlySpan<uint> clutPoints,
        uint inputChan,
        uint outputChan,
        ReadOnlySpan<float> Table)
    {
        _cmsAssert(clutPoints);

        if (inputChan > MAX_INPUT_DIMENSIONS)
        {
            LogError(ContextID, ErrorCodes.Range, $"Too many input channels ({inputChan} channels, max={MAX_INPUT_DIMENSIONS})");
            return null;
        }

        var NewMPE = new Stage(
            ContextID,
            Signature.Stage.CLutElem,
            inputChan,
            outputChan,
            EvaluateCLUTfloat,
            CLUTElemDup,
            CLutElemTypeFree,
            null);
        if (NewMPE is null) return null;

        var NewElem = new StageCLutData<float>();
        if (NewElem is null)
        {
            cmsStageFree(NewMPE);
            return null;
        }

        NewMPE.Data = NewElem;

        // There is a potential integer overflow on conputing n and nEntries.
        var n = NewElem.nEntries = outputChan * CubeSize(clutPoints, inputChan);

        if (n is 0)
        {
            cmsStageFree(NewMPE);
            return null;
        }

        //NewElem.Tab = _cmsCalloc<float>(ContextID, n);
        //if (NewElem.Tab is null)
        //{
        //    cmsStageFree(NewMPE);
        //    return null;
        //}
        //NewElem.Tab = Context.GetPool<float>(ContextID).Rent((int)n);
        NewElem.Tab = new float[n];

        if (!Table.IsEmpty)
        {
            for (var i = 0; i < n; i++)
            {
                NewElem.TFloat[i] = Table[i];
            }
        }

        NewElem.Params = InterpParams<float>.Create(ContextID, clutPoints, inputChan, outputChan, NewElem.Tab, LerpFlag.Float);
        if (NewElem.Params is null)
        {
            cmsStageFree(NewMPE);
            return null;
        }

        return NewMPE;
    }

    private static bool IdentitySampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        if (Cargo is not Box<int> nChan)
            return false;

        for (var i = 0; i < nChan; i++)
            Out[i] = In[i];

        return true;
    }

    internal static Stage? _cmsStageAllocIdentityCLut(Context? ContextID, uint nChan)
    {
        Span<uint> Dimensions = stackalloc uint[MAX_INPUT_DIMENSIONS];

        for (var i = 0; i < MAX_INPUT_DIMENSIONS; i++)
            Dimensions[i] = 2;

        var mpe = cmsStageAllocCLut16bitGranular(ContextID, Dimensions, nChan, nChan, null);
        if (mpe is null) return null;

        if (!cmsStageSampleCLut16bit(mpe, IdentitySampler, new Box<int>((int)nChan), 0))
        {
            cmsStageFree(mpe);
            return null;
        }

        mpe.Implements = Signature.Stage.IdentityElem;
        return mpe;
    }

    [DebuggerStepThrough]
    internal static ushort _cmsQuantizeVal(double i, uint MaxSamples)
    {
        var x = (i * 65535.0) / (MaxSamples - 1);
        return _cmsQuickSaturateWord(x);
    }

    public static bool cmsStageSampleCLut16bit(Stage? mpe, SAMPLER16 Sampler, object? Cargo, SamplerFlag dwFlags)
    {
        Span<ushort> In = stackalloc ushort[MAX_INPUT_DIMENSIONS + 1];
        Span<ushort> Out = stackalloc ushort[MAX_INPUT_DIMENSIONS];

        if (mpe?.Data is not StageCLutData<ushort> clut)
            return false;

        var nSamples = clut.Params.nSamples;
        var nInputs = clut.Params.nInputs;
        var nOutputs = clut.Params.nOutputs;

        if (nInputs <= 0) return false;
        if (nOutputs <= 0) return false;
        if (nInputs > MAX_INPUT_DIMENSIONS) return false;
        if (nOutputs >= MAX_STAGE_CHANNELS) return false;

        var nTotalPoints = CubeSize(nSamples, nInputs);
        if (nTotalPoints is 0) return false;

        var index = 0;
        for (var i = 0; i < (int)nTotalPoints; i++)
        {
            var rest = i;
            for (var t = (int)nInputs - 1; t >= 0; --t)
            {
                var Colorant = (uint)(rest % nSamples[t]);

                rest /= (int)nSamples[t];

                In[t] = _cmsQuantizeVal(Colorant, nSamples[t]);
            }

            if (!clut.TUshort.IsEmpty)
            {
                for (var t = 0; t < (int)nOutputs; t++)
                    Out[t] = clut.TUshort[index + t];
            }

            if (!Sampler(In, Out, Cargo))
                return false;

            if (dwFlags.IsUnset(SamplerFlag.Inspect))
            {
                if (!clut.TUshort.IsEmpty)
                {
                    for (var t = 0; t < (int)nOutputs; t++)
                        clut.TUshort[index + t] = Out[t];
                }
            }

            index += (int)nOutputs;
        }

        return true;
    }

    public static bool cmsStageSampleCLutFloat(Stage? mpe, SAMPLERFLOAT Sampler, object? Cargo, SamplerFlag dwFlags)
    {
        Span<float> In = stackalloc float[MAX_INPUT_DIMENSIONS + 1];
        Span<float> Out = stackalloc float[MAX_INPUT_DIMENSIONS];

        if (mpe?.Data is not StageCLutData<float> clut)
            return false;

        var nSamples = clut.Params.nSamples;
        var nInputs = clut.Params.nInputs;
        var nOutputs = clut.Params.nOutputs;

        if (nInputs <= 0) return false;
        if (nOutputs <= 0) return false;
        if (nInputs > MAX_INPUT_DIMENSIONS) return false;
        if (nOutputs >= MAX_STAGE_CHANNELS) return false;

        var nTotalPoints = CubeSize(nSamples, nInputs);
        if (nTotalPoints is 0) return false;

        var index = 0;
        for (var i = 0; i < (int)nTotalPoints; i++)
        {
            var rest = i;
            for (var t = (int)nInputs - 1; t >= 0; --t)
            {
                var Colorant = (uint)(rest % nSamples[t]);

                rest /= (int)nSamples[t];

                In[t] = (float)(_cmsQuantizeVal(Colorant, nSamples[t]) / 65535.0);
            }

            if (!clut.TFloat.IsEmpty)
            {
                for (var t = 0; t < (int)nOutputs; t++)
                    Out[t] = clut.TFloat[index + t];
            }

            if (!Sampler(In, Out, Cargo))
                return false;

            if (dwFlags.IsUnset(SamplerFlag.Inspect))
            {
                if (!clut.TFloat.IsEmpty)
                {
                    for (var t = 0; t < (int)nOutputs; t++)
                        clut.TFloat[index + t] = Out[t];
                }
            }

            index += (int)nOutputs;
        }

        return true;
    }

    public static bool cmsSliceSpace16(uint nInputs, ReadOnlySpan<uint> clutPoints, SAMPLER16 Sampler, object? Cargo)
    {
        Span<ushort> In = stackalloc ushort[cmsMAXCHANNELS];

        if (nInputs >= cmsMAXCHANNELS) return false;

        var nTotalPoints = CubeSize(clutPoints, nInputs);
        if (nTotalPoints is 0) return false;

        for (var i = 0; i < (int)nTotalPoints; i++)
        {
            var rest = i;
            for (var t = (int)nInputs - 1; t >= 0; --t)
            {
                var Colorant = (uint)(rest % clutPoints[t]);

                rest /= (int)clutPoints[t];
                In[t] = _cmsQuantizeVal(Colorant, clutPoints[t]);
            }

            if (!Sampler(In, null, Cargo)) return false;
        }

        return true;
    }

    public static bool cmsSliceSpaceFloat(uint nInputs, ReadOnlySpan<uint> clutPoints, SAMPLERFLOAT Sampler, object? Cargo)
    {
        Span<float> In = stackalloc float[cmsMAXCHANNELS];

        if (nInputs >= cmsMAXCHANNELS) return false;

        var nTotalPoints = CubeSize(clutPoints, nInputs);
        if (nTotalPoints is 0) return false;

        for (var i = 0; i < (int)nTotalPoints; i++)
        {
            var rest = i;
            for (var t = (int)nInputs - 1; t >= 0; --t)
            {
                var Colorant = (uint)(rest % clutPoints[t]);

                rest /= (int)clutPoints[t];
                In[t] = (float)(_cmsQuantizeVal(Colorant, clutPoints[t]) / 65535.0);
            }

            if (!Sampler(In, null, Cargo)) return false;
        }

        return true;
    }

    private static void EvaluateLab2XYZ(ReadOnlySpan<float> In, Span<float> Out, Stage _)
    {
        CIELab Lab;
        CIEXYZ XYZ;
        const double XYZadj = MAX_ENCODEABLE_XYZ;

        // V4 rules
        Lab.L = In[0] * 100.0;
        Lab.a = (In[1] * 255.0) - 128.0;
        Lab.b = (In[2] * 255.0) - 128.0;

        cmsLab2XYZ(null, out XYZ, Lab);

        // From XYZ, range 0..19997 to 0..1.0, note that 1.99997 comes from 0xffff
        // encoded as 1.15 fixed point, so 1 + (32767.0 / 32768.0)

        Out[0] = (float)(XYZ.X / XYZadj);
        Out[1] = (float)(XYZ.Y / XYZadj);
        Out[2] = (float)(XYZ.Z / XYZadj);
    }

    internal static Stage? _cmsStageAllocLab2XYZ(Context? ContextID) =>
        new Stage(ContextID, Signature.Stage.Lab2XYZElem, 3, 3, EvaluateLab2XYZ, null, null, null);

    internal static Stage? _cmsStageAllocLabV2ToV4curves(Context? ContextID)
    {
        //var pool = Context.GetPool<ToneCurve>(ContextID);
        //ToneCurve[] LabTable = pool.Rent(3);
        var LabTable = new ToneCurve[3];

        LabTable[0] = cmsBuildTabulatedToneCurve16(ContextID, 258, null)!;
        LabTable[1] = cmsBuildTabulatedToneCurve16(ContextID, 258, null)!;
        LabTable[2] = cmsBuildTabulatedToneCurve16(ContextID, 258, null)!;

        for (var j = 0; j < 3; j++)
        {
            if (LabTable[j] is null)
            {
                cmsFreeToneCurveTriple(LabTable);
                //ReturnArray(pool, LabTable);
                return null;
            }

            // We need to map * (0xffff / 0xff00), that's same as (257 / 256)
            // So we can use 258-entry tables to do the trick (i / 257) * (255 * 257) * (257 / 256);
            for (var i = 0; i < 257; i++)
            {
                LabTable[j].Table16![i] = (ushort)(((i * 0xffff) + 0x80) >> 8);
            }

            LabTable[j].Table16![257] = 0xffff;
        }

        var mpe = cmsStageAllocToneCurves(ContextID, 3, LabTable);
        cmsFreeToneCurveTriple(LabTable);
        //ReturnArray(pool, LabTable);

        if (mpe is null) return null;
        mpe.Implements = Signature.Stage.LabV2toV4Elem;
        return mpe;
    }

    internal static Stage? _cmsStageAllocLabV2ToV4(Context? ContextID)
    {
        ReadOnlySpan<double> V2ToV4 = stackalloc double[] {
            65535.0 / 65280.0, 0, 0,
            0, 65535.0 / 65280.0, 0,
            0, 0, 65535.0 / 65280.0
        };

        var mpe = cmsStageAllocMatrix(ContextID, 3, 3, V2ToV4, null);

        if (mpe is null) return mpe;
        mpe.Implements = Signature.Stage.LabV2toV4Elem;
        return mpe;
    }

    internal static Stage? _cmsStageAllocLabV4ToV2(Context? ContextID)
    {
        ReadOnlySpan<double> V4ToV2 = stackalloc double[] {
            65280.0 / 65535.0, 0, 0,
            0, 65280.0 / 65535.0, 0,
            0, 0, 65280.0 / 65535.0
        };

        var mpe = cmsStageAllocMatrix(ContextID, 3, 3, V4ToV2, null);

        if (mpe is null) return mpe;
        mpe.Implements = Signature.Stage.LabV4toV2Elem;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeFromLabFloat(Context? ContextID)
    {
        ReadOnlySpan<double> a1 = stackalloc double[] {
            1.0 / 100.0, 0, 0,
            0, 1.0 / 255.0, 0,
            0, 0, 1.0 / 255.0
        };
        ReadOnlySpan<double> o1 = stackalloc double[] {
            0,
            128.0 / 255.0,
            128.0 / 255.0
        };

        var mpe = cmsStageAllocMatrix(ContextID, 3, 3, a1, o1);

        if (mpe is null) return mpe;
        mpe.Implements = Signature.Stage.Lab2FloatPCS;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeFromXyzFloat(Context? ContextID)
    {
        const double n = 32768.0 / 65535.0;
        ReadOnlySpan<double> a1 = stackalloc double[9]
        {
            n, 0, 0,
            0, n, 0,
            0, 0, n,
        };

        var mpe = cmsStageAllocMatrix(ContextID, 3, 3, a1, null);

        if (mpe is null) return mpe;
        mpe.Implements = Signature.Stage.XYZ2FloatPCS;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeToLabFloat(Context? ContextID)
    {
        ReadOnlySpan<double> a1 = stackalloc double[9] {
            100.0, 0, 0,
            0, 255.0, 0,
            0, 0, 255.0
        };
        ReadOnlySpan<double> o1 = stackalloc double[3] {
            0,
            -128.0,
            -128.0
        };

        var mpe = cmsStageAllocMatrix(ContextID, 3, 3, a1, o1);
        if (mpe is null) return mpe;
        mpe.Implements = Signature.Stage.FloatPCS2Lab;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeToXYZFloat(Context? ContextID)
    {
        const double n = 65535.0 / 32768;
        ReadOnlySpan<double> a1 = stackalloc double[9]
        {
            n, 0, 0,
            0, n, 0,
            0, 0, n,
        };

        var mpe = cmsStageAllocMatrix(ContextID, 3, 3, a1, null);
        if (mpe is null) return mpe;
        mpe.Implements = Signature.Stage.FloatPCS2XYZ;
        return mpe;
    }

    private static void Clipper(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        for (var i = 0; i < mpe.InputChannels; i++)
        {
            var n = In[i];
            Out[i] = MathF.Max(n, 0);
        }
    }

    internal static Stage? _cmsStageClipNegatives(Context? ContextID, uint nChannels) =>
        new Stage(ContextID, Signature.Stage.ClipNegativesElem, nChannels, nChannels, Clipper, null, null, null);

    private static void EvaluateXYZ2Lab(ReadOnlySpan<float> In, Span<float> Out, Stage _)
    {
        CIELab Lab;
        CIEXYZ XYZ;
        const double XYZadj = MAX_ENCODEABLE_XYZ;

        // From 0..1.0 to XYZ
        XYZ.X = In[0] * XYZadj;
        XYZ.Y = In[1] * XYZadj;
        XYZ.Z = In[2] * XYZadj;

        cmsXYZ2Lab(null, out Lab, XYZ);

        // From V4 Lab to 0..1.0
        Out[0] = (float)(Lab.L / 100);
        Out[1] = (float)((Lab.a + 128) / 255);
        Out[2] = (float)((Lab.b + 128) / 255);
    }

    internal static Stage? _cmsStageAllocXYZ2Lab(Context? ContextID) =>
        new Stage(ContextID, Signature.Stage.XYZ2LabElem, 3, 3, EvaluateXYZ2Lab, null, null, null);

    internal static Stage? _cmsStageAllocLabPrelin(Context? ContextID)
    {
        //var pool = Context.GetPool<ToneCurve>(ContextID);
        //ToneCurve?[] LabTable = pool.Rent(3);
        var LabTable = new ToneCurve?[3];
        Span<double> Params = stackalloc double[1] { 2.4 };

        LabTable[0] = cmsBuildGamma(ContextID, 1.0);
        LabTable[1] = cmsBuildParametricToneCurve(ContextID, 108, Params);
        LabTable[2] = cmsBuildParametricToneCurve(ContextID, 108, Params);

        return cmsStageAllocToneCurves(ContextID, 3, LabTable);
    }

    public static void cmsStageFree(Stage? mpe) =>
        //_cmsFree(mpe.ContextID, mpe);
        mpe?.FreePtr?.Invoke(mpe);

    public static uint cmsStageInputChannels(Stage mpe) =>
        mpe.InputChannels;

    public static uint cmsStageOutputChannels(Stage mpe) =>
        mpe.OutputChannels;

    public static Signature cmsStageType(Stage mpe) =>
        mpe.Type;

    public static object? cmsStageData(Stage mpe) =>
        mpe.Data;

    public static Context? cmsGetStageContextID(Stage mpe) =>
        mpe.ContextID;

    public static Stage? cmsStageNext(Stage mpe) =>
        mpe.Next;

    public static Stage? cmsStageDup(Stage? mpe)
    {
        var NewMPE = new Stage(
            mpe.ContextID,
            mpe.Type,
            mpe.InputChannels,
            mpe.OutputChannels,
            mpe.EvalPtr,
            mpe.DupElemPtr,
            mpe.FreePtr,
            null)
        {
            //if (NewMPE is null) return null;

            Implements = mpe.Implements
        };

        if (mpe.DupElemPtr is not null)
        {
            NewMPE.Data = mpe.DupElemPtr(mpe);

            if (NewMPE.Data is null)
            {
                cmsStageFree(NewMPE);
                return null;
            }
        }
        else
        {
            NewMPE.Data = null;
        }

        return NewMPE;
    }

    private static bool BlessLUT(Pipeline? lut)
    {
        if (lut is null)
            return false;
        if (lut.Elements is null)
            return true;

        var First = cmsPipelineGetPtrToFirstStage(lut);
        var Last = cmsPipelineGetPtrToLastStage(lut);

        if (First is null || Last is null) return false;

        lut.InputChannels = First.InputChannels;
        lut.OutputChannels = Last.OutputChannels;

        // Check chain consistency
        var prev = First;
        var next = prev.Next;

        while (next is not null)
        {
            if (next.InputChannels != prev!.OutputChannels)
                return false;

            next = next.Next;
            prev = prev.Next;
        }

        return true;
    }

    internal static void _LUTeval16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? D)
    {
        if (D is not Pipeline lut) return;
        Span<float> Storage = stackalloc float[2 * MAX_STAGE_CHANNELS];
        var Phase = 0;

        From16ToFloat(In, Storage[(Phase * MAX_STAGE_CHANNELS)..], lut.InputChannels);

        for (var mpe = lut.Elements;
            mpe is not null;
            mpe = mpe.Next)
        {
            var NextPhase = Phase ^ 1;
            mpe.EvalPtr(Storage[(Phase * MAX_STAGE_CHANNELS)..], Storage[(NextPhase * MAX_STAGE_CHANNELS)..], mpe);
            Phase = NextPhase;
        }

        FromFloatTo16(Storage[(Phase * MAX_STAGE_CHANNELS)..], Out, lut.OutputChannels);
    }

    internal static void _LUTevalFloat(ReadOnlySpan<float> In, Span<float> Out, object? D)
    {
        if (D is not Pipeline lut) return;
        Span<float> Storage = stackalloc float[2 * MAX_STAGE_CHANNELS];
        var Phase = 0;

        memmove(Storage[(Phase * MAX_STAGE_CHANNELS)..], In, lut.InputChannels);

        for (var mpe = lut.Elements;
            mpe is not null;
            mpe = mpe.Next)
        {
            var NextPhase = Phase ^ 1;
            mpe.EvalPtr(Storage[(Phase * MAX_STAGE_CHANNELS)..], Storage[(NextPhase * MAX_STAGE_CHANNELS)..], mpe);
            Phase = NextPhase;
        }

        memmove(Out, Storage[(Phase * MAX_STAGE_CHANNELS)..], lut.OutputChannels);
    }

    public static Pipeline? cmsPipelineAlloc(Context? ContextID, uint InputChannels, uint OutputChannels)
    {
        // A value of zero in channels is allowed as a placeholder
        if (InputChannels >= cmsMAXCHANNELS ||
            OutputChannels >= cmsMAXCHANNELS) { return null; }

        var NewLUT = new Pipeline(ContextID, InputChannels, OutputChannels, _LUTeval16, _LUTevalFloat);
        //if (NewLUT is null) return null;

        //NewLUT->InputChannels = InputChannels;
        //NewLUT->OutputChannels = OutputChannels;

        //NewLUT->Eval16Fn = _LUTeval16;
        //NewLUT->EvalFloatFn = _LUTevalFloat;
        //NewLUT->DupDataFn = null;
        //NewLUT->FreeDataFn = null;
        NewLUT.Data = NewLUT;
        //NewLUT->ContextID = ContextID;

        if (!BlessLUT(NewLUT))
        {
            //_cmsFree(ContextID, NewLUT);
            return null;
        }

        return NewLUT;
    }

    public static Context? cmsGetPipelineContextID(Pipeline lut)
    {
        _cmsAssert(lut);
        return lut.ContextID;
    }

    public static uint cmsPipelineInputChannels(Pipeline lut)
    {
        _cmsAssert(lut);
        return lut.InputChannels;
    }

    public static uint cmsPipelineOutputChannels(Pipeline lut)
    {
        _cmsAssert(lut);
        return lut.OutputChannels;
    }

    public static void cmsPipelineFree(Pipeline? lut)
    {
        Stage? Next;

        if (lut is null) return;

        for (var mpe = lut.Elements;
            mpe is not null;
            mpe = Next)
        {
            Next = mpe.Next;
            cmsStageFree(mpe);
        }

        lut.FreeDataFn?.Invoke(lut.ContextID, lut.Data);

        //_cmsFree(lut.ContextID, lut);
    }

    public static void cmsPipelineEval16(ReadOnlySpan<ushort> In, Span<ushort> Out, Pipeline lut)
    {
        _cmsAssert(lut);
        lut.Eval16Fn(In, Out, lut.Data);
    }

    public static void cmsPipelineEvalFloat(ReadOnlySpan<float> In, Span<float> Out, Pipeline lut)
    {
        _cmsAssert(lut);
        lut.EvalFloatFn(In, Out, lut);
    }

    public static Pipeline? cmsPipelineDup(Pipeline? lut)
    {
        Pipeline? NewLUT;
        Stage? NewMPE, Anterior = null, mpe;
        var First = true;

        if (lut is null) return null;

        NewLUT = cmsPipelineAlloc(lut.ContextID, lut.InputChannels, lut.OutputChannels);
        if (NewLUT is null) return null;

        for (mpe = lut.Elements; mpe is not null; mpe = mpe.Next)
        {
            NewMPE = cmsStageDup(mpe);

            if (NewMPE is null)
            {
                cmsPipelineFree(NewLUT);
                return null;
            }

            if (First)
            {
                NewLUT.Elements = NewMPE;
                First = false;
            }
            else
            {
                if (Anterior is not null)
                    Anterior.Next = NewMPE;
            }

            Anterior = NewMPE;
        }

        NewLUT.Eval16Fn = lut.Eval16Fn;
        NewLUT.EvalFloatFn = lut.EvalFloatFn;
        NewLUT.DupDataFn = lut.DupDataFn;
        NewLUT.FreeDataFn = lut.FreeDataFn;

        if (NewLUT.DupDataFn is not null)
            NewLUT.Data = NewLUT.DupDataFn!(lut.ContextID, lut.Data);

        NewLUT.SaveAs8Bits = lut.SaveAs8Bits;

        if (!BlessLUT(NewLUT))
        {
            //_cmsFree(lut.ContextID, NewLUT);
            return null;
        }

        return NewLUT;
    }

    public static bool cmsPipelineInsertStage(Pipeline? lut, StageLoc loc, [NotNullWhen(true)] Stage? mpe)
    {
        Stage? Anterior = null, pt;

        if (lut is null || mpe is null)
            return false;

        switch (loc)
        {
            case StageLoc.AtBegin:
                mpe.Next = lut.Elements;
                lut.Elements = mpe;
                break;

            case StageLoc.AtEnd:

                if (lut.Elements is null) { lut.Elements = mpe; }
                else
                {
                    for (pt = lut.Elements; pt is not null; pt = pt.Next)
                        Anterior = pt;

                    Anterior!.Next = mpe;
                    mpe.Next = null;
                }
                break;

            default:
                return false;
        }

        return BlessLUT(lut);
    }

    public static void cmsPipelineUnlinkStage(Pipeline? lut, StageLoc loc)
    {
        cmsPipelineUnlinkStage(lut, loc, out var mpe);
        cmsStageFree(mpe);
    }

    public static void cmsPipelineUnlinkStage(Pipeline? lut, StageLoc loc, out Stage? mpe)
    {
        Stage? Anterior, pt, Last;
        Stage? Unlinked = null;

        // If empty LUT, there is nothing to remove
        if (lut?.Elements is null)
        {
            mpe = null;
            return;
        }

        // On depending on the strategy...
        switch (loc)
        {
            case StageLoc.AtBegin:
                {
                    Stage? elem = lut.Elements;

                    lut.Elements = elem!.Next;
                    elem.Next = null;
                    Unlinked = elem;
                }
                break;

            case StageLoc.AtEnd:
                Anterior = Last = null;
                for (pt = lut.Elements;
                    pt is not null;
                    pt = pt.Next)
                {
                    Anterior = Last;
                    Last = pt;
                }

                Unlinked = Last;  // Next already points to null

                // Truncate the chain
                if (Anterior is not null)
                    Anterior.Next = null;
                else
                    lut.Elements = null;
                break;

            default:
                break;
        }

        mpe = Unlinked;

        // May fail, but we ignore it
        BlessLUT(lut);
    }

    public static bool cmsPipelineCat(Pipeline l1, Pipeline l2)
    {
        Stage? mpe;

        // If both LUTS does not have elements, we need to inherit
        // the number of channels
        if (l1.Elements is null && l2.Elements is null)
        {
            l1.InputChannels = l2.InputChannels;
            l1.OutputChannels = l2.OutputChannels;
        }

        // Cat second
        for (mpe = l2.Elements; mpe is not null; mpe = mpe.Next)
        {
            // We have to dup each element
            if (!cmsPipelineInsertStage(l1, StageLoc.AtEnd, cmsStageDup(mpe)))
                return false;
        }

        return BlessLUT(l1);
    }

    public static bool? cmsPipelineSetSaveAs8bitsFlag(Pipeline lut, bool On)
    {
        if (lut is null)
            return null;

        var Anterior = lut.SaveAs8Bits;

        lut.SaveAs8Bits = On;
        return Anterior;
    }

    public static Stage? cmsPipelineGetPtrToFirstStage(Pipeline? lut) =>
        lut?.Elements;

    public static Stage? cmsPipelineGetPtrToLastStage(Pipeline? lut)
    {
        Stage? Anterior = null;

        for (var mpe = lut?.Elements; mpe is not null; mpe = mpe.Next)
            Anterior = mpe;

        return Anterior;
    }

    public static uint cmsPipelineStageCount(Pipeline? lut)
    {
        Stage? mpe;
        uint n;

        for (n = 0, mpe = lut?.Elements; mpe is not null; mpe = mpe.Next)
            n++;
        return n;
    }

    // ----------------------------------------------------------- Reverse interpolation
    // Here's how it goes. The derivative Df(x) of the function f is the linear
    // transformation that best approximates f near the point x. It can be represented
    // by a matrix A whose entries are the partial derivatives of the components of f
    // with respect to all the coordinates. This is know as the Jacobian
    //
    // The best linear approximation to f is given by the matrix equation:
    //
    // y-y0 = A (x-x0)
    //
    // So, if x0 is a good "guess" for the zero of f, then solving for the zero of this
    // linear approximation will give a "better guess" for the zero of f. Thus let y=0,
    // and since y0=f(x0) one can solve the above equation for x. This leads to the
    // Newton's method formula:
    //
    // xn+1 = xn - A-1 f(xn)
    //
    // where xn+1 denotes the (n+1)-st guess, obtained from the n-th guess xn in the
    // fashion described above. Iterating this will give better and better approximations
    // if you have a "good enough" initial guess.

    private const float JACOBIAN_EPSILON = 0.001f;
    private const byte INVERSION_MAX_ITERATIONS = 30;

    // Increment with reflexion on boundary
    private static void IncDelta(ref float Val)
    {
        Val += Val < (1.0 - JACOBIAN_EPSILON)
            ? JACOBIAN_EPSILON
            : -JACOBIAN_EPSILON;
    }

    // Euclidean distance between two vectors of n elements each one
    private static float EuclideanDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int n)
    {
        var sum = 0f;
        int i;

        for (i = 0; i < n; i++)
        {
            var dif = b[i] - a[i];
            sum += dif * dif;
        }

        return MathF.Sqrt(sum);
    }

    // Evaluate a LUT in reverse direction. It only searches on 3->3 LUT. Uses Newton method    /* types/Pipeline.cs:Pipeline.BlessLUT */
    //
    // x1 <- x - [J(x)]^-1 * f(x)
    //
    // lut: The LUT on where to do the search
    // Target: LabK, 3 values of Lab plus destination K which is fixed
    // Result: The obtained CMYK
    // Hint:   Location where begin the search

    public static bool cmsPipelineEvalReverseFloat(
        ReadOnlySpan<float> Target,
        Span<float> Result,
        ReadOnlySpan<float> Hint,
        Pipeline lut)
    {
        int i, j;
        double error, LastError = 1E20;
        Span<float> fx = stackalloc float[4];
        Span<float> x = stackalloc float[4];
        Span<float> xd = stackalloc float[4];
        Span<float> fxd = stackalloc float[4];
        VEC3 tmp2;
        MAT3 Jacobian = new();

        // Only 3->3 and 4->3 are supported
        if (lut.InputChannels is not 3 and not 4) return false;
        if (lut.OutputChannels is not 3) return false;

        // Take the hint as starting point if specified
        if (Hint.IsEmpty)
        {
            // Begin at any point, we choose 1/3 of CMY axis
            x[0] = x[1] = x[2] = 0.3f;
        }
        else
        {
            // Only copy 3 channels from hint...
            for (j = 0; j < 3; j++)
                x[j] = Hint[j];
        }

        // If Lut is 4-dimensions, then grab target[3], which is fixed
        x[3] = (lut.InputChannels is 4)
            ? Target[3]
            : 0; // To keep lint happy

        // Iterate
        for (i = 0; i < INVERSION_MAX_ITERATIONS; i++)
        {
            // Get beginning fx
            cmsPipelineEvalFloat(x, fx, lut);

            // Compute error
            error = EuclideanDistance(fx, Target, 3);

            // If not convergent, return last safe value
            if (error >= LastError)
                break;

            // Keep latest values
            LastError = error;
            for (j = 0; j < lut.InputChannels; j++)
                Result[j] = x[j];

            // Found an exact match?
            if (error <= 0)
                break;

            // Obtain slope (the Jacobian)
            Slope(lut, fx, x, xd, fxd, 0, ref Jacobian.X.X, ref Jacobian.Y.X, ref Jacobian.Z.X);
            Slope(lut, fx, x, xd, fxd, 1, ref Jacobian.X.Y, ref Jacobian.Y.Y, ref Jacobian.Z.Y);
            Slope(lut, fx, x, xd, fxd, 2, ref Jacobian.X.Z, ref Jacobian.Y.Z, ref Jacobian.Z.Z);

            // Solve system
            tmp2.X = fx[0] - Target[0];
            tmp2.Y = fx[1] - Target[1];
            tmp2.Z = fx[2] - Target[2];

            var tmp = Jacobian.Solve(tmp2);
            if (tmp.IsNaN)
                return false;

            // Move our guess
            x[0] -= (float)tmp.X;
            x[1] -= (float)tmp.Y;
            x[2] -= (float)tmp.Z;

            // Some clipping....
            for (j = 0; j < 3; j++)
            {
                if (x[j] < 0) x[j] = 0;
                else
                    if (x[j] > 1.0) x[j] = 1.0f;
            }
        }

        return true;

        static void Slope(Pipeline lut, Span<float> fx, Span<float> x, Span<float> xd, Span<float> fxd, int j, ref double j0, ref double j1, ref double j2)
        {
            xd[0] = x[0];
            xd[1] = x[1];
            xd[2] = x[2];
            xd[3] = x[3];  // Keep fixed channel

            IncDelta(ref xd[j]);

            cmsPipelineEvalFloat(xd, fxd, lut);

            j0 = (fxd[0] - fx[0]) / JACOBIAN_EPSILON;
            j1 = (fxd[1] - fx[1]) / JACOBIAN_EPSILON;
            j2 = (fxd[2] - fx[2]) / JACOBIAN_EPSILON;
        }
    }
}
