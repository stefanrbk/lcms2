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

using System.Diagnostics.CodeAnalysis;

namespace lcms2;

public delegate void PipelineEval16Fn(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Data);
public delegate void PipelineEvalFloatFn(ReadOnlySpan<float> In, Span<float> Out, object? Data);

public class Pipeline : IDisposable, ICloneable
{
    internal Stage? Elements;
    public uint InputChannels { get; private set; }
    public uint OutputChannels { get; private set; }

    internal object? Data;

    internal PipelineEval16Fn Eval16Fn;
    internal PipelineEvalFloatFn EvalFloatFn;
    internal FreeUserDataFn? FreeDataFn;
    internal DupUserDataFn? DupDataFn;

    public readonly Context? ContextID;
    public bool SaveAs8Bits;

    private const float JacobianEpsilon = 0.001f;
    private const byte InversionMaxIterations = 30;

    public Stage? FirstStage =>
        Elements;

    public Stage? LastStage
    {
        get
        {
            Stage? Anterior = null;

            for (var mpe = Elements; mpe is not null; mpe = mpe.Next)
                Anterior = mpe;

            return Anterior;
        }
    }

    public uint StageCount
    {
        get
        {
            Stage? mpe;
            uint n;

            for (n = 0, mpe = Elements; mpe is not null; mpe = mpe.Next)
                n++;
            return n;
        }
    }

    public Pipeline(Context? contextID, uint inputChannels, uint outputChannels)
    {
        InputChannels = inputChannels;
        OutputChannels = outputChannels;

        Eval16Fn = Evaluate;
        EvalFloatFn = Evaluate;
        DupDataFn = null;
        FreeDataFn = null;
        Data = this;
        ContextID = contextID;

        Bless();
    }

    // This function may be used to set the optional evaluator and a block of private data. If private data is being used, an optional
    // duplicator and free functions should also be specified in order to duplicate the LUT construct. Use NULL to inhibit such functionality.
    public void SetOptimizationParameters(PipelineEval16Fn Eval16,
                                          object? PrivateData,
                                          FreeUserDataFn? FreePrivateDataFn,
                                          DupUserDataFn? DupPrivateDataFn)  // _cmsPipelineSetOptimizationParameters
    {
        Eval16Fn = Eval16;
        DupDataFn = DupPrivateDataFn;
        FreeDataFn = FreePrivateDataFn;
        Data = PrivateData;
    }

    public void Evaluate(ReadOnlySpan<ushort> In, Span<ushort> Out) =>
        Eval16Fn(In, Out, Data);

    public void Evaluate(ReadOnlySpan<float> In, Span<float> Out) =>
        EvalFloatFn(In, Out, this);

    // Evaluate a LUT in reverse direction. It only searches on 3->3 LUT. Uses Newton method
    //
    // x1 <- x - [J(x)]^-1 * f(x)
    //
    // lut: The LUT on where to do the search
    // Target: LabK, 3 values of Lab plus destination K which is fixed
    // Result: The obtained CMYK
    // Hint:   Location where begin the search
    public bool EvaluateReverse(ReadOnlySpan<float> Target,
                                Span<float> Result,
                                ReadOnlySpan<float> Hint)
    {
        int i, j;
        double error, LastError = 1E20;
        Span<float> fx = stackalloc float[4];
        Span<float> x = stackalloc float[4];
        Span<float> xd = stackalloc float[4];
        Span<float> fxd = stackalloc float[4];
        VEC3 tmp2;
        MAT3 Jacobian = new();

        // Only 3->3 and 4->3 are supported
        if (InputChannels is not 3 and not 4)
            return false;
        if (OutputChannels is not 3)
            return false;

        // Take the hint as starting point if specified
        if (Hint.IsEmpty)
        {
            // Begin at any point, we choose 1/3 of CMY axis
            x[0] = x[1] = x[2] = 0.3f;
        }
        else
        {
            // Only copy 3 channels from hint...
            for (j = 0; j < 3; j++)
                x[j] = Hint[j];
        }

        // If Lut is 4-dimensions, then grab target[3], which is fixed
        x[3] = InputChannels is 4
                   ? Target[3]
                   : 0; // To keep lint happy

        // Iterate
        for (i = 0; i < InversionMaxIterations; i++)
        {
            // Get beginning fx
            Evaluate(x, fx);

            // Compute error
            error = EuclideanDistance(fx, Target, 3);

            // If not convergent, return last safe value
            if (error >= LastError)
                break;

            // Keep latest values
            LastError = error;
            for (j = 0; j < InputChannels; j++)
                Result[j] = x[j];

            // Found an exact match?
            if (error <= 0)
                break;

            // Obtain slope (the Jacobian)
            Slope(fx, x, xd, fxd, 0, ref Jacobian.X.X, ref Jacobian.Y.X, ref Jacobian.Z.X);
            Slope(fx, x, xd, fxd, 1, ref Jacobian.X.Y, ref Jacobian.Y.Y, ref Jacobian.Z.Y);
            Slope(fx, x, xd, fxd, 2, ref Jacobian.X.Z, ref Jacobian.Y.Z, ref Jacobian.Z.Z);

            // Solve system
            tmp2.X = fx[0] - Target[0];
            tmp2.Y = fx[1] - Target[1];
            tmp2.Z = fx[2] - Target[2];

            var tmp = Jacobian.Solve(tmp2);
            if (tmp.IsNaN)
                return false;

            // Move our guess
            x[0] -= (float)tmp.X;
            x[1] -= (float)tmp.Y;
            x[2] -= (float)tmp.Z;

            // Some clipping....
            for (j = 0; j < 3; j++)
            {
                if (x[j] < 0)
                    x[j] = 0;
                else if (x[j] > 1.0)
                    x[j] = 1.0f;
            }
        }

        return true;

        void Slope(Span<float> fx,
                   Span<float> x,
                   Span<float> xd,
                   Span<float> fxd,
                   int j,
                   ref double j0,
                   ref double j1,
                   ref double j2)
        {
            xd[0] = x[0];
            xd[1] = x[1];
            xd[2] = x[2];
            xd[3] = x[3];  // Keep fixed channel

            IncDelta(ref xd[j]);

            Evaluate(xd, fxd);

            j0 = (fxd[0] - fx[0]) / JacobianEpsilon;
            j1 = (fxd[1] - fx[1]) / JacobianEpsilon;
            j2 = (fxd[2] - fx[2]) / JacobianEpsilon;
        }
    }

    public bool Concat(Pipeline l2)
    {
        Stage? mpe;

        // If both LUTS does not have elements, we need to inherit
        // the number of channels
        if (Elements is null && l2.Elements is null)
        {
            InputChannels = l2.InputChannels;
            OutputChannels = l2.OutputChannels;
        }

        // Concat second
        for (mpe = l2.Elements; mpe is not null; mpe = mpe.Next)
        {
            // We have to dup each element
            if (!InsertStageAtEnd(mpe.Clone()))
                return false;
        }

        return Bless();
    }

    public bool InsertStageAtStart(Stage mpe)
    {
        mpe.Next = Elements;
        Elements = mpe;

        return Bless();
    }

    public bool InsertStageAtEnd(Stage mpe)
    {
        var last = LastStage;
        if (last is null)
            Elements = mpe;
        else
            last.Next = mpe;

        return Bless();
    }

    public void UnlinkStageAtStart(out Stage? mpe)
    {
        // If empty LUT, there is nothing to remove
        if (Elements is null)
        {
            mpe = null;
            return;
        }

        var elem = Elements;

        Elements = elem.Next;
        elem.Next = null;

        mpe = elem;

        // May fail, but we ignore it
        Bless();
    }

    public void UnlinkStageAtEnd(out Stage? mpe)
    {
        Stage? Anterior, pt, Last;
        Stage? Unlinked = null;

        // If empty LUT, there is nothing to remove
        if (Elements is null)
        {
            mpe = null;
            return;
        }

        Anterior = Last = null;
        for (pt = Elements;
             pt is not null;
             pt = pt.Next)
        {
            Anterior = Last;
            Last = pt;
        }

        Unlinked = Last;  // Next already points to null

        // Truncate the chain
        if (Anterior is not null)
            Anterior.Next = null;
        else
            Elements = null;

        mpe = Unlinked;

        // May fail, but we ignore it
        Bless();
    }

    public bool CheckAndRetrieveStages(Signature sig1,
                                       [NotNullWhen(true)] out Stage? out1) =>
        CheckAndRetrieveStages(
            sig1,
            out out1,
            default,
            out var _,
            default,
            out var _,
            default,
            out var _,
            default,
            out var _);

    public bool CheckAndRetrieveStages(Signature sig1,
                                       [NotNullWhen(true)] out Stage? out1,
                                       Signature sig2,
                                       [NotNullWhen(true)] out Stage? out2) =>
        CheckAndRetrieveStages(
            sig1,
            out out1,
            sig2,
            out out2,
            default,
            out var _,
            default,
            out var _,
            default,
            out var _);

    public bool CheckAndRetrieveStages(Signature sig1,
                                       [NotNullWhen(true)] out Stage? out1,
                                       Signature sig2,
                                       [NotNullWhen(true)] out Stage? out2,
                                       Signature sig3,
                                       [NotNullWhen(true)] out Stage? out3) =>
        CheckAndRetrieveStages(
            sig1,
            out out1,
            sig2,
            out out2,
            sig3,
            out out3,
            default,
            out var _,
            default,
            out var _);

    public bool CheckAndRetrieveStages(Signature sig1,
                                       [NotNullWhen(true)] out Stage? out1,
                                       Signature sig2,
                                       [NotNullWhen(true)] out Stage? out2,
                                       Signature sig3,
                                       [NotNullWhen(true)] out Stage? out3,
                                       Signature sig4,
                                       [NotNullWhen(true)] out Stage? out4) =>
        CheckAndRetrieveStages(
            sig1,
            out out1,
            sig2,
            out out2,
            sig3,
            out out3,
            sig4,
            out out4,
            default,
            out var _);

    public bool CheckAndRetrieveStages(Signature sig1,
                                       [NotNullWhen(true)] out Stage? out1,
                                       Signature sig2,
                                       [NotNullWhen(true)] out Stage? out2,
                                       Signature sig3,
                                       [NotNullWhen(true)] out Stage? out3,
                                       Signature sig4,
                                       [NotNullWhen(true)] out Stage? out4,
                                       Signature sig5,
                                       [NotNullWhen(true)] out Stage? out5)
    {
        out1 = out2 = out3 = out4 = out5 = null!;

        var n = (uint)sig2 is 0
                    ? 1
                    : (uint)sig3 is 0
                        ? 2
                        : (uint)sig4 is 0
                            ? 3
                            : (uint)sig5 is 0
                                ? 4
                                : 5;

        Span<Signature> args = stackalloc Signature[] { sig1, sig2, sig3, sig4, sig5 };
        args = args[..n];

        // Make sure same number of elements
        if (StageCount != n)
            return false;

        // Iterate across asked types
        var mpe = Elements;
        for (var i = 0; i < n; i++)
        {
            // Get asked type.
            var Type = args[i];
            if (mpe?.Type != Type)
                return false;
            mpe = mpe.Next;
        }

        // Found a combination, fill pointers
        out1 = Elements!;

        if (out1.Next is not null)
        {
            out2 = out1.Next;
            if (out2.Next is not null)
            {
                out3 = out2.Next;
                if (out3.Next is not null)
                {
                    out4 = out3.Next;
                    if (out4.Next is not null)
                        out5 = out4.Next;
                }
            }
        }

        return true;
    }

    private static void Evaluate(ReadOnlySpan<ushort> In, Span<ushort> Out, object? d)
    {
        var lut = d as Pipeline;
        Span<float> Storage = stackalloc float[2 * Stage.MaxChannels];
        var Phase = 0;

        From16ToFloat(In, Storage, lut.InputChannels);

        for (var mpe = lut.Elements;
             mpe is not null;
             mpe = mpe.Next)
        {
            var NextPhase = Phase ^ Stage.MaxChannels;
            mpe.Evaluate(Storage[Phase..], Storage[NextPhase..]);
            Phase = NextPhase;
        }

        FromFloatTo16(Storage[Phase..], Out, lut.OutputChannels);
    }

    private static void Evaluate(ReadOnlySpan<float> In, Span<float> Out, object? d)
    {
        var lut = d as Pipeline;
        Span<float> Storage = stackalloc float[2 * Stage.MaxChannels];
        var Phase = 0;

        In[..(int)lut.InputChannels].CopyTo(Storage);

        for (var mpe = lut.Elements;
             mpe is not null;
             mpe = mpe.Next)
        {
            var NextPhase = Phase ^ Stage.MaxChannels;
            mpe.Evaluate(Storage[Phase..], Storage[NextPhase..]);
            Phase = NextPhase;
        }

        Storage[Phase..][..(int)lut.OutputChannels].CopyTo(Out);
    }

    private static void FromFloatTo16(ReadOnlySpan<float> In, Span<ushort> Out, uint n)
    {
        for (var i = 0; i < n; i++)
            Out[i] = QuickSaturateWord(In[i] * 65535.0f);
    }

    private static void From16ToFloat(ReadOnlySpan<ushort> In, Span<float> Out, uint n)
    {
        for (var i = 0; i < n; i++)
            Out[i] = In[i] / 65535.0f;
    }

    private bool Bless()
    {
        if (Elements is null)
            return true;

        var First = FirstStage;
        var Last = LastStage;

        if (First is null || Last is null)
            return false;

        InputChannels = First.InputChannels;
        OutputChannels = Last.OutputChannels;

        // Check chain consistency
        var prev = First;
        var next = prev.Next;

        while (next is not null)
        {
            if (next.InputChannels != prev!.OutputChannels)
                return false;

            next = next.Next;
            prev = prev.Next;
        }

        return true;
    }

    // Euclidean distance between two vectors of n elements each one
    private static float EuclideanDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int n)
    {
        var sum = 0f;
        int i;

        for (i = 0; i < n; i++)
        {
            var dif = b[i] - a[i];
            sum += dif * dif;
        }

        return Single.Sqrt(sum);
    }

    // ----------------------------------------------------------- Reverse interpolation
    // Here's how it goes. The derivative Df(x) of the function f is the linear
    // transformation that best approximates f near the point x. It can be represented
    // by a matrix A whose entries are the partial derivatives of the components of f
    // with respect to all the coordinates. This is know as the Jacobian
    //
    // The best linear approximation to f is given by the matrix equation:
    //
    // y-y0 = A (x-x0)
    //
    // So, if x0 is a good "guess" for the zero of f, then solving for the zero of this
    // linear approximation will give a "better guess" for the zero of f. Thus let y=0,
    // and since y0=f(x0) one can solve the above equation for x. This leads to the
    // Newton's method formula:
    //
    // xn+1 = xn - A-1 f(xn)
    //
    // where xn+1 denotes the (n+1)-st guess, obtained from the n-th guess xn in the
    // fashion described above. Iterating this will give better and better approximations
    // if you have a "good enough" initial guess.

    // Increment with reflexion on boundary
    private static void IncDelta(ref float Val)
    {
        Val += Val < 1.0 - JacobianEpsilon
                   ? JacobianEpsilon
                   : -JacobianEpsilon;
    }

    public void Dispose()
    {
        Stage? Next;

        for (var mpe = Elements;
             mpe is not null;
             mpe = Next)
        {
            Next = mpe.Next;
            mpe.Dispose();
        }

        FreeDataFn?.Invoke(ContextID, Data);
        Data = null;
    }

    object ICloneable.Clone() =>
        Clone();

    public Pipeline Clone()
    {
        Pipeline? NewLUT;
        Stage? NewMPE, Anterior = null, mpe;
        var First = true;

        NewLUT = new Pipeline(ContextID, InputChannels, OutputChannels);

        for (mpe = Elements; mpe is not null; mpe = mpe.Next)
        {
            NewMPE = mpe.Clone();

            if (First)
            {
                NewLUT.Elements = NewMPE;
                First = false;
            }
            else
            {
                if (Anterior is not null)
                    Anterior.Next = NewMPE;
            }

            Anterior = NewMPE;
        }

        NewLUT.Eval16Fn = Eval16Fn;
        NewLUT.EvalFloatFn = EvalFloatFn;
        NewLUT.DupDataFn = DupDataFn;
        NewLUT.FreeDataFn = FreeDataFn;

        NewLUT.Data = NewLUT.DupDataFn?.Invoke(ContextID, Data) ?? (Data == this ? NewLUT : Data);

        NewLUT.SaveAs8Bits = SaveAs8Bits;

        if (!NewLUT.Bless())
        {
            NewLUT.Dispose();
            return null;
        }

        return NewLUT;
    }
}
