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

using lcms2.io;
using lcms2.types;

namespace lcms2;
public static partial class Plugin
{
    [DebuggerStepThrough]
    public static ushort AdjustEndianess(ushort Word)   // _cmsAdjustEndianess16
    {
        Span<byte> pByte = stackalloc byte[2];
        BitConverter.TryWriteBytes(pByte, Word);

        (pByte[1], pByte[0]) = (pByte[0], pByte[1]);
        return BitConverter.ToUInt16(pByte);
    }

    [DebuggerStepThrough]
    public static Signature AdjustEndianess(Signature sig) =>
        (Signature)AdjustEndianess((uint)sig);

    [DebuggerStepThrough]
    public static uint AdjustEndianess(uint DWord)  // _cmsAdjustEndianess32
    {
        Span<byte> pByte = stackalloc byte[4];
        BitConverter.TryWriteBytes(pByte, DWord);

        (pByte[3], pByte[2], pByte[1], pByte[0]) = (pByte[0], pByte[1], pByte[2], pByte[3]);
        return BitConverter.ToUInt32(pByte);
    }

    [DebuggerStepThrough]
    public static ulong AdjustEndianess(ulong QWord)    // _cmsAdjustEndianess64
    {
        Span<byte> pByte = stackalloc byte[8];
        BitConverter.TryWriteBytes(pByte, QWord);

        (pByte[7], pByte[0]) = (pByte[0], pByte[7]);
        (pByte[6], pByte[1]) = (pByte[1], pByte[6]);
        (pByte[5], pByte[2]) = (pByte[2], pByte[5]);
        (pByte[4], pByte[3]) = (pByte[3], pByte[4]);

        return BitConverter.ToUInt64(pByte);
    }

    [DebuggerStepThrough]
    public static bool _cmsReadSignature(IOHandler io, out Signature sig)
    {
        sig = default;

        if (!io.ReadUint(out var value))
            return false;

        sig = (Signature)value;
        return true;
    }
}
