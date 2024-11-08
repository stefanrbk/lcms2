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

public delegate void StageEvalFn(ReadOnlySpan<float> In, Span<float> Out, Stage mpe);
public delegate object? StageDupElemFn(Stage mpe);
public delegate void StageFreeElemFn(Stage mpe);

public class Stage
{
    public Context? ContextID;
    public Signature Type;
    public Signature Implements;
    public uint InputChannels;
    public uint OutputChannels;

    public StageEvalFn EvalPtr;
    public StageDupElemFn? DupElemPtr;
    public StageFreeElemFn? FreePtr;

    public object? Data;

    public Stage? Next;

    internal const byte MaxChannels = 128;
    internal const byte MaxInputDimensions = 15;

    public Stage(Context? ContextID,
                 Signature Type,
                 uint InputChannels,
                 uint OutputChannels,
                 StageEvalFn EvalPtr,
                 StageDupElemFn? DupElemPtr,
                 StageFreeElemFn? FreePtr,
                 object? Data)  // _cmsStageAllocPlaceholder
    {
        this.ContextID = ContextID;

        this.Type = Type;
        Implements = Type;  // By default, no clue on what is implementing

        this.InputChannels = InputChannels;
        this.OutputChannels = OutputChannels;
        this.EvalPtr = EvalPtr;
        this.DupElemPtr = DupElemPtr;
        this.FreePtr = FreePtr;
        this.Data = Data;
    }
}
