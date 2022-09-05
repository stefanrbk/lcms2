﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2022 Marti Maria Saguer
//                2022      Stefan Kewatt
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
//
namespace lcms2.types;

public partial struct Signature
{
    #region Classes

    public static class TagType
    {
        #region Fields

        public static readonly Signature Chromaticity = new("chrm");
        public static readonly Signature ColorantOrder = new("clro");
        public static readonly Signature ColorantTable = new("clrt");
        public static readonly Signature CorbisBrokenXYZ = new(0x17A505B8);
        public static readonly Signature CrdInfo = new("crdi");
        public static readonly Signature Curve = new("curv");
        public static readonly Signature Data = new("data");
        public static readonly Signature DateTime = new("dtim");
        public static readonly Signature DeviceSettings = new("devs");
        public static readonly Signature Dict = new("dict");
        public static readonly Signature Lut16 = new("mft2");
        public static readonly Signature Lut8 = new("mft1");
        public static readonly Signature LutAtoB = new("mAB ");
        public static readonly Signature LutBtoA = new("mBA ");
        public static readonly Signature Measurement = new("meas");
        public static readonly Signature MonacoBrokenCurve = new(0x9478EE00);
        public static readonly Signature MultiLocalizedUnicode = new("mluc");
        public static readonly Signature MultiProcessElement = new("mpet");

        [Obsolete("Use NamedColor2")]
        public static readonly Signature NamedColor = new("ncol");

        public static readonly Signature NamedColor2 = new("ncl2");
        public static readonly Signature ParametricCurve = new("para");
        public static readonly Signature ProfileSequenceDesc = new("pseq");
        public static readonly Signature ProfileSequenceId = new("psid");
        public static readonly Signature ResponseCurveSet16 = new("rcs2");
        public static readonly Signature S15Fixed16Array = new("sf32");
        public static readonly Signature Screening = new("scrn");
        public static readonly Signature Signature = new("sig ");
        public static readonly Signature Text = new("text");
        public static readonly Signature TextDescription = new("desc");
        public static readonly Signature U16Fixed16Array = new("uf32");
        public static readonly Signature UcrBg = new("bfd ");
        public static readonly Signature UInt16Array = new("ui16");
        public static readonly Signature UInt32Array = new("ui32");
        public static readonly Signature UInt64Array = new("ui64");
        public static readonly Signature UInt8Array = new("ui08");
        public static readonly Signature Vcgt = new("vcgt");
        public static readonly Signature ViewingConditions = new("view");
        public static readonly Signature XYZ = new("XYZ ");

        #endregion Fields
    }

    #endregion Classes
}
