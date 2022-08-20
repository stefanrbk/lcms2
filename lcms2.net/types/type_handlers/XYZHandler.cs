﻿using lcms2.io;
using lcms2.plugins;
using lcms2.state;

namespace lcms2.types.type_handlers;

public class XYZHandler: TagTypeHandler
{
    public XYZHandler(Signature sig, Context? context = null)
        : base(sig, context, 0) { }

    public XYZHandler(Context? context = null)
        : this(default, context) { }

    public override object? Duplicate(object value, int num) =>
        ((XYZ)value).Clone();

    public override void Free(object value)
    { }

    public override object? Read(Stream io, int sizeOfTag, out int numItems)
    {
        numItems = 0;
        if (!io.ReadXYZNumber(out var xyz)) return null;

        numItems = 1;
        return xyz;
    }

    public override bool Write(Stream io, object value, int numItems) =>
        io.Write((XYZ)value);
}
