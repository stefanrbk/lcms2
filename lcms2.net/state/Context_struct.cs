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

using lcms2.types;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace lcms2.state;

[DebuggerStepThrough]
public class Context : ICloneable
{
    private object? userData;
    internal LogErrorChunkType ErrorLogger = new();
    internal AlarmCodesChunkType AlarmCodes = new();
    internal AdaptationStateChunkType AdaptationState = new(DEFAULT_OBSERVER_ADAPTATION_STATE);
    internal InterpPluginChunkType InterpPlugin = new();
    internal CurvesPluginChunkType CurvesPlugin = new();
    internal FormattersPluginChunkType FormattersPlugin = new();
    internal TagTypePluginChunkType TagTypePlugin = new();
    internal TagPluginChunkType TagPlugin = new();
    internal IntentsPluginChunkType IntentsPlugin = new();
    internal TagTypePluginChunkType MPEPlugin = new();
    internal OptimizationPluginChunkType OptimizationPlugin = new();
    internal TransformPluginChunkType TransformPlugin = new();
    internal MutexPluginChunkType MutexPlugin = new();
    internal ParallelizationPluginChunkType ParallelizationPlugin = new(0, 0, null);

    public ref object? UserData => ref userData;

    public static Context Default => new();
    public static readonly Context Shared = new();

    public const ushort LibraryVersion = 2160;

    public Context(object? userdata = null) =>
        userData = userdata;

    public Context(IEnumerable<PluginBase> plugins,
                   object? UserData = null) : this(UserData)
    {
        foreach (var plugin in plugins)
        {
            RegisterPlugin(plugin);
        }
    }

    public Context Clone(object? NewUserData = null) =>
        new()
        {
            UserData = NewUserData ?? UserData,
            ErrorLogger = ErrorLogger.Clone(),
            AlarmCodes = AlarmCodes.Clone(),
            AdaptationState = AdaptationState.Clone(),
            InterpPlugin = InterpPlugin.Clone(),
            CurvesPlugin = CurvesPlugin.Clone(),
            FormattersPlugin = FormattersPlugin.Clone(),
            TagTypePlugin = TagTypePlugin.Clone(),
            TagPlugin = TagPlugin.Clone(),
            IntentsPlugin = IntentsPlugin.Clone(),
            MPEPlugin = MPEPlugin.Clone(),
            OptimizationPlugin = OptimizationPlugin.Clone(),
            TransformPlugin = TransformPlugin.Clone(),
            MutexPlugin = MutexPlugin.Clone(),
            ParallelizationPlugin = ParallelizationPlugin.Clone(),
        };

    public void RegisterPlugin(PluginBase plugin)
    {
        if (plugin.Magic != Signature.Plugin.MagicNumber)
        {
            LogError(this, ErrorCodes.UnknownExtension, "Unrecognized plugin");
        }

        if (plugin.ExpectedVersion > LibraryVersion)
        {
            LogError(this, ErrorCodes.UnknownExtension, $"plugin needs Little CMS {plugin.ExpectedVersion}, current version is {LibraryVersion}");
        }

        switch ((uint)plugin.Type)
        {
            case cmsPluginInterpolationSig:
                _cmsRegisterInterpPlugin(this, plugin);
                break;

            case cmsPluginTagTypeSig:
                _cmsRegisterTagTypePlugin(this, plugin);
                break;

            case cmsPluginTagSig:
                _cmsRegisterTagPlugin(this, plugin);
                break;

            case cmsPluginFormattersSig:
                _cmsRegisterFormattersPlugin(this, plugin);
                break;

            case cmsPluginRenderingIntentSig:
                _cmsRegisterRenderingIntentPlugin(this, plugin);
                break;

            case cmsPluginParametricCurveSig:
                _cmsRegisterParametricCurvesPlugin(this, plugin);
                break;

            case cmsPluginMultiProcessElementSig:
                _cmsRegisterMultiProcessElementPlugin(this, plugin);
                break;

            case cmsPluginOptimizationSig:
                _cmsRegisterOptimizationPlugin(this, plugin);
                break;

            case cmsPluginTransformSig:
                _cmsRegisterTransformPlugin(this, plugin);
                break;

            case cmsPluginMutexSig:
                _cmsRegisterMutexPlugin(this, plugin);
                break;

            case cmsPluginParalellizationSig:
                _cmsRegisterParallelizationPlugin(this, plugin);
                break;

            default:
                LogError(this, ErrorCodes.UnknownExtension, $"Unrecognized plugin type '{plugin.Type}'");
                break;
        }
    }

    public void RegisterPlugin(IEnumerable<PluginBase> plugins)
    {
        foreach (var plugin in plugins)
            RegisterPlugin(plugin);
    }

    public void ClearAllPlugins()
    {
        InterpPlugin.Interpolators = null;
        TagTypePlugin.List.Clear();
        TagPlugin.List.Clear();
        FormattersPlugin.FactoryInList.Clear();
        FormattersPlugin.FactoryOutList.Clear();
        IntentsPlugin.Intents.Clear();
        CurvesPlugin.ParametricCurves.Clear();
        MPEPlugin.List.Clear();
        OptimizationPlugin.List.Clear();
        TransformPlugin.List.Clear();
        MutexPlugin.CreateFn = null;
        MutexPlugin.DestroyFn = null;
        MutexPlugin.LockFn = null;
        MutexPlugin.UnlockFn = null;
        ParallelizationPlugin.MaxWorkers = 0;
        ParallelizationPlugin.WorkerFlags = 0;
        ParallelizationPlugin.SchedulerFn = null;
    }

    public void SetLoggerFactory(ILoggerFactory factory) =>
        ErrorLogger.Factory = factory;

    object ICloneable.Clone() =>
        Clone();

    internal static Context Get(Context? ContextID) =>   // _cmsGetContext
        ContextID ?? Shared;

    private const double DEFAULT_OBSERVER_ADAPTATION_STATE = 1.0;

    public static object? CreateMutex(Context? context) // _cmsCreateMutex
    {
        var ctx = Get(context);
        return ctx.MutexPlugin.MutexFactory?.Invoke(ctx) ?? ctx.MutexPlugin.CreateFn?.Invoke(ctx);
    }

    public static void DestroyMutex(Context? context, object? mtx)  // _cmsDestroyMutex
    {
        var ctx = Get(context);

        if (mtx is IMutex mutex)
            mutex.Destroy(ctx);
        else if (mtx is not null)
            ctx.MutexPlugin.DestroyFn?.Invoke(ctx, mtx);
    }

    public static bool LockMutex(Context? context, object? mtx) // _cmsLockMutex
    {
        var ctx = Get(context);

        if (mtx is IMutex mutex)
            return mutex.Lock(ctx);
        else if (mtx is not null)
            return ctx.MutexPlugin.LockFn?.Invoke(ctx, mtx) ?? true;
        return true; // The user can technically not use a mutex, so locking a null shouldn't fail.
    }

    public static void UnlockMutex(Context? context, object? mtx)   // _cmsUnlockMutex
    {
        var ctx = Get(context);

        if (mtx is IMutex mutex)
            mutex.Unlock(ctx);
        else if (mtx is not null)
            ctx.MutexPlugin.UnlockFn?.Invoke(ctx, mtx);
    }
}
