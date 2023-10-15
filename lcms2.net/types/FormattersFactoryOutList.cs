﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System
//  Copyright (c) 1998-2023 Marti Maria Saguer
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

internal class FormattersFactoryOutList : IList<FormatterFactoryOut>, ICloneable
{
    private readonly List<FormatterFactoryOut> _list;

    public FormattersFactoryOutList() =>
        _list = new();

    public FormattersFactoryOutList(int capacity) =>
        _list = new(capacity);

    public FormattersFactoryOutList(IEnumerable<FormatterFactoryOut> list) =>
        _list = new(list);

    public FormatterFactoryOut this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    public int Count =>
        _list.Count;

    public bool IsReadOnly =>
        ((ICollection<FormatterFactoryOut>)_list).IsReadOnly;

    public void Add(FormatterFactoryOut item) =>
        _list.Add(item);

    public void Clear() =>
        _list.Clear();

    public object Clone() =>
        new FormattersFactoryOutList(_list.Select(c => (FormatterFactoryOut)c.Clone()));

    public bool Contains(FormatterFactoryOut item) =>
        _list.Contains(item);

    public void CopyTo(FormatterFactoryOut[] array, int arrayIndex) =>
        _list.CopyTo(array, arrayIndex);

    public IEnumerator<FormatterFactoryOut> GetEnumerator() =>
        _list.GetEnumerator();

    public int IndexOf(FormatterFactoryOut item) =>
        _list.IndexOf(item);

    public void Insert(int index, FormatterFactoryOut item) =>
        _list.Insert(index, item);

    public bool Remove(FormatterFactoryOut item) =>
        _list.Remove(item);

    public void RemoveAt(int index) =>
        _list.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)_list).GetEnumerator();
}