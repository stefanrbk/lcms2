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

public class ColorantOrderHandler : TagTypeHandler
{
    #region Public Constructors

    public ColorantOrderHandler(Signature sig, object? state = null)
        : base(sig, state, 0) { }

    public ColorantOrderHandler(object? state = null)
        : this(default, state) { }

    #endregion Public Constructors

    #region Public Methods

    public override object? Duplicate(object value, int num) =>
        ((byte[])value).Clone();

    public override void Free(object value)
    { }

    public override object? Read(Stream io, int sizeOfTag, out int numItems)
    {
        numItems = 0;
        if (!io.ReadUInt32Number(out var count)) return null;
        if (count > maxChannels) return null;

        byte[] colorantOrder = new byte[maxChannels];

        // We use FF as end marker
        for (var i = 0; i < maxChannels; i++)
            colorantOrder[i] = 0xFF;

        if (io.Read(colorantOrder, 0, (int)count) != count) return null;

        numItems = 1;
        return colorantOrder;
    }

    public override bool Write(Stream io, object value, int numItems)
    {
        var colorantOrder = (byte[])value;
        int count;

        // Get the length
        for (var i = count = 0; i < maxChannels; i++)
            if (colorantOrder[i] != 0xFF) count++;

        if (!io.Write(count)) return false;

        var sz = count * sizeof(byte);
        io.Write(colorantOrder, 0, sz);

        return true;
    }

    #endregion Public Methods
}
