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
using lcms2.types;

using static lcms2.types.Signature;

namespace lcms2.plugins;

public delegate Signature TagTypeDecider(double iccVersion, ref object data);

internal static class TagHandlers
{
    #region Fields

    public static readonly TagDescriptor AToB = new(1, new Signature[] { TagType.Lut16, TagType.LutAtoB, TagType.Lut8 }, DecideLutA2B);

    public static readonly TagDescriptor BToA = new(1, new Signature[] { TagType.Lut16, TagType.LutBtoA, TagType.Lut8 }, DecideLutB2A);

    public static readonly TagDescriptor ColorantTable = new(1, new Signature[] { TagType.ColorantTable }, null);

    public static readonly TagDescriptor Curve = new(1, new Signature[] { TagType.Curve, TagType.ParametricCurve }, DecideCurve);

    public static readonly TagDescriptor CurveEx = new(1, new Signature[] { TagType.Curve, TagType.ParametricCurve, TagType.MonacoBrokenCurve }, DecideCurve);

    public static readonly TagDescriptor DateTime = new(1, new Signature[] { TagType.DateTime }, null);

    public static readonly TagDescriptor MultiProcessElement = new(1, new Signature[] { TagType.MultiProcessElement }, null);

    public static readonly TagDescriptor S15Fixed16Array = new(9, new Signature[] { TagType.S15Fixed16Array }, null);

    public static readonly TagDescriptor Signature = new(1, new Signature[] { TagType.Signature }, null);

    public static readonly TagDescriptor Text = new(1, new Signature[] { TagType.TextDescription, TagType.MultiLocalizedUnicode, TagType.Text }, DecideText);

    public static readonly TagDescriptor TextDescription = new(1, new Signature[] { TagType.TextDescription, TagType.MultiLocalizedUnicode, TagType.Text }, DecideTextDescription);

    public static readonly TagDescriptor Xyz = new(1, new Signature[] { TagType.XYZ }, DecideXYZ);

    public static readonly TagDescriptor XyzEx = new(1, new Signature[] { TagType.XYZ, TagType.CorbisBrokenXYZ }, DecideXYZ);

    #endregion Fields

    #region Public Methods

    public static Signature DecideCurve(double iccVersion, ref object data)
    {
        var curve = (ToneCurve)data;

        return (iccVersion < 4.0 || curve.NumSegments != 1 || curve.segments[0].Type is < 0 or > 5)
            ? TagType.Curve
            : TagType.ParametricCurve;
    }

    public static Signature DecideLutA2B(double iccVersion, ref object data) =>
        iccVersion switch
        {
            < 4.0 =>
                ((Pipeline)data).SaveAs8Bits
                    ? TagType.Lut8
                    : TagType.Lut16,
            _ =>
                TagType.LutAtoB
        };

    public static Signature DecideLutB2A(double iccVersion, ref object data) =>
        iccVersion switch
        {
            < 4.0 =>
                ((Pipeline)data).SaveAs8Bits
                    ? TagType.Lut8
                    : TagType.Lut16,
            _ =>
                TagType.LutBtoA
        };

    public static Signature DecideText(double iccVersion, ref object data) =>
        iccVersion >= 4.0
            ? TagType.MultiLocalizedUnicode
            : TagType.Text;

    public static Signature DecideTextDescription(double iccVersion, ref object data) =>
        iccVersion >= 4.0
            ? TagType.MultiLocalizedUnicode
            : TagType.TextDescription;

    public static Signature DecideXYZ(double iccVersion, ref object data) =>
        TagType.XYZ;

    #endregion Public Methods
}
