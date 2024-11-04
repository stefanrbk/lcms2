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

using lcms2.state;

using Microsoft.Extensions.Logging;

namespace lcms2;

public static partial class Lcms2
{
    internal const int MaxErrorMessageLen = 1024;
    internal const uint MaxMemoryForAlloc = 1024u * 1024u * 512u;

    public static int cmsstrcasecmp(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
    {
        var index = 0;

        var us1 = s1[index];
        var us2 = s2[index];

        while (Char.ToUpper(us1) == Char.ToUpper(us2))
        {
            us1 = s1[++index];
            us2 = s2[index];

            if (us1 is '\0')
                return 0;
        }

        return Char.ToUpper(us1) - Char.ToUpper(us2);
    }

    public static int cmsstrcasecmp(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
    {
        s1 = TrimBuffer(s1);
        s2 = TrimBuffer(s2);

        var end = cmsmin(s1.Length, s2.Length);

        for (var i = 0; i < end; i++)
        {
            var val = Char.ToUpper((char)s2[i]) - Char.ToUpper((char)s1[i]);

            if (val is not 0)
                return val;
        }

        if (s1.Length > s2.Length)
            return -s1[end];
        if (s1.Length < s2.Length)
            return s2[end];
        return 0;
    }

    [DebuggerStepThrough]
    internal static T[] _cmsRealloc<T>(Context? ContextID, T[] array, uint count) where T : struct
    {
        //var pool = Context.Get(ContextID).GetBufferPool<T>();

        //var newBuffer = pool.Rent((int)count);

        var newBuffer = new T[count];

        array.AsSpan().CopyTo(newBuffer.AsSpan()[..array.Length]);

        //ReturnArray(pool, array);

        return newBuffer;
    }

    //[DebuggerStepThrough]
    //internal static T* _cmsRealloc<T>(Context? ContextID, void* Ptr, uint size) where T : struct =>
    //    (T*)_cmsRealloc(ContextID, Ptr, size);

    //[DebuggerStepThrough]
    //internal static void ReturnArray<T>(ArrayPool<T> pool, T[]? array)
    //{
    //    if (array is not null)
    //    {
    //        if (typeof(T).IsByRef)
    //        {
    //            for (var i = 0; i < array.Length; i++)
    //                array[i] = default!;
    //        }
    //        pool.Return(array);
    //    }
    //}

    //[DebuggerStepThrough]
    //internal static void ReturnArray<T>(Context? ContextID, T[]? array)
    //{
    //    var pool = Context.Get(ContextID).GetBufferPool<T>();
    //    ReturnArray(pool, array);
    //}

    //[DebuggerStepThrough]
    //internal static void _cmsFree(Context? ContextID, void* Ptr)
    //{
    //    if (Ptr is not null)
    //    {
    //        var ptr = Context.Get(ContextID).MemPlugin;
    //        ptr.FreePtr(ContextID, Ptr);
    //    }
    //}


    //[DebuggerStepThrough]
    //internal static void* _cmsDupMem(Context? ContextID, in void* Org, uint size, Type type)
    //{
    //    if (Org is null)
    //        return null;

    //    var ptr = Context.Get(ContextID).MemPlugin;
    //    return ptr.DupPtr(ContextID, Org, size, type);
    //}

    [DebuggerStepThrough]
    internal static T[] _cmsDupMem<T>(Context? ContextID, ReadOnlySpan<T> Org, uint num) where T : struct
    {
        //var pool = Context.Get(ContextID).GetBufferPool<T>();
        //var array = pool.Rent((int)num);
        var array = new T[num];

        Org[..(int)num].CopyTo(array.AsSpan()[..(int)num]);

        return array;
    }

    //[DebuggerStepThrough]
    //internal static T* _cmsDupMem<T>(Context? ContextID, in void* Org, uint num) where T : struct =>
    //    (T*)_cmsDupMem(ContextID, Org, num * _sizeof<T>(), typeof(T));

    //[DebuggerStepThrough]
    //internal static T* _cmsDupMem<T>(Context? ContextID, in void* Org) where T : struct =>
    //    (T*)_cmsDupMem(ContextID, Org, _sizeof<T>(), typeof(T));

    //[DebuggerStepThrough]
    //internal static T** _cmsDupMem2<T>(Context? ContextID, in void* Org, uint num) where T : struct =>
    //    (T**)_cmsDupMem(ContextID, Org, num * _sizeof<nint>(), typeof(T*));
    //[DebuggerStepThrough]
    //internal static T** _cmsDupMem2<T>(Context? ContextID, in void* Org) where T : struct =>
    //    (T**)_cmsDupMem(ContextID, Org, _sizeof<nint>(), typeof(T*));

    //internal static SubAllocator.Chunk? _cmsCreateSubAllocChunk(Context? ContextID, uint Initial)
    //{
    //    // 20K by default
    //    if (Initial is 0)
    //        Initial = 20 * 1024;

    //    // Create the container
    //    var chunk = new SubAllocator.Chunk();
    //    //if (chunk is null) return null;

    //    // Initialize values
    //    chunk.Block = (byte*)_cmsMalloc(ContextID, Initial, typeof(byte));
    //    if (chunk.Block is null)
    //    {
    //        // Something went wrong
    //        //_cmsFree(ContextID, chunk);
    //        return null;
    //    }

    //    chunk.BlockSize = Initial;
    //    chunk.Used = 0;
    //    chunk.next = null;

    //    return chunk;
    //}

    //internal static SubAllocator? _cmsCreateSubAlloc(Context? ContextID, uint Initial)
    //{
    //    // Create the container
    //    var sub = new SubAllocator();
    //    if (sub is null) return null;

    //    sub.ContextID = ContextID;

    //    sub.h = _cmsCreateSubAllocChunk(ContextID, Initial);
    //    if (sub.h is null)
    //    {
    //        //_cmsFree(ContextID, sub);
    //        return null;
    //    }

    //    return sub;
    //}

    //internal static void _cmsSubAllocDestroy(SubAllocator sub)
    //{
    //    SubAllocator.Chunk? n;

    //    for (var chunk = sub.h; chunk is not null; chunk = n)
    //    {
    //        n = chunk.next;
    //        if (chunk.Block is not null) _cmsFree(sub.ContextID, chunk.Block);
    //        //_cmsFree(sub.ContextID, chunk);
    //    }

    //    // Free the header
    //    //_cmsFree(sub.ContextID, sub);
    //}

    //internal static T* _cmsSubAlloc<T>(SubAllocator sub) where T : struct =>
    //    (T*)_cmsSubAlloc(sub, _sizeof<T>());

    //internal static T** _cmsSubAlloc2<T>(SubAllocator sub) =>
    //    (T**)_cmsSubAlloc<nint>(sub);

    //internal static void* _cmsSubAlloc(SubAllocator sub, int size) =>
    //    _cmsSubAlloc(sub, (uint)size);

    //internal static void* _cmsSubAlloc(SubAllocator sub, uint size)
    //{
    //    var Free = sub.h.BlockSize - sub.h.Used;

    //    size = _cmsALIGNMEM(size);

    //    // Check for memory. If there is no room, allocate a new chunk of double memory size.
    //    if (size > Free)
    //    {
    //        var newSize = sub.h.BlockSize * 2;
    //        if (newSize < size) newSize = size;

    //        var chunk = _cmsCreateSubAllocChunk(sub.ContextID, newSize);
    //        if (chunk is null) return null;

    //        // Link list
    //        chunk.next = sub.h;
    //        sub.h = chunk;
    //    }

    //    var ptr = sub.h.Block + sub.h.Used;
    //    sub.h.Used += size;

    //    return ptr;
    //}

    //internal static T* _cmsSubAllocDup<T>(SubAllocator s, in void* ptr) where T : struct =>
    //    (T*)_cmsSubAllocDup(s, ptr, _sizeof<T>());

    //internal static T** _cmsSubAllocDup2<T>(SubAllocator s, in void* ptr) where T : struct =>
    //    (T**)_cmsSubAllocDup<nint>(s, ptr);

    //internal static void* _cmsSubAllocDup(SubAllocator s, in void* ptr, int size) =>
    //    _cmsSubAllocDup(s, ptr, (uint)size);

    //internal static void* _cmsSubAllocDup(SubAllocator s, in void* ptr, uint size)
    //{
    //    // Dup of null pointer is also NULL
    //    if (ptr is null)
    //        return null;

    //    var NewPtr = _cmsSubAlloc(s, size);

    //    if (ptr is not null && NewPtr is not null)
    //    {
    //        memcpy(NewPtr, ptr, size);
    //    }

    //    return NewPtr;
    //}

    private static readonly LogErrorChunkType LogErrorChunk = new();

    /// <summary>
    ///     Global context storage
    /// </summary>
    private static readonly LogErrorChunkType globalLogErrorChunk = new();

    /// <summary>
    ///     "Allocates" and inits error logger container for a given context.
    /// </summary>
    /// <remarks>
    ///     If src is null, only initiallizes to the default.
    ///     Otherwise, it duplicates the value from the other context.
    /// </remarks>
    internal static void _cmsAllocLogErrorChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.ErrorLogger
                       : LogErrorChunk;

        ctx.ErrorLogger = (LogErrorChunkType)((ICloneable)from).Clone();
    }

    internal static ILoggerFactory DefaultLogErrorHandlerFunction()
    {
        return LoggerFactory.Create(
            builder =>
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("lcms2", LogLevel.Debug)
                    .SetMinimumLevel(LogLevel.Error)
                    .AddConsole());
    }

    [DebuggerStepThrough]
    private static object defMtxCreate(Context? id) =>
        new Mutex(false);

    [DebuggerStepThrough]
    private static void defMtxDestroy(Context? id, object mtx) =>
        ((Mutex)mtx).Dispose();

    [DebuggerStepThrough]
    private static bool defMtxLock(Context? _, object mtx) =>
        ((Mutex)mtx).WaitOne();

    [DebuggerStepThrough]
    private static void defMtxUnlock(Context? id, object mtx) =>
        ((Mutex)mtx).ReleaseMutex();

    private static readonly MutexPluginChunkType globalMutexPluginChunk = new()
    {
        CreateFn = defMtxCreate, DestroyFn = defMtxDestroy, LockFn = defMtxLock, UnlockFn = defMtxUnlock,
    };

    private static readonly MutexPluginChunkType MutexChunk = new()
    {
        CreateFn = defMtxCreate, DestroyFn = defMtxDestroy, LockFn = defMtxLock, UnlockFn = defMtxUnlock,
    };

    /// <summary>
    ///     "Allocates" and inits mutex container.
    /// </summary>
    /// <remarks>
    ///     If src is null, only initiallizes to the default.
    ///     Otherwise, it duplicates the value from the other context.
    /// </remarks>
    internal static void _cmsAllocMutexPluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.MutexPlugin
                       : MutexChunk;

        ctx.MutexPlugin = (MutexPluginChunkType)((ICloneable)from).Clone();
    }

    internal static bool _cmsRegisterMutexPlugin(Context? context, PluginBase? data)
    {
        var ctx = Context.Get(context).MutexPlugin;

        if (data is PluginMutex Plugin)
        {
            ctx.MutexFactory = Plugin.Factory;
            ctx.CreateFn = null;
            ctx.DestroyFn = null;
            ctx.LockFn = null;
            ctx.UnlockFn = null;

            return true;
        }
        else
        {
            var LegacyPlugin = (PluginLegacyMutex?)data;

            if (data is null)
            {
                // Mo lock routines
                ctx.CreateFn = null;
                ctx.DestroyFn = null;
                ctx.LockFn = null;
                ctx.UnlockFn = null;
                ctx.MutexFactory = null;

                return true;
            }

            // Factory callback is required
            if (LegacyPlugin!.CreateMutexPtr is null || LegacyPlugin.DestroyMutexPtr is null ||
                LegacyPlugin.LockMutexPtr is null || LegacyPlugin.UnlockMutexPtr is null)
                return false;

            ctx.CreateFn = LegacyPlugin.CreateMutexPtr;
            ctx.DestroyFn = LegacyPlugin.DestroyMutexPtr;
            ctx.LockFn = LegacyPlugin.LockMutexPtr;
            ctx.UnlockFn = LegacyPlugin.UnlockMutexPtr;
            ctx.MutexFactory = null;

            return true;
        }
    }

    private static readonly ParallelizationPluginChunkType globalParallelizationPluginChunk = new(0, 0, null);

    private static readonly ParallelizationPluginChunkType ParallelizationChunk = new(0, 0, null);

    internal static void _cmsAllocParallelizationPluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.ParallelizationPlugin
                       : ParallelizationChunk;

        ctx.ParallelizationPlugin = (ParallelizationPluginChunkType)((ICloneable)from).Clone();
    }

    internal static bool _cmsRegisterParallelizationPlugin(Context? context, PluginBase? data)
    {
        var Plugin = (PluginParalellization?)data;
        var ctx = Context.Get(context).ParallelizationPlugin;

        if (data is null)
        {
            // Mo parallelization routines
            ctx.MaxWorkers = 0;
            ctx.WorkerFlags = 0;
            ctx.SchedulerFn = null;

            return true;
        }

        // Callback is required
        if (Plugin!.SchedulerFn is null)
            return false;

        ctx.MaxWorkers = Plugin.MaxWorkers;
        ctx.WorkerFlags = (int)Plugin.WorkerFlags;
        ctx.SchedulerFn = Plugin.SchedulerFn;

        return true;
    }
}
