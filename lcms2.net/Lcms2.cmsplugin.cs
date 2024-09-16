﻿//---------------------------------------------------------------------------------
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

using lcms2.state;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace lcms2;

public static partial class Lcms2
{
    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint _cmsALIGNLONG(uint x) =>
        (x + (sizeof(uint) - 1u)) & ~(sizeof(uint) - 1u);

    [DebuggerStepThrough]
    internal static string SpanToString(ReadOnlySpan<byte> span)
    {
        Span<char> str = stackalloc char[span.Length];
        var index = span.IndexOf<byte>(0);
        if (index is not -1)
            str = str[..index];
        for (var i = 0; i < str.Length; i++)
            str[i] = (char)span[i];

        return new string(str);
    }

    public static bool cmsPlugin(PluginBase plugin)
    {
        // See Context.RegisterPlugin()
        Context.Shared.RegisterPlugin(plugin);
        return true;
    }

    public static bool cmsPlugin(List<PluginBase> Plugins)
    {
        // See Context.RegisterPlugin()
        Context.Shared.RegisterPlugin(Plugins);
        return true;
    }

    public static bool cmsPluginTHR(Context? id, List<PluginBase> Plugins)
    {
        // See Context.RegisterPlugin()
        (id ?? Context.Shared).RegisterPlugin(Plugins);
        return true;
}

    public static bool cmsPluginTHR(Context? id, PluginBase? Plugin)
    {
        // See Context.RegisterPlugin()
        if (Plugin is not null)
        {
            (id ?? Context.Shared).RegisterPlugin(Plugin);
        }

        // Keep a reference to the plug-in
        return true;
    }

    [DebuggerStepThrough]
    internal static Context _cmsGetContext(Context? ContextID) =>
        ContextID ?? Context.Shared;

    /// <summary>
    ///     This function returns the given context its default, pristene state, as if no
    ///     plug-ins were declared.
    /// </summary>
    /// <remarks>
    ///     There is no way to unregister a single plug-in, as a single call to
    ///     <see cref="cmsPluginTHR"/> may register many different plug-ins
    ///     simultaneously, then there is no way to identify which plug-in to unregister.
    /// </remarks>
    public static void cmsUnregisterPlugins() =>
        // See Context.ClearAllPlugins()
        Context.Shared.ClearAllPlugins();

    /// <summary>
    ///     This function returns the given context its default, pristene state, as if no
    ///     plug-ins were declared.
    /// </summary>
    /// <remarks>
    ///     There is no way to unregister a single plug-in, as a single call to
    ///     <see cref="cmsPluginTHR"/> may register many different plug-ins
    ///     simultaneously, then there is no way to identify which plug-in to unregister.
    /// </remarks>
    public static void cmsUnregisterPluginsTHR(Context? context) =>
        // See Context.ClearAllPlugins()
        (context ?? Context.Shared).ClearAllPlugins();

    /// <summary>
    ///     Creates a new context with optional associated plug-ins.
    /// </summary>
    /// <param name="UserData">
    ///     An optional pointer to user-defined data that will be forwarded to plug-ins and logger
    /// </param>
    public static Context? cmsCreateContext(IEnumerable<PluginBase> Plugins, object? UserData = null) =>
        // See Context new()
        new(Plugins, UserData);

    /// <summary>
    ///     Creates a new context with optional associated plug-in.
    /// </summary>
    /// <param name="UserData">
    ///     An optional pointer to user-defined data that will be forwarded to plug-ins and logger
    /// </param>
    public static Context? cmsCreateContext(PluginBase? Plugin = null, object? UserData = null) =>
        // See Context new()
        new(UserData);

    /// <summary>
    ///     Duplicates a context with all associated plug-ins.
    /// </summary>
    /// <param name="NewUserData">
    ///     An optional pointer to user-defined data that will be forwarded to plug-ins and logger.<br/>
    ///     If <see langword="null"/>, the pointer to user-defined data of the original will be used.
    /// </param>
    public static Context? cmsDupContext(Context? context, object? NewUserData) =>
        // See Context.Clone()
        context?.Clone(NewUserData) ?? Context.Shared.Clone(NewUserData);

    /// <summary>
    ///     Frees any resources associated with the given <see cref="Context"/>,
    ///     and destroys the placeholder.
    /// </summary>
    /// <remarks>
    ///     <paramref name="context"/> can no longer be used in any THR operation.
    /// </remarks>
    public static void cmsDeleteContext(Context? ctx) { } // Not needed with garbage collection

    /// <summary>
    ///     Returns a reference to the user data associated to the given <paramref name="context"/>,
    ///     or <see langword="null"/> if no user data was attached on context creation.
    /// </summary>
    /// <remarks>
    ///     This can be used to change the user data if needed, but probably not thread safe!
    /// </remarks>
    
    [DebuggerStepThrough]
    public static ref object? cmsGetContextUserData(Context? context)
    {
        // See Context.UserData
        if (context is null)
        {
            return ref Context.Shared.UserData;
        }
        else
        {
            return ref context.UserData;
        }
    }

    /// <summary>
    ///     Provides thread-safe time
    /// </summary>
    /// <remarks>
    ///     <see cref="DateTime.UtcNow"/> is already thread-safe.
    ///     Providing for completeness.
    /// </remarks>

    [DebuggerStepThrough]
    internal static bool _cmsGetTime(out DateTime ptr_time)
    {
        ptr_time = DateTime.UtcNow;
        return true;
    }
}
