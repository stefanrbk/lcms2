﻿namespace lcms2.types;
public class IccMeasurementConditions : ICloneable
{
    // 0 = unknown, 1=CIE 1931, 2=CIE 1964
    public uint Observer;
    // Value of backing
    public XYZ Backing;
    // 0=unknown, 1=45/0, 0/45 2=0d, d/0
    public uint Geometry;
    // 0..1.0
    public double Flare;
    public IlluminantType IlluminantType;

    public object Clone() =>
        new IccMeasurementConditions()
        {
            Observer = Observer,
            Backing = Backing,
            Geometry = Geometry,
            Flare = Flare,
            IlluminantType = IlluminantType,
        };
}