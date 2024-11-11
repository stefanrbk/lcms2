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

using lcms2.types;

using Microsoft.Extensions.Logging;

using System.Runtime.CompilerServices;

namespace lcms2.testbed;

internal static partial class Testbed
{
    public static bool CheckBaseTypes()
    {
#pragma warning disable CS8519 // The given expression never matches the provided pattern.
        if (sizeof(byte) is not 1) return false;
        if (sizeof(sbyte) is not 1) return false;
        if (sizeof(ushort) is not 2) return false;
        if (sizeof(short) is not 2) return false;
        if (sizeof(uint) is not 4) return false;
        if (sizeof(int) is not 4) return false;
        if (sizeof(ulong) is not 8) return false;
        if (sizeof(long) is not 8) return false;
        if (sizeof(float) is not 4) return false;
        if (sizeof(double) is not 8) return false;
        if (Unsafe.SizeOf<Signature>() is not 4) return false;
#pragma warning restore CS8519 // The given expression never matches the provided pattern.

        return true;
    }

    public static bool CheckQuickFloor()
    {
        if (_cmsQuickFloor(1.234) is not 1 ||
            _cmsQuickFloor(32767.234) is not 32767 ||
            _cmsQuickFloor(-1.234) is not -2 ||
            _cmsQuickFloor(-32767.1) is not -32768)
        {
            Die("""

                OOOPPSS! Helpers.QuickFloor() does not work as expected in your machine!

                Please use the "(No Fast Floor)" configuration toggles.

                """);
            return false;
        }
        return true;
    }

    public static bool CheckQuickFloorWord()
    {
        for (var i = 0; i < UInt16.MaxValue; i++)
        {
            if (_cmsQuickFloorWord(i + 0.1234) != i)
            {
                Die("""

                    OOOPPSS! Helpers.QuickFloorWord() does not work as expected in your machine!

                    Please use the "(No Fast Floor)" configuration toggles.

                    """);
                return false;
            }
        }
        return true;
    }

    private static bool TestSingleFixed15_16(double value)
    {
        var f = _cmsDoubleTo15Fixed16(value);
        var roundTrip = _cms15Fixed16toDouble(f);
        var error = Math.Abs(value - roundTrip);

        return error <= FIXED_PRECISION_15_16;
    }

    public static bool CheckFixedPoint15_16() =>
        TestSingleFixed15_16(1.0) &&
        TestSingleFixed15_16(2.0) &&
        TestSingleFixed15_16(1.23456) &&
        TestSingleFixed15_16(0.99999) &&
        TestSingleFixed15_16(0.1234567890123456789099999) &&
        TestSingleFixed15_16(-1.0) &&
        TestSingleFixed15_16(-2.0) &&
        TestSingleFixed15_16(-1.123456) &&
        TestSingleFixed15_16(-1.1234567890123456789099999) &&
        TestSingleFixed15_16(32767.1234567890123456789099999) &&
        TestSingleFixed15_16(-32767.1234567890123456789099999);

    private static bool TestSingleFixed8_8(double value)
    {
        var f = _cmsDoubleTo8Fixed8(value);
        var roundTrip = _cms8Fixed8toDouble(f);
        var error = Math.Abs(value - roundTrip);

        return error <= FIXED_PRECISION_8_8;
    }

    public static bool CheckFixedPoint8_8() =>
        TestSingleFixed8_8(1.0) &&
        TestSingleFixed8_8(2.0) &&
        TestSingleFixed8_8(1.23456) &&
        TestSingleFixed8_8(0.99999) &&
        TestSingleFixed8_8(0.1234567890123456789099999) &&
        TestSingleFixed8_8(255.1234567890123456789099999);

    public static bool CheckD50Roundtrip()
    {
        const double d50x2 = 0.96420288;
        const double d50y2 = 1.0;
        const double d50z2 = 0.82490540;

        var xe = _cmsDoubleTo15Fixed16(cmsD50X);
        var ye = _cmsDoubleTo15Fixed16(cmsD50Y);
        var ze = _cmsDoubleTo15Fixed16(cmsD50Z);

        var x = _cms15Fixed16toDouble(xe);
        var y = _cms15Fixed16toDouble(ye);
        var z = _cms15Fixed16toDouble(ze);

        var dx = Math.Abs(cmsD50X - x);
        var dy = Math.Abs(cmsD50Y - y);
        var dz = Math.Abs(cmsD50Z - z);

        var euc = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));

        if (euc > 1E-5)
        {
            logger.LogWarning("D50 roundtrip |err| > ({euc})", euc);
            return false;
        }

        xe = _cmsDoubleTo15Fixed16(d50x2);
        ye = _cmsDoubleTo15Fixed16(d50y2);
        ze = _cmsDoubleTo15Fixed16(d50z2);

        x = _cms15Fixed16toDouble(xe);
        y = _cms15Fixed16toDouble(ye);
        z = _cms15Fixed16toDouble(ze);

        dx = Math.Abs(d50x2 - x);
        dy = Math.Abs(d50y2 - y);
        dz = Math.Abs(d50z2 - z);

        euc = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));

        if (euc > 1E-5)
        {
            logger.LogWarning("D50 roundtrip |err| > ({euc})", euc);
            return false;
        }

        return true;
    }
}
