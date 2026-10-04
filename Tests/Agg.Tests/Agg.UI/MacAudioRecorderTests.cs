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
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MatterHackers.Agg.UI.Tests
{
	/// <summary>
	/// The mac microphone recorder. Reading the permission status never prompts, so it always runs; an actual
	/// recording needs a microphone and a permission answer from a person, so it runs only with
	/// AGG_LIVE_MIC_TESTS=1 (and, unbundled, Terminal or the IDE allowed in Privacy &amp; Security &gt; Microphone).
	/// </summary>
	public class MacAudioRecorderTests
	{
		[Test]
		public async Task ThePermissionStatusIsReadableWithoutPrompting()
		{
			long status = MacAudioRecorder.AuthorizationStatus();

			await Assert.That(status).IsGreaterThanOrEqualTo(MacAudioRecorder.AuthorizationNotDetermined);
			await Assert.That(status).IsLessThanOrEqualTo(MacAudioRecorder.AuthorizationAuthorized);
			await Assert.That(new MacAudioRecorder().IsAvailable).IsTrue();
		}

		[Test]
		public async Task ALiveRecordingComesBackAsSixteenKilohertzMonoWav()
		{
			if (Environment.GetEnvironmentVariable("AGG_LIVE_MIC_TESTS") != "1")
			{
				Skip.Test("Set AGG_LIVE_MIC_TESTS=1 to record from the real microphone.");
			}

			var recorder = new MacAudioRecorder();
			await recorder.StartAsync();
			await Assert.That(recorder.IsRecording).IsTrue();

			// A real second of audio; there is no event to wait for when the thing measured is time itself.
			await Task.Delay(1000);
			float level = recorder.Level;
			RecordedAudio audio = await recorder.StopAsync();

			await Assert.That(level).IsGreaterThanOrEqualTo(0f);
			await Assert.That(audio.MediaType).IsEqualTo("audio/wav");
			await Assert.That(Encoding.ASCII.GetString(audio.Bytes, 0, 4)).IsEqualTo("RIFF");
			await Assert.That(Encoding.ASCII.GetString(audio.Bytes, 8, 4)).IsEqualTo("WAVE");

			// The fmt chunk follows the RIFF header: PCM (1), one channel, 16000 Hz, 16 bits.
			await Assert.That(Encoding.ASCII.GetString(audio.Bytes, 12, 4)).IsEqualTo("fmt ");
			await Assert.That(BitConverter.ToInt16(audio.Bytes, 20)).IsEqualTo((short)1);
			await Assert.That(BitConverter.ToInt16(audio.Bytes, 22)).IsEqualTo((short)1);
			await Assert.That(BitConverter.ToInt32(audio.Bytes, 24)).IsEqualTo(16000);
			await Assert.That(BitConverter.ToInt16(audio.Bytes, 34)).IsEqualTo((short)16);

			// Roughly a second at 32 000 bytes a second, header aside.
			await Assert.That(audio.Bytes.Length).IsGreaterThan(16000);
		}
	}
}
