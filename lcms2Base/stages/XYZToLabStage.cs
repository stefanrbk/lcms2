namespace lcms2.stages;

internal class XYZToLabStage : Stage
{
    public XYZToLabStage(Context? context) : base(context, Signatures.Stage.XYZ2LabElem, 3, 3) { }

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        const double xyzAdj = CIEXYZ.MaxEncodeableXYZ;

        // From 0..1.0 to XYZ

        var xyz = new CIEXYZ(In[0] * xyzAdj, In[1] * xyzAdj, In[2] * xyzAdj);

        var lab = xyz.AsLab();

        // From V4 Lab to 0..1.0

        Out[0] = (float)(lab.L / 100.0);
        Out[1] = (float)((lab.a + 128.0) / 255.0);
        Out[2] = (float)((lab.b + 128.0) / 255.0);
    }

    public override Stage Clone() =>
        (Stage)MemberwiseClone();

    public override void Dispose() { }
}

internal class LabToXYZStage : Stage
{
    public LabToXYZStage(Context? context) : base(context, Signatures.Stage.Lab2XYZElem, 3, 3) { }

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        const double xyzAdj = CIEXYZ.MaxEncodeableXYZ;

        // V4 rules

        var lab = new CIELab(In[0] * 100.0, In[1] * 255.0 - 128.0, In[2] * 255.0 - 128.0);

        var xyz = lab.AsXYZ();

        // From XYZ, range 0..19997 to 0..1.0, note that 1.99997 comes from 0xffff
        // encoded as 1.15 fixed point, so 1 + (32767.0 / 32768.0)

        Out[0] = (float)(xyz.X / xyzAdj);
        Out[1] = (float)(xyz.Y / xyzAdj);
        Out[2] = (float)(xyz.Z / xyzAdj);
    }

    public override Stage Clone() =>
        (Stage)MemberwiseClone();

    public override void Dispose() { }
}
