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

using System.Runtime.InteropServices;

namespace lcms2.FastFloatPlugin;
public unsafe static partial class FastFloat
{
    private static void MatShaperFloat(Transform CMMcargo,
                                       ReadOnlySpan<byte> In,
                                       Span<byte> Out,
                                       uint PixelsPerLine,
                                       uint LineCount,
                                       Stride Stride)
    {
        if (CMMcargo.UserData is not nuint pPtr)
            return;

        var p = (VXMatShaperFloatData*)pPtr;

        var SourceStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        var SourceIncrements = stackalloc uint[cmsMAXCHANNELS];
        var DestStartingOrder = stackalloc uint[cmsMAXCHANNELS];
        var DestIncrements = stackalloc uint[cmsMAXCHANNELS];

        _cmsComputeComponentIncrements(cmsGetTransformInputFormat(CMMcargo), Stride.BytesPerPlaneIn, out _, out var nalpha, new(SourceStartingOrder, cmsMAXCHANNELS), new(SourceIncrements, cmsMAXCHANNELS));
        _cmsComputeComponentIncrements(cmsGetTransformOutputFormat(CMMcargo), Stride.BytesPerPlaneOut, out _, out nalpha, new(DestStartingOrder, cmsMAXCHANNELS), new(DestIncrements, cmsMAXCHANNELS));

        if ((CMMcargo.Flags & cmsFLAGS_COPY_ALPHA) is 0)
            nalpha = 0;

        fixed (byte* Input = In, Output = Out)
        {
            nuint strideIn = 0;
            nuint strideOut = 0;
            for (var i = 0; i < LineCount; i++)
            {
                var rin = Input + SourceStartingOrder[0] + strideIn;
                var gin = Input + SourceStartingOrder[1] + strideIn;
                var bin = Input + SourceStartingOrder[2] + strideIn;
                var ain =
                    nalpha is not 0
                        ? Input + SourceStartingOrder[3] + strideIn
                        : default;

                var rout = Output + DestStartingOrder[0] + strideOut;
                var gout = Output + DestStartingOrder[1] + strideOut;
                var bout = Output + DestStartingOrder[2] + strideOut;
                var aout =
                    nalpha is not 0
                        ? Output + DestStartingOrder[3] + strideOut
                        : default;

                for (var ii = 0; ii < PixelsPerLine; ii++)
                {
                    var r = flerp(p->Shaper1R, *(float*)rin);
                    var g = flerp(p->Shaper1G, *(float*)gin);
                    var b = flerp(p->Shaper1B, *(float*)bin);

                    var l1 = (p->Mat[0] * r) + (p->Mat[1] * g) + (p->Mat[2] * b);
                    var l2 = (p->Mat[3] * r) + (p->Mat[4] * g) + (p->Mat[5] * b);
                    var l3 = (p->Mat[6] * r) + (p->Mat[7] * g) + (p->Mat[8] * b);

                    if (p->UseOff)
                    {
                        l1 += p->Off[0];
                        l2 += p->Off[1];
                        l3 += p->Off[2];
                    }

                    *(float*)rout = flerp(p->Shaper2R, l1);
                    *(float*)gout = flerp(p->Shaper2G, l2);
                    *(float*)bout = flerp(p->Shaper2B, l3);

                    rin += SourceIncrements[0];
                    gin += SourceIncrements[1];
                    bin += SourceIncrements[2];

                    rout += DestIncrements[0];
                    gout += DestIncrements[1];
                    bout += DestIncrements[2];

                    if (nalpha is not 0)
                    {
                        *(float*)aout = *(float*)ain;
                        ain += SourceIncrements[3];
                        aout += DestIncrements[3];
                    }
                }

                strideIn += Stride.BytesPerLineIn;
                strideOut += Stride.BytesPerLineOut;
            }
        }
    }

    private static bool OptimizeFloatMatrixShaper(out Transform2Fn TransformFn,
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

        // Apply only to floating-point cases
        if (T_FLOAT(InputFormat) is 0 || T_FLOAT(OutputFormat) is 0)
            return false;

        // Only works on RGB to RGB and gray to gray
        if (!((T_CHANNELS(InputFormat) is 3 && T_CHANNELS(OutputFormat) is 3) ||
              (T_CHANNELS(InputFormat) is 1 && T_CHANNELS(OutputFormat) is 1)))
        {
            return false;
        }

        // Only works on float
        if (T_BYTES(InputFormat) is not 4 || T_BYTES(OutputFormat) is not 4)
        {
            return false;
        }

        // Seems suitable, proceed

        var Src = Lut;
        Span<double> factor = stackalloc double[1] { 1.0 };
        var IdentityMat = false;
        MAT3 res = default;

        // Check for shaper-matrix-matrix-shaper structure, that is what this optimizer stands for
        if (!cmsPipelineCheckAndRetrieveStages(Src, Signature.Stage.CurveSetElem, out var Curve1,
                                                    Signature.Stage.MatrixElem, out var Matrix1,
                                                    Signature.Stage.MatrixElem, out var Matrix2,
                                                    Signature.Stage.CurveSetElem, out var Curve2))
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

        if (cmsStageInputChannels(Matrix1) is 1 && cmsStageInputChannels(Matrix2) is 1)
        {
            // This is a gray to gray. Just multiply
            factor[0] = (Data1.Double[0] * Data2.Double[0]) +
                        (Data1.Double[1] * Data2.Double[1]) +
                        (Data1.Double[2] * Data2.Double[2]);

            if (Math.Abs(1 - factor[0]) < (1.0 / 65535.0))
                IdentityMat = true;
        }
        else
        {
            // Multiply both matrices to get the result
            res = new MAT3(Data2.Double) * new MAT3(Data1.Double);

            // Now the result is in res + Data2.Offset. Maybe is a plain identity?
            IdentityMat = res.IsIdentity && Data2.Offset is null;   // We can get rid of full matrix
        }

        // Allocate an empty LUT
        var Dest = cmsPipelineAlloc(ContextID, nChans, nChans);
        if (Dest is null)
            return false;

        // Assemble the new LUT
        cmsPipelineInsertStage(Dest, StageLoc.AtBegin, cmsStageDup(Curve1));

        if (!IdentityMat)
        {
            if (nChans is 1)
            {
                cmsPipelineInsertStage(Dest, StageLoc.AtEnd, cmsStageAllocMatrix(ContextID, 1, 1, factor, Data2.Offset));
            }
            else
            {
                cmsPipelineInsertStage(Dest, StageLoc.AtEnd, cmsStageAllocMatrix(ContextID, 3, 3, res, Data2.Offset));
            }
        }

        cmsPipelineInsertStage(Dest, StageLoc.AtEnd, cmsStageDup(Curve2));

        {
            // If identity on matrix, we can further optimize the curves, so call the join curves routine
            if (IdentityMat)
            {
                OptimizeFloatByJoiningCurves(out TransformFn, out UserData, out FreeUserData, ref Dest, ref InputFormat, ref OutputFormat, ref dwFlags);
            }
            else
            {
                if (cmsStageData(Curve1) is not StageToneCurvesData mpeC1 || cmsStageData(Curve2) is not StageToneCurvesData mpeC2)
                    return false;

                // In this particular optimization, cache does not help as it takes more time to deal with
                // the cache than with the pixel handling
                dwFlags |= cmsFLAGS_NOCACHE;

                // Setup the optimization routines
                UserData = (nuint)VXMatShaperFloatData.SetShaper(ContextID, mpeC1.TheCurves, res, Data2.Offset is null ? null : new VEC3(Data2.Offset), mpeC2.TheCurves);
                FreeUserData = FreeDisposable;

                TransformFn = MatShaperFloat;
            }
        }

        cmsPipelineFree(Src);
        dwFlags &= ~cmsFLAGS_CAN_CHANGE_FORMATTER;
        Lut = Dest;
        return true;
    }
}

file unsafe struct VXMatShaperFloatData
{
    public fixed float Mat[9];
    public fixed float Off[3];

    public fixed float Shaper1R[MAX_NODES_IN_CURVE];
    public fixed float Shaper1G[MAX_NODES_IN_CURVE];
    public fixed float Shaper1B[MAX_NODES_IN_CURVE];

    public fixed float Shaper2R[MAX_NODES_IN_CURVE];
    public fixed float Shaper2G[MAX_NODES_IN_CURVE];
    public fixed float Shaper2B[MAX_NODES_IN_CURVE];

    public bool UseOff;

    public static VXMatShaperFloatData* MallocAligned()
    {
        var aligned = NativeMemory.AlignedAlloc((nuint)sizeof(VXMatShaperFloatData), 16);
        NativeMemory.Clear(aligned, (nuint)sizeof(VXMatShaperFloatData));
        return (VXMatShaperFloatData*)aligned;
    }

    public static void Free(VXMatShaperFloatData* data)
    {
        NativeMemory.AlignedFree(data);
    }

    public static VXMatShaperFloatData* SetShaper(Context? ContextID,
                                                  ReadOnlySpan<ToneCurve> Curve1,
                                                  MAT3 Mat,
                                                  VEC3? Off,
                                                  ReadOnlySpan<ToneCurve> Curve2)
    {
        // Allocate a big chunk of memory to store precomputed tables
        var p = MallocAligned();

        // Precompute tables
        FillShaperFloatMat(p->Shaper1R, Curve1[0]);
        FillShaperFloatMat(p->Shaper1G, Curve1[1]);
        FillShaperFloatMat(p->Shaper1B, Curve1[2]);

        FillShaperFloatMat(p->Shaper2R, Curve2[0]);
        FillShaperFloatMat(p->Shaper2G, Curve2[1]);
        FillShaperFloatMat(p->Shaper2B, Curve2[2]);

        // Convert matrix to nFixed14. Note that those values may take more than 16 bits
        p->Mat[0] = (float)Mat.X.X;
        p->Mat[1] = (float)Mat.X.Y;
        p->Mat[2] = (float)Mat.X.Z;

        p->Mat[3] = (float)Mat.Y.X;
        p->Mat[4] = (float)Mat.Y.Y;
        p->Mat[5] = (float)Mat.Y.Z;

        p->Mat[6] = (float)Mat.Z.X;
        p->Mat[7] = (float)Mat.Z.Y;
        p->Mat[8] = (float)Mat.Z.Z;

        for (var i = 0; i < 3; i++)
        {
            if (Off is null)
            {
                p->UseOff = false;
                p->Off[i] = 0;
            }
            else
            {
                p->UseOff = true;
                p->Off[i] = (float)Off.Value[i];
            }
        }

        return p;
    }

    private static void FillShaperFloatMat(float* Table, ToneCurve Curve)
    {
        for (var i = 0; i < MAX_NODES_IN_CURVE; i++)
        {
            var R = (float)i / (MAX_NODES_IN_CURVE - 1);
            Table[i] = cmsEvalToneCurveFloat(Curve, R);
        }
    }
}
