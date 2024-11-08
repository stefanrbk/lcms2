namespace lcms2;

public delegate double ParametricCurveEvaluator(int Type, ReadOnlySpan<double> Params, double R);

internal class ParametricCurve : ICloneable
{
    public readonly (int type, uint paramCount)[] Functions;
    public ParametricCurveEvaluator Evaluator;

    public ParametricCurve(ReadOnlySpan<(int type, uint paramCount)> fns, ParametricCurveEvaluator eval) =>
        (Functions, Evaluator) =
        (fns.Length > Context.MaxTypesInLcmsPlugin ? fns[..Context.MaxTypesInLcmsPlugin].ToArray() : fns.ToArray(),
         eval);

    public object Clone() =>
        new ParametricCurve(Functions, Evaluator);
}
