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

using lcms2.io;
using lcms2.pdk;
using lcms2.types;

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
        if (iohandler is null)
            return null;

        var fm = new FILENULL { Pointer = 0 };
        //if (fm is null) return null;

        iohandler.ContextID = ContextID;
        iohandler.stream = fm;
        iohandler.UsedSpace = 0;
        iohandler.reportedSize = 0;
        iohandler.physicalFile = String.Empty;

        iohandler.ReadFunc = NULLRead;
        iohandler.SeekFunc = NULLSeek;
        iohandler.CloseFunc = NULLClose;
        iohandler.TellFunc = NULLTell;
        iohandler.WriteFunc = NULLWrite;

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
            Context.LogError(
                iohandler.ContextID,
                ErrorCodes.Read,
                $"Read from memory error. Got {len} bytes, block should be of {count * size} bytes");
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
            Context.LogError(iohandler.ContextID, ErrorCodes.Seek, "Too few data; probably corrupted profile");
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

        if (size is 0)
            return true;     // Write zero bytes is ok, but does nothing

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

        //if (ResData.FreeBlockOnClose && ResData.Array is not null) ReturnArray(iohandler.ContextID, ResData.Array);

        //_cmsFree(iohandler.ContextID, ResData);
        //_cmsFree(iohandler.ContextID, iohandler);

        return true;
    }

    [DebuggerStepThrough]
    public static IOHandler? cmsOpenIOhandlerFromMem(Context? ContextID,
                                                     Memory<byte> Buffer,
                                                     uint size,
                                                     string AccessMode)
    {
        FILEMEM? fm = null;

        _cmsAssert(AccessMode);

        var iohandler = new IOHandler();
        if (iohandler is null)
            return null;

        switch (AccessMode)
        {
            case "r":
                //fm = _cmsMallocZero<FILEMEM>(ContextID);
                //if (fm is null) goto Error;

                if (Buffer.IsEmpty)
                {
                    Context.LogError(ContextID, ErrorCodes.Read, "Couldn't read profile from NULL pointer");
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

                //fm = new FILEMEM(_cmsGetContext(ContextID).GetBufferPool<byte>().Rent((int)size), size, 0);
                fm = new FILEMEM(new byte[size], size, 0);
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
                Context.LogError(ContextID, ErrorCodes.UnknownExtension, $"Unknown access mode '{AccessMode}'");
                return null;
        }

        iohandler.ContextID = ContextID;
        iohandler.stream = fm;
        iohandler.UsedSpace = 0;

        iohandler.ReadFunc = MemoryRead;
        iohandler.SeekFunc = MemorySeek;
        iohandler.CloseFunc = MemoryClose;
        iohandler.TellFunc = MemoryTell;
        iohandler.WriteFunc = MemoryWrite;

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
            Context.LogError(
                iohandler.ContextID,
                ErrorCodes.File,
                $"Read error. Got {nReaded * size} bytes, block should be of {count * size} bytes");
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
            Context.LogError(iohandler.ContextID, ErrorCodes.File, "Seek error; probably corrupted file");
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
            Context.LogError(iohandler.ContextID, ErrorCodes.File, "Tell error; probably corrupted file");
            return 0;
        }

        return (uint)t;
    }

    [DebuggerStepThrough]
    private static bool FileWrite(IOHandler iohandler, uint size, ReadOnlySpan<byte> Buffer)
    {
        if (size is 0)
            return true;     // We allow to write 0 bytes, but nothing is written

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
        if (iohandler is null)
            return null;

        switch (AccessMode[0])
        {
            case 'r':
                fm = fopen(FileName, "rb");
                if (fm is null)
                {
                    //_cmsFree(ContextID, iohandler);
                    Context.LogError(ContextID, ErrorCodes.File, $"File '{FileName}' not found");
                    return null;
                }

                fileLen = (int)fm.Length();
                if (fileLen < 0)
                {
                    fclose(fm);
                    //_cmsFree(ContextID, iohandler);
                    Context.LogError(ContextID, ErrorCodes.File, $"Cannot get size of file '{FileName}'");
                    return null;
                }

                iohandler.reportedSize = (uint)fileLen;
                break;

            case 'w':
                fm = fopen(FileName, "wb");
                if (fm is null)
                {
                    //_cmsFree(ContextID, iohandler);
                    Context.LogError(ContextID, ErrorCodes.File, $"Couldn't create '{FileName}'");
                    return null;
                }

                iohandler.reportedSize = 0;
                break;

            default:
                //_cmsFree(ContextID, iohandler);
                return null;
        }

        iohandler.ContextID = ContextID;
        iohandler.stream = fm;
        iohandler.UsedSpace = 0;

        // Keep track of the original file
        iohandler.physicalFile = FileName;

        iohandler.ReadFunc = FileRead;
        iohandler.SeekFunc = FileSeek;
        iohandler.CloseFunc = FileClose;
        iohandler.TellFunc = FileTell;
        iohandler.WriteFunc = FileWrite;

        return iohandler;
    }

    [DebuggerStepThrough]
    public static IOHandler? cmsOpenIOhandlerFromStream(Context? ContextID, Stream Stream)
    {
        IOHandler? iohandler = null;

        var file = new FILE(Stream);
        //file.Stream = Stream;

        var fileSize = file.Length();
        if (fileSize < 0)
        {
            Context.LogError(ContextID, ErrorCodes.File, "Cannot get size of stream");
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
            ReadFunc = FileRead,
            SeekFunc = FileSeek,
            CloseFunc = FileClose,
            TellFunc = FileTell,
            WriteFunc = FileWrite
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
        io.CloseFunc(io);

    [DebuggerStepThrough]
    public static IOHandler? cmsGetProfileIOhandler(Profile? Icc) =>
        Icc?.IOHandler;

    [DebuggerStepThrough]
    public static Profile? cmsCreateProfilePlaceholder(Context? ContextID)
    {
        var Icc = new Profile();
        if (Icc is null)
            return null;

        Icc.ContextID = ContextID;

        // Set it to empty
        //Icc.TagCount = 0;

        // Set default version
        Icc.Version = 0x02100000;

        // Set default device class
        Icc.Version = 0x02100000;

        // Set default CMM (that's me!)
        Icc.CMM = Signatures.LcmsSignature;

        // Set default creator
        // Created by LittleCMS (that's me!)
        Icc.creator = Signatures.LcmsSignature;

        // Set default platform
        Icc.platform = Environment.OSVersion.Platform switch
                       {
                           PlatformID.Win32S       => Signatures.Platform.Microsoft,
                           PlatformID.Win32Windows => Signatures.Platform.Microsoft,
                           PlatformID.Win32NT      => Signatures.Platform.Microsoft,
                           PlatformID.WinCE        => Signatures.Platform.Microsoft,
                           PlatformID.Unix         => Signatures.Platform.Unices,
                           PlatformID.MacOSX       => Signatures.Platform.Macintosh,
                           _                       => throw new NotImplementedException(),
                       };

        // Set creation date/time
        Icc.Created = DateTime.UtcNow;

        // Create a mutex if the user provided proper plugin. NULL otherwise
        Icc.UserMutex = Context.Get(ContextID).CreateMutex();

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
                    LocalTypeHandler.FreePtr?.Invoke(LocalTypeHandler, tag.TagObject);
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
                Context.LogError(Icc.ContextID, ErrorCodes.Range, $"Too many tags ({MAX_TABLE_TAG})");
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

    private static bool CompatibleTypes(TagDescriptor? desc1, TagDescriptor? desc2)
    {
        if (desc1 is null || desc2 is null)
            return false;

        if (desc1.SupportedTypes.Length != desc2.SupportedTypes.Length)
            return false;

        if (desc1.ElemCount != desc2.ElemCount)
            return false;

        for (var i = 0; i < desc1.SupportedTypes.Length; i++)
        {
            if (desc1.SupportedTypes[i] != desc2.SupportedTypes[i])
                return false;
        }

        return true;
    }

    [DebuggerStepThrough]
    private static uint _validatedVersion(uint DWord)
    {
        Span<byte> pByte = stackalloc byte[4];
        BitConverter.TryWriteBytes(pByte, DWord);

        if (pByte[0] > 0x09)
            pByte[0] = 0x09;
        var temp1 = (byte)(pByte[1] & 0xF0);
        var temp2 = (byte)(pByte[1] & 0x0F);
        if (temp1 > 0x90)
            temp1 = 0x90;
        if (temp2 > 0x09)
            temp2 = 0x09;
        pByte[1] = (byte)(temp1 | temp2);
        pByte[2] = 0;
        pByte[3] = 0;

        return BitConverter.ToUInt32(pByte);
    }

    private static bool validDeviceClass(Signature cl)
    {
        return cl == default ||
               cl == Signatures.ProfileClass.Input ||
               cl == Signatures.ProfileClass.Display ||
               cl == Signatures.ProfileClass.Output ||
               cl == Signatures.ProfileClass.Link ||
               cl == Signatures.ProfileClass.Abstract ||
               cl == Signatures.ProfileClass.ColorSpace ||
               cl == Signatures.ProfileClass.NamedColor;
    }

    [DebuggerStepThrough]
    internal static bool _cmsReadHeader(Profile Icc)
    {
        Span<byte> buffer = stackalloc byte[128];

        Profile.Header Header;
        TagEntry Tag;
        uint TagCount;

        var io = Icc.IOHandler;

        if (io is null)
            return false;

        // Read the header
        if (io.ReadFunc(io, buffer, (uint)buffer.Length, 1) is not 1)
            return false;

        Header = MemoryMarshal.Read<Profile.Header>(buffer);

        // Validate file as an ICC profile
        if (AdjustEndianness(Header.magic) != Signatures.MagicNumber)
        {
            Context.LogError(Icc.ContextID, ErrorCodes.BadSignature, "not an Icc profile, invalid signature");
            return false;
        }

        // Adjust endianness of the used parameters
        Icc.CMM = AdjustEndianness(Header.cmmId);
        Icc.DeviceClass = AdjustEndianness(Header.deviceClass);
        Icc.ColorSpace = AdjustEndianness(Header.colorSpace);
        Icc.PCS = AdjustEndianness(Header.pcs);

        Icc.RenderingIntent = AdjustEndianness(Header.renderingIntent);
        Icc.platform = AdjustEndianness(Header.platform);
        Icc.flags = AdjustEndianness(AdjustEndianness(Header.flags));
        Icc.manufacturer = AdjustEndianness(Header.manufacturer);
        Icc.model = AdjustEndianness(Header.model);
        Icc.creator = AdjustEndianness(Header.creator);

        Icc.attributes = AdjustEndianness(Header.attributes);
        Icc.Version = AdjustEndianness(_validatedVersion(Header.version));

        if (Icc.Version > 0x5000000)
        {
            Context.LogError(
                Icc.ContextID,
                cmsERROR_UNKNOWN_EXTENSION,
                "Unsupported profile version '0x{0:x}'",
                Icc.Version);
            return false;
        }

        if (!validDeviceClass(Icc.DeviceClass))
        {
            Context.LogError(
                Icc.ContextID,
                cmsERROR_UNKNOWN_EXTENSION,
                "Unsupported device class '0x{0:x}'",
                (uint)Icc.DeviceClass);
            return false;
        }

        // Get size as reported in header
        var HeaderSize = AdjustEndianness(Header.size);

        // Make sure HeaderSize is lower than profile size
        if (HeaderSize >= io.reportedSize)
            HeaderSize = io.reportedSize;

        // Get creation date/time
        Header.date.Decode(out Icc.Created);

        // The profile ID are 32 raw bytes
        Icc.ProfileID = Header.profileID;

        // Read tag directory
        if (!io.ReadUint(out TagCount))
            return false;
        if (TagCount > MAX_TABLE_TAG)
        {
            Context.LogError(Icc.ContextID, ErrorCodes.Range, $"Too many tags({TagCount})");
            return false;
        }

        // Read tag directory
        //Icc.TagCount = 0;
        for (var i = 0; i < TagCount; i++)
        {
            if (!io.ReadUint(out var sig))
                return false;
            Tag.sig = new(sig);
            if (!io.ReadUint(out Tag.offset))
                return false;
            if (!io.ReadUint(out Tag.size))
                return false;

            // Perform some sanity check. Offset + size should fall inside file.
            if (Tag.size is 0 || Tag.offset is 0)
                continue;
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
                    // Check types.
                    if (CompatibleTypes(
                            _cmsGetTagDescriptor(Icc.ContextID, Icc.Tags[j].Name),
                            _cmsGetTagDescriptor(Icc.ContextID, Tag.sig)))
                    {
                        linked = Icc.Tags[j].Name;
                    }
                }
            }

            //Icc.TagCount++;
            Icc.Tags.Add(new() { Name = name, Offset = offset, Size = size, Linked = linked });
        }

        for (var i = 0; i < Icc.Tags.Count; i++)
        {
            for (var j = 0; j < Icc.Tags.Count; j++)
            {
                // Tags cannot be duplicate
                if ((i != j) && (Icc.Tags[i].Name == Icc.Tags[j].Name))
                {
                    Context.LogError(Icc.ContextID, cmsERROR_RANGE, "Duplicate tag found");
                    return false;
                }
            }
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

        Header.size = AdjustEndianness(UsedSpace);
        Header.cmmId = AdjustEndianness(Icc.CMM);
        Header.version = AdjustEndianness(Icc.Version);

        Header.deviceClass = AdjustEndianness(Icc.DeviceClass);
        Header.colorSpace = AdjustEndianness(Icc.ColorSpace);
        Header.pcs = AdjustEndianness(Icc.PCS);

        // NOTE: in v4 Timestamp must be in UTC rather than in local time
        DateTimeNumber.Encode(out Header.date, Icc.Created);

        Header.magic = AdjustEndianness(Signatures.MagicNumber);
        Header.platform = AdjustEndianness(Icc.platform);

        Header.flags = AdjustEndianness(Icc.flags);
        Header.manufacturer = AdjustEndianness(Icc.manufacturer);
        Header.model = AdjustEndianness(Icc.model);

        Header.attributes = AdjustEndianness(Icc.attributes);

        // Rendering intent in the header (for embedded profiles)
        Header.renderingIntent = AdjustEndianness(Icc.RenderingIntent);

        // Illuminant is always D50
        Header.illuminant.X = (int)AdjustEndianness((uint)DoubleToS15Fixed16(CIEXYZ.D50.X));
        Header.illuminant.Y = (int)AdjustEndianness((uint)DoubleToS15Fixed16(CIEXYZ.D50.Y));
        Header.illuminant.Z = (int)AdjustEndianness((uint)DoubleToS15Fixed16(CIEXYZ.D50.Z));

        Header.creator = AdjustEndianness(Icc.creator);

        //memset(&Header.reserved, 0, 28);

        // Set profile ID. Endianness is always big endian
        Header.profileID = Icc.ProfileID;

        // Dump the header
        MemoryMarshal.Write(headerBuffer, in Header);
        if (Icc.IOHandler?.WriteFunc(Icc.IOHandler, (uint)headerBuffer.Length, headerBuffer) != true)
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
        if (!Icc.IOHandler.Write(Count))
            return false;

        for (var i = 0; i < Icc.Tags.Count; i++)
        {
            var t = Icc.Tags[i];
            if (t.Name == default)
                continue;      // It is just a placeholder

            Tag.sig = AdjustEndianness(t.Name);
            Tag.offset = AdjustEndianness(t.Offset);
            Tag.size = AdjustEndianness(t.Size);

            MemoryMarshal.Write(tagBuffer, in Tag);

            if (!Icc.IOHandler.WriteFunc(Icc.IOHandler, (uint)tagBuffer.Length, tagBuffer))
                return false;
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
    public static Signature cmsGetHeaderManufacturer(Profile Icc) =>
        Icc.manufacturer;

    [DebuggerStepThrough]
    public static void cmsSetHeaderManufacturer(Profile Icc, Signature manufacturer) =>
        Icc.manufacturer = manufacturer;

    [DebuggerStepThrough]
    public static Signature cmsGetHeaderCreator(Profile Icc) =>
        Icc.creator;

    [DebuggerStepThrough]
    public static Signature cmsGetHeaderModel(Profile Icc) =>
        Icc.model;

    [DebuggerStepThrough]
    public static void cmsSetHeaderModel(Profile Icc, Signature model) =>
        Icc.model = model;

    [DebuggerStepThrough]
    public static ulong cmsGetHeaderAttributes(Profile Icc) =>
        Icc.attributes;

    [DebuggerStepThrough]
    public static void cmsSetHeaderAttributes(Profile Icc, ulong Flags) =>
        Icc.attributes = Flags;

    [DebuggerStepThrough]
    public static void cmsGetHeaderProfileID(Profile Icc, Span<byte> ProfileID) =>
        MemoryMarshal.Write(ProfileID, in Icc.ProfileID);

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
        if (hEmpty is null)
            return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = io;
        if (!_cmsReadHeader(NewIcc))
            goto Error;
        return NewIcc;
    Error:
        cmsCloseProfile(NewIcc);
        return null;
    }

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromIOHandler2THR(Context? ContextID, IOHandler io, bool write)
    {
        var hEmpty = cmsCreateProfilePlaceholder(ContextID);
        if (hEmpty is null)
            return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = io;
        if (write)
        {
            NewIcc.IsWrite = true;
            return NewIcc;
        }

        if (!_cmsReadHeader(NewIcc))
            goto Error;
        return NewIcc;
    Error:
        cmsCloseProfile(NewIcc);
        return null;
    }

    [DebuggerStepThrough]
    public static Profile? cmsOpenProfileFromFileTHR(Context? ContextID, string FileName, string Access)
    {
        var hEmpty = cmsCreateProfilePlaceholder(ContextID);
        if (hEmpty is null)
            return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = cmsOpenIOhandlerFromFile(ContextID, FileName, Access);
        if (NewIcc.IOHandler is null)
            goto Error;

        if (Access.ToLower().Contains('w'))
        {
            NewIcc.IsWrite = true;
            return NewIcc;
        }

        if (!_cmsReadHeader(NewIcc))
            goto Error;
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
        if (hEmpty is null)
            return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = cmsOpenIOhandlerFromStream(ContextID, ICCProfile);
        if (NewIcc.IOHandler is null)
            goto Error;

        if (Access.ToLower().Contains('w'))
        {
            NewIcc.IsWrite = true;
            return NewIcc;
        }

        if (!_cmsReadHeader(NewIcc))
            goto Error;
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
        if (hEmpty is null)
            return null;

        var NewIcc = hEmpty;

        NewIcc.IOHandler = cmsOpenIOhandlerFromMem(ContextID, MemPtr, Size, "r");
        if (NewIcc.IOHandler is null)
            goto Error;

        if (!_cmsReadHeader(NewIcc))
            goto Error;
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
        //var pool = Context.GetPool<byte>(Icc.ContextID);

        var io = Icc.IOHandler;
        if (io is null)
            return false;

        var Version = cmsGetProfileVersion(Icc);

        for (var i = 0; i < Icc.Tags.Count; i++)
        {
            var Tag = Icc.Tags[i];
            if ((uint)Tag.Name is 0)
                continue;

            // Linked tags are not written
            if ((uint)Tag.Linked is not 0)
                continue;

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

                    if (!FileOrig.IOHandler.SeekFunc(FileOrig.IOHandler, TagOffset))
                        return false;

                    //Mem = _cmsMalloc(Icc.ContextID, TagSize, typeof(byte));
                    //if (Mem is null) return false;
                    //var Mem = pool.Rent((int)TagSize);
                    var Mem = new byte[TagSize];

                    if (FileOrig.IOHandler.ReadFunc(FileOrig.IOHandler, Mem, TagSize, 1) is not 1)
                    {
                        //ReturnArray(Icc.ContextID, Mem);
                        return false;
                    }

                    if (!io.WriteFunc(io, TagSize, Mem))
                    {
                        //ReturnArray(Icc.ContextID, Mem);
                        return false;
                    }

                    //ReturnArray(Icc.ContextID, Mem);

                    Tag.Size = io.UsedSpace - Begin;

                    // Align to 32 bit boundary.
                    if (!io.WriteAlignment())
                        return false;
                }

                Icc.Tags[i] = Tag;
                continue;
            }

            // Should this tag be saved as RAW? If so, tagsizes should be specified in advance (no further cooking is done)
            if (Tag.SaveAsRaw && Data is byte[] buffer)
            {
                if (!io.WriteFunc(io, Tag.Size, buffer))
                    return false;
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
                    Context.LogError(Icc.ContextID, cmsERROR_INTERNAL, $"(Internal) no handler for tag {Tag.Name:x}");
                    Icc.Tags[i] = Tag;
                    continue;
                }

                var TypeBase = TypeHandler.Signature;
                if (!io.WriteTypeBase(TypeBase))
                    return false;

                var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();
                LocalTypeHandler.ContextID = Icc.ContextID;
                LocalTypeHandler.ICCVersion = Icc.Version;
                if (!LocalTypeHandler.WritePtr(LocalTypeHandler, io, Data, TagDescriptor.ElemCount))
                {
                    Context.LogError(Icc.ContextID, cmsERROR_WRITE, $"Couldn't write type '{TypeBase}'");
                    return false;
                }
            }

            Tag.Size = io.UsedSpace - Begin;

            // Align to 32 bit boundary
            if (!io.WriteAlignment())
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

        if (!Context.Get(Keep.ContextID).LockMutex(Keep.UserMutex))
            return 0;
        var Icc = (Profile)Keep.Clone();
        //memmove(&Keep, Icc, _sizeof<Profile>());

        var ContextID = cmsGetProfileContextID(Icc);
        var PrevIO = Icc.IOHandler = cmsOpenIOhandlerFromNULL(ContextID);
        if (PrevIO is null)
        {
            Context.Get(Icc.ContextID).UnlockMutex(Icc.UserMutex);
            return 0;
        }

        // Pass #1 does computer offsets
        if (!_cmsWriteHeader(Icc, 0))
            goto Error;
        if (!SaveTags(Icc, Keep))
            goto Error;

        var UsedSpace = PrevIO.UsedSpace;

        // Pass #2 does save to iohandler

        if (io is not null)
        {
            Icc.IOHandler = io;
            if (!SetLinks(Icc))
                goto Error;
            if (!_cmsWriteHeader(Icc, UsedSpace))
                goto Error;
            if (!SaveTags(Icc, Keep))
                goto Error;
        }

        //memmove(Icc, &Keep, _sizeof<Profile>());
        if (!cmsCloseIOhandler(PrevIO))
            UsedSpace = 0; // As an error marker

        Context.Get(Keep.ContextID).UnlockMutex(Keep.UserMutex);

        return UsedSpace;

    Error:
        cmsCloseIOhandler(PrevIO);
        //memmove(Icc, &Keep, _sizeof<Profile>());
        Context.Get(Keep.ContextID).UnlockMutex(Keep.UserMutex);

        return 0;
    }

    public static bool cmsSaveProfileToFile(Profile Profile, string FileName)
    {
        var ContextID = cmsGetProfileContextID(Profile);
        var io = cmsOpenIOhandlerFromFile(ContextID, FileName, "w");

        if (io is null)
            return false;

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

        if (io is null)
            return false;

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
        if (io is null)
            return false;

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
            //else
            //{
            //    if (tag.TagObject is byte[] ptr)
            //        ReturnArray(Icc.ContextID, ptr);
            //    //_cmsFree(Icc.ContextID, Icc.TagPtrs[i]);
            //}
        }
    }

    public static bool cmsCloseProfile(Profile? Profile)
    {
        var Icc = Profile;
        var rc = true;

        if (Icc is null)
            return false;

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

        Context.Get(Icc.ContextID).DestroyMutex(Icc.UserMutex);

        //_cmsFree(Icc.ContextID, Icc);  // Free placeholder memory

        return rc;
    }

    [DebuggerStepThrough]
    private static bool IsTypeSupported(TagDescriptor TagDescriptor, Signature Type)
    {
        var nMaxTypes = TagDescriptor.SupportedTypes.Length;
        if (nMaxTypes >= Context.MaxTypesInLcmsPlugin)
            nMaxTypes = Context.MaxTypesInLcmsPlugin;

        for (var i = 0; i < nMaxTypes; i++)
            if (Type == TagDescriptor.SupportedTypes[i])
                return true;

        return false;
    }

    public static object? cmsReadTag(Profile Profile, Signature sig)
    {
        TagDescriptor? TagDescriptor;
        Signature BaseType;

        var Icc = Profile;

        var context = Icc.ContextID;
        if (!Context.Get(context).LockMutex(Icc.UserMutex))
            return null;

        var n = _cmsSearchTag(Icc, sig, true);
        if (n < 0)
        {
            // Not found, return null
            Context.Get(context).UnlockMutex(Icc.UserMutex);
            return null;
        }

        // If the element is already in memory, return the pointer
        var tag = Icc.Tags[n];
        if (tag.TagObject is not null)
        {
            if (tag.TypeHandler is null)
                goto Error;

            // Sanity check
            BaseType = tag.TypeHandler.Signature;
            if ((uint)BaseType is 0)
                goto Error;

            TagDescriptor = _cmsGetTagDescriptor(context, sig);
            if (TagDescriptor is null)
                goto Error;

            if (!IsTypeSupported(TagDescriptor, BaseType))
                goto Error;

            if (tag.SaveAsRaw)
                goto Error;   // We don't support read raw tags as cooked

            Context.Get(context).UnlockMutex(Icc.UserMutex);
            return tag.TagObject;
        }

        // We need to read it. Get the offset and size to the file
        var Offset = tag.Offset;
        var TagSize = tag.Size;

        if (TagSize < 8)
            goto Error;

        var io = Icc.IOHandler;

        if (io is null)     // This is a built-in profile that has been manipulated, abort early
        {
            Context.LogError(context, cmsERROR_CORRUPTION_DETECTED, "Corrupted built-in profile");
            goto Error;
        }

        // Seek to its location
        if (io?.SeekFunc(io, Offset) != true)
            goto Error;

        // Search for support on this tag
        TagDescriptor = _cmsGetTagDescriptor(context, sig);
        if (TagDescriptor is null)
        {
            // An unknown element was found.
            Context.LogError(context, cmsERROR_UNKNOWN_EXTENSION, $"Unknown tag type '{sig}' found.");
            goto Error;     // Unsupported
        }

        // if supported, get type and check if in list
        BaseType = io.ReadTypeBase();
        if ((uint)BaseType is 0)
            goto Error;

        if (!IsTypeSupported(TagDescriptor, BaseType))
            goto Error;

        TagSize -= 8;   // Already read by the type base logic

        // Get type handler
        var TypeHandler = _cmsGetTagTypeHandler(context, BaseType);
        if (TypeHandler is null)
            goto Error;
        var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();

        // Read the tag
        tag.TypeHandler = TypeHandler;
        LocalTypeHandler.ContextID = context;
        LocalTypeHandler.ICCVersion = Icc.Version;
        tag.TagObject = LocalTypeHandler.ReadPtr(LocalTypeHandler, io, out var ElemCount, TagSize);

        // The tag type is supported, but something wrong happened and we cannot read the tag.
        // let the user know about this (although it is just a warning)
        if (tag.TagObject is null)
        {
            Context.LogError(context, cmsERROR_CORRUPTION_DETECTED, $"Corrupted tag '{sig}'");
            goto Error2;
        }

        // This is a weird error that may be a symptom of something more serious, the number of
        // stored items is actuall less than the number of required elements.
        if (ElemCount < TagDescriptor.ElemCount)
        {
            Context.LogError(
                context,
                cmsERROR_CORRUPTION_DETECTED,
                $"'{sig}' Inconsistent number of items: expected {TagDescriptor.ElemCount}, got {ElemCount}");
            goto Error2;
        }

        // Return the data
        Icc.Tags[n] = tag;
        Context.Get(context).UnlockMutex(Icc.UserMutex);
        return tag.TagObject;
    Error2:
        if (tag.TagObject is not null)
            LocalTypeHandler.FreePtr(LocalTypeHandler, tag.TagObject);
        tag.TagObject = null;
    Error:

        Context.Get(context).UnlockMutex(Icc.UserMutex);
        return null;
    }

    [DebuggerStepThrough]
    internal static Signature _cmsGetTagTrueType(Profile Profile, Signature sig)
    {
        var Icc = Profile;

        // Search for given tag in Icc profile directory
        var n = _cmsSearchTag(Icc, sig, true);
        if (n < 0)
            return default;                        // Not found, return null

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

        var context = Icc.ContextID;
        if (!Context.Get(context).LockMutex(Icc.UserMutex))
            return false;

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
                tag.Name = default;
                Icc.Tags[i] = tag;
                Context.Get(context).UnlockMutex(Icc.UserMutex);
                return true;
            }

            // Didn't find the tag
            goto Error;
        }

        if (!_cmsNewTag(Icc, sig, out i))
            goto Error;
        tag = Icc.Tags[i];

        // This is not raw
        tag.SaveAsRaw = false;

        // This is not a link
        tag.Linked = default;

        // Get information about the TAG
        var TagDescriptor = _cmsGetTagDescriptor(context, sig);
        if (TagDescriptor is null)
        {
            Context.LogError(context, cmsERROR_UNKNOWN_EXTENSION, $"Unsupported tag '{(sig)}'");
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
            Context.LogError(context, cmsERROR_UNKNOWN_EXTENSION, $"Unsupported type '{(Type)}' for tag '{(sig)}'");
            goto Error;
        }

        // Do we have a handler for this type?
        var TypeHandler = _cmsGetTagTypeHandler(context, Type);
        if (TypeHandler is null)
        {
            Context.LogError(context, cmsERROR_UNKNOWN_EXTENSION, $"Unsupported type '{(Type)}' for tag '{(sig)}'");
            goto Error;     // Should never happen
        }

        // Fill fields on icc structure
        tag.TypeHandler = TypeHandler;
        tag.Name = sig;
        tag.Size = 0;
        tag.Offset = 0;

        var LocalTagTypeHandler = (TagTypeHandler)TypeHandler.Clone();
        LocalTagTypeHandler.ContextID = context;
        LocalTagTypeHandler.ICCVersion = Icc.Version;
        tag.TagObject = LocalTagTypeHandler.DupPtr(LocalTagTypeHandler, data, TagDescriptor.ElemCount);

        if (tag.TagObject is null)
        {
            Context.LogError(
                context,
                cmsERROR_UNKNOWN_EXTENSION,
                $"Malformed struct in type '{(Type)}' for tag '{(sig)}'");
            goto Error;
        }

        Icc.Tags[i] = tag;
        Context.Get(context).UnlockMutex(Icc.UserMutex);
        return true;
    Error:
        Context.Get(context).UnlockMutex(Icc.UserMutex);
        return false;
    }

    public static uint cmsReadRawTag(Profile Profile, Signature sig, Memory<byte> data, uint BufferSize)
    {
        var Icc = Profile;

        // Sanity check
        if (!data.IsEmpty && BufferSize is 0)
            return 0;

        var context = Icc.ContextID;
        if (!Context.Get(context).LockMutex(Icc.UserMutex))
            return 0;

        // Search for given tag in ICC profile directory
        var i = _cmsSearchTag(Icc, sig, true);
        if (i < 0)
            goto Error;  // Not found

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

                if (Icc.IOHandler?.SeekFunc(Icc.IOHandler, Offset) != true)
                    goto Error;
                if (Icc.IOHandler.ReadFunc(Icc.IOHandler, data.Span, 1, TagSize) is 0)
                    goto Error;

                Context.Get(context).UnlockMutex(Icc.UserMutex);
                return TagSize;
            }

            Context.Get(context).UnlockMutex(Icc.UserMutex);
            return tag.Size;
        }

        // The data has been already read, or written. But wait! maybe the user choose to save as
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

                Context.Get(context).UnlockMutex(Icc.UserMutex);
                return TagSize;
            }

            Context.Get(context).UnlockMutex(Icc.UserMutex);
            return tag.Size;
        }

        // Already read, or previously set by cmsWriteTag(). We need to serialize that
        // data to raw to get something that makes sense.

        Context.Get(context).UnlockMutex(Icc.UserMutex);
        var Object = cmsReadTag(Icc, sig);
        if (!Context.Get(context).LockMutex(Icc.UserMutex))
            return 0;

        if (Object is null)
            goto Error;

        // Now we need to serialize to a memory block: just use a memory iohandler

        var MemIO = data.IsEmpty
                        ? cmsOpenIOhandlerFromNULL(cmsGetProfileContextID(Icc))
                        : cmsOpenIOhandlerFromMem(cmsGetProfileContextID(Icc), data, BufferSize, "w");
        if (MemIO is null)
            goto Error;

        // Obtain type handling for the tag
        var TypeHandler = tag.TypeHandler;
        var TagDescriptor = _cmsGetTagDescriptor(context, sig);
        if (TagDescriptor is null)
        {
            cmsCloseIOhandler(MemIO);
            goto Error;
        }

        if (TypeHandler is null)
            goto Error;

        // Serialize
        var LocalTypeHandler = (TagTypeHandler)TypeHandler.Clone();
        LocalTypeHandler.ContextID = context;
        LocalTypeHandler.ICCVersion = Icc.Version;

        if (!MemIO.WriteTypeBase(TypeHandler.Signature))
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
        var rc = MemIO.TellFunc(MemIO);
        cmsCloseIOhandler(MemIO);       // Ignore return code this time

        Context.Get(context).UnlockMutex(Icc.UserMutex);
        return rc;

    Error:
        Context.Get(context).UnlockMutex(Icc.UserMutex);
        return 0;
    }

    public static bool cmsWriteRawTag(Profile Profile, Signature sig, ReadOnlySpan<byte> data, uint Size)
    {
        var Icc = Profile;

        var context = Icc.ContextID;
        if (!Context.Get(context).LockMutex(Icc.UserMutex))
            return false;

        if (!_cmsNewTag(Icc, sig, out var i))
        {
            Context.Get(context).UnlockMutex(Icc.UserMutex);
            return false;
        }

        var tag = Icc.Tags[i];

        // Mark the tag as being written as RAW
        tag.SaveAsRaw = true;
        tag.Name = sig;
        tag.Linked = default;

        // Keep a copy of the block
        tag.TagObject = _cmsDupMem(context, data, Size);
        tag.Size = Size;

        Context.Get(context).UnlockMutex(Icc.UserMutex);

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

        var context = Icc.ContextID;
        if (!Context.Get(context).LockMutex(Icc.UserMutex))
            return false;

        if (!_cmsNewTag(Icc, sig, out var i))
        {
            Context.Get(context).UnlockMutex(Icc.UserMutex);
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
        Context.Get(context).UnlockMutex(Icc.UserMutex);
        return true;
    }

    [DebuggerStepThrough]
    public static Signature cmsTagLinkedTo(Profile Profile, Signature sig)
    {
        var Icc = Profile;

        // Search for given tag in ICC profile directory
        var i = _cmsSearchTag(Icc, sig, false);
        if (i < 0)
            return default;        // Not found, return 0

        return Icc.Tags[i].Linked;
    }
}
