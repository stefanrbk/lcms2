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

using lcms2.io;
using lcms2.state;
using lcms2.types;

using Microsoft.Extensions.Logging;

namespace lcms2;

public static partial class Lcms2
{
    //public delegate void FreeUserDataFn(Context? ContextID, void* Data);
    //public delegate void* DupUserDataFn(Context? ContextID, in void* Data);
    public delegate void FreeManagedUserDataFn(Context? ContextID, object? Data);
    public delegate object? DupManagedUserDataFn(Context? ContextID, object? Data);
    //public delegate void* MallocFnPtrType(Context? ContextID, uint size, Type type);
    //public delegate void FreeFnPtrType(Context? ContextID, void* Ptr);
    //public delegate void* ReallocFnPtrType(Context? ContextID, void* Ptr, uint NewSize);
    //public delegate void* MallocZerocFnPtrType(Context? ContextID, uint size, Type type);
    //public delegate void* CallocFnPtrType(Context? ContextID, uint num, uint size, Type type);
    //public delegate void* DupFnPtrType(Context? ContextID, in void* Org, uint size, Type type);
    public delegate void InterpFn<T>(ReadOnlySpan<T> Input, Span<T> Output, InterpParams<T> p);
    public delegate InterpFunction? InterpFnFactory(uint nInputChannels, uint nOutputChannels, uint dwFlags);
    public delegate double ParametricCurveEvaluator(int Type, ReadOnlySpan<double> Params, double R);
    public delegate ReadOnlySpan<byte> Formatter16In(Transform CMMcargo, Span<ushort> Values, ReadOnlySpan<byte> Buffer, uint Stride);
    public delegate ReadOnlySpan<byte> FormatterFloatIn(Transform CMMcargo, Span<float> Values, ReadOnlySpan<byte> Buffer, uint Stride);
    public delegate Span<byte> Formatter16Out(Transform CMMcargo, ReadOnlySpan<ushort> Values, Span<byte> Buffer, uint Stride);
    public delegate Span<byte> FormatterFloatOut(Transform CMMcargo, ReadOnlySpan<float> Values, Span<byte> Buffer, uint Stride);
    public delegate FormatterIn FormatterFactoryIn(uint Type, uint dwFlags);
    public delegate FormatterOut FormatterFactoryOut(uint Type, uint dwFlags);
    public delegate Pipeline? IntentFn(Context? ContextID, uint nProfiles, ReadOnlySpan<uint> Intents, Profile[] Profiles, ReadOnlySpan<bool> BPC, ReadOnlySpan<double> AdaptationStates, uint dwFlags);
    public delegate void StageEvalFn(ReadOnlySpan<float> In, Span<float> Out, Stage mpe);
    public delegate object? StageDupElemFn(Stage mpe);
    public delegate void StageFreeElemFn(Stage mpe);
    public delegate bool OPToptimizeFn(ref Pipeline Lut, uint Intent, ref uint InputFormat, ref uint OutputFormat, ref uint dwFlags);
    public delegate void PipelineEval16Fn(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Data);
    public delegate void PipelineEvalFloatFn(ReadOnlySpan<float> In, Span<float> Out, object? Data);
    public delegate void TransformFn(Transform CMMcargo, ReadOnlySpan<byte> InputBuffer, Span<byte> OutputBuffer, uint Size, uint Stride);
    public delegate void Transform2Fn(Transform CMMcargo, ReadOnlySpan<byte> InputBuffer, Span<byte> OutputBuffer, uint PixelsPerLine, uint LineCount, Stride Stride);
    public delegate bool TransformFactory(out TransformFn xform, out object? UserData, out FreeManagedUserDataFn? FreePrivateDataFn, ref Pipeline Lut, ref uint InputFormat, ref uint OutputFormat, ref uint dwFlags);
    public delegate bool Transform2Factory(out Transform2Fn xform, out object? UserData, out FreeManagedUserDataFn? FreePrivateDataFn, ref Pipeline Lut, ref uint InputFormat, ref uint OutputFormat, ref uint dwFlags);
    public delegate object? CreateMutexFnPtrType(Context? ContextID);
    public delegate void DestroyMutexFnPtrType(Context? ContextID, object? mtx);
    public delegate bool LockMutexFnPtrType(Context? ContextID, object? mtx);
    public delegate void UnlockMutexFnPtrType(Context? ContextID, object? mtx);
    public delegate void LogErrorHandlerFunction(Context? ContextID, EventId ErrorCode, string Text);
    public delegate bool SAMPLER16(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo);
    public delegate bool SAMPLERFLOAT(ReadOnlySpan<float> In, Span<float> Out, object? Cargo);
    internal delegate bool PositionTableEntryFn(TagTypeHandler self, IOHandler io, object? Cargo, uint n, uint SizeOfTag);

    internal delegate void FormatterAlphaFn(Span<byte> dst, ReadOnlySpan<byte> src);
}
