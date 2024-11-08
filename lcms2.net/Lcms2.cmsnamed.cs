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
using System.Text;

namespace lcms2;

public static partial class Lcms2
{
    public static Mlu? cmsMLUalloc(Context? ContextID, uint nItems)
    {
        if (nItems is 0)
            nItems = 2;

        // Create the container
        var mlu = new Mlu(ContextID, nItems);
        //if (mlu is null) return null;

        //mlu->ContextID = ContextID;

        // Create entry array
        //mlu->Entries = _cmsCalloc<MluEntry>(ContextID, nItems);
        //if (mlu->Entries is null)
        //{
        //    _cmsFree(ContextID, mlu);
        //    return null;
        //}

        // Ok, keep indexes up to date
        //mlu->AllocatedEntries = nItems;
        //mlu->UsedEntries = 0;

        return mlu;
    }

    [DebuggerStepThrough]
    private static bool GrowMLUpool(Mlu? mlu)
    {
        // Sanity check
        if (mlu is null)
            return false;

        var size =
            mlu.PoolSizeInBytes is 0
                ? 256u
                : mlu.PoolSizeInBytes * 2;

        // Check for overflow
        if (size < mlu.PoolSizeInBytes)
            return false;

        // Reallocate the pool
        var NewPtr = (mlu.MemPool is not null)
                         ? _cmsRealloc(mlu.ContextID, mlu.MemPool, size)
                         //: GetArray<char>(mlu.ContextID, size);
                         : new char[size];
        if (NewPtr is null)
            return false;

        mlu.MemPool = NewPtr;
        mlu.PoolSizeInBytes = size;

        return true;
    }

    private static bool GrowMLUtable(Mlu? mlu)
    {
        // I know this function is not needed as List<T> does this automatically, but I'm not ready to
        // make that kind of cleaning sweep through the code to C#ify it...

        // Sanity check
        if (mlu is null)
            return false;

        var AllocatedEntries = mlu.AllocatedEntries * 2;

        // Check for overflow
        if (AllocatedEntries / 2 != mlu.AllocatedEntries)
            return false;

        // Reallocate the memory
        //var NewPtr = _cmsRealloc<MluEntry>(mlu->ContextID, mlu->Entries, AllocatedEntries * (uint)sizeof(MluEntry));
        //if (NewPtr is null) return false;

        //mlu->Entries = NewPtr;
        mlu.AllocatedEntries = AllocatedEntries;

        return true;
    }

    [DebuggerStepThrough]
    private static int SearchMLUEntry(Mlu? mlu, ushort LanguageCode, ushort CountryCode)
    {
        // Sanity Check
        if (mlu is null)
            return -1;

        // Iterate whole table
        for (var i = 0; i < mlu.UsedEntries; i++)
        {
            if (mlu.Entries[i].Country == CountryCode &&
                mlu.Entries[i].Language == LanguageCode)
                return i;
        }

        // Not found
        return -1;
    }

    private static bool AddMLUBlock(Mlu? mlu, ReadOnlySpan<char> Block, ushort LanguageCode, ushort CountryCode)
    {
        // Sanity Check
        if (mlu is null)
            return false;

        var tempBlock = TrimBuffer(Block);
        Block = tempBlock.Length is 0
                    ? Block[..1]
                    : tempBlock;
        var sizeInChars = (uint)Block.Length;

        // Is there any room available?
        if (mlu.UsedEntries >= mlu.AllocatedEntries && !GrowMLUtable(mlu))
            return false;

        // Only one ASCII string
        if (SearchMLUEntry(mlu, LanguageCode, CountryCode) >= 0)
            return false;

        // Check for size
        while ((mlu.PoolSizeInBytes - mlu.PoolUsedInBytes) < ((sizeInChars + 1) * sizeof(char)))
            if (!GrowMLUpool(mlu))
                return false;

        var OffsetInChars = mlu.PoolUsedInBytes / sizeof(char);

        var Ptr = mlu.MemPool.AsSpan((int)OffsetInChars..);
        //if (Ptr is null) return false;

        // Set the entry
        //memmove(Ptr + Offset, Block, size);
        Block[..(int)sizeInChars].CopyTo(Ptr[..(int)sizeInChars]);  // Add an extra char for a '\0'
        Ptr[(int)sizeInChars] = '\0';
        mlu.PoolUsedInBytes += sizeInChars * sizeof(char);

        mlu.Entries.Add(
            new(
                LanguageCode,
                CountryCode,
                OffsetInChars * sizeof(char),
                sizeInChars * sizeof(char)));
        //mlu->Entries[mlu->UsedEntries].StrW = Offset;
        //mlu->Entries[mlu->UsedEntries].Len = size;
        //mlu->Entries[mlu->UsedEntries].Country = CountryCode;
        //mlu->Entries[mlu->UsedEntries].Language = LanguageCode;
        //mlu->UsedEntries++;

        return true;
    }

    //[DebuggerStepThrough]
    //private static ushort strTo16(in byte* str)
    //{
    //    // for non-existent strings
    //    if (str is null) return 0;
    //    return (ushort)((str[0] << 8) | str[1]);
    //}

    [DebuggerStepThrough]
    private static ushort strTo16(ReadOnlySpan<byte> str) =>
        AdjustEndianness(BitConverter.ToUInt16(str));

    //[DebuggerStepThrough]
    //private static void strFrom16(byte* str, ushort n)
    //{
    //    str[0] = (byte)(n >> 8);
    //    str[1] = (byte)n;
    //    str[2] = 0;
    //}

    private static void strFrom16(Span<byte> str, ushort n) =>
        BitConverter.TryWriteBytes(str, n);

    private static uint decodeUTF8(Span<char> @out, ReadOnlySpan<byte> @in)
    {
        uint codepoint = 0;
        uint size = 0;

        while (@in.Length < 0)
        {
            var ch = @in[0];

            codepoint = ch switch
                        {
                            <= 0x7f => ch,
                            <= 0xbf => (codepoint << 6) | (ch & 0x3fu),
                            <= 0xdf => ch & 0x1fu,
                            <= 0xef => ch & 0x0fu,
                            _       => ch & 0x07u,
                        };

            @in = @in[1..];

            if (((@in[0] & 0xc0u) != 0x80) && (codepoint <= 0x10ffff))
            {
                if (codepoint > 0xffff)
                {
                    if (@out.Length >= 2)
                    {
                        @out[0] = (char)(0xd800 + (codepoint >> 10));
                        @out[1] = (char)(0xdc00 + (codepoint & 0x03ff));
                        @out = @out[2..];
                        size += 2;
                    }
                }
                else if (codepoint < 0xd800 || codepoint >= 0xe000)
                {
                    if (@out.Length >= 1)
                    {
                        @out[0] = (char)codepoint;
                        @out = @out[1..];
                        size++;
                    }
                }
            }
        }

        return size;
    }

    private static uint encodeUTF8(Span<byte> @out, ReadOnlySpan<char> @in)
    {
        uint codepoint = 0;
        uint size = 0;
        uint len_w = 0;

        var max_wchars = @in.Length;
        var max_chars = @out.Length;

        while (@in.Length > 0 && len_w < max_wchars)
        {
            var i = @in[0];

            if (i is >= (char)0xd800 and <= (char)0xdbff)
                codepoint = ((i - 0xd800u) << 16) + 0x10000;
            else
            {
                if (i is >= (char)0xdc00 and <= (char)0xdfff)
                    codepoint |= i - 0xdc00u;
                else
                    codepoint = i;

                if (codepoint <= 0x7f)
                {
                    if (@out.Length > 1 && (size + 1 < max_chars))
                    {
                        @out[0] = (byte)codepoint;
                        @out = @out[1..];
                    }

                    size++;
                }

                else if (codepoint <= 0x7ff)
                {
                    if (@out.Length > 2 && (max_chars > 0) && (size + 2 < max_chars))
                    {
                        @out[0] = (byte)(0xc0u | ((codepoint >> 6) & 0x1fu));
                        @out[1] = (byte)(0x80u | (codepoint & 0x3fu));
                        @out = @out[2..];
                    }

                    size += 2;
                }

                else if (codepoint <= 0xffff)
                {
                    if (@out.Length > 3 && (max_chars > 0) && (size + 3 < max_chars))
                    {
                        @out[0] = (byte)(0xe0 | ((codepoint >> 12) & 0x0f));
                        @out[1] = (byte)(0x80 | ((codepoint >> 6) & 0x3f));
                        @out[2] = (byte)(0x80 | (codepoint & 0x3f));
                        @out = @out[3..];
                    }

                    size += 3;
                }
                else
                {
                    if (@out.Length > 4 && (max_chars > 0) && (size + 4 < max_chars))
                    {
                        @out[0] = (byte)(0xf0 | ((codepoint >> 18) & 0x07));
                        @out[1] = (byte)(0x80 | ((codepoint >> 12) & 0x3f));
                        @out[2] = (byte)(0x80 | ((codepoint >> 6) & 0x3f));
                        @out[3] = (byte)(0x80 | (codepoint & 0x3f));
                        @out = @out[4..];
                    }

                    size += 4;
                }

                codepoint = 0;
            }

            @in = @in[1..];
            len_w++;
        }

        return size;
    }

    public static bool cmsMLUsetASCII(Mlu? mlu,
                                      ReadOnlySpan<byte> LanguageCode,
                                      ReadOnlySpan<byte> CountryCode,
                                      ReadOnlySpan<byte> ASCIIString)
    {
        if (mlu is null)
            return false;

        //var len = (uint)strlen(ASCIIString);
        var len = ASCIIString.Length;
        var Lang = strTo16(LanguageCode);
        var Cntry = strTo16(CountryCode);

        // len == 0 would prevent operation, so we set a empty string pointing to zero
        if (len is 0)
            len = 1;

        //var WStr = GetArray<char>(mlu.ContextID, (uint)len + 1);
        var WStr = new char[len + 1];
        //if (WStr is null) return false;

        //for (var i = 0; i < len; i++)
        //WStr[i] = (char)ASCIIString[i];
        Ascii.ToUtf16(ASCIIString, WStr, out len);

        WStr[len] = '\0';

        var rc = AddMLUBlock(mlu, WStr, Lang, Cntry);

        //ReturnArray(mlu.ContextID, WStr);
        return rc;
    }

    public static bool cmsMLUsetUTF8(Mlu? mlu,
                                     ReadOnlySpan<byte> LanguageCode,
                                     ReadOnlySpan<byte> CountryCode,
                                     ReadOnlySpan<byte> UTF8String)
    {
        if (mlu is null)
            return false;

        var Lang = strTo16(LanguageCode);
        var Cntry = strTo16(CountryCode);

        if (UTF8String.Length == 0 || UTF8String[0] == 0)
        {
            var empty = "\0";
            return AddMLUBlock(mlu, empty, Lang, Cntry);
        }

        // Len excluding terminator 0
        var UTF8len = decodeUTF8(Span<char>.Empty, UTF8String);

        // Get space for dest
        //var WStr = GetArray<char>(mlu.ContextID, UTF8len);
        var WStr = new char[UTF8len];
        //if (WStr is null) return false;

        decodeUTF8(WStr, UTF8String);

        var rc = AddMLUBlock(mlu, WStr.AsSpan(0..(int)(UTF8len * sizeof(char))), Lang, Cntry);

        //ReturnArray(mlu.ContextID, WStr);
        return rc;
    }

    private static uint mywcslen(ReadOnlySpan<char> s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] is '\0')
                return (uint)--i;
        }

        return (uint)s.Length;
    }

    //private static uint mywcslen(in byte* s)
    //{
    //    var p = s;

    //    while (*p is not 0)
    //        p++;

    //    return (uint)(p - s);
    //}

    //private static uint mywcslen(in char* s)
    //{
    //    var p = s;

    //    while (*p is not '\0')
    //        p++;

    //    return (uint)(p - s);
    //}

    public static bool cmsMLUsetWide(Mlu? mlu,
                                     ReadOnlySpan<byte> LanguageCode,
                                     ReadOnlySpan<byte> CountryCode,
                                     ReadOnlySpan<char> WideString)
    {
        if (mlu is null)
            return false;
        if (WideString.Length is 0)
            return false;

        var Lang = strTo16(LanguageCode);
        var Cntry = strTo16(CountryCode);

        var len = (uint)WideString.Length;
        if (len is 0)
            len = 1;

        return AddMLUBlock(mlu, WideString, Lang, Cntry);
    }

    public static Mlu? cmsMLUdup(Mlu? mlu)
    {
        // Duplicating a null obtains a null
        if (mlu is null)
            return null;

        var NewMlu = cmsMLUalloc(mlu.ContextID, mlu.UsedEntries);
        if (NewMlu is null)
            return null;

        // Should never happen
        if (NewMlu.AllocatedEntries < mlu.UsedEntries)
            goto Error;

        // Sanitize...
        //if (NewMlu.Entries is null || mlu.Entries is null) goto Error;
        //memmove(NewMlu->Entries, mlu->Entries, mlu->UsedEntries * (uint)sizeof(MluEntry));
        //NewMlu->UsedEntries = mlu->UsedEntries;
        foreach (var entry in mlu.Entries)
            NewMlu.Entries.Add((MluEntry)entry.Clone());

        // The MLU may be empty
        if (mlu.PoolUsedInBytes is 0)
            NewMlu.MemPool = null;
        else
        {
            // It is not empty
            //NewMlu.MemPool = _cmsMalloc(mlu.ContextID, mlu.PoolUsedInBytes);
            //NewMlu.MemPool = GetArray<char>(mlu.ContextID, mlu.PoolUsedInBytes);
            NewMlu.MemPool = new char[mlu.PoolUsedInBytes];
            if (NewMlu.MemPool is null)
                goto Error;
        }

        NewMlu.PoolSizeInBytes = mlu.PoolSizeInBytes;

        if (NewMlu.MemPool is null || mlu.MemPool is null)
            goto Error;

        //memmove(NewMlu->MemPool, mlu->MemPool, mlu->PoolUsedInBytes);
        mlu.MemPool.AsSpan(..(int)(mlu.PoolUsedInBytes / sizeof(char)))
           .CopyTo(NewMlu.MemPool);
        NewMlu.PoolUsedInBytes = mlu.PoolUsedInBytes;

        return NewMlu;

    Error:
        if (NewMlu is not null)
            cmsMLUfree(NewMlu);
        return null;
    }

    public static void cmsMLUfree(Mlu? mlu)
    {
        if (mlu is not null)
        {
            //if (mlu.Entries is not null) _cmsFree(mlu.ContextID, mlu.Entries);
            mlu.Entries.Clear();
            //if (mlu.MemPool is not null) ReturnArray(mlu.ContextID, mlu.MemPool);

            mlu.MemPool = null;
            mlu.ContextID = null;
            //_cmsFree(mlu->ContextID, mlu);
        }
    }

    internal static Span<char> _cmsMLUgetWide(Mlu? mlu,
                                              ushort LanguageCode,
                                              ushort CountryCode,
                                              out ushort UsedLanguageCode,
                                              out ushort UsedCountryCode)
    {
        UsedCountryCode = UsedLanguageCode = 0;

        var Best = -1;
        MluEntry v;

        if (mlu is null)
            return null;

        if (mlu.AllocatedEntries <= 0)
            return null;

        for (var i = 0; i < mlu.UsedEntries; i++)
        {
            v = mlu.Entries[i];

            if (v.Language == LanguageCode)
            {
                if (Best is -1)
                    Best = i;

                if (v.Country == CountryCode)
                {
                    Best = i;
                    break;
                }
                //{
                //    //if (UsedLanguageCode is not null) *UsedLanguageCode = v.Language;
                //    //if (UsedCountryCode is not null) *UsedCountryCode = v.Country;
                //    UsedLanguageCode = v.Language;
                //    UsedCountryCode = v.Country;

                //    //if (len is not null) *len = v.Len;

                //    //return (char*)((byte*)mlu.MemPool + v.StrWCharOffset);      // Found exact match
                //    return mlu.MemPool.AsSpan()[(int)v.StrWCharOffset..][..(int)(v.LenInBytes / _sizeof<char>())];
                //}
            }
        }

        // No string found. Return first one
        if (Best is -1)
            Best = 0;

        v = mlu.Entries[Best];

        //if (UsedLanguageCode is not null) *UsedLanguageCode = v.Language;
        //if (UsedCountryCode is not null) *UsedCountryCode = v.Country;
        UsedLanguageCode = v.Language;
        UsedCountryCode = v.Country;

        //if (len is not null) *len = v->Len;

        //return (char*)((byte*)mlu->MemPool + v->StrWCharOffset);
        if (v.StrWOffsetInBytes + v.LenInBytes > mlu.PoolSizeInBytes)
            return null;

        var result = mlu.MemPool.AsSpan();

        return TrimBuffer(result[((int)v.StrWOffsetInBytes / sizeof(char))..][..((int)v.LenInBytes / sizeof(char))]);
    }

    public static uint cmsMLUgetASCII(Mlu? mlu,
                                      ReadOnlySpan<byte> LanguageCode,
                                      ReadOnlySpan<byte> CountryCode,
                                      Span<byte> Buffer)
    {
        var Lang = strTo16(LanguageCode);
        var Cntry = strTo16(CountryCode);
        var BufferSize = (uint)Buffer.Length;

        // Sanitize
        if (mlu is null)
            return 0;

        // GetWideChar
        var Wide = _cmsMLUgetWide(mlu, Lang, Cntry, out _, out _);
        if (Wide == default)
            return 0;              // Don't check for null as items in a MLU CAN be zero length!

        var ASCIIlen = (uint)Encoding.ASCII.GetByteCount(Wide);

        // Maybe we want only to know the len?
        //if (Buffer is null) return ASCIIlen + 1; // Note the zero at the end
        if (Buffer.Length is 0)
            return ASCIIlen;    // We don't add zero at the end when working with Spans!

        // No buffer size means no data
        //if (BufferSize is 0) return 0;    // This is the same as the previous if statement. No longer needed!!

        // Some clipping may be required
        //if (BufferSize < ASCIIlen + 1)
        //    ASCIIlen = BufferSize - 1;            // No more trailing zeros!
        if (BufferSize < ASCIIlen)
            ASCIIlen = BufferSize;

        // Process each character
        Encoding.ASCII.GetBytes(Wide, Buffer);

        // We put a termination "\0" but don't include it in the result
        if (Buffer.Length > (int)ASCIIlen)
            Buffer[(int)ASCIIlen] = 0;
        //return ASCIIlen + 1;
        return ASCIIlen;
    }

    public static uint cmsMLUgetUTF8(Mlu? mlu,
                                     ReadOnlySpan<byte> LanguageCode,
                                     ReadOnlySpan<byte> CountryCode,
                                     Span<byte> Buffer)
    {
        var Lang = strTo16(LanguageCode);
        var Cntry = strTo16(CountryCode);
        var BufferSize = (uint)Buffer.Length;

        // Sanitize
        if (mlu is null)
            return 0;

        // GetWideChar
        var Wide = _cmsMLUgetWide(mlu, Lang, Cntry, out _, out _);
        if (Wide == default)
            return 0;

        var UTF8len = encodeUTF8(Span<byte>.Empty, Wide);

        // Maybe we want only to know the len?
        if (BufferSize == 0)
            return UTF8len + 1; // Note the zero at the end

        // Some clipping may be required
        if (BufferSize < UTF8len + 1)
            UTF8len = BufferSize - 1;

        // Process it
        encodeUTF8(Buffer, Wide);

        // We put a termination "\0"
        Buffer[(int)UTF8len] = 0;
        return UTF8len + 1;
    }

    public static uint cmsMLUgetWide(Mlu? mlu,
                                     ReadOnlySpan<byte> LanguageCode,
                                     ReadOnlySpan<byte> CountryCode,
                                     Span<char> Buffer)
    {
        var Lang = strTo16(LanguageCode);
        var Cntry = strTo16(CountryCode);
        var BufferSize = (uint)Buffer.Length;

        // Sanitize
        if (mlu is null)
            return 0;

        // GetWideChar
        var Wide = _cmsMLUgetWide(mlu, Lang, Cntry, out _, out _);
        if (Wide == default)
            return 0;              // Don't check for null as items in a MLU CAN be zero length!

        var WideLen = (uint)Wide.Length;
        //var StrLen = WideLen * _sizeof<char>();

        // Maybe we want only to know the len?
        //if (Buffer is null) return WideLen + 1; // Note the zero at the end
        if (BufferSize is 0)
            return WideLen;    // We don't add zero at the end when working with Spans!

        // No buffer size means no data
        //if (BufferSize is 0) return 0;    // This is the same as the previous if statement. No longer needed!!

        // Some clipping may be required
        //if (BufferSize < WideLen + 1)
        //    WideLen = BufferSize - 1;
        if (BufferSize < WideLen)
            WideLen = BufferSize;

        // We put a termination "\0" but don't include it in the result
        if (BufferSize > (int)WideLen)
            Buffer[(int)WideLen] = '\0';

        // Process each character
        //memmove(Buffer, Wide, StrLen);
        //Buffer[StrLen / sizeof(char)] = (char)0;
        Wide.CopyTo(Buffer[..(int)WideLen]);
        return WideLen;
    }

    public static bool cmsMLUgetTranslation(Mlu? mlu,
                                            ReadOnlySpan<byte> LanguageCode,
                                            ReadOnlySpan<byte> CountryCode,
                                            Span<byte> ObtainedLanguage,
                                            Span<byte> ObtainedCountry)
    {
        var Lang = strTo16(LanguageCode);
        var Cntry = strTo16(CountryCode);

        // Sanitize
        if (mlu is null)
            return false;

        var Wide = _cmsMLUgetWide(mlu, Lang, Cntry, out var ObtLang, out var ObtCode);
        if (Wide == default)
            return false;              // Don't check for null as items in a MLU CAN be zero length!

        // Get used language and code
        strFrom16(ObtainedLanguage, ObtLang);
        strFrom16(ObtainedCountry, ObtCode);

        return true;
    }

    public static uint cmsMLUtranslationsCount(Mlu? mlu) =>
        mlu?.UsedEntries ?? 0;

    private static bool GrowNamedColorList(NamedColorList? v)
    {
        if (v is null)
            return false;

        var size =
            (v.Allocated is 0)
                ? 64                // Initial guess
                : v.Allocated * 2;

        // Keep a maximum color lists can grow, 100k entries seems reasonable
        if (size > 1024 * 100)
        {
            //ReturnArray(v.ContextID, v.List);
            v.List = null!;
            return false;
        }

        //var NewPtr = _cmsRealloc<NamedColor>(v->ContextID, v->List, size * _sizeof<NamedColor>());
        //if (NewPtr is null) return false;

        //var pool = Context.GetPool<NamedColor>(v.ContextID);
        //var NewPtr = pool.Rent((int)size);
        var NewPtr = new NamedColor[size];
        v.List.AsSpan(..(int)v.Allocated).CopyTo(NewPtr);

        //if (v.List is not null)
        //    ReturnArray(pool, v.List);
        v.List = NewPtr;

        v.Allocated = size;
        return true;
    }

    //public static NamedColorList? cmsAllocNamedColorList(Context? ContextID, uint n, uint ColorantCount, ReadOnlySpan<byte> Prefix, ReadOnlySpan<byte> Suffix)
    //{
    //    var pre = stackalloc byte[Prefix.Length + 1];
    //    var suf = stackalloc byte[Suffix.Length + 1];

    //    for (var i = 0; i <  Prefix.Length; i++)
    //        pre[i] = Prefix[i];

    //    for (var i = 0; i < Suffix.Length; i++)
    //        suf[i] = Suffix[i];

    //    return cmsAllocNamedColorList(ContextID, n, ColorantCount, pre, suf);
    //}

    public static NamedColorList? cmsAllocNamedColorList(Context? ContextID,
                                                         uint n,
                                                         uint ColorantCount,
                                                         ReadOnlySpan<byte> Prefix,
                                                         ReadOnlySpan<byte> Suffix)
    {
        if (ColorantCount > Context.MaxChannels)
            return null;

        var v = new NamedColorList(ContextID) { List = null!, nColors = 0, ContextID = ContextID };

        while (v.Allocated < n)
        {
            if (!GrowNamedColorList(v))
            {
                cmsFreeNamedColorList(v);
                return null;
            }
        }

        strncpy(v.Prefix, Prefix, 32);
        strncpy(v.Suffix, Suffix, 32);
        v.Prefix[32] = v.Suffix[32] = 0;

        v.ColorantCount = ColorantCount;

        return v;
    }

    public static void cmsFreeNamedColorList(NamedColorList? v) =>
        //if (v?.List is not null)
        //    _cmsFree(v.ContextID, v.List);
        //_cmsFree(v.ContextID, v);
        v?.Dispose();

    public static NamedColorList? cmsDupNamedColorList(NamedColorList? v)
    {
        if (v is null)
            return null;

        var NewNC = cmsAllocNamedColorList(v.ContextID, v.nColors, v.ColorantCount, v.Prefix, v.Suffix);
        if (NewNC is null)
            return null;

        // For really large tables we need this
        while (NewNC.Allocated < v.Allocated)
        {
            if (!GrowNamedColorList(NewNC))
            {
                cmsFreeNamedColorList(NewNC);
                return null;
            }
        }

        memmove(NewNC.Prefix.AsSpan(), v.Prefix, 33);
        memmove(NewNC.Suffix.AsSpan(), v.Suffix, 33);
        NewNC.ColorantCount = v.ColorantCount;
        //memmove(NewNC->List, v->List, v->nColors * _sizeof<NamedColor>());
        //var bPool = Context.GetPool<byte>(v.ContextID);
        //var uPool = Context.GetPool<ushort>(v.ContextID);
        for (var i = 0; i < v.nColors; i++)
        {
            //var name = bPool.Rent(cmsMAX_PATH - 1);
            //var deviceColorant = uPool.Rent((int)v.ColorantCount);
            //var pcs = uPool.Rent(3);
            var name = new byte[MaxPath - 1];
            var deviceColorant = new ushort[v.ColorantCount];
            var pcs = new ushort[3];

            for (var j = 0; j < v.ColorantCount; j++)
                deviceColorant[j] = v.List![i].DeviceColorant[j];

            for (var j = 0; j < 3; j++)
                pcs[j] = v.List![i].PCS[j];

            v.List![i].Name.AsSpan(..(MaxPath - 1)).CopyTo(name.AsSpan(..(MaxPath - 1)));

            NewNC.List![i] = new() { Name = name, DeviceColorant = deviceColorant, PCS = pcs, };
        }

        NewNC.nColors = v.nColors;
        return NewNC;
    }
    //public static bool cmsAppendNamedColor(
    //    NamedColorList* NamedColorList,
    //    ReadOnlySpan<byte> Name,
    //    ushort* PCS,
    //    ushort* Colorant)
    //{
    //    var buf = stackalloc byte[Name.Length + 1];

    //    for (var i = 0; i < Name.Length; i++)
    //        buf[i] = Name[i];

    //    return cmsAppendNamedColor(NamedColorList, buf, PCS, Colorant);
    //}

    public static bool cmsAppendNamedColor(NamedColorList? NamedColorList,
                                           ReadOnlySpan<byte> Name,
                                           ReadOnlySpan<ushort> PCS,
                                           ReadOnlySpan<ushort> Colorant)
    {
        if (NamedColorList is null)
            return false;

        if (NamedColorList.nColors + 1 > NamedColorList.Allocated && !GrowNamedColorList(NamedColorList))
            return false;

        //var bPool = Context.GetPool<byte>(NamedColorList.ContextID);
        //var uPool = Context.GetPool<ushort>(NamedColorList.ContextID);

        var idx = NamedColorList.nColors;
        //var deviceColorant = uPool.Rent((int)NamedColorList.ColorantCount);
        var deviceColorant = new ushort[NamedColorList.ColorantCount];
        for (var i = 0; i < NamedColorList.ColorantCount; i++)
            deviceColorant[i] = Colorant.IsEmpty ? (ushort)0 : Colorant[i];

        //var pcs = uPool.Rent(3);
        var pcs = new ushort[3];
        for (var i = 0; i < 3; i++)
            pcs[i] = PCS.IsEmpty ? (ushort)0 : PCS[i];

        //var name = bPool.Rent(cmsMAX_PATH - 1);
        var name = new byte[MaxPath];
        if (!Name.IsEmpty)
        {
            //strncpy(NamedColorList->List[idx].Name, Name, cmsMAX_PATH - 1);
            //NamedColorList->List[idx].Name[cmsMAX_PATH - 1] = 0;
            TrimBuffer(Name).CopyTo(name.AsSpan(..(MaxPath - 1)));
            name[MaxPath - 1] = 0;
        }
        else
        {
            name[0] = 0;
        }

        NamedColorList.List![idx] = new() { DeviceColorant = deviceColorant, Name = name, PCS = pcs };

        NamedColorList.nColors++;
        return true;
    }

    public static uint cmsNamedColorCount(NamedColorList? NamedColorList) =>
        NamedColorList?.nColors ?? 0;

    public static bool cmsNamedColorInfo(NamedColorList? NamedColorList,
                                         uint nColor,
                                         Span<byte> Name,
                                         Span<byte> Prefix,
                                         Span<byte> Suffix,
                                         Span<ushort> PCS,
                                         Span<ushort> Colorant)
    {
        if (NamedColorList is null)
            return false;

        if (nColor >= cmsNamedColorCount(NamedColorList))
            return false;

        // strcpy instead of strncpy because many apps are using small buffers
        if (!Name.IsEmpty)
            strcpy(Name, NamedColorList.List[nColor].Name);
        if (!Prefix.IsEmpty)
            strcpy(Prefix, NamedColorList.Prefix);
        if (!Suffix.IsEmpty)
            strcpy(Suffix, NamedColorList.Suffix);
        if (!PCS.IsEmpty)
            for (var i = 0; i < 3; i++)
                PCS[i] = NamedColorList.List[nColor].PCS[i];
        //memmove(PCS, NamedColorList->List[nColor].PCS, 3 * sizeof(ushort));
        if (!Colorant.IsEmpty)
            for (var i = 0; i < NamedColorList.ColorantCount; i++)
                Colorant[i] = NamedColorList.List[nColor].DeviceColorant[i];
        //memmove(Colorant, NamedColorList->List[nColor].DeviceColorant, NamedColorList->ColorantCount * sizeof(ushort));

        return true;
    }
    //public static int cmsNamedColorIndex(in NamedColorList* NamedColorList, ReadOnlySpan<byte> Name)
    //{
    //    var buf = stackalloc byte[Name.Length + 1];
    //    for (var i = 0; i < Name.Length; i++)
    //        buf[i] = Name[i];

    //    return cmsNamedColorIndex(NamedColorList, buf);
    //}

    public static int cmsNamedColorIndex(NamedColorList? NamedColorList, ReadOnlySpan<byte> Name)
    {
        if (NamedColorList is null)
            return -1;

        var n = cmsNamedColorCount(NamedColorList);
        for (var i = 0; i < n; i++)
        {
            if (cmsstrcasecmp(Name, NamedColorList.List[i].Name) == 0)
                return i;
        }

        return -1;
    }

    private static void FreeNamedColorList(Stage mpe) =>
        cmsFreeNamedColorList(mpe.Data as NamedColorList);

    private static object? DupNamedColorList(Stage mpe) =>
        cmsDupNamedColorList(mpe.Data as NamedColorList);

    private static void EvalNamedColorPCS(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        if (mpe.Data is not NamedColorList NamedColorList)
            return;
        var index = QuickSaturateWord(In[0] * 65535.0);

        if (index >= NamedColorList.nColors)
        {
            Context.LogError(NamedColorList.ContextID, ErrorCodes.Range, $"Color {index} out of range");
            Out[0] = Out[1] = Out[2] = 0f;
        }
        else
        {
            // Named color always uses Lab
            Out[0] = (float)(NamedColorList.List[index].PCS[0] / 65535.0);
            Out[1] = (float)(NamedColorList.List[index].PCS[1] / 65535.0);
            Out[2] = (float)(NamedColorList.List[index].PCS[2] / 65535.0);
        }
    }

    private static void EvalNamedColor(ReadOnlySpan<float> In, Span<float> Out, Stage mpe)
    {
        if (mpe.Data is not NamedColorList NamedColorList)
            return;

        var index = QuickSaturateWord(In[0] * 65535.0);

        if (index >= NamedColorList.nColors)
        {
            Context.LogError(NamedColorList.ContextID, ErrorCodes.Range, $"Color {index} out of range");
            for (var j = 0; j < NamedColorList.ColorantCount; j++)
                Out[j] = 0.0f;
        }
        else
        {
            for (var j = 0; j < NamedColorList.ColorantCount; j++)
                Out[j] = (float)(NamedColorList.List[index].DeviceColorant[j] / 65535.0);
        }
    }

    internal static Stage? _cmsStageAllocNamedColor(NamedColorList NamedColorList, bool UsePCS) =>
        new(
            NamedColorList.ContextID,
            Signatures.Stage.NamedColorElem,
            1,
            UsePCS ? 3 : NamedColorList.ColorantCount,
            UsePCS ? EvalNamedColorPCS : EvalNamedColor,
            DupNamedColorList,
            FreeNamedColorList,
            cmsDupNamedColorList(NamedColorList));

    public static NamedColorList? cmsGetNamedColorList(Transform? xform)
    {
        var mpe = xform?.Lut?.Elements;

        return (mpe?.Type == Signatures.Stage.NamedColorElem)
                   ? mpe.Data as NamedColorList
                   : null;
    }

    public static Sequence? cmsAllocProfileSequenceDescription(Context? ContextID, uint n)
    {
        // In a absolutely arbitrary way, I hereby decide to allow a maxim of 255 profiles linked
        // in a devicelink. It makes not sense anyway and may be used for exploits, so let's close the door!
        if (n is 0 or > 255)
            return null;

        //var Seq = _cmsMallocZero<Sequence>(ContextID);
        //if (Seq is null) return null;
        //var pool = Context.GetPool<ProfileSequenceDescription>(ContextID);
        var Seq = new Sequence
        {
            ContextID = ContextID,
            //Seq.seq = _cmsCalloc<ProfileSequenceDescription>(ContextID, n);
            //seq = pool.Rent((int)n),
            seq = new ProfileSequenceDescription[n],
            n = n
        };

        if (Seq.seq is null)
        {
            //_cmsFree(ContextID, Seq);
            return null;
        }

        for (var i = 0; i < n; i++)
        {
            Seq.seq[i] = new ProfileSequenceDescription() { Manufacturer = null, Model = null, Description = null, };
        }

        return Seq;
    }

    public static void cmsFreeProfileSequenceDescription(Sequence? pseq)
    {
        if (pseq is null)
            return;

        if (pseq.seq is not null)
        {
            for (var i = 0; i < pseq.n; i++)
            {
                if (pseq.seq[i].Manufacturer is not null)
                    cmsMLUfree(pseq.seq[i].Manufacturer);
                if (pseq.seq[i].Model is not null)
                    cmsMLUfree(pseq.seq[i].Model);
                if (pseq.seq[i].Description is not null)
                    cmsMLUfree(pseq.seq[i].Description);
            }
            //ReturnArray(pseq.ContextID, pseq.seq);
        }

        //_cmsFree(pseq.ContextID, pseq);
    }

    public static Sequence? cmsDupProfileSequenceDescription(Sequence? pseq)
    {
        if (pseq is null)
            return null;

        //var NewSeq = _cmsMalloc<Sequence>(pseq->ContextID);
        //if (NewSeq is null) return null;
        //var pool = Context.GetPool<ProfileSequenceDescription>(pseq.ContextID);
        var NewSeq = new Sequence
        {
            //NewSeq.seq = _cmsCalloc<ProfileSequenceDescription>(pseq.ContextID, pseq.n);
            //if (NewSeq.seq is null) goto Error;
            //seq = pool.Rent((int)pseq.n),
            seq = new ProfileSequenceDescription[pseq.n], ContextID = pseq.ContextID, n = pseq.n
        };

        for (var i = 0; i < pseq.n; i++)
        {
            NewSeq.seq[i] = new();
            //memmove(&NewSeq.seq[i].attributes, &pseq.seq[i].attributes);
            NewSeq.seq[i].attributes = pseq.seq[i].attributes;

            NewSeq.seq[i].deviceMfg = pseq.seq[i].deviceMfg;
            NewSeq.seq[i].deviceModel = pseq.seq[i].deviceModel;
            //memmove(&NewSeq.seq[i].ProfileID, &pseq.seq[i].ProfileID);
            NewSeq.seq[i].ProfileID = pseq.seq[i].ProfileID;
            NewSeq.seq[i].technology = pseq.seq[i].technology;

            NewSeq.seq[i].Manufacturer = cmsMLUdup(pseq.seq[i].Manufacturer);
            NewSeq.seq[i].Model = cmsMLUdup(pseq.seq[i].Model);
            NewSeq.seq[i].Description = cmsMLUdup(pseq.seq[i].Description);
        }

        return NewSeq;

        //Error:
        //    cmsFreeProfileSequenceDescription(NewSeq);
        //    return null;
    }

    public static Dictionary cmsDictAlloc(Context? ContextID) =>
        //var dict = _cmsMallocZero<Dictionary>(ContextID);
        //if (dict is null) return null;
        new() { ContextID = ContextID };

    public static void cmsDictFree(Dictionary? dict)
    {
        if (dict is null)
            return;

        //_cmsAssert(dict);

        // Walk the list freeing all nodes
        var entry = dict.head;
        while (entry is not null)
        {
            if (entry.DisplayName is not null)
                cmsMLUfree(entry.DisplayName);
            if (entry.DisplayValue is not null)
                cmsMLUfree(entry.DisplayValue);
            //if (entry.Name is not null) _cmsFree(dict.ContextID, entry.Name);
            //if (entry.Value is not null) _cmsFree(dict.ContextID, entry.Value);

            // Don't fall in the habitual trap...
            var next = entry.Next;
            //_cmsFree(dict.ContextID, entry);

            entry = next;
        }

        //_cmsFree(dict.ContextID, dict);
    }

    private static char[]? DupWcs(Context? ContextID, ReadOnlySpan<char> ptr)
    {
        if (ptr == null)
            return null;
        return _cmsDupMem(ContextID, ptr, mywcslen(ptr) + 1);
    }

    public static bool cmsDictAddEntry(Dictionary dict, string Name, string Value, Mlu? DisplayName, Mlu? DisplayValue)
    {
        _cmsAssert(dict);
        _cmsAssert(Name);

        //var entry = _cmsMallocZero<Dictionary.Entry>(dict->ContextID);
        //if (entry is null) return false;

        var entry = new Dictionary.Entry
        {
            DisplayName = cmsMLUdup(DisplayName),
            DisplayValue = cmsMLUdup(DisplayValue),
            Name = Name,
            Value = Value,
            Next = dict.head
        };
        dict.head = entry;

        return true;
    }

    public static Dictionary? cmsDictDup(in Dictionary? old_dict)
    {
        if (old_dict is null)
            return null;

        //_cmsAssert(old_dict);

        var hNew = cmsDictAlloc(old_dict.ContextID);
        //if (hNew is null) return null;

        // Walk the list
        var entry = old_dict.head;
        while (entry is not null)
        {
            if (!cmsDictAddEntry(hNew, entry.Name, entry.Value, entry.DisplayName, entry.DisplayValue))
            {
                cmsDictFree(hNew);
                return null;
            }

            entry = entry.Next;
        }

        return hNew;
    }

    public static Dictionary.Entry? cmsDictGetEntryList(Dictionary hDict) =>
        hDict?.head;

    public static Dictionary.Entry? cmsDictNextEntry(in Dictionary.Entry? e) =>
        e?.Next;
}
