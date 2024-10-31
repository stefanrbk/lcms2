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

public partial struct Signature
{
    public static class Tag
    {
        public static readonly Signature ArgyllArts = new("arts"u8);
        public static readonly Signature AToB0 = new("A2B0"u8);
        public static readonly Signature AToB1 = new("A2B1"u8);
        public static readonly Signature AToB2 = new("A2B2"u8);
        public static readonly Signature BlueColorant = new("bXYZ"u8);
        public static readonly Signature BlueMatrixColumn = new("bXYZ"u8);
        public static readonly Signature BlueTRC = new("bTRC"u8);
        public static readonly Signature BToA0 = new("B2A0"u8);
        public static readonly Signature BToA1 = new("B2A1"u8);
        public static readonly Signature BToA2 = new("B2A2"u8);
        public static readonly Signature BToD0 = new("B2D0"u8);
        public static readonly Signature BToD1 = new("B2D1"u8);
        public static readonly Signature BToD2 = new("B2D2"u8);
        public static readonly Signature BToD3 = new("B2D3"u8);
        public static readonly Signature CalibrationDateTime = new("calt"u8);
        public static readonly Signature CharTarget = new("targ"u8);
        public static readonly Signature ChromaticAdaptation = new("chad"u8);
        public static readonly Signature Chromaticity = new("chrm"u8);
        public static readonly Signature ColorantOrder = new("clro"u8);
        public static readonly Signature ColorantTable = new("clrt"u8);
        public static readonly Signature ColorantTableOut = new("clot"u8);
        public static readonly Signature ColorimetricIntentImageState = new("ciis"u8);
        public static readonly Signature Copyright = new("cprt"u8);
        public static readonly Signature CrdInfo = new("crdi"u8);
        public static readonly Signature Data = new("data"u8);
        public static readonly Signature DateTime = new("dtim"u8);
        public static readonly Signature DeviceMfgDesc = new("dmnd"u8);
        public static readonly Signature DeviceModelDesc = new("dmdd"u8);
        public static readonly Signature DeviceSettings = new("devs"u8);
        public static readonly Signature DToB0 = new("D2B0"u8);
        public static readonly Signature DToB1 = new("D2B1"u8);
        public static readonly Signature DToB2 = new("D2B2"u8);
        public static readonly Signature DToB3 = new("D2B3"u8);
        public static readonly Signature Gamut = new("gamt"u8);
        public static readonly Signature GrayTRC = new("kTRC"u8);
        public static readonly Signature GreenColorant = new("gXYZ"u8);
        public static readonly Signature GreenMatrixColumn = new("gXYZ"u8);
        public static readonly Signature GreenTRC = new("gTRC"u8);
        public static readonly Signature Luminance = new("lumi"u8);
        public static readonly Signature Measurement = new("meas"u8);
        public static readonly Signature MediaBlackPoint = new("bkpt"u8);
        public static readonly Signature MediaWhitePoint = new("wtpt"u8);
        public static readonly Signature Meta = new("meta"u8);

        [Obsolete("Use NamedColor2")]
        public static readonly Signature NamedColor = new("ncol"u8);

        public static readonly Signature NamedColor2 = new("ncl2"u8);
        public static readonly Signature OutputResponse = new("resp"u8);
        public static readonly Signature PerceptualRenderingIntentGamut = new("rig0"u8);
        public static readonly Signature Preview0 = new("pre0"u8);
        public static readonly Signature Preview1 = new("pre1"u8);
        public static readonly Signature Preview2 = new("pre2"u8);
        public static readonly Signature ProfileDescription = new("desc"u8);
        public static readonly Signature ProfileDescriptionML = new("decm"u8);
        public static readonly Signature ProfileSequenceDesc = new("pseq"u8);
        public static readonly Signature ProfileSequenceId = new("psid"u8);
        public static readonly Signature Ps2CRD0 = new("psd0"u8);
        public static readonly Signature Ps2CRD1 = new("psd1"u8);
        public static readonly Signature Ps2CRD2 = new("psd2"u8);
        public static readonly Signature Ps2CRD3 = new("psd3"u8);
        public static readonly Signature Ps2CSA = new("ps2s"u8);
        public static readonly Signature Ps2RenderingIntent = new("ps2i"u8);
        public static readonly Signature RedColorant = new("rXYZ"u8);
        public static readonly Signature RedMatrixColumn = new("rXYZ"u8);
        public static readonly Signature RedTRC = new("rTRC"u8);
        public static readonly Signature SaturationRenderingIntentGamut = new("rig2"u8);
        public static readonly Signature Screening = new("scrn"u8);
        public static readonly Signature ScreeningDesc = new("scrd"u8);
        public static readonly Signature Technology = new("tech"u8);
        public static readonly Signature UcrBg = new("bfd"u8);
        public static readonly Signature Vcgt = new("vcgt"u8);
        public static readonly Signature ViewingCondDesc = new("vued"u8);
        public static readonly Signature ViewingConditions = new("view"u8);
    }
}
