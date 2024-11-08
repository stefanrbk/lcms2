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

public class StageMatrixData : IDisposable
{
    private double[] @double;
    private double[]? offset;
    private bool disposedValue;
    //private readonly ArrayPool<double>? pool;

    public double[] Double
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            return @double;
        }
        set => @double = value;
    }

    public double[]? Offset
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);
            return offset;
        }
        set => offset = value;
    }

    public StageMatrixData(ReadOnlySpan<double> @double,
                           ReadOnlySpan<double> offset = default /*, ArrayPool<double>? pool = null*/)
    {
        this.@double = /*pool is null
            ?*/ new double[@double.Length]
            /*: pool.Rent(@double.Length)*/;
        @double.CopyTo(Double);

        Offset = offset.Length > 0
                     ? /*pool is null
                         ?*/ new double[offset.Length]
                     /*: pool.Rent(offset.Length)*/
                     : null;
        if (Offset is not null)
            offset.CopyTo(Offset);

        disposedValue = false;
        //this.pool = pool;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                //if (pool is not null)
                //{
                //    ReturnArray(pool, Double);
                Double = null!;
                //if (Offset is not null)
                //    ReturnArray(pool, Offset);
                Offset = null!;
                //}
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
