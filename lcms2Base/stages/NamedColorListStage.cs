namespace lcms2.stages;

internal class NamedColorListStage : Stage
{
    internal NamedColorList List;

    public NamedColorListStage(NamedColorList namedColorList)
        : base(namedColorList.ContextID, Signatures.Stage.NamedColorElem, 1, namedColorList.ColorantCount) =>
        List = namedColorList.Clone();

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        var index = QuickSaturateWord(In[0] * 65535.0);

        if (index >= List.nColors)
        {
            Context.LogError(List.ContextID, ErrorCodes.Range, $"Color {index} out of range");
            for (var j = 0; j < List.ColorantCount; j++)
                Out[j] = 0.0f;
        }
        else
        {
            for (var j = 0; j < List.ColorantCount; j++)
                Out[j] = (float)(List.List[index].DeviceColorant[j] / 65535.0);
        }
    }

    public override Stage Clone() =>
        new NamedColorListStage(List.Clone());

    public override void Dispose() =>
        List.Dispose();
}
