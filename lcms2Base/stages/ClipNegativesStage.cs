namespace lcms2.stages;

internal class ClipNegativesStage : Stage
{
    public ClipNegativesStage(Context? context, uint nChannels)
        : base(context, Signatures.Stage.ClipNegativesElem, nChannels, nChannels) { }

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        for (var i = 0; i < InputChannels; i++)
            Out[i] = Single.Max(In[i], 0);
    }

    public override Stage Clone() =>
        (Stage)MemberwiseClone();

    public override void Dispose() { }
}
