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

using System.Runtime.CompilerServices;

using lcms2.state;
using lcms2.types;

namespace lcms2;

public static partial class Lcms2
{
    internal static readonly OptimizationCollection DefaultOptimization = new(
        new OPToptimizeFn[]
        {
            new(OptimizeByJoiningCurves),
            new(OptimizeMatrixShaper),
            new(OptimizeByComputingLinearization),
            new(OptimizeByResampling)
        });

    internal static readonly OptimizationPluginChunkType OptimizationPluginChunk = new();

    internal static readonly OptimizationPluginChunkType globalOptimizationPluginChunk = new();

    private sealed class Prelin8Data : IDisposable, ICloneable
    {
        public readonly Context? ContextID;

        public InterpParams<ushort> p;
        private readonly ushort[] _rx;
        private readonly ushort[] _ry;
        private readonly ushort[] _rz;
        private readonly uint[] _x0;
        private readonly uint[] _y0;
        private readonly uint[] _z0;

        public Span<ushort> rx =>
            _rx.AsSpan(..256);

        public Span<ushort> ry =>
            _ry.AsSpan(..256);

        public Span<ushort> rz =>
            _rz.AsSpan(..256);

        public Span<uint> X0 =>
            _x0.AsSpan(..256);

        public Span<uint> Y0 =>
            _y0.AsSpan(..256);

        public Span<uint> Z0 =>
            _z0.AsSpan(..256);

        public Prelin8Data(Context? context, InterpParams<ushort> @params)
        {
            ContextID = context;
            p = @params;

            //var usPool = Context.GetPool<ushort>(context);
            //var uiPool = Context.GetPool<uint>(context);

            //_rx = usPool.Rent(256);
            //_ry = usPool.Rent(256);
            //_rz = usPool.Rent(256);
            //_x0 = uiPool.Rent(256);
            //_y0 = uiPool.Rent(256);
            //_z0 = uiPool.Rent(256);
            _rx = new ushort[256];
            _ry = new ushort[256];
            _rz = new ushort[256];
            _x0 = new uint[256];
            _y0 = new uint[256];
            _z0 = new uint[256];
        }

        public void Dispose()
        {
            //var usPool = Context.GetPool<ushort>(ContextID);
            //var uiPool = Context.GetPool<uint>(ContextID);

            //ReturnArray(usPool, _rx);
            //ReturnArray(usPool, _ry);
            //ReturnArray(usPool, _rz);
            //ReturnArray(uiPool, _x0);
            //ReturnArray(uiPool, _y0);
            //ReturnArray(uiPool, _z0);

            GC.SuppressFinalize(this);
        }

        public object Clone()
        {
            var result = new Prelin8Data(ContextID, p);

            _rx.CopyTo(result.rx);
            _ry.CopyTo(result.ry);
            _rz.CopyTo(result.rz);

            _x0.CopyTo(result.X0);
            _y0.CopyTo(result.Y0);
            _z0.CopyTo(result.Z0);

            return result;
        }
    }

    private sealed class Prelin16Data : IDisposable, ICloneable
    {
        public Context? ContextID;

        // Number of channels
        public uint nInputs;

        public uint nOutputs;

        // The maximum number of input channels is known in advance
        private readonly InterpFn<ushort>[] evalCurveIn16;

        private readonly InterpParams<ushort>?[] paramsCurveIn16;

        // The evaluator for 3D grid
        public InterpFn<ushort> EvalCLUT;

        // (not-owned pointer)
        public InterpParams<ushort> CLUTparams;

        // Points to an array of curve evaluators in 16 bits (not-owned pointer)
        private readonly InterpFn<ushort>[] evalCurveOut16;

        // Points to an array of references to interpolation params (not-owned pointer)
        private readonly InterpParams<ushort>?[] paramsCurveOut16;

        public Span<InterpFn<ushort>> EvalCurveIn16 =>
            evalCurveIn16;

        public Span<InterpParams<ushort>?> ParamsCurveIn16 =>
            paramsCurveIn16;

        public Span<InterpFn<ushort>> EvalCurveOut16 =>
            evalCurveOut16;

        public Span<InterpParams<ushort>?> ParamsCurveOut16 =>
            paramsCurveOut16;

        public Prelin16Data(Context? context, uint numInputs, uint numOutputs)
        {
            ContextID = context;
            nInputs = numInputs;
            nOutputs = numOutputs;

            //var ifPool = Context.GetPool<InterpFn<ushort>>(context);
            //var ipPool = Context.GetPool<InterpParams<ushort>>(context);

            //evalCurveIn16 = ifPool.Rent((int)numInputs);
            //paramsCurveIn16 = ipPool.Rent((int)numInputs);

            //evalCurveOut16 = ifPool.Rent((int)numOutputs);
            //paramsCurveOut16 = ipPool.Rent((int)numOutputs);

            evalCurveIn16 = new InterpFn<ushort>[numInputs];
            paramsCurveIn16 = new InterpParams<ushort>[numInputs];

            evalCurveOut16 = new InterpFn<ushort>[numOutputs];
            paramsCurveOut16 = new InterpParams<ushort>[numOutputs];
        }

        public void Dispose()
        {
            //var ifPool = Context.GetPool<InterpFn<ushort>>(ContextID);
            //var ipPool = Context.GetPool<InterpParams<ushort>>(ContextID);

            //ReturnArray(ifPool, evalCurveIn16);
            //ReturnArray(ipPool, paramsCurveIn16);

            //ReturnArray(ifPool, evalCurveOut16);
            //ReturnArray(ipPool, paramsCurveOut16);

            GC.SuppressFinalize(this);
        }

        public object Clone()
        {
            var result = new Prelin16Data(ContextID, nInputs, nOutputs)
            {
                EvalCLUT = EvalCLUT, CLUTparams = CLUTparams
            };

            // EvalCurveIn16 and ParamsCurveIn16 excluded on purpose!!

            EvalCurveOut16.CopyTo(result.EvalCurveOut16);
            ParamsCurveOut16.CopyTo(result.ParamsCurveOut16);

            return result;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DOUBLE_TO_1FIXED14(double x) =>
        (int)Math.Floor((x * 16384.0) + 0.5);

    private sealed class MatShaper8Data(Context? context) : IDisposable, ICloneable
    {
        public readonly Context? ContextID = context;

        // from 0..255 to 1.14 (0.0...1.0)
        //private readonly int[] shaper1R = Context.GetPool<int>(context).Rent(256);
        //private readonly int[] shaper1G = Context.GetPool<int>(context).Rent(256);
        //private readonly int[] shaper1B = Context.GetPool<int>(context).Rent(256);
        private readonly int[] shaper1R = new int[256];
        private readonly int[] shaper1G = new int[256];
        private readonly int[] shaper1B = new int[256];


        // n.14 to n.14 (needs a saturation after that)
        //private readonly int[] mat = Context.GetPool<int>(context).Rent(3 * 3);
        private readonly int[] mat = new int[3 * 3];

        //private readonly int[] off = Context.GetPool<int>(context).Rent(3);
        private readonly int[] off = new int[3];

        // 1.14 to 0..255
        //private readonly ushort[] shaper2R = Context.GetPool<ushort>(context).Rent(16385);
        //private readonly ushort[] shaper2G = Context.GetPool<ushort>(context).Rent(16385);
        //private readonly ushort[] shaper2B = Context.GetPool<ushort>(context).Rent(16385);
        private readonly ushort[] shaper2R = new ushort[16385];
        private readonly ushort[] shaper2G = new ushort[16385];
        private readonly ushort[] shaper2B = new ushort[16385];

        public Span<int> Shaper1R =>
            shaper1R.AsSpan(..256);

        public Span<int> Shaper1G =>
            shaper1G.AsSpan(..256);

        public Span<int> Shaper1B =>
            shaper1B.AsSpan(..256);

        public Span<int> Mat =>
            mat.AsSpan(..(3 * 3));

        public Span<int> Off =>
            off.AsSpan(..3);

        public Span<ushort> Shaper2R =>
            shaper2R.AsSpan(..16385);

        public Span<ushort> Shaper2G =>
            shaper2G.AsSpan(..16385);

        public Span<ushort> Shaper2B =>
            shaper2B.AsSpan(..16385);

        public object Clone()
        {
            var result = new MatShaper8Data(ContextID);

            Shaper1R.CopyTo(result.Shaper1R);
            Shaper1G.CopyTo(result.Shaper1G);
            Shaper1B.CopyTo(result.Shaper1B);

            Mat.CopyTo(result.Mat);
            Off.CopyTo(result.Off);

            Shaper2R.CopyTo(result.Shaper2R);
            Shaper2G.CopyTo(result.Shaper2G);
            Shaper2B.CopyTo(result.Shaper2B);

            return result;
        }

        public void Dispose()
        {
            //var iPool = Context.GetPool<int>(ContextID);
            //var usPool = Context.GetPool<ushort>(ContextID);

            //ReturnArray(iPool, shaper1R);
            //ReturnArray(iPool, shaper1G);
            //ReturnArray(iPool, shaper1B);

            //ReturnArray(iPool, mat);
            //ReturnArray(iPool, off);

            //ReturnArray(usPool, shaper2R);
            //ReturnArray(usPool, shaper2G);
            //ReturnArray(usPool, shaper2B);

            GC.SuppressFinalize(this);
        }
    }

    private sealed class Curves16Data(Context? context, uint numCurves, uint numElements) : ICloneable
    {
        public readonly Context? ContextID = context;

        // Number of curves
        public readonly uint nCurves = numCurves;

        // Elements in curves
        public readonly uint nElements = numElements;

        // Points to a dynamically allocated array
        public readonly ushort[,] Curves = new ushort[numCurves, numElements];

        public object Clone()
        {
            var result = new Curves16Data(ContextID, nCurves, nElements);

            for (var i = 0; i < nCurves; i++)
            {
                for (var j = 0; j < nElements; j++)
                    result.Curves[i, j] = Curves[i, j];
            }

            return result;
        }
    }

    private static void _RemoveElement(ref Stage? head)
    {
        if (head is null)
            return;

        var mpe = head;
        head = mpe.Next;
        cmsStageFree(mpe);
    }

    private static bool _Remove1Op(Pipeline Lut, Signature UnaryOp)
    {
        ref var pt = ref Lut.Elements;
        var AnyOpt = false;

        while (pt is not null)
        {
            if (pt.Implements == UnaryOp)
            {
                _RemoveElement(ref pt);
                AnyOpt = true;
            }
            else
            {
                pt = ref pt.Next;
            }
        }

        return AnyOpt;
    }

    private static bool _Remove2Op(Pipeline Lut, Signature Op1, Signature Op2)
    {
        var AnyOpt = false;

        ref var pt1 = ref Lut.Elements;
        if (pt1 is null)
            return AnyOpt;

        while (pt1 is not null)
        {
            ref var pt2 = ref pt1.Next;
            if (pt2 is null)
                return AnyOpt;

            if (pt1.Implements == Op1 && pt2.Implements == Op2)
            {
                _RemoveElement(ref pt2);
                _RemoveElement(ref pt1);
                AnyOpt = true;
            }
            else
            {
                pt1 = ref pt1.Next;
            }
        }

        return AnyOpt;
    }

    private static bool CloseEnoughFloat(double a, double b) =>
        Math.Abs(b - a) < 1e-5f;

    private static bool isFloatMatrixIdentity(MAT3 a)
    {
        var Identity = MAT3.Identity;

        return CloseEnoughFloat(a.X.X, Identity.X.X) &&
               CloseEnoughFloat(a.X.Y, Identity.X.Y) &&
               CloseEnoughFloat(a.X.Z, Identity.X.Z) &&
               CloseEnoughFloat(a.Y.X, Identity.Y.X) &&
               CloseEnoughFloat(a.Y.Y, Identity.Y.Y) &&
               CloseEnoughFloat(a.Y.Z, Identity.Y.Z) &&
               CloseEnoughFloat(a.Z.X, Identity.Z.X) &&
               CloseEnoughFloat(a.Z.Y, Identity.Z.Y) &&
               CloseEnoughFloat(a.Z.Z, Identity.Z.Z);
    }

    private static bool _MultiplyMatrix(Pipeline Lut)
    {
        var AnyOpt = false;

        ref var pt1 = ref Lut.Elements;
        if (pt1 is null)
            return AnyOpt;

        while (pt1 is not null)
        {
            ref var pt2 = ref pt1.Next;
            if (pt2 is null)
                return AnyOpt;

            if (pt1.Implements == Signatures.Stage.MatrixElem && pt2.Implements == Signatures.Stage.MatrixElem)
            {
                // Get both matrices
                var m1 = (StageMatrixData)cmsStageData(pt1)!;
                var m2 = (StageMatrixData)cmsStageData(pt2)!;

                // Input offset and output offset should be zero to use this optimization
                if (m1.Offset is not null || m2.Offset is not null ||
                    cmsStageInputChannels(pt1) is not 3 || cmsStageOutputChannels(pt1) is not 3 ||
                    cmsStageInputChannels(pt2) is not 3 || cmsStageOutputChannels(pt2) is not 3)
                {
                    return false;
                }

                // Multiply both matrices to get the result
                var res = new MAT3(m2.Double) * new MAT3(m1.Double);

                // Get the next in chain after the matrices
                var chain = pt2.Next;

                // Remove both matrices
                _RemoveElement(ref pt2);
                _RemoveElement(ref pt1);

                // Now what if the result is a plain identity?
                if (!isFloatMatrixIdentity(res))
                {
                    // We can not get rid of full matrix
                    var ctx = Context.Get(Lut.ContextID);
                    //var pool = ctx.GetBufferPool<double>();
                    var resArray = res.AsArray( /*pool*/);
                    var Multmat = cmsStageAllocMatrix(Lut.ContextID, 3, 3, resArray, null);
                    //ReturnArray(pool, resArray);
                    if (Multmat is null)
                        return false;

                    // Recover the chain
                    Multmat.Next = chain;
                    pt1 = Multmat;
                }

                AnyOpt = true;
            }
            else
            {
                pt1 = ref pt1.Next;
            }
        }

        return AnyOpt;
    }

    private static bool PreOptimize(Pipeline Lut)
    {
        bool AnyOpt = false, Opt;

        do
        {
            Opt = false;

            // Remove all identities
            Opt |= _Remove1Op(Lut, Signatures.Stage.IdentityElem);

            // Remove XYZ2Lab followed by Lab2XYZ
            Opt |= _Remove2Op(Lut, Signatures.Stage.XYZ2LabElem, Signatures.Stage.Lab2XYZElem);

            // Remove Lab2XYZ followed by XYZ2Lab
            Opt |= _Remove2Op(Lut, Signatures.Stage.Lab2XYZElem, Signatures.Stage.XYZ2LabElem);

            // Remove V4 to V2 followed by V2 to V4
            Opt |= _Remove2Op(Lut, Signatures.Stage.LabV4toV2Elem, Signatures.Stage.LabV2toV4Elem);

            // Remove V2 to V4 followed by V4 to V2
            Opt |= _Remove2Op(Lut, Signatures.Stage.LabV2toV4Elem, Signatures.Stage.LabV4toV2Elem);

            // Remove float pcs Lab conversions
            Opt |= _Remove2Op(Lut, Signatures.Stage.Lab2FloatPCS, Signatures.Stage.FloatPCS2Lab);
            Opt |= _Remove2Op(Lut, Signatures.Stage.FloatPCS2Lab, Signatures.Stage.Lab2FloatPCS);

            // Simplify matrix.
            Opt |= _MultiplyMatrix(Lut);

            if (Opt)
                AnyOpt = true;
        } while (Opt);

        return AnyOpt;
    }

    private static void Eval16nop1D(ReadOnlySpan<ushort> Input, Span<ushort> Output, InterpParams<ushort> _)
    {
        Output[0] = Input[0];
    }

    private static void PrelinEval16(ReadOnlySpan<ushort> Input, Span<ushort> Output, object? D)
    {
        if (D is not Prelin16Data p16)
            return;

        Span<ushort> StageABC = stackalloc ushort[MAX_INPUT_DIMENSIONS];
        Span<ushort> StageDEF = stackalloc ushort[cmsMAXCHANNELS];

        for (var i = 0; i < p16.nInputs; i++)
            p16.EvalCurveIn16[i](Input[i..], StageABC[i..], p16.ParamsCurveIn16[i]);

        p16.EvalCLUT(StageABC, StageDEF, p16.CLUTparams);

        for (var i = 0; i < p16.nOutputs; i++)
            p16.EvalCurveIn16[i](StageDEF[i..], Output[i..], p16.ParamsCurveOut16[i]);
    }

    private static void PrelinOpt16free(Context? ContextID, object? ptr) =>

        //_cmsFree(ContextID, p16.EvalCurveOut16);
        //_cmsFree(ContextID, p16.ParamsCurveOut16);

        //_cmsFree(ContextID, p16);
        (ptr as Prelin16Data)?.Dispose();

    private static object? Prelin16dup(Context? ContextID, object? ptr) =>
        //var Duped = _cmsDupMem<Prelin16Data>(ContextID, p16);
        //if (Duped is null) return null;

        //Duped->EvalCurveOut16 = (InterpFn16*)_cmsDupMem(ContextID, p16.Ptr->EvalCurveOut16, p16.Ptr->nOutputs * _sizeof<nint>());
        //Duped->ParamsCurveOut16 = (InterpParams**)_cmsDupMem(ContextID, p16.Ptr->ParamsCurveOut16, p16.Ptr->nOutputs * _sizeof<nint>());
        (ptr as Prelin16Data)?.Clone();

    private static Prelin16Data? PrelinOpt16alloc(Context? ContextID,
                                                  InterpParams<ushort> ColorMap,
                                                  uint nInputs,
                                                  Span<ToneCurve> In,
                                                  uint nOutputs,
                                                  Span<ToneCurve> Out)
    {
        //var p16 = _cmsMallocZero<Prelin16Data>(ContextID);
        //if (p16 is null) return null;

        //p16->nInputs = nInputs;
        //p16->nOutputs = nOutputs;

        var p16 = new Prelin16Data(ContextID, nInputs, nOutputs);

        for (var i = 0; i < nInputs; i++)
        {
            if (In.IsEmpty)
            {
                p16.ParamsCurveIn16[i] = null;
                p16.EvalCurveIn16[i] = Eval16nop1D;
            }
            else
            {
                p16.ParamsCurveIn16[i] = In[i].InterpParams;
                p16.EvalCurveIn16[i] = p16.ParamsCurveIn16[i].Interpolation.Lerp16;
            }
        }

        p16.CLUTparams = ColorMap;
        p16.EvalCLUT = ColorMap.Interpolation.Lerp16;

        //p16.EvalCurveOut16 = (InterpFn16*)_cmsCalloc(ContextID, nOutputs, _sizeof<nint>());
        //if (p16.EvalCurveOut16 is null)
        //{
        //    _cmsFree(ContextID, p16);
        //    return null;
        //}

        //p16.ParamsCurveOut16 = _cmsCalloc2<InterpParams>(ContextID, nOutputs);
        //if (p16.ParamsCurveOut16 is null)
        //{
        //    _cmsFree(ContextID, p16.EvalCurveOut16);
        //    _cmsFree(ContextID, p16);
        //    return null;
        //}

        for (var i = 0; i < nOutputs; i++)
        {
            if (Out.IsEmpty)
            {
                p16.ParamsCurveOut16[i] = null;
                p16.EvalCurveOut16[i] = Eval16nop1D;
            }
            else
            {
                p16.ParamsCurveOut16[i] = Out[i].InterpParams;
                p16.EvalCurveOut16[i] = p16.ParamsCurveOut16[i].Interpolation;
            }
        }

        return p16;
    }

    private const uint PRELINEARIZATION_POINTS = 4096;

    private static bool XFormSampler16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        if (Cargo is not Pipeline Lut)
            return false;

        Span<float> InFloat = stackalloc float[cmsMAXCHANNELS];
        Span<float> OutFloat = stackalloc float[cmsMAXCHANNELS];

        _cmsAssert(
            Lut.InputChannels < cmsMAXCHANNELS,
            Lut.OutputChannels < cmsMAXCHANNELS);

        // From 16 bit to floating point
        for (var i = 0; i < Lut.InputChannels; i++)
            InFloat[i] = (float)(In[i] / 65535.0);

        // Evaluate in floating point
        cmsPipelineEvalFloat(InFloat, OutFloat, Lut);

        // Back to 16 bit representation
        for (var i = 0; i < Lut.OutputChannels; i++)
            Out[i] = _cmsQuickSaturateWord(OutFloat[i] * 65535.0);

        // Always succeed
        return true;
    }

    private static bool AllCurvesAreLinear(Stage mpe)
    {
        var Curves = _cmsStageGetPtrToCurveSet(mpe);
        if (Curves.IsEmpty)
            return false;

        var n = cmsStageOutputChannels(mpe);

        for (var i = 0; i < n; i++)
            if (!cmsIsToneCurveLinear(Curves[i]))
                return false;

        return true;
    }

    private static bool PatchLUT(Stage? CLUT,
                                 ReadOnlySpan<ushort> At,
                                 ReadOnlySpan<ushort> Value,
                                 uint nChannelsOut,
                                 uint nChannelsIn)
    {
        if (CLUT is null || CLUT.Data is not StageCLutData<ushort> Grid)
            return false;
        var p16 = Grid.Params;
        double px, py, pz, pw;
        int x0, y0, z0, w0, index;

        if (CLUT.Type != Signatures.Stage.CLutElem)
        {
            LogError(CLUT.ContextID, cmsERROR_INTERNAL, "(internal) Attempt to PatchLUT on non-lut stage");
            return false;
        }

        switch (nChannelsIn)
        {
            case 4:
            {
                px = (double)At[0] * p16.Domain[0] / 65535.0;
                py = (double)At[1] * p16.Domain[1] / 65535.0;
                pz = (double)At[2] * p16.Domain[2] / 65535.0;
                pw = (double)At[3] * p16.Domain[3] / 65535.0;

                x0 = (int)Math.Floor(px);
                y0 = (int)Math.Floor(py);
                z0 = (int)Math.Floor(pz);
                w0 = (int)Math.Floor(pw);

                if (((px - x0) is not 0) ||
                    ((py - y0) is not 0) ||
                    ((pz - z0) is not 0) ||
                    ((pw - w0) is not 0))
                {
                    return false; // Not on exact node
                }

                index = ((int)p16.opta[3] * x0) +
                        ((int)p16.opta[2] * y0) +
                        ((int)p16.opta[1] * z0) +
                        ((int)p16.opta[0] * w0);
            }
                break;

            case 3:
            {
                px = (double)At[0] * p16.Domain[0] / 65535.0;
                py = (double)At[1] * p16.Domain[1] / 65535.0;
                pz = (double)At[2] * p16.Domain[2] / 65535.0;

                x0 = (int)Math.Floor(px);
                y0 = (int)Math.Floor(py);
                z0 = (int)Math.Floor(pz);

                if (((px - x0) is not 0) ||
                    ((py - y0) is not 0) ||
                    ((pz - z0) is not 0))
                {
                    return false; // Not on exact node
                }

                index = ((int)p16.opta[2] * x0) +
                        ((int)p16.opta[1] * y0) +
                        ((int)p16.opta[0] * z0);
            }
                break;

            case 1:
            {
                px = (double)At[0] * p16.Domain[0] / 65535.0;

                x0 = (int)Math.Floor(px);

                if ((px - x0) is not 0)
                {
                    return false; // Not on exact node
                }

                index = (int)p16.opta[0] * x0;
            }
                break;

            default:
                LogError(
                    CLUT.ContextID,
                    cmsERROR_INTERNAL,
                    $"(internal) {nChannelsIn} Channels are not supported on PatchLUT");
                return false;
        }

        for (var i = 0; i < nChannelsOut; i++)
            Grid.Tab[index + i] = Value[i];

        return true;
    }

    private static bool WhitesAreEqual(uint n, ReadOnlySpan<ushort> White1, ReadOnlySpan<ushort> White2)
    {
        for (var i = 0; i < n; i++)
        {
            if (Math.Abs(White1[i] - White2[i]) > 0xF000)
                return true;  // Values are so extremely different that the fixup should be avoided
            if (White1[i] != White2[i])
                return false;
        }

        return true;
    }

    private static bool FixWhiteMisalignment(Pipeline Lut, Signature EntryColorSpace, Signature ExitColorSpace)
    {
        Span<ushort> WhiteIn = stackalloc ushort[cmsMAXCHANNELS];
        Span<ushort> WhiteOut = stackalloc ushort[cmsMAXCHANNELS];
        Span<ushort> ObtainedOut = stackalloc ushort[cmsMAXCHANNELS];

        if (!_cmsEndPointsBySpace(EntryColorSpace, out var WhitePointIn, out _, out var nIns))
            return false;

        if (!_cmsEndPointsBySpace(ExitColorSpace, out var WhitePointOut, out _, out var nOuts))
            return false;

        // It needs to be fixed?
        if (Lut.InputChannels != nIns)
            return false;
        if (Lut.OutputChannels != nOuts)
            return false;

        cmsPipelineEval16(WhitePointIn, ObtainedOut, Lut);

        if (WhitesAreEqual(nOuts, WhitePointOut, ObtainedOut))
            return true;    // Whites already match

        // Check if the LUT comes as Prelin, CLUT or Postlin. We allow all combinations
        if (!cmsPipelineCheckAndRetrieveStages(
                Lut,
                Signatures.Stage.CurveSetElem,
                out Stage? PreLin,
                Signatures.Stage.CLutElem,
                out Stage? CLUT,
                Signatures.Stage.CurveSetElem,
                out Stage? PostLin) &&
            !cmsPipelineCheckAndRetrieveStages(
                Lut,
                Signatures.Stage.CurveSetElem,
                out PreLin,
                Signatures.Stage.CLutElem,
                out CLUT) &&
            !cmsPipelineCheckAndRetrieveStages(
                Lut,
                Signatures.Stage.CLutElem,
                out CLUT,
                Signatures.Stage.CurveSetElem,
                out PostLin) &&
            !cmsPipelineCheckAndRetrieveStages(Lut, Signatures.Stage.CLutElem, out CLUT))
        {
            return false;
        }

        // We need to interpolate white points of both, pre and post curves
        if (PreLin is not null)
        {
            var Curves = _cmsStageGetPtrToCurveSet(PreLin);

            for (var i = 0; i < nIns; i++)
                WhiteIn[i] = cmsEvalToneCurve16(Curves[i], WhitePointIn[i]);
        }
        else
        {
            for (var i = 0; i < nIns; i++)
                WhiteIn[i] = WhitePointIn[i];
        }

        // If any post-linearization, we need to find how it represented white before the curve, do
        // a reverse interpolation in this case
        if (PostLin is not null)
        {
            var Curves = _cmsStageGetPtrToCurveSet(PostLin);

            for (var i = 0; i < nOuts; i++)
            {
                var InversePostLin = cmsReverseToneCurve(Curves[i]);
                if (InversePostLin is null)
                {
                    WhiteOut[i] = WhitePointOut[i];
                }
                else
                {
                    WhiteOut[i] = cmsEvalToneCurve16(InversePostLin, WhitePointOut[i]);
                    cmsFreeToneCurve(InversePostLin);
                }
            }
        }
        else
        {
            for (var i = 0; i < nOuts; i++)
                WhiteOut[i] = WhitePointOut[i];
        }

        // Ok, proceed with patching. May fail and we don't care if it fails
        PatchLUT(CLUT, WhiteIn, WhiteOut, nOuts, nIns);

        return true;
    }

    private static bool OptimizeByResampling(ref Pipeline Lut,
                                             uint Intent,
                                             ref uint InputFormat,
                                             ref uint OutputFormat,
                                             ref uint dwFlags)
    {
        Pipeline? Src = null, Dest = null;
        Stage? KeepPreLin = null, KeepPostLin = null;
        Stage? NewPreLin = null, NewPostLin = null;

        // This is a lossy optimization! does not apply in floating-point cases
        if (_cmsFormatterIsFloat(InputFormat) || _cmsFormatterIsFloat(OutputFormat))
            return false;

        var ColorSpace = _cmsICCcolorSpace((int)T_COLORSPACE(InputFormat));
        var OutputColorSpace = _cmsICCcolorSpace((int)T_COLORSPACE(OutputFormat));

        // Color space must be specified
        if ((uint)ColorSpace is 0 ||
            (uint)OutputColorSpace is 0)
        {
            return false;
        }

        var nGridPoints = _cmsReasonableGridpointsByColorspace(ColorSpace, dwFlags);

        // For empty LUTs, 2 points are enough
        if (cmsPipelineStageCount(Lut) is 0)
            nGridPoints = 2;

        Src = Lut;

        // Allocate an empty LUT
        Dest = cmsPipelineAlloc(Src.ContextID, Src.InputChannels, Src.OutputChannels);
        if (Dest is null)
            return false;

        // Prelinearization tables are kept unless indicated by flags
        if ((dwFlags & cmsFLAGS_CLUT_PRE_LINEARIZATION) is not 0)
        {
            // Get a pointer to the prelinearization element
            var PreLin = cmsPipelineGetPtrToFirstStage(Src);

            // Check if suitable
            if (PreLin is not null && PreLin.Type == Signatures.Stage.CurveSetElem)
            {
                // Maybe this is a linear tram, so we can avoid the whole stuff
                if (!AllCurvesAreLinear(PreLin))
                {
                    // All seems ok, proceed.
                    NewPreLin = cmsStageDup(PreLin);
                    if (!cmsPipelineInsertStage(Dest, StageLoc.AtBegin, NewPreLin))
                        goto Error;

                    // Remove prelinearization. Since we have duplicated the curve
                    // in destination LUT, the sampling should be applied after this stage.
                    cmsPipelineUnlinkStage(Src, StageLoc.AtBegin, out KeepPreLin);
                }
            }
        }

        // Allocate the CLUT
        var CLUT = cmsStageAllocCLut16bit(Src.ContextID, nGridPoints, Src.InputChannels, Src.OutputChannels, null);
        if (CLUT is null)
            goto Error;

        // Add the CLUT to the destination LUT
        if (!cmsPipelineInsertStage(Dest, StageLoc.AtEnd, CLUT))
            goto Error;

        // Postlinearization tables are kept unless indicated by flags
        if ((dwFlags & cmsFLAGS_CLUT_POST_LINEARIZATION) is not 0)
        {
            // Get a pointer to the postlinearization if present
            var PostLin = cmsPipelineGetPtrToLastStage(Src);

            // Check if suitable
            if (PostLin is not null && cmsStageType(PostLin) == Signatures.Stage.CurveSetElem)
            {
                // Maybe this is a linear tran, so we can avoid the whole stuff
                if (!AllCurvesAreLinear(PostLin))
                {
                    // All seems ok, proceed.
                    NewPostLin = cmsStageDup(PostLin);
                    if (!cmsPipelineInsertStage(Dest, StageLoc.AtEnd, NewPostLin))
                        goto Error;

                    // In destination LUT, the sampling should be applied after this stage.
                    cmsPipelineUnlinkStage(Src, StageLoc.AtEnd, out KeepPostLin);
                }
            }
        }

        // Now its time to do the sampling. We have to ignore pre/post linearization
        // The source LUT without pre/post curves is passed as parameter.
        if (!cmsStageSampleCLut16bit(CLUT, XFormSampler16, Src, 0))
            goto Error;

        // Done.

        if (KeepPreLin is not null)
            cmsStageFree(KeepPreLin);
        if (KeepPostLin is not null)
            cmsStageFree(KeepPostLin);
        cmsPipelineFree(Src);

        var DataCLUT = (StageCLutData<ushort>)CLUT.Data!;

        var DataSetIn = (NewPreLin?.Data as StageToneCurvesData)?.TheCurves;
        var DataSetOut = (NewPostLin?.Data as StageToneCurvesData)?.TheCurves;

        if (DataSetIn is null && DataSetOut is null)
        {
            Dest.SetOptimizationParameters(
                (ReadOnlySpan<ushort> i, Span<ushort> o, object? p) =>
                {
                    if (p is not InterpParams<ushort> ptr)
                        return;
                    DataCLUT.Params.Interpolation.Lerp16?.Invoke(i, o, ptr);
                },
                DataCLUT.Params,
                null,
                null);
        }
        else
        {
            var p16 = PrelinOpt16alloc(
                Dest.ContextID,
                DataCLUT.Params,
                Dest.InputChannels,
                DataSetIn,
                Dest.OutputChannels,
                DataSetOut);

            Dest.SetOptimizationParameters(PrelinEval16, p16, PrelinOpt16free, Prelin16dup);
        }

        // Don't fix white on absolute colorimentric
        if (Intent is INTENT_ABSOLUTE_COLORIMETRIC)
            dwFlags |= cmsFLAGS_NOWHITEONWHITEFIXUP;

        if ((dwFlags & cmsFLAGS_NOWHITEONWHITEFIXUP) is 0)
            FixWhiteMisalignment(Dest, ColorSpace, OutputColorSpace);

        Lut = Dest;
        return true;
    Error:
        // Ops, something went wrong, Restore stages
        if (KeepPreLin is not null)
        {
            if (!cmsPipelineInsertStage(Src, StageLoc.AtBegin, KeepPreLin))
                _cmsAssert(false);
        }

        if (KeepPostLin is not null)
        {
            if (!cmsPipelineInsertStage(Src, StageLoc.AtEnd, KeepPostLin))
                _cmsAssert(false);
        }

        cmsPipelineFree(Dest);
        return false;
    }

    private static void SlopeLimiting(ToneCurve g)
    {
        var AtBegin = (int)Math.Floor((g.nEntries * 0.02) + 0.5); // Cutoff at 2%
        var AtEnd = (int)g.nEntries - AtBegin - 1;                // And 98%

        var (BeginVal, EndVal) = cmsIsToneCurveDescending(g) ? (0xFFFF, 0) : (0, 0xFFFF);

        // Compute slope and offset for begin of curve
        var Val = (double)g.Table16[AtBegin];
        var Slope = (Val - BeginVal) / AtBegin;
        var beta = Val - (Slope * AtBegin);

        for (var i = 0; i < AtBegin; i++)
            g.Table16[i] = _cmsQuickSaturateWord((i * Slope) + beta);

        // Compute slope and offset for the end
        Val = g.Table16[AtEnd];
        Slope = (EndVal - Val) / AtBegin;   // AtBegin holds the X interval, which is the same in both cases
        beta = Val - (Slope * AtEnd);

        for (var i = AtEnd; i < g.nEntries; i++)
            g.Table16[i] = _cmsQuickSaturateWord((i * Slope) + beta);
    }

    private static Prelin8Data? PrelinOpt8alloc(Context? ContextID, InterpParams<ushort> p, Span<ToneCurve> G)
    {
        Span<ushort> Input = stackalloc ushort[3];

        //var p8 = _cmsMallocZero<Prelin8Data>(ContextID);
        //if (p8 is null) return null;

        var p8 = new Prelin8Data(ContextID, p);

        // Since this only works for 8 bit input, values always come as x * 257,
        // we can safely take msb byte (x << 8 + x)

        for (var i = 0; i < 256; i++)
        {
            if (!G.IsEmpty)
            {
                // Get 16-bit representation
                Input[0] = cmsEvalToneCurve16(G[0], FROM_8_TO_16((uint)i));
                Input[1] = cmsEvalToneCurve16(G[1], FROM_8_TO_16((uint)i));
                Input[2] = cmsEvalToneCurve16(G[2], FROM_8_TO_16((uint)i));
            }
            else
            {
                Input[0] = FROM_8_TO_16((uint)i);
                Input[1] = FROM_8_TO_16((uint)i);
                Input[2] = FROM_8_TO_16((uint)i);
            }

            // Move to 0..1.0 in fixed domain
            var v1 = _cmsToFixedDomain((int)(Input[0] * p.Domain[0]));
            var v2 = _cmsToFixedDomain((int)(Input[1] * p.Domain[1]));
            var v3 = _cmsToFixedDomain((int)(Input[2] * p.Domain[2]));

            // Store the precalculated table of nodes
            p8.X0[i] = p.opta[0] * (uint)FIXED_TO_INT(v1);
            p8.Y0[i] = p.opta[1] * (uint)FIXED_TO_INT(v2);
            p8.Z0[i] = p.opta[2] * (uint)FIXED_TO_INT(v3);

            // Store the precalculated table of offsets
            p8.rx[i] = (ushort)FIXED_REST_TO_INT(v1);
            p8.ry[i] = (ushort)FIXED_REST_TO_INT(v2);
            p8.rz[i] = (ushort)FIXED_REST_TO_INT(v3);
        }

        return p8;
    }

    private static void Prelin8free(Context? _, object? ptr) =>
        //_cmsFree(ContextID, ptr as BoxPtr<Prelin16Data>);
        (ptr as Prelin8Data)?.Dispose();

    private static object? Prelin8dup(Context? _, object? ptr) =>
        (ptr as Prelin8Data)?.Clone();

    private static void PrelinEval8(ReadOnlySpan<ushort> Input, Span<ushort> Output, object? D)
    {
        int c0, c1, c2, c3;

        if (D is not Prelin8Data p8)
            return;

        var p = p8.p;
        var TotalOut = (int)p.nOutputs;
        var tab = p.Table;

        var r = (byte)(Input[0] >> 8);
        var g = (byte)(Input[1] >> 8);
        var b = (byte)(Input[2] >> 8);

        var X0 = (int)p8.X0[r];
        var Y0 = (int)p8.Y0[g];
        var Z0 = (int)p8.Z0[b];

        var rx = p8.rx[r];
        var ry = p8.ry[g];
        var rz = p8.rz[b];

        var X1 = X0 + (int)((rx is 0) ? 0 : p.opta[2]);
        var Y1 = Y0 + (int)((ry is 0) ? 0 : p.opta[1]);
        var Z1 = Z0 + (int)((rz is 0) ? 0 : p.opta[0]);

        // These are the 6 Tetrahedrals
        for (var OutChan = 0; OutChan < TotalOut; OutChan++)
        {
            ushort DENS(int i, int j, int k) =>
                tab.Span[i + j + k + OutChan];

            c0 = DENS(X0, Y0, Z0);

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

            var Rest = (c1 * rx) + (c2 * ry) + (c3 * rz) + 0x8001;
            Output[OutChan] = (ushort)(c0 + ((Rest + (Rest >> 16)) >> 16));
        }
    }

    private static bool IsDegenerated(ToneCurve g)
    {
        uint Zeros = 0, Poles = 0;
        var nEntries = g.nEntries;

        for (var i = 0; i < nEntries; i++)
        {
            if (g.Table16[i] is 0x0000)
                Zeros++;
            if (g.Table16[i] is 0xFFFF)
                Poles++;
        }

        if (Zeros is 1 && Poles is 1)
            return false; // For linear tables
        if (Zeros > (nEntries / 20))
            return true;   // Degenerated, many zeros
        if (Poles > (nEntries / 20))
            return true;   // Degenerated, many poles

        return false;
    }

    private static bool OptimizeByComputingLinearization(ref Pipeline Lut,
                                                         uint Intent,
                                                         ref uint InputFormat,
                                                         ref uint OutputFormat,
                                                         ref uint dwFlags)
    {
        //var pool = Context.GetPool<ToneCurve>(Lut.ContextID);
        Span<float> In = stackalloc float[cmsMAXCHANNELS];
        Span<float> Out = stackalloc float[cmsMAXCHANNELS];
        Pipeline? OptimizedLUT = null, LutPlusCurves = null;

        // This is a lossy optimization! does not apply in floating-point cases
        if (_cmsFormatterIsFloat(InputFormat) || _cmsFormatterIsFloat(OutputFormat))
            return false;

        // Only on chunky RGB
        if (T_COLORSPACE(InputFormat) is not PT_RGB)
            return false;
        if (T_PLANAR(InputFormat) is not 0)
            return false;

        if (T_COLORSPACE(OutputFormat) is not PT_RGB)
            return false;
        if (T_PLANAR(OutputFormat) is not 0)
            return false;

        // On 16 bits, user has to specify the feature
        if (!_cmsFormatterIs8bit(InputFormat) && (dwFlags & cmsFLAGS_CLUT_PRE_LINEARIZATION) is 0)
            return false;

        var OriginalLut = Lut;

        var ColorSpace = _cmsICCcolorSpace((int)T_COLORSPACE(InputFormat));
        var OutputColorSpace = _cmsICCcolorSpace((int)T_COLORSPACE(OutputFormat));

        // Color space must be specified
        if ((uint)ColorSpace is 0 || (uint)OutputColorSpace is 0)
            return false;

        var nGridPoints = _cmsReasonableGridpointsByColorspace(ColorSpace, dwFlags);

        // Empty gamma containers
        //memset(Trans, 0, _sizeof<nint>() * cmsMAXCHANNELS);
        //memset(TransReverse, 0, _sizeof<nint>() * cmsMAXCHANNELS);
        //ToneCurve[] Trans = pool.Rent(cmsMAXCHANNELS);
        //ToneCurve[] TransReverse = pool.Rent(cmsMAXCHANNELS);
        var Trans = new ToneCurve[cmsMAXCHANNELS];
        var TransReverse = new ToneCurve[cmsMAXCHANNELS];

        // If the last stage of the original lut are curves, and those curves are
        // degenerated, it is likely the transform is squeezing and clipping
        // the output from previous CLUT. We cannot optimize this case
        {
            var last = cmsPipelineGetPtrToLastStage(OriginalLut);

            if (last is null)
                goto Error;
            if (cmsStageType(last) == Signatures.Stage.CurveSetElem)
            {
                var Data = cmsStageData(last) as StageToneCurvesData;
                for (var i = 0; i < Data?.nCurves; i++)
                {
                    if (IsDegenerated(Data.TheCurves[i]))
                        goto Error;
                }
            }
        }

        for (var t = 0; t < OriginalLut.InputChannels; t++)
        {
            Trans[t] = cmsBuildTabulatedToneCurve16(OriginalLut.ContextID, PRELINEARIZATION_POINTS, null)!;
            if (Trans[t] is null)
                goto Error;
        }

        // Populate the curves
        for (var i = 0; i < PRELINEARIZATION_POINTS; i++)
        {
            var v = (float)((double)i / (PRELINEARIZATION_POINTS - 1));

            // Feed input with a gray ramp
            for (var t = 0; t < OriginalLut.InputChannels; t++)
            {
                var curve = Trans[t];
                if (curve.Table16 is not null)
                    curve.Table16[i] = _cmsQuickSaturateWord(Out[t] * 65535.0);
            }
        }

        // Slope-limit the obtained curves
        for (var t = 0; t < OriginalLut.InputChannels; t++)
            SlopeLimiting(Trans[t]);

        // Check for validity.
        var lIsSuitable = true;
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        // lIsLinear is here for debug purposes
        var lIsLinear = true;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
        for (var t = 0; (lIsSuitable && (t < OriginalLut.InputChannels)); t++)
        {
            // Exclude if already linear
            if (!cmsIsToneCurveLinear(Trans[t]))
                lIsLinear = false;

            // Exclude if non-monotonic
            if (!cmsIsToneCurveMonotonic(Trans[t]))
                lIsSuitable = false;

            if (IsDegenerated(Trans[t]))
                lIsSuitable = false;
        }

        // If it is not suitable, just quit
        if (!lIsSuitable)
            goto Error;

        // Invert curves if possible
        for (var t = 0; t < OriginalLut.InputChannels; t++)
        {
            TransReverse[t] = cmsReverseToneCurveEx(PRELINEARIZATION_POINTS, Trans[t]);
            if (TransReverse[t] is null)
                goto Error;
        }

        // Now inset the reversed curves at the begin of transform
        LutPlusCurves = cmsPipelineDup(OriginalLut);
        if (LutPlusCurves is null)
            goto Error;

        if (!cmsPipelineInsertStage(
                LutPlusCurves,
                StageLoc.AtBegin,
                cmsStageAllocToneCurves(OriginalLut.ContextID, OriginalLut.InputChannels, TransReverse)))
            goto Error;

        // Create the result LUT
        OptimizedLUT = cmsPipelineAlloc(OriginalLut.ContextID, OriginalLut.InputChannels, OriginalLut.OutputChannels);
        if (OptimizedLUT is null)
            goto Error;

        var OptimizedPrelinMpe = cmsStageAllocToneCurves(OriginalLut.ContextID, OriginalLut.InputChannels, Trans);

        // Create and insert the curves at the beginning
        if (!cmsPipelineInsertStage(OptimizedLUT, StageLoc.AtBegin, OptimizedPrelinMpe))
            goto Error;

        // Allocate the CLUT for result
        var OptimizedCLUTmpe = cmsStageAllocCLut16bit(
            OriginalLut.ContextID,
            nGridPoints,
            OriginalLut.InputChannels,
            OriginalLut.OutputChannels,
            null);

        // Add the CLUT to the destination LUT
        if (!cmsPipelineInsertStage(OptimizedLUT, StageLoc.AtEnd, OptimizedCLUTmpe))
            goto Error;

        // Resample the LUT
        if (!cmsStageSampleCLut16bit(OptimizedCLUTmpe, XFormSampler16, LutPlusCurves, 0))
            goto Error;

        // Free resources
        for (var t = 0; t < OriginalLut.InputChannels; t++)
        {
            if (Trans[t] is not null)
                cmsFreeToneCurve(Trans[t]);
            if (TransReverse[t] is not null)
                cmsFreeToneCurve(TransReverse[t]);
        }

        cmsPipelineFree(LutPlusCurves);

        var OptimizedPrelinCurves = _cmsStageGetPtrToCurveSet(OptimizedPrelinMpe);
        if (OptimizedCLUTmpe.Data is not StageCLutData<ushort> OptimizedPrelinCLUT)
            return false;

        // Set the evaluator if 8-bit
        if (_cmsFormatterIs8bit(InputFormat))
        {
            var p8 = PrelinOpt8alloc(OptimizedLUT.ContextID, OptimizedPrelinCLUT.Params, OptimizedPrelinCurves);
            if (p8 is null)
                goto Error;

            OptimizedLUT.SetOptimizationParameters(PrelinEval8, p8, Prelin8free, Prelin8dup);
        }
        else
        {
            var p16 = PrelinOpt16alloc(
                OptimizedLUT.ContextID,
                OptimizedPrelinCLUT.Params,
                3,
                OptimizedPrelinCurves,
                3,
                null);
            if (p16 is null)
                goto Error;

            OptimizedLUT.SetOptimizationParameters(PrelinEval16, p16, PrelinOpt16free, Prelin16dup);
        }

        // Don't fix white on absolute colorimetric
        if (Intent is INTENT_ABSOLUTE_COLORIMETRIC)
            dwFlags |= cmsFLAGS_NOWHITEONWHITEFIXUP;

        if ((dwFlags & cmsFLAGS_NOWHITEONWHITEFIXUP) is 0)
            if (!FixWhiteMisalignment(OptimizedLUT, ColorSpace, OutputColorSpace))
                goto Error;

        // And return the obtained LUT

        cmsPipelineFree(OriginalLut);
        Lut = OptimizedLUT;
        //ReturnArray(pool, TransReverse);
        //ReturnArray(pool, Trans);
        return true;

    Error:
        for (var t = 0; t < OriginalLut.InputChannels; t++)
        {
            if (Trans[t] is not null)
                cmsFreeToneCurve(Trans[t]);
            if (TransReverse[t] is not null)
                cmsFreeToneCurve(TransReverse[t]);
        }
        //ReturnArray(pool, TransReverse);
        //ReturnArray(pool, Trans);

        if (LutPlusCurves is not null)
            cmsPipelineFree(LutPlusCurves);
        if (OptimizedLUT is not null)
            cmsPipelineFree(OptimizedLUT);

        return false;
    }

    private static void CurvesFree(Context? ContextID, object? ptr)
    {
        //if (ptr is not Curves16Data Data)
        //    return;

        //for (var i = 0; i < Data.Ptr->nCurves; i++)
        //    _cmsFree(ContextID, Data.Ptr->Curves[i]);

        //_cmsFree(ContextID, Data.Ptr->Curves);
        //_cmsFree(ContextID, Data.Ptr);
    }

    private static Curves16Data? CurvesDup(Context? ContextID, object? ptr) =>
        //if (ptr is not Curves16Data curves)
        //    return null;

        //var Data = _cmsDupMem<Curves16Data>(ContextID, curves);

        //if (Data is null) return null;

        //Data->Curves = _cmsDupMem2<ushort>(ContextID, Data->Curves, Data->nCurves);

        //for (var i = 0; i < Data->nCurves; i++)
        //    Data->Curves[i] = _cmsDupMem<ushort>(ContextID, Data->Curves[i], Data->nElements);

        //return Data;
        (ptr as Curves16Data)?.Clone() as Curves16Data;

    private static Curves16Data? CurvesAlloc(Context? ContextID, uint nCurves, uint nElements, ToneCurve[] G)
    {
        int i;
        //var c16 = _cmsMallocZero<Curves16Data>(ContextID);
        //if (c16 is null) return null;

        //c16->nCurves = nCurves;
        //c16->nElements = nElements;

        //c16->Curves = _cmsCalloc2<ushort>(ContextID, nCurves);
        //if (c16 is null) /*goto Error1;*/
        //{
        //    _cmsFree(ContextID, c16);
        //    return null;
        //}

        var c16 = new Curves16Data(ContextID, nCurves, nElements);

        for (i = 0; i < nCurves; i++)
        {
            //c16->Curves[i] = _cmsCalloc<ushort>(ContextID, nElements);

            //if (c16->Curves[i] is null) /*goto Error2;*/
            //{
            //    for (var j = 0; j < i; j++)
            //        _cmsFree(ContextID, c16->Curves[j]);
            //    _cmsFree(ContextID, c16->Curves);
            //    _cmsFree(ContextID, c16);
            //    return null;
            //}

            if (nElements is 256)
            {
                for (var j = 0; j < nElements; j++)
                    c16.Curves[i, j] = cmsEvalToneCurve16(G[i], FROM_8_TO_16((uint)j));
            }
            else
            {
                for (var j = 0; j < nElements; j++)
                    c16.Curves[i, j] = cmsEvalToneCurve16(G[i], (ushort)j);
            }
        }

        return c16;

        //Error2:
        //    for (var j = 0; j < i; j++)
        //        _cmsFree(ContextID, c16->Curves[j]);
        //    _cmsFree(ContextID, c16->Curves);
        //Error1:
        //    _cmsFree(ContextID, c16);
        //    return null;
    }

    private static void FastEvaluateCurves8(ReadOnlySpan<ushort> In, Span<ushort> Out, object? D)
    {
        if (D is not Curves16Data Data)
            return;

        for (var i = 0; i < Data.nCurves; i++)
        {
            var x = In[i] >> 8;
            Out[i] = Data.Curves[i, x];
        }
    }

    private static void FastEvaluateCurves16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? D)
    {
        if (D is not Curves16Data Data)
            return;

        for (var i = 0; i < Data.nCurves; i++)
            Out[i] = Data.Curves[i, In[i]];
    }

    private static void FastIdentify16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? D)
    {
        if (D is not Pipeline Lut)
            return;

        for (var i = 0; i < Lut.InputChannels; i++)
            Out[i] = In[i];
    }

    private static bool OptimizeByJoiningCurves(ref Pipeline Lut,
                                                uint _,
                                                ref uint InputFormat,
                                                ref uint OutputFormat,
                                                ref uint dwFlags)
    {
        Span<float> InFloat = stackalloc float[cmsMAXCHANNELS];
        Span<float> OutFloat = stackalloc float[cmsMAXCHANNELS];

        Stage? ObtainedCurves = null;
        Pipeline? Dest = null;
        var Src = Lut;

        // This is a lossy optimization! does not apply in floating-point cases
        if (_cmsFormatterIsFloat(InputFormat) || _cmsFormatterIsFloat(OutputFormat))
            return false;

        // Only curves in this LUT?
        for (var mpe = cmsPipelineGetPtrToFirstStage(Src);
             mpe is not null;
             mpe = cmsStageNext(mpe))
        {
            if (cmsStageType(mpe) != Signatures.Stage.CurveSetElem)
                return false;
        }

        // Allocate an empty LUT
        Dest = cmsPipelineAlloc(Src.ContextID, Src.InputChannels, Src.OutputChannels);
        if (Dest is null)
            return false;

        //// Create target curves
        //GammaTables = _cmsCalloc2<ToneCurve>(Src.ContextID, Src.InputChannels);
        //if (GammaTables is null) goto Error;
        //var pool = Context.GetPool<ToneCurve>(Src.ContextID);
        //ToneCurve[] GammaTables = pool.Rent((int)Src.InputChannels);
        var GammaTables = new ToneCurve[Src.InputChannels];

        for (var i = 0; i < Src.InputChannels; i++)
        {
            GammaTables[i] = cmsBuildTabulatedToneCurve16(Src.ContextID, PRELINEARIZATION_POINTS, null)!;
            if (GammaTables[i] is null)
                goto Error;
        }

        // Compute 16 bit result by using floating point
        for (var i = 0; i < PRELINEARIZATION_POINTS; i++)
        {
            for (var j = 0; j < Src.InputChannels; j++)
                InFloat[j] = (float)((double)i / (PRELINEARIZATION_POINTS - 1));

            cmsPipelineEvalFloat(InFloat, OutFloat, Src);

            for (var j = 0; j < Src.InputChannels; j++)
                GammaTables[j].Table16[i] = _cmsQuickSaturateWord(OutFloat[j] * 65535.0);
        }

        ObtainedCurves = cmsStageAllocToneCurves(Src.ContextID, Src.InputChannels, GammaTables);
        if (ObtainedCurves is null)
            goto Error;

        for (var i = 0; i < Src.InputChannels; i++)
        {
            cmsFreeToneCurve(GammaTables[i]);
            GammaTables[i] = null!;
        }

        if (GammaTables is not null)
        {
            //ReturnArray(Src.ContextID, GammaTables);
            GammaTables = null!;
        }

        // Maybe the curves are linear at the end
        if (!AllCurvesAreLinear(ObtainedCurves))
        {
            if (!cmsPipelineInsertStage(Dest, StageLoc.AtBegin, ObtainedCurves))
                goto Error;
            if (cmsStageData(ObtainedCurves) is not StageToneCurvesData Data)
                goto Error;
            ObtainedCurves = null;

            // If the curves are to by applied in 8 bits, we can save memory
            if (_cmsFormatterIs8bit(InputFormat))
            {
                var c16 = CurvesAlloc(Dest.ContextID, Data.nCurves, 256, Data.TheCurves);

                if (c16 is null)
                    goto Error;
                dwFlags |= cmsFLAGS_NOCACHE;
                Dest.SetOptimizationParameters(FastEvaluateCurves8, c16, CurvesFree, CurvesDup);
            }
            else
            {
                var c16 = CurvesAlloc(Dest.ContextID, Data.nCurves, 65536, Data.TheCurves);
                if (c16 is null)
                    goto Error;
                dwFlags |= cmsFLAGS_NOCACHE;
                Dest.SetOptimizationParameters(FastEvaluateCurves16, c16, CurvesFree, CurvesDup);
            }
        }
        else
        {
            // LUT optimizes to nothing. Set the identity LUT
            cmsStageFree(ObtainedCurves);
            ObtainedCurves = null;

            if (!cmsPipelineInsertStage(
                    Dest,
                    StageLoc.AtBegin,
                    cmsStageAllocIdentity(Dest.ContextID, Src.InputChannels)))
                goto Error;

            dwFlags |= cmsFLAGS_NOCACHE;
            Dest.SetOptimizationParameters(FastIdentify16, Dest, null, null);
        }

        // We are done.
        cmsPipelineFree(Src);
        Lut = Dest;
        return true;

    Error:

        for (var i = 0; i < Src.InputChannels; i++)
            cmsFreeToneCurve(GammaTables?[i]);
        //ReturnArray(Src.ContextID, GammaTables);

        if (ObtainedCurves is not null)
            cmsStageFree(ObtainedCurves);

        if (Dest is not null)
            cmsPipelineFree(Dest);

        return false;
    }

    private static void FreeMatShaper(Context? ContextID, object? Data) =>
        //_cmsFree(ContextID, Data as BoxPtr<MatShaper8Data>);
        (Data as MatShaper8Data)?.Dispose();

    private static MatShaper8Data? DupMatShaper(Context? ContextID, object? Data) =>
        (Data as MatShaper8Data)?.Clone() as MatShaper8Data;

    private static void MatShaperEval16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? D)
    {
        if (D is not MatShaper8Data p)
            return;

        // In this case (and only in this case!) we can use this simplification since
        // In[] is assured to come from an 8 bit number. (a << 8 | a)
        var ri = In[0] & 0xFF;
        var gi = In[1] & 0xFF;
        var bi = In[2] & 0xFF;

        // Across first shaper, which also converts to 1.14 fixed point
        var r = p.Shaper1R[ri];
        var g = p.Shaper1G[gi];
        var b = p.Shaper1B[bi];

        // Evaluate the matrix in 1.14 fixed point
        var l1 = ((p.Mat[(0 * 3) + 0] * r) + (p.Mat[(0 * 3) + 1] * g) + (p.Mat[(0 * 3) + 2] * b) + p.Off[0] + 0x2000) >>
                 14;
        var l2 = ((p.Mat[(1 * 3) + 0] * r) + (p.Mat[(1 * 3) + 1] * g) + (p.Mat[(1 * 3) + 2] * b) + p.Off[1] + 0x2000) >>
                 14;
        var l3 = ((p.Mat[(2 * 3) + 0] * r) + (p.Mat[(2 * 3) + 1] * g) + (p.Mat[(2 * 3) + 2] * b) + p.Off[2] + 0x2000) >>
                 14;

        // Now we have to clip to 0..1.0 range
        ri = (l1 < 0) ? 0 : ((l1 > 16384) ? 16384 : l1);
        gi = (l2 < 0) ? 0 : ((l2 > 16384) ? 16384 : l2);
        bi = (l3 < 0) ? 0 : ((l3 > 16384) ? 16384 : l3);

        // And across second shaper
        Out[0] = p.Shaper2R[ri];
        Out[1] = p.Shaper2G[gi];
        Out[2] = p.Shaper2B[bi];
    }

    private static void FillFirstShaper(Span<int> Table, ToneCurve Curve)
    {
        for (var i = 0; i < 256; i++)
        {
            var R = (float)(i / 255.0);
            var y = cmsEvalToneCurveFloat(Curve, R);

            Table[i] = y < 131072.0
                           ? DOUBLE_TO_1FIXED14(y)
                           : 0x7FFFFFFF;
        }
    }

    private static void FillSecondShaper(Span<ushort> Table, ToneCurve Curve, bool Is8BitsOutput)
    {
        for (var i = 0; i < 16385; i++)
        {
            var R = (float)(i / 16384.0);
            var Val = cmsEvalToneCurveFloat(Curve, R);

            Val = Math.Max(Math.Min(Val, 1), 0);

            if (Is8BitsOutput)
            {
                // If 8 bits output, we can optimize further by computing the / 257 part.
                // first we compute the resulting byte and then we store the byte times
                // 257. This quantization allows to round very quick by doing a >> 8, but
                // since the low byte is always equal to msb, we can do a & 0xff and this works!
                var w = _cmsQuickSaturateWord(Val * 65535.0);
                var b = FROM_16_TO_8(w);

                Table[i] = FROM_8_TO_16(b);
            }
            else
            {
                Table[i] = _cmsQuickSaturateWord(Val * 65535.0);
            }
        }
    }

    private static bool SetMatShaper(Pipeline Dest,
                                     ReadOnlySpan<ToneCurve> Curve1,
                                     MAT3 Mat,
                                     VEC3? Off,
                                     ReadOnlySpan<ToneCurve> Curve2,
                                     ref uint OutputFormat)
    {
        bool Is8Bits = _cmsFormatterIs8bit(OutputFormat);

        // Allocate a big chunk of memory to store precomputed tables
        //var p = _cmsMalloc<MatShaper8Data>(Dest.ContextID);
        //if (p is null) return false;

        //p->ContextID = Dest.ContextID;

        var p = new MatShaper8Data(Dest.ContextID);

        // Precompute tables
        FillFirstShaper(p.Shaper1R, Curve1[0]);
        FillFirstShaper(p.Shaper1G, Curve1[1]);
        FillFirstShaper(p.Shaper1B, Curve1[2]);

        FillSecondShaper(p.Shaper2R, Curve2[0], Is8Bits);
        FillSecondShaper(p.Shaper2G, Curve2[1], Is8Bits);
        FillSecondShaper(p.Shaper2B, Curve2[2], Is8Bits);

        // Convert matrix to nFixed14
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                p.Mat[(i * 3) + j] = DOUBLE_TO_1FIXED14(Mat[i][j]);
            }
        }

        for (var i = 0; i < 3; i++)
        {
            p.Off[i] = Off is null ? 0 : DOUBLE_TO_1FIXED14(Off.Value[i]);
        }

        // Mark as optimized for faster formatter
        if (Is8Bits)
            OutputFormat |= OPTIMIZED_SH(1);

        // Fill function pointers
        Dest.SetOptimizationParameters(MatShaperEval16, p, FreeMatShaper, DupMatShaper);
        return true;
    }

    private static bool OptimizeMatrixShaper(ref Pipeline Lut,
                                             uint Intent,
                                             ref uint InputFormat,
                                             ref uint OutputFormat,
                                             ref uint dwFlags)
    {
        double[]? Offset;
        MAT3 res;

        // Only works on RGB to RGB
        if (T_CHANNELS(InputFormat) is not 3 || T_CHANNELS(OutputFormat) is not 3)
            return false;

        // Only works on 8 bit input
        if (!_cmsFormatterIs8bit(InputFormat))
            return false;

        // Seems suitable, proceed
        var Src = Lut;

        // Check for:
        //
        //   shaper-matrix-matrix-shaper
        //   shaper-matrix-shaper
        //
        // Both of those constructs are possible (first because abs. colorimetric).
        // Additionally, in the first case, the input matrix offset should be zero.

        var IdentityMat = false;
        if (cmsPipelineCheckAndRetrieveStages(
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
            // Get both matrices
            var Data1 = (StageMatrixData)cmsStageData(Matrix1!)!;
            var Data2 = (StageMatrixData)cmsStageData(Matrix2!)!;

            // Only RGB to RGB
            if (Matrix1.InputChannels is not 3 ||
                Matrix1.OutputChannels is not 3 ||
                Matrix2.InputChannels is not 3 ||
                Matrix2.OutputChannels is not 3)
            {
                return false;
            }

            // Input offset should be zero
            if (Data1.Offset is not null)
                return false;

            // Multiply both matrices to get the result
            res = new MAT3(Data2.Double) * new MAT3(Data1.Double);

            // Only 2nd matrix has offset, or it is zero
            Offset = Data2.Offset;

            // Now the result is in res + Data2->Offset. Maybe it is a plain identity?
            if (res.IsIdentity && Offset is null)
            {
                // We can get rid of full matrix
                IdentityMat = true;
            }
        }
        else if (cmsPipelineCheckAndRetrieveStages(
                     Src,
                     Signatures.Stage.CurveSetElem,
                     out Curve1,
                     Signatures.Stage.MatrixElem,
                     out Matrix1,
                     Signatures.Stage.CurveSetElem,
                     out Curve2))
        {
            var Data = (StageMatrixData)cmsStageData(Matrix1!)!;

            if (Matrix1.InputChannels != 3 || Matrix1.OutputChannels != 3)
                return false;

            // Copy the matrix to our result
            //memcpy(&res, (MAT3*)Data.Double);
            res = new(Data.Double);

            // Preserve the offset (may be null as a zero offset)
            Offset = Data.Offset;

            if (res.IsIdentity && Offset is null)
            {
                // We can get rid of full matrix
                IdentityMat = true;
            }
        }
        else
        {
            return false;
        }

        // Allocate an empty LUT
        var Dest = cmsPipelineAlloc(Src.ContextID, Src.InputChannels, Src.OutputChannels);
        if (Dest == null)
            return false;

        // Assemble the new LUT
        if (!cmsPipelineInsertStage(Dest, StageLoc.AtBegin, cmsStageDup(Curve1)))
            goto Error;

        if (!IdentityMat)
        {
            //var pool = Context.Get(Src.ContextID).GetBufferPool<double>();
            var resArray = res.AsArray( /*pool*/);
            if (!cmsPipelineInsertStage(
                    Dest,
                    StageLoc.AtEnd,
                    cmsStageAllocMatrix(Dest.ContextID, 3, 3, resArray, Offset)))
            {
                //ReturnArray(pool, resArray);
                goto Error;
            }
            //ReturnArray(pool, resArray);
        }

        // If identity on matrix, we can further optimize the curves, so call the join curves routine
        if (IdentityMat)
        {
            OptimizeByJoiningCurves(ref Dest, Intent, ref InputFormat, ref OutputFormat, ref dwFlags);
        }
        else
        {
            if (cmsStageData(Curve1) is not StageToneCurvesData mpeC1 ||
                cmsStageData(Curve2) is not StageToneCurvesData mpeC2)
            {
                goto Error;
            }

            // In this particular optimization, cache does not help as it takes more time to deal with
            // the cache than with the pixel handling
            dwFlags |= cmsFLAGS_NOCACHE;

            // Setup the optimization routinds
            var resOffset = Offset is null ? default : new VEC3(Offset);
            SetMatShaper(Dest, mpeC1.TheCurves, res, resOffset, mpeC2.TheCurves, ref OutputFormat);
        }

        cmsPipelineFree(Src);
        Lut = Dest;
        return true;

    Error:
        // Leave Src unchanged
        cmsPipelineFree(Dest);
        return false;
    }

    internal static void DupPluginOptimizationList(ref OptimizationPluginChunkType dest,
                                                   in OptimizationPluginChunkType src) =>
        dest = (OptimizationPluginChunkType)((ICloneable)src).Clone();

    internal static void _cmsAllocOptimizationPluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.OptimizationPlugin
                       : OptimizationPluginChunk;

        DupPluginOptimizationList(ref ctx.OptimizationPlugin, from);
    }

    internal static bool _cmsRegisterOptimizationPlugin(Context? id, PluginBase? Data)
    {
        var ctx = Context.Get(id).OptimizationPlugin;
        if (Data is not PluginOptimization Plugin)
        {
            ctx.List.Clear();
            return true;
        }

        // Optimizer callback is required
        if (Plugin.OptimizePtr is null)
            return false;

        //var fl = _cmsPluginMalloc<OptimizationCollection>(id);
        //if (fl is null) return false;

        // Copy the parameters
        //fl->OptimizePtr = Plugin.OptimizePtr;

        // Keep linked list
        //fl->Next = ctx.OptimizationCollection;

        ctx.List.Add(Plugin.OptimizePtr);

        // All is ok
        return true;
    }

    internal static bool _cmsOptimizePipeline(Context? ContextID,
                                              ref Pipeline PtrLut,
                                              uint Intent,
                                              ref uint InputFormat,
                                              ref uint OutputFormat,
                                              ref uint dwFlags)
    {
        var ctx = Context.Get(ContextID).OptimizationPlugin;
        var AnySuccess = false;

        // A CLUT is being asked, so force this specific optimization
        if ((dwFlags & cmsFLAGS_FORCE_CLUT) is not 0)
        {
            PreOptimize(PtrLut);
            return OptimizeByResampling(ref PtrLut, Intent, ref InputFormat, ref OutputFormat, ref dwFlags);
        }

        // Anything to optimize?
        if (PtrLut.Elements is null)
        {
            PtrLut.SetOptimizationParameters(FastIdentify16, PtrLut, null, null);
            return true;
        }

        // Named color pipelines cannot be optimized
        for (var mpe = cmsPipelineGetPtrToFirstStage(PtrLut);
             mpe is not null;
             mpe = cmsStageNext(mpe))
        {
            if (cmsStageType(mpe) == Signatures.Stage.NamedColorElem)
                return false;
        }

        // Try to get rid of identities and trivial conversions.
        AnySuccess = PreOptimize(PtrLut);

        // After removal do we end with an identity?
        if (PtrLut.Elements is null)
        {
            PtrLut.SetOptimizationParameters(FastIdentify16, PtrLut, null, null);
            return true;
        }

        // Do not optimize, keep all precision
        if ((dwFlags & cmsFLAGS_NOOPTIMIZE) is not 0)
            return false;

        // Try plug-in optimizations
        foreach (var Opts in ctx.List)
        {
            // If one schema succeeded, we are done
            if (Opts(ref PtrLut, Intent, ref InputFormat, ref OutputFormat, ref dwFlags))
                return true;    // Optimized!
        }

        // Try built-in optimizations
        foreach (var Opts in DefaultOptimization)
        {
            if (Opts(ref PtrLut, Intent, ref InputFormat, ref OutputFormat, ref dwFlags))
                return true;
        }

        // Only simple optimizations succeeded
        return AnySuccess;
    }
}
