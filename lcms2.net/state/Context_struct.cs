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

namespace lcms2.state;

public unsafe struct Context_struct
{
    internal Context Next;                                  //   8 +   0 =   8
    internal SubAllocator* MemPool;                         //   8 +   8 =  16
    internal MemPluginChunkType DefaultMemoryManager;       //  48 +  16 =  64
    internal ContextChunks chunks;                          //   8 +  64 =  72
    private fixed long actualChunks[(int)Chunks.Max];       // 120 +  72 = 192

    public struct ContextChunks
    {
        internal Context parent;

        internal void* this[Chunks c]
        {
            get =>
                ((void**)parent->actualChunks)[(int)c];
            set =>
                ((void**)parent->actualChunks)[(int)c] = value;
        }
    }
}
