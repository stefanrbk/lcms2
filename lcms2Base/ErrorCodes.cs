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

using Microsoft.Extensions.Logging;

namespace lcms2;

public static class ErrorCodes
{
    public static readonly EventId Undefined = new(1, "Undefined error");
    public static readonly EventId File = new(2, "File system error");
    public static readonly EventId Range = new(3, "Value out of range");
    public static readonly EventId Internal = new(4, "Internal error");
    public static readonly EventId Null = new(5, "Value was null");
    public static readonly EventId Read = new(6, "IO read error");
    public static readonly EventId Seek = new(7, "IO seek error");
    public static readonly EventId Write = new(8, "IO write error");
    public static readonly EventId UnknownExtension = new(9, "Unknown extension type");
    public static readonly EventId ColorspaceCheck = new(10, "Invalid color space");
    public static readonly EventId AlreadyDefined = new(11, "Value already defined");
    public static readonly EventId BadSignature = new(12, "Object has bad signature");
    public static readonly EventId CorruptionDetected = new(13, "Corruption detected");
    public static readonly EventId NotSuitable = new(14, "Value not suitable");
}
