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

namespace lcms2;

public delegate object? CreateMutexFnPtrType(Context? ContextID);
public delegate void DestroyMutexFnPtrType(Context? ContextID, object mtx);
public delegate bool LockMutexFnPtrType(Context? ContextID, object mtx);
public delegate void UnlockMutexFnPtrType(Context? ContextID, object mtx);
public delegate IMutex MutexFactory(Context? ContextID);

internal class MutexPluginChunkType : ICloneable
{
    public CreateMutexFnPtrType? CreateFn = null;
    public DestroyMutexFnPtrType? DestroyFn = null;
    public LockMutexFnPtrType? LockFn = null;
    public UnlockMutexFnPtrType? UnlockFn = null;
    public MutexFactory? MutexFactory = DefaultMutexFactory;

    object ICloneable.Clone() =>
        Clone();

    public MutexPluginChunkType Clone() =>
        new() { CreateFn = CreateFn, DestroyFn = DestroyFn, LockFn = LockFn, UnlockFn = UnlockFn };

    [DebuggerStepThrough]
    private static IMutex DefaultMutexFactory(Context? _) =>
        new DefaultMutex();

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
}
