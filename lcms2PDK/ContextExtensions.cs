using static lcms2.Context;

namespace lcms2.pdk;

public static class ContextExtensions
{
    public static Context RegisterPlugin(this Context context, PluginBase plugin)
    {
        if (plugin.Magic != PluginSignatures.MagicNumber)
        {
            LogError(context, ErrorCodes.UnknownExtension, "Unrecognized plugin");
        }

        if (plugin.ExpectedVersion > LibraryVersion)
        {
            LogError(
                context,
                ErrorCodes.UnknownExtension,
                $"plugin needs Little CMS {plugin.ExpectedVersion}, current version is {LibraryVersion}");
        }

        if (plugin.Type == PluginSignatures.Interpolation)
            RegisterInterpPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.TagType)
            RegisterTagTypePlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.Tag)
            RegisterTagPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.Formatters)
            RegisterFormattersPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.RenderingIntent)
            RegisterRenderingIntentPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.ParametricCurve)
            RegisterParametricCurvesPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.MultiProcessElement)
            RegisterMultiProcessElementPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.Optimization)
            RegisterOptimizationPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.Transform)
            RegisterTransformPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.Mutex)
            RegisterMutexPlugin(context, plugin);
        else if (plugin.Type == PluginSignatures.Parallelization)
            RegisterParallelizationPlugin(context, plugin);
        else
            LogError(context, ErrorCodes.UnknownExtension, $"Unrecognized plugin type '{plugin.Type}'");

        return context;
    }

    public static Context RegisterPlugin(this Context context, IEnumerable<PluginBase> plugins)
    {
        foreach (var plugin in plugins)
            context.RegisterPlugin(plugin);

        return context;
    }

    public static object? CreateMutex(this Context context)
    {
        return context.MutexPlugin.MutexFactory?.Invoke(context) ?? context.MutexPlugin.CreateFn?.Invoke(context);
    }

    public static void DestroyMutex(this Context context, object? mtx)
    {
        if (mtx is IMutex mutex)
            mutex.Destroy(context);
        else if (mtx is not null)
            context.MutexPlugin.DestroyFn?.Invoke(context, mtx);
    }

    public static bool LockMutex(this Context context, object? mtx)
    {
        if (mtx is IMutex mutex)
            return mutex.Lock(context);
        else if (mtx is not null)
            return context.MutexPlugin.LockFn?.Invoke(context, mtx) ?? true;
        return true; // The user can technically not use a mutex, so locking a null shouldn't fail.
    }

    public static void UnlockMutex(this Context context, object? mtx)
    {
        if (mtx is IMutex mutex)
            mutex.Unlock(context);
        else if (mtx is not null)
            context.MutexPlugin.UnlockFn?.Invoke(context, mtx);
    }

    private static bool RegisterFormattersPlugin(this Context ContextID, PluginBase? Data)
    {
        var ctx = Get(ContextID).FormattersPlugin;

        // Reset to build-in defaults
        if (Data is not PluginFormatters Plugin)
        {
            ctx.FactoryInList.Clear();
            ctx.FactoryOutList.Clear();
            return true;
        }

        if (Plugin.FormattersFactoryIn is not null)
            ctx.FactoryInList.Add(Plugin.FormattersFactoryIn);

        if (Plugin.FormattersFactoryOut is not null)
            ctx.FactoryOutList.Add(Plugin.FormattersFactoryOut);

        return true;
    }

    private static bool RegisterInterpPlugin(Context? ctx, PluginBase? Data)
    {
        var Plugin = (PluginInterpolation?)Data;
        var ptr = Get(ctx).InterpPlugin;

        if (Data is not null)
        {
            // Set replacement functions
            ptr.Interpolators = Plugin!.InterpolatorsFactory;
            return true;
        }
        else
        {
            ptr.Interpolators = null;
            return true;
        }
    }

    private static bool RegisterTagTypePlugin(Context? id, PluginBase? Data) =>
        RegisterTypesPlugin(id, Data, false);

    private static bool RegisterMultiProcessElementPlugin(Context? id, PluginBase? Data) =>
        RegisterTypesPlugin(id, Data, true);

    private static bool RegisterTypesPlugin(Context? id, PluginBase? Data, bool isMpe)
    {
        var ctx = isMpe
                      ? Get(id).MPEPlugin
                      : Get(id).TagTypePlugin;

        // Calling the function with NULL as plug-in would unregister the plug in
        if (Data is null)
        {
            // There is no need to set free the memory, as pool is destroyed as a whole.
            ctx.List.Clear();
            return true;
        }

        if (Data is not PluginTagType Plugin)
            return false;

        // Registering happens in plug-in memory pool.
        //var pt = _cmsPluginMalloc<TagTypeLinkedList>(id);
        //if (pt is null) return false;

        //pt->Handler = Plugin!.Handler;
        //pt->Next = ctx.TagTypes;

        ctx.List.Add(Plugin.Handler);

        return true;
    }

    private static bool RegisterTagPlugin(Context? id, PluginBase? Data)
    {
        var TagPluginChunk = Get(id).TagPlugin;

        if (Data is null)
        {
            TagPluginChunk.List.Clear();
            return true;
        }

        if (Data is not PluginTag Plugin)
            return false;

        //var pt = _cmsPluginMalloc<TagLinkedList>(id);
        //if (pt == null) return false;

        //pt->Signature = Plugin!.Signature;
        //pt->Descriptor = Plugin.Descriptor;
        //pt->Next = TagPluginChunk.Tag;

        TagPluginChunk.List.Add(new(Plugin.Signature, Plugin.Descriptor));

        return true;
    }

    private static bool RegisterMutexPlugin(Context? context, PluginBase? data)
    {
        var ctx = Get(context).MutexPlugin;

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

    private static bool RegisterOptimizationPlugin(Context? id, PluginBase? Data)
    {
        var ctx = Get(id).OptimizationPlugin;
        if (Data is not PluginOptimization Plugin)
        {
            ctx.List.Clear();
            return true;
        }

        // Optimizer callback is required
        if (Plugin.OptimizePtr is null)
            return false;

        //var fl = _cmsPluginMalloc<OptimizationCollection>(id);
        //if (fl is null) return false;

        // Copy the parameters
        //fl->OptimizePtr = Plugin.OptimizePtr;

        // Keep linked list
        //fl->Next = ctx.OptimizationCollection;

        ctx.List.Add(Plugin.OptimizePtr);

        // All is ok
        return true;
    }

    private static bool RegisterParallelizationPlugin(Context? context, PluginBase? data)
    {
        var Plugin = (PluginParalellization?)data;
        var ctx = Get(context).ParallelizationPlugin;

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

    private static bool RegisterParametricCurvesPlugin(Context? ContextID, PluginBase? Data)
    {
        var ctx = Get(ContextID).CurvesPlugin;

        if (Data is not PluginParametricCurves Plugin)
        {
            ctx.ParametricCurves.Clear();
            return true;
        }

        ctx.ParametricCurves.Add(
            new ParametricCurve(
                ((int, uint)[])Plugin.Functions.Clone(),
                (ParametricCurveEvaluator)Plugin.Evaluator.Clone()));

        // All is ok
        return true;
    }

    private static bool RegisterRenderingIntentPlugin(Context? id, PluginBase? Data)
    {
        var ctx = Get(id).IntentsPlugin;

        // Do we have to reset the custom intents?
        if (Data is not PluginRenderingIntent Plugin)
        {
            ctx.Intents.Clear();
            return true;
        }

        ctx.Intents.Add(new(Plugin.Intent, Plugin.Description, Plugin.Link));

        return true;
    }

    private static bool RegisterTransformPlugin(Context? id, PluginBase? Data)
    {
        var ctx = Get(id).TransformPlugin;

        if (Data is null)
        {
            // Free the chain. Memory is safely freed at exit
            ctx.List.Clear();
            return true;
        }

        if (Data is not PluginTransform Plugin)
            return false;

        // Factory callback is required
        if (Plugin!.factories.xform is null)
            return false;

        //var fl = _cmsPluginMalloc<TransformCollection>(id);
        //if (fl is null) return false;

        // Check for full xform plug-ins previous to 2.8, we would need an adapter in that case
        //fl->OldXform = Plugin.ExpectedVersion < 2080;

        // Copy the parameters
        //fl->Factory = Plugin.factories.xform;
        ctx.List.Add(
            (Plugin.ExpectedVersion < 2080)
                ? new TransformFunc(Plugin.factories.legacy_xform)
                : new TransformFunc(Plugin.factories.xform));

        // All is ok
        return true;
    }
}
