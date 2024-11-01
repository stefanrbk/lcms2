namespace lcms2.types;
public struct Format
{
    private uint _value;

    public bool PremultipliedAlpha
    {
        readonly get =>
            ((_value >> 23) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 23)) | (((value ? 1u : 0) << 23) & 1);
    }

    public bool FloatingPoint
    {
        readonly get =>
            ((_value >> 22) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 22)) | (((value ? 1u : 0) << 22) & 1);
    }

    public bool Optimized
    {
        readonly get =>
            ((_value >> 21) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 21)) | (((value ? 1u : 0) << 21) & 1);
    }

    public byte ColorSpace
    {
        readonly get =>
            (byte)((_value >> 16) & 31);
        set =>
            _value = (_value & ~(31u << 16)) | (uint)((value << 16) & 31);
    }

    public bool SwapFirst
    {
        readonly get =>
            ((_value >> 14) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 14)) | (((value ? 1u : 0) << 14) & 1);
    }

    public bool Subtractive
    {
        readonly get =>
            ((_value >> 13) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 13)) | (((value ? 1u : 0) << 13) & 1);
    }

    public bool Planar
    {
        readonly get =>
            ((_value >> 12) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 12)) | (((value ? 1u : 0) << 12) & 1);
    }

    public bool BigEndian
    {
        readonly get =>
            ((_value >> 11) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 11)) | (((value ? 1u : 0) << 11) & 1);
    }

    public bool Reversed
    {
        readonly get =>
            ((_value >> 10) & 1) is not 0;
        set =>
            _value = (_value & ~(1u << 10)) | (((value ? 1u : 0) << 10) & 1);
    }

    public byte ExtraSamples
    {
        readonly get =>
            (byte)((_value >> 7) & 7);
        set =>
            _value = (_value & ~(7u << 7)) | ((uint)(value << 7) & 7u);
    }

    public byte Channels
    {
        readonly get =>
            (byte)((_value >> 3) & 15);
        set =>
            _value = (_value & ~(15u << 3)) | ((uint)(value << 3) & 15u);
    }

    public byte Bytes
    {
        readonly get =>
            (byte)(_value & 7);
        set =>
            _value = (_value & ~7u) | (value & 7u);
    }
}
