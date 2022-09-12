﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2022 Marti Maria Saguer
//                2022      Stefan Kewatt
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
namespace lcms2.types;

public partial class Stage
{
    #region Public Methods

    public bool Sample(CLut16Data.Sampler sampler, in object? cargo, SamplerFlags flags) =>
        (Data as CLut16Data)?.Sample(sampler, cargo, flags) ?? throw new InvalidOperationException();

    #endregion Public Methods

    #region Classes

    public class CLut16Data : StageData
    {
        #region Fields

        public uint NumEntries;

        public InterpParams Params;

        public ushort[] Table;

        #endregion Fields

        #region Internal Constructors

        internal CLut16Data(ushort[] table, InterpParams @params, uint numEntries)
        {
            Table = table;
            Params = @params;
            NumEntries = numEntries;
        }

        #endregion Internal Constructors

        #region Delegates

        public delegate bool Sampler(ReadOnlySpan<ushort> @in, Span<ushort> @out, in object? cargo);

        #endregion Delegates

        #region Public Methods

        public static bool SliceSpace(uint numInputs, in uint[] clutPoints, Sampler sampler, in object cargo)
        {
            /** Original Code (cmslut.c line:868)
             **
             ** // This routine does a sweep on whole input space, and calls its callback
             ** // function on knots. returns TRUE if all ok, FALSE otherwise.
             ** cmsBool CMSEXPORT cmsSliceSpace16(cmsUInt32Number nInputs, const cmsUInt32Number clutPoints[],
             **                                          cmsSAMPLER16 Sampler, void * Cargo)
             ** {
             **     int i, t, rest;
             **     cmsUInt32Number nTotalPoints;
             **     cmsUInt16Number In[cmsMAXCHANNELS];
             **
             **     if (nInputs >= cmsMAXCHANNELS) return FALSE;
             **
             **     nTotalPoints = CubeSize(clutPoints, nInputs);
             **     if (nTotalPoints == 0) return FALSE;
             **
             **     for (i = 0; i < (int) nTotalPoints; i++) {
             **
             **         rest = i;
             **         for (t = (int) nInputs-1; t >=0; --t) {
             **
             **             cmsUInt32Number  Colorant = rest % clutPoints[t];
             **
             **             rest /= clutPoints[t];
             **             In[t] = _cmsQuantizeVal(Colorant, clutPoints[t]);
             **
             **         }
             **
             **         if (!Sampler(In, NULL, Cargo))
             **             return FALSE;
             **     }
             **
             **     return TRUE;
             ** }
             **/

            var @in = new ushort[maxChannels];

            if (numInputs >= maxChannels) return false;

            var numTotalPoints = CubeSize(clutPoints, (int)numInputs);
            if (numTotalPoints is 0) return false;

            for (var i = 0; i < numTotalPoints; i++)
            {
                var rest = i;
                for (var t = (int)numInputs - 1; t >= 0; --t)
                {
                    var colorant = rest % clutPoints[t];

                    rest /= (int)clutPoints[t];
                    @in[t] = QuantizeValue(colorant, clutPoints[t]);
                }

                if (!sampler(@in, null, in cargo)) return false;
            }

            return true;
        }

        public bool Sample(Sampler sampler, in object? cargo, SamplerFlags flags)
        {
            /** Original Code (cmslut.c line: 742)
             **
             ** // This routine does a sweep on whole input space, and calls its callback
             ** // function on knots. returns TRUE if all ok, FALSE otherwise.
             ** cmsBool CMSEXPORT cmsStageSampleCLut16bit(cmsStage* mpe, cmsSAMPLER16 Sampler, void * Cargo, cmsUInt32Number dwFlags)
             ** {
             **     int i, t, index, rest;
             **     cmsUInt32Number nTotalPoints;
             **     cmsUInt32Number nInputs, nOutputs;
             **     cmsUInt32Number* nSamples;
             **     cmsUInt16Number In[MAX_INPUT_DIMENSIONS+1], Out[MAX_STAGE_CHANNELS];
             **     _cmsStageCLutData* clut;
             **
             **     if (mpe == NULL) return FALSE;
             **
             **     clut = (_cmsStageCLutData*) mpe->Data;
             **
             **     if (clut == NULL) return FALSE;
             **
             **     nSamples = clut->Params ->nSamples;
             **     nInputs  = clut->Params ->nInputs;
             **     nOutputs = clut->Params ->nOutputs;
             **
             **     if (nInputs <= 0) return FALSE;
             **     if (nOutputs <= 0) return FALSE;
             **     if (nInputs > MAX_INPUT_DIMENSIONS) return FALSE;
             **     if (nOutputs >= MAX_STAGE_CHANNELS) return FALSE;
             **
             **     memset(In, 0, sizeof(In));
             **     memset(Out, 0, sizeof(Out));
             **
             **     nTotalPoints = CubeSize(nSamples, nInputs);
             **     if (nTotalPoints == 0) return FALSE;
             **
             **     index = 0;
             **     for (i = 0; i < (int) nTotalPoints; i++) {
             **
             **         rest = i;
             **         for (t = (int)nInputs - 1; t >= 0; --t) {
             **
             **             cmsUInt32Number  Colorant = rest % nSamples[t];
             **
             **             rest /= nSamples[t];
             **
             **             In[t] = _cmsQuantizeVal(Colorant, nSamples[t]);
             **         }
             **
             **         if (clut ->Tab.T != NULL) {
             **             for (t = 0; t < (int)nOutputs; t++)
             **                 Out[t] = clut->Tab.T[index + t];
             **         }
             **
             **         if (!Sampler(In, Out, Cargo))
             **             return FALSE;
             **
             **         if (!(dwFlags & SAMPLER_INSPECT)) {
             **
             **             if (clut ->Tab.T != NULL) {
             **                 for (t=0; t < (int) nOutputs; t++)
             **                     clut->Tab.T[index + t] = Out[t];
             **             }
             **         }
             **
             **         index += nOutputs;
             **     }
             **
             **     return TRUE;
             ** }
             **/

            var @in = new ushort[maxInputDimensions + 1];
            var @out = new ushort[maxStageChannels];

            var numSamples = Params.NumSamples;
            var numInputs = Params.NumInputs;
            var numOutputs = Params.NumOutputs;

            if ((numInputs is <= 0 or > maxInputDimensions) ||
                (numOutputs is <= 0 or > maxStageChannels))
            {
                return false;
            }

            var numTotalPoints = CubeSize(numSamples, (int)numInputs);
            if (numTotalPoints is 0) return false;

            var index = 0u;
            for (var i = 0; i < numTotalPoints; i++)
            {
                var rest = i;
                for (var t = (int)numInputs - 1; t >= 0; --t)
                {
                    var colorant = rest % numSamples[t];

                    rest /= (int)numSamples[t];

                    @in[t] = QuantizeValue(colorant, numSamples[t]);
                }

                for (var t = 0; t < numOutputs; t++)
                    @out[t] = Table[index + t];

                if (!sampler(@in, @out, in cargo))
                    return false;

                if (flags is not SamplerFlags.Inspect)
                {
                    for (var t = 0; t < numOutputs; t++)
                        Table[index + t] = @out[t];
                }

                index += numOutputs;
            }

            return true;
        }

        #endregion Public Methods

        #region Internal Methods

        internal override StageData? Duplicate(Stage parent)
        {
            /** Original Code (cmslut.c line: 481)
             **
             ** static
             ** void* CLUTElemDup(cmsStage* mpe)
             ** {
             **     _cmsStageCLutData* Data = (_cmsStageCLutData*) mpe ->Data;
             **     _cmsStageCLutData* NewElem;
             **
             **
             **     NewElem = (_cmsStageCLutData*) _cmsMallocZero(mpe ->ContextID, sizeof(_cmsStageCLutData));
             **     if (NewElem == NULL) return NULL;
             **
             **     NewElem ->nEntries       = Data ->nEntries;
             **     NewElem ->HasFloatValues = Data ->HasFloatValues;
             **
             **     if (Data ->Tab.T) {
             **
             **         if (Data ->HasFloatValues) {
             **             NewElem ->Tab.TFloat = (cmsFloat32Number*) _cmsDupMem(mpe ->ContextID, Data ->Tab.TFloat, Data ->nEntries * sizeof *(cmsFloat32Number));
             **             if (NewElem ->Tab.TFloat == NULL)
             **                 goto Error;
             **         } else {
             **             NewElem ->Tab.T = (cmsUInt16Number*) _cmsDupMem(mpe ->ContextID, Data ->Tab.T, Data ->nEntries * sizeof *(cmsUInt16Number));
             **             if (NewElem ->Tab.T == NULL)
             **                 goto Error;
             **         }
             **     }
             **
             **     NewElem ->Params   = _cmsComputeInterpParamsEx(mpe ->ContextID,
             **                                                    Data ->Params ->nSamples,
             **                                                    Data ->Params ->nInputs,
             **                                                    Data ->Params ->nOutputs,
             **                                                    NewElem ->Tab.T,
             **                                                    Data ->Params ->dwFlags);
             **     if (NewElem->Params != NULL)
             **         return (void*) NewElem;
             **  Error:
             **     if (NewElem->Tab.T)
             **         // This works for both types
             **         _cmsFree(mpe ->ContextID, NewElem -> Tab.T);
             **     _cmsFree(mpe ->ContextID, NewElem);
             **     return NULL;
             ** }
             **/

            var p =
                InterpParams.Compute(
                    parent.StateContainer,
                    Params.NumSamples,
                    Params.NumInputs,
                    Params.NumOutputs,
                    Table,
                    Params.Flags);

            if (p is null) return null;

            return
                new CLut16Data(
                    (ushort[])Table.Clone(),
                    p,
                    NumEntries);
        }

        internal override void Evaluate(ReadOnlySpan<float> @in, Span<float> @out, Stage parent)
        {
            /** Original Code (cmslut.c line: 443)
             **
             ** // Convert to 16 bits, evaluate, and back to floating point
             ** static
             ** void EvaluateCLUTfloatIn16(const cmsFloat32Number In[], cmsFloat32Number Out[], const cmsStage *mpe)
             ** {
             **     _cmsStageCLutData* Data = (_cmsStageCLutData*) mpe ->Data;
             **     cmsUInt16Number In16[MAX_STAGE_CHANNELS], Out16[MAX_STAGE_CHANNELS];
             **
             **     _cmsAssert(mpe ->InputChannels  <= MAX_STAGE_CHANNELS);
             **     _cmsAssert(mpe ->OutputChannels <= MAX_STAGE_CHANNELS);
             **
             **     FromFloatTo16(In, In16, mpe ->InputChannels);
             **     Data -> Params ->Interpolation.Lerp16(In16, Out16, Data->Params);
             **     From16ToFloat(Out16, Out,  mpe ->OutputChannels);
             ** }
             **/

            var in16 = new ushort[maxStageChannels];
            var out16 = new ushort[maxStageChannels];

            FromFloatTo16(@in, in16, (int)parent.InputChannels);
            Params.Lerp(in16, out16);
            From16ToFloat(out16, @out, (int)parent.OutputChannels);
        }

        #endregion Internal Methods
    }

    #endregion Classes
}