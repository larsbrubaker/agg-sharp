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
*/

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MatterHackers.Agg.UI
{
	/// <summary>
	/// The platform-neutral half of an <see cref="IAudioRecorder"/>: the one-recording-at-a-time state, the
	/// elapsed clock, and the <see cref="MaxDuration"/> cap. A platform supplies only the capture itself
	/// (<see cref="StartCaptureAsync"/>, <see cref="StopCaptureAsync"/>, <see cref="CancelCapture"/>,
	/// <see cref="ReadLevel"/>), so every platform stops at the cap the same way.
	/// </summary>
	public abstract class AudioRecorderBase : IAudioRecorder
	{
		/// <summary>Long enough for any spoken request, short enough that a forgotten recording can't fill a disk or an upload.</summary>
		public static readonly TimeSpan DefaultMaxDuration = TimeSpan.FromMinutes(5);

		private static readonly TimeSpan DefaultCapCheckInterval = TimeSpan.FromMilliseconds(250);

		private readonly object sync = new object();
		private readonly Func<TimeSpan> clock;
		private readonly TimeSpan capCheckInterval;

		private State state = State.Idle;
		private bool cancelWhileStarting;
		private TimeSpan startedAt;
		private Timer capTimer;

		// A recording the cap stopped, waiting for StopAsync to collect it.
		private Task<RecordedAudio> autoStopped;

		protected AudioRecorderBase()
			: this(null, DefaultCapCheckInterval)
		{
		}

		/// <param name="clock">A monotonic "now"; null for the real one. Tests pass a hand-advanced clock.</param>
		/// <param name="capCheckInterval">How often the cap is checked; zero for never (a test calls <see cref="CheckDurationCap"/>).</param>
		protected AudioRecorderBase(Func<TimeSpan> clock, TimeSpan capCheckInterval)
		{
			this.clock = clock ?? (() => TimeSpan.FromTicks(Stopwatch.GetTimestamp() * TimeSpan.TicksPerSecond / Stopwatch.Frequency));
			this.capCheckInterval = capCheckInterval;
		}

		private enum State
		{
			Idle,
			Starting,
			Recording,
		}

		public abstract bool IsAvailable { get; }

		public TimeSpan MaxDuration { get; set; } = DefaultMaxDuration;

		public event EventHandler MaxDurationReached;

		public bool IsRecording
		{
			get
			{
				lock (sync)
				{
					return state == State.Recording;
				}
			}
		}

		public TimeSpan Elapsed
		{
			get
			{
				lock (sync)
				{
					return state == State.Recording ? ElapsedWhileRecording() : TimeSpan.Zero;
				}
			}
		}

		public float Level => IsRecording ? Math.Clamp(ReadLevel(), 0, 1) : 0;

		public async Task StartAsync()
		{
			if (!IsAvailable)
			{
				throw new AudioRecorderUnavailableException("Voice recording isn't available on this platform yet.");
			}

			lock (sync)
			{
				if (state != State.Idle)
				{
					throw new InvalidOperationException("A recording is already running.");
				}

				state = State.Starting;
				cancelWhileStarting = false;
				autoStopped = null;
			}

			try
			{
				await StartCaptureAsync();
			}
			catch
			{
				lock (sync)
				{
					state = State.Idle;
				}

				throw;
			}

			bool cancelled;
			lock (sync)
			{
				cancelled = cancelWhileStarting;
				if (cancelled)
				{
					state = State.Idle;
				}
				else
				{
					state = State.Recording;
					startedAt = clock();
					if (capCheckInterval > TimeSpan.Zero)
					{
						capTimer = new Timer(_ => CheckDurationCap(), null, capCheckInterval, capCheckInterval);
					}
				}
			}

			// Cancel arrived while the permission prompt was up; honour it now there is something to cancel.
			if (cancelled)
			{
				CancelCapture();
			}
		}

		public Task<RecordedAudio> StopAsync()
		{
			lock (sync)
			{
				if (autoStopped != null)
				{
					Task<RecordedAudio> collected = autoStopped;
					autoStopped = null;
					return collected;
				}

				if (state != State.Recording)
				{
					return Task.FromException<RecordedAudio>(new InvalidOperationException("Nothing is recording."));
				}

				return StopRecordingLocked();
			}
		}

		public void Cancel()
		{
			lock (sync)
			{
				autoStopped = null;
				if (state == State.Starting)
				{
					cancelWhileStarting = true;
					return;
				}

				if (state != State.Recording)
				{
					return;
				}

				state = State.Idle;
				DisposeCapTimer();
			}

			CancelCapture();
		}

		/// <summary>
		/// Stops the recording if it has reached <see cref="MaxDuration"/>; the cap timer's tick. Returns whether
		/// it stopped one.
		/// </summary>
		internal bool CheckDurationCap()
		{
			lock (sync)
			{
				if (state != State.Recording || clock() - startedAt < MaxDuration)
				{
					return false;
				}

				autoStopped = StopRecordingLocked();
			}

			MaxDurationReached?.Invoke(this, EventArgs.Empty);
			return true;
		}

		/// <summary>Begins capture; asks for permission first and throws <see cref="MicrophonePermissionDeniedException"/> on a no.</summary>
		protected abstract Task StartCaptureAsync();

		/// <summary>Ends capture and returns the encoded recording of <paramref name="duration"/>.</summary>
		protected abstract Task<RecordedAudio> StopCaptureAsync(TimeSpan duration);

		/// <summary>Ends capture and throws the audio away.</summary>
		protected abstract void CancelCapture();

		/// <summary>The current input level, 0 to 1. Only called while recording.</summary>
		protected abstract float ReadLevel();

		/// <summary>Maps a meter reading in dBFS (0 = full scale, -160 = silence) to 0..1, linear in dB over the last 60 dB.</summary>
		public static float LevelFromDecibels(float decibels)
		{
			const float floor = -60;
			if (float.IsNaN(decibels) || decibels <= floor)
			{
				return 0;
			}

			return Math.Min(1, (decibels - floor) / -floor);
		}

		private TimeSpan ElapsedWhileRecording()
		{
			TimeSpan elapsed = clock() - startedAt;
			return elapsed < MaxDuration ? elapsed : MaxDuration;
		}

		// Called holding sync with state == Recording.
		private Task<RecordedAudio> StopRecordingLocked()
		{
			TimeSpan duration = ElapsedWhileRecording();
			state = State.Idle;
			DisposeCapTimer();
			return StopCaptureAsync(duration);
		}

		private void DisposeCapTimer()
		{
			capTimer?.Dispose();
			capTimer = null;
		}
	}
}
