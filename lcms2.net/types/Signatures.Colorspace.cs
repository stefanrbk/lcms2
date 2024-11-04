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

public static partial class Signatures
{
    public static class Colorspace
    {
        public static readonly Signature XYZ = new("XYZ "u8);
        public static readonly Signature Lab = new("Lab "u8);
        public static readonly Signature Luv = new("Luv "u8);
        public static readonly Signature YCbCr = new("YCbr"u8);
        public static readonly Signature Yxy = new("Yxy "u8);
        public static readonly Signature Rgb = new("RGB "u8);
        public static readonly Signature Gray = new("GRAY"u8);
        public static readonly Signature Hsv = new("HSV "u8);
        public static readonly Signature Hls = new("HLS "u8);
        public static readonly Signature Cmyk = new("CMYK"u8);
        public static readonly Signature Cmy = new("Cmy "u8);
        public static readonly Signature MCH1 = new("MCH1"u8);
        public static readonly Signature MCH2 = new("MCH2"u8);
        public static readonly Signature MCH3 = new("MCH3"u8);
        public static readonly Signature MCH4 = new("MCH4"u8);
        public static readonly Signature MCH5 = new("MCH5"u8);
        public static readonly Signature MCH6 = new("MCH6"u8);
        public static readonly Signature MCH7 = new("MCH7"u8);
        public static readonly Signature MCH8 = new("MCH8"u8);
        public static readonly Signature MCH9 = new("MCH9"u8);
        public static readonly Signature MCHA = new("MCHA"u8);
        public static readonly Signature MCHB = new("MCHB"u8);
        public static readonly Signature MCHC = new("MCHC"u8);
        public static readonly Signature MCHD = new("MCHD"u8);
        public static readonly Signature MCHE = new("MCHE"u8);
        public static readonly Signature MCHF = new("MCHF"u8);
        public static readonly Signature Named = new("nmcl"u8);
        public static readonly Signature Color1 = new("1CLR"u8);
        public static readonly Signature Color2 = new("2CLR"u8);
        public static readonly Signature Color3 = new("3CLR"u8);
        public static readonly Signature Color4 = new("4CLR"u8);
        public static readonly Signature Color5 = new("5CLR"u8);
        public static readonly Signature Color6 = new("6CLR"u8);
        public static readonly Signature Color7 = new("7CLR"u8);
        public static readonly Signature Color8 = new("8CLR"u8);
        public static readonly Signature Color9 = new("9CLR"u8);
        public static readonly Signature Color10 = new("ACLR"u8);
        public static readonly Signature Color11 = new("BCLR"u8);
        public static readonly Signature Color12 = new("CCLR"u8);
        public static readonly Signature Color13 = new("DCLR"u8);
        public static readonly Signature Color14 = new("ECLR"u8);
        public static readonly Signature Color15 = new("FCLR"u8);
        public static readonly Signature LuvK = new("LuvK"u8);
    }
}
