using System.Runtime.InteropServices;

namespace lcms2.stages;

public delegate bool Sampler<T>(ReadOnlySpan<T> In, Span<T> Out, object? Cargo) where T : unmanaged;

public class CLutStage<T> : Stage where T : unmanaged
{
    internal T[] Tab;
    internal InterpParams<T> Params;

    public bool HasFloatValues =>
        typeof(T) == typeof(float);

    public Span<ushort> TUshort =>
        Tab is ushort[] wordTab
            ? wordTab.AsSpan()
            : Span<ushort>.Empty;

    public Span<float> TFloat =>
        Tab is float[] floatTab
            ? floatTab.AsSpan()
            : Span<float>.Empty;

    public CLutStage(Context? context,
                     ReadOnlySpan<uint> clutPoints,
                     uint inputChannels,
                     uint outputChannels,
                     ReadOnlySpan<T> table)
        : base(context, Signatures.Stage.CLutElem, inputChannels, outputChannels)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inputChannels, Context.MaxInputDimensions);

        var n = outputChannels * CubeSize(clutPoints, inputChannels);

        ArgumentOutOfRangeException.ThrowIfZero(n);

        Tab = new T[n];

        if (table.Length >= n)
            table.CopyTo(Tab.AsSpan());

        Params = InterpParams<T>.Create(
            Context,
            clutPoints,
            inputChannels,
            outputChannels,
            Tab,
            HasFloatValues ? LerpFlag.Float : LerpFlag.Ushort);
    }

    public CLutStage(Context? context, uint nGridPoints, uint inputChan, uint outputChan, ReadOnlySpan<T> table)
        : this(
            context,
            Enumerable.Repeat(nGridPoints, Context.MaxInputDimensions).ToArray(),
            inputChan,
            outputChan,
            table) { }

    public bool Sample(Sampler<T> sampler, object? cargo, SamplerFlag flags = SamplerFlag.None)
    {
        Span<T> In = stackalloc T[Context.MaxInputDimensions + 1];
        Span<T> Out = stackalloc T[Context.MaxInputDimensions];

        var nSamples = Params.nSamples;
        var nInputs = Params.nInputs;
        var nOutputs = Params.nOutputs;

        if (nInputs  is <= 0 or > Context.MaxInputDimensions ||
            nOutputs is <= 0 or > MaxChannels)
            return false;

        var nTotalPoints = CubeSize(nSamples, nInputs);
        if (nTotalPoints is 0)
            return false;

        var index = 0;
        for (var i = 0; i < (int)nTotalPoints; i++)
        {
            var rest = i;
            for (var t = (int)nInputs - 1; t >= 0; --t)
            {
                var colorant = (uint)(rest % nSamples[t]);

                rest /= (int)nSamples[t];

                var value = QuantizeDoubleToUShort(colorant, nSamples[t]);

                if (typeof(T) == typeof(float))
                    MemoryMarshal.Cast<T, float>(In)[t] = (float)(value / 65535.0);
                else if (typeof(T) == typeof(ushort))
                    MemoryMarshal.Cast<T, ushort>(In)[t] = value;
            }

            if (Tab.Length is not 0)
            {
                for (var t = 0; t < (int)nOutputs; t++)
                    Out[t] = Tab[index + t];
            }

            if (!sampler(In, Out, cargo))
                return false;

            if (flags.IsUnset(SamplerFlag.Inspect))
            {
                if (Tab.Length is not 0)
                    for (var t = 0; t < (int)nOutputs; t++)
                        Tab[index + t] = Out[t];
            }

            index += (int)nOutputs;
        }

        return true;
    }

    public static bool SliceSpace(uint nInputs, ReadOnlySpan<uint> clutPoints, Sampler<T> sampler, object? cargo)
    {
        Span<T> In = stackalloc T[Context.MaxChannels];

        if (nInputs >= Context.MaxChannels)
            return false;

        var nTotalPoints = CubeSize(clutPoints, nInputs);
        if (nTotalPoints is 0)
            return false;

        for (var i = 0; i < (int)nTotalPoints; i++)
        {
            var rest = i;
            for (var t = (int)nInputs - 1; t >= 0; --t)
            {
                var colorant = (uint)(rest % clutPoints[t]);

                rest /= (int)clutPoints[t];

                if (typeof(T) == typeof(float))
                    MemoryMarshal.Cast<T, float>(In)[t] =
                        (float)(QuantizeDoubleToUShort(colorant, clutPoints[t]) / 65535.0);
                else if (typeof(T) == typeof(ushort))
                    MemoryMarshal.Cast<T, ushort>(In)[t] = QuantizeDoubleToUShort(colorant, clutPoints[t]);

                if (!sampler(In, null, cargo))
                    return false;
            }
        }

        return true;
    }

    internal override void Evaluate(ReadOnlySpan<float> In, Span<float> Out)
    {
        if (HasFloatValues)
        {
            Params.Interpolation.LerpFloat(In, Out, Params as InterpParams<float>);
            return;
        }

        Span<ushort> In16  = stackalloc ushort[MaxChannels];
        Span<ushort> Out16 = stackalloc ushort[MaxChannels];

        FromFloatTo16(In, In16, InputChannels);
        Params.Interpolation.Lerp16(In16, Out16, Params as InterpParams<ushort>);
        From16ToFloat(Out16, Out, OutputChannels);
    }

    private static uint CubeSize(ReadOnlySpan<uint> Dims, uint b)
    {
        var rv = 0u;

        for (rv = 1; b > 0; b--)
        {
            var dim = Dims[(int)b - 1];
            if (dim <= 1)
                return 0;  // Error

            rv *= dim;

            // Check for overflow
            if (rv > UInt32.MaxValue / dim)
                return 0;
        }

        // Again, prevent overflow
        if (rv > UInt32.MaxValue / 15)
            return 0;

        return rv;
    }

    public override CLutStage<T> Clone() =>
        new(Context, Params.nSamples, InputChannels, OutputChannels, Tab);

    public override void Dispose()
    {
        Params.Dispose();
    }
}
