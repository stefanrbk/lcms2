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

public static partial class Lcms2
{
    internal static readonly IntentsPluginChunkType IntentsPluginChunk = new();

    internal static readonly IntentsPluginChunkType globalIntentsPluginChunk = new();

    internal static void DupPluginIntentsList(ref IntentsPluginChunkType dest, in IntentsPluginChunkType src) =>
        dest = (IntentsPluginChunkType)((ICloneable)src).Clone();

    internal static void _cmsAllocIntentsPluginChunk(Context ctx, Context? src)
    {
        _cmsAssert(ctx);

        var from = src?.IntentsPlugin ?? IntentsPluginChunk;
        DupPluginIntentsList(ref ctx.IntentsPlugin, from);
    }

    internal static Pipeline? _cmsLinkProfiles(Context? ContextID,
                                               uint nProfiles,
                                               ReadOnlySpan<uint> TheIntents,
                                               Profile[] Profiles,
                                               Span<bool> BPC,
                                               ReadOnlySpan<double> AdaptationStates,
                                               uint dwFlags)
    {
        // Make sure a reasonable number of profiles is provided
        if (nProfiles is <= 0 or > 255)
        {
            Context.LogError(ContextID, cmsERROR_RANGE, $"Couldn't link '{nProfiles}' profiles");
            return null;
        }

        for (var i = 0; i < nProfiles; i++)
        {
            // Check if black point is really needed or allowed. Note that
            // following Adobe's document:
            // BPC does not apply to devicelink profiles, nor to abs colorimetric,
            // and applies always on V4 perceptual and saturation.

            if (TheIntents[i] is INTENT_ABSOLUTE_COLORIMETRIC)
                BPC[i] = false;

            if (TheIntents[i] is INTENT_PERCEPTUAL or INTENT_SATURATION)
            {
                // Force BPC for V4 profiles in perceptual and saturation
                if (cmsGetEncodedICCVersion(Profiles[i]) >= 0x04000000)
                    BPC[i] = true;
            }
        }

        // Search for a handler. The first intent in the chain defines the handler. That would
        // prevent using multiple custom intents in a multiintent chain, but the behaviour of
        // this case would present some issues if the custom intent tries to do things like
        // preserve primaries. This solution is not perfect, but works well on most cases.

        var intent = IntentFunctions.Search(ContextID, TheIntents[0]);
        if (intent is null)
        {
            Context.LogError(ContextID, cmsERROR_UNKNOWN_EXTENSION, $"Unsupported intent '{TheIntents[0]}'");
            return null;
        }

        // Call the handler
        return intent.Link(ContextID, nProfiles, TheIntents, Profiles, BPC, AdaptationStates, dwFlags);
    }

    public static uint cmsGetSupportedIntentsTHR(Context? ContextID,
                                                 uint nMax,
                                                 Span<uint> Codes,
                                                 Span<string> Descriptions)
    {
        var ctx = Context.Get(ContextID).IntentsPlugin;

        var i = 0;
        foreach (var intent in IntentFunctions.DefaultFunctions.Concat(ctx.Intents).Take((int)nMax))
        {
            if (Codes.Length > i)
                Codes[i] = intent;
            if (Descriptions.Length > i)
                Descriptions[i] = intent.Description;

            i++;
        }

        return (uint)i;
    }

    public static uint cmsGetSupportedIntents(uint nMax, Span<uint> Codes, Span<string> Descriptions) =>
        cmsGetSupportedIntentsTHR(null, nMax, Codes, Descriptions);
}
