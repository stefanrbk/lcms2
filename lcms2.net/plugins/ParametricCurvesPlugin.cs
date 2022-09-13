﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2022 Marti Maria Saguer
//                2022      Stefan Kewatt
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
using lcms2.state;
using lcms2.types;

namespace lcms2.plugins;

/// <summary>
///     Parametric Curves
/// </summary>
/// <remarks>
///     A plugin may implement an arbitrary number of parametric curves. <br/> Implements the
///     <c>cmsPluginParametricCurves</c> struct.
/// </remarks>
public sealed class ParametricCurvesPlugin : Plugin
{
    #region Fields

    /// <summary>
    ///     The evaluator
    /// </summary>
    public ParametricCurveEvaluator Evaluator;

    public (int Types, int Count)[] Functions;

    #endregion Fields

    #region Public Constructors

    public ParametricCurvesPlugin(Signature magic, uint expectedVersion, Signature type, (int Types, int Count)[] functions, ParametricCurveEvaluator evaluator)
        : base(magic, expectedVersion, type)
    {
        Functions = functions;

        Evaluator = evaluator;
    }

    #endregion Public Constructors

    #region Internal Methods

    internal static bool RegisterPlugin(object? state, ParametricCurvesPlugin? plugin)
    {
        /** Original Code (cmsgamma.c line: 126)
         **
         ** // As a way to install new parametric curves
         ** cmsBool _cmsRegisterParametricCurvesPlugin(cmsContext ContextID, cmsPluginBase* Data)
         ** {
         **     _cmsCurvesPluginChunkType* ctx = ( _cmsCurvesPluginChunkType*) _cmsContextGetClientChunk(ContextID, CurvesPlugin);
         **     cmsPluginParametricCurves* Plugin = (cmsPluginParametricCurves*) Data;
         **     _cmsParametricCurvesCollection* fl;
         **
         **     if (Data == NULL) {
         **
         **           ctx -> ParametricCurves =  NULL;
         **           return TRUE;
         **     }
         **
         **     fl = (_cmsParametricCurvesCollection*) _cmsPluginMalloc(ContextID, sizeof(_cmsParametricCurvesCollection));
         **     if (fl == NULL) return FALSE;
         **
         **     // Copy the parameters
         **     fl ->Evaluator  = Plugin ->Evaluator;
         **     fl ->nFunctions = Plugin ->nFunctions;
         **
         **     // Make sure no mem overwrites
         **     if (fl ->nFunctions > MAX_TYPES_IN_LCMS_PLUGIN)
         **         fl ->nFunctions = MAX_TYPES_IN_LCMS_PLUGIN;
         **
         **     // Copy the data
         **     memmove(fl->FunctionTypes,  Plugin ->FunctionTypes,   fl->nFunctions * sizeof(cmsUInt32Number));
         **     memmove(fl->ParameterCount, Plugin ->ParameterCount,  fl->nFunctions * sizeof(cmsUInt32Number));
         **
         **     // Keep linked list
         **     fl ->Next = ctx->ParametricCurves;
         **     ctx->ParametricCurves = fl;
         **
         **     // All is ok
         **     return TRUE;
         ** }
         **/

        var ctx = State.GetCurvesPlugin(state);

        if (plugin is null)
        {
            ctx.parametricCurves = null;
            return true;
        }

        var fl = new ParametricCurvesCollection(plugin.Functions, plugin.Evaluator, ctx.parametricCurves);

        // Keep linked list
        {
            fl.next = ctx.parametricCurves;
            ctx.parametricCurves = fl;
        }

        return true;
    }

    #endregion Internal Methods
}

internal sealed class ParametricCurvesPluginChunk
{
    #region Fields

    internal static ParametricCurvesPluginChunk global = new();
    internal ParametricCurvesCollection? parametricCurves;

    #endregion Fields

    #region Private Constructors

    private ParametricCurvesPluginChunk()
    { }

    #endregion Private Constructors

    #region Properties

    internal static ParametricCurvesPluginChunk Default => new();

    #endregion Properties
}
