namespace lcms2.stages;

public class IdentityStage : Stage
{
    public IdentityStage(Context? context, uint nChannels)
        : base(context, Signatures.Stage.CurveSetElem, nChannels, nChannels) { }

    internal override void Evaluate(ReadOnlySpan<float> @in, Span<float> @out) =>
        @in[..(int)InputChannels].CopyTo(@out);

    public override Stage Clone() =>
        new IdentityStage(Context, InputChannels);

    public override void Dispose() { }
}
