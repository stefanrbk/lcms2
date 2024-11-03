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

internal class CAM02COLOR : IDisposable
{
    private readonly double[] _XYZ;
    private readonly double[] _RGB = new double[3];
    private readonly double[] _RGBc = new double[3];
    private readonly double[] _RGBp = new double[3];
    private readonly double[] _RGBpa = new double[3];
    public double a, b, h, e, H, A, J, Q, s, t, C, M;
    private readonly double[] _AbC = new double[2];
    private readonly double[] _Abs = new double[2];
    private readonly double[] _AbM = new double[2];
    private bool disposedValue;

    public CAM02COLOR() =>
        _XYZ = new double[3];

    public CAM02COLOR(double x, double y, double z)
    {
        _XYZ = [ x, y, z ];
    }

    public Span<double> XYZ =>
        _XYZ.AsSpan(..3);

    public Span<double> RGB =>
        _RGB.AsSpan(..3);

    public Span<double> RGBc =>
        _RGBc.AsSpan(..3);

    public Span<double> RGBp =>
        _RGBp.AsSpan(..3);

    public Span<double> RGBpa =>
        _RGBpa.AsSpan(..3);

    public Span<double> AbC =>
        _AbC.AsSpan(..2);

    public Span<double> Abs =>
        _Abs.AsSpan(..2);

    public Span<double> AbM =>
        _AbM.AsSpan(..2);

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void XYZtoCAT02()
    {
        RGB[0] = (XYZ[0] * 0.7328) + (XYZ[1] * 0.4296) + (XYZ[2] * -0.1624);
        RGB[1] = (XYZ[0] * -0.7036) + (XYZ[1] * 1.6975) + (XYZ[2] * 0.0061);
        RGB[2] = (XYZ[0] * 0.0030) + (XYZ[1] * 0.0136) + (XYZ[2] * 0.9834);
    }

    public void ChromaticAdaptation(double D)
    {
        for (var i = 0; i < 3; i++)
        {
            RGBc[i] = ((XYZ[1] *
                        (D / RGB[i])) +
                       (1.0 - D)) * RGB[i];
        }
    }

    public void CAT02toHPE()
    {
        Span<double> m =
        [
            ((0.38971 * 1.096124) + (0.68898 * 0.454369) + (-0.07868 * -0.009628)),
            ((0.38971 * -0.278869) + (0.68898 * 0.473533) + (-0.07868 * -0.005698)),
            ((0.38971 * 0.182745) + (0.68898 * 0.072098) + (-0.07868 * 1.015326)),
            ((-0.22981 * 1.096124) + (1.18340 * 0.454369) + (0.04641 * -0.009628)),
            ((-0.22981 * -0.278869) + (1.18340 * 0.473533) + (0.04641 * -0.005698)),
            ((-0.22981 * 0.182745) + (1.18340 * 0.072098) + (0.04641 * 1.015326)),
            (-0.009628),
            (-0.005698),
            (1.015326),
        ];

        RGBp[0] = (RGBc[0] * m[0]) + (RGBc[1] * m[1]) + (RGBc[2] * m[2]);
        RGBp[1] = (RGBc[0] * m[3]) + (RGBc[1] * m[4]) + (RGBc[2] * m[5]);
        RGBp[2] = (RGBc[0] * m[6]) + (RGBc[1] * m[7]) + (RGBc[2] * m[8]);
    }

    public void NonlinearCompression(double FL, double Nbb)
    {
        for (var i = 0; i < 3; i++)
        {
            if (RGBp[i] < 0)
            {
                var temp = Double.Pow((-1.0 * FL * RGBp[i] / 100.0), 0.42);
                RGBpa[i] = (-1.0 * 400.0 * temp) / (temp + 27.13) + 0.1;
            }
            else
            {
                var temp = Double.Pow(FL * RGBp[i] / 100.0, 0.42);
                RGBpa[i] = (400.0 * temp) / (temp + 27.13) + 0.1;
            }
        }

        A = (((2.0 * RGBpa[0]) + RGBpa[1] +
              (RGBpa[2] / 20.0)) - 0.305) * Nbb;
    }

    public void InverseNonlinearity(double FL)
    {
        for (var i = 0; i < 3; i++)
        {
            var c1 = (RGBpa[i] - 0.1) < 0 ? -1 : 1;
            RGBp[i] = c1 * (100.0 / FL) *
                      Double.Pow(
                          ((27.13 * Double.Abs(RGBpa[i] - 0.1)) /
                           (400.0 - Double.Abs(RGBpa[i] - 0.1))),
                          (1.0 / 0.42));
        }
    }

    public void HPEtoCAT02()
    {
        Span<double> m =
        [
            ((0.7328 * 1.910197) + (0.4296 * 0.370950)),
            ((0.7328 * -1.112124) + (0.4296 * 0.629054)),
            ((0.7328 * 0.201908) + (0.4296 * 0.000008) - 0.1624),
            ((-0.7036 * 1.910197) + (1.6975 * 0.370950)),
            ((-0.7036 * -1.112124) + (1.6975 * 0.629054)),
            ((-0.7036 * 0.201908) + (1.6975 * 0.000008) + 0.0061),
            ((0.0030 * 1.910197) + (0.0136 * 0.370950)),
            ((0.0030 * -1.112124) + (0.0136 * 0.629054)),
            ((0.0030 * 0.201908) + (0.0136 * 0.000008) + 0.9834),
        ];

        RGBc[0] = (RGBp[0] * m[0]) + (RGBp[1] * m[1]) + (RGBp[2] * m[2]);
        RGBc[1] = (RGBp[0] * m[3]) + (RGBp[1] * m[4]) + (RGBp[2] * m[5]);
        RGBc[2] = (RGBp[0] * m[6]) + (RGBp[1] * m[7]) + (RGBp[2] * m[8]);
    }

    public void InverseChromaticAdaptation(double D)
    {
        for (var i = 0; i < 3; i++)
        {
            RGB[i] = RGBc[i] /
                     ((XYZ[1] * D / RGB[i]) + 1.0 - D);
        }
    }

    public void CAT02toXYZ()
    {
        XYZ[0] = (RGB[0] * 1.096124) + (RGB[1] * -0.278869) + (RGB[2] * 0.182745);
        XYZ[1] = (RGB[0] * 0.454369) + (RGB[1] * 0.473533) + (RGB[2] * 0.072098);
        XYZ[2] = (RGB[0] * -0.009628) + (RGB[1] * -0.005698) + (RGB[2] * 1.015326);
    }
}
