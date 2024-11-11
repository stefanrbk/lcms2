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

using System.Text;

namespace lcms2.testbed;

internal static partial class Testbed
{
    // Since prime factors of 65535 (FFFF) are,
    //
    //            0xFFFF = 3 * 5 * 17 * 257
    //
    // I test tables of 2, 4, 6, and 18 points, that will be exact.
    private static void BuildTable(int n, Span<ushort> tab, bool descending)
    {
        for (var i = 0; i < n; i++)
        {
            var v = 65535.0 * i / (n - 1);

            tab[descending ? n - i - 1 : i] = (ushort)Math.Floor(v + 0.5);
        }
    }

    // A single function that does check 1D interpolation
    // nNodesToCheck = number on nodes to check
    // Down = Create decreasing tables
    // Reverse = Check reverse interpolation
    // max_err = max allowed error
    public static bool Check1D(int numNodesToCheck, bool down, int maxErr)
    {
        Span<ushort> @in = stackalloc ushort[1];
        Span<ushort> @out = stackalloc ushort[1];

        //var tab = calloc<ushort>((uint)numNodesToCheck);
        //if (tab is null) return false;
        var tab = new ushort[numNodesToCheck];

        var p = _cmsComputeInterpParams(DbgThread(), (uint)numNodesToCheck, 1, 1, tab, LerpFlag.Ushort);
        if (p is null) return false;

        BuildTable(numNodesToCheck, tab, down);

        for (var i = 0; i <= 0xFFFF; i++)
        {
            @in[0] = (ushort)i;
            @out[0] = 0;

            p.Interpolation.Lerp16(@in, @out, p);

            if (down) @out[0] = (ushort)(0xFFFF - @out[0]);

            if (Math.Abs(@out[0] - @in[0]) > maxErr)
            {
                logger.LogWarning("({numNodesToCheck}): Must be {in}, but was {out}", numNodesToCheck, @in[0], @out[0]);
                _cmsFreeInterpParams(p);
                //free(tab);
                return false;
            }
        }

        _cmsFreeInterpParams(p);
        //free(tab);
        return true;
    }

    public static bool Check1DLerp2() =>
        Check1D(2, false, 0);

    public static bool Check1DLerp3() =>
            Check1D(3, false, 1);

    public static bool Check1DLerp4() =>
            Check1D(4, false, 0);

    public static bool Check1DLerp6() =>
            Check1D(6, false, 0);

    public static bool Check1DLerp18() =>
        Check1D(18, false, 0);

    public static bool Check1DLerp2Down() =>
        Check1D(2, true, 0);

    public static bool Check1DLerp3Down() =>
        Check1D(3, true, 1);

    public static bool Check1DLerp4Down() =>
        Check1D(4, true, 0);

    public static bool Check1DLerp6Down() =>
        Check1D(6, true, 0);

    public static bool Check1DLerp18Down() =>
        Check1D(18, true, 0);

    public static bool ExhaustiveCheck1DLerp()
    {
        var rc = true;

        var tasks = new List<Task<bool>>();
        var check = new bool[4086];
        for (var i = 0; i < 4096; i += 256)
        {
            tasks.Add(Task.Factory.StartNew((o) =>
            {
                var offset = (int)o!;
                var list = new List<bool>(256);
                for (var j = offset; j < offset + 256; j++)
                {
                    if (j is 0)
                        j = 10;

                    check[j - 10] = true;

                    list.Add(Check1D(j, false, 1));
                }
                return !list.Contains(false);
            }, i));
        }

        Task.WaitAll(tasks.ToArray());

        if (tasks.Select(t => t.IsCompletedSuccessfully && t.Result).Contains(false))
            goto Err;

        goto Done;

    Err:
        rc = false;

    Done:
        var sb = new StringBuilder();
        if (HasConsole)
        {
            var first = true;
            for (var i = 0; i < 4086; i++)
            {
                if (!check[i])
                {
                    if (!first)
                        sb.Append(", ");

                    sb.Append(i - 10);
                    first = false;
                }
            }

            var errors = sb.ToString();
            if (!String.IsNullOrEmpty(errors))
                logger.LogWarning("{errors}", errors);
        }
        return rc;
    }

    public static bool ExhaustiveCheck1DLerpDown()
    {
        var rc = true;

        var tasks = new List<Task<bool>>();
        var check = new bool[4086];
        for (var i = 0; i < 4096; i += 256)
        {
            tasks.Add(Task.Factory.StartNew((o) =>
            {
                var offset = (int)o!;
                var list = new List<bool>(256);
                for (var j = offset; j < offset + 256; j++)
                {
                    if (j is 0)
                        j = 10;

                    check[j - 10] = true;

                    list.Add(Check1D(j, true, 1));
                }
                return !list.Contains(false);
            }, i));
        }

        Task.WaitAll(tasks.ToArray());

        if (tasks.Select(t => t.IsCompletedSuccessfully && t.Result).Contains(false))
            goto Err;

        goto Done;

    Err:
        rc = false;

    Done:
        var sb = new StringBuilder();
        if (HasConsole)
        {
            var first = true;
            for (var i = 0; i < 4086; i++)
            {
                if (!check[i])
                {
                    if (!first)
                        sb.Append(", ");

                    sb.Append(i - 10);
                    first = false;
                }
            }

            var errors = sb.ToString();
            if (!String.IsNullOrEmpty(errors))
                logger.LogWarning("{errors}", errors);
        }
        return rc;
    }

    public static bool Check3DInterpolationFloatTetrahedral()
    {
        var floatTable = new float[]
        {
            0,
            0,
            0,            // B=0, G=0, R=0

            0,
            0,
            .25f,         // B=1, G=0, R=0

            0,
            .5f,
            0,          // B=0, G=1, R=0

            0,
            .5f,
            .25f,       // B=1, G=1, R=0

            1,
            0,
            0,            // B=0, G=0, R=1

            1,
            0,
            .25f,         // B=1, G=0, R=1

            1,
            .5f,
            0,          // B=0, G=1, R=1

            1,
            .5f,
            .25f,       // B=1, G=1, R=1
        };
        Span<float> @in = stackalloc float[3];
        Span<float> @out = stackalloc float[3];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, floatTable, LerpFlag.Float);

        MaxErr = 0.0;
        for (var i = 0; i < 0xFFFF; i++)
        {
            @in[0] = @in[1] = @in[2] = i / 65535f;

            p.Interpolation.LerpFloat(@in, @out, p);

            if (!IsGoodFixed15_16("Channel 1", @out[0], @in[0])) goto Error;
            if (!IsGoodFixed15_16("Channel 2", @out[1], @in[1] / 2f)) goto Error;
            if (!IsGoodFixed15_16("Channel 2", @out[2], @in[2] / 4f)) goto Error;
        }

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);

        _cmsFreeInterpParams(p);
        return true;
    Error:
        _cmsFreeInterpParams(p);
        return false;
    }

    public static bool Check3DInterpolationFloatTrilinear()
    {
        var floatTable = new float[]
        {
            0,
            0,
            0,            // B=0, G=0, R=0

            0,
            0,
            .25f,         // B=1, G=0, R=0

            0,
            .5f,
            0,          // B=0, G=1, R=0

            0,
            .5f,
            .25f,       // B=1, G=1, R=0

            1,
            0,
            0,            // B=0, G=0, R=1

            1,
            0,
            .25f,         // B=1, G=0, R=1

            1,
            .5f,
            0,          // B=0, G=1, R=1

            1,
            .5f,
            .25f,       // B=1, G=1, R=1
        };
        Span<float> @in = stackalloc float[3];
        Span<float> @out = stackalloc float[3];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, floatTable, LerpFlag.Float | LerpFlag.Trilinear);

        MaxErr = 0.0;
        for (var i = 0; i < 0xFFFF; i++)
        {
            @in[0] = @in[1] = @in[2] = i / 65535f;

            p.Interpolation.LerpFloat(@in, @out, p);

            if (!IsGoodFixed15_16("Channel 1", @out[0], @in[0])) goto Error;
            if (!IsGoodFixed15_16("Channel 2", @out[1], @in[1] / 2f)) goto Error;
            if (!IsGoodFixed15_16("Channel 2", @out[2], @in[2] / 4f)) goto Error;
        }

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);
        _cmsFreeInterpParams(p);
        return true;
    Error:
        _cmsFreeInterpParams(p);
        return false;
    }

    public static bool Check3DInterpolationTetrahedral16()
    {
        var table = new ushort[]
        {
            0,
            0,
            0,                // B=0, G=0, R=0

            0,
            0,
            0xFFFF,           // B=1, G=0, R=0

            0,
            0xFFFF,
            0,           // B=0, G=1, R=0

            0,
            0xFFFF,
            0xFFFF,      // B=1, G=1, R=0

            0xFFFF,
            0,
            0,           // B=0, G=0, R=1

            0xFFFF,
            0,
            0xFFFF,      // B=1, G=0, R=1

            0xFFFF,
            0xFFFF,
            0,      // B=0, G=1, R=1

            0xFFFF,
            0xFFFF,
            0xFFFF, // B=1, G=1, R=1
        };
        Span<ushort> @in = stackalloc ushort[3];
        Span<ushort> @out = stackalloc ushort[3];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, table, LerpFlag.Ushort);

        MaxErr = 0;
        for (var i = 0; i < 0xFFFF; i++)
        {
            @in[0] = @in[1] = @in[2] = (ushort)i;

            p.Interpolation.Lerp16(@in, @out, p);

            if (!IsGoodWord("Channel 1", @out[0], @in[0])) goto Error;
            if (!IsGoodWord("Channel 2", @out[1], @in[1])) goto Error;
            if (!IsGoodWord("Channel 2", @out[2], @in[2])) goto Error;
        }

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);
        _cmsFreeInterpParams(p);
        return true;
    Error:
        _cmsFreeInterpParams(p);
        return false;
    }

    public static bool Check3DInterpolationTrilinear16()
    {
        var table = new ushort[]
        {
            0,
            0,
            0,                // B=0, G=0, R=0

            0,
            0,
            0xFFFF,           // B=1, G=0, R=0

            0,
            0xFFFF,
            0,           // B=0, G=1, R=0

            0,
            0xFFFF,
            0xFFFF,      // B=1, G=1, R=0

            0xFFFF,
            0,
            0,           // B=0, G=0, R=1

            0xFFFF,
            0,
            0xFFFF,      // B=1, G=0, R=1

            0xFFFF,
            0xFFFF,
            0,      // B=0, G=1, R=1

            0xFFFF,
            0xFFFF,
            0xFFFF, // B=1, G=1, R=1
        };
        Span<ushort> @in = stackalloc ushort[3];
        Span<ushort> @out = stackalloc ushort[3];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, table, LerpFlag.Trilinear);

        MaxErr = 0;
        for (var i = 0; i < 0xFFFF; i++)
        {
            @in[0] = @in[1] = @in[2] = (ushort)i;

            p.Interpolation.Lerp16(@in, @out, p);

            if (!IsGoodWord("Channel 1", @out[0], @in[0])) goto Error;
            if (!IsGoodWord("Channel 2", @out[1], @in[1])) goto Error;
            if (!IsGoodWord("Channel 2", @out[2], @in[2])) goto Error;
        }

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);
        _cmsFreeInterpParams(p);
        return true;
    Error:
        _cmsFreeInterpParams(p);
        return false;
    }

    private static void Check3DInterpolationFloatTask(InterpParams<float> p, bool[,,] check, bool[,,] isGood, object? o)
    {
        var offset = (int)o!;
        Span<float> @in = stackalloc float[3];
        Span<float> @out = stackalloc float[3];
        for (var r = offset; r < offset + 4; r++)
        {
            for (var g = 0; g <= 0xFF; g++)
            {
                for (var b = 0; b <= 0xFF; b++)
                {
                    @in[0] = r / 255f;
                    @in[1] = g / 255f;
                    @in[2] = b / 255f;

                    p.Interpolation.LerpFloat(@in, @out, p);

                    isGood[r, g, b] = IsGoodFixed15_16($"({r},{g},{b}): Channel 1", @out[0], @in[0]) &&
                                      IsGoodFixed15_16($"({r},{g},{b}): Channel 2", @out[1], @in[1] / 2) &&
                                      IsGoodFixed15_16($"({r},{g},{b}): Channel 2", @out[2], @in[2] / 4);

                    check[r, g, b] = true;
                }
            }
        }
    }

    private static bool Validate3DInterpolationValues(bool[,,] check, bool[,,] isGood)
    {
        var sb = new StringBuilder();
        var first = true;
        for (var r = 0; r < 256; r++)
        {
            for (var g = 0; g < 256; g++)
            {
                for (var b = 0; b < 256; b++)
                {
                    if (!isGood[r, g, b])
                    {
                        if (!first)
                            sb.Append(", ");

                        sb.Append('(')
                          .Append(r)
                          .Append(", ")
                          .Append(g)
                          .Append(", ")
                          .Append(b)
                          .Append(')');
                        first = false;
                    }
                    if (!check[r, g, b])
                    {
                        throw new Exception($"Missing check [{r},{g},{b}]");
                    }
                }
            }
        }

        var errors = sb.ToString();
        var rc = String.IsNullOrEmpty(errors);
        if (HasConsole && !rc)
            logger.LogWarning("{errors}", errors);

        return rc;
    }

    public static bool ExhaustiveCheck3DInterpolationFloatTetrahedral()
    {
        var floatTable = new float[]
        {
            0,
            0,
            0,            // B=0, G=0, R=0

            0,
            0,
            .25f,         // B=1, G=0, R=0

            0,
            .5f,
            0,          // B=0, G=1, R=0

            0,
            .5f,
            .25f,       // B=1, G=1, R=0

            1,
            0,
            0,            // B=0, G=0, R=1

            1,
            0,
            .25f,         // B=1, G=0, R=1

            1,
            .5f,
            0,          // B=0, G=1, R=1

            1,
            .5f,
            .25f,       // B=1, G=1, R=1
        };
        bool[,,] check = new bool[256, 256, 256];
        bool[,,] isGood = new bool[256, 256, 256];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, floatTable, LerpFlag.Float);

        MaxErr = 0.0;

        var tasks = new List<Task>(64);
        for (var i = 0; i < 0xFF; i += 4)
            tasks.Add(Task.Factory.StartNew((o) => Check3DInterpolationFloatTask(p, check, isGood, o), i));

        Task.WaitAll(tasks.ToArray());

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);
        _cmsFreeInterpParams(p);

        return Validate3DInterpolationValues(check, isGood);
    }

    public static bool ExhaustiveCheck3DInterpolationFloatTrilinear()
    {
        var floatTable = new float[]
        {
            0,
            0,
            0,            // B=0, G=0, R=0

            0,
            0,
            .25f,         // B=1, G=0, R=0

            0,
            .5f,
            0,          // B=0, G=1, R=0

            0,
            .5f,
            .25f,       // B=1, G=1, R=0

            1,
            0,
            0,            // B=0, G=0, R=1

            1,
            0,
            .25f,         // B=1, G=0, R=1

            1,
            .5f,
            0,          // B=0, G=1, R=1

            1,
            .5f,
            .25f,       // B=1, G=1, R=1
        };
        bool[,,] check = new bool[256, 256, 256];
        bool[,,] isGood = new bool[256, 256, 256];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, floatTable, LerpFlag.Float | LerpFlag.Trilinear);

        MaxErr = 0.0;

        var tasks = new List<Task>(64);
        for (var i = 0; i < 0xFF; i += 4)
            tasks.Add(Task.Factory.StartNew((o) => Check3DInterpolationFloatTask(p, check, isGood, o), i));

        Task.WaitAll(tasks.ToArray());

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);

        _cmsFreeInterpParams(p);
        return Validate3DInterpolationValues(check, isGood);
    }

    private static void Check3DInterpolation16Task(InterpParams<ushort> p, bool[,,] check, bool[,,] isGood, object? o)
    {
        var offset = (int)o!;
        Span<ushort> @in = stackalloc ushort[3];
        Span<ushort> @out = stackalloc ushort[3];
        for (var r = offset; r < offset + 16; r++)
        {
            for (var g = 0; g <= 0xFF; g++)
            {
                for (var b = 0; b <= 0xFF; b++)
                {
                    @in[0] = (ushort)r;
                    @in[1] = (ushort)g;
                    @in[2] = (ushort)b;

                    p.Interpolation.Lerp16(@in, @out, p);

                    isGood[r, g, b] = IsGoodWord("Channel 1", @out[0], @in[0]) &&
                                      IsGoodWord("Channel 2", @out[1], @in[1]) &&
                                      IsGoodWord("Channel 2", @out[2], @in[2]);

                    check[r, g, b] = true;
                }
            }
        }
    }

    public static bool ExhaustiveCheck3DInterpolationTetrahedral16()
    {
        var table = new ushort[]
        {
            0,
            0,
            0,                // B=0, G=0, R=0

            0,
            0,
            0xFFFF,           // B=1, G=0, R=0

            0,
            0xFFFF,
            0,           // B=0, G=1, R=0

            0,
            0xFFFF,
            0xFFFF,      // B=1, G=1, R=0

            0xFFFF,
            0,
            0,           // B=0, G=0, R=1

            0xFFFF,
            0,
            0xFFFF,      // B=1, G=0, R=1

            0xFFFF,
            0xFFFF,
            0,      // B=0, G=1, R=1

            0xFFFF,
            0xFFFF,
            0xFFFF, // B=1, G=1, R=1
        };
        bool[,,] check = new bool[256, 256, 256];
        bool[,,] isGood = new bool[256, 256, 256];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, table, LerpFlag.Ushort);

        MaxErr = 0.0;
        var tasks = new List<Task>(16);
        for (var i = 0; i <= 0xFF; i += 16)
        {
            tasks.Add(Task.Factory.StartNew((o) => Check3DInterpolation16Task(p, check, isGood, o), i));
        }

        Task.WaitAll(tasks.ToArray());

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);
        _cmsFreeInterpParams(p);
        return Validate3DInterpolationValues(check, isGood);
    }

    public static bool ExhaustiveCheck3DInterpolationTrilinear16()
    {
        var table = new ushort[]
        {
            0,
            0,
            0,                // B=0, G=0, R=0
            0,
            0,
            0xFFFF,           // B=1, G=0, R=0

            0,
            0xFFFF,
            0,           // B=0, G=1, R=0
            0,
            0xFFFF,
            0xFFFF,      // B=1, G=1, R=0

            0xFFFF,
            0,
            0,           // B=0, G=0, R=1
            0xFFFF,
            0,
            0xFFFF,      // B=1, G=0, R=1

            0xFFFF,
            0xFFFF,
            0,      // B=0, G=1, R=1
            0xFFFF,
            0xFFFF,
            0xFFFF, // B=1, G=1, R=1
        };
        bool[,,] check = new bool[256, 256, 256];
        bool[,,] isGood = new bool[256, 256, 256];

        var p = _cmsComputeInterpParams(DbgThread(), 2, 3, 3, table, LerpFlag.Trilinear);

        MaxErr = 0.0;
        var tasks = new List<Task>(16);
        for (var i = 0; i <= 0xFF; i += 16)
        {
            tasks.Add(Task.Factory.StartNew((o) => Check3DInterpolation16Task(p, check, isGood, o), i));
        }

        Task.WaitAll(tasks.ToArray());

        if (MaxErr > 0)
            logger.LogInformation("|Err|<{MaxErr}", MaxErr);
        _cmsFreeInterpParams(p);
        return Validate3DInterpolationValues(check, isGood);
    }

    public static bool CheckReverseInterpolation3x3()
    {
        Span<float> target = stackalloc float[4];
        Span<float> result = stackalloc float[4];
        Span<float> hint = stackalloc float[4];

        Span<ushort> table = stackalloc ushort[]
        {
            0,
            0,
            0,                // B=0, G=0, R=0
            0,
            0,
            0xFFFF,           // B=1, G=0, R=0

            0,
            0xFFFF,
            0,           // B=0, G=1, R=0
            0,
            0xFFFF,
            0xFFFF,      // B=1, G=1, R=0

            0xFFFF,
            0,
            0,           // B=0, G=0, R=1
            0xFFFF,
            0,
            0xFFFF,      // B=1, G=0, R=1

            0xFFFF,
            0xFFFF,
            0,      // B=0, G=1, R=1
            0xFFFF,
            0xFFFF,
            0xFFFF, // B=1, G=1, R=1
        };

        var lut = cmsPipelineAlloc(DbgThread(), 3, 3);

        var clut = cmsStageAllocCLut16bit(DbgThread(), 2, 3, 3, table);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, clut);

        target[0] = target[1] = target[2] = 0;
        hint[0] = hint[1] = hint[2] = 0;
        cmsPipelineEvalReverseFloat(target, result, null, lut);
        if (result[0] is not 0 || result[1] is not 0 || result[2] is not 0)
        {
            logger.LogWarning("Reverse interpolation didn't find zero");
            goto Error;
        }

        // Transverse identity
        var max = 0f;
        for (var i = 0; i <= 100; i++)
        {
            var @in = i / 100f;

            target[0] = @in; target[1] = 0; target[2] = 0;
            cmsPipelineEvalReverseFloat(target, result, hint, lut);

            var err = MathF.Abs(@in - result[0]);
            if (err > max) max = err;

            memcpy(hint, result, 4);
        }

        cmsPipelineFree(lut);
        return max <= FLOAT_PRECISION;

    Error:
        cmsPipelineFree(lut);
        return false;
    }

    public static bool CheckReverseInterpolation4x3()
    {
        Span<float> target = stackalloc float[4];
        Span<float> result = stackalloc float[4];
        Span<float> hint = stackalloc float[4];

        Span<ushort> table = stackalloc ushort[]
        {
            0,
            0,
            0,                // 0 0 0 0 = ( 0, 0, 0)
            0,
            0,
            0,                // 0 0 0 1 = ( 0, 0, 0)

            0,
            0,
            0xFFFF,           // 0 0 1 0 = ( 0, 0, 1)
            0,
            0,
            0xFFFF,           // 0 0 1 1 = ( 0, 0, 1)

            0,
            0xFFFF,
            0,           // 0 1 0 0 = ( 0, 1, 0)
            0,
            0xFFFF,
            0,           // 0 1 0 1 = ( 0, 1, 0)

            0,
            0xFFFF,
            0xFFFF,      // 0 1 1 0 = ( 0, 1, 1)
            0,
            0xFFFF,
            0xFFFF,      // 0 1 1 1 = ( 0, 1, 1)

            0xFFFF,
            0,
            0,           // 1 0 0 0 = ( 1, 0, 0)
            0xFFFF,
            0,
            0,           // 1 0 0 1 = ( 1, 0, 0)

            0xFFFF,
            0,
            0xFFFF,      // 1 0 1 0 = ( 1, 0, 1)
            0xFFFF,
            0,
            0xFFFF,      // 1 0 1 1 = ( 1, 0, 1)

            0xFFFF,
            0xFFFF,
            0,      // 1 1 0 0 = ( 1, 1, 0)
            0xFFFF,
            0xFFFF,
            0,      // 1 1 0 1 = ( 1, 1, 0)

            0xFFFF,
            0xFFFF,
            0xFFFF, // 1 1 1 0 = ( 1, 1, 1)
            0xFFFF,
            0xFFFF,
            0xFFFF, // 1 1 1 1 = ( 1, 1, 1)
        };

        var lut = cmsPipelineAlloc(DbgThread(), 4, 3);

        var clut = cmsStageAllocCLut16bit(DbgThread(), 2, 4, 3, table);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, clut);

        // Check if the LUT is behaving as expected
        using (logger.BeginScope("4->3 feasibility"))
        {
            for (var i = 0; i <= 100; i++)
            {
                target[0] = i / 100f;
                target[1] = target[0];
                target[2] = 0;
                target[3] = 12;

                cmsPipelineEvalFloat(target, result, lut);

                if (!IsGoodFixed15_16("0", target[0], result[0])) goto Error;
                if (!IsGoodFixed15_16("1", target[1], result[1])) goto Error;
                if (!IsGoodFixed15_16("2", target[2], result[2])) goto Error;
            }
        }

        using (logger.BeginScope("4->3 zero"))
        {
            target[0] = target[1] = target[2] = 0;

            // This one holds the fixed k
            target[3] = 0;

            // This is our hint (which is a big lie in this case)
            hint[0] = hint[1] = hint[2] = 0.1f;

            cmsPipelineEvalReverseFloat(target, result, hint, lut);

            if (result[0] is not 0 || result[1] is not 0 || result[2] is not 0 || result[3] is not 0)
            {
                logger.LogWarning("Reverse interpolation didn't find zero");
                goto Error;
            }
        }

        var max = 0f;
        using (logger.BeginScope("4->3 find CMY"))
        {
            for (var i = 0; i <= 100; i++)
            {
                var @in = i / 100f;

                target[0] = @in; target[1] = 0; target[2] = 0;
                cmsPipelineEvalReverseFloat(target, result, hint, lut);

                var err = MathF.Abs(@in - result[0]);
                if (err > max) max = err;

                memcpy(hint, result, 4);
            }
        }

        cmsPipelineFree(lut);
        return max <= FLOAT_PRECISION;

    Error:
        cmsPipelineFree(lut);
        return false;
    }

    private static ushort Fn8D1(uint m, ushort a1, ushort a2, ushort a3, ushort a4 = 0, ushort a5 = 0, ushort a6 = 0, ushort a7 = 0, ushort a8 = 0) =>
        (ushort)((a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8) / m);

    private static ushort Fn8D2(uint m, ushort a1, ushort a2, ushort a3, ushort a4 = 0, ushort a5 = 0, ushort a6 = 0, ushort a7 = 0, ushort a8 = 0) =>
        (ushort)((a1 + (3 * a2) + (3 * a3) + a4 + a5 + a6 + a7 + a8) / (m + 4));

    private static ushort Fn8D3(uint m, ushort a1, ushort a2, ushort a3, ushort a4 = 0, ushort a5 = 0, ushort a6 = 0, ushort a7 = 0, ushort a8 = 0) =>
        (ushort)(((3 * a1) + (2 * a2) + (3 * a3) + a4 + a5 + a6 + a7 + a8) / (m + 5));

    private static bool Sampler3D(ReadOnlySpan<ushort> In, Span<ushort> Out, object? _)
    {
        Out[0] = Fn8D1(3, In[0], In[1], In[2]);
        Out[1] = Fn8D2(3, In[0], In[1], In[2]);
        Out[2] = Fn8D3(3, In[0], In[1], In[2]);

        return true;
    }

    private static bool Sampler4D(ReadOnlySpan<ushort> In, Span<ushort> Out, object? _)
    {
        Out[0] = Fn8D1(4, In[0], In[1], In[2], In[3]);
        Out[1] = Fn8D2(4, In[0], In[1], In[2], In[3]);
        Out[2] = Fn8D3(4, In[0], In[1], In[2], In[3]);

        return true;
    }

    private static bool Sampler5D(ReadOnlySpan<ushort> In, Span<ushort> Out, object? _)
    {
        Out[0] = Fn8D1(5, In[0], In[1], In[2], In[3], In[4]);
        Out[1] = Fn8D2(5, In[0], In[1], In[2], In[3], In[4]);
        Out[2] = Fn8D3(5, In[0], In[1], In[2], In[3], In[4]);
        //Con.WriteLine("\n");
        //Con.WriteLine($"In = \t{In[0]:x}\t{In[1]:x}\t{In[2]:x}\t{In[3]:x}\t{In[4]:x}");
        //Con.WriteLine($"Out = \t{Out[0]:x}\t{Out[1]:x}\t{Out[2]:x}");

        return true;
    }

    private static bool Sampler6D(ReadOnlySpan<ushort> In, Span<ushort> Out, object? _)
    {
        Out[0] = Fn8D1(6, In[0], In[1], In[2], In[3], In[4], In[5]);
        Out[1] = Fn8D2(6, In[0], In[1], In[2], In[3], In[4], In[5]);
        Out[2] = Fn8D3(6, In[0], In[1], In[2], In[3], In[4], In[5]);

        return true;
    }

    private static bool Sampler7D(ReadOnlySpan<ushort> In, Span<ushort> Out, object? _)
    {
        Out[0] = Fn8D1(7, In[0], In[1], In[2], In[3], In[4], In[5], In[6]);
        Out[1] = Fn8D2(7, In[0], In[1], In[2], In[3], In[4], In[5], In[6]);
        Out[2] = Fn8D3(7, In[0], In[1], In[2], In[3], In[4], In[5], In[6]);

        return true;
    }

    private static bool Sampler8D(ReadOnlySpan<ushort> In, Span<ushort> Out, object? _)
    {
        Out[0] = Fn8D1(8, In[0], In[1], In[2], In[3], In[4], In[5], In[6], In[7]);
        Out[1] = Fn8D2(8, In[0], In[1], In[2], In[3], In[4], In[5], In[6], In[7]);
        Out[2] = Fn8D3(8, In[0], In[1], In[2], In[3], In[4], In[5], In[6], In[7]);

        return true;
    }

    private static bool CheckOne3D(Pipeline lut, ushort a1, ushort a2, ushort a3)
    {
        Span<ushort> In = stackalloc ushort[3];
        Span<ushort> Out1 = stackalloc ushort[3];
        Span<ushort> Out2 = stackalloc ushort[3];

        In[0] = a1; In[1] = a2; In[2] = a3;

        // This is the interpolated value
        cmsPipelineEval16(In, Out1, lut);

        // This is the real value
        Sampler3D(In, Out2, null);

        // Let's see the difference

        if (!IsGoodWordPrec("Channel 1", Out1[0], Out2[0], 2)) return false;
        if (!IsGoodWordPrec("Channel 2", Out1[1], Out2[1], 2)) return false;
        if (!IsGoodWordPrec("Channel 3", Out1[2], Out2[2], 2)) return false;

        return true;
    }

    private static bool CheckOne4D(Pipeline lut, ushort a1, ushort a2, ushort a3, ushort a4)
    {
        Span<ushort> In = stackalloc ushort[4];
        Span<ushort> Out1 = stackalloc ushort[3];
        Span<ushort> Out2 = stackalloc ushort[3];

        In[0] = a1; In[1] = a2; In[2] = a3; In[3] = a4;

        // This is the interpolated value
        cmsPipelineEval16(In, Out1, lut);

        // This is the real value
        Sampler4D(In, Out2, null);

        // Let's see the difference

        if (!IsGoodWordPrec("Channel 1", Out1[0], Out2[0], 2)) return false;
        if (!IsGoodWordPrec("Channel 2", Out1[1], Out2[1], 2)) return false;
        if (!IsGoodWordPrec("Channel 3", Out1[2], Out2[2], 2)) return false;

        return true;
    }

    private static bool CheckOne5D(Pipeline lut, ushort a1, ushort a2, ushort a3, ushort a4, ushort a5)
    {
        Span<ushort> In = stackalloc ushort[5];
        Span<ushort> Out1 = stackalloc ushort[3];
        Span<ushort> Out2 = stackalloc ushort[3];

        In[0] = a1; In[1] = a2; In[2] = a3; In[3] = a4; In[4] = a5;

        // This is the interpolated value
        cmsPipelineEval16(In, Out1, lut);

        // This is the real value
        Sampler5D(In, Out2, null);

        // Let's see the difference

        if (!IsGoodWordPrec("Channel 1", Out1[0], Out2[0], 2)) return false;
        if (!IsGoodWordPrec("Channel 2", Out1[1], Out2[1], 2)) return false;
        if (!IsGoodWordPrec("Channel 3", Out1[2], Out2[2], 2)) return false;

        return true;
    }

    private static bool CheckOne6D(Pipeline lut, ushort a1, ushort a2, ushort a3, ushort a4, ushort a5, ushort a6)
    {
        Span<ushort> In = stackalloc ushort[6];
        Span<ushort> Out1 = stackalloc ushort[3];
        Span<ushort> Out2 = stackalloc ushort[3];

        In[0] = a1; In[1] = a2; In[2] = a3; In[3] = a4; In[4] = a5; In[5] = a6;

        // This is the interpolated value
        cmsPipelineEval16(In, Out1, lut);

        // This is the real value
        Sampler6D(In, Out2, null);

        // Let's see the difference

        if (!IsGoodWordPrec("Channel 1", Out1[0], Out2[0], 2)) return false;
        if (!IsGoodWordPrec("Channel 2", Out1[1], Out2[1], 2)) return false;
        if (!IsGoodWordPrec("Channel 3", Out1[2], Out2[2], 2)) return false;

        return true;
    }

    private static bool CheckOne7D(Pipeline lut, ushort a1, ushort a2, ushort a3, ushort a4, ushort a5, ushort a6, ushort a7)
    {
        Span<ushort> In = stackalloc ushort[7];
        Span<ushort> Out1 = stackalloc ushort[3];
        Span<ushort> Out2 = stackalloc ushort[3];

        In[0] = a1; In[1] = a2; In[2] = a3; In[3] = a4; In[4] = a5; In[5] = a6; In[6] = a7;

        // This is the interpolated value
        cmsPipelineEval16(In, Out1, lut);

        // This is the real value
        Sampler7D(In, Out2, null);

        // Let's see the difference

        if (!IsGoodWordPrec("Channel 1", Out1[0], Out2[0], 2)) return false;
        if (!IsGoodWordPrec("Channel 2", Out1[1], Out2[1], 2)) return false;
        if (!IsGoodWordPrec("Channel 3", Out1[2], Out2[2], 2)) return false;

        return true;
    }

    private static bool CheckOne8D(Pipeline lut, ushort a1, ushort a2, ushort a3, ushort a4, ushort a5, ushort a6, ushort a7, ushort a8)
    {
        Span<ushort> In = stackalloc ushort[8];
        Span<ushort> Out1 = stackalloc ushort[3];
        Span<ushort> Out2 = stackalloc ushort[3];

        In[0] = a1; In[1] = a2; In[2] = a3; In[3] = a4; In[4] = a5; In[5] = a6; In[6] = a7; In[7] = a8;

        // This is the interpolated value
        cmsPipelineEval16(In, Out1, lut);

        // This is the real value
        Sampler8D(In, Out2, null);

        // Let's see the difference

        if (!IsGoodWordPrec("Channel 1", Out1[0], Out2[0], 2)) return false;
        if (!IsGoodWordPrec("Channel 2", Out1[1], Out2[1], 2)) return false;
        if (!IsGoodWordPrec("Channel 3", Out1[2], Out2[2], 2)) return false;

        return true;
    }

    public static bool Check3Dinterp()
    {
        var lut = cmsPipelineAlloc(DbgThread(), 3, 3);
        var mpe = cmsStageAllocCLut16bit(DbgThread(), 9, 3, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler3D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne3D(lut, 0, 0, 0)) return false;
        if (!CheckOne3D(lut, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne3D(lut, 0x8080, 0x8080, 0x8080)) return false;
        if (!CheckOne3D(lut, 0x0000, 0xfe00, 0x80ff)) return false;
        if (!CheckOne3D(lut, 0x1111, 0x2222, 0x3333)) return false;
        if (!CheckOne3D(lut, 0x0000, 0x0012, 0x0013)) return false;
        if (!CheckOne3D(lut, 0x3141, 0x1415, 0x1592)) return false;
        if (!CheckOne3D(lut, 0xff00, 0xff01, 0xff12)) return false;

        cmsPipelineFree(lut);

        return true;
    }

    public static bool Check3DinterpGranular()
    {
        Span<uint> Dimensions = stackalloc uint[] { 7, 8, 9 };

        var lut = cmsPipelineAlloc(DbgThread(), 3, 3);
        var mpe = cmsStageAllocCLut16bitGranular(DbgThread(), Dimensions, 3, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler3D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne3D(lut, 0, 0, 0)) return false;
        if (!CheckOne3D(lut, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne3D(lut, 0x8080, 0x8080, 0x8080)) return false;
        if (!CheckOne3D(lut, 0x0000, 0xfe00, 0x80ff)) return false;
        if (!CheckOne3D(lut, 0x1111, 0x2222, 0x3333)) return false;
        if (!CheckOne3D(lut, 0x0000, 0x0012, 0x0013)) return false;
        if (!CheckOne3D(lut, 0x3141, 0x1415, 0x1592)) return false;
        if (!CheckOne3D(lut, 0xff00, 0xff01, 0xff12)) return false;

        cmsPipelineFree(lut);

        return true;
    }

    public static bool Check4Dinterp()
    {
        var lut = cmsPipelineAlloc(DbgThread(), 4, 3);
        var mpe = cmsStageAllocCLut16bit(DbgThread(), 9, 4, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler4D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne4D(lut, 0, 0, 0, 0)) return false;
        if (!CheckOne4D(lut, 0xffff, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne4D(lut, 0x8080, 0x8080, 0x8080, 0x8080)) return false;
        if (!CheckOne4D(lut, 0x0000, 0xfe00, 0x80ff, 0x8888)) return false;
        if (!CheckOne4D(lut, 0x1111, 0x2222, 0x3333, 0x4444)) return false;
        if (!CheckOne4D(lut, 0x0000, 0x0012, 0x0013, 0x0014)) return false;
        if (!CheckOne4D(lut, 0x3141, 0x1415, 0x1592, 0x9261)) return false;
        if (!CheckOne4D(lut, 0xff00, 0xff01, 0xff12, 0xff13)) return false;

        cmsPipelineFree(lut);

        return true;
    }

    public static bool Check4DinterpGranular()
    {
        Span<uint> Dimensions = stackalloc uint[] { 9, 8, 7, 6 };

        var lut = cmsPipelineAlloc(DbgThread(), 4, 3);
        var mpe = cmsStageAllocCLut16bitGranular(DbgThread(), Dimensions, 4, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler4D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne4D(lut, 0, 0, 0, 0)) return false;
        if (!CheckOne4D(lut, 0xffff, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne4D(lut, 0x8080, 0x8080, 0x8080, 0x8080)) return false;
        if (!CheckOne4D(lut, 0x0000, 0xfe00, 0x80ff, 0x8888)) return false;
        if (!CheckOne4D(lut, 0x1111, 0x2222, 0x3333, 0x4444)) return false;
        if (!CheckOne4D(lut, 0x0000, 0x0012, 0x0013, 0x0014)) return false;
        if (!CheckOne4D(lut, 0x3141, 0x1415, 0x1592, 0x9261)) return false;
        if (!CheckOne4D(lut, 0xff00, 0xff01, 0xff12, 0xff13)) return false;

        cmsPipelineFree(lut);

        return true;
    }

    public static bool Check5DinterpGranular()
    {
        Span<uint> Dimensions = stackalloc uint[] { 3, 2, 2, 2, 2 };

        var lut = cmsPipelineAlloc(DbgThread(), 5, 3);
        var mpe = cmsStageAllocCLut16bitGranular(DbgThread(), Dimensions, 5, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler5D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne5D(lut, 0, 0, 0, 0, 0)) return false;
        if (!CheckOne5D(lut, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne5D(lut, 0x8080, 0x8080, 0x8080, 0x8080, 0x1234)) return false;
        if (!CheckOne5D(lut, 0x0000, 0xfe00, 0x80ff, 0x8888, 0x8078)) return false;
        if (!CheckOne5D(lut, 0x1111, 0x2222, 0x3333, 0x4444, 0x1455)) return false;
        if (!CheckOne5D(lut, 0x0000, 0x0012, 0x0013, 0x0014, 0x2333)) return false;
        if (!CheckOne5D(lut, 0x3141, 0x1415, 0x1592, 0x9261, 0x4567)) return false;
        if (!CheckOne5D(lut, 0xff00, 0xff01, 0xff12, 0xff13, 0xf344)) return false;

        cmsPipelineFree(lut);

        return true;
    }

    public static bool Check6DinterpGranular()
    {
        Span<uint> Dimensions = stackalloc uint[] { 4, 3, 2, 2, 2, 2 };

        var lut = cmsPipelineAlloc(DbgThread(), 6, 3);
        var mpe = cmsStageAllocCLut16bitGranular(DbgThread(), Dimensions, 6, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler6D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne6D(lut, 0, 0, 0, 0, 0, 0)) return false;
        if (!CheckOne6D(lut, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne6D(lut, 0x8080, 0x8080, 0x8080, 0x8080, 0x1234, 0x1122)) return false;
        if (!CheckOne6D(lut, 0x0000, 0xfe00, 0x80ff, 0x8888, 0x8078, 0x2233)) return false;
        if (!CheckOne6D(lut, 0x1111, 0x2222, 0x3333, 0x4444, 0x1455, 0x3344)) return false;
        if (!CheckOne6D(lut, 0x0000, 0x0012, 0x0013, 0x0014, 0x2333, 0x4455)) return false;
        if (!CheckOne6D(lut, 0x3141, 0x1415, 0x1592, 0x9261, 0x4567, 0x5566)) return false;
        if (!CheckOne6D(lut, 0xff00, 0xff01, 0xff12, 0xff13, 0xf344, 0x6677)) return false;

        cmsPipelineFree(lut);

        return true;
    }

    public static bool Check7DinterpGranular()
    {
        Span<uint> Dimensions = stackalloc uint[] { 4, 3, 3, 2, 2, 2, 2 };

        var lut = cmsPipelineAlloc(DbgThread(), 7, 3);
        var mpe = cmsStageAllocCLut16bitGranular(DbgThread(), Dimensions, 7, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler7D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne7D(lut, 0, 0, 0, 0, 0, 0, 0)) return false;
        if (!CheckOne7D(lut, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne7D(lut, 0x8080, 0x8080, 0x8080, 0x8080, 0x1234, 0x1122, 0x0056)) return false;
        if (!CheckOne7D(lut, 0x0000, 0xfe00, 0x80ff, 0x8888, 0x8078, 0x2233, 0x0088)) return false;
        if (!CheckOne7D(lut, 0x1111, 0x2222, 0x3333, 0x4444, 0x1455, 0x3344, 0x1987)) return false;
        if (!CheckOne7D(lut, 0x0000, 0x0012, 0x0013, 0x0014, 0x2333, 0x4455, 0x9988)) return false;
        if (!CheckOne7D(lut, 0x3141, 0x1415, 0x1592, 0x9261, 0x4567, 0x5566, 0xfe56)) return false;
        if (!CheckOne7D(lut, 0xff00, 0xff01, 0xff12, 0xff13, 0xf344, 0x6677, 0xbabe)) return false;

        cmsPipelineFree(lut);

        return true;
    }

    public static bool Check8DinterpGranular()
    {
        Span<uint> Dimensions = stackalloc uint[] { 4, 3, 3, 2, 2, 2, 2, 2 };

        var lut = cmsPipelineAlloc(DbgThread(), 8, 3);
        var mpe = cmsStageAllocCLut16bitGranular(DbgThread(), Dimensions, 8, 3, null);
        cmsStageSampleCLut16bit(mpe, Sampler8D, null, SamplerFlag.None);
        cmsPipelineInsertStage(lut, StageLoc.AtBegin, mpe);

        // Check accuracy

        if (!CheckOne8D(lut, 0, 0, 0, 0, 0, 0, 0, 0)) return false;
        if (!CheckOne8D(lut, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff, 0xffff)) return false;

        if (!CheckOne8D(lut, 0x8080, 0x8080, 0x8080, 0x8080, 0x1234, 0x1122, 0x0056, 0x0011)) return false;
        if (!CheckOne8D(lut, 0x0000, 0xfe00, 0x80ff, 0x8888, 0x8078, 0x2233, 0x0088, 0x2020)) return false;
        if (!CheckOne8D(lut, 0x1111, 0x2222, 0x3333, 0x4444, 0x1455, 0x3344, 0x1987, 0x4532)) return false;
        if (!CheckOne8D(lut, 0x0000, 0x0012, 0x0013, 0x0014, 0x2333, 0x4455, 0x9988, 0x1200)) return false;
        if (!CheckOne8D(lut, 0x3141, 0x1415, 0x1592, 0x9261, 0x4567, 0x5566, 0xfe56, 0x6666)) return false;
        if (!CheckOne8D(lut, 0xff00, 0xff01, 0xff12, 0xff13, 0xf344, 0x6677, 0xbabe, 0xface)) return false;

        cmsPipelineFree(lut);

        return true;
    }
}
