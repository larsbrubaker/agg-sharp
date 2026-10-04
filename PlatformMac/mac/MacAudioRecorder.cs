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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MatterHackers.Agg.Platform.Mac;
using static MatterHackers.Agg.Platform.Mac.AVFoundationInterop;

namespace MatterHackers.Agg.UI
{
	/// <summary>
	/// The macOS <see cref="IAudioRecorder"/>: an AVAudioRecorder writing 16-bit PCM mono 16 kHz WAV to a temp
	/// file, which <see cref="IAudioRecorder.StopAsync"/> reads back and deletes. That is the smallest format
	/// speech-to-text services take without conversion (about 1.9 MB a minute), and AVAudioRecorder does the
	/// resampling from the hardware rate. An app installs it with
	/// <c>AudioRecorder.SetSystemAudioRecorder(new MacAudioRecorder())</c>.
	/// <para>
	/// Why AVAudioRecorder and not an AVAudioEngine input tap: the tap hands buffers to an Objective-C block on
	/// a real-time thread, which from raw P/Invoke means a hand-built block plus format conversion and WAV
	/// writing of our own. AVAudioRecorder needs neither; its only block is the one-off permission callback.
	/// </para>
	/// <para>
	/// <b>Permission.</b> macOS asks the user once (TCC) and remembers the answer. The prompt has to name a
	/// reason, which comes from <c>NSMicrophoneUsageDescription</c> in the app's Info.plist - so when the app
	/// is packaged as a .app bundle, that key must be added. Until then the app runs unbundled (a bare
	/// <c>dotnet</c> process), and macOS attributes the request to the "responsible" process that launched it:
	/// the prompt says Terminal (or the IDE) wants the microphone, the grant lands on that app in System
	/// Settings &gt; Privacy &amp; Security &gt; Microphone, and every program launched from it shares the answer.
	/// A user who said no has to turn it back on there; <see cref="StartCaptureAsync"/> says so.
	/// </para>
	/// </summary>
	public class MacAudioRecorder : AudioRecorderBase
	{
		// AVAuthorizationStatus.
		internal const long AuthorizationNotDetermined = 0;
		internal const long AuthorizationRestricted = 1;
		internal const long AuthorizationDenied = 2;
		internal const long AuthorizationAuthorized = 3;

		// kAudioFormatLinearPCM, the four-char code 'lpcm'.
		private const long FormatLinearPcm = 0x6C70636D;
		private const double SampleRate = 16000;

		private static readonly IntPtr SelAuthorizationStatusForMediaType = ObjC.Sel("authorizationStatusForMediaType:");
		private static readonly IntPtr SelRequestAccessForMediaType = ObjC.Sel("requestAccessForMediaType:completionHandler:");
		private static readonly IntPtr SelFileUrlWithPath = ObjC.Sel("fileURLWithPath:");
		private static readonly IntPtr SelNumberWithLongLong = ObjC.Sel("numberWithLongLong:");
		private static readonly IntPtr SelNumberWithDouble = ObjC.Sel("numberWithDouble:");
		private static readonly IntPtr SelDictionaryWithObjects = ObjC.Sel("dictionaryWithObjects:forKeys:count:");
		private static readonly IntPtr SelInitWithUrlSettingsError = ObjC.Sel("initWithURL:settings:error:");
		private static readonly IntPtr SelSetMeteringEnabled = ObjC.Sel("setMeteringEnabled:");
		private static readonly IntPtr SelPrepareToRecord = ObjC.Sel("prepareToRecord");
		private static readonly IntPtr SelRecord = ObjC.Sel("record");
		private static readonly IntPtr SelStop = ObjC.Sel("stop");
		private static readonly IntPtr SelUpdateMeters = ObjC.Sel("updateMeters");
		private static readonly IntPtr SelAveragePowerForChannel = ObjC.Sel("averagePowerForChannel:");
		private static readonly IntPtr SelLocalizedDescription = ObjC.Sel("localizedDescription");

		private static readonly object PermissionLock = new object();
		private static TaskCompletionSource<bool> pendingPermission;
		private static IntPtr permissionBlock;

		// Guards the native recorder: Level reads it from the UI thread while stop/cancel release it.
		private readonly object nativeLock = new object();
		private IntPtr recorder;
		private string filePath;

		public override bool IsAvailable => true;

		/// <summary>The current AVAuthorizationStatus for audio capture. Never prompts.</summary>
		internal static long AuthorizationStatus()
		{
			EnsureLoaded();
			return Send_q_r(ObjC.Class("AVCaptureDevice"), SelAuthorizationStatusForMediaType, AVString("AVMediaTypeAudio"));
		}

		protected override async Task StartCaptureAsync()
		{
			EnsureLoaded();

			long status = AuthorizationStatus();
			if (status == AuthorizationNotDetermined)
			{
				status = await RequestPermissionAsync() ? AuthorizationAuthorized : AuthorizationDenied;
			}

			if (status != AuthorizationAuthorized)
			{
				throw new MicrophonePermissionDeniedException(
					"Microphone access is turned off. Turn it on in System Settings > Privacy & Security > Microphone"
					+ " (for the app MatterCAD was started from, such as Terminal), then try again.");
			}

			lock (nativeLock)
			{
				StartRecorderLocked();
			}
		}

		protected override Task<RecordedAudio> StopCaptureAsync(TimeSpan duration)
		{
			string path;
			lock (nativeLock)
			{
				path = filePath;
				StopRecorderLocked();
			}

			// AVAudioRecorder finishes the file (sizes in the header) inside -stop, so it is complete here.
			return Task.Run(() =>
			{
				try
				{
					return new RecordedAudio(File.ReadAllBytes(path), "audio/wav", "recording.wav", duration);
				}
				finally
				{
					TryDelete(path);
				}
			});
		}

		protected override void CancelCapture()
		{
			string path;
			lock (nativeLock)
			{
				path = filePath;
				StopRecorderLocked();
			}

			TryDelete(path);
		}

		protected override float ReadLevel()
		{
			lock (nativeLock)
			{
				if (recorder == IntPtr.Zero)
				{
					return 0;
				}

				ObjC.Send_v(recorder, SelUpdateMeters);
				return LevelFromDecibels(Send_f_Q(recorder, SelAveragePowerForChannel, 0));
			}
		}

		private void StartRecorderLocked()
		{
			filePath = Path.Combine(Path.GetTempPath(), $"agg-recording-{Guid.NewGuid():N}.wav");

			IntPtr pool = objc_autoreleasePoolPush();
			try
			{
				IntPtr url = ObjC.Send_r_r(ObjC.Class("NSURL"), SelFileUrlWithPath, ObjC.NSString(filePath));

				// The container comes from the URL's .wav extension; these pick the sample format inside it.
				IntPtr numberClass = ObjC.Class("NSNumber");
				IntPtr[] keys =
				{
					AVString("AVFormatIDKey"),
					AVString("AVSampleRateKey"),
					AVString("AVNumberOfChannelsKey"),
					AVString("AVLinearPCMBitDepthKey"),
					AVString("AVLinearPCMIsFloatKey"),
					AVString("AVLinearPCMIsBigEndianKey"),
				};
				IntPtr[] values =
				{
					ObjC.Send_r_q(numberClass, SelNumberWithLongLong, FormatLinearPcm),
					ObjC.Send_r_d(numberClass, SelNumberWithDouble, SampleRate),
					ObjC.Send_r_q(numberClass, SelNumberWithLongLong, 1),
					ObjC.Send_r_q(numberClass, SelNumberWithLongLong, 16),
					ObjC.Send_r_q(numberClass, SelNumberWithLongLong, 0),
					ObjC.Send_r_q(numberClass, SelNumberWithLongLong, 0),
				};
				IntPtr settings = Dictionary(values, keys);

				IntPtr created = Send_r_r_r_outr(ObjC.Alloc(ObjC.Class("AVAudioRecorder")), SelInitWithUrlSettingsError, url, settings, out IntPtr error);
				if (created == IntPtr.Zero)
				{
					string reason = error == IntPtr.Zero ? "unknown error" : ObjC.FromNSString(ObjC.Send_r(error, SelLocalizedDescription));
					throw new InvalidOperationException($"Could not set up the microphone recorder: {reason}");
				}

				recorder = created;
				ObjC.Send_v_B(recorder, SelSetMeteringEnabled, ObjC.YES);
				if (ObjC.Send_B(recorder, SelPrepareToRecord) == ObjC.NO || ObjC.Send_B(recorder, SelRecord) == ObjC.NO)
				{
					StopRecorderLocked();
					throw new InvalidOperationException("The microphone could not start recording. Check that an input device is connected.");
				}
			}
			catch
			{
				TryDelete(filePath);
				throw;
			}
			finally
			{
				objc_autoreleasePoolPop(pool);
			}
		}

		private void StopRecorderLocked()
		{
			if (recorder != IntPtr.Zero)
			{
				ObjC.Send_v(recorder, SelStop);
				ObjC.Release(recorder);
				recorder = IntPtr.Zero;
			}
		}

		private static unsafe IntPtr Dictionary(IntPtr[] values, IntPtr[] keys)
		{
			fixed (IntPtr* valuePointer = values)
			fixed (IntPtr* keyPointer = keys)
			{
				return Send_r_r_r_Q(ObjC.Class("NSDictionary"), SelDictionaryWithObjects, (IntPtr)valuePointer, (IntPtr)keyPointer, (ulong)keys.Length);
			}
		}

		/// <summary>
		/// One of AVFoundation's exported NSString constants (AVMediaTypeAudio, AVFormatIDKey, ...). Read from
		/// the symbol rather than spelled as a literal: the values are documented only by name.
		/// </summary>
		private static IntPtr AVString(string symbol)
			=> Marshal.ReadIntPtr(NativeLibrary.GetExport(NativeLibrary.Load(AVFoundation), symbol));

		/// <summary>
		/// +[AVCaptureDevice requestAccessForMediaType:completionHandler:], awaited. The handler has to be an
		/// Objective-C block; with no binding library this builds a global block by hand - the literal layout
		/// from the Clang block ABI, invoking an unmanaged-callable static. A global block is never copied or
		/// freed by the runtime, so it is built once and kept, and requests are serialized through one pending
		/// task source.
		/// </summary>
		private static Task<bool> RequestPermissionAsync()
		{
			lock (PermissionLock)
			{
				if (pendingPermission != null)
				{
					return pendingPermission.Task;
				}

				pendingPermission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				Task<bool> result = pendingPermission.Task;
				ObjC.Send_v_r_r(ObjC.Class("AVCaptureDevice"), SelRequestAccessForMediaType, AVString("AVMediaTypeAudio"), PermissionBlock());
				return result;
			}
		}

		private static unsafe IntPtr PermissionBlock()
		{
			if (permissionBlock == IntPtr.Zero)
			{
				const int blockIsGlobal = 1 << 28;

				// struct Block_descriptor { unsigned long reserved; unsigned long size; }
				IntPtr descriptor = Marshal.AllocHGlobal(2 * sizeof(ulong));
				Marshal.WriteInt64(descriptor, 0, 0);
				Marshal.WriteInt64(descriptor, 8, 32);

				// struct Block_literal { void *isa; int flags; int reserved; void *invoke; Block_descriptor *descriptor; }
				IntPtr block = Marshal.AllocHGlobal(32);
				IntPtr isa = NativeLibrary.GetExport(NativeLibrary.Load("/usr/lib/libSystem.B.dylib"), "_NSConcreteGlobalBlock");
				Marshal.WriteIntPtr(block, 0, isa);
				Marshal.WriteInt32(block, 8, blockIsGlobal);
				Marshal.WriteInt32(block, 12, 0);
				Marshal.WriteIntPtr(block, 16, (IntPtr)(delegate* unmanaged<IntPtr, byte, void>)&OnPermissionAnswered);
				Marshal.WriteIntPtr(block, 24, descriptor);
				permissionBlock = block;
			}

			return permissionBlock;
		}

		// Called by AVFoundation on one of its own queues, not the thread that asked.
		[UnmanagedCallersOnly]
		private static void OnPermissionAnswered(IntPtr block, byte granted)
		{
			TaskCompletionSource<bool> answered;
			lock (PermissionLock)
			{
				answered = pendingPermission;
				pendingPermission = null;
			}

			answered?.TrySetResult(granted != ObjC.NO);
		}

		private static void TryDelete(string path)
		{
			try
			{
				if (path != null && File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch (IOException)
			{
				// A leftover temp file is harmless; the recording itself already succeeded or was discarded.
			}
		}

		/// <summary>-(NSInteger)selector:(id) - +[AVCaptureDevice authorizationStatusForMediaType:].</summary>
		[DllImport(ObjC.LibObjC, EntryPoint = "objc_msgSend")]
		private static extern long Send_q_r(IntPtr receiver, IntPtr selector, IntPtr arg0);

		/// <summary>-(float)selector:(NSUInteger) - -[AVAudioRecorder averagePowerForChannel:].</summary>
		[DllImport(ObjC.LibObjC, EntryPoint = "objc_msgSend")]
		private static extern float Send_f_Q(IntPtr receiver, IntPtr selector, ulong arg0);

		/// <summary>+[NSDictionary dictionaryWithObjects:forKeys:count:]</summary>
		[DllImport(ObjC.LibObjC, EntryPoint = "objc_msgSend")]
		private static extern IntPtr Send_r_r_r_Q(IntPtr receiver, IntPtr selector, IntPtr arg0, IntPtr arg1, ulong arg2);

		/// <summary>-[AVAudioRecorder initWithURL:settings:error:]</summary>
		[DllImport(ObjC.LibObjC, EntryPoint = "objc_msgSend")]
		private static extern IntPtr Send_r_r_r_outr(IntPtr receiver, IntPtr selector, IntPtr arg0, IntPtr arg1, out IntPtr error);
	}
}
