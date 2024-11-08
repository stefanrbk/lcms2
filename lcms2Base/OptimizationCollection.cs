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

using System.Collections;

namespace lcms2;

public class OptimizationCollection : IList<OptimizeFn>, ICloneable
{
    private readonly List<OptimizeFn> _list;

    public OptimizationCollection() =>
        _list = new();

    public OptimizationCollection(int capacity) =>
        _list = new(capacity);

    public OptimizationCollection(IEnumerable<OptimizeFn> list) =>
        _list = new(list);

    public OptimizeFn this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    public int Count =>
        _list.Count;

    public bool IsReadOnly =>
        ((ICollection<OptimizeFn>)_list).IsReadOnly;

    public void Add(OptimizeFn item) =>
        _list.Add(item);

    public void Clear() =>
        _list.Clear();

    public object Clone() =>
        new OptimizationCollection(_list.Select(c => (OptimizeFn)c.Clone()));

    public bool Contains(OptimizeFn item) =>
        _list.Contains(item);

    public void CopyTo(OptimizeFn[] array, int arrayIndex) =>
        _list.CopyTo(array, arrayIndex);

    public IEnumerator<OptimizeFn> GetEnumerator() =>
        _list.GetEnumerator();

    public int IndexOf(OptimizeFn item) =>
        _list.IndexOf(item);

    public void Insert(int index, OptimizeFn item) =>
        _list.Insert(index, item);

    public bool Remove(OptimizeFn item) =>
        _list.Remove(item);

    public void RemoveAt(int index) =>
        _list.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)_list).GetEnumerator();
}
