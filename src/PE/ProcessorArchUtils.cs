// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace dnlib.PE {
	static class ProcessorArchUtils {
		static Machine cachedMachine = 0;

		public static Machine GetProcessCpuArchitecture() {
			if (cachedMachine == 0)
				cachedMachine = GetProcessCpuArchitectureCore();
			return cachedMachine;
		}

		static class RuntimeInformationUtils {
#if NETSTANDARD
			public static bool TryGet_RuntimeInformation_Architecture(out Machine machine) =>
				TryGetArchitecture((int)RuntimeInformation.ProcessArchitecture, out machine);
#else
			static Assembly RuntimeInformationAssembly => typeof(object).Assembly;
			static Type System_Runtime_InteropServices_RuntimeInformation => RuntimeInformationAssembly.GetType("System.Runtime.InteropServices.RuntimeInformation", throwOnError: false);

			public static bool TryGet_RuntimeInformation_Architecture(out Machine machine) {
				machine = 0;
				var processArchitectureMethod = System_Runtime_InteropServices_RuntimeInformation?.GetMethod("get_ProcessArchitecture", Array2.Empty<Type>());
				if (processArchitectureMethod is null)
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
					Debug.Fail($"Unknown process architecture: {architecture}");
					machine = 0;
					return false;
				}
			}
		}

		static Machine GetProcessCpuArchitectureCore() {
			if (WindowsUtils.TryGetProcessCpuArchitecture(out var machine))
				return machine;
			try {
				if (RuntimeInformationUtils.TryGet_RuntimeInformation_Architecture(out machine))
					return machine;
			}
			catch (PlatformNotSupportedException) {
			}

			Debug.WriteLine("Couldn't detect CPU arch, assuming x86 or x64");
			return IntPtr.Size == 4 ? Machine.I386 : Machine.AMD64;
		}

		static class WindowsUtils {
			[DllImport("kernel32")]
			static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

			struct SYSTEM_INFO {
				public ushort wProcessorArchitecture;
				public ushort wReserved;
				public uint dwPageSize;
				public IntPtr lpMinimumApplicationAddress;
				public IntPtr lpMaximumApplicationAddress;
				public IntPtr dwActiveProcessorMask;
				public uint dwNumberOfProcessors;
				public uint dwProcessorType;
				public uint dwAllocationGranularity;
				public ushort wProcessorLevel;
				public ushort wProcessorRevision;
			}

			enum ProcessorArchitecture : ushort {
				INTEL		= 0,
				ARM			= 5,
				IA64		= 6,
				AMD64		= 9,
				ARM64		= 12,
				UNKNOWN		= 0xFFFF,
			}

			public static bool TryGetProcessCpuArchitecture(out Machine machine) {
				if (canTryGetSystemInfo) {
					try {
						GetSystemInfo(out var sysInfo);
						switch ((ProcessorArchitecture)sysInfo.wProcessorArchitecture) {
						case ProcessorArchitecture.INTEL:
							Debug.Assert(IntPtr.Size == 4);
							machine = Machine.I386;
							return true;

						case ProcessorArchitecture.ARM:
							Debug.Assert(IntPtr.Size == 4);
							machine = Machine.ARMNT;
							return true;

						case ProcessorArchitecture.IA64:
							Debug.Assert(IntPtr.Size == 8);
							machine = Machine.IA64;
							return true;

						case ProcessorArchitecture.AMD64:
							Debug.Assert(IntPtr.Size == 8);
							machine = Machine.AMD64;
							return true;

						case ProcessorArchitecture.ARM64:
							Debug.Assert(IntPtr.Size == 8);
							machine = Machine.ARM64;
							return true;

						case ProcessorArchitecture.UNKNOWN:
						default:
							break;
						}
					}
					catch (EntryPointNotFoundException) {
						canTryGetSystemInfo = false;
					}
					catch (DllNotFoundException) {
						canTryGetSystemInfo = false;
					}
				}

				machine = 0;
				return false;
			}
			static bool canTryGetSystemInfo = true;
		}
	}
}
