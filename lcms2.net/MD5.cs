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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace lcms2;

internal readonly struct MD5(Context? context) : IDisposable    // cmsMD5alloc
{
    private readonly uint[] _buf = [ 0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476 ];
    private readonly uint[] _bits = new uint[2];
    private readonly byte[] _in = new byte[64];

    public readonly Span<uint> Buf =>
        _buf.AsSpan();

    public readonly Span<uint> Bits =>
        _bits.AsSpan();

    public readonly Span<byte> In =>
        _in.AsSpan();

    public readonly Context? ContextID = context;

    public readonly void Add(Span<byte> buf, uint len)  // cmsMD5add
    {
        var t = Bits[0];
        if ((Bits[0] = t + (len << 3)) < t)
            Bits[1]++;

        Bits[1] += len >> 29;

        t = (t >> 3) & 0x3f;

        if (t is not 0)
        {
            var p = In[(int)t..];

            t = 64 - t;
            if (len < t)
            {
                buf[..(int)len].CopyTo(p);
                return;
            }

            buf[..(int)t].CopyTo(p);

            Transform(MemoryMarshal.Cast<byte, uint>(buf), MemoryMarshal.Cast<byte, uint>(In));
            buf = buf[(int)t..];
            len -= t;
        }

        while (len >= 64)
        {
            buf[..64].CopyTo(In);

            Transform(MemoryMarshal.Cast<byte, uint>(buf), MemoryMarshal.Cast<byte, uint>(In));
            buf = buf[64..];
            len -= 64;
        }

        buf[..(int)len].CopyTo(In);
    }

    private static void Transform(Span<uint> buf, ReadOnlySpan<uint> @in)   // cmsMD5_Transform
    {
        var a = buf[0];
        var b = buf[1];
        var c = buf[2];
        var d = buf[3];

        STEP(F1, ref a, b, c, d, @in[0] + 0xd76aa478, 7);
        STEP(F1, ref d, a, b, c, @in[1] + 0xe8c7b756, 12);
        STEP(F1, ref c, d, a, b, @in[2] + 0x242070db, 17);
        STEP(F1, ref b, c, d, a, @in[3] + 0xc1bdceee, 22);
        STEP(F1, ref a, b, c, d, @in[4] + 0xf57c0faf, 7);
        STEP(F1, ref d, a, b, c, @in[5] + 0x4787c62a, 12);
        STEP(F1, ref c, d, a, b, @in[6] + 0xa8304613, 17);
        STEP(F1, ref b, c, d, a, @in[7] + 0xfd469501, 22);
        STEP(F1, ref a, b, c, d, @in[8] + 0x698098d8, 7);
        STEP(F1, ref d, a, b, c, @in[9] + 0x8b44f7af, 12);
        STEP(F1, ref c, d, a, b, @in[10] + 0xffff5bb1, 17);
        STEP(F1, ref b, c, d, a, @in[11] + 0x895cd7be, 22);
        STEP(F1, ref a, b, c, d, @in[12] + 0x6b901122, 7);
        STEP(F1, ref d, a, b, c, @in[13] + 0xfd987193, 12);
        STEP(F1, ref c, d, a, b, @in[14] + 0xa679438e, 17);
        STEP(F1, ref b, c, d, a, @in[15] + 0x49b40821, 22);

        STEP(F2, ref a, b, c, d, @in[1] + 0xf61e2562, 5);
        STEP(F2, ref d, a, b, c, @in[6] + 0xc040b340, 9);
        STEP(F2, ref c, d, a, b, @in[11] + 0x265e5a51, 14);
        STEP(F2, ref b, c, d, a, @in[0] + 0xe9b6c7aa, 20);
        STEP(F2, ref a, b, c, d, @in[5] + 0xd62f105d, 5);
        STEP(F2, ref d, a, b, c, @in[10] + 0x02441453, 9);
        STEP(F2, ref c, d, a, b, @in[15] + 0xd8a1e681, 14);
        STEP(F2, ref b, c, d, a, @in[4] + 0xe7d3fbc8, 20);
        STEP(F2, ref a, b, c, d, @in[9] + 0x21e1cde6, 5);
        STEP(F2, ref d, a, b, c, @in[14] + 0xc33707d6, 9);
        STEP(F2, ref c, d, a, b, @in[3] + 0xf4d50d87, 14);
        STEP(F2, ref b, c, d, a, @in[8] + 0x455a14ed, 20);
        STEP(F2, ref a, b, c, d, @in[13] + 0xa9e3e905, 5);
        STEP(F2, ref d, a, b, c, @in[2] + 0xfcefa3f8, 9);
        STEP(F2, ref c, d, a, b, @in[7] + 0x676f02d9, 14);
        STEP(F2, ref b, c, d, a, @in[12] + 0x8d2a4c8a, 20);

        STEP(F3, ref a, b, c, d, @in[5] + 0xfffa3942, 4);
        STEP(F3, ref d, a, b, c, @in[8] + 0x8771f681, 11);
        STEP(F3, ref c, d, a, b, @in[11] + 0x6d9d6122, 16);
        STEP(F3, ref b, c, d, a, @in[14] + 0xfde5380c, 23);
        STEP(F3, ref a, b, c, d, @in[1] + 0xa4beea44, 4);
        STEP(F3, ref d, a, b, c, @in[4] + 0x4bdecfa9, 11);
        STEP(F3, ref c, d, a, b, @in[7] + 0xf6bb4b60, 16);
        STEP(F3, ref b, c, d, a, @in[10] + 0xbebfbc70, 23);
        STEP(F3, ref a, b, c, d, @in[13] + 0x289b7ec6, 4);
        STEP(F3, ref d, a, b, c, @in[0] + 0xeaa127fa, 11);
        STEP(F3, ref c, d, a, b, @in[3] + 0xd4ef3085, 16);
        STEP(F3, ref b, c, d, a, @in[6] + 0x04881d05, 23);
        STEP(F3, ref a, b, c, d, @in[9] + 0xd9d4d039, 4);
        STEP(F3, ref d, a, b, c, @in[12] + 0xe6db99e5, 11);
        STEP(F3, ref c, d, a, b, @in[15] + 0x1fa27cf8, 16);
        STEP(F3, ref b, c, d, a, @in[2] + 0xc4ac5665, 23);

        STEP(F4, ref a, b, c, d, @in[0] + 0xf4292244, 6);
        STEP(F4, ref d, a, b, c, @in[7] + 0x432aff97, 10);
        STEP(F4, ref c, d, a, b, @in[14] + 0xab9423a7, 15);
        STEP(F4, ref b, c, d, a, @in[5] + 0xfc93a039, 21);
        STEP(F4, ref a, b, c, d, @in[12] + 0x655b59c3, 6);
        STEP(F4, ref d, a, b, c, @in[3] + 0x8f0ccc92, 10);
        STEP(F4, ref c, d, a, b, @in[10] + 0xffeff47d, 15);
        STEP(F4, ref b, c, d, a, @in[1] + 0x85845dd1, 21);
        STEP(F4, ref a, b, c, d, @in[8] + 0x6fa87e4f, 6);
        STEP(F4, ref d, a, b, c, @in[15] + 0xfe2ce6e0, 10);
        STEP(F4, ref c, d, a, b, @in[6] + 0xa3014314, 15);
        STEP(F4, ref b, c, d, a, @in[13] + 0x4e0811a1, 21);
        STEP(F4, ref a, b, c, d, @in[4] + 0xf7537e82, 6);
        STEP(F4, ref d, a, b, c, @in[11] + 0xbd3af235, 10);
        STEP(F4, ref c, d, a, b, @in[2] + 0x2ad7d2bb, 15);
        STEP(F4, ref b, c, d, a, @in[9] + 0xeb86d391, 21);

        buf[0] += a;
        buf[1] += b;
        buf[2] += c;
        buf[3] += d;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void STEP(Func<uint, uint, uint, uint> f, ref uint w, uint x, uint y, uint z, uint data, byte s)
        {
            w += f(x, y, z) + data;
            w = (w << s) | (w >> (32 - s));
            w += x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint F1(uint x, uint y, uint z)
        {
            return z ^ (x & (y ^ z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint F2(uint x, uint y, uint z)
        {
            return F1(z, x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint F3(uint x, uint y, uint z)
        {
            return x ^ y ^ z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint F4(uint x, uint y, uint z)
        {
            return y ^ (x | ~z);
        }
    }

    public ProfileID Finish()   // cmsMD5finish
    {
        var count = (Bits[0] >> 3) & 0x3f;

        var p = In[(int)count..];
        p[0] = 0x80;
        p = p[1..];

        count = 64 - 1 - count;

        if (count < 8)
        {
            p[..(int)count].Clear();

            Transform(Buf, MemoryMarshal.Cast<byte, uint>(In));

            In[..56].Clear();
        }
        else
        {
            p[..((int)count - 8)].Clear();
        }

        var _in = MemoryMarshal.Cast<byte, uint>(In);
        _in[14] = Bits[0];
        _in[15] = Bits[1];

        Transform(Buf, _in);

        return ProfileID.Set(MemoryMarshal.Cast<uint, byte>(Buf));
    }

    public readonly void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
