using System.Diagnostics;

namespace lcms2;

[DebuggerStepThrough]
internal static class Helpers
{
    internal static double RADIANS(double deg) =>
        deg * M_PI / 180;
}
