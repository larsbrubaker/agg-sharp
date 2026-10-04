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
using System.Threading.Tasks;

namespace MatterHackers.Agg.UI
{
	/// <summary>
	/// Where the platform's <see cref="IAudioRecorder"/> lives, installed the way the clipboard is: a host or
	/// app head calls <see cref="SetSystemAudioRecorder"/> once at startup (the mac head with a
	/// <c>MacAudioRecorder</c>). Until then, and on every platform with no recorder, <see cref="Instance"/> is an
	/// <see cref="UnavailableAudioRecorder"/>, so callers can check <see cref="IAudioRecorder.IsAvailable"/>
	/// without a null check.
	/// </summary>
	public static class AudioRecorder
	{
		private static IAudioRecorder recorder = new UnavailableAudioRecorder();

		/// <summary>Installs the platform recorder. Null puts the unavailable stub back.</summary>
		public static void SetSystemAudioRecorder(IAudioRecorder audioRecorder)
		{
			recorder = audioRecorder ?? new UnavailableAudioRecorder();
		}

		public static IAudioRecorder Instance => recorder;
	}

	/// <summary>
	/// The recorder for a platform that cannot record yet (today Windows, Linux and the browser). It says so
	/// through <see cref="IsAvailable"/>, and refuses to start with a message a user can act on.
	/// </summary>
	public sealed class UnavailableAudioRecorder : IAudioRecorder
	{
		public bool IsAvailable => false;

		public bool IsRecording => false;

		public TimeSpan Elapsed => TimeSpan.Zero;

		public float Level => 0;

		public TimeSpan MaxDuration { get; set; } = AudioRecorderBase.DefaultMaxDuration;

		// Never raised: nothing ever records.
		public event EventHandler MaxDurationReached
		{
			add { }
			remove { }
		}

		public Task StartAsync()
			=> Task.FromException(new AudioRecorderUnavailableException("Voice recording isn't available on this platform yet."));

		public Task<RecordedAudio> StopAsync()
			=> Task.FromException<RecordedAudio>(new InvalidOperationException("Nothing is recording."));

		public void Cancel()
		{
		}
	}
}
