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
using System.Text;

using lcms2.cgats;

using static lcms2.cgats.CGATS;

namespace lcms2;

public static partial class Lcms2
{
    //private static @string StringAlloc(IT8 it8, int max)
    //{
    //    var s = AllocChunk<@string>(it8);

    //    s->it8 = it8;
    //    s->max = max;
    //    s->len = 0;
    //    s->begin = Context.GetPool<byte>(it8.ContextID).Rent(max); //(byte*)AllocChunk(it8, (uint)s->max);

    //    return s;
    //}

    //private static void StringClear(@string* s) =>
    //    s->len = 0;

    //private static void StringAppend(@string* s, byte c)
    //{
    //    if (s->len + 1 >= s->max)
    //    {
    //        byte[] new_ptr;

    //        s->max *= 10;
    //        if (s->max > s->begin.Length)
    //        {
    //            var pool = Context.GetPool<byte>(s->it8->ContextID);
    //            new_ptr = pool.Rent(s->max); //new_ptr = (byte*)AllocChunk(s->it8, (uint)s->max);
    //            s->begin.CopyTo(new_ptr.AsSpan()); //memcpy(new_ptr, s->begin, s->len);
    //            pool.Return(s->begin);
    //            s->begin = new_ptr;
    //        }
    //    }

    //    s->begin[s->len++] = c;
    //}

    //private static Span<byte> StringPtr(@string* s) =>
    //    s->begin.AsSpan(..s->len);

    //private static void StringCat(@string* s, ReadOnlySpan<byte> c)
    //{
    //    var i = 0;
    //    while (c[i] is not 0)
    //    {
    //        StringAppend(s, c[i++]);
    //    }
    //}

    private static bool isseparator(int c) =>
        c is ' ' or '\t';

    private static bool ismiddle(int c) =>
        !isseparator(c) && c is not '#' and not '\"' and not '\'' and > 32 and < 127;

    private static bool isidchar(int c) =>
        char.IsAsciiLetterOrDigit((char)c) || ismiddle(c);

    private static bool isfirstidchar(int c) =>
        !char.IsDigit((char)c) && ismiddle(c);

    private static bool isabsolutepath(ReadOnlySpan<byte> path)
    {
        Span<byte> ThreeChars = stackalloc byte[4];

        if (path.IsEmpty || path[0] is 0)
            return false;

        strncpy(ThreeChars, path, 3);
        ThreeChars[3] = 0;

        if (ThreeChars[0] == DIR_CHAR)
            return true;

        if (char.IsAsciiLetter((char)ThreeChars[0]) && ThreeChars[1] is (byte)':')
            return true;

        return false;
    }

    private static bool BuildAbsolutePath(ReadOnlySpan<byte> relPath,
                                          ReadOnlySpan<byte> basePath,
                                          Span<byte> buffer,
                                          uint MaxLen)
    {
        // Already absolute?
        if (isabsolutepath(relPath))
        {
            strncpy(buffer, relPath, MaxLen);
            buffer[(int)MaxLen - 1] = 0;
            return true;
        }

        // No, search for last
        strncpy(buffer, basePath, MaxLen);
        buffer[(int)MaxLen - 1] = 0;

        var tail = strrchr(buffer, DIR_CHAR);
        if (tail.IsEmpty)
            return false;

        var len = (uint)(buffer.Length - tail.Length);
        if (len >= MaxLen)
            return false;

        // No need to assure zero terminator over here
        strncpy(tail[1..], relPath, MaxLen - len);

        return true;
    }

    private static ReadOnlySpan<byte> NoMeta(ReadOnlySpan<byte> str)
    {
        if (!strchr(str, '%').IsEmpty)
            return "**** CORRUPTED FORMAT STRING ***"u8;

        return str;
    }

    private static bool SynError(IT8 it8, string Txt, params object[] args)
    {
        Span<byte> str = stackalloc byte[Encoding.ASCII.GetByteCount(Txt)];
        Encoding.ASCII.GetBytes(Txt, str);

        return SynError(it8, str, args);
    }

    private static bool SynError(IT8 it8, ReadOnlySpan<byte> Txt, params object[] args)
    {
        Span<byte> Buffer = stackalloc byte[256];
        Span<byte> ErrMsg = stackalloc byte[1024];

        snprintf(Buffer, 255, Txt, args);
        Buffer[255] = 0;

        snprintf(
            ErrMsg,
            1023,
            "{0}: Line {1}, {2}"u8,
            SpanToString(it8.FileStack[it8.IncludeSP].FileName),
            it8.lineno,
            SpanToString(Buffer));
        ErrMsg[1023] = 0;
        it8.sy = SYMBOL.SSYNERROR;
        Context.LogError(it8.ContextID, cmsERROR_CORRUPTION_DETECTED, SpanToString(ErrMsg));
        return false;
    }

    private static bool Check(IT8 it8, SYMBOL sy, string Err)
    {
        Span<byte> buf = stackalloc byte[Encoding.ASCII.GetByteCount(Err)];
        Encoding.ASCII.GetBytes(Err, buf);
        if (it8.sy != sy)
            return SynError(it8, NoMeta(buf));
        return true;
    }

    private static void NextCh(IT8 it8)
    {
        var stream = it8.FileStack[it8.IncludeSP].Stream;
        if (stream is not null)
        {
            it8.ch = stream.ReadByte();

            if (it8.ch is -1)
            {
                if (it8.IncludeSP > 0)
                {
                    stream.Close();
                    it8.ch = ' ';      // Whitespace to be ignored
                }
                else
                {
                    it8.ch = '\0';
                }
            }
        }
        else
        {
            it8.ch = it8.Source.Span[0];
            if (it8.ch is not 0)
                it8.Source = it8.Source.Length is not 1 ? it8.Source[1..] : default;
        }
    }

    private static SYMBOL BinSrchKey(ReadOnlySpan<byte> id)
    {
        int l = 1;
        int r = NUMKEYS;

        while (r >= l)
        {
            var x = (l + r) / 2;
            var res = cmsstrcasecmp(id, TabKeys[x - 1].id);
            if (res is 0)
                return TabKeys[x - 1].sy;
            if (res < 0)
                r = x - 1;
            else
                l = x + 1;
        }

        return SYMBOL.SUNDEFINED;
    }

    private static double xpow10(int n) =>
        Math.Pow(10, n);

    private static void ReadReal(IT8 it8, int inum)
    {
        it8.dnum = inum;

        while (Char.IsDigit((char)it8.ch))
        {
            it8.dnum = (it8.dnum * 10.0) + (it8.ch - '0');
            NextCh(it8);
        }

        if (it8.ch is '.')     // Decimal point
        {
            var frac = 0.0; // fraction
            var prec = 0;   // precision

            NextCh(it8);

            while (Char.IsDigit((char)it8.ch))
            {
                frac = (frac * 10.0) + (it8.ch - '0');
                prec++;
                NextCh(it8);
            }

            it8.dnum += frac / xpow10(prec);
        }

        // Exponent, example 34.00E+20
        if (Char.ToUpper((char)it8.ch) is 'E')
        {
            NextCh(it8);
            var sgn = 1;

            if (it8.ch is '-')
            {
                sgn = -1;
                NextCh(it8);
            }
            else if (it8.ch is '+')
            {
                sgn = +1;
                NextCh(it8);
            }

            var e = 0;
            while (Char.IsDigit((char)it8.ch))
            {
                var digit = it8.ch - '0';

                if (e * 10.0 + digit < +2147483647.0)
                    e = e * 10 + digit;

                NextCh(it8);
            }

            e *= sgn;
            it8.dnum *= xpow10(e);
        }
    }

    private static double ParseFloatNumber(ReadOnlySpan<byte> Buffer)
    {
        var dnum = 0.0;
        var sign = 1;
        var i = 0;

        // keep safe
        if (Buffer.IsEmpty)
            return 0.0;

        if ((char)Buffer[i] is '-' or '+')
        {
            sign = ((char)Buffer[i++] is '-') ? -1 : 1;
        }

        while (i < Buffer.Length && Buffer[i] is not 0 && Char.IsDigit((char)Buffer[i]))
        {
            dnum = dnum * 10.0 + (Buffer[i] - '0');
            if (Buffer[i] is not 0)
                i++;
        }

        if (i < Buffer.Length && (char)Buffer[i] is '.')
        {
            var frac = 0.0;
            var prec = 0;

            if (Buffer[i] is not 0)
                i++;

            while (i < Buffer.Length && Char.IsDigit((char)Buffer[i]))
            {
                frac = (frac * 10.0) + (Buffer[i] - '0');
                prec++;
                if (Buffer[i] is not 0)
                    i++;
            }

            dnum += frac / xpow10(prec);
        }

        // Exponent, example 34.00E+20
        if (i < Buffer.Length && Char.ToUpper((char)Buffer[i]) is 'E')
        {
            if (Buffer[i] is not 0)
                i++;
            var sgn = 1;

            if (i < Buffer.Length && (char)Buffer[i] is '-')
            {
                sgn = -1;
                if (Buffer[i] is not 0)
                    i++;
            }
            else if (i < Buffer.Length && (char)Buffer[i] is '+')
            {
                sgn = +1;
                if (Buffer[i] is not 0)
                    i++;
            }

            var e = 0;
            while (i < Buffer.Length && Char.IsDigit((char)Buffer[i]))
            {
                var digit = Buffer[i] - '0';

                if (e * 10.0 + digit < +2147483647.0)
                    e = e * 10 + digit;

                if (Buffer[i] is not 0)
                    i++;
            }

            e *= sgn;
            dnum *= xpow10(e);
        }

        return sign * dnum;
    }

    private static void InStringSymbol(IT8 it8)
    {
        while (isseparator(it8.ch))
            NextCh(it8);

        if (it8.ch is '\'' or '\"')
        {
            var sng = it8.ch;
            //StringClear(it8->str);
            it8.str.Clear();

            NextCh(it8);

            while (it8.ch != sng)
            {
                if (it8.ch is '\n' or '\r' or '\0')
                    break;
                else
                {
                    //StringAppend(it8->str, (byte)it8->ch);
                    it8.str.Append((char)(byte)it8.ch);
                    NextCh(it8);
                }
            }

            it8.sy = SYMBOL.SSTRING;
            NextCh(it8);
        }
        else
            SynError(it8, "String expected"u8);
    }

    private static void InSymbol(IT8 it8)
    {
        Span<byte> buffer = stackalloc byte[127];
        SYMBOL key;

        do
        {
            while (isseparator(it8.ch))
                NextCh(it8);

            if (isfirstidchar(it8.ch))
            {
                //StringClear(it8->id);
                it8.id.Clear();

                do
                {
                    //StringAppend(it8->id, (byte)it8->ch);
                    it8.id.Append((char)(byte)it8.ch);
                    NextCh(it8);
                } while (isidchar(it8.ch));

                key = BinSrchKey(Encoding.ASCII.GetBytes(it8.id.ToString()));
                if (key is SYMBOL.SUNDEFINED)
                    it8.sy = SYMBOL.SIDENT;
                else
                    it8.sy = key;
            }
            else if (Char.IsDigit((char)it8.ch) || it8.ch is '.' or '-' or '+')
            {
                var sign = 1;

                if (it8.ch is '-')
                {
                    sign = -1;
                    NextCh(it8);
                }

                it8.inum = 0;
                it8.sy = SYMBOL.SINUM;

                if (it8.ch is '0')     // 0xnnnn (Hexa) or 0bnnnn (Binary)
                {
                    NextCh(it8);
                    if (Char.ToUpper((char)it8.ch) is 'X')
                    {
                        NextCh(it8);
                        while (Char.IsAsciiHexDigit((char)it8.ch))
                        {
                            it8.ch = Char.ToUpper((char)it8.ch);
                            var j = (it8.ch is >= 'A' and <= 'F')
                                        ? it8.ch - 'A' + 10
                                        : it8.ch - '0';

                            if (it8.inum * 16.0 + j > +2147483647.0)
                            {
                                SynError(it8, "Invalid hexadecimal number"u8);
                                return;
                            }

                            it8.inum = it8.inum * 16 + j;
                            NextCh(it8);
                        }

                        return;
                    }
                    else if (Char.ToUpper((char)it8.ch) is 'B')
                    {
                        NextCh(it8);
                        while (it8.ch is '0' or '1')
                        {
                            var j = it8.ch - '0';

                            if (it8.inum * 2.0 + j > +2147483647.0)
                            {
                                SynError(it8, "Invalid binary number"u8);
                                return;
                            }

                            it8.inum = it8.inum * 2 + j;
                            NextCh(it8);
                        }

                        return;
                    }
                }

                while (Char.IsDigit((char)it8.ch))
                {
                    var digit = it8.ch - '0';

                    if (it8.inum * 10.0 + digit > +2147483647.0)
                    {
                        ReadReal(it8, it8.inum);
                        it8.sy = SYMBOL.SDNUM;
                        it8.dnum *= sign;
                        return;
                    }

                    it8.inum = it8.inum * 10 + digit;
                    NextCh(it8);
                }

                if (it8.ch is '.')
                {
                    ReadReal(it8, it8.inum);
                    it8.sy = SYMBOL.SDNUM;
                    it8.dnum *= sign;
                    return;
                }

                it8.dnum *= sign;

                // Special case. Numbers followed by letters are taken as identifiers
                if (isidchar(it8.ch))
                {
                    var len = it8.sy is SYMBOL.SINUM
                                  ? snprintf(buffer, 127, DEFAULT_NUM_FORMAT, it8.inum)
                                  : snprintf(buffer, 127, it8.DoubleFormatter, it8.dnum);

                    //StringCat(it8->id, buffer);
                    it8.id.Append(Encoding.ASCII.GetString(buffer[..len]));

                    do
                    {
                        //StringAppend(it8->id, (byte)it8->ch);
                        it8.id.Append((char)(byte)it8.ch);
                        NextCh(it8);
                    } while (isidchar(it8.ch));

                    it8.sy = SYMBOL.SIDENT;
                }

                return;
            }
            else
            {
                switch (it8.ch)
                {
                    // EOF marker -- ignore it
                    case '\x1a':
                        NextCh(it8);
                        break;

                    // Eof stream markers
                    case 0:
                    case -1:
                        it8.sy = SYMBOL.SEOF;
                        break;

                    // Next line
                    case '\r':
                        NextCh(it8);
                        if (it8.ch is '\n')
                            NextCh(it8);
                        it8.sy = SYMBOL.SEOLN;
                        it8.lineno++;
                        break;

                    case '\n':
                        NextCh(it8);
                        it8.sy = SYMBOL.SEOLN;
                        it8.lineno++;
                        break;

                    // Comment
                    case '#':
                        NextCh(it8);
                        while (it8.ch is not '\0' and not '\n' and not '\r')
                            NextCh(it8);

                        it8.sy = SYMBOL.SCOMMENT;
                        break;

                    // String
                    case '\'':
                    case '\"':
                        InStringSymbol(it8);
                        break;

                    default:
                        SynError(it8, "Unrecognized character: 0x{0:x}"u8, it8.ch);
                        return;
                }
            }
        } while (it8.sy is SYMBOL.SCOMMENT);

        // Handle the include special token

        if (it8.sy is SYMBOL.SINCLUDE)
        {
            if (it8.IncludeSP >= MAXINCLUDE - 1)
            {
                SynError(it8, "Too many recursion levels"u8);
                return;
            }

            InStringSymbol(it8);
            if (!Check(it8, SYMBOL.SSTRING, "Filename expected"))
                return;

            var FileNest = it8.FileStack[it8.IncludeSP + 1];
            if (FileNest is null)
            {
                //FileNest = it8.FileStack[it8.IncludeSP + 1] = AllocChunk<FILECTX>(it8);
                FileNest = it8.FileStack[it8.IncludeSP + 1] = new();
                //FileNest.FileName = Context.GetPool<byte>(it8.ContextID).Rent(cmsMAX_PATH);
                FileNest.FileName = new byte[MaxPath];
            }

            //if (BuildAbsolutePath(StringPtr(it8->str), it8->FileStack[it8->IncludeSP]->FileName, FileNest->FileName, cmsMAX_PATH - 1) is false)
            if (!BuildAbsolutePath(
                    Encoding.ASCII.GetBytes(it8.str.ToString()),
                    it8.FileStack[it8.IncludeSP].FileName,
                    FileNest.FileName,
                    MaxPath - 1))
            {
                SynError(it8, "File path too long"u8);
                return;
            }

            try
            {
                FileNest.Stream = File.Open(SpanToString(FileNest.FileName), FileMode.Open, FileAccess.Read);
            }
            catch
            {
                FileNest.Stream = null;
            }

            if (FileNest.Stream is null)
            {
                SynError(it8, "File {0} not found"u8, SpanToString(FileNest.FileName));
                return;
            }

            it8.IncludeSP++;

            it8.ch = ' ';
            InSymbol(it8);
        }
    }

    private static bool CheckEOLN(IT8 it8)
    {
        if (!Check(it8, SYMBOL.SEOLN, "Expected separator"))
            return false;
        while (it8.sy is SYMBOL.SEOLN)
            InSymbol(it8);
        return true;
    }

    private static void Skip(IT8 it8, SYMBOL sy)
    {
        if (it8.sy == sy && it8.sy is not SYMBOL.SEOF)
            InSymbol(it8);
    }

    private static void SkipEOLN(IT8 it8)
    {
        while (it8.sy is SYMBOL.SEOLN)
            InSymbol(it8);
    }

    private static bool GetVal(IT8 it8, Span<byte> Buffer, uint max, string ErrorTitle)
    {
        switch (it8.sy)
        {
            case SYMBOL.SEOLN:
                Buffer[0] = 0;
                break;
            case SYMBOL.SIDENT:
                //strncpy(Buffer, StringPtr(it8.id), max);
                strncpy(Buffer, it8.id.ToString(), max);
                Buffer[(int)max - 1] = 0;
                break;
            case SYMBOL.SINUM:
                snprintf(Buffer, max, DEFAULT_NUM_FORMAT, it8.inum);
                break;
            case SYMBOL.SDNUM:
                snprintf(Buffer, max, it8.DoubleFormatter, it8.dnum);
                break;
            case SYMBOL.SSTRING:
                //strncpy(Buffer, StringPtr(it8.str), max);
                strncpy(Buffer, it8.str.ToString(), max);
                Buffer[(int)max - 1] = 0;
                break;
            default:
                return SynError(it8, ErrorTitle);
        }

        Buffer[(int)max] = 0;
        return true;
    }

    private static ref TABLE GetTable(IT8 it8)
    {
        if (it8.nTable >= it8.TablesCount)
        {
            SynError(it8, "Table {0} out of sequence"u8, it8.nTable);
            return ref it8.Tab[0];
        }

        return ref it8.Tab[it8.nTable];
    }

    public static void cmsIT8Free(object? hIT8)
    {
        //if (hIT8 is not IT8 it8)
        return;

        //if (it8.MemorySink is not null)
        //{
        //    foreach (var p in it8.MemorySink)
        //    //{
        //    //    n = p->Next;
        //    //    if (p->Ptr is not null) _cmsFree(it8->ContextID, p->Ptr);
        //        ReturnArray(it8.ContextID, p);
        //    //}
        //}

        //if (it8.MemoryBlock is not null)
        //    ReturnArray(it8.ContextID, it8.MemoryBlock);

        //ReturnArray(it8.ContextID, it8.DoubleFormatter);

        //_cmsFree(it8->ContextID, it8);
    }

    //private static Span<byte> AllocBigBlock(IT8 it8, uint size)
    //{
    //    var ptr = _cmsMallocZero(it8->ContextID, size, typeof(byte));
    //    var ptr = Context.GetPool<byte>(it8.ContextID).Rent((int)size);

    //    var ptr1 = _cmsMallocZero<OWNEDMEM>(it8->ContextID);

    //    if (ptr1 is null)
    //    {
    //        _cmsFree(it8->ContextID, ptr);
    //        return null;
    //    }

    //    ptr1.Ptr = ptr;
    //    ptr1.Next = it8.MemorySink;
    //    it8.MemorySink.Add(ptr);

    //    return ptr.AsSpan(..(int)size);
    //}

    //private static T** AllocChunk2<T>(IT8 it8, uint count) where T : struct =>
    //    (T**)AllocChunk(it8, _sizeof<nint>() * count);

    //private static T* AllocChunk<T>(IT8 it8) where T : struct =>
    //    (T*)AllocChunk(it8, _sizeof<T>());

    //private static void* AllocChunk(IT8 it8, uint size)
    //{
    //    var Free = it8.Allocator.BlockSize - it8.Allocator.Used;

    //    size = _cmsALIGNMEM(size);

    //    if (size > Free)
    //    {
    //        it8.Allocator.BlockSize = (it8.Allocator.BlockSize is 0)
    //            ? 20 * 1024
    //            : it8.Allocator.BlockSize * 2;

    //        if (it8.Allocator.BlockSize < size)
    //            it8.Allocator.BlockSize = size;

    //        it8.Allocator.Used = 0;
    //        it8.Allocator.Block = (byte*)AllocBigBlock(it8, it8.Allocator.BlockSize);
    //    }

    //    var ptr = it8.Allocator.Block + it8.Allocator.Used;
    //    it8.Allocator.Used += size;

    //    return ptr;
    //}

    private static byte[] AllocString(IT8 it8, ReadOnlySpan<byte> str)
    {
        var Size = strlen(str) /* + sizeof(byte)*/;

        //var ptr = Context.GetPool<byte>(it8.ContextID).Rent((int)Size); //(byte*)AllocChunk(it8, (uint)Size);
        var ptr = new byte[Size];
        str.CopyTo(ptr); //if (ptr is not null) memcpy(ptr, str, Size - (nint)sizeof(byte));

        return ptr;
    }

    private static bool IsAvailableOnList(KEYVALUE? p,
                                          ReadOnlySpan<byte> Key,
                                          ReadOnlySpan<byte> Subkey,
                                          out KEYVALUE LastPtr)
    {
        /*if (LastPtr is not null)*/
        LastPtr = p!;

        for (; p is not null; p = p.Next!)
        {
            /*if (LastPtr is not null)*/
            LastPtr = p;

            if ((char)Key[0] is not '#')    // Comments are ignored
            {
                if (cmsstrcasecmp(Key, p.Keyword) is 0)
                    break;
            }
        }

        if (p is null)
            return false;

        if (Subkey.IsEmpty)
            return true;

        for (; p is not null; p = p.NextSubkey!)
        {
            if (p.Subkey is null)
                continue;

            /*if (LastPtr is not null)*/
            LastPtr = p;

            if (cmsstrcasecmp(Subkey, p.Subkey) is 0)
                return true;
        }

        return false;
    }

    private static KEYVALUE? AddToList(IT8 it8,
                                       ref KEYVALUE? Head,
                                       ReadOnlySpan<byte> Key,
                                       ReadOnlySpan<byte> Subkey,
                                       ReadOnlySpan<byte> xValue,
                                       WRITEMODE WriteAs)
    {
        // Check if property is already in list
        if (IsAvailableOnList(Head, Key, Subkey, out var p))
        {
            // This may work for editing properties

            //     return SynError(it8, "duplicate key <{0}>".ToCharPtr(), Key);
        }
        else
        {
            var last = p;

            // Allocate the container
            //p = AllocChunk<KEYVALUE>(it8);
            //if (p is null)
            //{
            //    SynError(it8, "AddToList: out of memory"u8);
            //    return null;
            //}
            p = new();

            // Store name and value
            p.Keyword = AllocString(it8, Key);
            p.Subkey = Subkey.IsEmpty ? null : AllocString(it8, Subkey);

            // Keep the container in our list
            if (Head is null)
            {
                Head = p;
            }
            else
            {
                if (!Subkey.IsEmpty && last is not null)
                {
                    last.NextSubkey = p;

                    // If Subkey is not null, then last is the last property with the same key,
                    // but not necessarily is the last property in the list, so we need to move
                    // to the actual list end
                    while (last.Next is not null)
                        last = last.Next;
                }

                if (last is not null)
                    last.Next = p;
            }

            p.Next = null;
            p.NextSubkey = null;
        }

        p.WriteAs = WriteAs;

        p.Value = !xValue.IsEmpty
                      ? AllocString(it8, xValue)
                      : null;

        return p;
    }

    private static KEYVALUE? AddAvailableProperty(IT8 it8, ReadOnlySpan<byte> Key, WRITEMODE @as) =>
        AddToList(it8, ref it8.ValidKeywords, Key, null, null, @as);

    private static KEYVALUE? AddAvailableSampleID(IT8 it8, ReadOnlySpan<byte> Key) =>
        AddToList(it8, ref it8.ValidSampleID, Key, null, null, WRITEMODE.WRITE_UNCOOKED);

    private static void AllocTable(IT8 it8)
    {
        var t = it8.Tab[it8.TablesCount];

        t.HeaderList = null;
        t.DataFormat = null!;
        t.Data = null!;

        it8.Tab[it8.TablesCount] = t;

        it8.TablesCount++;
    }

    public static int cmsIT8SetTable(object? IT8, uint nTable)
    {
        if (IT8 is not IT8 it8)
            return -1;

        if (nTable >= it8.TablesCount)
        {
            if (nTable == it8.TablesCount)
            {
                AllocTable(it8);
            }
            else
            {
                SynError(it8, "Table {0} is out of sequence"u8, nTable);
                return -1;
            }
        }

        it8.nTable = nTable;

        return (int)nTable;
    }

    public static object cmsIT8Alloc(Context? ContextID)
    {
        //var it8 = _cmsMallocZero<IT8>(ContextID);
        //if (it8 is null) return null;
        var it8 = new IT8
        {
            //it8->Tab = _cmsCalloc<TABLE>(ContextID, MAXTABLES);
            //if (it8->Tab is null) goto Error;
            //Tab = Context.GetPool<TABLE>(ContextID).Rent(MAXID),
            Tab = new TABLE[MAXID],

            //it8->FileStack = _cmsCalloc2<FILECTX>(ContextID, MAXINCLUDE);
            //if (it8->FileStack is null) goto Error;
            //FileStack = Context.GetPool<FILECTX>(ContextID).Rent(MAXID)
            FileStack = new FILECTX[MAXID],
        };

        AllocTable(it8);

        it8.MemoryBlock = null;
        it8.MemorySink = null;

        it8.nTable = 0;

        it8.ContextID = ContextID;
        it8.Allocator.Used = 0;
        it8.Allocator.Block = null;
        it8.Allocator.BlockSize = 0;

        it8.ValidKeywords = null;
        it8.ValidSampleID = null;

        it8.sy = SYMBOL.SUNDEFINED;
        it8.ch = ' ';
        it8.Source = null;
        it8.inum = 0;
        it8.dnum = 0;

        //it8.FileStack[0] = AllocChunk<FILECTX>(it8);
        it8.IncludeSP = 0;
        it8.lineno = 1;

        //it8.id = StringAlloc(it8, MAXSTR);
        //it8.str = StringAlloc(it8, MAXSTR);

        //it8.DoubleFormatter = Context.GetPool<byte>(ContextID).Rent(MAXID);
        it8.DoubleFormatter = new byte[MAXID];
        strcpy(it8.DoubleFormatter, DEFAULT_DBL_FORMAT);
        cmsIT8SetSheetType(it8, "CGATS.17");

        // Initialize predefined properties & data

        for (var i = 0; i < NUMPREDEFINEDPROPS; i++)
            AddAvailableProperty(it8, PredefinedProperties[i].id, PredefinedProperties[i].@as);

        for (var i = 0; i < NUMPREDEFINEDSAMPLEID; i++)
            AddAvailableSampleID(it8, PredefinedSampleID[i]);

        return it8;

        //Error:
        //    if (it8->Tab is not null) _cmsFree(ContextID, it8->Tab);
        //    if (it8 is not null) _cmsFree(ContextID, it8);

        //    return null;
    }

    public static byte[]? cmsIT8GetSheetType(object? hIT8) =>
        hIT8 is IT8 it8 ? GetTable(it8).SheetType : null;

    public static bool cmsIT8SetSheetType(object? hIT8, string Type)
    {
        if (hIT8 is not IT8 it8)
            return false;

        var t = GetTable(it8);

        strncpy(t.SheetType, Type, MAXSTR - 1);
        t.SheetType[MAXSTR - 1] = 0;
        return true;
    }

    public static bool cmsIT8SetComment(object? hIT8, string Val)
    {
        if (hIT8 is not IT8 it8)
            return false;

        if (String.IsNullOrEmpty(Val))
            return false;

        Span<byte> buf = stackalloc byte[Encoding.ASCII.GetByteCount(Val)];
        Encoding.ASCII.GetBytes(Val, buf);

        return AddToList(
                   it8,
                   ref GetTable(it8).HeaderList,
                   COMMENT_DELIMITER,
                   null,
                   buf,
                   WRITEMODE.WRITE_UNCOOKED) is not null;
    }

    public static bool cmsIT8SetPropertyStr(object? hIT8, ReadOnlySpan<byte> Key, string Val)
    {
        if (hIT8 is not IT8 it8)
            return false;

        if (String.IsNullOrEmpty(Val))
            return false;

        Span<byte> buf = stackalloc byte[Encoding.ASCII.GetByteCount(Val)];
        Encoding.ASCII.GetBytes(Val, buf);

        return AddToList(it8, ref GetTable(it8).HeaderList, Key, null, buf, WRITEMODE.WRITE_STRINGIFY) is not null;
    }

    public static bool cmsIT8SetPropertyDbl(object? hIT8, ReadOnlySpan<byte> cProp, double Val)
    {
        if (hIT8 is not IT8 it8)
            return false;

        Span<byte> Buffer = stackalloc byte[1024];

        snprintf(Buffer, 1023, it8.DoubleFormatter, Val);

        return AddToList(it8, ref GetTable(it8).HeaderList, cProp, null, Buffer, WRITEMODE.WRITE_UNCOOKED) is not null;
    }

    public static bool cmsIT8SetPropertyHex(object? hIT8, ReadOnlySpan<byte> cProp, uint Val)
    {
        if (hIT8 is not IT8 it8)
            return false;

        Span<byte> Buffer = stackalloc byte[1024];

        snprintf(Buffer, 1023, DEFAULT_HEX_FORMAT, Val);

        return AddToList(
                   it8,
                   ref GetTable(it8).HeaderList,
                   cProp,
                   null,
                   Buffer,
                   WRITEMODE.WRITE_HEXADECIMAL) is not null;
    }

    public static bool cmsIT8SetPropertyUncooked(object? hIT8, ReadOnlySpan<byte> Key, ReadOnlySpan<byte> Buffer) =>
        hIT8 is IT8 it8 &&
        AddToList(it8, ref GetTable(it8).HeaderList, Key, null, Buffer, WRITEMODE.WRITE_UNCOOKED) is not null;

    public static bool cmsIT8SetPropertyMulti(object? hIT8,
                                              ReadOnlySpan<byte> Key,
                                              ReadOnlySpan<byte> Subkey,
                                              ReadOnlySpan<byte> Buffer) =>
        hIT8 is IT8 it8 &&
        AddToList(it8, ref GetTable(it8).HeaderList, Key, Subkey, Buffer, WRITEMODE.WRITE_PAIR) is not null;

    public static byte[]? cmsIT8GetProperty(object? hIT8, ReadOnlySpan<byte> Key)
    {
        if (hIT8 is not IT8 it8)
            return null;

        if (IsAvailableOnList(GetTable(it8).HeaderList, Key, null, out var p))
            return p.Value;
        return null;
    }

    public static double cmsIT8GetPropertyDbl(object? hIT8, ReadOnlySpan<byte> cProp)
    {
        var v = cmsIT8GetProperty(hIT8, cProp);

        if (v is null)
            return 0;

        return ParseFloatNumber(v);
    }

    public static byte[]? cmsIT8GetPropertyMulti(object? hIT8, ReadOnlySpan<byte> Key, ReadOnlySpan<byte> Subkey)
    {
        if (hIT8 is not IT8 it8)
            return null;

        if (IsAvailableOnList(GetTable(it8).HeaderList, Key, Subkey, out var p))
            return p.Value;
        return null;
    }

    private static void AllocateDataFormat(IT8 it8)
    {
        var t = GetTable(it8);

        if (t.DataFormat is not null)
            return;  // Already allocated

        t.nSamples = (int)cmsIT8GetPropertyDbl(it8, "NUMBER_OF_FIELDS"u8);

        if (t.nSamples <= 0)
        {
            SynError(it8, "AllocateDataFormat: Unknown NUMBER_OF_FIELDS"u8);
            t.nSamples = 10;
        }

        //t.DataFormat = Context.GetPool<byte[]>(it8.ContextID).Rent(t.nSamples + 1); //t->DataFormat = AllocChunk2<byte>(it8, (uint)t->nSamples + 1);
        t.DataFormat = new byte[t.nSamples + 1][];
        if (t.DataFormat is null)
            SynError(it8, "AllocateDataFormat: Unable to allocate dataFormat array"u8);
    }

    private static byte[]? GetDataFormat(IT8 it8, int n)
    {
        var t = GetTable(it8);

        if (t.DataFormat is not null)
            return t.DataFormat[n];
        return null;
    }

    private static bool SetDataFormat(IT8 it8, int n, ReadOnlySpan<byte> label)
    {
        var t = GetTable(it8);

        if (t.DataFormat is null)
            AllocateDataFormat(it8);

        if (n > t.nSamples)
        {
            SynError(it8, "More than NUMBER_OF_FIELDS fields"u8);
            return false;
        }

        if (t.DataFormat is not null)
            t.DataFormat[n] = AllocString(it8, label);

        return true;
    }

    public static bool cmsIT8SetDataFormat(object? h, int n, ReadOnlySpan<byte> Sample) =>
        h is IT8 it8 && SetDataFormat(it8, n, Sample);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int atoi(ReadOnlySpan<byte> str) =>
        Int32.Parse(SpanToString(str));

    private static int satoi(ReadOnlySpan<byte> b) =>
        !b.IsEmpty
            ? atoi(b)
            : 0;

    private static ReadOnlySpan<byte> satob(ReadOnlySpan<byte> v)
    {
        var buf = BinaryConversionBuffer;
        var s = 33;

        if (v.IsEmpty)
            return "0"u8;

        var x = atoi(v);
        buf[--s] = 0;
        if (x is 0)
            buf[--s] = (byte)'0';
        for (; x is not 0; x /= 2)
            buf[--s] = (byte)('0' + (x % 2));

        return buf[s..];
    }

    private static void AllocateDataSet(IT8 it8)
    {
        var t = GetTable(it8);

        if (t.Data is not null)
            return;    // Already allocated

        t.nSamples = satoi(cmsIT8GetProperty(it8, "NUMBER_OF_FIELDS"u8));
        t.nPatches = satoi(cmsIT8GetProperty(it8, "NUMBER_OF_SETS"u8));

        if (t.nSamples is < 0 or > 0x7ffe || t.nPatches is < 0 or > 0x7ffe)
        {
            SynError(it8, "AllocateDataSet: too much data"u8);
        }
        else
        {
            //t->Data = AllocChunk2<byte>(it8, ((uint)t->nSamples + 1) * ((uint)t->nPatches + 1));
            //t.Data = Context.GetPool<byte[]>(it8.ContextID).Rent((t.nSamples + 1) * (t.nPatches + 1));
            t.Data = new byte[(t.nSamples + 1) * (t.nPatches + 1)][];
            if (t.Data is null)
                SynError(it8, "AllocateDataSet: Unable to allocate data array"u8);
        }
    }

    private static byte[]? GetData(IT8 it8, int nSet, int nField)
    {
        var t = GetTable(it8);
        var nSamples = t.nSamples;
        var nPatches = t.nPatches;

        if (nSet >= nPatches || nField >= nSamples)
            return null;

        if (t.Data is null)
            return null;
        return t.Data[nSet * nSamples + nField];
    }

    private static bool SetData(IT8 it8, int nSet, int nField, ReadOnlySpan<byte> Val)
    {
        var t = GetTable(it8);

        if (t.Data is null)
            AllocateDataSet(it8);

        if (t.Data is null)
            return false;

        if (nSet > t.nPatches || nSet < 0)
            return SynError(it8, "Patch {0} out of range, there are {1} patches"u8, nSet, t.nPatches);

        if (nField > t.nSamples || nField < 0)
            return SynError(it8, "Sample {0} out of range, there are {1} samples"u8, nSet, t.nSamples);

        t.Data[nSet * t.nSamples + nField] = AllocString(it8, Val);
        return true;
    }

    private static void WriteStr(SAVESTREAM f, ReadOnlySpan<byte> str)
    {
        if (str.IsEmpty)
            str = " "u8;

        // Length to write
        var len = (uint)strlen(str);
        f.Used += len;

        if (f.stream is not null)  // Should I write it to a file?
        {
            if (fwrite(str, 1, len, f.stream) != len)
            {
                Context.LogError(null, cmsERROR_WRITE, "Write to file error in CGATS parser");
                return;
            }
        }
        else        // Or to a memory block?
        {
            if (f.Base is not null)    // Am I just counting the bytes?
            {
                if (f.Used > f.Max)
                {
                    Context.LogError(null, cmsERROR_WRITE, "Write to memoty overflows in CGATS parser");
                    return;
                }

                memmove(f.Ptr.Span, str, len);
                f.Ptr = f.Ptr[(int)len..];
            }
        }
    }

    private static void Writef(SAVESTREAM f, ReadOnlySpan<byte> frm, params object[] args)
    {
        Span<byte> Buffer = stackalloc byte[4096];

        snprintf(Buffer, 4095, frm, args);
        Buffer[4096] = 0;
        WriteStr(f, Buffer);
    }

    private static void WriteHeader(IT8 it8, SAVESTREAM fp)
    {
        var t = GetTable(it8);

        // Writes the type
        WriteStr(fp, t.SheetType);
        WriteStr(fp, "\n"u8);

        for (var p = t.HeaderList; p is not null; p = p.Next)
        {
            if ((char)p.Keyword[0] is '#')
            {
                WriteStr(fp, "#\n# "u8);
                var Pt = p.Value;
                for (var i = 0; Pt[i] is not 0; i++)
                {
                    Writef(fp, "{0}"u8, Pt[i]);

                    if ((char)Pt[i] is '\n')
                        WriteStr(fp, "# "u8);
                }

                WriteStr(fp, "\n#\n"u8);
                continue;
            }

            if (!IsAvailableOnList(it8.ValidKeywords, p.Keyword, null, out _))
            {
                //WriteStr(fp, "KEYWORD\t\"".ToCharPtr());
                //WriteStr(fp, p->Keyword);
                //WriteStr(fp, "\"\n".ToCharPtr());

                AddAvailableProperty(it8, p.Keyword, WRITEMODE.WRITE_UNCOOKED);
            }

            WriteStr(fp, p.Keyword);
            if (p.Value is not null)
            {
                switch (p.WriteAs)
                {
                    case WRITEMODE.WRITE_UNCOOKED:
                        Writef(fp, "\t{0}"u8, SpanToString(p.Value));
                        break;
                    case WRITEMODE.WRITE_STRINGIFY:
                        Writef(fp, "\t\"{0}\""u8, SpanToString(p.Value));
                        break;
                    case WRITEMODE.WRITE_HEXADECIMAL:
                        Writef(fp, "\t0x{0:X}"u8, satoi(p.Value));
                        break;
                    case WRITEMODE.WRITE_BINARY:
                        Writef(fp, "\t0b{0}"u8, SpanToString(satob(p.Value)));
                        break;
                    case WRITEMODE.WRITE_PAIR:
                        Writef(fp, "\t\"{0},{1}\""u8, SpanToString(p.Subkey), SpanToString(p.Value));
                        break;
                    default:
                        SynError(it8, "Unknown write mode {0}"u8, (object?)Enum.GetName(p.WriteAs) ?? p.WriteAs);
                        return;
                }
            }

            WriteStr(fp, "\n"u8);
        }
    }

    private static void WriteDataFormat(SAVESTREAM fp, IT8 it8)
    {
        var t = GetTable(it8);

        if (t.DataFormat is null)
            return;

        WriteStr(fp, "BEGIN_DATA_FORMAT\n"u8);
        WriteStr(fp, " "u8);
        var nSamples = satoi(cmsIT8GetProperty(it8, "NUMBER_OF_FIELDS"u8));

        for (var i = 0; i < nSamples; i++)
        {
            WriteStr(fp, t.DataFormat[i]);
            WriteStr(fp, ((i == (nSamples - 1)) ? "\n"u8 : "\t"u8));
        }

        WriteStr(fp, "END_DATA_FORMAT\n"u8);
    }

    private static void WriteData(SAVESTREAM fp, IT8 it8)
    {
        var t = GetTable(it8);

        if (t.Data is null)
            return;

        WriteStr(fp, "BEGIN_DATA\n"u8);

        t.nPatches = satoi(cmsIT8GetProperty(it8, "NUMBER_OF_SETS"u8));

        for (var i = 0; i < t.nPatches; i++)
        {
            WriteStr(fp, " "u8);

            for (var j = 0; j < t.nSamples; j++)
            {
                var ptr = t.Data[(i * t.nSamples) + j];

                if (ptr is null)
                    WriteStr(fp, "\"\""u8);
                else
                {
                    // If value contains whitespace, enclose within quote

                    if (!strchr(ptr, ' ').IsEmpty)
                    {
                        WriteStr(fp, "\""u8);
                        WriteStr(fp, ptr);
                        WriteStr(fp, "\""u8);
                    }
                    else
                        WriteStr(fp, ptr);
                }

                WriteStr(fp, ((j == (t.nSamples - 1)) ? "\n"u8 : "\t"u8));
            }
        }

        WriteStr(fp, "END_DATA\n"u8);
    }

    public static bool cmsIT8SaveToFile(object? hIT8, ReadOnlySpan<char> cFileName)
    {
        SAVESTREAM sd = new();
        if (hIT8 is not IT8 it8)
            return false;

        //memset(&sd, 0);

        sd.stream = fopen(new(cFileName), "wt");
        if (sd.stream is null)
            return false;

        for (var i = 0u; i < it8.TablesCount; i++)
        {
            cmsIT8SetTable(hIT8, i);
            WriteHeader(it8, sd);
            WriteDataFormat(sd, it8);
            WriteData(sd, it8);
        }

        if (fclose(sd.stream) is not 0)
            return false;

        return true;
    }

    public static bool cmsIT8SaveToMem(object? hIT8, byte[] MemPtr, ref uint BytesNeeded)
    {
        SAVESTREAM sd = new();
        if (hIT8 is not IT8 it8)
            return false;

        //memset(&sd, 0);

        sd.stream = null;
        sd.Base = MemPtr;
        sd.Ptr = sd.Base;

        sd.Used = 0;

        sd.Max = (sd.Base is not null)
                     ? BytesNeeded // Write to memory?
                     : 0;          // Just counting the needed bytes

        for (var i = 0u; i < it8.TablesCount; i++)
        {
            cmsIT8SetTable(hIT8, i);
            WriteHeader(it8, sd);
            WriteDataFormat(sd, it8);
            WriteData(sd, it8);
        }

        sd.Used++;      // the \0 at the very end

        if (sd.Base is not null)
            sd.Ptr.Span[0] = 0;

        BytesNeeded = sd.Used;

        return true;
    }

    private static bool DataFormatSection(IT8 it8)
    {
        var iField = 0;
        var t = GetTable(it8);

        InSymbol(it8);      // Eats "BEGIN_DATA_FORMAT"
        CheckEOLN(it8);

        while (it8.sy is not SYMBOL.SEND_DATA_FORMAT
                     and not SYMBOL.SEOLN
                     and not SYMBOL.SEOF
                     and not SYMBOL.SSYNERROR)
        {
            if (it8.sy is not SYMBOL.SIDENT)
                return SynError(it8, "Sample type expected"u8);

            //if (!SetDataFormat(it8, iField, StringPtr(it8->id))) return false;
            if (!SetDataFormat(it8, iField, Encoding.ASCII.GetBytes(it8.id.ToString())))
                return false;
            iField++;

            InSymbol(it8);
            SkipEOLN(it8);
        }

        SkipEOLN(it8);
        Skip(it8, SYMBOL.SEND_DATA_FORMAT);
        SkipEOLN(it8);

        if (iField != t.nSamples)
            SynError(it8, "Count mismatch. NUMBER_OF_FIELDS was {0}, found {1}\n"u8, t.nSamples, iField);

        return true;
    }

    private static bool DataSection(IT8 it8)
    {
        Span<byte> Buffer = stackalloc byte[256];
        var iField = 0;
        var iSet = 0;
        var t = GetTable(it8);

        InSymbol(it8);      // Eats "BEGIN_DATA"
        CheckEOLN(it8);

        if (t.Data is null)
            AllocateDataSet(it8);

        while (it8.sy is not SYMBOL.SEND_DATA and not SYMBOL.SEOF)
        {
            if (iField >= t.nSamples)
            {
                iField = 0;
                iSet++;
            }

            if (it8.sy is not SYMBOL.SEND_DATA and not SYMBOL.SEOF)
            {
                switch (it8.sy)
                {
                    // To keep very long data
                    case SYMBOL.SIDENT:
                        //if (!SetData(it8, iSet, iField, StringPtr(it8->id)))
                        if (!SetData(it8, iSet, iField, Encoding.ASCII.GetBytes(it8.id.ToString())))
                            return false;
                        break;
                    case SYMBOL.SSTRING:
                        //if (!SetData(it8, iSet, iField, StringPtr(it8->str)))
                        if (!SetData(it8, iSet, iField, Encoding.ASCII.GetBytes(it8.str.ToString())))
                            return false;
                        break;
                    default:
                        if (!GetVal(it8, Buffer, 255, "Sample data expected"))
                            return false;
                        if (!SetData(it8, iSet, iField, Buffer))
                            return false;
                        break;
                }

                iField++;

                InSymbol(it8);
                SkipEOLN(it8);
            }
        }

        SkipEOLN(it8);
        Skip(it8, SYMBOL.SEND_DATA);
        SkipEOLN(it8);

        // Check for data completion.

        if ((iSet + 1) != t.nPatches)
            return SynError(it8, "Count mismatch. NUMBER_OF_SETS was {0}, found {1}\n"u8, t.nPatches, iSet + 1);

        return true;
    }

    private static bool HeaderSection(IT8 it8)
    {
        Span<byte> VarName = stackalloc byte[MAXID];
        Span<byte> Buffer = stackalloc byte[MAXSTR];
        KEYVALUE? Key;

        while (it8.sy is not SYMBOL.SEOF
                     and not SYMBOL.SSYNERROR
                     and not SYMBOL.SBEGIN_DATA_FORMAT
                     and not SYMBOL.SBEGIN_DATA)
        {
            switch (it8.sy)
            {
                case SYMBOL.SKEYWORD:
                    InSymbol(it8);
                    if (!GetVal(it8, Buffer, MAXSTR - 1, "Keyword expected"))
                        return false;
                    if (AddAvailableProperty(it8, Buffer, WRITEMODE.WRITE_UNCOOKED) is null)
                        return false;
                    InSymbol(it8);
                    break;

                case SYMBOL.SDATA_FORMAT_ID:
                    InSymbol(it8);
                    if (!GetVal(it8, Buffer, MAXSTR - 1, "Keyword expected"))
                        return false;
                    if (AddAvailableSampleID(it8, Buffer) is null)
                        return false;
                    InSymbol(it8);
                    break;

                case SYMBOL.SIDENT:
                    //strncpy(VarName, StringPtr(it8->id), MAXID - 1);
                    strncpy(VarName, it8.id.ToString(), MAXID - 1);
                    VarName[MAXID - 1] = 0;

                    if (!IsAvailableOnList(it8.ValidKeywords, VarName, null, out Key))
                    {
                        //return SynError(it8, "Undefined keyword '{0}'".ToCharPtr(), new string(VarName));

                        Key = AddAvailableProperty(it8, VarName, WRITEMODE.WRITE_UNCOOKED);
                        if (Key is null)
                            return false;
                    }

                    InSymbol(it8);
                    if (!GetVal(it8, Buffer, MAXSTR - 1, "Property data expected"))
                        return false;

                    if (Key?.WriteAs is not WRITEMODE.WRITE_PAIR)
                    {
                        AddToList(
                            it8,
                            ref GetTable(it8).HeaderList,
                            VarName,
                            null,
                            Buffer,
                            (it8.sy is SYMBOL.SSTRING) ? WRITEMODE.WRITE_STRINGIFY : WRITEMODE.WRITE_UNCOOKED);
                    }
                    else
                    {
                        int Subkey, Nextkey = 0;

                        if (it8.sy is not SYMBOL.SSTRING)
                            return SynError(
                                it8,
                                "Invalid value '{0}' for property '{1}'."u8,
                                SpanToString(Buffer),
                                SpanToString(VarName));

                        // chop the string as a list of "subkey, value" pairs, using ';' as a separator
                        for (Subkey = 0; Buffer[Subkey] is not 0; Subkey = Nextkey)
                        {
                            int Value, temp;

                            // identify token pair boundary
                            var nextkeySpan = strchr(Buffer[Subkey..], ';');
                            if (!nextkeySpan.IsEmpty)
                            {
                                Nextkey = Buffer[Subkey..].IndexOf(nextkeySpan);
                                Buffer[Nextkey++] = 0;
                            }

                            // for each pair, split the subkey and the value
                            var valueSpan = strrchr(Buffer[Subkey..], (byte)',');
                            if (valueSpan.IsEmpty)
                                return SynError(it8, "Invalid value for property '{0}."u8, SpanToString(VarName));
                            Value = Buffer[Subkey..].IndexOf(valueSpan);

                            // gobble the spaces before the comma, and the comma itself
                            temp = Value++;
                            do
                                Buffer[temp--] = 0;
                            while (temp >= Subkey && Buffer[temp] == ' ');

                            // gobble any space at the right
                            temp = Value + (int)strlen(Buffer[Value..]) - 1;
                            while (Buffer[temp] == ' ')
                                Buffer[temp--] = 0;

                            // trim the strings from the left
                            Subkey += (int)strspn(Buffer[Subkey..], " "u8);
                            Value += (int)strspn(Buffer[Value..], " "u8);

                            if (Buffer[Subkey] is 0 || Buffer[Value] is 0)
                                return SynError(it8, "Invalid value for property '{0}'."u8, SpanToString(VarName));
                            AddToList(
                                it8,
                                ref GetTable(it8).HeaderList,
                                VarName,
                                Buffer[Subkey..],
                                Buffer[Value..],
                                WRITEMODE.WRITE_PAIR);
                        }
                    }

                    InSymbol(it8);
                    break;

                case SYMBOL.SEOLN:
                    break;

                default:
                    return SynError(it8, "expected keyword or identifier"u8);
            }

            SkipEOLN(it8);
        }

        return true;
    }

    private static void ReadType(IT8 it8, Span<byte> SheetTypePtr)
    {
        var cnt = 0;

        // First line is a very special case.

        while (isseparator(it8.ch))
            NextCh(it8);

        while (it8.ch is not '\r' and not '\n' and not '\t' and not '\0')
        {
            if (cnt < MAXSTR)
                SheetTypePtr[cnt++] = (byte)it8.ch;
            NextCh(it8);
        }

        SheetTypePtr[cnt] = 0;
    }

    private static bool ParseIT8(IT8 it8, bool nosheet)
    {
        var SheetTypePtr = it8.Tab[0].SheetType;

        if (!nosheet)
        {
            ReadType(it8, SheetTypePtr);
        }

        InSymbol(it8);

        SkipEOLN(it8);

        while (it8.sy is not SYMBOL.SEOF and not SYMBOL.SSYNERROR)
        {
            switch (it8.sy)
            {
                case SYMBOL.SBEGIN_DATA_FORMAT:
                    if (!DataFormatSection(it8))
                        return false;
                    break;

                case SYMBOL.SBEGIN_DATA:
                    if (!DataSection(it8))
                        return false;

                    if (it8.sy is not SYMBOL.SEOF)
                    {
                        AllocTable(it8);
                        it8.nTable = it8.TablesCount - 1;

                        // Read sheet type if present. We only support identifier and string
                        // <ident> <eoln> is a type string
                        // anything else, is not a type string
                        if (!nosheet)
                        {
                            if (it8.sy is SYMBOL.SIDENT)
                            {
                                // May be a type sheet or may be a prop value statement. We cannot use insymbol in
                                // this special case...
                                while (isseparator(it8.ch))
                                    NextCh(it8);

                                // If a newline is found, then this is a type string
                                if (it8.ch is '\n' or '\r')
                                {
                                    //cmsIT8SetSheetType(it8, SpanToString(StringPtr(it8->id)));
                                    cmsIT8SetSheetType(it8, it8.id.ToString());
                                    InSymbol(it8);
                                }
                                else
                                {
                                    // It is not. Just continue
                                    cmsIT8SetSheetType(it8, "");
                                }
                            }
                            else
                            {
                                // Validate quoted strings
                                if (it8.sy is SYMBOL.SSTRING)
                                {
                                    //cmsIT8SetSheetType(it8, SpanToString(StringPtr(it8->str)));
                                    cmsIT8SetSheetType(it8, it8.str.ToString());
                                    InSymbol(it8);
                                }
                            }
                        }
                    }

                    break;

                case SYMBOL.SEOLN:
                    SkipEOLN(it8);
                    break;

                default:
                    if (!HeaderSection(it8))
                        return false;
                    break;
            }
        }

        return it8.sy is not SYMBOL.SSYNERROR;
    }

    private static void CookPointers(IT8 it8)
    {
        Span<byte> Buffer = stackalloc byte[256];

        var nOldTable = it8.nTable;

        for (var j = 0u; j < it8.TablesCount; j++)
        {
            var t = it8.Tab[j];

            t.SampleID = 0;
            it8.nTable = j;

            for (var idField = 0; idField < t.nSamples; idField++)
            {
                if (t.DataFormat is null)
                {
                    SynError(it8, "Undefined DATA_FORMAT"u8);
                    return;
                }

                var Fld = t.DataFormat[idField];
                if (Fld == null)
                    continue;

                if (cmsstrcasecmp(Fld, "SAMPLE_ID"u8) is 0)
                    t.SampleID = idField;

                // "LABEL" is an extension. It keeps references to forward tables
                if ((cmsstrcasecmp(Fld, "LABEL"u8) is 0) || Fld[0] == '$')
                {
                    // Search for table references...
                    for (var i = 0; i < t.nPatches; i++)
                    {
                        var Label = GetData(it8, i, idField);

                        if (Label is not null)
                        {
                            // This is the label, search for a table containing
                            // this property

                            for (var k = 0u; k < it8.TablesCount; k++)
                            {
                                var Table = it8.Tab[k];

                                if (IsAvailableOnList(Table.HeaderList, Label, null, out var p))
                                {
                                    // Available, keep type and table
                                    //memset(Buffer, 0, 256 * sizeof(byte));
                                    Buffer.Clear();

                                    var Type = p.Value;
                                    var nTable = (int)k;

                                    snprintf(
                                        Buffer,
                                        255,
                                        "{0} {1} {2}"u8,
                                        SpanToString(Label),
                                        nTable,
                                        SpanToString(Type));

                                    SetData(it8, i, idField, Buffer);
                                }
                            }
                        }
                    }
                }
            }
        }

        it8.nTable = nOldTable;
    }

    private static int IsMyBlock(ReadOnlySpan<byte> Buffer, uint n)
    {
        int words = 1, space = 0, quot = 0;

        if (n < 10)
            return 0;       // Too small

        if (n > 132)
            n = 132;

        for (var i = 1u; i < n; i++)
        {
            switch ((char)Buffer[(int)i])
            {
                case '\n':
                case '\r':
                    return ((quot is 1) || (words > 2)) ? 0 : words;
                case '\t':
                case ' ':
                    if (quot is 0 && space is 0)
                        space = 1;
                    break;
                case '\"':
                    quot = quot is 1 ? 0 : 1;
                    break;
                default:
                    if (Buffer[(int)i] is < 32 or > 127)
                        return 0;
                    words += space;
                    space = 0;
                    break;
            }
        }

        return 0;
    }

    private static bool IsMyFile(string FileName)
    {
        Span<byte> Ptr = stackalloc byte[133];

        var fp = fopen(FileName, "rt");
        if (fp is null)
        {
            Context.LogError(null, cmsERROR_FILE, "File '{0}' not found", FileName);
            return false;
        }

        var Size = (uint)fread(Ptr, 1, 132, fp);

        if (fclose(fp) is not 0)
            return false;

        Ptr[(int)Size] = 0;

        return IsMyBlock(Ptr, Size) is not 0;
    }

    public static object? cmsIT8LoadFromMem(Context? ContextID, Span<byte> Ptr, uint len)
    {
        //_cmsAssert(Ptr);
        _cmsAssert(len is not 0);

        var type = IsMyBlock(Ptr, len);
        if (type is 0)
            return null;

        var hIT8 = cmsIT8Alloc(ContextID);
        if (hIT8 is not IT8 it8)
            return null;

        //var it8 = (IT8*)hIT8;
        //it8.MemoryBlock = GetArray<byte>(ContextID, len + 1);
        it8.MemoryBlock = new byte[len + 1];
        //if (it8.MemoryBlock is null)
        //{
        //    cmsIT8Free(hIT8);
        //    return null;
        //}

        strncpy(it8.MemoryBlock, Ptr, len);
        it8.MemoryBlock[len] = 0;

        strncpy(it8.FileStack[0].FileName, "", MaxPath - 1);
        it8.Source = it8.MemoryBlock;

        if (!ParseIT8(it8, type is not 0))
        {
            cmsIT8Free(hIT8);
            return null;
        }

        CookPointers(it8);
        it8.nTable = 0;

        //ReturnArray(ContextID, it8.MemoryBlock);
        it8.MemoryBlock = null;

        return hIT8;
    }

    public static object? cmsIT8LoadFromFile(Context? ContextID, string cFileName)
    {
        _cmsAssert(cFileName);

        var type = IsMyFile(cFileName);
        if (!type)
            return null;

        var hIT8 = cmsIT8Alloc(ContextID);
        if (hIT8 is not IT8 it8)
            return null;

        //var it8 = (IT8*)hIT8;
        var file = fopen(cFileName, "rt");

        if (file is null)
        {
            cmsIT8Free(hIT8);
            return null;
        }

        it8.FileStack[0].Stream = (FileStream)file.Stream;

        strncpy(it8.FileStack[0].FileName, cFileName, MaxPath - 1);
        it8.FileStack[0].FileName[MaxPath - 1] = 0;

        if (!ParseIT8(it8, !type))
        {
            fclose(file);
            cmsIT8Free(hIT8);
            return null;
        }

        CookPointers(it8);
        it8.nTable = 0;

        if (fclose(file) is not 0)
        {
            cmsIT8Free(hIT8);
            return null;
        }

        return hIT8;
    }

    public static int cmsIT8EnumDataFormat(object? hIT8, out byte[][] SampleNames)
    {
        SampleNames = null!;
        if (hIT8 is not IT8 it8)
            return -1;

        _cmsAssert(hIT8);

        var t = GetTable(it8);

        //if (SampleNames is not null)
        //*SampleNames = t->DataFormat;
        SampleNames = t.DataFormat;
        return t.nSamples;
    }

    public static uint cmsIT8EnumProperties(object? hIT8, out byte[][] PropertyNames)
    {
        PropertyNames = null!;
        if (hIT8 is not IT8 it8)
            return uint.MaxValue;

        var t = GetTable(it8);

        // Pass#1 - count properties

        var n = 0u;
        for (var p = t.HeaderList; p is not null; p = p.Next)
            n++;

        //var Props = AllocChunk2<byte>(it8, n);
        //var Props = Context.GetPool<byte[]>(it8.ContextID).Rent((int)n);
        var Props = new byte[n][];

        // Pass#2 - Fill pointers
        n = 0;
        for (var p = t.HeaderList; p is not null; p = p.Next)
            Props[n++] = p.Keyword;

        PropertyNames = Props;
        return n;
    }

    public static uint cmsIT8EnumPropertyMulti(object? hIT8, ReadOnlySpan<byte> cProp, out byte[][] SubpropertyNames)
    {
        SubpropertyNames = null!;
        if (hIT8 is not IT8 it8)
            return uint.MaxValue;

        var t = GetTable(it8);

        if (!IsAvailableOnList(t.HeaderList, cProp, null, out var p))
        {
            SubpropertyNames = null!;
            return 0;
        }

        // Pass#1 - count properties

        var n = 0u;
        for (var tmp = p; tmp is not null; tmp = tmp.NextSubkey)
        {
            if (tmp.Subkey is not null)
                n++;
        }

        //var Props = AllocChunk2<byte>(it8, n);
        //var Props = Context.GetPool<byte[]>(it8.ContextID).Rent((int)n);
        var Props = new byte[n][];

        // Pass#2 - Fill pointers
        n = 0;
        for (var tmp = p; tmp is not null; tmp = tmp.NextSubkey)
            Props[n++] = tmp.Subkey!;

        SubpropertyNames = Props;
        return n;
    }

    private static int LocatePatch(IT8 it8, ReadOnlySpan<byte> cPatch)
    {
        var t = GetTable(it8);

        for (var i = 0; i < t.nPatches; i++)
        {
            var data = GetData(it8, i, t.SampleID);

            if (data is not null)
            {
                if (cmsstrcasecmp(data, cPatch) is 0)
                    return i;
            }
        }

        // SynError(it8, "Couldn't find patch '{0}'\n"u8, new string((sbyte*)cPatch));
        return -1;
    }

    private static int LocateEmptyPatch(IT8 it8)
    {
        var t = GetTable(it8);

        for (var i = 0; i < t.nPatches; i++)
        {
            var data = GetData(it8, i, t.SampleID);

            if (data is null)
                return i;
        }

        return -1;
    }

    private static int LocateSample(IT8 it8, ReadOnlySpan<byte> cSample)
    {
        var t = GetTable(it8);

        for (var i = 0; i < t.nSamples; i++)
        {
            var fld = GetDataFormat(it8, i);
            if (fld is not null)
            {
                if (cmsstrcasecmp(fld, cSample) is 0)
                    return i;
            }
        }

        return -1;
    }

    public static int cmsIT8FindDataFormat(object? hIT8, ReadOnlySpan<byte> cSample) =>
        hIT8 is IT8 it8 ? LocateSample(it8, cSample) : -1;

    public static byte[]? cmsIT8GetDataRowCol(object? hIT8, int row, int col) =>
        hIT8 is not IT8 it8 ? null : GetData(it8, row, col);

    public static double cmsIT8GetDataRowColDbl(object? hIT8, int row, int col)
    {
        var Buffer = cmsIT8GetDataRowCol(hIT8, row, col);

        if (Buffer is null)
            return 0.0;

        return ParseFloatNumber(Buffer);
    }

    public static bool cmsIT8SetDataRowCol(object? hIT8, int row, int col, ReadOnlySpan<byte> Val) =>
        hIT8 is IT8 it8 && SetData(it8, row, col, Val);

    public static bool cmsIT8SetDataRowColDbl(object? hIT8, int row, int col, double Val)
    {
        Span<byte> Buff = stackalloc byte[256];

        if (hIT8 is not IT8 it8)
            return false;

        snprintf(Buff, 255, it8.DoubleFormatter, Val);

        return SetData(it8, row, col, Buff);
    }

    public static byte[]? cmsIT8GetData(object? hIT8, ReadOnlySpan<byte> cPatch, ReadOnlySpan<byte> cSample)
    {
        if (hIT8 is not IT8 it8)
            return null;

        var iField = LocateSample(it8, cSample);
        if (iField < 0)
            return null;

        var iSet = LocatePatch(it8, cPatch);
        if (iSet < 0)
            return null;

        return GetData(it8, iSet, iField);
    }

    public static double cmsIT8GetDataDbl(object? hIT8, ReadOnlySpan<byte> cPatch, ReadOnlySpan<byte> cSample) =>
        ParseFloatNumber(cmsIT8GetData(hIT8, cPatch, cSample));

    public static bool cmsIT8SetData(object? hIT8,
                                     ReadOnlySpan<byte> cPatch,
                                     ReadOnlySpan<byte> cSample,
                                     ReadOnlySpan<byte> Val)
    {
        if (hIT8 is not IT8 it8)
            return false;

        var t = GetTable(it8);

        var iField = LocateSample(it8, cSample);
        if (iField < 0)
            return false;

        if (t.nPatches is 0)
        {
            AllocateDataFormat(it8);
            AllocateDataSet(it8);
            CookPointers(it8);
        }

        int iSet;
        if (cmsstrcasecmp(cSample, "SAMPLE_ID"u8) is 0)
        {
            iSet = LocateEmptyPatch(it8);
            if (iSet < 0)
                return SynError(it8, "Couldn't add more patches '{0}'\n"u8, SpanToString(cPatch));

            iField = t.SampleID;
        }
        else
        {
            iSet = LocatePatch(it8, cPatch);
            if (iSet < 0)
                return false;
        }

        return SetData(it8, iSet, iField, Val);
    }

    public static bool cmsIT8SetDataDbl(object? hIT8, ReadOnlySpan<byte> cPatch, ReadOnlySpan<byte> cSample, double Val)
    {
        Span<byte> Buff = stackalloc byte[256];

        if (hIT8 is not IT8 it8)
            return false;

        snprintf(Buff, 255, it8.DoubleFormatter, Val);
        return cmsIT8SetData(hIT8, cPatch, cSample, Buff);
    }

    public static Span<byte> cmsIT8GetPatchName(object? hIT8, int nPatch, Span<byte> buffer)
    {
        if (hIT8 is not IT8 it8)
            return default;

        var t = GetTable(it8);
        var Data = GetData(it8, nPatch, t.SampleID);

        if (Data is null)
            return null;
        if (buffer.IsEmpty)
            return Data;

        strncpy(buffer, Data, MAXSTR - 1);
        buffer[MAXSTR - 1] = 0;
        return buffer;
    }

    public static int cmsIT8GetPatchByName(object? hIT8, ReadOnlySpan<byte> cPatch)
    {
        return hIT8 is not IT8 it8 ? -1 : LocatePatch(it8, cPatch);
    }

    public static uint cmsIT8TableCount(object? hIT8)
    {
        return hIT8 is not IT8 it8 ? uint.MaxValue : it8.TablesCount;
    }

    private static int ParseLabel(ReadOnlySpan<byte> buffer, Span<byte> label, out uint tableNum, Span<byte> type)
    {
        tableNum = 0;

        var str = SpanToString(buffer);
        var split = str.Split(' ');
        var result = split.Length;

        if (result is 0)
            return 0;

        if (split[0].Length > 255)
            split[0] = split[0][..255];

        strncpy(label, split[0], 255);

        if (result is 1)
            return 1;

        var success = uint.TryParse(split[1], out var n);
        if (!success)
            return 1;

        tableNum = n;

        if (result is 2)
            return 2;

        if (split[2].Length > 255)
            split[2] = split[2][..255];

        strncpy(type, split[2], 255);

        return 3;
    }

    public static int cmsIT8SetTableByLabel(object? hIT8,
                                            ReadOnlySpan<byte> cSet,
                                            ReadOnlySpan<byte> cField,
                                            ReadOnlySpan<byte> ExpectedType)
    {
        Span<byte> Type = stackalloc byte[256];
        Span<byte> Label = stackalloc byte[256];

        _cmsAssert(hIT8);

        if ((!cField.IsEmpty && cField[0] is 0) || cField.IsEmpty)
            cField = "LABEL"u8;

        var cLabelFld = cmsIT8GetData(hIT8, cSet, cField);
        if (cLabelFld is null)
            return -1;

        if (ParseLabel(cLabelFld, Label, out var nTable, Type) is not 3)
            return -1;

        if (!ExpectedType.IsEmpty && ExpectedType[0] is 0)
            ExpectedType = null;

        if (!ExpectedType.IsEmpty && cmsstrcasecmp(Type, ExpectedType) is not 0)
            return -1;

        return cmsIT8SetTable(hIT8, nTable);
    }

    public static bool cmsIT8SetIndexColumn(object? hIT8, ReadOnlySpan<byte> cSample)
    {
        if (hIT8 is not IT8 it8)
            return false;

        var pos = LocateSample(it8, cSample);
        if (pos is -1)
            return false;

        it8.Tab[it8.nTable].SampleID = pos;
        return true;
    }

    public static void cmsIT8DefineDblFormat(object? hIT8, ReadOnlySpan<byte> Formatter)
    {
        if (hIT8 is not IT8 it8)
            return;

        if (Formatter.IsEmpty)
            strcpy(it8.DoubleFormatter, DEFAULT_DBL_FORMAT);
        else
            strncpy(it8.DoubleFormatter, Formatter, MAXID);

        it8.DoubleFormatter[MAXID - 1] = 0;
    }

    public static Profile? cmsCreateDeviceLinkFromCubeFileTHR(Context? context, string FileName)
    {
        throw new NotImplementedException();
    }

    public static Profile? cmsCreateDeviceLinkFromCubeFile(string FileName)
    {
        throw new NotImplementedException();
    }
}
