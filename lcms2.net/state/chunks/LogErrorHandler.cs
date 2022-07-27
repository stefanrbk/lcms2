﻿namespace lcms2.state.chunks;

public delegate void LogErrorHandlerFunction(Context? context, ErrorCode errorCode, string text);

public enum ErrorCode
{
    Undefined,
    File,
    Range,
    Internal,
    Null,
    Read,
    Seek,
    Write,
    UnknownExtension,
    ColorspaceCheck,
    AlreadyDefined,
    BadSignature,
    CorruptionDetected,
    NotSuitable,
}

internal class LogErrorHandler
{
    internal LogErrorHandlerFunction? handler = null; // Set to null for global fallback

    internal static void Alloc(ref Context ctx, in Context? src)
    {
        var from = src is not null ? (LogErrorHandler?)src.chunks[(int)Chunks.Logger] : logErrorChunk;

        ctx.chunks[(int)Chunks.Logger] = from;
    }

    private LogErrorHandler() { }

    internal static LogErrorHandler global = new() { handler = DefaultLogErrorHandlerFunction };
    private readonly static LogErrorHandler logErrorChunk = new() { handler = DefaultLogErrorHandlerFunction };

    internal static void DefaultLogErrorHandlerFunction(Context? _context, ErrorCode _errorCode, string _text) { }
}
