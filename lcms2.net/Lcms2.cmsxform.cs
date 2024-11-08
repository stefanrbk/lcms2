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
using System.Runtime.InteropServices;

using lcms2.types;

namespace lcms2;

public static partial class Lcms2
{
    private const double DEFAULT_OBSERVER_ADAPTATION_STATE = 1.0;

    private static readonly AdaptationStateChunkType AdaptationStateChunk = new(DEFAULT_OBSERVER_ADAPTATION_STATE);

    private static readonly AdaptationStateChunkType
        globalAdaptationStateChunk = new(DEFAULT_OBSERVER_ADAPTATION_STATE);

    private static readonly AlarmCodesChunkType AlarmCodesChunk = new();

    private static readonly AlarmCodesChunkType globalAlarmCodesChunk = new();

    internal static readonly TransformPluginChunkType TransformPluginChunk = new();

    internal static readonly TransformPluginChunkType globalTransformPluginChunk = new();

    internal static void _cmsAllocAdaptationStateChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.AdaptationState
                       : AdaptationStateChunk;

        ctx.AdaptationState = (AdaptationStateChunkType)((ICloneable)from).Clone();

        //fixed (AdaptationStateChunkType* @default = &AdaptationStateChunk)
        //    AllocPluginChunk(ctx, src, Chunks.AdaptationStateContext, @default);
    }

    [DebuggerStepThrough]
    public static double cmsSetAdaptationStateTHR(Context? context, double d)
    {
        var ptr = Context.Get(context).AdaptationState;

        // Get previous value for return
        var prev = ptr.AdaptationState;

        // Set the value if d is positive or zero
        if (d >= 0)
            ptr.AdaptationState = d;

        // Always return previous value
        return prev;
    }

    public static double cmsSetAdaptationState(double d) =>
        cmsSetAdaptationStateTHR(null, d);

    public static void cmsSetAlarmCodesTHR(Context? context, ReadOnlySpan<ushort> AlarmCodesP)
    {
        var contextAlarmCodes = Context.Get(context).AlarmCodes;
        _cmsAssert(contextAlarmCodes); // Can't happen
        for (var i = 0; i < Context.MaxChannels; i++)
            contextAlarmCodes.AlarmCodes[i] = AlarmCodesP[i];
    }

    public static void cmsGetAlarmCodesTHR(Context? context, Span<ushort> AlarmCodesP)
    {
        var contextAlarmCodes = Context.Get(context).AlarmCodes;
        _cmsAssert(contextAlarmCodes); // Can't happen
        for (var i = 0; i < Context.MaxChannels; i++)
            AlarmCodesP[i] = contextAlarmCodes.AlarmCodes[i];
    }

    public static void cmsSetAlarmCodes(ReadOnlySpan<ushort> AlarmCodes) =>
        cmsSetAlarmCodesTHR(null, AlarmCodes);

    public static void cmsGetAlarmCodes(Span<ushort> AlarmCodes) =>
        cmsGetAlarmCodesTHR(null, AlarmCodes);

    internal static void _cmsAllocAlarmCodesChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.AlarmCodes
                       : AlarmCodesChunk;

        ctx.AlarmCodes = (AlarmCodesChunkType)((ICloneable)from).Clone();

        //fixed (AlarmCodesChunkType* @default = &AlarmCodesChunk)
        //    AllocPluginChunk(ctx, src, Chunks.AlarmCodesContext, @default);
    }

    public static void cmsDeleteTransform(Transform p)
    {
        _cmsAssert(p);

        //ReturnArray(p.ContextID, p.Cache.CacheIn);
        //ReturnArray(p.ContextID, p.Cache.CacheOut);

        cmsPipelineFree(p.GamutCheck);

        cmsPipelineFree(p.Lut);

        cmsFreeNamedColorList(p.InputColorant);

        cmsFreeNamedColorList(p.OutputColorant);

        cmsFreeProfileSequenceDescription(p.Sequence);

        if (p.UserData is not null)
            p.FreeUserData?.Invoke(p.ContextID, p.UserData);

        //_cmsFree(p.ContextID, p);
    }

    // PixelSize already defined in Lcms2.cmspack.cs

    public static void cmsDoTransform<Tfrom, Tto>(Transform p, Tfrom InputBuffer, out Tto OutputBuffer, uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        ReadOnlySpan<Tfrom> buf = stackalloc Tfrom[] { InputBuffer };
        cmsDoTransform(p, buf, out OutputBuffer, Size);
    }

    public static void cmsDoTransform<Tfrom, Tto>(Transform p, Span<Tfrom> InputBuffer, out Tto OutputBuffer, uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransform(p, (ReadOnlySpan<Tfrom>)InputBuffer, out OutputBuffer, Size);

    public static void cmsDoTransform<Tfrom, Tto>(Transform p,
                                                  ReadOnlySpan<Tfrom> InputBuffer,
                                                  out Tto OutputBuffer,
                                                  uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        Span<Tto> buf = stackalloc Tto[1];
        cmsDoTransform(p, InputBuffer, buf, Size);
        OutputBuffer = buf[0];
    }

    public static void cmsDoTransform<Tfrom, Tto>(Transform p, Tfrom[] InputBuffer, out Tto OutputBuffer, uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransform(p, (ReadOnlySpan<Tfrom>)InputBuffer, out OutputBuffer, Size);

    public static void cmsDoTransform<Tfrom, Tto>(Transform p, Tfrom InputBuffer, Span<Tto> OutputBuffer, uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        ReadOnlySpan<Tfrom> buf = stackalloc Tfrom[] { InputBuffer };
        cmsDoTransform(p, buf, OutputBuffer, Size);
    }

    public static void cmsDoTransform<Tfrom, Tto>(Transform p, Tfrom[] InputBuffer, Tto[] OutputBuffer, uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransform(p, (ReadOnlySpan<Tfrom>)InputBuffer, (Span<Tto>)OutputBuffer, Size);

    public static void cmsDoTransform<Tfrom, Tto>(Transform p, Tfrom[] InputBuffer, Span<Tto> OutputBuffer, uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransform(p, (ReadOnlySpan<Tfrom>)InputBuffer, OutputBuffer, Size);

    public static void cmsDoTransform<Tfrom, Tto>(Transform p,
                                                  ReadOnlySpan<Tfrom> InputBuffer,
                                                  Tto[] OutputBuffer,
                                                  uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransform(p, InputBuffer, (Span<Tto>)OutputBuffer, Size);

    public static void cmsDoTransform<Tfrom, Tto>(Transform p, Span<Tfrom> InputBuffer, Tto[] OutputBuffer, uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransform(p, (ReadOnlySpan<Tfrom>)InputBuffer, (Span<Tto>)OutputBuffer, Size);

    public static void cmsDoTransform<Tfrom, Tto>(Transform p,
                                                  Span<Tfrom> InputBuffer,
                                                  Span<Tto> OutputBuffer,
                                                  uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransform(p, (ReadOnlySpan<Tfrom>)InputBuffer, OutputBuffer, Size);

    public static void cmsDoTransform<Tfrom, Tto>(Transform p,
                                                  ReadOnlySpan<Tfrom> InputBuffer,
                                                  Span<Tto> OutputBuffer,
                                                  uint Size)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        Stride stride;

        stride.BytesPerLineIn = 0;  // Not used
        stride.BytesPerLineOut = 0;
        stride.BytesPerPlaneIn = Size * PixelSize(p.InputFormat);
        stride.BytesPerPlaneOut = Size * PixelSize(p.OutputFormat);

        p.xform(
            p,
            MemoryMarshal.Cast<Tfrom, byte>(InputBuffer),
            MemoryMarshal.Cast<Tto, byte>(OutputBuffer),
            Size,
            1,
            stride);
    }

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Tfrom InputBuffer,
                                                        out Tto OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        ReadOnlySpan<Tfrom> buf = stackalloc Tfrom[] { InputBuffer };
        cmsDoTransformStride(p, buf, out OutputBuffer, Size, Stride);
    }

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Span<Tfrom> InputBuffer,
                                                        out Tto OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformStride(p, (ReadOnlySpan<Tfrom>)InputBuffer, out OutputBuffer, Size, Stride);

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        ReadOnlySpan<Tfrom> InputBuffer,
                                                        out Tto OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        Span<Tto> buf = stackalloc Tto[1];
        cmsDoTransformStride(p, InputBuffer, buf, Size, Stride);
        OutputBuffer = buf[0];
    }

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Tfrom[] InputBuffer,
                                                        out Tto OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformStride(p, (ReadOnlySpan<Tfrom>)InputBuffer, out OutputBuffer, Size, Stride);

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Tfrom InputBuffer,
                                                        Span<Tto> OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        ReadOnlySpan<Tfrom> buf = stackalloc Tfrom[] { InputBuffer };
        cmsDoTransformStride(p, buf, OutputBuffer, Size, Stride);
    }

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Tfrom[] InputBuffer,
                                                        Tto[] OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformStride(p, (ReadOnlySpan<Tfrom>)InputBuffer, (Span<Tto>)OutputBuffer, Size, Stride);

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Tfrom[] InputBuffer,
                                                        Span<Tto> OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformStride(p, (ReadOnlySpan<Tfrom>)InputBuffer, OutputBuffer, Size, Stride);

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        ReadOnlySpan<Tfrom> InputBuffer,
                                                        Tto[] OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformStride(p, InputBuffer, (Span<Tto>)OutputBuffer, Size, Stride);

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Span<Tfrom> InputBuffer,
                                                        Tto[] OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformStride(p, (ReadOnlySpan<Tfrom>)InputBuffer, (Span<Tto>)OutputBuffer, Size, Stride);

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        Span<Tfrom> InputBuffer,
                                                        Span<Tto> OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformStride(p, (ReadOnlySpan<Tfrom>)InputBuffer, OutputBuffer, Size, Stride);

    public static void cmsDoTransformStride<Tfrom, Tto>(Transform p,
                                                        ReadOnlySpan<Tfrom> InputBuffer,
                                                        Span<Tto> OutputBuffer,
                                                        uint Size,
                                                        uint Stride)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        Stride stride;

        stride.BytesPerLineIn = 0;  // Not used
        stride.BytesPerLineOut = 0;
        stride.BytesPerPlaneIn = Stride;
        stride.BytesPerPlaneOut = Stride;

        p.xform(
            p,
            MemoryMarshal.Cast<Tfrom, byte>(InputBuffer),
            MemoryMarshal.Cast<Tto, byte>(OutputBuffer),
            Size,
            1,
            stride);
    }

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Tfrom InputBuffer,
                                                            out Tto OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        ReadOnlySpan<Tfrom> buf = stackalloc Tfrom[] { InputBuffer };
        cmsDoTransformLineStride(
            p,
            buf,
            out OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);
    }

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Span<Tfrom> InputBuffer,
                                                            out Tto OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformLineStride(
            p,
            (ReadOnlySpan<Tfrom>)InputBuffer,
            out OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            ReadOnlySpan<Tfrom> InputBuffer,
                                                            out Tto OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        Span<Tto> buf = stackalloc Tto[1];
        cmsDoTransformLineStride(
            p,
            InputBuffer,
            buf,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);
        OutputBuffer = buf[0];
    }

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Tfrom[] InputBuffer,
                                                            out Tto OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformLineStride(
            p,
            (ReadOnlySpan<Tfrom>)InputBuffer,
            out OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Tfrom InputBuffer,
                                                            Span<Tto> OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        ReadOnlySpan<Tfrom> buf = stackalloc Tfrom[] { InputBuffer };
        cmsDoTransformLineStride(
            p,
            buf,
            OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);
    }

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Tfrom[] InputBuffer,
                                                            Tto[] OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformLineStride(
            p,
            (ReadOnlySpan<Tfrom>)InputBuffer,
            (Span<Tto>)OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Tfrom[] InputBuffer,
                                                            Span<Tto> OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformLineStride(
            p,
            (ReadOnlySpan<Tfrom>)InputBuffer,
            OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            ReadOnlySpan<Tfrom> InputBuffer,
                                                            Tto[] OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformLineStride(
            p,
            InputBuffer,
            (Span<Tto>)OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Span<Tfrom> InputBuffer,
                                                            Tto[] OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformLineStride(
            p,
            (ReadOnlySpan<Tfrom>)InputBuffer,
            (Span<Tto>)OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            Span<Tfrom> InputBuffer,
                                                            Span<Tto> OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged =>
        cmsDoTransformLineStride(
            p,
            (ReadOnlySpan<Tfrom>)InputBuffer,
            OutputBuffer,
            PixelsPerLine,
            LineCount,
            BytesPerLineIn,
            BytesPerLineOut,
            BytesPerPlaneIn,
            BytesPerPlaneOut);

    public static void cmsDoTransformLineStride<Tfrom, Tto>(Transform p,
                                                            ReadOnlySpan<Tfrom> InputBuffer,
                                                            Span<Tto> OutputBuffer,
                                                            uint PixelsPerLine,
                                                            uint LineCount,
                                                            uint BytesPerLineIn,
                                                            uint BytesPerLineOut,
                                                            uint BytesPerPlaneIn,
                                                            uint BytesPerPlaneOut)
        where Tfrom : unmanaged
        where Tto : unmanaged
    {
        Stride stride;

        stride.BytesPerLineIn = BytesPerLineIn;
        stride.BytesPerLineOut = BytesPerLineOut;
        stride.BytesPerPlaneIn = BytesPerPlaneIn;
        stride.BytesPerPlaneOut = BytesPerPlaneOut;

        p.xform(
            p,
            MemoryMarshal.Cast<Tfrom, byte>(InputBuffer),
            MemoryMarshal.Cast<Tto, byte>(OutputBuffer),
            PixelsPerLine,
            LineCount,
            stride);
    }

    private static void FloatXFORM(Transform p,
                                   ReadOnlySpan<byte> @in,
                                   Span<byte> @out,
                                   uint PixelsPerLine,
                                   uint LineCount,
                                   Stride Stride)
    {
        //var pool = Context.GetPool<float>(p.ContextID);
        //float[] fIn = pool.Rent(cmsMAXCHANNELS);
        //float[] fOut = pool.Rent(cmsMAXCHANNELS);
        var fIn = new float[Context.MaxChannels];
        var fOut = new float[Context.MaxChannels];
        Span<float> OutOfGamut = stackalloc float[1];

        _cmsHandleExtraChannels(p, @in, @out, PixelsPerLine, LineCount, Stride);

        nuint strideIn = 0;
        nuint strideOut = 0;
        //memset(fIn, 0, sizeof(float) * cmsMAXCHANNELS);
        //memset(fOut, 0, sizeof(float) * cmsMAXCHANNELS);
        Array.Clear(fIn);
        Array.Clear(fOut);

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = @in[(int)strideIn..];
            var output = @out[(int)strideOut..];

            for (nuint j = 0; j < PixelsPerLine; j++)
            {
                accum = p.FromInputFloat(p, fIn, accum, Stride.BytesPerPlaneIn);

                // Any gamut check to do?
                if (p.GamutCheck is not null)
                {
                    // Evaluate gamut marker.
                    cmsPipelineEvalFloat(fIn, OutOfGamut, p.GamutCheck);

                    // Is current color out of gamut?
                    if (OutOfGamut[0] > 0.0)
                    {
                        // Certainly, out of gamut
                        for (nuint c = 0; c < Context.MaxChannels; c++)
                            fOut[c] = -1.0f;
                    }
                    else
                    {
                        // No, proceed normally
                        cmsPipelineEvalFloat(fIn, fOut, p.Lut);
                    }
                }
                else
                {
                    // No gamut check at all
                    cmsPipelineEvalFloat(fIn, fOut, p.Lut);
                }

                output = p.ToOutputFloat(p, fOut, output, Stride.BytesPerPlaneOut);
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }

        //ReturnArray(pool, fIn);
        //ReturnArray(pool, fOut);
    }

    private static void NullFloatXFORM(Transform p,
                                       ReadOnlySpan<byte> @in,
                                       Span<byte> @out,
                                       uint PixelsPerLine,
                                       uint LineCount,
                                       Stride Stride)
    {
        //var pool = Context.GetPool<float>(p.ContextID);
        //var fIn = pool.Rent(cmsMAXCHANNELS);
        var fIn = new float[Context.MaxChannels];

        _cmsHandleExtraChannels(p, @in, @out, PixelsPerLine, LineCount, Stride);

        nuint strideIn = 0u;
        nuint strideOut = 0u;
        //memset(fIn, 0, sizeof(float) * cmsMAXCHANNELS);

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = @in[(int)strideIn..];
            var output = @out[(int)strideOut..];

            for (nuint j = 0; j < PixelsPerLine; j++)
            {
                accum = p.FromInputFloat(p, fIn, accum, Stride.BytesPerPlaneIn);
                output = p.ToOutputFloat(p, fIn, output, Stride.BytesPerPlaneOut);
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }

        //ReturnArray(pool, fIn);
    }

    private static void NullXFORM(Transform p,
                                  ReadOnlySpan<byte> @in,
                                  Span<byte> @out,
                                  uint PixelsPerLine,
                                  uint LineCount,
                                  Stride Stride)
    {
        //var pool = Context.GetPool<ushort>(p.ContextID);
        var wIn = new ushort[Context.MaxChannels];

        _cmsHandleExtraChannels(p, @in, @out, PixelsPerLine, LineCount, Stride);

        nuint strideIn = 0u;
        nuint strideOut = 0u;
        //memset(wIn, 0, sizeof(ushort) * cmsMAXCHANNELS);

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = @in[(int)strideIn..];
            var output = @out[(int)strideOut..];

            for (nuint j = 0; j < PixelsPerLine; j++)
            {
                accum = p.FromInput(p, wIn, accum, Stride.BytesPerPlaneIn);
                output = p.ToOutput(p, wIn, output, Stride.BytesPerPlaneOut);
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }

        //ReturnArray(pool, wIn);
    }

    private static void PrecalculatedXFORM(Transform p,
                                           ReadOnlySpan<byte> @in,
                                           Span<byte> @out,
                                           uint PixelsPerLine,
                                           uint LineCount,
                                           Stride Stride)
    {
        //var pool = Context.GetPool<ushort>(p.ContextID);
        var wIn = new ushort[Context.MaxChannels];
        var wOut = new ushort[Context.MaxChannels];

        _cmsHandleExtraChannels(p, @in, @out, PixelsPerLine, LineCount, Stride);

        nuint strideIn = 0u;
        nuint strideOut = 0u;
        //memset(wIn, 0, sizeof(ushort) * cmsMAXCHANNELS);
        //memset(wOut, 0, sizeof(ushort) * cmsMAXCHANNELS);
        Array.Clear(wIn);
        Array.Clear(wOut);

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = @in[(int)strideIn..];
            var output = @out[(int)strideOut..];

            for (nuint j = 0; j < PixelsPerLine; j++)
            {
                accum = p.FromInput(p, wIn, accum, Stride.BytesPerPlaneIn);
                p.Lut.Eval16Fn(wIn, wOut, p.Lut.Data);
                output = p.ToOutput(p, wOut, output, Stride.BytesPerPlaneOut);
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }

        //ReturnArray(pool, wIn);
        //ReturnArray(pool, wOut);
    }

    private static void TransformOnePixelWithGamutCheck(Transform p, ReadOnlySpan<ushort> wIn, Span<ushort> wOut)
    {
        Span<ushort> wOutOfGamut = stackalloc ushort[1];

        p.GamutCheck.Eval16Fn(wIn, wOutOfGamut, p.GamutCheck.Data);
        if (wOutOfGamut[0] >= 1)
        {
            var ContextAlarmCodes = Context.Get(p.ContextID).AlarmCodes;

            for (var i = 0; i < p.Lut.OutputChannels; i++)
                wOut[i] = ContextAlarmCodes.AlarmCodes[i];
        }
        else
        {
            p.Lut.Eval16Fn(wIn, wOut, p.Lut.Data);
        }
    }

    private static void PrecalculatedXFORMGamutCheck(Transform p,
                                                     ReadOnlySpan<byte> @in,
                                                     Span<byte> @out,
                                                     uint PixelsPerLine,
                                                     uint LineCount,
                                                     Stride Stride)
    {
        //var pool = Context.GetPool<ushort>(p.ContextID);
        var wIn = new ushort[Context.MaxChannels];
        var wOut = new ushort[Context.MaxChannels];

        _cmsHandleExtraChannels(p, @in, @out, PixelsPerLine, LineCount, Stride);

        nuint strideIn = 0u;
        nuint strideOut = 0u;
        //memset(wIn, 0, sizeof(ushort) * cmsMAXCHANNELS);
        //memset(wOut, 0, sizeof(ushort) * cmsMAXCHANNELS);

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = @in[(int)strideIn..];
            var output = @out[(int)strideOut..];

            for (nuint j = 0; j < PixelsPerLine; j++)
            {
                accum = p.FromInput(p, wIn, accum, Stride.BytesPerPlaneIn);
                TransformOnePixelWithGamutCheck(p, wIn, wOut);
                output = p.ToOutput(p, wIn, output, Stride.BytesPerPlaneOut);
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }

        //ReturnArray(pool, wIn);
        //ReturnArray(pool, wOut);
    }

    private static void CachedXFORM(Transform p,
                                    ReadOnlySpan<byte> @in,
                                    Span<byte> @out,
                                    uint PixelsPerLine,
                                    uint LineCount,
                                    Stride Stride)
    {
        //var pool = Context.GetPool<ushort>(p.ContextID);
        var wIn = new ushort[Context.MaxChannels];
        var wOut = new ushort[Context.MaxChannels];
        Cache Cache = new() { CacheIn = new ushort[Context.MaxChannels], CacheOut = new ushort[Context.MaxChannels] };

        _cmsHandleExtraChannels(p, @in, @out, PixelsPerLine, LineCount, Stride);

        // Empty buffers for quick memcmp
        //memset(wIn, 0, sizeof(ushort) * cmsMAXCHANNELS);
        //memset(wOut, 0, sizeof(ushort) * cmsMAXCHANNELS);

        // Get copy of zero cache
        //fixed (Cache* ptr = &p.Cache)
        //    memcpy(&Cache, ptr);

        nuint strideIn = 0u;
        nuint strideOut = 0u;

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = @in[(int)strideIn..];
            var output = @out[(int)strideOut..];

            for (nuint j = 0; j < PixelsPerLine; j++)
            {
                accum = p.FromInput(p, wIn, accum, Stride.BytesPerPlaneIn);

                if (memcmp(wIn.AsSpan(..Context.MaxChannels), Cache.CacheIn.AsSpan(..Context.MaxChannels)) is 0)
                {
                    memcpy(wOut.AsSpan(..Context.MaxChannels), Cache.CacheOut.AsSpan(..Context.MaxChannels));
                }
                else
                {
                    p.Lut.Eval16Fn(wIn, wOut, p.Lut.Data);

                    memcpy(Cache.CacheIn.AsSpan(..Context.MaxChannels), wIn.AsSpan(..Context.MaxChannels));
                    memcpy(Cache.CacheOut.AsSpan(..Context.MaxChannels), wOut.AsSpan(..Context.MaxChannels));
                }

                output = p.ToOutput(p, wIn, output, Stride.BytesPerPlaneOut);
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }

        //ReturnArray(pool, wIn);
        //ReturnArray(pool, wOut);
        //ReturnArray(pool, Cache.CacheIn);
        //ReturnArray(pool, Cache.CacheOut);
    }

    private static void CachedXFORMGamutCheck(Transform p,
                                              ReadOnlySpan<byte> @in,
                                              Span<byte> @out,
                                              uint PixelsPerLine,
                                              uint LineCount,
                                              Stride Stride)
    {
        //var pool = Context.GetPool<ushort>(p.ContextID);
        var wIn = new ushort[Context.MaxChannels];
        var wOut = new ushort[Context.MaxChannels];
        Cache Cache = new() { CacheIn = new ushort[Context.MaxChannels], CacheOut = new ushort[Context.MaxChannels] };

        _cmsHandleExtraChannels(p, @in, @out, PixelsPerLine, LineCount, Stride);

        // Empty buffers for quick memcmp
        //memset(wIn, 0, sizeof(ushort) * cmsMAXCHANNELS);
        //memset(wOut, 0, sizeof(ushort) * cmsMAXCHANNELS);

        // Get copy of zero cache
        //fixed (Cache* ptr = &p.Cache)
        //    memcpy(&Cache, ptr);

        nuint strideIn = 0u;
        nuint strideOut = 0u;

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = @in[(int)strideIn..];
            var output = @out[(int)strideOut..];

            for (nuint j = 0; j < PixelsPerLine; j++)
            {
                accum = p.FromInput(p, wIn, accum, Stride.BytesPerPlaneIn);

                if (memcmp(wIn.AsSpan(..Context.MaxChannels), Cache.CacheIn.AsSpan(..Context.MaxChannels)) is 0)
                {
                    memcpy(wOut.AsSpan(..Context.MaxChannels), Cache.CacheOut.AsSpan(..Context.MaxChannels));
                }
                else
                {
                    TransformOnePixelWithGamutCheck(p, wIn, wOut);

                    memcpy(Cache.CacheIn.AsSpan(..Context.MaxChannels), wIn.AsSpan(..Context.MaxChannels));
                    memcpy(Cache.CacheOut.AsSpan(..Context.MaxChannels), wOut.AsSpan(..Context.MaxChannels));
                }

                output = p.ToOutput(p, wIn, output, Stride.BytesPerPlaneOut);
            }

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }

        //ReturnArray(pool, wIn);
        //ReturnArray(pool, wOut);
        //ReturnArray(pool, Cache.CacheIn);
        //ReturnArray(pool, Cache.CacheOut);
    }

    internal static void DupPluginTransformList(ref TransformPluginChunkType dest, in TransformPluginChunkType src) =>
        dest = (TransformPluginChunkType)((ICloneable)src).Clone();

    internal static void _cmsAllocTransformPluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.TransformPlugin
                       : TransformPluginChunk;

        DupPluginTransformList(ref ctx.TransformPlugin, from);
    }

    internal static void _cmsTransform2toTransformAdaptor(Transform CMMcargo,
                                                          ReadOnlySpan<byte> InputBuffer,
                                                          Span<byte> OutputBuffer,
                                                          uint PixelsPerLine,
                                                          uint LineCount,
                                                          Stride Stride)
    {
        _cmsHandleExtraChannels(CMMcargo, InputBuffer, OutputBuffer, PixelsPerLine, LineCount, Stride);

        nuint strideIn = 0u;
        nuint strideOut = 0u;

        for (nuint i = 0; i < LineCount; i++)
        {
            var accum = InputBuffer[(int)strideIn..];
            var output = OutputBuffer[(int)strideOut..];

            CMMcargo.OldXform!(CMMcargo, accum, output, PixelsPerLine, Stride.BytesPerPlaneIn);

            strideIn += Stride.BytesPerLineIn;
            strideOut += Stride.BytesPerLineOut;
        }
    }

    private static void ParalellizeIfSuitable(Transform p)
    {
        var ctx = Context.Get(p.ContextID).ParallelizationPlugin;

        _cmsAssert(p);
        if (ctx?.SchedulerFn is not null)
        {
            p.Worker = p.xform;
            p.xform = ctx.SchedulerFn;
            p.MaxWorkers = ctx.MaxWorkers;
            p.WorkerFlags = (uint)ctx.WorkerFlags;
        }
    }

    private static ReadOnlySpan<byte> UnrollNothing(Transform _1, Span<ushort> _2, ReadOnlySpan<byte> accum, uint _3) =>
        accum;

    private static Span<byte> PackNothing(Transform _1, ReadOnlySpan<ushort> _2, Span<byte> output, uint _3) =>
        output;

    private static Transform? AllocEmptyTransform(Context? ContextID,
                                                  Pipeline? lut,
                                                  uint Intent,
                                                  ref uint InputFormat,
                                                  ref uint OutputFormat,
                                                  ref uint dwFlags)
    {
        var ctx = Context.Get(ContextID).TransformPlugin;
        //var pool = Context.GetPool<ushort>(ContextID);

        // Allocate needed memory
        //var p = _cmsMallocZero<Transform>(ContextID);
        //if (p is null)
        //{
        //    cmsPipelineFree(lut);
        //    return null;
        //}

        // Store the proposed pipeline
        //p.Lut = lut;

        var p = new Transform() { Lut = lut };
        p.Cache.CacheIn = new ushort[Context.MaxChannels];
        p.Cache.CacheOut = new ushort[Context.MaxChannels];

        // Let's see if any plug-in wants to do the transform by itself
        if (p.Lut is not null)
        {
            if ((dwFlags & cmsFLAGS_NOOPTIMIZE) is 0)
            {
                foreach (var Plugin in ctx.List)
                {
                    p.ContextID = ContextID;
                    p.InputFormat = InputFormat;
                    p.OutputFormat = OutputFormat;
                    p.dwOriginalFlags = dwFlags;

                    p.FromInput = _cmsGetFormatterIn(ContextID, InputFormat, PackFlags.Ushort).Fmt16;
                    p.ToOutput = _cmsGetFormatterOut(ContextID, OutputFormat, PackFlags.Ushort).Fmt16;
                    p.FromInputFloat = _cmsGetFormatterIn(ContextID, InputFormat, PackFlags.Float).FmtFloat;
                    p.ToOutputFloat = _cmsGetFormatterOut(ContextID, OutputFormat, PackFlags.Float).FmtFloat;

                    if (Plugin.OldXform)
                    {
                        if (!Plugin.OldFactory(
                                out p.OldXform,
                                out p.userData,
                                out p.FreeUserData,
                                ref p.Lut,
                                ref p.InputFormat,
                                ref p.OutputFormat,
                                ref p.dwOriginalFlags))
                        {
                            continue;
                        }

                        p.xform = _cmsTransform2toTransformAdaptor;
                        return p;
                    }

                    if (!Plugin.Factory(
                            out p.xform,
                            out p.userData,
                            out p.FreeUserData,
                            ref p.Lut,
                            ref p.InputFormat,
                            ref p.OutputFormat,
                            ref p.dwOriginalFlags))
                    {
                        continue;
                    }
                    // Last plugin in the declaration order takes control. We just keep
                    // the original parameters as a logging.
                    // Note that cmsFLAGS_CAN_CHANGE_FORMATTER is not set, so by default
                    // an optimized transform is not reusable. The plug-in can, however, change
                    // the flags and make it suitable.

                    //p.ContextID = ContextID;
                    //p.InputFormat = *InputFormat;
                    //p.OutputFormat = *OutputFormat;
                    //p.dwOriginalFlags = *dwFlags;

                    // Fill the formatters just in case the optimized routine is interested.
                    // No error is thrown if the formatter doesn't exist. It is up to the optimization
                    // factory to decide what to do in those cases.
                    //p.FromInput = _cmsGetFormatter(ContextID, *InputFormat, FormatterDirection.Input, PackFlags.Ushort).Fmt16;
                    //p.ToOutput = _cmsGetFormatter(ContextID, *OutputFormat, FormatterDirection.Output, PackFlags.Ushort).Fmt16;
                    //p.FromInputFloat = _cmsGetFormatter(ContextID, *InputFormat, FormatterDirection.Input, PackFlags.Float).FmtFloat;
                    //p.ToOutputFloat = _cmsGetFormatter(ContextID, *OutputFormat, FormatterDirection.Output, PackFlags.Float).FmtFloat;

                    // Save the day? (Ignore the warning)
                    //if (Plugin->OldXform)
                    //{
                    //    p.OldXform = *(TransformFn*)&p->xform;
                    //    p.xform = _cmsTransform2toTransformAdaptor;
                    //}

                    ParalellizeIfSuitable(p);
                    return p;
                }
            }

            // Not suitable for the transform plug-in, let's check the pipeline plug-in
            _cmsOptimizePipeline(ContextID, ref p.Lut, Intent, ref InputFormat, ref OutputFormat, ref dwFlags);
        }

        // Check whether this is a true floating point transform
        if (_cmsFormatterIsFloat(InputFormat) || _cmsFormatterIsFloat(OutputFormat))
        {
            // Get formatter function always return a valid union, but the context of this union may be null.
            p.FromInputFloat = _cmsGetFormatterIn(ContextID, InputFormat, PackFlags.Float).FmtFloat;
            p.ToOutputFloat = _cmsGetFormatterOut(ContextID, OutputFormat, PackFlags.Float).FmtFloat;
            dwFlags |= cmsFLAGS_CAN_CHANGE_FORMATTER;

            if (p.FromInputFloat is null || p.ToOutputFloat is null)
            {
                Context.LogError(ContextID, cmsERROR_UNKNOWN_EXTENSION, "Unsupported raster format");
                cmsDeleteTransform(p);
                return null;
            }

            p.xform = ((dwFlags & cmsFLAGS_NULLTRANSFORM) is not 0)
                          ? NullFloatXFORM
                          // Float transforms don't use cache, always are non-null
                          : FloatXFORM;
        }
        else
        {
            // Formats are intended to be changed before use
            if (InputFormat is 0 && OutputFormat is 0)
            {
                p.FromInput = UnrollNothing;
                p.ToOutput = PackNothing;
                dwFlags |= cmsFLAGS_CAN_CHANGE_FORMATTER;
            }
            else
            {
                p.FromInput = _cmsGetFormatterIn(ContextID, InputFormat, PackFlags.Ushort).Fmt16;
                p.ToOutput = _cmsGetFormatterOut(ContextID, OutputFormat, PackFlags.Ushort).Fmt16;

                if (p.FromInput is null || p.ToOutput is null)
                {
                    Context.LogError(ContextID, cmsERROR_UNKNOWN_EXTENSION, "Unsupported raster format");
                    cmsDeleteTransform(p);
                    return null;
                }

                var BytesPerPixelInput = T_BYTES(p.InputFormat);
                if (BytesPerPixelInput is 0 or >= 2)
                    dwFlags |= cmsFLAGS_CAN_CHANGE_FORMATTER;
            }

            p.xform =
                (dwFlags & cmsFLAGS_NULLTRANSFORM, dwFlags & cmsFLAGS_NOCACHE, dwFlags & cmsFLAGS_GAMUTCHECK) switch
                {
                    (not 0, _, _)     => NullXFORM,
                    (_, not 0, not 0) => PrecalculatedXFORMGamutCheck,
                    (_, not 0, _)     => PrecalculatedXFORM,
                    (_, _, not 0)     => CachedXFORMGamutCheck,
                    _                 => CachedXFORM,
                };
        }

        // Check consistency for alpha channel copy

        if ((dwFlags & cmsFLAGS_COPY_ALPHA) is not 0)
        {
            if (T_EXTRA(InputFormat) != T_EXTRA(OutputFormat))
            {
                Context.LogError(ContextID, cmsERROR_NOT_SUITABLE, "Mismatched alpha channels");
                cmsDeleteTransform(p);
                return null;
            }
        }

        p.InputFormat = InputFormat;
        p.OutputFormat = OutputFormat;
        p.dwOriginalFlags = dwFlags;
        p.ContextID = ContextID;
        p.userData = null;
        ParalellizeIfSuitable(p);
        return p;
    }

    private static bool GetXFormColorSpaces(uint nProfiles,
                                            Profile?[] Profiles,
                                            out Signature Input,
                                            out Signature Output)
    {
        Input = Output = default;

        if (nProfiles is 0)
            return false;
        if (Profiles[0] is null)
            return false;

        var PostColorSpace = Input = cmsGetColorSpace(Profiles[0]!);

        for (var i = 0; i < nProfiles; i++)
        {
            var Profile = Profiles[i];

            var lIsInput = PostColorSpace != Signatures.Colorspace.XYZ && PostColorSpace != Signatures.Colorspace.Lab;

            if (Profile is null)
                return false;

            var cls = cmsGetDeviceClass(Profile);

            Signature ColorSpaceIn;
            Signature ColorSpaceOut;
            if (cls == Signatures.ProfileClass.NamedColor)
            {
                (ColorSpaceIn, ColorSpaceOut) = (Signatures.Colorspace.Color1,
                                                 (nProfiles > 1) ? cmsGetPCS(Profile) : cmsGetColorSpace(Profile));
            }
            else if (lIsInput || cls == Signatures.ProfileClass.Link)
            {
                (ColorSpaceIn, ColorSpaceOut) = (cmsGetColorSpace(Profile), cmsGetPCS(Profile));
            }
            else
            {
                (ColorSpaceIn, ColorSpaceOut) = (cmsGetPCS(Profile), cmsGetColorSpace(Profile));
            }

            if (i is 0)
                Input = ColorSpaceIn;

            PostColorSpace = ColorSpaceOut;
        }

        Output = PostColorSpace;

        return true;
    }

    private static bool IsProperColorSpace(Signature Check, uint dwFormat)
    {
        var Space1 = T_COLORSPACE(dwFormat);
        var Space2 = _cmsLCMScolorSpace(Check);

        if (Space1 is PT_ANY)
            return true;
        if (Space1 == Space2)
            return true;

        if (Space1 is PT_LabV2 && Space2 is PT_Lab)
            return true;
        if (Space1 is PT_Lab && Space2 is PT_LabV2)
            return true;

        return false;
    }

    private static void NormalizeXYZ(ref CIEXYZ Dest)
    {
        while (Dest.X > 2 && Dest.Y > 2 && Dest.Z > 2)
        {
            Dest.X /= 10;
            Dest.Y /= 10;
            Dest.Z /= 10;
        }
    }

    private static void SetWhitePoint(out CIEXYZ wtPt, Box<CIEXYZ>? src)
    {
        if (src is null)
        {
            wtPt.X = CIEXYZ.D50.X;
            wtPt.Y = CIEXYZ.D50.Y;
            wtPt.Z = CIEXYZ.D50.Z;
        }
        else
        {
            wtPt.X = src.Value.X;
            wtPt.Y = src.Value.Y;
            wtPt.Z = src.Value.Z;

            NormalizeXYZ(ref wtPt);
        }
    }

    public static Transform? cmsCreateExtendedTransform(Context? ContextID,
                                                        uint nProfiles,
                                                        Profile[] Profiles,
                                                        Span<bool> BPC,
                                                        ReadOnlySpan<uint> Intents,
                                                        ReadOnlySpan<double> AdaptationStates,
                                                        Profile? hGamutProfile,
                                                        uint nGamutPCSposition,
                                                        uint InputFormat,
                                                        uint OutputFormat,
                                                        uint dwFlags)
    {
        Signature EntryColorSpace, ExitColorSpace;

        // Safeguard
        if (nProfiles is 0 or >= 255)
        {
            Context.LogError(
                ContextID,
                cmsERROR_RANGE,
                "Wrong number of profiles. 1..255 expected, {0} found.",
                nProfiles);
            return null;
        }

        var LastIntent = Intents[(int)nProfiles - 1];

        // If it is a fake transform
        if ((dwFlags & cmsFLAGS_NULLTRANSFORM) is not 0)
            return AllocEmptyTransform(
                ContextID,
                null,
                INTENT_PERCEPTUAL,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags);

        // If gamut check is requested, make sure we have a gamut profile
        if ((dwFlags & cmsFLAGS_GAMUTCHECK) is not 0 && hGamutProfile is null)
            dwFlags &= ~(uint)cmsFLAGS_GAMUTCHECK;

        // On floating point transforms, inhibit cache
        if (_cmsFormatterIsFloat(InputFormat) || _cmsFormatterIsFloat(OutputFormat))
            dwFlags |= cmsFLAGS_NOCACHE;

        // Mark entry/exit spaces
        if (!GetXFormColorSpaces(nProfiles, Profiles, out EntryColorSpace, out ExitColorSpace))
        {
            Context.LogError(ContextID, cmsERROR_NULL, "NULL input profiles on transform");
            return null;
        }

        // Check if proper colorspaces
        if (!IsProperColorSpace(EntryColorSpace, InputFormat))
        {
            Context.LogError(ContextID, cmsERROR_COLORSPACE_CHECK, "Wrong input color space on transform");
            return null;
        }

        if (!IsProperColorSpace(ExitColorSpace, OutputFormat))
        {
            Context.LogError(ContextID, cmsERROR_COLORSPACE_CHECK, "Wrong output color space on transform");
            return null;
        }

        // Check whether the transform is 16 bits and involves linear RGB in first profile. If so, disable optimizations
        if (EntryColorSpace == Signatures.Colorspace.Rgb && T_BYTES(InputFormat) is 2 &&
            (dwFlags & cmsFLAGS_NOOPTIMIZE) is 0)
        {
            var gamma = cmsDetectRGBProfileGamma(Profiles[0], 0.1);

            if (gamma is > 0 and < 1.6)
                dwFlags |= cmsFLAGS_NOOPTIMIZE;
        }

        // Create a pipeline with all transformations
        var Lut = _cmsLinkProfiles(ContextID, nProfiles, Intents, Profiles, BPC, AdaptationStates, dwFlags);
        if (Lut is null)
        {
            Context.LogError(ContextID, cmsERROR_NOT_SUITABLE, "Couldn't link the profiles");
            return null;
        }

        // Check channel count
        if (((uint)cmsChannelsOfColorSpace(EntryColorSpace) != cmsPipelineInputChannels(Lut)) ||
            ((uint)cmsChannelsOfColorSpace(ExitColorSpace) != cmsPipelineOutputChannels(Lut)))
        {
            cmsPipelineFree(Lut);
            Context.LogError(ContextID, cmsERROR_NOT_SUITABLE, "Channel count douesn't match. Profile is corrupted");
            return null;
        }

        // All seems ok
        var xform = AllocEmptyTransform(ContextID, Lut, LastIntent, ref InputFormat, ref OutputFormat, ref dwFlags);
        if (xform is null)
            goto Error;

        // Keep values
        xform.EntryColorSpace = EntryColorSpace;
        xform.ExitColorSpace = ExitColorSpace;
        xform.RenderingIntent = Intents[(int)nProfiles - 1];

        // Take white points
        SetWhitePoint(
            out xform.EntryWhitePoint,
            cmsReadTag(Profiles[0], Signatures.Tag.MediaWhitePoint) as Box<CIEXYZ>);
        SetWhitePoint(
            out xform.ExitWhitePoint,
            cmsReadTag(Profiles[nProfiles - 1], Signatures.Tag.MediaWhitePoint) as Box<CIEXYZ>);

        // Create a gamut check LUT if requested
        if (hGamutProfile is not null && ((dwFlags & cmsFLAGS_GAMUTCHECK) is not 0))
            xform.GamutCheck = _cmsCreateGamutCheckPipeline(
                ContextID,
                Profiles,
                BPC,
                Intents,
                AdaptationStates,
                nGamutPCSposition,
                hGamutProfile);

        // Try to read input and output colorant table
        if (cmsIsTag(Profiles[0], Signatures.Tag.ColorantTable))
        {
            // Input table can only come in this way.
            xform.InputColorant =
                cmsDupNamedColorList(cmsReadTag(Profiles[0], Signatures.Tag.ColorantTable) as NamedColorList)!;
        }

        // Output is a little bit more complex.
        if (cmsGetDeviceClass(Profiles[nProfiles - 1]) == Signatures.ProfileClass.Link)
        {
            // This tag may exist only on devicelink profiles.
            if (cmsIsTag(Profiles[nProfiles - 1], Signatures.Tag.ColorantTableOut))
            {
                // It may be null if error
                xform.OutputColorant = cmsDupNamedColorList(
                    cmsReadTag(Profiles[nProfiles - 1], Signatures.Tag.ColorantTableOut) as NamedColorList)!;
            }
        }
        else
        {
            if (cmsIsTag(Profiles[nProfiles - 1], Signatures.Tag.ColorantTable))
                xform.OutputColorant = cmsDupNamedColorList(
                    cmsReadTag(Profiles[nProfiles - 1], Signatures.Tag.ColorantTable) as NamedColorList)!;
        }

        // Store the sequence of profiles
        xform.Sequence = ((dwFlags & cmsFLAGS_KEEP_SEQUENCE) is not 0)
                             ? _cmsCompileProfileSequence(ContextID, nProfiles, Profiles)
                             : null;

        // If this is a cached transform, init first value, which is zero (16 bits only)
        if ((dwFlags & cmsFLAGS_NOCACHE) is 0)
        {
            Array.Clear(xform.Cache.CacheIn);

            if (xform.GamutCheck is not null)
            {
                TransformOnePixelWithGamutCheck(xform, xform.Cache.CacheIn, xform.Cache.CacheOut);
            }
            else
            {
                xform.Lut.Eval16Fn(xform.Cache.CacheIn, xform.Cache.CacheOut, xform.Lut.Data);
            }
        }

        return xform;

    Error:
        cmsPipelineFree(Lut);
        return null;
    }

    public static Transform? cmsCreateMultiprofileTransformTHR(Context? ContextID,
                                                               Profile[] Profiles,
                                                               uint InputFormat,
                                                               uint OutputFormat,
                                                               uint nProfiles,
                                                               uint Intent,
                                                               uint dwFlags)
    {
        Span<bool> BPC = stackalloc bool[256];
        Span<uint> Intents = stackalloc uint[256];
        Span<double> AdaptationStates = stackalloc double[256];

        if (nProfiles is <= 0 or > 255)
        {
            Context.LogError(
                ContextID,
                cmsERROR_RANGE,
                $"Wrong number of profiles. 1..255 expected, {nProfiles} found.");
            return null;
        }

        for (var i = 0; i < nProfiles; i++)
        {
            BPC[i] = (dwFlags & cmsFLAGS_BLACKPOINTCOMPENSATION) is not 0;
            Intents[i] = Intent;
            AdaptationStates[i] = cmsSetAdaptationStateTHR(ContextID, -1);
        }

        return cmsCreateExtendedTransform(
            ContextID,
            nProfiles,
            Profiles,
            BPC,
            Intents,
            AdaptationStates,
            null,
            0,
            InputFormat,
            OutputFormat,
            dwFlags);
    }

    public static Transform? cmsCreateMultiprofileTransform(Profile[] Profiles,
                                                            uint InputFormat,
                                                            uint OutputFormat,
                                                            uint nProfiles,
                                                            uint Intent,
                                                            uint dwFlags) =>
        cmsCreateMultiprofileTransformTHR(null, Profiles, InputFormat, OutputFormat, nProfiles, Intent, dwFlags);

    public static Transform? cmsCreateTransformTHR(Context? ContextID,
                                                   Profile Input,
                                                   uint InputFormat,
                                                   Profile? Output,
                                                   uint OutputFormat,
                                                   uint Intent,
                                                   uint dwFlags)
    {
        var hArray = new Profile[2] { Input, Output! };

        return cmsCreateMultiprofileTransformTHR(
            ContextID,
            hArray,
            InputFormat,
            OutputFormat,
            Output is null ? 1u : 2u,
            Intent,
            dwFlags);
    }

    public static Transform? cmsCreateTransform(Profile Input,
                                                uint InputFormat,
                                                Profile? Output,
                                                uint OutputFormat,
                                                uint Intent,
                                                uint dwFlags) =>
        cmsCreateTransformTHR(null, Input, InputFormat, Output, OutputFormat, Intent, dwFlags);

    public static Transform? cmsCreateProofingTransformTHR(Context? ContextID,
                                                           Profile InputProfile,
                                                           uint InputFormat,
                                                           Profile OutputProfile,
                                                           uint OutputFormat,
                                                           Profile ProofingProfile,
                                                           uint nIntent,
                                                           uint ProofingIntent,
                                                           uint dwFlags)
    {
        var DoBPC = (dwFlags & cmsFLAGS_BLACKPOINTCOMPENSATION) is not 0;
        var hArray = new Profile[4] { InputProfile, ProofingProfile, ProofingProfile, OutputProfile };
        Span<uint> Intents = stackalloc uint[4] { nIntent, nIntent, INTENT_RELATIVE_COLORIMETRIC, ProofingIntent };
        Span<bool> BPC = stackalloc bool[4] { DoBPC, DoBPC, false, false };
        Span<double> Adaptation = stackalloc double[4];

        Adaptation[0] = Adaptation[1] = Adaptation[2] = Adaptation[3] = cmsSetAdaptationStateTHR(ContextID, -1);

        if ((dwFlags & (cmsFLAGS_SOFTPROOFING | cmsFLAGS_GAMUTCHECK)) is 0)
            return cmsCreateTransformTHR(
                ContextID,
                InputProfile,
                InputFormat,
                OutputProfile,
                OutputFormat,
                nIntent,
                dwFlags);

        return cmsCreateExtendedTransform(
            ContextID,
            4,
            hArray,
            BPC,
            Intents,
            Adaptation,
            ProofingProfile,
            1,
            InputFormat,
            OutputFormat,
            dwFlags);
    }

    public static Transform? cmsCreateProofingTransform(Profile InputProfile,
                                                        uint InputFormat,
                                                        Profile OutputProfile,
                                                        uint OutputFormat,
                                                        Profile ProofingProfile,
                                                        uint nIntent,
                                                        uint ProofingIntent,
                                                        uint dwFlags) =>
        cmsCreateProofingTransformTHR(
            null,
            InputProfile,
            InputFormat,
            OutputProfile,
            OutputFormat,
            ProofingProfile,
            nIntent,
            ProofingIntent,
            dwFlags);

    public static Context? cmsGetTransformContextID(Transform? xform) =>
        xform?.ContextID;

    public static uint cmsGetTransformInputFormat(Transform? xform) =>
        xform?.InputFormat ?? 0;

    public static uint cmsGetTransformOutputFormat(Transform? xform) =>
        xform?.OutputFormat ?? 0;

    public static bool cmsChangeBuffersFormat(Transform? xform, uint InputFormat, uint OutputFormat)
    {
        // We only can afford to change formatters if previous transform is at least 16 bits
        if ((xform.dwOriginalFlags & cmsFLAGS_CAN_CHANGE_FORMATTER) is 0)
        {
            Context.LogError(
                xform.ContextID,
                cmsERROR_NOT_SUITABLE,
                "cmsChangeBuffersFormat works only on transforms created originally with at least 16 bits of precision");
            return false;
        }

        var FromInput = _cmsGetFormatterIn(xform.ContextID, InputFormat, PackFlags.Ushort).Fmt16;
        var ToOutput = _cmsGetFormatterOut(xform.ContextID, OutputFormat, PackFlags.Ushort).Fmt16;

        if (FromInput is null || ToOutput is null)
        {
            Context.LogError(xform.ContextID, cmsERROR_UNKNOWN_EXTENSION, "Unsupported raster format");
            return false;
        }

        xform.InputFormat = InputFormat;
        xform.OutputFormat = OutputFormat;
        xform.FromInput = FromInput;
        xform.ToOutput = ToOutput;
        return true;
    }
}
