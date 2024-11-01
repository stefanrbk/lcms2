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
    private static readonly ushort[] RGBblack = new ushort[4];
    private static readonly ushort[] RGBwhite = new ushort[4] { 0xFFFF, 0xFFFF, 0xFFFF, 0 };
    private static readonly ushort[] CMYKblack = new ushort[4] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };
    private static readonly ushort[] CMYKwhite = new ushort[4];
    private static readonly ushort[] LABblack = new ushort[4] { 0, 0x8080, 0x8080, 0 };
    private static readonly ushort[] LABwhite = new ushort[4] { 0xFFFF, 0x8080, 0x8080, 0 };
    private static readonly ushort[] CMYblack = new ushort[4] { 0xFFFF, 0xFFFF, 0xFFFF, 0 };
    private static readonly ushort[] CMYwhite = new ushort[4];
    private static readonly ushort[] GrayBlack = new ushort[4];
    private static readonly ushort[] GrayWhite = new ushort[4] { 0xFFFF, 0, 0, 0 };


    public static double cmsDeltaE(CIELab Lab1, CIELab Lab2) =>
        // See DeltaE.De76()
        DeltaE.De76(Lab1, Lab2);

    public static double cmsCIE94DeltaE(CIELab Lab1, CIELab Lab2) =>
        // See DeltaE.CIE94()
        DeltaE.CIE94(Lab1, Lab2);

    public static double cmsBFDdeltaE(CIELab Lab1, CIELab Lab2) =>
        // See DeltaE.BFD()
        DeltaE.BFD(Lab1, Lab2);

    public static double cmsCMCdeltaE(CIELab Lab1, CIELab Lab2, double l, double c) =>
        // See DeltaE.CMC()
        DeltaE.CMC(Lab1, Lab2, l, c);

    public static double cmsCIE2000DeltaE(CIELab Lab1, CIELab Lab2, double Kl, double Kc, double Kh) =>
        // See DeltaE.CIE2000
        DeltaE.CIE2000(Lab1, Lab2, Kl, Kc, Kh);

    internal static uint _cmsReasonableGridpointsByColorspace(Signature Colorspace, uint dwFlags)
    {
        // Already specified?
        if ((dwFlags & 0x00FF0000) is not 0)
            return (dwFlags >> 16) & 0xFF;

        var nChannles = cmsChannelsOf(Colorspace);

        // HighResPrecalc is maximum resolution
        if ((dwFlags & cmsFLAGS_HIGHRESPRECALC) is not 0)
        {
            return nChannles switch
            {
                > 4 => 6,
                4 => 33,
                _ => 17
            };
        }

        // LowResPrecalc is lower resolution
        if ((dwFlags & cmsFLAGS_LOWRESPRECALC) is not 0)
        {
            return nChannles switch
            {
                > 4 => 7,
                4 => 23,
                _ => 49
            };
        }

        // Default values
        return nChannles switch
        {
            > 4 => 7,
            4 => 17,
            _ => 33,
        };
    }

    internal static bool _cmsEndPointsBySpace(Signature Space, out ushort[] White, out ushort[] Black, out uint nOutputs)
    {
        // Only most common spaces
        if (Space == Signature.Colorspace.Gray)
        {
            White = GrayWhite;
            Black = GrayBlack;
            nOutputs = 1;

            return true;
        }

        if (Space == Signature.Colorspace.Rgb)
        {
            White = RGBwhite;
            Black = RGBblack;
            nOutputs = 3;

            return true;
        }

        if (Space == Signature.Colorspace.Lab)
        {
            White = LABwhite;
            Black = LABblack;
            nOutputs = 3;

            return true;
        }

        if (Space == Signature.Colorspace.Cmyk)
        {
            White = CMYKwhite;
            Black = CMYKblack;
            nOutputs = 4;

            return true;
        }

        if (Space == Signature.Colorspace.Cmy)
        {
            White = CMYwhite;
            Black = CMYblack;
            nOutputs = 3;

            return true;
        }

        White = null!;
        Black = null!;
        nOutputs = 0;

        return false;
    }

    internal static Signature _cmsICCcolorSpace(int OutNotation) =>
        OutNotation switch
        {
            1 or
            PT_GRAY => Signature.Colorspace.Gray,
            2 or
            PT_RGB => Signature.Colorspace.Rgb,
            PT_CMY => Signature.Colorspace.Cmy,
            PT_CMYK => Signature.Colorspace.Cmyk,
            PT_YCbCr => Signature.Colorspace.YCbCr,
            PT_YUV => Signature.Colorspace.Luv,
            PT_XYZ => Signature.Colorspace.XYZ,
            PT_Lab or
            PT_LabV2 => Signature.Colorspace.Lab,
            PT_YUVK => Signature.Colorspace.LuvK,
            PT_HSV => Signature.Colorspace.Hsv,
            PT_HLS => Signature.Colorspace.Hls,
            PT_Yxy => Signature.Colorspace.Yxy,
            PT_MCH1 => Signature.Colorspace.MCH1,
            PT_MCH2 => Signature.Colorspace.MCH2,
            PT_MCH3 => Signature.Colorspace.MCH3,
            PT_MCH4 => Signature.Colorspace.MCH4,
            PT_MCH5 => Signature.Colorspace.MCH5,
            PT_MCH6 => Signature.Colorspace.MCH6,
            PT_MCH7 => Signature.Colorspace.MCH7,
            PT_MCH8 => Signature.Colorspace.MCH8,
            PT_MCH9 => Signature.Colorspace.MCH9,
            PT_MCH10 => Signature.Colorspace.MCHA,
            PT_MCH11 => Signature.Colorspace.MCHB,
            PT_MCH12 => Signature.Colorspace.MCHC,
            PT_MCH13 => Signature.Colorspace.MCHD,
            PT_MCH14 => Signature.Colorspace.MCHE,
            PT_MCH15 => Signature.Colorspace.MCHF,
            _ => default
        };

    internal static int _cmsLCMScolorSpace(Signature ProfileSpace)
    {
        if (ProfileSpace == Signature.Colorspace.Gray)
        {
            return PT_GRAY;
        }

        if (ProfileSpace == Signature.Colorspace.Rgb)
        {
            return PT_RGB;
        }

        if (ProfileSpace == Signature.Colorspace.Cmy)
        {
            return PT_CMY;
        }

        if (ProfileSpace == Signature.Colorspace.Cmyk)
        {
            return PT_CMYK;
        }

        if (ProfileSpace == Signature.Colorspace.YCbCr)
        {
            return PT_YCbCr;
        }

        if (ProfileSpace == Signature.Colorspace.Luv)
        {
            return PT_YUV;
        }

        if (ProfileSpace == Signature.Colorspace.XYZ)
        {
            return PT_XYZ;
        }

        if (ProfileSpace == Signature.Colorspace.Lab)
        {
            return PT_Lab;
        }

        if (ProfileSpace == Signature.Colorspace.LuvK)
        {
            return PT_YUVK;
        }

        if (ProfileSpace == Signature.Colorspace.Hsv)
        {
            return PT_HSV;
        }

        if (ProfileSpace == Signature.Colorspace.Hls)
        {
            return PT_HLS;
        }

        if (ProfileSpace == Signature.Colorspace.Yxy)
        {
            return PT_Yxy;
        }

        if (ProfileSpace == Signature.Colorspace.Color1 ||
            ProfileSpace == Signature.Colorspace.MCH1)
        {
            return PT_MCH1;
        }

        if (ProfileSpace == Signature.Colorspace.Color2 ||
            ProfileSpace == Signature.Colorspace.MCH2)
        {
            return PT_MCH2;
        }

        if (ProfileSpace == Signature.Colorspace.Color3 ||
            ProfileSpace == Signature.Colorspace.MCH3)
        {
            return PT_MCH3;
        }

        if (ProfileSpace == Signature.Colorspace.Color4 ||
            ProfileSpace == Signature.Colorspace.MCH4)
        {
            return PT_MCH4;
        }

        if (ProfileSpace == Signature.Colorspace.Color5 ||
            ProfileSpace == Signature.Colorspace.MCH5)
        {
            return PT_MCH5;
        }

        if (ProfileSpace == Signature.Colorspace.Color6 ||
            ProfileSpace == Signature.Colorspace.MCH6)
        {
            return PT_MCH6;
        }

        if (ProfileSpace == Signature.Colorspace.Color7 ||
            ProfileSpace == Signature.Colorspace.MCH7)
        {
            return PT_MCH7;
        }

        if (ProfileSpace == Signature.Colorspace.Color8 ||
            ProfileSpace == Signature.Colorspace.MCH8)
        {
            return PT_MCH8;
        }

        if (ProfileSpace == Signature.Colorspace.Color9 ||
            ProfileSpace == Signature.Colorspace.MCH9)
        {
            return PT_MCH9;
        }

        if (ProfileSpace == Signature.Colorspace.Color10 ||
            ProfileSpace == Signature.Colorspace.MCHA)
        {
            return PT_MCH10;
        }

        if (ProfileSpace == Signature.Colorspace.Color11 ||
            ProfileSpace == Signature.Colorspace.MCHB)
        {
            return PT_MCH11;
        }

        if (ProfileSpace == Signature.Colorspace.Color12 ||
            ProfileSpace == Signature.Colorspace.MCHC)
        {
            return PT_MCH12;
        }

        if (ProfileSpace == Signature.Colorspace.Color13 ||
            ProfileSpace == Signature.Colorspace.MCHD)
        {
            return PT_MCH13;
        }

        if (ProfileSpace == Signature.Colorspace.Color14 ||
            ProfileSpace == Signature.Colorspace.MCHE)
        {
            return PT_MCH14;
        }

        if (ProfileSpace == Signature.Colorspace.Color15 ||
            ProfileSpace == Signature.Colorspace.MCHF)
        {
            return PT_MCH15;
        }

        return 0;
    }

    [Obsolete("Deprecated, use cmsChannelsOfColorSpace instead")]
    public static uint cmsChannelsOf(Signature Colorspace)
    {
        var n = cmsChannelsOfColorSpace(Colorspace);
        if (n < 0)
            return 3;
        return (uint)n;
    }

    public static int cmsChannelsOfColorSpace(Signature Colorspace)
    {
        if (Colorspace == Signature.Colorspace.MCH1 ||
            Colorspace == Signature.Colorspace.Color1 ||
            Colorspace == Signature.Colorspace.Gray)
        {
            return 1;
        }

        if (Colorspace == Signature.Colorspace.MCH2 ||
            Colorspace == Signature.Colorspace.Color2)
        {
            return 2;
        }

        if (Colorspace == Signature.Colorspace.XYZ ||
            Colorspace == Signature.Colorspace.Lab ||
            Colorspace == Signature.Colorspace.Luv ||
            Colorspace == Signature.Colorspace.YCbCr ||
            Colorspace == Signature.Colorspace.Yxy ||
            Colorspace == Signature.Colorspace.Rgb ||
            Colorspace == Signature.Colorspace.Hsv ||
            Colorspace == Signature.Colorspace.Hls ||
            Colorspace == Signature.Colorspace.Cmy ||
            Colorspace == Signature.Colorspace.MCH3 ||
            Colorspace == Signature.Colorspace.Color3)
        {
            return 3;
        }

        if (Colorspace == Signature.Colorspace.LuvK ||
            Colorspace == Signature.Colorspace.Cmyk ||
            Colorspace == Signature.Colorspace.MCH4 ||
            Colorspace == Signature.Colorspace.Color4)
        {
            return 4;
        }

        if (Colorspace == Signature.Colorspace.MCH5 ||
            Colorspace == Signature.Colorspace.Color5)
        {
            return 5;
        }

        if (Colorspace == Signature.Colorspace.MCH6 ||
            Colorspace == Signature.Colorspace.Color6)
        {
            return 6;
        }

        if (Colorspace == Signature.Colorspace.MCH7 ||
            Colorspace == Signature.Colorspace.Color7)
        {
            return 7;
        }

        if (Colorspace == Signature.Colorspace.MCH8 ||
            Colorspace == Signature.Colorspace.Color8)
        {
            return 8;
        }

        if (Colorspace == Signature.Colorspace.MCH9 ||
            Colorspace == Signature.Colorspace.Color9)
        {
            return 9;
        }

        if (Colorspace == Signature.Colorspace.MCHA ||
            Colorspace == Signature.Colorspace.Color10)
        {
            return 10;
        }

        if (Colorspace == Signature.Colorspace.MCHB ||
            Colorspace == Signature.Colorspace.Color11)
        {
            return 11;
        }

        if (Colorspace == Signature.Colorspace.MCHC ||
            Colorspace == Signature.Colorspace.Color12)
        {
            return 12;
        }

        if (Colorspace == Signature.Colorspace.MCHD ||
            Colorspace == Signature.Colorspace.Color13)
        {
            return 13;
        }

        if (Colorspace == Signature.Colorspace.MCHE ||
            Colorspace == Signature.Colorspace.Color14)
        {
            return 14;
        }

        if (Colorspace == Signature.Colorspace.MCHF ||
            Colorspace == Signature.Colorspace.Color15)
        {
            return 15;
        }

        return -1;
    }
}
