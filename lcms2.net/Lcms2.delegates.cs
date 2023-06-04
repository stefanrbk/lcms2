﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2022 Marti Maria Saguer
//                2022-2023 Stefan Kewatt
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
//

using lcms2.types;

namespace lcms2;

public static unsafe partial class Lcms2
{
    public delegate void FreeUserDataFn(Context ContextID, void* Data);
    public delegate void* DupUserDataFn(Context ContextID, in void* Data);
    public delegate void* MallocFnPtrType(Context ContextID, uint size);
    public delegate void FreeFnPtrType(Context ContextID, void* Ptr);
    public delegate void* ReallocFnPtrType(Context ContextID, void* Ptr, uint NewSize);
    public delegate void* MallocZerocFnPtrType(Context ContextID, uint size);
    public delegate void* CallocFnPtrType(Context ContextID, uint num, uint size);
    public delegate void* DupFnPtrType(Context ContextID, in void* Org, uint size);
    public delegate void InterpFn16(in ushort* Input, ushort* Output, in InterpParams* p);
    public delegate void InterpFnFloat(in float* Input, float* Output, in InterpParams* p);
    public delegate InterpFunction InterpFnFactory(uint nInputChannels, uint nOutputChannels, uint dwFlags);
    public delegate double ParametricCurveEvaluator(int Type, in double* Params, double R);
    public delegate byte* Formatter16(Transform* CMMcargo, ushort* Values, byte* Buffer, uint Stride);
    public delegate byte* FormatterFloat(Transform* CMMcargo, float* Values, byte* Buffer, uint Stride);
    public delegate Formatter FormatterFactory(uint Type, FormatterDirection Dir, uint dwFlags);
    public delegate Pipeline* IntentFn(Context ContextID, uint nProfiles, uint* Intents, HPROFILE* hProfiles, bool* BPC, double* AdaptationStates, uint dwFlags);
    public delegate void StageEvalFn(in float* In, float* Out, in Stage* mpe);
    public delegate void* StageDupElemFn(Stage* mpe);
    public delegate void StageFreeElemFn(Stage* mpe);
    public delegate bool OPToptimizeFn(Pipeline** Lut, uint Intent, uint* InputFormat, uint* OutputFormat, uint* dwFlags);
    public delegate void PipelineEval16Fn(in ushort* In, ushort* Out, in void* Data);
    public delegate void PipelineEvalFloatFn(in float* In, float* Out, in void* Data);
    public delegate void TransformFn(Transform* CMMcargo, in void* InputBuffer, void* OutputBuffer, uint Size, uint Stride);
    public delegate void Transform2Fn(Transform* CMMcargo, in void* InputBuffer, void* OutputBuffer, uint PixelsPerLine, uint LineCount, in Stride* Stride);
    public delegate bool TransformFactory(TransformFn xform, void** UserData, FreeUserDataFn? FreePrivateDataFn, Pipeline** Lut, uint* InputFormat, uint* OutputFormat, uint* dwFlags);
    public delegate bool Transform2Factory(Transform2Fn xform, void** UserData, FreeUserDataFn? FreePrivateDataFn, Pipeline** Lut, uint* InputFormat, uint* OutputFormat, uint* dwFlags);
    public delegate void* CreateMutexFnPtrType(Context ContextID);
    public delegate void DestroyMutexFnPtrType(Context ContextID, void* mtx);
    public delegate bool LockMutexFnPtrType(Context ContextID, void* mtx);
    public delegate void UnlockMutexFnPtrType(Context ContextID, void* mtx);
    public delegate void LogErrorHandlerFunction(Context ContextID, ErrorCode ErrorCode, string Text);
    public delegate bool SAMPLER16(in ushort* In, ushort* Out, void* Cargo);
    public delegate bool SAMPLERFLOAT(in float* In, float* Out, void* Cargo);

    internal delegate void FormatterAlphaFn(void* dst, in void* src);
}