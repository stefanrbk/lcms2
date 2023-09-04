﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright (c) 1998-2022 Marti Maria Saguer, all rights reserved
//                     2023 Stefan Kewatt, all rights reserved
//
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
using lcms2.state;

namespace lcms2.FastFloatPlugin.shapers;
public class Curves8Data : IDisposable
{
    private bool disposedValue;
    public Context? ContextID;
    public int nCurves;
    private readonly byte[] _curves;
    public ref byte Curves(int a, int b) =>
        ref _curves[(a * cmsMAXCHANNELS) + b];

    public Curves8Data(Context? context)
    {
        ContextID = context;
        _curves = Context.GetPool<byte>(context).Rent(cmsMAXCHANNELS * 256);
        Array.Clear(_curves);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Context.GetPool<byte>(ContextID).Return(_curves);
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}