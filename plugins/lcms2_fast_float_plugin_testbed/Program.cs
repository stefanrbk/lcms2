﻿//---------------------------------------------------------------------------------
//
//  Little Color Management System, fast floating point extensions
//  Copyright (c) 1998-2022 Marti Maria Saguer, all rights reserved
//  Copyright (c) 2022-2023 Stefan Kewatt, all rights reserved
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
using lcms2;

var now = DateTime.Now;

trace("LittleCMS.net FastFloating point extensions testbed - 1.5 {now:MMM d yyyy HH:mm:ss}", now);
trace("Copyright (c) 1998-2022 Marti Maria Saguer, all rights reserved");
trace("Copyright (c) 2022-2023 Stefan Kewatt, all rights reserved");

Thread.Sleep(10);
Console.WriteLine();

using (logger.BeginScope("Installing error logger"))
{
    Lcms2.cmsSetLogErrorHandler(BuildDebugLogger());
    trace("Done");
}

using (logger.BeginScope("Installing plugin"))
{
    Lcms2.cmsPlugin(cmsFastFloatExtensions());
    trace("Done");
}
