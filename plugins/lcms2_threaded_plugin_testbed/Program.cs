//---------------------------------------------------------------------------------
//
//  Little Color Management System, multithread extensions
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

using lcms2.state;

var now = DateTime.Now;

var cliResult = CommandLine.Parser.Default.ParseArguments<lcms2.ThreadedPlugin.testbed.CliOptions>(args);

var doSpeedTests = cliResult.Value.DoSpeed;
var noCheckTests = cliResult.Value.NoChecks;

trace("LittleCMS.net Multithreaded extensions testbed - 1.1 {0:MMM d yyyy HH:mm:ss}", now);
trace("Copyright (c) 1998-2023 Marti Maria Saguer, all rights reserved");
trace("Copyright (c) 2022-2023 Stefan Kewatt, all rights reserved\n");

using (logger.BeginScope("Installing error logger"))
{
    Context.Shared.SetLoggerFactory(BuildDebugLogger());
    trace("Done");
}

using (logger.BeginScope("Installing plugin"))
{
    Context.Shared.RegisterPlugin(cmsThreadedExtensions(CMS_THREADED_GUESS_MAX_THREADS, 0));
    trace("Done");
}

if (!noCheckTests)
{
    // Change format
    CheckChangeFormat();

    // Accuracy
    CheckAccuracy8Bits();
    CheckAccuracy16Bits();
}

if (doSpeedTests)
{
    // Check speed
    SpeedTest8();
    SpeedTest16();
    ComparativeLineStride8bits();
}

Context.Shared.ClearAllPlugins();

Console.WriteLine();
trace("All tests passed OK");
fflush();

return 0;