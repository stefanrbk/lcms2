﻿namespace lcms2.state;

internal sealed class AlarmCodes
{
    internal ushort[] alarmCodes = new ushort[Lcms2.MaxChannels];

    internal static void Alloc(ref Context ctx, in Context? src)
    {
        AlarmCodes from = (AlarmCodes?)src?.chunks[(int)Chunks.AlarmCodesContext] ?? alarmCodesChunk;

        ctx.chunks[(int)Chunks.AlarmCodesContext] = from;
    }

    private AlarmCodes()
    { }

    internal static AlarmCodes global = new() { alarmCodes = (ushort[])DefaultAlarmCodes!.Clone() };
    private static readonly AlarmCodes alarmCodesChunk = new() { alarmCodes = DefaultAlarmCodes };

    internal static readonly ushort[] DefaultAlarmCodes = new ushort[Lcms2.MaxChannels] { 0x7F00, 0x7F00, 0x7F00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
}