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

namespace lcms2.FastFloatPlugin;
public static partial class FastFloat
{
    private const ushort PRELINEARIZATION_POINTS = 4096;

    private static bool XFormSampler16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        if (Cargo is not Pipeline c)
            return false;

        // Evaluate in 16 bits
        cmsPipelineEval16(In, Out, c);

        return true;
    }

    private unsafe static void PerformanceEval8(Transform CMMcargo,
                                                ReadOnlySpan<byte> Input,
                                                Span<byte> Output,
                                                uint PixelsPerLine,
                                                uint LineCount,
                                                Stride Stride)
    {
        fixed (byte* OutputPtr = Output)
        {
            if (CMMcargo.UserData is not Performance8Data p8)
                return;
            var p = p8.p;
            var TotalOut = p.nOutputs;
            var LutTable = p.Table.Span;

            var @out = stackalloc byte*[cmsMAXCHANNELS];

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

                var TotalPlusAlpha = TotalOut;
                if (nalpha is not 0)
                    TotalPlusAlpha++;

                for (var OutChan = 0; OutChan < TotalPlusAlpha; OutChan++)
                {
                    @out[OutChan] = OutputPtr + DestStartingOrder[OutChan] + strideOut;
                }

                for (var ii = 0; ii < PixelsPerLine; ii++)
                {
                    var r = Input[rin];
                    var g = Input[gin];
                    var b = Input[bin];

                    rin += (int)SourceIncrements[0];
                    gin += (int)SourceIncrements[1];
                    bin += (int)SourceIncrements[2];

                    var X0 = (int)p8.X0[r];
                    var Y0 = (int)p8.Y0[g];
                    var Z0 = (int)p8.Z0[b];

                    var rx = p8.rx[r];
                    var ry = p8.ry[g];
                    var rz = p8.rz[b];

                    var X1 = X0 + ((rx is 0) ? 0 : (int)p.opta[2]);
                    var Y1 = Y0 + ((ry is 0) ? 0 : (int)p.opta[1]);
                    var Z1 = Z0 + ((rz is 0) ? 0 : (int)p.opta[0]);

                    // These are the 6 Tetrahedral
                    for (var OutChan = 0; OutChan < TotalOut; OutChan++)
                    {
                        [DebuggerStepThrough]
                        int DENS(int i, int j, int k, Span<ushort> table)
                        {
                            return table[i + j + k + OutChan];
                        }

                        var c0 = DENS(X0, Y0, Z0, LutTable);
                        int c1, c2, c3;

                        if (rx >= ry && ry >= rz)
                        {
                            c1 = DENS(X1, Y0, Z0, LutTable) - c0;
                            c2 = DENS(X1, Y1, Z0, LutTable) - DENS(X1, Y0, Z0, LutTable);
                            c3 = DENS(X1, Y1, Z1, LutTable) - DENS(X1, Y1, Z0, LutTable);
                        }
                        else if (rx >= rz && rz >= ry)
                        {
                            c1 = DENS(X1, Y0, Z0, LutTable) - c0;
                            c2 = DENS(X1, Y1, Z1, LutTable) - DENS(X1, Y0, Z1, LutTable);
                            c3 = DENS(X1, Y0, Z1, LutTable) - DENS(X1, Y0, Z0, LutTable);
                        }
                        else if (rz >= rx && rx >= ry)
                        {
                            c1 = DENS(X1, Y0, Z1, LutTable) - DENS(X0, Y0, Z1, LutTable);
                            c2 = DENS(X1, Y1, Z1, LutTable) - DENS(X1, Y0, Z1, LutTable);
                            c3 = DENS(X0, Y0, Z1, LutTable) - c0;
                        }
                        else if (ry >= rx && rx >= rz)
                        {
                            c1 = DENS(X1, Y1, Z0, LutTable) - DENS(X0, Y1, Z0, LutTable);
                            c2 = DENS(X0, Y1, Z0, LutTable) - c0;
                            c3 = DENS(X1, Y1, Z1, LutTable) - DENS(X1, Y1, Z0, LutTable);
                        }
                        else if (ry >= rz && rz >= rx)
                        {
                            c1 = DENS(X1, Y1, Z1, LutTable) - DENS(X0, Y1, Z1, LutTable);
                            c2 = DENS(X0, Y1, Z0, LutTable) - c0;
                            c3 = DENS(X0, Y1, Z1, LutTable) - DENS(X0, Y1, Z0, LutTable);
                        }
                        else if (rz >= ry && ry >= rx)
                        {
                            c1 = DENS(X1, Y1, Z1, LutTable) - DENS(X0, Y1, Z1, LutTable);
                            c2 = DENS(X0, Y1, Z1, LutTable) - DENS(X0, Y0, Z1, LutTable);
                            c3 = DENS(X0, Y0, Z1, LutTable) - c0;
                        }
                        else
                        {
                            c1 = c2 = c3 = 0;
                        }

                        var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
                        var res16 = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));

                        *@out[OutChan] = FROM_16_TO_8(res16);
                        @out[OutChan] += DestIncrements[OutChan];
                    }

                    if (nalpha is not 0)
                    {
                        *@out[TotalOut] = Input[ain];
                        ain += (int)SourceIncrements[3];
                        @out[TotalOut] += DestIncrements[(int)TotalOut];
                    }
                }

                strideIn += Stride.BytesPerLineIn;
                strideOut += Stride.BytesPerLineOut;
            }
        }
    }

    private static bool IsDegenerated(ToneCurve g)
    {
        var Zeros = 0;
        var Poles = 0;
        var nEntries = cmsGetToneCurveEstimatedTableEntries(g);
        var Table16 = cmsGetToneCurveEstimatedTable(g);

        for (var i = 0; i < nEntries; i++)
        {
            if (Table16[i] is 0x0000) Zeros++;
            if (Table16[i] is 0xffff) Poles++;
        }

        if (Zeros is 1 && Poles is 1)
            return false;               // For linear tables
        if (Zeros > (nEntries / 4))
            return true;                // Degenerated, mostly zeros
        if (Poles > (nEntries / 4))
            return true;                // Degenerated, mostly poles

        return false;
    }

    private static void SlopeLimiting(Span<ushort> Table16, int nEntries)
    {
        var AtBegin = (int)Math.Floor((nEntries * 0.02) + 0.5);
        var AtEnd = nEntries = AtBegin - 1;

        var (BeginVal, EndVal) =
            (Table16[0] > Table16[nEntries - 1])
                ? (0xffff, 0)
                : (0, 0xffff);

        // Compute slope and offset for begin of curve
        var Val = (double)Table16[AtBegin];
        var Slope = (Val - BeginVal) / AtBegin;
        var beta = Val - Slope * AtBegin;

        for (var i = 0; i < AtBegin; i++)
            Table16[i] = _cmsSaturateWord((i * Slope) + beta);

        // Compute slope and offset for the end
        Val = Table16[AtEnd];
        Slope = (EndVal - Val) / AtBegin;   //AtBegin holds the X interval, which is same in both cases
        beta = Val - (Slope * AtEnd);

        for (var i = AtEnd; i < nEntries; i++)
            Table16[i] = _cmsSaturateWord((i * Slope) + beta);
    }

    private static bool Optimize8BitRGBTransform(out Transform2Fn TransformFn,
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
        Pipeline? OptimizedLUT = null, LutPlusCurves = null;

        // For empty transforms, do nothing
        if (Lut is null)
            return false;

        // This is a lossy optimization! does not apply in floating-point cases
        if (T_FLOAT(InputFormat) is not 0 || T_FLOAT(OutputFormat) is not 0)
            return false;

        // Only on 8 bit
        if (T_BYTES(InputFormat) is not 1 || T_BYTES(OutputFormat) is not 1)
            return false;

        // Only on RGB
        if (T_COLORSPACE(InputFormat) is not PT_RGB)
            return false;

        // This optimization only works on RGB8->RGB8
        if (T_COLORSPACE(OutputFormat) is not PT_RGB)
            return false;

        // Seems suitable, proceed

        var OriginalLut = Lut;

        var ContextID = cmsGetPipelineContextID(OriginalLut);
        var nGridPoints = _cmsReasonableGridpointsByColorspace(Signature.Colorspace.Rgb, dwFlags);

        //var tcPool = Context.GetPool<ToneCurve>(ContextID);
        //var uaPool = Context.GetPool<ushort[]>(ContextID);
        //var usPool = Context.GetPool<ushort>(ContextID);

        //var TransArray = tcPool.Rent(cmsMAXCHANNELS);
        var TransArray = new ToneCurve[cmsMAXCHANNELS];
        var Trans = TransArray.AsSpan(..cmsMAXCHANNELS);

        //var TransReverseArray = tcPool.Rent(cmsMAXCHANNELS);
        var TransReverseArray = new ToneCurve[cmsMAXCHANNELS];
        var TransReverse = TransReverseArray.AsSpan(..cmsMAXCHANNELS);

        //var MyTableArray = uaPool.Rent(3);
        var MyTableArray = new ushort[3][];
        var MyTable = MyTableArray.AsSpan(..3);

        // Empty gamma containers
        //Trans.Clear();
        //TransReverse.Clear();

        //MyTable[0] = usPool.Rent(PRELINEARIZATION_POINTS);
        //MyTable[1] = usPool.Rent(PRELINEARIZATION_POINTS);
        //MyTable[2] = usPool.Rent(PRELINEARIZATION_POINTS);
        MyTable[0] = new ushort[PRELINEARIZATION_POINTS];
        MyTable[1] = new ushort[PRELINEARIZATION_POINTS];
        MyTable[2] = new ushort[PRELINEARIZATION_POINTS];

        //Array.Clear(MyTable[0]);
        //Array.Clear(MyTable[1]);
        //Array.Clear(MyTable[2]);

        // Populate the curves

        for (var i = 0; i < PRELINEARIZATION_POINTS; i++)
        {
            var v = (float)((double)i / (PRELINEARIZATION_POINTS - 1));

            // Feed input with a gray ramp
            for (var j = 0; j < 3; j++)
                In[j] = v;

            // Evaluate the gray value
            cmsPipelineEvalFloat(In, Out, OriginalLut);

            // Store result in curve
            for (var j = 0; j < 3; j++)
                MyTable[j][i] = _cmsSaturateWord(Out[j] * 65535.0);
        }

        for (var t = 0; t < 3; t++)
        {
            SlopeLimiting(MyTable[t], PRELINEARIZATION_POINTS);

            Trans[t] = cmsBuildTabulatedToneCurve16(ContextID, PRELINEARIZATION_POINTS, MyTable[t])!;
            if (Trans[t] is null) goto Error;

            //usPool.Return(MyTable[t]);
            MyTable[t] = null!;
        }

        // Check for validity
        var isSuitable = true;
        for (var t = 0; isSuitable && (t < 3); t++)
        {
            // Exclude if non-monotonic
            if (!cmsIsToneCurveMonotonic(Trans[t]))
                isSuitable = false;

            if (IsDegenerated(Trans[t]))
                isSuitable = false;
        }

        // If it is not suitable, just quit
        if (!isSuitable)
            goto Error;

        // Invert curves if possible
        var inputChannels = cmsPipelineInputChannels(OriginalLut);
        for (var t = 0; t < inputChannels; t++)
        {
            TransReverse[t] = cmsReverseToneCurveEx(PRELINEARIZATION_POINTS, Trans[t])!;
            if (TransReverse[t] is null)
                goto Error;
        }

        // Now insert the reversed curves at the beginning of the transform
        LutPlusCurves = cmsPipelineDup(OriginalLut);
        if (LutPlusCurves is null)
            goto Error;

        cmsPipelineInsertStage(LutPlusCurves, StageLoc.AtBegin, cmsStageAllocToneCurves(ContextID, 3, TransReverse));

        // Create the result LUT
        OptimizedLUT = cmsPipelineAlloc(ContextID, 3, cmsPipelineOutputChannels(OriginalLut));
        if (OptimizedLUT is null)
            goto Error;

        var OptimizedPrelinMpe = cmsStageAllocToneCurves(ContextID, 3, Trans);

        // Create and insert the curves at the beginning
        cmsPipelineInsertStage(OptimizedLUT, StageLoc.AtBegin, OptimizedPrelinMpe);

        // Allocate the CLUT for result
        var OptimizedCLUTmpe = cmsStageAllocCLut16bit(ContextID, nGridPoints, 3, cmsPipelineOutputChannels(OriginalLut), null);

        // Add the CLUT to the destination LUT
        cmsPipelineInsertStage(OptimizedLUT, StageLoc.AtEnd, OptimizedCLUTmpe);

        // Resample the LUT
        if (!cmsStageSampleCLut16bit(OptimizedCLUTmpe, XFormSampler16, LutPlusCurves, SamplerFlag.None))
            goto Error;

        // Set the evaluator
        var data = (StageCLutData<ushort>)cmsStageData(OptimizedCLUTmpe!)!;

        var p8 = Performance8Data.Alloc(ContextID, data.Params, Trans);
        if (p8 is null)
            goto Error;

        // Free resources
        freeResources();

        cmsPipelineFree(OriginalLut);

        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;
        Lut = OptimizedLUT;
        TransformFn = PerformanceEval8;
        UserData = p8;
        FreeUserData = FreeDisposable;

        return true;

    Error:
        freeResources();

        if (OptimizedLUT is not null)
            cmsPipelineFree(OptimizedLUT);

        return false;

        void freeResources()
        {
            for (var t = 0; t < 3; t++)
            {
                if (TransArray[t] is not null)
                    cmsFreeToneCurve(TransArray[t]);
                if (TransReverseArray[t] is not null)
                    cmsFreeToneCurve(TransReverseArray[t]);

                if (MyTableArray[t] is not null)
                {
                    //usPool.Return(MyTableArray[t]);
                    MyTableArray[t] = null!;
                }
            }

            if (LutPlusCurves is not null)
                cmsPipelineFree(LutPlusCurves);

            //uaPool.Return(MyTableArray);
            //tcPool.Return(TransArray);
            //tcPool.Return(TransReverseArray);
        }
    }
}

file class Performance8Data : IDisposable
{
    public readonly Context? ContextID;
    public readonly InterpParams<ushort> p;     // Tetrahedrical interpolation parameters

    private readonly ushort[] _rx;
    private readonly ushort[] _ry;
    private readonly ushort[] _rz;

    private readonly uint[] _X0;  // Precomputed nodes and offsets for 8-bit input data
    private readonly uint[] _Y0;
    private readonly uint[] _Z0;

    private bool disposedValue;

    public Span<ushort> rx => _rx.AsSpan(..256);
    public Span<ushort> ry => _ry.AsSpan(..256);
    public Span<ushort> rz => _rz.AsSpan(..256);

    public Span<uint> X0 => _X0.AsSpan(..0x4001);
    public Span<uint> Y0 => _Y0.AsSpan(..0x4001);
    public Span<uint> Z0 => _Z0.AsSpan(..0x4001);

    public Performance8Data(Context? context, InterpParams<ushort> p)
    {
        ContextID = context;
        this.p = p;

        //var ipool = Context.GetPool<uint>(context);
        //var spool = Context.GetPool<ushort>(context);

        //_rx = spool.Rent(256);
        //_ry = spool.Rent(256);
        //_rz = spool.Rent(256);

        //_X0 = ipool.Rent(0x4001);
        //_Y0 = ipool.Rent(0x4001);
        //_Z0 = ipool.Rent(0x4001);

        _rx = new ushort[256];
        _ry = new ushort[256];
        _rz = new ushort[256];

        _X0 = new uint[0x4001];
        _Y0 = new uint[0x4001];
        _Z0 = new uint[0x4001];

    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            //if (disposing)
            //{
            //    var ipool = Context.GetPool<uint>(ContextID);
            //    var spool = Context.GetPool<ushort>(ContextID);

            //    spool.Return(_rx);
            //    spool.Return(_ry);
            //    spool.Return(_rz);

            //    ipool.Return(_X0);
            //    ipool.Return(_Y0);
            //    ipool.Return(_Z0);
            //}
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static Performance8Data Alloc(Context? ContextID, InterpParams<ushort> p, ReadOnlySpan<ToneCurve> G)
    {
        Span<ushort> Input = stackalloc ushort[3];

        var p8 = new Performance8Data(ContextID, p);

        // Since this only works for 8 bit input, values comes always as x * 257
        // we can safely take msb byte (x << 8 + x)
        for (var i = 0; i < 256; i++)
        {
            if (!G.IsEmpty)
            {
                // Get 16-bit representation
                Input[0] = cmsEvalToneCurve16(G[0], FROM_8_TO_16((byte)i));
                Input[1] = cmsEvalToneCurve16(G[1], FROM_8_TO_16((byte)i));
                Input[2] = cmsEvalToneCurve16(G[2], FROM_8_TO_16((byte)i));
            }
            else
            {
                Input[0] = FROM_8_TO_16((byte)i);
                Input[1] = FROM_8_TO_16((byte)i);
                Input[2] = FROM_8_TO_16((byte)i);
            }

            // Move to 0..1.0 in fixed domain
            var v1 = _cmsToFixedDomain(Input[0] * (int)p.Domain[0]);
            var v2 = _cmsToFixedDomain(Input[1] * (int)p.Domain[1]);
            var v3 = _cmsToFixedDomain(Input[2] * (int)p.Domain[2]);

            // Store the precalculated table of nodes
            p8.X0[i] = (uint)(p.opta[2] * FIXED_TO_INT(v1));
            p8.Y0[i] = (uint)(p.opta[1] * FIXED_TO_INT(v2));
            p8.Z0[i] = (uint)(p.opta[0] * FIXED_TO_INT(v3));

            // Store the precalculated table of offsets
            p8.rx[i] = (ushort)FIXED_REST_TO_INT(v1);
            p8.ry[i] = (ushort)FIXED_REST_TO_INT(v2);
            p8.rz[i] = (ushort)FIXED_REST_TO_INT(v3);
        }

        return p8;
    }
}
