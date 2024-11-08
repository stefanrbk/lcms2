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

public class TagLinkedList : IList<Tag>, ICloneable
{
    private readonly List<Tag> _list;

    public TagLinkedList() =>
        _list = new();

    public TagLinkedList(int capacity) =>
        _list = new(capacity);

    public TagLinkedList(params Tag[] list) =>
        _list = new(list);

    public TagLinkedList(IEnumerable<Tag> list) =>
        _list = new(list);

    public Tag this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    public int Count =>
        _list.Count;

    public bool IsReadOnly =>
        ((ICollection<Tag>)_list).IsReadOnly;

    public void Add(Tag item) =>
        _list.Add(item);

    public void Clear() =>
        _list.Clear();

    public object Clone() =>
        new TagLinkedList(_list.Select(c => (Tag)c.Clone()));

    public bool Contains(Tag item) =>
        _list.Contains(item);

    public void CopyTo(Tag[] array, int arrayIndex) =>
        _list.CopyTo(array, arrayIndex);

    public IEnumerator<Tag> GetEnumerator() =>
        _list.GetEnumerator();

    public int IndexOf(Tag item) =>
        _list.IndexOf(item);

    public void Insert(int index, Tag item) =>
        _list.Insert(index, item);

    public bool Remove(Tag item) =>
        _list.Remove(item);

    public void RemoveAt(int index) =>
        _list.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)_list).GetEnumerator();
}

public class Tag(Signature sig, TagDescriptor desc) : ICloneable
{
    public Signature Signature = sig;
    public TagDescriptor Descriptor = desc;

    public Tag(Signature sig, uint elemCount, ReadOnlySpan<Signature> supportedTypes, TagTypeDecider? decideType)
        : this(sig, new(elemCount, supportedTypes, decideType)) { }

    public object Clone() =>
        new Tag(Signature, Descriptor);
}
