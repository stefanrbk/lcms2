﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright (c) 1998-2023 Marti Maria Saguer, all rights reserved
//                2022-2023 Stefan Kewatt, all rights reserved
//
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
    private static bool XFormSamplerFloat(ReadOnlySpan<float> In, Span<float> Out, object? Cargo)
    {
        if (Cargo is not Pipeline c)
            return false;

        cmsPipelineEvalFloat(In, Out, c);

        return true;
    }

    private unsafe static void FloatCLUTEval(Transform CMMcargo,
                                             ReadOnlySpan<byte> Input,
                                             Span<byte> Output,
                                             uint PixelsPerLine,
                                             uint LineCount,
                                             Stride Stride)
    {
        fixed (byte* OutputPtr = Output)
        {
            if (_cmsGetTransformUserData(CMMcargo) is not FloatCLUTData pfloat)
                return;
            var p = pfloat.p;
            var TotalOut = p.nOutputs;

            var @out = stackalloc byte*[cmsMAXCHANNELS];

            Span<uint> SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
            Span<uint> SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
            Span<uint> DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
            Span<uint> DestIncrements = stackalloc uint[cmsMAXCHANNELS];

            _cmsComputeComponentIncrements(cmsGetTransformInputFormat(CMMcargo), Stride.BytesPerPlaneIn, out _, out var nalpha, SourceStartingOrder, SourceIncrements);
            _cmsComputeComponentIncrements(cmsGetTransformOutputFormat(CMMcargo), Stride.BytesPerPlaneOut, out _, out nalpha, DestStartingOrder, DestIncrements);

            if ((_cmsGetTransformFlags(CMMcargo) & cmsFLAGS_COPY_ALPHA) is 0)
                nalpha = 0;

            var strideIn = 0u;
            var strideOut = 0u;

            fixed (float* LutTablePtr = p.Table.Span)
            {
                var LutTable = LutTablePtr;

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
                    if (nalpha is not 0) TotalPlusAlpha++;

                    for (var OutChan = 0; OutChan < TotalPlusAlpha; OutChan++)
                    {
                        @out[OutChan] = OutputPtr + DestStartingOrder[OutChan] + strideOut;
                    }

                    for (var ii = 0; ii < PixelsPerLine; ii++)
                    {
                        var r = fclamp(BitConverter.ToSingle(Input[rin..]));
                        var g = fclamp(BitConverter.ToSingle(Input[gin..]));
                        var b = fclamp(BitConverter.ToSingle(Input[bin..]));

                        rin += (int)SourceIncrements[0];
                        gin += (int)SourceIncrements[1];
                        bin += (int)SourceIncrements[2];

                        var px = r * p.Domain[0];
                        var py = g * p.Domain[1];
                        var pz = b * p.Domain[2];

                        var x0 = _cmsQuickFloor(px); var rx = px - x0;
                        var y0 = _cmsQuickFloor(py); var ry = py - y0;
                        var z0 = _cmsQuickFloor(pz); var rz = pz - z0;

                        var X0 = (int)p.opta[2] * x0;
                        var X1 = X0 + ((rx >= 1.0) ? 0 : (int)p.opta[2]);

                        var Y0 = (int)p.opta[1] * y0;
                        var Y1 = Y0 + ((ry >= 1.0) ? 0 : (int)p.opta[1]);

                        var Z0 = (int)p.opta[0] * z0;
                        var Z1 = Z0 + ((rz >= 1.0) ? 0 : (int)p.opta[0]);

                        // These are the 6 Tetrahedral
                        for (var OutChan = 0; OutChan < TotalOut; OutChan++)
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

                            *(float*)@out[OutChan] = c0 + (c1 * rx) + (c2 * ry) + (c3 * rz);
                            @out[OutChan] += DestIncrements[OutChan];

                            float DENS(int i, int j, int k)
                            {
                                return LutTable[i + j + k + OutChan];
                            }
                        }

                        if (nalpha is not 0)
                        {
                            *(float*)@out[TotalOut] = BitConverter.ToSingle(Input[ain..]);
                            ain += (int)SourceIncrements[3];
                            @out[TotalOut] += DestIncrements[(int)TotalOut];
                        }
                    }
                }

                strideIn += Stride.BytesPerLineIn;
                strideOut += Stride.BytesPerLineOut;
            }
        }
    }

    private static bool OptimizeCLUTRGBTransform(out Transform2Fn TransformFn,
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

        // Check for floating point only
        if (T_FLOAT(InputFormat) is 0 || T_FLOAT(OutputFormat) is 0)
            return false;

        // Only on floats
        if (T_BYTES(InputFormat) is not sizeof(float) || T_BYTES(OutputFormat) is not sizeof(float))
            return false;

        // Input has to be RGB, Output may be any
        if (T_COLORSPACE(InputFormat) is not PT_RGB)
            return false;

        // Seems suitable, proceed

        var OriginalLut = Lut;

        var ContextID = cmsGetPipelineContextID(OriginalLut);
        var nGridPoints = _cmsReasonableGridpointsByColorspace(cmsSigRgbData, dwFlags);

        // Create the result LUT
        OptimizedLUT = cmsPipelineAlloc(ContextID, 3, cmsPipelineOutputChannels(OriginalLut));
        if (OptimizedLUT is null)
            goto Error;

        // Allocate the CLUT for result
        var OptimizedCLUTmpe = cmsStageAllocCLutFloat(ContextID, nGridPoints, 3, cmsPipelineOutputChannels(OriginalLut), null);

        // Add the CLUT to the destination LUT
        cmsPipelineInsertStage(OptimizedLUT, StageLoc.AtBegin, OptimizedCLUTmpe);

        // Resample the LUT
        if (!cmsStageSampleCLutFloat(OptimizedCLUTmpe, XFormSamplerFloat, OriginalLut, SamplerFlag.None))
            goto Error;

        // Set the evaluator
        var data = (StageCLutData<float>)cmsStageData(OptimizedCLUTmpe!)!;

        var pfloat = FloatCLUTData.Alloc(ContextID, data.Params);
        if (pfloat is null)
            goto Error;

        // And return the obtained LUT
        cmsPipelineFree(OriginalLut);

        Lut = OptimizedLUT;
        TransformFn = FloatCLUTEval;
        UserData = pfloat;
        FreeUserData = FreeDisposable;
        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;

        return true;

    Error:
        if (OptimizedLUT is not null)
            cmsPipelineFree(OptimizedLUT);

        return false;
    }
}

file class FloatCLUTData(Context? context, InterpParams<float> p) : IDisposable
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
    public static FloatCLUTData Alloc(Context? ContextID, InterpParams<float> p) =>
        new(ContextID, p);
}
