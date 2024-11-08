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

using System.Text;

using lcms2.types;

namespace lcms2;

public static partial class Lcms2
{
    private static readonly byte[] PSBuffer = new byte[2048];

    private const byte MAXPSCOLS = 60;
    /*
        Implementation
        --------------

      PostScript does use XYZ as its internal PCS. But since PostScript
      interpolation tables are limited to 8 bits, I use Lab as a way to
      improve the accuracy, favoring perceptual results. So, for the creation
      of each CRD, CSA the profiles are converted to Lab via a device
      link between  profile -> Lab or Lab -> profile. The PS code necessary to
      convert Lab <-> XYZ is also included.

      Color Space Arrays (CSA)
      ==================================================================================

      In order to obtain precision, code chooses between three ways to implement
      the device -> XYZ transform. These cases identifies monochrome profiles (often
      implemented as a set of curves), matrix-shaper and Pipeline-based.

      Monochrome
      -----------

      This is implemented as /CIEBasedA CSA. The prelinearization curve is
      placed into /DecodeA section, and matrix equals to D50. Since here is
      no interpolation tables, I do the conversion directly to XYZ

      NOTE: CLUT-based monochrome profiles are NOT supported. So, cmsFLAGS_MATRIXINPUT
      flag is forced on such profiles.

        [ /CIEBasedA
          <<
                /DecodeA { transfer function } bind
                /MatrixA [D50]
                /RangeLMN [ 0.0 cmsD50X 0.0 cmsD50Y 0.0 cmsD50Z ]
                /WhitePoint [D50]
                /BlackPoint [BP]
                /RenderingIntent (intent)
          >>
        ]

       On simpler profiles, the PCS is already XYZ, so no conversion is required.

       Matrix-shaper based
       -------------------

       This is implemented both with /CIEBasedABC or /CIEBasedDEF depending on the
       profile implementation. Since here there are no interpolation tables, I do
       the conversion directly to XYZ

        [ /CIEBasedABC
                <<
                    /DecodeABC [ {transfer1} {transfer2} {transfer3} ]
                    /MatrixABC [Matrix]
                    /RangeLMN [ 0.0 cmsD50X 0.0 cmsD50Y 0.0 cmsD50Z ]
                    /DecodeLMN [ { / 2} dup dup ]
                    /WhitePoint [D50]
                    /BlackPoint [BP]
                    /RenderingIntent (intent)
                >>
        ]

        CLUT based
        ----------

         Lab is used in such cases.

        [ /CIEBasedDEF
                <<
                /DecodeDEF [ <prelinearization> ]
                /Table [ p p p [<...>]]
                /RangeABC [ 0 1 0 1 0 1]
                /DecodeABC[ <postlinearization> ]
                /RangeLMN [ -0.236 1.254 0 1 -0.635 1.640 ]
                   % -128/500 1+127/500 0 1  -127/200 1+128/200
                /MatrixABC [ 1 1 1 1 0 0 0 0 -1]
                /WhitePoint [D50]
                /BlackPoint [BP]
                /RenderingIntent (intent)
        ]

      Color Rendering Dictionaries (CRD)
      ==================================
      These are always implemented as CLUT, and always are using Lab. Since CRD are expected to
      be used as resources, the code adds the definition as well.

      <<
        /ColorRenderingType 1
        /WhitePoint [ D50 ]
        /BlackPoint [BP]
        /MatrixPQR [ Bradford ]
        /RangePQR [-0.125 1.375 -0.125 1.375 -0.125 1.375 ]
        /TransformPQR [
        {4 index 3 get div 2 index 3 get mul exch pop exch pop exch pop exch pop } bind
        {4 index 4 get div 2 index 4 get mul exch pop exch pop exch pop exch pop } bind
        {4 index 5 get div 2 index 5 get mul exch pop exch pop exch pop exch pop } bind
        ]
        /MatrixABC <...>
        /EncodeABC <...>
        /RangeABC  <.. used for  XYZ -> Lab>
        /EncodeLMN
        /RenderTable [ p p p [<...>]]

        /RenderingIntent (Perceptual)
      >>
      /Current exch /ColorRendering defineresource pop

      The following stages are used to convert from XYZ to Lab
      --------------------------------------------------------

      Input is given at LMN stage on X, Y, Z

      Encode LMN gives us f(X/Xn), f(Y/Yn), f(Z/Zn)

      /EncodeLMN [

        { 0.964200  div dup 0.008856 le {7.787 mul 16 116 div add}{1 3 div exp} ifelse } bind
        { 1.000000  div dup 0.008856 le {7.787 mul 16 116 div add}{1 3 div exp} ifelse } bind
        { 0.824900  div dup 0.008856 le {7.787 mul 16 116 div add}{1 3 div exp} ifelse } bind

        ]

      MatrixABC is used to compute f(Y/Yn), f(X/Xn) - f(Y/Yn), f(Y/Yn) - f(Z/Zn)

      | 0  1  0|
      | 1 -1  0|
      | 0  1 -1|

      /MatrixABC [ 0 1 0 1 -1 1 0 0 -1 ]

     EncodeABC finally gives Lab values.

      /EncodeABC [
        { 116 mul  16 sub 100 div  } bind
        { 500 mul 128 add 255 div  } bind
        { 200 mul 128 add 255 div  } bind
        ]

      The following stages are used to convert Lab to XYZ
      ----------------------------------------------------

        /RangeABC [ 0 1 0 1 0 1]
        /DecodeABC [ { 100 mul 16 add 116 div } bind
                     { 255 mul 128 sub 500 div } bind
                     { 255 mul 128 sub 200 div } bind
                   ]

        /MatrixABC [ 1 1 1 1 0 0 0 0 -1]
        /DecodeLMN [
                    {dup 6 29 div ge {dup dup mul mul} {4 29 div sub 108 841 div mul} ifelse 0.964200 mul} bind
                    {dup 6 29 div ge {dup dup mul mul} {4 29 div sub 108 841 div mul} ifelse } bind
                    {dup 6 29 div ge {dup dup mul mul} {4 29 div sub 108 841 div mul} ifelse 0.824900 mul} bind
                    ]

    */

    /*

     PostScript algorithms discussion.
     =========================================================================================================

      1D interpolation algorithm

      1D interpolation (float)
      ------------------------

        val2 = Domain * Value;

        cell0 = (int) floor(val2);
        cell1 = (int) ceil(val2);

        rest = val2 - cell0;

        y0 = LutTable[cell0] ;
        y1 = LutTable[cell1] ;

        y = y0 + (y1 - y0) * rest;

      PostScript code                   Stack
      ================================================

      {                                 % v
        <check 0..1.0>
        [array]                         % v tab
        dup                             % v tab tab
        length 1 sub                    % v tab dom

        3 -1 roll                       % tab dom v

        mul                             % tab val2
        dup                             % tab val2 val2
        dup                             % tab val2 val2 val2
        floor cvi                       % tab val2 val2 cell0
        exch                            % tab val2 cell0 val2
        ceiling cvi                     % tab val2 cell0 cell1

        3 index                         % tab val2 cell0 cell1 tab
        exch                            % tab val2 cell0 tab cell1
        get                             % tab val2 cell0 y1

        4 -1 roll                       % val2 cell0 y1 tab
        3 -1 roll                       % val2 y1 tab cell0
        get                             % val2 y1 y0

        dup                             % val2 y1 y0 y0
        3 1 roll                        % val2 y0 y1 y0

        sub                             % val2 y0 (y1-y0)
        3 -1 roll                       % y0 (y1-y0) val2
        dup                             % y0 (y1-y0) val2 val2
        floor cvi                       % y0 (y1-y0) val2 floor(val2)
        sub                             % y0 (y1-y0) rest
        mul                             % y0 t1
        add                             % y
        65535 div                       % result
      } bind

    */

    private class PsSamplerCargo
    {
        public StageCLutData<ushort> Pipeline;
        public IOHandler m;

        public int FirstComponent;
        public int SecondComponent;

        private readonly byte[] preMaj;
        private readonly byte[] postMaj;
        private readonly byte[] preMin;
        private readonly byte[] postMin;

        public Span<byte> PreMaj =>
            preMaj.AsSpan(..Array.IndexOf<byte>(preMaj, 0));

        public Span<byte> PostMaj =>
            preMaj.AsSpan(..Array.IndexOf<byte>(postMaj, 0));

        public Span<byte> PreMin =>
            preMaj.AsSpan(..Array.IndexOf<byte>(preMin, 0));

        public Span<byte> PostMin =>
            preMaj.AsSpan(..Array.IndexOf<byte>(postMin, 0));

        public bool FixWhite;        // Force mapping of pure white

        public Signature ColorSpace;    // ColorSpace of profile

        public PsSamplerCargo(IOHandler m,
                              StageCLutData<ushort> pipeline,
                              int firstComponent,
                              int secondComponent,
                              ReadOnlySpan<byte> preMaj,
                              ReadOnlySpan<byte> postMaj,
                              ReadOnlySpan<byte> preMin,
                              ReadOnlySpan<byte> postMin,
                              bool fixWhite,
                              Signature colorSpace)
        {
            Pipeline = pipeline;
            this.m = m;
            FirstComponent = firstComponent;
            SecondComponent = secondComponent;
            FixWhite = fixWhite;
            ColorSpace = colorSpace;

            //var pool = Context.GetPool<byte>(m.ContextID);
            //this.preMaj = pool.Rent(preMaj.Length);
            //this.postMaj = pool.Rent(postMaj.Length);
            //this.preMin = pool.Rent(preMin.Length);
            //this.postMin = pool.Rent(postMin.Length);
            this.preMaj = new byte[preMaj.Length];
            this.postMaj = new byte[postMaj.Length];
            this.preMin = new byte[preMin.Length];
            this.postMin = new byte[postMin.Length];

            preMaj.CopyTo(this.preMaj);
            postMaj.CopyTo(this.postMaj);
            preMin.CopyTo(this.preMin);
            postMin.CopyTo(this.postMin);
        }
    }

    private static int _cmsPSActualColumn = 0;

    private static byte Word2Byte(ushort w) =>
        (byte)Math.Floor((w / 257.0) + 0.5);

    private static void WriteByte(IOHandler m, byte b)
    {
        m.PrintF("{0:x2}", b);
        _cmsPSActualColumn += 2;

        if (_cmsPSActualColumn > MAXPSCOLS)
        {
            m.PrintF("\n");
            _cmsPSActualColumn = 0;
        }
    }

    private static Span<byte> RemoveCR(ReadOnlySpan<byte> txt)
    {
        //strncpy(PSBuffer, txt, 2047);
        //PSBuffer[2047] = 0;
        if (txt.Length > 2048)
            txt[..2048].CopyTo(PSBuffer);
        else
            txt.CopyTo(PSBuffer.AsSpan()[..txt.Length]);
        for (var pt = 0; pt < txt.Length && pt < 2048; pt++)
        {
            if (PSBuffer[pt] is (byte)'\n' or (byte)'\r')
                PSBuffer[pt] = (byte)' ';
        }

        return PSBuffer;
    }

    private static string ctime(DateTime timer) =>
        timer.ToString("ddd MMM dd HH:mm:ss yyyy") + '\n';

    private static void EmitHeader(IOHandler m, ReadOnlySpan<byte> Title, Profile Profile)
    {
        Span<byte> DescASCII = stackalloc byte[256];
        Span<byte> CopyrightASCII = stackalloc byte[256];

        var timer = DateTime.UtcNow;

        var Description = (Mlu?)cmsReadTag(Profile, Signatures.Tag.ProfileDescription);
        var Copyright = (Mlu?)cmsReadTag(Profile, Signatures.Tag.Copyright);

        DescASCII[0] = DescASCII[255] = 0;
        CopyrightASCII[0] = CopyrightASCII[255] = 0;

        if (Description is not null)
            cmsMLUgetASCII(Description, cmsNoLanguage, cmsNoCountry, DescASCII);
        if (Copyright is not null)
            cmsMLUgetASCII(Copyright, cmsNoLanguage, cmsNoCountry, CopyrightASCII);

        m.PrintF("%!PS-Adobe-3.0\n");
        m.PrintF("%\n");
        m.PrintF("% {0}\n", SpanToString(Title));
        m.PrintF("% Source: {0}\n", Encoding.ASCII.GetString(RemoveCR(DescASCII)));
        m.PrintF("%         {0}\n", Encoding.ASCII.GetString(RemoveCR(CopyrightASCII)));
        m.PrintF("% Created: {0}", ctime(timer));    // ctime appends a \n!!!
        m.PrintF("%\n");
        m.PrintF("%%BeginResource\n");
    }

    private static void EmitWhiteBlackD50(IOHandler m, CIEXYZ BlackPoint)
    {
        m.PrintF("/BlackPoint [{0} {1} {2}]\n", BlackPoint.X, BlackPoint.Y, BlackPoint.Z);
        m.PrintF("/WhitePoint [{0} {1} {2}]\n", CIEXYZ.D50.X, CIEXYZ.D50.Y, CIEXYZ.D50.Z);
    }

    private static void EmitRangeCheck(IOHandler m) =>
        m.PrintF("dup 0.0 lt {{ pop 0.0 }} if dup 1.0 gt {{ pop 1.0 }} if ");

    private static void EmitIntent(IOHandler m, uint RenderingIntent)
    {
        var intent = RenderingIntent switch
                     {
                         INTENT_PERCEPTUAL            => "Perceptual",
                         INTENT_RELATIVE_COLORIMETRIC => "RelativeColorimetric",
                         INTENT_ABSOLUTE_COLORIMETRIC => "AbsoluteColorimetric",
                         INTENT_SATURATION            => "Saturation",
                         _                            => "Undefined"
                     };
        m.PrintF("/RenderingIntent ({0})\n", intent);
    }

    private static void EmitLab2XYZ(IOHandler m)
    {
        m.PrintF("/RangeABC [ 0 1 0 1 0 1]\n");
        m.PrintF("/DecodeABC [\n");
        m.PrintF("{{100 mul  16 add 116 div }} bind\n");
        m.PrintF("{{255 mul 128 sub 500 div }} bind\n");
        m.PrintF("{{255 mul 128 sub 200 div }} bind\n");
        m.PrintF("]\n");
        m.PrintF("/MatrixABC [ 1 1 1 1 0 0 0 0 -1]\n");
        m.PrintF("/RangeLMN [ -0.236 1.254 0 1 -0.635 1.640 ]\n");
        m.PrintF("/DecodeLMN [\n");
        m.PrintF("{{dup 6 29 div ge {{dup dup mul mul}} {{4 29 div sub 108 841 div mul}} ifelse 0.964200 mul}} bind\n");
        m.PrintF("{{dup 6 29 div ge {{dup dup mul mul}} {{4 29 div sub 108 841 div mul}} ifelse }} bind\n");
        m.PrintF("{{dup 6 29 div ge {{dup dup mul mul}} {{4 29 div sub 108 841 div mul}} ifelse 0.824900 mul}} bind\n");
        m.PrintF("]\n");
    }

    private static void Emit1Gamma(IOHandler m, ToneCurve Table)
    {
        /**
         * On error, empty tables or linear assume gamma 1.0
         */

        if (Table is null ||
            Table.nEntries <= 0 ||
            cmsIsToneCurveLinear(Table))
        {
            m.PrintF("{{ 1 }} bind ");
            return;
        }

        // Check if is really an exponential. If so, emit "exp"
        var gamma = cmsEstimateGamma(Table, 0.001);
        if (gamma > 0)
        {
            m.PrintF("{{ {0:g} exp }} bind ", gamma);
            return;
        }

        m.PrintF("{{ ");

        // Bounds check
        EmitRangeCheck(m);

        // Emit interpolation code

        // PostScript code                            Stack
        // ===============                            ========================
        // v

        m.PrintF(" [");

        for (var i = 0; i < Table.nEntries; i++)
        {
            if (i % 10 is 0)
                m.PrintF("\n  ");
            m.PrintF("{0:d} ", Table.Table16[i]);
        }

        m.PrintF("] ");                        // v tab

        m.PrintF("dup ");          // v tab tab
        m.PrintF("length 1 sub "); // v tab dom
        m.PrintF("3 -1 roll ");    // tab dom v
        m.PrintF("mul ");          // tab val2
        m.PrintF("dup ");          // tab val2 val2
        m.PrintF("dup ");          // tab val2 val2 val2
        m.PrintF("floor cvi ");    // tab val2 val2 cell0
        m.PrintF("exch ");         // tab val2 cell0 val2
        m.PrintF("ceiling cvi ");  // tab val2 cell0 cell1
        m.PrintF("3 index ");      // tab val2 cell0 cell1 tab
        m.PrintF("exch ");         // tab val2 cell0 tab cell1
        m.PrintF("get\n  ");       // tab val2 cell0 y1
        m.PrintF("4 -1 roll ");    // val2 cell0 y1 tab
        m.PrintF("3 -1 roll ");    // val2 y1 tab cell0
        m.PrintF("get ");          // val2 y1 y0
        m.PrintF("dup ");          // val2 y1 y0 y0
        m.PrintF("3 1 roll ");     // val2 y0 y1 y0
        m.PrintF("sub ");          // val2 y0 (y1-y0)
        m.PrintF("3 -1 roll ");    // y0 (y1-y0) val2
        m.PrintF("dup ");          // y0 (y1-y0) val2 val2
        m.PrintF("floor cvi ");    // y0 (y1-y0) val2 floor(val2)
        m.PrintF("sub ");          // y0 (y1-y0) rest
        m.PrintF("mul ");          // y0 t1
        m.PrintF("add ");          // y
        m.PrintF("65535 div\n");   // result

        m.PrintF("}} bind ");
    }

    private static bool GammaTableEquals(ReadOnlySpan<ushort> g1, ReadOnlySpan<ushort> g2, uint nG1, uint nG2) =>
        nG1 == nG2 && memcmp(g1, g2) == 0;

    private static void EmitNGamma(IOHandler m, uint n, ReadOnlySpan<ToneCurve> g)
    {
        for (var i = 0; i < n; i++)
        {
            if (g[i] is null)
                return;   // Error

            if (i > 0 && GammaTableEquals(g[i - 1].Table16, g[i].Table16, g[i - 1].nEntries, g[i].nEntries))
            {
                m.PrintF("def ");
            }
            else
            {
                Emit1Gamma(m, g[i]);
            }
        }
    }

    private static bool OutputValueSampler(ReadOnlySpan<ushort> In, Span<ushort> Out, object? Cargo)
    {
        if (Cargo is not PsSamplerCargo sc)
            return false;

        if (sc.FixWhite)
        {
            if (In[0] == 0xffff)    // Only in L* = 100, ab = [-8..8]
            {
                if ((In[1] is >= 0x7800 and <= 0x8800) &&
                    (In[2] is >= 0x7800 and <= 0x8800))
                {
                    if (!_cmsEndPointsBySpace(sc.ColorSpace, out var White, out _, out var nOutputs))
                        return false;

                    for (var i = 0; i < nOutputs; i++)
                        Out[i] = White[i];
                }
            }
        }

        // Handle the parenthesis on rows

        if (In[0] != sc.FirstComponent)
        {
            if (sc.FirstComponent != -1)
            {
                sc.m.PrintF(sc.PostMin);
                sc.SecondComponent = -1;
                sc.m.PrintF(sc.PostMaj);
            }

            // Begin block
            _cmsPSActualColumn = 0;

            sc.m.PrintF(sc.PreMaj);
            sc.FirstComponent = In[0];
        }

        if (In[1] != sc.SecondComponent)
        {
            if (sc.SecondComponent != -1)
            {
                sc.m.PrintF(sc.PostMin);
            }

            sc.m.PrintF(sc.PreMin);
            sc.SecondComponent = In[1];
        }

        // Dump table.

        for (var i = 0; i < sc.Pipeline.Params.nOutputs; i++)
        {
            var wWordOut = Out[i];

            // We always deal with Lab4;

            var wByteOut = Word2Byte(wWordOut);
            WriteByte(sc.m, wByteOut);
        }

        return true;
    }

    private static void WriteCLUT(IOHandler m,
                                  Stage mpe,
                                  ReadOnlySpan<byte> PreMaj,
                                  ReadOnlySpan<byte> PostMaj,
                                  ReadOnlySpan<byte> PreMin,
                                  ReadOnlySpan<byte> PostMin,
                                  bool FixWhite,
                                  Signature ColorSpace)
    {
        if (mpe.Data is not StageCLutData<ushort> clut || clut.Params is null)
            return;

        var sc = new PsSamplerCargo(m, clut, -1, -1, PreMaj, PostMaj, PreMin, PostMin, FixWhite, ColorSpace);

        m.PrintF("[");

        for (var i = 0u; i < sc.Pipeline.Params.nInputs; i++)
        {
            if (i < Context.MaxInputDimensions)
                m.PrintF(" {0:d} ", sc.Pipeline.Params.nSamples[i]);
        }

        m.PrintF(" [\n");

        cmsStageSampleCLut16bit(mpe, OutputValueSampler, sc, SamplerFlag.Inspect);

        m.PrintF(PostMin);
        m.PrintF(PostMaj);
        m.PrintF("] ");
    }

    private static bool EmitCIEBasedA(IOHandler m, ToneCurve Curve, CIEXYZ BlackPoint)
    {
        m.PrintF("[ /CIEBasedA\n");
        m.PrintF("  <<\n");

        m.PrintF("/DecodeA ");

        Emit1Gamma(m, Curve);

        m.PrintF(" \n");

        m.PrintF("/MatrixA [ 0.9642 1.0000 0.8249 ]\n");
        m.PrintF("/RangeLMN [ 0.0 0.9642 0.0 1.0000 0.0 0.8249 ]\n");

        EmitWhiteBlackD50(m, BlackPoint);
        EmitIntent(m, INTENT_PERCEPTUAL);

        m.PrintF(">>\n");
        m.PrintF("]\n");

        return true;
    }

    private static bool EmitCIEBasedABC(IOHandler m,
                                        ReadOnlySpan<double> Matrix,
                                        ReadOnlySpan<ToneCurve> CurveSet,
                                        CIEXYZ BlackPoint)
    {
        m.PrintF("[ /CIEBasedABC\n");
        m.PrintF("<<\n");
        m.PrintF("/DecodeABC [ ");

        EmitNGamma(m, 3, CurveSet);

        m.PrintF("]\n");

        m.PrintF("/MatrixABC [ ");

        for (var i = 0; i < 3; i++)
        {
            m.PrintF(
                "{0:f6} {1:f6} {2:f6} ",
                Matrix[i + 3 * 0],
                Matrix[i + 3 * 1],
                Matrix[i + 3 * 2]);
        }

        m.PrintF("]\n");

        m.PrintF("/RangeLMN [ 0.0 0.9642 0.0 1.0000 0.0 0.8249 ]\n");

        EmitWhiteBlackD50(m, BlackPoint);
        EmitIntent(m, INTENT_PERCEPTUAL);

        m.PrintF(">>\n");
        m.PrintF("]\n");

        return true;
    }

    private static bool EmitCIEBasedDEF(IOHandler m, Pipeline Pipeline, uint Intent, CIEXYZ BlackPoint)
    {
        ReadOnlySpan<byte> PreMaj;
        ReadOnlySpan<byte> PostMaj;
        ReadOnlySpan<byte> PreMin, PostMin;
        Span<byte> buffer = stackalloc byte[2048];

        var mpe = Pipeline.Elements;

        switch (cmsStageInputChannels(mpe))
        {
            case 3:
                m.PrintF("[ /CIEBasedDEF\n");
                PreMaj = "<"u8;
                PostMaj = ">\n"u8;
                PreMin = PostMin = ""u8;
                break;

            case 4:
                m.PrintF("[ /CIEBasedDEFG\n");
                PreMaj = "["u8;
                PostMaj = "]\n"u8;
                PreMin = "<"u8;
                PostMin = ">\n"u8;
                break;

            default:
                return false;
        }

        m.PrintF("<<\n");

        if (cmsStageType(mpe) == Signatures.Stage.CurveSetElem)
        {
            m.PrintF("/DecodeDEF [ ");
            EmitNGamma(m, cmsStageOutputChannels(mpe), _cmsStageGetPtrToCurveSet(mpe));
            m.PrintF("]\n");

            mpe = mpe.Next;
        }

        if (cmsStageType(mpe) == Signatures.Stage.CLutElem)
        {
            m.PrintF("/Table ");
            WriteCLUT(m, mpe, PreMaj, PostMaj, PreMin, PostMin, false, default);
            m.PrintF("]\n");
        }

        EmitLab2XYZ(m);
        EmitWhiteBlackD50(m, BlackPoint);
        EmitIntent(m, Intent);

        m.PrintF("   >>\n");
        m.PrintF("]\n");

        return true;
    }

    private static ToneCurve? ExtractGray2Y(Context? ContextID, Profile Profile, uint Intent)
    {
        var Out = cmsBuildTabulatedToneCurve16(ContextID, 256, null);
        var hXYZ = cmsCreateXYZProfile();
        var xform = cmsCreateTransformTHR(
            ContextID,
            Profile,
            TYPE_GRAY_8,
            hXYZ,
            TYPE_XYZ_DBL,
            Intent,
            cmsFLAGS_NOOPTIMIZE);

        Span<byte> Gray = stackalloc byte[1];
        Span<CIEXYZ> XYZ = stackalloc CIEXYZ[1];

        if (Out is not null && xform is not null)
        {
            for (var i = 0; i < 256; i++)
            {
                Gray[0] = (byte)i;

                cmsDoTransform(xform, Gray, XYZ, 1);

                Out.Table16[i] = QuickSaturateWord(XYZ[0].Y * 65535.0);
            }
        }

        if (xform is not null)
            cmsDeleteTransform(xform);
        if (hXYZ is not null)
            cmsCloseProfile(hXYZ);
        return Out;
    }

    private static bool WriteInputLUT(IOHandler m, Profile Profile, uint Intent, uint dwFlags)
    {
        CIEXYZ BlackPointAdaptedToD50;
        var Profiles = new Profile[2];

        // Does create a device-link based transform.
        // The DeviceLink is next dumped as working CSA.

        var InputFormat = cmsFormatterForColorspaceOfProfile(Profile, 2, false);
        var nChannels = T_CHANNELS(InputFormat);

        BlackPointAdaptedToD50 = cmsDetectBlackPoint(Profile, Intent);

        // Adjust output to Lab4
        var hLab = cmsCreateLab4ProfileTHR(m.ContextID, null);

        Profiles[0] = Profile;
        Profiles[1] = hLab;

        var xform = cmsCreateMultiprofileTransform(Profiles, 2, InputFormat, TYPE_Lab_DBL, Intent, 0);
        cmsCloseProfile(hLab);

        if (xform is null)
        {
            Context.LogError(m.ContextID, cmsERROR_COLORSPACE_CHECK, "Cannot create transform Profile -> Lab");
            return false;
        }

        // Only 1, 3, and 4 channels are allowed

        switch (nChannels)
        {
            case 1:
            {
                var Gray2Y = ExtractGray2Y(m.ContextID, Profile, Intent);
                EmitCIEBasedA(m, Gray2Y, BlackPointAdaptedToD50);
                cmsFreeToneCurve(Gray2Y);
            }
                break;

            case 3:
            case 4:
            {
                var OutFrm = TYPE_Lab_16;
                var v = xform;

                var DeviceLink = cmsPipelineDup(v.Lut);
                if (DeviceLink is null)
                {
                    cmsDeleteTransform(xform);
                    return false;
                }

                dwFlags |= cmsFLAGS_FORCE_CLUT;
                _cmsOptimizePipeline(m.ContextID, ref DeviceLink, Intent, ref InputFormat, ref OutFrm, ref dwFlags);

                var rc = EmitCIEBasedDEF(m, DeviceLink, Intent, BlackPointAdaptedToD50);
                cmsPipelineFree(DeviceLink);
                if (!rc)
                {
                    cmsDeleteTransform(xform);
                    return false;
                }
            }
                break;

            default:
                cmsDeleteTransform(xform);
                Context.LogError(
                    m.ContextID,
                    cmsERROR_COLORSPACE_CHECK,
                    "Only 3, 4 channels are supported for CSA. This profile has {0} channels.",
                    nChannels);
                return false;
        }

        cmsDeleteTransform(xform);

        return true;
    }

    private static double[]? GetPtrToMatrix(Stage mpe) =>
        (mpe.Data is StageMatrixData Data)
            ? Data.Double
            : null;

    private static bool WriteInputMatrixShaper(IOHandler m, Profile Profile, Stage Matrix, Stage Shaper)
    {
        bool rc;
        CIEXYZ BlackPointAdaptedToD50;

        var ColorSpace = cmsGetColorSpace(Profile);

        BlackPointAdaptedToD50 = cmsDetectBlackPoint(Profile, INTENT_RELATIVE_COLORIMETRIC);

        if (ColorSpace == Signatures.Colorspace.Gray)
        {
            var ShaperCurve = _cmsStageGetPtrToCurveSet(Shaper);
            rc = EmitCIEBasedA(m, ShaperCurve[0], BlackPointAdaptedToD50);
        }
        else if (ColorSpace == Signatures.Colorspace.Rgb)
        {
            var Mat = GetPtrToMatrix(Matrix)!;

            for (var i = 0; i < 9; i++)
                Mat[i] *= CIEXYZ.MaxEncodeableXYZ;

            rc = EmitCIEBasedABC(m, Mat, _cmsStageGetPtrToCurveSet(Shaper), BlackPointAdaptedToD50);
        }
        else
        {
            Context.LogError(
                m.ContextID,
                cmsERROR_COLORSPACE_CHECK,
                "Profile is not suitable for CSA. Unsupported colorspace.");
            return false;
        }

        return rc;
    }

    private static bool WriteNamedColorCSA(IOHandler m, Profile hNamedColor, uint Intent)
    {
        var hLab = cmsCreateLab4ProfileTHR(m.ContextID, null);
        var xform = cmsCreateTransform(hNamedColor, TYPE_NAMED_COLOR_INDEX, hLab, TYPE_Lab_DBL, Intent, 0);
        cmsCloseProfile(hLab);

        if (xform is null)
            return false;

        var NamedColorList = cmsGetNamedColorList(xform);
        if (NamedColorList is null)
        {
            cmsDeleteTransform(xform);
            return false;
        }

        //var pool = Context.GetPool<byte>(NamedColorList.ContextID);
        //var ColorName = pool.Rent(cmsMAX_PATH);
        var ColorName = new byte[MaxPath];

        m.PrintF("<<\n");
        m.PrintF("(colorlistcomment) (Named color CSA)\n");
        m.PrintF("(Prefix) [ (Pantone ) (PANTONE ) ]\n");
        m.PrintF("(Suffix) [ ( CV) ( CVC) ( C) ]\n");

        var nColors = cmsNamedColorCount(NamedColorList);

        Span<ushort> In = stackalloc ushort[1];
        Span<CIELab> Lab = stackalloc CIELab[1];
        for (var i = 0u; i < nColors; i++)
        {
            In[0] = (ushort)i;

            if (!cmsNamedColorInfo(NamedColorList, i, ColorName, null, null, null, null))
                continue;

            cmsDoTransform(xform, In, Lab, 1);
            m.PrintF(
                "  ({0}) [ {1:f3} {2:f3} {3:f3} ]\n",
                Encoding.ASCII.GetString(TrimBuffer(ColorName)),
                Lab[0].L,
                Lab[0].a,
                Lab[0].b);
        }

        m.PrintF(">>\n");

        cmsDeleteTransform(xform);
        return true;
    }

    private static uint GenerateCSA(Context? ContextID,
                                    Profile Profile,
                                    uint Intent,
                                    uint dwFlags,
                                    IOHandler mem)
    {
        Pipeline? lut = null;

        // Is a named color profile?
        if (cmsGetDeviceClass(Profile) == Signatures.ProfileClass.NamedColor)
        {
            if (!WriteNamedColorCSA(mem, Profile, Intent))
                goto Error;
        }
        else
        {
            // Any profile class are allowed (including devicelink), but
            // output (PCS) colorspace must be XYZ or LAB
            var ColorSpace = cmsGetPCS(Profile);

            if (ColorSpace != Signatures.Colorspace.XYZ &&
                ColorSpace != Signatures.Colorspace.Lab)
            {
                Context.LogError(ContextID, cmsERROR_COLORSPACE_CHECK, "Invalid output color space");
                goto Error;
            }

            // Read the lut with all necessary conversion stages
            lut = _cmsReadInputLUT(Profile, Intent);
            if (lut is null)
                goto Error;

            // TOne curves + matrix can be implemented without and LUT
            if (cmsPipelineCheckAndRetrieveStages(
                    lut,
                    Signatures.Stage.CurveSetElem,
                    out var Shaper,
                    Signatures.Stage.MatrixElem,
                    out var Matrix))
            {
                if (!WriteInputMatrixShaper(mem, Profile, Matrix, Shaper))
                    goto Error;
            }
            else
            {
                // We need a LUT for the rest
                if (!WriteInputLUT(mem, Profile, Intent, dwFlags))
                    goto Error;
            }
        }

        // Done, keep memory usage
        var dwBytesUsed = mem.UsedSpace;

        // Get rid of LUT
        if (lut is not null)
            cmsPipelineFree(lut);

        // Finally, return used byte count
        return dwBytesUsed;

    Error:
        if (lut is not null)
            cmsPipelineFree(lut);
        return 0;
    }

    private static void EmitPQRStage(IOHandler m, Profile Profile, bool DoBPC, bool lIsAbsolute)
    {
        if (lIsAbsolute)
        {
            // For absolute colorimetric intent, encode back to relative
            // and generate a relative Pipeline

            // Relative encoding is obtained across XYZpcs*(D50/WhitePoint)

            _cmsReadMediaWhitePoint(out var White, Profile);

            m.PrintF("/MatrixPQR [1 0 0 0 1 0 0 0 1 ]\n");
            m.PrintF("/RangePQR [ -0.5 2 -0.5 2 -0.5 2 ]\n");

            m.PrintF(
                "% Absolute colorimetric -- encode to relative to maximize LUT usage\n" +
                "/TransformPQR [\n" +
                "{{0.9642 mul {0:g} div exch pop exch pop exch pop exch pop}} bind\n" +
                "{{1.0000 mul {1:g} div exch pop exch pop exch pop exch pop}} bind\n" +
                "{{0.8249 mul {2:g} div exch pop exch pop exch pop exch pop}} bind\n]\n",
                White.Value.X,
                White.Value.Y,
                White.Value.Z);
            return;
        }

        m.PrintF(
            "% Bradford Cone Space\n" +
            "/MatrixPQR [0.8951 -0.7502 0.0389 0.2664 1.7135 -0.0685 -0.1614 0.0367 1.0296 ] \n");

        m.PrintF("/RangePQR [ -0.5 2 -0.5 2 -0.5 2 ]\n");

        // No BPC

        if (!DoBPC)
        {
            m.PrintF(
                "% VonKries-like transform in Bradford Cone Space\n" +
                "/TransformPQR [\n" +
                "{{exch pop exch 3 get mul exch pop exch 3 get div}} bind\n" +
                "{{exch pop exch 4 get mul exch pop exch 4 get div}} bind\n" +
                "{{exch pop exch 5 get mul exch pop exch 5 get div}} bind\n]\n");
        }
        else
        {
            // BPC

            m.PrintF(
                "%% VonKries-like transform in Bradford Cone Space plus BPC\n" +
                "/TransformPQR [\n");

            m.PrintF(
                "{{4 index 3 get div 2 index 3 get mul " +
                "2 index 3 get 2 index 3 get sub mul " +
                "2 index 3 get 4 index 3 get 3 index 3 get sub mul sub " +
                "3 index 3 get 3 index 3 get exch sub div " +
                "exch pop exch pop exch pop exch pop }} bind\n");

            m.PrintF(
                "{{4 index 4 get div 2 index 4 get mul " +
                "2 index 4 get 2 index 4 get sub mul " +
                "2 index 4 get 4 index 4 get 3 index 4 get sub mul sub " +
                "3 index 4 get 3 index 4 get exch sub div " +
                "exch pop exch pop exch pop exch pop }} bind\n");

            m.PrintF(
                "{{4 index 5 get div 2 index 5 get mul " +
                "2 index 5 get 2 index 5 get sub mul " +
                "2 index 5 get 4 index 5 get 3 index 5 get sub mul sub " +
                "3 index 5 get 3 index 5 get exch sub div " +
                "exch pop exch pop exch pop exch pop }} bind\n]\n");
        }
    }

    private static void EmitXYZ2Lab(IOHandler m)
    {
        m.PrintF("/RangeLMN [ -0.635 2.0 0 2 -0.635 2.0 ]\n");
        m.PrintF("/EncodeLMN [\n");
        m.PrintF("{{ 0.964200  div dup 0.008856 le {{7.787 mul 16 116 div add}}{{1 3 div exp}} ifelse }} bind\n");
        m.PrintF("{{ 1.000000  div dup 0.008856 le {{7.787 mul 16 116 div add}}{{1 3 div exp}} ifelse }} bind\n");
        m.PrintF("{{ 0.824900  div dup 0.008856 le {{7.787 mul 16 116 div add}}{{1 3 div exp}} ifelse }} bind\n");
        m.PrintF("]\n");
        m.PrintF("/MatrixABC [ 0 1 0 1 -1 1 0 0 -1 ]\n");
        m.PrintF("/EncodeABC [\n");

        m.PrintF("{{ 116 mul  16 sub 100 div  }} bind\n");
        m.PrintF("{{ 500 mul 128 add 256 div  }} bind\n");
        m.PrintF("{{ 200 mul 128 add 256 div  }} bind\n");

        m.PrintF("]\n");
    }

    private static bool WriteOutputLUT(IOHandler m, Profile Profile, uint Intent, uint dwFlags)
    {
        var Profiles = new Profile[3];
        CIEXYZ BlackPointAdaptedToD50;
        var lDoBPC = (dwFlags & cmsFLAGS_BLACKPOINTCOMPENSATION) is not 0;
        var lFixWhite = (dwFlags & cmsFLAGS_NOWHITEONWHITEFIXUP) is 0;
        var InFrm = TYPE_Lab_16;

        var hLab = cmsCreateLab4ProfileTHR(m.ContextID, null);
        if (hLab is null)
            return false;

        var OutputFormat = cmsFormatterForColorspaceOfProfile(Profile, 2, false);
        var nChannels = T_CHANNELS(OutputFormat);

        var ColorSpace = cmsGetColorSpace(Profile);

        // For absolute colorimetric, the LUT is encoded as relative in order to preserve precision.

        var RelativeEncodingIntent = Intent;
        if (RelativeEncodingIntent is INTENT_ABSOLUTE_COLORIMETRIC)
            RelativeEncodingIntent = INTENT_RELATIVE_COLORIMETRIC;

        // Use V4 Lab always
        Profiles[0] = hLab;
        Profiles[1] = Profile;

        var xform = cmsCreateMultiprofileTransformTHR(
            m.ContextID,
            Profiles,
            2,
            TYPE_Lab_DBL,
            OutputFormat,
            RelativeEncodingIntent,
            0);

        cmsCloseProfile(hLab);

        if (xform is null)
        {
            Context.LogError(
                m.ContextID,
                cmsERROR_COLORSPACE_CHECK,
                "Cannot create transform Lab -> Profile in CRD creation");
            return false;
        }

        // Get a copy of the internal devicelink
        var v = xform;
        var DeviceLink = cmsPipelineDup(v.Lut);
        if (DeviceLink is null)
        {
            cmsDeleteTransform(xform);
            Context.LogError(m.ContextID, cmsERROR_CORRUPTION_DETECTED, "Cannot access link for CRD");
            return false;
        }

        // We need a CLUT
        dwFlags |= cmsFLAGS_FORCE_CLUT;
        if (!_cmsOptimizePipeline(
                m.ContextID,
                ref DeviceLink,
                RelativeEncodingIntent,
                ref InFrm,
                ref OutputFormat,
                ref dwFlags))
        {
            cmsPipelineFree(DeviceLink);
            cmsDeleteTransform(xform);
            Context.LogError(m.ContextID, cmsERROR_CORRUPTION_DETECTED, "Cannot create CLUT table for CRD");
            return false;
        }

        m.PrintF("<<\n");
        m.PrintF("/ColorRenderingType 1\n");

        BlackPointAdaptedToD50 = cmsDetectBlackPoint(Profile, Intent);

        // Emit headers, etc.
        EmitWhiteBlackD50(m, BlackPointAdaptedToD50);
        EmitPQRStage(m, Profile, lDoBPC, Intent is INTENT_ABSOLUTE_COLORIMETRIC);
        EmitXYZ2Lab(m);

        // FIXUP: map Lab (100, 0, 0) to perfect white, because the particular encoding for Lab
        // does map a=b=0 not falling into any specific node. Since range a,b goes -128..127,
        // zero is slightly moved towards right, so assure next node (in L=100 slice) is mapped to
        // zero. This would sacrifice a bit of highlights, but failure to do so would cause
        // scum dot. Ouch.

        if (Intent is INTENT_ABSOLUTE_COLORIMETRIC)
            lFixWhite = false;

        m.PrintF("/RenderTable ");

        var first = cmsPipelineGetPtrToFirstStage(DeviceLink);
        if (first is not null)
        {
            WriteCLUT(m, first, "<"u8, ">\n"u8, ""u8, ""u8, lFixWhite, ColorSpace);
        }

        WriteCLUT(
            m,
            cmsPipelineGetPtrToFirstStage(DeviceLink),
            "<"u8,
            ">\n"u8,
            ""u8,
            ""u8,
            lFixWhite,
            ColorSpace);

        m.PrintF(" {0:d} {{}} bind ", nChannels);

        for (var i = 1u; i < nChannels; i++)
            m.PrintF("dup ");

        m.PrintF("]\n");

        EmitIntent(m, Intent);

        m.PrintF(">>\n");

        if ((dwFlags & cmsFLAGS_NODEFAULTRESOURCEDEF) is 0)
            m.PrintF("/Current exch /ColorRendering defineresource pop\n");

        cmsPipelineFree(DeviceLink);
        cmsDeleteTransform(xform);

        return true;
    }

    private static void BuildColorantList(Span<byte> Colorant, uint nColorant, ReadOnlySpan<ushort> Out)
    {
        Span<byte> Buff = stackalloc byte[32];

        Colorant[0] = 0;
        if (nColorant > Context.MaxChannels)
            nColorant = Context.MaxChannels;

        var format = "{0:f3}"u8;
        for (var j = 0; j < nColorant; j++)
        {
            snprintf(Buff, 31, format, Out[j] / 65535.0);
            Buff[31] = 0;
            strcat(Colorant, Buff);
            if (j < nColorant - 1)
                strcat(Colorant, " "u8);
        }
    }

    private static bool WriteNamedColorCRD(IOHandler m, Profile hNamedColor, uint Intent, uint dwFlags)
    {
        Span<byte> ColorName = stackalloc byte[MaxPath];
        Span<byte> Colorant = stackalloc byte[512];

        var OutputFormat = cmsFormatterForColorspaceOfProfile(hNamedColor, 2, false);
        var nColorant = (uint)T_CHANNELS(OutputFormat);

        var xform = cmsCreateTransform(hNamedColor, TYPE_NAMED_COLOR_INDEX, null, OutputFormat, Intent, dwFlags);
        if (xform is null)
            return false;

        var NamedColorList = cmsGetNamedColorList(xform);
        if (NamedColorList is null)
        {
            cmsDeleteTransform(xform);
            return false;
        }

        m.PrintF("<<\n");
        m.PrintF("(colorlistcomment) (Named profile) \n");
        m.PrintF("(Prefix) [ (Pantone ) (PANTONE ) ]\n");
        m.PrintF("(Suffix) [ ( CV) ( CVC) ( C) ]\n");

        var nColors = cmsNamedColorCount(NamedColorList);

        Span<ushort> In = stackalloc ushort[1];
        Span<ushort> Out = stackalloc ushort[Context.MaxChannels];
        for (var i = 0u; i < nColors; i++)
        {
            In[0] = (ushort)i;

            if (!cmsNamedColorInfo(NamedColorList, i, ColorName, null, null, null, null))
                continue;

            cmsDoTransform(xform, In, Out, 1);
            BuildColorantList(Colorant, nColorant, Out);
            m.PrintF(
                "  ({0}) [ {1} ]\n",
                Encoding.ASCII.GetString(TrimBuffer(ColorName)),
                Encoding.ASCII.GetString(TrimBuffer(Colorant)));
        }

        m.PrintF("   >>");

        if ((dwFlags & cmsFLAGS_NODEFAULTRESOURCEDEF) is 0)
        {
            m.PrintF(" /Current exch /HPSpotTable defineresource pop\n");
        }

        cmsDeleteTransform(xform);
        return true;
    }

    private static uint GenerateCRD(Context? _, Profile Profile, uint Intent, uint dwFlags, IOHandler mem)
    {
        if ((dwFlags & cmsFLAGS_NODEFAULTRESOURCEDEF) is 0)
            EmitHeader(mem, "Color Rendering Dictionary (CRD)"u8, Profile);

        // Is a named color profile?
        if (cmsGetDeviceClass(Profile) == Signatures.ProfileClass.NamedColor)
        {
            if (!WriteNamedColorCRD(mem, Profile, Intent, dwFlags))
                return 0;
        }
        else
        {
            // CRD are always implemented as LUT
            if (!WriteOutputLUT(mem, Profile, Intent, dwFlags))
                return 0;
        }

        if ((dwFlags & cmsFLAGS_NODEFAULTRESOURCEDEF) is 0)
        {
            mem.PrintF("%%EndResource\n");
            mem.PrintF("\n% CRD End\n");
        }

        // Return used byte count
        return mem.UsedSpace;
    }

    public static uint cmsGetPostScriptColorResource(Context? ContextID,
                                                     PostScriptResourceType Type,
                                                     Profile Profile,
                                                     uint Intent,
                                                     uint dwFlags,
                                                     IOHandler io) =>
        Type switch
        {
            PostScriptResourceType.CSA => GenerateCSA(ContextID, Profile, Intent, dwFlags, io),
            _                          => GenerateCRD(ContextID, Profile, Intent, dwFlags, io)
        };

    public static uint cmsGetPostScriptCRD(Context? ContextID,
                                           Profile Profile,
                                           uint Intent,
                                           uint dwFlags,
                                           Memory<byte> Buffer,
                                           uint dwBufferLen)
    {
        // Set up the serialization engine
        var mem = Buffer.IsEmpty
                      ? cmsOpenIOhandlerFromNULL(ContextID)
                      : cmsOpenIOhandlerFromMem(ContextID, Buffer, dwBufferLen, "w");

        if (mem is null)
            return 0;

        var dwBytesUsed = cmsGetPostScriptColorResource(
            ContextID,
            PostScriptResourceType.CRD,
            Profile,
            Intent,
            dwFlags,
            mem);

        // Get rid of memory stream
        cmsCloseIOhandler(mem);

        return dwBytesUsed;
    }

    public static uint cmsGetPostScriptCSA(Context? ContextID,
                                           Profile Profile,
                                           uint Intent,
                                           uint dwFlags,
                                           Memory<byte> Buffer,
                                           uint dwBufferLen)
    {
        // Set up the serialization engine
        var mem = Buffer.IsEmpty
                      ? cmsOpenIOhandlerFromNULL(ContextID)
                      : cmsOpenIOhandlerFromMem(ContextID, Buffer, dwBufferLen, "w");

        if (mem is null)
            return 0;

        var dwBytesUsed = cmsGetPostScriptColorResource(
            ContextID,
            PostScriptResourceType.CSA,
            Profile,
            Intent,
            dwFlags,
            mem);

        // Get rid of memory stream
        cmsCloseIOhandler(mem);

        return dwBytesUsed;
    }

    private static void EmitSafeGuardBegin(IOHandler m, ReadOnlySpan<byte> name)
    {
        Span<char> str = stackalloc char[name.Length];
        for (var i = 0; i < name.Length; i++)
            str[i] = (char)name[i];
        var nameStr = new string(str);
        m.PrintF("%%LCMS2: Save previous definition of {0} on the operand stack\n", nameStr);
        m.PrintF("currentdict /{0} known {{ /{0} load }} {{ null }} ifelse\n", nameStr);
    }

    private static void EmitSafeGuardEnd(IOHandler m, ReadOnlySpan<byte> name, int depth)
    {
        Span<char> str = stackalloc char[name.Length];
        for (var i = 0; i < name.Length; i++)
            str[i] = (char)name[i];
        var nameStr = new string(str);

        m.PrintF("%%LCMS2: Restore previous definition of {0}\n", nameStr);
        if (depth > 1)
        {
            // cycle topmost items on the stack to bring the previous definition to the front
            m.PrintF("{0} -1 roll ", depth);
        }

        m.PrintF("dup null eq {{ pop currentdict /{0} undef }} {{ /{0} exch def }} ifelse\n", nameStr);
    }
}
