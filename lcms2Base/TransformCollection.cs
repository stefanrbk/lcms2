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
using System.Diagnostics.CodeAnalysis;

namespace lcms2;

public class TransformCollection : IList<TransformFunc>, ICloneable
{
    private readonly List<TransformFunc> _list;

    public TransformCollection() =>
        _list = new();

    public TransformCollection(int capacity) =>
        _list = new(capacity);

    public TransformCollection(IEnumerable<TransformFunc> list) =>
        _list = new(list);

    public TransformFunc this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    public int Count =>
        _list.Count;

    public bool IsReadOnly =>
        ((ICollection<TransformFunc>)_list).IsReadOnly;

    public void Add(TransformFunc item) =>
        _list.Add(item);

    public void Clear() =>
        _list.Clear();

    public object Clone() =>
        new TransformCollection(_list.Select(c => (TransformFunc)c.Clone()));

    public bool Contains(TransformFunc item) =>
        _list.Contains(item);

    public void CopyTo(TransformFunc[] array, int arrayIndex) =>
        _list.CopyTo(array, arrayIndex);

    public IEnumerator<TransformFunc> GetEnumerator() =>
        _list.GetEnumerator();

    public int IndexOf(TransformFunc item) =>
        _list.IndexOf(item);

    public void Insert(int index, TransformFunc item) =>
        _list.Insert(index, item);

    public bool Remove(TransformFunc item) =>
        _list.Remove(item);

    public void RemoveAt(int index) =>
        _list.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)_list).GetEnumerator();
}

public class TransformFunc : ICloneable
{
    public Transform2Factory? Factory;
    public TransformFactory? OldFactory;

    [MemberNotNullWhen(true, nameof(OldFactory))]
    [MemberNotNullWhen(false, nameof(Factory))]
    public bool OldXform =>
        OldFactory is not null;

    public TransformFunc(Transform2Factory factory) =>
        Factory = factory;

    public TransformFunc(TransformFactory factory) =>
        OldFactory = factory;

    public object Clone() =>
        OldXform
            ? new TransformFunc(OldFactory)
            : new TransformFunc(Factory);
}
