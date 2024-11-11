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

using Microsoft.Extensions.Logging;

namespace lcms2.testbed;

internal static partial class Testbed
{
    public static bool CheckLab2LCh()
    {
        var Max = 0.0;
        CIELab Lab, Lab2;
        CIELCh LCh;

        for (var l = 0; l < 100; l += 10)
        {
            for (var a = -128; a < 128; a += 8)
            {
                for (var b = -128; b < 128; b += 8)
                {
                    Lab.L = l;
                    Lab.a = a;
                    Lab.b = b;

                    LCh = cmsLab2LCh(Lab);
                    Lab2 = cmsLCh2Lab(LCh);

                    var dist = cmsDeltaE(Lab, Lab2);
                    Max = Math.Max(dist, Max);
                }
            }
        }

        return Max < 1e-12;
    }

    public static bool CheckLab2XYZ()
    {
        var Max = 0.0;
        CIELab Lab, Lab2;
        CIEXYZ XYZ;

        for (var l = 0; l < 100; l += 10)
        {
            for (var a = -128; a < 128; a += 8)
            {
                for (var b = -128; b < 128; b += 8)
                {
                    Lab.L = l;
                    Lab.a = a;
                    Lab.b = b;

                    cmsLab2XYZ(null, out XYZ, Lab);
                    cmsXYZ2Lab(null, out Lab2, XYZ);

                    var dist = cmsDeltaE(Lab, Lab2);
                    Max = Math.Max(dist, Max);
                }
            }
        }

        return Max < 1e-12;
    }

    public static bool CheckLab2xyY()
    {
        var Max = 0.0;
        CIELab Lab, Lab2;
        CIEXYZ XYZ;
        CIExyY xyY;

        for (var l = 0; l <= 100; l += 10)
        {
            for (var a = -128; a <= 128; a += 8)
            {
                for (var b = -128; b <= 128; b += 8)
                {
                    Lab.L = l;
                    Lab.a = a;
                    Lab.b = b;

                    cmsLab2XYZ(null, out XYZ, Lab);
                    xyY = cmsXYZ2xyY(XYZ);
                    XYZ = cmsxyY2XYZ(xyY);
                    cmsXYZ2Lab(null, out Lab2, XYZ);

                    var dist = cmsDeltaE(Lab, Lab2);
                    if (!Double.IsNaN(dist))
                        Max = Math.Max(dist, Max);
                    if (Max > 1e-12)
                    {
                        logger.LogWarning("{L},{a},{b}\t{L2},{a2},{b2}", Lab.L, Lab.a, Lab.b, Lab2.L, Lab2.a, Lab2.b);
                        return false;
                    }
                }
            }
        }

        return Max < 1e-12;
    }

    public static bool CheckLabV2encoding()
    {
        var n2 = 0;
        Span<ushort> Inw = stackalloc ushort[3];
        Span<ushort> aw = stackalloc ushort[3];
        CIELab Lab;

        for (var j = 0; j < 65535; j++)
        {
            Inw[0] = Inw[1] = Inw[2] = (ushort)j;

            Lab = cmsLabEncoded2FloatV2(Inw);
            cmsFloat2LabEncodedV2(aw, Lab);

            for (var i = 0; i < 3; i++)
            {
                if (aw[i] != j)
                    n2++;
            }
        }

        return n2 is 0;
    }

    public static bool CheckLabV4encoding()
    {
        var n2 = 0;
        Span<ushort> Inw = stackalloc ushort[3];
        Span<ushort> aw = stackalloc ushort[3];
        CIELab Lab;

        for (var j = 0; j < 65535; j++)
        {
            Inw[0] = Inw[1] = Inw[2] = (ushort)j;

            Lab = cmsLabEncoded2Float(Inw);
            cmsFloat2LabEncoded(aw, Lab);

            for (var i = 0; i < 3; i++)
            {
                if (aw[i] != j)
                    n2++;
            }
        }

        return n2 is 0;
    }
}
