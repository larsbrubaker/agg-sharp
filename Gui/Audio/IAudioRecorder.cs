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
	/// Microphone capture as a platform service, the audio peer of <see cref="ISystemClipboard"/>. An app
	/// reaches the installed one through <see cref="AudioRecorder.Instance"/>, which is never null: a platform
	/// that has no recorder yet reports <see cref="IsAvailable"/> false rather than leaving a hole.
	/// <para>
	/// One recording at a time: <see cref="StartAsync"/>, then either <see cref="StopAsync"/> for the bytes or
	/// <see cref="Cancel"/> to throw them away. A recording that runs to <see cref="MaxDuration"/> stops itself,
	/// raises <see cref="MaxDurationReached"/>, and its bytes are still collected with <see cref="StopAsync"/>.
	/// </para>
	/// </summary>
	public interface IAudioRecorder
	{
		/// <summary>Whether this platform can record at all. Says nothing about permission, which is only asked on start.</summary>
		bool IsAvailable { get; }

		/// <summary>True from a successful <see cref="StartAsync"/> until stop, cancel or the duration cap.</summary>
		bool IsRecording { get; }

		/// <summary>How long the current recording has run, never more than <see cref="MaxDuration"/>; zero when idle.</summary>
		TimeSpan Elapsed { get; }

		/// <summary>The input level right now, 0 (silence) to 1 (full scale), for a "listening" meter. Zero when idle.</summary>
		float Level { get; }

		/// <summary>The safety cap: a recording this long stops itself. Defaults to five minutes.</summary>
		TimeSpan MaxDuration { get; set; }

		/// <summary>Raised (on a worker thread) when a recording hit <see cref="MaxDuration"/> and stopped itself.</summary>
		event EventHandler MaxDurationReached;

		/// <summary>
		/// Starts recording, asking the user for microphone permission first if they have not been asked.
		/// </summary>
		/// <exception cref="AudioRecorderUnavailableException">This platform cannot record (<see cref="IsAvailable"/> is false).</exception>
		/// <exception cref="MicrophonePermissionDeniedException">The user said no, now or earlier.</exception>
		/// <exception cref="InvalidOperationException">A recording is already running.</exception>
		Task StartAsync();

		/// <summary>Stops the recording (or collects one the duration cap already stopped) and returns its encoded bytes.</summary>
		/// <exception cref="InvalidOperationException">Nothing was recording.</exception>
		Task<RecordedAudio> StopAsync();

		/// <summary>Stops and discards the current recording. Does nothing when idle.</summary>
		void Cancel();
	}

	/// <summary>A finished recording: encoded bytes ready to upload, and what they are.</summary>
	public sealed class RecordedAudio
	{
		public RecordedAudio(byte[] bytes, string mediaType, string fileName, TimeSpan duration)
		{
			Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
			MediaType = mediaType ?? throw new ArgumentNullException(nameof(mediaType));
			FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
			Duration = duration;
		}

		/// <summary>The whole encoded file (for example a complete WAV, header included).</summary>
		public byte[] Bytes { get; }

		/// <summary>The bytes' MIME type, for example <c>audio/wav</c>.</summary>
		public string MediaType { get; }

		/// <summary>
		/// A file name with the right extension, for example <c>recording.wav</c>. Transcription services
		/// tell the format from the upload's file name, so it travels with the bytes.
		/// </summary>
		public string FileName { get; }

		public TimeSpan Duration { get; }
	}

	/// <summary>Thrown by <see cref="IAudioRecorder.StartAsync"/> on a platform with no recorder.</summary>
	public class AudioRecorderUnavailableException : InvalidOperationException
	{
		public AudioRecorderUnavailableException(string message)
			: base(message)
		{
		}
	}

	/// <summary>Thrown by <see cref="IAudioRecorder.StartAsync"/> when the user has not allowed microphone access.</summary>
	public class MicrophonePermissionDeniedException : UnauthorizedAccessException
	{
		public MicrophonePermissionDeniedException(string message)
			: base(message)
		{
		}
	}
}
