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

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace lcms2;

[DebuggerStepThrough, StructLayout(LayoutKind.Explicit)]
public struct VEC3(double x, double y, double z)    // _cmsVEC3init
{
    [FieldOffset(0)] public double X = x;

    [FieldOffset(8)] public double Y = y;

    [FieldOffset(16)] public double Z = z;

    public VEC3(ReadOnlySpan<double> d)
        : this(d[0], d[1], d[2]) { }

    internal readonly double[] AsArray(ArrayPool<double>? pool = null)
    {
        var result = pool is not null
                         ? pool.Rent(3)
                         : new double[3];

        result[0] = X;
        result[1] = Y;
        result[2] = Z;

        return result;
    }

    public double this[int index]
    {
        get
        {
            return index switch
                   {
                       0 => X,
                       1 => Y,
                       2 => Z,
                       _ => throw new IndexOutOfRangeException(nameof(index)),
                   };
        }
        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Z = value;
                    break;
                default:
                    throw new IndexOutOfRangeException(nameof(index));
            }
        }
    }

    public readonly void Deconstruct(out double x, out double y, out double z) =>
        (x, y, z) = (X, Y, Z);

    public static VEC3 operator *(VEC3 lhs, double rhs) =>
        new(lhs.X * rhs, lhs.Y * rhs, lhs.Z * rhs);

    public static VEC3 operator *(double lhs, VEC3 rhs) =>
        new(rhs.X * lhs, rhs.Y * lhs, rhs.Z * lhs);

    public static VEC3 NaN =>
        new(double.NaN, double.NaN, double.NaN);

    public readonly bool IsNaN =>
        double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z);

    public static VEC3 operator -(VEC3 left, VEC3 right) => // _cmsVEC3minus
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public readonly VEC3 Cross(VEC3 other) =>   // _cmsVEC3cross
        new(
            (Y * other.Z) - (other.Y * Z),
            (Z * other.X) - (other.Z * X),
            (X * other.Y) - (other.X * Y));

    public readonly double Dot(VEC3 other) =>   // _cmsVEC3dot
        (X * other.X) + (Y * other.Y) + (Z * other.Z);

    public readonly double Length =>    // _cmsVEC3length
        Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

    public readonly double Distance(VEC3 other) =>  // _cmsVEC3distance
        (this - other).Length;

    public readonly CIEXYZ AsXYZ =>
        new(X, Y, Z);
}
