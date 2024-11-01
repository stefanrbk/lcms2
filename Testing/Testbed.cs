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

using lcms2.state;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace lcms2.testbed;

internal static partial class Testbed
{
    public static string ReasonToFailBuffer = String.Empty;
    public static string SubTestBuffer = String.Empty;
    public static int TotalFail = 0;
    public static int TotalTests = 0;
    public static bool TrappedError;
    public static int SimultaneousErrors = 0;
    public static uint SingleHit, MaxAllocated, TotalMemory;

    public const double FIXED_PRECISION_15_16 = 1.0 / 65535;
    public const double FIXED_PRECISION_8_8 = 1.0 / 255;
    public const double FLOAT_PRECISION = 0.00001;

    public static double MaxErr = 0;
    public static readonly object MaxErrLock = new();

    public static readonly int SIZE_OF_MEM_HEADER = 16;

    public static readonly ILoggerFactory factory = BuildDebugLogger();
    public static readonly ILogger logger = factory.CreateLogger<Program>();

    internal static volatile int _emitAnsiColorCodes = -1;

    //public static readonly PluginMemHandler DebugMemHandler = new()
    //{
    //    Next = null,
    //    ExpectedVersion = 2060,
    //    Magic = cmsPluginMagicNumber,
    //    Type = cmsPluginMemHandlerSig,
    //    MallocPtr = DebugMalloc,
    //    FreePtr = DebugFree,
    //    ReallocPtr = DebugRealloc,
    //};
    public static bool HasConsole = !Console.IsInputRedirected;
    private static uint thread = 1;

    public static bool timeTests;
    private static readonly List<(string name, int time)> testTimes = new();

    static Testbed()
    {
        HiddenTagPluginSample.Descriptor.SupportedTypes[0] = SigIntType;
    }

    public static T cmsmin<T>(T a, T b) where T : IComparisonOperators<T, T, bool> =>
        (a < b) ? a : b;

    [DoesNotReturn]
    public static void Die(string message, params object[] args)
    {
        logger.LogError(message, args);
        Environment.Exit(1);
    }

    [DoesNotReturn]
    public static void Die(EventId eventId, string message, params object[] args)
    {
        logger.LogError(eventId, message, args);
        Environment.Exit(1);
    }

    [DebuggerStepThrough]
    public static Context DbgThread()
    {
        return new(null, thread++ % 0xff0);
    }

//    public static void* DebugMalloc(Context? ContextID, uint size, Type type)
//    {
//        if (size <= 0)
//        {
//            GetLogger(ContextID).LogError("malloc requested with zero bytes");
//            Die();
//        }

//        TotalMemory += size;

//        if (TotalMemory > MaxAllocated)
//            MaxAllocated = TotalMemory;

//        if (size > SingleHit) SingleHit = size;

//        try
//        {
//            var blk = (MemoryBlock*)alloc(size + (uint)sizeof(MemoryBlock), type);

//            blk->KeepSize = size;
//            blk->WhoAllocated = _cmsGetContext(ContextID).GetHashCode();
//            blk->DontCheck = 0;

//            return (byte*)blk + (uint)sizeof(MemoryBlock);
//        }
//        catch
//        {
//            return null;
//        }
//    }

//    public static void DebugFree(Context? ContextID, void* Ptr)
//    {
//        if (Ptr is null)
//        {
//            GetLogger(ContextID).LogError("NULL free (which is a no-op in C, but may be a clue of something going wrong)");
//            Die();
//        }

//        var blk = (MemoryBlock*)((byte*)Ptr - (uint)sizeof(MemoryBlock));
//        TotalMemory -= blk->KeepSize;

//        if (blk->WhoAllocated != _cmsGetContext(ContextID).GetHashCode() && blk->DontCheck is 0)
//        {
//            GetLogger(ContextID).LogError("Trying to free memory allocated by a different thread\nAllocated by Context at\t0x{expected:x16}\nFreed by Context at\t0x{actual:x16}", blk->WhoAllocated!.GetHashCode(), ContextID!.GetHashCode());
//            Die();
//        }

//        try
//        {
//            free(blk);
//        }
//        catch { }
//    }

//    public static void* DebugRealloc(Context? ContextID, void* Ptr, uint NewSize)
//    {
//        var type = typeof(void);

//        if (debugAllocs)
//        {
//            lock (AllocList)
//            {
//                if (AllocList.TryGetValue((nuint)Ptr - _sizeof<MemoryBlock>(), out var item))
//                    type = item.type;
//            }
//        }
//        var NewPtr = DebugMalloc(ContextID, NewSize, type);
//        if (Ptr is null) return NewPtr;

//        var blk = (MemoryBlock*)((byte*)Ptr - (uint)sizeof(MemoryBlock));
//        var max_sz = blk->KeepSize > NewSize ? NewSize : blk->KeepSize;
//        NativeMemory.Copy(Ptr, NewPtr, max_sz);
//        DebugFree(ContextID, Ptr);

//        return NewPtr;
//    }

//    public static void DebugMemPrintTotals()
//    {
//        using (logger.BeginScope("Memory statistics"))
//        {
//            logger.LogInformation("""
//{{"Allocated": {TotalMemory}, "MaxAlloc": {MaxAllocated}, "LargestAlloc": {SingleHit}}}
//""", TotalMemory, MaxAllocated, SingleHit);
//        }
//    }

//    public static void DebugMemDontCheckThis(void* Ptr)
//    {
//        var blk = (MemoryBlock*)((byte*)Ptr - (uint)sizeof(MemoryBlock));

//        blk->DontCheck = 1;
//    }

    public static string MemStr(uint size) =>
        size switch
        {
            > 1024 * 1024 => $"{size / (1024 * 1024)} Mb",
            > 1024 => $"{size / 1024} Kb",
            _ => $"{size} bytes",
        };

    //public static void TestMemoryLeaks(bool ok)
    //{
    //    if (TotalMemory > 0)
    //        logger.LogWarning("Ok, but {TotalMemory}, are left!", MemStr(TotalMemory));
    //    else
    //        logger.LogInformation("Ok");

    //    CheckHeap();
    //}

    public static Context WatchDogContext(object? usr) =>
        new(usr);

    public static ILoggerFactory BuildDebugLogger()
    {
        return LoggerFactory.Create(builder =>
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddTestBedFormatter(options => { options.IncludeScopes = true; options.SingleLine = true; }));
    }

    public static ILoggerFactory BuildNullLogger()
    {
        return LoggerFactory.Create(builder =>
            builder
                .ClearProviders()
                .SetMinimumLevel(LogLevel.None));
    }

    public static void FatalErrorQuit(EventId eventId, string text) =>
        Die(eventId, text);

    public static void ResetFatalError() =>
        cmsSetLogErrorHandler(BuildDebugLogger());

    public static void Check(string title, Func<bool> test)
    {
        var timer = new Stopwatch();
        using (logger.BeginScope("Checking {title}", title))
        {
            ReasonToFailBuffer = String.Empty;
            SubTestBuffer = String.Empty;
            TrappedError = false;
            SimultaneousErrors = 0;
            TotalTests++;
            timer.Start();
            var rc = test();
            timer.Stop();
            testTimes.Add((title, timer.Elapsed.Milliseconds));
            if (rc && !TrappedError)
            {
                // It is a good place to check memory
                //TestMemoryLeaks(true);
                logger.LogInformation("Ok");
            }
            else
            {
                logger.LogError("Test failed");
                TotalFail++;
            }
        }
    }

    public static bool CheckExhaustive()
    {
        Console.Write("Run exhaustive tests? (y/N) (N in 5 sec) ");
        var key = WaitForKey(5000);
        if (key.HasValue)
        {
            if (key.Value.Key is ConsoleKey.Enter or ConsoleKey.N)
            {
                if (key.Value.Key is ConsoleKey.Enter)
                    Console.WriteLine("N");
                else
                    Console.WriteLine(key.Value.KeyChar.ToString());
                return false;
            }
            else if (key.Value.Key is ConsoleKey.Y)
            {
                Console.Write(key.Value.KeyChar.ToString());
                return true;
            }
        }
        else
        {
            Console.WriteLine();
        }

        return false;
    }

    public static ConsoleKeyInfo? WaitForKey(int ms)
    {
        var cancel = new CancellationTokenSource(ms);
        var token = cancel.Token;
        var task = Task.Run(() => Console.ReadKey(true), token);
        try
        {
            task.Wait(token);
            var read = task.IsCompletedSuccessfully;
            if (read) return task.Result;
        }
        catch
        {
        }
        return null;
    }

    public static void PrintSupportedIntents()
    {
        Span<uint> codes = stackalloc uint[200];
        var descriptions = new string[200];
        var intents = cmsGetSupportedIntents(200, codes, descriptions);

        var str = "Supported intents:";
        for (var i = 0; i < intents; i++)
        {
            str += $"\n\t{codes[i]} - {descriptions[i]}";
        }

        logger.LogInformation(str);
        Thread.Sleep(10);
        Console.WriteLine();
    }

    public static bool IsGoodVal(string title, double @in, double @out, double max)
    {
        var err = Math.Abs(@in - @out);

        lock (MaxErrLock)
            if (err > MaxErr) MaxErr = err;

        if (err > max)
        {
            logger.LogWarning("({title}): Must be {in}, but was {out} ", title, @in, @out);
            return false;
        }
        return true;
    }

    public static bool IsGoodFixed15_16(string title, double @in, double @out) =>
        IsGoodVal(title, @in, @out, FIXED_PRECISION_15_16);

    public static bool IsGoodFixed8_8(string title, double @in, double @out) =>
        IsGoodVal(title, @in, @out, FIXED_PRECISION_8_8);

    public static bool IsGoodWord(string title, ushort @in, ushort @out) =>
        IsGoodWordPrec(title, @in, @out, 0);

    public static bool IsGoodWordPrec(string title, ushort @in, ushort @out, ushort maxErr)
    {
        if (Math.Abs(@in - @out) > maxErr)
        {
            logger.LogWarning("({title}): Must be {in}, but was {out} ", title, @in, @out);
            return false;
        }

        return true;
    }

    //internal static void ConsoleWrite(string str) =>
    //    _write(str, Console.Out);

    //internal static void ConsoleWriteLine(string str = "") =>
    //    _write(str + '\n', Console.Out);

    //internal static void ErrorWrite(string str) =>
    //    _write(str, Console.Error);

    //internal static void ErrorWriteLine(string str = "") =>
    //    _write(str + '\n', Console.Error);

    //private static void _write(string str, TextWriter w)
    //{
    //    var value = new Queue<char>(str.AsSpan().ToArray());
    //    var color = new StringBuilder();

    //    while (value.Count > 0)
    //    {
    //        var ch = value.Dequeue();
    //        if (ch is '{')
    //        {
    //            color.Clear();
    //            do
    //            {
    //                color.Append(value.Peek());
    //            } while (value.Dequeue() is not ':');

    //            switch (color.ToString().ToLower())
    //            {
    //                case "green:":
    //                    Console.ForegroundColor = ConsoleColor.Green;
    //                    break;

    //                case "red:":
    //                    Console.ForegroundColor = ConsoleColor.Red;
    //                    break;
    //            }
    //        }
    //        else if (ch is '}')
    //        {
    //            Console.ResetColor();
    //        }
    //        else
    //        {
    //            w.Write(ch);
    //        }
    //    }
    //}

    public static bool EmitAnsiColorCodes
    {
        get
        {
            int num = _emitAnsiColorCodes;
            if (num != -1)
            {
                return Convert.ToBoolean(num);
            }
            bool flag;
            if (!Console.IsOutputRedirected)
            {
                flag = Environment.GetEnvironmentVariable("NO_COLOR") == null;
            }
            else
            {
                var environmentVariable = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION");
                flag = environmentVariable != null && (environmentVariable == "1" || environmentVariable.Equals("true", StringComparison.OrdinalIgnoreCase));
            }
            _emitAnsiColorCodes = Convert.ToInt32(flag);
            return flag;
        }
    }

    internal static void WriteColoredMessage(this TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground)
    {
        if (background.HasValue)
            textWriter.Write(GetBackgroundColorEscapeCode(background.Value));

        if (foreground.HasValue)
            textWriter.Write(GetForegroundColorEscapeCode(foreground.Value));

        textWriter.Write(message);

        if (foreground.HasValue)
            textWriter.Write(DefaultForegroundColor);

        if (background.HasValue)
            textWriter.Write(DefaultBackgroundColor);
    }

    internal static string GetForegroundColorEscapeCode(ConsoleColor color) =>
        color switch
        {
            ConsoleColor.Black => "\u001b[30m",
            ConsoleColor.DarkRed => "\u001b[31m",
            ConsoleColor.DarkGreen => "\u001b[32m",
            ConsoleColor.DarkYellow => "\u001b[33m",
            ConsoleColor.DarkBlue => "\u001b[34m",
            ConsoleColor.DarkMagenta => "\u001b[35m",
            ConsoleColor.DarkCyan => "\u001b[36m",
            ConsoleColor.Gray => "\u001b[37m",
            ConsoleColor.Red => "\u001b[1m\u001b[31m",
            ConsoleColor.Green => "\u001b[1m\u001b[32m",
            ConsoleColor.Yellow => "\u001b[1m\u001b[33m",
            ConsoleColor.Blue => "\u001b[1m\u001b[34m",
            ConsoleColor.Magenta => "\u001b[1m\u001b[35m",
            ConsoleColor.Cyan => "\u001b[1m\u001b[36m",
            ConsoleColor.White => "\u001b[1m\u001b[37m",
            _ => "\u001b[39m\u001b[22m",
        };

    internal static string GetBackgroundColorEscapeCode(ConsoleColor color) =>
        color switch
        {
            ConsoleColor.Black => "\u001b[40m",
            ConsoleColor.DarkRed => "\u001b[41m",
            ConsoleColor.DarkGreen => "\u001b[42m",
            ConsoleColor.DarkYellow => "\u001b[43m",
            ConsoleColor.DarkBlue => "\u001b[44m",
            ConsoleColor.DarkMagenta => "\u001b[45m",
            ConsoleColor.DarkCyan => "\u001b[46m",
            ConsoleColor.Gray => "\u001b[47m",
            _ => "\u001b[49m",
        };

    internal const string DefaultForegroundColor = "\u001b[39m\u001b[22m";
    internal const string DefaultBackgroundColor = "\u001b[49m";

    public static ILoggingBuilder AddTestBedFormatter(
        this ILoggingBuilder builder,
        Action<SimpleConsoleFormatterOptions> configure) =>
        builder.AddConsole(options => options.FormatterName = "TestBed")
               .AddConsoleFormatter<TestBedFormatter, SimpleConsoleFormatterOptions>(configure);

    public static void PrintTestTimes()
    {
        logger.LogInformation("Test durations from longest to shortest");
        foreach (var (name, time) in testTimes.OrderByDescending(t => t.time))
        {
            using (logger.BeginScope("\t{test}", name))
            {
                logger.LogInformation("{time}", time);
            }
        }
        Thread.Sleep(1000);
    }
}
