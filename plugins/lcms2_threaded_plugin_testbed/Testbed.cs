//---------------------------------------------------------------------------------
//
//  Little Color Management System, multithread extensions
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

using lcms2.testbed;
using lcms2.types;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace lcms2.ThreadedPlugin.testbed;
internal static partial class Testbed
{
    const uint FLAGS = cmsFLAGS_NOOPTIMIZE;

    public static readonly ILoggerFactory factory = BuildDebugLogger();
    public static readonly ILogger logger = factory.CreateLogger<Program>();

    private record struct Scanline_rgb8bits(byte r, byte g, byte b);

    private record struct Scanline_rgba8bits(byte r, byte g, byte b, byte a);

    private record struct Scanline_cmyk8bits(byte c, byte m, byte y, byte k);

    private record struct Scanline_rgb16bits(ushort r, ushort g, ushort b);

    private record struct Scanline_rgba16bits(ushort r, ushort g, ushort b, ushort a);

    private record struct Scanline_cmyk16bits(ushort c, ushort m, ushort y, ushort k);

    private record struct Scanline_Lab16bits(ushort L, ushort a, ushort b);

    private record struct Scanline_rgb15bits(ushort r, ushort g, ushort b);

    private record struct Scanline_rgba15bits(ushort r, ushort g, ushort b, ushort a);

    private record struct Scanline_cmyk15bits(ushort c, ushort m, ushort y, ushort k);

    private record struct Scanline_rgbFloat(float r, float g, float b);

    private record struct Scanline_rgbaFloat(float r, float g, float b, float a);

    private record struct Scanline_cmykFloat(float c, float m, float y, float k);

    private record struct Scanline_LabFloat(float L, float a, float b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort FROM_8_TO_16(byte rgb) =>
        (ushort)((rgb << 8) | rgb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte FROM_16_TO_8(ushort rgb) =>
        (byte)((((rgb * 65281) + 8388608) >> 24) & 0xFF);

    private static Stopwatch? timer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MeasureTimeStart() =>
        timer = Stopwatch.StartNew();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double MeasureTimeStop()
    {
        timer?.Stop();

        var elapsed = (double)(timer?.Elapsed.Seconds ?? 0.0);
        elapsed += (double)(timer?.Elapsed.TotalNanoseconds ?? 0.0) / 1000000000.0;
        return elapsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static void fflush()
    {
        Console.Out.Flush();
        Console.Error.Flush();
    }

    public static ILoggerFactory BuildDebugLogger()
    {
        return LoggerFactory.Create(builder =>
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddTestBedFormatter(options => { options.IncludeScopes = true; options.SingleLine = true; }));
    }

    public static ILoggingBuilder AddTestBedFormatter(
        this ILoggingBuilder builder,
        Action<SimpleConsoleFormatterOptions> configure) =>
        builder.AddConsole(options => options.FormatterName = "TestBed")
               .AddConsoleFormatter<TestBedFormatter, SimpleConsoleFormatterOptions>(configure);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void trace(string frm, params object[] args) =>
        logger.LogInformation("{frm}", string.Format(frm, args));

    [DebuggerStepThrough, DoesNotReturn]
    public static void Fail(string message, params object[] args)
    {
        logger.LogError("{msg}", string.Format(message, args));
        fflush();
        Environment.Exit(1);
    }

    [DebuggerStepThrough, DoesNotReturn]
    public static void Fail(EventId eventId, string message, params object[] args)
    {
        logger.LogError(eventId, "{msg}", string.Format(message, args));
        fflush();
        Environment.Exit(1);
    }

    [DebuggerStepThrough, DoesNotReturn]
    public static void FatalErrorQuit(string Text) =>
        Fail("** Fatal error: {0}", Text);

    public static Profile CreateCurves()
    {
        var Gamma = cmsBuildGamma(null, 1.1);
        var Transfer = new ToneCurve[3];

        Transfer[0] = Transfer[1] = Transfer[2] = Gamma!;
        var h = cmsCreateLinearizationDeviceLink(Signature.Colorspace.Rgb, Transfer);

        cmsFreeToneCurve(Gamma);

        return h!;
    }
}
