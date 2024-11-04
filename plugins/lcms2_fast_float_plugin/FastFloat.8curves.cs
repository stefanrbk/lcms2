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
    private static void FastEvaluateRGBCurves8(Transform CMMcargo,
                                               ReadOnlySpan<byte> Input,
                                               Span<byte> Output,
                                               uint PixelsPerLine,
                                               uint LineCount,
                                               Stride Stride)
    {
        if (CMMcargo.UserData is not Curves8Data Data)
            return;

        Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(
            cmsGetTransformInputFormat(CMMcargo),
            Stride.BytesPerPlaneIn,
            out _,
            out var nalpha,
            SourceStartingOrder,
            SourceIncrements);
        _cmsComputeComponentIncrements(
            cmsGetTransformOutputFormat(CMMcargo),
            Stride.BytesPerPlaneOut,
            out _,
            out nalpha,
            DestStartingOrder,
            DestIncrements);

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
                Output[rout] = Data.Curves(0, Input[rin]);
                Output[gout] = Data.Curves(1, Input[gin]);
                Output[bout] = Data.Curves(2, Input[bin]);

                // Handle alpha
                if (nalpha is not 0)
                    Output[aout] = Input[ain];

                rin += (int)SourceIncrements[0];
                gin += (int)SourceIncrements[1];
                bin += (int)SourceIncrements[2];
                if (nalpha is not 0)
                    ain += (int)SourceIncrements[3];

                rout += (int)DestIncrements[0];
                gout += (int)DestIncrements[1];
                bout += (int)DestIncrements[2];
                if (nalpha is not 0)
                    aout += (int)DestIncrements[3];
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static void FastRGBIdentity8(Transform CMMcargo,
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

        _cmsComputeComponentIncrements(
            cmsGetTransformInputFormat(CMMcargo),
            Stride.BytesPerPlaneIn,
            out _,
            out var nalpha,
            SourceStartingOrder,
            SourceIncrements);
        _cmsComputeComponentIncrements(
            cmsGetTransformOutputFormat(CMMcargo),
            Stride.BytesPerPlaneOut,
            out _,
            out nalpha,
            DestStartingOrder,
            DestIncrements);

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
                Output[rout] = Input[rin];
                Output[gout] = Input[gin];
                Output[bout] = Input[bin];

                // Handle alpha
                if (nalpha is not 0)
                    Output[aout] = Input[ain];

                rin += (int)SourceIncrements[0];
                gin += (int)SourceIncrements[1];
                bin += (int)SourceIncrements[2];
                if (nalpha is not 0)
                    ain += (int)SourceIncrements[3];

                rout += (int)DestIncrements[0];
                gout += (int)DestIncrements[1];
                bout += (int)DestIncrements[2];
                if (nalpha is not 0)
                    aout += (int)DestIncrements[3];
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static void FastEvaluateGrayCurves8(Transform CMMcargo,
                                                ReadOnlySpan<byte> Input,
                                                Span<byte> Output,
                                                uint PixelsPerLine,
                                                uint LineCount,
                                                Stride Stride)
    {
        if (CMMcargo.UserData is not Curves8Data Data)
            return;

        Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(
            cmsGetTransformInputFormat(CMMcargo),
            Stride.BytesPerPlaneIn,
            out _,
            out var nalpha,
            SourceStartingOrder,
            SourceIncrements);
        _cmsComputeComponentIncrements(
            cmsGetTransformOutputFormat(CMMcargo),
            Stride.BytesPerPlaneOut,
            out _,
            out nalpha,
            DestStartingOrder,
            DestIncrements);

        if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
            nalpha = 0;

        nuint strideIn = 0;
        nuint strideOut = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var gin = (int)(SourceStartingOrder[0] + strideIn);
            var ain =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideIn)
                    : default;

            var gout = (int)(DestStartingOrder[0] + strideOut);
            var aout =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideOut)
                    : default;

            for (var ii = 0; ii < PixelsPerLine; ii++)
            {
                Output[gout] = Data.Curves(0, Input[gin]);

                // Handle alpha
                if (nalpha is not 0)
                    Output[aout] = Input[ain];

                gin += (int)SourceIncrements[0];
                if (nalpha is not 0)
                    ain += (int)SourceIncrements[1];

                gout += (int)DestIncrements[0];
                if (nalpha is not 0)
                    aout += (int)DestIncrements[1];
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static void FastGrayIdentity8(Transform CMMcargo,
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

        _cmsComputeComponentIncrements(
            cmsGetTransformInputFormat(CMMcargo),
            Stride.BytesPerPlaneIn,
            out _,
            out var nalpha,
            SourceStartingOrder,
            SourceIncrements);
        _cmsComputeComponentIncrements(
            cmsGetTransformOutputFormat(CMMcargo),
            Stride.BytesPerPlaneOut,
            out _,
            out nalpha,
            DestStartingOrder,
            DestIncrements);

        if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
            nalpha = 0;

        nuint strideIn = 0;
        nuint strideOut = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var gin = (int)(SourceStartingOrder[0] + strideIn);
            var ain =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideIn)
                    : default;

            var gout = (int)(DestStartingOrder[0] + strideOut);
            var aout =
                nalpha is not 0
                    ? (int)(SourceStartingOrder[1] + strideOut)
                    : default;

            for (var ii = 0; ii < PixelsPerLine; ii++)
            {
                Output[gout] = Input[gin];

                // Handle alpha
                if (nalpha is not 0)
                    Output[aout] = Input[ain];

                gin += (int)SourceIncrements[0];
                if (nalpha is not 0)
                    ain += (int)SourceIncrements[1];

                gout += (int)DestIncrements[0];
                if (nalpha is not 0)
                    aout += (int)DestIncrements[1];
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static bool Optimize8ByJoiningCurves(out Transform2Fn TransformFn,
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

        // This is a lossy optimization! does not apply in floating-point cases
        if (T_FLOAT(InputFormat) is not 0 || T_FLOAT(OutputFormat) is not 0)
            return false;

        // Only on 8-bit
        if (T_BYTES(InputFormat) is not 1 || T_BYTES(OutputFormat) is not 1)
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
            if (cmsStageType(mpe) != Signatures.Stage.CurveSetElem)
                return false;
        }

        // Seems suitable, proceed

        var Data = Curves8Data.ComputeCompositeCurves((uint)nChans, Src);

        dwFlags |= cmsFLAGS_NOCACHE;
        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;
        UserData = Data;
        FreeUserData = FreeDisposable;

        // Maybe the curves are linear at the end
        TransformFn =
            nChans is 1
                ? Data.AllCurvesAreLinear
                      ? FastGrayIdentity8
                      : FastEvaluateGrayCurves8
                : Data.AllCurvesAreLinear
                    ? FastRGBIdentity8
                    : FastEvaluateRGBCurves8;

        return true;
    }
}

file class Curves8Data : IDisposable
{
    private bool disposedValue;
    public Context? ContextID;
    public int nCurves;
    private readonly byte[] _curves;

    public ref byte Curves(int a, int b) =>
        ref _curves[(a * 256) + b];

    public Curves8Data(Context? context)
    {
        ContextID = context;
        //_curves = Context.GetPool<byte>(context).Rent(cmsMAXCHANNELS * 256);
        //Array.Clear(_curves);
        _curves = new byte[cmsMAXCHANNELS * 256];
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            //if (disposing)
            //{
            //    Context.GetPool<byte>(ContextID).Return(_curves);
            //}

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public bool AllCurvesAreLinear
    {
        get
        {
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 256; j++)
                    if (Curves(i, j) != j)
                        return false;
            }

            return true;
        }
    }

    public static Curves8Data ComputeCompositeCurves(uint nChan, Pipeline Src)
    {
        Span<float> InFloat = stackalloc float[3];
        Span<float> OutFloat = stackalloc float[3];

        var Data = new Curves8Data(cmsGetPipelineContextID(Src));

        // Create target curves
        for (var i = 0; i < 256; i++)
        {
            for (var j = 0; j < nChan; j++)
                InFloat[j] = (float)(i / 255.0);

            cmsPipelineEvalFloat(InFloat, OutFloat, Src);

            for (var j = 0; j < nChan; j++)
                Data.Curves(j, i) = FROM_16_TO_8(_cmsSaturateWord(OutFloat[j] * 65535.0));
        }

        return Data;
    }
}
