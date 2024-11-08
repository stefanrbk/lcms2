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

public delegate void TransformFn(Transform CMMcargo,
                                 ReadOnlySpan<byte> InputBuffer,
                                 Span<byte> OutputBuffer,
                                 uint Size,
                                 uint Stride);

public delegate void Transform2Fn(Transform CMMcargo,
                                  ReadOnlySpan<byte> InputBuffer,
                                  Span<byte> OutputBuffer,
                                  uint PixelsPerLine,
                                  uint LineCount,
                                  Stride Stride);

public class Transform : IDisposable
{
    internal uint InputFormat, OutputFormat;

    internal Transform2Fn xform;

    public Formatter16In FromInput
    {
        get;    // _cmsGetTransformFormatters16
        internal set;
    }

    public Formatter16Out ToOutput
    {
        get;    // _cmsGetTransformFormatters16
        internal set;
    }

    public FormatterFloatIn FromInputFloat
    {
        get;    // _cmsGetTransformFormattersFloat
        internal set;
    }

    public FormatterFloatOut ToOutputFloat
    {
        get;    // _cmsGetTransformFormattersFloat
        internal set;
    }

    internal Cache Cache;

    internal Pipeline? Lut;

    internal Pipeline? GamutCheck;

    internal NamedColorList InputColorant;
    internal NamedColorList OutputColorant;

    internal Signature EntryColorSpace;
    internal Signature ExitColorSpace;

    internal CIEXYZ EntryWhitePoint;
    internal CIEXYZ ExitWhitePoint;

    internal Sequence Sequence;

    internal uint dwOriginalFlags;

    public uint Flags =>    // _cmsGetTransformFlags
        dwOriginalFlags;

    internal double AdaptationState;

    internal uint RenderingIntent;

    internal Context? ContextID;

    internal object? userData;

    public object? UserData =>
        userData;

    internal FreeUserDataFn? FreeUserData;

    internal TransformFn? OldXform;

    public Transform2Fn? Worker
    {
        get;    // _cmsGetTransformWorker
        internal set;
    }

    public int MaxWorkers
    {
        get;    // _cmsGetTransformMaxWorkers
        internal set;
    }

    public uint WorkerFlags
    {
        get;    // _cmsGetTransformWorkerFlags
        internal set;
    }

    private bool disposedValue;

    public void SetUserData(object? ptr, FreeUserDataFn? FreePrivateDataFn) // _cmsSetTransformUserData
    {
        userData = ptr;
        FreeUserData = FreePrivateDataFn;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            FreeUserData?.Invoke(ContextID, UserData);

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    ~Transform()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
