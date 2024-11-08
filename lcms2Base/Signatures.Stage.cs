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

namespace lcms2;

public static partial class Signatures
{
    public static class Stage
    {
        public static readonly Signature BAcsElem = new("bACS"u8);
        public static readonly Signature ClipNegativesElem = new("clp"u8);
        public static readonly Signature CLutElem = new("clut"u8);
        public static readonly Signature CurveSetElem = new("cvst"u8);
        public static readonly Signature EAcsElem = new("eACS"u8);
        public static readonly Signature FloatPCS2Lab = new("l2d"u8);
        public static readonly Signature FloatPCS2XYZ = new("x2d"u8);

        // Identities
        public static readonly Signature IdentityElem = new("idn"u8);

        // Float to floatPCS
        public static readonly Signature Lab2FloatPCS = new("d2l"u8);

        public static readonly Signature Lab2XYZElem = new("x2l"u8);
        public static readonly Signature LabV2toV4Elem = new("2 4"u8);
        public static readonly Signature LabV4toV2Elem = new("4 2"u8);
        public static readonly Signature MatrixElem = new("matf"u8);
        public static readonly Signature NamedColorElem = new("ncl"u8);

        public static readonly Signature XYZ2FloatPCS = new("d2x"u8);

        // Custom from here, not in the ICC Spec
        public static readonly Signature XYZ2LabElem = new("l2x"u8);
    }
}
