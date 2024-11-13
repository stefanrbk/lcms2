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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using lcms2.stages;
using lcms2.types;

namespace lcms2;

public static partial class Lcms2
{
    private const uint cmsCorbisBrokenXYZtype = 0x17A505B8;
    private const uint cmsMonacoBrokenCurveType = 0x9478EE00;

    internal static readonly TagTypeLinkedList supportedMPEtypes = new(
        new(Signatures.Stage.BAcsElem, null, null, null, null, null, 0),
        new(Signatures.Stage.EAcsElem, null, null, null, null, null, 0),
        new(
            Signatures.Stage.CurveSetElem,
            Type_MPEcurve_Read,
            Type_MPEcurve_Write,
            GenericMPEdup,
            GenericMPEfree,
            null,
            0),
        new(
            Signatures.Stage.MatrixElem,
            Type_MPEmatrix_Read,
            Type_MPEmatrix_Write,
            GenericMPEdup,
            GenericMPEfree,
            null,
            0),
        new(Signatures.Stage.CLutElem, Type_MPEclut_Read, Type_MPEclut_Write, GenericMPEdup, GenericMPEfree, null, 0));

    internal static readonly TagTypePluginChunkType MPETypePluginChunk = new();

    internal static readonly TagTypePluginChunkType globalMPETypePluginChunk = new();

    internal static readonly TagTypeLinkedList supportedTagTypes = new(
        new(
            Signatures.TagType.Chromaticity,
            Type_Chromaticity_Read,
            Type_Chromaticity_Write,
            Type_Chromaticity_Dup,
            null,
            null,
            0),
        new(
            Signatures.TagType.ColorantOrder,
            Type_ColorantOrderType_Read,
            Type_ColorantOrderType_Write,
            Type_ColorantOrderType_Dup,
            Type_ColorantOrderType_Free,
            null,
            0),
        new(
            Signatures.TagType.S15Fixed16Array,
            Type_S15Fixed16_Read,
            Type_S15Fixed16_Write,
            Type_S15Fixed16_Dup,
            Type_S15Fixed16_Free,
            null,
            0),
        new(
            Signatures.TagType.U16Fixed16Array,
            Type_U16Fixed16_Read,
            Type_U16Fixed16_Write,
            Type_U16Fixed16_Dup,
            Type_U16Fixed16_Free,
            null,
            0),
        new(Signatures.TagType.Text, Type_Text_Read, Type_Text_Write, Type_Text_Dup, Type_Text_Free, null, 0),
        new(
            Signatures.TagType.TextDescription,
            Type_Text_Description_Read,
            Type_Text_Description_Write,
            Type_Text_Description_Dup,
            Type_Text_Description_Free,
            null,
            0),
        new(Signatures.TagType.Curve, Type_Curve_Read, Type_Curve_Write, ToneCurve_Dup, ToneCurve_Free, null, 0),
        new(
            Signatures.TagType.ParametricCurve,
            Type_ParametricCurve_Read,
            Type_ParametricCurve_Write,
            ToneCurve_Dup,
            ToneCurve_Free,
            null,
            0),
        new(Signatures.TagType.DateTime, Type_DateTime_Read, Type_DateTime_Write, Type_DateTime_Dup, null, null, 0),
        new(Signatures.TagType.Lut8, Type_LUT8_Read, Type_LUT8_Write, Type_LUT8_Dup, Type_LUT8_Free, null, 0),
        new(Signatures.TagType.Lut16, Type_LUT16_Read, Type_LUT16_Write, Type_LUT16_Dup, Type_LUT16_Free, null, 0),
        new(
            Signatures.TagType.ColorantTable,
            Type_ColorantTable_Read,
            Type_ColorantTable_Write,
            NamedColor_Dup,
            NamedColor_Free,
            null,
            0),
        new(
            Signatures.TagType.NamedColor2,
            Type_NamedColor_Read,
            Type_NamedColor_Write,
            NamedColor_Dup,
            NamedColor_Free,
            null,
            0),
        new(
            Signatures.TagType.MultiLocalizedUnicode,
            Type_MLU_Read,
            Type_MLU_Write,
            Type_MLU_Dup,
            Type_MLU_Free,
            null,
            0),
        new(
            Signatures.TagType.ProfileSequenceDesc,
            Type_ProfileSequenceDesc_Read,
            Type_ProfileSequenceDesc_Write,
            Sequence_Dup,
            Sequence_Free,
            null,
            0),
        new(Signatures.TagType.Signature, Type_Signature_Read, Type_Signature_Write, Type_Signature_Dup, null, null, 0),
        new(
            Signatures.TagType.Measurement,
            Type_Measurement_Read,
            Type_Measurement_Write,
            Type_Measurement_Dup,
            null,
            null,
            0),
        new(Signatures.TagType.Data, Type_Data_Read, Type_Data_Write, Type_Data_Dup, Type_Data_Free, null, 0),
        new(
            Signatures.TagType.LutAtoB,
            Type_LUTA2B_Read,
            Type_LUTA2B_Write,
            Type_LUTA2B_Dup,
            Type_LUTA2B_Free,
            null,
            0),
        new(
            Signatures.TagType.LutBtoA,
            Type_LUTB2A_Read,
            Type_LUTB2A_Write,
            Type_LUTB2A_Dup,
            Type_LUTB2A_Free,
            null,
            0),
        new(Signatures.TagType.UcrBg, Type_UcrBg_Read, Type_UcrBg_Write, Type_UcrBg_Dup, Type_UcrBg_Free, null, 0),
        new(
            Signatures.TagType.CrdInfo,
            Type_CrdInfo_Read,
            Type_CrdInfo_Write,
            Type_CrdInfo_Dup,
            Type_CrdInfo_Free,
            null,
            0),
        new(
            Signatures.TagType.MultiProcessElement,
            Type_MPE_Read,
            Type_MPE_Write,
            Type_MPE_Dup,
            Type_MPE_Free,
            null,
            0),
        new(
            Signatures.TagType.Screening,
            Type_Screening_Read,
            Type_Screening_Write,
            Type_Screening_Dup,
            Type_Screening_Free,
            null,
            0),
        new(
            Signatures.TagType.ViewingConditions,
            Type_ViewingConditions_Read,
            Type_ViewingConditions_Write,
            Type_ViewingConditions_Dup,
            null,
            null,
            0),
        new(Signatures.TagType.XYZ, Type_XYZ_Read, Type_XYZ_Write, Type_XYZ_Dup, null, null, 0),
        new(Signatures.TagType.CorbisBrokenXYZ, Type_XYZ_Read, Type_XYZ_Write, Type_XYZ_Dup, null, null, 0),
        new(
            Signatures.TagType.MonacoBrokenCurve,
            Type_Curve_Read,
            Type_Curve_Write,
            ToneCurve_Dup,
            ToneCurve_Free,
            null,
            0),
        new(
            Signatures.TagType.ProfileSequenceId,
            Type_ProfileSequenceId_Read,
            Type_ProfileSequenceId_Write,
            Sequence_Dup,
            Sequence_Free,
            null,
            0),
        new(
            Signatures.TagType.Dict,
            Type_Dictionary_Read,
            Type_Dictionary_Write,
            Type_Dictionary_Dup,
            Type_Dictionary_Free,
            null,
            0),
        new(
            Signatures.TagType.cicp,
            Type_VideoSignal_Read,
            Type_VideoSignal_Write,
            Type_VideoSignal_Dup,
            null,
            null,
            0),
        new(Signatures.TagType.Vcgt, Type_vcgt_Read, Type_vcgt_Write, Type_vcgt_Dup, Type_vcgt_Free, null, 0),
        new(Signatures.TagType.MHC2, Type_MHC2_Read, Type_MHC2_Write, Type_MHC2_Dup, Type_MHC2_Free, null, 0));

    internal static readonly TagTypePluginChunkType TagTypePluginChunk = new();

    internal static readonly TagTypePluginChunkType globalTagTypePluginChunk = new();

    internal static readonly TagLinkedList supportedTags = new(
        new(
            Signatures.Tag.AToB0,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutAtoB, Signatures.TagType.Lut8, },
            DecideLUTtypeA2B),
        new(
            Signatures.Tag.AToB1,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutAtoB, Signatures.TagType.Lut8, },
            DecideLUTtypeA2B),
        new(
            Signatures.Tag.AToB2,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutAtoB, Signatures.TagType.Lut8, },
            DecideLUTtypeA2B),
        new(
            Signatures.Tag.BToA0,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutBtoA, Signatures.TagType.Lut8, },
            DecideLUTtypeB2A),
        new(
            Signatures.Tag.BToA1,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutBtoA, Signatures.TagType.Lut8, },
            DecideLUTtypeB2A),
        new(
            Signatures.Tag.BToA2,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutBtoA, Signatures.TagType.Lut8, },
            DecideLUTtypeB2A),
        new(
            Signatures.Tag.RedColorant,
            1,
            new Signature[] { Signatures.TagType.XYZ, Signatures.TagType.CorbisBrokenXYZ, },
            DecideXYZtype),
        new(
            Signatures.Tag.GreenColorant,
            1,
            new Signature[] { Signatures.TagType.XYZ, Signatures.TagType.CorbisBrokenXYZ, },
            DecideXYZtype),
        new(
            Signatures.Tag.BlueColorant,
            1,
            new Signature[] { Signatures.TagType.XYZ, Signatures.TagType.CorbisBrokenXYZ, },
            DecideXYZtype),
        new(
            Signatures.Tag.RedTRC,
            1,
            new Signature[]
            {
                Signatures.TagType.Curve, Signatures.TagType.ParametricCurve, Signatures.TagType.MonacoBrokenCurve,
            },
            DecideCurveType),
        new(
            Signatures.Tag.GreenTRC,
            1,
            new Signature[]
            {
                Signatures.TagType.Curve, Signatures.TagType.ParametricCurve, Signatures.TagType.MonacoBrokenCurve,
            },
            DecideCurveType),
        new(
            Signatures.Tag.BlueTRC,
            1,
            new Signature[]
            {
                Signatures.TagType.Curve, Signatures.TagType.ParametricCurve, Signatures.TagType.MonacoBrokenCurve,
            },
            DecideCurveType),
        new(Signatures.Tag.CalibrationDateTime, 1, new Signature[] { Signatures.TagType.DateTime, }, null),
        new(Signatures.Tag.CharTarget, 1, new Signature[] { Signatures.TagType.Text, }, null),
        new(Signatures.Tag.ChromaticAdaptation, 9, new Signature[] { Signatures.TagType.S15Fixed16Array, }, null),
        new(Signatures.Tag.Chromaticity, 1, new Signature[] { Signatures.TagType.Chromaticity, }, null),
        new(Signatures.Tag.ColorantOrder, 1, new Signature[] { Signatures.TagType.ColorantOrder, }, null),
        new(Signatures.Tag.ColorantTable, 1, new Signature[] { Signatures.TagType.ColorantTable, }, null),
        new(Signatures.Tag.ColorantTableOut, 1, new Signature[] { Signatures.TagType.ColorantTable, }, null),
        new(
            Signatures.Tag.Copyright,
            1,
            new Signature[]
            {
                Signatures.TagType.Text,
                Signatures.TagType.MultiLocalizedUnicode,
                Signatures.TagType.TextDescription,
            },
            DecideTextType),
        new(Signatures.Tag.DateTime, 1, new Signature[] { Signatures.TagType.DateTime, }, null),
        new(
            Signatures.Tag.DeviceMfgDesc,
            1,
            new Signature[]
            {
                Signatures.TagType.TextDescription,
                Signatures.TagType.MultiLocalizedUnicode,
                Signatures.TagType.Text,
            },
            DecideTextDescType),
        new(
            Signatures.Tag.DeviceModelDesc,
            1,
            new Signature[]
            {
                Signatures.TagType.TextDescription,
                Signatures.TagType.MultiLocalizedUnicode,
                Signatures.TagType.Text,
            },
            DecideTextDescType),
        new(
            Signatures.Tag.Gamut,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutBtoA, Signatures.TagType.Lut8, },
            DecideLUTtypeB2A),
        new(
            Signatures.Tag.GrayTRC,
            1,
            new Signature[] { Signatures.TagType.Curve, Signatures.TagType.ParametricCurve, },
            DecideCurveType),
        new(Signatures.Tag.Luminance, 1, new Signature[] { Signatures.TagType.XYZ, }, null),
        new(
            Signatures.Tag.MediaBlackPoint,
            1,
            new Signature[] { Signatures.TagType.XYZ, Signatures.TagType.CorbisBrokenXYZ, },
            null),
        new(
            Signatures.Tag.MediaWhitePoint,
            1,
            new Signature[] { Signatures.TagType.XYZ, Signatures.TagType.CorbisBrokenXYZ, },
            null),
        new(Signatures.Tag.NamedColor2, 1, new Signature[] { Signatures.TagType.NamedColor2, }, null),
        new(
            Signatures.Tag.Preview0,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutBtoA, Signatures.TagType.Lut8, },
            DecideLUTtypeB2A),
        new(
            Signatures.Tag.Preview1,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutBtoA, Signatures.TagType.Lut8, },
            DecideLUTtypeB2A),
        new(
            Signatures.Tag.Preview2,
            1,
            new Signature[] { Signatures.TagType.Lut16, Signatures.TagType.LutBtoA, Signatures.TagType.Lut8, },
            DecideLUTtypeB2A),
        new(
            Signatures.Tag.ProfileDescription,
            1,
            new Signature[]
            {
                Signatures.TagType.TextDescription,
                Signatures.TagType.MultiLocalizedUnicode,
                Signatures.TagType.Text,
            },
            DecideTextDescType),
        new(Signatures.Tag.ProfileSequenceDesc, 1, new Signature[] { Signatures.TagType.ProfileSequenceDesc, }, null),
        new(Signatures.Tag.Technology, 1, new Signature[] { Signatures.TagType.Signature, }, null),
        new(Signatures.Tag.ColorimetricIntentImageState, 1, new Signature[] { Signatures.TagType.Signature, }, null),
        new(Signatures.Tag.PerceptualRenderingIntentGamut, 1, new Signature[] { Signatures.TagType.Signature, }, null),
        new(Signatures.Tag.SaturationRenderingIntentGamut, 1, new Signature[] { Signatures.TagType.Signature, }, null),
        new(Signatures.Tag.Measurement, 1, new Signature[] { Signatures.TagType.Measurement, }, null),
        new(Signatures.Tag.Ps2CRD0, 1, new Signature[] { Signatures.TagType.Data, }, null),
        new(Signatures.Tag.Ps2CRD1, 1, new Signature[] { Signatures.TagType.Data, }, null),
        new(Signatures.Tag.Ps2CRD2, 1, new Signature[] { Signatures.TagType.Data, }, null),
        new(Signatures.Tag.Ps2CRD3, 1, new Signature[] { Signatures.TagType.Data, }, null),
        new(Signatures.Tag.Ps2CSA, 1, new Signature[] { Signatures.TagType.Data, }, null),
        new(Signatures.Tag.Ps2RenderingIntent, 1, new Signature[] { Signatures.TagType.Data, }, null),
        new(
            Signatures.Tag.ViewingCondDesc,
            1,
            new Signature[]
            {
                Signatures.TagType.TextDescription,
                Signatures.TagType.MultiLocalizedUnicode,
                Signatures.TagType.Text,
            },
            DecideTextDescType),
        new(Signatures.Tag.UcrBg, 1, new Signature[] { Signatures.TagType.UcrBg, }, null),
        new(Signatures.Tag.CrdInfo, 1, new Signature[] { Signatures.TagType.CrdInfo, }, null),
        new(Signatures.Tag.DToB0, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.DToB1, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.DToB2, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.DToB3, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.BToD0, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.BToD1, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.BToD2, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.BToD3, 1, new Signature[] { Signatures.TagType.MultiProcessElement, }, null),
        new(Signatures.Tag.ScreeningDesc, 1, new Signature[] { Signatures.TagType.TextDescription, }, null),
        new(Signatures.Tag.ViewingConditions, 1, new Signature[] { Signatures.TagType.ViewingConditions, }, null),
        new(Signatures.Tag.Screening, 1, new Signature[] { Signatures.TagType.Screening, }, null),
        new(Signatures.Tag.Vcgt, 1, new Signature[] { Signatures.TagType.Vcgt, }, null),
        new(Signatures.Tag.Meta, 1, new Signature[] { Signatures.TagType.Dict, }, null),
        new(Signatures.Tag.ProfileSequenceId, 1, new Signature[] { Signatures.TagType.ProfileSequenceId, }, null),
        new(
            Signatures.Tag.ProfileDescriptionML,
            1,
            new Signature[] { Signatures.TagType.MultiLocalizedUnicode, },
            null),
        new(Signatures.Tag.cicp, 1, new Signature[] { Signatures.TagType.cicp }, null),
        new(Signatures.Tag.ArgyllArts, 9, new Signature[] { Signatures.TagType.S15Fixed16Array, }, null),
        new(Signatures.Tag.MHC2, 1, new Signature[] { Signatures.TagType.MHC2 }, null));

    internal static readonly TagPluginChunkType TagPluginChunk = new();

    internal static readonly TagPluginChunkType globalTagPluginChunk = new();

    private static TagTypeHandler? GetHandler(Signature sig,
                                              TagTypeLinkedList? PluginLinkedList,
                                              TagTypeLinkedList DefaultLinkedList)
    {
        if (PluginLinkedList is not null)
            foreach (var pt in PluginLinkedList)
                if (sig == pt.Signature)
                    return pt;

        foreach (var pt in DefaultLinkedList)
            if (sig == pt.Signature)
                return pt;

        return null;
    }

    #region IO

    internal static bool _cmsWriteWCharArray(IOHandler io, uint n, ReadOnlySpan<char> Array)
    {
        _cmsAssert(io);

        n = Math.Min(n, (uint)Array.Length);

        for (var i = 0; i < n; i++)
            if (!io.Write(Array[i]))
                return false;

        return true;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static bool is_surrogate(uint uc) =>
    //    (uc - 0xD800u) < 2048u;

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static bool is_high_surrogate(uint uc) =>
    //    (uc & 0xFFFFFC00) == 0xD800;

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static bool is_low_surrogate(uint uc) =>
    //    (uc & 0xFFFFFC00) == 0xDC00;

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static uint surrogate_to_utf32(uint high, uint low) =>
    //    (high << 10) + low - 0x35FDC00;

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static bool convert_utf16_to_utf32(IOHandler io, int n, char* output)
    //{
    //    ushort uc;

    //    while (n > 0)
    //    {
    //        if (!io.ReadUshort(&uc)) return false;
    //        n--;

    //        if (!is_surrogate(uc))
    //        {
    //            *output++ = (char)uc;
    //        }
    //        else
    //        {
    //            ushort low;

    //            if (!io.ReadUshort(&low)) return false;
    //            n--;

    //            if (is_high_surrogate(uc) && is_low_surrogate(low))
    //            {
    //                *output++ = (char)surrogate_to_utf32(uc, low);
    //            }
    //            else
    //            {
    //                return false;       // Corrupted string, just ignore
    //            }
    //        }
    //    }

    //    return true;
    //}

    internal static bool _cmsReadWCharArray(IOHandler io, uint n, out string str)
    {
        Span<char> Array = stackalloc char[(int)n];

        var result = _cmsReadWCharArray(io, n, Array);
        str = new(Array);

        return result;
    }

    internal static bool _cmsReadWCharArray(IOHandler io, uint n, Span<char> Array)
    {
        //const bool is32 = sizeof(char) > sizeof(ushort);

        _cmsAssert(io);

        //if (is32 && Array is not null)
        //    return convert_utf16_to_utf32(io, (int)n, Array);

        for (var i = 0; i < n; i++)
        {
            if (!Array.IsEmpty)
            {
                if (!io.ReadUshort(out var tmp))
                    return false;
                if (i < Array.Length)
                    Array[i] = (char)tmp;
            }
            else
            {
                if (!io.ReadUshort(out _))
                    return false;
            }
        }

        return true;
    }

    private static bool ReadPositionTable(TagTypeHandler self,
                                          IOHandler io,
                                          uint Count,
                                          uint BaseOffset,
                                          object? Cargo,
                                          PositionTableEntryFn ElementFn)
    {
        //var pool = Context.GetPool<uint>(self.ContextID);
        var currentPosition = io.TellFunc(io);

        // Verify there is enough space left to read at least two uint items for Count items
        if (((io.reportedSize - currentPosition) / (2 * sizeof(uint))) < Count)
            return false;

        // Let's take the offsets to each element
        //ElementOffsets = _cmsCalloc<uint>(io.ContextID, Count);
        //if (ElementOffsets is null) goto Error;
        //var ElementOffsets = pool.Rent((int)Count);
        var ElementOffsets = new uint[Count];

        //ElementSizes = _cmsCalloc<uint>(io.ContextID, Count);
        //if (ElementSizes is null) goto Error;
        //var ElementSizes = pool.Rent((int)Count);
        var ElementSizes = new uint[Count];

        for (var i = 0; i < Count; i++)
        {
            if (!io.ReadUint(out ElementOffsets[i]))
                goto Error;
            if (!io.ReadUint(out ElementSizes[i]))
                goto Error;

            ElementOffsets[i] += BaseOffset;
        }

        // Seek to each element and read it
        for (var i = 0; i < Count; i++)
        {
            if (!io.SeekFunc(io, ElementOffsets[i]))
                goto Error;

            // This is the reader callback
            if (!ElementFn(self, io, Cargo, (uint)i, ElementSizes[i]))
                goto Error;
        }

        //Success
        //if (ElementOffsets is not null) ReturnArray(io.ContextID, ElementOffsets);
        //if (ElementSizes is not null) ReturnArray(io.ContextID, ElementSizes);
        return true;

    Error:
        //if (ElementOffsets is not null) ReturnArray(io.ContextID, ElementOffsets);
        //if (ElementSizes is not null) ReturnArray(io.ContextID, ElementSizes);
        return false;
    }

    private static bool WritePositionTable(TagTypeHandler self,
                                           IOHandler io,
                                           uint SizeOfTag,
                                           uint Count,
                                           uint BaseOffset,
                                           object? Cargo,
                                           PositionTableEntryFn ElementFn)
    {
        //var pool = Context.GetPool<uint>(self.ContextID);

        // Create table
        //ElementOffsets = _cmsCalloc<uint>(io.ContextID, Count);
        //if (ElementOffsets is null) goto Error;
        //var ElementOffsets = pool.Rent((int)Count);
        var ElementOffsets = new uint[Count];

        //ElementSizes = _cmsCalloc<uint>(io.ContextID, Count);
        //if (ElementSizes is null) goto Error;
        //var ElementSizes = pool.Rent((int)Count);
        var ElementSizes = new uint[Count];

        // Keep starting position of curve offsets
        var DirectoryPos = io.TellFunc(io);

        // Write a fake directory to be filled later on
        for (var i = 0; i < Count; i++)
        {
            if (!io.Write((uint)0))
                goto Error;  // Offset
            if (!io.Write((uint)0))
                goto Error;  // size
        }

        // Write each element. Keep track of the size as well.
        for (var i = 0; i < Count; i++)
        {
            var Before = io.TellFunc(io);
            ElementOffsets[i] = Before - BaseOffset;

            // Callback to write...
            if (!ElementFn(self, io, Cargo, (uint)i, SizeOfTag))
                goto Error;

            // Now the size
            ElementSizes[i] = io.TellFunc(io) - Before;
        }

        // Write the directory
        var CurrentPos = io.TellFunc(io);
        if (!io.SeekFunc(io, DirectoryPos))
            goto Error;

        for (var i = 0; i < Count; i++)
        {
            if (!io.Write(ElementOffsets[i]))
                goto Error;
            if (!io.Write(ElementSizes[i]))
                goto Error;
        }

        if (!io.SeekFunc(io, CurrentPos))
            goto Error;

        //Success
        //if (ElementOffsets is not null) ReturnArray(io.ContextID, ElementOffsets);
        //if (ElementSizes is not null) ReturnArray(io.ContextID, ElementSizes);
        return true;

    Error:
        //if (ElementOffsets is not null) ReturnArray(io.ContextID, ElementOffsets);
        //if (ElementSizes is not null) ReturnArray(io.ContextID, ElementSizes);
        return false;
    }

    #endregion IO

    #region TypeHandlers

    #region XYZ

    private static Box<CIEXYZ>? Type_XYZ_Read(TagTypeHandler _1, IOHandler io, out uint nItems, uint _2)
    {
        nItems = 0;
        //xyz = _cmsMallocZero<CIEXYZ>(self.ContextID);
        //if (xyz is null) return null;

        if (!io.ReadXYZ(out var xyz))
        {
            //_cmsFree(self.ContextID, xyz);
            return null;
        }

        nItems = 1;
        return new(xyz);
    }

    private static bool Type_XYZ_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not Box<CIEXYZ> xyz)
            return false;

        return io.Write(xyz);
    }

    private static Box<CIEXYZ>? Type_XYZ_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Box<CIEXYZ> xyz
            ? new(xyz)
            : null;

    //private static void Type_XYZ_Free(TagTypeHandler self, object? Ptr)
    //{
    //    //if (Ptr is BoxPtr<CIEXYZ> xyz)
    //    //    _cmsFree(self.ContextID, xyz);
    //}

    private static Signature DecideXYZtype(double _1, object? _2) =>
        Signatures.TagType.XYZ;

    #endregion XYZ

    #region Chromaticity

    private static Box<CIExyYTRIPLE>? Type_Chromaticity_Read(TagTypeHandler _1,
                                                             IOHandler io,
                                                             out uint nItems,
                                                             uint SizeOfTag)
    {
        CIExyYTRIPLE chrm = new();

        nItems = 0;
        //chrm = _cmsMallocZero<CIExyYTRIPLE>(self.ContextID);
        //if (chrm is null) return null;

        if (!io.ReadUshort(out var nChans))
            goto Error;

        // Let's recover from a bug interoduced in early versions of lcms1
        if (nChans is 0 && SizeOfTag is 32)
        {
            if (!io.ReadUshort(out _))
                goto Error;
            if (!io.ReadUshort(out nChans))
                goto Error;
        }

        if (nChans is not 3)
            goto Error;

        if (!io.ReadUshort(out var Table))
            goto Error;

        if (!io.ReadFixed15_16(out chrm.Red.x))
            goto Error;
        if (!io.ReadFixed15_16(out chrm.Red.y))
            goto Error;

        chrm.Red.Y = 1.0;

        if (!io.ReadFixed15_16(out chrm.Green.x))
            goto Error;
        if (!io.ReadFixed15_16(out chrm.Green.y))
            goto Error;

        chrm.Green.Y = 1.0;

        if (!io.ReadFixed15_16(out chrm.Blue.x))
            goto Error;
        if (!io.ReadFixed15_16(out chrm.Blue.y))
            goto Error;

        chrm.Blue.Y = 1.0;

        nItems = 1;
        return new(chrm);

    Error:
        //_cmsFree(self.ContextID, chrm);
        return null;
    }

    private static bool SaveOneChromaticity(double x, double y, IOHandler io)
    {
        if (!io.Write((uint)DoubleToS15Fixed16(x)))
            return false;
        if (!io.Write((uint)DoubleToS15Fixed16(y)))
            return false;

        return true;
    }

    private static bool Type_Chromaticity_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        var chrm = Ptr as Box<CIExyYTRIPLE>;
        if (chrm is null)
            return false;

        if (!io.Write((ushort)3))
            return false;    // nChannels
        if (!io.Write((ushort)0))
            return false;    // Table

        if (!SaveOneChromaticity(chrm.Value.Red.x, chrm.Value.Red.y, io))
            return false;
        if (!SaveOneChromaticity(chrm.Value.Green.x, chrm.Value.Green.y, io))
            return false;
        if (!SaveOneChromaticity(chrm.Value.Blue.x, chrm.Value.Blue.y, io))
            return false;

        return true;
    }

    private static Box<CIExyYTRIPLE>? Type_Chromaticity_Dup(TagTypeHandler self, object? Ptr, uint _) =>
        Ptr is Box<CIExyYTRIPLE> value
            ? new(value)
            : null;

    //private static void Type_Chromaticity_Free(TagTypeHandler self, object? Ptr)
    //{
    //    //if (Ptr is BoxPtr<CIExyYTRIPLE> value)
    //    //    _cmsFree(self.ContextID, value);
    //}

    #endregion Chromaticity

    #region ColorantOrder

    private static byte[]? Type_ColorantOrderType_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        nItems = 0;

        if (!io.ReadUint(out var Count))
            return null;
        if (Count > Context.MaxChannels)
            return null;

        //var ColorantOrder = GetArray<byte>(self.ContextID, cmsMAXCHANNELS);
        var ColorantOrder = new byte[Context.MaxChannels];
        //if (ColorantOrder is null) return null;

        // We use FF as end marker
        //memset(ColorantOrder, 0xFF, cmsMAXCHANNELS * sizeof(byte));
        Array.Fill<byte>(ColorantOrder, 0xFF);

        if (io.ReadFunc(io, ColorantOrder, sizeof(byte), Count) != Count)
        {
            //ReturnArray(self.ContextID, ColorantOrder);
            return null;
        }

        nItems = 1;
        return ColorantOrder;
    }

    private static bool Type_ColorantOrderType_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not byte[] ColorantOrder)
            return false;

        var Count = 0u;
        // Get the length
        for (var i = 0; i < Context.MaxChannels; i++)
            if (ColorantOrder[i] is not 0xFF)
                Count++;

        if (!io.Write(Count))
            return false;

        //var sz = Count * sizeof(byte);
        if (!io.WriteFunc(io, Count, ColorantOrder))
            return false;

        return true;
    }

    private static byte[]? Type_ColorantOrderType_Dup(TagTypeHandler self, object? Ptr, uint _) =>
        Ptr is byte[] value
            ? _cmsDupMem<byte>(self.ContextID, value, Context.MaxChannels)
            : null;

    private static void Type_ColorantOrderType_Free(TagTypeHandler self, object? Ptr)
    {
        //if (Ptr is byte[] value)
        //    ReturnArray(self.ContextID, value);
    }

    #endregion ColorantOrder

    #region S15Fixed16

    private static double[]? Type_S15Fixed16_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        double[] array_double;

        nItems = 0;
        var n = SizeOfTag / sizeof(uint);
        //array_double = GetArray<double>(self.ContextID, n);
        array_double = new double[n];
        //if (array_double is null) return null;

        for (var i = 0; i < n; i++)
        {
            if (!io.ReadFixed15_16(out array_double[i]))
            {
                //ReturnArray(self.ContextID, array_double);
                return null;
            }
        }

        nItems = n;
        return array_double;
    }

    private static bool Type_S15Fixed16_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint nItems)
    {
        if (Ptr is not double[] Value)
            return false;

        for (var i = 0; i < nItems; i++)
            if (!io.Write(Value[i]))
                return false;

        return true;
    }

    private static double[]? Type_S15Fixed16_Dup(TagTypeHandler self, object? Ptr, uint n) =>
        Ptr is double[] Value
            ? _cmsDupMem<double>(self.ContextID, Value, n)
            : null;

    private static void Type_S15Fixed16_Free(TagTypeHandler self, object? Ptr)
    {
        //if (Ptr is double[] value)
        //    ReturnArray(self.ContextID, value);
    }

    #endregion S15Fixed16

    #region U16Fixed16

    private static double[]? Type_U16Fixed16_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        nItems = 0;

        var n = SizeOfTag / sizeof(uint);
        //var array_double = GetArray<double>(self.ContextID, n);
        var array_double = new double[n];
        //if (array_double is null) return null;

        for (var i = 0; i < n; i++)
        {
            if (!io.ReadUint(out var v))
            {
                //ReturnArray(self.ContextID, array_double);
                return null;
            }

            // Convert to double
            array_double[i] = v / 65536.0;
        }

        nItems = n;
        return array_double;
    }

    private static bool Type_U16Fixed16_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint nItems)
    {
        if (Ptr is not double[] Value)
            return false;

        for (var i = 0; i < nItems; i++)
        {
            var v = (uint)Math.Floor((Value[i] * 65536.0) + 0.5);
            if (!io.Write(v))
                return false;
        }

        return true;
    }

    private static double[]? Type_U16Fixed16_Dup(TagTypeHandler self, object? Ptr, uint n) =>
        Ptr is double[] value
            ? _cmsDupMem<double>(self.ContextID, value, n)
            : null;

    private static void Type_U16Fixed16_Free(TagTypeHandler self, object? Ptr)
    {
        //if (Ptr is double[] value)
        //    ReturnArray(self.ContextID, value);
    }

    #endregion U16Fixed16

    #region Signature

    private static Box<Signature>? Type_Signature_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        nItems = 0;

        //var SigPtr = new Signature();
        //if (SigPtr is null) return null;

        if (!io.ReadUint(out var SigPtr))
        {
            //_cmsFree(self.ContextID, SigPtr);
            return null;
        }

        nItems = 1;
        return new Box<Signature>(new(SigPtr));
    }

    private static bool Type_Signature_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2) =>
        Ptr is Box<Signature> sig && io.Write((uint)sig.Value);

    private static Box<Signature>? Type_Signature_Dup(TagTypeHandler self, object? Ptr, uint _) =>
        Ptr is Box<Signature> sig
            ? new Box<Signature>(sig)
            : null;

    //private static void Type_Signature_Free(TagTypeHandler self, object? Ptr)
    //{
    //    //if (Ptr is BoxPtr<Signature> sig)
    //    //    _cmsFree(self.ContextID, sig);
    //}

    #endregion Signature

    #region Text

    private static Mlu? Type_Text_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        nItems = 0;

        var mlu = cmsMLUalloc(self.ContextID, 1);
        if (mlu is null)
            return null;

        // We need to store the "\0" at the end, so +1
        if (SizeOfTag is UInt32.MaxValue)
            goto Error1;

        //Text = _cmsMalloc<byte>(self.ContextID, SizeOfTag + 1);
        //var Text = GetArray<byte>(self.ContextID, SizeOfTag);
        var Text = new byte[SizeOfTag];                     // No trailing zero required!
        //if (Text is null) goto Error1;

        //var tmpText = stackalloc byte[(int)SizeOfTag];
        //if (io.Read(io, tmpText, sizeof(byte), SizeOfTag) != SizeOfTag) goto Error2;
        //new ReadOnlySpan<byte>(tmpText, (int)SizeOfTag).CopyTo(Text);

        if (io.ReadFunc(io, Text, sizeof(byte), SizeOfTag) != SizeOfTag)
            goto Error2;

        // Make sure text is properly ended
        //Text[SizeOfTag] = 0;              // Nope!!
        nItems = 1;

        // Keep the result
        if (!cmsMLUsetASCII(mlu, cmsNoLanguage, cmsNoCountry, Text.AsSpan(..(int)SizeOfTag)))
            goto Error2;

        //ReturnArray(self.ContextID, Text);
        return mlu;

    Error2:
        //if (Text is not null) ReturnArray(self.ContextID, Text);

    Error1:
        if (mlu is not null)
            cmsMLUfree(mlu);

        return null;
    }

    private static bool Type_Text_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Mlu mlu)
            return false;

        // Get the size of the string. Note there is an extra "\0" at the end       // No extra "\0"
        var size = cmsMLUgetASCII(mlu, cmsNoLanguage, cmsNoCountry, null);
        if (size is 0)
            return false;    // Cannot be zero!

        // Create memory
        //var Text = GetArray<byte>(self.ContextID, size);
        var Text = new byte[size];
        //if (Text is null) return false;

        cmsMLUgetASCII(mlu, cmsNoLanguage, cmsNoCountry, Text);
        //var tmpText = stackalloc byte[(int)size];
        //Text.AsSpan()[..(int)size].CopyTo(new(tmpText, (int)size));

        // Write it, including separators
        var rc = io.WriteFunc(io, size, Text);

        //ReturnArray(self.ContextID, Text);
        return rc;
    }

    private static Mlu? Type_Text_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Mlu value
            ? cmsMLUdup(value)
            : null;

    private static void Type_Text_Free(TagTypeHandler _1, object? Ptr) =>
        cmsMLUfree(Ptr as Mlu);

    private static Signature DecideTextType(double ICCVersion, object? _) =>
        ICCVersion >= 4.0
            ? Signatures.TagType.MultiLocalizedUnicode
            : Signatures.TagType.Text;

    #endregion Text

    #region Data

    private static Box<IccData>? Type_Data_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        nItems = 0;

        if (SizeOfTag < sizeof(uint))
            return null;

        var LenOfData = SizeOfTag - sizeof(uint);
        if (LenOfData > Int32.MaxValue)
            return null;

        //BinData = _cmsMalloc<IccData>(self.ContextID, _sizeof<IccData>() + LenOfData - 1);
        //if (BinData is null) return null;
        var BinData = new IccData
        {
            //data = Context.GetPool<byte>(self.ContextID).Rent((int)LenOfData),
            data = new byte[LenOfData], len = LenOfData
        };
        if (!io.ReadUint(out BinData.flag))
            goto Error;

        if (io.ReadFunc(io, BinData.data, sizeof(byte), LenOfData) != LenOfData)
            goto Error;

        nItems = 1;

        return new(BinData);

    Error:
        //if (BinData.data is not null)
        //ReturnArray(self.ContextID, BinData.data);
        //_cmsFree(self.ContextID, BinData);
        return null;
    }

    private static bool Type_Data_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not Box<IccData> BinData)
            return false;

        if (!io.Write(BinData.Value.flag))
            return false;

        return io.WriteFunc(io, BinData.Value.len, BinData.Value.data);
    }

    private static Box<IccData>? Type_Data_Dup(TagTypeHandler self, object? Ptr, uint _1)
    {
        if (Ptr is not Box<IccData> data)
            return null;

        var newData = data.Value;
        newData.data = _cmsDupMem<byte>(self.ContextID, data.Value.data, data.Value.len);

        return new(newData);
    }

    private static void Type_Data_Free(TagTypeHandler self, object? Ptr)
    {
        //if (Ptr is Box<IccData> data)
        //    ReturnArray(self.ContextID, data.Value.data);
    }

    #endregion Data

    #region Text Description

    private static Mlu? Type_Text_Description_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        nItems = 0;

        // Ond dword should be there
        if (SizeOfTag < sizeof(uint))
            return null;

        // Read len of ASCII
        if (!io.ReadUint(out var AsciiCount))
            return null;
        SizeOfTag -= sizeof(uint);

        // Check for size
        if (SizeOfTag < AsciiCount)
            return null;

        // All seems Ok, allocate the container
        var mlu = cmsMLUalloc(self.ContextID, 2);
        if (mlu is null)
            return null;

        // As much memory as size of tag
        //Text = _cmsMalloc<byte>(self.ContextID, AsciiCount + 1);
        //var Text = GetArray<byte>(self.ContextID, AsciiCount);
        var Text = new byte[AsciiCount];
        //if (Text is null) goto Error;

        // Read it
        //var tmpText = stackalloc byte[(int)AsciiCount];
        if (io.ReadFunc(io, Text, sizeof(byte), AsciiCount) != AsciiCount)
            goto Error;
        //new Span<byte>(tmpText, (int)AsciiCount).CopyTo(Text);
        SizeOfTag -= AsciiCount;

        // Make sure there is a terminator
        //Text[AsciiCount] = 0;

        // Set the MLU entry. From here we can be tolerant to wrong types
        if (!cmsMLUsetASCII(mlu, cmsNoLanguage, cmsNoCountry, Text.AsSpan(..(int)AsciiCount)))
            goto Error;
        //ReturnArray(self.ContextID, Text);
        Text = null;

        // Skip Unicode code
        if (SizeOfTag < 2 * sizeof(uint))
            goto Done;
        if (!io.ReadUint(out var UnicodeCode))
            goto Done;
        if (!io.ReadUint(out var UnicodeCount))
            goto Done;
        SizeOfTag -= 2u * sizeof(uint);

        if (UnicodeCount == 0 || SizeOfTag < UnicodeCount * sizeof(ushort))
            goto Done;

        if (!_cmsReadWCharArray(io, UnicodeCode, out var UnicodeString))
            goto Done;

        Span<byte> Dummy = stackalloc byte[sizeof(ushort)];
        for (var i = 0; i < UnicodeCount; i++)
        {
            if (io.ReadFunc(io, Dummy, sizeof(ushort), 1) is 0)
                goto Done;
        }

        SizeOfTag -= UnicodeCount * sizeof(ushort);

        // Skip ScriptCode code if present. Some buggy profiles does have less
        // data that strictly required. We need to skip it as this type may come
        // embedded in other types.

        if (SizeOfTag >= sizeof(ushort) + sizeof(byte) + 67)
        {
            if (!io.ReadUshort(out var ScriptCodeCode))
                goto Done;
            if (!io.ReadByte(out var ScriptCodeCount))
                goto Done;

            // Skip rest of tag
            for (var i = 0; i < 67; i++)
                if (io.ReadFunc(io, Dummy, sizeof(byte), 1) is 0)
                    goto Error;
        }

    Done:
        nItems = 1;
        return mlu;

    Error:
        //if (Text is not null) ReturnArray(self.ContextID, Text);
        if (mlu is not null)
            cmsMLUfree(mlu);

        return null;
    }

    private static bool Type_Text_Description_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Mlu mlu)
            return false;
        byte[]? Text = null;
        char[]? Wide = null;
        uint len, len_text, len_tag_requirement, len_aligned;
        var rc = false;
        Span<byte> Filler = stackalloc byte[68];

        // Used below for writing zeros
        //memset(Filler, 0, 68);

        // Get the len of string
        len = cmsMLUgetASCII(mlu, cmsNoLanguage, cmsNoCountry, null);

        // Specification ICC.1:2001-04 (v2.4.0): It has been found that textDescriptionType can contain misaligned data
        //(see clause 4.1 for the definition of 'aligned'). Because the Unicode language
        // code and Unicode count immediately follow the ASCII description, their
        // alignment is not correct if the ASCII count is not a multiple of four. The
        // ScriptCode code is misaligned when the ASCII count is odd. Profile reading and
        // writing software must be written carefully in order to handle these alignment
        // problems.
        //
        // The above last sentence suggest to handle alignment issues in the
        // parser. The provided example (Table 69 on Page 60) makes this clear.
        // The padding only in the ASCII count is not sufficient for a aligned tag
        // size, with the same text size in ASCII and Unicode.

        // Null strings
        if (len <= 0)
        {
            //Text = _cmsDupMem<byte>(self.ContextID, Filler, sizeof(byte));
            //Wide = _cmsDupMem<char>(self.ContextID, Filler, _sizeof<char>());
            Text = Array.Empty<byte>();
            Wide = Array.Empty<char>();
        }
        else
        {
            // Create independent buffers
            //Text = GetArray<byte>(self.ContextID, len);
            Text = new byte[len];
            //if (Text is null) goto Error;
            //Wide = GetArray<char>(self.ContextID, len);
            Wide = new char[len];
            //if (Wide is null) goto Error;

            // Get both representations.
            cmsMLUgetASCII(mlu, cmsNoLanguage, cmsNoCountry, Text);
            cmsMLUgetWide(mlu, cmsV2Unicode, cmsV2Unicode, Wide);
        }

        // Tell the real text len including the null terminator and padding
        //len_text = (uint)strlen(Text) + 1;
        len_text = (uint)Text.Length;
        // Compute a total tag size requirement
        len_tag_requirement =
            8                // Alignment
            + 4              // count
            + len_text       // desc[count]
            + 4              // ucLangCode
            + 4              // ucCount
            + (2 * len_text) // ucDesc[ucCount]
            + 2              // scCode
            + 1              // scCount
            + 67;            // scDesc[67]
        len_aligned = _cmsALIGNLONG(len_tag_requirement);

        if (!io.Write(len_text))
            goto Error;
        if (len_text > 0)
        {
            //var tmpText = stackalloc byte[(int)len_text];
            //Text.AsSpan()[..(int)len_text].CopyTo(new(tmpText, (int)len_text));
            if (!io.WriteFunc(io, len_text, Text))
                goto Error;
        }

        if (!io.Write((uint)0))
            goto Error;  // ucLangCode

        if (!io.Write(len_text))
            goto Error;
        if (len_text > 0)
        {
            //var tmpWide = stackalloc char[(int)len_text];
            //Wide.AsSpan()[..(int)len_text].CopyTo(new(tmpWide, (int)len_text));
            if (!_cmsWriteWCharArray(io, len_text, Wide))
                goto Error;
        }

        // ScriptCode Code & Count (unused)
        if (!io.Write((ushort)0))
            goto Error;
        if (!io.Write((byte)0))
            goto Error;

        if (!io.WriteFunc(io, 67, Filler))
            goto Error;

        // possibly add pad at the end of tag
        if (len_aligned - len_tag_requirement > 0 && !io.WriteFunc(io, len_aligned - len_tag_requirement, Filler))
            goto Error;

        rc = true;

    Error:
        //if (Text is not null) ReturnArray(self.ContextID, Text);
        //if (Wide is not null) ReturnArray(self.ContextID, Wide);

        return rc;
    }

    private static Mlu? Type_Text_Description_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Mlu mlu
            ? cmsMLUdup(mlu)
            : null;

    private static void Type_Text_Description_Free(TagTypeHandler _1, object? Ptr) =>
        cmsMLUfree(Ptr as Mlu);

    private static Signature DecideTextDescType(double ICCVersion, object? _1) =>
        ICCVersion >= 4.0
            ? Signatures.TagType.MultiLocalizedUnicode
            : Signatures.TagType.TextDescription;

    #endregion Text Description

    #region Curve

    private static ToneCurve? Type_Curve_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Span<double> SingleGamma = stackalloc double[1];
        nItems = 0;

        if (!io.ReadUint(out var Count))
            return null;

        switch (Count)
        {
            case 0:     // Linear
            {
                SingleGamma[0] = 1.0;

                var NewGamma = ToneCurve.BuildParametric(self.ContextID, 1, SingleGamma);
                if (NewGamma is null)
                    return null;
                nItems = 1;
                return NewGamma;
            }
            case 1:     // Specified as the exponent of gamma function
            {
                if (!io.ReadUshort(out var SingleGammaFixed))
                    return null;
                SingleGamma[0] = U8Fixed8ToDouble(SingleGammaFixed);

                nItems = 1;
                return ToneCurve.BuildParametric(self.ContextID, 1, SingleGamma);
            }
            default:
            {
                if (Count > 0x7FFF)
                    return null;    // This is to prevent bad guys from doing bad things

                var NewGamma = ToneCurve.BuildTabulated(self.ContextID, Count, ReadOnlySpan<ushort>.Empty);
                if (NewGamma is null)
                    return null;

                if (!io.ReadUshortArray(Count, NewGamma.Table16))
                {
                    return null;
                }

                nItems = 1;
                return NewGamma;
            }
        }
    }

    private static bool Type_Curve_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not ToneCurve Curve)
            return false;

        if (Curve.nSegments is 1 && Curve.Segments[0].Type is 1)
        {
            // Single gamma, preserve number
            var SingleGammaFixed = DoubleToU8Fixed8(Curve.Segments[0].Params[0]);

            if (!io.Write((uint)1))
                return false;
            if (!io.Write(SingleGammaFixed))
                return false;
            return true;
        }

        if (!io.Write(Curve.nEntries))
            return false;

        return io.Write(Curve.nEntries, Curve.Table16);
    }

    #endregion Curve

    #region ParametricCurve

    private static Signature DecideCurveType(double ICCVersion, object? Data)
    {
        if (ICCVersion < 4.0)
            return Signatures.TagType.Curve;

        if (Data is ToneCurve Curve)
        {
            if (Curve.nSegments is not 1)
                return Signatures.TagType.Curve;
            if (Curve.Segments?[0].Type is < 0 or > 5)
                return Signatures.TagType.Curve;
        }

        return Signatures.TagType.ParametricCurve;
    }

    private static ToneCurve? Type_ParametricCurve_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Span<int> ParamsByType = stackalloc int[] { 1, 3, 4, 5, 7 };
        Span<double> Params = stackalloc double[10];

        nItems = 0;
        if (!io.ReadUshort(out var Type))
            return null;
        if (!io.ReadUshort(out _))
            return null;   // Reserved

        if (Type > 4)
        {
            Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, $"Unknown parametric curve type '{Type}'");
            return null;
        }

        //memset(Params, 0, sizeof(double) * 10);
        var n = ParamsByType[Type];

        for (var i = 0; i < n; i++)
            if (!io.ReadFixed15_16(out Params[i]))
                return null;

        var NewGamma = ToneCurve.BuildParametric(self.ContextID, Type + 1, Params);

        nItems = 1;
        return NewGamma;
    }

    private static bool Type_ParametricCurve_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not ToneCurve Curve)
            return false;

        Span<int> ParamsByType = stackalloc int[] { 0, 1, 3, 4, 5, 7 };

        var typen = Curve.Segments[0].Type;

        if (Curve.nSegments is 1 && typen < 1)
        {
            Context.LogError(
                self.ContextID,
                ErrorCodes.UnknownExtension,
                "Multisegment or Inverted parametric curves cannot be written");
            return false;
        }

        if (typen > 5)
        {
            Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, "Unsupported parametric curve");
            return false;
        }

        var nParam = ParamsByType[typen];

        if (!io.Write((ushort)(Curve.Segments[0].Type - 1)))
            return false;
        if (!io.Write((ushort)0))
            return false;    // Reserved

        for (var i = 0; i < nParam; i++)
        {
            if (!io.Write(Curve.Segments[0].Params[i]))
                return false;
        }

        return true;
    }

    #endregion ParametricCurve

    #region DateTime

    private static Box<DateTime>? Type_DateTime_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Span<byte> timestamp = stackalloc byte[sizeof(ushort) * 6];
        DateTime NewDateTime;

        nItems = 0;

        if (io.ReadFunc(io, timestamp, (uint)timestamp.Length, 1) is not 1)
            return null;

        //NewDateTime = _cmsMalloc<DateTime>(self.ContextID);
        //if (NewDateTime is null) return null;

        MemoryMarshal.Read<DateTimeNumber>(timestamp).Decode(out NewDateTime);

        nItems = 1;
        return new(NewDateTime);
    }

    private static bool Type_DateTime_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        Span<byte> buf = stackalloc byte[sizeof(ushort) * 6];

        if (Ptr is not Box<DateTime> DateTime)
            return false;

        DateTimeNumber.Encode(out var timestamp, DateTime);
        MemoryMarshal.Write(buf, ref timestamp);
        return io.WriteFunc(io, (uint)buf.Length, buf);
    }

    private static Box<DateTime>? Type_DateTime_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Box<DateTime> dt
            ? new(dt)
            : null;

    //private static void Type_DateTime_Free(TagTypeHandler self, object? Ptr)
    //{
    //    //if (Ptr is BoxPtr<DateTime> dt)
    //    //    _cmsFree(self.ContextID, dt);
    //}

    #endregion DateTime

    #region Measurement

    private static Box<IccMeasurementConditions>? Type_Measurement_Read(TagTypeHandler _1,
                                                                        IOHandler io,
                                                                        out uint nItems,
                                                                        uint _2)
    {
        IccMeasurementConditions mc = new();

        nItems = 0;
        //memset(&mc, 0, _sizeof<IccMeasurementConditions>());

        if (!io.ReadUint(out mc.Observer))
            return null;
        if (!io.ReadXYZ(out mc.Backing))
            return null;
        if (!io.ReadUint(out mc.Geometry))
            return null;
        if (!io.ReadFixed15_16(out mc.Flare))
            return null;
        if (!io.ReadUint(out var illuminant))
            return null;
        mc.IlluminantType = (IlluminantType)illuminant;

        //var result = _cmsDupMem<IccMeasurementConditions>(self.ContextID, &mc);
        //if (result is null) return null;

        nItems = 1;
        return new(mc);
    }

    private static bool Type_Measurement_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not Box<IccMeasurementConditions> mc)
            return false;

        if (!io.Write(mc.Value.Observer))
            return false;
        if (!io.Write(mc.Value.Backing))
            return false;
        if (!io.Write(mc.Value.Geometry))
            return false;
        if (!io.Write(mc.Value.Flare))
            return false;
        if (!io.Write((uint)mc.Value.IlluminantType))
            return false;

        return true;
    }

    private static Box<IccMeasurementConditions>? Type_Measurement_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Box<IccMeasurementConditions> mc
            ? new(mc)
            : null;

    //private static void Type_Measurement_Free(TagTypeHandler self, object? Ptr)
    //{
    //    //if (Ptr is BoxPtr<IccMeasurementConditions> mc)
    //    //    _cmsFree(self.ContextID, mc);
    //}

    #endregion Measurement

    #region MLU

    private static Mlu? Type_MLU_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        uint NumOfWchar, SizeOfHeader, BeginOfThisString, EndOfThisString, LargestPosition;
        char[]? Block;

        nItems = 0;
        if (!io.ReadUint(out var Count))
            return null;
        if (!io.ReadUint(out var RecLen))
            return null;

        if (RecLen is not 12)
        {
            Context.LogError(
                self.ContextID,
                ErrorCodes.UnknownExtension,
                "multiLocalizedUnicodeType of len != 12 is not supported");
            return null;
        }

        Mlu? mlu = cmsMLUalloc(self.ContextID, Count);
        if (mlu is null)
            return null;

        //mlu->UsedEntries = Count;

        SizeOfHeader = (12 * Count) + 8;
        LargestPosition = 0;

        for (var i = 0; i < Count; i++)
        {
            if (!io.ReadUshort(out var Language))
                goto Error;
            if (!io.ReadUshort(out var Country))
                goto Error;

            // Now deal with Len and offset
            if (!io.ReadUint(out var Len))
                goto Error;
            if (!io.ReadUint(out var Offset))
                goto Error;

            // Offset MUST be even because it indexes a block of utf16 chars.
            // Tricky profiles that uses odd positions will not work anyway
            // because the whole utf16 block is previously converted to wchar_t
            // and sizeof this type may be of 4 bytes. On Linux systems, for example.
            if ((Offset & 1) is not 0)
                goto Error;

            // Check for overflow
            if (Offset < (SizeOfHeader + 8))
                goto Error;
            if (((Offset + Len) < Len) || ((Offset + Len) > SizeOfTag + 8))
                goto Error;

            // True begin of the string
            BeginOfThisString = Offset - SizeOfHeader - 8;

            // Adjust to char elements
            //Len *= (uint)(_sizeof<char>() / sizeof(ushort));
            //BeginOfThisString *= (uint)(_sizeof<char>() / sizeof(ushort));

            mlu.Entries.Add(new(Language, Country, BeginOfThisString, Len));

            // To guess maximum size, add offset + len
            EndOfThisString = BeginOfThisString + Len;
            if (EndOfThisString > LargestPosition)
                LargestPosition = EndOfThisString;
        }

        // Now read the remaining of tag and fill all strings. Subtract the directory
        SizeOfTag = LargestPosition /* * _sizeof<char>() / sizeof(ushort)*/;
        if (SizeOfTag is 0)
        {
            Block = null;
        }
        else
        {
            // Make sure this is an even utf16 size.
            if ((SizeOfTag & 1) is not 0)
                goto Error;
            //Block = _cmsMalloc<char>(self.ContextID, SizeOfTag);
            NumOfWchar = SizeOfTag / sizeof(char);
            //Block = GetArray<char>(self.ContextID, NumOfWchar);
            //Array.Clear(Block);
            Block = new char[NumOfWchar];
            //if (Block is null) goto Error;
            //var tmpBlock = stackalloc char[(int)NumOfWchar];
            if (!_cmsReadWCharArray(io, NumOfWchar, Block))
            {
                //_cmsFree(self.ContextID, Block);
                goto Error;
            }
            //new Span<char>(tmpBlock, (int)NumOfWchar).CopyTo(Block.AsSpan()[..(int)NumOfWchar]);
        }

        mlu.MemPool = Block;
        mlu.PoolSizeInBytes = SizeOfTag;
        mlu.PoolUsedInBytes = SizeOfTag;

        nItems = 1;
        return mlu;

    Error:
        if (mlu is not null)
            cmsMLUfree(mlu);

        return null;
    }

    private static bool Type_MLU_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is null)
        {
            // Empty placeholder
            if (!io.Write((uint)0))
                return false;
            if (!io.Write((uint)12))
                return false;
            return true;
        }

        if (Ptr is not Mlu mlu)
            return false;

        if (!io.Write(mlu.UsedEntries))
            return false;
        if (!io.Write((uint)12))
            return false;

        var HeaderSize = (12 * mlu.UsedEntries) + (sizeof(uint) * 2);

        for (var i = 0; i < mlu.UsedEntries; i++)
        {
            var Len = mlu.Entries[i].LenInBytes;
            var Offset = mlu.Entries[i].StrWOffsetInBytes;

            Offset += HeaderSize + 8;

            if (!io.Write(mlu.Entries[i].Language))
                return false;
            if (!io.Write(mlu.Entries[i].Country))
                return false;
            if (!io.Write(Len))
                return false;
            if (!io.Write(Offset))
                return false;
        }

        return _cmsWriteWCharArray(
            io,
            mlu.PoolUsedInBytes / sizeof(char),
            mlu.MemPool.AsSpan()[..(int)(mlu.PoolUsedInBytes / sizeof(char))]);
    }

    private static Mlu? Type_MLU_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Mlu mlu
            ? cmsMLUdup(mlu)
            : null;

    private static void Type_MLU_Free(TagTypeHandler _1, object? Ptr) =>
        cmsMLUfree(Ptr as Mlu);

    #endregion MLU

    #region LUT8

    /*
    This structure represents a colour transform using tables of 8-bit precision.
    This type contains four processing elements: a 3 by 3 matrix (which shall be
    the identity matrix unless the input colour space is XYZ), a set of one dimensional
    input tables, a multidimensional lookup table, and a set of one dimensional output
    tables. Data is processed using these elements via the following sequence:
    (matrix) -> (1d input tables)  -> (multidimensional lookup table - CLUT) -> (1d output tables)

    Byte Position   Field Length (bytes)  Content Encoded as...
    8                  1          Number of Input Channels (i)    uInt8Number
    9                  1          Number of Output Channels (o)   uInt8Number
    10                 1          Number of CLUT grid points (identical for each side) (g) uInt8Number
    11                 1          Reserved for padding (fill with 00h)

    12..15             4          Encoded e00 parameter   s15Fixed16Number
    */

    private static Signature DecideLUTtypeA2B(double ICCVersion, object? Data) =>
        (ICCVersion < 4.0)
            ? Data is Pipeline p
                  ? p.SaveAs8Bits
                        ? Signatures.TagType.Lut8
                        : Signatures.TagType.Lut16
                  : Signatures.TagType.LutAtoB
            : Signatures.TagType.LutAtoB;

    private static Signature DecideLUTtypeB2A(double ICCVersion, object? Data) =>
        (ICCVersion < 4.0)
            ? Data is Pipeline p
                  ? p.SaveAs8Bits
                        ? Signatures.TagType.Lut8
                        : Signatures.TagType.Lut16
                  : Signatures.TagType.LutBtoA
            : Signatures.TagType.LutBtoA;

    private static bool Read8bitTables(Context? ContextID, IOHandler io, Pipeline lut, uint nChannels)
    {
        //byte* Temp = null;

        //var pool = Context.GetPool<ToneCurve>(ContextID);
        //ToneCurve[] Tables = pool.Rent(cmsMAXCHANNELS);
        var Tables = new ToneCurve[Context.MaxChannels];

        if (nChannels is > Context.MaxChannels or <= 0)
            goto Error2;

        //memset(Tables, 0, _sizeof<nint>() * cmsMAXCHANNELS);

        //Temp = _cmsMalloc<byte>(ContextID, 256);
        //if (Temp is null) goto Error2;
        //var Temp = Context.GetPool<byte>(ContextID).Rent(256);
        var Temp = new byte[256];

        for (var i = 0; i < nChannels; i++)
        {
            Tables[i] = ToneCurve.BuildTabulated(ContextID, 256, ReadOnlySpan<ushort>.Empty);
            if (Tables[i] is null)
                goto Error;
        }

        for (var i = 0; i < nChannels; i++)
        {
            if (io.ReadFunc(io, Temp, 256, 1) is not 1)
                goto Error;

            for (var j = 0; j < 256; j++)
                if (Tables[i].Table16 is not null)
                    Tables[i].Table16![j] = FROM_8_TO_16(Temp[j]);
        }

        Temp = null;

        if (!lut.InsertStageAtEnd(new ToneCurvesStage(ContextID, Tables[..(int)nChannels])))
            goto Error;

        return true;

    Error:
    Error2:
        return false;
    }

    private static bool Write8bitTables(Context? ContextID, IOHandler io, uint n, ToneCurvesStage? Tables)
    {
        if (Tables is not null)
        {
            for (var i = 0; i < n; i++)
            {
                // Usual case of identity curves
                if ((Tables.Curves[i].nEntries is 2) &&
                    (Tables.Curves[i].Table16 is not null) &&
                    (Tables.Curves[i].Table16![0] is 0) &&
                    (Tables.Curves[i].Table16![1] is 65535))
                {
                    for (var j = 0; j < 256; j++)
                        if (!io.Write((byte)j))
                            return false;
                }
                else
                {
                    if (Tables.Curves[i].nEntries is not 256)
                    {
                        Context.LogError(ContextID, ErrorCodes.Range, "LUT8 needs 256 entries on prelinearization");
                        return false;
                    }
                    else
                    {
                        for (var j = 0; j < 256; j++)
                        {
                            var val = FROM_16_TO_8(Tables.Curves[i].Table16[j]);
                            if (!io.Write(val))
                                return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private static uint uipow(uint n, uint a, uint b)
    {
        var rv = 1u;

        if (a is 0)
            return 0;
        if (n is 0)
            return 0;

        for (; b > 0; b--)
        {
            rv *= a;

            // Check for overflow
            if (rv > UInt32.MaxValue / a)
                return unchecked((uint)-1);
        }

        var rc = rv * n;

        if (rv != rc / n)
            return unchecked((uint)-1);
        return rc;
    }

    private static Pipeline? Type_LUT8_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Pipeline? NewLUT = null;
        Span<double> Matrix = stackalloc double[3 * 3];

        nItems = 0;

        if (!io.ReadByte(out var InputChannels))
            goto Error;
        if (!io.ReadByte(out var OutputChannels))
            goto Error;
        if (!io.ReadByte(out var CLUTpoints))
            goto Error;

        if (CLUTpoints is 1)
            goto Error;    // Impossible value, 0 for no CLUT and then 2 at least

        // Padding
        if (!io.ReadByte(out _))
            goto Error;

        // Do some checking
        if (InputChannels is 0 or > Context.MaxChannels)
            goto Error;
        if (OutputChannels is 0 or > Context.MaxChannels)
            goto Error;

        // Allocates an empty Pipeline
        NewLUT = new Pipeline(self.ContextID, InputChannels, OutputChannels);

        // Read the Matrix
        for (var i = 0; i < 9; i++)
            if (!io.ReadFixed15_16(out Matrix[i]))
                goto Error;

        // Only operates if not identity...
        if ((InputChannels is 3) &&
            !new MAT3(Matrix).IsIdentity &&
            !NewLUT.InsertStageAtStart(new MatrixStage(self.ContextID, 3, 3, Matrix, null)))
        {
            goto Error;
        }

        // Get input tables
        if (!Read8bitTables(self.ContextID, io, NewLUT, InputChannels))
            goto Error;

        // Get 3D CLUT. Check the overflow...
        var nTabSize = uipow(OutputChannels, CLUTpoints, InputChannels);
        if (nTabSize == unchecked((uint)-1))
            goto Error;
        if (nTabSize > 0)
        {
            //var bytePool = Context.GetPool<byte>(self.ContextID);
            //var usPool = Context.GetPool<ushort>(self.ContextID);

            //var PtrW = _cmsCalloc<ushort>(self.ContextID, nTabSize);
            //var T = usPool.Rent((int)nTabSize);
            //Array.Clear(T);
            var T = new ushort[nTabSize];
            var PtrW = T.AsSpan();
            //if (T is null) goto Error;

            //Temp = _cmsMalloc<byte>(self.ContextID, nTabSize);
            //if (Temp is null)
            //{
            //    _cmsFree(self.ContextID, T);
            //    goto Error;
            //}
            //var Temp = bytePool.Rent((int)nTabSize);
            var Temp = new byte[nTabSize];

            if (io.ReadFunc(io, Temp, nTabSize, 1) is not 1)
            {
                //ReturnArray(self.ContextID, T);
                //ReturnArray(self.ContextID, Temp);
                goto Error;
            }

            for (var i = 0; i < nTabSize; i++)
                PtrW[i] = FROM_8_TO_16(Temp[i]);
            //ReturnArray(self.ContextID, Temp);
            Temp = null;

            if (!NewLUT.InsertStageAtEnd(
                    new CLutStage<ushort>(self.ContextID, CLUTpoints, InputChannels, OutputChannels, T)))
            {
                //ReturnArray(self.ContextID, T);
                goto Error;
            }
            //ReturnArray(self.ContextID, T);
        }

        // Get output tables
        if (!Read8bitTables(self.ContextID, io, NewLUT, OutputChannels))
            goto Error;

        nItems = 1;
        return NewLUT;

    Error:
        return null;
    }

    private static bool Type_LUT8_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Pipeline NewLut)
            return false;

        ReadOnlySpan<double> ident = stackalloc double[9] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
        ToneCurvesStage? PreMPE = null, PostMPE = null;
        MatrixStage? MatMPE = null;
        CLutStage<ushort>? clut = null;

        // Disassemble the LUT into components.
        var mpe = NewLut.Elements;
        if (mpe is null)    // Should never be empty. Corrupted?
        {
            Context.LogError(self.ContextID, cmsERROR_UNKNOWN_EXTENSION, "empty LUT8 is not supported");
            return false;
        }

        if (mpe.Type == Signatures.Stage.MatrixElem)
        {
            if (mpe.InputChannels is not 3 || mpe.OutputChannels is not 3)
                return false;
            MatMPE = mpe as MatrixStage;
            mpe = mpe.Next;
        }

        if (mpe?.Type == Signatures.Stage.CurveSetElem)
        {
            PreMPE = mpe as ToneCurvesStage;
            mpe = mpe.Next;
        }

        if (mpe?.Type == Signatures.Stage.CLutElem)
        {
            clut = mpe as CLutStage<ushort>;
            mpe = mpe.Next;
        }

        if (mpe?.Type == Signatures.Stage.CurveSetElem)
        {
            PostMPE = mpe as ToneCurvesStage;
            mpe = mpe.Next;
        }

        // That should be all
        if (mpe is not null)
        {
            Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, "LUT is not suitable to be saved as LUT8");
            return false;
        }

        var clutPoints = 0u;
        if (clut is not null)
        {
            // Lut8 only allows same CLUT points in all dimensions
            clutPoints = clut.Params.nSamples[0];
            for (var i = 1; i < NewLut.InputChannels; i++)
            {
                if (clut.Params.nSamples[i] != clutPoints)
                {
                    Context.LogError(
                        self.ContextID,
                        ErrorCodes.UnknownExtension,
                        "LUT with different samples per dimension not suitable to be saved as LUT8");
                    return false;
                }
            }
        }

        if (!io.Write((byte)NewLut.InputChannels))
            return false;
        if (!io.Write((byte)NewLut.OutputChannels))
            return false;
        if (!io.Write((byte)clutPoints))
            return false;
        if (!io.Write((byte)0))
            return false; // Padding

        var mat = MatMPE is not null ? MatMPE.Values : ident;
        for (var i = 0; i < 9; i++)
            if (!io.Write(mat[i]))
                return false;

        // The prelinearization table
        if (!Write8bitTables(self.ContextID, io, NewLut.InputChannels, PreMPE))
            return false;

        var nTabSize = uipow(NewLut.OutputChannels, clutPoints, NewLut.InputChannels);
        if (nTabSize == unchecked((uint)-1))
            return false;
        if (nTabSize > 0)
        {
            // The 3D CLUT
            if (clut is not null)
            {
                for (var j = 0; j < nTabSize; j++)
                {
                    var val = FROM_16_TO_8(clut.TUshort[j]);
                    if (!io.Write(val))
                        return false;
                }
            }
        }

        // The postlinearization table
        return Write8bitTables(self.ContextID, io, NewLut.OutputChannels, PostMPE);
    }

    private static Pipeline? Type_LUT8_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        (Ptr as Pipeline)?.Clone();

    private static void Type_LUT8_Free(TagTypeHandler _1, object? Ptr) =>
        (Ptr as Pipeline)?.Dispose();

    #endregion LUT8

    #region LUT16

    private static bool Read16bitTables(Context? ContextID, IOHandler io, Pipeline lut, uint nChannels, uint nEntries)
    {
        //var pool = Context.GetPool<ToneCurve>(ContextID);

        // Maybe an empty table? (this is a lcms extension)
        if (nEntries <= 0)
            return true;

        if (nEntries < 2 || nChannels > Context.MaxChannels)
            return false;

        // Init table to zero
        //memset(Tables, 0, _sizeof<nint>() * cmsMAXCHANNELS);
        //ToneCurve[] Tables = pool.Rent(cmsMAXCHANNELS);
        var Tables = new ToneCurve[Context.MaxChannels];

        for (var i = 0; i < nChannels; i++)
        {
            Tables[i] = ToneCurve.BuildTabulated(ContextID, nEntries, ReadOnlySpan<ushort>.Empty);
            if (Tables[i] is null)
                goto Error;

            if (!io.ReadUshortArray(nEntries, Tables[i].Table16))
                goto Error;
        }

        // Add the table (which may certainly be an identity, but this is up to the optimizer, not the reading code)
        if (!lut.InsertStageAtEnd(new ToneCurvesStage(ContextID, Tables[..(int)nChannels])))
            goto Error;

        return true;

    Error:
        return false;
    }

    private static bool Write16bitTables(Context? _1, IOHandler io, ToneCurvesStage Tables)
    {
        _cmsAssert(Tables);

        for (var i = 0; i < Tables.nCurves; i++)
        {
            var nEntries = Tables.Curves[i].nEntries;

            // Usual case of identity curves
            for (var j = 0; j < nEntries; j++)
                if (!io.Write(Tables.Curves[i].Table16[j]))
                    return false;
        }

        return true;
    }

    private static Pipeline? Type_LUT16_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Pipeline? NewLUT = null;
        Span<double> Matrix = stackalloc double[3 * 3];

        nItems = 0;

        if (!io.ReadByte(out var InputChannels))
            goto Error;
        if (!io.ReadByte(out var OutputChannels))
            goto Error;
        if (!io.ReadByte(out var CLUTpoints))
            goto Error;  // 255 maximum

        // Padding
        if (!io.ReadByte(out _))
            goto Error;

        // Do some checking
        if (InputChannels is 0 or > Context.MaxChannels)
            goto Error;
        if (OutputChannels is 0 or > Context.MaxChannels)
            goto Error;

        // Allocates an empty LUT
        NewLUT = new Pipeline(self.ContextID, InputChannels, OutputChannels);
        if (NewLUT is null)
            goto Error;

        // Read the Matrix
        for (var i = 0; i < 9; i++)
            if (!io.ReadFixed15_16(out Matrix[i]))
                goto Error;

        // Only operates on 3 channels
        if ((InputChannels is 3) &&
            !new MAT3(Matrix).IsIdentity &&
            !NewLUT.InsertStageAtEnd(new MatrixStage(self.ContextID, 3, 3, Matrix, null)))
        {
            goto Error;
        }

        if (!io.ReadUshort(out var InputEntries))
            goto Error;
        if (!io.ReadUshort(out var OutputEntries))
            goto Error;

        if (InputEntries > 0x7FFF || OutputEntries > 0x7FFF)
            goto Error;
        if (CLUTpoints is 1)
            goto Error;    // Impossible value, 0 for no CLUT and then 2 at least

        // Get input tables
        if (!Read16bitTables(self.ContextID, io, NewLUT, InputChannels, InputEntries))
            goto Error;

        // Get 3D CLUT
        var nTabSize = uipow(OutputChannels, CLUTpoints, InputChannels);
        if (nTabSize == unchecked((uint)-1))
            goto Error;
        if (nTabSize > 0)
        {
            var T = new ushort[nTabSize];

            if (!io.ReadUshortArray(nTabSize, T))
            {
                goto Error;
            }

            if (!NewLUT.InsertStageAtEnd(
                    new CLutStage<ushort>(self.ContextID, CLUTpoints, InputChannels, OutputChannels, T)))
            {
                goto Error;
            }
        }

        // Get output tables
        if (!Read16bitTables(self.ContextID, io, NewLUT, OutputChannels, OutputEntries))
            goto Error;

        nItems = 1;
        return NewLUT;

    Error:
        return null;
    }

    private static bool Type_LUT16_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Pipeline NewLut)
            return false;

        ReadOnlySpan<double> ident = stackalloc double[9] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
        ToneCurvesStage? PreMPE = null, PostMPE = null;
        MatrixStage? MatMPE = null;
        CLutStage<ushort>? clut = null;

        // Disassemble the LUT into components.
        var mpe = NewLut.Elements;
        if (mpe?.Type == Signatures.Stage.MatrixElem)
        {
            if (mpe.InputChannels is not 3 || mpe.OutputChannels is not 3)
                return false;
            MatMPE = mpe as MatrixStage;
            mpe = mpe.Next;
        }

        if (mpe?.Type == Signatures.Stage.CurveSetElem)
        {
            PreMPE = mpe as ToneCurvesStage;
            mpe = mpe.Next;
        }

        if (mpe?.Type == Signatures.Stage.CLutElem)
        {
            clut = mpe as CLutStage<ushort>;
            mpe = mpe.Next;
        }

        if (mpe?.Type == Signatures.Stage.CurveSetElem)
        {
            PostMPE = mpe as ToneCurvesStage;
            mpe = mpe.Next;
        }

        // That should be all
        if (mpe is not null)
        {
            Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, "LUT is not suitable to be saved as LUT16");
            return false;
        }

        var InputChannels = NewLut.InputChannels;
        var OutputChannels = NewLut.OutputChannels;

        var clutPoints = 0u;
        if (clut is not null)
        {
            // Lut16 only allows same CLUT points in all dimensions
            clutPoints = clut.Params.nSamples[0];
            for (var i = 1; i < NewLut.InputChannels; i++)
            {
                if (clut.Params.nSamples[i] != clutPoints)
                {
                    Context.LogError(
                        self.ContextID,
                        ErrorCodes.UnknownExtension,
                        "LUT with different samples per dimension not suitable to be saved as LUT16");
                    return false;
                }
            }
        }

        if (!io.Write((byte)NewLut.InputChannels))
            return false;
        if (!io.Write((byte)NewLut.OutputChannels))
            return false;
        if (!io.Write((byte)clutPoints))
            return false;
        if (!io.Write((byte)0))
            return false; // Padding

        var mat = MatMPE is not null ? MatMPE.Values : ident;
        for (var i = 0; i < 9; i++)
            if (!io.Write(mat[i]))
                return false;

        if (!io.Write((ushort)(PreMPE is not null ? PreMPE.Curves[0].nEntries : 2)))
            return false;
        if (!io.Write((ushort)(PostMPE is not null ? PostMPE.Curves[0].nEntries : 2)))
            return false;

        // The prelinearization table
        if (PreMPE is not null)
        {
            if (!Write16bitTables(self.ContextID, io, PreMPE))
                return false;
        }
        else
        {
            for (var i = 0; i < InputChannels; i++)
            {
                if (!io.Write((ushort)0))
                    return false;
                if (!io.Write((ushort)0xFFFF))
                    return false;
            }
        }

        var nTabSize = uipow(NewLut.OutputChannels, clutPoints, NewLut.InputChannels);
        if (nTabSize == unchecked((uint)-1))
            return false;
        if (nTabSize > 0)
        {
            // The 3D CLUT
            if (clut is not null && !io.Write(nTabSize, clut.TUshort))
                return false;
        }

        // The postlinearization table
        if (PostMPE is not null)
        {
            if (!Write16bitTables(self.ContextID, io, PostMPE))
                return false;
        }
        else
        {
            for (var i = 0; i < InputChannels; i++)
            {
                if (!io.Write((ushort)0))
                    return false;
                if (!io.Write((ushort)0xFFFF))
                    return false;
            }
        }

        return true;
    }

    private static Pipeline? Type_LUT16_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        (Ptr as Pipeline)?.Clone();

    private static void Type_LUT16_Free(TagTypeHandler _1, object? Ptr) =>
        (Ptr as Pipeline)?.Dispose();

    #endregion LUT16

    #region LUTA2B

    /* LutAtoB type

        This structure represents a colour transform. The type contains up to five processing
        elements which are stored in the AtoBTag tag in the following order: a set of one
        dimensional curves, a 3 by 3 matrix with offset terms, a set of one dimensional curves,
        a multidimensional lookup table, and a set of one dimensional output curves.
        Data are processed using these elements via the following sequence:

    ("A" curves) -> (multidimensional lookup table - CLUT) -> ("M" curves) -> (matrix) -> ("B" curves).

    It is possible to use any or all of these processing elements. At least one processing element
    must be included.Only the following combinations are allowed:

    B
    M - Matrix - B
    A - CLUT - B
    A - CLUT - M - Matrix - B

    */

    private static Stage? ReadMatrix(TagTypeHandler self, IOHandler io, uint Offset)
    {
        Span<double> dMat = stackalloc double[3 * 3];
        Span<double> dOff = stackalloc double[3];

        // Go to address
        if (!io.SeekFunc(io, Offset))
            return null;

        // Read the Matrix and Offsets
        for (var i = 0; i < 3 * 3; i++)
            if (!io.ReadFixed15_16(out dMat[i]))
                return null;
        for (var i = 0; i < 3; i++)
            if (!io.ReadFixed15_16(out dOff[i]))
                return null;

        return new MatrixStage(self.ContextID, 3, 3, dMat, dOff);
    }

    private static Stage? ReadCLUT(TagTypeHandler self,
                                   IOHandler io,
                                   uint Offset,
                                   uint InputChannels,
                                   uint OutputChannels)
    {
        Span<byte> gridPoints8 = stackalloc byte[Context.MaxChannels];  // Number of grid points in each dimension
        Span<uint> GridPoints = stackalloc uint[Context.MaxChannels];

        if (!io.SeekFunc(io, Offset))
            return null;
        if (io.ReadFunc(io, gridPoints8, Context.MaxChannels, 1) is not 1)
            return null;

        for (var i = 0; i < Context.MaxChannels; i++)
        {
            if (gridPoints8[i] is 1)
                return null; // Imposible value, 0 for no CLUT and then 2 at least
            GridPoints[i] = gridPoints8[i];
        }

        if (!io.ReadByte(out var Precision))
            return null;

        if (!io.ReadByte(out _))
            return null;
        if (!io.ReadByte(out _))
            return null;
        if (!io.ReadByte(out _))
            return null;

        var CLUT = new CLutStage<ushort>(self.ContextID, GridPoints, InputChannels, OutputChannels, null);
        if (CLUT is null)
            return null;

        // Predcision can be 1 or 2 bytes
        switch (Precision)
        {
            case 1:
                Span<byte> v = stackalloc byte[1];

                for (var i = 0; i < CLUT.Tab.Length; i++)
                {
                    if (io.ReadFunc(io, v, sizeof(byte), 1) is not 1)
                    {
                        return null;
                    }

                    CLUT.TUshort[i] = FROM_8_TO_16(v[0]);
                }

                break;

            case 2:
                if (!io.ReadUshortArray((uint)CLUT.Tab.Length, CLUT.TUshort))
                {
                    return null;
                }

                break;

            default:
                Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, $"Unknown precision of '{Precision}'");
                return null;
        }

        return CLUT;
    }

    private static ToneCurve? ReadEmbeddedCurve(TagTypeHandler self, IOHandler io)
    {
        var BaseType = io.ReadTypeBase();
        if (BaseType == Signatures.TagType.Curve)
        {
            return Type_Curve_Read(self, io, out _, 0);
        }
        else if (BaseType == Signatures.TagType.ParametricCurve)
        {
            return Type_ParametricCurve_Read(self, io, out _, 0);
        }
        else
        {
            Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, $"Unknown curve type '{(BaseType)}'");
            return null;
        }
    }

    private static Stage? ReadSetOfCurves(TagTypeHandler self, IOHandler io, uint Offset, uint nCurves)
    {
        //var pool = Context.GetPool<ToneCurve>(self.ContextID);
        Stage? Lin = null;

        if (nCurves > Context.MaxChannels)
            return null;

        if (!io.SeekFunc(io, Offset))
            return null;

        //ToneCurve[] Curves = pool.Rent(cmsMAXCHANNELS);
        var Curves = new ToneCurve[Context.MaxChannels];
        for (var i = 0; i < nCurves; i++)
            Curves[i] = null!;

        for (var i = 0; i < nCurves; i++)
        {
            Curves[i] = ReadEmbeddedCurve(self, io)!;
            if (Curves[i] is null)
                goto Error;
            if (!io.ReadAlignment())
                goto Error;
        }

        Lin = new ToneCurvesStage(self.ContextID, Curves[..(int)nCurves]);

    Error:
        return Lin;
    }

    private static Pipeline? Type_LUTA2B_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Pipeline? NewLUT = null;

        nItems = 0;

        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        if (!io.ReadByte(out var inputChan))
            goto Error;     // Number of input channels
        if (!io.ReadByte(out var outputChan))
            goto Error;    // Number of output channels

        if (!io.ReadUshort(out _))
            goto Error;

        if (!io.ReadUint(out var offsetB))
            goto Error;      // Offset to first "B" curve
        if (!io.ReadUint(out var offsetMat))
            goto Error;    // Offset to matrix
        if (!io.ReadUint(out var offsetM))
            goto Error;      // Offset to first "M" curve
        if (!io.ReadUint(out var offsetC))
            goto Error;      // Offset to CLUT
        if (!io.ReadUint(out var offsetA))
            goto Error;      // Offset to first "A" curve

        // Do some checking
        if (inputChan is 0 or > Context.MaxChannels)
            goto Error;
        if (outputChan is 0 or > Context.MaxChannels)
            goto Error;

        // Allocates an empty LUT
        NewLUT = new Pipeline(self.ContextID, inputChan, outputChan);
        if (NewLUT is null)
            goto Error;

        if (offsetA is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadSetOfCurves(self, io, BaseOffset + offsetA, inputChan)))
                goto Error;
        }

        if (offsetC is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadCLUT(self, io, BaseOffset + offsetC, inputChan, outputChan)))
                goto Error;
        }

        if (offsetM is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadSetOfCurves(self, io, BaseOffset + offsetM, outputChan)))
                goto Error;
        }

        if (offsetMat is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadMatrix(self, io, BaseOffset + offsetMat)))
                goto Error;
        }

        if (offsetB is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadSetOfCurves(self, io, BaseOffset + offsetB, outputChan)))
                goto Error;
        }

        nItems = 1;
        return NewLUT;

    Error:
        return null;
    }

    private static bool WriteMatrix(TagTypeHandler _1, IOHandler io, Stage mpe)
    {
        Span<double> zeros = stackalloc double[(int)mpe.OutputChannels];
        if (mpe is not MatrixStage m)
            return false;

        //memset(zeros, 0, (int)mpe.OutputChannels * sizeof(double));
        //zeros.Clear();

        var n = mpe.InputChannels * mpe.OutputChannels;

        // Write the Matrix
        for (var i = 0; i < n; i++)
            if (!io.Write(m.Values[i]))
                return false;

        var offsets = m.Offset is not null ? m.Offset : zeros;
        for (var i = 0; i < mpe.OutputChannels; i++)
            if (!io.Write(offsets[i]))
                return false;

        return true;
    }

    private static bool WriteSetOfCurves(TagTypeHandler self, IOHandler io, Signature Type, Stage mpe)
    {
        var n = mpe.OutputChannels;
        var Curves = _cmsStageGetPtrToCurveSet(mpe);

        for (var i = 0; i < n; i++)
        {
            // If this is a table-based curve, use curve type even on V4
            var CurrentType = Type;

            if ((Curves[i].nSegments is 0) ||                                        // 16 bits tabulated
                ((Curves[i].nSegments is 3) && (Curves[i].Segments[1].Type is 0)) || // Floating-point tabulated
                Curves[i].Segments[0].Type < 0)
            {
                CurrentType = Signatures.TagType.Curve;
            }

            if (!io.WriteTypeBase(CurrentType))
                return false;

            if (CurrentType == Signatures.TagType.Curve)
            {
                if (!Type_Curve_Write(self, io, Curves[i], 1))
                    return false;
            }
            else if (CurrentType == Signatures.TagType.ParametricCurve)
            {
                if (!Type_ParametricCurve_Write(self, io, Curves[i], 1))
                    return false;
            }
            else
            {
                Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, $"Unknown curve type '{(Type)}'");
                return false;
            }

            if (!io.WriteAlignment())
                return false;
        }

        return true;
    }

    private static bool WriteCLUT(TagTypeHandler self, IOHandler io, byte Precision, Stage mpe)
    {
        Span<byte> gridPoints = stackalloc byte[Context.MaxChannels]; // Number of grid points in each dimension.

        if (mpe is CLutStage<float>)
        {
            Context.LogError(
                self.ContextID,
                ErrorCodes.NotSuitable,
                "Cannot save floating point data, CLUT are 8 or 16 bit only");
            return false;
        }

        if (mpe is not CLutStage<ushort> CLUT)
            return false;

        //memset(gridPoints, 0, sizeof(byte) * cmsMAXCHANNELS);
        for (var i = 0; i < CLUT.Params.nInputs; i++)
            gridPoints[i] = (byte)CLUT.Params.nSamples[i];

        if (!io.WriteFunc(io, (uint)(Context.MaxChannels * sizeof(byte)), gridPoints))
            return false;

        if (!io.Write(Precision))
            return false;
        if (!io.Write((byte)0))
            return false;
        if (!io.Write((byte)0))
            return false;
        if (!io.Write((byte)0))
            return false;

        // Precision can be 1 or 2 bytes
        switch (Precision)
        {
            case 1:
                for (var i = 0; i < CLUT.Tab.Length; i++)
                    if (!io.Write(FROM_16_TO_8(CLUT.TUshort[i])))
                        return false;
                break;

            case 2:
                if (!io.Write((uint)CLUT.Tab.Length, CLUT.TUshort))
                    return false;
                break;

            default:
                Context.LogError(self.ContextID, ErrorCodes.UnknownExtension, $"Unknown precision of '{Precision}'");
                return false;
        }

        return io.WriteAlignment();
    }

    private static bool Type_LUTA2B_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Pipeline Lut)
            return false;

        Stage? A = null, B = null, M = null, Matrix = null, CLUT = null;
        uint offsetB = 0, offsetMat = 0, offsetM = 0, offsetC = 0, offsetA = 0;

        // Get the base for all offsets
        var BassOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        if (Lut.Elements is not null &&
            !Lut.CheckAndRetrieveStages(Signatures.Stage.CurveSetElem, out B) &&
            !Lut.CheckAndRetrieveStages(
                Signatures.Stage.CurveSetElem,
                out M,
                Signatures.Stage.MatrixElem,
                out Matrix,
                Signatures.Stage.CurveSetElem,
                out B) &&
            !Lut.CheckAndRetrieveStages(
                Signatures.Stage.CurveSetElem,
                out A,
                Signatures.Stage.CLutElem,
                out CLUT,
                Signatures.Stage.CurveSetElem,
                out B) &&
            !Lut.CheckAndRetrieveStages(
                Signatures.Stage.CurveSetElem,
                out A,
                Signatures.Stage.CLutElem,
                out CLUT,
                Signatures.Stage.CurveSetElem,
                out M,
                Signatures.Stage.MatrixElem,
                out Matrix,
                Signatures.Stage.CurveSetElem,
                out B))
        {
            Context.LogError(self.ContextID, ErrorCodes.NotSuitable, "LUT is not suitable to be saved as LutAToB");
            return false;
        }

        // Get input/output channels
        var inputChan = Lut.InputChannels;
        var outputChan = Lut.OutputChannels;

        // Write channel count
        if (!io.Write((byte)inputChan))
            return false;
        if (!io.Write((byte)outputChan))
            return false;
        if (!io.Write((ushort)0))
            return false;

        // Keep directory location to be filled later
        var DirectoryPos = io.TellFunc(io);

        // Write the directory
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;

        if (A is not null)
        {
            offsetA = io.TellFunc(io) - BassOffset;
            if (!WriteSetOfCurves(self, io, Signatures.TagType.ParametricCurve, A))
                return false;
        }

        if (CLUT is not null)
        {
            offsetC = io.TellFunc(io) - BassOffset;
            if (!WriteCLUT(self, io, (byte)(Lut.SaveAs8Bits ? 1 : 2), CLUT))
                return false;
        }

        if (M is not null)
        {
            offsetM = io.TellFunc(io) - BassOffset;
            if (!WriteSetOfCurves(self, io, Signatures.TagType.ParametricCurve, M))
                return false;
        }

        if (Matrix is not null)
        {
            offsetMat = io.TellFunc(io) - BassOffset;
            if (!WriteMatrix(self, io, Matrix))
                return false;
        }

        if (B is not null)
        {
            offsetB = io.TellFunc(io) - BassOffset;
            if (!WriteSetOfCurves(self, io, Signatures.TagType.ParametricCurve, B))
                return false;
        }

        var CurrentPos = io.TellFunc(io);

        if (!io.SeekFunc(io, DirectoryPos))
            return false;

        if (!io.Write(offsetB))
            return false;
        if (!io.Write(offsetMat))
            return false;
        if (!io.Write(offsetM))
            return false;
        if (!io.Write(offsetC))
            return false;
        if (!io.Write(offsetA))
            return false;

        return io.SeekFunc(io, CurrentPos);
    }

    private static Pipeline? Type_LUTA2B_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        (Ptr as Pipeline)?.Clone();

    private static void Type_LUTA2B_Free(TagTypeHandler _1, object? Ptr) =>
        (Ptr as Pipeline)?.Dispose();

    #endregion LUTA2B

    #region LUTB2A

    private static Pipeline? Type_LUTB2A_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Pipeline? NewLUT = null;

        nItems = 0;

        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        if (!io.ReadByte(out var inputChan))
            goto Error;  // Number of input channels
        if (!io.ReadByte(out var outputChan))
            goto Error; // Number of output channels

        if (!io.ReadUshort(out _))
            goto Error;

        if (!io.ReadUint(out var offsetB))
            goto Error;   // Offset to first "B" curve
        if (!io.ReadUint(out var offsetMat))
            goto Error; // Offset to matrix
        if (!io.ReadUint(out var offsetM))
            goto Error;   // Offset to first "M" curve
        if (!io.ReadUint(out var offsetC))
            goto Error;   // Offset to CLUT
        if (!io.ReadUint(out var offsetA))
            goto Error;   // Offset to first "A" curve

        // Do some checking
        if (inputChan is 0 or > Context.MaxChannels)
            goto Error;
        if (outputChan is 0 or > Context.MaxChannels)
            goto Error;

        // Allocates an empty LUT
        NewLUT = new Pipeline(self.ContextID, inputChan, outputChan);
        if (NewLUT is null)
            goto Error;

        if (offsetB is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadSetOfCurves(self, io, BaseOffset + offsetB, inputChan)))
                goto Error;
        }

        if (offsetMat is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadMatrix(self, io, BaseOffset + offsetMat)))
                goto Error;
        }

        if (offsetM is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadSetOfCurves(self, io, BaseOffset + offsetM, inputChan)))
                goto Error;
        }

        if (offsetC is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadCLUT(self, io, BaseOffset + offsetC, inputChan, outputChan)))
                goto Error;
        }

        if (offsetA is not 0)
        {
            if (!NewLUT.InsertStageAtEnd(ReadSetOfCurves(self, io, BaseOffset + offsetA, outputChan)))
                goto Error;
        }

        nItems = 1;
        return NewLUT;

    Error:
        return null;
    }

    private static bool Type_LUTB2A_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Pipeline Lut)
            return false;

        Stage? A = null, B = null, M = null, Matrix = null, CLUT = null;
        uint offsetB = 0, offsetMat = 0, offsetM = 0, offsetC = 0, offsetA = 0;

        // Get the base for all offsets
        var BassOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        if (Lut.Elements is not null &&
            !Lut.CheckAndRetrieveStages(Signatures.Stage.CurveSetElem, out B) &&
            !Lut.CheckAndRetrieveStages(
                Signatures.Stage.CurveSetElem,
                out B,
                Signatures.Stage.MatrixElem,
                out Matrix,
                Signatures.Stage.CurveSetElem,
                out M) &&
            !Lut.CheckAndRetrieveStages(
                Signatures.Stage.CurveSetElem,
                out B,
                Signatures.Stage.CLutElem,
                out CLUT,
                Signatures.Stage.CurveSetElem,
                out A) &&
            !Lut.CheckAndRetrieveStages(
                Signatures.Stage.CurveSetElem,
                out B,
                Signatures.Stage.MatrixElem,
                out Matrix,
                Signatures.Stage.CurveSetElem,
                out M,
                Signatures.Stage.CLutElem,
                out CLUT,
                Signatures.Stage.CurveSetElem,
                out A))
        {
            Context.LogError(self.ContextID, ErrorCodes.NotSuitable, "LUT is not suitable to be saved as LutAToB");
            return false;
        }

        // Get input/output channels
        var inputChan = Lut.InputChannels;
        var outputChan = Lut.OutputChannels;

        // Write channel count
        if (!io.Write((byte)inputChan))
            return false;
        if (!io.Write((byte)outputChan))
            return false;
        if (!io.Write((ushort)0))
            return false;

        // Keep directory location to be filled later
        var DirectoryPos = io.TellFunc(io);

        // Write the directory
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((uint)0))
            return false;

        if (A is not null)
        {
            offsetA = io.TellFunc(io) - BassOffset;
            if (!WriteSetOfCurves(self, io, Signatures.TagType.ParametricCurve, A))
                return false;
        }

        if (CLUT is not null)
        {
            offsetC = io.TellFunc(io) - BassOffset;
            if (!WriteCLUT(self, io, (byte)(Lut.SaveAs8Bits ? 1 : 2), CLUT))
                return false;
        }

        if (M is not null)
        {
            offsetM = io.TellFunc(io) - BassOffset;
            if (!WriteSetOfCurves(self, io, Signatures.TagType.ParametricCurve, M))
                return false;
        }

        if (Matrix is not null)
        {
            offsetMat = io.TellFunc(io) - BassOffset;
            if (!WriteMatrix(self, io, Matrix))
                return false;
        }

        if (B is not null)
        {
            offsetB = io.TellFunc(io) - BassOffset;
            if (!WriteSetOfCurves(self, io, Signatures.TagType.ParametricCurve, B))
                return false;
        }

        var CurrentPos = io.TellFunc(io);

        if (!io.SeekFunc(io, DirectoryPos))
            return false;

        if (!io.Write(offsetB))
            return false;
        if (!io.Write(offsetMat))
            return false;
        if (!io.Write(offsetM))
            return false;
        if (!io.Write(offsetC))
            return false;
        if (!io.Write(offsetA))
            return false;

        return io.SeekFunc(io, CurrentPos);
    }

    private static Pipeline? Type_LUTB2A_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        (Ptr as Pipeline)?.Clone();

    private static void Type_LUTB2A_Free(TagTypeHandler _1, object? Ptr) =>
        (Ptr as Pipeline)?.Dispose();

    #endregion LUTB2A

    #region ColorantTable

    private static NamedColorList? Type_ColorantTable_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Span<byte> Name = stackalloc byte[34];
        Span<ushort> PCS = stackalloc ushort[3];
        NamedColorList? List = null;
        Span<byte> preSufix = stackalloc byte[1] { 0 };

        nItems = 0;
        if (!io.ReadUint(out var Count))
            return null;

        if (Count > Context.MaxChannels)
        {
            Context.LogError(self.ContextID, ErrorCodes.Range, $"Too many colorants '{Count}'");
            return null;
        }

        List = cmsAllocNamedColorList(self.ContextID, Count, 0, preSufix, preSufix);
        if (List is null)
            return null;

        for (var i = 0; i < Count; i++)
        {
            if (io.ReadFunc(io, Name, 32, 1) is not 1)
                goto Error;
            Name[32] = 0;

            if (!io.ReadUshortArray(3, PCS))
                goto Error;

            if (!cmsAppendNamedColor(List, Name, PCS, null))
                goto Error;
        }

        nItems = 1;

        return List;

    Error:
        cmsFreeNamedColorList(List);
        return null;
    }

    private static bool Type_ColorantTable_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        Span<byte> root = stackalloc byte[MaxPath];
        Span<ushort> PCS = stackalloc ushort[3];

        if (Ptr is not NamedColorList NamedColorList)
            return false;

        var nColors = cmsNamedColorCount(NamedColorList);

        if (!io.Write(nColors))
            return false;

        for (var i = 0u; i < nColors; i++)
        {
            //memset(root, 0, cmsMAX_PATH * sizeof(byte));

            if (!cmsNamedColorInfo(NamedColorList, i, root, null, null, PCS, null))
                return false;
            root[32] = 0;

            if (!io.WriteFunc(io, 32, root))
                return false;
            if (!io.Write(3, PCS))
                return false;
        }

        return true;
    }

    #endregion ColorantTable

    #region NamedColor

    private static NamedColorList? Type_NamedColor_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        uint vendorFlag;                         // Bottom 16 bits for ICC use
        uint count;                              // Count of named colors
        uint nDeviceCoords;                      // Num of device coordinates
        Span<byte> prefix = stackalloc byte[32]; // Prefix for each color name
        Span<byte> suffix = stackalloc byte[32]; // Suffix for each color name
        NamedColorList? v = null;
        Span<ushort> PCS = stackalloc ushort[3];
        Span<ushort> Colorant = stackalloc ushort[Context.MaxChannels];
        Span<byte> Root = stackalloc byte[33];

        nItems = 0;
        if (!io.ReadUint(out vendorFlag))
            return null;
        if (!io.ReadUint(out count))
            return null;
        if (!io.ReadUint(out nDeviceCoords))
            return null;

        if (io.ReadFunc(io, prefix, 32, 1) is not 1)
            return null;
        if (io.ReadFunc(io, suffix, 32, 1) is not 1)
            return null;

        prefix[31] = suffix[31] = 0;

        v = cmsAllocNamedColorList(self.ContextID, count, nDeviceCoords, prefix, suffix);
        if (v is null)
        {
            Context.LogError(self.ContextID, ErrorCodes.Range, $"Too many named colors '{count}'");
            return null;
        }

        if (nDeviceCoords > Context.MaxChannels)
        {
            Context.LogError(self.ContextID, ErrorCodes.Range, $"Too many device coordinates '{nDeviceCoords}'");
            goto Error;
        }

        for (var i = 0; i < count; i++)
        {
            //memset(Colorant, 0, sizeof(ushort) * cmsMAXCHANNELS);
            if (io.ReadFunc(io, Root, 32, 1) is not 1)
                goto Error;
            Root[32] = 0;

            if (!io.ReadUshortArray(3, PCS))
                goto Error;
            if (!io.ReadUshortArray(nDeviceCoords, Colorant))
                goto Error;

            if (!cmsAppendNamedColor(v, Root, PCS, Colorant))
                goto Error;
        }

        nItems = 1;

        return v;

    Error:
        cmsFreeNamedColorList(v);
        return null;
    }

    private static bool Type_NamedColor_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        Span<byte> prefix = stackalloc byte[33];
        Span<byte> suffix = stackalloc byte[33];
        Span<byte> Root = stackalloc byte[MaxPath];
        Span<ushort> PCS = stackalloc ushort[3];
        Span<ushort> Colorant = stackalloc ushort[Context.MaxChannels];

        if (Ptr is not NamedColorList NamedColorList)
            return false;

        var nColors = cmsNamedColorCount(NamedColorList);

        if (!io.Write((uint)0))
            return false;
        if (!io.Write(nColors))
            return false;
        if (!io.Write(NamedColorList.ColorantCount))
            return false;

        memcpy(prefix, NamedColorList.Prefix, 33);
        memcpy(suffix, NamedColorList.Suffix, 33);

        suffix[32] = prefix[32] = 0;

        if (!io.WriteFunc(io, 32, prefix))
            return false;
        if (!io.WriteFunc(io, 32, suffix))
            return false;

        for (var i = 0u; i < nColors; i++)
        {
            Root.Clear();
            PCS.Clear();
            Colorant.Clear();

            if (!cmsNamedColorInfo(NamedColorList, i, Root, null, null, PCS, Colorant))
                return false;
            Root[32] = 0;
            if (!io.WriteFunc(io, 32, Root))
                return false;
            if (!io.Write(3, PCS))
                return false;
            if (!io.Write(NamedColorList.ColorantCount, Colorant))
                return false;
        }

        return true;
    }

    #endregion NamedColor

    #region ProfileSequenceDesc

    private static bool ReadEmbeddedText(TagTypeHandler self, IOHandler io, out Mlu? mlu, uint SizeOfTag)
    {
        var sig = io.ReadTypeBase();
        if (sig == Signatures.TagType.Text)
        {
            //if (*mlu is not null) cmsMLUfree(*mlu);
            mlu = Type_Text_Read(self, io, out _, SizeOfTag);
            return mlu is not null;
        }

        if (sig == Signatures.TagType.TextDescription)
        {
            //if (*mlu is not null) cmsMLUfree(*mlu);
            mlu = Type_Text_Description_Read(self, io, out _, SizeOfTag);
            return mlu is not null;
        }
        /*
            TBD: Size is needed for MLU, and we have no idea on which is the available size
            */

        if (sig == Signatures.TagType.MultiLocalizedUnicode)
        {
            //if (*mlu is not null) cmsMLUfree(*mlu);
            mlu = Type_MLU_Read(self, io, out _, SizeOfTag);
            return mlu is not null;
        }

        mlu = null;
        return false;
    }

    private static Sequence? Type_ProfileSequenceDesc_Read(TagTypeHandler self,
                                                           IOHandler io,
                                                           out uint nItems,
                                                           uint SizeOfTag)
    {
        nItems = 0;

        if (!io.ReadUint(out var Count))
            return null;

        if (SizeOfTag < sizeof(uint))
            return null;
        SizeOfTag -= sizeof(uint);

        var OutSeq = cmsAllocProfileSequenceDescription(self.ContextID, Count);
        if (OutSeq is null)
            return null;

        OutSeq.n = Count;

        // Get structures as well

        for (var i = 0; i < Count; i++)
        {
            var sec = OutSeq.seq[i];

            if (!_cmsReadSignature(io, out sec.deviceMfg))
                goto Error;
            if (SizeOfTag < sizeof(uint))
                goto Error;
            SizeOfTag -= sizeof(uint);

            if (!_cmsReadSignature(io, out sec.deviceModel))
                goto Error;
            if (SizeOfTag < sizeof(uint))
                goto Error;
            SizeOfTag -= sizeof(uint);

            if (!io.ReadUlong(out sec.attributes))
                goto Error;
            if (SizeOfTag < sizeof(ulong))
                goto Error;
            SizeOfTag -= sizeof(ulong);

            if (!_cmsReadSignature(io, out sec.technology))
                goto Error;
            if (SizeOfTag < sizeof(uint))
                goto Error;
            SizeOfTag -= sizeof(uint);

            if (!ReadEmbeddedText(self, io, out sec.Manufacturer, SizeOfTag))
                goto Error;
            if (!ReadEmbeddedText(self, io, out sec.Model, SizeOfTag))
                goto Error;
        }

        nItems = 1;

        return OutSeq;

    Error:
        cmsFreeProfileSequenceDescription(OutSeq);
        return null;
    }

    private static bool SaveDescription(TagTypeHandler self, IOHandler io, Mlu Text)
    {
        if (self.ICCVersion < 0x04000000)
        {
            if (!io.WriteTypeBase(Signatures.TagType.TextDescription))
                return false;
            return Type_Text_Description_Write(self, io, Text, 1);
        }
        else
        {
            if (!io.WriteTypeBase(Signatures.TagType.MultiLocalizedUnicode))
                return false;
            return Type_MLU_Write(self, io, Text, 1);
        }
    }

    private static bool Type_ProfileSequenceDesc_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Sequence Seq)
            return false;

        if (!io.Write(Seq.n))
            return false;

        for (var i = 0u; i < Seq.n; i++)
        {
            var sec = Seq.seq[i];

            if (!io.Write((uint)sec.deviceMfg))
                return false;
            if (!io.Write((uint)sec.deviceModel))
                return false;
            if (!io.Write(sec.attributes))
                return false;
            if (!io.Write((uint)sec.technology))
                return false;

            if (!SaveDescription(self, io, sec.Manufacturer))
                return false;
            if (!SaveDescription(self, io, sec.Model))
                return false;
        }

        return true;
    }

    #endregion ProfileSequenceDesc

    #region ProfileSequenceId

    private static bool ReadSeqID(TagTypeHandler self, IOHandler io, object? Cargo, uint n, uint SizeOfTag)
    {
        if (Cargo is not Sequence OutSeq)
            return false;

        var seq = OutSeq.seq[n];
        Span<byte> buffer = stackalloc byte[16];
        if (io.ReadFunc(io, buffer, 16, 1) is not 1)
            return false;
        seq.ProfileID = ProfileID.Set(buffer);

        return ReadEmbeddedText(self, io, out seq.Description, SizeOfTag);
    }

    private static Sequence? Type_ProfileSequenceId_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        nItems = 0;

        // Get actual position as a basis for element offsets
        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        // Get table count
        if (!io.ReadUint(out var Count))
            return null;

        // Allocate an empty structure
        var OutSeq = cmsAllocProfileSequenceDescription(self.ContextID, Count);
        if (OutSeq is null)
            return null;

        // Read the position table
        if (!ReadPositionTable(self, io, Count, BaseOffset, OutSeq, ReadSeqID))
        {
            cmsFreeProfileSequenceDescription(OutSeq);
            return null;
        }

        // Success
        nItems = 1;
        return OutSeq;
    }

    private static bool WriteSeqID(TagTypeHandler self, IOHandler io, object? Cargo, uint n, uint _1)
    {
        if (Cargo is not Sequence Seq)
            return false;

        Span<byte> buffer = stackalloc byte[16];
        Seq.seq[n].ProfileID.Get(buffer);
        if (!io.WriteFunc(io, 16, buffer))
            return false;

        // Store MLU here
        if (!SaveDescription(self, io, Seq.seq[n].Description))
            return false;

        return true;
    }

    private static bool Type_ProfileSequenceId_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Sequence Seq)
            return false;

        // Keep the base offset
        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        // This is the table count
        if (!io.Write(Seq.n))
            return false;

        // This is the position table and content
        if (!WritePositionTable(self, io, 0, Seq.n, BaseOffset, Seq, WriteSeqID))
            return false;

        return true;
    }

    #endregion ProfileSequenceId

    #region UcrBg

    private static Box<UcrBg>? Type_UcrBg_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        int SignedSizeOfTag = (int)SizeOfTag;

        nItems = 0;
        var n = new UcrBg();
        //if (n is null) return null;

        // First curve is Under color removal

        if (SignedSizeOfTag < sizeof(uint))
            return null;
        if (!io.ReadUint(out var CountUcr))
            return null;
        SignedSizeOfTag -= sizeof(uint);

        n.Ucr = ToneCurve.BuildTabulated(self.ContextID, CountUcr, ReadOnlySpan<ushort>.Empty);
        if (n.Ucr is null)
            goto Error;

        if (SignedSizeOfTag < (int)(CountUcr * sizeof(ushort)))
            goto Error;
        if (!io.ReadUshortArray(CountUcr, n.Ucr.Table16))
            goto Error;

        SignedSizeOfTag -= (int)(CountUcr * sizeof(ushort));

        // Secong curve is Black generation

        if (SignedSizeOfTag < sizeof(uint))
            goto Error;
        if (!io.ReadUint(out var CountBg))
            goto Error;
        SignedSizeOfTag -= sizeof(uint);

        n.Bg = ToneCurve.BuildTabulated(self.ContextID, CountBg, ReadOnlySpan<ushort>.Empty);
        if (n.Bg is null)
            goto Error;

        if (SignedSizeOfTag < (int)(CountBg * sizeof(ushort)))
            goto Error;
        if (!io.ReadUshortArray(CountBg, n.Bg.Table16))
            goto Error;
        SignedSizeOfTag -= (int)(CountBg * sizeof(ushort));

        if (SignedSizeOfTag is < 0 or > 32000)
            goto Error;

        // Now comes the text. The length is specified by the tag size
        var desc = cmsMLUalloc(self.ContextID, 1);
        if (desc is null)
            goto Error;
        n.Desc = desc;

        //ASCIIString = _cmsMalloc<byte>(self.ContextID, (uint)SignedSizeOfTag + 1);
        //var ASCIIString = GetArray<byte>(self.ContextID, (uint)SignedSizeOfTag);
        var ASCIIString = new byte[SignedSizeOfTag];
        //var tmpASCIIString = stackalloc byte[SignedSizeOfTag];
        if (io.ReadFunc(io, ASCIIString, sizeof(byte), (uint)SignedSizeOfTag) != SignedSizeOfTag)
        {
            //ReturnArray(self.ContextID, ASCIIString);
            goto Error;
        }
        //new Span<byte>(tmpASCIIString, SignedSizeOfTag).CopyTo(ASCIIString.AsSpan(..SignedSizeOfTag));

        //ASCIIString[SignedSizeOfTag] = 0;
        cmsMLUsetASCII(n.Desc, cmsNoLanguage, cmsNoCountry, ASCIIString);
        //ReturnArray(self.ContextID, ASCIIString);

        nItems = 1;
        return new(n);

    Error:
        return null;
    }

    private static bool Type_UcrBg_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Box<UcrBg> Value)
            return false;

        // First curve is Under color removal
        if (!io.Write(Value.Value.Ucr.nEntries))
            return false;
        if (!io.Write(Value.Value.Ucr.nEntries, Value.Value.Ucr.Table16))
            return false;

        // Then black generation
        if (!io.Write(Value.Value.Bg.nEntries))
            return false;
        if (!io.Write(Value.Value.Bg.nEntries, Value.Value.Bg.Table16))
            return false;

        // Now comes the text. The length is specified by the tag size
        var TextSize = cmsMLUgetASCII(Value.Value.Desc, cmsNoLanguage, cmsNoCountry, null);
        //Text = _cmsMalloc<byte>(self.ContextID, TextSize);
        //var Text = GetArray<byte>(self.ContextID, TextSize);
        var Text = new byte[TextSize];
        if (cmsMLUgetASCII(Value.Value.Desc, cmsNoLanguage, cmsNoCountry, Text.AsSpan()[..(int)TextSize]) != TextSize)
            return false;

        //var tmp = stackalloc char[(int)TextSize];
        //Text.AsSpan()[..(int)TextSize].CopyTo(new(tmp, (int)TextSize));
        if (!io.WriteFunc(io, TextSize, Text))
            return false;
        //ReturnArray(self.ContextID, Text);

        return true;
    }

    private static Box<UcrBg>? Type_UcrBg_Dup(TagTypeHandler self, object? Ptr, uint _1)
    {
        if (Ptr is not Box<UcrBg> Src)
            return null;
        var NewUcrBg = new UcrBg
        {
            //if (NewUcrBg is null) return null;

            Bg = Src.Value.Bg.Clone(), Ucr = Src.Value.Ucr.Clone(), Desc = cmsMLUdup(Src.Value.Desc)!
        };

        if (NewUcrBg.Bg is null ||
            NewUcrBg.Ucr is null ||
            NewUcrBg.Desc is null)
        {
            goto Error;
        }

        return new(NewUcrBg);

    Error:
        Type_UcrBg_Free(self, new Box<UcrBg>(NewUcrBg));
        return null;
    }

    private static void Type_UcrBg_Free(TagTypeHandler self, object? Ptr) { }

    #endregion UcrBg

    #region CrdInfo

    private static bool ReadCountAndString(TagTypeHandler self,
                                           IOHandler io,
                                           Mlu mlu,
                                           ref uint SizeOfTag,
                                           ReadOnlySpan<byte> Section)
    {
        var ps = "PS"u8;

        if (SizeOfTag < sizeof(uint))
            return false;

        if (!io.ReadUint(out var Count))
            return false;

        if (Count > UInt32.MaxValue - sizeof(uint))
            return false;
        if (SizeOfTag < Count + sizeof(uint))
            return false;

        var Text = new byte[Count];

        if (io.ReadFunc(io, Text, sizeof(byte), Count) != Count)
        {
            return false;
        }

        cmsMLUsetASCII(mlu, ps, Section, Text.AsSpan(..(int)Count));

        SizeOfTag -= Count + sizeof(uint);
        return true;
    }

    private static bool WriteCountAndString(TagTypeHandler self, IOHandler io, Mlu mlu, ReadOnlySpan<byte> Section)
    {
        var ps = "PS"u8;

        var TextSize = cmsMLUgetASCII(mlu, ps, Section, null);
        var Text = new byte[TextSize];

        if (!io.Write(TextSize))
            goto Error;

        if (cmsMLUgetASCII(mlu, ps, Section, Text.AsSpan(..(int)TextSize)) is 0)
            goto Error;

        //var tmp = stackalloc byte[(int)TextSize];
        //Text.AsSpan()[..(int)TextSize].CopyTo(new(tmp, (int)TextSize));
        if (!io.WriteFunc(io, TextSize, Text))
            goto Error;
        //ReturnArray(self.ContextID, Text);

        return true;

    Error:
        //ReturnArray(self.ContextID, Text);
        return false;
    }

    private static Mlu? Type_CrdInfo_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        var nm = "nm"u8;
        var n0 = "#0"u8;
        var n1 = "#1"u8;
        var n2 = "#2"u8;
        var n3 = "#3"u8;

        nItems = 0;
        var mlu = cmsMLUalloc(self.ContextID, 5);
        if (mlu is null)
            return null;

        if (!ReadCountAndString(self, io, mlu, ref SizeOfTag, nm))
            goto Error;
        if (!ReadCountAndString(self, io, mlu, ref SizeOfTag, n0))
            goto Error;
        if (!ReadCountAndString(self, io, mlu, ref SizeOfTag, n1))
            goto Error;
        if (!ReadCountAndString(self, io, mlu, ref SizeOfTag, n2))
            goto Error;
        if (!ReadCountAndString(self, io, mlu, ref SizeOfTag, n3))
            goto Error;

        nItems = 1;
        return mlu;

    Error:
        cmsMLUfree(mlu);
        return null;
    }

    private static bool Type_CrdInfo_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        var nm = "nm"u8;
        var n0 = "#0"u8;
        var n1 = "#1"u8;
        var n2 = "#2"u8;
        var n3 = "#3"u8;

        if (Ptr is not Mlu mlu)
            return false;

        if (!WriteCountAndString(self, io, mlu, nm))
            return false;
        if (!WriteCountAndString(self, io, mlu, n0))
            return false;
        if (!WriteCountAndString(self, io, mlu, n1))
            return false;
        if (!WriteCountAndString(self, io, mlu, n2))
            return false;
        if (!WriteCountAndString(self, io, mlu, n3))
            return false;

        return true;
    }

    private static Mlu? Type_CrdInfo_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Mlu mlu
            ? cmsMLUdup(mlu)
            : null;

    private static void Type_CrdInfo_Free(TagTypeHandler _1, object? Ptr) =>
        cmsMLUfree(Ptr as Mlu);

    #endregion CrdInfo

    #region Screening

    private static Box<Screening>? Type_Screening_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _)
    {
        nItems = 0;

        var sc = new Screening(self.ContextID); //var sc = _cmsMallocZero<Screening>(self.ContextID);
        //if (sc is null) return null;

        if (!io.ReadUint(out sc.Flag))
            goto Error;
        if (!io.ReadUint(out sc.nChannels))
            goto Error;

        if (sc.nChannels > Context.MaxChannels - 1)
            sc.nChannels = Context.MaxChannels - 1;

        for (var i = 0; i < sc.nChannels; i++)
        {
            if (!io.ReadFixed15_16(out sc.Channels[i].Frequency))
                goto Error;
            if (!io.ReadFixed15_16(out sc.Channels[i].ScreenAngle))
                goto Error;
            if (!io.ReadUint(out sc.Channels[i].SpotShape))
                goto Error;
        }

        nItems = 1;
        return new(sc);

    Error:
        //ReturnArray(self.ContextID, sc.channels); //_cmsFree(self.ContextID, sc);

        return null;
    }

    private static bool Type_Screening_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not Box<Screening> sc)
            return false;

        if (!io.Write(sc.Value.Flag))
            return false;
        if (!io.Write(sc.Value.nChannels))
            return false;

        if (sc.Value.nChannels > Context.MaxChannels - 1)
            sc.Value.nChannels = Context.MaxChannels - 1;

        for (var i = 0; i < sc.Value.nChannels; i++)
        {
            if (!io.Write(sc.Value.Channels[i].Frequency))
                return false;
            if (!io.Write(sc.Value.Channels[i].ScreenAngle))
                return false;
            if (!io.Write(sc.Value.Channels[i].SpotShape))
                return false;
        }

        return true;
    }

    private static Box<Screening>? Type_Screening_Dup(TagTypeHandler _1, object? Ptr, uint _2)
    {
        if (Ptr is not Box<Screening> s)
            return null;

        var result = new Box<Screening>(s);
        s.Value.Channels.CopyTo(result.Value.Channels);
        return result;
    }

    private static void Type_Screening_Free(TagTypeHandler self, object? Ptr)
    {
        //if (Ptr is Box<Screening> s)
        //ReturnArray(self.ContextID, s.Value.channels); //_cmsFree(self.ContextID, s);
    }

    #endregion Screening

    #region ViewingConditions

    private static Box<IccViewingConditions>? Type_ViewingConditions_Read(TagTypeHandler _1,
                                                                          IOHandler io,
                                                                          out uint nItems,
                                                                          uint _2)
    {
        nItems = 0;

        var vc = new IccViewingConditions(); //var vc = _cmsMallocZero<IccViewingConditions>(self.ContextID);
        //if (vc is null) return null;

        if (!io.ReadXYZ(out vc.IlluminantXYZ))
            goto Error;
        if (!io.ReadXYZ(out vc.SurroundXYZ))
            goto Error;
        if (!io.ReadUint(out var illuminant))
            goto Error;
        vc.IlluminantType = (IlluminantType)illuminant;

        nItems = 1;
        return new(vc);

    Error:
        //_cmsFree(self.ContextID, vc);

        return null;
    }

    private static bool Type_ViewingConditions_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not Box<IccViewingConditions> sc)
            return false;

        return io.Write(sc.Value.IlluminantXYZ) &&
               io.Write(sc.Value.SurroundXYZ) &&
               io.Write((uint)sc.Value.IlluminantType);
    }

    private static Box<IccViewingConditions>? Type_ViewingConditions_Dup(TagTypeHandler self, object? Ptr, uint _) =>
        Ptr is Box<IccViewingConditions> sc
            ? new(sc)
            : null;

    #endregion ViewingConditions

    #region MPE

    private static bool ReadMPEElem(TagTypeHandler self, IOHandler io, object? Cargo, uint _1, uint SizeOfTag)
    {
        if (Cargo is not Pipeline NewLUT)
            return false;

        var MPETypePluginChunk = Context.Get(self.ContextID).MPEPlugin;

        // Take signature and channels for each element.
        if (!_cmsReadSignature(io, out var ElementSig))
            return false;

        // The reserved placeholder
        if (!io.ReadUint(out _))
            return false;

        // Read diverse MPE types
        var TypeHandler = GetHandler(ElementSig, MPETypePluginChunk.List, supportedMPEtypes);
        if (TypeHandler is null)
        {
            // An unknown element was found.
            Context.LogError(self.ContextID, cmsERROR_UNKNOWN_EXTENSION, $"Unknown MPE type '{(ElementSig)}' found.");
            return false;
        }

        // If no read method, just ignore the element (valid for cmsSigBAcsElemType and cmsSigEAcsElemType)
        // Read the MPE. No size is given
        if (TypeHandler.ReadPtr is not null)
        {
            // This is a real element which should be read and processed
            var stage = (Stage?)TypeHandler.ReadPtr(self, io, out _, SizeOfTag);
            if (stage is null || !NewLUT.InsertStageAtEnd(stage))
                return false;
        }

        return true;
    }

    private static Pipeline? Type_MPE_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        nItems = 0;

        // Get actual position as a basis for element offsets
        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        // Read channels and element count
        if (!io.ReadUshort(out var InputChans))
            return null;
        if (!io.ReadUshort(out var OutputChans))
            return null;

        if (InputChans is 0 or >= Context.MaxChannels)
            return null;
        if (OutputChans is 0 or >= Context.MaxChannels)
            return null;

        // Allocates an empty LUT
        var NewLUT = new Pipeline(self.ContextID, InputChans, OutputChans);
        if (NewLUT is null)
            return null;

        if (!io.ReadUint(out var ElementCount))
            goto Error;
        if (!ReadPositionTable(self, io, ElementCount, BaseOffset, NewLUT, ReadMPEElem))
            goto Error;

        // Check channel count
        if (InputChans != NewLUT.InputChannels ||
            OutputChans != NewLUT.OutputChannels)
            goto Error;

        // Success
        nItems = 1;
        return NewLUT;

        // Error
    Error:
        return null;
    }

    private static bool Type_MPE_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Pipeline Lut)
            return false;

        var Elem = Lut.Elements;
        var MPETypePluginChunk = Context.Get(self.ContextID).MPEPlugin;

        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        var inputChan = Lut.InputChannels;
        var outputChan = Lut.OutputChannels;
        var ElemCount = Lut.StageCount;

        var ElementOffsets = new uint[ElemCount];

        var ElementSizes = new uint[ElemCount];

        // Write the head
        if (!io.Write((ushort)inputChan))
            goto Error;
        if (!io.Write((ushort)outputChan))
            goto Error;
        if (!io.Write((uint)(ushort)ElemCount))
            goto Error;

        var DirectoryPos = io.TellFunc(io);

        // Write a fake directory to be filled later on
        for (var i = 0; i < ElemCount; i++)
        {
            if (!io.Write((uint)0))
                goto Error;  // Offset
            if (!io.Write((uint)0))
                goto Error;  // Size
        }

        // Write each single tag. Keep track of the size as well.
        for (var i = 0; i < ElemCount; i++)
        {
            ElementOffsets[i] = io.TellFunc(io) - BaseOffset;

            var ElementSig = Elem.Type;

            var TypeHandler = GetHandler(ElementSig, MPETypePluginChunk.List, supportedMPEtypes);
            if (TypeHandler is null)
            {
                // An unknown element was found.
                Context.LogError(
                    self.ContextID,
                    cmsERROR_UNKNOWN_EXTENSION,
                    $"Found unknown MPE type '{(ElementSig)}'");
                goto Error;
            }

            if (!io.Write((uint)ElementSig))
                goto Error;
            if (!io.Write((uint)0))
                goto Error;
            var Before = io.TellFunc(io);
            if (!TypeHandler.WritePtr(self, io, Elem, 1))
                goto Error;
            if (!io.WriteAlignment())
                goto Error;

            ElementSizes[i] = io.TellFunc(io) - Before;

            Elem = Elem.Next;
        }

        // Write the directory
        var CurrentPos = io.TellFunc(io);

        if (!io.SeekFunc(io, DirectoryPos))
            goto Error;

        for (var i = 0; i < ElemCount; i++)
        {
            if (!io.Write(ElementOffsets[i]))
                goto Error;
            if (!io.Write(ElementSizes[i]))
                goto Error;
        }

        if (!io.SeekFunc(io, CurrentPos))
            goto Error;

        return true;

    Error:
        return false;
    }

    private static Pipeline? Type_MPE_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        (Ptr as Pipeline)?.Clone();

    private static void Type_MPE_Free(TagTypeHandler _1, object? Ptr) =>
        (Ptr as Pipeline)?.Dispose();

    #endregion MPE

    #region vcgt

    private const byte cmsVideoCardGammaTableType = 0;
    private const byte cmsVideoCardGammaFormulaType = 1;

    private struct VCGTGAMMA()
    {
        public double Gamma, Min, Max;
    }

    private static ToneCurve[]? Type_vcgt_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        nItems = 0;

        // Read tag type
        if (!io.ReadUint(out var TagType))
            return null;

        // Allocate space for the array
        //Curves = (ToneCurve**)_cmsCalloc(self.ContextID, 3, _sizeof<nint>());
        //if (Curves is null) return null;
        //var Curves = Context.GetPool<ToneCurve>(self.ContextID).Rent(3);
        var Curves = new ToneCurve[3];

        // There are two possible flavors
        switch (TagType)
        {
            // Gamma is stored as a table
            case cmsVideoCardGammaTableType:
            {
                // Check channel count, which should be 3 (we don't support monochrome this time)
                if (!io.ReadUshort(out var nChannels))
                    goto Error;

                if (nChannels is not 3)
                {
                    Context.LogError(
                        self.ContextID,
                        cmsERROR_UNKNOWN_EXTENSION,
                        $"Unsupported number of channels for VCGT '{nChannels}'");
                    goto Error;
                }

                // Get Table element count and bytes per element
                if (!io.ReadUshort(out var nElems))
                    goto Error;
                if (!io.ReadUshort(out var nBytes))
                    goto Error;

                // Adobe's quirk fixup. Fixing broken profiles...
                if (nElems is 256 && nBytes is 1 && SizeOfTag is 1576)
                    nBytes = 2;

                // Populate tone curves
                for (var n = 0; n < 3; n++)
                {
                    Curves[n] = ToneCurve.BuildTabulated(self.ContextID, (uint)nElems, ReadOnlySpan<ushort>.Empty);
                    if (Curves[n] is null)
                        goto Error;

                    // Depending on byte depth...
                    switch (nBytes)
                    {
                        // One byte, 0..255
                        case 1:
                            for (var i = 0; i < nElems; i++)
                            {
                                if (!io.ReadByte(out var v))
                                    goto Error;
                                Curves[n].Table16[i] = FROM_8_TO_16(v);
                            }

                            break;

                        // One word 0..65535
                        case 2:
                            if (!io.ReadUshortArray(nElems, Curves[n].Table16))
                                goto Error;
                            break;

                        // Unsupported
                        default:
                            Context.LogError(
                                self.ContextID,
                                cmsERROR_UNKNOWN_EXTENSION,
                                $"Unsupported bit depth for VCGT '{nBytes * 8}'");
                            goto Error;
                    }
                } // For all 3 channels
            }
                break;

            // In this case, gamma is stored as a formula
            case 1:
            {
                Span<VCGTGAMMA> Colorant = stackalloc VCGTGAMMA[3];
                Span<double> Params = stackalloc double[10];

                //memset(Params, 0, 10 * sizeof(double));

                // Populate tone curves
                for (var n = 0; n < 3; n++)
                {
                    if (!io.ReadFixed15_16(out Colorant[n].Gamma))
                        goto Error;
                    if (!io.ReadFixed15_16(out Colorant[n].Min))
                        goto Error;
                    if (!io.ReadFixed15_16(out Colorant[n].Max))
                        goto Error;

                    // Parametric curve type 5 is:
                    // Y = (aX + b)^Gamma + e | X >= d
                    // Y = cX + f             | X < d

                    // vcgt formula is:
                    // Y = (Max - Min) * (X ^ Gamma) + Min

                    // So, the translation is
                    // a = (Max - Min) ^ ( 1 / Gamma)
                    // e = Min
                    // b=c=d=f=0

                    Params[0] = Colorant[n].Gamma;
                    Params[1] = Math.Pow(Colorant[n].Max - Colorant[n].Min, 1.0 / Colorant[n].Gamma);
                    Params[5] = Colorant[n].Min;
                    // Params[2,3,4,6] are 0 and will stay 0

                    Curves[n] = ToneCurve.BuildParametric(self.ContextID, 5, Params);
                    if (Curves[n] is null)
                        goto Error;
                }
            }
                break;

            // Unsupported
            default:
                Context.LogError(
                    self.ContextID,
                    cmsERROR_UNKNOWN_EXTENSION,
                    $"Unsupported tag type for VCGT '{TagType}'");
                goto Error;
        }

        nItems = 1;
        return Curves;

    Error:
        return null;
    }

    private static bool Type_vcgt_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not ToneCurve[] Curves)
            return false;

        if (Curves[0].ParametricType is 5 &&
            Curves[1].ParametricType is 5 &&
            Curves[2].ParametricType is 5)
        {
            if (!io.Write(cmsVideoCardGammaFormulaType))
                return false;

            // Save parameters
            for (var i = 0; i < 3; i++)
            {
                VCGTGAMMA v;

                v.Gamma = Curves[i].Segments[0].Params[0];
                v.Min = Curves[i].Segments[0].Params[5];
                v.Max = Math.Pow(Curves[i].Segments[0].Params[1], v.Gamma) + v.Min;

                if (!io.Write(v.Gamma))
                    return false;
                if (!io.Write(v.Min))
                    return false;
                if (!io.Write(v.Max))
                    return false;
            }
        }
        else
        {
            // Always store as a table of 256 words
            if (!io.Write(cmsVideoCardGammaTableType))
                return false;
            if (!io.Write((ushort)3))
                return false;
            if (!io.Write((ushort)256))
                return false;
            if (!io.Write((ushort)2))
                return false;

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 256; j++)
                {
                    var v = Curves[i].Evaluate((float)(j / 255.0));
                    var n = QuickSaturateWord(v * 65535.0);

                    if (!io.Write(n))
                        return false;
                }
            }
        }

        return true;
    }

    private static ToneCurve[]? Type_vcgt_Dup(TagTypeHandler self, object? Ptr, uint _)
    {
        if (Ptr is not ToneCurve[] OldCurves)
            return null;

        var NewCurves = new ToneCurve[3];

        NewCurves[0] = OldCurves[0].Clone();
        NewCurves[1] = OldCurves[1].Clone();
        NewCurves[2] = OldCurves[2].Clone();

        return NewCurves;
    }

    private static void Type_vcgt_Free(TagTypeHandler self, object? Ptr) { }

    #endregion vcgt

    #region Dictionary

    private struct DICelem
    {
        public Context? ContextID;
        public uint[] Offsets;
        public uint[] Sizes;
    }

    private struct DICarray
    {
        public DICelem Name, Value, DisplayName, DisplayValue;
    }

    private static bool AllocElem(Context? ContextID, out DICelem e, uint Count)
    {
        //var pool = Context.Get(ContextID).GetBufferPool<uint>();

        e = new DICelem
        {
            ContextID = ContextID,
            //Offsets = pool.Rent((int)Count),
            //Sizes = pool.Rent((int)Count)
            Offsets = new uint[Count],
            Sizes = new uint[Count]
        };
        //Array.Clear(e.Offsets);
        //Array.Clear(e.Sizes);

        //e.Offsets = _cmsCalloc<uint>(ContextID, Count);
        //if (e->Offsets is null) return false;

        //e->Sizes = _cmsCalloc<uint>(ContextID, Count);
        //if (e->Sizes is null)
        //{
        //    _cmsFree(ContextID, e->Offsets);
        //    return false;
        //}

        return true;
    }

    private static void FreeElem(ref DICelem e)
    {
        //if (e.Offsets is not null) ReturnArray(e.ContextID, e.Offsets);
        //if (e.Sizes is not null) ReturnArray(e.ContextID, e.Sizes);
        e.Offsets = e.Sizes = null!;
    }

    private static void FreeArray(ref DICarray a)
    {
        if (a.Name.Offsets is not null || a.Name.Sizes is not null)
            FreeElem(ref a.Name);
        if (a.Value.Offsets is not null || a.Value.Sizes is not null)
            FreeElem(ref a.Value);
        if (a.DisplayName.Offsets is not null || a.DisplayName.Sizes is not null)
            FreeElem(ref a.DisplayName);
        if (a.DisplayValue.Offsets is not null || a.DisplayValue.Sizes is not null)
            FreeElem(ref a.DisplayValue);
    }

    private static bool AllocArray(Context? ContextID, out DICarray a, uint Count, uint Length)
    {
        a = new();
        // Empty values
        //memset(a, 0, _sizeof<DICarray>());

        // Depending on record size, create column arrays
        if (!AllocElem(ContextID, out a.Name, Count))
            goto Error;
        if (!AllocElem(ContextID, out a.Value, Count))
            goto Error;

        if (Length > 16 && !AllocElem(ContextID, out a.DisplayName, Count))
            goto Error;

        if (Length > 24 && !AllocElem(ContextID, out a.DisplayValue, Count))
            goto Error;

        return true;

    Error:
        FreeArray(ref a);
        return false;
    }

    private static bool ReadOneElem(IOHandler io, ref DICelem e, uint i, uint BaseOffset)
    {
        if (!io.ReadUint(out e.Offsets[i]))
            return false;
        if (!io.ReadUint(out e.Sizes[i]))
            return false;

        // An offset of zero has special meaning and shall be preserved
        if (e.Offsets[i] > 0)
            e.Offsets[i] += BaseOffset;

        return true;
    }

    private static bool ReadOffsetArray(IOHandler io,
                                        ref DICarray a,
                                        uint Count,
                                        uint Length,
                                        uint BaseOffset,
                                        ref int SignedSizeOfTagPtr)
    {
        var SignedSizeOfTag = SignedSizeOfTagPtr;

        // Read column arrays
        for (var i = 0u; i < Count; i++)
        {
            if (SignedSizeOfTag < 4 * sizeof(uint))
                return false;
            SignedSizeOfTag -= 4 * sizeof(uint);

            if (!ReadOneElem(io, ref a.Name, i, BaseOffset))
                return false;
            if (!ReadOneElem(io, ref a.Value, i, BaseOffset))
                return false;

            if (Length > 16)
            {
                if (SignedSizeOfTag < 2 * sizeof(uint))
                    return false;
                SignedSizeOfTag -= 2 * sizeof(uint);

                if (!ReadOneElem(io, ref a.DisplayName, i, BaseOffset))
                    return false;
            }

            if (Length > 24)
            {
                if (SignedSizeOfTag < 2 * sizeof(uint))
                    return false;
                SignedSizeOfTag -= 2 * sizeof(uint);

                if (!ReadOneElem(io, ref a.DisplayValue, i, BaseOffset))
                    return false;
            }
        }

        SignedSizeOfTagPtr = SignedSizeOfTag;
        return true;
    }

    private static bool WriteOneElem(IOHandler io, ref DICelem e, uint i)
    {
        if (!io.Write(e.Offsets[i]))
            return false;
        if (!io.Write(e.Sizes[i]))
            return false;

        return true;
    }

    private static bool WriteOffsetArray(IOHandler io, ref DICarray a, uint Count, uint Length)
    {
        for (var i = 0u; i < Count; i++)
        {
            if (!WriteOneElem(io, ref a.Name, i))
                return false;
            if (!WriteOneElem(io, ref a.Value, i))
                return false;

            if (Length > 16)
                if (!WriteOneElem(io, ref a.DisplayName, i))
                    return false;

            if (Length > 24)
                if (!WriteOneElem(io, ref a.DisplayValue, i))
                    return false;
        }

        return true;
    }

    private static bool ReadOneWChar(IOHandler io, ref DICelem e, uint i, out string? wcstr)
    {
        wcstr = null;

        // Special case for undefined strings (see ICC Votable
        // Proposal Submission, Dictionary Type and Metadata TAG Definition)
        if (e.Offsets[i] is 0)
            return true;

        if (!io.SeekFunc(io, e.Offsets[i]))
            return false;

        var nChars = e.Sizes[i] / sizeof(ushort);

        //wcstr = _cmsMallocZero<char>(e.ContextID, nChars + 1);
        //if (*wcstr is null) return false;

        if (!_cmsReadWCharArray(io, nChars, out wcstr))
        {
            //_cmsFree(e->ContextID, *wcstr);
            return false;
        }

        // End of string marker
        //(*wcstr)[nChars] = '\0';
        return true;
    }

    private static bool WriteOneWChar(IOHandler io, ref DICelem e, uint i, in string? wcstr, uint BaseOffset)
    {
        var Before = io.TellFunc(io);

        e.Offsets[i] = Before - BaseOffset;

        if (wcstr is null)
        {
            e.Sizes[i] = 0;
            e.Offsets[i] = 0;
            return true;
        }

        //var n = mywcslen(wcstr);
        var n = (uint)wcstr.Length;
        if (!_cmsWriteWCharArray(io, n, wcstr))
            return false;

        e.Sizes[i] = io.TellFunc(io) - Before;
        return true;
    }

    private static bool ReadOneMLUC(TagTypeHandler self, IOHandler io, ref DICelem e, uint i, out Mlu? mlu)
    {
        // A way to get nul MLUCs
        if (e.Offsets[i] is 0 || e.Sizes[i] is 0)
        {
            mlu = null;
            return true;
        }

        if (!io.SeekFunc(io, e.Offsets[i]))
        {
            mlu = null;
            return false;
        }

        mlu = Type_MLU_Read(self, io, out _, e.Sizes[i]);
        return mlu is not null;
    }

    private static bool WriteOneMLUC(TagTypeHandler self,
                                     IOHandler io,
                                     ref DICelem e,
                                     uint i,
                                     Mlu? mlu,
                                     uint BaseOffset)
    {
        // Special case for undefined strings (see ICC Votable
        // Proposal Submission, Dictionary Type and Metadata TAG Definition)
        if (mlu is null)
        {
            e.Sizes[i] = 0;
            e.Offsets[i] = 0;
            return true;
        }

        var Before = io.TellFunc(io);
        if (e.Offsets is not null)
            e.Offsets[i] = Before - BaseOffset;

        if (!Type_MLU_Write(self, io, mlu, 1))
            return false;

        if (e.Sizes is not null)
            e.Sizes[i] = io.TellFunc(io) - Before;
        return true;
    }

    private static Dictionary? Type_Dictionary_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint SizeOfTag)
    {
        Dictionary? hDict = null;
        string? NameWCS = null, ValueWCS = null;
        Mlu? DisplayNameMLU = null, DisplayValueMLU = null;
        DICarray a = default;
        var SignedSizeOfTag = (int)SizeOfTag;

        nItems = 0;
        //memset(&a, 0, _sizeof<DICarray>());

        // Get actual position as a basis for element offsets
        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        // Get name-value record count
        SignedSizeOfTag -= sizeof(uint);
        if (SignedSizeOfTag < 0)
            goto Error;
        if (!io.ReadUint(out var Count))
            goto Error;

        // Get rec length
        SignedSizeOfTag -= sizeof(uint);
        if (SignedSizeOfTag < 0)
            goto Error;
        if (!io.ReadUint(out var Length))
            goto Error;

        // Check for valid lengths
        if (Length is not 16 and not 24 and not 32)
        {
            Context.LogError(
                self.ContextID,
                cmsERROR_UNKNOWN_EXTENSION,
                $"Unknown record length in dictionry '{Length}'");
            return null;
        }

        // Creates an empty dictionary
        hDict = cmsDictAlloc(self.ContextID);
        if (hDict is null)
            goto Error;

        // Depending on record size, create column arrays
        if (!AllocArray(self.ContextID, out a, Count, Length))
            goto Error;

        // Read column arrays
        if (!ReadOffsetArray(io, ref a, Count, Length, BaseOffset, ref SignedSizeOfTag))
            goto Error;

        // Seek to each element and read it
        for (var i = 0u; i < Count; i++)
        {
            if (!ReadOneWChar(io, ref a.Name, i, out NameWCS))
                goto Error;
            if (!ReadOneWChar(io, ref a.Value, i, out ValueWCS))
                goto Error;

            if (Length > 16)
                if (!ReadOneMLUC(self, io, ref a.DisplayName, i, out DisplayNameMLU))
                    goto Error;

            if (Length > 24)
                if (!ReadOneMLUC(self, io, ref a.DisplayValue, i, out DisplayValueMLU))
                    goto Error;

            bool rc;
            if (NameWCS is null || ValueWCS is null)
            {
                Context.LogError(self.ContextID, cmsERROR_CORRUPTION_DETECTED, "Bad dictionary Name/Value");
                rc = false;
            }
            else
            {
                rc = cmsDictAddEntry(hDict, NameWCS, ValueWCS, DisplayNameMLU, DisplayValueMLU);
            }

            //if (NameWCS is not null) _cmsFree(self.ContextID, NameWCS);
            //if (ValueWCS is not null) _cmsFree(self.ContextID, ValueWCS);
            if (DisplayNameMLU is not null)
                cmsMLUfree(DisplayNameMLU);
            if (DisplayValueMLU is not null)
                cmsMLUfree(DisplayValueMLU);

            if (!rc)
                goto Error;
        }

        FreeArray(ref a);
        nItems = 1;
        return hDict;

    Error:
        FreeArray(ref a);
        if (hDict is not null)
            cmsDictFree(hDict);
        return null;
    }

    private static bool Type_Dictionary_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _1)
    {
        if (Ptr is not Dictionary hDict)
            return false;

        Dictionary.Entry? p;

        if (hDict is null)
            return false;

        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        // Let's inspect the dictionary
        var Count = 0u;
        var AnyName = false;
        var AnyValue = false;
        for (p = cmsDictGetEntryList(hDict); p is not null; p = cmsDictNextEntry(p))
        {
            if (p.DisplayName is not null)
                AnyName = true;
            if (p.DisplayValue is not null)
                AnyValue = true;
            Count++;
        }

        var Length = 16u;
        if (AnyName)
            Length += 8;
        if (AnyValue)
            Length += 8;

        if (!io.Write(Count))
            return false;
        if (!io.Write(Length))
            return false;

        // Keep starting posiotion of offsets table
        var DirectoryPos = io.TellFunc(io);

        // Allocate offsets array
        if (!AllocArray(self.ContextID, out var a, Count, Length))
            goto Error;

        // Write a fake directory to be filled later on
        if (!WriteOffsetArray(io, ref a, Count, Length))
            goto Error;

        // Write each element. Keep track of the size as well.
        p = cmsDictGetEntryList(hDict);
        for (var i = 0u; i < Count; i++)
        {
            if (!WriteOneWChar(io, ref a.Name, i, p.Name, BaseOffset))
                goto Error;
            if (!WriteOneWChar(io, ref a.Value, i, p.Value, BaseOffset))
                goto Error;

            if (p.DisplayName is not null)
                if (!WriteOneMLUC(self, io, ref a.DisplayName, i, p.DisplayName, BaseOffset))
                    goto Error;

            if (p.DisplayValue is not null)
                if (!WriteOneMLUC(self, io, ref a.DisplayValue, i, p.DisplayValue, BaseOffset))
                    goto Error;

            p = cmsDictNextEntry(p);
        }

        // Write the directory
        var CurrentPos = io.TellFunc(io);
        if (!io.SeekFunc(io, DirectoryPos))
            goto Error;

        if (!WriteOffsetArray(io, ref a, Count, Length))
            goto Error;

        if (!io.SeekFunc(io, CurrentPos))
            goto Error;

        FreeArray(ref a);
        return true;

    Error:
        FreeArray(ref a);
        return false;
    }

    private static Dictionary? Type_Dictionary_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        cmsDictDup(Ptr as Dictionary);

    private static void Type_Dictionary_Free(TagTypeHandler _1, object? Ptr) =>
        cmsDictFree(Ptr as Dictionary);

    #endregion Dictionary

    #region cicp VideoSignalType

    private static Box<VideoSignalType>? Type_VideoSignal_Read(TagTypeHandler _1,
                                                               IOHandler io,
                                                               out uint nItems,
                                                               uint SizeOfTag)
    {
        nItems = 0;

        if (SizeOfTag is not 8)
            return null;

        if (!io.ReadUint(out _))
            return null;

        var cicp = new VideoSignalType();

        if (!io.ReadByte(out cicp.ColourPrimaries))
            return null;
        if (!io.ReadByte(out cicp.TransferCharacteristics))
            return null;
        if (!io.ReadByte(out cicp.MatrixCoefficients))
            return null;
        if (!io.ReadByte(out cicp.VideoFullRangeFlag))
            return null;

        nItems = 1;
        return new(cicp);
    }

    private static bool Type_VideoSignal_Write(TagTypeHandler _1, IOHandler io, object Ptr, uint _2)
    {
        if (Ptr is not Box<VideoSignalType> cicp)
            return false;

        if (!io.Write((uint)0))
            return false;
        if (!io.Write(cicp.Value.ColourPrimaries))
            return false;
        if (!io.Write(cicp.Value.TransferCharacteristics))
            return false;
        if (!io.Write(cicp.Value.MatrixCoefficients))
            return false;
        if (!io.Write(cicp.Value.VideoFullRangeFlag))
            return false;

        return true;
    }

    private static Box<VideoSignalType>? Type_VideoSignal_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        Ptr is Box<VideoSignalType> sc
            ? new(sc)
            : null;

    #endregion cicp VideoSignalType

    #region Microsoft's MHC2

    private static void SetIdentity(Span<double> XYZ2XYZmatrix)
    {
        XYZ2XYZmatrix[0] = 1.0;
        XYZ2XYZmatrix[1] = 0.0;
        XYZ2XYZmatrix[2] = 0.0;
        XYZ2XYZmatrix[3] = 0.0;
        XYZ2XYZmatrix[4] = 0.0;
        XYZ2XYZmatrix[5] = 1.0;
        XYZ2XYZmatrix[6] = 0.0;
        XYZ2XYZmatrix[7] = 0.0;
        XYZ2XYZmatrix[8] = 0.0;
        XYZ2XYZmatrix[9] = 0.0;
        XYZ2XYZmatrix[10] = 1.0;
        XYZ2XYZmatrix[11] = 0.0;
    }

    private static bool CloseEnough(double a, double b) =>
        Math.Abs(b - a) < (1.0 / 65535.0);

    private static bool IsIdentity(ReadOnlySpan<double> XYZ2XYZmatrix)
    {
        Span<double> Identity = stackalloc double[12];

        SetIdentity(Identity);

        for (var i = 0; i < 12; i++)
        {
            if (!CloseEnough(XYZ2XYZmatrix[i], Identity[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static void Type_MHC2_Free(TagTypeHandler self, object? Ptr)
    {
        if (Ptr is not Box<MHC2> mhc2)
            return;

        //var pool = self.ContextID!.GetBufferPool<double>();

        if (mhc2.Value.redCurve is not null)
        {
            //ReturnArray(pool, mhc2.Value.redCurve);
            mhc2.Value.redCurve = null!;
        }

        if (mhc2.Value.greenCurve is not null)
        {
            //ReturnArray(pool, mhc2.Value.greenCurve);
            mhc2.Value.greenCurve = null!;
        }

        if (mhc2.Value.blueCurve is not null)
        {
            //ReturnArray(pool, mhc2.Value.blueCurve);
            mhc2.Value.blueCurve = null!;
        }
    }

    private static Box<MHC2>? Type_MHC2_Dup(TagTypeHandler self, object? Ptr, uint _1)
    {
        if (Ptr is not Box<MHC2> org)
            return null;

        var mhc2 = org.Value;

        mhc2.redCurve = _cmsDupMem<double>(self.ContextID, mhc2.redCurve, mhc2.entries);
        mhc2.greenCurve = _cmsDupMem<double>(self.ContextID, mhc2.greenCurve, mhc2.entries);
        mhc2.blueCurve = _cmsDupMem<double>(self.ContextID, mhc2.blueCurve, mhc2.entries);

        return new(mhc2);
    }

    private static bool WriteDoubles(IOHandler io, uint n, ReadOnlySpan<double> Values)
    {
        for (var i = 0; i < n; i++)
        {
            if (!io.Write(Values[i]))
                return false;
        }

        return true;
    }

    private static bool Type_MHC2_Write(TagTypeHandler _1, IOHandler io, object Ptr, uint _2)
    {
        uint MatrixOffset;

        if (Ptr is not Box<MHC2> mhc2)
            return false;

        var BaseOffset = io.TellFunc(io) - (uint)Unsafe.SizeOf<TagBase>();

        if (!io.Write((uint)0))
            return false;
        if (!io.Write(mhc2.Value.entries))
            return false;

        if (!io.Write(mhc2.Value.minLuminance))
            return false;
        if (!io.Write(mhc2.Value.peakLuminance))
            return false;

        var TablesOffsetPos = io.TellFunc(io);

        if (!io.Write((uint)0))
            return false;    // Matrix
        if (!io.Write((uint)0))
            return false;    // Curve R
        if (!io.Write((uint)0))
            return false;    // Curve G
        if (!io.Write((uint)0))
            return false;    // Curve B

        if (IsIdentity(mhc2.Value.matrix))
        {
            MatrixOffset = 0;
        }
        else
        {
            MatrixOffset = io.TellFunc(io) - BaseOffset;
            if (!WriteDoubles(io, 3 * 4, mhc2.Value.matrix))
                return false;
        }

        var OffsetRedTable = io.TellFunc(io) - BaseOffset;
        if (!WriteDoubles(io, mhc2.Value.entries, mhc2.Value.redCurve))
            return false;
        var OffsetGreenTable = io.TellFunc(io) - BaseOffset;
        if (!WriteDoubles(io, mhc2.Value.entries, mhc2.Value.greenCurve))
            return false;
        var OffsetBlueTable = io.TellFunc(io) - BaseOffset;
        if (!WriteDoubles(io, mhc2.Value.entries, mhc2.Value.blueCurve))
            return false;

        if (!io.SeekFunc(io, TablesOffsetPos))
            return false;

        if (!io.Write(MatrixOffset))
            return false;
        if (!io.Write(OffsetRedTable))
            return false;
        if (!io.Write(OffsetGreenTable))
            return false;
        if (!io.Write(OffsetBlueTable))
            return false;

        return true;
    }

    private static bool ReadDoublesAt(IOHandler io, uint At, uint n, Span<double> Values)
    {
        var CurrentPos = io.TellFunc(io);

        if (!io.SeekFunc(io, At))
            return false;

        for (var i = 0; i < n; i++)
        {
            if (!io.ReadFixed15_16(out Values[i]))
                return false;
        }

        if (!io.SeekFunc(io, CurrentPos))
            return false;

        return true;
    }

    private static Box<MHC2>? Type_MHC2_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        MHC2 mhc2 = new();

        nItems = 0;

        var BaseOffset = io.TellFunc(io) - (uint)Unsafe.SizeOf<TagBase>();

        if (!io.ReadUint(out _))
            return null;

        if (!io.ReadUint(out mhc2.entries))
            goto Error;

        if (mhc2.entries > 4096)
            goto Error;

        //var pool = self.ContextID!.GetBufferPool<double>();
        //mhc2.redCurve = pool.Rent((int)mhc2.entries);
        //mhc2.greenCurve = pool.Rent((int)mhc2.entries);
        //mhc2.blueCurve = pool.Rent((int)mhc2.entries);
        mhc2.redCurve = new double[mhc2.entries];
        mhc2.greenCurve = new double[mhc2.entries];
        mhc2.blueCurve = new double[mhc2.entries];

        mhc2.matrix = new double[3 * 4];

        if (!io.ReadFixed15_16(out mhc2.minLuminance))
            goto Error;
        if (!io.ReadFixed15_16(out mhc2.peakLuminance))
            goto Error;

        if (!io.ReadUint(out var MatrixOffset))
            goto Error;
        if (!io.ReadUint(out var OffsetRedTable))
            goto Error;
        if (!io.ReadUint(out var OffsetGreenTable))
            goto Error;
        if (!io.ReadUint(out var OffsetBlueTable))
            goto Error;

        if (MatrixOffset is 0)
            SetIdentity(mhc2.matrix);
        else
        {
            if (!ReadDoublesAt(io, BaseOffset + MatrixOffset, 3 * 4, mhc2.matrix))
                goto Error;
        }

        if (!ReadDoublesAt(io, BaseOffset + OffsetRedTable, mhc2.entries, mhc2.redCurve))
            goto Error;
        if (!ReadDoublesAt(io, BaseOffset + OffsetGreenTable, mhc2.entries, mhc2.greenCurve))
            goto Error;
        if (!ReadDoublesAt(io, BaseOffset + OffsetBlueTable, mhc2.entries, mhc2.blueCurve))
            goto Error;

        // Success
        nItems = 1;
        return new(mhc2);

    Error:
        Type_MHC2_Free(self, new Box<MHC2>(mhc2));
        return null;
    }

    #endregion Microsoft's MHC2

    #region Generic

    #region ToneCurve types

    private static ToneCurve? ToneCurve_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        (Ptr as ToneCurve)?.Clone();

    private static void ToneCurve_Free(TagTypeHandler _1, object? Ptr) =>
        (Ptr as ToneCurve)?.Dispose();

    #endregion ToneCurve types

    #region NamedColorList types

    private static NamedColorList? NamedColor_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        cmsDupNamedColorList(Ptr as NamedColorList);

    private static void NamedColor_Free(TagTypeHandler _1, object? Ptr) =>
        cmsFreeNamedColorList(Ptr as NamedColorList);

    #endregion NamedColorList types

    #region Sequence types

    private static Sequence? Sequence_Dup(TagTypeHandler _1, object? Ptr, uint _2) =>
        cmsDupProfileSequenceDescription(Ptr as Sequence);

    private static void Sequence_Free(TagTypeHandler _1, object? Ptr) =>
        cmsFreeProfileSequenceDescription(Ptr as Sequence);

    #endregion Sequence types

    #endregion Generic

    #endregion TypeHandlers

    #region MPE Handlers

    #region Generic

    private static Stage? GenericMPEdup(TagTypeHandler _1, object? Ptr, uint _2) =>
        (Ptr as Stage)?.Clone();

    private static void GenericMPEfree(TagTypeHandler _1, object? Ptr) =>
        (Ptr as Stage)?.Dispose();

    #endregion Generic

    #region MPEcurve

    private static ToneCurve? ReadSegmentedCurve(TagTypeHandler self, IOHandler io)
    {
        Span<uint> ParamsByType = stackalloc uint[] { 4, 5, 5 };
        float PrevBreak = Context.NegativeInfinity;

        // Take signature and channels for each element.
        if (!_cmsReadSignature(io, out var ElementSig))
            return null;

        // That should be a segmented curve
        if (ElementSig != Signatures.CurveSegment.Segmented)
            return null;

        if (!io.ReadUint(out _))
            return null;
        if (!io.ReadUshort(out var nSegments))
            return null;
        if (!io.ReadUshort(out _))
            return null;

        if (nSegments < 1)
            return null;
        //var Segments = GetArray<CurveSegment>(self.ContextID, nSegments);
        var Segments = new CurveSegment[nSegments];
        if (Segments is null)
            return null;

        // Read breakpoints
        for (var i = 0; i < nSegments - 1; i++)
        {
            Segments[i].x0 = PrevBreak;
            if (!io.ReadFloat(out Segments[i].x1))
                goto Error;
            PrevBreak = Segments[i].x1;
        }

        Segments[nSegments - 1].x0 = PrevBreak;
        Segments[nSegments - 1].x1 = Context.PositiveInfinity;

        // Read segments
        for (var i = 0; i < nSegments; i++)
        {
            if (!_cmsReadSignature(io, out ElementSig))
                goto Error;
            if (!io.ReadUint(out _))
                goto Error;

            if (ElementSig == Signatures.CurveSegment.Formula)
            {
                if (!io.ReadUshort(out var Type))
                    goto Error;
                if (!io.ReadUshort(out _))
                    goto Error;

                Segments[i].Type = Type + 6;
                if (Type > 2)
                    goto Error;

                //Segments[i].Params = Context.GetPool<double>(self.ContextID).Rent(10);
                Segments[i].Params = new double[10];

                for (var j = 0; j < ParamsByType[Type]; j++)
                {
                    if (!io.ReadFloat(out var f))
                        goto Error;
                    Segments[i].Params[j] = f;
                }
            }
            else if (ElementSig == Signatures.CurveSegment.Sampled)
            {
                if (!io.ReadUint(out var Count))
                    goto Error;

                // This first point is implicit in the last stage, we allocate an extra note to be populated later on
                Count++;
                Segments[i].nGridPoints = Count;
                //Segments[i].SampledPoints = _cmsCalloc<float>(self.ContextID, Count);
                //if (Segments[i].SampledPoints is null) goto Error;
                //Segments[i].SampledPoints = Context.GetPool<float>(self.ContextID).Rent((int)Count);
                Segments[i].SampledPoints = new float[Count];

                Segments[i].SampledPoints[0] = 0;
                for (var j = 1; j < Count; j++)
                    if (!io.ReadFloat(out Segments[i].SampledPoints[j]))
                        goto Error;
            }
            else
            {
                Context.LogError(
                    self.ContextID,
                    cmsERROR_UNKNOWN_EXTENSION,
                    $"Unknown curve element type '{(ElementSig)}' found.");
                goto Error;
            }
        }

        var Curve = ToneCurve.BuildSegmented(self.ContextID, nSegments, Segments);

        //for (var i = 0; i < nSegments; i++)
        //    if (Segments[i].SampledPoints is not null) ReturnArray(self.ContextID, Segments[i].SampledPoints);
        //ReturnArray(self.ContextID, Segments);

        // Explore for missing implicit points
        for (var i = 0; i < nSegments; i++)
        {
            // If sampled curve, fix it
            if (Curve.Segments[i].Type is 0)
                Curve.Segments[i].SampledPoints[0] = Curve.Evaluate(Curve.Segments[i].x0);
        }

        return Curve;

    Error:
        return null;
    }

    private static bool ReadMPECurve(TagTypeHandler self, IOHandler io, object? Cargo, uint n, uint _)
    {
        if (Cargo is not ToneCurve[] GammaTables)
            return false;

        GammaTables[n] = ReadSegmentedCurve(self, io)!;
        return GammaTables[n] is not null;
    }

    private static Stage? Type_MPEcurve_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        nItems = 0;

        // Get actual position as a basis for element offsets
        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        if (!io.ReadUshort(out var InputChans))
            return null;
        if (!io.ReadUshort(out var OutputChans))
            return null;

        if (InputChans != OutputChans)
            return null;

        //var GammaTables = (ToneCurve**)_cmsCalloc(self.ContextID, InputChans, _sizeof<nint>());
        //if (GammaTables is null) return null;
        //var pool = Context.GetPool<ToneCurve>(self.ContextID);
        //var GammaTables = pool.Rent(InputChans);
        var GammaTables = new ToneCurve[InputChans];

        var mpe = ReadPositionTable(self, io, InputChans, BaseOffset, GammaTables, ReadMPECurve)
                      ? new ToneCurvesStage(self.ContextID, GammaTables[..(int)InputChans])
                      : null;

        nItems = mpe is not null ? 1u : 0;
        return mpe;
    }

    private static bool WriteSegmentedCurve(IOHandler io, ToneCurve g)
    {
        Span<uint> ParamsByType = stackalloc uint[] { 4, 5, 5 };

        var Segments = g.Segments;
        var nSegments = g.nSegments;

        if (!io.Write((uint)Signatures.CurveSegment.Segmented))
            return false;
        if (!io.Write((uint)0))
            return false;
        if (!io.Write((ushort)nSegments))
            return false;
        if (!io.Write((ushort)0))
            return false;

        // Write the break points
        for (var i = 0; i < nSegments - 1; i++)
            if (!io.Write(Segments[i].x1))
                return false;

        // Write the segments
        for (var i = 0; i < nSegments; i++)
        {
            var ActualSeg = Segments[i];

            if (ActualSeg.Type is 0)
            {
                // This is a sampled curve. First point is implicit in the ICC format, but not in our representation
                if (!io.Write((uint)Signatures.CurveSegment.Sampled))
                    return false;
                if (!io.Write((uint)0))
                    return false;
                if (!io.Write(ActualSeg.nGridPoints - 1))
                    return false;

                for (var j = 1; j < g.Segments[i].nGridPoints; j++)
                    if (!io.Write(ActualSeg.SampledPoints[j]))
                        return false;
            }
            else
            {
                // This is a formula-based
                if (!io.Write((uint)Signatures.CurveSegment.Formula))
                    return false;
                if (!io.Write((uint)0))
                    return false;

                // We only allow 1, 2, and 3 as types
                var Type = ActualSeg.Type - 6;
                if (Type is > 2 or < 0)
                    return false;

                if (!io.Write((ushort)Type))
                    return false;
                if (!io.Write((ushort)0))
                    return false;

                for (var j = 0; j < ParamsByType[Type]; j++)
                    if (!io.Write((float)ActualSeg.Params[j]))
                        return false;
            }

            // It seems there is no need to align. Code is here, and for safety commented out
            // if (!io.WriteAlignment()) goto Error;
        }

        return true;
    }

    private static bool WriteMPECurve(TagTypeHandler _1, IOHandler io, object? Cargo, uint n, uint _2) =>
        Cargo is ToneCurvesStage Curves && WriteSegmentedCurve(io, Curves.Curves[n]);

    private static bool Type_MPEcurve_Write(TagTypeHandler self, IOHandler io, object? Ptr, uint _)
    {
        if (Ptr is not Stage mpe ||
            mpe is not ToneCurvesStage Curves)
        {
            return false;
        }

        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        // Write the header. Since those are curves, input and output channels are the same
        if (!io.Write((ushort)mpe.InputChannels))
            return false;
        if (!io.Write((ushort)mpe.InputChannels))
            return false;

        if (!WritePositionTable(self, io, 0, mpe.InputChannels, BaseOffset, Curves, WriteMPECurve))
            return false;

        return true;
    }

    #endregion MPEcurve

    #region MPEmatrix

    private static Stage? Type_MPEmatrix_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        nItems = 0;

        if (!io.ReadUshort(out var InputChans))
            return null;
        if (!io.ReadUshort(out var OutputChans))
            return null;

        if (InputChans >= Context.MaxChannels)
            return null;
        if (OutputChans >= Context.MaxChannels)
            return null;

        var nElems = (uint)InputChans * OutputChans;

        Span<double> Matrix = stackalloc double[(int)nElems];
        Span<double> Offsets = stackalloc double[OutputChans];

        for (var i = 0; i < nElems; i++)
        {
            if (!io.ReadFloat(out var v))
            {
                return null;
            }

            Matrix[i] = v;
        }

        for (var i = 0; i < OutputChans; i++)
        {
            if (!io.ReadFloat(out var v))
            {
                return null;
            }

            Offsets[i] = v;
        }

        var mpe = new MatrixStage(self.ContextID, OutputChans, InputChans, Matrix, Offsets);
        nItems = 1u;
        return mpe;
    }

    private static bool Type_MPEmatrix_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        if (Ptr is not Stage mpe ||
            mpe is not MatrixStage Matrix)
        {
            return false;
        }

        if (!io.Write((ushort)mpe.InputChannels))
            return false;
        if (!io.Write((ushort)mpe.OutputChannels))
            return false;

        var nElems = mpe.InputChannels * mpe.OutputChannels;

        for (var i = 0; i < nElems; i++)
            if (!io.Write((float)Matrix.Values[i]))
                return false;

        for (var i = 0; i < mpe.OutputChannels; i++)
        {
            if (!io.Write(Matrix.Offset is null ? 0 : (float)Matrix.Offset[i]))
                return false;
        }

        return true;
    }

    #endregion MPEmatrix

    #region MPEclut

    private static Stage? Type_MPEclut_Read(TagTypeHandler self, IOHandler io, out uint nItems, uint _1)
    {
        Span<byte> Dimensions8 = stackalloc byte[16];
        Span<uint> GridPoints = stackalloc uint[Context.MaxInputDimensions];
        Stage? mpe = null;

        nItems = 0;

        // Get actual position as a basis for element offsets
        var BaseOffset = io.TellFunc(io) - (sizeof(uint) * 2);

        if (!io.ReadUshort(out var InputChans))
            return null;
        if (!io.ReadUshort(out var OutputChans))
            return null;

        if (InputChans is 0 || InputChans >= Context.MaxChannels)
            goto Error;
        if (OutputChans is 0 || OutputChans >= Context.MaxChannels)
            goto Error;

        if (io.ReadFunc(io, Dimensions8, sizeof(byte), 16) is not 16)
            goto Error;

        // Copy MAX_INPUT_DIMENSIONS at most. Expact to uint
        var nMaxGrids = Math.Min(InputChans, Context.MaxInputDimensions);

        for (var i = 0; i < nMaxGrids; i++)
        {
            if (Dimensions8[i] is 1)
                goto Error;    // Impossible value, 0 for no CLUT and at least 2 otherwise
            GridPoints[i] = Dimensions8[i];
        }

        // Allocate the true CLUT
        mpe = new CLutStage<float>(self.ContextID, GridPoints, InputChans, OutputChans, null);

        // Read and sanitize the data
        if (mpe is not CLutStage<float> clut)
            goto Error;

        for (var i = 0; i < clut.Tab.Length; i++)
            if (!io.ReadFloat(out clut.TFloat[i]))
                goto Error;

        nItems = 1;
        return mpe;

    Error:
        return null;
    }

    private static bool Type_MPEclut_Write(TagTypeHandler _1, IOHandler io, object? Ptr, uint _2)
    {
        Span<byte> Dimensions8 = stackalloc byte[16];

        if (Ptr is not Stage mpe || mpe is not CLutStage<float> clut)
            return false;

        // Check for maximum number of channels supported by lcms
        if (mpe.InputChannels > Context.MaxInputDimensions)
            return false;

        //// Only floats are supported in MPE
        //if (!clut.HasFloatValues) return false;
        //
        // We already check for HasFloatValues by making sure we are working with
        // a StageCLutData<float> from the beginning.

        if (!io.Write((ushort)mpe.InputChannels))
            return false;
        if (!io.Write((ushort)mpe.OutputChannels))
            return false;

        //memset(Dimensions8, 0, sizeof(byte) * 16);

        for (var i = 0; i < mpe.InputChannels; i++)
            Dimensions8[i] = (byte)clut.Params.nSamples[i];

        if (!io.WriteFunc(io, 16, Dimensions8))
            return false;

        for (var i = 0; i < clut.Tab.Length; i++)
            if (!io.Write(clut.TFloat[i]))
                return false;

        return true;
    }

    #endregion MPEclut

    #endregion MPE Handlers

    #region Plugin

    internal static void DupTagTypeList(ref TagTypePluginChunkType dest, in TagTypePluginChunkType src) =>
        dest = (TagTypePluginChunkType)((ICloneable)src).Clone();

    internal static void _cmsAllocTagTypePluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.TagTypePlugin
                       : TagTypePluginChunk;

        DupTagTypeList(ref ctx.TagTypePlugin, from);
    }

    internal static void _cmsAllocMPETypePluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.MPEPlugin
                       : MPETypePluginChunk;

        DupTagTypeList(ref ctx.MPEPlugin, from);
    }

    internal static void DupTagList(ref TagPluginChunkType dest, in TagPluginChunkType? src) =>
        dest = (TagPluginChunkType)((ICloneable)src).Clone();

    internal static void _cmsAllocTagPluginChunk(Context ctx, in Context? src)
    {
        _cmsAssert(ctx);

        var from = src is not null
                       ? src.TagPlugin
                       : TagPluginChunk;

        DupTagList(ref ctx.TagPlugin, from);
    }

    #endregion Plugin

    internal static TagTypeHandler? _cmsGetTagTypeHandler(Context? ContextID, Signature sig)
    {
        var ctx = Context.Get(ContextID).TagTypePlugin;

        return GetHandler(sig, ctx.List, supportedTagTypes);
    }

    internal static TagDescriptor? _cmsGetTagDescriptor(Context? ContextID, Signature sig)
    {
        var TagPluginChunk = Context.Get(ContextID).TagPlugin;

        foreach (var pt in TagPluginChunk.List)
            if (sig == pt.Signature)
                return pt.Descriptor;

        foreach (var pt in supportedTags)
            if (sig == pt.Signature)
                return pt.Descriptor;

        return null;
    }
}
