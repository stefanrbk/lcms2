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

using Microsoft.Extensions.Logging;

namespace lcms2;

public delegate void FreeUserDataFn(Context? ContextID, object? Data);
public delegate object? DupUserDataFn(Context? ContextID, object? Data);

[DebuggerStepThrough]
public class Context : ICloneable
{
    private static readonly Dictionary<LogErrorChunkType, ILogger> loggers = [ ];

    private object? userData;
    internal LogErrorChunkType ErrorLogger = new();
    internal AlarmCodesChunkType AlarmCodes = new();
    internal AdaptationStateChunkType AdaptationState = new(DefaultObserverAdaptationState);
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

    public const byte MaxChannels = 16;

    public ref object? UserData =>
        ref userData;

    public static Context Default =>
        new();

    public static readonly Context Shared = new();

    public const ushort LibraryVersion = 2160;

    public Context(object? userdata = null) =>
        userData = userdata;

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

    [DebuggerStepThrough]
    public static void
        LogError(Context? ContextID, EventId errorCode, string text, params object?[] args)  // cmsSignalError
    {
        // Check for the context, if specified go there. If not, go for the global
        var lhg = GetLogger(ContextID);
        text = String.Format(text, args);
        if (text.Length > MaxErrorMessageLen)
            text = text.Remove(MaxErrorMessageLen);

        lhg.LogError(errorCode, "{ErrorText}", text);
    }

    [DebuggerStepThrough]
    internal static ILogger GetLogger(Context? context)
    {
        context = Get(context);

        lock (loggers)
        {
            if (loggers.TryGetValue(context.ErrorLogger, out var logger))
                return logger;

            logger = context.ErrorLogger.Factory.CreateLogger("Lcms2");
            loggers.Add(context.ErrorLogger, logger);

            return logger;
        }
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

    private const double DefaultObserverAdaptationState = 1.0;
    public const byte MaxInputDimensions = 15;
    internal const int MaxErrorMessageLen = 1024;

    public const byte MaxTypesInLcmsPlugin = 20;
}
