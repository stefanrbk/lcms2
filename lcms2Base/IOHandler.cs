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

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using S15Fixed16Number = System.Int32;

namespace lcms2;

public class IOHandler
{
    internal object? stream;
    internal Context? ContextID;
    internal uint UsedSpace;
    internal uint reportedSize;
    internal string? physicalFile;

    internal delegate uint ReadFn(IOHandler iohandler, Span<byte> buffer, uint size, uint count);
    internal delegate bool SeekFn(IOHandler iohandler, uint offset);
    internal delegate bool CloseFn(IOHandler iohandler);
    internal delegate uint TellFn(IOHandler iohandler);
    internal delegate bool WriteFn(IOHandler iohandler, uint size, ReadOnlySpan<byte> buffer);

    internal ReadFn ReadFunc;
    internal SeekFn SeekFunc;
    internal CloseFn CloseFunc;
    internal TellFn TellFunc;
    internal WriteFn WriteFunc;

    [DebuggerStepThrough]
    public bool ReadByte(out byte n)    // _cmsReadUInt8Number
    {
        n = 0;
        Span<byte> tmp = stackalloc byte[1];

        if (ReadFunc(this, tmp, sizeof(byte), 1) != 1)
            return false;

        n = tmp[0];
        return true;
    }

    [DebuggerStepThrough]
    public bool ReadUshort(out ushort n)    // _cmsReadUInt16Number
    {
        n = 0;
        Span<byte> tmp = stackalloc byte[2];

        if (ReadFunc(this, tmp, sizeof(ushort), 1) != 1)
            return false;

        n = AdjustEndianness(BitConverter.ToUInt16(tmp));
        return true;
    }

    [DebuggerStepThrough]
    public bool ReadUshortArray(uint n, Span<ushort> array) // _cmsReadUInt16Array
    {
        for (var i = 0; i < n; i++)
        {
            if (!ReadUshort(out array[i]))
                return false;
        }

        return true;
    }

    [DebuggerStepThrough]
    public bool ReadUint(out uint n)    // _cmsReadUInt32Number
    {
        n = 0;
        Span<byte> tmp = stackalloc byte[4];

        if (ReadFunc(this, tmp, sizeof(uint), 1) != 1)
            return false;

        n = AdjustEndianness(BitConverter.ToUInt32(tmp));
        return true;
    }

    [DebuggerStepThrough]
    public bool ReadFloat(out float n)  // _cmsReadFloat32Number
    {
        n = 0;
        Span<byte> tmp = stackalloc byte[4];

        if (ReadFunc(this, tmp, sizeof(uint), 1) != 1)
            return false;

        n = BitConverter.UInt32BitsToSingle(AdjustEndianness(BitConverter.ToUInt32(tmp)));

        // Safeguard which covers against absurd values
        if (n is > 1E+20f or < -1E+20f)
            return false;

        // I guess we don't deal with subnormal values!
        return Single.IsNormal(n) || n is 0;
    }

    [DebuggerStepThrough]
    public bool ReadUlong(out ulong n)  // _cmsReadUInt64Number
    {
        n = 0;
        Span<byte> tmp = stackalloc byte[8];

        if (ReadFunc(this, tmp, sizeof(ulong), 1) != 1)
            return false;

        n = AdjustEndianness(BitConverter.ToUInt64(tmp));
        return true;
    }

    [DebuggerStepThrough]
    public bool ReadFixed15_16(out double n)    // _cmsRead15Fixed16Number
    {
        n = 0;
        Span<byte> tmp = stackalloc byte[4];

        if (ReadFunc(this, tmp, sizeof(uint), 1) != 1)
            return false;

        n = S15Fixed16ToDouble((S15Fixed16Number)AdjustEndianness(BitConverter.ToUInt32(tmp)));

        return true;
    }

    [DebuggerStepThrough]
    public bool ReadXYZ(out CIEXYZ XYZ) // _cmsReadXYZNumber
    {
        XYZ = new CIEXYZ();
        Span<byte> xyz = stackalloc byte[(sizeof(uint) * 3)];

        if (ReadFunc(this, xyz, sizeof(uint) * 3, 1) != 1)
            return false;

        var ints = MemoryMarshal.Cast<byte, uint>(xyz);

        XYZ.X = S15Fixed16ToDouble((S15Fixed16Number)AdjustEndianness(ints[0]));
        XYZ.Y = S15Fixed16ToDouble((S15Fixed16Number)AdjustEndianness(ints[1]));
        XYZ.Z = S15Fixed16ToDouble((S15Fixed16Number)AdjustEndianness(ints[2]));

        return true;
    }

    [DebuggerStepThrough]
    public bool Write(byte n)    // _cmsWriteUInt8Number
    {
        return WriteFunc(this, sizeof(byte), [ n ]);
    }

    [DebuggerStepThrough]
    public bool Write(ushort n) // _cmsWriteUInt16Number
    {
        Span<byte> tmp = stackalloc byte[2];
        BitConverter.TryWriteBytes(tmp, AdjustEndianness(n));

        return WriteFunc(this, sizeof(ushort), tmp);
    }

    [DebuggerStepThrough]
    public bool Write(uint n, ReadOnlySpan<ushort> array)    // _cmsWriteUInt16Array
    {
        for (var i = 0; i < n; i++)
        {
            if (!Write(array[i]))
                return false;
        }

        return true;
    }

    [DebuggerStepThrough]
    public bool Write(uint n)   // _cmsWriteUInt32Number
    {
        Span<byte> tmp = stackalloc byte[4];
        BitConverter.TryWriteBytes(tmp, AdjustEndianness(n));

        return WriteFunc(this, sizeof(uint), tmp);
    }

    [DebuggerStepThrough]
    public bool Write(float n) // _cmsWriteFloat32Number
    {
        Span<byte> tmp = stackalloc byte[4];
        BitConverter.TryWriteBytes(tmp, AdjustEndianness(BitConverter.SingleToUInt32Bits(n)));

        return WriteFunc(this, sizeof(uint), tmp);
    }

    [DebuggerStepThrough]
    public bool Write(ulong n)  // _cmsWriteUInt64Number
    {
        Span<byte> tmp = stackalloc byte[8];
        BitConverter.TryWriteBytes(tmp, AdjustEndianness(n));

        return WriteFunc(this, sizeof(ulong), tmp);
    }

    [DebuggerStepThrough]
    public bool Write(double n)  // _cmsWrite15Fixed16Number
    {
        Span<byte> tmp = stackalloc byte[4];
        BitConverter.TryWriteBytes(tmp, AdjustEndianness((uint)DoubleToS15Fixed16(n)));

        return WriteFunc(this, sizeof(uint), tmp);
    }

    [DebuggerStepThrough]
    public bool Write(CIEXYZ XYZ)  // _cmsWriteXYZNumber
    {
        Span<int> xyz =
        [
            (S15Fixed16Number)AdjustEndianness((uint)DoubleToS15Fixed16(XYZ.X)),
            (S15Fixed16Number)AdjustEndianness((uint)DoubleToS15Fixed16(XYZ.Y)),
            (S15Fixed16Number)AdjustEndianness((uint)DoubleToS15Fixed16(XYZ.Z)),
        ];
        return WriteFunc(this, sizeof(uint) * 3, MemoryMarshal.Cast<int, byte>(xyz));
    }

    [DebuggerStepThrough]
    public Signature ReadTypeBase()  // _cmsReadTypeBase
    {
        Span<byte> Base = stackalloc byte[(sizeof(uint) * 2)];

        if (ReadFunc(this, Base, sizeof(uint) * 2, 1) != 1)
            return default;

        return new(AdjustEndianness(BitConverter.ToUInt32(Base)));
    }

    [DebuggerStepThrough]
    public bool WriteTypeBase(Signature sig)   // _cmsWriteTypeBase
    {
        Span<byte> Base = stackalloc byte[(sizeof(uint) * 2)];

        BitConverter.TryWriteBytes(Base, AdjustEndianness((uint)sig));
        return WriteFunc(this, sizeof(uint) * 2, Base);
    }

    [DebuggerStepThrough]
    public bool ReadAlignment()  // _cmsReadAlignment
    {
        Span<byte> Buffer = stackalloc byte[4];

        var At = TellFunc(this);
        var NextAligned = AlignLong(At);
        var BytesToNextAlignedPos = NextAligned - At;
        if (BytesToNextAlignedPos is 0)
            return true;
        if (BytesToNextAlignedPos > 4)
            return false;

        return ReadFunc(this, Buffer, BytesToNextAlignedPos, 1) == 1;
    }

    [DebuggerStepThrough]
    public bool WriteAlignment() // _cmsWriteAlignment
    {
        Span<byte> Buffer = stackalloc byte[4];

        var At = TellFunc(this);
        var NextAligned = AlignLong(At);
        var BytesToNextAlignedPos = NextAligned - At;
        if (BytesToNextAlignedPos is 0)
            return true;
        if (BytesToNextAlignedPos > 4)
            return false;

        return WriteFunc(this, BytesToNextAlignedPos, Buffer);
    }

    [DebuggerStepThrough]
    public bool PrintF(ReadOnlySpan<byte> frm, params object[] args) =>
        PrintF(SpanToString(frm), args);

    [DebuggerStepThrough]
    public bool PrintF(string frm, params object[] args) // _cmsIOPrintf
    {
        var str = new StringBuilder(String.Format(frm, args));
        str.Replace(',', '.');
        if (str.Length > 2047)
            return false;
        var buffer = Encoding.UTF8.GetBytes(str.ToString());

        return WriteFunc(this, (uint)str.Length, buffer);
    }
}
