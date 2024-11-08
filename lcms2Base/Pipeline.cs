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

namespace lcms2;

public delegate void PipelineEval16Fn(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Data);
public delegate void PipelineEvalFloatFn(ReadOnlySpan<float> In, Span<float> Out, object? Data);

public class Pipeline(Context? contextID,
                      uint inputChans,
                      uint outputChans,
                      PipelineEval16Fn eval16,
                      PipelineEvalFloatFn evalFloat,
                      object? data = null)
{
    internal Stage? Elements;
    internal uint InputChannels = inputChans, OutputChannels = outputChans;

    internal object? Data = data;

    internal PipelineEval16Fn Eval16Fn = eval16;
    internal PipelineEvalFloatFn EvalFloatFn = evalFloat;
    internal FreeUserDataFn? FreeDataFn;
    internal DupUserDataFn? DupDataFn;

    internal Context? ContextID = contextID;
    internal bool SaveAs8Bits;

    // This function may be used to set the optional evaluator and a block of private data. If private data is being used, an optional
    // duplicator and free functions should also be specified in order to duplicate the LUT construct. Use NULL to inhibit such functionality.
    public void SetOptimizationParameters(PipelineEval16Fn Eval16,
                                          object? PrivateData,
                                          FreeUserDataFn? FreePrivateDataFn,
                                          DupUserDataFn? DupPrivateDataFn)  // _cmsPipelineSetOptimizationParameters
    {
        Eval16Fn = Eval16;
        DupDataFn = DupPrivateDataFn;
        FreeDataFn = FreePrivateDataFn;
        Data = PrivateData;
    }
}
