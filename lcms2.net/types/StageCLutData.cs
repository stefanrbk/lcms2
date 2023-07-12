﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2022 Marti Maria Saguer
//                2022-2023 Stefan Kewatt
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

public unsafe class StageCLutData
{
    //[StructLayout(LayoutKind.Explicit)]
    //public struct CmsTab
    //{
    //    [FieldOffset(0)]
    //    public ushort* T;

    //    [FieldOffset(0)]
    //    public float* TFloat;
    //}

    //public CmsTab Tab;
    public object? Tab;
    public InterpParams Params;
    public uint nEntries;
    public bool HasFloatValues;

    public Span<ushort> T =>
        Tab is ushort[] wordTab
            ? wordTab.AsSpan(..(int)nEntries)
            : Span<ushort>.Empty;

    public Span<float> TFloat =>
        Tab is float[] floatTab
            ? floatTab.AsSpan(..(int)nEntries)
            : Span<float>.Empty;
}
