namespace lcms2.stages;

internal class NamedColorListPcsStage : Stage
{
    internal NamedColorList List;

    public NamedColorListPcsStage(NamedColorList namedColorList)
        : base(namedColorList.ContextID, Signatures.Stage.NamedColorElem, 1, 3) =>
        List = namedColorList.Clone();

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        var index = QuickSaturateWord(In[0] * 65535.0);

        if (index >= List.nColors)
        {
            Context.LogError(List.ContextID, ErrorCodes.Range, $"Color {index} out of range");
            for (var j = 0; j < List.ColorantCount; j++)
                Out[0] = Out[1] = Out[2] = 0.0f;
        }
        else
        {
            Out[0] = (float)(List.List[index].PCS[0] / 65535.0);
            Out[1] = (float)(List.List[index].PCS[1] / 65535.0);
            Out[2] = (float)(List.List[index].PCS[2] / 65535.0);
        }
    }

    public override Stage Clone() =>
        new NamedColorListStage(List.Clone());

    public override void Dispose() =>
        List.Dispose();
}
