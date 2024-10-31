using lcms2.types;

namespace lcms2.legacy;

public class Lcms2
{
    // Version/release
    public const ushort LCMS_VERSION = state.Context.LibraryVersion;

    public const ushort cmsMAX_PATH = lcms2.Lcms2.MaxPath;

    // D50 XYZ normalized to Y=1.0
    public static readonly double cmsD50X = CIEXYZ.D50.X;
    public static readonly double cmsD50Y = CIEXYZ.D50.Y;
    public static readonly double cmsD50Z = CIEXYZ.D50.Z;

    // V4 perceptual black
    public static readonly double cmsPERCEPTUAL_BLACK_X = Profile.PerceptualBlack.X;
    public static readonly double cmsPERCEPTUAL_BLACK_Y = Profile.PerceptualBlack.Y;
    public static readonly double cmsPERCEPTUAL_BLACK_Z = Profile.PerceptualBlack.Z;
}
