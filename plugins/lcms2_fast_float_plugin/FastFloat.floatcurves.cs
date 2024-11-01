//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright ©️ 1998-2024 Marti Maria Saguer, all rights reserved
//              2022-2024 Stefan Kewatt, all rights reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//---------------------------------------------------------------------------------

using lcms2.state;
using lcms2.types;

namespace lcms2.FastFloatPlugin;
public static partial class FastFloat
{
    private static void FastEvaluateFloatRGBCurves(Transform CMMcargo,
                                                   ReadOnlySpan<byte> Input,
                                                   Span<byte> Output,
                                                   uint PixelsPerLine,
                                                   uint LineCount,
                                                   Stride Stride)
    {
        if (CMMcargo.UserData is not CurvesFloatData Data)
            return;

        Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(cmsGetTransformInputFormat(CMMcargo), Stride.BytesPerPlaneIn, out _, out var nalpha, SourceStartingOrder, SourceIncrements);
        _cmsComputeComponentIncrements(cmsGetTransformOutputFormat(CMMcargo), Stride.BytesPerPlaneOut, out _, out nalpha, DestStartingOrder, DestIncrements);

        if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
            nalpha = 0;

        nuint strideIn = 0;
        nuint strideOut = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var rin = (int)(SourceStartingOrder[0] + strideIn);
            var gin = (int)(SourceStartingOrder[1] + strideIn);
            var bin = (int)(SourceStartingOrder[2] + strideIn);
            var ain =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[3] + strideIn)
                    : default;

            var rout = (int)(DestStartingOrder[0] + strideOut);
            var gout = (int)(DestStartingOrder[1] + strideOut);
            var bout = (int)(DestStartingOrder[2] + strideOut);
            var aout =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[3] + strideOut)
                    : default;

            for (var ii = 0; ii < PixelsPerLine; ii++)
            {
                BitConverter.TryWriteBytes(Output[rout..], flerp(Data.CurveR, BitConverter.ToSingle(Input[rin..])));
                BitConverter.TryWriteBytes(Output[gout..], flerp(Data.CurveR, BitConverter.ToSingle(Input[gin..])));
                BitConverter.TryWriteBytes(Output[bout..], flerp(Data.CurveR, BitConverter.ToSingle(Input[bin..])));

                rin += (int)SourceIncrements[0];
                gin += (int)SourceIncrements[1];
                bin += (int)SourceIncrements[2];

                rout += (int)DestIncrements[0];
                gout += (int)DestIncrements[1];
                bout += (int)DestIncrements[2];

                // Handle alpha
                if (nalpha is not 0)
                {
                    BitConverter.TryWriteBytes(Output[aout..], BitConverter.ToSingle(Input[ain..]));
                    ain += (int)SourceIncrements[3];
                    aout += (int)DestIncrements[3];
                }
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static void FastFloatRGBIdentity(Transform CMMcargo,
                                             ReadOnlySpan<byte> Input,
                                             Span<byte> Output,
                                             uint PixelsPerLine,
                                             uint LineCount,
                                             Stride Stride)
    {
        Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(cmsGetTransformInputFormat(CMMcargo), Stride.BytesPerPlaneIn, out _, out var nalpha, SourceStartingOrder, SourceIncrements);
        _cmsComputeComponentIncrements(cmsGetTransformOutputFormat(CMMcargo), Stride.BytesPerPlaneOut, out _, out nalpha, DestStartingOrder, DestIncrements);

        if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
            nalpha = 0;

        nuint strideIn = 0;
        nuint strideOut = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var rin = (int)(SourceStartingOrder[0] + strideIn);
            var gin = (int)(SourceStartingOrder[1] + strideIn);
            var bin = (int)(SourceStartingOrder[2] + strideIn);
            var ain =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[3] + strideIn)
                    : default;

            var rout = (int)(DestStartingOrder[0] + strideOut);
            var gout = (int)(DestStartingOrder[1] + strideOut);
            var bout = (int)(DestStartingOrder[2] + strideOut);
            var aout =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[3] + strideOut)
                    : default;

            for (var ii = 0; ii < PixelsPerLine; ii++)
            {
                BitConverter.TryWriteBytes(Output[rout..], BitConverter.ToSingle(Input[rin..]));
                BitConverter.TryWriteBytes(Output[gout..], BitConverter.ToSingle(Input[gin..]));
                BitConverter.TryWriteBytes(Output[bout..], BitConverter.ToSingle(Input[bin..]));

                rin += (int)SourceIncrements[0];
                gin += (int)SourceIncrements[1];
                bin += (int)SourceIncrements[2];

                rout += (int)DestIncrements[0];
                gout += (int)DestIncrements[1];
                bout += (int)DestIncrements[2];

                // Handle alpha
                if (nalpha is not 0)
                {
                    BitConverter.TryWriteBytes(Output[aout..], BitConverter.ToSingle(Input[ain..]));
                    ain += (int)SourceIncrements[3];
                    aout += (int)DestIncrements[3];
                }
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static void FastEvaluateFloatGrayCurves(Transform CMMcargo,
                                                    ReadOnlySpan<byte> Input,
                                                    Span<byte> Output,
                                                    uint PixelsPerLine,
                                                    uint LineCount,
                                                    Stride Stride)
    {
        if (CMMcargo.UserData is not CurvesFloatData Data)
            return;

        Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(cmsGetTransformInputFormat(CMMcargo), Stride.BytesPerPlaneIn, out _, out var nalpha, SourceStartingOrder, SourceIncrements);
        _cmsComputeComponentIncrements(cmsGetTransformOutputFormat(CMMcargo), Stride.BytesPerPlaneOut, out _, out nalpha, DestStartingOrder, DestIncrements);

        if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
            nalpha = 0;

        nuint strideIn = 0;
        nuint strideOut = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var kin = (int)(SourceStartingOrder[0] + strideIn);
            var kout = (int)(DestStartingOrder[0] + strideOut);

            var ain =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideIn)
                    : default;

            var aout =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideOut)
                    : default;

            for (var ii = 0; ii < PixelsPerLine; ii++)
            {
                BitConverter.TryWriteBytes(Output[kout..], flerp(Data.CurveR, BitConverter.ToSingle(Input[kin..])));

                kin += (int)SourceIncrements[0];
                kout += (int)DestIncrements[0];

                // Handle alpha
                if (nalpha is not 0)
                {
                    BitConverter.TryWriteBytes(Output[aout..], BitConverter.ToSingle(Input[ain..]));
                    ain += (int)SourceIncrements[1];
                    aout += (int)DestIncrements[1];
                }
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static void FastFloatGrayIdentity(Transform CMMcargo,
                                              ReadOnlySpan<byte> Input,
                                              Span<byte> Output,
                                              uint PixelsPerLine,
                                              uint LineCount,
                                              Stride Stride)
    {
        Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(cmsGetTransformInputFormat(CMMcargo), Stride.BytesPerPlaneIn, out _, out var nalpha, SourceStartingOrder, SourceIncrements);
        _cmsComputeComponentIncrements(cmsGetTransformOutputFormat(CMMcargo), Stride.BytesPerPlaneOut, out _, out nalpha, DestStartingOrder, DestIncrements);

        if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
            nalpha = 0;

        nuint strideIn = 0;
        nuint strideOut = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var kin = (int)(SourceStartingOrder[0] + strideIn);
            var kout = (int)(DestStartingOrder[0] + strideOut);

            var ain =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideIn)
                    : default;

            var aout =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideOut)
                    : default;

            for (var ii = 0; ii < PixelsPerLine; ii++)
            {
                BitConverter.TryWriteBytes(Output[kout..], BitConverter.ToSingle(Input[kin..]));

                kin += (int)SourceIncrements[0];
                kout += (int)DestIncrements[0];

                // Handle alpha
                if (nalpha is not 0)
                {
                    BitConverter.TryWriteBytes(Output[aout..], BitConverter.ToSingle(Input[ain..]));
                    ain += (int)SourceIncrements[1];
                    aout += (int)DestIncrements[1];
                }
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static bool OptimizeFloatByJoiningCurves(out Transform2Fn TransformFn,
                                                     out object? UserData,
                                                     out FreeUserDataFn? FreeUserData,
                                                     ref Pipeline Lut,
                                                     ref uint InputFormat,
                                                     ref uint OutputFormat,
                                                     ref uint dwFlags)
    {
        FreeUserData = null;
        UserData = null;
        TransformFn = null!;

        var Src = Lut;

        // Apply only to floating-point cases
        if (T_FLOAT(InputFormat) is 0 || T_FLOAT(OutputFormat) is 0)
            return false;

        // Only on 8-bit
        if (T_BYTES(InputFormat) is not 4 || T_BYTES(OutputFormat) is not 4)
            return false;

        // Curves need same channels on input and output (despite extra channels may differ)
        var nChans = T_CHANNELS(InputFormat);
        if (nChans != T_CHANNELS(OutputFormat))
            return false;

        // gray and RGB
        if (nChans is not 1 and not 3)
            return false;

        // Only curves in this LUT?
        for (var mpe = cmsPipelineGetPtrToFirstStage(Src); mpe is not null; mpe = cmsStageNext(mpe))
        {
            if (cmsStageType(mpe) != Signature.Stage.CurveSetElem)
                return false;
        }

        // Seems suitable, proceed

        var Data = CurvesFloatData.ComputeCompositeFloatCurves((uint)nChans, Src);

        dwFlags |= cmsFLAGS_NOCACHE;
        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;
        UserData = Data;
        FreeUserData = FreeDisposable;

        // Maybe the curves are linear at the end
        TransformFn =
            nChans is 1
                ? Data.KCurveIsLinear
                    ? FastFloatGrayIdentity
                    : FastEvaluateFloatGrayCurves
                : Data.AllRGBCurvesAreLinear
                    ? FastFloatRGBIdentity
                    : FastEvaluateFloatRGBCurves;

        return true;
    }
}

file class CurvesFloatData : IDisposable
{
    private bool disposedValue;
    public Context? ContextID;
    private readonly float[] _curveR;
    private readonly float[] _curveG;
    private readonly float[] _curveB;

    public Span<float> CurveR => _curveR.AsSpan(..MAX_NODES_IN_CURVE);
    public Span<float> CurveG => _curveG.AsSpan(..MAX_NODES_IN_CURVE);
    public Span<float> CurveB => _curveB.AsSpan(..MAX_NODES_IN_CURVE);

    public CurvesFloatData(Context? context)
    {
        ContextID = context;
        //var pool = Context.GetPool<float>(context);
        //_curveR = pool.Rent(MAX_NODES_IN_CURVE);
        //_curveG = pool.Rent(MAX_NODES_IN_CURVE);
        //_curveB = pool.Rent(MAX_NODES_IN_CURVE);
        //Array.Clear(_curveR);
        //Array.Clear(_curveG);
        //Array.Clear(_curveB);
        _curveR = new float[MAX_NODES_IN_CURVE];
        _curveG = new float[MAX_NODES_IN_CURVE];
        _curveB = new float[MAX_NODES_IN_CURVE];

    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            //if (disposing)
            //{
            //    var pool = Context.GetPool<float>(ContextID);
            //    pool.Return(_curveR);
            //    pool.Return(_curveG);
            //    pool.Return(_curveB);
            //}

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private const double LINEAR_CURVES_EPSILON = 1e-5;

    public bool AllRGBCurvesAreLinear
    {
        get
        {
            for (var j = 0; j < MAX_NODES_IN_CURVE; j++)
            {
                var expected = (float)j / (MAX_NODES_IN_CURVE - 1);

                if (Math.Abs(CurveR[j] - expected) > LINEAR_CURVES_EPSILON ||
                    Math.Abs(CurveG[j] - expected) > LINEAR_CURVES_EPSILON ||
                    Math.Abs(CurveB[j] - expected) > LINEAR_CURVES_EPSILON)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool KCurveIsLinear
    {
        get
        {
            for (var j = 0; j < MAX_NODES_IN_CURVE; j++)
            {
                var expected = (float)j / (MAX_NODES_IN_CURVE - 1);

                if (Math.Abs(CurveR[j] - expected) > LINEAR_CURVES_EPSILON)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public static CurvesFloatData ComputeCompositeFloatCurves(uint nChan, Pipeline Src)
    {
        Span<float> InFloat = stackalloc float[3];
        Span<float> OutFloat = stackalloc float[3];

        var Data = new CurvesFloatData(cmsGetPipelineContextID(Src));

        // Create target curves
        for (var i = 0; i < MAX_NODES_IN_CURVE; i++)
        {
            for (var j = 0; j < nChan; j++)
                InFloat[j] = (float)i / (MAX_NODES_IN_CURVE - 1);

            cmsPipelineEvalFloat(InFloat, OutFloat, Src);

            if (nChan is 1)
            {
                Data.CurveR[i] = OutFloat[0];
            }
            else
            {
                Data.CurveR[i] = OutFloat[0];
                Data.CurveG[i] = OutFloat[1];
                Data.CurveB[i] = OutFloat[2];
            }
        }

        return Data;
    }
}
