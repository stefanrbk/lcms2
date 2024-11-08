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

using System.Runtime.CompilerServices;

namespace lcms2.ThreadedPlugin;

public static partial class Threaded
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComponentSize(uint format)
    {
        var BytesPerComponent = T_BYTES(format);

        // For double, the T_BYTES field is zero
        if (BytesPerComponent is 0)
            BytesPerComponent = sizeof(ulong);

        return (uint)BytesPerComponent;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint PixelSpacing(uint format) =>
        T_PLANAR(format) is not 0
            ? ComponentSize(format)
            : ComponentSize(format) * (uint)(T_CHANNELS(format) + T_EXTRA(format));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint minimum(uint a, uint b) =>
        Math.Min(a, b);

    private static uint MemSize(uint format,
                                uint PixelsPerLine,
                                uint LineCount,
                                ref uint BytesPerLine,
                                uint BytesPerPlane)
    {
        if (T_PLANAR(format) is not 0)
        {
            if (BytesPerLine is 0)
                BytesPerLine = ComponentSize(format) * PixelsPerLine;

            return (uint)(T_CHANNELS(format) + T_EXTRA(format)) * BytesPerPlane;
        }
        else
        {
            if (BytesPerLine is 0)
                BytesPerLine = ComponentSize(format) * (uint)(T_CHANNELS(format) + T_EXTRA(format)) * PixelsPerLine;

            return LineCount * BytesPerLine;
        }
    }

    internal static uint _cmsThrCountSlices(Transform CMMcargo,
                                            int MaxWorkers,
                                            uint PixelsPerLine,
                                            uint LineCount,
                                            Stride Stride)
    {
        var MaxCPUs = _cmsThrIdealThreadCount();

        if (MaxWorkers == CMS_THREADED_GUESS_MAX_THREADS)
        {
            MaxWorkers = MaxCPUs;
        }
        else
        {
            // We allow large number of threads, but this is not going to work well. Warn it.
            if (MaxWorkers > MaxCPUs)
            {
                Context.LogError(
                    null,
                    cmsERROR_RANGE,
                    "Warning: too many threads for actual processor (CPUs={0}, asked={1})",
                    MaxCPUs,
                    MaxWorkers);
            }
        }

        var MaxInputMem = MemSize(
            cmsGetTransformInputFormat(CMMcargo),
            PixelsPerLine,
            LineCount,
            ref Stride.BytesPerLineIn,
            Stride.BytesPerPlaneIn);

        var MaxOutputMem = MemSize(
            cmsGetTransformOutputFormat(CMMcargo),
            PixelsPerLine,
            LineCount,
            ref Stride.BytesPerLineOut,
            Stride.BytesPerPlaneOut);

        // Each thread takes 128k at least
        var WorkerCount = (MaxInputMem + MaxOutputMem) / (128 * 1024);

        if (WorkerCount < 1)
            WorkerCount = 1;
        else if (WorkerCount > MaxWorkers)
            WorkerCount = (uint)MaxWorkers;

        return WorkerCount;
    }

    private unsafe static void SlicePerLines(WorkSlice master, int nslices, int LinesPerSlice, Span<WorkSlice> slices)
    {
        var TotalLines = master.LineCount;

        for (var i = 0; i < nslices; i++)
        {
            var PtrInput = master.InputBuffer;
            var PtrOutput = master.OutputBuffer;

            var lines = minimum((uint)LinesPerSlice, TotalLines);

            slices[i] = (WorkSlice)master.Clone();

            slices[i].InputBufferLength = LinesPerSlice * (int)master.Stride.BytesPerLineIn;
            slices[i].OutputBufferLength = LinesPerSlice * (int)master.Stride.BytesPerLineOut;

            slices[i].InputBuffer = PtrInput + (i * slices[i].InputBufferLength);
            slices[i].OutputBuffer = PtrOutput + (i * slices[i].OutputBufferLength);

            slices[i].LineCount = lines;
            TotalLines -= lines;
        }

        // Add left lines because rounding
        if (!slices.IsEmpty)
        {
            slices[nslices - 1].LineCount += TotalLines;
            slices[nslices - 1].InputBufferLength += (int)(TotalLines * master.Stride.BytesPerLineIn);
            slices[nslices - 1].OutputBufferLength += (int)(TotalLines * master.Stride.BytesPerLineOut);
        }
    }

    private unsafe static void SlicePerPixels(WorkSlice master,
                                              int nslices,
                                              int PixelsPerSlice,
                                              Span<WorkSlice> slices)
    {
        var TotalPixels = master.PixelsPerLine; // As this works on one line only

        var PixelSpacingIn = PixelSpacing(cmsGetTransformInputFormat(master.CMMcargo));
        var PixelSpacingOut = PixelSpacing(cmsGetTransformOutputFormat(master.CMMcargo));

        for (var i = 0; i < nslices; i++)
        {
            var PtrInput = master.InputBuffer;
            var PtrOutput = master.OutputBuffer;

            var pixels = minimum((uint)PixelsPerSlice, TotalPixels);

            slices[i] = (WorkSlice)master.Clone();

            slices[i].InputBufferLength = PixelsPerSlice * (int)PixelSpacingIn;
            slices[i].OutputBufferLength = PixelsPerSlice * (int)PixelSpacingOut;

            slices[i].InputBuffer = PtrInput + (i * slices[i].InputBufferLength);
            slices[i].OutputBuffer = PtrOutput + (i * slices[i].OutputBufferLength);
            slices[i].PixelsPerLine = pixels;

            TotalPixels -= pixels;
        }

        // Add left pixels because rounding
        if (!slices.IsEmpty)
        {
            slices[nslices - 1].PixelsPerLine += TotalPixels;
            slices[nslices - 1].InputBufferLength += (int)(TotalPixels * PixelSpacingIn);
            slices[nslices - 1].OutputBufferLength += (int)(TotalPixels * PixelSpacingOut);
        }
    }

    internal static bool _cmsThrSplitWork(WorkSlice master, int nslices, Span<WorkSlice> slices)
    {
        // Check parameters
        if (master.PixelsPerLine is 0 ||
            master.Stride.BytesPerLineIn is 0 ||
            master.Stride.BytesPerLineOut is 0)
            return false;

        // Do the splitting depending on lines
        if (master.LineCount <= 1)
        {
            var PixelsPerWorker = (int)master.PixelsPerLine / nslices;

            if (PixelsPerWorker <= 0)
                return false;
            else
                SlicePerPixels(master, nslices, PixelsPerWorker, slices);
        }
        else
        {
            var LinesPerWorker = (int)master.LineCount / nslices;

            if (LinesPerWorker <= 0)
                return false;
            else
                SlicePerLines(master, nslices, LinesPerWorker, slices);
        }

        return true;
    }
}
