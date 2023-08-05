﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2022 Marti Maria Saguer
//                2022-2023 Stefan Kewatt
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
using lcms2.state;
using lcms2.types;

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace lcms2;

public static partial class Lcms2
{
    [DebuggerStepThrough]
    private static uint NULLRead(IOHandler iohandler, Span<byte> _, uint size, uint count)
    {
        if (iohandler.stream is not FILENULL ResData)
            return 0;

        var len = size * count;
        ResData.Pointer += len;
        return count;
    }

    [DebuggerStepThrough]
    private static bool NULLSeek(IOHandler iohandler, uint offset)
    {
        if (iohandler.stream is not FILENULL ResData)
            return false;

        ResData.Pointer = offset;
        return true;
    }

    [DebuggerStepThrough]
    private static uint NULLTell(IOHandler iohandler)
    {
        if (iohandler.stream is not FILENULL ResData)
            return uint.MaxValue;

        return ResData.Pointer;
    }

    [DebuggerStepThrough]
    private static bool NULLWrite(IOHandler iohandler, uint size, ReadOnlySpan<byte> _)
    {
        if (iohandler.stream is not FILENULL ResData)
            return false;

        ResData.Pointer += size;
        if (ResData.Pointer > iohandler.UsedSpace)
            iohandler.UsedSpace = ResData.Pointer;

        return true;
    }

    [DebuggerStepThrough]
    private static bool NULLClose(IOHandler iohandler)
    {
        if (iohandler.stream is not FILENULL ResData)
            return false;

        //_cmsFree(iohandler.ContextID, ResData);
        //_cmsFree(iohandler.ContextID, iohandler);
        return true;
    }

    [DebuggerStepThrough]
    public static IOHandler? cmsOpenIOhandlerFromNULL(Context? ContextID)
    {
        var iohandler = new IOHandler();
        if (iohandler is null) return null;

        var fm = new FILENULL { Pointer = 0 };
        //if (fm is null) return null;

        iohandler.ContextID = ContextID;
        iohandler.stream = fm;
        iohandler.UsedSpace = 0;
        iohandler.reportedSize = 0;
        iohandler.physicalFile = String.Empty;

        iohandler.Read = NULLRead;
        iohandler.Seek = NULLSeek;
        iohandler.Close = NULLClose;
        iohandler.Tell = NULLTell;
        iohandler.Write = NULLWrite;

        return iohandler;
    }

    [DebuggerStepThrough]
    private static uint MemoryRead(IOHandler iohandler, Span<byte> Buffer, uint size, uint count)
    {
        if (iohandler.stream is not FILEMEM ResData)
            return 0;

        var len = size * count;

        if (ResData.Pointer + len > ResData.Size)
        {
            len = ResData.Size - ResData.Pointer;
            cmsSignalError(iohandler.ContextID, ErrorCode.Read, $"Read from memory error. Got {len} bytes, block should be of {count * size} bytes");
            return 0;
        }

        var Ptr = ResData.Block.Span[(int)ResData.Pointer..][..(int)len];
        Ptr.CopyTo(Buffer[..(int)len]);
        //memmove(Buffer, Ptr, len);
        ResData.Pointer += len;

        return count;
    }

    [DebuggerStepThrough]
    private static bool MemorySeek(IOHandler iohandler, uint offset)
    {
        if (iohandler.stream is not FILEMEM ResData)
            return false;

        if (offset > ResData.Size)
        {
            cmsSignalError(iohandler.ContextID, ErrorCode.Seek, "Too few data; probably corrupted profile");
            return false;
        }

        ResData.Pointer = offset;
        return true;
    }

    [DebuggerStepThrough]
    private static uint MemoryTell(IOHandler iohandler)
    {
        if (iohandler.stream is not FILEMEM ResData)
            return 0;

        return ResData.Pointer;
    }

    [DebuggerStepThrough]
    private static bool MemoryWrite(IOHandler iohandler, uint size, ReadOnlySpan<byte> Ptr)
    {
        if (iohandler.stream is not FILEMEM ResData)
            return false;

        // Check for available space. Clip.
        if (ResData.Pointer + size > ResData.Size)
            size = ResData.Size - ResData.Pointer;

        if (size is 0) return true;     // Write zero bytes is ok, but does nothing

        Ptr[..(int)size].CopyTo(ResData.Block.Span[(int)ResData.Pointer..]);
        //memmove(ResData.Block + ResData.Pointer, Ptr, size);
        ResData.Pointer += size;

        if (ResData.Pointer > iohandler.UsedSpace)
            iohandler.UsedSpace = ResData.Pointer;

        return true;
    }

    [DebuggerStepThrough]
    private static bool MemoryClose(IOHandler iohandler)
    {
        if (iohandler.stream is not FILEMEM ResData)
            return false;

        if (ResData.FreeBlockOnClose && ResData.Array is not null) ReturnArray(iohandler.ContextID, ResData.Array);

        //_cmsFree(iohandler.ContextID, ResData);
        //_cmsFree(iohandler.ContextID, iohandler);

        return true;
    }

    [DebuggerStepThrough]
    public static IOHandler? cmsOpenIOhandlerFromMem(Context? ContextID, Memory<byte> Buffer, uint size, string AccessMode)
    {
        FILEMEM? fm = null;

        _cmsAssert(AccessMode);

        var iohandler = new IOHandler();
        if (iohandler is null) return null;

        switch (AccessMode)
        {
            case "r":
                //fm = _cmsMallocZero<FILEMEM>(ContextID);
                //if (fm is null) goto Error;

                if (Buffer.IsEmpty)
                {
                    cmsSignalError(ContextID, ErrorCode.Read, "Couldn't read profile from NULL pointer");
                    goto Error;
                }

                //fm->Block = _cmsMalloc<byte>(ContextID, size);
                //if (fm->Block is null)
                //{
                //    cmsSignalError(ContextID, ErrorCode.Read, $"Couldn't allocate {size} bytes for profile");
                //    goto Error;
                //}

                //memmove(fm->Block, Buffer, size);
                //fm->FreeBlockOnClose = true;
                //fm->Size = size;
                //fm->Pointer = 0;

                fm = new FILEMEM(_cmsGetContext(ContextID).GetBufferPool<byte>().Rent((int)size), size, 0);
                (Buffer.Span[..(int)size]).CopyTo(fm.Block.Span);
                iohandler.reportedSize = size;

                break;

            case "w":
                //fm = _cmsMallocZero<FILEMEM>(ContextID);
                //if (fm is null) goto Error;

                //fm->Block = (byte*)Buffer;
                //fm->FreeBlockOnClose = false;
                //fm->Size = size;
                //fm->Pointer = 0;

                fm = new FILEMEM(Buffer, size, 0);
                iohandler.reportedSize = 0;

                break;

            default:
                cmsSignalError(ContextID, ErrorCode.UnknownExtension, $"Unknown access mode '{AccessMode}'");
                return null;
        }

        iohandler.ContextID = ContextID;
        iohandler.stream = fm;
        iohandler.UsedSpace = 0;

        iohandler.Read = MemoryRead;
        iohandler.Seek = MemorySeek;
        iohandler.Close = MemoryClose;
        iohandler.Tell = MemoryTell;
        iohandler.Write = MemoryWrite;

        return iohandler;

    Error:
        //if (fm is not null) _cmsFree(ContextID, fm);
        //if (iohandler is not null) _cmsFree(ContextID, iohandler);
        return null;
    }

    [DebuggerStepThrough]
    private static uint FileRead(IOHandler iohandler, Span<byte> Buffer, uint size, uint count)
    {
        if (iohandler.stream is not FILE file)
            return 0;

        var nReaded = (uint)fread(Buffer, size, count, file);

        if (nReaded != count)
        {
            cmsSignalError(iohandler.ContextID, ErrorCode.File, $"Read error. Got {nReaded * size} bytes, block should be of {count * size} bytes");
            return 0;
        }

        return nReaded;
    }

    [DebuggerStepThrough]
    private static bool FileSeek(IOHandler iohandler, uint offset)
    {
        if (iohandler.stream is not FILE file)
            return false;

        if (fseek(file, offset, SEEK_SET) is not 0)
        {
            cmsSignalError(iohandler.ContextID, ErrorCode.File, "Seek error; probably corrupted file");
            return false;
        }

        return true;
    }

    [DebuggerStepThrough]
    private static uint FileTell(IOHandler iohandler)
    {
        if (iohandler.stream is not FILE file)
            return uint.MaxValue;

        var t = ftell(file);
        if (t is -1)
        {
            cmsSignalError(iohandler.ContextID, ErrorCode.File, "Tell error; probably corrupted file");
            return 0;
        }

        return (uint)t;
    }

    [DebuggerStepThrough]
    private static bool FileWrite(IOHandler iohandler, uint size, ReadOnlySpan<byte> Buffer)
    {
        if (size is 0) return true;     // We allow to write 0 bytes, but nothing is written

        iohandler.UsedSpace += size;
        
        if (iohandler.stream is not FILE file)
            return false;

        return fwrite(Buffer, size, 1, file) is 1;
    }

    [DebuggerStepThrough]
    private static bool FileClose(IOHandler iohandler)
    {
        //_cmsFree(iohandler.ContextID, iohandler);
        return iohandler.stream is FILE file && fclose(file) is 0;
    }

    [DebuggerStepThrough]
    public static IOHandler? cmsOpenIOhandlerFromFile(Context? ContextID, string FileName, string AccessMode)
    {
        FILE? fm = null;
        int fileLen;

        _cmsAssert((object)FileName);
        _cmsAssert(AccessMode);

        var iohandler = new IOHandler();
        if (iohandler is null) return null;

        switch (AccessMode[0])
        {
            case 'r':
                fm = fopen(FileName, "rb");
                if (fm is null)
                {
                    //_cmsFree(ContextID, iohandler);
                    cmsSignalError(ContextID, ErrorCode.File, $"File '{FileName}' not found");
                    return null;
                }
                fileLen = (int)cmsfilelength(fm);
                if (fileLen < 0)
                {
                    fclose(fm);
                    //_cmsFree(ContextID, iohandler);
                    cmsSignalError(ContextID, ErrorCode.File, $"Cannot get size of file '{FileName}'");
                    return null;
                }

                iohandler.reportedSize = (uint)fileLen;
                break;

            case 'w':
                fm = fopen(FileName, "wb");
                if (fm is null)
                {
                    //_cmsFree(ContextID, iohandler);
                    cmsSignalError(ContextID, ErrorCode.File, $"Couldn't create '{FileName}'");
                    return null;
                }

                iohandler.reportedSize = 0;
                break;

            default:
                //_cmsFree(ContextID, iohandler);
                cmsSignalError(ContextID, ErrorCode.File, $"Unknown access mode '{AccessMode}'");
                return null;
        }

        iohandler.ContextID = ContextID;
        iohandler.stream = fm;
        iohandler.UsedSpace = 0;

        // Keep track of the original file
        iohandler.physicalFile = FileName;

        iohandler.Read = FileRead;
        iohandler.Seek = FileSeek;
        iohandler.Close = FileClose;
        iohandler.Tell = FileTell;
        iohandler.Write = FileWrite;

        return iohandler;
    }

    [DebuggerStepThrough]
    public static IOHandler? cmsOpenIOhandlerFromStream(Context? ContextID, Stream Stream)
    {
        IOHandler? iohandler = null;

        var file = new FILE(Stream);
        //file.Stream = Stream;

        var fileSize = cmsfilelength(file);
        if (fileSize < 0)
        {
            cmsSignalError(ContextID, ErrorCode.File, "Cannot get size of stream");
            return null;
            //goto Error;
        }

        iohandler = new IOHandler
        {
            ContextID = ContextID,
            stream = file,
            UsedSpace = 0,
            reportedSize = (uint)fileSize,
            physicalFile = String.Empty,

            Read = FileRead,
            Seek = FileSeek,
            Close = FileClose,
            Tell = FileTell,
            Write = FileWrite
        };
        //if (iohandler is null) goto Error;

        return iohandler;
        //Error:
        //if (file is not null) free(file);
        //if (iohandler is not null) _cmsFree(ContextID, iohandler);
        //return null;
    }

    [DebuggerStepThrough]
    public static bool cmsCloseIOhandler(IOHandler io) =>
        io.Close(io);

    [DebuggerStepThrough]
    public static IOHandler? cmsGetProfileIOhandler(Profile? Icc) =>
        Icc?.IOHandler;

    [DebuggerStepThrough]
    public static Profile? cmsCreateProfilePlaceholder(Context? ContextID)
    {
        var Icc = new Profile();
        if (Icc is null) return null;

        Icc.ContextID = ContextID;

        // Set it to empty
        //Icc.TagCount = 0;

        // Set default version
        Icc.Version = 0x02100000;

        // Set creation date/time
        if (!_cmsGetTime(out Icc.Created))
            return null;

        // Create a mutex if the user provided proper plugin. NULL otherwise
        Icc.UserMutex = _cmsCreateMutex(ContextID);

        // Return the handle
        return Icc;
    //Error:
    //    _cmsFree(ContextID, Icc);
    //    return null;
    }

    [DebuggerStepThrough]
    public static Context? cmsGetProfileContextID(Profile? Icc) =>
        Icc?.ContextID;

    [DebuggerStepThrough]
    public static int cmsGetTagCount(Profile? Icc) =>
        Icc?.Tags.Count ?? -1;

    [DebuggerStepThrough]
    public static Signature cmsGetTagSignature(Profile Profile, uint n)
    {
        var Icc = Profile;

        if (n > Icc.Tags.Count || n >= MAX_TABLE_TAG)
            return default;

        return Icc.Tags[(int)n].Name;
    }

    [DebuggerStepThrough]
    private static int SearchOneTag(Profile Profile, Signature sig)
    {
        var i = -1;
        foreach (var tag in Profile.Tags)
        {
            i++;
            if (sig == tag.Name)
                return i;
        }

        return -1;
    }

    [DebuggerStepThrough]
    internal static int _cmsSearchTag(Profile Icc, Signature sig, bool lFollowLinks)
    {
        int n;
        Signature LinkedSig;
        do
        {
            // Search for given tag in ICC profile directory
            n = SearchOneTag(Icc, sig);
            if (n < 0)
                return -1;          // Not found

            if (!lFollowLinks)
                return n;           // Found, don't follow links

            // Is this a linked tag?
            LinkedSig = Icc.Tags[n].Linked;

            // Yes, follow links
            if (LinkedSig != default)
                sig = LinkedSig;
        } while (LinkedSig != default);

        return n;
    }

    [DebuggerStepThrough]
    internal static void _cmsDeleteTagByPos(Profile Icc, int i)
    {
        _cmsAssert(Icc);
        _cmsAssert(i >= 0);

        var tag = Icc.Tags[i];
        if (tag.TagObject is not null)
        {
            // Free previous version
            if (tag.SaveAsRaw)
            {
                //_cmsFree(Icc.ContextID, Icc.TagPtrs[i]);
            }
            else
            {
                var TypeHandler = tag.TypeHandler;

                if (TypeHandler is not null)
                {
                    var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();
                    LocalTypeHandler.ContextID = Icc.ContextID;    // As an aditional parameter
                    LocalTypeHandler.ICCVersion = Icc.Version;
                    LocalTypeHandler.FreePtr(LocalTypeHandler, tag.TagObject);
                    tag.TagObject = null;
                }
            }
            Icc.Tags[i] = tag;
        }
    }

    [DebuggerStepThrough]
    internal static bool _cmsNewTag(Profile Icc, Signature sig, out int NewPos)
    {
        // Search for the tag
        var i = _cmsSearchTag(Icc, sig, false);
        if (i >= 0)
        {
            // Already exists? delete it
            _cmsDeleteTagByPos(Icc, i);
            NewPos = i;
        }
        else
        {
            // No, make a new one
            if (Icc.Tags.Count >= MAX_TABLE_TAG)
            {
                cmsSignalError(Icc.ContextID, ErrorCode.Range, $"Too many tags ({MAX_TABLE_TAG})");
                NewPos = -1;
                return false;
            }

            NewPos = Icc.Tags.Count;
            //Icc.TagCount++;
            Icc.Tags.Add(new());
        }

        return true;
    }

    [DebuggerStepThrough]
    public static bool cmsIsTag(Profile Icc, Signature sig) =>
        _cmsSearchTag(Icc, sig, false) >= 0;

    [DebuggerStepThrough]
    private static uint _validatedVersion(uint DWord)
    {
        Span<byte> pByte = stackalloc byte[4];
        BitConverter.TryWriteBytes(pByte, DWord);

        if (pByte[0] > 0x09) pByte[0] = 0x09;
        var temp1 = (byte)(pByte[1] & 0xF0);
        var temp2 = (byte)(pByte[1] & 0x0F);
        if (temp1 > 0x90) temp1 = 0x90;
        if (temp2 > 0x09) temp2 = 0x09;
        pByte[1] = (byte)(temp1 | temp2);
        pByte[2] = 0;
        pByte[3] = 0;

        return BitConverter.ToUInt32(pByte);
    }

    [DebuggerStepThrough]
    internal static bool _cmsReadHeader(Profile Icc)
    {
        Span<byte> buffer = stackalloc byte[128];

        Profile.Header Header;
        TagEntry Tag;
        uint TagCount;

        var io = Icc.IOHandler;

        if (io is null) return false;

        // Read the header
        if (io.Read(io, buffer, (uint)buffer.Length, 1) is not 1)
            return false;

        Header = MemoryMarshal.Read<Profile.Header>(buffer);

        // Validate file as an ICC profile
        if (_cmsAdjustEndianess32(Header.magic) != cmsMagicNumber)
        {
            cmsSignalError(Icc.ContextID, ErrorCode.BadSignature, "not an Icc profile, invalid signature");
            return false;
        }

        // Adjust endianness of the used parameters
        Icc.DeviceClass = new(_cmsAdjustEndianess32(Header.deviceClass));
        Icc.ColorSpace = new(_cmsAdjustEndianess32(Header.colorSpace));
        Icc.PCS = new(_cmsAdjustEndianess32(Header.pcs));

        Icc.RenderingIntent = _cmsAdjustEndianess32(Header.renderingIntent);
        Icc.flags = _cmsAdjustEndianess32(_cmsAdjustEndianess32(Header.flags)); ;
        Icc.manufacturer = _cmsAdjustEndianess32(Header.manufacturer);
        Icc.model = _cmsAdjustEndianess32(Header.model);
        Icc.creator = _cmsAdjustEndianess32(Header.creator);

        Icc.attributes = _cmsAdjustEndianess64(Header.attributes);
        Icc.Version = _cmsAdjustEndianess32(_validatedVersion(Header.version));

        // Get size as reported in header
        var HeaderSize = _cmsAdjustEndianess32(Header.size);

        // Make sure HeaderSize is lower than profile size
        if (HeaderSize >= io.reportedSize)
            HeaderSize = io.reportedSize;

        // Get creation date/time
        _cmsDecodeDateTimeNumber(Header.date, out Icc.Created);

        // The profile ID are 32 raw bytes
        Icc.ProfileID = Header.profileID;

        // Read tag directory
        if (!_cmsReadUInt32Number(io, out TagCount)) return false;
        if (TagCount > MAX_TABLE_TAG)
        {
            cmsSignalError(Icc.ContextID, ErrorCode.Range, $"Too many tags({TagCount})");
            return false;
        }

        // Read tag directory
        //Icc.TagCount = 0;
        for (var i = 0; i < TagCount; i++)
        {
            if (!_cmsReadUInt32Number(io, out var sig)) return false;
            Tag.sig = sig;
            if (!_cmsReadUInt32Number(io, out Tag.offset)) return false;
            if (!_cmsReadUInt32Number(io, out Tag.size)) return false;

            // Perform some sanity check. Offset + size should fall inside file.
            if (Tag.offset + Tag.size > HeaderSize ||
                Tag.offset + Tag.size < Tag.offset)
            {
                continue;
            }

            //Icc.TagNames[Icc.TagCount] = Tag.sig;
            //Icc.TagOffsets[Icc.TagCount] = Tag.offset;
            //Icc.TagSizes[Icc.TagCount] = Tag.size;

            var name = Tag.sig;
            var offset = Tag.offset;
            var size = Tag.size;

            // Search for links
            Signature linked = default;
            for (var j = 0; j < Icc.Tags.Count; j++)
            {
                if ((Icc.Tags[j].Offset == Tag.offset) &&
                    (Icc.Tags[j].Size == Tag.size))
                {
                    linked = Icc.Tags[j].Name;
                }
            }

            //Icc.TagCount++;
            Icc.Tags.Add(new()
            {
                Name = name,
                Offset = offset,
                Size = size,
                Linked = linked
            });
        }

        return true;
    }

    [DebuggerStepThrough]
    internal static bool _cmsWriteHeader(Profile Icc, uint UsedSpace)
    {
        Span<byte> headerBuffer = stackalloc byte[128];
        Span<byte> tagBuffer = stackalloc byte[12];

        Profile.Header Header = new();
        TagEntry Tag = new();

        Header.size = _cmsAdjustEndianess32(UsedSpace);
        Header.cmmId = new(_cmsAdjustEndianess32(lcmsSignature));
        Header.version = _cmsAdjustEndianess32(Icc.Version);

        Header.deviceClass = new(_cmsAdjustEndianess32(Icc.DeviceClass));
        Header.colorSpace = new(_cmsAdjustEndianess32(Icc.ColorSpace));
        Header.pcs = new(_cmsAdjustEndianess32(Icc.PCS));

        // NOTE: in v4 Timestamp must be in UTC rather than in local time
        _cmsEncodeDateTimeNumber(out Header.date, Icc.Created);

        Header.magic = new(_cmsAdjustEndianess32(cmsMagicNumber));
        Header.manufacturer = new(_cmsAdjustEndianess32(Environment.OSVersion.Platform is PlatformID.Win32NT ? cmsSigMicrosoft : cmsSigMacintosh));

        Header.flags = _cmsAdjustEndianess32(Icc.flags);
        Header.manufacturer = new(_cmsAdjustEndianess32(Icc.manufacturer));
        Header.model = _cmsAdjustEndianess32(Icc.model);

        Header.attributes = _cmsAdjustEndianess64(Icc.attributes);

        // Rendering intent in the header (for embedded profiles)
        Header.renderingIntent = _cmsAdjustEndianess32(Icc.RenderingIntent);

        // Illuminant is always D50
        Header.illuminant.X = (int)_cmsAdjustEndianess32((uint)_cmsDoubleTo15Fixed16(D50XYZ.X));
        Header.illuminant.Y = (int)_cmsAdjustEndianess32((uint)_cmsDoubleTo15Fixed16(D50XYZ.Y));
        Header.illuminant.Z = (int)_cmsAdjustEndianess32((uint)_cmsDoubleTo15Fixed16(D50XYZ.Z));

        // Created by LittleCMS (that's me!)
        Header.creator = new(_cmsAdjustEndianess32(lcmsSignature));

        //memset(&Header.reserved, 0, 28);

        // Set profile ID. Endianness is always big endian
        Header.profileID = Icc.ProfileID;

        // Dump the header
        MemoryMarshal.Write(headerBuffer, ref Header);
        if (Icc.IOHandler?.Write(Icc.IOHandler, (uint)headerBuffer.Length, headerBuffer) != true)
            return false;

        // Saves Tag directory

        // Get true count
        var Count = 0u;
        for (var i = 0; i < Icc.Tags.Count; i++)
        {
            if (Icc.Tags[i].Name != default)
                Count++;
        }

        // Store number of tags
        if (!_cmsWriteUInt32Number(Icc.IOHandler, Count)) return false;

        for (var i = 0; i < Icc.Tags.Count; i++)
        {
            var t = Icc.Tags[i];
            if (t.Name == default) continue;      // It is just a placeholder

            Tag.sig = new(_cmsAdjustEndianess32(t.Name));
            Tag.offset = _cmsAdjustEndianess32(t.Offset);
            Tag.size = _cmsAdjustEndianess32(t.Size);

            MemoryMarshal.Write(tagBuffer, ref Tag);

            if (!Icc.IOHandler.Write(Icc.IOHandler, (uint)tagBuffer.Length, tagBuffer)) return false;
        }

        return true;
    }

    [DebuggerStepThrough]
    public static uint cmsGetHeaderRenderingIntent(Profile Icc) =>
        Icc.RenderingIntent;

    [DebuggerStepThrough]
    public static void cmsSetHeaderRenderingIntent(Profile Icc, uint RenderingIntent) =>
        Icc.RenderingIntent = RenderingIntent;

    [DebuggerStepThrough]
    public static uint cmsGetHeaderFlags(Profile Icc) =>
        Icc.flags;

    [DebuggerStepThrough]
    public static void cmsSetHeaderFlags(Profile Icc, uint Flags) =>
        Icc.flags = Flags;

    [DebuggerStepThrough]
    public static uint cmsGetHeaderManufacturer(Profile Icc) =>
        Icc.manufacturer;

    [DebuggerStepThrough]
    public static void cmsSetHeaderManufacturer(Profile Icc, uint manufacturer) =>
        Icc.manufacturer = manufacturer;

    [DebuggerStepThrough]
    public static uint cmsGetHeaderCreator(Profile Icc) =>
        Icc.creator;

    [DebuggerStepThrough]
    public static uint cmsGetHeaderModel(Profile Icc) =>
        Icc.model;

    [DebuggerStepThrough]
    public static void cmsSetHeaderModel(Profile Icc, uint model) =>
        Icc.model = model;

    [DebuggerStepThrough]
    public static ulong cmsGetHeaderAttributes(Profile Icc) =>
        Icc.attributes;

    [DebuggerStepThrough]
    public static void cmsSetHeaderAttributes(Profile Icc, ulong Flags) =>
        Icc.attributes = Flags;

    [DebuggerStepThrough]
    public static void cmsGetHeaderProfileID(Profile Icc, Span<byte> ProfileID) =>
        MemoryMarshal.Write(ProfileID, ref Icc.ProfileID);

    [DebuggerStepThrough]
    public static void cmsSetHeaderProfileID(Profile Icc, ReadOnlySpan<byte> ProfileID) =>
        Icc.ProfileID = MemoryMarshal.Read<ProfileID>(ProfileID);

    [DebuggerStepThrough]
    public static bool cmsGetHeaderCreationDateTime(Profile Icc, out DateTime Dest)
    {
        Dest = Icc.Created;
        return true;
    }

    [DebuggerStepThrough]
    public static Signature cmsGetPCS(Profile Icc) =>
        Icc.PCS;

    [DebuggerStepThrough]
    public static void cmsSetPCS(Profile Icc, Signature pcs) =>
        Icc.PCS = pcs;

    [DebuggerStepThrough]
    public static Signature cmsGetColorSpace(Profile Icc) =>
        Icc.ColorSpace;

    [DebuggerStepThrough]
    public static void cmsSetColorSpace(Profile Icc, Signature sig) =>
        Icc.ColorSpace = sig;

    [DebuggerStepThrough]
    public static Signature cmsGetDeviceClass(Profile Icc) =>
        Icc.DeviceClass;

    [DebuggerStepThrough]
    public static void cmsSetDeviceClass(Profile Icc, Signature sig) =>
        Icc.DeviceClass = sig;

    [DebuggerStepThrough]
    public static uint cmsGetEncodedICCVersion(Profile Icc) =>
        Icc.Version;

    [DebuggerStepThrough]
    public static void cmsSetEncodedICCVersion(Profile Icc, uint Version) =>
        Icc.Version = Version;

    [DebuggerStepThrough]
    private static uint BaseToBase(uint @in, int BaseIn, int BaseOut)
    {
        Span<byte> Buff = stackalloc byte[100];
        var @out = 0u;
        int len = 0, i;

        for (; @in > 0 && len < 100; len++)
        {
            Buff[len] = (byte)(@in % BaseIn);
            @in = (uint)(@in / BaseIn);
        }

        for (i = len - 1, @out = 0; i >= 0; --i)
        {
            @out = (uint)((@out * BaseOut) + Buff[i]);
        }

        return @out;
    }

    [DebuggerStepThrough]
    public static void cmsSetProfileVersion(Profile Icc, double Version) =>
        // 4.2 -> 0x04200000
        Icc.Version = BaseToBase((uint)Math.Floor((Version * 100.0) + 0.5), 10, 16) << 16;

    [DebuggerStepThrough]
    public static double cmsGetProfileVersion(Profile Icc) =>
        // 0x04200000 -> 4.2
        BaseToBase(Icc.Version >> 16, 16, 10) / 100.0;

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromIOHandlerTHR(Context? ContextID, IOHandler io)
    {
        var hEmpty = cmsCreateProfilePlaceholder(ContextID);
        if (hEmpty is null) return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = io;
        if (!_cmsReadHeader(NewIcc)) goto Error;
        return NewIcc;
    Error:
        cmsCloseProfile(NewIcc);
        return null;
    }

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromIOHandler2THR(Context? ContextID, IOHandler io, bool write)
    {
        var hEmpty = cmsCreateProfilePlaceholder(ContextID);
        if (hEmpty is null) return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = io;
        if (write)
        {
            NewIcc.IsWrite = true;
            return NewIcc;
        }

        if (!_cmsReadHeader(NewIcc)) goto Error;
        return NewIcc;
    Error:
        cmsCloseProfile(NewIcc);
        return null;
    }

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromFileTHR(Context? ContextID, string FileName, string Access)
    {
        var hEmpty = cmsCreateProfilePlaceholder(ContextID);
        if (hEmpty is null) return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = cmsOpenIOhandlerFromFile(ContextID, FileName, Access);
        if (NewIcc.IOHandler is null) goto Error;

        if (Access.ToLower().Contains('w'))
        {
            NewIcc.IsWrite = true;
            return NewIcc;
        }

        if (!_cmsReadHeader(NewIcc)) goto Error;
        return NewIcc;

    Error:
        cmsCloseProfile(NewIcc);
        return null;
    }

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromFile(string FileName, string Access) =>
        cmsOpenProfileFromFileTHR(null, FileName, Access);

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromStreamTHR(Context? ContextID, Stream ICCProfile, string Access)
    {
        var hEmpty = cmsCreateProfilePlaceholder(ContextID);
        if (hEmpty is null) return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = cmsOpenIOhandlerFromStream(ContextID, ICCProfile);
        if (NewIcc.IOHandler is null) goto Error;

        if (Access.ToLower().Contains('w'))
        {
            NewIcc.IsWrite = true;
            return NewIcc;
        }

        if (!_cmsReadHeader(NewIcc)) goto Error;
        return NewIcc;

    Error:
        cmsCloseProfile(NewIcc);
        return null;
    }

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromStream(Stream ICCProfile, string Access) =>
        cmsOpenProfileFromStreamTHR(null, ICCProfile, Access);

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromMemTHR(Context? ContextID, Memory<byte> MemPtr, uint Size)
    {
        var hEmpty = cmsCreateProfilePlaceholder(ContextID);
        if (hEmpty is null) return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = cmsOpenIOhandlerFromMem(ContextID, MemPtr, Size, "r");
        if (NewIcc.IOHandler is null) goto Error;

        if (!_cmsReadHeader(NewIcc)) goto Error;
        return NewIcc;

    Error:
        cmsCloseProfile(NewIcc);
        return null;
    }

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromMemTHR(Context? ContextID, Memory<byte> MemPtr) =>
        cmsOpenProfileFromMemTHR(ContextID, MemPtr, (uint)MemPtr.Length);

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromMem(Memory<byte> MemPtr) =>
        cmsOpenProfileFromMemTHR(null, MemPtr, (uint)MemPtr.Length);

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromMem(Memory<byte> MemPtr, uint Size) =>
        cmsOpenProfileFromMemTHR(null, MemPtr, Size);

    private static bool SaveTags(Profile Icc, Profile? FileOrig)
    {
        var pool = Context.GetPool<byte>(Icc.ContextID);

        var io = Icc.IOHandler;
        if (io is null) return false;

        var Version = cmsGetProfileVersion(Icc);

        for (var i = 0; i < Icc.Tags.Count; i++)
        {
            var Tag = Icc.Tags[i];
            if ((uint)Tag.Name is 0) continue;

            // Linked tags are not written
            if ((uint)Tag.Linked is not 0) continue;

            var Begin = Tag.Offset = io.UsedSpace;

            var Data = Tag.TagObject;
            if (Data is null)
            {
                //void* Mem;
                // Reach here if we are copying a tag from a disk-based ICC profile which has not been modified by user.
                // In this case a blind copy of the block data is performed
                if (FileOrig is not null &&
                    Tag.Offset is not 0 &&
                    FileOrig.IOHandler is not null)
                {
                    var TagSize = FileOrig.Tags[i].Size;
                    var TagOffset = FileOrig.Tags[i].Offset;

                    if (!FileOrig.IOHandler.Seek(FileOrig.IOHandler, TagOffset)) return false;

                    //Mem = _cmsMalloc(Icc.ContextID, TagSize, typeof(byte));
                    //if (Mem is null) return false;
                    var Mem = pool.Rent((int)TagSize);

                    if (FileOrig.IOHandler.Read(FileOrig.IOHandler, Mem, TagSize, 1) is not 1) goto Error;
                    if (!io.Write(io, TagSize, Mem)) goto Error;

                    ReturnArray(Icc.ContextID, Mem);

                    Tag.Size = io.UsedSpace - Begin;

                    // Align to 32 bit boundary.
                    if (!_cmsWriteAlignment(io))
                        return false;

                Error:
                    ReturnArray(Icc.ContextID, Mem);
                    return false;
                }

                Icc.Tags[i] = Tag;
                continue;
            }

            // Should this tag be saved as RAW? If so, tagsizes should be specified in advance (no further cooking is done)
            if (Tag.SaveAsRaw && Data is byte[] buffer)
            {
                if (!io.Write(io, Tag.Size, buffer)) return false;
            }
            else
            {
                // Search for support on this tag
                var TagDescriptor = _cmsGetTagDescriptor(Icc.ContextID, Tag.Name);
                if (TagDescriptor is null)
                {
                    Icc.Tags[i] = Tag;
                    continue;        // Unsupported, ignore it
                }

                var Type = (TagDescriptor.DecideType is not null)
                    ? TagDescriptor.DecideType(Version, Data)
                    : TagDescriptor.SupportedTypes[0];

                var TypeHandler = _cmsGetTagTypeHandler(Icc.ContextID, Type);

                if (TypeHandler is null)
                {
                    cmsSignalError(Icc.ContextID, cmsERROR_INTERNAL, $"(Internal) no handler for tag {Tag.Name:x}");
                    Icc.Tags[i] = Tag;
                    continue;
                }

                var TypeBase = TypeHandler.Signature;
                if (!_cmsWriteTypeBase(io, TypeBase))
                    return false;

                var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();
                LocalTypeHandler.ContextID = Icc.ContextID;
                LocalTypeHandler.ICCVersion = Icc.Version;
                if (!LocalTypeHandler.WritePtr(LocalTypeHandler, io, Data, TagDescriptor.ElemCount))
                {
                    cmsSignalError(Icc.ContextID, cmsERROR_WRITE, $"Couldn't write type '{_cmsTagSignature2String(TypeBase)}'");
                    return false;
                }
            }

            Tag.Size = io.UsedSpace - Begin;

            // Align to 32 bit boundary
            if (!_cmsWriteAlignment(io))
                return false;

            Icc.Tags[i] = Tag;
        }

        return true;
    }

    private static bool SetLinks(Profile Icc)
    {
        for (var i = 0; i < Icc.Tags.Count; i++)
        {
            var tag = Icc.Tags[i];
            var lnk = tag.Linked;
            if ((uint)lnk is not 0)
            {
                var j = _cmsSearchTag(Icc, lnk, false);
                if (j >= 0)
                {
                    tag.Offset = Icc.Tags[j].Offset;
                    tag.Size = Icc.Tags[j].Size;

                    Icc.Tags[i] = tag;
                }
            }
        }

        return true;
    }

    public static uint cmsSaveProfileToIOhandler(Profile Profile, IOHandler? io)
    {
        Profile Keep;

        _cmsAssert(Profile);

        Keep = Profile;

        if (!_cmsLockMutex(Keep.ContextID, Keep.UserMutex)) return 0;
        var Icc = (Profile)Keep.Clone();
        //memmove(&Keep, Icc, _sizeof<Profile>());

        var ContextID = cmsGetProfileContextID(Icc);
        var PrevIO = Icc.IOHandler = cmsOpenIOhandlerFromNULL(ContextID);
        if (PrevIO is null)
        {
            _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
            return 0;
        }

        // Pass #1 does computer offsets
        if (!_cmsWriteHeader(Icc, 0)) goto Error;
        if (!SaveTags(Icc, Keep)) goto Error;

        var UsedSpace = PrevIO.UsedSpace;

        // Pass #2 does save to iohandler

        if (io is not null)
        {
            Icc.IOHandler = io;
            if (!SetLinks(Icc)) goto Error;
            if (!_cmsWriteHeader(Icc, UsedSpace)) goto Error;
            if (!SaveTags(Icc, Keep)) goto Error;
        }


        //memmove(Icc, &Keep, _sizeof<Profile>());
        if (!cmsCloseIOhandler(PrevIO))
            UsedSpace = 0; // As an error marker

        _cmsUnlockMutex(Keep.ContextID, Keep.UserMutex);

        return UsedSpace;

    Error:
        cmsCloseIOhandler(PrevIO);
        //memmove(Icc, &Keep, _sizeof<Profile>());
        _cmsUnlockMutex(Keep.ContextID, Keep.UserMutex);

        return 0;
    }

    public static bool cmsSaveProfileToFile(Profile Profile, string FileName)
    {
        var ContextID = cmsGetProfileContextID(Profile);
        var io = cmsOpenIOhandlerFromFile(ContextID, FileName, "w");

        if (io is null) return false;

        var rc = cmsSaveProfileToIOhandler(Profile, io) is not 0;
        rc &= cmsCloseIOhandler(io);

        if (!rc)
        {
            try
            {
                File.Delete(FileName);
            }
            catch { }
        }
        return rc;
    }

    public static bool cmsSaveProfileToStream(Profile Profile, Stream Stream)
    {
        var ContextID = cmsGetProfileContextID(Profile);
        var io = cmsOpenIOhandlerFromStream(ContextID, Stream);

        if (io is null) return false;

        var rc = cmsSaveProfileToIOhandler(Profile, io) is not 0;
        rc &= cmsCloseIOhandler(io);

        return rc;
    }

    public static bool cmsSaveProfileToMem(Profile Profile, Memory<byte> MemPtr, out uint BytesNeeded)
    {
        var ContextID = cmsGetProfileContextID(Profile);

        //_cmsAssert(BytesNeeded);

        // Should we just calculate the needed space?
        if (MemPtr.IsEmpty)
        {
            BytesNeeded = cmsSaveProfileToIOhandler(Profile, null);
            return BytesNeeded is not 0;
        }

        BytesNeeded = (uint)MemPtr.Length;

        // That is a read write operation
        var io = cmsOpenIOhandlerFromMem(ContextID, MemPtr, BytesNeeded, "w");
        if (io is null) return false;

        var rc = cmsSaveProfileToIOhandler(Profile, io) is not 0;
        rc &= cmsCloseIOhandler(io);

        return rc;
    }

    [DebuggerStepThrough]
    private static void freeOneTag(Profile Icc, uint i)
    {
        var tag = Icc.Tags[(int)i];
        if (tag.TagObject is not null)
        {
            var TypeHandler = tag.TypeHandler;

            if (TypeHandler is not null)
            {
                var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();

                LocalTypeHandler.ContextID = Icc.ContextID;
                LocalTypeHandler.ICCVersion = Icc.Version;
                LocalTypeHandler.FreePtr?.Invoke(LocalTypeHandler, tag.TagObject);
            }
            else
            {
                if (tag.TagObject is byte[] ptr)
                    ReturnArray(Icc.ContextID, ptr);
                //_cmsFree(Icc.ContextID, Icc.TagPtrs[i]);
            }
        }
    }

    public static bool cmsCloseProfile(Profile Profile)
    {
        var Icc = Profile;
        var rc = true;

        if (Icc is null) return false;

        // Was open in write mode?
        if (Icc.IsWrite)
        {
            Icc.IsWrite = false;   // Assure no further writing
            if (Icc.IOHandler is null || Icc.IOHandler.physicalFile is null)
                return false;
            rc &= cmsSaveProfileToFile(Icc, Icc.IOHandler.physicalFile!);
        }

        for (var i = 0u; i < Icc.Tags.Count; i++)
            freeOneTag(Icc, i);

        if (Icc.IOHandler is not null)
            rc &= cmsCloseIOhandler(Icc.IOHandler);

        _cmsDestroyMutex(Icc.ContextID, Icc.UserMutex);

        //_cmsFree(Icc.ContextID, Icc);  // Free placeholder memory

        return rc;
    }

    [DebuggerStepThrough]
    private static bool IsTypeSupported(TagDescriptor TagDescriptor, Signature Type)
    {
        var nMaxTypes = TagDescriptor.nSupportedTypes;
        if (nMaxTypes >= MAX_TYPES_IN_LCMS_PLUGIN)
            nMaxTypes = MAX_TYPES_IN_LCMS_PLUGIN;

        for (var i = 0; i < nMaxTypes; i++)
            if (Type == TagDescriptor.SupportedTypes[i]) return true;

        return false;
    }

    public static object? cmsReadTag(Profile Profile, Signature sig)
    {
        TagDescriptor? TagDescriptor;
        Signature BaseType;

        var Icc = Profile;

        if (!_cmsLockMutex(Icc.ContextID, Icc.UserMutex)) return null;

        var n = _cmsSearchTag(Icc, sig, true);
        if (n < 0)
        {
            // Not found, return null
            _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
            return null;
        }

        // If the element is already in memory, return the pointer
        var tag = Icc.Tags[n];
        if (tag.TagObject is not null)
        {
            if (tag.TypeHandler is null) goto Error;

            // Sanity check
            BaseType = tag.TypeHandler.Signature;
            if ((uint)BaseType is 0) goto Error;

            TagDescriptor = _cmsGetTagDescriptor(Icc.ContextID, sig);
            if (TagDescriptor is null) goto Error;

            if (!IsTypeSupported(TagDescriptor, BaseType)) goto Error;

            if (tag.SaveAsRaw) goto Error;   // We don't support read raw tags as cooked

            _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
            return tag.TagObject;
        }

        // We need to read it. Get the offset and size to the file
        var Offset = tag.Offset;
        var TagSize = tag.Size;

        if (TagSize < 8) goto Error;

        var io = Icc.IOHandler;
        // Seek to its location
        if (io?.Seek(io, Offset) != true)
            goto Error;

        // Search for support on this tag
        TagDescriptor = _cmsGetTagDescriptor(Icc.ContextID, sig);
        if (TagDescriptor is null)
        {
            // An unknown element was found.
            cmsSignalError(Icc.ContextID, cmsERROR_UNKNOWN_EXTENSION, $"Unknown tag type '{_cmsTagSignature2String(sig)}' found.");
            goto Error;     // Unsupported
        }

        // if supported, get type and check if in list
        BaseType = _cmsReadTypeBase(io);
        if ((uint)BaseType is 0) goto Error;

        if (!IsTypeSupported(TagDescriptor, BaseType)) goto Error;

        TagSize -= 8;   // Already read by the type base logic

        // Get type handler
        var TypeHandler = _cmsGetTagTypeHandler(Icc.ContextID, BaseType);
        if (TypeHandler is null) goto Error;
        var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();

        // Read the tag
        tag.TypeHandler = TypeHandler;
        LocalTypeHandler.ContextID = Icc.ContextID;
        LocalTypeHandler.ICCVersion = Icc.Version;
        tag.TagObject = LocalTypeHandler.ReadPtr(LocalTypeHandler, io, out var ElemCount, TagSize);

        // The tag type is supported, but something wrong happened and we cannot read the tag.
        // let the user know about this (although it is just a warning)
        if (tag.TagObject is null)
        {
            cmsSignalError(Icc.ContextID, cmsERROR_CORRUPTION_DETECTED, $"Corrupted tag '{_cmsTagSignature2String(sig)}'");
            goto Error2;
        }

        // This is a weird error that may be a symptom of something more serious, the number of
        // stored items is actuall less than the number of required elements.
        if (ElemCount < TagDescriptor.ElemCount)
        {
            cmsSignalError(Icc.ContextID, cmsERROR_CORRUPTION_DETECTED,
                $"'{_cmsTagSignature2String(sig)}' Inconsistent number of items: expected {TagDescriptor.ElemCount}, got {ElemCount}");
            goto Error2;
        }

        // Return the data
        Icc.Tags[n] = tag;
        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        return tag.TagObject;
    Error2:
        if (tag.TagObject is not null)
            LocalTypeHandler.FreePtr(LocalTypeHandler, tag.TagObject);
        tag.TagObject = null;
    Error:

        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        return null;
    }

    [DebuggerStepThrough]
    internal static Signature _cmsGetTagTrueType(Profile Profile, Signature sig)
    {
        var Icc = Profile;

        // Search for given tag in Icc profile directory
        var n = _cmsSearchTag(Icc, sig, true);
        if (n < 0) return 0;                        // Not found, return null

        // Get the handler. The true type is there
        var TypeHandler = Icc.Tags[n].TypeHandler;
        return TypeHandler.Signature;
    }

    public static bool cmsWriteTag(Profile Profile, Signature sig, object? data)
    {
        var Icc = Profile;

        Signature Type;
        int i;
        Profile.TagEntry tag;

        if (!_cmsLockMutex(Icc.ContextID, Icc.UserMutex)) return false;

        // To delete tags
        if (data is null)
        {
            // Delete the tag
            i = _cmsSearchTag(Icc, sig, false);
            tag = Icc.Tags[i];
            if (i >= 0)
            {
                // Use zero as a mark of deleted
                _cmsDeleteTagByPos(Icc, i);
                tag.Name = 0;
                Icc.Tags[i] = tag;
                _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
                return true;
            }
            // Didn't find the tag
            goto Error;
        }

        if (!_cmsNewTag(Icc, sig, out i)) goto Error;
        tag = Icc.Tags[i];

        // This is not raw
        tag.SaveAsRaw = false;

        // This is not a link
        tag.Linked = 0;

        // Get information about the TAG
        var TagDescriptor = _cmsGetTagDescriptor(Icc.ContextID, sig);
        if (TagDescriptor is null)
        {
            cmsSignalError(Icc.ContextID, cmsERROR_UNKNOWN_EXTENSION, $"Unsupported tag '{_cmsTagSignature2String(sig)}'");
            goto Error;
        }

        // Now we need to know which type to use. It depends on the version.
        var Version = cmsGetProfileVersion(Icc);

        if (TagDescriptor.DecideType is not null)
        {
            // Let the tag descriptor to decide the type base on depending on
            // the data. This is useful for example on parametric curves, where
            // curves specified by a table cannot be saved as parametric and needs
            // to be casted to single v2-curves, even on v4 profiles.

            Type = TagDescriptor.DecideType(Version, data);
        }
        else
        {
            Type = TagDescriptor.SupportedTypes[0];
        }

        // Does the tag support this type?
        if (!IsTypeSupported(TagDescriptor, Type))
        {
            cmsSignalError(Icc.ContextID, cmsERROR_UNKNOWN_EXTENSION, $"Unsupported type '{_cmsTagSignature2String(Type)}' for tag '{_cmsTagSignature2String(sig)}'");
            goto Error;
        }

        // Do we have a handler for this type?
        var TypeHandler = _cmsGetTagTypeHandler(Icc.ContextID, Type);
        if (TypeHandler is null)
        {
            cmsSignalError(Icc.ContextID, cmsERROR_UNKNOWN_EXTENSION, $"Unsupported type '{_cmsTagSignature2String(Type)}' for tag '{_cmsTagSignature2String(sig)}'");
            goto Error;     // Should never happen
        }

        // Fill fields on icc structure
        tag.TypeHandler = TypeHandler;
        tag.Name = sig;
        tag.Size = 0;
        tag.Offset = 0;

        var LocalTagTypeHandler = (TagTypeHandler)TypeHandler.Clone();
        LocalTagTypeHandler.ContextID = Icc.ContextID;
        LocalTagTypeHandler.ICCVersion = Icc.Version;
        tag.TagObject = LocalTagTypeHandler.DupPtr(LocalTagTypeHandler, data, TagDescriptor.ElemCount);

        if (tag.TagObject is null)
        {
            cmsSignalError(Icc.ContextID, cmsERROR_UNKNOWN_EXTENSION, $"Malformed struct in type '{_cmsTagSignature2String(Type)}' for tag '{_cmsTagSignature2String(sig)}'");
            goto Error;
        }

        Icc.Tags[i] = tag;
        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        return true;
    Error:
        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        return false;
    }

    public static uint cmsReadRawTag(Profile Profile, Signature sig, Memory<byte> data, uint BufferSize)
    {
        var Icc = Profile;

        if (!_cmsLockMutex(Icc.ContextID, Icc.UserMutex)) return 0;

        // Search for given tag in ICC profile directory
        var i = _cmsSearchTag(Icc, sig, true);
        if (i < 0) goto Error;  // Not found

        // It is already read?
        var tag = Icc.Tags[i];
        if (tag.TagObject is null)
        {
            // Not yet, get original position
            var Offset = tag.Offset;
            var TagSize = tag.Size;

            // read the data directly, don't keep copy
            if (!data.IsEmpty)
            {
                if (BufferSize < TagSize)
                    TagSize = BufferSize;

                if (Icc.IOHandler?.Seek(Icc.IOHandler, Offset) != true) goto Error;
                if (Icc.IOHandler.Read(Icc.IOHandler, data.Span, 1, TagSize) is 0) goto Error;

                _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
                return TagSize;
            }

            _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
            return tag.Size;
        }

        // The data has been already read, or written. But wait! maybe the user chose to save as
        // raw data. In this case, return the raw data directly
        if (tag.SaveAsRaw)
        {
            if (!data.IsEmpty)
            {
                var TagSize = tag.Size;
                if (BufferSize < TagSize)
                    TagSize = BufferSize;

                ((byte[])tag.TagObject).AsSpan(..(int)TagSize).CopyTo(data.Span);
                //memmove(data, (BoxPtrVoid)Icc.TagPtrs[i], TagSize);

                _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
                return TagSize;
            }

            _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
            return tag.Size;
        }

        // Already read, or previously set by cmsWriteTag(). We need to serialize that
        // data to raw in order to maintain consistency.

        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        var Object = cmsReadTag(Icc, sig);
        if (!_cmsLockMutex(Icc.ContextID, Icc.UserMutex)) return 0;

        if (Object is null) goto Error;

        // Now we need to serialize to a memory block: just use a memory iohandler

        var MemIO = data.IsEmpty
            ? cmsOpenIOhandlerFromNULL(cmsGetProfileContextID(Icc))
            : cmsOpenIOhandlerFromMem(cmsGetProfileContextID(Icc), data, BufferSize, "w");
        if (MemIO is null) goto Error;

        // Obtain type handling for the tag
        var TypeHandler = tag.TypeHandler;
        var TagDescriptor = _cmsGetTagDescriptor(Icc.ContextID, sig);
        if (TagDescriptor is null)
        {
            cmsCloseIOhandler(MemIO);
            goto Error;
        }

        if (TypeHandler is null) goto Error;

        // Serialize
        var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();
        LocalTypeHandler.ContextID = Icc.ContextID;
        LocalTypeHandler.ICCVersion = Icc.Version;

        if (!_cmsWriteTypeBase(MemIO, TypeHandler.Signature))
        {
            cmsCloseIOhandler(MemIO);
            goto Error;
        }

        if (!LocalTypeHandler.WritePtr(LocalTypeHandler, MemIO, Object, TagDescriptor.ElemCount))
        {
            cmsCloseIOhandler(MemIO);
            goto Error;
        }

        // Get Size and close
        var rc = MemIO.Tell(MemIO);
        cmsCloseIOhandler(MemIO);       // Ignore return code this time

        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        return rc;

    Error:
        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        return 0;
    }

    public static bool cmsWriteRawTag(Profile Profile, Signature sig, ReadOnlySpan<byte> data, uint Size)
    {
        var Icc = Profile;

        if (!_cmsLockMutex(Icc.ContextID, Icc.UserMutex)) return false;

        if (!_cmsNewTag(Icc, sig, out var i))
        {
            _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
            return false;
        }

        var tag = Icc.Tags[i];

        // Mark the tag as being written as RAW
        tag.SaveAsRaw = true;
        tag.Name = sig;
        tag.Linked = 0;

        // Keep a copy of the block
        tag.TagObject = _cmsDupMem(Icc.ContextID, data, Size);
        tag.Size = Size;

        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);

        if (tag.TagObject is null)
        {
            //tag.Name = 0;  //Not saving back to Icc.Tags directly up to this point, so don't need to clean up
            return false;
        }

        Icc.Tags[i] = tag;
        return true;
    }

    public static bool cmsLinkTag(Profile Profile, Signature sig, Signature dest)
    {
        var Icc = Profile;

        if (!_cmsLockMutex(Icc.ContextID, Icc.UserMutex)) return false;

        if (!_cmsNewTag(Icc, sig, out var i))
        {
            _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
            return false;
        }

        var tag = Icc.Tags[i];

        // Keep necessary information
        tag.SaveAsRaw = false;
        tag.Name = sig;
        tag.Linked = dest;

        tag.TagObject = null;
        tag.Size = 0;
        tag.Offset = 0;

        Icc.Tags[i] = tag;
        _cmsUnlockMutex(Icc.ContextID, Icc.UserMutex);
        return true;
    }

    [DebuggerStepThrough]
    public static Signature cmsTagLinkedTo(Profile Profile, Signature sig)
    {
        var Icc = Profile;

        // Search for given tag in ICC profile directory
        var i = _cmsSearchTag(Icc, sig, false);
        if (i < 0) return 0;        // Not found, return 0

        return Icc.Tags[i].Linked;
    }
}
