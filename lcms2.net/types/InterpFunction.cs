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
using System.Diagnostics;

namespace lcms2.types;

[DebuggerStepThrough]
public class InterpFunction
{
    private readonly object? _value;

    public InterpFunction(InterpFn<float> fn) =>
        _value = fn;

    public InterpFunction(InterpFn<ushort> fn) =>
        _value = fn;

    public bool IsFloat =>
        _value is InterpFn<float>;

    public bool IsUshort =>
        _value is InterpFn<ushort>;

    public InterpFn<float>? LerpFloat =>
        _value as InterpFn<float>;

    public InterpFn<ushort>? Lerp16 =>
        _value as InterpFn<ushort>;

    public static implicit operator InterpFn<float>?(InterpFunction value) =>
        value.LerpFloat;

    public static implicit operator InterpFn<ushort>?(InterpFunction value) =>
        value.Lerp16;

    public static implicit operator InterpFunction(InterpFn<float> value) =>
        new(value);

    public static implicit operator InterpFunction(InterpFn<ushort> value) =>
        new(value);
}
