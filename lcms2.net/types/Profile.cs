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

using lcms2.io;
using lcms2.state;

using System.Runtime.InteropServices;

namespace lcms2.types;

public class Profile : ICloneable
{
    public IOHandler? IOHandler;
    public Context? ContextID;

    public DateTime Created;

    public Signature CMM;

    public uint Version;
    public Signature DeviceClass;
    public Signature ColorSpace;
    public Signature PCS;
    public uint RenderingIntent;

    public Signature platform;
    public uint flags;
    public Signature manufacturer, model;
    public ulong attributes;
    public Signature creator;

    public ProfileID ProfileID;

    public static CIEXYZ PerceptualBlack => new(0.00336, 0.0034731, 0.00287);

    //public uint TagCount;
    //public readonly Signature[] TagNames = new Signature[MAX_TABLE_TAG];
    //public readonly Signature[] TagLinked = new Signature[MAX_TABLE_TAG];
    //public readonly uint[] TagSizes = new uint[MAX_TABLE_TAG];
    //public readonly uint[] TagOffsets = new uint[MAX_TABLE_TAG];
    //public readonly bool[] TagSaveAsRaw = new bool[MAX_TABLE_TAG];
    //public readonly object?[] TagPtrs = new object?[MAX_TABLE_TAG];
    //public readonly TagTypeHandler*[] TagTypeHandlers = new TagTypeHandler*[MAX_TABLE_TAG];
    public readonly List<TagEntry> Tags = new();

    public bool IsWrite;

    public object? UserMutex;

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public struct Header
    {
        [FieldOffset(0)] public uint size;
        [FieldOffset(4)] public Signature cmmId;
        [FieldOffset(8)] public uint version;
        [FieldOffset(12)] public Signature deviceClass;
        [FieldOffset(16)] public Signature colorSpace;
        [FieldOffset(20)] public Signature pcs;
        [FieldOffset(24)] public DateTimeNumber date;
        [FieldOffset(36)] public Signature magic;
        [FieldOffset(40)] public Signature platform;
        [FieldOffset(44)] public uint flags;
        [FieldOffset(48)] public Signature manufacturer;
        [FieldOffset(52)] public Signature model;
        [FieldOffset(56)] public ulong attributes;
        [FieldOffset(64)] public uint renderingIntent;
        [FieldOffset(68)] public EncodedXYZNumber illuminant;
        [FieldOffset(80)] public Signature creator;
        [FieldOffset(84)] public ProfileID profileID;
    }

    public struct TagEntry
    {
        public Signature Name;
        public Signature Linked;
        public uint Size;
        public uint Offset;
        public bool SaveAsRaw;
        public object? TagObject;
        public TagTypeHandler? TypeHandler;
    }

    public object Clone()
    {
        var result = new Profile()
        {
            attributes = attributes,
            ColorSpace = ColorSpace,
            ContextID = ContextID,
            Created = Created,
            creator = creator,
            DeviceClass = DeviceClass,
            flags = flags,
            IOHandler = IOHandler,
            IsWrite = IsWrite,
            UserMutex = UserMutex,
            manufacturer = manufacturer,
            model = model,
            PCS = PCS,
            ProfileID = ProfileID,
            RenderingIntent = RenderingIntent,
            Version = Version,
        };

        result.Tags.AddRange(Tags);

        return result;
    }
}
