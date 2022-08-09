﻿using lcms2.state;
using lcms2.types;

namespace lcms2.plugins;

public abstract class Plugin
{
    public Signature Magic;
    public uint ExpectedVersion;
    public Signature Type;
    public Plugin? Next;

    protected internal Plugin(Signature magic, uint expectedVersion, Signature type)
    {
        Magic = magic;
        ExpectedVersion = expectedVersion;
        Type = type;
    }

    public static bool Register(Plugin? plugin) =>
        Register(null, plugin);
    
    public static bool Register(Context? context, Plugin? plug_in)
    {
        for (var plugin = plug_in; plugin is not null; plugin = plugin.Next)
        {
            if (plugin.Magic != Signature.Plugin.MagicNumber)
            {
                Context.SignalError(context, ErrorCode.UnknownExtension, "Unrecognized plugin");
                return false;
            }

            if (plugin.ExpectedVersion > Lcms2.Version)
            {
                Context.SignalError(context, ErrorCode.UnknownExtension,
                    "plugin needs Little CMS {0}, current version is {1}", plugin.ExpectedVersion, Lcms2.Version);
                return false;
            }

            if (plugin.Type == Signature.Plugin.Interpolation)
            {
                return InterpolationPlugin.RegisterPlugin(context, plugin as InterpolationPlugin);
            }
            else if (plugin.Type == Signature.Plugin.TagType)
            {
                return TagTypePlugin.RegisterPlugin(context, plugin as TagTypePlugin);
            }
            else if (plugin.Type == Signature.Plugin.Tag)
            {
                return TagPlugin.RegisterPlugin(context, plugin as TagPlugin);
            }
            else if (plugin.Type == Signature.Plugin.Formatters)
            {
                return FormattersPlugin.RegisterPlugin(context, plugin as FormattersPlugin);
            }
            else if (plugin.Type == Signature.Plugin.RenderingIntent)
            {
                return RenderingIntentPlugin.RegisterPlugin(context, plugin as RenderingIntentPlugin);
            }
            else if (plugin.Type == Signature.Plugin.ParametricCurve)
            {
                return ParametricCurvesPlugin.RegisterPlugin(context, plugin as ParametricCurvesPlugin);
            }
            else if (plugin.Type == Signature.Plugin.MultiProcessElement)
            {
                return MultiProcessElementPlugin.RegisterPlugin(context, plugin as MultiProcessElementPlugin);
            }
            else if (plugin.Type == Signature.Plugin.Optimization)
            {
                return OptimizationPlugin.RegisterPlugin(context, plugin as OptimizationPlugin);
            }
            else if (plugin.Type == Signature.Plugin.Translform)
            {
                return TransformPlugin.RegisterPlugin(context, plugin as TransformPlugin);
            }
            else if (plugin.Type == Signature.Plugin.Mutex)
            {
                return MutexPlugin.RegisterPlugin(context, plugin as MutexPlugin);
            }
            else
            {
                Context.SignalError(context, ErrorCode.UnknownExtension, "Unrecognized plugin type {0:X8}", plugin.Type);
                return false;
            }
        }
        // plug_in was null somehow? I would expect this to be false, but it is true in the original...
        return true;
    }

    public static void UnregisterAll() =>
        UnregisterAll(null);

    public static void UnregisterAll(Context? context)
    {
        InterpolationPlugin.RegisterPlugin(context, null);
        TagTypePlugin.RegisterPlugin(context, null);
        TagPlugin.RegisterPlugin(context, null);
        FormattersPlugin.RegisterPlugin(context, null);
        RenderingIntentPlugin.RegisterPlugin(context, null);
        ParametricCurvesPlugin.RegisterPlugin(context, null);
        MultiProcessElementPlugin.RegisterPlugin(context, null);
        OptimizationPlugin.RegisterPlugin(context, null);
        TransformPlugin.RegisterPlugin(context, null);
        MutexPlugin.RegisterPlugin(context, null);
    }
}