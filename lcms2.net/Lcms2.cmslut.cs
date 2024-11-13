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

using lcms2.stages;

namespace lcms2;

public static partial class Lcms2
{
    internal static Span<ToneCurve> _cmsStageGetPtrToCurveSet(Stage mpe) =>
        mpe is ToneCurvesStage Data
            ? Data.Curves.AsSpan()
            : null;

    internal static Stage? _cmsStageAllocIdentityCurves(Context? ContextID, uint nChannels)
    {
        var mpe = new ToneCurvesStage(ContextID, nChannels);

        mpe.Implements = Signatures.Stage.IdentityElem;
        return mpe;
    }

    private static bool IdentitySampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        if (Cargo is not Box<int> nChan)
            return false;

        for (var i = 0; i < nChan; i++)
            Out[i] = In[i];

        return true;
    }

    internal static Stage? _cmsStageAllocIdentityCLut(Context? ContextID, uint nChan)
    {
        Span<uint> Dimensions = stackalloc uint[Context.MaxInputDimensions];

        for (var i = 0; i < Context.MaxInputDimensions; i++)
            Dimensions[i] = 2;

        var mpe = new CLutStage<ushort>(ContextID, Dimensions, nChan, nChan, null);

        if (!mpe.Sample(IdentitySampler, new Box<int>((int)nChan), 0))
        {
            return null;
        }

        mpe.Implements = Signatures.Stage.IdentityElem;
        return mpe;
    }

    private static void EvaluateLab2XYZ(ReadOnlySpan<float> In, Span<float> Out, Stage _)
    {
        CIELab Lab;
        CIEXYZ XYZ;
        const double XYZadj = CIEXYZ.MaxEncodeableXYZ;

        // V4 rules
        Lab.L = In[0] * 100.0;
        Lab.a = (In[1] * 255.0) - 128.0;
        Lab.b = (In[2] * 255.0) - 128.0;

        XYZ = Lab.AsXYZ();

        // From XYZ, range 0..19997 to 0..1.0, note that 1.99997 comes from 0xffff
        // encoded as 1.15 fixed point, so 1 + (32767.0 / 32768.0)

        Out[0] = (float)(XYZ.X / XYZadj);
        Out[1] = (float)(XYZ.Y / XYZadj);
        Out[2] = (float)(XYZ.Z / XYZadj);
    }

    internal static Stage? _cmsStageAllocLab2XYZ(Context? ContextID) =>
        new LabToXYZStage(ContextID);

    internal static Stage? _cmsStageAllocLabV2ToV4curves(Context? ContextID)
    {
        //var pool = Context.GetPool<ToneCurve>(ContextID);
        //ToneCurve[] LabTable = pool.Rent(3);
        var LabTable = new ToneCurve[3];

        LabTable[0] = ToneCurve.BuildTabulated(ContextID, 258, ReadOnlySpan<ushort>.Empty)!;
        LabTable[1] = ToneCurve.BuildTabulated(ContextID, 258, ReadOnlySpan<ushort>.Empty)!;
        LabTable[2] = ToneCurve.BuildTabulated(ContextID, 258, ReadOnlySpan<ushort>.Empty)!;

        for (var j = 0; j < 3; j++)
        {
            if (LabTable[j] is null)
            {
                return null;
            }

            // We need to map * (0xffff / 0xff00), that's same as (257 / 256)
            // So we can use 258-entry tables to do the trick (i / 257) * (255 * 257) * (257 / 256);
            for (var i = 0; i < 257; i++)
                LabTable[j].Table16![i] = (ushort)(((i * 0xffff) + 0x80) >> 8);

            LabTable[j].Table16![257] = 0xffff;
        }

        var mpe = new ToneCurvesStage(ContextID, LabTable.AsSpan(..3));

        mpe.Implements = Signatures.Stage.LabV2toV4Elem;
        return mpe;
    }

    internal static Stage? _cmsStageAllocLabV2ToV4(Context? ContextID)
    {
        ReadOnlySpan<double> V2ToV4 =
            stackalloc double[] { 65535.0 / 65280.0, 0, 0, 0, 65535.0 / 65280.0, 0, 0, 0, 65535.0 / 65280.0 };

        var mpe = new MatrixStage(ContextID, 3, 3, V2ToV4, null);

        mpe.Implements = Signatures.Stage.LabV2toV4Elem;
        return mpe;
    }

    internal static Stage? _cmsStageAllocLabV4ToV2(Context? ContextID)
    {
        ReadOnlySpan<double> V4ToV2 =
            stackalloc double[] { 65280.0 / 65535.0, 0, 0, 0, 65280.0 / 65535.0, 0, 0, 0, 65280.0 / 65535.0 };

        var mpe = new MatrixStage(ContextID, 3, 3, V4ToV2, null);

        mpe.Implements = Signatures.Stage.LabV4toV2Elem;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeFromLabFloat(Context? ContextID)
    {
        ReadOnlySpan<double> a1 = stackalloc double[] { 1.0 / 100.0, 0, 0, 0, 1.0 / 255.0, 0, 0, 0, 1.0 / 255.0 };
        ReadOnlySpan<double> o1 = stackalloc double[] { 0, 128.0 / 255.0, 128.0 / 255.0 };

        var mpe = new MatrixStage(ContextID, 3, 3, a1, o1);

        mpe.Implements = Signatures.Stage.Lab2FloatPCS;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeFromXyzFloat(Context? ContextID)
    {
        const double n = 32768.0 / 65535.0;
        ReadOnlySpan<double> a1 = stackalloc double[9] { n, 0, 0, 0, n, 0, 0, 0, n };

        var mpe = new MatrixStage(ContextID, 3, 3, a1, null);

        mpe.Implements = Signatures.Stage.XYZ2FloatPCS;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeToLabFloat(Context? ContextID)
    {
        ReadOnlySpan<double> a1 = stackalloc double[9] { 100.0, 0, 0, 0, 255.0, 0, 0, 0, 255.0 };
        ReadOnlySpan<double> o1 = stackalloc double[3] { 0, -128.0, -128.0 };

        var mpe = new MatrixStage(ContextID, 3, 3, a1, o1);

        mpe.Implements = Signatures.Stage.FloatPCS2Lab;
        return mpe;
    }

    internal static Stage? _cmsStageNormalizeToXYZFloat(Context? ContextID)
    {
        const double n = 65535.0 / 32768;
        ReadOnlySpan<double> a1 = stackalloc double[9] { n, 0, 0, 0, n, 0, 0, 0, n };

        var mpe = new MatrixStage(ContextID, 3, 3, a1, null);

        mpe.Implements = Signatures.Stage.FloatPCS2XYZ;
        return mpe;
    }

    private static void Clipper(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        for (var i = 0; i < mpe.InputChannels; i++)
        {
            var n = In[i];
            Out[i] = MathF.Max(n, 0);
        }
    }

    internal static Stage? _cmsStageClipNegatives(Context? ContextID, uint nChannels) =>
        new ClipNegativesStage(ContextID, nChannels);

    private static void EvaluateXYZ2Lab(ReadOnlySpan<float> In, Span<float> Out, Stage _)
    {
        CIELab Lab;
        CIEXYZ XYZ;
        const double XYZadj = CIEXYZ.MaxEncodeableXYZ;

        // From 0..1.0 to XYZ
        XYZ.X = In[0] * XYZadj;
        XYZ.Y = In[1] * XYZadj;
        XYZ.Z = In[2] * XYZadj;

        Lab = XYZ.AsLab();

        // From V4 Lab to 0..1.0
        Out[0] = (float)(Lab.L / 100);
        Out[1] = (float)((Lab.a + 128) / 255);
        Out[2] = (float)((Lab.b + 128) / 255);
    }

    internal static Stage? _cmsStageAllocXYZ2Lab(Context? ContextID) =>
        new XYZToLabStage(ContextID);

    internal static Stage? _cmsStageAllocLabPrelin(Context? ContextID)
    {
        var LabTable = new ToneCurve[3];
        Span<double> Params = stackalloc double[1] { 2.4 };

        LabTable[0] = ToneCurve.BuildGamma(ContextID, 1.0);
        LabTable[1] = ToneCurve.BuildParametric(ContextID, 108, Params);
        LabTable[2] = ToneCurve.BuildParametric(ContextID, 108, Params);

        return new ToneCurvesStage(ContextID, LabTable);
    }
}
