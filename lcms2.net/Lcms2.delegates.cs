﻿//---------------------------------------------------------------------------------
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

using lcms2.io;
using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

namespace lcms2;

public static partial class Lcms2
{
    public delegate void LogErrorHandlerFunction(Context? ContextID, EventId ErrorCode, string Text);
    public delegate bool SAMPLER16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo);
    public delegate bool SAMPLERFLOAT(ReadOnlySpan<float> In, Span<float> Out, object? Cargo);
    internal delegate bool PositionTableEntryFn(TagTypeHandler self, IOHandler io, object? Cargo, uint n, uint SizeOfTag);

    internal delegate void FormatterAlphaFn(Span<byte> dst, ReadOnlySpan<byte> src);
}
