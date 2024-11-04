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
                       4   => 33,
                       _   => 17,
                   };
        }

        // LowResPrecalc is lower resolution
        if ((dwFlags & cmsFLAGS_LOWRESPRECALC) is not 0)
        {
            return nChannles switch
                   {
                       > 4 => 7,
                       4   => 23,
                       _   => 49,
                   };
        }

        // Default values
        return nChannles switch
               {
                   > 4 => 7,
                   4   => 17,
                   _   => 33,
               };
    }

    internal static bool _cmsEndPointsBySpace(Signature Space,
                                              out ushort[] White,
                                              out ushort[] Black,
                                              out uint nOutputs)
    {
        // Only most common spaces
        if (Space == Signatures.Colorspace.Gray)
        {
            White = GrayWhite;
            Black = GrayBlack;
            nOutputs = 1;

            return true;
        }

        if (Space == Signatures.Colorspace.Rgb)
        {
            White = RGBwhite;
            Black = RGBblack;
            nOutputs = 3;

            return true;
        }

        if (Space == Signatures.Colorspace.Lab)
        {
            White = LABwhite;
            Black = LABblack;
            nOutputs = 3;

            return true;
        }

        if (Space == Signatures.Colorspace.Cmyk)
        {
            White = CMYKwhite;
            Black = CMYKblack;
            nOutputs = 4;

            return true;
        }

        if (Space == Signatures.Colorspace.Cmy)
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
            1 or PT_GRAY       => Signatures.Colorspace.Gray,
            2 or PT_RGB        => Signatures.Colorspace.Rgb,
            PT_CMY             => Signatures.Colorspace.Cmy,
            PT_CMYK            => Signatures.Colorspace.Cmyk,
            PT_YCbCr           => Signatures.Colorspace.YCbCr,
            PT_YUV             => Signatures.Colorspace.Luv,
            PT_XYZ             => Signatures.Colorspace.XYZ,
            PT_Lab or PT_LabV2 => Signatures.Colorspace.Lab,
            PT_YUVK            => Signatures.Colorspace.LuvK,
            PT_HSV             => Signatures.Colorspace.Hsv,
            PT_HLS             => Signatures.Colorspace.Hls,
            PT_Yxy             => Signatures.Colorspace.Yxy,
            PT_MCH1            => Signatures.Colorspace.MCH1,
            PT_MCH2            => Signatures.Colorspace.MCH2,
            PT_MCH3            => Signatures.Colorspace.MCH3,
            PT_MCH4            => Signatures.Colorspace.MCH4,
            PT_MCH5            => Signatures.Colorspace.MCH5,
            PT_MCH6            => Signatures.Colorspace.MCH6,
            PT_MCH7            => Signatures.Colorspace.MCH7,
            PT_MCH8            => Signatures.Colorspace.MCH8,
            PT_MCH9            => Signatures.Colorspace.MCH9,
            PT_MCH10           => Signatures.Colorspace.MCHA,
            PT_MCH11           => Signatures.Colorspace.MCHB,
            PT_MCH12           => Signatures.Colorspace.MCHC,
            PT_MCH13           => Signatures.Colorspace.MCHD,
            PT_MCH14           => Signatures.Colorspace.MCHE,
            PT_MCH15           => Signatures.Colorspace.MCHF,
            _                  => default,
        };

    internal static int _cmsLCMScolorSpace(Signature ProfileSpace)
    {
        if (ProfileSpace == Signatures.Colorspace.Gray)
            return PT_GRAY;

        if (ProfileSpace == Signatures.Colorspace.Rgb)
            return PT_RGB;

        if (ProfileSpace == Signatures.Colorspace.Cmy)
            return PT_CMY;

        if (ProfileSpace == Signatures.Colorspace.Cmyk)
            return PT_CMYK;

        if (ProfileSpace == Signatures.Colorspace.YCbCr)
            return PT_YCbCr;

        if (ProfileSpace == Signatures.Colorspace.Luv)
            return PT_YUV;

        if (ProfileSpace == Signatures.Colorspace.XYZ)
            return PT_XYZ;

        if (ProfileSpace == Signatures.Colorspace.Lab)
            return PT_Lab;

        if (ProfileSpace == Signatures.Colorspace.LuvK)
            return PT_YUVK;

        if (ProfileSpace == Signatures.Colorspace.Hsv)
            return PT_HSV;

        if (ProfileSpace == Signatures.Colorspace.Hls)
            return PT_HLS;

        if (ProfileSpace == Signatures.Colorspace.Yxy)
            return PT_Yxy;

        if (ProfileSpace == Signatures.Colorspace.Color1 ||
            ProfileSpace == Signatures.Colorspace.MCH1)
            return PT_MCH1;

        if (ProfileSpace == Signatures.Colorspace.Color2 ||
            ProfileSpace == Signatures.Colorspace.MCH2)
            return PT_MCH2;

        if (ProfileSpace == Signatures.Colorspace.Color3 ||
            ProfileSpace == Signatures.Colorspace.MCH3)
            return PT_MCH3;

        if (ProfileSpace == Signatures.Colorspace.Color4 ||
            ProfileSpace == Signatures.Colorspace.MCH4)
            return PT_MCH4;

        if (ProfileSpace == Signatures.Colorspace.Color5 ||
            ProfileSpace == Signatures.Colorspace.MCH5)
            return PT_MCH5;

        if (ProfileSpace == Signatures.Colorspace.Color6 ||
            ProfileSpace == Signatures.Colorspace.MCH6)
            return PT_MCH6;

        if (ProfileSpace == Signatures.Colorspace.Color7 ||
            ProfileSpace == Signatures.Colorspace.MCH7)
            return PT_MCH7;

        if (ProfileSpace == Signatures.Colorspace.Color8 ||
            ProfileSpace == Signatures.Colorspace.MCH8)
            return PT_MCH8;

        if (ProfileSpace == Signatures.Colorspace.Color9 ||
            ProfileSpace == Signatures.Colorspace.MCH9)
            return PT_MCH9;

        if (ProfileSpace == Signatures.Colorspace.Color10 ||
            ProfileSpace == Signatures.Colorspace.MCHA)
            return PT_MCH10;

        if (ProfileSpace == Signatures.Colorspace.Color11 ||
            ProfileSpace == Signatures.Colorspace.MCHB)
            return PT_MCH11;

        if (ProfileSpace == Signatures.Colorspace.Color12 ||
            ProfileSpace == Signatures.Colorspace.MCHC)
            return PT_MCH12;

        if (ProfileSpace == Signatures.Colorspace.Color13 ||
            ProfileSpace == Signatures.Colorspace.MCHD)
            return PT_MCH13;

        if (ProfileSpace == Signatures.Colorspace.Color14 ||
            ProfileSpace == Signatures.Colorspace.MCHE)
            return PT_MCH14;

        if (ProfileSpace == Signatures.Colorspace.Color15 ||
            ProfileSpace == Signatures.Colorspace.MCHF)
            return PT_MCH15;

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
        if (Colorspace == Signatures.Colorspace.MCH1 ||
            Colorspace == Signatures.Colorspace.Color1 ||
            Colorspace == Signatures.Colorspace.Gray)
            return 1;

        if (Colorspace == Signatures.Colorspace.MCH2 ||
            Colorspace == Signatures.Colorspace.Color2)
            return 2;

        if (Colorspace == Signatures.Colorspace.XYZ ||
            Colorspace == Signatures.Colorspace.Lab ||
            Colorspace == Signatures.Colorspace.Luv ||
            Colorspace == Signatures.Colorspace.YCbCr ||
            Colorspace == Signatures.Colorspace.Yxy ||
            Colorspace == Signatures.Colorspace.Rgb ||
            Colorspace == Signatures.Colorspace.Hsv ||
            Colorspace == Signatures.Colorspace.Hls ||
            Colorspace == Signatures.Colorspace.Cmy ||
            Colorspace == Signatures.Colorspace.MCH3 ||
            Colorspace == Signatures.Colorspace.Color3)
            return 3;

        if (Colorspace == Signatures.Colorspace.LuvK ||
            Colorspace == Signatures.Colorspace.Cmyk ||
            Colorspace == Signatures.Colorspace.MCH4 ||
            Colorspace == Signatures.Colorspace.Color4)
            return 4;

        if (Colorspace == Signatures.Colorspace.MCH5 ||
            Colorspace == Signatures.Colorspace.Color5)
            return 5;

        if (Colorspace == Signatures.Colorspace.MCH6 ||
            Colorspace == Signatures.Colorspace.Color6)
            return 6;

        if (Colorspace == Signatures.Colorspace.MCH7 ||
            Colorspace == Signatures.Colorspace.Color7)
            return 7;

        if (Colorspace == Signatures.Colorspace.MCH8 ||
            Colorspace == Signatures.Colorspace.Color8)
            return 8;

        if (Colorspace == Signatures.Colorspace.MCH9 ||
            Colorspace == Signatures.Colorspace.Color9)
            return 9;

        if (Colorspace == Signatures.Colorspace.MCHA ||
            Colorspace == Signatures.Colorspace.Color10)
            return 10;

        if (Colorspace == Signatures.Colorspace.MCHB ||
            Colorspace == Signatures.Colorspace.Color11)
            return 11;

        if (Colorspace == Signatures.Colorspace.MCHC ||
            Colorspace == Signatures.Colorspace.Color12)
            return 12;

        if (Colorspace == Signatures.Colorspace.MCHD ||
            Colorspace == Signatures.Colorspace.Color13)
            return 13;

        if (Colorspace == Signatures.Colorspace.MCHE ||
            Colorspace == Signatures.Colorspace.Color14)
            return 14;

        if (Colorspace == Signatures.Colorspace.MCHF ||
            Colorspace == Signatures.Colorspace.Color15)
            return 15;

        return -1;
    }
}
