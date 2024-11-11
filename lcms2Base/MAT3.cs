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

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace lcms2;

[DebuggerStepThrough, StructLayout(LayoutKind.Explicit)]
public struct MAT3(VEC3 x, VEC3 y, VEC3 z)
{
    [FieldOffset(0)] public VEC3 X = x;

    [FieldOffset(24)] public VEC3 Y = y;

    [FieldOffset(48)] public VEC3 Z = z;

    public MAT3(double xx,
                double xy,
                double xz,
                double yx,
                double yy,
                double yz,
                double zx,
                double zy,
                double zz)
        : this(
            new(xx, xy, xz),
            new(yx, yy, yz),
            new(zx, zy, zz)) { }

    public MAT3(ReadOnlySpan<double> d)
        : this(
            new(d[0], d[1], d[2]),
            new(d[3], d[4], d[5]),
            new(d[6], d[7], d[8])) { }

    public VEC3 this[int index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                default:
                    throw new IndexOutOfRangeException(nameof(index));
            }
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

    public readonly void Deconstruct(out VEC3 x, out VEC3 y, out VEC3 z) =>
        (x, y, z) = (X, Y, Z);

    public readonly double[] AsArray( /*ArrayPool<double>? pool = null*/)
    {
        var result = /*(pool is null)
            ?*/ new double[9]
            /*: pool.Rent(9)*/;

        result[0] = X.X;
        result[1] = X.Y;
        result[2] = X.Z;
        result[3] = Y.X;
        result[4] = Y.Y;
        result[5] = Y.Z;
        result[6] = Z.X;
        result[7] = Z.Y;
        result[8] = Z.Z;

        return result;
    }

    public static MAT3 NaN =>
        new(VEC3.NaN, VEC3.NaN, VEC3.NaN);

    public readonly bool IsNaN =>
        X.IsNaN || Y.IsNaN || Z.IsNaN;

    public static MAT3 Identity =>  // _cmsMAT3identity
        new(
            new(1, 0, 0),
            new(0, 1, 0),
            new(0, 0, 1));

    internal const double DetTolerance = 1e-4;

    private static bool CloseEnough(double a, double b) =>
        Math.Abs(b - a) < (1.0 / 65535.0);

    public readonly bool IsIdentity =>  // _cmsMAT3isIdentity
        CloseEnough(X.X, 1) &&
        CloseEnough(X.Y, 0) &&
        CloseEnough(X.Z, 0) &&
        CloseEnough(Y.X, 0) &&
        CloseEnough(Y.Y, 1) &&
        CloseEnough(Y.Z, 0) &&
        CloseEnough(Z.X, 0) &&
        CloseEnough(Z.Y, 0) &&
        CloseEnough(Z.Z, 1);

    public static MAT3 operator *(MAT3 a, MAT3 b)   // _cmsMAT3per
    {
        Span<double> av = stackalloc double[9];
        Span<double> bv = stackalloc double[9];

        int get(int x, int y)
        {
            return (y * 3) + x;
        }

        av[get(0, 0)] = a.X.X;
        bv[get(0, 0)] = b.X.X;
        av[get(0, 1)] = a.X.Y;
        bv[get(0, 1)] = b.X.Y;
        av[get(0, 2)] = a.X.Z;
        bv[get(0, 2)] = b.X.Z;
        av[get(1, 0)] = a.Y.X;
        bv[get(1, 0)] = b.Y.X;
        av[get(1, 1)] = a.Y.Y;
        bv[get(1, 1)] = b.Y.Y;
        av[get(1, 2)] = a.Y.Z;
        bv[get(1, 2)] = b.Y.Z;
        av[get(2, 0)] = a.Z.X;
        bv[get(2, 0)] = b.Z.X;
        av[get(2, 1)] = a.Z.Y;
        bv[get(2, 1)] = b.Z.Y;
        av[get(2, 2)] = a.Z.Z;
        bv[get(2, 2)] = b.Z.Z;

        double ROWCOL(Span<double> a, Span<double> b, int i, int j)
        {
            return (a[get(i, 0)] * b[get(0, j)]) + (a[get(i, 1)] * b[get(1, j)]) + (a[get(i, 2)] * b[get(2, j)]);
        }

        return new(
            ROWCOL(av, bv, 0, 0),
            ROWCOL(av, bv, 0, 1),
            ROWCOL(av, bv, 0, 2),
            ROWCOL(av, bv, 1, 0),
            ROWCOL(av, bv, 1, 1),
            ROWCOL(av, bv, 1, 2),
            ROWCOL(av, bv, 2, 0),
            ROWCOL(av, bv, 2, 1),
            ROWCOL(av, bv, 2, 2));
    }

    public readonly MAT3 Inverse    // _cmsMAT3inverse
    {
        get
        {
            var c0 = (Y.Y * Z.Z) - (Y.Z * Z.Y);
            var c1 = (-Y.X * Z.Z) + (Y.Z * Z.X);
            var c2 = (Y.X * Z.Y) - (Y.Y * Z.X);

            var det = (X.X * c0) + (X.Y * c1) + (X.Z * c2);

            if (Math.Abs(det) < DetTolerance)
                return NaN;  // singular matrix; can't invert

            return new(
                c0 / det,
                ((X.Z * Z.Y) - (X.Y * Z.Z)) / det,
                ((X.Y * Y.Z) - (X.Z * Y.Y)) / det,
                c1 / det,
                ((X.X * Z.Z) - (X.Z * Z.X)) / det,
                ((X.Z * Y.X) - (X.X * Y.Z)) / det,
                c2 / det,
                ((X.Y * Z.X) - (X.X * Z.Y)) / det,
                ((X.X * Y.Y) - (X.Y * Y.X)) / det);
        }
    }

    public readonly VEC3 Solve(VEC3 vec)    // _cmsMAT3solve
    {
        var a_1 = Inverse;
        if (a_1.IsNaN)
            return VEC3.NaN;  // Singular matrix

        return a_1.Eval(vec);
    }

    public readonly VEC3 Eval(VEC3 vec) =>  // _cmsMAT3eval
        new(
            (X.X * vec.X) + (X.Y * vec.Y) + (X.Z * vec.Z),
            (Y.X * vec.X) + (Y.Y * vec.Y) + (Y.Z * vec.Z),
            (Z.X * vec.X) + (Z.Y * vec.Y) + (Z.Z * vec.Z));
}
