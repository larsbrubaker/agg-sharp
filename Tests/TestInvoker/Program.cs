using Xunit;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace TestInvoker
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class ChildProcessTestAttribute : FactAttribute
	{
		// This attribute simply marks a test to be run in a child process
		// The actual logic is handled by the ChildProcessTestRunner helper
	}

	public static class ChildProcessTestRunner
	{
		/// <summary>
		/// Call this method at the beginning of any test method decorated with [ChildProcessTest].
		/// It will either set up the child process environment or execute the test in a child process.
		/// 
		/// Example usage:
		/// [ChildProcessTest]
		/// public void MyTest()
		/// {
		///     ChildProcessTestRunner.RunTest(() =>
		///     {
		///         // Your test code here - this will run in a child process with STA thread
		///         var form = new Form();
		///         Assert.NotNull(form);
		///         // ... test Windows Forms code
		///     });
		/// }
		/// </summary>
		/// <param name="testAction">The test method to execute</param>
		/// <param name="testMethodName">Optional: the name of the test method (will be auto-detected if not provided)</param>
		public static void RunTest(Action testAction, [System.Runtime.CompilerServices.CallerMemberName] string testMethodName = "")
		{
			if (Program.InChildProcess)
			{
				// We're in the child process - set up STA thread and run the test
				Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
				SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
				
				// Execute the test action
				testAction();
			}
			else
			{
				// We're in the parent process - execute the test in a child process
				var stackTrace = new StackTrace();
				var callingMethod = stackTrace.GetFrame(1)?.GetMethod() as MethodInfo;
				if (callingMethod == null)
					throw new InvalidOperationException("Could not determine calling method");

				ExecuteInChildProcess(callingMethod);
			}
		}

		private static void ExecuteInChildProcess(MethodInfo methodInfo)
		{
			string output;
			using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
			using (var pipeSense = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
			{
				var type = methodInfo.DeclaringType;
				var psi = new ProcessStartInfo();
				psi.FileName = "TestInvoker";
				psi.ArgumentList.Add(pipeServer.GetClientHandleAsString());
				psi.ArgumentList.Add(pipeSense.GetClientHandleAsString());
				psi.ArgumentList.Add(type!.Assembly.Location);
				psi.ArgumentList.Add(type.FullName!);
				psi.ArgumentList.Add(methodInfo.Name);
				psi.UseShellExecute = false;
				psi.CreateNoWindow = true;
				using var proc = Process.Start(psi);
				pipeServer.DisposeLocalCopyOfClientHandle();
				pipeSense.DisposeLocalCopyOfClientHandle();
				using (var pipeReader = new StreamReader(pipeServer))
				{
					output = pipeReader.ReadToEnd();
				}
                
				proc!.WaitForExit();
			}

			if (output.Length <= 0)
			{
				throw new Exception("Test child process did not return a result.");
			}

			try
			{
				var xmlserializer = new XmlSerializer(typeof(FakeTestResult));
				var fakeResult = (FakeTestResult)xmlserializer.Deserialize(XmlReader.Create(new StringReader(output)))!;
				
				// If the child process test failed, throw an exception to fail the parent test
				if (fakeResult.FailCount > 0)
				{
					throw new Exception($"Child process test failed: {fakeResult.Message}\n{fakeResult.StackTrace}");
				}
				// If the test passed, we don't throw an exception, allowing the parent test to pass
			}
			catch (Exception ex) when (!(ex.Message.StartsWith("Child process test failed:")))
			{
				throw new Exception("Test child process failed to run the test.", ex);
			}
		}
	}
	
	public class Program
	{
		public static bool InChildProcess { get; private set; } = false;

		[STAThread]
		static int Main(string[] args)
		{
			InChildProcess = true;

			// Terminate this process if the parent exits.
			Task.Run(() =>
			{
				try
				{
					using (var pipeSense = new AnonymousPipeClientStream(PipeDirection.In, args[1]))
					{
						while (pipeSense.ReadByte() >= 0);
					}
				}
				finally
				{
                    Environment.Exit(5);
				}
			});

			using var pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, args[0]);

			string assemblyPath = args[2];
			string typeName = args[3];
			string methodName = args[4];

			Assembly asm = Assembly.LoadFrom(assemblyPath!);
            Assert.NotNull(asm);

			Type? testType = asm.GetType(typeName);
			Assert.NotNull(testType);
			
			MethodInfo? methodInfo = testType.GetMethod(methodName);
            Assert.NotNull(methodInfo);

			try
			{
				// Create test instance
				var testInstance = Activator.CreateInstance(testType);
				Assert.NotNull(testInstance);

				// Execute the test method
				var result = ExecuteTestMethod(testInstance, methodInfo);

				// Serialize result
				XmlSerializer xmlserializer = new(typeof(FakeTestResult));
				XmlWriterSettings settings = new();
				using (XmlWriter writer = XmlWriter.Create(pipeClient, settings))
				{
					xmlserializer.Serialize(writer, result);
				}
				
				return result.FailCount > 0 ? 1 : 0;
			}
			catch (Exception ex)
			{
				// Create failed result
				var failedResult = new FakeTestResult
				{
					ResultState = new FakeTestResultStatus
					{
						Status = XunitTestStatus.Failed,
						Label = "Failed",
						Site = XunitFailureSite.Test
					},
					Name = methodName,
					FullName = $"{typeName}.{methodName}",
					Duration = 0,
					StartTime = DateTime.Now,
					EndTime = DateTime.Now,
					Message = ex.Message,
					StackTrace = ex.StackTrace,
					TotalCount = 1,
					AssertCount = 0,
					FailCount = 1,
					WarningCount = 0,
					PassCount = 0,
					SkipCount = 0,
					InconclusiveCount = 0,
					Output = "",
					AssertionResults = new List<FakeTestResultAssertionResult>()
				};

				XmlSerializer xmlserializer = new(typeof(FakeTestResult));
				XmlWriterSettings settings = new();
				using (XmlWriter writer = XmlWriter.Create(pipeClient, settings))
				{
					xmlserializer.Serialize(writer, failedResult);
				}
				
				return 1;
			}
		}

		private static FakeTestResult ExecuteTestMethod(object testInstance, MethodInfo methodInfo)
		{
			var startTime = DateTime.Now;
			var stopwatch = Stopwatch.StartNew();
			
			try
			{
				// Execute any setup methods (xUnit constructor)
				// The constructor has already been called when creating the instance
				
				// Execute the test method
				methodInfo.Invoke(testInstance, null);
				
				stopwatch.Stop();
				var endTime = DateTime.Now;
				
				return new FakeTestResult
				{
					ResultState = new FakeTestResultStatus
					{
						Status = XunitTestStatus.Passed,
						Label = "Passed",
						Site = XunitFailureSite.Test
					},
					Name = methodInfo.Name,
					FullName = $"{methodInfo.DeclaringType!.FullName}.{methodInfo.Name}",
					Duration = stopwatch.Elapsed.TotalSeconds,
					StartTime = startTime,
					EndTime = endTime,
					Message = null,
					StackTrace = null,
					TotalCount = 1,
					AssertCount = 1, // Assume at least one assertion
					FailCount = 0,
					WarningCount = 0,
					PassCount = 1,
					SkipCount = 0,
					InconclusiveCount = 0,
					Output = "",
					AssertionResults = new List<FakeTestResultAssertionResult>()
				};
			}
			catch (Exception ex)
			{
				stopwatch.Stop();
				var endTime = DateTime.Now;
				
				// Handle xUnit exceptions
				var innerEx = ex.InnerException ?? ex;
				
				return new FakeTestResult
				{
					ResultState = new FakeTestResultStatus
					{
						Status = XunitTestStatus.Failed,
						Label = "Failed",
						Site = XunitFailureSite.Test
					},
					Name = methodInfo.Name,
					FullName = $"{methodInfo.DeclaringType!.FullName}.{methodInfo.Name}",
					Duration = stopwatch.Elapsed.TotalSeconds,
					StartTime = startTime,
					EndTime = endTime,
					Message = innerEx.Message,
					StackTrace = innerEx.StackTrace,
					TotalCount = 1,
					AssertCount = 1,
					FailCount = 1,
					WarningCount = 0,
					PassCount = 0,
					SkipCount = 0,
					InconclusiveCount = 0,
					Output = "",
					AssertionResults = new List<FakeTestResultAssertionResult>
					{
						new FakeTestResultAssertionResult
						{
							Status = XunitAssertionStatus.Failed,
							Message = innerEx.Message,
							StackTrace = innerEx.StackTrace
						}
					}
				};
			}
		}
	}

	// xUnit equivalent enums
	public enum XunitTestStatus
	{
		Inconclusive,
		Skipped,
		Passed,
		Warning,
		Failed
	}

	public enum XunitFailureSite
	{
		Test,
		SetUp,
		TearDown,
		Parent,
		Child
	}

	public enum XunitAssertionStatus
	{
		Passed,
		Failed,
		Warning,
		Inconclusive
	}

	public struct FakeTestResultStatus
	{
		public XunitTestStatus Status;
		public string Label;
		public XunitFailureSite Site;
	}

	public struct FakeTestResultAssertionResult
	{
		public XunitAssertionStatus Status;
		public string? Message;
		public string? StackTrace;
	}

	[Serializable]
	public struct FakeTestResult
	{
		public FakeTestResultStatus ResultState;
		public string Name;
		public string FullName;
		public double Duration;
		public DateTime StartTime;
		public DateTime EndTime;
		public string? Message;
		public string? StackTrace;
		public int TotalCount;
		public int AssertCount;
		public int FailCount;
		public int WarningCount;
		public int PassCount;
		public int SkipCount;
		public int InconclusiveCount;
		public string Output;
		public List<FakeTestResultAssertionResult> AssertionResults;
	}
}