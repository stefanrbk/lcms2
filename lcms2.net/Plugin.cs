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

public static partial class Plugin
{
    public const int VX = 0;
    public const int VY = 1;
    public const int VZ = 2;

    public const uint cmsPluginMagicNumber = 0x61637070;              // 'acpp'

    public const uint cmsPluginMemHandlerSig = 0x6D656D48;          // 'memH'
    public const uint cmsPluginInterpolationSig = 0x696E7048;       // 'inpH'
    public const uint cmsPluginParametricCurveSig = 0x70617248;     // 'parH'
    public const uint cmsPluginFormattersSig = 0x66726D48;          // 'frmH
    public const uint cmsPluginTagTypeSig = 0x74797048;             // 'typH'
    public const uint cmsPluginTagSig = 0x74616748;                 // 'tagH'
    public const uint cmsPluginRenderingIntentSig = 0x696E7448;     // 'intH'
    public const uint cmsPluginMultiProcessElementSig = 0x6D706548; // 'mpeH'
    public const uint cmsPluginOptimizationSig = 0x6F707448;        // 'optH'
    public const uint cmsPluginTransformSig = 0x7A666D48;           // 'xfmH'
    public const uint cmsPluginMutexSig = 0x6D747A48;               // 'mtxH'
    public const uint cmsPluginParalellizationSig = 0x70726c48;     // 'prlH'
}
