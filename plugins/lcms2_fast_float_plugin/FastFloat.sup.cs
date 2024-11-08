//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright ©️ 1998-2024 Marti Maria Saguer, all rights reserved
//              2022-2024 Stefan Kewatt, all rights reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//---------------------------------------------------------------------------------

using lcms2.pdk;

namespace lcms2.FastFloatPlugin;

public static partial class FastFloat
{
    private static bool Floating_Point_Transforms_Dispatcher(out Transform2Fn TransformFn,
                                                             out object? UserData,
                                                             out FreeUserDataFn? FreeUserData,
                                                             ref Pipeline Lut,
                                                             ref uint InputFormat,
                                                             ref uint OutputFormat,
                                                             ref uint dwFlags)
    {
        TransformFn = null!;
        UserData = null;
        FreeUserData = null;

        // Softproofing & gamut check does not use plugin, both are activated via following flag.
        if ((dwFlags & cmsFLAGS_SOFTPROOFING) is not 0)
            return false;

        // Special flags for reversing are not supported
        if (T_FLAVOR(InputFormat) is not 0 || T_FLAVOR(OutputFormat) is not 0)
            return false;

        // Check consistency for alpha channel copy
        if ((dwFlags & cmsFLAGS_COPY_ALPHA) is not 0)
        {
            if (T_EXTRA(InputFormat) != T_EXTRA(OutputFormat))
                return false;
            if (T_PREMUL(InputFormat) is not 0 || T_PREMUL(OutputFormat) is not 0)
                return false;
        }

        // Try to optimize as a set of curves plus a matrix plus a set of curves
        if (OptimizeMatrixShaper15(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize by joining curves
        if (Optimize8ByJoiningCurves(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to use SSE2 to optimize as a set of curves plus a matrix plus a set of curves
        if (Optimize8MatrixShaperSSE(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize as a set of curves plus a matrix plus a set of curves
        if (Optimize8MatrixShaper(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize by joining curves
        if (OptimizeFloatByJoiningCurves(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize as a set of curves plus a matrix plus a set of curves
        if (OptimizeFloatMatrixShaper(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize using prelinearization plus tetrahedral on 8 bit RGB
        if (Optimize8BitRGBTransform(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize using prelinearization plus tetrahedral on 16 bit RGB
        if (Optimize16BitRGBTransform(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize using prelinearization plus tetrahedral on CLut RGB
        if (OptimizeCLUTRGBTransform(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize using prelinearization plus tetrahedral on CLut CMYK
        if (OptimizeCLUTCMYKTransform(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Try to optimize using prelinearization plus tetrahedral on CLut Lab
        if (OptimizeCLUTLabTransform(
                out TransformFn,
                out UserData,
                out FreeUserData,
                ref Lut,
                ref InputFormat,
                ref OutputFormat,
                ref dwFlags))
            return true;

        // Cannot optimize, use lcms normal process
        return false;
    }

    private static readonly List<PluginBase> PluginList =
    [
        new PluginTransform(
            PluginSignatures.MagicNumber,
            REQUIRED_LCMS_VERSION,
            PluginSignatures.Transform,
            new() { xform = Floating_Point_Transforms_Dispatcher }),
        new PluginFormatters(PluginSignatures.MagicNumber, REQUIRED_LCMS_VERSION, PluginSignatures.Formatters)
        {
            FormattersFactoryIn = Formatter_15Bit_Factory_In, FormattersFactoryOut = Formatter_15Bit_Factory_Out
        }
    ];

    // This is the main plug-in installer.
    // Using a function to retrieve the plug-in entry point allows us to execute initialization data
    public static List<PluginBase> cmsFastFloatExtensions() =>
        PluginList;
}
