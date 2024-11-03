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

public class CIECAM02
{
    internal CAM02COLOR adoptedWhite;
    internal double LA;
    internal double Yb;
    internal double F;
    internal double c;
    internal double Nc;
    internal uint surround;
    internal double n;
    internal double Nbb;
    internal double Ncb;
    internal double z;
    internal double FL;
    internal double D;

    public CIECAM02(ViewingConditions pVC)
    {
        adoptedWhite = new(pVC.whitePoint.X, pVC.whitePoint.Y, pVC.whitePoint.Z);

        LA = pVC.La;
        Yb = pVC.Yb;
        D = pVC.D_value;
        surround = pVC.surround;

        switch (surround)
        {
            case CUTSHEET_SURROUND:
                F = 0.8;
                c = 0.41;
                Nc = 0.8;
                break;

            case DARK_SURROUND:
                F = 0.8;
                c = 0.525;
                Nc = 0.8;
                break;

            case DIM_SURROUND:
                F = 0.9;
                c = 0.59;
                Nc = 0.95;
                break;

            default:
                // Average surround
                F = 1.0;
                c = 0.69;
                Nc = 1.0;
                break;
        }

        n = Yb / adoptedWhite.XYZ[1];
        z = 1.48 + Double.Pow(1.0 / n, 0.5);
        Nbb = 0.725 * Double.Pow(1.0 / n, 0.2);

        var k = 1.0 / ((5.0 * LA) + 1.0);
        FL = (0.2 * Double.Pow(k, 4.0) * (5.0 * LA)) +
             (0.1 * Double.Pow(1.0 - Double.Pow(k, 4.0), 2.0) * Double.Pow(5.0 * LA, 1.0 / 3.0));

        if (Math.Abs(D - D_CALCULATE) < 1e-12)
        {
            D = F * (1.0 - ((1.0 / 3.6) * Double.Exp((-LA - 42) / 92.0)));
        }

        Ncb = Nbb;

        adoptedWhite.XYZtoCAT02();
        adoptedWhite.ChromaticAdaptation(D);
        adoptedWhite.CAT02toHPE();
        adoptedWhite.NonlinearCompression(FL, Nbb);
    }

    public JCh Forward(CIEXYZ pIn)
    {
        CAM02COLOR clr = new(pIn.X, pIn.Y, pIn.Z);

        clr.XYZtoCAT02();
        clr.ChromaticAdaptation(D);
        clr.CAT02toHPE();
        clr.NonlinearCompression(FL, Nbb);
        ComputeCorrelates();

        return new JCh { J = clr.J, C = clr.C, h = clr.h };
    }

    public CIEXYZ Reverse(JCh pIn)
    {
        CAM02COLOR clr = new();

        clr.J = pIn.J;
        clr.C = pIn.C;
        clr.h = pIn.h;

        InverseCorrelates();
        clr.InverseNonlinearity(FL);
        clr.HPEtoCAT02();
        clr.InverseChromaticAdaptation(D);
        clr.CAT02toXYZ();

        return new() { X = clr.XYZ[0], Y = clr.XYZ[1], Z = clr.XYZ[2] };
    }

    private void ComputeCorrelates()
    {
        ref var clr = ref adoptedWhite;
        double a, b, temp, e, t, r2d, d2r;

        a = clr.RGBpa[0] - (12.0 * clr.RGBpa[1] / 11.0) + (clr.RGBpa[2] / 11.0);
        b = (clr.RGBpa[0] + clr.RGBpa[1] - (2.0 * clr.RGBpa[2])) / 9.0;

        r2d = (180.0 / 3.141592654);
        if (a == 0)
        {
            if (b == 0)
                clr.h = 0;
            else if (b > 0)
                clr.h = 90;
            else
                clr.h = 270;
        }
        else if (a > 0)
        {
            temp = b / a;
            if (b > 0)
                clr.h = (r2d * Double.Atan(temp));
            else if (b == 0)
                clr.h = 0;
            else
                clr.h = (r2d * Double.Atan(temp)) + 360;
        }
        else
        {
            temp = b / a;
            clr.h = (r2d * Double.Atan(temp)) + 180;
        }

        d2r = (3.141592654 / 180.0);
        e = ((12500.0 / 13.0) * Nc * Ncb) *
            (Double.Cos((clr.h * d2r + 2.0)) + 3.8);

        if (clr.h < 20.14)
        {
            temp = ((clr.h + 122.47) / 1.2) + ((20.14 - clr.h) / 0.8);
            clr.H = 300 + (100 * ((clr.h + 122.47) / 1.2)) / temp;
        }
        else if (clr.h < 90.0)
        {
            temp = ((clr.h - 20.14) / 0.8) + ((90.00 - clr.h) / 0.7);
            clr.H = (100 * ((clr.h - 20.14) / 0.8)) / temp;
        }
        else if (clr.h < 164.25)
        {
            temp = ((clr.h - 90.00) / 0.7) + ((164.25 - clr.h) / 1.0);
            clr.H = 100 + ((100 * ((clr.h - 90.00) / 0.7)) / temp);
        }
        else if (clr.h < 237.53)
        {
            temp = ((clr.h - 164.25) / 1.0) + ((237.53 - clr.h) / 1.2);
            clr.H = 200 + ((100 * ((clr.h - 164.25) / 1.0)) / temp);
        }
        else
        {
            temp = ((clr.h - 237.53) / 1.2) + ((360 - clr.h + 20.14) / 0.8);
            clr.H = 300 + ((100 * ((clr.h - 237.53) / 1.2)) / temp);
        }

        clr.J = 100.0 * Double.Pow(
                    (clr.A / adoptedWhite.A),
                    (c * z));

        clr.Q = (4.0 / c) * Double.Pow((clr.J / 100.0), 0.5) *
                (adoptedWhite.A + 4.0) * Double.Pow(FL, 0.25);

        t = (e * Double.Pow(((a * a) + (b * b)), 0.5)) /
            (clr.RGBpa[0] + clr.RGBpa[1] +
             ((21.0 / 20.0) * clr.RGBpa[2]));

        clr.C = Double.Pow(t, 0.9) * Double.Pow((clr.J / 100.0), 0.5) *
                Double.Pow((1.64 - Double.Pow(0.29, n)), 0.73);

        clr.M = clr.C * Double.Pow(FL, 0.25);
        clr.s = 100.0 * Double.Pow((clr.M / clr.Q), 0.5);
    }

    private void InverseCorrelates()
    {
        ref var clr = ref adoptedWhite;
        double t, e, p1, p2, p3, p4, p5, hr, d2r;
        d2r = 3.141592654 / 180.0;

        t = Double.Pow(
            (clr.C / (Double.Pow((clr.J / 100.0), 0.5) *
                      (Double.Pow((1.64 - Double.Pow(0.29, n)), 0.73)))),
            (1.0 / 0.9));
        e = ((12500.0 / 13.0) * Nc * Ncb) *
            (Double.Cos((clr.h * d2r + 2.0)) + 3.8);

        clr.A = adoptedWhite.A * Double.Pow(
                    (clr.J / 100.0),
                    (1.0 / (c * z)));

        p1 = e / t;
        p2 = (clr.A / Nbb) + 0.305;
        p3 = 21.0 / 20.0;

        hr = clr.h * d2r;

        if (Double.Abs(Double.Sin(hr)) >= Double.Abs(Double.Cos(hr)))
        {
            p4 = p1 / Double.Sin(hr);
            clr.b = (p2 * (2.0 + p3) * (460.0 / 1403.0)) /
                    (p4 + (2.0 + p3) * (220.0 / 1403.0) *
                     (Double.Cos(hr) / Double.Sin(hr)) - (27.0 / 1403.0) +
                     p3 * (6300.0 / 1403.0));
            clr.a = clr.b * (Double.Cos(hr) / Double.Sin(hr));
        }
        else
        {
            p5 = p1 / Double.Cos(hr);
            clr.a = (p2 * (2.0 + p3) * (460.0 / 1403.0)) /
                    (p5 + (2.0 + p3) * (220.0 / 1403.0) -
                     ((27.0 / 1403.0) - p3 * (6300.0 / 1403.0)) *
                     (Double.Sin(hr) / Double.Cos(hr)));
            clr.b = clr.a * (Double.Sin(hr) / Double.Cos(hr));
        }

        clr.RGBpa[0] = ((460.0 / 1403.0) * p2) +
                       ((451.0 / 1403.0) * clr.a) +
                       ((288.0 / 1403.0) * clr.b);
        clr.RGBpa[1] = ((460.0 / 1403.0) * p2) -
                       ((891.0 / 1403.0) * clr.a) -
                       ((261.0 / 1403.0) * clr.b);
        clr.RGBpa[2] = ((460.0 / 1403.0) * p2) -
                       ((220.0 / 1403.0) * clr.a) -
                       ((6300.0 / 1403.0) * clr.b);
    }
}
