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
using System.Collections;

namespace lcms2.types;

internal class ParametricCurvesCollection : IList<ParametricCurve>, ICloneable
{
    private readonly List<ParametricCurve> _list;

    public ParametricCurvesCollection() =>
        _list = new();

    public ParametricCurvesCollection(int capacity) =>
        _list = new(capacity);

    public ParametricCurvesCollection(IEnumerable<ParametricCurve> list) =>
        _list = new(list);

    public ParametricCurve this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    public int Count =>
        _list.Count;

    public bool IsReadOnly =>
        ((ICollection<ParametricCurve>)_list).IsReadOnly;

    public void Add(ParametricCurve item) =>
        _list.Add(item);

    public void Clear() =>
        _list.Clear();

    public object Clone() =>
        new ParametricCurvesCollection(_list.Select(c => (ParametricCurve)c.Clone()));

    public bool Contains(ParametricCurve item) =>
        _list.Contains(item);

    public void CopyTo(ParametricCurve[] array, int arrayIndex) =>
        _list.CopyTo(array, arrayIndex);

    public IEnumerator<ParametricCurve> GetEnumerator() =>
        _list.GetEnumerator();

    public int IndexOf(ParametricCurve item) =>
        _list.IndexOf(item);

    public void Insert(int index, ParametricCurve item) =>
        _list.Insert(index, item);

    public bool Remove(ParametricCurve item) =>
        _list.Remove(item);

    public void RemoveAt(int index) =>
        _list.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)_list).GetEnumerator();
}
internal class ParametricCurve : ICloneable
{
    public readonly (int type, uint paramCount)[] Functions;
    public ParametricCurveEvaluator Evaluator;

    public ParametricCurve(ReadOnlySpan<(int type, uint paramCount)> fns, ParametricCurveEvaluator eval) =>
        (Functions, Evaluator) = (fns.Length > MAX_TYPES_IN_LCMS_PLUGIN ? fns[..MAX_TYPES_IN_LCMS_PLUGIN].ToArray() : fns.ToArray(), eval);

    public object Clone() =>
        new ParametricCurve(Functions, Evaluator);
}
