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

using lcms2.stages;

namespace lcms2.FastFloatPlugin;

public static partial class FastFloat
{
    internal const ushort SIGMOID_POINTS = 1024;

    public static uint TYPE_SIGMOID =>
        109;

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float LinLerp1D(float Value, ReadOnlySpan<float> LutTable)
    {
        if (Value >= 1.0f)
        {
            return LutTable[SIGMOID_POINTS - 1];
        }
        else if (Value <= 0)
        {
            return LutTable[0];
        }
        else
        {
            Value *= SIGMOID_POINTS - 1;

            var cell0 = Conversions.QuickFloor(Value);
            var cell1 = cell0 + 1;

            var rest = Value - cell0;

            var y0 = LutTable[cell0];
            var y1 = LutTable[cell1];

            return y0 + ((y1 - y0) * rest);
        }
    }

    private static bool XFormSamplerLab(ReadOnlySpan<float> In, Span<float> Out, object? Cargo)
    {
        if (Cargo is not ResamplingContainer container)
            return false;

        Span<float> linearized = stackalloc float[3];

        // Apply inverse sigmoid
        linearized[0] = In[0];
        linearized[1] = LinLerp1D(In[1], container.data.sigmoidOut);
        linearized[2] = LinLerp1D(In[2], container.data.sigmoidOut);

        container.original.Evaluate(linearized, Out);

        return true;
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float fclamp128(float v) =>
        float.IsNaN(v)
            ? -128f
            : Math.Clamp(v, -128, 128);

    private unsafe static void LabCLUTEval(Transform CMMcargo,
                                           ReadOnlySpan<byte> Input,
                                           Span<byte> Output,
                                           uint PixelsPerLine,
                                           uint LineCount,
                                           Stride Stride)
    {
        fixed (byte* OutputPtr = Output)
        {
            if (CMMcargo.UserData is not LabCLUTData pfloat)
                return;
            var p = pfloat.p;
            var TotalOut = p.nOutputs;

            var @out = stackalloc byte*[Context.MaxChannels];

            Span<uint> SourceStartingOrder = stackalloc uint[Context.MaxChannels];
            Span<uint> SourceIncrements = stackalloc uint[Context.MaxChannels];
            Span<uint> DestStartingOrder = stackalloc uint[Context.MaxChannels];
            Span<uint> DestIncrements = stackalloc uint[Context.MaxChannels];

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
                    var lin = (int)(SourceStartingOrder[0] + strideIn);
                    var ain = (int)(SourceStartingOrder[1] + strideIn);
                    var bin = (int)(SourceStartingOrder[2] + strideIn);
                    var xin =
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
                        // Decode Lab and go across sigmoids on a*/b*
                        var l = fclamp100(BitConverter.ToSingle(Input[lin..])) / 100f;

                        var a = LinLerp1D(
                            (fclamp128(BitConverter.ToSingle(Input[ain..])) + 128.0f) / 255.0f,
                            pfloat.sigmoidIn);
                        var b = LinLerp1D(
                            (fclamp128(BitConverter.ToSingle(Input[bin..])) + 128.0f) / 255.0f,
                            pfloat.sigmoidIn);

                        lin += (int)SourceIncrements[0];
                        ain += (int)SourceIncrements[1];
                        bin += (int)SourceIncrements[2];

                        var px = l * p.Domain[0];
                        var py = a * p.Domain[1];
                        var pz = b * p.Domain[2];

                        var x0 = Conversions.QuickFloor(px);
                        var rx = px - x0;
                        var y0 = Conversions.QuickFloor(py);
                        var ry = py - y0;
                        var z0 = Conversions.QuickFloor(pz);
                        var rz = pz - z0;

                        var X0 = (int)p.opta[2] * x0;
                        var X1 = X0 + ((l >= 1.0f) ? 0 : (int)p.opta[2]);

                        var Y0 = (int)p.opta[1] * y0;
                        var Y1 = Y0 + ((a >= 1.0f) ? 0 : (int)p.opta[1]);

                        var Z0 = (int)p.opta[0] * z0;
                        var Z1 = Z0 + ((b >= 1.0f) ? 0 : (int)p.opta[0]);

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

                            *(float*)@out[OutChan] = c0 + c1 * rx + c2 * ry + c3 * rz;

                            @out[OutChan] += DestIncrements[OutChan];
                        }

                        if (nalpha is not 0)
                        {
                            *(float*)@out[TotalOut] = BitConverter.ToSingle(Input[xin..]);
                            xin += (int)SourceIncrements[3];
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

    private static int GetGridPoints(uint dwFlags)
    {
        // Already specified?
        if ((dwFlags & 0x00FF0000) is not 0)
            return (int)((dwFlags >> 16) & 0xff);

        // HighResPrecalc is maximun resolution
        if ((dwFlags & cmsFLAGS_HIGHRESPRECALC) is not 0)
        {
            return 66;
        }
        // LowResPrecalc is lower resolution
        else if ((dwFlags & cmsFLAGS_LOWRESPRECALC) is not 0)
        {
            return 33;
        }
        else
        {
            return 51;
        }
    }

    private static bool OptimizeCLUTLabTransform(out Transform2Fn TransformFn,
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

        // Only on Lab
        if (T_COLORSPACE(InputFormat) is not PT_Lab)
            return false;

        // Seems suitable, proceed

        var OriginalLut = Lut;

        var ContextID = OriginalLut.ContextID;
        var nGridPoints = (uint)GetGridPoints(dwFlags);

        // Create the result LUT
        OptimizedLUT = new Pipeline(ContextID, 3, OriginalLut.OutputChannels);

        // Allocate the CLUT for result
        var OptimizedCLUTmpe = new CLutStage<float>(
            ContextID,
            nGridPoints,
            3,
            OriginalLut.OutputChannels,
            null);

        // Add the CLUT to the destination LUT
        OptimizedLUT.InsertStageAtStart(OptimizedCLUTmpe);

        // Set the evaluator
        var data = OptimizedCLUTmpe;

        var pfloat = LabCLUTData.Alloc(ContextID, data.Params);

        var container = new ResamplingContainer(pfloat, OriginalLut);

        // Resample the LUT
        if (!OptimizedCLUTmpe.Sample(XFormSamplerLab, container))
            goto Error;

        // And return the obtained LUT
        Lut = OptimizedLUT;
        TransformFn = LabCLUTEval;
        UserData = pfloat;
        FreeUserData = FreeDisposable;
        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;

        return true;

    Error:
        return false;
    }
}

file record ResamplingContainer(LabCLUTData data, Pipeline original);

file class LabCLUTData : IDisposable
{
    public readonly Context? ContextID;
    public readonly InterpParams<float> p;     // Tetrahedrical interpolation parameters

    private readonly float[] sIn;
    private readonly float[] sOut;

    public Span<float> sigmoidIn =>
        sIn.AsSpan(..SIGMOID_POINTS);

    public Span<float> sigmoidOut =>
        sOut.AsSpan(..SIGMOID_POINTS);

    private bool disposedValue;

    public LabCLUTData(Context? context, InterpParams<float> p)
    {
        ContextID = context;
        this.p = p;

        //var pool = Context.GetPool<float>(context);

        //sIn = pool.Rent(SIGMOID_POINTS);
        //sOut = pool.Rent(SIGMOID_POINTS);
        sIn = new float[SIGMOID_POINTS];
        sOut = new float[SIGMOID_POINTS];
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            //if (disposing)
            //{
            //    var pool = Context.GetPool<float>(ContextID);
            //    pool.Return(sIn);
            //    pool.Return(sOut);
            //}
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static LabCLUTData Alloc(Context? ContextID, InterpParams<float> p)
    {
        var fd = new LabCLUTData(ContextID, p);

        tabulateSigmoid(ContextID, +(int)TYPE_SIGMOID, fd.sigmoidIn, SIGMOID_POINTS);
        tabulateSigmoid(ContextID, -(int)TYPE_SIGMOID, fd.sigmoidOut, SIGMOID_POINTS);

        return fd;
    }

    private static void tabulateSigmoid(Context? ContextID, int type, Span<float> table, int tablePoints)
    {
        ReadOnlySpan<double> sigmoidal_slope = stackalloc double[] { 2.5 };

        table = table[..tablePoints];
        table.Clear();

        var original = ToneCurve.BuildParametric(ContextID, type, sigmoidal_slope);
        if (original is not null)
        {
            for (var i = 0; i < tablePoints; i++)
            {
                var v = (float)i / (tablePoints - 1);

                table[i] = fclamp(original.Evaluate(v));
            }
        }
    }
}
