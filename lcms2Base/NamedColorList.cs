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

namespace lcms2;

public class NamedColorList : IDisposable, ICloneable
{
    public uint nColors;
    public uint Allocated;
    public uint ColorantCount;

    public readonly byte[] Prefix;
    public readonly byte[] Suffix;

    public NamedColor[] List;

    public Context? ContextID;
    private bool disposedValue;

    public NamedColorList(Context? contextID)
    {
        ContextID = contextID;

        //var pool = Context.GetPool<byte>(contextID);

        //Prefix = pool.Rent(33);
        //Suffix = pool.Rent(33);
        Prefix = new byte[33];
        Suffix = new byte[33];

        List = null!;   // shhhh, it will get set by GrowNamedColorList()... for now...
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            //if (disposing)
            //{
            //    var bPool = Context.GetPool<byte>(ContextID);
            //    var ncPool = Context.GetPool<NamedColor>(ContextID);

            //    ReturnArray(bPool, Prefix);
            //    ReturnArray(bPool, Suffix);

            //    if (List is not null)
            //        ReturnArray(ncPool, List);
            //}

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    object ICloneable.Clone() =>
        Clone();

    public NamedColorList Clone() =>
        throw new NotImplementedException();
}
