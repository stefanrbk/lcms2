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

using lcms2.state;
using lcms2.types;

namespace lcms2.ThreadedPlugin;
public static partial class Threaded
{
    internal unsafe static void _cmsThrScheduler(Transform CMMcargo,
                                          ReadOnlySpan<byte> InputBuffer,
                                          Span<byte> OutputBuffer,
                                          uint PixelsPerLine,
                                          uint LineCount,
                                          Stride Stride)
    {
        var ContextID = cmsGetTransformContextID(CMMcargo);
        var worker = CMMcargo.Worker;
        var MaxWorkers = CMMcargo.MaxWorkers;

        // flags are not actually being used
        // uint flags = _cmsGetTransformWorkerFlags(CMMcargo);

        var FixedStride = Stride;

        // Count the number of threads needed for this job. MaxWorkers is the upper limit or -1 to auto
        var nSlices = _cmsThrCountSlices(CMMcargo, MaxWorkers, PixelsPerLine, LineCount, FixedStride);

        // Abort early if no threaded code
        if (nSlices <= 1)
        {
            worker?.Invoke(CMMcargo, InputBuffer, OutputBuffer, PixelsPerLine, LineCount, Stride);
            return;
        }

        fixed (byte* inBuf = InputBuffer)
        {
            fixed (byte* outBuf = OutputBuffer)
            {
                // Setup master thread
                var master = new WorkSlice
                {
                    CMMcargo = CMMcargo,
                    InputBuffer = inBuf,
                    OutputBuffer = outBuf,
                    PixelsPerLine = PixelsPerLine,
                    LineCount = LineCount,
                    Stride = FixedStride
                };

                // Create memory for the slices
                //var slices = Context.GetPool<WorkSlice>(ContextID).Rent((int)nSlices);
                //var handles = Context.GetPool<Task>(ContextID).Rent((int)nSlices);
                var slices = new WorkSlice[nSlices];
                var handles = new Task[nSlices];

                // slices and handles cannot be null, so not implementing the failure path!

                // All seems ok so far
                if (_cmsThrSplitWork(master, (int)nSlices, slices))
                {
                    // Work is split. Create threads
                    for (var i = 1; i < nSlices; i++)
                    {
                        handles[i] = _cmsThrCreateWorker(ContextID, worker, slices[i]);
                    }

                    // Do our portion of work
                    worker(
                        CMMcargo,
                        new(slices[0].InputBuffer, slices[0].InputBufferLength),
                        new(slices[0].OutputBuffer, slices[0].OutputBufferLength),
                        slices[0].PixelsPerLine,
                        slices[0].LineCount,
                        slices[0].Stride);

                    // Wait until all threads are finished
                    for (var i = 1; i < nSlices; i++)
                    {
                        _cmsThrJoinWorker(ContextID, handles[i]);
                    }
                }
                else
                {
                    // Not able to split the work, so don't thread
                    worker(CMMcargo, InputBuffer, OutputBuffer, PixelsPerLine, LineCount, Stride);
                }

                //ReturnArray(ContextID, slices);
                //ReturnArray(ContextID, handles);
            }
        }
    }
}
