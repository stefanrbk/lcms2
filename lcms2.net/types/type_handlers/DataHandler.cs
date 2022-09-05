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
using lcms2.io;
using lcms2.plugins;

namespace lcms2.types.type_handlers;

public class DataHandler : TagTypeHandler
{
    #region Public Constructors

    public DataHandler(Signature sig, object? state = null)
        : base(sig, state, 0) { }

    public DataHandler(object? state = null)
        : this(default, state) { }

    #endregion Public Constructors

    #region Public Methods

    public override object? Duplicate(object value, int num) =>
        ((IccData)value).Clone();

    public override void Free(object value) =>
        ((IccData)value).Dispose();

    public override object? Read(Stream io, int sizeOfTag, out int numItems)
    {
        numItems = 0;

        if (sizeOfTag < sizeof(uint)) return null;

        var lenOfData = sizeOfTag - sizeof(uint);
        if (lenOfData < 0) return null;

        if (!io.ReadUInt32Number(out var flag)) return null;

        var buf = new byte[lenOfData];
        if (io.Read(buf) != lenOfData) return null;

        numItems = 1;

        return new IccData((uint)lenOfData, flag, buf);
    }

    public override bool Write(Stream io, object value, int numItems)
    {
        var binData = (IccData)value;

        if (!io.Write(binData.flag)) return false;

        io.Write(binData.data, 0, (int)binData.length);

        return true;
    }

    #endregion Public Methods
}
