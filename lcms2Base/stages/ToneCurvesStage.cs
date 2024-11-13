namespace lcms2.stages;

public class ToneCurvesStage : Stage
{
    internal ToneCurve[] Curves;

    public ToneCurvesStage(Context? context, uint nChannels)
        : base(context, Signatures.Stage.CurveSetElem, nChannels, nChannels)
    {
        Curves = new ToneCurve[nChannels];

        for (var i = 0; i < nChannels; i++)
            Curves[i] = ToneCurve.BuildGamma(context, 1.0);
    }

    public ToneCurvesStage(Context? context, ReadOnlySpan<ToneCurve> curves)
        : base(context, Signatures.Stage.CurveSetElem, (uint)curves.Length, (uint)curves.Length)
    {
        Curves = new ToneCurve[curves.Length];

        for (var i = 0; i < curves.Length; i++)
            Curves[i] = curves[i].Clone();
    }

    public int nCurves =>
        Curves.Length;

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        for (var i = 0; i < Curves.Length; i++)
            Out[i] = Curves[i].Evaluate(In[i]);
    }

    public override Stage Clone() =>
        new ToneCurvesStage(Context, Curves);

    public override void Dispose()
    {
        foreach (var curve in Curves)
            curve.Dispose();
    }
}
