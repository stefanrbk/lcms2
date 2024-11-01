//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright ©️ 1998-2024 Marti Maria Saguer, all rights reserved
//              2022-2024 Stefan Kewatt, all rights reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//---------------------------------------------------------------------------------

using lcms2.state;
using lcms2.testbed;
using lcms2.types;

using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace lcms2.FastFloatPlugin.testbed;
internal static partial class Testbed
{
    public static readonly ILoggerFactory factory = BuildDebugLogger();
    public static readonly ILogger logger = factory.CreateLogger<Program>();
    private static readonly List<(string name, int time)> testTimes = new();

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort FROM_8_TO_15(byte x8) =>
        (ushort)(((ulong)x8 << 15) / 0xFF);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte FROM_15_TO_8(ushort x8) =>
        (byte)((((ulong)x8 * 0xFF) + 0x4000) >> 15);

    internal const double EPSILON_FLOAT_TESTS = 5e-3;

    public static ILoggerFactory BuildDebugLogger()
    {
        return LoggerFactory.Create(builder =>
            builder
#if DEBUG
                .SetMinimumLevel(LogLevel.Debug)
#else
                .SetMinimumLevel(LogLevel.Information)
#endif
                .AddTestBedFormatter(options => { options.IncludeScopes = true; options.SingleLine = true; }));
    }

    public static ILoggerFactory BuildNullLogger()
    {
        return LoggerFactory.Create(builder =>
            builder
                .SetMinimumLevel(LogLevel.None));
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T LargestPowerOf2<T>(T value) where T : IBinaryInteger<T>
    {
        if (value < T.One)
        {
            return T.Zero;
        }

        var res = T.One;

        while (res <= value)
        {
            res <<= 1;
        }

        return res >> 1;
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogTimer(Stopwatch timer)
    {
        var elapsed = timer.Elapsed;
        var ms = new TimeSpan(TimeSpan.TicksPerMillisecond);
        var s = new TimeSpan(TimeSpan.TicksPerSecond);

        if (elapsed >= s)
            logger.LogDebug("{time} sec", elapsed.TotalSeconds);
        else if (elapsed >= ms)
            logger.LogDebug("{time} ms", elapsed.TotalMilliseconds);
        else
            logger.LogDebug("{time} ns", elapsed.TotalNanoseconds);
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void trace(string frm, params object[] args) =>
        logger.LogInformation("{frm}", string.Format(frm, args));

    [DebuggerStepThrough, DoesNotReturn]
    public static void Fail(string message, params object[] args)
    {
        logger.LogError("{msg}", string.Format(message, args));
        Thread.Sleep(1000);
        Environment.Exit(1);
    }

    [DebuggerStepThrough, DoesNotReturn]
    public static void Fail(EventId eventId, string message, params object[] args)
    {
        logger.LogError(eventId, "{msg}", string.Format(message, args));
        Environment.Exit(1);
    }

    private static Profile CreateCurves()
    {
        var Gamma = cmsBuildGamma(null, 1.1)!;
        var Transfer = new ToneCurve[3] { Gamma, Gamma, Gamma };

        var h = cmsCreateLinearizationDeviceLink(Signature.Colorspace.Rgb, Transfer);

        cmsFreeToneCurve(Gamma);

        return h!;
    }

    private static Profile loadProfile(string name)
    {
        if (name.StartsWith('*'))
        {
            if (name == "*lab")
            {
                return cmsCreateLab4Profile(null)!;
            }
            else if (name == "*xyz")
            {
                return cmsCreateXYZProfile()!;
            }
            else if (name == "*curves")
            {
                return CreateCurves();
            }
            else
            {
                Fail("Unknown builtin '{0}'", name);
            }
        }

        return cmsOpenProfileFromFile(name, "r")!;
    }

    private static Profile loadProfile(Memory<byte> mem) =>
        cmsOpenProfileFromMem(mem)!;

    private static double MPixSec(double diff)
    {
        var seconds = diff / 1000;
        return 256.0 * 256.0 * 256.0 / (1024.0 * 1024.0 * seconds);
    }

    private delegate TimeSpan perf_fn(Context? ct, Profile profileIn, Profile profileOut);

    private static TimeSpan Performance(string Title, perf_fn fn, Context? ct, string inICC, Memory<byte> outICC, long sz, TimeSpan prev) =>
        Performance(Title, fn, ct, loadProfile(inICC), loadProfile(outICC), sz, prev);

    private static TimeSpan Performance(string Title, perf_fn fn, Context? ct, Memory<byte> inICC, string outICC, long sz, TimeSpan prev) =>
        Performance(Title, fn, ct, loadProfile(inICC), loadProfile(outICC), sz, prev);

    private static TimeSpan Performance(string Title, perf_fn fn, Context? ct, string inICC, string outICC, long sz, TimeSpan prev) =>
        Performance(Title, fn, ct, loadProfile(inICC), loadProfile(outICC), sz, prev);

    private static TimeSpan Performance(string Title, perf_fn fn, Context? ct, Memory<byte> inICC, Memory<byte> outICC, long sz, TimeSpan prev) =>
        Performance(Title, fn, ct, loadProfile(inICC), loadProfile(outICC), sz, prev);

    private static TimeSpan Performance(string Title, perf_fn fn, Context? ct, Profile inICC, Profile outICC, long sz, TimeSpan prev)
    {
        using (logger.BeginScope(Title))
        {
            var profileIn = inICC;
            var profileOut = outICC;

            var n = fn(ct, profileIn, profileOut);

            var prevMPix = MPixSec(prev.TotalMilliseconds);
            var nMPix = MPixSec(n.TotalMilliseconds);
            if (prevMPix > 0.0)
            {
                var imp = nMPix / prevMPix;
                if (imp > 1)
                    trace("{1:F2} MPixel/sec. \t{2:F2} MByte/sec. \t(x {3:F1})", Title, nMPix, nMPix * sz, imp);
                else
                    trace("{1:F2} MPixel/sec. \t{2:F2} MByte/sec.", Title, nMPix, nMPix * sz);

            }
            else
            {
                trace("{1:F2} MPixel/sec. \t{2:F2} MByte/sec.", Title, nMPix, nMPix * sz);
            }

            return n;
        }
    }

}
