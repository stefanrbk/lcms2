using LanguageExt;

using lcms2.types;

namespace lcms2;

public static class WhitePoint
{
    public static Option<CIExyY> FromTemp(double TempK)
    {
        double x;

        var T = TempK;
        var T2 = T * T;  // Square
        var T3 = T2 * T; // Cube

        // For correlated color temperature (T) between 4000K and 7000K:

        if (T is >= 4000 and <= 7000)
        {
            x = (-4.6070 * (1E9 / T3)) + (2.9678 * (1E6 / T2)) + (0.09911 * (1E3 / T)) + 0.244063;
        }
        else
            // or for correlated color temperature (T) between 7000K and 25000K:

        if (T is > 7000.0 and <= 25000.0)
        {
            x = (-2.0064 * (1E9 / T3)) + (1.9018 * (1E6 / T2)) + (0.24748 * (1E3 / T)) + 0.237040;
        }
        else
        {
            LogError(null, ErrorCodes.Range, "cmsWhitePointFromTemp: invalid temp");
            return Option<CIExyY>.None;
        }

        // Obtain y(x)
        var y = (-3.000 * (x * x)) + (2.870 * x) - 0.275;

        // wave factors (not used, but here for futures extensions)

        // M1 = (-1.3515 - 1.7703*x + 5.9114 *y)/(0.0241 + 0.2562*x - 0.7341*y);
        // M2 = (0.0300 - 31.4424*x + 30.0717*y)/(0.0241 + 0.2562*x - 0.7341*y);

        return Option<CIExyY>.Some(new(x: x,
                                       y: y,
                                       Y: 1.0));
    }

    private struct ISOTEMPERATURE
    {
        public double mirek;
        public double ut;
        public double vt;
        public double tt;
    }

    private static readonly ISOTEMPERATURE[] isotempdata =
    [
        new() { mirek = 0,     ut = 0.18006,  vt = 0.26352,  tt = -0.24341 },
        new() { mirek = 10,    ut = 0.18066,  vt = 0.26589,  tt = -0.25479 },
        new() { mirek = 20,    ut = 0.18133,  vt = 0.26846,  tt = -0.26876 },
        new() { mirek = 30,    ut = 0.18208,  vt = 0.27119,  tt = -0.28539 },
        new() { mirek = 40,    ut = 0.18293,  vt = 0.27407,  tt = -0.30470 },
        new() { mirek = 50,    ut = 0.18388,  vt = 0.27709,  tt = -0.32675 },
        new() { mirek = 60,    ut = 0.18494,  vt = 0.28021,  tt = -0.35156 },
        new() { mirek = 70,    ut = 0.18611,  vt = 0.28342,  tt = -0.37915 },
        new() { mirek = 80,    ut = 0.18740,  vt = 0.28668,  tt = -0.40955 },
        new() { mirek = 90,    ut = 0.18880,  vt = 0.28997,  tt = -0.44278 },
        new() { mirek = 100,   ut = 0.19032,  vt = 0.29326,  tt = -0.47888 },
        new() { mirek = 125,   ut = 0.19462,  vt = 0.30141,  tt = -0.58204 },
        new() { mirek = 150,   ut = 0.19962,  vt = 0.30921,  tt = -0.70471 },
        new() { mirek = 175,   ut = 0.20525,  vt = 0.31647,  tt = -0.84901 },
        new() { mirek = 200,   ut = 0.21142,  vt = 0.32312,  tt = -1.0182 },
        new() { mirek = 225,   ut = 0.21807,  vt = 0.32909,  tt = -1.2168 },
        new() { mirek = 250,   ut = 0.22511,  vt = 0.33439,  tt = -1.4512 },
        new() { mirek = 275,   ut = 0.23247,  vt = 0.33904,  tt = -1.7298 },
        new() { mirek = 300,   ut = 0.24010,  vt = 0.34308,  tt = -2.0637 },
        new() { mirek = 325,   ut = 0.24702,  vt = 0.34655,  tt = -2.4681 },
        new() { mirek = 350,   ut = 0.25591,  vt = 0.34951,  tt = -2.9641 },
        new() { mirek = 375,   ut = 0.26400,  vt = 0.35200,  tt = -3.5814 },
        new() { mirek = 400,   ut = 0.27218,  vt = 0.35407,  tt = -4.3633 },
        new() { mirek = 425,   ut = 0.28039,  vt = 0.35577,  tt = -5.3762 },
        new() { mirek = 450,   ut = 0.28863,  vt = 0.35714,  tt = -6.7262 },
        new() { mirek = 475,   ut = 0.29685,  vt = 0.35823,  tt = -8.5955 },
        new() { mirek = 500,   ut = 0.30505,  vt = 0.35907,  tt = -11.324 },
        new() { mirek = 525,   ut = 0.31320,  vt = 0.35968,  tt = -15.628 },
        new() { mirek = 550,   ut = 0.32129,  vt = 0.36011,  tt = -23.325 },
        new() { mirek = 575,   ut = 0.32931,  vt = 0.36038,  tt = -40.770 },
        new() { mirek = 600,   ut = 0.33724,  vt = 0.36051,  tt = -116.45 },
    ];

    private static uint NISO =>
        (uint)isotempdata.Length;

    public static Option<double> ToTemp(CIExyY WhitePoint)
    {
        if (WhitePoint.IsNaN)
            return Option<double>.None;

        var di = 0.0;
        var mi = 0.0;
        var xs = WhitePoint.x;
        var ys = WhitePoint.y;

        // convert (x,y) to CIE 1960 (u,WhitePoint)

        var us = 2 * xs / (-xs + (6 * ys) + 1.5);
        var vs = 3 * ys / (-xs + (6 * ys) + 1.5);

        for (var j = 0; j < NISO; j++)
        {
            var uj = isotempdata[j].ut;
            var vj = isotempdata[j].vt;
            var tj = isotempdata[j].tt;
            var mj = isotempdata[j].mirek;

            var dj = (vs - vj - (tj * (us - uj))) / Math.Sqrt(1.0 + (tj * tj));

            if ((j != 0) && (di / dj < 0.0))
            {
                // Found a match
                return Option<double>.Some(1000000.0 / (mi + (di / (di - dj) * (mj - mi))));
            }

            di = dj;
            mi = mj;
        }

        // Not found
        return Option<double>.None;
    }
}
