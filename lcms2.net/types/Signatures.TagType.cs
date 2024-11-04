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
    public static class TagType
    {
        public static readonly Signature Chromaticity = new("chrm"u8);
        public static readonly Signature ColorantOrder = new("clro"u8);
        public static readonly Signature ColorantTable = new("clrt"u8);
        public static readonly Signature CorbisBrokenXYZ = new(0x17A505B8);
        public static readonly Signature CrdInfo = new("crdi"u8);
        public static readonly Signature Curve = new("curv"u8);
        public static readonly Signature Data = new("data"u8);
        public static readonly Signature DateTime = new("dtim"u8);
        public static readonly Signature DeviceSettings = new("devs"u8);
        public static readonly Signature Dict = new("dict"u8);
        public static readonly Signature Lut16 = new("mft2"u8);
        public static readonly Signature Lut8 = new("mft1"u8);
        public static readonly Signature LutAtoB = new("mAB"u8);
        public static readonly Signature LutBtoA = new("mBA"u8);
        public static readonly Signature Measurement = new("meas"u8);
        public static readonly Signature MonacoBrokenCurve = new(0x9478EE00);
        public static readonly Signature MultiLocalizedUnicode = new("mluc"u8);
        public static readonly Signature MultiProcessElement = new("mpet"u8);

        [Obsolete("Use NamedColor2")] public static readonly Signature NamedColor = new("ncol"u8);

        public static readonly Signature NamedColor2 = new("ncl2"u8);
        public static readonly Signature ParametricCurve = new("para"u8);
        public static readonly Signature ProfileSequenceDesc = new("pseq"u8);
        public static readonly Signature ProfileSequenceId = new("psid"u8);
        public static readonly Signature ResponseCurveSet16 = new("rcs2"u8);
        public static readonly Signature S15Fixed16Array = new("sf32"u8);
        public static readonly Signature Screening = new("scrn"u8);
        public static readonly Signature Signature = new("sig"u8);
        public static readonly Signature Text = new("text"u8);
        public static readonly Signature TextDescription = new("desc"u8);
        public static readonly Signature U16Fixed16Array = new("uf32"u8);
        public static readonly Signature UcrBg = new("bfd"u8);
        public static readonly Signature UInt16Array = new("ui16"u8);
        public static readonly Signature UInt32Array = new("ui32"u8);
        public static readonly Signature UInt64Array = new("ui64"u8);
        public static readonly Signature UInt8Array = new("ui08"u8);
        public static readonly Signature Vcgt = new("vcgt"u8);
        public static readonly Signature ViewingConditions = new("view"u8);
        public static readonly Signature XYZ = new("XYZ"u8);
        public static readonly Signature cicp = new("cicp"u8);
        public static readonly Signature MHC2 = new("MHC2"u8);
    }
}
