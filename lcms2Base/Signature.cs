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

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace lcms2;

public readonly struct Signature : ICloneable, IEquatable<Signature>
{
    private readonly uint _value;

    [DebuggerStepThrough]
    public Signature(uint value) =>
        _value = value;

    public Signature(ReadOnlySpan<byte> value)
    {
        Span<byte> bytes = stackalloc byte[] { 0x20, 0x20, 0x20, 0x20 };
        if (value.Length > 4)
            value = value[..4];
        value.CopyTo(bytes);
        _value = BitConverter.ToUInt32(bytes);
    }

    public static explicit operator uint(Signature v) =>
        v._value;

    public static explicit operator Signature(uint v) =>
        new(v);

    object ICloneable.Clone() =>
        Clone();

    public Signature Clone() =>
        new(_value);

    public override string ToString()
    {
        Span<byte> buf = stackalloc byte[4];
        Span<char> chars = stackalloc char[4];

        // Convert to big endian
        var be = BinaryPrimitives.ReverseEndianness(_value);

        // Get bytes
        BitConverter.TryWriteBytes(buf, be);

        // Move characters
        for (var i = 0; i < 4; i++)
            chars[i] = (char)buf[i];

        return new(chars);
    }

    public bool Equals(Signature other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Signature other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)_value;
    }

    public static bool operator ==(Signature left, Signature right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Signature left, Signature right)
    {
        return !left.Equals(right);
    }

    public class Formatter : IFormatProvider, ICustomFormatter
    {
        public string Format(string? format, object? obj, IFormatProvider? provider)
        {
            if (obj is null)
                return string.Empty;

            if (obj is Signature value)
            {
                // Text output
                if (format?.StartsWith("T", StringComparison.CurrentCultureIgnoreCase) ?? false)
                    return value.ToString();

                // Hex output
                if (format?.StartsWith("X", StringComparison.CurrentCultureIgnoreCase) ?? false)
                    return String.Format(provider, "{" + format + "}", (uint)obj);
            }

            // Use default for all other formatting
            return obj is IFormattable formattable
                       ? formattable.ToString(format, CultureInfo.CurrentCulture)
                       : obj.ToString() ?? String.Empty;
        }

        public object? GetFormat(Type? formatType) =>
            formatType == typeof(ICustomFormatter) ? this : (object?)null;
    }
}
