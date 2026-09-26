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
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using MatterHackers.Agg.Platform;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.Tests
{
	// FSEvents on macOS delivers changes with noticeable latency, so every wait here is on the callback
	// itself; the timeout is only a safety net that fails the test, never the thing that paces it.
	[UnsupportedOSPlatform("browser")]
	public class FileSystemDirectoryMonitorTests
	{
		private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(30);

		private static string NewTempDirectory()
		{
			var path = Path.Combine(Path.GetTempPath(), "FileSystemDirectoryMonitorTests_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		private static void DeleteQuietly(string path)
		{
			try
			{
				Directory.Delete(path, recursive: true);
			}
			catch (IOException)
			{
			}
		}

		private static async Task<bool> Reported(TaskCompletionSource<bool> signal)
		{
			var finished = await Task.WhenAny(signal.Task, Task.Delay(SafetyTimeout));
			return finished == signal.Task;
		}

		[Test]
		public async Task CreatingASubfolderReports()
		{
			var folder = NewTempDirectory();
			try
			{
				var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				using var watch = new FileSystemDirectoryMonitor().Watch(folder, () => signal.TrySetResult(true));
				await Assert.That(watch).IsNotNull();

				Directory.CreateDirectory(Path.Combine(folder, "sub"));

				await Assert.That(await Reported(signal)).IsTrue();
			}
			finally
			{
				DeleteQuietly(folder);
			}
		}

		[Test]
		public async Task CreatingAFileReports()
		{
			var folder = NewTempDirectory();
			try
			{
				var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				using var watch = new FileSystemDirectoryMonitor().Watch(folder, () => signal.TrySetResult(true));
				await Assert.That(watch).IsNotNull();

				File.WriteAllText(Path.Combine(folder, "file.txt"), "hello");

				await Assert.That(await Reported(signal)).IsTrue();
			}
			finally
			{
				DeleteQuietly(folder);
			}
		}

		[Test]
		public async Task DisposedWatchStaysSilent()
		{
			var disposedFolder = NewTempDirectory();
			var sentinelFolder = NewTempDirectory();
			try
			{
				var monitor = new FileSystemDirectoryMonitor();
				int disposedCalls = 0;
				var disposedWatch = monitor.Watch(disposedFolder, () => System.Threading.Interlocked.Increment(ref disposedCalls));
				await Assert.That(disposedWatch).IsNotNull();
				disposedWatch.Dispose();

				var sentinel = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				using var sentinelWatch = monitor.Watch(sentinelFolder, () => sentinel.TrySetResult(true));
				await Assert.That(sentinelWatch).IsNotNull();

				// The change to the disposed folder goes first, so by the time the sentinel's later change
				// has been delivered, any report the disposed watch was going to make would have been too.
				Directory.CreateDirectory(Path.Combine(disposedFolder, "sub"));
				Directory.CreateDirectory(Path.Combine(sentinelFolder, "sub"));

				await Assert.That(await Reported(sentinel)).IsTrue();
				await Assert.That(disposedCalls).IsEqualTo(0);
			}
			finally
			{
				DeleteQuietly(disposedFolder);
				DeleteQuietly(sentinelFolder);
			}
		}

		[Test]
		public async Task MissingDirectoryAnswersNull()
		{
			var missing = Path.Combine(Path.GetTempPath(), "FileSystemDirectoryMonitorTests_missing_" + Guid.NewGuid().ToString("N"));

			var watch = new FileSystemDirectoryMonitor().Watch(missing, () => { });

			await Assert.That(watch).IsNull();
		}

		[Test]
		public async Task DefaultMonitorWatchesNothing()
		{
			var folder = NewTempDirectory();
			try
			{
				await Assert.That(AggContext.DirectoryMonitor.Watch(folder, () => { })).IsNull();
			}
			finally
			{
				DeleteQuietly(folder);
			}
		}
	}
}
