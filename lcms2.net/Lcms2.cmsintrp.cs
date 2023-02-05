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

using lcms2.plugins;

namespace lcms2;

public static partial class Lcms2
{
    private static InterpPluginChunkType defaultInterpPluginChunk => new();
    private static readonly InterpPluginChunkType globalInterpPluginChunk = defaultInterpPluginChunk;

    internal static void _cmsAllocInterpPluginChunk(Context ctx, Context? src)
    {
        ctx.chunks[(int)Chunks.InterpPlugin] =
            src?.chunks[(int)Chunks.InterpPlugin] is InterpPluginChunkType chunk
            ? new InterpPluginChunkType() { Interpolators = chunk.Interpolators }
            : defaultInterpPluginChunk;
    }

    internal static bool _cmsRegisterInterpPlugin(Context? ctx, PluginBase? Data)
    {
        var ptr = (InterpPluginChunkType)_cmsContextGetClientChunk(ctx, Chunks.InterpPlugin)!;

        if (Data is PluginInterpolation Plugin)
        {
            // Set replacement functions
            ptr.Interpolators = Plugin.InterpolatorsFactory;
            return true;
        }
        else
        {
            ptr.Interpolators = null;
            return false;
        }
    }
}
