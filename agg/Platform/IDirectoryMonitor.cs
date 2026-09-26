/*
Copyright (c) 2026, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;

namespace MatterHackers.Agg.Platform
{
	/// <summary>
	/// Tells an application when a directory it is showing changes underneath it. The app head registers an
	/// implementation at boot on <see cref="AggContext.DirectoryMonitor"/>; a head that has no way to watch -
	/// or no wish to - registers nothing, and the application simply never learns about outside edits.
	/// </summary>
	/// <remarks>
	/// This is a seam rather than a direct <c>FileSystemWatcher</c> because that type is
	/// <c>[UnsupportedOSPlatform("browser")]</c>, so code that must also build for the browser cannot even
	/// name it - and the browser has no watcher to offer. Live watching is genuinely optional: a listing can
	/// always be refreshed by the user. <see cref="FileSystemDirectoryMonitor"/> is the desktop answer.
	/// </remarks>
	public interface IDirectoryMonitor
	{
		/// <summary>
		/// Starts watching <paramref name="directoryPath"/> - its immediate level only, not its
		/// subdirectories - calling <paramref name="directoryChanged"/> when a file or folder in it is
		/// created, deleted, renamed or written to.
		/// </summary>
		/// <returns>
		/// The live watch, or null if this directory cannot be watched. Null is an ordinary answer, not a
		/// failure: callers already have to work without a monitor at all, so they take the same path.
		/// </returns>
		/// <remarks>
		/// Implementations must not throw. Watching is a convenience, and the directories an application
		/// browses include ones the OS will refuse (a permission-restricted system folder, a network share
		/// that has gone away) - a caller that has to guard every Watch call would get no benefit from the
		/// answer anyway. The callback may arrive on any thread, so callers marshal it themselves.
		/// </remarks>
		IDirectoryWatch Watch(string directoryPath, Action directoryChanged);
	}
}
