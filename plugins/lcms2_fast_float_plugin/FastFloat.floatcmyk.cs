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

using System.Diagnostics;
using System.Runtime.CompilerServices;

using lcms2.types;

namespace lcms2.FastFloatPlugin;

public static partial class FastFloat
{
    private static bool XFormSamplerFloatCMYK(ReadOnlySpan<float> In, Span<float> Out, object? Cargo)
    {
        if (Cargo is not Pipeline c)
            return false;

        cmsPipelineEvalFloat(In, Out, c);

        return true;
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float LinearInterpInt(float a, float l, float h) =>
        ((h - l) * a) + l;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float fclamp100(float v) =>
        ((v < 1.0e-9f) || float.IsNaN(v))
            ? 0f
            : v > 100f
                ? 100f
                : v;

    private unsafe static void FloatCMYKCLUTEval(Transform CMMcargo,
                                                 ReadOnlySpan<byte> Input,
                                                 Span<byte> Output,
                                                 uint PixelsPerLine,
                                                 uint LineCount,
                                                 Stride Stride)
    {
        fixed (byte* OutputPtr = Output)
        {
            if (CMMcargo.UserData is not FloatCMYKData pcmyk)
                return;
            var p = pcmyk.p;
            var TotalOut = p.nOutputs;

            var @out = stackalloc byte*[Context.MaxChannels];

            Span<uint> SourceStartingOrder = stackalloc uint[Context.MaxChannels];
            Span<uint> SourceIncrements = stackalloc uint[Context.MaxChannels];
            Span<uint> DestStartingOrder = stackalloc uint[Context.MaxChannels];
            Span<uint> DestIncrements = stackalloc uint[Context.MaxChannels];

            Span<float> Tmp1 = stackalloc float[Context.MaxChannels];
            Span<float> Tmp2 = stackalloc float[Context.MaxChannels];

            var InputFormat = cmsGetTransformInputFormat(CMMcargo);
            var OutputFormat = cmsGetTransformOutputFormat(CMMcargo);

            _cmsComputeComponentIncrements(
                InputFormat,
                Stride.BytesPerPlaneIn,
                out _,
                out var nalpha,
                SourceStartingOrder,
                SourceIncrements);
            _cmsComputeComponentIncrements(
                OutputFormat,
                Stride.BytesPerPlaneOut,
                out _,
                out nalpha,
                DestStartingOrder,
                DestIncrements);

            if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
                nalpha = 0;

            nuint strideIn = 0;
            nuint strideOut = 0;
            fixed (float* LutTablePtr = p.Table.Span)
            {
                var LutTable = LutTablePtr;

                for (var i = 0; i < LineCount; i++)
                {
                    var cin = (int)(SourceStartingOrder[0] + strideIn);
                    var min = (int)(SourceStartingOrder[1] + strideIn);
                    var yin = (int)(SourceStartingOrder[2] + strideIn);
                    var kin = (int)(SourceStartingOrder[3] + strideIn);
                    var ain =
                        nalpha is not 0
                            ? (int)(SourceStartingOrder[4] + strideIn)
                            : default;

                    var TotalPlusAlpha = TotalOut;
                    if (nalpha is not 0)
                        TotalPlusAlpha++;

                    for (var OutChan = 0; OutChan < TotalPlusAlpha; OutChan++)
                    {
                        @out[OutChan] = OutputPtr + DestStartingOrder[OutChan] + strideOut;
                    }

                    for (var ii = 0; ii < PixelsPerLine; ii++)
                    {
                        var c = fclamp100(BitConverter.ToSingle(Input[cin..])) / 100f;
                        var m = fclamp100(BitConverter.ToSingle(Input[min..])) / 100f;
                        var y = fclamp100(BitConverter.ToSingle(Input[yin..])) / 100f;
                        var k = fclamp100(BitConverter.ToSingle(Input[kin..])) / 100f;

                        cin += (int)SourceIncrements[0];
                        min += (int)SourceIncrements[1];
                        yin += (int)SourceIncrements[2];
                        kin += (int)SourceIncrements[3];

                        var pk = c * p.Domain[0];
                        var px = m * p.Domain[1];
                        var py = y * p.Domain[2];
                        var pz = k * p.Domain[3];

                        var k0 = Conversions.QuickFloor(pk);
                        var rk = pk - k0;
                        var x0 = Conversions.QuickFloor(px);
                        var rx = px - x0;
                        var y0 = Conversions.QuickFloor(py);
                        var ry = py - y0;
                        var z0 = Conversions.QuickFloor(pz);
                        var rz = pz - z0;

                        var K0 = (int)p.opta[3] * k0;
                        var K1 = K0 + ((c >= 1.0) ? 0 : (int)p.opta[3]);

                        var X0 = (int)p.opta[2] * x0;
                        var X1 = X0 + ((m >= 1.0) ? 0 : (int)p.opta[2]);

                        var Y0 = (int)p.opta[1] * y0;
                        var Y1 = Y0 + ((y >= 1.0) ? 0 : (int)p.opta[1]);

                        var Z0 = (int)p.opta[0] * z0;
                        var Z1 = Z0 + ((k >= 1.0) ? 0 : (int)p.opta[0]);

                        int OutChan;

                        for (OutChan = 0; OutChan < TotalOut; OutChan++)
                        {
                            var c0 = DENS(X0, Y0, Z0);
                            float c1, c2, c3;

                            if (rx >= ry && ry >= rz)
                            {
                                c1 = DENS(X1, Y0, Z0) - c0;
                                c2 = DENS(X1, Y1, Z0) - DENS(X1, Y0, Z0);
                                c3 = DENS(X1, Y1, Z1) - DENS(X1, Y1, Z0);
                            }
                            else if (rx >= rz && rz >= ry)
                            {
                                c1 = DENS(X1, Y0, Z0) - c0;
                                c2 = DENS(X1, Y1, Z1) - DENS(X1, Y0, Z1);
                                c3 = DENS(X1, Y0, Z1) - DENS(X1, Y0, Z0);
                            }
                            else if (rz >= rx && rx >= ry)
                            {
                                c1 = DENS(X1, Y0, Z1) - DENS(X0, Y0, Z1);
                                c2 = DENS(X1, Y1, Z1) - DENS(X1, Y0, Z1);
                                c3 = DENS(X0, Y0, Z1) - c0;
                            }
                            else if (ry >= rx && rx >= rz)
                            {
                                c1 = DENS(X1, Y1, Z0) - DENS(X0, Y1, Z0);
                                c2 = DENS(X0, Y1, Z0) - c0;
                                c3 = DENS(X1, Y1, Z1) - DENS(X1, Y1, Z0);
                            }
                            else if (ry >= rz && rz >= rx)
                            {
                                c1 = DENS(X1, Y1, Z1) - DENS(X0, Y1, Z1);
                                c2 = DENS(X0, Y1, Z0) - c0;
                                c3 = DENS(X0, Y1, Z1) - DENS(X0, Y1, Z0);
                            }
                            else if (rz >= ry && ry >= rx)
                            {
                                c1 = DENS(X1, Y1, Z1) - DENS(X0, Y1, Z1);
                                c2 = DENS(X0, Y1, Z1) - DENS(X0, Y0, Z1);
                                c3 = DENS(X0, Y0, Z1) - c0;
                            }
                            else
                            {
                                c1 = c2 = c3 = 0;
                            }

                            Tmp1[OutChan] = c0 + (c1 * rx) + (c2 * ry) + (c3 * rz);
                        }

                        LutTable = LutTablePtr + K1;

                        for (OutChan = 0; OutChan < TotalOut; OutChan++)
                        {
                            var c0 = DENS(X0, Y0, Z0);
                            float c1, c2, c3;

                            if (rx >= ry && ry >= rz)
                            {
                                c1 = DENS(X1, Y0, Z0) - c0;
                                c2 = DENS(X1, Y1, Z0) - DENS(X1, Y0, Z0);
                                c3 = DENS(X1, Y1, Z1) - DENS(X1, Y1, Z0);
                            }
                            else if (rx >= rz && rz >= ry)
                            {
                                c1 = DENS(X1, Y0, Z0) - c0;
                                c2 = DENS(X1, Y1, Z1) - DENS(X1, Y0, Z1);
                                c3 = DENS(X1, Y0, Z1) - DENS(X1, Y0, Z0);
                            }
                            else if (rz >= rx && rx >= ry)
                            {
                                c1 = DENS(X1, Y0, Z1) - DENS(X0, Y0, Z1);
                                c2 = DENS(X1, Y1, Z1) - DENS(X1, Y0, Z1);
                                c3 = DENS(X0, Y0, Z1) - c0;
                            }
                            else if (ry >= rx && rx >= rz)
                            {
                                c1 = DENS(X1, Y1, Z0) - DENS(X0, Y1, Z0);
                                c2 = DENS(X0, Y1, Z0) - c0;
                                c3 = DENS(X1, Y1, Z1) - DENS(X1, Y1, Z0);
                            }
                            else if (ry >= rz && rz >= rx)
                            {
                                c1 = DENS(X1, Y1, Z1) - DENS(X0, Y1, Z1);
                                c2 = DENS(X0, Y1, Z0) - c0;
                                c3 = DENS(X0, Y1, Z1) - DENS(X0, Y1, Z0);
                            }
                            else if (rz >= ry && ry >= rx)
                            {
                                c1 = DENS(X1, Y1, Z1) - DENS(X0, Y1, Z1);
                                c2 = DENS(X0, Y1, Z1) - DENS(X0, Y0, Z1);
                                c3 = DENS(X0, Y0, Z1) - c0;
                            }
                            else
                            {
                                c1 = c2 = c3 = 0;
                            }

                            Tmp2[OutChan] = c0 + (c1 * rx) + (c2 * ry) + (c3 * rz);
                        }

                        for (OutChan = 0; OutChan < p.nOutputs; OutChan++)
                        {
                            *(float*)@out[OutChan] = LinearInterpInt(rk, Tmp1[OutChan], Tmp2[OutChan]);
                            @out[OutChan] += DestIncrements[OutChan];
                        }

                        if (nalpha is not 0)
                        {
                            *(float*)@out[TotalOut] = BitConverter.ToSingle(Input[ain..]);
                            ain += (int)SourceIncrements[4];
                            @out[TotalOut] += DestIncrements[(int)TotalOut];
                        }

                        float DENS(int i, int j, int k)
                        {
                            return LutTable[i + j + k + OutChan];
                        }
                    }
                }

                strideIn += Stride.BytesPerLineIn;
                strideOut += Stride.BytesPerLineOut;
            }
        }
    }

    private static bool OptimizeCLUTCMYKTransform(out Transform2Fn TransformFn,
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

        Pipeline? OptimizedLUT = null;

        // For empty transforms, do nothing
        if (Lut is null)
            return false;

        // This is a lossy optimization! does not apply in floating-point cases
        if (T_FLOAT(InputFormat) is 0 || T_FLOAT(OutputFormat) is 0)
            return false;

        // Only on 8-bit
        if (T_BYTES(InputFormat) is not 4 || T_BYTES(OutputFormat) is not 4)
            return false;

        // Only on CMYK
        if (T_COLORSPACE(InputFormat) is not PT_CMYK)
            return false;

        // Seems suitable, proceed

        var OriginalLut = Lut;

        var ContextID = cmsGetPipelineContextID(OriginalLut);
        var nGridPoints = _cmsReasonableGridpointsByColorspace(Signatures.Colorspace.Rgb, dwFlags);

        // Create the result LUT
        OptimizedLUT = cmsPipelineAlloc(ContextID, 4, cmsPipelineOutputChannels(OriginalLut));
        if (OptimizedLUT is null)
            goto Error;

        // Allocate the CLUT for result
        var OptimizedCLUTmpe = cmsStageAllocCLutFloat(
            ContextID,
            nGridPoints,
            4,
            cmsPipelineOutputChannels(OriginalLut),
            null);

        // Add the CLUT to the destination LUT
        cmsPipelineInsertStage(OptimizedLUT, StageLoc.AtBegin, OptimizedCLUTmpe);

        // Resample the LUT
        if (!cmsStageSampleCLutFloat(OptimizedCLUTmpe, XFormSamplerFloatCMYK, OriginalLut, SamplerFlag.None))
            goto Error;

        // Set the evaluator
        var data = (StageCLutData<float>)cmsStageData(OptimizedCLUTmpe!)!;

        var pcmyk = FloatCMYKData.Alloc(ContextID, data.Params);
        if (pcmyk is null)
            goto Error;

        // And return the obtained LUT
        cmsPipelineFree(OriginalLut);

        Lut = OptimizedLUT;
        TransformFn = FloatCMYKCLUTEval;
        UserData = pcmyk;
        FreeUserData = FreeDisposable;
        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;

        return true;

    Error:
        if (OptimizedLUT is not null)
            cmsPipelineFree(OptimizedLUT);

        return false;
    }
}

file class FloatCMYKData(Context? context, InterpParams<float> p) : IDisposable
{
    public readonly Context? ContextID = context;
    public readonly InterpParams<float> p = p;     // Tetrahedrical interpolation parameters

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) { }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static FloatCMYKData Alloc(Context? ContextID, InterpParams<float> p) =>
        new(ContextID, p);
}
