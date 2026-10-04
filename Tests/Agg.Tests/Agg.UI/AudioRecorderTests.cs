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
using MatterHackers.Agg.Tests;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	/// <summary>
	/// The platform-neutral half of microphone capture: <see cref="AudioRecorderBase"/>'s state and duration
	/// cap (driven through a fake capture and a hand-advanced clock, so no microphone is needed), and the
	/// unavailable stub every platform without a recorder reports.
	/// </summary>
	public class AudioRecorderTests
	{
		[Test]
		public async Task TheCapStopsTheRecordingAndStopCollectsIt()
		{
			var recorder = new FakeRecorder { MaxDuration = TimeSpan.FromMinutes(5) };
			int reached = 0;
			recorder.MaxDurationReached += (s, e) => reached++;

			await recorder.StartAsync();
			recorder.Now = TimeSpan.FromMinutes(4);

			await Assert.That(recorder.CheckDurationCap()).IsFalse();
			await Assert.That(recorder.IsRecording).IsTrue();
			await Assert.That(recorder.Elapsed).IsEqualTo(TimeSpan.FromMinutes(4));

			recorder.Now = TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(200);

			await Assert.That(recorder.CheckDurationCap()).IsTrue();
			await Assert.That(reached).IsEqualTo(1);
			await Assert.That(recorder.IsRecording).IsFalse();
			await Assert.That(recorder.Elapsed).IsEqualTo(TimeSpan.Zero);
			await Assert.That(recorder.Stops).IsEqualTo(1);

			// The capped recording is not lost: StopAsync hands it over, at exactly the cap's length.
			RecordedAudio audio = await recorder.StopAsync();
			await Assert.That(audio.Duration).IsEqualTo(TimeSpan.FromMinutes(5));
			await Assert.That(audio.MediaType).IsEqualTo("audio/wav");

			// Collected once only, and a second check does not stop anything again.
			await Assert.That(recorder.CheckDurationCap()).IsFalse();
			await Assert.That(async () => await recorder.StopAsync()).Throws<InvalidOperationException>();
		}

		[Test]
		public async Task ElapsedNeverReadsPastTheCap()
		{
			var recorder = new FakeRecorder { MaxDuration = TimeSpan.FromSeconds(10) };
			await recorder.StartAsync();

			// Between the cap passing and the next timer tick, the meter still must not show 10.2 s of 10.
			recorder.Now = TimeSpan.FromSeconds(10.2);

			await Assert.That(recorder.Elapsed).IsEqualTo(TimeSpan.FromSeconds(10));
		}

		[Test]
		public async Task StopReturnsTheRecordingAndItsLength()
		{
			var recorder = new FakeRecorder();
			recorder.Now = TimeSpan.FromSeconds(100);
			await recorder.StartAsync();
			recorder.Now = TimeSpan.FromSeconds(103);

			RecordedAudio audio = await recorder.StopAsync();

			await Assert.That(audio.Duration).IsEqualTo(TimeSpan.FromSeconds(3));
			await Assert.That(recorder.IsRecording).IsFalse();

			// And the recorder is reusable.
			await recorder.StartAsync();
			await Assert.That(recorder.IsRecording).IsTrue();
		}

		[Test]
		public async Task StartingTwiceIsRefused()
		{
			var recorder = new FakeRecorder();
			await recorder.StartAsync();

			await Assert.That(async () => await recorder.StartAsync()).Throws<InvalidOperationException>();
		}

		[Test]
		public async Task CancelDiscardsWithoutStopping()
		{
			var recorder = new FakeRecorder();
			await recorder.StartAsync();

			recorder.Cancel();

			await Assert.That(recorder.Cancels).IsEqualTo(1);
			await Assert.That(recorder.Stops).IsEqualTo(0);
			await Assert.That(recorder.IsRecording).IsFalse();
			await Assert.That(async () => await recorder.StopAsync()).Throws<InvalidOperationException>();

			// Cancelling while idle is harmless.
			recorder.Cancel();
			await Assert.That(recorder.Cancels).IsEqualTo(1);
		}

		[Test]
		public async Task CancelDuringThePermissionPromptCancelsOnceItAnswers()
		{
			var recorder = new FakeRecorder { StartGate = new TaskCompletionSource() };
			Task starting = recorder.StartAsync();

			recorder.Cancel();
			recorder.StartGate.SetResult();
			await starting;

			await Assert.That(recorder.IsRecording).IsFalse();
			await Assert.That(recorder.Cancels).IsEqualTo(1);
		}

		[Test]
		public async Task ADeniedPermissionLeavesTheRecorderIdle()
		{
			var recorder = new FakeRecorder { Deny = true };

			await Assert.That(async () => await recorder.StartAsync()).Throws<MicrophonePermissionDeniedException>();
			await Assert.That(recorder.IsRecording).IsFalse();

			recorder.Deny = false;
			await recorder.StartAsync();
			await Assert.That(recorder.IsRecording).IsTrue();
		}

		[Test]
		public async Task LevelIsZeroWhenIdleAndClampedWhileRecording()
		{
			var recorder = new FakeRecorder { RawLevel = 3 };

			await Assert.That(recorder.Level).IsEqualTo(0f);

			await recorder.StartAsync();
			await Assert.That(recorder.Level).IsEqualTo(1f);
		}

		[Test]
		public async Task DecibelsMapOntoTheLastSixtyDb()
		{
			await Assert.That(AudioRecorderBase.LevelFromDecibels(0)).IsEqualTo(1f);
			await Assert.That(AudioRecorderBase.LevelFromDecibels(-30)).IsEqualTo(0.5f);
			await Assert.That(AudioRecorderBase.LevelFromDecibels(-160)).IsEqualTo(0f);
			await Assert.That(AudioRecorderBase.LevelFromDecibels(float.NaN)).IsEqualTo(0f);
			await Assert.That(AudioRecorderBase.LevelFromDecibels(3)).IsEqualTo(1f);
		}

		[Test]
		public async Task TheUnavailableStubSaysSoAndRefusesToStart()
		{
			var recorder = new UnavailableAudioRecorder();

			await Assert.That(recorder.IsAvailable).IsFalse();
			await Assert.That(async () => await recorder.StartAsync()).Throws<AudioRecorderUnavailableException>();
			await Assert.That(async () => await recorder.StopAsync()).Throws<InvalidOperationException>();
			recorder.Cancel();
		}

		[Test]
		[NotInParallel(SharedStateKeys.AudioRecorder)]
		public async Task TheProviderIsNeverNull()
		{
			IAudioRecorder installed = AudioRecorder.Instance;
			try
			{
				var fake = new FakeRecorder();
				AudioRecorder.SetSystemAudioRecorder(fake);
				await Assert.That(AudioRecorder.Instance).IsSameReferenceAs(fake);

				AudioRecorder.SetSystemAudioRecorder(null);
				await Assert.That(AudioRecorder.Instance).IsNotNull();
				await Assert.That(AudioRecorder.Instance.IsAvailable).IsFalse();
			}
			finally
			{
				AudioRecorder.SetSystemAudioRecorder(installed);
			}
		}

		[Test]
		public async Task ARecorderThatIsNotAvailableRefusesBeforeCapturing()
		{
			var recorder = new FakeRecorder { Available = false };

			await Assert.That(async () => await recorder.StartAsync()).Throws<AudioRecorderUnavailableException>();
			await Assert.That(recorder.Starts).IsEqualTo(0);
		}

		/// <summary>A capture that records nothing, over the real <see cref="AudioRecorderBase"/>, with a clock the test moves.</summary>
		private class FakeRecorder : AudioRecorderBase
		{
			private readonly Clock clock;

			public FakeRecorder()
				: this(new Clock())
			{
			}

			private FakeRecorder(Clock clock)
				: base(() => clock.Now, TimeSpan.Zero)
			{
				this.clock = clock;
			}

			public TimeSpan Now
			{
				get => clock.Now;
				set => clock.Now = value;
			}

			public bool Available { get; set; } = true;

			public bool Deny { get; set; }

			public float RawLevel { get; set; }

			public TaskCompletionSource StartGate { get; set; }

			public int Starts { get; private set; }

			public int Stops { get; private set; }

			public int Cancels { get; private set; }

			public override bool IsAvailable => Available;

			protected override async Task StartCaptureAsync()
			{
				if (StartGate != null)
				{
					await StartGate.Task;
				}

				if (Deny)
				{
					throw new MicrophonePermissionDeniedException("denied");
				}

				Starts++;
			}

			protected override Task<RecordedAudio> StopCaptureAsync(TimeSpan duration)
			{
				Stops++;
				return Task.FromResult(new RecordedAudio(new byte[44], "audio/wav", "recording.wav", duration));
			}

			protected override void CancelCapture() => Cancels++;

			protected override float ReadLevel() => RawLevel;

			private class Clock
			{
				public TimeSpan Now { get; set; }
			}
		}
	}
}
