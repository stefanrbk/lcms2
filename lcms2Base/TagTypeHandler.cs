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

public class TagTypeHandler(Signature signature,
                            TagTypeHandler.ReadFn readPtr,
                            TagTypeHandler.WriteFn writePtr,
                            TagTypeHandler.DupFn dupPtr,
                            TagTypeHandler.FreeFn? freePtr,
                            Context? contextID,
                            uint iCCVersion) : ICloneable
{
    public Signature Signature = signature;
    public Context? ContextID = contextID;

    public delegate object? ReadFn(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag);
    public delegate bool WriteFn(TagTypeHandler self, IOHandler io, object? Ptr, uint nItems);
    public delegate object? DupFn(TagTypeHandler self, object? Ptr, uint nItems);
    public delegate void FreeFn(TagTypeHandler self, object? Ptr);

    public ReadFn ReadPtr = readPtr;
    public WriteFn WritePtr = writePtr;
    public DupFn DupPtr = dupPtr;
    public FreeFn? FreePtr = freePtr;

    public uint ICCVersion = iCCVersion;

    public object Clone() =>
        new TagTypeHandler(Signature, ReadPtr, WritePtr, DupPtr, FreePtr, ContextID, ICCVersion);
}
