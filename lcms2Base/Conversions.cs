using System.Diagnostics;
using System.Runtime.CompilerServices;

using S15Fixed16Number = System.Int32;
using U8Fixed8Number = System.UInt16;

namespace lcms2;

public static class Conversions
{
    [DebuggerStepThrough]
    public static ushort AdjustEndianness(ushort Word)   // _cmsAdjustEndianess16
    {
        Span<byte> pByte = stackalloc byte[2];
        BitConverter.TryWriteBytes(pByte, Word);

        (pByte[1], pByte[0]) = (pByte[0], pByte[1]);
        return BitConverter.ToUInt16(pByte);
    }

    [DebuggerStepThrough]
    public static Signature AdjustEndianness(Signature sig) =>
        (Signature)AdjustEndianness((uint)sig);

    [DebuggerStepThrough]
    public static uint AdjustEndianness(uint DWord)  // _cmsAdjustEndianess32
    {
        Span<byte> pByte = stackalloc byte[4];
        BitConverter.TryWriteBytes(pByte, DWord);

        (pByte[3], pByte[2], pByte[1], pByte[0]) = (pByte[0], pByte[1], pByte[2], pByte[3]);
        return BitConverter.ToUInt32(pByte);
    }

    [DebuggerStepThrough]
    public static ulong AdjustEndianness(ulong QWord)    // _cmsAdjustEndianess64
    {
        Span<byte> pByte = stackalloc byte[8];
        BitConverter.TryWriteBytes(pByte, QWord);

        (pByte[7], pByte[0]) = (pByte[0], pByte[7]);
        (pByte[6], pByte[1]) = (pByte[1], pByte[6]);
        (pByte[5], pByte[2]) = (pByte[2], pByte[5]);
        (pByte[4], pByte[3]) = (pByte[3], pByte[4]);

        return BitConverter.ToUInt64(pByte);
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint AlignLong(uint x) =>
        (x + (sizeof(uint) - 1u)) & ~(sizeof(uint) - 1u);

    internal static double XYZ2float(ushort v) =>
        S15Fixed16ToDouble(v << 1);

    public static double U8Fixed8ToDouble(U8Fixed8Number fixed8) =>  // _cms8Fixed8toDouble
        fixed8 / 256.0;

    public static U8Fixed8Number DoubleToU8Fixed8(double val)    // _cmsDoubleTo8Fixed8
    {
        var tmp = DoubleToS15Fixed16(val);
        return (ushort)((tmp >> 8) & 0xffff);
    }

    public static double S15Fixed16ToDouble(S15Fixed16Number fix32) =>   // _cms15Fixed16toDouble
        fix32 / 65536.0;

    public static S15Fixed16Number DoubleToS15Fixed16(double v) =>   // _cmsDoubleTo15Fixed16
        (S15Fixed16Number)Math.Floor((v * 65536.0) + 0.5);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIEXYZ XYZEncodedToFloat(this ReadOnlySpan<ushort> XYZ) =>
        CIEXYZ.FromXYZEncoded(XYZ);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIEXYZ XYZEncodedToFloat(this Span<ushort> XYZ) =>
        CIEXYZ.FromXYZEncoded(XYZ);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIELab LabEncodedToFloat(this ReadOnlySpan<ushort> wLab) =>
        CIELab.FromLabEncoded(wLab);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIELab LabEncodedToFloat(this Span<ushort> wLab) =>
        CIELab.FromLabEncoded(wLab);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIELab LabEncodedToFloatV2(this ReadOnlySpan<ushort> wLab) =>
        CIELab.FromLabEncodedV2(wLab);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CIELab LabEncodedToFloatV2(this Span<ushort> wLab) =>
        CIELab.FromLabEncodedV2(wLab);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int QuickFloor(double val)
    {
        Span<byte> buffer = stackalloc byte[8];
#if CMS_DONT_USE_FAST_FLOOR
        (int)Math.Floor(val);
#else
        const double _lcms_double2fixmagic = 68719476736.0 * 1.5;
        BitConverter.TryWriteBytes(buffer, val + _lcms_double2fixmagic);

        return BitConverter.ToInt32(buffer) >> 16;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static ushort QuickFloorWord(double d) =>
        (ushort)(QuickFloor(d - 32767) + 32767);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static ushort QuickSaturateWord(double d)
    {
        d += 0.5;
        return d switch
               {
                   <= 0       => 0,
                   >= 65535.0 => 0xffff,
                   _          => QuickFloorWord(d),
               };
    }

    internal static double atan2deg(double a, double b)
    {
        var h = a is 0 && b is 0
                    ? 0
                    : Math.Atan2(a, b);

        h *= 180 / Math.PI;

        while (h > 360)
            h -= 360;
        while (h < 0)
            h += 360;

        return h;
    }

    internal static double Sqr(double v) =>
        v * v;

    [DebuggerStepThrough]
    internal static string SpanToString(ReadOnlySpan<byte> span)
    {
        Span<char> str = stackalloc char[span.Length];
        var index = span.IndexOf<byte>(0);
        if (index is not -1)
            str = str[..index];
        for (var i = 0; i < str.Length; i++)
            str[i] = (char)span[i];

        return new string(str);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int FIXED_TO_INT(int x) =>
        x >> 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int FIXED_REST_TO_INT(int x) =>
        x & 0xFFFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int ROUND_FIXED_TO_INT(int x) =>
        (x + 0x8000) >> 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int ToFixedDomain(int a) =>
        a + ((a + 0x7fff) / 0xffff);

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    internal static int FromFixedDomain(int a) =>
        a - ((a + 0x7fff) >> 16);
}
