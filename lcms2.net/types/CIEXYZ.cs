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

public struct CIEXYZ(double x, double y, double z)
{
    public double X = x;
    public double Y = y;
    public double Z = z;

    public readonly VEC3 AsVec =>
        new(X, Y, Z);

    public static CIEXYZ NaN =>
        VEC3.NaN.AsXYZ;

    public readonly bool IsNaN =>
        AsVec.IsNaN;

    public static CIEXYZ D50 =>
        new(0.9642, 1.0, 0.8249);

    public readonly CIExyY As_xyY
    {
        get
        {
            var d = X + Y + Z;
            if (d is 0)
                return CIExyY.NaN;

            var ISum = 1 / d;
            return new(X * ISum, Y * ISum, Y);
        }
    }

    public static explicit operator CIExyY(CIEXYZ xyz) =>
        xyz.As_xyY;

    public static CIEXYZ FromXYZEncoded(ReadOnlySpan<ushort> xyz)
    {
        if (xyz.Length < 3)
            return NaN;

        return new(XYZ2float(xyz[0]), XYZ2float(xyz[1]), XYZ2float(xyz[2]));
}

    public readonly CIELab AsLab(CIEXYZ? WhitePoint = null)
    {
        var wp = WhitePoint ?? D50;

        if (wp.X is 0 || wp.Y is 0 || wp.Z is 0)
            return CIELab.NaN;

        var fx = f(X / wp.X);
        var fy = f(Y / wp.Y);
        var fz = f(Z / wp.Z);

        return new((116 * fy) - 16, 500 * (fx - fy), 200 * (fy - fz));
    }

    public readonly ushort[] FloatToEncoded()
    {
        var xyz = new ushort[3];

        FloatToEncoded(xyz);

        return xyz;
    }

    public readonly void FloatToEncoded(Span<ushort> xyz)
    {
        if (xyz.Length < 3)
            return;

        var x = Math.Clamp(X, 0.0, MAX_ENCODEABLE_XYZ);
        var y = Math.Clamp(Y, 0.0, MAX_ENCODEABLE_XYZ);
        var z = Math.Clamp(Z, 0.0, MAX_ENCODEABLE_XYZ);

        xyz[0] = XYZ2Fix(x);
        xyz[1] = XYZ2Fix(y);
        xyz[2] = XYZ2Fix(z);
    }

    private static ushort XYZ2Fix(double d) =>
        _cmsQuickSaturateWord(d * 32768);
}
