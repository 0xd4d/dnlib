// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;

namespace dnlib.PE {
	static class ProcessorArchUtils {
		static Machine cachedMachine = 0;

		public static Machine GetProcessCpuArchitecture() {
			if (cachedMachine == 0)
				cachedMachine = GetProcessCpuArchitectureCore();
			return cachedMachine;
		}

		static Machine GetProcessCpuArchitectureCore() {
			bool isWindows = true;//TODO:
			if (isWindows && TryGetProcessCpuArchitecture_Windows(out var machine))
				return machine;

			Debug.WriteLine("Couldn't detect CPU arch, assuming x86 or x64");
			return IntPtr.Size == 4 ? Machine.I386 : Machine.AMD64;
		}

		static bool TryGetProcessCpuArchitecture_Windows(out Machine machine) {
			//TODO: We shouldn't trust the environment
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
