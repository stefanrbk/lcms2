//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright ©️ 1998-2024 Marti Maria Saguer
//              2022-2024 Stefan Kewatt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software
// is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//---------------------------------------------------------------------------------

namespace lcms2.types;

public struct CIELab(double L, double a, double b)
{
    public double L = L;
    public double a = a;
    public double b = b;

    public static CIELab NaN =>
        new(Double.NaN, Double.NaN, Double.NaN);

    public readonly bool IsNaN =>
        Double.IsNaN(L) || Double.IsNaN(a) || Double.IsNaN(b);

    public readonly CIELCh AsLCh =>
        new(L, Math.Pow(Sqr(a) + Sqr(b), 0.5), atan2deg(b, a));

    public static explicit operator CIELCh(CIELab lab) =>
        lab.AsLCh;

    public readonly CIEXYZ AsXYZ(CIEXYZ? WhitePoint = null)
    {
        var wp = WhitePoint ?? CIEXYZ.D50;

        var y = (L + 16.0) / 116.0;
        var x = y + (0.002 * a);
        var z = y - (0.005 * b);

        return new(f_1(x) * wp.X, f_1(y) * wp.Y, f_1(z) * wp.Z);
    }

    public readonly ushort[] FloatToEncoded()
    {
        var lab = new ushort[3];

        FloatToEncoded(lab);

        return lab;
    }

    public readonly void FloatToEncoded(Span<ushort> wLab)
    {
        if (wLab.Length < 3)
            return;

        wLab[0] = L2Fix4(Clamp_L_doubleV4(L));
        wLab[1] = ab2Fix4(Clamp_ab_doubleV4(a));
        wLab[2] = ab2Fix4(Clamp_ab_doubleV4(b));
    }

    public readonly ushort[] FloatToEncodedV2()
    {
        var lab = new ushort[3];

        FloatToEncodedV2(lab);

        return lab;
    }

    public readonly void FloatToEncodedV2(Span<ushort> wLab)
    {
        if (wLab.Length < 3)
            return;

        wLab[0] = L2Fix2(Clamp_L_doubleV2(L));
        wLab[1] = ab2Fix2(Clamp_ab_doubleV2(a));
        wLab[2] = ab2Fix2(Clamp_ab_doubleV2(b));
    }

    public static CIELab FromLabEncoded(ReadOnlySpan<ushort> wLab)
    {
        if (wLab.Length < 3)
            return NaN;

        return new(L2float4(wLab[0]), ab2float4(wLab[1]), ab2float4(wLab[2]));
    }

    public static CIELab FromLabEncodedV2(ReadOnlySpan<ushort> wLab)
    {
        if (wLab.Length < 3)
            return NaN;

        return new(L2float2(wLab[0]), ab2float2(wLab[1]), ab2float2(wLab[2]));
    }

    private static double L2float2(ushort v) =>
        v / 652.8;

    private static double ab2float2(ushort v) =>
        (v / 256.0) - 128;

    private static ushort L2Fix2(double L) =>
        _cmsQuickSaturateWord(L * 652.8);

    private static ushort ab2Fix2(double ab) =>
        _cmsQuickSaturateWord((ab + 128) * 256);

    private static double L2float4(ushort v) =>
        v / 655.35;

    private static double ab2float4(ushort v) =>
        (v / 257.0) - 128;

    private static double Clamp_L_doubleV4(double L) =>
        Math.Max(Math.Min(L, 100), 0);

    private static double Clamp_ab_doubleV4(double ab) =>
        Math.Max(Math.Min(ab, MAX_ENCODEABLE_ab4), MIN_ENCODEABLE_ab4);

    private static ushort L2Fix4(double L) =>
        _cmsQuickSaturateWord(L * 655.35);

    private static ushort ab2Fix4(double ab) =>
        _cmsQuickSaturateWord((ab + 128) * 257);

    private static double Clamp_L_doubleV2(double L) =>
        Math.Max(Math.Min(L, (0xffff * 100.0) / 0xff00), 0);

    private static double Clamp_ab_doubleV2(double ab) =>
        Math.Max(Math.Min(ab, MAX_ENCODEABLE_ab2), MIN_ENCODEABLE_ab2);
}
