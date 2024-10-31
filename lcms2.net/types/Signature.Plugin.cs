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
    public static class Plugin
    {
        public static readonly Signature Formatters = new("frmH"u8);
        public static readonly Signature Interpolation = new("inpH"u8);
        public static readonly Signature MagicNumber = new("acpp"u8);
        public static readonly Signature MemHandler = new("memH"u8);
        public static readonly Signature MultiProcessElement = new("mpeH"u8);
        public static readonly Signature Mutex = new("mtxH"u8);
        public static readonly Signature Optimization = new("optH"u8);
        public static readonly Signature ParametricCurve = new("parH"u8);
        public static readonly Signature RenderingIntent = new("intH"u8);
        public static readonly Signature Tag = new("tagH"u8);
        public static readonly Signature TagType = new("typH"u8);
        public static readonly Signature Transform = new("xfmH"u8);
        public static readonly Signature Parallelization = new("prlH"u8);
    }
}
