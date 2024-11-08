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

using System.Runtime.InteropServices;

namespace lcms2;

public delegate ReadOnlySpan<byte> Formatter16In(Transform CMMcargo,
                                                 Span<ushort> Values,
                                                 ReadOnlySpan<byte> Buffer,
                                                 uint Stride);

public delegate ReadOnlySpan<byte> FormatterFloatIn(Transform CMMcargo,
                                                    Span<float> Values,
                                                    ReadOnlySpan<byte> Buffer,
                                                    uint Stride);

[StructLayout(LayoutKind.Explicit)]
public struct FormatterIn
{
    [FieldOffset(0)] public Formatter16In? Fmt16;

    [FieldOffset(0)] public FormatterFloatIn? FmtFloat;
}
