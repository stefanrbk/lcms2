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

using lcms2.types;

using S1Fixed15Number = System.Int32;

namespace lcms2.FastFloatPlugin;

public static partial class FastFloat
{
    private static void MatShaperXform(Transform CMMcargo,
                                       ReadOnlySpan<byte> Input,
                                       Span<byte> Output,
                                       uint PixelsPerLine,
                                       uint LineCount,
                                       Stride Stride)
    {
        if (CMMcargo.UserData is not XMatShaperData p)
            return;

        Span<uint> SourceStartingOrder = stackalloc uint[Context.MaxChannels];
        Span<uint> SourceIncrements = stackalloc uint[Context.MaxChannels];
        Span<uint> DestStartingOrder = stackalloc uint[Context.MaxChannels];
        Span<uint> DestIncrements = stackalloc uint[Context.MaxChannels];

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
                // Across first shaper, which also converts to 1.15 fixed point.
                var r = p.Shaper1R[BitConverter.ToUInt16(Input[rin..])];
                var g = p.Shaper1G[BitConverter.ToUInt16(Input[gin..])];
                var b = p.Shaper1B[BitConverter.ToUInt16(Input[bin..])];

                var (l1, l2, l3) =
                    p.IdentityMat
                        ? (r, g, b)
                        : (     // Evaluate the matrix in 1.14 fixed point
                              ((p.Mat[(0 * 3) + 0] * r) + (p.Mat[(0 * 3) + 1] * g) + (p.Mat[(0 * 3) + 2] * b) +
                               p.Off[0]) >> 15,
                              ((p.Mat[(1 * 3) + 0] * r) + (p.Mat[(1 * 3) + 1] * g) + (p.Mat[(1 * 3) + 2] * b) +
                               p.Off[1]) >> 15,
                              ((p.Mat[(2 * 3) + 0] * r) + (p.Mat[(2 * 3) + 1] * g) + (p.Mat[(2 * 3) + 2] * b) +
                               p.Off[2]) >> 15);

                // Now we have to clip to 0..1.0 range
                var ri = (uint)Math.Clamp(l1, 0, 0x8000);
                var gi = (uint)Math.Clamp(l2, 0, 0x8000);
                var bi = (uint)Math.Clamp(l3, 0, 0x8000);

                // And across second shaper
                BitConverter.TryWriteBytes(Output[rout..], p.Shaper2R[(int)ri]);
                BitConverter.TryWriteBytes(Output[gout..], p.Shaper2G[(int)gi]);
                BitConverter.TryWriteBytes(Output[bout..], p.Shaper2B[(int)bi]);

                // Handle alpha
                if (nalpha is not 0)
                    BitConverter.TryWriteBytes(Output[aout..], BitConverter.ToUInt16(Input[ain..]));

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

    private static bool OptimizeMatrixShaper15(out Transform2Fn TransformFn,
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

        // Only works on RGB to RGB and gray
        if (!(T_CHANNELS(InputFormat) is 3 && T_CHANNELS(OutputFormat) is 3))
            return false;

        // Only works of 15 bit to 15 bit
        if (T_BYTES(InputFormat) is not 2 || T_BYTES(OutputFormat) is not 2 ||
            T_BIT15(InputFormat) is 0 || T_BIT15(OutputFormat) is 0)
        {
            return false;
        }

        // Seems suitable, proceed

        var Src = Lut;

        // Check for shaper-matrix-matrix-shaper structure, that is what this optimizer stands for
        if (!cmsPipelineCheckAndRetrieveStages(
                Src,
                Signatures.Stage.CurveSetElem,
                out var Curve1,
                Signatures.Stage.MatrixElem,
                out var Matrix1,
                Signatures.Stage.MatrixElem,
                out var Matrix2,
                Signatures.Stage.CurveSetElem,
                out var Curve2))
        {
            return false;
        }

        var ContextID = cmsGetPipelineContextID(Src);
        var nChans = (uint)T_CHANNELS(InputFormat);

        // Get both matrices, which are 3x3
        if (cmsStageData(Matrix1) is not StageMatrixData Data1 || cmsStageData(Matrix2) is not StageMatrixData Data2)
            return false;

        // Input offset should be zero
        if (Data1.Offset is not null)
            return false;

        // Multiply both matrices to get the result
        var res = new MAT3(Data2.Double) * new MAT3(Data1.Double);

        // Now the result is in res + Data2.Offset. Maybe is a plain identity?
        var IdentityMat = res.IsIdentity && Data2.Offset is null;   // We can get rid of full matrix

        // Allocate an empty LUT
        var Dest = cmsPipelineAlloc(ContextID, nChans, nChans);
        if (Dest is null)
            return false;

        // Assemble the new LUT
        cmsPipelineInsertStage(Dest, StageLoc.AtEnd, cmsStageDup(Curve1));

        if (!IdentityMat)
        {
            cmsPipelineInsertStage(Dest, StageLoc.AtEnd, cmsStageAllocMatrix(ContextID, 3, 3, res, Data2.Offset));
        }

        cmsPipelineInsertStage(Dest, StageLoc.AtEnd, cmsStageDup(Curve2));

        {
            if (cmsStageData(Curve1) is not StageToneCurvesData mpeC1 ||
                cmsStageData(Curve2) is not StageToneCurvesData mpeC2)
                return false;

            // In this particular optimization, cache does not help as it takes more time to deal with
            // the cache than with the pixel handling
            dwFlags |= cmsFLAGS_NOCACHE;

            // Setup the optimization routines
            UserData = XMatShaperData.SetShaper(
                ContextID,
                mpeC1.TheCurves,
                res,
                Data2.Offset is null ? default : new VEC3(Data2.Offset),
                mpeC2.TheCurves,
                IdentityMat);
            FreeUserData = FreeDisposable;

            TransformFn = MatShaperXform;
        }

        cmsPipelineFree(Src);
        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;
        Lut = Dest;
        return true;
    }
}

//file
public class XMatShaperData(Context? context) : IDisposable
{
    //private readonly S1Fixed15Number[] _mat = Context.GetPool<S1Fixed15Number>(context).Rent(9);
    //private readonly S1Fixed15Number[] _off = Context.GetPool<S1Fixed15Number>(context).Rent(3);
    //private readonly ushort[] _shapers = Context.GetPool<ushort>(context).Rent(MAX_NODES_IN_CURVE * 6);
    private readonly S1Fixed15Number[] _mat = new S1Fixed15Number[9];
    private readonly S1Fixed15Number[] _off = new S1Fixed15Number[3];
    private readonly ushort[] _shapers = new ushort[MAX_NODES_IN_CURVE * 6];

    public Span<S1Fixed15Number> Mat =>
        _mat.AsSpan(..9);

    public Span<S1Fixed15Number> Off =>
        _off.AsSpan(..3);

    public Span<ushort> Shaper1R =>
        _shapers.AsSpan(..MAX_NODES_IN_CURVE);

    public Span<ushort> Shaper1G =>
        _shapers.AsSpan(MAX_NODES_IN_CURVE..(MAX_NODES_IN_CURVE * 2));

    public Span<ushort> Shaper1B =>
        _shapers.AsSpan((MAX_NODES_IN_CURVE * 2)..(MAX_NODES_IN_CURVE * 3));

    public Span<ushort> Shaper2R =>
        _shapers.AsSpan((MAX_NODES_IN_CURVE * 3)..(MAX_NODES_IN_CURVE * 4));

    public Span<ushort> Shaper2G =>
        _shapers.AsSpan((MAX_NODES_IN_CURVE * 4)..(MAX_NODES_IN_CURVE * 5));

    public Span<ushort> Shaper2B =>
        _shapers.AsSpan((MAX_NODES_IN_CURVE * 5)..(MAX_NODES_IN_CURVE * 6));

    public bool IdentityMat;

    public Context? ContextID = context;
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            //if (disposing)
            //{
            //    var pool = Context.GetPool<S1Fixed15Number>(ContextID);

            //    pool.Return(_mat);
            //    pool.Return(_off);

            //    Context.GetPool<ushort>(ContextID).Return(_shapers);
            //}

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private static S1Fixed15Number DOUBLE_TO_1FIXED15(double x) =>
        (S1Fixed15Number)(x * 0x8000 + 0.5);

    private static void FillShaper(Span<ushort> Table, ToneCurve Curve)
    {
        for (var i = 0; i < MAX_NODES_IN_CURVE; i++)
        {
            var R = (float)i / (MAX_NODES_IN_CURVE - 1);
            var y = cmsEvalToneCurveFloat(Curve, R);

            Table[i] = (ushort)DOUBLE_TO_1FIXED15(y);
        }
    }

    public static XMatShaperData? SetShaper(Context? ContextID,
                                            ReadOnlySpan<ToneCurve> Curve1,
                                            MAT3 Mat,
                                            VEC3? Off,
                                            ReadOnlySpan<ToneCurve> Curve2,
                                            bool IdentityMat)
    {
        var p = new XMatShaperData(ContextID) { IdentityMat = IdentityMat, };

        p.Shaper1R.Clear();
        p.Shaper1G.Clear();
        p.Shaper1B.Clear();

        p.Shaper2R.Clear();
        p.Shaper2G.Clear();
        p.Shaper2B.Clear();

        p.Mat.Clear();
        p.Off.Clear();

        // Precompute tables
        FillShaper(p.Shaper1R, Curve1[0]);
        FillShaper(p.Shaper1G, Curve1[1]);
        FillShaper(p.Shaper1B, Curve1[2]);

        FillShaper(p.Shaper2R, Curve2[0]);
        FillShaper(p.Shaper2G, Curve2[1]);
        FillShaper(p.Shaper2B, Curve2[2]);

        // Convert matrix to nFixed14. Note that those values may take more than 16 bits if negative
        p.Mat[0] = DOUBLE_TO_1FIXED15(Mat.X.X);
        p.Mat[1] = DOUBLE_TO_1FIXED15(Mat.X.Y);
        p.Mat[2] = DOUBLE_TO_1FIXED15(Mat.X.Z);

        p.Mat[3] = DOUBLE_TO_1FIXED15(Mat.Y.X);
        p.Mat[4] = DOUBLE_TO_1FIXED15(Mat.Y.Y);
        p.Mat[5] = DOUBLE_TO_1FIXED15(Mat.Y.Z);

        p.Mat[6] = DOUBLE_TO_1FIXED15(Mat.Z.X);
        p.Mat[7] = DOUBLE_TO_1FIXED15(Mat.Z.Y);
        p.Mat[8] = DOUBLE_TO_1FIXED15(Mat.Z.Z);

        if (Off is null)
        {
            p.Off[0] = 0x4000;
            p.Off[1] = 0x4000;
            p.Off[2] = 0x4000;
        }
        else
        {
            p.Off[0] = DOUBLE_TO_1FIXED15(Off.Value.X) + 0x4000;
            p.Off[1] = DOUBLE_TO_1FIXED15(Off.Value.Y) + 0x4000;
            p.Off[2] = DOUBLE_TO_1FIXED15(Off.Value.Z) + 0x4000;
        }

        return p;
    }
}
