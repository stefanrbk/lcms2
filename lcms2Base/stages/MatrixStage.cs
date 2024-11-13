namespace lcms2.stages;

public class MatrixStage : Stage
{
    internal double[] Values;
    internal double[]? Offset;

    public MatrixStage(Context? context,
                       uint rows,
                       uint cols,
                       ReadOnlySpan<double> matrix,
                       ReadOnlySpan<double> offset)
        : base(context, Signatures.Stage.MatrixElem, cols, rows)
    {
        var n = (int)(rows * cols);

        // Check for overflow
        ArgumentOutOfRangeException.ThrowIfZero(n);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(n, (int)(UInt32.MaxValue / cols));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(n, (int)(UInt32.MaxValue / rows));
        ArgumentOutOfRangeException.ThrowIfLessThan(n, (int)rows);
        ArgumentOutOfRangeException.ThrowIfLessThan(n, (int)cols);

        Values = matrix.ToArray();
        Offset = offset.Length >= rows
                     ? offset[..(int)rows].ToArray()
                     : default;
    }

    public MatrixStage(Context? context, uint rows, uint cols, MAT3 matrix, ReadOnlySpan<double> offset)
        : this(context, rows, cols, matrix.IntoArray(stackalloc double[9]), offset) { }

    public MatrixStage(Context? context, uint rows, uint cols, MAT3 matrix, VEC3 offset)
        : this(context, rows, cols, matrix.IntoArray(stackalloc double[9]), offset.IntoArray(stackalloc double[3])) { }

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        // Input is already in 0..1.0 notation
        for (var i = 0; i < OutputChannels; i++)
        {
            var Tmp = 0.0;
            for (var j = 0; j < InputChannels; j++)
                Tmp += In[j] * Values[(i * InputChannels) + j];

            if (Offset is not null)
                Tmp += Offset[i];

            Out[i] = (float)Tmp;
        }

        // Output in 0..1.0 domain
    }

    public override MatrixStage Clone()
    {
        var sz = (int)(InputChannels * OutputChannels);

        return Offset is not null
                   ? new(
                       Context,
                       OutputChannels,
                       InputChannels,
                       Values.AsSpan()[..sz],
                       Offset.AsSpan()[..(int)OutputChannels])
                   : new(Context, OutputChannels, InputChannels, Values.AsSpan()[..sz], default);
    }

    public override void Dispose() { }
}
