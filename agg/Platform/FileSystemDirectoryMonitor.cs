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
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace MatterHackers.Agg.Platform
{
	/// <summary>
	/// Watches directories with <see cref="FileSystemWatcher"/>, for <see cref="IDirectoryMonitor"/>. An app
	/// head registers one on <see cref="AggContext.DirectoryMonitor"/> on Windows, macOS and Linux alike.
	/// </summary>
	/// <remarks>
	/// Lives in agg rather than a platform assembly because <c>FileSystemWatcher</c> works the same on every
	/// desktop OS; only the browser lacks it, hence the annotation. On macOS it rides FSEvents, which delivers
	/// changes with some latency - callers must not expect a report the instant a change is made.
	/// </remarks>
	[UnsupportedOSPlatform("browser")]
	public class FileSystemDirectoryMonitor : IDirectoryMonitor
	{
		/// <inheritdoc/>
		public IDirectoryWatch Watch(string directoryPath, Action directoryChanged)
		{
			FileSystemWatcher watcher = null;

			try
			{
				watcher = new FileSystemWatcher(directoryPath)
				{
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,

					// The seam promises the immediate level only - a recursive watch on a library root would
					// report every asset the app itself writes deep below it.
					IncludeSubdirectories = false,
				};

				return new FileSystemWatch(watcher, directoryChanged);
			}
			catch (Exception ex)
			{
				// Swallowed to keep the seam's no-throw promise: a directory the OS will not let us watch
				// (a missing or restricted folder, a share that has gone away) is a listing that does not
				// live-refresh, not a failure the user needs to hear about.
				Debug.WriteLine($"Cannot monitor directory '{directoryPath}': {ex.Message}");

				watcher?.Dispose();

				return null;
			}
		}

		private class FileSystemWatch : IDirectoryWatch
		{
			private readonly FileSystemWatcher watcher;

			private readonly FileSystemEventHandler changed;

			private readonly RenamedEventHandler renamed;

			public FileSystemWatch(FileSystemWatcher watcher, Action directoryChanged)
			{
				this.watcher = watcher;

				// All four events mean the same thing to the caller - "this folder is not what you listed" -
				// so they collapse into the one callback. Held in fields so Dispose can take them off again.
				this.changed = (sender, e) => directoryChanged();
				this.renamed = (sender, e) => directoryChanged();

				watcher.Changed += this.changed;
				watcher.Created += this.changed;
				watcher.Deleted += this.changed;
				watcher.Renamed += this.renamed;

				// Last, so no report can arrive before the handlers are on.
				watcher.EnableRaisingEvents = true;
			}

			public bool Enabled
			{
				get => this.watcher.EnableRaisingEvents;
				set => this.watcher.EnableRaisingEvents = value;
			}

			public void Dispose()
			{
				this.watcher.EnableRaisingEvents = false;

				this.watcher.Changed -= this.changed;
				this.watcher.Created -= this.changed;
				this.watcher.Deleted -= this.changed;
				this.watcher.Renamed -= this.renamed;

				this.watcher.Dispose();
			}
		}
	}
}
