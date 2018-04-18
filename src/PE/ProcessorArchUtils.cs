// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Reflection;

namespace dnlib.PE {
	static class ProcessorArchUtils {
		static Machine cachedMachine = 0;

		public static Machine GetProcessCpuArchitecture() {
			if (cachedMachine == 0)
				cachedMachine = GetProcessCpuArchitectureCore();
			return cachedMachine;
		}

		static class RuntimeInformationUtils {
#if NETSTANDARD2_0
			public static bool TryGet_RuntimeInformation_Architecture(out Machine machine) =>
				TryGetArchitecture((int)System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture, out machine);
#else
			// .NET Framework 4.7.1: mscorlib
			static Assembly RuntimeInformationAssembly => typeof(object).Assembly;
			static Type System_Runtime_InteropServices_RuntimeInformation => RuntimeInformationAssembly.GetType("System.Runtime.InteropServices.RuntimeInformation", throwOnError: false);

			public static bool TryGet_RuntimeInformation_Architecture(out Machine machine) {
				machine = 0;
				var processArchitectureMethod = System_Runtime_InteropServices_RuntimeInformation?.GetMethod("get_ProcessArchitecture", Array2.Empty<Type>());
				if ((object)processArchitectureMethod == null)
					return false;

				var result = processArchitectureMethod.Invoke(null, Array2.Empty<object>());
				return TryGetArchitecture((int)result, out machine);
			}
#endif

			static bool TryGetArchitecture(int architecture, out Machine machine) {
				switch (architecture) {
				case 0: // Architecture.X86
					Debug.Assert(IntPtr.Size == 4);
					machine = Machine.I386;
					return true;

				case 1: // Architecture.X64
					Debug.Assert(IntPtr.Size == 8);
					machine = Machine.AMD64;
					return true;

				case 2: // Architecture.Arm
					Debug.Assert(IntPtr.Size == 4);
					machine = Machine.ARMNT;
					return true;

				case 3: // Architecture.Arm64
					Debug.Assert(IntPtr.Size == 8);
					machine = Machine.ARM64;
					return true;

				default:
					machine = 0;
					return false;
				}
			}
		}

		static Machine GetProcessCpuArchitectureCore() {
			if (RuntimeInformationUtils.TryGet_RuntimeInformation_Architecture(out var machine))
				return machine;

			bool isWindows = true;//TODO:
			if (isWindows && TryGetProcessCpuArchitecture_Windows(out machine))
				return machine;

			Debug.WriteLine("Couldn't detect CPU arch, assuming x86 or x64");
			return IntPtr.Size == 4 ? Machine.I386 : Machine.AMD64;
		}

		static bool TryGetProcessCpuArchitecture_Windows(out Machine machine) {
			string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
			if (arch != null) {
				// https://msdn.microsoft.com/en-us/library/aa384274.aspx ("WOW64 Implementation Details / Environment Variables")
				switch (arch.ToUpperInvariant()) {
				case "AMD64":
					if (IntPtr.Size == 8) {
						machine = Machine.AMD64;
						return true;
					}
					Debug.Fail($"Bad PROCESSOR_ARCHITECTURE env var");
					break;

				case "X86":
					if (IntPtr.Size == 4) {
						machine = Machine.I386;
						return true;
					}
					Debug.Fail($"Bad PROCESSOR_ARCHITECTURE env var");
					break;

				case "IA64":
					if (IntPtr.Size == 8) {
						machine = Machine.IA64;
						return true;
					}
					Debug.Fail($"Bad PROCESSOR_ARCHITECTURE env var");
					break;

				//TODO: This string hasn't been tested
				case "ARM":
					if (IntPtr.Size == 4) {
						machine = Machine.ARMNT;
						return true;
					}
					Debug.Fail($"Bad PROCESSOR_ARCHITECTURE env var");
					break;

				case "ARM64":
					if (IntPtr.Size == 8) {
						machine = Machine.ARM64;
						return true;
					}
					Debug.Fail($"Bad PROCESSOR_ARCHITECTURE env var");
					break;
				}
			}

			machine = default;
			return false;
		}
	}
}
