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

using lcms2.types;

namespace lcms2;

public static partial class Lcms2
{
    internal static bool _cmsAdaptMatrixToD50(ref MAT3 r, CIExyY SourceWhitePt)
    {
        var Dn = SourceWhitePt.AsXYZ;

        var Bradford = CHAD.AdaptationMatrix(null, Dn, CIEXYZ.D50);
        if (Bradford.IsNaN)
            return false;

        r = Bradford * r;

        return true;
    }

    internal static bool _cmsBuildRGB2XYZtransferMatrix(ref MAT3 r, CIExyY WhitePt, CIExyYTRIPLE Primrs)
    {
        var xn = WhitePt.x;
        var yn = WhitePt.y;
        var xr = Primrs.Red.x;
        var yr = Primrs.Red.y;
        var xg = Primrs.Green.x;
        var yg = Primrs.Green.y;
        var xb = Primrs.Blue.x;
        var yb = Primrs.Blue.y;

        // Build Primaries matrix
        var Primaries = new MAT3(
            new(xr, xg, xb),
            new(yr, yg, yb),
            new(1 - xr - yr, 1 - xg - yg, 1 - xb - yb));

        // Result = Primaries ^ (-1) inverse matrix
        var Result = Primaries.Inverse;
        if (Result.IsNaN)
            return false;

        var WhitePoint = new VEC3(xn / yn, 1.0, (1.0 - xn - yn) / yn);

        // Across inverse primaries...
        var Coef = Result.Eval(WhitePoint);

        // Give us the Coefs, then I build transformation matrix
        r = new(
            new(Coef.X * xr, Coef.Y * xg, Coef.Z * xb),
            new(Coef.X * yr, Coef.Y * yg, Coef.Z * yb),
            new(Coef.X * (1.0 - xr - yr), Coef.Y * (1.0 - xg - yg), Coef.Z * (1.0 - xb - yb)));

        return _cmsAdaptMatrixToD50(ref r, WhitePt);
    }
}
