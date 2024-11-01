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

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace lcms2.FastFloatPlugin;
public static partial class FastFloat
{
    private unsafe static void PerformanceEval16(Transform CMMcargo,
                                                 ReadOnlySpan<byte> Input,
                                                 Span<byte> Output,
                                                 uint PixelsPerLine,
                                                 uint LineCount,
                                                 Stride Stride)
    {
        fixed (byte* OutputPtr = Output)
        {
            if (CMMcargo.UserData is not Performance16Data p16)
                return;
            var p = p16.p;
            var TotalOut = p.nOutputs;
            var BaseTable = p.Table.Span;

            var @out = stackalloc byte*[cmsMAXCHANNELS];

            Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
            Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
            Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
            Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

            var inFormat = cmsGetTransformInputFormat(CMMcargo);
            var outFormat = cmsGetTransformOutputFormat(CMMcargo);

            _cmsComputeComponentIncrements(inFormat, Stride.BytesPerPlaneIn, out _, out var nalpha, SourceStartingOrder, SourceIncrements);
            _cmsComputeComponentIncrements(outFormat, Stride.BytesPerPlaneOut, out _, out nalpha, DestStartingOrder, DestIncrements);

            var in16 = T_BYTES(inFormat) is 2;
            var out16 = T_BYTES(outFormat) is 2;

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

                var TotalPlusAlpha = TotalOut;
                if (nalpha is not 0)
                    TotalPlusAlpha++;

                for (var OutChan = 0; OutChan < TotalPlusAlpha; OutChan++)
                {
                    @out[OutChan] = OutputPtr + DestStartingOrder[OutChan] + strideOut;
                }

                for (var ii = 0; ii < PixelsPerLine; ii++)
                {
                    var r = FROM_INPUT(Input[rin..]);
                    var g = FROM_INPUT(Input[gin..]);
                    var b = FROM_INPUT(Input[bin..]);

                    rin += (int)SourceIncrements[0];
                    gin += (int)SourceIncrements[1];
                    bin += (int)SourceIncrements[2];

                    var fx = _cmsToFixedDomain(r * (int)p.Domain[0]);
                    var fy = _cmsToFixedDomain(g * (int)p.Domain[1]);
                    var fz = _cmsToFixedDomain(b * (int)p.Domain[2]);

                    var x0 = FIXED_TO_INT(fx);
                    var y0 = FIXED_TO_INT(fy);
                    var z0 = FIXED_TO_INT(fz);

                    var rx = FIXED_REST_TO_INT(fx);
                    var ry = FIXED_REST_TO_INT(fy);
                    var rz = FIXED_REST_TO_INT(fz);

                    var X0 = (int)p.opta[2] * x0;
                    var X1 = r is (ushort)0xffffu ? 0 : (int)p.opta[2];

                    var Y0 = (int)p.opta[1] * y0;
                    var Y1 = g is (ushort)0xffffu ? 0 : (int)p.opta[1];

                    var Z0 = (int)p.opta[0] * z0;
                    var Z1 = b is (ushort)0xffffu ? 0 : (int)p.opta[0];

                    fixed (ushort* LutTablePtr = &BaseTable[X0 + Y0 + Z0])
                    {
                        var LutTable = LutTablePtr;

                        // Output should be computed as x = ROUND_FIXED_TO_INT(_cmsToFixedDomain(Rest))
                        // which expands as: x = (Rest + ((Rest+0x7fff)/0xFFFF) + 0x8000)>>16
                        // This can be replaced by: t = Rest+0x8001, x = (t + (t>>16))>>16
                        // at the cost of being off by one at 7fff and 17ffe.

                        if (rx >= ry)
                        {
                            if (ry >= rz)
                            {
                                Y1 += X1;
                                Z1 += Y1;
                                for (var OutChan = 0; OutChan < TotalOut; OutChan++)
                                {
                                    var c1 = (int)LutTable[X1];
                                    var c2 = (int)LutTable[Y1];
                                    var c3 = (int)LutTable[Z1];
                                    var c0 = (int)*LutTable++;
                                    c3 -= c2;
                                    c2 -= c1;
                                    c1 -= c0;
                                    var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
                                    var res16 = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));
                                    TO_OUTPUT(@out[OutChan], res16);
                                    @out[OutChan] += DestIncrements[OutChan];
                                }
                            }
                            else if (rz >= rx)
                            {
                                X1 += Z1;
                                Y1 += X1;
                                for (var OutChan = 0; OutChan < TotalOut; OutChan++)
                                {
                                    var c1 = (int)LutTable[X1];
                                    var c2 = (int)LutTable[Y1];
                                    var c3 = (int)LutTable[Z1];
                                    var c0 = (int)*LutTable++;
                                    c2 -= c1;
                                    c1 -= c3;
                                    c3 -= c0;
                                    var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
                                    var res16 = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));
                                    TO_OUTPUT(@out[OutChan], res16);
                                    @out[OutChan] += DestIncrements[OutChan];
                                }
                            }
                            else
                            {
                                Z1 += X1;
                                Y1 += Z1;
                                for (var OutChan = 0; OutChan < TotalOut; OutChan++)
                                {
                                    var c1 = (int)LutTable[X1];
                                    var c2 = (int)LutTable[Y1];
                                    var c3 = (int)LutTable[Z1];
                                    var c0 = (int)*LutTable++;
                                    c2 -= c3;
                                    c3 -= c1;
                                    c1 -= c0;
                                    var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
                                    var res16 = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));
                                    TO_OUTPUT(@out[OutChan], res16);
                                    @out[OutChan] += DestIncrements[OutChan];
                                }
                            }
                        }
                        else
                        {
                            if (rx >= rz)
                            {
                                X1 += Y1;
                                Z1 += X1;
                                for (var OutChan = 0; OutChan < TotalOut; OutChan++)
                                {
                                    var c1 = (int)LutTable[X1];
                                    var c2 = (int)LutTable[Y1];
                                    var c3 = (int)LutTable[Z1];
                                    var c0 = (int)*LutTable++;
                                    c3 -= c1;
                                    c1 -= c2;
                                    c2 -= c0;
                                    var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
                                    var res16 = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));
                                    TO_OUTPUT(@out[OutChan], res16);
                                    @out[OutChan] += DestIncrements[OutChan];
                                }
                            }
                            else if (ry >= rz)
                            {
                                Z1 += Y1;
                                X1 += Z1;
                                for (var OutChan = 0; OutChan < TotalOut; OutChan++)
                                {
                                    var c1 = (int)LutTable[X1];
                                    var c2 = (int)LutTable[Y1];
                                    var c3 = (int)LutTable[Z1];
                                    var c0 = (int)*LutTable++;
                                    c1 -= c3;
                                    c3 -= c2;
                                    c2 -= c0;
                                    var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
                                    var res16 = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));
                                    TO_OUTPUT(@out[OutChan], res16);
                                    @out[OutChan] += DestIncrements[OutChan];
                                }
                            }
                            else
                            {
                                Y1 += Z1;
                                X1 += Y1;
                                for (var OutChan = 0; OutChan < TotalOut; OutChan++)
                                {
                                    var c1 = (int)LutTable[X1];
                                    var c2 = (int)LutTable[Y1];
                                    var c3 = (int)LutTable[Z1];
                                    var c0 = (int)*LutTable++;
                                    c1 -= c2;
                                    c2 -= c3;
                                    c3 -= c0;
                                    var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
                                    var res16 = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));
                                    TO_OUTPUT(@out[OutChan], res16);
                                    @out[OutChan] += DestIncrements[OutChan];
                                }
                            }
                        }

                        if (nalpha is not 0)
                        {
                            var res16 = BitConverter.ToUInt16(Input[ain..]);
                            TO_OUTPUT(@out[TotalOut], res16);
                            ain += (int)SourceIncrements[3];
                            @out[TotalOut] += DestIncrements[(int)TotalOut];
                        }
                    }
                }

                strideIn += Stride.BytesPerLineIn;
                strideOut += Stride.BytesPerLineOut;

                [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
                ushort FROM_INPUT(ReadOnlySpan<byte> v)
                {
                    return in16 ? BitConverter.ToUInt16(v) : FROM_8_TO_16(v[0]);
                }

                [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
                void TO_OUTPUT(byte* d, ushort v)
                {
                    if (out16)
                    {
                        TO_OUTPUT_16(d, v);
                    }
                    else
                    {
                        TO_OUTPUT_8(d, v);
                    }
                }

                [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
                void TO_OUTPUT_16(byte* d, ushort v)
                {
                    *(ushort*)d = v;
                }

                [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
                void TO_OUTPUT_8(byte* d, ushort v)
                {
                    *d = FROM_16_TO_8(v);
                }
            }
        }
    }

    private static bool Optimize16BitRGBTransform(out Transform2Fn TransformFn,
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

        Span<float> In = stackalloc float[cmsMAXCHANNELS];
        Span<float> Out = stackalloc float[cmsMAXCHANNELS];

        // For empty transforms, do nothing
        if (Lut is null)
            return false;

        // This is a lossy optimization! does not apply in floating-point cases
        if (T_FLOAT(InputFormat) is not 0 || T_FLOAT(OutputFormat) is not 0)
            return false;

        // Only on 16 bit
        if (T_BYTES(InputFormat) is not 2 || T_BYTES(OutputFormat) is not 2)
            return false;

        // Only real 16 bit
        if (T_BIT15(InputFormat) is not 0 || T_BIT15(OutputFormat) is not 0)
            return false;

        // Swap endian is not supported
        if (T_ENDIAN16(InputFormat) is not 0 || T_ENDIAN16(OutputFormat) is not 0)
            return false;

        // Only on input RGB
        if (T_COLORSPACE(InputFormat) is not PT_RGB)
            return false;

        // If this is a matrix-shaper, the default already does a good job

        if (cmsPipelineCheckAndRetrieveStages(Lut,
            Signature.Stage.CurveSetElem, out _,
            Signature.Stage.MatrixElem, out _,
            Signature.Stage.MatrixElem, out _,
            Signature.Stage.CurveSetElem, out _))
        {
            return false;
        }

        if (cmsPipelineCheckAndRetrieveStages(Lut,
            Signature.Stage.CurveSetElem, out _,
            Signature.Stage.CurveSetElem, out _))
        {
            return false;
        }

        // Seems suitable, proceed

        var ContextID = cmsGetPipelineContextID(Lut);
        var newFlags = dwFlags | cmsFLAGS_FORCE_CLUT;

        if (!_cmsOptimizePipeline(ContextID, ref Lut, INTENT_PERCEPTUAL /* Don't care */, ref InputFormat, ref OutputFormat, ref newFlags))
            return false;

        var OptimizedCLUTmpe = cmsPipelineGetPtrToFirstStage(Lut);

        // Set the evaluator
        var data = (StageCLutData<ushort>)cmsStageData(OptimizedCLUTmpe!)!;

        var p16 = Performance16Data.Alloc(ContextID, data.Params);
        if (p16 is null) return false;

        TransformFn = PerformanceEval16;
        UserData = p16;
        FreeUserData = FreeDisposable;
        InputFormat |= 0x02000000;
        OutputFormat |= 0x02000000;
        dwFlags |= cmsFLAGS_CAN_CHANGE_FORMATTER;

        return true;
    }
}

file class Performance16Data(Context? context, InterpParams<ushort> p) : IDisposable
{
    public readonly Context? ContextID = context;
    public readonly InterpParams<ushort> p = p;     // Tetrahedrical interpolation parameters

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
    public static Performance16Data Alloc(Context? ContextID, InterpParams<ushort> p) =>
        new(ContextID, p);
}
